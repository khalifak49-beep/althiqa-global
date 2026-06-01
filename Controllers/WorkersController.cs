using HomeMaids.Data;
using HomeMaids.Models;
using HomeMaids.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HomeMaids.Controllers;

public class WorkersController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public WorkersController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? q, int? serviceId, string? nationality,
        decimal? maxPrice, string sort = "rating", int page = 1)
    {
        const int pageSize = 9;
        var query = _db.Workers.AsNoTracking()
            .Include(w => w.Service)
            .Where(w => w.IsActive);

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(w => w.FullName.Contains(q) || w.Languages.Contains(q));

        if (serviceId.HasValue)
            query = query.Where(w => w.ServiceId == serviceId);

        if (!string.IsNullOrWhiteSpace(nationality))
            query = query.Where(w => w.Nationality == nationality);

        if (maxPrice.HasValue)
            query = query.Where(w => w.HourlyRate <= maxPrice);

        query = sort switch
        {
            "price-asc" => query.OrderBy(w => w.HourlyRate),
            "price-desc" => query.OrderByDescending(w => w.HourlyRate),
            "popular" => query.OrderByDescending(w => w.TotalBookings),
            _ => query.OrderByDescending(w => w.AverageRating)
        };

        var total = await query.CountAsync();
        var workers = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        var favIds = new HashSet<int>();
        var userId = _userManager.GetUserId(User);
        if (!string.IsNullOrEmpty(userId))
        {
            favIds = (await _db.Favorites.AsNoTracking()
                .Where(f => f.UserId == userId)
                .Select(f => f.WorkerId)
                .ToListAsync()).ToHashSet();
        }

        return View(new WorkerSearchViewModel
        {
            Workers = workers,
            Services = await _db.Services.AsNoTracking().Where(s => s.IsActive).ToListAsync(),
            Q = q,
            ServiceId = serviceId,
            Nationality = nationality,
            MaxPrice = maxPrice,
            Sort = sort,
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
            FavoriteIds = favIds
        });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var worker = await _db.Workers.AsNoTracking()
            .Include(w => w.Service)
            .Include(w => w.Schedules)
            .FirstOrDefaultAsync(w => w.Id == id && w.IsActive);

        if (worker == null) return NotFound();

        var reviews = await _db.Reviews.AsNoTracking()
            .Include(r => r.Customer)
            .Where(r => r.WorkerId == id && r.IsApproved)
            .OrderByDescending(r => r.CreatedAt)
            .Take(20)
            .ToListAsync();

        var isFav = false;
        var userId = _userManager.GetUserId(User);
        if (!string.IsNullOrEmpty(userId))
        {
            isFav = await _db.Favorites.AnyAsync(f => f.UserId == userId && f.WorkerId == id);
        }

        return View(new WorkerDetailsViewModel
        {
            Worker = worker,
            Reviews = reviews,
            Schedules = worker.Schedules.OrderBy(s => s.DayOfWeek).ToList(),
            IsFavorite = isFav
        });
    }

    [HttpPost]
    public async Task<IActionResult> ToggleFavorite(int workerId)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var existing = await _db.Favorites.FirstOrDefaultAsync(f => f.UserId == userId && f.WorkerId == workerId);
        bool isFav;
        if (existing != null)
        {
            _db.Favorites.Remove(existing);
            isFav = false;
        }
        else
        {
            _db.Favorites.Add(new Favorite { UserId = userId, WorkerId = workerId });
            isFav = true;
        }
        await _db.SaveChangesAsync();
        return Json(new { ok = true, isFavorite = isFav });
    }
}
