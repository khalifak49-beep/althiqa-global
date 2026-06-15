using HomeMaids.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HomeMaids.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Worker> Workers => Set<Worker>();
    public DbSet<WorkerSchedule> WorkerSchedules => Set<WorkerSchedule>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<MonthlyVisit> MonthlyVisits => Set<MonthlyVisit>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Coupon> Coupons => Set<Coupon>();
    public DbSet<Offer> Offers => Set<Offer>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Favorite> Favorites => Set<Favorite>();
    public DbSet<UserAddress> UserAddresses => Set<UserAddress>();
    public DbSet<PaymentGatewayConfig> PaymentGatewayConfigs => Set<PaymentGatewayConfig>();
    public DbSet<PhoneOtp> PhoneOtps => Set<PhoneOtp>();
    public DbSet<EmailConfig> EmailConfigs => Set<EmailConfig>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Worker>(e =>
        {
            e.HasIndex(w => w.FullName);
            e.HasIndex(w => w.Availability);
            e.HasOne(w => w.Service)
             .WithMany(s => s.Workers)
             .HasForeignKey(w => w.ServiceId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<WorkerSchedule>(e =>
        {
            e.HasIndex(s => new { s.WorkerId, s.DayOfWeek }).IsUnique();
            e.HasOne(s => s.Worker)
             .WithMany(w => w.Schedules)
             .HasForeignKey(s => s.WorkerId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Booking>(e =>
        {
            e.HasIndex(b => b.BookingNumber).IsUnique();
            e.HasIndex(b => new { b.WorkerId, b.BookingDate });
            e.HasIndex(b => b.Status);

            e.HasOne(b => b.Customer)
             .WithMany(u => u.Bookings)
             .HasForeignKey(b => b.CustomerId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(b => b.Worker)
             .WithMany(w => w.Bookings)
             .HasForeignKey(b => b.WorkerId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(b => b.Service)
             .WithMany()
             .HasForeignKey(b => b.ServiceId)
             .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(b => b.Coupon)
             .WithMany(c => c.Bookings)
             .HasForeignKey(b => b.CouponId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<MonthlyVisit>(e =>
        {
            e.HasIndex(v => new { v.BookingId, v.ScheduledDate });
            e.HasIndex(v => new { v.ScheduledDate, v.StartTime });
            e.HasOne(v => v.Booking)
             .WithMany(b => b.Visits)
             .HasForeignKey(v => v.BookingId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Payment>(e =>
        {
            e.HasIndex(p => p.TransactionRef).IsUnique();
            e.HasOne(p => p.Booking)
             .WithOne(b => b.Payment)
             .HasForeignKey<Payment>(p => p.BookingId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Coupon>(e =>
        {
            e.HasIndex(c => c.Code).IsUnique();
        });

        builder.Entity<Review>(e =>
        {
            e.HasIndex(r => new { r.BookingId }).IsUnique();
            e.HasOne(r => r.Booking)
             .WithOne(b => b.Review)
             .HasForeignKey<Review>(r => r.BookingId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(r => r.Worker)
             .WithMany(w => w.Reviews)
             .HasForeignKey(r => r.WorkerId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(r => r.Customer)
             .WithMany(u => u.Reviews)
             .HasForeignKey(r => r.CustomerId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Notification>(e =>
        {
            e.HasIndex(n => new { n.UserId, n.IsRead });
            e.HasOne(n => n.User)
             .WithMany(u => u.Notifications)
             .HasForeignKey(n => n.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Favorite>(e =>
        {
            e.HasIndex(f => new { f.UserId, f.WorkerId }).IsUnique();
            e.HasOne(f => f.User)
             .WithMany(u => u.Favorites)
             .HasForeignKey(f => f.UserId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(f => f.Worker)
             .WithMany(w => w.Favorites)
             .HasForeignKey(f => f.WorkerId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<UserAddress>(e =>
        {
            e.HasOne(a => a.User)
             .WithMany(u => u.Addresses)
             .HasForeignKey(a => a.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<PaymentGatewayConfig>(e =>
        {
            e.HasIndex(p => p.Provider).IsUnique();
        });

        builder.Entity<PhoneOtp>(e =>
        {
            e.HasIndex(o => new { o.Phone, o.CreatedAt });
        });

        // Seed default email config row (admin will fill App Password from UI)
        builder.Entity<EmailConfig>().HasData(new EmailConfig
        {
            Id = 1,
            Host = "smtp.gmail.com",
            Port = 587,
            EnableSsl = true,
            Username = "althiqaglobalom@gmail.com",
            AppPassword = "",
            FromEmail = "althiqaglobalom@gmail.com",
            FromName = "الثقة العالمية لخدمات التنظيف",
            IsActive = true,
            ShowOtpInDev = true,
            UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });

        SeedStaticData(builder);
    }

    private static void SeedStaticData(ModelBuilder builder)
    {
        builder.Entity<Service>().HasData(
            new Service { Id = 1, Name = "تنظيف عام", Description = "تنظيف منزلي شامل لكل الغرف", IconClass = "bi-house-heart", BasePrice = 3.000m, IsActive = true },
            new Service { Id = 2, Name = "تنظيف داخلي", Description = "تنظيف داخلي تفصيلي للأرضيات والمطبخ والحمامات", IconClass = "bi-house-door", BasePrice = 3.000m, IsActive = true },
            new Service { Id = 3, Name = "غسيل وكي ملابس", Description = "خدمات غسيل وكي مع التوصيل", IconClass = "bi-tsunami", BasePrice = 3.000m, IsActive = true },
            new Service { Id = 4, Name = "تنظيف خارجي", Description = "تنظيف الواجهات والأسطح والحديقة", IconClass = "bi-tree", BasePrice = 3.500m, IsActive = true }
        );

        builder.Entity<Offer>().HasData(
            new Offer
            {
                Id = 1,
                Title = "خصم الترحيب 20%",
                Description = "خصم 20% على أول حجز للعملاء الجدد",
                ImageUrl = "/images/offers/welcome.jpg",
                DiscountType = DiscountType.Percent,
                DiscountValue = 20,
                ValidFrom = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                ValidTo = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc),
                IsActive = true,
                CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Offer
            {
                Id = 2,
                Title = "عرض رمضان",
                Description = "خصم خاص بمناسبة شهر رمضان المبارك",
                ImageUrl = "/images/offers/ramadan.jpg",
                DiscountType = DiscountType.Percent,
                DiscountValue = 15,
                ValidFrom = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
                ValidTo = new DateTime(2026, 4, 30, 0, 0, 0, DateTimeKind.Utc),
                IsActive = true,
                CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );

        builder.Entity<PaymentGatewayConfig>().HasData(
            new PaymentGatewayConfig
            {
                Id = 1,
                Provider = "Thawani",
                DisplayName = "Thawani Pay",
                ApiBaseUrl = "https://checkout.thawani.om/api/v1",
                CheckoutBaseUrl = "https://checkout.thawani.om/pay/",
                // Keys MUST be set via /Admin/PaymentGateways after first deploy.
                // Never commit real keys to source control.
                SecretKey = "REPLACE_VIA_ADMIN_UI",
                PublishableKey = "REPLACE_VIA_ADMIN_UI",
                SuccessUrl = "/Payments/ThawaniSuccess",
                CancelUrl = "/Payments/ThawaniCancel",
                IsLive = false,
                IsActive = true,
                UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );

        builder.Entity<Coupon>().HasData(
            new Coupon
            {
                Id = 1,
                Code = "WELCOME20",
                Description = "خصم 20% للعملاء الجدد (حد أقصى 5 ر.ع.)",
                DiscountType = DiscountType.Percent,
                DiscountValue = 20,
                MinOrderAmount = 5,
                MaxDiscountAmount = 5,
                ValidFrom = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                ValidTo = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc),
                UsageLimit = 1000,
                UsedCount = 0,
                IsActive = true
            },
            new Coupon
            {
                Id = 2,
                Code = "SAVE2",
                Description = "خصم 2 ر.ع. على الحجوزات أكثر من 8 ر.ع.",
                DiscountType = DiscountType.FixedAmount,
                DiscountValue = 2,
                MinOrderAmount = 8,
                ValidFrom = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                ValidTo = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc),
                UsageLimit = 500,
                UsedCount = 0,
                IsActive = true
            }
        );

        var seedDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        builder.Entity<Worker>().HasData(
            new Worker { Id = 1, FullName = "فاطمة عبدالله", Age = 32, Nationality = "فلبينية", YearsOfExperience = 8, Languages = "العربية، الإنجليزية", Bio = "خبرة طويلة في تنظيف المنازل والاهتمام بالتفاصيل.", PhotoUrl = "/images/workers/w1.jpg", HourlyRate = 3.000m, AverageRating = 4.8m, TotalBookings = 124, Availability = WorkerAvailability.Available, IsActive = true, ServiceId = 1, CreatedAt = seedDate },
            new Worker { Id = 2, FullName = "ماريا سانتوس", Age = 29, Nationality = "فلبينية", YearsOfExperience = 5, Languages = "الإنجليزية", Bio = "متخصصة في التنظيف الداخلي وغسيل الستائر.", PhotoUrl = "/images/workers/w2.jpg", HourlyRate = 3.000m, AverageRating = 4.7m, TotalBookings = 89, Availability = WorkerAvailability.Available, IsActive = true, ServiceId = 2, CreatedAt = seedDate },
            new Worker { Id = 3, FullName = "أمينة كانتي", Age = 34, Nationality = "كينية", YearsOfExperience = 7, Languages = "الإنجليزية، السواحلية", Bio = "خبرة في تنظيف الواجهات والأسطح.", PhotoUrl = "/images/workers/w3.jpg", HourlyRate = 3.500m, AverageRating = 4.9m, TotalBookings = 156, Availability = WorkerAvailability.Available, IsActive = true, ServiceId = 4, CreatedAt = seedDate },
            new Worker { Id = 4, FullName = "ساندرا ميندوزا", Age = 27, Nationality = "إندونيسية", YearsOfExperience = 4, Languages = "الإندونيسية، الإنجليزية", Bio = "متخصصة في غسيل وكي الملابس.", PhotoUrl = "/images/workers/w4.jpg", HourlyRate = 3.000m, AverageRating = 4.6m, TotalBookings = 67, Availability = WorkerAvailability.Busy, IsActive = true, ServiceId = 3, CreatedAt = seedDate },
            new Worker { Id = 5, FullName = "ليلى نصور", Age = 31, Nationality = "إثيوبية", YearsOfExperience = 6, Languages = "العربية، الأمهرية", Bio = "تنظيف عام واهتمام بالتفاصيل.", PhotoUrl = "/images/workers/w5.jpg", HourlyRate = 3.000m, AverageRating = 4.85m, TotalBookings = 98, Availability = WorkerAvailability.Available, IsActive = true, ServiceId = 1, CreatedAt = seedDate },
            new Worker { Id = 6, FullName = "كلارا أوغوستو", Age = 35, Nationality = "فلبينية", YearsOfExperience = 10, Languages = "الإنجليزية، التاغالوغ", Bio = "خبرة عشر سنوات في خدمة المنازل.", PhotoUrl = "/images/workers/w6.jpg", HourlyRate = 3.000m, AverageRating = 4.95m, TotalBookings = 210, Availability = WorkerAvailability.Available, IsActive = true, ServiceId = 1, CreatedAt = seedDate }
        );

        var schedules = new List<WorkerSchedule>();
        var scheduleId = 1;
        for (var workerId = 1; workerId <= 6; workerId++)
        {
            for (var d = 0; d < 7; d++)
            {
                schedules.Add(new WorkerSchedule
                {
                    Id = scheduleId++,
                    WorkerId = workerId,
                    DayOfWeek = (DayOfWeek)d,
                    StartTime = new TimeSpan(8, 0, 0),
                    EndTime = new TimeSpan(20, 0, 0),
                    IsAvailable = d != (int)DayOfWeek.Friday
                });
            }
        }
        builder.Entity<WorkerSchedule>().HasData(schedules);
    }
}
