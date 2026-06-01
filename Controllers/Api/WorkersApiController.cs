using HomeMaids.Data;
using HomeMaids.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HomeMaids.Controllers.Api;

[ApiController]
[Route("api/workers")]
public class WorkersApiController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public WorkersApiController(ApplicationDbContext db) => _db = db;

    /// <summary>قائمة العاملات بإمكانية الفلترة</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<WorkerDto>>>> List(
        string? q, int? serviceId, decimal? maxPrice, string sort = "rating", int page = 1, int pageSize = 12)
    {
        var query = _db.Workers.AsNoTracking().Include(w => w.Service).Where(w => w.IsActive);
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(w => w.FullName.Contains(q));
        if (serviceId.HasValue) query = query.Where(w => w.ServiceId == serviceId);
        if (maxPrice.HasValue) query = query.Where(w => w.HourlyRate <= maxPrice);

        query = sort switch
        {
            "price-asc" => query.OrderBy(w => w.HourlyRate),
            "price-desc" => query.OrderByDescending(w => w.HourlyRate),
            "popular" => query.OrderByDescending(w => w.TotalBookings),
            _ => query.OrderByDescending(w => w.AverageRating)
        };

        var items = await query
            .Skip(Math.Max(0, page - 1) * pageSize)
            .Take(pageSize)
            .Select(w => WorkerDto.From(w))
            .ToListAsync();
        return Ok(ApiResponse<IEnumerable<WorkerDto>>.Ok(items));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<WorkerDto>>> Get(int id)
    {
        var w = await _db.Workers.AsNoTracking().Include(x => x.Service).FirstOrDefaultAsync(x => x.Id == id && x.IsActive);
        if (w == null) return NotFound(ApiResponse<WorkerDto>.Fail("Not found"));
        return Ok(ApiResponse<WorkerDto>.Ok(WorkerDto.From(w)));
    }
}
