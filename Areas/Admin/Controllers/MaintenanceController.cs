using HomeMaids.Data;
using HomeMaids.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HomeMaids.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = DbInitializer.AdminRole)]
public class MaintenanceController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<MaintenanceController> _logger;

    public MaintenanceController(ApplicationDbContext db, IWebHostEnvironment env, ILogger<MaintenanceController> logger)
    {
        _db = db;
        _env = env;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var maint = await _db.SystemSettings.FirstOrDefaultAsync(s => s.Key == "MaintenanceMode");
        ViewBag.IsOn = maint?.Value == "true";
        ViewBag.Message = (await _db.SystemSettings.FirstOrDefaultAsync(s => s.Key == "MaintenanceMessage"))?.Value;

        // Backup list
        var backupDir = Path.Combine(_env.ContentRootPath, "Backups");
        Directory.CreateDirectory(backupDir);
        ViewBag.Backups = new DirectoryInfo(backupDir).GetFiles("*.bak")
            .OrderByDescending(f => f.LastWriteTime)
            .Take(20)
            .ToList();
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Toggle(bool isOn, string? message)
    {
        async Task SetAsync(string key, string val)
        {
            var s = await _db.SystemSettings.FirstOrDefaultAsync(x => x.Key == key);
            if (s == null) _db.SystemSettings.Add(new SystemSetting { Key = key, Value = val });
            else { s.Value = val; s.UpdatedAt = DateTime.UtcNow; }
        }
        await SetAsync("MaintenanceMode", isOn ? "true" : "false");
        await SetAsync("MaintenanceMessage", message ?? "");
        await _db.SaveChangesAsync();

        TempData["Success"] = isOn ? "تم تفعيل وضع الصيانة. الموقع يعرض صفحة الصيانة لغير الأدمن." : "تم إيقاف وضع الصيانة.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Backup()
    {
        var backupDir = Path.Combine(_env.ContentRootPath, "Backups");
        Directory.CreateDirectory(backupDir);
        var fileName = $"HomeMaidsDb-{DateTime.Now:yyyyMMdd-HHmmss}.bak";
        var fullPath = Path.Combine(backupDir, fileName);
        try
        {
            var sql = $"BACKUP DATABASE [HomeMaidsDb] TO DISK = N'{fullPath}' WITH FORMAT, INIT, COMPRESSION, NAME = N'HomeMaidsDb-{DateTime.Now:yyyyMMdd}';";
            await _db.Database.ExecuteSqlRawAsync(sql);
            TempData["Success"] = $"تم إنشاء نسخة احتياطية: {fileName}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Backup failed for {FullPath}", fullPath);
            TempData["Error"] = "فشل النسخ الاحتياطي. راجع السجلات للتفاصيل.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult Download(string fileName)
    {
        var backupDir = Path.Combine(_env.ContentRootPath, "Backups");
        var fullPath = Path.GetFullPath(Path.Combine(backupDir, fileName));
        if (!fullPath.StartsWith(Path.GetFullPath(backupDir), StringComparison.OrdinalIgnoreCase) || !System.IO.File.Exists(fullPath))
            return NotFound();
        return PhysicalFile(fullPath, "application/octet-stream", fileName);
    }

    [HttpPost]
    public IActionResult Delete(string fileName)
    {
        var backupDir = Path.Combine(_env.ContentRootPath, "Backups");
        var fullPath = Path.GetFullPath(Path.Combine(backupDir, fileName));
        if (fullPath.StartsWith(Path.GetFullPath(backupDir), StringComparison.OrdinalIgnoreCase) && System.IO.File.Exists(fullPath))
        {
            System.IO.File.Delete(fullPath);
            TempData["Success"] = "تم حذف النسخة.";
        }
        return RedirectToAction(nameof(Index));
    }
}
