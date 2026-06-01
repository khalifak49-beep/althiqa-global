using HomeMaids.Data;
using HomeMaids.Models;
using Microsoft.EntityFrameworkCore;

namespace HomeMaids.Services;

public class PaymentRequest
{
    public int BookingId { get; set; }
    public PaymentMethod Method { get; set; }
    public string? CardNumber { get; set; }
    public string? CardHolderName { get; set; }
    public string? CardExpiry { get; set; }
    public string? Cvv { get; set; }
}

public class PaymentResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public Payment? Payment { get; set; }
}

public interface IPaymentService
{
    Task<PaymentResult> ChargeAsync(string customerId, PaymentRequest request);
}

public class PaymentService : IPaymentService
{
    private readonly ApplicationDbContext _db;
    private readonly INotificationService _notifications;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(ApplicationDbContext db, INotificationService notifications, ILogger<PaymentService> logger)
    {
        _db = db;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task<PaymentResult> ChargeAsync(string customerId, PaymentRequest request)
    {
        var booking = await _db.Bookings.Include(b => b.Worker)
            .FirstOrDefaultAsync(b => b.Id == request.BookingId && b.CustomerId == customerId);
        if (booking == null) return new PaymentResult { Success = false, Error = "الحجز غير موجود." };

        if (booking.Payment != null && booking.Payment.Status == PaymentStatus.Paid)
            return new PaymentResult { Success = false, Error = "تم دفع هذا الحجز مسبقاً." };

        // ===== Payment Gateway Integration Placeholder =====
        // In production, integrate with Stripe / HyperPay / PayTabs / Apple Pay / Google Pay etc.
        // We simulate a successful gateway call here.
        var success = true;
        var transactionRef = $"TX-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";

        var last4 = !string.IsNullOrEmpty(request.CardNumber) && request.CardNumber.Length >= 4
            ? request.CardNumber[^4..]
            : null;

        var payment = new Payment
        {
            BookingId = booking.Id,
            Amount = booking.TotalAmount,
            Method = request.Method,
            Status = success ? PaymentStatus.Paid : PaymentStatus.Failed,
            TransactionRef = transactionRef,
            CardLast4 = last4,
            CardHolderName = request.CardHolderName,
            GatewayResponse = success ? "APPROVED" : "DECLINED",
            CreatedAt = DateTime.UtcNow,
            PaidAt = success ? DateTime.UtcNow : null
        };

        _db.Payments.Add(payment);

        if (success)
        {
            booking.Status = BookingStatus.Confirmed;
            // Loyalty points: 1 point per 10 currency units paid
            var customer = await _db.Users.FirstOrDefaultAsync(u => u.Id == customerId);
            if (customer != null)
            {
                customer.LoyaltyPoints += (int)Math.Floor(booking.TotalAmount / 10m);
            }
        }
        await _db.SaveChangesAsync();

        await _notifications.SendAsync(customerId,
            success ? NotificationType.PaymentSuccess : NotificationType.PaymentFailed,
            success ? "تم الدفع بنجاح" : "فشل الدفع",
            success ? $"تم دفع مبلغ {booking.TotalAmount:N2} للحجز {booking.BookingNumber}." : "تعذر إكمال عملية الدفع. حاول مرة أخرى.",
            $"/Bookings/Details/{booking.Id}");

        _logger.LogInformation("Payment {Ref} status {Status} for booking {Booking}",
            transactionRef, payment.Status, booking.BookingNumber);

        return new PaymentResult { Success = success, Payment = payment, Error = success ? null : "Gateway declined" };
    }
}
