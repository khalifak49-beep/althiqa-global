using HomeMaids.Data;
using HomeMaids.Models;
using HomeMaids.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HomeMaids.Controllers;

[Authorize]
public class NotificationsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly INotificationService _service;
    private readonly UserManager<ApplicationUser> _userManager;

    public NotificationsController(ApplicationDbContext db, INotificationService service, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _service = service;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);
        var items = await _db.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(100)
            .ToListAsync();
        await _service.MarkAllReadAsync(userId!);
        return View(items);
    }

    [HttpGet]
    public async Task<IActionResult> Unread()
    {
        var userId = _userManager.GetUserId(User);
        var count = await _service.UnreadCountAsync(userId!);
        var latest = await _db.Notifications.AsNoTracking()
            .Where(n => n.UserId == userId && !n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .Take(5)
            .Select(n => new { n.Id, n.Title, n.Message, n.ActionUrl, n.CreatedAt })
            .ToListAsync();
        return Json(new { count, items = latest });
    }
}
