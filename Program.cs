using HomeMaids.Data;
using HomeMaids.Models;
using HomeMaids.Repositories;
using HomeMaids.Services;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;

// Enable legacy DateTime behavior so Npgsql accepts DateTime with Kind=Unspecified
// against timestamp-with-time-zone columns. Required because the domain uses
// DateTime.Today / form-bound dates without explicit Utc conversion.
// MUST be set before the first connection is opened.
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
AppContext.SetSwitch("Npgsql.DisableDateTimeInfinityConversions", true);

var builder = WebApplication.CreateBuilder(args);

// Honor the PORT env var (used by Render, Heroku, Railway, etc.)
var portEnv = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(portEnv))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{portEnv}");
}

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("Logs/homemaids-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();
builder.Host.UseSerilog();

// === Database provider selection ===
// On Render/Heroku/Railway: DATABASE_URL is set (postgres://...).
// Locally: falls back to SQL Server via "DefaultConnection".
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
var useNpgsql = !string.IsNullOrWhiteSpace(databaseUrl);

string connectionString;
if (useNpgsql)
{
    // Robust manual parser that tolerates unescaped @ # ! etc. in passwords
    // (System.Uri rejects unescaped reserved chars in user-info).
    // Format expected: postgres[ql]://user:password@host[:port]/database[?...]
    var raw = databaseUrl!;
    var schemeEnd = raw.IndexOf("://", StringComparison.Ordinal);
    if (schemeEnd < 0) throw new InvalidOperationException("Invalid DATABASE_URL: missing scheme.");
    var rest = raw[(schemeEnd + 3)..];

    // Split off query string
    var queryStart = rest.IndexOf('?');
    if (queryStart >= 0) rest = rest[..queryStart];

    // Split into "userinfo@hostpath" using the LAST '@' (passwords may contain '@')
    var atIdx = rest.LastIndexOf('@');
    if (atIdx < 0) throw new InvalidOperationException("Invalid DATABASE_URL: missing '@' before host.");
    var userInfoPart = rest[..atIdx];
    var hostPath = rest[(atIdx + 1)..];

    var colonIdx = userInfoPart.IndexOf(':');
    var dbUser = colonIdx >= 0 ? userInfoPart[..colonIdx] : userInfoPart;
    var dbPass = colonIdx >= 0 ? userInfoPart[(colonIdx + 1)..] : string.Empty;

    var slashIdx = hostPath.IndexOf('/');
    var hostPort = slashIdx >= 0 ? hostPath[..slashIdx] : hostPath;
    var dbName = slashIdx >= 0 ? hostPath[(slashIdx + 1)..] : "postgres";

    var hostColon = hostPort.IndexOf(':');
    var dbHost = hostColon >= 0 ? hostPort[..hostColon] : hostPort;
    var dbPort = 5432;
    if (hostColon >= 0 && int.TryParse(hostPort[(hostColon + 1)..], out var p)) dbPort = p;

    var b = new Npgsql.NpgsqlConnectionStringBuilder
    {
        Host = dbHost,
        Port = dbPort,
        Username = Uri.UnescapeDataString(dbUser),
        Password = Uri.UnescapeDataString(dbPass),
        Database = dbName,
        SslMode = Npgsql.SslMode.Prefer,
        Pooling = true,
        MaxPoolSize = 10,
        Timeout = 30,
        CommandTimeout = 60
    };
    connectionString = b.ConnectionString;
}
else
{
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (useNpgsql)
        options.UseNpgsql(connectionString);
    else
        options.UseSqlServer(connectionString);
});

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        // Stronger password requirements
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.User.RequireUniqueEmail = true;
        // Account lockout: prevent brute-force attacks
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders()
    .AddDefaultUI();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(14);
    options.SlidingExpiration = true;
    // Cookie hardening
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

builder.Services.Configure<BookingSettings>(builder.Configuration.GetSection("BookingSettings"));

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IPriceCalculator, PriceCalculator>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IThawaniGateway, ThawaniGateway>();
builder.Services.AddScoped<IOtpService, OtpService>();
builder.Services.AddScoped<ISmsSender, WhatsAppSender>();
builder.Services.AddScoped<IEmailOtpSender, GmailOtpSender>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IWorkerRecommender, WorkerRecommender>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddHttpClient();

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
});
builder.Services.AddRazorPages();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "HomeMaids Mobile API", Version = "v1" });
});

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddHttpContextAccessor();

// Trust the reverse proxy (Nginx) to forward X-Forwarded-Proto + X-Forwarded-For.
// CRITICAL: without this, Request.Scheme returns "http" behind Nginx → Thawani Success/Cancel URLs are wrong.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
    // In VPS deployments the proxy is on the same host (loopback) — accept from any private/loopback address
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

// MUST run before any other middleware that reads the URL scheme
app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "HomeMaids API v1");
        c.RoutePrefix = "swagger";
    });
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseMiddleware<HomeMaids.Services.SecurityHeadersMiddleware>();

app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<HomeMaids.Services.MaintenanceMiddleware>();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

using (var scope = app.Services.CreateScope())
{
    var sp = scope.ServiceProvider;
    var db = sp.GetRequiredService<ApplicationDbContext>();

    // Render Postgres may not be DNS-resolvable immediately on first deploy.
    // Retry with backoff so the container survives the cold-start race.
    const int maxAttempts = 6;
    for (int attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            if (useNpgsql)
                await db.Database.EnsureCreatedAsync();
            else
                await db.Database.MigrateAsync();
            break;
        }
        catch (Exception ex) when (attempt < maxAttempts)
        {
            var delay = TimeSpan.FromSeconds(Math.Min(30, 5 * attempt));
            Console.Error.WriteLine($"[DB-INIT] attempt {attempt}/{maxAttempts} failed: {ex.GetType().Name}: {ex.Message}");
            Console.Error.WriteLine($"[DB-INIT] retrying in {delay.TotalSeconds:N0}s...");
            await Task.Delay(delay);
        }
    }
    await DbInitializer.SeedRolesAndAdminAsync(sp, builder.Configuration);
}

app.Run();
