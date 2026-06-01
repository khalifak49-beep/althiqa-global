using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace HomeMaids.Services;

public class WhatsAppOptions
{
    /// <summary>"log" (dev), "callmebot", or "cloud" (Meta WhatsApp Cloud API).</summary>
    public string Mode { get; set; } = "log";

    // CallMeBot config (free, but recipient must opt-in first by texting the bot once)
    public string? CallMeBotApiKey { get; set; }

    // WhatsApp Cloud API (Meta) config
    public string? PhoneNumberId { get; set; }
    public string? AccessToken { get; set; }
    public string TemplateName { get; set; } = "authentication";
    public string TemplateLanguage { get; set; } = "ar";

    /// <summary>If true, also surfaces the OTP code on screen in fallback/log mode (dev only).</summary>
    public bool ShowOtpInDev { get; set; } = true;
}

/// <summary>
/// Multi-mode WhatsApp OTP sender. Configure via appsettings.json → "WhatsApp" section.
/// </summary>
public class WhatsAppSender : ISmsSender
{
    private readonly HttpClient _http;
    private readonly WhatsAppOptions _opts;
    private readonly ILogger<WhatsAppSender> _logger;

    public WhatsAppSender(IHttpClientFactory httpFactory, IConfiguration config, ILogger<WhatsAppSender> logger)
    {
        _http = httpFactory.CreateClient();
        _http.Timeout = TimeSpan.FromSeconds(15);
        _opts = new WhatsAppOptions();
        config.GetSection("WhatsApp").Bind(_opts);
        _logger = logger;
    }

    public async Task<string?> SendOtpAsync(string phone, string code)
    {
        var mode = (_opts.Mode ?? "log").Trim().ToLowerInvariant();
        try
        {
            return mode switch
            {
                "callmebot" => await SendViaCallMeBotAsync(phone, code),
                "cloud" => await SendViaCloudApiAsync(phone, code),
                _ => LogOnly(phone, code)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WhatsApp send failed for {Phone}; falling back to log mode", phone);
            return LogOnly(phone, code);
        }
    }

    private string? LogOnly(string phone, string code)
    {
        _logger.LogWarning("=== WHATSAPP DEV === Phone: {Phone}  Code: {Code} (set WhatsApp:Mode='callmebot' or 'cloud' to send real messages)", phone, code);
        return _opts.ShowOtpInDev ? code : null;
    }

    /// <summary>
    /// CallMeBot — free service. The recipient must first text
    /// "I allow callmebot to send me messages" to +34 644 51 95 23
    /// once to opt-in and receive their personal API key.
    /// Documentation: https://www.callmebot.com/blog/free-api-whatsapp-messages/
    /// Suitable for personal testing — NOT for end-customer production.
    /// </summary>
    private async Task<string?> SendViaCallMeBotAsync(string phone, string code)
    {
        if (string.IsNullOrWhiteSpace(_opts.CallMeBotApiKey))
        {
            _logger.LogWarning("CallMeBot API key not configured");
            return LogOnly(phone, code);
        }

        var text = $"رمز التحقق الخاص بك في الثقة العالمية لخدمات التنظيف:%0A*{code}*%0Aصالح لـ 5 دقائق فقط.%0Aلا تشاركه مع أحد.";
        var p = phone.TrimStart('+');
        var url = $"https://api.callmebot.com/whatsapp.php?phone={p}&text={text}&apikey={_opts.CallMeBotApiKey}";

        var resp = await _http.GetAsync(url);
        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode || body.Contains("ERROR", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("CallMeBot failed ({Status}): {Body}", resp.StatusCode, body);
            return LogOnly(phone, code);
        }
        _logger.LogInformation("CallMeBot OTP sent to {Phone}", phone);
        return null;
    }

    /// <summary>
    /// Meta WhatsApp Cloud API — production-grade.
    /// Setup: business.facebook.com → WhatsApp Business Account → register a number → get
    /// PhoneNumberId + permanent System User AccessToken → pre-create an "authentication" template
    /// (free tier: 1000 service conversations/month).
    /// </summary>
    private async Task<string?> SendViaCloudApiAsync(string phone, string code)
    {
        if (string.IsNullOrWhiteSpace(_opts.PhoneNumberId) || string.IsNullOrWhiteSpace(_opts.AccessToken))
        {
            _logger.LogWarning("WhatsApp Cloud API not configured");
            return LogOnly(phone, code);
        }

        var to = phone.TrimStart('+');
        var url = $"https://graph.facebook.com/v18.0/{_opts.PhoneNumberId}/messages";

        var payload = new
        {
            messaging_product = "whatsapp",
            to,
            type = "template",
            template = new
            {
                name = _opts.TemplateName,
                language = new { code = _opts.TemplateLanguage },
                components = new object[]
                {
                    new
                    {
                        type = "body",
                        parameters = new object[] { new { type = "text", text = code } }
                    },
                    new
                    {
                        type = "button",
                        sub_type = "url",
                        index = "0",
                        parameters = new object[] { new { type = "text", text = code } }
                    }
                }
            }
        };

        var json = JsonSerializer.Serialize(payload);
        using var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opts.AccessToken);

        var resp = await _http.SendAsync(req);
        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogWarning("WhatsApp Cloud API failed ({Status}): {Body}", resp.StatusCode, body);
            return LogOnly(phone, code);
        }
        _logger.LogInformation("WhatsApp Cloud API OTP sent to {Phone}", phone);
        return null;
    }
}
