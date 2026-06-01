using System.Diagnostics;
using HomeMaids.Data;
using HomeMaids.Models;
using HomeMaids.Services;
using HomeMaids.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HomeMaids.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IWorkerRecommender _recommender;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<HomeController> _logger;

    public HomeController(
        ApplicationDbContext db,
        IWorkerRecommender recommender,
        UserManager<ApplicationUser> userManager,
        ILogger<HomeController> logger)
    {
        _db = db;
        _recommender = recommender;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);

        var topWorkers = await _recommender.RecommendAsync(userId, take: 4);
        var services = await _db.Services.Where(s => s.IsActive).ToListAsync();
        var offers = await _db.Offers
            .Where(o => o.IsActive && o.ValidTo >= DateTime.UtcNow)
            .OrderByDescending(o => o.CreatedAt)
            .Take(3)
            .ToListAsync();

        var latestReviews = await _db.Reviews
            .Include(r => r.Customer)
            .Include(r => r.Worker)
            .Where(r => r.IsApproved && r.Comment != null)
            .OrderByDescending(r => r.CreatedAt)
            .Take(6)
            .ToListAsync();

        var stats = new HomeStats
        {
            TotalWorkers = await _db.Workers.CountAsync(w => w.IsActive),
            TotalBookings = await _db.Bookings.CountAsync(),
            HappyCustomers = await _db.Users.CountAsync(),
            AverageRating = await _db.Workers.AnyAsync()
                ? Math.Round(await _db.Workers.AverageAsync(w => w.AverageRating), 1)
                : 5
        };

        return View(new HomePageViewModel
        {
            TopWorkers = topWorkers,
            Services = services,
            Offers = offers,
            LatestReviews = latestReviews,
            Stats = stats
        });
    }

    public IActionResult Privacy() => View();

    public IActionResult About() => View();

    public IActionResult Refund() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
