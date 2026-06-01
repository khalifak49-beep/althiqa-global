using System.ComponentModel.DataAnnotations;

namespace HomeMaids.Models;

public class WhatsAppConfig
{
    public int Id { get; set; }

    /// <summary>"log" (dev), "callmebot", or "cloud" (Meta Cloud API).</summary>
    [Required, StringLength(20)]
    public string Mode { get; set; } = "log";

    public bool ShowOtpInDev { get; set; } = true;

    // CallMeBot (free, opt-in per recipient)
    [StringLength(120)]
    public string? CallMeBotApiKey { get; set; }

    // Meta WhatsApp Cloud API (production)
    [StringLength(80)]
    public string? PhoneNumberId { get; set; }

    [StringLength(500)]
    public string? AccessToken { get; set; }

    [StringLength(80)]
    public string TemplateName { get; set; } = "authentication";

    [StringLength(10)]
    public string TemplateLanguage { get; set; } = "ar";

    /// <summary>Optional: WhatsApp Business Account ID (for diagnostics).</summary>
    [StringLength(80)]
    public string? BusinessAccountId { get; set; }

    /// <summary>Optional: display name of the business phone (Settings → Phone numbers in Meta).</summary>
    [StringLength(120)]
    public string? DisplayNumber { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
