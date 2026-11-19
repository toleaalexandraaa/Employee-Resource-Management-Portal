using company_resource_manager.Server.Models.Common;

namespace company_resource_manager.Server.Models.Hr;

public class EmploymentContract : AuditableEntity
{
    public Guid EmployeeId { get; set; }
    public ContractType ContractType { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public decimal GrossSalary { get; set; }
    public string Currency { get; set; } = "RON";
    public bool IsActive { get; set; } = true;

    public Employee Employee { get; set; } = null!;
}
