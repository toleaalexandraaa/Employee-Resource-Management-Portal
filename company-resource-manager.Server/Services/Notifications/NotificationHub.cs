using company_resource_manager.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace company_resource_manager.Server.Services.Notifications;

[Authorize]
public class NotificationHub : Hub
{
    private readonly INotificationService _notificationService;

    public NotificationHub(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called by the Angular client to mark a specific notification as read.
    /// </summary>
    public async Task MarkRead(Guid notificationId)
    {
        var userId = Guid.Parse(Context.UserIdentifier!);
        await _notificationService.MarkReadAsync(notificationId, userId);
    }
}
