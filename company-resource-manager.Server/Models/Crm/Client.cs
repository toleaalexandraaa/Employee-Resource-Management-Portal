using company_resource_manager.Server.Models.Common;

namespace company_resource_manager.Server.Models.Crm;

public class Client : AuditableEntity
{
    public string CompanyName { get; set; } = string.Empty;
    public string? Industry { get; set; }
    public string? Website { get; set; }
    public string? Address { get; set; }
    public string? Country { get; set; }
    public Guid AccountManagerId { get; set; }
    public ClientStatus Status { get; set; } = ClientStatus.Active;

    public ICollection<ClientContact> Contacts { get; set; } = [];
    public ICollection<CrmContract> Contracts { get; set; } = [];
    public ICollection<Invoice> Invoices { get; set; } = [];
}
