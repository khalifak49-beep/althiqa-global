using HomeMaids.Data;
using HomeMaids.Models;
using Microsoft.EntityFrameworkCore;

namespace HomeMaids.Services;

public class OtpSendResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string? DevCode { get; set; } // populated only by LogSmsSender (dev visibility)
    public DateTime ExpiresAt { get; set; }
}

public class OtpVerifyResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
}

public interface IOtpService
{
    /// <summary>E.164 normalize Oman phone (default country code +968).</summary>
    string NormalizePhone(string phone);
    Task<OtpSendResult> SendAsync(string phone);
    Task<OtpVerifyResult> VerifyAsync(string phone, string code);
}

public class OtpService : IOtpService
{
    private const int OtpLifetimeMinutes = 5;
    private const int MaxAttempts = 5;
    private const int ThrottleSeconds = 30;

    private readonly ApplicationDbContext _db;
    private readonly ISmsSender _sms;
    private readonly ILogger<OtpService> _logger;

    public OtpService(ApplicationDbContext db, ISmsSender sms, ILogger<OtpService> logger)
    {
        _db = db;
        _sms = sms;
        _logger = logger;
    }

    public string NormalizePhone(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return string.Empty;
        var digits = new string(phone.Where(c => char.IsDigit(c)).ToArray());
        // Drop any leading 00 (international) → use +
        if (digits.StartsWith("00")) digits = digits[2..];
        // Local 8-digit Oman → prefix 968
        if (digits.Length == 8) digits = "968" + digits;
        // Ensure leading + handled later
        return "+" + digits;
    }

    public async Task<OtpSendResult> SendAsync(string phone)
    {
        var normalized = NormalizePhone(phone);
        if (normalized.Length < 8)
            return new OtpSendResult { Success = false, Error = "رقم جوال غير صالح." };

        // Throttle: prevent spam (one OTP per 30 seconds per phone)
        var recent = await _db.PhoneOtps
            .Where(o => o.Phone == normalized)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync();

        if (recent != null && (DateTime.UtcNow - recent.CreatedAt).TotalSeconds < ThrottleSeconds)
        {
            var wait = ThrottleSeconds - (int)(DateTime.UtcNow - recent.CreatedAt).TotalSeconds;
            return new OtpSendResult { Success = false, Error = $"يرجى الانتظار {wait} ثانية قبل طلب رمز جديد." };
        }

        var code = Random.Shared.Next(100000, 999999).ToString("D6");
        var record = new PhoneOtp
        {
            Phone = normalized,
            Code = code,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(OtpLifetimeMinutes),
            Attempts = 0,
            Used = false
        };
        _db.PhoneOtps.Add(record);
        await _db.SaveChangesAsync();

        var devCode = await _sms.SendOtpAsync(normalized, code);
        return new OtpSendResult { Success = true, ExpiresAt = record.ExpiresAt, DevCode = devCode };
    }

    public async Task<OtpVerifyResult> VerifyAsync(string phone, string code)
    {
        var normalized = NormalizePhone(phone);
        var otp = await _db.PhoneOtps
            .Where(o => o.Phone == normalized && !o.Used)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync();

        if (otp == null)
            return new OtpVerifyResult { Success = false, Error = "لم يتم إرسال رمز لهذا الرقم. أعد المحاولة." };

        if (otp.ExpiresAt < DateTime.UtcNow)
            return new OtpVerifyResult { Success = false, Error = "انتهت صلاحية الرمز. اطلب رمزاً جديداً." };

        if (otp.Attempts >= MaxAttempts)
            return new OtpVerifyResult { Success = false, Error = "تجاوزت الحد المسموح من المحاولات. اطلب رمزاً جديداً." };

        otp.Attempts++;
        if (otp.Code != (code ?? string.Empty).Trim())
        {
            await _db.SaveChangesAsync();
            return new OtpVerifyResult { Success = false, Error = "الرمز غير صحيح." };
        }

        otp.Used = true;
        await _db.SaveChangesAsync();
        _logger.LogInformation("OTP verified for {Phone}", normalized);
        return new OtpVerifyResult { Success = true };
    }
}
