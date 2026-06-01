using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using HomeMaids.Data;
using HomeMaids.Models;
using Microsoft.EntityFrameworkCore;

namespace HomeMaids.Services;

public class ThawaniSessionResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string? SessionId { get; set; }
    public string? RedirectUrl { get; set; }
}

public class ThawaniCustomer
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? Phone { get; set; }
    public string? DefaultAddress { get; set; }
    public int LoyaltyPoints { get; set; }
    public DateTime CreatedAt { get; set; }
}

public interface IThawaniGateway
{
    Task<ThawaniSessionResult> CreateSessionAsync(Booking booking, ThawaniCustomer customer, string baseAppUrl);
    Task<string?> GetSessionStatusAsync(string sessionId);
    Task<PaymentGatewayConfig?> GetActiveConfigAsync();
}

public class ThawaniGateway : IThawaniGateway
{
    private readonly ApplicationDbContext _db;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<ThawaniGateway> _logger;

    public ThawaniGateway(ApplicationDbContext db, IHttpClientFactory httpFactory, ILogger<ThawaniGateway> logger)
    {
        _db = db;
        _httpFactory = httpFactory;
        _logger = logger;
    }

    public Task<PaymentGatewayConfig?> GetActiveConfigAsync()
        => _db.PaymentGatewayConfigs.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Provider == "Thawani" && c.IsActive);

    public async Task<ThawaniSessionResult> CreateSessionAsync(Booking booking, ThawaniCustomer customer, string baseAppUrl)
    {
        var cfg = await GetActiveConfigAsync();
        if (cfg == null)
            return new ThawaniSessionResult { Success = false, Error = "بوابة الدفع غير مهيأة." };

        // Thawani requires unit_amount in baisa (1 OMR = 1000 baisa) as integer
        var amountBaisa = (int)Math.Round(booking.TotalAmount * 1000m, 0);

        var successUrl = (cfg.SuccessUrl ?? "/Payments/ThawaniSuccess").StartsWith("http")
            ? cfg.SuccessUrl!
            : $"{baseAppUrl.TrimEnd('/')}{cfg.SuccessUrl}?bookingId={booking.Id}";
        var cancelUrl = (cfg.CancelUrl ?? "/Payments/ThawaniCancel").StartsWith("http")
            ? cfg.CancelUrl!
            : $"{baseAppUrl.TrimEnd('/')}{cfg.CancelUrl}?bookingId={booking.Id}";

        var productName = booking.Type == BookingType.Monthly
            ? $"عقد شهري #{booking.BookingNumber} — {booking.MonthlyVisits} زيارة"
            : $"حجز #{booking.BookingNumber} — {booking.Hours} ساعة";

        // Thawani limits metadata to 10 key-value pairs. We pack secondary fields into compact JSON strings
        // so all useful info still travels with the payment session for reconciliation/CRM.
        var customerExtras = JsonSerializer.Serialize(new
        {
            address = customer.DefaultAddress ?? booking.Address,
            loyalty = customer.LoyaltyPoints,
            since = customer.CreatedAt.ToString("yyyy-MM-dd")
        });

        var bookingExtras = JsonSerializer.Serialize(new
        {
            type = booking.Type.ToString(),
            date = booking.BookingDate.ToString("yyyy-MM-dd"),
            start = booking.StartTime.ToString(@"hh\:mm"),
            hours = booking.Hours,
            service = booking.Service?.Name,
            address = booking.Address,
            lat = booking.Latitude,
            lng = booking.Longitude,
            monthly_plan = booking.MonthlyPlan?.ToString(),
            monthly_visits = booking.MonthlyVisits,
            contract_end = booking.ContractEndDate?.ToString("yyyy-MM-dd")
        });

        var pricing = JsonSerializer.Serialize(new
        {
            sub = booking.SubTotal,
            disc = booking.DiscountAmount,
            tax = booking.TaxAmount,
            total = booking.TotalAmount
        });

        var metadata = new Dictionary<string, string>
        {
            ["customer_id"] = customer.Id,
            ["customer_name"] = customer.FullName ?? string.Empty,
            ["customer_email"] = customer.Email ?? string.Empty,
            ["customer_phone"] = customer.Phone ?? string.Empty,
            ["customer_extras"] = customerExtras,
            ["booking_id"] = booking.Id.ToString(),
            ["booking_number"] = booking.BookingNumber,
            ["booking_extras"] = bookingExtras,
            ["worker"] = booking.Worker?.FullName ?? booking.WorkerId.ToString(),
            ["pricing_omr"] = pricing
        };

        var payload = new ThawaniSessionRequest
        {
            ClientReferenceId = booking.BookingNumber,
            Mode = "payment",
            Products = new List<ThawaniProduct>
            {
                new()
                {
                    Name = productName,
                    Quantity = 1,
                    UnitAmount = amountBaisa
                }
            },
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
            Metadata = metadata
        };

        var http = _httpFactory.CreateClient();
        http.BaseAddress = new Uri(cfg.ApiBaseUrl.TrimEnd('/') + "/");
        http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        http.DefaultRequestHeaders.Add("Thawani-Api-Key", cfg.SecretKey);

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });

        try
        {
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var resp = await http.PostAsync("checkout/session", content);
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("Thawani create-session failed {Code}: {Body}", resp.StatusCode, body);
                return new ThawaniSessionResult { Success = false, Error = $"فشل الدفع ({(int)resp.StatusCode}): {body}" };
            }

            using var doc = JsonDocument.Parse(body);
            if (!doc.RootElement.TryGetProperty("data", out var data)
                || !data.TryGetProperty("session_id", out var sid))
            {
                return new ThawaniSessionResult { Success = false, Error = "استجابة بوابة الدفع غير متوقعة." };
            }
            var sessionId = sid.GetString()!;
            var redirect = $"{cfg.CheckoutBaseUrl.TrimEnd('/')}/{sessionId}?key={cfg.PublishableKey}";
            return new ThawaniSessionResult { Success = true, SessionId = sessionId, RedirectUrl = redirect };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Thawani create-session exception");
            return new ThawaniSessionResult { Success = false, Error = "تعذر الاتصال ببوابة الدفع." };
        }
    }

    public async Task<string?> GetSessionStatusAsync(string sessionId)
    {
        var cfg = await GetActiveConfigAsync();
        if (cfg == null) return null;

        var http = _httpFactory.CreateClient();
        http.BaseAddress = new Uri(cfg.ApiBaseUrl.TrimEnd('/') + "/");
        http.DefaultRequestHeaders.Add("Thawani-Api-Key", cfg.SecretKey);

        try
        {
            using var resp = await http.GetAsync($"checkout/session/{sessionId}");
            if (!resp.IsSuccessStatusCode) return null;
            var body = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("data", out var data)
                && data.TryGetProperty("payment_status", out var st))
            {
                return st.GetString();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Thawani session status check failed");
        }
        return null;
    }

    private class ThawaniSessionRequest
    {
        [JsonPropertyName("client_reference_id")]
        public string? ClientReferenceId { get; set; }

        [JsonPropertyName("mode")]
        public string Mode { get; set; } = "payment";

        [JsonPropertyName("products")]
        public List<ThawaniProduct> Products { get; set; } = new();

        [JsonPropertyName("success_url")]
        public string? SuccessUrl { get; set; }

        [JsonPropertyName("cancel_url")]
        public string? CancelUrl { get; set; }

        [JsonPropertyName("metadata")]
        public Dictionary<string, string>? Metadata { get; set; }
    }

    private class ThawaniProduct
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }

        [JsonPropertyName("unit_amount")]
        public int UnitAmount { get; set; }
    }
}
