using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MindAttic.Authentication.Crypto;
using StreetSamurai.Core.Data;

namespace StreetSamurai.Cli;

/// <summary>
/// Operator password reset for a MindAttic.Authentication account, run without
/// the web server. Hashes the supplied password with the LIVE Argon2id+pepper
/// hasher (so the login flow verifies it), clears any legacy bcrypt scheme, and
/// rotates the SecurityStamp to revoke existing sessions.
///
/// This deliberately BYPASSES the set-time password policy (min-length / HIBP).
/// It is an admin/dev reset tool: the operator is choosing the exact string,
/// often a short dev password the policy would reject. Login itself only
/// verifies the hash, so the chosen password works regardless. Pass
/// <c>--require-change</c> to force a change on next login instead.
///
/// Usage:
///   ss --reset-password --email user@example.com --password "NewPass" [--require-change]
/// </summary>
public static class ResetPasswordCli
{
    public static async Task<int> RunAsync(IReadOnlyList<string> args, IServiceProvider sp)
    {
        var email = GetArg(args, "--email");
        var password = GetArg(args, "--password");
        var requireChange = args.Contains("--require-change");

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            Console.Error.WriteLine("Usage: ss --reset-password --email <email> --password <newPassword> [--require-change]");
            return 2;
        }

        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<StreetSamuraiAuthDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var normalized = Normalize(email);
        var user = await db.AuthUsers.FirstOrDefaultAsync(u =>
            u.NormalizedUserName == normalized || u.NormalizedEmail == normalized);
        if (user is null)
        {
            Console.Error.WriteLine($"No account found for '{email}'.");
            return 1;
        }

        var hash = hasher.Hash(password);
        user.PasswordHash = hash.Phc;
        user.PasswordPepperKeyId = hash.PepperKeyId;
        user.LegacyHashScheme = null;                       // now native Argon2id
        user.PasswordUpdatedUtc = DateTime.UtcNow;
        user.MustChangePassword = requireChange;
        user.SecurityStamp = Guid.NewGuid().ToString("N");  // revoke existing sessions
        user.IsActive = true;
        await db.SaveChangesAsync();

        Console.WriteLine(
            $"Password reset for {user.UserName} (role {user.Role}). " +
            $"MustChangePassword={requireChange}. Existing sessions revoked.");
        return 0;
    }

    private static string? GetArg(IReadOnlyList<string> args, string flag)
    {
        var i = args.ToList().FindIndex(a => string.Equals(a, flag, StringComparison.OrdinalIgnoreCase));
        return i >= 0 && i + 1 < args.Count ? args[i + 1] : null;
    }

    // Matches AuthUserImportService.Normalize so lookups line up with imported rows.
    private static string Normalize(string s) => s.Normalize(NormalizationForm.FormKC).Trim().ToUpperInvariant();
}
