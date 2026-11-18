using System.Net;
using System.Text;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MindAttic.Authentication;
using MindAttic.Authentication.Crypto;
using MindAttic.Authentication.Secrets;
using MindAttic.Authentication.Services;
using MindAttic.Authentication.Web;
using Prose.Core.Data;

namespace Prose.Hub.Hosting;

/// <summary>
/// Hosted mode's sign-in pages (PRS-T1004). MindAttic.Authentication owns the login: its endpoints under
/// <c>/_ma-auth</c> verify the password and the TOTP code, apply its back-off, write its audit log and issue
/// the cookie. These pages are only the forms that post to them, plus the TOTP enrolment page.
///
/// <para>They are plain server-rendered HTML rather than Razor components on purpose. The Hub maps Blazor with
/// <c>MapRazorComponents&lt;App&gt;()</c> and no router, so a routable component would render the observability
/// page; and a page with no script and no stylesheet link needs nothing else to be served anonymously — the
/// anonymous surface stays these few routes and not one static asset.</para>
/// </summary>
public static class HostedAuthPages
{
    public static WebApplication MapHostedAuthPages(this WebApplication app, HubHosting hosting)
    {
        // Liveness only: no version, no pid, no database call (a probe must not keep a serverless database awake).
        app.MapGet(HostedGate.HealthPath, () => Results.Text("ok")).AllowAnonymous();

        // /_ma-auth/{login,mfa-challenge,logout,change-password,reset/*}. Who may reach which is the gate's
        // allow-list; the password-reset pair stays behind the login (nothing here serves a reset page).
        app.MapMindAtticAuthEndpoints(group => group.RequireRateLimiting(HostedGate.LoginRateLimitPolicy));

        app.MapGet(HostedGate.LoginPath, (HttpContext http, IAntiforgery antiforgery) =>
        {
            var returnUrl = SafeReturnUrl(http.Request.Query["returnUrl"].ToString());
            if (http.User.Identity?.IsAuthenticated == true) return Results.Redirect(returnUrl);

            var body = new StringBuilder();
            body.Append("<h1>Prose</h1>");
            if (http.Request.Query.ContainsKey("error"))
                body.Append("<p class=\"error\" role=\"alert\">Your sign-in attempt was unsuccessful. Please try again.</p>");
            body.Append("<form method=\"post\" action=\"/_ma-auth/login\">")
                .Append(TokenField(http, antiforgery))
                .Append(Hidden("returnUrl", returnUrl))
                .Append("<label>Username<input name=\"userName\" autocomplete=\"username\" autocapitalize=\"none\" spellcheck=\"false\" required autofocus></label>")
                .Append("<label>Password<input name=\"password\" type=\"password\" autocomplete=\"current-password\" required></label>")
                .Append("<button type=\"submit\">Sign in</button></form>");
            return Page(http, "Sign in", body.ToString());
        });

        app.MapGet(HostedGate.MfaPath, (HttpContext http, IAntiforgery antiforgery) =>
        {
            var returnUrl = SafeReturnUrl(http.Request.Query["returnUrl"].ToString());
            var body = new StringBuilder();
            body.Append("<h1>Two-factor code</h1>");
            if (http.Request.Query.ContainsKey("error"))
                body.Append("<p class=\"error\" role=\"alert\">That code was not accepted. Please try again.</p>");
            body.Append("<form method=\"post\" action=\"/_ma-auth/mfa-challenge\">")
                .Append(TokenField(http, antiforgery))
                .Append(Hidden("returnUrl", returnUrl))
                .Append("<label>Authentication code<input name=\"code\" inputmode=\"numeric\" autocomplete=\"one-time-code\" autocapitalize=\"none\" spellcheck=\"false\" required autofocus></label>")
                .Append("<label class=\"check\"><input type=\"checkbox\" name=\"recovery\" value=\"true\"> Use a recovery code instead</label>")
                .Append("<button type=\"submit\">Verify</button></form>")
                .Append("<p><a href=\"/login\">Start again</a></p>");
            return Page(http, "Two-factor code", body.ToString());
        });

        app.MapGet("/account", (HttpContext http, IAntiforgery antiforgery) =>
        {
            var body = new StringBuilder();
            body.Append("<h1>Account</h1>");
            if (http.Request.Query.ContainsKey("changed"))
                body.Append("<p role=\"status\">Your password was changed.</p>");
            body.Append("<p>Signed in as <strong>").Append(E(http.User.Identity?.Name)).Append("</strong>.</p>")
                .Append("<p><a href=\"/writer\">Open the Writer</a> · <a href=\"/account/change-password\">Change password</a></p>")
                .Append("<form method=\"post\" action=\"/_ma-auth/logout\">")
                .Append(TokenField(http, antiforgery))
                .Append("<button type=\"submit\">Sign out</button></form>");
            return Page(http, "Account", body.ToString());
        });

        app.MapGet("/account/change-password", (HttpContext http, IAntiforgery antiforgery) =>
        {
            var body = new StringBuilder();
            body.Append("<h1>Change password</h1>");
            if (http.User.HasClaim(MaClaims.MustChangePassword, "1"))
                body.Append("<p role=\"status\">You must choose a new password before continuing.</p>");
            if (http.Request.Query.ContainsKey("error"))
                body.Append("<p class=\"error\" role=\"alert\">Couldn't change your password. Check your current password and try again.</p>");
            body.Append("<form method=\"post\" action=\"/_ma-auth/change-password\">")
                .Append(TokenField(http, antiforgery))
                .Append("<label>Current password<input name=\"currentPassword\" type=\"password\" autocomplete=\"current-password\" required autofocus></label>")
                .Append("<label>New password<input name=\"newPassword\" type=\"password\" autocomplete=\"new-password\" minlength=\"12\" required></label>")
                .Append("<button type=\"submit\">Change password</button></form>");
            return Page(http, "Change password", body.ToString());
        });

        // TOTP enrolment. GET starts a pending secret (the library keeps it encrypted and short-lived) and shows
        // it; POST confirms it with a code and the current password, and shows the one-time recovery codes.
        app.MapGet(HostedGate.MfaSetupPath, async (HttpContext http, IAntiforgery antiforgery, IMfaEnrollmentService enrollment) =>
        {
            if (!Guid.TryParse(http.User.FindFirst(MaClaims.UserId)?.Value, out var userId)) return Results.Redirect(HostedGate.LoginPath);
            if (http.User.HasClaim(MaClaims.Amr, "mfa"))
                return Page(http, "Two-factor authentication",
                    "<h1>Two-factor authentication</h1><p>Two-factor authentication is already on for this account.</p><p><a href=\"/\">Continue</a></p>");

            var begin = await enrollment.BeginAsync(userId, http.RequestAborted);
            var body = new StringBuilder();
            body.Append("<h1>Set up two-factor authentication</h1>");
            if (http.Request.Query.ContainsKey("error"))
                body.Append("<p class=\"error\" role=\"alert\">That was not accepted. A new key was generated below: replace the entry in your authenticator app and try again.</p>");
            body.Append("<p>Add this key to your authenticator app:</p>")
                .Append("<p><code class=\"secret\">").Append(E(begin.SecretBase32)).Append("</code></p>")
                .Append("<p class=\"small\">Or use this address in an app that takes one:<br><code>").Append(E(begin.OtpAuthUri)).Append("</code></p>")
                .Append("<form method=\"post\" action=\"").Append(HostedGate.MfaSetupPath).Append("\">")
                .Append(TokenField(http, antiforgery))
                .Append("<label>Authenticator code<input name=\"code\" inputmode=\"numeric\" autocomplete=\"one-time-code\" required autofocus></label>")
                .Append("<label>Confirm your password<input name=\"password\" type=\"password\" autocomplete=\"current-password\" required></label>")
                .Append("<button type=\"submit\">Enable two-factor</button></form>");
            return Page(http, "Two-factor authentication", body.ToString());
        });

        app.MapPost(HostedGate.MfaSetupPath, async (HttpContext http, IAntiforgery antiforgery, IMfaEnrollmentService enrollment) =>
        {
            try { await antiforgery.ValidateRequestAsync(http); }
            catch (AntiforgeryValidationException) { return Results.BadRequest(); }
            if (!Guid.TryParse(http.User.FindFirst(MaClaims.UserId)?.Value, out var userId)) return Results.Redirect(HostedGate.LoginPath);

            var form = await http.Request.ReadFormAsync(http.RequestAborted);
            var result = await enrollment.ConfirmAsync(userId, form["code"].ToString().Trim(), form["password"].ToString(), http.RequestAborted);
            if (!result.Ok || result.RecoveryCodes is null) return Results.Redirect($"{HostedGate.MfaSetupPath}?error=1");

            var body = new StringBuilder();
            body.Append("<h1>Save your recovery codes</h1>")
                .Append("<p>Store these somewhere safe. Each works once if you lose your authenticator. They won't be shown again.</p><ul class=\"codes\">");
            foreach (var code in result.RecoveryCodes) body.Append("<li><code>").Append(E(code)).Append("</code></li>");
            body.Append("</ul><p>Two-factor authentication is on. You will be asked to sign in again, this time with a code.</p>")
                .Append("<p><a href=\"/login\">Sign in</a></p>");
            return Page(http, "Recovery codes", body.ToString());
        }).RequireRateLimiting(HostedGate.LoginRateLimitPolicy);

        return app;
    }

    private static string SafeReturnUrl(string? candidate) => HostedGate.IsSafeReturnUrl(candidate) ? candidate! : "/";

    private static string E(string? value) => WebUtility.HtmlEncode(value ?? "");

    private static string Hidden(string name, string value) =>
        $"<input type=\"hidden\" name=\"{E(name)}\" value=\"{E(value)}\">";

    private static string TokenField(HttpContext http, IAntiforgery antiforgery)
    {
        var tokens = antiforgery.GetAndStoreTokens(http);
        return Hidden(tokens.FormFieldName, tokens.RequestToken ?? "");
    }

    /// <summary>One self-contained document: no script, no external asset, styles under the auth pages' CSP nonce.</summary>
    private static IResult Page(HttpContext http, string title, string bodyHtml)
    {
        var nonce = http.Items[MindAtticAuthAppExtensions.CspNonceItem] as string;
        var nonceAttribute = string.IsNullOrEmpty(nonce) ? "" : $" nonce=\"{E(nonce)}\"";
        http.Response.Headers.CacheControl = "no-store";
        var html = $$"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1">
            <meta name="robots" content="noindex, nofollow">
            <title>{{E(title)}} - Prose</title>
            <style{{nonceAttribute}}>
            body { margin: 0; min-height: 100vh; display: grid; place-items: center; background: #14161a; color: #e6e6e6; font: 16px/1.5 system-ui, sans-serif; }
            main { width: min(26rem, calc(100vw - 2rem)); padding: 2rem; background: #1d2026; border: 1px solid #2c313a; border-radius: 8px; }
            h1 { margin: 0 0 1rem; font-size: 1.4rem; }
            label { display: block; margin: 0 0 1rem; }
            label.check { display: flex; gap: .5rem; align-items: center; }
            input:not([type=checkbox]) { display: block; width: 100%; box-sizing: border-box; margin-top: .25rem; padding: .55rem .6rem; background: #14161a; color: inherit; border: 1px solid #3a404b; border-radius: 4px; font: inherit; }
            button { padding: .55rem 1rem; background: #3b6fd4; color: #fff; border: 0; border-radius: 4px; font: inherit; cursor: pointer; }
            a { color: #8ab0f5; }
            code { word-break: break-all; }
            .secret { font-size: 1.15rem; letter-spacing: .08em; }
            .small { font-size: .85rem; color: #a9afba; }
            .codes { columns: 2; padding-left: 1.2rem; }
            .error { color: #ff9a8f; }
            </style>
            </head>
            <body><main>{{bodyHtml}}</main></body>
            </html>
            """;
        return Results.Content(html, "text/html; charset=utf-8");
    }
}

/// <summary>
/// Hosted mode's start-up proof that a login is actually possible (PRS-T1004). Run after the database is
/// awake and before the Hub listens; any failure stops the Hub rather than leaving it serving a login
/// nobody can pass or, worse, nothing guarding it.
/// </summary>
public static class HostedLogin
{
    public static async Task EnsureReadyAsync(IServiceProvider services, HubHosting hosting, Action<string> log, CancellationToken ct = default)
    {
        await using var scope = services.CreateAsyncScope();
        var sp = scope.ServiceProvider;

        // The pepper: the hasher reads it when constructed and throws without it.
        try { _ = sp.GetRequiredService<IPasswordHasher>(); }
        catch (Exception ex)
        {
            throw new HubHostingException(
                "[hub] Hosted mode: the password hasher could not start, so nobody could sign in. Set MindAttic:Vault:Security:pepper.v1 " +
                $"(env MindAttic__Vault__Security__pepper.v1) to the value the accounts were hashed with. Not starting. ({ex.GetBaseException().Message})");
        }

        var db = sp.GetRequiredService<ProseAuthDbContext>();
        List<string> activeUsers;
        bool anyUsers;
        try
        {
            var pending = (await db.Database.GetPendingMigrationsAsync(ct)).ToList();
            if (pending.Count > 0)
            {
                log($"[hub] Applying {pending.Count} pending auth migration(s): {string.Join(", ", pending)}");
                await db.Database.MigrateAsync(ct);
            }
            anyUsers = await db.AuthUsers.AsNoTracking().AnyAsync(ct);
            activeUsers = await db.AuthUsers.AsNoTracking().Where(u => u.IsActive).Select(u => u.UserName).ToListAsync(ct);
        }
        catch (Exception ex)
        {
            throw new HubHostingException(
                $"[hub] Hosted mode: the auth tables (schema 'auth') could not be read or migrated, so nobody could sign in. Not starting. ({ex.GetBaseException().Message})");
        }

        if (!anyUsers)
        {
            // First run on an empty auth schema: the library seeds one admin whose password is the operator's
            // bootstrap token, flagged to change it and to enrol TOTP at first sign-in.
            if (string.IsNullOrWhiteSpace(sp.GetRequiredService<IAuthSecrets>().GetOptional("bootstrap-token")))
                throw new HubHostingException(
                    "[hub] Hosted mode: the auth tables hold no user and MindAttic:Vault:Security:bootstrap-token is not set, so there is " +
                    "nobody to sign in as. Set it (it becomes the first password, changed at first sign-in) or create the account first. Not starting.");
            var owner = hosting.AllowedUsers[0];
            await sp.GetRequiredService<AuthBootstrapper>().SeedAdminAsync(owner, ct);
            log($"[hub] Hosted mode: seeded the first account '{owner}' from the bootstrap token. Sign in, change the password, enrol two-factor, then remove the token.");
            return;
        }

        if (!activeUsers.Any(hosting.IsUserAllowed))
            throw new HubHostingException(
                $"[hub] Hosted mode: none of {HubHosting.AllowedUsersKey} ({string.Join(", ", hosting.AllowedUsers)}) is an active account in the auth " +
                "tables, so nobody could sign in. Fix the setting or the account (prose --reset-password reactivates one). Not starting.");
    }
}
