using HomeMaids.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeMaids.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = DbInitializer.AdminRole)]
public class BrandingController : Controller
{
    private static readonly string[] AllowedExtensions = { ".png", ".jpg", ".jpeg", ".webp", ".svg" };
    private const long MaxBytes = 5 * 1024 * 1024;

    private readonly IWebHostEnvironment _env;
    private readonly ILogger<BrandingController> _logger;

    public BrandingController(IWebHostEnvironment env, ILogger<BrandingController> logger)
    {
        _env = env;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Logo()
    {
        ViewBag.CurrentLogoPng = LogoExists("al-thiqa-full.png") ? "/images/logo/al-thiqa-full.png?v=" + Guid.NewGuid().ToString("N")[..6] : null;
        ViewBag.CurrentLogoSvg = "/images/logo/al-thiqa-full.svg";
        return View();
    }

    [HttpPost]
    [RequestSizeLimit(8 * 1024 * 1024)]
    public async Task<IActionResult> Logo(IFormFile? file, string slot = "full")
    {
        if (file == null || file.Length == 0)
        {
            TempData["Error"] = "اختر ملف الصورة أولاً.";
            return RedirectToAction(nameof(Logo));
        }
        if (file.Length > MaxBytes)
        {
            TempData["Error"] = $"حجم الملف أكبر من {MaxBytes / (1024 * 1024)} ميجابايت.";
            return RedirectToAction(nameof(Logo));
        }
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
        {
            TempData["Error"] = "صيغة غير مدعومة. المسموح: PNG, JPG, WEBP, SVG.";
            return RedirectToAction(nameof(Logo));
        }
        if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "الملف ليس صورة.";
            return RedirectToAction(nameof(Logo));
        }

        var fileName = slot switch
        {
            "icon" => "al-thiqa-icon.png",
            _ => "al-thiqa-full.png"
        };
        // We always save as PNG path so layout's primary <img src=...png> picks it up automatically.
        // SVG uploads are stored under their original ext as well (so user can still link directly).
        var dir = Path.Combine(_env.WebRootPath, "images", "logo");
        Directory.CreateDirectory(dir);
        var pngPath = Path.Combine(dir, fileName);

        await using (var fs = new FileStream(pngPath, FileMode.Create))
        {
            await file.CopyToAsync(fs);
        }

        _logger.LogInformation("Logo replaced ({Slot}): {Path}", slot, pngPath);
        TempData["Success"] = "تم تحديث الشعار. اضغط Ctrl+F5 لمسح الكاش لو لم يظهر فوراً.";
        return RedirectToAction(nameof(Logo));
    }

    private bool LogoExists(string name)
        => System.IO.File.Exists(Path.Combine(_env.WebRootPath, "images", "logo", name));
}
