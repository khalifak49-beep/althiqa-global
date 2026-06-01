using HomeMaids.Data;
using HomeMaids.Models;
using HomeMaids.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HomeMaids.Controllers;

[Authorize]
public class ReviewsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public ReviewsController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Create(int bookingId)
    {
        var userId = _userManager.GetUserId(User);
        var booking = await _db.Bookings.Include(b => b.Worker)
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.CustomerId == userId);
        if (booking == null) return NotFound();
        if (booking.Status != BookingStatus.Completed)
        {
            TempData["Error"] = "يمكن التقييم فقط بعد اكتمال الخدمة.";
            return RedirectToAction("Details", "Bookings", new { id = bookingId });
        }

        if (await _db.Reviews.AnyAsync(r => r.BookingId == bookingId))
        {
            TempData["Error"] = "تم تقييم هذا الحجز مسبقاً.";
            return RedirectToAction("Details", "Bookings", new { id = bookingId });
        }

        return View(new ReviewViewModel
        {
            BookingId = booking.Id,
            WorkerId = booking.WorkerId,
            WorkerName = booking.Worker?.FullName
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create(ReviewViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var userId = _userManager.GetUserId(User)!;
        var booking = await _db.Bookings.FirstOrDefaultAsync(b => b.Id == vm.BookingId && b.CustomerId == userId);
        if (booking == null) return NotFound();

        if (await _db.Reviews.AnyAsync(r => r.BookingId == vm.BookingId))
        {
            TempData["Error"] = "تم التقييم مسبقاً.";
            return RedirectToAction("Details", "Bookings", new { id = vm.BookingId });
        }

        _db.Reviews.Add(new Review
        {
            BookingId = vm.BookingId,
            WorkerId = vm.WorkerId,
            CustomerId = userId,
            Rating = vm.Rating,
            Comment = vm.Comment
        });

        var worker = await _db.Workers.FirstAsync(w => w.Id == vm.WorkerId);
        var totalReviews = await _db.Reviews.CountAsync(r => r.WorkerId == vm.WorkerId) + 1;
        var avg = (await _db.Reviews.Where(r => r.WorkerId == vm.WorkerId).SumAsync(r => (decimal)r.Rating) + vm.Rating) / totalReviews;
        worker.AverageRating = Math.Round(avg, 2);

        await _db.SaveChangesAsync();
        TempData["Success"] = "شكراً لتقييمك!";
        return RedirectToAction("Details", "Bookings", new { id = vm.BookingId });
    }
}
