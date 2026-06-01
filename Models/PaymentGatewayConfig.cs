using System.ComponentModel.DataAnnotations;

namespace HomeMaids.Models;

/// <summary>
/// Per-provider gateway credentials and endpoints.
/// Stored in DB so admin can rotate API keys / switch base URL without redeploy.
/// </summary>
public class PaymentGatewayConfig
{
    public int Id { get; set; }

    [Required, StringLength(40)]
    public string Provider { get; set; } = "Thawani";

    [StringLength(80)]
    public string DisplayName { get; set; } = "Thawani Pay";

    [Required, StringLength(300)]
    public string ApiBaseUrl { get; set; } = "https://uatcheckout.thawani.om/api/v1";

    [Required, StringLength(300)]
    public string CheckoutBaseUrl { get; set; } = "https://uatcheckout.thawani.om/pay/";

    [Required, StringLength(200)]
    public string SecretKey { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string PublishableKey { get; set; } = string.Empty;

    [StringLength(200)]
    public string? SuccessUrl { get; set; }

    [StringLength(200)]
    public string? CancelUrl { get; set; }

    public bool IsLive { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
