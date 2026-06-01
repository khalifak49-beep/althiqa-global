using HomeMaids.Models;

namespace HomeMaids.ViewModels;

public class HomePageViewModel
{
    public IReadOnlyList<Worker> TopWorkers { get; set; } = Array.Empty<Worker>();
    public IReadOnlyList<Service> Services { get; set; } = Array.Empty<Service>();
    public IReadOnlyList<Offer> Offers { get; set; } = Array.Empty<Offer>();
    public IReadOnlyList<Review> LatestReviews { get; set; } = Array.Empty<Review>();
    public HomeStats Stats { get; set; } = new();
}

public class HomeStats
{
    public int TotalWorkers { get; set; }
    public int TotalBookings { get; set; }
    public int HappyCustomers { get; set; }
    public decimal AverageRating { get; set; }
}

public class WorkerSearchViewModel
{
    public IReadOnlyList<Worker> Workers { get; set; } = Array.Empty<Worker>();
    public IReadOnlyList<Service> Services { get; set; } = Array.Empty<Service>();
    public string? Q { get; set; }
    public int? ServiceId { get; set; }
    public string? Nationality { get; set; }
    public decimal? MaxPrice { get; set; }
    public string Sort { get; set; } = "rating";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 9;
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public HashSet<int> FavoriteIds { get; set; } = new();
}

public class WorkerDetailsViewModel
{
    public Worker Worker { get; set; } = null!;
    public IReadOnlyList<Review> Reviews { get; set; } = Array.Empty<Review>();
    public IReadOnlyList<WorkerSchedule> Schedules { get; set; } = Array.Empty<WorkerSchedule>();
    public bool IsFavorite { get; set; }
}
