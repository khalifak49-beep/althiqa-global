using HomeMaids.Data;
using HomeMaids.Models;
using Microsoft.EntityFrameworkCore;

namespace HomeMaids.Services;

public interface IWorkerRecommender
{
    Task<IReadOnlyList<Worker>> RecommendAsync(string? userId, int? serviceId = null, int take = 4);
}

/// <summary>
/// Simple weighted scoring recommender combining rating, popularity, and per-user history.
/// </summary>
public class WorkerRecommender : IWorkerRecommender
{
    private readonly ApplicationDbContext _db;

    public WorkerRecommender(ApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<Worker>> RecommendAsync(string? userId, int? serviceId = null, int take = 4)
    {
        var query = _db.Workers.AsNoTracking()
            .Where(w => w.IsActive && w.Availability != WorkerAvailability.Inactive);

        if (serviceId.HasValue) query = query.Where(w => w.ServiceId == serviceId);

        HashSet<int> historyServiceIds = new();
        if (!string.IsNullOrEmpty(userId))
        {
            historyServiceIds = (await _db.Bookings.AsNoTracking()
                .Where(b => b.CustomerId == userId && b.ServiceId != null)
                .Select(b => b.ServiceId!.Value)
                .Distinct()
                .ToListAsync()).ToHashSet();
        }

        var candidates = await query.ToListAsync();

        return candidates
            .Select(w => new
            {
                Worker = w,
                Score = (double)w.AverageRating * 4
                        + Math.Log(w.TotalBookings + 1) * 1.5
                        + (historyServiceIds.Contains(w.ServiceId ?? -1) ? 2.0 : 0)
                        + (w.Availability == WorkerAvailability.Available ? 1.0 : 0)
            })
            .OrderByDescending(x => x.Score)
            .Take(take)
            .Select(x => x.Worker)
            .ToList();
    }
}
