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
public class BookingsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IBookingService _bookings;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IInvoiceService _invoices;

    public BookingsController(
        ApplicationDbContext db,
        IBookingService bookings,
        UserManager<ApplicationUser> userManager,
        IInvoiceService invoices)
    {
        _db = db;
        _bookings = bookings;
        _userManager = userManager;
        _invoices = invoices;
    }

    private static int VisitsPerMonth(MonthlyPlan p) => p switch
    {
        MonthlyPlan.Weekly => 4,
        MonthlyPlan.TwiceWeekly => 8,
        MonthlyPlan.ThriceWeekly => 12,
        MonthlyPlan.Daily => 24,
        _ => 4
    };

    [HttpGet]
    public async Task<IActionResult> Monthly(int workerId)
    {
        var worker = await _db.Workers.AsNoTracking().Include(w => w.Service)
            .FirstOrDefaultAsync(w => w.Id == workerId && w.IsActive);
        if (worker == null) return NotFound();
        return View(new CreateMonthlyBookingViewModel
        {
            WorkerId = worker.Id,
            ServiceId = worker.ServiceId,
            Worker = worker,
            Services = await _db.Services.Where(s => s.IsActive).ToListAsync()
        });
    }

    [HttpPost]
    public async Task<IActionResult> Monthly(CreateMonthlyBookingViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            vm.Worker = await _db.Workers.Include(w => w.Service).FirstOrDefaultAsync(w => w.Id == vm.WorkerId);
            vm.Services = await _db.Services.Where(s => s.IsActive).ToListAsync();
            return View(vm);
        }

        var worker = await _db.Workers.FirstOrDefaultAsync(w => w.Id == vm.WorkerId && w.IsActive);
        if (worker == null)
        {
            ModelState.AddModelError("", "العاملة غير متاحة.");
            return View(vm);
        }

        var visits = VisitsPerMonth(vm.Plan);
        var totalHours = visits * vm.HoursPerVisit;
        var subTotal = worker.HourlyRate * totalHours;
        var taxPercent = 5m;

        // Apply coupon if provided
        var discount = 0m;
        Coupon? coupon = null;
        if (!string.IsNullOrWhiteSpace(vm.CouponCode))
        {
            coupon = await _db.Coupons.FirstOrDefaultAsync(c =>
                c.Code == vm.CouponCode &&
                c.IsActive &&
                c.ValidFrom <= DateTime.UtcNow &&
                c.ValidTo >= DateTime.UtcNow &&
                (c.UsageLimit == null || c.UsedCount < c.UsageLimit));

            if (coupon == null)
            {
                ModelState.AddModelError(nameof(vm.CouponCode), "كوبون غير صالح أو منتهي.");
                vm.Worker = await _db.Workers.Include(w => w.Service).FirstOrDefaultAsync(w => w.Id == vm.WorkerId);
                vm.Services = await _db.Services.Where(s => s.IsActive).ToListAsync();
                return View(vm);
            }
            if (coupon.MinOrderAmount.HasValue && subTotal < coupon.MinOrderAmount.Value)
            {
                ModelState.AddModelError(nameof(vm.CouponCode), $"هذا الكوبون يحتاج طلب لا يقل عن {coupon.MinOrderAmount:N3} ر.ع.");
                vm.Worker = await _db.Workers.Include(w => w.Service).FirstOrDefaultAsync(w => w.Id == vm.WorkerId);
                vm.Services = await _db.Services.Where(s => s.IsActive).ToListAsync();
                return View(vm);
            }
            discount = coupon.DiscountType == DiscountType.Percent
                ? subTotal * (coupon.DiscountValue / 100m)
                : coupon.DiscountValue;
            if (coupon.MaxDiscountAmount.HasValue && discount > coupon.MaxDiscountAmount.Value)
                discount = coupon.MaxDiscountAmount.Value;
        }

        var taxableBase = Math.Max(0, subTotal - discount);
        var tax = Math.Round(taxableBase * (taxPercent / 100m), 3);
        var total = Math.Round(taxableBase + tax, 3);

        var userId = _userManager.GetUserId(User)!;
        var booking = new Models.Booking
        {
            BookingNumber = $"HM-M-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}",
            CustomerId = userId,
            WorkerId = worker.Id,
            ServiceId = vm.ServiceId,
            BookingDate = vm.StartDate.Date,
            StartTime = new TimeSpan(8, 0, 0),
            EndTime = new TimeSpan(8, 0, 0).Add(TimeSpan.FromHours(vm.HoursPerVisit)),
            Hours = totalHours,
            Address = vm.Address,
            Latitude = vm.Latitude,
            Longitude = vm.Longitude,
            Notes = vm.Notes,
            SubTotal = Math.Round(subTotal, 3),
            DiscountAmount = Math.Round(discount, 3),
            TaxAmount = tax,
            TotalAmount = total,
            CouponId = coupon?.Id,
            Status = BookingStatus.Pending,
            Type = BookingType.Monthly,
            MonthlyPlan = vm.Plan,
            MonthlyVisits = visits,
            ContractEndDate = vm.StartDate.Date.AddMonths(1),
            CreatedAt = DateTime.UtcNow
        };

        _db.Bookings.Add(booking);
        if (coupon != null)
        {
            coupon.UsedCount += 1;
            _db.Coupons.Update(coupon);
        }
        await _db.SaveChangesAsync();
        TempData["Success"] = $"تم إنشاء عقدك الشهري برقم {booking.BookingNumber}. الزيارات: {visits}";
        return RedirectToAction("Checkout", "Payments", new { bookingId = booking.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Create(int workerId)
    {
        var worker = await _db.Workers.AsNoTracking()
            .Include(w => w.Service)
            .FirstOrDefaultAsync(w => w.Id == workerId && w.IsActive);
        if (worker == null) return NotFound();

        var userId = _userManager.GetUserId(User);
        var savedAddresses = await _db.UserAddresses.AsNoTracking()
            .Where(a => a.UserId == userId)
            .ToListAsync();

        var user = await _userManager.GetUserAsync(User);

        return View(new CreateBookingViewModel
        {
            WorkerId = worker.Id,
            ServiceId = worker.ServiceId,
            Worker = worker,
            Services = await _db.Services.Where(s => s.IsActive).ToListAsync(),
            SavedAddresses = savedAddresses,
            Address = user?.DefaultAddress ?? string.Empty
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateBookingViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            await RehydrateAsync(vm);
            return View(vm);
        }

        var userId = _userManager.GetUserId(User)!;
        var result = await _bookings.CreateAsync(userId, new CreateBookingRequest
        {
            WorkerId = vm.WorkerId,
            ServiceId = vm.ServiceId,
            BookingDate = vm.BookingDate,
            StartTime = vm.StartTime,
            Hours = vm.Hours,
            Address = vm.Address,
            Latitude = vm.Latitude,
            Longitude = vm.Longitude,
            Notes = vm.Notes,
            CouponCode = vm.CouponCode
        });

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "تعذر إنشاء الحجز.");
            await RehydrateAsync(vm);
            return View(vm);
        }

        return RedirectToAction("Checkout", "Payments", new { bookingId = result.Booking!.Id });
    }

    private async Task RehydrateAsync(CreateBookingViewModel vm)
    {
        vm.Worker = await _db.Workers.Include(w => w.Service).FirstOrDefaultAsync(w => w.Id == vm.WorkerId);
        vm.Services = await _db.Services.Where(s => s.IsActive).ToListAsync();
        var userId = _userManager.GetUserId(User);
        vm.SavedAddresses = await _db.UserAddresses.Where(a => a.UserId == userId).ToListAsync();
    }

    [HttpGet]
    public async Task<IActionResult> Quote(int workerId, int hours, string? code)
    {
        var quote = await _bookings.QuoteAsync(workerId, hours, code);
        return Json(quote);
    }

    [HttpGet]
    public async Task<IActionResult> AvailableTimes(int workerId, DateTime date, int hours)
    {
        var slots = await _bookings.GetAvailableStartTimesAsync(workerId, date, hours);
        return Json(slots.Select(t => new { value = t.ToString(@"hh\:mm"), text = t.ToString(@"hh\:mm") }));
    }

    [HttpGet]
    public async Task<IActionResult> Index(BookingStatus? status)
    {
        var userId = _userManager.GetUserId(User);
        var query = _db.Bookings.AsNoTracking()
            .Include(b => b.Worker)
            .Include(b => b.Payment)
            .Where(b => b.CustomerId == userId);

        if (status.HasValue) query = query.Where(b => b.Status == status);

        var list = await query.OrderByDescending(b => b.CreatedAt).ToListAsync();
        return View(new BookingListViewModel { Bookings = list, StatusFilter = status });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var userId = _userManager.GetUserId(User);
        var booking = await _bookings.GetDetailedAsync(id);
        if (booking == null || booking.CustomerId != userId) return NotFound();
        return View(booking);
    }

    [HttpPost]
    public async Task<IActionResult> Cancel(int id, string? reason)
    {
        var userId = _userManager.GetUserId(User)!;
        var result = await _bookings.CancelAsync(id, userId, reason);
        if (!result.Success) TempData["Error"] = result.Error;
        else TempData["Success"] = "تم إلغاء الحجز.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Invoice(int id)
    {
        var userId = _userManager.GetUserId(User);
        var booking = await _bookings.GetDetailedAsync(id);
        if (booking == null || booking.CustomerId != userId) return NotFound();
        var bytes = _invoices.GenerateInvoicePdf(booking);
        return File(bytes, "application/pdf", $"Invoice-{booking.BookingNumber}.pdf");
    }
}
