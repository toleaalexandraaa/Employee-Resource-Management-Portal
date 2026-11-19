using company_resource_manager.Server.Models.Shared;

namespace company_resource_manager.Server.DTOs.Notifications;

public record NotificationDto(
    Guid Id,
    NotificationType Type,
    string Title,
    string Body,
    bool IsRead,
    DateTime CreatedAt
);
