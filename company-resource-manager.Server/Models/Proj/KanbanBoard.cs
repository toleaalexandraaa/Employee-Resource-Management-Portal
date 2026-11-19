using company_resource_manager.Server.Models.Common;

namespace company_resource_manager.Server.Models.Proj;

public class KanbanBoard : AuditableEntity
{
    public Guid ProjectId { get; set; }

    public Project Project { get; set; } = null!;
    public ICollection<KanbanColumn> Columns { get; set; } = [];
}
