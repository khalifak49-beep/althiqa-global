using HomeMaids.Data;
using HomeMaids.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HomeMaids.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = DbInitializer.AdminRole)]
public class PaymentsController : Controller
{
    private readonly ApplicationDbContext _db;
    public PaymentsController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index(PaymentStatus? status)
    {
        var query = _db.Payments.Include(p => p.Booking).ThenInclude(b => b!.Customer).AsQueryable();
        if (status.HasValue) query = query.Where(p => p.Status == status);
        var list = await query.OrderByDescending(p => p.CreatedAt).Take(200).ToListAsync();
        ViewBag.Status = status;
        return View(list);
    }
}
