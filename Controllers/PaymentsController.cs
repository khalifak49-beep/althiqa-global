using HomeMaids.Data;
using HomeMaids.Models;
using HomeMaids.Services;
using HomeMaids.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HomeMaids.Controllers;

[Authorize]
public class PaymentsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IThawaniGateway _thawani;
    private readonly INotificationService _notifications;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        ApplicationDbContext db,
        IThawaniGateway thawani,
        INotificationService notifications,
        UserManager<ApplicationUser> userManager,
        ILogger<PaymentsController> logger)
    {
        _db = db;
        _thawani = thawani;
        _notifications = notifications;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Checkout(int bookingId)
    {
        var userId = _userManager.GetUserId(User);
        var booking = await _db.Bookings
            .Include(b => b.Worker)
            .Include(b => b.Payment)
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.CustomerId == userId);
        if (booking == null) return NotFound();

        return View(new CheckoutViewModel { Booking = booking });
    }

    [HttpPost, ActionName("Checkout")]
    public async Task<IActionResult> CheckoutPost(int bookingId)
    {
        var userId = _userManager.GetUserId(User)!;
        var user = await _userManager.GetUserAsync(User);
        var booking = await _db.Bookings
            .Include(b => b.Worker)
            .Include(b => b.Service)
            .Include(b => b.Payment)
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.CustomerId == userId);
        if (booking == null) return NotFound();
        if (booking.Payment != null && booking.Payment.Status == PaymentStatus.Paid)
        {
            TempData["Error"] = "تم دفع هذا الحجز مسبقاً.";
            return RedirectToAction("Details", "Bookings", new { id = booking.Id });
        }

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var customer = new ThawaniCustomer
        {
            Id = user!.Id,
            Email = user.Email ?? string.Empty,
            FullName = user.FullName,
            Phone = user.PhoneNumber,
            DefaultAddress = user.DefaultAddress,
            LoyaltyPoints = user.LoyaltyPoints,
            CreatedAt = user.CreatedAt
        };
        var session = await _thawani.CreateSessionAsync(booking, customer, baseUrl);

        if (!session.Success || string.IsNullOrEmpty(session.RedirectUrl))
        {
            TempData["Error"] = session.Error ?? "تعذر بدء عملية الدفع.";
            return RedirectToAction("Details", "Bookings", new { id = booking.Id });
        }

        // Save a Pending payment row referencing the Thawani session
        var existing = await _db.Payments.FirstOrDefaultAsync(p => p.BookingId == booking.Id);
        if (existing == null)
        {
            _db.Payments.Add(new Payment
            {
                BookingId = booking.Id,
                Amount = booking.TotalAmount,
                Method = PaymentMethod.Visa,
                Status = PaymentStatus.Pending,
                TransactionRef = session.SessionId!,
                GatewayResponse = "session_created",
                CreatedAt = DateTime.UtcNow
            });
        }
        else
        {
            existing.TransactionRef = session.SessionId!;
            existing.Status = PaymentStatus.Pending;
            existing.GatewayResponse = "session_created";
        }
        await _db.SaveChangesAsync();

        return Redirect(session.RedirectUrl);
    }

    [HttpGet]
    public async Task<IActionResult> ThawaniSuccess(int bookingId)
    {
        var userId = _userManager.GetUserId(User);
        var booking = await _db.Bookings.Include(b => b.Payment).Include(b => b.Worker)
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.CustomerId == userId);
        if (booking == null) return NotFound();

        if (booking.Payment != null && !string.IsNullOrEmpty(booking.Payment.TransactionRef))
        {
            var status = await _thawani.GetSessionStatusAsync(booking.Payment.TransactionRef);
            _logger.LogInformation("Thawani status for booking {Id}: {Status}", booking.Id, status);

            if (string.Equals(status, "paid", StringComparison.OrdinalIgnoreCase))
            {
                booking.Payment.Status = PaymentStatus.Paid;
                booking.Payment.PaidAt = DateTime.UtcNow;
                booking.Payment.GatewayResponse = "PAID";
                booking.Status = BookingStatus.Confirmed;

                var customer = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (customer != null)
                {
                    customer.LoyaltyPoints += (int)Math.Floor(booking.TotalAmount / 10m);
                }

                await _db.SaveChangesAsync();
                await _notifications.SendAsync(userId!, NotificationType.PaymentSuccess,
                    "تم الدفع بنجاح",
                    $"تم دفع {booking.TotalAmount:N3} ر.ع. للحجز {booking.BookingNumber}.",
                    $"/Bookings/Details/{booking.Id}");
            }
            else
            {
                booking.Payment.GatewayResponse = status ?? "unknown";
                await _db.SaveChangesAsync();
            }
        }

        return View("Success", booking);
    }

    [HttpGet]
    public async Task<IActionResult> ThawaniCancel(int bookingId)
    {
        var userId = _userManager.GetUserId(User);
        var booking = await _db.Bookings.Include(b => b.Payment)
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.CustomerId == userId);
        if (booking != null && booking.Payment != null)
        {
            booking.Payment.Status = PaymentStatus.Failed;
            booking.Payment.GatewayResponse = "CANCELLED";
            await _db.SaveChangesAsync();
        }
        TempData["Error"] = "تم إلغاء عملية الدفع.";
        return RedirectToAction("Details", "Bookings", new { id = bookingId });
    }

    [HttpGet]
    public async Task<IActionResult> Success(int bookingId)
    {
        var userId = _userManager.GetUserId(User);
        var booking = await _db.Bookings
            .Include(b => b.Worker)
            .Include(b => b.Payment)
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.CustomerId == userId);
        if (booking == null) return NotFound();
        return View(booking);
    }
}
