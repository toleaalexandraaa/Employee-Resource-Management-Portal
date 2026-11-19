using company_resource_manager.Server.Models.Common;

namespace company_resource_manager.Server.Models.Proj;

public class KanbanColumn : AuditableEntity
{
    public Guid BoardId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Order { get; set; }
    public int? WipLimit { get; set; }

    public KanbanBoard Board { get; set; } = null!;
    public ICollection<ProjectTask> Tasks { get; set; } = [];
}
