using HomeMaids.Models;

namespace HomeMaids.Services;

public class BookingResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public Booking? Booking { get; set; }
}

public class CreateBookingRequest
{
    public int WorkerId { get; set; }
    public int? ServiceId { get; set; }
    public DateTime BookingDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public int Hours { get; set; }
    public string Address { get; set; } = string.Empty;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? Notes { get; set; }
    public string? CouponCode { get; set; }
}

public interface IBookingService
{
    Task<PriceQuote> QuoteAsync(int workerId, int hours, string? couponCode);
    Task<bool> IsTimeSlotAvailableAsync(int workerId, DateTime date, TimeSpan start, int hours, int? ignoreBookingId = null);
    Task<BookingResult> CreateAsync(string customerId, CreateBookingRequest request);
    Task<BookingResult> CancelAsync(int bookingId, string customerId, string? reason);
    Task<IReadOnlyList<TimeSpan>> GetAvailableStartTimesAsync(int workerId, DateTime date, int hours);
    Task<Booking?> GetDetailedAsync(int bookingId);
}
