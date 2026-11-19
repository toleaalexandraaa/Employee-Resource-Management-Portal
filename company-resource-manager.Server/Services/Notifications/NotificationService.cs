using company_resource_manager.Server.Data;
using company_resource_manager.Server.DTOs.Notifications;
using company_resource_manager.Server.Models.Shared;
using company_resource_manager.Server.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace company_resource_manager.Server.Services.Notifications;

public class NotificationService : INotificationService
{
    private readonly AppDbContext _db;
    private readonly IHubContext<NotificationHub> _hub;

    public NotificationService(AppDbContext db, IHubContext<NotificationHub> hub)
    {
        _db = db;
        _hub = hub;
    }

    public async Task CreateAsync(Guid recipientUserId, NotificationType type, string title, string body)
    {
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            RecipientUserId = recipientUserId,
            Type = type,
            Title = title,
            Body = body,
            IsRead = false,
        };

        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync();

        var dto = ToDto(notification);
        await _hub.Clients
            .User(recipientUserId.ToString())
            .SendAsync("ReceiveNotification", dto);
    }

    public async Task<IReadOnlyList<NotificationDto>> GetUnreadAsync(Guid userId)
    {
        return await _db.Notifications
            .AsNoTracking()
            .Where(n => n.RecipientUserId == userId && !n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => ToDto(n))
            .ToListAsync();
    }

    public async Task MarkReadAsync(Guid notificationId, Guid userId)
    {
        var notification = await _db.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.RecipientUserId == userId);

        if (notification is null)
            return;

        notification.IsRead = true;
        await _db.SaveChangesAsync();
    }

    public async Task MarkAllReadAsync(Guid userId)
    {
        await _db.Notifications
            .Where(n => n.RecipientUserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
    }

    private static NotificationDto ToDto(Notification n) =>
        new(n.Id, n.Type, n.Title, n.Body, n.IsRead, n.CreatedAt);
}
