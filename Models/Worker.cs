using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HomeMaids.Models;

public class Worker
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Range(18, 70)]
    public int Age { get; set; }

    [Required, StringLength(60)]
    public string Nationality { get; set; } = string.Empty;

    [Range(0, 50)]
    public int YearsOfExperience { get; set; }

    [StringLength(300)]
    public string Languages { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Bio { get; set; }

    [StringLength(500)]
    public string? PhotoUrl { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    [Range(0, 10000)]
    public decimal HourlyRate { get; set; }

    [Column(TypeName = "decimal(3,2)")]
    [Range(0, 5)]
    public decimal AverageRating { get; set; }

    public int TotalBookings { get; set; }

    public WorkerAvailability Availability { get; set; } = WorkerAvailability.Available;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int? ServiceId { get; set; }
    public Service? Service { get; set; }

    public ICollection<WorkerSchedule> Schedules { get; set; } = new List<WorkerSchedule>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
}

public class WorkerSchedule
{
    public int Id { get; set; }

    public int WorkerId { get; set; }
    public Worker? Worker { get; set; }

    public DayOfWeek DayOfWeek { get; set; }

    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }

    public bool IsAvailable { get; set; } = true;
}

public class Service
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [StringLength(100)]
    public string? IconClass { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal BasePrice { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Worker> Workers { get; set; } = new List<Worker>();
}

public class Favorite
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    public int WorkerId { get; set; }
    public Worker? Worker { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
