using HomeMaids.Data;
using Microsoft.EntityFrameworkCore;

namespace HomeMaids.Services;

/// <summary>
/// Blocks the public from accessing the site when "MaintenanceMode" = "true" in SystemSettings.
/// Admins, the maintenance page itself, login, and static assets remain accessible.
/// </summary>
public class MaintenanceMiddleware
{
    private readonly RequestDelegate _next;
    private static DateTime _lastCheck = DateTime.MinValue;
    private static bool _isOn;
    private static string? _msg;
    private static readonly SemaphoreSlim _gate = new(1, 1);

    public MaintenanceMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext ctx, ApplicationDbContext db)
    {
        // Refresh flag every 30 seconds (cheap DB read)
        if ((DateTime.UtcNow - _lastCheck).TotalSeconds > 30)
        {
            await _gate.WaitAsync();
            try
            {
                if ((DateTime.UtcNow - _lastCheck).TotalSeconds > 30)
                {
                    var rows = await db.SystemSettings.AsNoTracking()
                        .Where(s => s.Key == "MaintenanceMode" || s.Key == "MaintenanceMessage")
                        .ToListAsync();
                    _isOn = rows.FirstOrDefault(r => r.Key == "MaintenanceMode")?.Value == "true";
                    _msg  = rows.FirstOrDefault(r => r.Key == "MaintenanceMessage")?.Value;
                    _lastCheck = DateTime.UtcNow;
                }
            }
            finally { _gate.Release(); }
        }

        if (!_isOn) { await _next(ctx); return; }

        var path = ctx.Request.Path.Value ?? "";
        // Allowed paths during maintenance: admin, login, static
        if (path.StartsWith("/Admin", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/Account/Login", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/Account/Logout", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/css", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/js", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/images", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/lib", StringComparison.OrdinalIgnoreCase) ||
            path == "/manifest.webmanifest" || path == "/sw.js" ||
            path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase))
        {
            await _next(ctx);
            return;
        }

        if (ctx.User.Identity?.IsAuthenticated == true && ctx.User.IsInRole("Admin"))
        {
            await _next(ctx);
            return;
        }

        // Serve maintenance page
        ctx.Response.StatusCode = 503;
        ctx.Response.ContentType = "text/html; charset=utf-8";
        var msg = string.IsNullOrEmpty(_msg) ? "نعتذر، الموقع تحت الصيانة وسنعود قريباً." : _msg;
        await ctx.Response.WriteAsync($@"<!DOCTYPE html>
<html lang='ar' dir='rtl'>
<head>
<meta charset='utf-8'>
<title>الموقع تحت الصيانة - الثقة العالمية</title>
<link href='https://fonts.googleapis.com/css2?family=Tajawal:wght@400;700;900&display=swap' rel='stylesheet'>
<style>
body{{margin:0;font-family:Tajawal,sans-serif;background:linear-gradient(135deg,#0ec5a4 0%,#1cb1d8 100%);min-height:100vh;display:flex;align-items:center;justify-content:center;color:#fff;padding:20px}}
.box{{background:rgba(255,255,255,.08);backdrop-filter:blur(20px);border:1px solid rgba(255,255,255,.18);border-radius:24px;padding:48px 32px;max-width:520px;text-align:center;box-shadow:0 20px 60px rgba(0,0,0,.20)}}
.icon{{font-size:80px;margin-bottom:16px}}
h1{{font-size:32px;font-weight:900;margin:0 0 12px}}
p{{font-size:17px;opacity:.92;margin:0 0 24px;line-height:1.7}}
.brand{{font-size:14px;opacity:.7;margin-top:24px;padding-top:24px;border-top:1px solid rgba(255,255,255,.18)}}
</style>
</head>
<body>
<div class='box'>
<div class='icon'>🛠️</div>
<h1>الموقع تحت الصيانة</h1>
<p>{System.Net.WebUtility.HtmlEncode(msg)}</p>
<div class='brand'>الثقة العالمية لخدمات التنظيف<br>Al Thiqa Global Cleaning Services</div>
</div>
</body>
</html>");
    }
}
