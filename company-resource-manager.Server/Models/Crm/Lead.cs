using company_resource_manager.Server.Models.Common;

namespace company_resource_manager.Server.Models.Crm;

public class Lead : AuditableEntity
{
    public string CompanyName { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string? ContactPhone { get; set; }
    public LeadSource Source { get; set; }
    public LeadStage Stage { get; set; } = LeadStage.New;
    public decimal? EstimatedValue { get; set; }
    public Guid AssignedToUserId { get; set; }
    public string? Notes { get; set; }
    public Guid? ConvertedToClientId { get; set; }
}
