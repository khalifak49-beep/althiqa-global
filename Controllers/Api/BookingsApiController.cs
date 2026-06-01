using HomeMaids.Data;
using HomeMaids.Dtos;
using HomeMaids.Models;
using HomeMaids.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HomeMaids.Controllers.Api;

[ApiController]
[Authorize]
[Route("api/bookings")]
[Microsoft.AspNetCore.Mvc.IgnoreAntiforgeryToken]
public class BookingsApiController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IBookingService _bookings;
    private readonly UserManager<ApplicationUser> _userManager;

    public BookingsApiController(ApplicationDbContext db, IBookingService bookings, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _bookings = bookings;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<BookingDto>>>> Mine()
    {
        var userId = _userManager.GetUserId(User);
        var list = await _db.Bookings.AsNoTracking()
            .Include(b => b.Worker)
            .Where(b => b.CustomerId == userId)
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => BookingDto.From(b))
            .ToListAsync();
        return Ok(ApiResponse<IEnumerable<BookingDto>>.Ok(list));
    }

    [HttpGet("quote")]
    public async Task<ActionResult<ApiResponse<PriceQuote>>> Quote(int workerId, int hours, string? code)
    {
        var quote = await _bookings.QuoteAsync(workerId, hours, code);
        return Ok(ApiResponse<PriceQuote>.Ok(quote));
    }

    [HttpGet("availability")]
    public async Task<ActionResult<ApiResponse<IEnumerable<string>>>> Availability(int workerId, DateTime date, int hours)
    {
        var slots = await _bookings.GetAvailableStartTimesAsync(workerId, date, hours);
        return Ok(ApiResponse<IEnumerable<string>>.Ok(slots.Select(t => t.ToString(@"hh\:mm"))));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<BookingDto>>> Create([FromBody] CreateBookingDto dto)
    {
        var userId = _userManager.GetUserId(User)!;
        var result = await _bookings.CreateAsync(userId, new CreateBookingRequest
        {
            WorkerId = dto.WorkerId,
            ServiceId = dto.ServiceId,
            BookingDate = dto.BookingDate,
            StartTime = dto.StartTime,
            Hours = dto.Hours,
            Address = dto.Address,
            Notes = dto.Notes,
            CouponCode = dto.CouponCode
        });
        if (!result.Success) return BadRequest(ApiResponse<BookingDto>.Fail(result.Error ?? "Failed"));
        var b = await _db.Bookings.Include(x => x.Worker).FirstAsync(x => x.Id == result.Booking!.Id);
        return Ok(ApiResponse<BookingDto>.Ok(BookingDto.From(b)));
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<ActionResult<ApiResponse<string>>> Cancel(int id, [FromBody] string? reason)
    {
        var userId = _userManager.GetUserId(User)!;
        var result = await _bookings.CancelAsync(id, userId, reason);
        return result.Success
            ? Ok(ApiResponse<string>.Ok("cancelled"))
            : BadRequest(ApiResponse<string>.Fail(result.Error ?? "Failed"));
    }
}
