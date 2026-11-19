using company_resource_manager.Server.Models.Common;

namespace company_resource_manager.Server.Models.Hr;

public class LeaveRequest : AuditableEntity
{
    public Guid EmployeeId { get; set; }
    public LeaveType LeaveType { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int WorkingDaysCount { get; set; }
    public string? Reason { get; set; }
    public LeaveStatus Status { get; set; } = LeaveStatus.Pending;
    public Guid? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }

    public Employee Employee { get; set; } = null!;
}
