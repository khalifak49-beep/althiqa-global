using System.ComponentModel.DataAnnotations;
using HomeMaids.Models;

namespace HomeMaids.ViewModels;

public class AdminDashboardViewModel
{
    public int TotalBookings { get; set; }
    public int BookingsToday { get; set; }
    public int BookingsThisMonth { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal RevenueThisMonth { get; set; }
    public int TotalCustomers { get; set; }
    public int TotalWorkers { get; set; }
    public int AvailableWorkers { get; set; }
    public int PendingBookings { get; set; }

    public IReadOnlyList<Booking> RecentBookings { get; set; } = Array.Empty<Booking>();
    public IReadOnlyList<MonthlyStat> MonthlyStats { get; set; } = Array.Empty<MonthlyStat>();
    public IReadOnlyList<TopWorkerStat> TopWorkers { get; set; } = Array.Empty<TopWorkerStat>();
    public IReadOnlyDictionary<string, int> BookingsByStatus { get; set; } = new Dictionary<string, int>();
}

public class MonthlyStat
{
    public string Month { get; set; } = string.Empty;
    public int Bookings { get; set; }
    public decimal Revenue { get; set; }
}

public class TopWorkerStat
{
    public string Name { get; set; } = string.Empty;
    public int Bookings { get; set; }
    public decimal Revenue { get; set; }
    public decimal Rating { get; set; }
}

public class WorkerEditViewModel
{
    public int Id { get; set; }
    [Required, StringLength(100)] public string FullName { get; set; } = string.Empty;
    [Range(18, 70)] public int Age { get; set; }
    [Required, StringLength(60)] public string Nationality { get; set; } = string.Empty;
    [Range(0, 50)] public int YearsOfExperience { get; set; }
    [StringLength(300)] public string Languages { get; set; } = string.Empty;
    [StringLength(1000)] public string? Bio { get; set; }

    /// <summary>Existing/saved photo path. Read-only in UI (preview only).</summary>
    [StringLength(500)] public string? PhotoUrl { get; set; }

    /// <summary>Uploaded image file (replaces PhotoUrl when provided).</summary>
    public IFormFile? PhotoFile { get; set; }

    [Range(0, 10000)] public decimal HourlyRate { get; set; }
    public WorkerAvailability Availability { get; set; }
    public bool IsActive { get; set; } = true;
    public int? ServiceId { get; set; }
}

public class CouponEditViewModel
{
    public int Id { get; set; }
    [Required, StringLength(30)] public string Code { get; set; } = string.Empty;
    [StringLength(200)] public string? Description { get; set; }
    public DiscountType DiscountType { get; set; }
    public decimal DiscountValue { get; set; }
    public decimal? MinOrderAmount { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    [DataType(DataType.Date)] public DateTime ValidFrom { get; set; } = DateTime.Today;
    [DataType(DataType.Date)] public DateTime ValidTo { get; set; } = DateTime.Today.AddMonths(1);
    public int? UsageLimit { get; set; }
    public bool IsActive { get; set; } = true;
}

public class OfferEditViewModel
{
    public int Id { get; set; }
    [Required, StringLength(150)] public string Title { get; set; } = string.Empty;
    [StringLength(1000)] public string? Description { get; set; }
    [StringLength(500)] public string? ImageUrl { get; set; }
    public DiscountType DiscountType { get; set; }
    public decimal DiscountValue { get; set; }
    [DataType(DataType.Date)] public DateTime ValidFrom { get; set; } = DateTime.Today;
    [DataType(DataType.Date)] public DateTime ValidTo { get; set; } = DateTime.Today.AddMonths(1);
    public bool IsActive { get; set; } = true;
}
