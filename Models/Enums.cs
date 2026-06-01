namespace HomeMaids.Models;

public enum BookingStatus
{
    Pending = 0,
    Confirmed = 1,
    InProgress = 2,
    Completed = 3,
    Cancelled = 4,
    Refunded = 5
}

public enum PaymentStatus
{
    Pending = 0,
    Paid = 1,
    Failed = 2,
    Refunded = 3
}

public enum PaymentMethod
{
    Visa = 0,
    MasterCard = 1,
    ApplePay = 2,
    GooglePay = 3,
    PayPal = 4,
    Cash = 5
}

public enum DiscountType
{
    Percent = 0,
    FixedAmount = 1
}

public enum NotificationType
{
    BookingConfirmed = 0,
    BookingCancelled = 1,
    PaymentSuccess = 2,
    PaymentFailed = 3,
    NewOffer = 4,
    System = 5
}

public enum WorkerAvailability
{
    Available = 0,
    Busy = 1,
    OffDuty = 2,
    Inactive = 3
}

public enum BookingType
{
    Hourly = 0,
    Monthly = 1
}

public enum MonthlyPlan
{
    /// <summary>زيارة واحدة أسبوعياً (4 زيارات شهرياً)</summary>
    Weekly = 0,
    /// <summary>زيارتان أسبوعياً (8 زيارات شهرياً)</summary>
    TwiceWeekly = 1,
    /// <summary>3 زيارات أسبوعياً (12 زيارة شهرياً)</summary>
    ThriceWeekly = 2,
    /// <summary>يومياً (24 زيارة شهرياً)</summary>
    Daily = 3
}
