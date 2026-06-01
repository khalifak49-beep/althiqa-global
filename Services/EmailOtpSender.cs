using System.Net;
using System.Net.Mail;
using HomeMaids.Models;
using Microsoft.EntityFrameworkCore;

namespace HomeMaids.Services;

public class EmailSettings
{
    public string Host { get; set; } = "smtp.gmail.com";
    public int Port { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string AppPassword { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "الثقة العالمية لخدمات التنظيف";
    public bool EnableSsl { get; set; } = true;
    /// <summary>If true and SMTP not configured, falls back to log + showing code on screen.</summary>
    public bool ShowOtpInDev { get; set; } = true;
}

public interface IEmailOtpSender
{
    /// <summary>Sends an OTP to the given email. Returns the code on screen if dev/log mode.</summary>
    Task<string?> SendOtpAsync(string email, string code);
}

public class GmailOtpSender : IEmailOtpSender
{
    private readonly IConfiguration _config;
    private readonly HomeMaids.Data.ApplicationDbContext _db;
    private readonly ILogger<GmailOtpSender> _logger;

    public GmailOtpSender(IConfiguration config, HomeMaids.Data.ApplicationDbContext db, ILogger<GmailOtpSender> logger)
    {
        _config = config;
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Loads live SMTP settings — DB first (admin-editable), then appsettings.json as fallback.
    /// </summary>
    private async Task<EmailSettings> LoadSettingsAsync()
    {
        var row = await _db.EmailConfigs
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.IsActive);

        var defaults = new EmailSettings();
        _config.GetSection("Email").Bind(defaults);

        if (row == null) return defaults;

        return new EmailSettings
        {
            Host = string.IsNullOrEmpty(row.Host) ? defaults.Host : row.Host,
            Port = row.Port > 0 ? row.Port : defaults.Port,
            EnableSsl = row.EnableSsl,
            Username = string.IsNullOrEmpty(row.Username) ? defaults.Username : row.Username,
            AppPassword = string.IsNullOrEmpty(row.AppPassword) ? defaults.AppPassword : row.AppPassword,
            FromEmail = string.IsNullOrEmpty(row.FromEmail) ? defaults.FromEmail : row.FromEmail,
            FromName = string.IsNullOrEmpty(row.FromName) ? defaults.FromName : row.FromName,
            ShowOtpInDev = row.ShowOtpInDev
        };
    }

    public async Task<string?> SendOtpAsync(string email, string code)
    {
        var opts = await LoadSettingsAsync();
        if (string.IsNullOrWhiteSpace(opts.Username) || string.IsNullOrWhiteSpace(opts.AppPassword))
        {
            _logger.LogWarning("=== EMAIL OTP DEV === To: {Email} Code: {Code} (SMTP not configured — set Username + AppPassword from /Admin/EmailSettings)", email, code);
            return opts.ShowOtpInDev ? code : null;
        }

        try
        {
            using var smtp = new SmtpClient(opts.Host, opts.Port)
            {
                Credentials = new NetworkCredential(opts.Username, opts.AppPassword),
                EnableSsl = opts.EnableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            var fromEmail = string.IsNullOrEmpty(opts.FromEmail) ? opts.Username : opts.FromEmail;
            var fromAddr = new MailAddress(fromEmail!, opts.FromName);

            using var msg = new MailMessage
            {
                From = fromAddr,
                Sender = fromAddr,
                Subject = "رمز التحقق - الثقة العالمية لخدمات التنظيف",
                SubjectEncoding = System.Text.Encoding.UTF8,
                HeadersEncoding = System.Text.Encoding.UTF8,
                Priority = MailPriority.Normal
            };
            msg.To.Add(email);
            msg.ReplyToList.Add(fromAddr);
            msg.Headers.Add("X-Mailer", "AlThiqa-OTP");
            msg.Headers.Add("List-Unsubscribe", $"<mailto:{fromEmail}?subject=unsubscribe>");

            // Multipart: plain-text first then HTML — improves Gmail deliverability significantly.
            var plain = BuildPlainBody(code);
            var html = BuildHtmlBody(code);
            msg.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(plain, System.Text.Encoding.UTF8, "text/plain"));
            msg.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(html, System.Text.Encoding.UTF8, "text/html"));

            await smtp.SendMailAsync(msg);
            _logger.LogInformation("Email OTP sent to {Email}", email);
            return null; // sent successfully — don't show code on screen
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email OTP send failed for {Email}", email);
            return opts.ShowOtpInDev ? code : null;
        }
    }

    private string BuildPlainBody(string code) =>
        $@"الثقة العالمية لخدمات التنظيف

رمز التحقق الخاص بك: {code}

الرمز صالح لمدة 5 دقائق.
لم تطلب هذا الرمز؟ تجاهل هذه الرسالة.

للتواصل: 77005570
althiqaglobalom@gmail.com";

    private string BuildHtmlBody(string code)
    {
        return $@"<!DOCTYPE html>
<html lang='ar' dir='rtl'>
<head><meta charset='UTF-8'><title>رمز التحقق</title></head>
<body style='font-family: Tahoma, Arial, sans-serif; background:#f3f8fb; padding:20px; margin:0;'>
  <table style='max-width:520px; margin:auto; background:#fff; border-radius:18px; padding:32px; box-shadow:0 6px 24px rgba(14,197,164,.10);'>
    <tr><td style='text-align:center;'>
      <h2 style='color:#0ec5a4; margin:0 0 8px;'>الثقة العالمية لخدمات التنظيف</h2>
      <p style='color:#6b7c87; margin:0 0 24px;'>Al Thiqa Global Cleaning Services</p>
      <p style='color:#0f2730; font-size:16px; margin:0 0 12px;'>رمز التحقق الخاص بك:</p>
      <div style='font-size:36px; font-weight:900; letter-spacing:8px; color:#0ec5a4; padding:16px 32px; background:linear-gradient(135deg, rgba(14,197,164,.10), rgba(28,177,216,.10)); border-radius:14px; display:inline-block; direction:ltr;'>{code}</div>
      <p style='color:#6b7c87; font-size:14px; margin:24px 0 8px;'>الرمز صالح لمدة 5 دقائق فقط.</p>
      <p style='color:#6b7c87; font-size:14px; margin:0 0 24px;'>لم تطلب هذا الرمز؟ تجاهل هذه الرسالة.</p>
      <hr style='border:0; border-top:1px solid #e2ebf0; margin:24px 0;'/>
      <p style='color:#6b7c87; font-size:12px; margin:0;'>للتواصل: <a href='tel:+96877005570' style='color:#0ec5a4;'>77005570</a> · <a href='mailto:althiqaglobalom@gmail.com' style='color:#0ec5a4;'>althiqaglobalom@gmail.com</a></p>
    </td></tr>
  </table>
</body>
</html>";
    }
}
