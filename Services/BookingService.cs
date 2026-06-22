using HomeMaids.Data;
using HomeMaids.Models;
using HomeMaids.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HomeMaids.Services;

public class BookingService : IBookingService
{
    private readonly ApplicationDbContext _db;
    private readonly IUnitOfWork _uow;
    private readonly IPriceCalculator _calculator;
    private readonly INotificationService _notifications;
    private readonly BookingSettings _settings;
    private readonly ILogger<BookingService> _logger;

    public BookingService(
        ApplicationDbContext db,
        IUnitOfWork uow,
        IPriceCalculator calculator,
        INotificationService notifications,
        IOptions<BookingSettings> settings,
        ILogger<BookingService> logger)
    {
        _db = db;
        _uow = uow;
        _calculator = calculator;
        _notifications = notifications;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<PriceQuote> QuoteAsync(int workerId, int hours, string? couponCode)
    {
        var worker = await _db.Workers.AsNoTracking().FirstOrDefaultAsync(w => w.Id == workerId)
            ?? throw new InvalidOperationException("Worker not found");

        Coupon? coupon = null;
        if (!string.IsNullOrWhiteSpace(couponCode))
        {
            coupon = await _db.Coupons.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Code == couponCode);
        }

        var quote = _calculator.Calculate(worker, hours, coupon);
        if (coupon == null && !string.IsNullOrWhiteSpace(couponCode))
        {
            quote.CouponStatus = "invalid";
            quote.CouponMessage = "كوبون غير معروف.";
        }
        return quote;
    }

    public async Task<bool> IsTimeSlotAvailableAsync(int workerId, DateTime date, TimeSpan start, int hours, int? ignoreBookingId = null)
    {
        var end = start.Add(TimeSpan.FromHours(hours));
        var dayOfWeek = date.DayOfWeek;

        var schedule = await _db.WorkerSchedules
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.WorkerId == workerId && s.DayOfWeek == dayOfWeek);

        if (schedule == null || !schedule.IsAvailable) return false;
        if (start < schedule.StartTime || end > schedule.EndTime) return false;

        var bookingDate = date.Date;
        var query = _db.Bookings
            .AsNoTracking()
            .Where(b => b.WorkerId == workerId
                        && b.BookingDate == bookingDate
                        && b.Status != BookingStatus.Cancelled
                        && b.Status != BookingStatus.Refunded);

        if (ignoreBookingId.HasValue)
            query = query.Where(b => b.Id != ignoreBookingId.Value);

        var existing = await query.ToListAsync();
        return !existing.Any(b => b.StartTime < end && start < b.EndTime);
    }

    public async Task<BookingResult> CreateAsync(string customerId, CreateBookingRequest request)
    {
        if (request.Hours < _settings.MinHoursPerBooking || request.Hours > _settings.MaxHoursPerBooking)
            return new BookingResult { Success = false, Error = $"عدد الساعات يجب أن يكون بين {_settings.MinHoursPerBooking} و {_settings.MaxHoursPerBooking}." };

        if (request.BookingDate.Date < DateTime.UtcNow.Date)
            return new BookingResult { Success = false, Error = "لا يمكن الحجز في تاريخ ماضٍ." };

        var worker = await _db.Workers.FirstOrDefaultAsync(w => w.Id == request.WorkerId && w.IsActive);
        if (worker == null) return new BookingResult { Success = false, Error = "العاملة غير متاحة." };

        var slotOk = await IsTimeSlotAvailableAsync(request.WorkerId, request.BookingDate, request.StartTime, request.Hours);
        if (!slotOk) return new BookingResult { Success = false, Error = "الوقت المختار غير متاح." };

        Coupon? coupon = null;
        if (!string.IsNullOrWhiteSpace(request.CouponCode))
        {
            coupon = await _db.Coupons.FirstOrDefaultAsync(c =>
                c.Code == request.CouponCode &&
                c.IsActive &&
                c.ValidFrom <= DateTime.UtcNow &&
                c.ValidTo >= DateTime.UtcNow &&
                (c.UsageLimit == null || c.UsedCount < c.UsageLimit));

            if (coupon == null)
                return new BookingResult { Success = false, Error = "كوبون غير صالح." };
        }

        var quote = _calculator.Calculate(worker, request.Hours, coupon);

        var booking = new Booking
        {
            BookingNumber = $"HM-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}",
            CustomerId = customerId,
            WorkerId = request.WorkerId,
            ServiceId = request.ServiceId,
            BookingDate = request.BookingDate.Date,
            StartTime = request.StartTime,
            EndTime = request.StartTime.Add(TimeSpan.FromHours(request.Hours)),
            Hours = request.Hours,
            Address = request.Address,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Notes = request.Notes,
            SubTotal = quote.SubTotal,
            DiscountAmount = quote.Discount,
            TaxAmount = quote.Tax,
            TotalAmount = quote.Total,
            CouponId = quote.CouponId,
            Status = BookingStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            TermsAcceptedAt = request.TermsAccepted ? DateTime.UtcNow : (DateTime?)null,
            TermsVersion = request.TermsAccepted ? (request.TermsVersion ?? "2026-02-17") : null
        };

        await _uow.Bookings.AddAsync(booking);

        if (coupon != null)
        {
            coupon.UsedCount += 1;
            _uow.Coupons.Update(coupon);
        }

        await _uow.SaveChangesAsync();

        await _notifications.SendAsync(customerId, NotificationType.BookingConfirmed,
            "تم إنشاء حجزك بنجاح",
            $"رقم الحجز {booking.BookingNumber}. يرجى إكمال عملية الدفع.",
            $"/Bookings/Details/{booking.Id}");

        _logger.LogInformation("Booking {Number} created by {User}", booking.BookingNumber, customerId);
        return new BookingResult { Success = true, Booking = booking };
    }

    public async Task<BookingResult> CancelAsync(int bookingId, string customerId, string? reason)
    {
        var booking = await _db.Bookings.Include(b => b.Coupon)
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.CustomerId == customerId);

        if (booking == null) return new BookingResult { Success = false, Error = "الحجز غير موجود." };
        if (booking.Status is BookingStatus.Cancelled or BookingStatus.Completed)
            return new BookingResult { Success = false, Error = "لا يمكن إلغاء الحجز في حالته الحالية." };

        var hoursUntil = (booking.BookingDate.Add(booking.StartTime) - DateTime.UtcNow).TotalHours;
        if (hoursUntil < _settings.CancellationWindowHours)
            return new BookingResult { Success = false, Error = $"لا يمكن الإلغاء قبل أقل من {_settings.CancellationWindowHours} ساعة من الموعد." };

        booking.Status = BookingStatus.Cancelled;
        booking.CancelledAt = DateTime.UtcNow;
        booking.CancellationReason = reason;
        _uow.Bookings.Update(booking);
        await _uow.SaveChangesAsync();

        await _notifications.SendAsync(customerId, NotificationType.BookingCancelled,
            "تم إلغاء الحجز",
            $"تم إلغاء الحجز رقم {booking.BookingNumber}.", $"/Bookings/Details/{booking.Id}");

        return new BookingResult { Success = true, Booking = booking };
    }

    public async Task<IReadOnlyList<TimeSpan>> GetAvailableStartTimesAsync(int workerId, DateTime date, int hours)
    {
        var dayOfWeek = date.DayOfWeek;
        var schedule = await _db.WorkerSchedules.AsNoTracking()
            .FirstOrDefaultAsync(s => s.WorkerId == workerId && s.DayOfWeek == dayOfWeek);
        if (schedule == null || !schedule.IsAvailable) return Array.Empty<TimeSpan>();

        var bookings = await _db.Bookings.AsNoTracking()
            .Where(b => b.WorkerId == workerId
                && b.BookingDate == date.Date
                && b.Status != BookingStatus.Cancelled
                && b.Status != BookingStatus.Refunded)
            .Select(b => new { b.StartTime, b.EndTime })
            .ToListAsync();

        var slots = new List<TimeSpan>();
        var step = TimeSpan.FromHours(1);
        for (var t = schedule.StartTime; t.Add(TimeSpan.FromHours(hours)) <= schedule.EndTime; t = t.Add(step))
        {
            var end = t.Add(TimeSpan.FromHours(hours));
            var conflict = bookings.Any(b => b.StartTime < end && t < b.EndTime);
            if (!conflict) slots.Add(t);
        }
        return slots;
    }

    public Task<Booking?> GetDetailedAsync(int bookingId)
        => _db.Bookings
            .Include(b => b.Worker)
            .Include(b => b.Service)
            .Include(b => b.Customer)
            .Include(b => b.Payment)
            .Include(b => b.Review)
            .Include(b => b.Coupon)
            .FirstOrDefaultAsync(b => b.Id == bookingId);
}
