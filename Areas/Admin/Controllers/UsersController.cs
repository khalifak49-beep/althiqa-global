using HomeMaids.Data;
using HomeMaids.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HomeMaids.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = DbInitializer.AdminRole)]
public class UsersController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _db;
    private readonly ILogger<UsersController> _logger;

    public UsersController(UserManager<ApplicationUser> userManager, ApplicationDbContext db, ILogger<UsersController> logger)
    {
        _userManager = userManager;
        _db = db;
        _logger = logger;
    }

    public async Task<IActionResult> Index(string? q)
    {
        var query = _userManager.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(u => u.FullName.Contains(q) || u.Email!.Contains(q));
        var users = await query.OrderByDescending(u => u.CreatedAt).Take(200).ToListAsync();
        ViewBag.Q = q;
        return View(users);
    }

    [HttpPost]
    public async Task<IActionResult> ToggleActive(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();
        user.IsActive = !user.IsActive;
        await _userManager.UpdateAsync(user);
        TempData["Success"] = user.IsActive ? "تم تفعيل المستخدم." : "تم تعطيل المستخدم.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Admin-only account deletion. Hard-deletes when there's no booking history;
    /// otherwise anonymises PII and keeps the row so booking FKs stay valid.
    /// Protects against deleting the last remaining admin or deleting yourself.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Delete(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var currentUserId = _userManager.GetUserId(User);
        if (user.Id == currentUserId)
        {
            TempData["Error"] = "لا يمكنك حذف حسابك أنت من هذه الصفحة.";
            return RedirectToAction(nameof(Index));
        }

        if (await _userManager.IsInRoleAsync(user, DbInitializer.AdminRole))
        {
            var admins = await _userManager.GetUsersInRoleAsync(DbInitializer.AdminRole);
            if (admins.Count <= 1)
            {
                TempData["Error"] = "لا يمكن حذف آخر حساب أدمن في النظام.";
                return RedirectToAction(nameof(Index));
            }
        }

        var bookingCount = await _db.Bookings.CountAsync(b => b.CustomerId == user.Id);
        var userId = user.Id;
        var oldEmail = user.Email;

        // Always purge low-value related rows first
        _db.Favorites.RemoveRange(_db.Favorites.Where(f => f.UserId == userId));
        _db.UserAddresses.RemoveRange(_db.UserAddresses.Where(a => a.UserId == userId));
        _db.Notifications.RemoveRange(_db.Notifications.Where(n => n.UserId == userId));
        await _db.SaveChangesAsync();

        if (bookingCount == 0)
        {
            // Safe hard-delete — no booking history references this user.
            var del = await _userManager.DeleteAsync(user);
            if (!del.Succeeded)
            {
                TempData["Error"] = "تعذر الحذف: " + string.Join("؛ ", del.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Index));
            }
            _logger.LogInformation("Admin {Admin} hard-deleted user {Email}", User.Identity?.Name, oldEmail);
            TempData["Success"] = $"تم حذف المستخدم \"{oldEmail}\" نهائياً.";
        }
        else
        {
            // Anonymise PII; keep the row so Booking.CustomerId remains valid.
            var anonId = Guid.NewGuid().ToString("N")[..10];
            user.FullName = "Deleted user";
            user.Email = $"deleted-{anonId}@deleted.local";
            user.NormalizedEmail = user.Email.ToUpperInvariant();
            user.UserName = user.Email;
            user.NormalizedUserName = user.NormalizedEmail;
            user.PhoneNumber = null;
            user.PhoneNumberConfirmed = false;
            user.DefaultAddress = null;
            user.AvatarUrl = null;
            user.IsActive = false;
            user.LockoutEnd = DateTimeOffset.MaxValue;
            user.LoyaltyPoints = 0;
            user.PasswordHash = Guid.NewGuid().ToString("N");
            user.SecurityStamp = Guid.NewGuid().ToString("N");
            await _userManager.UpdateAsync(user);

            _logger.LogInformation("Admin {Admin} anonymised user {Email} (had {Bookings} bookings)", User.Identity?.Name, oldEmail, bookingCount);
            TempData["Success"] = $"تم تجهيل بيانات \"{oldEmail}\" (يحتفظ بسجل {bookingCount} حجز لأغراض محاسبية).";
        }

        return RedirectToAction(nameof(Index));
    }
}
