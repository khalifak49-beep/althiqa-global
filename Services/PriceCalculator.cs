using HomeMaids.Models;
using Microsoft.Extensions.Options;

namespace HomeMaids.Services;

public class PriceQuote
{
    public decimal HourlyRate { get; set; }
    public int Hours { get; set; }
    public decimal SubTotal { get; set; }
    public decimal Discount { get; set; }
    public decimal TaxableBase { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }
    public string? CouponCode { get; set; }
    public int? CouponId { get; set; }
    /// <summary>"applied" | "invalid" | "expired" | "min_amount" | "none"</summary>
    public string CouponStatus { get; set; } = "none";
    public string? CouponMessage { get; set; }
}

public interface IPriceCalculator
{
    PriceQuote Calculate(Worker worker, int hours, Coupon? coupon = null);
}

public class PriceCalculator : IPriceCalculator
{
    private readonly BookingSettings _settings;

    public PriceCalculator(IOptions<BookingSettings> settings)
    {
        _settings = settings.Value;
    }

    public PriceQuote Calculate(Worker worker, int hours, Coupon? coupon = null)
    {
        var subTotal = worker.HourlyRate * hours;
        var discount = 0m;
        var status = "none";
        string? message = null;

        if (coupon != null)
        {
            if (!coupon.IsActive)
            {
                status = "invalid"; message = "هذا الكوبون غير مفعّل.";
            }
            else if (coupon.ValidFrom > DateTime.UtcNow || coupon.ValidTo < DateTime.UtcNow)
            {
                status = "expired"; message = "هذا الكوبون منتهي الصلاحية.";
            }
            else if (coupon.UsageLimit.HasValue && coupon.UsedCount >= coupon.UsageLimit.Value)
            {
                status = "invalid"; message = "تم استنفاد عدد مرات استخدام هذا الكوبون.";
            }
            else if (coupon.MinOrderAmount.HasValue && subTotal < coupon.MinOrderAmount.Value)
            {
                status = "min_amount";
                message = $"هذا الكوبون يحتاج طلب لا يقل عن {coupon.MinOrderAmount:N3} ر.ع. (المجموع الحالي {subTotal:N3}).";
            }
            else
            {
                discount = coupon.DiscountType == DiscountType.Percent
                    ? subTotal * (coupon.DiscountValue / 100m)
                    : coupon.DiscountValue;
                if (coupon.MaxDiscountAmount.HasValue && discount > coupon.MaxDiscountAmount.Value)
                    discount = coupon.MaxDiscountAmount.Value;
                status = "applied";
                message = $"تم تطبيق الكوبون: خصم {discount:N3} ر.ع.";
            }
        }

        var taxableBase = Math.Max(0, subTotal - discount);
        var tax = Math.Round(taxableBase * (_settings.TaxPercent / 100m), 2);
        var total = Math.Round(taxableBase + tax, 2);

        return new PriceQuote
        {
            HourlyRate = worker.HourlyRate,
            Hours = hours,
            SubTotal = Math.Round(subTotal, 2),
            Discount = Math.Round(discount, 2),
            TaxableBase = Math.Round(taxableBase, 2),
            Tax = tax,
            Total = total,
            CouponCode = discount > 0 ? coupon?.Code : null,
            CouponId = discount > 0 ? coupon?.Id : null,
            CouponStatus = status,
            CouponMessage = message
        };
    }
}
