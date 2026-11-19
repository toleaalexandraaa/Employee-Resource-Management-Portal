using company_resource_manager.Server.Models.Common;

namespace company_resource_manager.Server.Models.Crm;

public class CrmContract : AuditableEntity
{
    public Guid ClientId { get; set; }
    public Guid? ProjectId { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public string Currency { get; set; } = "RON";
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public DateOnly? SignedDate { get; set; }
    public ContractStatus Status { get; set; } = ContractStatus.Draft;
    public string? DocumentPath { get; set; }

    public Client Client { get; set; } = null!;
}
