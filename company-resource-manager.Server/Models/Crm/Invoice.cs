using company_resource_manager.Server.Models.Common;

namespace company_resource_manager.Server.Models.Crm;

public class Invoice : AuditableEntity
{
    public Guid ClientId { get; set; }
    public Guid? ContractId { get; set; }
    public Guid? ProjectId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateOnly IssueDate { get; set; }
    public DateOnly DueDate { get; set; }
    public decimal SubTotal { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "RON";
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    public decimal PaidAmount { get; set; }
    public DateTime? PaidAt { get; set; }

    public Client Client { get; set; } = null!;
    public ICollection<InvoiceLineItem> LineItems { get; set; } = [];
}
