using HomeMaids.Data;
using HomeMaids.Models;
using Microsoft.EntityFrameworkCore;

namespace HomeMaids.Services;

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(ApplicationDbContext db, ILogger<NotificationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task SendAsync(string userId, NotificationType type, string title, string message, string? actionUrl = null)
    {
        var n = new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            Type = type,
            ActionUrl = actionUrl,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };
        _db.Notifications.Add(n);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Notification {Type} sent to {User}: {Title}", type, userId, title);
    }

    public Task<int> UnreadCountAsync(string userId) =>
        _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);

    public async Task MarkAllReadAsync(string userId)
    {
        var notifications = await _db.Notifications.Where(n => n.UserId == userId && !n.IsRead).ToListAsync();
        foreach (var n in notifications) n.IsRead = true;
        await _db.SaveChangesAsync();
    }
}
