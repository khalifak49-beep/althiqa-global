using HomeMaids.Data;
using HomeMaids.Models;
using HomeMaids.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HomeMaids.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = DbInitializer.AdminRole)]
public class WorkersController : Controller
{
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
    private const long MaxBytes = 5 * 1024 * 1024; // 5 MB

    private readonly ApplicationDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<WorkersController> _logger;

    public WorkersController(ApplicationDbContext db, IWebHostEnvironment env, ILogger<WorkersController> logger)
    {
        _db = db;
        _env = env;
        _logger = logger;
    }

    public async Task<IActionResult> Index(string? q)
    {
        var query = _db.Workers.Include(w => w.Service).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(w => w.FullName.Contains(q) || w.Nationality.Contains(q));
        var list = await query.OrderByDescending(w => w.CreatedAt).ToListAsync();
        ViewBag.Q = q;
        return View(list);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await PopulateServicesAsync(null);
        return View(new WorkerEditViewModel { IsActive = true, Availability = WorkerAvailability.Available });
    }

    [HttpPost]
    [RequestSizeLimit(8 * 1024 * 1024)]
    public async Task<IActionResult> Create(WorkerEditViewModel vm)
    {
        if (!ValidatePhotoFile(vm)) { await PopulateServicesAsync(vm.ServiceId); return View(vm); }
        if (!ModelState.IsValid) { await PopulateServicesAsync(vm.ServiceId); return View(vm); }

        var photoUrl = await SavePhotoAsync(vm.PhotoFile) ?? vm.PhotoUrl;

        _db.Workers.Add(new Worker
        {
            FullName = vm.FullName,
            Age = vm.Age,
            Nationality = vm.Nationality,
            YearsOfExperience = vm.YearsOfExperience,
            Languages = vm.Languages,
            Bio = vm.Bio,
            PhotoUrl = photoUrl,
            HourlyRate = vm.HourlyRate,
            Availability = vm.Availability,
            IsActive = vm.IsActive,
            ServiceId = vm.ServiceId,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        TempData["Success"] = "تمت إضافة العاملة.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var w = await _db.Workers.FindAsync(id);
        if (w == null) return NotFound();
        await PopulateServicesAsync(w.ServiceId);
        return View(new WorkerEditViewModel
        {
            Id = w.Id,
            FullName = w.FullName,
            Age = w.Age,
            Nationality = w.Nationality,
            YearsOfExperience = w.YearsOfExperience,
            Languages = w.Languages,
            Bio = w.Bio,
            PhotoUrl = w.PhotoUrl,
            HourlyRate = w.HourlyRate,
            Availability = w.Availability,
            IsActive = w.IsActive,
            ServiceId = w.ServiceId
        });
    }

    [HttpPost]
    [RequestSizeLimit(8 * 1024 * 1024)]
    public async Task<IActionResult> Edit(WorkerEditViewModel vm)
    {
        if (!ValidatePhotoFile(vm)) { await PopulateServicesAsync(vm.ServiceId); return View(vm); }
        if (!ModelState.IsValid) { await PopulateServicesAsync(vm.ServiceId); return View(vm); }

        var w = await _db.Workers.FindAsync(vm.Id);
        if (w == null) return NotFound();

        // Save uploaded file if provided; otherwise keep existing PhotoUrl on the entity.
        var uploaded = await SavePhotoAsync(vm.PhotoFile);
        if (uploaded != null)
        {
            TryDeleteOldPhoto(w.PhotoUrl);
            w.PhotoUrl = uploaded;
        }

        w.FullName = vm.FullName;
        w.Age = vm.Age;
        w.Nationality = vm.Nationality;
        w.YearsOfExperience = vm.YearsOfExperience;
        w.Languages = vm.Languages;
        w.Bio = vm.Bio;
        w.HourlyRate = vm.HourlyRate;
        w.Availability = vm.Availability;
        w.IsActive = vm.IsActive;
        w.ServiceId = vm.ServiceId;

        await _db.SaveChangesAsync();
        TempData["Success"] = "تم حفظ التغييرات.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Hard-delete a worker when no historical data references her; otherwise soft-delete (IsActive=false)
    /// to preserve booking history integrity.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var w = await _db.Workers.FindAsync(id);
        if (w == null) return NotFound();

        var hasBookings = await _db.Bookings.AnyAsync(b => b.WorkerId == id);
        if (!hasBookings)
        {
            // Safe to hard-delete: remove schedules, favorites, reviews (no bookings exist), then worker
            _db.WorkerSchedules.RemoveRange(_db.WorkerSchedules.Where(s => s.WorkerId == id));
            _db.Favorites.RemoveRange(_db.Favorites.Where(f => f.WorkerId == id));
            _db.Reviews.RemoveRange(_db.Reviews.Where(r => r.WorkerId == id));
            TryDeletePhotoFile(w.PhotoUrl);
            _db.Workers.Remove(w);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Worker {Id} ({Name}) hard-deleted by admin", id, w.FullName);
            TempData["Success"] = $"تم حذف العاملة \"{w.FullName}\" نهائياً.";
        }
        else
        {
            w.IsActive = false;
            w.Availability = WorkerAvailability.Inactive;
            await _db.SaveChangesAsync();
            _logger.LogInformation("Worker {Id} ({Name}) soft-deleted (had bookings)", id, w.FullName);
            TempData["Success"] = $"تم تعطيل \"{w.FullName}\" لاحتفاظها بحجوزات سابقة.";
        }
        return RedirectToAction(nameof(Index));
    }

    private void TryDeletePhotoFile(string? url)
    {
        if (string.IsNullOrEmpty(url)) return;
        if (!url.StartsWith("/images/workers/uploads/", StringComparison.OrdinalIgnoreCase)) return;
        try
        {
            var rel = url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var full = Path.Combine(_env.WebRootPath, rel);
            if (System.IO.File.Exists(full)) System.IO.File.Delete(full);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed deleting worker photo {Url}", url);
        }
    }

    private async Task PopulateServicesAsync(int? selected)
    {
        var services = await _db.Services.Where(s => s.IsActive).ToListAsync();
        ViewBag.Services = new SelectList(services, "Id", "Name", selected);
    }

    private bool ValidatePhotoFile(WorkerEditViewModel vm)
    {
        if (vm.PhotoFile == null || vm.PhotoFile.Length == 0) return true;
        if (vm.PhotoFile.Length > MaxBytes)
        {
            ModelState.AddModelError(nameof(vm.PhotoFile), $"حجم الصورة أكبر من {MaxBytes / (1024 * 1024)} ميجابايت.");
            return false;
        }
        var ext = Path.GetExtension(vm.PhotoFile.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
        {
            ModelState.AddModelError(nameof(vm.PhotoFile), "نوع الصورة غير مدعوم. الأنواع المسموحة: jpg, jpeg, png, webp.");
            return false;
        }
        if (!vm.PhotoFile.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(vm.PhotoFile), "الملف ليس صورة صالحة.");
            return false;
        }
        return true;
    }

    private async Task<string?> SavePhotoAsync(IFormFile? file)
    {
        if (file == null || file.Length == 0) return null;

        var dir = Path.Combine(_env.WebRootPath, "images", "workers", "uploads");
        Directory.CreateDirectory(dir);

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var safeName = $"w_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid().ToString("N")[..8]}{ext}";
        var fullPath = Path.Combine(dir, safeName);

        await using (var fs = new FileStream(fullPath, FileMode.Create))
        {
            await file.CopyToAsync(fs);
        }

        _logger.LogInformation("Worker photo saved: {Path}", fullPath);
        return $"/images/workers/uploads/{safeName}";
    }

    private void TryDeleteOldPhoto(string? oldUrl)
    {
        // Only delete files we previously uploaded (under /images/workers/uploads/).
        if (string.IsNullOrEmpty(oldUrl)) return;
        if (!oldUrl.StartsWith("/images/workers/uploads/", StringComparison.OrdinalIgnoreCase)) return;
        try
        {
            var rel = oldUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var full = Path.Combine(_env.WebRootPath, rel);
            if (System.IO.File.Exists(full)) System.IO.File.Delete(full);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete old worker photo {Url}", oldUrl);
        }
    }
}
