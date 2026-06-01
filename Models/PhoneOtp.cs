using System.ComponentModel.DataAnnotations;

namespace HomeMaids.Models;

public class PhoneOtp
{
    public int Id { get; set; }

    [Required, StringLength(120)]
    public string Phone { get; set; } = string.Empty;

    [Required, StringLength(6)]
    public string Code { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }

    public int Attempts { get; set; }

    public bool Used { get; set; }
}
