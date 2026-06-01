using HomeMaids.Models;

namespace HomeMaids.Repositories;

public interface IUnitOfWork : IAsyncDisposable
{
    IGenericRepository<Worker> Workers { get; }
    IGenericRepository<WorkerSchedule> Schedules { get; }
    IGenericRepository<Service> Services { get; }
    IGenericRepository<Booking> Bookings { get; }
    IGenericRepository<Payment> Payments { get; }
    IGenericRepository<Coupon> Coupons { get; }
    IGenericRepository<Offer> Offers { get; }
    IGenericRepository<Review> Reviews { get; }
    IGenericRepository<Notification> Notifications { get; }
    IGenericRepository<Favorite> Favorites { get; }
    IGenericRepository<UserAddress> Addresses { get; }

    Task<int> SaveChangesAsync();
}
