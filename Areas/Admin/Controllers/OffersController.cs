using HomeMaids.Data;
using HomeMaids.Models;
using HomeMaids.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HomeMaids.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = DbInitializer.AdminRole)]
public class OffersController : Controller
{
    private readonly ApplicationDbContext _db;
    public OffersController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
        => View(await _db.Offers.OrderByDescending(o => o.CreatedAt).ToListAsync());

    [HttpGet] public IActionResult Create() => View(new OfferEditViewModel());

    [HttpPost]
    public async Task<IActionResult> Create(OfferEditViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);
        _db.Offers.Add(new Offer
        {
            Title = vm.Title,
            Description = vm.Description,
            ImageUrl = vm.ImageUrl,
            DiscountType = vm.DiscountType,
            DiscountValue = vm.DiscountValue,
            ValidFrom = DateTime.SpecifyKind(vm.ValidFrom, DateTimeKind.Utc),
            ValidTo = DateTime.SpecifyKind(vm.ValidTo, DateTimeKind.Utc),
            IsActive = vm.IsActive,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        TempData["Success"] = "تمت إضافة العرض.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var o = await _db.Offers.FindAsync(id);
        if (o == null) return NotFound();
        return View(new OfferEditViewModel
        {
            Id = o.Id,
            Title = o.Title,
            Description = o.Description,
            ImageUrl = o.ImageUrl,
            DiscountType = o.DiscountType,
            DiscountValue = o.DiscountValue,
            ValidFrom = o.ValidFrom,
            ValidTo = o.ValidTo,
            IsActive = o.IsActive
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(OfferEditViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);
        var o = await _db.Offers.FindAsync(vm.Id);
        if (o == null) return NotFound();
        o.Title = vm.Title;
        o.Description = vm.Description;
        o.ImageUrl = vm.ImageUrl;
        o.DiscountType = vm.DiscountType;
        o.DiscountValue = vm.DiscountValue;
        o.ValidFrom = DateTime.SpecifyKind(vm.ValidFrom, DateTimeKind.Utc);
        o.ValidTo = DateTime.SpecifyKind(vm.ValidTo, DateTimeKind.Utc);
        o.IsActive = vm.IsActive;
        await _db.SaveChangesAsync();
        TempData["Success"] = "تم تحديث العرض.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var o = await _db.Offers.FindAsync(id);
        if (o == null) return NotFound();
        o.IsActive = false;
        await _db.SaveChangesAsync();
        TempData["Success"] = "تم تعطيل العرض.";
        return RedirectToAction(nameof(Index));
    }
}
