using company_resource_manager.Server.Models.Common;

namespace company_resource_manager.Server.Models.Proj;

public class Project : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ClientId { get; set; }
    public Guid ProjectManagerId { get; set; }
    public ProjectStatus Status { get; set; } = ProjectStatus.Planning;
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public decimal EstimatedBudget { get; set; }
    public decimal ActualBudget { get; set; }
    public int EstimatedDurationDays { get; set; }

    public ICollection<ProjectMember> Members { get; set; } = [];
    public ICollection<ProjectTask> Tasks { get; set; } = [];
    public ICollection<TimeEntry> TimeEntries { get; set; } = [];
    public KanbanBoard? KanbanBoard { get; set; }
}
