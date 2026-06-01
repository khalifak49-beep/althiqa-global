using HomeMaids.Models;

namespace HomeMaids.Services;

public interface INotificationService
{
    Task SendAsync(string userId, NotificationType type, string title, string message, string? actionUrl = null);
    Task<int> UnreadCountAsync(string userId);
    Task MarkAllReadAsync(string userId);
}
