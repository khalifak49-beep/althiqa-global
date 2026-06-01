namespace HomeMaids.Services;

public interface ISmsSender
{
    /// <summary>Sends an SMS. Returns the literal code shown to the user if the sender is in dev/log mode (for UI display).</summary>
    Task<string?> SendOtpAsync(string phone, string code);
}

/// <summary>
/// Dev/log SMS sender. Logs the OTP to the application log AND returns it
/// so the caller can show it on screen for QA purposes. Replace with a real
/// gateway (Unifonic / Twilio / MSG91) in production.
/// </summary>
public class LogSmsSender : ISmsSender
{
    private readonly ILogger<LogSmsSender> _logger;
    private readonly IConfiguration _config;
    public LogSmsSender(ILogger<LogSmsSender> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;
    }

    public Task<string?> SendOtpAsync(string phone, string code)
    {
        _logger.LogWarning("=== DEV OTP === Phone: {Phone}  Code: {Code} (replace ISmsSender with real provider for prod)", phone, code);
        var showOnScreen = _config.GetValue<bool>("Sms:ShowOtpInDev", true);
        return Task.FromResult(showOnScreen ? code : null);
    }
}
