using company_resource_manager.Server.Models.Common;

namespace company_resource_manager.Server.Models.Hr;

public class PayrollRecord : AuditableEntity
{
    public Guid EmployeeId { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public decimal GrossSalary { get; set; }
    public decimal BonusAmount { get; set; }
    public int MealTicketCount { get; set; }
    public decimal MealTicketValue { get; set; }
    public decimal NetSalary { get; set; }
    public decimal TaxDeductions { get; set; }
    public decimal SocialContributions { get; set; }
    public DateTime GeneratedAt { get; set; }
    public PayrollStatus Status { get; set; } = PayrollStatus.Draft;

    public Employee Employee { get; set; } = null!;
}
