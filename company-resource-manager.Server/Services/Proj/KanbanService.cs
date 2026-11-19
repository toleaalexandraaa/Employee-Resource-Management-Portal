using company_resource_manager.Server.Data;
using company_resource_manager.Server.DTOs.Proj;
using company_resource_manager.Server.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace company_resource_manager.Server.Services.Proj;

public class KanbanService : IKanbanService
{
    private readonly AppDbContext _db;
    private readonly ITaskService _taskService;

    public KanbanService(AppDbContext db, ITaskService taskService)
    {
        _db = db;
        _taskService = taskService;
    }

    public async Task<KanbanBoardDto?> GetBoardAsync(Guid projectId)
    {
        var board = await _db.KanbanBoards.AsNoTracking()
            .Include(b => b.Project)
            .Include(b => b.Columns)
                .ThenInclude(c => c.Tasks)
            .FirstOrDefaultAsync(b => b.ProjectId == projectId);

        if (board is null) return null;

        // Auto-fix orphaned tasks (KanbanColumnId is null) by assigning them to the first column
        var backlogColumn = board.Columns.OrderBy(c => c.Order).FirstOrDefault();
        if (backlogColumn is not null)
        {
            var orphanedTasks = await _db.Tasks
                .Where(t => t.ProjectId == projectId && t.KanbanColumnId == null)
                .ToListAsync();

            if (orphanedTasks.Any())
            {
                var maxSort = backlogColumn.Tasks.Any()
                    ? backlogColumn.Tasks.Max(t => t.SortOrder)
                    : -1;

                foreach (var task in orphanedTasks)
                {
                    task.KanbanColumnId = backlogColumn.Id;
                    task.SortOrder = ++maxSort;
                    backlogColumn.Tasks.Add(task);
                }
                await _db.SaveChangesAsync();
            }
        }

        var employeeIds = board.Columns
            .SelectMany(c => c.Tasks)
            .Where(t => t.AssignedToEmployeeId.HasValue)
            .Select(t => t.AssignedToEmployeeId!.Value)
            .Distinct().ToList();

        var employees = employeeIds.Any()
            ? await _db.Employees.AsNoTracking()
                .Where(e => employeeIds.Contains(e.Id))
                .ToDictionaryAsync(e => e.Id, e => e.FirstName + " " + e.LastName)
            : new Dictionary<Guid, string>();

        var columns = board.Columns
            .OrderBy(c => c.Order)
            .Select(c => new KanbanColumnDto(
                c.Id, c.Name, c.Order, c.WipLimit,
                c.Tasks
                    .OrderBy(t => t.SortOrder)
                    .Select(t => new ProjectTaskDto(
                        t.Id, t.ProjectId, board.Project.Name,
                        t.Title, t.Description,
                        t.AssignedToEmployeeId,
                        t.AssignedToEmployeeId.HasValue
                            ? employees.GetValueOrDefault(t.AssignedToEmployeeId.Value)
                            : null,
                        t.Priority, t.Status, t.EstimatedHours,
                        t.DueDate, t.KanbanColumnId, c.Name, t.SortOrder))
                    .ToList()))
            .ToList();

        return new KanbanBoardDto(board.Id, projectId, board.Project.Name, columns);
    }

    public async Task ReorderColumnsAsync(Guid projectId, ReorderColumnsRequest request)
    {
        var board = await _db.KanbanBoards
            .Include(b => b.Columns)
            .FirstOrDefaultAsync(b => b.ProjectId == projectId);
        if (board is null) return;

        foreach (var item in request.Columns)
        {
            var column = board.Columns.FirstOrDefault(c => c.Id == item.ColumnId);
            if (column is not null)
                column.Order = item.NewOrder;
        }
        await _db.SaveChangesAsync();
    }
}
