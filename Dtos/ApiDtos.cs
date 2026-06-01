using System.ComponentModel.DataAnnotations;
using HomeMaids.Models;

namespace HomeMaids.Dtos;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }

    public static ApiResponse<T> Ok(T data, string? msg = null) => new() { Success = true, Data = data, Message = msg };
    public static ApiResponse<T> Fail(string msg) => new() { Success = false, Message = msg };
}

public class WorkerDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Nationality { get; set; } = string.Empty;
    public int YearsOfExperience { get; set; }
    public string Languages { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? PhotoUrl { get; set; }
    public decimal HourlyRate { get; set; }
    public decimal AverageRating { get; set; }
    public int TotalBookings { get; set; }
    public string Availability { get; set; } = string.Empty;
    public string? ServiceName { get; set; }

    public static WorkerDto From(Worker w) => new()
    {
        Id = w.Id,
        FullName = w.FullName,
        Age = w.Age,
        Nationality = w.Nationality,
        YearsOfExperience = w.YearsOfExperience,
        Languages = w.Languages,
        Bio = w.Bio,
        PhotoUrl = w.PhotoUrl,
        HourlyRate = w.HourlyRate,
        AverageRating = w.AverageRating,
        TotalBookings = w.TotalBookings,
        Availability = w.Availability.ToString(),
        ServiceName = w.Service?.Name
    };
}

public class BookingDto
{
    public int Id { get; set; }
    public string BookingNumber { get; set; } = string.Empty;
    public int WorkerId { get; set; }
    public string? WorkerName { get; set; }
    public DateTime BookingDate { get; set; }
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public int Hours { get; set; }
    public string Address { get; set; } = string.Empty;
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public static BookingDto From(Booking b) => new()
    {
        Id = b.Id,
        BookingNumber = b.BookingNumber,
        WorkerId = b.WorkerId,
        WorkerName = b.Worker?.FullName,
        BookingDate = b.BookingDate,
        StartTime = b.StartTime.ToString(@"hh\:mm"),
        EndTime = b.EndTime.ToString(@"hh\:mm"),
        Hours = b.Hours,
        Address = b.Address,
        SubTotal = b.SubTotal,
        TaxAmount = b.TaxAmount,
        DiscountAmount = b.DiscountAmount,
        TotalAmount = b.TotalAmount,
        Status = b.Status.ToString(),
        CreatedAt = b.CreatedAt
    };
}

public class CreateBookingDto
{
    [Required] public int WorkerId { get; set; }
    public int? ServiceId { get; set; }
    [Required] public DateTime BookingDate { get; set; }
    [Required] public TimeSpan StartTime { get; set; }
    [Required, Range(2, 12)] public int Hours { get; set; }
    [Required, StringLength(300)] public string Address { get; set; } = string.Empty;
    [StringLength(500)] public string? Notes { get; set; }
    [StringLength(30)] public string? CouponCode { get; set; }
}

public class LoginDto
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required] public string Password { get; set; } = string.Empty;
}

public class RegisterDto
{
    [Required, StringLength(100)] public string FullName { get; set; } = string.Empty;
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required, Phone] public string PhoneNumber { get; set; } = string.Empty;
    [Required, StringLength(100, MinimumLength = 6)] public string Password { get; set; } = string.Empty;
}

public class TokenDto
{
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Role { get; set; }
}
