using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HomeMaids.Models;

public class Booking
{
    public int Id { get; set; }

    [StringLength(20)]
    public string BookingNumber { get; set; } = string.Empty;

    [Required]
    public string CustomerId { get; set; } = string.Empty;
    public ApplicationUser? Customer { get; set; }

    [Required]
    public int WorkerId { get; set; }
    public Worker? Worker { get; set; }

    public int? ServiceId { get; set; }
    public Service? Service { get; set; }

    public DateTime BookingDate { get; set; }

    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }

    [Range(1, 24)]
    public int Hours { get; set; }

    [Required, StringLength(300)]
    public string Address { get; set; } = string.Empty;

    [Column(TypeName = "decimal(9,6)")]
    public decimal? Latitude { get; set; }

    [Column(TypeName = "decimal(9,6)")]
    public decimal? Longitude { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal SubTotal { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal TaxAmount { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal DiscountAmount { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal TotalAmount { get; set; }

    public int? CouponId { get; set; }
    public Coupon? Coupon { get; set; }

    public BookingStatus Status { get; set; } = BookingStatus.Pending;

    public BookingType Type { get; set; } = BookingType.Hourly;

    /// <summary>For monthly bookings only.</summary>
    public MonthlyPlan? MonthlyPlan { get; set; }

    /// <summary>For monthly bookings: total visits in the month.</summary>
    public int? MonthlyVisits { get; set; }

    /// <summary>For monthly bookings: contract end date (BookingDate is start).</summary>
    public DateTime? ContractEndDate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CancelledAt { get; set; }

    [StringLength(500)]
    public string? CancellationReason { get; set; }

    public Payment? Payment { get; set; }
    public Review? Review { get; set; }

    public ICollection<MonthlyVisit> Visits { get; set; } = new List<MonthlyVisit>();
}

public class MonthlyVisit
{
    public int Id { get; set; }

    public int BookingId { get; set; }
    public Booking? Booking { get; set; }

    public DateTime ScheduledDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }

    public DayOfWeek DayOfWeek { get; set; }

    public BookingStatus Status { get; set; } = BookingStatus.Pending;
    public DateTime? CompletedAt { get; set; }
}

public class Payment
{
    public int Id { get; set; }

    [StringLength(120)]
    public string TransactionRef { get; set; } = string.Empty;

    public int BookingId { get; set; }
    public Booking? Booking { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Amount { get; set; }

    public PaymentMethod Method { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    [StringLength(50)]
    public string? CardLast4 { get; set; }

    [StringLength(80)]
    public string? CardHolderName { get; set; }

    [StringLength(500)]
    public string? GatewayResponse { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; set; }
}

public class Coupon
{
    public int Id { get; set; }

    [Required, StringLength(30)]
    public string Code { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Description { get; set; }

    public DiscountType DiscountType { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal DiscountValue { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? MinOrderAmount { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? MaxDiscountAmount { get; set; }

    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }

    public int? UsageLimit { get; set; }
    public int UsedCount { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}

public class Offer
{
    public int Id { get; set; }

    [Required, StringLength(150)]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [StringLength(500)]
    public string? ImageUrl { get; set; }

    public DiscountType DiscountType { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal DiscountValue { get; set; }

    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
