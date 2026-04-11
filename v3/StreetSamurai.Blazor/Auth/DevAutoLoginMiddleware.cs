using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using StreetSamurai.Core.Models;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Blazor.Auth;

/// <summary>
/// Development-only middleware that auto-authenticates as the configured admin user.
/// Reads DevAuth:Email and DevAuth:Role from appsettings.Development.json.
/// Only signs in once per browser session (cookie persists after).
/// </summary>
public class DevAutoLoginMiddleware
{
    private readonly RequestDelegate next;
    private readonly string email;
    private readonly string role;
    private readonly string displayName;

    public DevAutoLoginMiddleware(RequestDelegate next, IConfiguration config)
    {
        this.next = next;
        email = config["DevAuth:Email"] ?? "admin@streetsamurai.local";
        displayName = config["DevAuth:DisplayName"] ?? "Dev Admin";

        // Validate configured role against the allowed set to prevent privilege injection via config
        var configuredRole = config["DevAuth:Role"] ?? UserRoles.Administrator;
        role = UserRoles.All.Contains(configuredRole) ? configuredRole : UserRoles.Administrator;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!(context.User.Identity?.IsAuthenticated ?? false))
        {
            // Skip for static files and the login API to avoid redirect loops
            var path = context.Request.Path.Value ?? "";
            if (path.StartsWith("/_") || path.StartsWith("/api/"))
            {
                await next(context);
                return;
            }

            // Look up the actual user to get UserId and SecurityStamp.
            // Without these claims, OnValidatePrincipal will reject the session.
            var userRepo = context.RequestServices.GetRequiredService<UserRepository>();
            var user = userRepo.GetByEmail(email);
            var userId = user?.Id ?? "dev-auto-login";
            // Use a stable stamp so OnValidatePrincipal doesn't reject the cookie on subsequent requests.
            var securityStamp = user?.SecurityStamp ?? "dev-session-stable";

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, displayName),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, role),
                new Claim("UserId", userId),
                new Claim("SecurityStamp", securityStamp),
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            // Redirect to the same URL so the new cookie takes effect.
            // Validate the redirect target to prevent open redirect attacks.
            var redirectUrl = context.Request.Path + context.Request.QueryString;
            if (!AuthService.IsLocalUrl(redirectUrl))
                redirectUrl = "/";

            context.Response.Redirect(redirectUrl);
            return;
        }

        await next(context);
    }
}
