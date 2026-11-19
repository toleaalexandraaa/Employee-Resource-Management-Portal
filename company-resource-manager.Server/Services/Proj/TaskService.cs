using company_resource_manager.Server.Data;
using company_resource_manager.Server.DTOs;
using company_resource_manager.Server.DTOs.Proj;
using company_resource_manager.Server.Models.Proj;
using TaskStatus = company_resource_manager.Server.Models.Proj.TaskStatus;
using company_resource_manager.Server.Models.Shared;
using company_resource_manager.Server.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace company_resource_manager.Server.Services.Proj;

public class TaskService : ITaskService
{
    private readonly AppDbContext _db;
    private readonly INotificationService _notifications;
    private readonly UserManager<Models.Identity.ApplicationUser> _userManager;

    public TaskService(
        AppDbContext db,
        INotificationService notifications,
        UserManager<Models.Identity.ApplicationUser> userManager)
    {
        _db = db;
        _notifications = notifications;
        _userManager = userManager;
    }

    public async Task<PagedResult<ProjectTaskDto>> GetAllAsync(
        Guid? projectId, Guid? assigneeId, TaskStatus? status, int page, int pageSize)
    {
        var query = _db.Tasks.AsNoTracking()
            .Include(t => t.Project)
            .Include(t => t.KanbanColumn)
            .AsQueryable();

        if (projectId.HasValue) query = query.Where(t => t.ProjectId == projectId.Value);
        if (assigneeId.HasValue) query = query.Where(t => t.AssignedToEmployeeId == assigneeId.Value);
        if (status.HasValue) query = query.Where(t => t.Status == status.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(t => t.SortOrder)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync();

        return new PagedResult<ProjectTaskDto>(await EnrichTasksAsync(items), total, page, pageSize);
    }

    public async Task<ProjectTaskDto?> GetByIdAsync(Guid id)
    {
        var task = await _db.Tasks.AsNoTracking()
            .Include(t => t.Project)
            .Include(t => t.KanbanColumn)
            .FirstOrDefaultAsync(t => t.Id == id);
        if (task is null) return null;
        return (await EnrichTasksAsync([task])).First();
    }

    public async Task<ProjectTaskDto> CreateAsync(CreateTaskRequest r, Guid createdByUserId)
    {
        var kanbanColumnId = r.KanbanColumnId;
        if (!kanbanColumnId.HasValue)
        {
            var board = await _db.KanbanBoards
                .Include(b => b.Columns)
                .FirstOrDefaultAsync(b => b.ProjectId == r.ProjectId);
            kanbanColumnId = board?.Columns.OrderBy(c => c.Order).FirstOrDefault()?.Id;
        }

        var maxOrder = await _db.Tasks
            .Where(t => t.ProjectId == r.ProjectId && t.KanbanColumnId == kanbanColumnId)
            .Select(t => (int?)t.SortOrder).MaxAsync() ?? -1;

        var task = new ProjectTask
        {
            ProjectId = r.ProjectId,
            Title = r.Title,
            Description = r.Description,
            AssignedToEmployeeId = r.AssignedToEmployeeId,
            CreatedByUserId = createdByUserId,
            Priority = r.Priority,
            Status = TaskStatus.Backlog,
            EstimatedHours = r.EstimatedHours,
            DueDate = r.DueDate,
            KanbanColumnId = kanbanColumnId,
            SortOrder = maxOrder + 1,
        };
        _db.Tasks.Add(task);
        await _db.SaveChangesAsync();

        if (r.AssignedToEmployeeId.HasValue)
            await NotifyAssigneeAsync(r.AssignedToEmployeeId.Value, task.Title);

        return await GetByIdAsync(task.Id) ?? throw new InvalidOperationException("Task not found after creation.");
    }

    public async Task<ProjectTaskDto?> UpdateAsync(Guid id, UpdateTaskRequest r)
    {
        var task = await _db.Tasks.FindAsync(id);
        if (task is null) return null;

        var previousAssignee = task.AssignedToEmployeeId;

        task.Title = r.Title;
        task.Description = r.Description;
        task.AssignedToEmployeeId = r.AssignedToEmployeeId;
        task.Priority = r.Priority;
        task.Status = r.Status;
        task.EstimatedHours = r.EstimatedHours;
        task.DueDate = r.DueDate;

        await _db.SaveChangesAsync();

        if (r.AssignedToEmployeeId.HasValue && r.AssignedToEmployeeId != previousAssignee)
            await NotifyAssigneeAsync(r.AssignedToEmployeeId.Value, task.Title);

        return await GetByIdAsync(id);
    }

    public async Task<ProjectTaskDto?> MoveAsync(Guid id, MoveTaskRequest r)
    {
        var task = await _db.Tasks.FindAsync(id);
        if (task is null) return null;

        task.KanbanColumnId = r.TargetColumnId;
        task.SortOrder = r.NewSortOrder;

        if (r.TargetColumnId.HasValue)
        {
            var column = await _db.KanbanColumns.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == r.TargetColumnId.Value);
            if (column?.Name == "Done")
                task.Status = TaskStatus.Done;
        }

        await _db.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var task = await _db.Tasks.FindAsync(id);
        if (task is null) return false;
        _db.Tasks.Remove(task);
        await _db.SaveChangesAsync();
        return true;
    }

    private async Task NotifyAssigneeAsync(Guid employeeId, string taskTitle)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.EmployeeId == employeeId);
        if (user is not null)
        {
            await _notifications.CreateAsync(
                user.Id,
                NotificationType.TaskAssigned,
                "Task Assigned",
                $"You have been assigned to task: \"{taskTitle}\".");
        }
    }

    private async Task<IReadOnlyList<ProjectTaskDto>> EnrichTasksAsync(IList<ProjectTask> tasks)
    {
        var employeeIds = tasks.Where(t => t.AssignedToEmployeeId.HasValue)
            .Select(t => t.AssignedToEmployeeId!.Value).Distinct().ToList();
        var employees = employeeIds.Any()
            ? await _db.Employees.AsNoTracking()
                .Where(e => employeeIds.Contains(e.Id))
                .ToDictionaryAsync(e => e.Id, e => e.FirstName + " " + e.LastName)
            : new Dictionary<Guid, string>();

        return tasks.Select(t => new ProjectTaskDto(
            t.Id, t.ProjectId, t.Project?.Name ?? string.Empty,
            t.Title, t.Description,
            t.AssignedToEmployeeId,
            t.AssignedToEmployeeId.HasValue ? employees.GetValueOrDefault(t.AssignedToEmployeeId.Value) : null,
            t.Priority, t.Status, t.EstimatedHours, t.DueDate,
            t.KanbanColumnId, t.KanbanColumn?.Name, t.SortOrder)).ToList();
    }
}
