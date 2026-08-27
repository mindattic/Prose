using System.Security.Cryptography;
using System.Text;

namespace Prose.Core.Services;

/// <summary>
/// Portable-writing-service plan, Phase 1 — the pure comparison logic behind the Hub's shared-
/// secret gate (<c>Prose.Hub.HubApiKeyFilter</c>). Kept here, framework-independent, so it's
/// unit-testable without a Prose.Hub project reference or an ASP.NET Core test host: the filter
/// itself is a thin wrapper that reads <see cref="SettingsService.HubApiKey"/> and the
/// <c>X-Prose-Key</c> header, then defers the actual decision to <see cref="IsAuthorized"/>.
/// </summary>
public static class HubApiKeyChecker
{
    /// <summary>Fail-closed: an unset expected key (Hub misconfigured) or missing/wrong provided
    /// key both return false — never treat "nothing configured" as "anything goes."</summary>
    public static bool IsAuthorized(string? provided, string? expected)
    {
        if (string.IsNullOrEmpty(expected) || string.IsNullOrEmpty(provided)) return false;

        var a = Encoding.UTF8.GetBytes(provided);
        var b = Encoding.UTF8.GetBytes(expected);
        return a.Length == b.Length && CryptographicOperations.FixedTimeEquals(a, b);
    }
}
