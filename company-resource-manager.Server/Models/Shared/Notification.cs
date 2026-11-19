using company_resource_manager.Server.Models.Common;

namespace company_resource_manager.Server.Models.Shared;

public class Notification : AuditableEntity
{
    public Guid RecipientUserId { get; set; }
    public NotificationType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsRead { get; set; }
}
