using System.Text;
using Microsoft.EntityFrameworkCore;
using MindAttic.Authentication;
using MindAttic.Authentication.Entities;
using StreetSamurai.Core.Data;

namespace StreetSamurai.Core.Services;

/// <summary>
/// One-time, idempotent migration of legacy <see cref="Models.UserAccount"/> rows (stored as the
/// <c>users.accounts</c> JSON blob in the Settings table, via <see cref="UserRepository"/>) into the
/// MindAttic.Authentication <see cref="AuthUser"/> table. The bcrypt hash is carried verbatim with
/// <c>LegacyHashScheme="bcrypt"</c> so it transparently upgrades to Argon2id+pepper on next login.
/// StreetSamurai's <c>Administrator</c> role maps to the canonical <c>Admin</c>; the well-known
/// <c>admin@streetsamurai.local</c> is force-reset (MustChangePassword) rather than trusted.
/// Idempotency key = NormalizedUserName (re-running skips already-imported accounts).
/// </summary>
public sealed class AuthUserImportService(UserRepository legacyUsers, StreetSamuraiAuthDbContext authDb)
{
    private const string WellKnownAdminEmail = "admin@streetsamurai.local";

    public async Task<int> ImportAsync(CancellationToken ct = default)
    {
        var legacy = legacyUsers.GetAll();
        if (legacy.Count == 0) return 0;

        var imported = 0;
        foreach (var u in legacy)
        {
            if (string.IsNullOrWhiteSpace(u.Email)) continue;
            var normalized = Normalize(u.Email);
            if (await authDb.AuthUsers.AnyAsync(a => a.NormalizedUserName == normalized, ct)) continue;

            var isWellKnownAdmin = string.Equals(u.Email, WellKnownAdminEmail, StringComparison.OrdinalIgnoreCase);

            authDb.AuthUsers.Add(new AuthUser
            {
                Id = Guid.TryParse(u.Id, out var g) ? g : Guid.NewGuid(),
                UserName = u.Email,
                NormalizedUserName = normalized,
                Email = u.Email,
                NormalizedEmail = Normalize(u.Email),
                EmailVerified = true,
                PasswordHash = u.PasswordHash,
                LegacyHashScheme = "bcrypt",            // upgrade-on-login
                PasswordPepperKeyId = null,
                PasswordUpdatedUtc = u.CreatedUtc == default ? DateTime.UtcNow : u.CreatedUtc,
                SecurityStamp = string.IsNullOrWhiteSpace(u.SecurityStamp) ? Guid.NewGuid().ToString("N") : u.SecurityStamp,
                Role = string.Equals(u.Role, "Administrator", StringComparison.OrdinalIgnoreCase) ? MaRoles.Admin : u.Role,
                MfaEnabled = false,
                MustChangePassword = u.MustChangePassword || isWellKnownAdmin,
                MustEnrollMfa = false,                  // MFA off for now (owner directive)
                IsActive = true,
                LastLoginUtc = u.LastLoginUtc,
                CreatedUtc = u.CreatedUtc == default ? DateTime.UtcNow : u.CreatedUtc,
            });
            imported++;
        }

        if (imported > 0) await authDb.SaveChangesAsync(ct);
        return imported;
    }

    private static string Normalize(string s) => s.Normalize(NormalizationForm.FormKC).Trim().ToUpperInvariant();
}
