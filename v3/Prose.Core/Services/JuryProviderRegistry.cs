using System.Text.Json;
using Microsoft.Extensions.Logging;
using MindAttic.Legion;

namespace Prose.Core.Services;

/// <summary>
/// One extra jury provider — an OpenAI-compatible cloud LLM OUTSIDE the Legion
/// trusted-4 catalog (Kimi/Moonshot, Grok, Mistral, Qwen, …). Declared in settings
/// (<see cref="SettingsService.ExtraJuryProvidersJson"/>) so adding a new model
/// family is a settings edit, never a code change. The API key is resolved from
/// <c>MindAtticCredentialStore</c> by <see cref="Id"/> — the same store every other
/// provider key already lives in.
/// </summary>
public sealed record JuryProvider(
    string Id,
    string BaseUrl,
    string CheapModel,
    string? Model = null,
    string? Label = null,
    double InPerMtok = 0,
    double CacheWritePerMtok = 0,
    double CacheReadPerMtok = 0,
    double OutPerMtok = 0);

/// <summary>
/// Parses and caches the extra-jury-provider roster from settings. Reader-Proxy QA
/// juries draw from Legion's trusted providers PLUS these registry entries, so
/// verdicts can come from genuinely different model families (the only diversity
/// that survives correlated-error analysis — see docs/READER-QA.md).
/// Registry entries with no stored API key are silently excluded from
/// <see cref="WithKeys"/>; nothing ever fails because a provider is unfunded.
/// </summary>
public sealed class JuryProviderRegistry
{
    private readonly SettingsService settings;
    private readonly ILogger<JuryProviderRegistry> log;
    private IReadOnlyList<JuryProvider>? cached;
    private string? cachedJson;

    public JuryProviderRegistry(SettingsService settings, ILogger<JuryProviderRegistry> log)
    {
        this.settings = settings;
        this.log = log;
        // Make registry pricing visible to the cost estimator up front, so estimate
        // tables never silently price an unknown registry model at Haiku rates.
        foreach (var p in All)
            if (p.InPerMtok > 0 || p.OutPerMtok > 0)
                ReviewCostEstimator.RegisterPricing(
                    p.CheapModel, p.Label ?? p.Id,
                    p.InPerMtok, p.CacheWritePerMtok, p.CacheReadPerMtok, p.OutPerMtok);
    }

    /// <summary>All declared registry providers, keyed off the settings JSON
    /// (re-parsed only when the JSON changes).</summary>
    public IReadOnlyList<JuryProvider> All
    {
        get
        {
            var json = settings.ExtraJuryProvidersJson;
            if (cached != null && string.Equals(json, cachedJson, StringComparison.Ordinal)) return cached;
            cached = Parse(json);
            cachedJson = json;
            return cached;
        }
    }

    /// <summary>Registry providers that actually have an API key stored — the only
    /// ones a jury may draw. A keyless entry is a declaration, not a voter.</summary>
    public IReadOnlyList<JuryProvider> WithKeys =>
        All.Where(p => !string.IsNullOrWhiteSpace(MindAtticCredentialStore.GetKey(p.Id))).ToList();

    public JuryProvider? Get(string id) =>
        All.FirstOrDefault(p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase));

    public bool Contains(string id) => Get(id) != null;

    public string? ResolveKey(string id) => MindAtticCredentialStore.GetKey(id);

    private IReadOnlyList<JuryProvider> Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Array.Empty<JuryProvider>();
        try
        {
            using var doc = JsonDocument.Parse(json);
            var list = new List<JuryProvider>();
            foreach (var el in doc.RootElement.EnumerateArray())
            {
                var id = Str(el, "id");
                var baseUrl = Str(el, "baseUrl");
                var cheap = Str(el, "cheapModel");
                if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(cheap))
                {
                    log.LogWarning("ExtraJuryProvidersJson entry missing id/baseUrl/cheapModel — skipped: {Entry}", el.GetRawText());
                    continue;
                }
                list.Add(new JuryProvider(
                    id!, baseUrl!, cheap!,
                    Model: Str(el, "model"),
                    Label: Str(el, "label"),
                    InPerMtok: Num(el, "inPerMtok"),
                    CacheWritePerMtok: Num(el, "cacheWritePerMtok"),
                    CacheReadPerMtok: Num(el, "cacheReadPerMtok"),
                    OutPerMtok: Num(el, "outPerMtok")));
            }
            return list;
        }
        catch (JsonException ex)
        {
            log.LogWarning(ex, "ExtraJuryProvidersJson is not valid JSON — registry providers disabled until fixed.");
            return Array.Empty<JuryProvider>();
        }
    }

    private static string? Str(JsonElement el, string name) =>
        el.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static double Num(JsonElement el, string name) =>
        el.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDouble() : 0;
}
