using HomeMaids.Data;
using HomeMaids.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HomeMaids.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = DbInitializer.AdminRole)]
public class PaymentGatewaysController : Controller
{
    private readonly ApplicationDbContext _db;
    public PaymentGatewaysController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
        => View(await _db.PaymentGatewayConfigs.OrderBy(g => g.Provider).ToListAsync());

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var cfg = await _db.PaymentGatewayConfigs.FindAsync(id);
        if (cfg == null) return NotFound();
        return View(cfg);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(PaymentGatewayConfig model)
    {
        if (!ModelState.IsValid) return View(model);

        var cfg = await _db.PaymentGatewayConfigs.FindAsync(model.Id);
        if (cfg == null) return NotFound();

        cfg.DisplayName = model.DisplayName;
        cfg.ApiBaseUrl = model.ApiBaseUrl.Trim();
        cfg.CheckoutBaseUrl = model.CheckoutBaseUrl.Trim();
        cfg.SecretKey = model.SecretKey.Trim();
        cfg.PublishableKey = model.PublishableKey.Trim();
        cfg.SuccessUrl = model.SuccessUrl?.Trim();
        cfg.CancelUrl = model.CancelUrl?.Trim();
        cfg.IsLive = model.IsLive;
        cfg.IsActive = model.IsActive;
        cfg.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        TempData["Success"] = "تم حفظ إعدادات بوابة الدفع.";
        return RedirectToAction(nameof(Index));
    }
}
