using HomeMaids.Data;
using HomeMaids.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HomeMaids.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = DbInitializer.AdminRole)]
public class SchedulesController : Controller
{
    private readonly ApplicationDbContext _db;
    public SchedulesController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Edit(int workerId)
    {
        var worker = await _db.Workers.FirstOrDefaultAsync(w => w.Id == workerId);
        if (worker == null) return NotFound();

        var existing = await _db.WorkerSchedules
            .Where(s => s.WorkerId == workerId)
            .ToListAsync();

        var rows = new List<WorkerSchedule>();
        for (var d = 0; d < 7; d++)
        {
            var day = (DayOfWeek)d;
            var found = existing.FirstOrDefault(s => s.DayOfWeek == day);
            rows.Add(found ?? new WorkerSchedule
            {
                WorkerId = workerId,
                DayOfWeek = day,
                StartTime = new TimeSpan(8, 0, 0),
                EndTime = new TimeSpan(20, 0, 0),
                IsAvailable = day != DayOfWeek.Friday
            });
        }

        ViewBag.Worker = worker;
        return View(rows);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int workerId, List<DayScheduleInput> days)
    {
        if (days == null || days.Count != 7)
        {
            TempData["Error"] = "بيانات الجدول غير صحيحة.";
            return RedirectToAction(nameof(Edit), new { workerId });
        }

        foreach (var d in days)
        {
            if (d.End <= d.Start)
            {
                ModelState.AddModelError("", $"وقت النهاية يجب أن يكون بعد البداية ({(DayOfWeek)d.Day}).");
                ViewBag.Worker = await _db.Workers.FindAsync(workerId);
                var fallback = await _db.WorkerSchedules.Where(s => s.WorkerId == workerId).ToListAsync();
                return View(fallback);
            }
        }

        var existing = await _db.WorkerSchedules.Where(s => s.WorkerId == workerId).ToListAsync();
        foreach (var input in days)
        {
            var day = (DayOfWeek)input.Day;
            var row = existing.FirstOrDefault(s => s.DayOfWeek == day);
            if (row == null)
            {
                _db.WorkerSchedules.Add(new WorkerSchedule
                {
                    WorkerId = workerId,
                    DayOfWeek = day,
                    StartTime = input.Start,
                    EndTime = input.End,
                    IsAvailable = input.IsAvailable
                });
            }
            else
            {
                row.StartTime = input.Start;
                row.EndTime = input.End;
                row.IsAvailable = input.IsAvailable;
            }
        }
        await _db.SaveChangesAsync();

        TempData["Success"] = "تم حفظ جدول العاملة.";
        return RedirectToAction(nameof(Edit), new { workerId });
    }
}

public class DayScheduleInput
{
    public int Day { get; set; }
    public TimeSpan Start { get; set; }
    public TimeSpan End { get; set; }
    public bool IsAvailable { get; set; }
}
