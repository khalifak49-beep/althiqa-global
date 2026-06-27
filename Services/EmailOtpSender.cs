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
    public string FromName { get; set; } = "Al Thiqa Global Cleaning";
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
            _logger.LogWarning("=== EMAIL OTP DEV === To: {Email} Code: {Code} (SMTP not configured)", email, code);
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

            // The From address MUST be a verified sender at the SMTP provider:
            //   - Gmail: same as the authenticated account (SPF/DKIM alignment).
            //   - Brevo / SendGrid / Mailgun: a verified sender that you confirmed via email.
            // If FromEmail is set, use it. Otherwise fall back to Username (Gmail flow).
            var fromAddress = !string.IsNullOrWhiteSpace(opts.FromEmail) ? opts.FromEmail : opts.Username;
            var fromAddr = new MailAddress(fromAddress, opts.FromName);

            using var msg = new MailMessage
            {
                From = fromAddr,
                Sender = fromAddr,
                Subject = $"Your verification code: {code}",
                SubjectEncoding = System.Text.Encoding.UTF8,
                HeadersEncoding = System.Text.Encoding.UTF8,
                BodyEncoding = System.Text.Encoding.UTF8,
                Priority = MailPriority.Normal
            };
            msg.To.Add(email);
            msg.ReplyToList.Add(fromAddr);

            // RFC headers that drastically improve inbox placement on Gmail / Outlook
            var hostDomain = fromAddress.Contains('@') ? fromAddress.Split('@')[1] : "althiqaom.com";
            var messageId = $"<{Guid.NewGuid():N}@{hostDomain}>";
            msg.Headers.Add("Message-ID", messageId);
            msg.Headers.Add("Date", DateTime.UtcNow.ToString("ddd, dd MMM yyyy HH:mm:ss") + " +0000");
            msg.Headers.Add("X-Entity-Ref-ID", Guid.NewGuid().ToString("N"));
            msg.Headers.Add("X-Mailer", "AlThiqa-Mailer/2.0");
            msg.Headers.Add("X-Priority", "3");
            msg.Headers.Add("MIME-Version", "1.0");
            // Mark as transactional, not bulk
            msg.Headers.Add("Auto-Submitted", "auto-generated");
            msg.Headers.Add("X-Auto-Response-Suppress", "OOF, AutoReply");
            // One-click unsubscribe — Gmail loves this header on transactional mail
            msg.Headers.Add("List-Unsubscribe", $"<mailto:{opts.Username}?subject=unsubscribe>");
            msg.Headers.Add("List-Unsubscribe-Post", "List-Unsubscribe=One-Click");
            // Marks it as a transactional code (Gmail bypasses promo tab)
            msg.Headers.Add("Feedback-ID", $"otp:althiqa:{hostDomain}");
            // Identify the SMTP envelope sender separately if Brevo/SendGrid expects it
            if (!string.IsNullOrWhiteSpace(opts.Username) && opts.Username != fromAddress)
            {
                msg.Headers.Add("X-Envelope-From", opts.Username);
            }

            // Plain text first, HTML second — important for spam scoring
            var plain = BuildPlainBody(code);
            var html = BuildHtmlBody(code);
            msg.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(plain, System.Text.Encoding.UTF8, "text/plain"));
            msg.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(html, System.Text.Encoding.UTF8, "text/html"));

            await smtp.SendMailAsync(msg);
            _logger.LogInformation("Email OTP sent to {Email} (Message-ID: {MessageId})", email, messageId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email OTP send failed for {Email}", email);
            return opts.ShowOtpInDev ? code : null;
        }
    }

    private string BuildPlainBody(string code) =>
        $@"Your verification code is: {code}

This code is valid for 5 minutes.
If you didn't request this, you can ignore this message.

—
Al Thiqa Global Cleaning Services
Phone: +968 77005570
althiqaom.com
";

    private string BuildHtmlBody(string code)
    {
        // Preview text (shown in inbox preview, hidden in body)
        var preview = $"Your verification code is {code}";
        return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
<meta charset=""UTF-8"">
<meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
<title>Verification code</title>
</head>
<body style=""margin:0; padding:0; background:#f4f7fa; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Arial, sans-serif; color:#0f2730;"">
  <span style=""display:none !important; opacity:0; color:transparent; height:0; width:0; font-size:1px; line-height:1px;"">{preview}</span>
  <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background:#f4f7fa; padding:24px 12px;"">
    <tr><td align=""center"">
      <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" style=""max-width:520px; width:100%; background:#ffffff; border-radius:12px; box-shadow:0 1px 3px rgba(15,39,48,.06); overflow:hidden;"">
        <tr><td style=""padding:32px 28px 24px;"">
          <h1 style=""margin:0 0 6px; font-size:18px; color:#0f2730; font-weight:600;"">Al Thiqa Global Cleaning</h1>
          <p style=""margin:0 0 24px; font-size:13px; color:#6b7c87;"">Verification code</p>

          <p style=""margin:0 0 12px; font-size:15px; color:#0f2730; line-height:1.6;"">
            Use the following one-time code to continue signing in:
          </p>

          <div style=""margin:8px 0 20px; padding:18px 24px; background:#f4f7fa; border-radius:8px; text-align:center;"">
            <span style=""font-size:32px; font-weight:700; letter-spacing:6px; color:#0f2730; font-family: 'Courier New', Courier, monospace;"">{code}</span>
          </div>

          <p style=""margin:0 0 8px; font-size:13px; color:#6b7c87;"">This code will expire in 5 minutes.</p>
          <p style=""margin:0; font-size:13px; color:#6b7c87;"">If you didn't try to sign in, you can safely ignore this email.</p>
        </td></tr>
        <tr><td style=""padding:16px 28px 24px; border-top:1px solid #eef2f5;"">
          <p style=""margin:0; font-size:12px; color:#9aa5ad; line-height:1.6;"">
            Need help? Reply to this email or call <a href=""tel:+96877005570"" style=""color:#0ec5a4; text-decoration:none;"">+968 77005570</a>.<br/>
            Al Thiqa Global Cleaning Services · Sultanate of Oman
          </p>
        </td></tr>
      </table>
    </td></tr>
  </table>
</body>
</html>";
    }
}
