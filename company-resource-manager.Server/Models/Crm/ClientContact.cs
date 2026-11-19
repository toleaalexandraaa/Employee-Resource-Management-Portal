using company_resource_manager.Server.Models.Common;

namespace company_resource_manager.Server.Models.Crm;

public class ClientContact : AuditableEntity
{
    public Guid ClientId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? JobTitle { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public bool IsPrimary { get; set; }

    public Client Client { get; set; } = null!;
}
