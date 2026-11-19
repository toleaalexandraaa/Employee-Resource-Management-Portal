using company_resource_manager.Server.Models.Common;

namespace company_resource_manager.Server.Models.Hr;

public class Employee : AuditableEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PersonalEmail { get; set; } = string.Empty;
    public string WorkEmail { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public DateOnly BirthDate { get; set; }
    public DateOnly HireDate { get; set; }
    public string Department { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public Guid? ManagerId { get; set; }
    public float SeniorityYears { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public Employee? Manager { get; set; }
    public ICollection<Employee> DirectReports { get; set; } = [];
    public ICollection<EmployeeDocument> Documents { get; set; } = [];
    public ICollection<EmploymentContract> Contracts { get; set; } = [];
    public ICollection<LeaveRequest> LeaveRequests { get; set; } = [];
    public ICollection<LeaveBalance> LeaveBalances { get; set; } = [];
    public ICollection<PayrollRecord> PayrollRecords { get; set; } = [];
}
