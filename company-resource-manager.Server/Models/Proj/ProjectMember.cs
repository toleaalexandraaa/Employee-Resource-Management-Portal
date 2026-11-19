using company_resource_manager.Server.Models.Common;

namespace company_resource_manager.Server.Models.Proj;

public class ProjectMember : AuditableEntity
{
    public Guid ProjectId { get; set; }
    public Guid EmployeeId { get; set; }
    public string Role { get; set; } = string.Empty;
    public decimal AllocatedHoursPerWeek { get; set; }
    public DateOnly JoinDate { get; set; }
    public DateOnly? LeaveDate { get; set; }

    public Project Project { get; set; } = null!;
}
