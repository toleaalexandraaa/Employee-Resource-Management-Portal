using company_resource_manager.Server.Data;
using company_resource_manager.Server.DTOs;
using company_resource_manager.Server.DTOs.Proj;
using company_resource_manager.Server.Models.Proj;
using company_resource_manager.Server.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace company_resource_manager.Server.Services.Proj;

public class ProjectService : IProjectService
{
    private readonly AppDbContext _db;
    private readonly UserManager<Models.Identity.ApplicationUser> _userManager;

    public ProjectService(AppDbContext db, UserManager<Models.Identity.ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<PagedResult<ProjectListItemDto>> GetAllAsync(int page, int pageSize, Guid? memberEmployeeId = null)
    {
        var query = _db.Projects.AsNoTracking();
        if (memberEmployeeId.HasValue)
        {
            var emp = memberEmployeeId.Value;
            var memberProjectIds = await _db.ProjectMembers
                .Where(m => m.EmployeeId == emp)
                .Select(m => m.ProjectId)
                .ToListAsync();
            query = query.Where(p => memberProjectIds.Contains(p.Id));
        }
        var total = await query.CountAsync();
        var items = await query
            .Include(p => p.Members)
            .Include(p => p.Tasks)
            .OrderByDescending(p => p.StartDate)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync();

        var managerIds = items.Select(p => p.ProjectManagerId).Distinct().ToList();
        var managers = await _userManager.Users
            .Where(u => managerIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName);

        var clientIds = items.Where(p => p.ClientId.HasValue).Select(p => p.ClientId!.Value).Distinct().ToList();
        var clients = clientIds.Any()
            ? await _db.Clients.AsNoTracking()
                .Where(c => clientIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => c.CompanyName)
            : [];

        var dtos = items.Select(p => new ProjectListItemDto(
            p.Id, p.Name,
            p.ClientId.HasValue ? clients.GetValueOrDefault(p.ClientId.Value) : null,
            p.Status, p.StartDate, p.EndDate, p.EstimatedBudget,
            p.ProjectManagerId, managers.GetValueOrDefault(p.ProjectManagerId, "Unknown"),
            p.Members.Count, p.Tasks.Count)).ToList();

        return new PagedResult<ProjectListItemDto>(dtos, total, page, pageSize);
    }

    public async Task<ProjectDto?> GetByIdAsync(Guid id)
    {
        var p = await _db.Projects.AsNoTracking()
            .Include(x => x.Members)
            .Include(x => x.Tasks)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (p is null) return null;

        var manager = await _userManager.FindByIdAsync(p.ProjectManagerId.ToString());
        string? clientName = null;
        if (p.ClientId.HasValue)
        {
            var client = await _db.Clients.AsNoTracking().FirstOrDefaultAsync(c => c.Id == p.ClientId.Value);
            clientName = client?.CompanyName;
        }

        return new ProjectDto(p.Id, p.Name, p.Description, p.ClientId, clientName,
            p.ProjectManagerId, manager?.FullName ?? "Unknown",
            p.Status, p.StartDate, p.EndDate, p.EstimatedBudget, p.ActualBudget,
            p.EstimatedDurationDays, p.Members.Count, p.Tasks.Count);
    }

    public async Task<ProjectDto> CreateAsync(CreateProjectRequest r, Guid managerUserId)
    {
        var project = new Project
        {
            Name = r.Name,
            Description = r.Description,
            ClientId = r.ClientId,
            ProjectManagerId = managerUserId,
            Status = r.Status,
            StartDate = r.StartDate,
            EndDate = r.EndDate,
            EstimatedBudget = r.EstimatedBudget,
            EstimatedDurationDays = r.EstimatedDurationDays,
        };
        _db.Projects.Add(project);

        // Auto-create KanbanBoard with 4 default columns
        var board = new KanbanBoard { ProjectId = project.Id };
        _db.KanbanBoards.Add(board);

        var defaultColumns = new[]
        {
            new KanbanColumn { BoardId = board.Id, Name = "Backlog", Order = 0 },
            new KanbanColumn { BoardId = board.Id, Name = "In Progress", Order = 1 },
            new KanbanColumn { BoardId = board.Id, Name = "In Review", Order = 2 },
            new KanbanColumn { BoardId = board.Id, Name = "Done", Order = 3 },
        };
        _db.KanbanColumns.AddRange(defaultColumns);

        await _db.SaveChangesAsync();
        return await GetByIdAsync(project.Id) ?? throw new InvalidOperationException("Project not found after creation.");
    }

    public async Task<ProjectDto?> UpdateAsync(Guid id, UpdateProjectRequest r)
    {
        var project = await _db.Projects.FindAsync(id);
        if (project is null) return null;

        project.Name = r.Name;
        project.Description = r.Description;
        project.ClientId = r.ClientId;
        project.Status = r.Status;
        project.StartDate = r.StartDate;
        project.EndDate = r.EndDate;
        project.EstimatedBudget = r.EstimatedBudget;
        project.ActualBudget = r.ActualBudget;
        project.EstimatedDurationDays = r.EstimatedDurationDays;

        await _db.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var project = await _db.Projects.FindAsync(id);
        if (project is null) return false;
        _db.Projects.Remove(project);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<IReadOnlyList<ProjectMemberDto>> GetMembersAsync(Guid projectId)
    {
        var members = await _db.ProjectMembers.AsNoTracking()
            .Where(m => m.ProjectId == projectId)
            .ToListAsync();

        var employeeIds = members.Select(m => m.EmployeeId).Distinct().ToList();
        var employees = await _db.Employees.AsNoTracking()
            .Where(e => employeeIds.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, e => e.FirstName + " " + e.LastName);

        return members.Select(m => new ProjectMemberDto(
            m.Id, m.ProjectId, m.EmployeeId,
            employees.GetValueOrDefault(m.EmployeeId, "Unknown"),
            m.Role, m.AllocatedHoursPerWeek, m.JoinDate, m.LeaveDate)).ToList();
    }

    public async Task<ProjectMemberDto> AddMemberAsync(Guid projectId, AddProjectMemberRequest r)
    {
        var member = new ProjectMember
        {
            ProjectId = projectId,
            EmployeeId = r.EmployeeId,
            Role = r.Role,
            AllocatedHoursPerWeek = r.AllocatedHoursPerWeek,
            JoinDate = r.JoinDate,
            LeaveDate = r.LeaveDate,
        };
        _db.ProjectMembers.Add(member);
        await _db.SaveChangesAsync();

        var employee = await _db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Id == r.EmployeeId);
        return new ProjectMemberDto(
            member.Id, projectId, r.EmployeeId,
            employee is null ? "Unknown" : employee.FirstName + " " + employee.LastName,
            r.Role, r.AllocatedHoursPerWeek, r.JoinDate, r.LeaveDate);
    }

    public async Task<bool> RemoveMemberAsync(Guid projectId, Guid memberId)
    {
        var member = await _db.ProjectMembers
            .FirstOrDefaultAsync(m => m.ProjectId == projectId && m.Id == memberId);
        if (member is null) return false;
        _db.ProjectMembers.Remove(member);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<IReadOnlyList<GanttTaskDto>> GetGanttAsync(Guid projectId)
    {
        var tasks = await _db.Tasks.AsNoTracking()
            .Where(t => t.ProjectId == projectId)
            .OrderBy(t => t.DueDate)
            .ToListAsync();

        var employeeIds = tasks.Where(t => t.AssignedToEmployeeId.HasValue)
            .Select(t => t.AssignedToEmployeeId!.Value).Distinct().ToList();
        var employees = await _db.Employees.AsNoTracking()
            .Where(e => employeeIds.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, e => e.FirstName + " " + e.LastName);

        return tasks.Select(t => new GanttTaskDto(
            t.Id, t.Title, null, t.DueDate,
            t.AssignedToEmployeeId,
            t.AssignedToEmployeeId.HasValue ? employees.GetValueOrDefault(t.AssignedToEmployeeId.Value) : null,
            t.Status, t.Priority, t.EstimatedHours)).ToList();
    }
}
