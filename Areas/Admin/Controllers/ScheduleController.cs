using HomeMaids.Data;
using HomeMaids.Models;
using HomeMaids.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HomeMaids.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = DbInitializer.AdminRole)]
public class ScheduleController : Controller
{
    private readonly ApplicationDbContext _db;

    public ScheduleController(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index(DateTime? from, int days = 7)
    {
        if (days < 1 || days > 30) days = 7;
        var start = (from ?? DateTime.Today).Date;
        var end = start.AddDays(days);

        var workers = await _db.Workers.AsNoTracking()
            .Where(w => w.IsActive)
            .Include(w => w.Service)
            .Include(w => w.Schedules)
            .OrderBy(w => w.FullName)
            .ToListAsync();

        var hourly = await _db.Bookings.AsNoTracking()
            .Where(b => b.Type == BookingType.Hourly
                     && b.Status != BookingStatus.Cancelled
                     && b.BookingDate >= start && b.BookingDate < end)
            .Select(b => new ScheduleBusySlot
            {
                WorkerId = b.WorkerId,
                Date = b.BookingDate,
                StartTime = b.StartTime,
                EndTime = b.EndTime,
                BookingId = b.Id,
                BookingNumber = b.BookingNumber,
                Kind = "ساعي"
            })
            .ToListAsync();

        var monthly = await _db.MonthlyVisits.AsNoTracking()
            .Where(v => v.Status != BookingStatus.Cancelled
                     && v.ScheduledDate >= start && v.ScheduledDate < end)
            .Select(v => new ScheduleBusySlot
            {
                WorkerId = v.Booking!.WorkerId,
                Date = v.ScheduledDate,
                StartTime = v.StartTime,
                EndTime = v.EndTime,
                BookingId = v.BookingId,
                BookingNumber = v.Booking!.BookingNumber,
                Kind = "شهري"
            })
            .ToListAsync();

        var allBusy = hourly.Concat(monthly)
            .GroupBy(s => s.WorkerId)
            .ToDictionary(g => g.Key, g => g.OrderBy(s => s.Date).ThenBy(s => s.StartTime).ToList());

        var vm = new AdminScheduleViewModel
        {
            From = start,
            Days = days,
            Workers = workers,
            BusyByWorker = allBusy
        };
        return View(vm);
    }
}
