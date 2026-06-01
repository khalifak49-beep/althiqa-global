using System.ComponentModel.DataAnnotations;

namespace HomeMaids.Models;

public class EmailConfig
{
    public int Id { get; set; }

    [Required, StringLength(120)] public string Host { get; set; } = "smtp.gmail.com";
    [Range(1, 65535)] public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;

    [StringLength(120)] public string? Username { get; set; }

    /// <summary>Gmail App Password (16 chars, NOT regular password).</summary>
    [StringLength(120)] public string? AppPassword { get; set; }

    [StringLength(120)] public string? FromEmail { get; set; }
    [StringLength(120)] public string FromName { get; set; } = "الثقة العالمية لخدمات التنظيف";

    public bool IsActive { get; set; } = true;
    public bool ShowOtpInDev { get; set; } = true;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
