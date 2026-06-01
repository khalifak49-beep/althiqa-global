using HomeMaids.Data;
using HomeMaids.Models;
using HomeMaids.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HomeMaids.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = DbInitializer.AdminRole)]
public class CouponsController : Controller
{
    private readonly ApplicationDbContext _db;
    public CouponsController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var list = await _db.Coupons.OrderByDescending(c => c.Id).ToListAsync();
        return View(list);
    }

    [HttpGet]
    public IActionResult Create() => View(new CouponEditViewModel());

    [HttpPost]
    public async Task<IActionResult> Create(CouponEditViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);
        if (await _db.Coupons.AnyAsync(c => c.Code == vm.Code))
        {
            ModelState.AddModelError(nameof(vm.Code), "الكوبون موجود بالفعل.");
            return View(vm);
        }
        _db.Coupons.Add(new Coupon
        {
            Code = vm.Code.ToUpperInvariant(),
            Description = vm.Description,
            DiscountType = vm.DiscountType,
            DiscountValue = vm.DiscountValue,
            MinOrderAmount = vm.MinOrderAmount,
            MaxDiscountAmount = vm.MaxDiscountAmount,
            ValidFrom = DateTime.SpecifyKind(vm.ValidFrom, DateTimeKind.Utc),
            ValidTo = DateTime.SpecifyKind(vm.ValidTo, DateTimeKind.Utc),
            UsageLimit = vm.UsageLimit,
            IsActive = vm.IsActive
        });
        await _db.SaveChangesAsync();
        TempData["Success"] = "تمت إضافة الكوبون.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var c = await _db.Coupons.FindAsync(id);
        if (c == null) return NotFound();
        return View(new CouponEditViewModel
        {
            Id = c.Id,
            Code = c.Code,
            Description = c.Description,
            DiscountType = c.DiscountType,
            DiscountValue = c.DiscountValue,
            MinOrderAmount = c.MinOrderAmount,
            MaxDiscountAmount = c.MaxDiscountAmount,
            ValidFrom = c.ValidFrom,
            ValidTo = c.ValidTo,
            UsageLimit = c.UsageLimit,
            IsActive = c.IsActive
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(CouponEditViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);
        var c = await _db.Coupons.FindAsync(vm.Id);
        if (c == null) return NotFound();

        c.Code = vm.Code.ToUpperInvariant();
        c.Description = vm.Description;
        c.DiscountType = vm.DiscountType;
        c.DiscountValue = vm.DiscountValue;
        c.MinOrderAmount = vm.MinOrderAmount;
        c.MaxDiscountAmount = vm.MaxDiscountAmount;
        c.ValidFrom = DateTime.SpecifyKind(vm.ValidFrom, DateTimeKind.Utc);
        c.ValidTo = DateTime.SpecifyKind(vm.ValidTo, DateTimeKind.Utc);
        c.UsageLimit = vm.UsageLimit;
        c.IsActive = vm.IsActive;

        await _db.SaveChangesAsync();
        TempData["Success"] = "تم حفظ الكوبون.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var c = await _db.Coupons.FindAsync(id);
        if (c == null) return NotFound();
        c.IsActive = false;
        await _db.SaveChangesAsync();
        TempData["Success"] = "تم تعطيل الكوبون.";
        return RedirectToAction(nameof(Index));
    }
}
