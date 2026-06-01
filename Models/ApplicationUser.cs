using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HomeMaids.Models;

public class ApplicationUser : IdentityUser
{
    [Required, StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [StringLength(500)]
    public string? AvatarUrl { get; set; }

    [StringLength(250)]
    public string? DefaultAddress { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int LoyaltyPoints { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
    public ICollection<UserAddress> Addresses { get; set; } = new List<UserAddress>();
}

public class UserAddress
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    [Required, StringLength(50)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(250)]
    public string AddressLine { get; set; } = string.Empty;

    [StringLength(50)]
    public string? City { get; set; }

    [StringLength(20)]
    public string? PostalCode { get; set; }

    public bool IsDefault { get; set; }
}
