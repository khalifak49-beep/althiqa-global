using System.ComponentModel.DataAnnotations;
using HomeMaids.Models;

namespace HomeMaids.ViewModels;

/// <summary>
/// Validates that a boolean property is true. Use for "I accept the terms" checkboxes
/// where [Required] is unhelpful (false would also satisfy it).
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class MustBeTrueAttribute : ValidationAttribute
{
    public override bool IsValid(object? value) => value is bool b && b;
}

public class CreateMonthlyBookingViewModel
{
    [Required] public int WorkerId { get; set; }
    public int? ServiceId { get; set; }
    public Worker? Worker { get; set; }
    public IReadOnlyList<Service>? Services { get; set; }

    [Required(ErrorMessage = "تاريخ البداية مطلوب"), DataType(DataType.Date)]
    public DateTime StartDate { get; set; } = DateTime.Today.AddDays(1);

    [Required(ErrorMessage = "اختر الباقة")]
    public MonthlyPlan Plan { get; set; } = MonthlyPlan.Weekly;

    [Required, Range(2, 8, ErrorMessage = "ساعات الزيارة بين 2 و 8")]
    public int HoursPerVisit { get; set; } = 3;

    [Required, StringLength(300)]
    public string Address { get; set; } = string.Empty;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }

    [StringLength(500)] public string? Notes { get; set; }
    [StringLength(30)] public string? CouponCode { get; set; }

    /// <summary>
    /// One entry per weekly visit slot. Count must equal weekly visits in the chosen plan
    /// (Weekly=1, TwiceWeekly=2, ThriceWeekly=3, Daily=7).
    /// </summary>
    public List<MonthlyVisitSlotInput> Slots { get; set; } = new();

    [MustBeTrue(ErrorMessage = "يجب الموافقة على الشروط والأحكام قبل المتابعة")]
    public bool AcceptTerms { get; set; }
}

public class MonthlyVisitSlotInput
{
    [Required(ErrorMessage = "حدد اليوم")]
    public DayOfWeek DayOfWeek { get; set; } = DayOfWeek.Sunday;

    [Required(ErrorMessage = "حدد وقت البدء")]
    public TimeSpan StartTime { get; set; } = new(10, 0, 0);
}

public class CreateBookingViewModel
{
    [Required]
    public int WorkerId { get; set; }

    public int? ServiceId { get; set; }

    [Required(ErrorMessage = "التاريخ مطلوب")]
    [DataType(DataType.Date)]
    public DateTime BookingDate { get; set; } = DateTime.Today.AddDays(1);

    [Required(ErrorMessage = "وقت البداية مطلوب")]
    public TimeSpan StartTime { get; set; } = new(10, 0, 0);

    [Required, Range(2, 12, ErrorMessage = "عدد الساعات بين 2 و 12")]
    public int Hours { get; set; } = 3;

    [Required(ErrorMessage = "العنوان مطلوب"), StringLength(300)]
    public string Address { get; set; } = string.Empty;

    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    [StringLength(30)]
    public string? CouponCode { get; set; }

    [MustBeTrue(ErrorMessage = "يجب الموافقة على الشروط والأحكام قبل المتابعة")]
    public bool AcceptTerms { get; set; }

    public Worker? Worker { get; set; }
    public IReadOnlyList<Service>? Services { get; set; }
    public IReadOnlyList<UserAddress>? SavedAddresses { get; set; }
}

public class BookingListViewModel
{
    public IReadOnlyList<Booking> Bookings { get; set; } = Array.Empty<Booking>();
    public BookingStatus? StatusFilter { get; set; }
}

public class CheckoutViewModel
{
    public Booking Booking { get; set; } = null!;

    [Required(ErrorMessage = "اسم حامل البطاقة مطلوب"), StringLength(80)]
    public string CardHolderName { get; set; } = string.Empty;

    [Required(ErrorMessage = "رقم البطاقة مطلوب")]
    [CreditCard(ErrorMessage = "رقم البطاقة غير صالح")]
    public string CardNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "تاريخ الانتهاء مطلوب")]
    [RegularExpression(@"^(0[1-9]|1[0-2])\/\d{2}$", ErrorMessage = "MM/YY")]
    public string CardExpiry { get; set; } = string.Empty;

    [Required(ErrorMessage = "CVV مطلوب")]
    [RegularExpression(@"^\d{3,4}$", ErrorMessage = "CVV غير صالح")]
    public string Cvv { get; set; } = string.Empty;

    [Required]
    public PaymentMethod Method { get; set; } = PaymentMethod.Visa;
}

public class ReviewViewModel
{
    public int BookingId { get; set; }
    public int WorkerId { get; set; }
    public string? WorkerName { get; set; }

    [Range(1, 5, ErrorMessage = "التقييم بين 1 و 5")]
    public int Rating { get; set; } = 5;

    [StringLength(1000)]
    public string? Comment { get; set; }
}
