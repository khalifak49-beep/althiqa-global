using HomeMaids.Data;
using HomeMaids.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HomeMaids.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = DbInitializer.AdminRole)]
public class ReportsController : Controller
{
    private readonly ApplicationDbContext _db;
    public ReportsController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index(DateTime? from, DateTime? to)
    {
        from ??= DateTime.UtcNow.AddMonths(-1).Date;
        to ??= DateTime.UtcNow.Date.AddDays(1);

        var paid = new[] { BookingStatus.Confirmed, BookingStatus.InProgress, BookingStatus.Completed };

        var bookings = await _db.Bookings
            .Where(b => b.CreatedAt >= from && b.CreatedAt < to)
            .ToListAsync();

        ViewBag.From = from.Value.ToString("yyyy-MM-dd");
        ViewBag.To = to.Value.AddDays(-1).ToString("yyyy-MM-dd");
        ViewBag.TotalBookings = bookings.Count;
        ViewBag.PaidBookings = bookings.Count(b => paid.Contains(b.Status));
        ViewBag.CancelledBookings = bookings.Count(b => b.Status == BookingStatus.Cancelled);
        ViewBag.Revenue = bookings.Where(b => paid.Contains(b.Status)).Sum(b => b.TotalAmount);

        var byDay = bookings
            .GroupBy(b => b.CreatedAt.Date)
            .Select(g => new
            {
                Date = g.Key.ToString("yyyy-MM-dd"),
                Count = g.Count(),
                Revenue = g.Where(b => paid.Contains(b.Status)).Sum(b => b.TotalAmount)
            })
            .OrderBy(x => x.Date)
            .ToList();
        ViewBag.ByDay = byDay;

        return View(bookings.OrderByDescending(b => b.CreatedAt).Take(100).ToList());
    }
}
