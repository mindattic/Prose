using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// Ensures every firearm/ranged weapon has at least one ammunition edge in the Edges table.
/// Uses the local LLM to match each unlinked weapon to the correct ammo from the existing
/// 70 ammunition entities (caliber, power cell, slug, CO2, etc.).
///
///   prose --link-weapon-ammo [--local-url URL] [--local-key KEY] [--local-model TAG] [--dry-run]
///
/// Melee weapons return ammoName=null and are skipped (no edge needed).
/// </summary>
public static class LinkWeaponAmmoCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider sp)
    {
        var localUrl   = Flag(args, "--local-url");
        var localKey   = Flag(args, "--local-key");
        var localModel = Flag(args, "--local-model");
        var dryRun     = args.Contains("--dry-run");

        // Persist / resolve settings same as ReviewEntityCli.
        if (localUrl != null || localKey != null || localModel != null)
        {
            using var settingsScope = sp.CreateScope();
            var settings = settingsScope.ServiceProvider.GetRequiredService<SettingsService>();
            if (!string.IsNullOrWhiteSpace(localUrl))   settings.LocalReviewBaseUrl = NormalizeUrl(localUrl);
            if (localKey   != null) settings.LocalReviewApiKey = localKey;
            if (localModel != null) settings.LocalReviewModel  = localModel;
        }

        using var scope   = sp.CreateScope();
        var cfg           = scope.ServiceProvider.GetRequiredService<SettingsService>();
        var linker        = scope.ServiceProvider.GetRequiredService<WeaponAmmoLinkerService>();

        var resolvedUrl   = !string.IsNullOrWhiteSpace(cfg.LocalReviewBaseUrl)   ? cfg.LocalReviewBaseUrl   : null;
        var resolvedKey   = !string.IsNullOrWhiteSpace(cfg.LocalReviewApiKey)    ? cfg.LocalReviewApiKey    : null;
        var resolvedModel = !string.IsNullOrWhiteSpace(cfg.LocalReviewModel)     ? cfg.LocalReviewModel     : null;

        Console.WriteLine($"=== Link weapon → ammo ===");
        Console.WriteLine($"  endpoint : {resolvedUrl ?? "(cloud)"}");
        Console.WriteLine($"  dry-run  : {dryRun}");
        Console.WriteLine();

        await linker.LinkAllAsync(resolvedUrl, resolvedKey, resolvedModel, dryRun, CancellationToken.None);

        Console.WriteLine();
        Console.WriteLine("Done.");
        return 0;
    }

    private static string? Flag(string[] args, string name)
    {
        var idx = Array.IndexOf(args, name);
        return idx >= 0 && idx + 1 < args.Length ? args[idx + 1] : null;
    }

    private static string NormalizeUrl(string url)
    {
        url = url.TrimEnd('/');
        if (!url.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase))
        {
            if (!url.EndsWith("/v1", StringComparison.OrdinalIgnoreCase)) url += "/v1";
            url += "/chat/completions";
        }
        return url;
    }
}
