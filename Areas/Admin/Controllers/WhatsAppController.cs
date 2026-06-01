using HomeMaids.Data;
using HomeMaids.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace HomeMaids.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = DbInitializer.AdminRole)]
public class WhatsAppController : Controller
{
    private readonly IConfiguration _config;
    private readonly ISmsSender _sender;
    private readonly ILogger<WhatsAppController> _logger;
    private readonly IWebHostEnvironment _env;

    public WhatsAppController(IConfiguration config, ISmsSender sender, ILogger<WhatsAppController> logger, IWebHostEnvironment env)
    {
        _config = config;
        _sender = sender;
        _logger = logger;
        _env = env;
    }

    [HttpGet]
    public IActionResult Index()
    {
        var opts = new WhatsAppOptions();
        _config.GetSection("WhatsApp").Bind(opts);
        return View(opts);
    }

    [HttpPost]
    public async Task<IActionResult> Test(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            TempData["Error"] = "أدخل رقم الجوال لاختبار الإرسال.";
            return RedirectToAction(nameof(Index));
        }
        var code = Random.Shared.Next(100000, 999999).ToString("D6");
        var result = await _sender.SendOtpAsync(phone.Trim(), code);
        TempData["Success"] = result != null
            ? $"تم توليد الرمز ({code}) — وضع التطوير يعرضه على الشاشة. لإرسال حقيقي، اضبط Mode=cloud أو callmebot في appsettings."
            : $"تم إرسال الرمز فعلياً عبر واتساب إلى {phone}.";
        return RedirectToAction(nameof(Index));
    }
}
