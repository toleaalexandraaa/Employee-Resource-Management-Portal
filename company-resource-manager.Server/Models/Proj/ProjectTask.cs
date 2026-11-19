using company_resource_manager.Server.Models.Common;

namespace company_resource_manager.Server.Models.Proj;

public class ProjectTask : AuditableEntity
{
    public Guid ProjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? AssignedToEmployeeId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public TaskStatus Status { get; set; } = TaskStatus.Backlog;
    public decimal EstimatedHours { get; set; }
    public DateOnly? DueDate { get; set; }
    public Guid? KanbanColumnId { get; set; }
    public int SortOrder { get; set; }

    public Project Project { get; set; } = null!;
    public KanbanColumn? KanbanColumn { get; set; }
    public ICollection<TimeEntry> TimeEntries { get; set; } = [];
}
