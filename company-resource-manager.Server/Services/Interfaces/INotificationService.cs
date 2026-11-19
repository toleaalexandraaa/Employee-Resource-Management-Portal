using company_resource_manager.Server.DTOs.Notifications;
using company_resource_manager.Server.Models.Shared;

namespace company_resource_manager.Server.Services.Interfaces;

public interface INotificationService
{
    Task CreateAsync(Guid recipientUserId, NotificationType type, string title, string body);
    Task<IReadOnlyList<NotificationDto>> GetUnreadAsync(Guid userId);
    Task MarkReadAsync(Guid notificationId, Guid userId);
    Task MarkAllReadAsync(Guid userId);
}
