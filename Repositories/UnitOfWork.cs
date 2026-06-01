using HomeMaids.Data;
using HomeMaids.Models;

namespace HomeMaids.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
        Workers = new GenericRepository<Worker>(context);
        Schedules = new GenericRepository<WorkerSchedule>(context);
        Services = new GenericRepository<Service>(context);
        Bookings = new GenericRepository<Booking>(context);
        Payments = new GenericRepository<Payment>(context);
        Coupons = new GenericRepository<Coupon>(context);
        Offers = new GenericRepository<Offer>(context);
        Reviews = new GenericRepository<Review>(context);
        Notifications = new GenericRepository<Notification>(context);
        Favorites = new GenericRepository<Favorite>(context);
        Addresses = new GenericRepository<UserAddress>(context);
    }

    public IGenericRepository<Worker> Workers { get; }
    public IGenericRepository<WorkerSchedule> Schedules { get; }
    public IGenericRepository<Service> Services { get; }
    public IGenericRepository<Booking> Bookings { get; }
    public IGenericRepository<Payment> Payments { get; }
    public IGenericRepository<Coupon> Coupons { get; }
    public IGenericRepository<Offer> Offers { get; }
    public IGenericRepository<Review> Reviews { get; }
    public IGenericRepository<Notification> Notifications { get; }
    public IGenericRepository<Favorite> Favorites { get; }
    public IGenericRepository<UserAddress> Addresses { get; }

    public Task<int> SaveChangesAsync() => _context.SaveChangesAsync();

    public ValueTask DisposeAsync() => _context.DisposeAsync();
}
