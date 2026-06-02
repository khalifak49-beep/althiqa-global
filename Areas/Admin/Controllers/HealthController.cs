using System.Diagnostics;
using System.Net.NetworkInformation;
using HomeMaids.Data;
using HomeMaids.Models;
using HomeMaids.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HomeMaids.Areas.Admin.Controllers;

public class HealthCheck
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "unknown";     // "ok" | "warn" | "error"
    public string Detail { get; set; } = string.Empty;
    public double? LatencyMs { get; set; }
}

public class HealthViewModel
{
    public List<HealthCheck> Checks { get; set; } = new();
    public string AppVersion { get; set; } = string.Empty;
    public string RuntimeVersion { get; set; } = string.Empty;
    public string MachineName { get; set; } = string.Empty;
    public TimeSpan Uptime { get; set; }
    public long MemoryMb { get; set; }
    public DateTime ServerTime { get; set; }
    public int CustomerCount { get; set; }
    public int BookingCount { get; set; }
    public int WorkerCount { get; set; }
}

[Area("Admin")]
[Authorize(Roles = DbInitializer.AdminRole)]
public class HealthController : Controller
{
    private static readonly DateTime _startTime = DateTime.UtcNow;
    private readonly ApplicationDbContext _db;
    private readonly IEmailOtpSender _emailSender;
    private readonly IThawaniGateway _thawani;
    private readonly IWebHostEnvironment _env;

    public HealthController(ApplicationDbContext db, IEmailOtpSender emailSender, IThawaniGateway thawani, IWebHostEnvironment env)
    {
        _db = db;
        _emailSender = emailSender;
        _thawani = thawani;
        _env = env;
    }

    public async Task<IActionResult> Index()
    {
        var vm = new HealthViewModel
        {
            AppVersion = typeof(HealthController).Assembly.GetName().Version?.ToString() ?? "1.0.0",
            RuntimeVersion = Environment.Version.ToString(),
            MachineName = Environment.MachineName,
            Uptime = DateTime.UtcNow - _startTime,
            MemoryMb = Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024,
            ServerTime = DateTime.Now
        };

        // === DB check ===
        var sw = Stopwatch.StartNew();
        try
        {
            await _db.Database.ExecuteSqlRawAsync("SELECT 1");
            sw.Stop();
            vm.Checks.Add(new HealthCheck { Name = "قاعدة البيانات SQL Server", Status = "ok", Detail = "متصل ويستجيب", LatencyMs = sw.Elapsed.TotalMilliseconds });
            vm.CustomerCount = await _db.Users.CountAsync();
            vm.BookingCount = await _db.Bookings.CountAsync();
            vm.WorkerCount = await _db.Workers.CountAsync(w => w.IsActive);
        }
        catch (Exception ex)
        {
            vm.Checks.Add(new HealthCheck { Name = "قاعدة البيانات SQL Server", Status = "error", Detail = ex.Message });
        }

        // === Email config check ===
        var emailCfg = await _db.EmailConfigs.AsNoTracking().FirstOrDefaultAsync();
        if (emailCfg == null || string.IsNullOrEmpty(emailCfg.Username) || string.IsNullOrEmpty(emailCfg.AppPassword))
            vm.Checks.Add(new HealthCheck { Name = "البريد الإلكتروني (Gmail SMTP)", Status = "warn", Detail = "غير مُهيّأ — الرمز يظهر على الشاشة فقط" });
        else
            vm.Checks.Add(new HealthCheck { Name = "البريد الإلكتروني (Gmail SMTP)", Status = "ok", Detail = $"مُهيّأ ({emailCfg.Username})" });

        // === Thawani check ===
        var thawaniCfg = await _thawani.GetActiveConfigAsync();
        if (thawaniCfg == null)
            vm.Checks.Add(new HealthCheck { Name = "بوابة Thawani للدفع", Status = "error", Detail = "غير مهيّأة" });
        else if (string.IsNullOrEmpty(thawaniCfg.SecretKey) || string.IsNullOrEmpty(thawaniCfg.PublishableKey))
            vm.Checks.Add(new HealthCheck { Name = "بوابة Thawani للدفع", Status = "warn", Detail = "المفاتيح ناقصة" });
        else
            vm.Checks.Add(new HealthCheck { Name = "بوابة Thawani للدفع", Status = "ok", Detail = $"{(thawaniCfg.IsLive ? "Live" : "UAT")} — {thawaniCfg.Provider}" });

        // === Internet (ping) ===
        sw.Restart();
        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync("1.1.1.1", 3000);
            sw.Stop();
            if (reply.Status == IPStatus.Success)
                vm.Checks.Add(new HealthCheck { Name = "الإنترنت", Status = "ok", Detail = "متصل", LatencyMs = reply.RoundtripTime });
            else
                vm.Checks.Add(new HealthCheck { Name = "الإنترنت", Status = "error", Detail = reply.Status.ToString() });
        }
        catch (Exception ex)
        {
            vm.Checks.Add(new HealthCheck { Name = "الإنترنت", Status = "error", Detail = ex.Message });
        }

        // === Disk space ===
        try
        {
            var drive = new DriveInfo(Path.GetPathRoot(_env.ContentRootPath) ?? "C:\\");
            var freeGb = drive.AvailableFreeSpace / 1024.0 / 1024.0 / 1024.0;
            var totalGb = drive.TotalSize / 1024.0 / 1024.0 / 1024.0;
            var usedPct = (1 - (drive.AvailableFreeSpace / (double)drive.TotalSize)) * 100;
            var status = usedPct > 90 ? "error" : usedPct > 75 ? "warn" : "ok";
            vm.Checks.Add(new HealthCheck
            {
                Name = "مساحة القرص",
                Status = status,
                Detail = $"{freeGb:F1} GB متاح من {totalGb:F0} GB ({usedPct:F0}% مستخدم)"
            });
        }
        catch (Exception ex)
        {
            vm.Checks.Add(new HealthCheck { Name = "مساحة القرص", Status = "warn", Detail = ex.Message });
        }

        // === Tunnel URL file ===
        var urlFile = Path.Combine(_env.ContentRootPath, "current-url.txt");
        if (System.IO.File.Exists(urlFile))
        {
            var info = new FileInfo(urlFile);
            var age = DateTime.UtcNow - info.LastWriteTimeUtc;
            vm.Checks.Add(new HealthCheck
            {
                Name = "نفق Cloudflare",
                Status = age.TotalMinutes > 15 ? "warn" : "ok",
                Detail = $"محدث منذ {(int)age.TotalMinutes} دقيقة"
            });
        }
        else
        {
            vm.Checks.Add(new HealthCheck { Name = "نفق Cloudflare", Status = "warn", Detail = "ملف الرابط غير موجود" });
        }

        return View(vm);
    }
}
