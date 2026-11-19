using company_resource_manager.Server.Models.Common;

namespace company_resource_manager.Server.Models.Proj;

public class TimeEntry : AuditableEntity
{
    public Guid EmployeeId { get; set; }
    public Guid? TaskId { get; set; }
    public Guid ProjectId { get; set; }
    public DateOnly Date { get; set; }
    public decimal HoursLogged { get; set; }
    public string? Description { get; set; }

    public Project Project { get; set; } = null!;
    public ProjectTask? Task { get; set; }
}
