using HomeMaids.Data;
using HomeMaids.Models;
using HomeMaids.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HomeMaids.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = DbInitializer.AdminRole)]
[Route("Admin/[controller]/[action]")]
public class EmailSettingsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IEmailOtpSender _sender;
    private readonly ILogger<EmailSettingsController> _logger;

    public EmailSettingsController(ApplicationDbContext db, IEmailOtpSender sender, ILogger<EmailSettingsController> logger)
    {
        _db = db;
        _sender = sender;
        _logger = logger;
    }

    private async Task<EmailConfig> GetOrCreateAsync()
    {
        var cfg = await _db.EmailConfigs.FirstOrDefaultAsync();
        if (cfg == null)
        {
            cfg = new EmailConfig
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                Username = "althiqaglobalom@gmail.com",
                FromEmail = "althiqaglobalom@gmail.com",
                FromName = "الثقة العالمية لخدمات التنظيف",
                IsActive = true,
                ShowOtpInDev = true,
                UpdatedAt = DateTime.UtcNow
            };
            _db.EmailConfigs.Add(cfg);
            await _db.SaveChangesAsync();
        }
        return cfg;
    }

    [HttpGet]
    public async Task<IActionResult> Index() => View(await GetOrCreateAsync());

    [HttpPost]
    public async Task<IActionResult> Save(EmailConfig model)
    {
        var cfg = await GetOrCreateAsync();
        cfg.Host = string.IsNullOrWhiteSpace(model.Host) ? "smtp.gmail.com" : model.Host.Trim();
        cfg.Port = model.Port > 0 ? model.Port : 587;
        cfg.EnableSsl = model.EnableSsl;
        cfg.Username = model.Username?.Trim();
        // Only overwrite password if user typed a new one (leave existing intact if blank)
        if (!string.IsNullOrWhiteSpace(model.AppPassword))
            cfg.AppPassword = model.AppPassword.Replace(" ", "").Trim();
        cfg.FromEmail = string.IsNullOrWhiteSpace(model.FromEmail) ? model.Username?.Trim() : model.FromEmail.Trim();
        cfg.FromName = string.IsNullOrWhiteSpace(model.FromName) ? cfg.FromName : model.FromName.Trim();
        cfg.IsActive = model.IsActive;
        cfg.ShowOtpInDev = model.ShowOtpInDev;
        cfg.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        TempData["Success"] = "تم حفظ إعدادات SMTP. اضغط زر الاختبار لإرسال رمز تجريبي.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Test(string toEmail)
    {
        if (string.IsNullOrWhiteSpace(toEmail) || !toEmail.Contains('@'))
        {
            TempData["Error"] = "أدخل بريداً صحيحاً للاختبار.";
            return RedirectToAction(nameof(Index));
        }
        var code = Random.Shared.Next(100000, 999999).ToString("D6");
        var devCode = await _sender.SendOtpAsync(toEmail.Trim(), code);
        TempData["Success"] = devCode != null
            ? $"SMTP غير مُهيّأ كاملاً. الرمز التجريبي: {code} (أكمل تعبئة App Password ثم اختبر مجدداً)"
            : $"تم إرسال بريد اختبار إلى {toEmail} — تحقق من Inbox + Spam.";
        return RedirectToAction(nameof(Index));
    }
}
