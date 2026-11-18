using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using MindAttic.Authentication;
using MindAttic.Authentication.Web;

namespace Prose.Hub.Hosting;

/// <summary>What the gate does with one request.</summary>
public enum GateDecision
{
    /// <summary>Let it through.</summary>
    Allow,
    /// <summary>Nobody is signed in: a page goes to /login, anything else gets 401.</summary>
    Challenge,
    /// <summary>Signed in, but not one of <see cref="HubHosting.AllowedUsers"/>: signed out and 403.</summary>
    Forbidden,
    /// <summary>Signed in and allowed, but without a TOTP step: sent to /account/mfa/setup.</summary>
    EnrollMfa,
}

/// <summary>
/// Hosted mode's access control (PRS-T1004). None of this is wired in local mode.
///
/// <para>The rule is one function, <see cref="Decide"/>, enforced twice so that neither enforcement is a
/// single point of failure: as middleware right after authentication (it sees every request, including
/// static files, the Blazor circuit and WebSocket upgrades, before anything is served), and as the
/// authorization <em>fallback policy</em> (it applies to every endpoint that carries no policy of its own,
/// so a route added later is closed without anyone remembering to close it).</para>
///
/// <para>Anonymous access is an allow-list of exact method + path pairs (<see cref="IsAnonymousAllowed"/>):
/// the sign-in pages, the two sign-in posts and <c>/healthz</c>. Everything else needs a signed-in,
/// allow-listed user, and with <see cref="HubHosting.RequireMfa"/> a TOTP step.</para>
///
/// <para><c>X-Prose-Key</c> is not consulted here and is never a substitute for the login: the routes that
/// carry <see cref="HubApiKeyFilter"/> still require the key as well. The one non-interactive caller is the
/// Hub itself: its /app page (Prose.ObserverUi) calls the Hub's own HTTP API and SignalR hub from inside the
/// process. Those calls are recognised by a per-process random token (<see cref="InternalHeader"/>) that must
/// arrive on a direct loopback connection with no proxy headers; an internet request cannot satisfy that
/// even if it guessed the header name.</para>
/// </summary>
public static class HostedGate
{
    public const string InternalHeader = "X-Prose-Internal";
    public const string LoginRateLimitPolicy = "hosted-login";
    public const string HealthPath = "/healthz";
    public const string LoginPath = "/login";
    public const string MfaPath = "/mfa";
    public const string MfaSetupPath = "/account/mfa/setup";

    private const string InternalItem = "prose.hub.internal";
    private static readonly ClaimsPrincipal InternalPrincipal = new(new ClaimsIdentity(
        [new Claim(ClaimTypes.Name, "hub-internal"), new Claim(MaClaims.Amr, "mfa")], "ProseHubInternal"));

    // ── The rule ─────────────────────────────────────────────────────────────

    /// <summary>The only requests served without a login. Exact paths; an optional trailing slash is tolerated.</summary>
    public static bool IsAnonymousAllowed(string method, PathString path)
    {
        var p = Normalize(path);
        if (HttpMethods.IsGet(method) || HttpMethods.IsHead(method))
            return Is(p, HealthPath) || Is(p, LoginPath) || Is(p, MfaPath);
        if (HttpMethods.IsPost(method))
            return Is(p, "/_ma-auth/login") || Is(p, "/_ma-auth/mfa-challenge");
        return false;
    }

    /// <summary>Pages a signed-in user may reach before the TOTP step: the account pages and the auth posts.</summary>
    public static bool IsAuthSurface(PathString path) =>
        path.StartsWithSegments("/account") || path.StartsWithSegments("/_ma-auth")
        || path.StartsWithSegments(LoginPath) || path.StartsWithSegments(MfaPath)
        || Is(Normalize(path), HealthPath);

    /// <summary>The whole access rule, pure.</summary>
    public static GateDecision Decide(string method, PathString path, bool isInternal, ClaimsPrincipal? user, HubHosting hosting)
    {
        if (isInternal) return GateDecision.Allow;
        if (IsAnonymousAllowed(method, path)) return GateDecision.Allow;
        if (user?.Identity?.IsAuthenticated != true) return GateDecision.Challenge;
        if (!hosting.IsUserAllowed(user.Identity.Name)) return GateDecision.Forbidden;
        if (hosting.RequireMfa && !user.HasClaim(MaClaims.Amr, "mfa") && !IsAuthSurface(path)) return GateDecision.EnrollMfa;
        return GateDecision.Allow;
    }

    /// <summary>Host header check: exact, case-insensitive match against the configured names.</summary>
    public static bool IsHostAllowed(string? host, IReadOnlyList<string> allowed) =>
        !string.IsNullOrEmpty(host) && allowed.Any(a => string.Equals(a, host, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// A same-site path only: starts with one <c>/</c>, no <c>//</c> or <c>/\</c>, no control characters.
    /// Stricter than the auth library's own check on purpose (it accepts <c>~/</c> forms).
    /// </summary>
    public static bool IsSafeReturnUrl(string? url)
    {
        if (string.IsNullOrEmpty(url) || url[0] != '/') return false;
        if (url.Length > 1 && (url[1] == '/' || url[1] == '\\')) return false;
        foreach (var ch in url)
            if (ch < 0x20 || ch == 0x7F) return false;
        return true;
    }

    /// <summary>True for a browser navigation that should be sent to a page instead of being answered with a status code.</summary>
    public static bool IsPageNavigation(HttpRequest request)
    {
        if (!HttpMethods.IsGet(request.Method)) return false;
        var path = request.Path;
        if (path.StartsWithSegments("/api") || path.StartsWithSegments("/hubs") || path.StartsWithSegments("/_blazor")
            || path.StartsWithSegments("/_framework") || path.StartsWithSegments("/_content"))
            return false;
        return request.Headers.Accept.ToString().Contains("text/html", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsInternal(HttpContext context) => context.Items.ContainsKey(InternalItem);

    private static string Normalize(PathString path)
    {
        var value = path.Value ?? "";
        return value.Length > 1 ? value.TrimEnd('/') : value;
    }

    private static bool Is(string normalizedPath, string expected) =>
        string.Equals(normalizedPath, expected, StringComparison.OrdinalIgnoreCase);

    // ── Services ─────────────────────────────────────────────────────────────

    /// <summary>Hosted mode's services: fallback policy, antiforgery cookie posture, sign-in rate limit.</summary>
    public static IServiceCollection AddHostedGate(this IServiceCollection services, HubHosting hosting)
    {
        // Everything without a policy of its own gets this one. Resource is the HttpContext under endpoint routing.
        services.AddAuthorizationBuilder().SetFallbackPolicy(new AuthorizationPolicyBuilder()
            .RequireAssertion(context => context.Resource is HttpContext http
                ? Decide(http.Request.Method, http.Request.Path, IsInternal(http), context.User, hosting) == GateDecision.Allow
                : context.User.Identity?.IsAuthenticated == true && hosting.IsUserAllowed(context.User.Identity.Name))
            .Build());

        services.AddAntiforgery(o =>
        {
            o.Cookie.HttpOnly = true;
            o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            o.Cookie.SameSite = SameSiteMode.Strict;
        });

        services.AddRateLimiter(o =>
        {
            o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            o.AddPolicy(LoginRateLimitPolicy, http => RateLimitPartition.GetFixedWindowLimiter(
                http.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = hosting.LoginPermitLimit,
                    Window = TimeSpan.FromSeconds(hosting.LoginWindowSeconds),
                    QueueLimit = 0,
                }));
        });
        return services;
    }

    // ── Pipeline ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Transport rules, first in the pipeline: recognise the Hub's own loopback calls (before any proxy
    /// header is applied), apply the platform's forwarded headers, then host allow-list, HTTPS and the
    /// security headers.
    /// </summary>
    public static WebApplication UseHostedTransport(this WebApplication app, HubHosting hosting)
    {
        var token = Encoding.ASCII.GetBytes(hosting.InternalToken);

        app.Use(async (context, next) =>
        {
            if (context.Request.Headers.TryGetValue(InternalHeader, out var presented))
            {
                // Judged on the raw connection, before UseForwardedHeaders can rewrite the remote address.
                var headers = context.Request.Headers;
                var direct = context.Connection.RemoteIpAddress is { } ip && IPAddress.IsLoopback(ip)
                    && !headers.ContainsKey("X-Forwarded-For") && !headers.ContainsKey("X-Forwarded-Proto")
                    && !headers.ContainsKey("X-Forwarded-Host") && !headers.ContainsKey("Forwarded");
                var match = CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(presented.ToString()), token);
                if (!direct || !match)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return;
                }
                context.Items[InternalItem] = true;
                context.User = InternalPrincipal;
            }
            await next();
        });

        if (hosting.TrustForwardedHeaders)
        {
            // The platform's front end is the only way in and appends the real client address and scheme.
            var forwarded = new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
                ForwardLimit = hosting.ForwardLimit,
            };
            forwarded.KnownIPNetworks.Clear();
            forwarded.KnownProxies.Clear();
            app.UseForwardedHeaders(forwarded);
        }

        app.Use(async (context, next) =>
        {
            var request = context.Request;
            // The liveness probe and the Hub's own loopback calls arrive by address, over plain HTTP.
            var exempt = IsInternal(context) || IsAnonymousHealth(request);
            if (!exempt)
            {
                if (!IsHostAllowed(request.Host.Host, hosting.AllowedHosts))
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    return;
                }
                if (hosting.RequireHttps && !request.IsHttps)
                {
                    if (HttpMethods.IsGet(request.Method) || HttpMethods.IsHead(request.Method))
                    {
                        var host = hosting.HttpsPort == 443 ? request.Host.Host : $"{request.Host.Host}:{hosting.HttpsPort}";
                        context.Response.Redirect($"https://{host}{request.PathBase}{request.Path}{request.QueryString}", permanent: true, preserveMethod: true);
                    }
                    else
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    return;
                }
            }

            context.Response.OnStarting(() =>
            {
                var headers = context.Response.Headers;
                if (request.IsHttps && hosting.HstsMaxAgeDays > 0)
                    headers.StrictTransportSecurity = $"max-age={hosting.HstsMaxAgeDays * 86_400L}";
                headers.XContentTypeOptions = "nosniff";
                headers["Referrer-Policy"] = "no-referrer";
                if (!headers.ContainsKey("X-Frame-Options")) headers.XFrameOptions = "SAMEORIGIN";
                return Task.CompletedTask;
            });
            await next();
        });
        return app;
    }

    private static bool IsAnonymousHealth(HttpRequest request) =>
        (HttpMethods.IsGet(request.Method) || HttpMethods.IsHead(request.Method)) && Is(Normalize(request.Path), HealthPath);

    /// <summary>
    /// Authentication, the gate, then MindAttic.Authentication's own middleware (authorization with the
    /// fallback policy, its forced password-change / MFA-enrolment step, and the CSP on the auth pages).
    /// Must sit before static files and every endpoint.
    /// </summary>
    public static WebApplication UseHostedLogin(this WebApplication app, HubHosting hosting)
    {
        app.UseRateLimiter();
        app.UseAuthentication();

        app.Use(async (context, next) =>
        {
            var request = context.Request;
            if (IsInternal(context)) context.User = InternalPrincipal;   // the cookie middleware has no say over these

            switch (Decide(request.Method, request.Path, IsInternal(context), context.User, hosting))
            {
                case GateDecision.Challenge:
                    if (IsPageNavigation(request))
                    {
                        var target = $"{request.Path}{request.QueryString}";
                        context.Response.Redirect(IsSafeReturnUrl(target) && target != "/"
                            ? $"{LoginPath}?returnUrl={Uri.EscapeDataString(target)}"
                            : LoginPath);
                    }
                    else
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return;

                case GateDecision.Forbidden:
                    // A real account that is not on the allow-list: end its session rather than leave it signed in to nothing.
                    await context.SignOutAsync(MaSchemes.Cookie);
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    context.Response.ContentType = "text/plain; charset=utf-8";
                    await context.Response.WriteAsync("This account is not permitted on this Hub.");
                    return;

                case GateDecision.EnrollMfa:
                    if (IsPageNavigation(request)) context.Response.Redirect(MfaSetupPath);
                    else context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return;
            }

            // The sign-in posts redirect to the form's returnUrl. Refuse anything that is not a plain
            // same-site path before the auth library sees it (its own check lets "~//host" through in 6.0.0).
            if (HttpMethods.IsPost(request.Method) && request.Path.StartsWithSegments("/_ma-auth") && request.HasFormContentType)
            {
                var form = await request.ReadFormAsync(context.RequestAborted);
                var returnUrl = form["returnUrl"].ToString();
                if (returnUrl.Length > 0 && !IsSafeReturnUrl(returnUrl))
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    return;
                }
            }

            await next();
        });

        app.UseMindAtticAuthentication();
        return app;
    }
}
