using System.Security.Claims;
using System.Threading.RateLimiting;
using Blazored.Toast;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.RateLimiting;
using Serilog;
using Serilog.Events;
using StreetSamurai.Blazor.Auth;
using StreetSamurai.Blazor.Components;
using StreetSamurai.Blazor.Services;
using StreetSamurai.Core.Extensions;
using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Models;
using StreetSamurai.Core.Services;
using StreetSamurai.Shared.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog — daily rolling log files in engine/logs/
var settings = new SettingsService();
var pathProvider = new FileSystemPathProvider(settings);
var logDir = pathProvider.LogDir;
Directory.CreateDirectory(logDir);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .WriteTo.Console(outputTemplate: $"{{Timestamp:{settings.TimestampFormat}}} [{{Level:u3}}] {{Message:lj}}{{NewLine}}{{Exception}}")
    .WriteTo.File(
        Path.Combine(logDir, "log-.txt"),
        rollingInterval: RollingInterval.Day,
        outputTemplate: $"{{Timestamp:{settings.TimestampFormat}}} [{{Level:u3}}] {{Message:lj}}{{NewLine}}{{Exception}}",
        retainedFileCountLimit: 90,
        shared: true)
    .CreateLogger();

builder.Host.UseSerilog();

// Razor + interactive server rendering
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Toast notifications
builder.Services.AddBlazoredToast();

// All StreetSamurai services (repos, graph, LLM, TTS, etc.)
builder.Services.AddStreetSamuraiServices();

// ReadOnly mode: set from appsettings.ReadOnly.json when ASPNETCORE_ENVIRONMENT=ReadOnly
var readOnlyState = new ReadOnlyState { IsReadOnly = builder.Configuration.GetValue<bool>("ReadOnly") };
builder.Services.AddSingleton(readOnlyState);

// Cookie authentication — hardened for production
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/api/auth/logout";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;                              // No JS access
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;     // HTTPS only
        options.Cookie.SameSite = SameSiteMode.Strict;               // No cross-site requests
        options.Cookie.Name = "__Host-SS-Auth";                       // __Host- prefix: browser enforces Secure+Path=/

        // Validate SecurityStamp on every request — rejects sessions after password/role change
        options.Events.OnValidatePrincipal = async context =>
        {
            var userId = context.Principal?.FindFirstValue("UserId");
            var stamp = context.Principal?.FindFirstValue("SecurityStamp");
            if (userId == null || stamp == null)
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync();
                return;
            }

            // Dev auto-login pseudo-user — skip DB validation.
            // DevAutoLoginMiddleware only runs in Development; in production only real users exist.
            if (userId == "dev-auto-login") return;

            var userRepo = context.HttpContext.RequestServices.GetRequiredService<UserRepository>();
            var user = userRepo.GetById(userId);
            if (user == null || user.SecurityStamp != stamp)
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync();
            }
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

// Per-IP rate limiting on login endpoint — prevents credential stuffing without DoS'ing legit users
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            // Partition by IP — each IP gets its own rate limit window
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,                   // 10 attempts per IP per window
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,                     // reject immediately, don't queue
            }));
});

// IWriteAccessProvider — Blazor implementation checks auth claims + ReadOnlyState
builder.Services.AddScoped<IWriteAccessProvider, BlazorWriteAccessProvider>();

// Toast wrapper — shows toast + logs [SS CODE] to browser console
builder.Services.AddScoped<ToastNotifier>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// Dev-only: auto-login as admin (reads DevAuth section from appsettings.Development.json)
if (app.Environment.IsDevelopment() && app.Configuration.GetSection("DevAuth").Exists())
{
    app.UseMiddleware<DevAutoLoginMiddleware>();
}

// Enforce MustChangePassword: redirect users who haven't changed their forced password.
// Without this, a user could navigate directly to any page and bypass the requirement.
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value ?? "";
    // Skip enforcement for: static files, API endpoints, the change-password page itself, and login
    if (path.StartsWith("/_") || path.StartsWith("/api/") || path == "/change-password" || path == "/login")
    {
        await next();
        return;
    }

    var userId = context.User?.FindFirst("UserId")?.Value;
    if (!string.IsNullOrEmpty(userId))
    {
        var userRepo = context.RequestServices.GetRequiredService<UserRepository>();
        var user = userRepo.GetById(userId);
        if (user?.MustChangePassword == true)
        {
            context.Response.Redirect("/change-password");
            return;
        }
    }

    await next();
});

app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(StreetSamurai.Shared.Components.Pages.Home).Assembly);

// Login endpoint — form POST from Login.razor, with antiforgery + open redirect + rate limiting
app.MapPost("/api/auth/login", async (HttpContext ctx, AuthService auth, IAntiforgery antiforgery) =>
{
    // Validate CSRF token
    try { await antiforgery.ValidateRequestAsync(ctx); }
    catch (AntiforgeryValidationException)
    {
        ctx.Response.StatusCode = 400;
        return;
    }

    var form = await ctx.Request.ReadFormAsync();
    var email = form["email"].ToString();
    var password = form["password"].ToString();
    var returnUrl = form["returnUrl"].ToString();

    // Open redirect protection: only allow local paths
    if (!AuthService.IsLocalUrl(returnUrl)) returnUrl = "/";

    var user = auth.Authenticate(email, password);
    if (user == null)
    {
        ctx.Response.Redirect("/login?error=invalid");
        return;
    }

    var claims = new[]
    {
        new Claim(ClaimTypes.Name, user.DisplayName),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(ClaimTypes.Role, user.Role),
        new Claim("UserId", user.Id),
        new Claim("SecurityStamp", user.SecurityStamp),
    };
    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    var principal = new ClaimsPrincipal(identity);

    await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

    // Force password change on first login (seeded admin, or admin-flagged accounts)
    if (user.MustChangePassword)
        ctx.Response.Redirect("/change-password");
    else
        ctx.Response.Redirect(returnUrl);
}).RequireRateLimiting("login");

// Logout endpoint — with antiforgery
app.MapPost("/api/auth/logout", async (HttpContext ctx, IAntiforgery antiforgery) =>
{
    try { await antiforgery.ValidateRequestAsync(ctx); }
    catch (AntiforgeryValidationException)
    {
        ctx.Response.StatusCode = 400;
        return;
    }

    await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    ctx.Response.Redirect("/");
});

// Open redirect protection is now in AuthService.IsLocalUrl() — single source of truth, unit-testable.

Log.Information("StreetSamurai Blazor host started");

try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
