namespace HomeMaids.Services;

/// <summary>
/// Adds standard browser security headers to every HTTP response.
/// Blocks clickjacking, MIME-sniffing, downgrades and referrer leakage.
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext ctx)
    {
        var h = ctx.Response.Headers;

        if (!h.ContainsKey("X-Content-Type-Options"))
            h["X-Content-Type-Options"] = "nosniff";

        if (!h.ContainsKey("X-Frame-Options"))
            h["X-Frame-Options"] = "SAMEORIGIN";

        if (!h.ContainsKey("Referrer-Policy"))
            h["Referrer-Policy"] = "strict-origin-when-cross-origin";

        if (!h.ContainsKey("Permissions-Policy"))
            h["Permissions-Policy"] = "geolocation=(self), camera=(), microphone=()";

        // Remove fingerprinting headers if present
        h.Remove("Server");
        h.Remove("X-Powered-By");
        h.Remove("X-AspNet-Version");
        h.Remove("X-AspNetMvc-Version");

        // Lightweight CSP — allows the CDNs the app already uses (Bootstrap, Leaflet, Google Fonts, etc.)
        if (!h.ContainsKey("Content-Security-Policy"))
        {
            h["Content-Security-Policy"] =
                "default-src 'self'; " +
                "script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://unpkg.com https://code.jquery.com https://cdnjs.cloudflare.com; " +
                "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://fonts.googleapis.com https://unpkg.com https://cdnjs.cloudflare.com; " +
                "font-src 'self' data: https://fonts.gstatic.com https://cdn.jsdelivr.net; " +
                "img-src 'self' data: blob: https:; " +
                "connect-src 'self' https://api.callmebot.com https://graph.facebook.com https://*.thawani.om https://*.tile.openstreetmap.org; " +
                "frame-ancestors 'self'; " +
                "base-uri 'self'; " +
                "form-action 'self' https://*.thawani.om;";
        }

        await _next(ctx);
    }
}
