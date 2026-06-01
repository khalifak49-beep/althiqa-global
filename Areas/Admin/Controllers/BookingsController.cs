using HomeMaids.Data;
using HomeMaids.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HomeMaids.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = DbInitializer.AdminRole)]
public class BookingsController : Controller
{
    private readonly ApplicationDbContext _db;

    public BookingsController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index(BookingStatus? status, string? q)
    {
        var query = _db.Bookings
            .Include(b => b.Worker)
            .Include(b => b.Customer)
            .Include(b => b.Payment)
            .AsQueryable();

        if (status.HasValue) query = query.Where(b => b.Status == status);

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(b => b.BookingNumber.Contains(q)
                || b.Customer!.FullName.Contains(q)
                || b.Worker!.FullName.Contains(q));

        var list = await query.OrderByDescending(b => b.CreatedAt).Take(200).ToListAsync();
        ViewBag.Status = status;
        ViewBag.Q = q;
        return View(list);
    }

    public async Task<IActionResult> Details(int id)
    {
        var booking = await _db.Bookings
            .Include(b => b.Worker)
            .Include(b => b.Customer)
            .Include(b => b.Payment)
            .Include(b => b.Coupon)
            .Include(b => b.Service)
            .Include(b => b.Review)
            .FirstOrDefaultAsync(b => b.Id == id);
        if (booking == null) return NotFound();
        return View(booking);
    }

    [HttpPost]
    public async Task<IActionResult> ChangeStatus(int id, BookingStatus status)
    {
        var booking = await _db.Bookings.FindAsync(id);
        if (booking == null) return NotFound();
        booking.Status = status;
        if (status == BookingStatus.Cancelled) booking.CancelledAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        TempData["Success"] = "تم تحديث حالة الحجز.";
        return RedirectToAction(nameof(Details), new { id });
    }
}
