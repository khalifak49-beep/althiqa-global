using HomeMaids.Data;
using HomeMaids.Models;
using HomeMaids.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HomeMaids.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = DbInitializer.AdminRole)]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _db;

    public DashboardController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var today = DateTime.UtcNow.Date;
        var monthStart = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var paidStatuses = new[] { BookingStatus.Confirmed, BookingStatus.InProgress, BookingStatus.Completed };

        var totalRevenue = await _db.Bookings.Where(b => paidStatuses.Contains(b.Status)).SumAsync(b => (decimal?)b.TotalAmount) ?? 0;
        var monthRevenue = await _db.Bookings.Where(b => paidStatuses.Contains(b.Status) && b.CreatedAt >= monthStart).SumAsync(b => (decimal?)b.TotalAmount) ?? 0;

        var monthly = new List<MonthlyStat>();
        for (var i = 5; i >= 0; i--)
        {
            var start = monthStart.AddMonths(-i);
            var end = start.AddMonths(1);
            var bookingCount = await _db.Bookings.CountAsync(b => b.CreatedAt >= start && b.CreatedAt < end);
            var revenue = await _db.Bookings.Where(b => paidStatuses.Contains(b.Status) && b.CreatedAt >= start && b.CreatedAt < end)
                .SumAsync(b => (decimal?)b.TotalAmount) ?? 0;
            monthly.Add(new MonthlyStat { Month = start.ToString("MMM yyyy"), Bookings = bookingCount, Revenue = revenue });
        }

        var top = await _db.Bookings
            .Where(b => paidStatuses.Contains(b.Status))
            .GroupBy(b => new { b.WorkerId, b.Worker!.FullName, b.Worker.AverageRating })
            .Select(g => new TopWorkerStat
            {
                Name = g.Key.FullName,
                Bookings = g.Count(),
                Revenue = g.Sum(b => b.TotalAmount),
                Rating = g.Key.AverageRating
            })
            .OrderByDescending(s => s.Revenue)
            .Take(5)
            .ToListAsync();

        var statusGroups = await _db.Bookings
            .GroupBy(b => b.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var vm = new AdminDashboardViewModel
        {
            TotalBookings = await _db.Bookings.CountAsync(),
            BookingsToday = await _db.Bookings.CountAsync(b => b.CreatedAt >= today),
            BookingsThisMonth = await _db.Bookings.CountAsync(b => b.CreatedAt >= monthStart),
            TotalRevenue = totalRevenue,
            RevenueThisMonth = monthRevenue,
            TotalCustomers = await _db.Users.CountAsync(),
            TotalWorkers = await _db.Workers.CountAsync(w => w.IsActive),
            AvailableWorkers = await _db.Workers.CountAsync(w => w.IsActive && w.Availability == WorkerAvailability.Available),
            PendingBookings = await _db.Bookings.CountAsync(b => b.Status == BookingStatus.Pending),
            RecentBookings = await _db.Bookings
                .Include(b => b.Worker)
                .Include(b => b.Customer)
                .OrderByDescending(b => b.CreatedAt).Take(8).ToListAsync(),
            MonthlyStats = monthly,
            TopWorkers = top,
            BookingsByStatus = statusGroups.ToDictionary(g => g.Status.ToString(), g => g.Count)
        };

        return View(vm);
    }
}
