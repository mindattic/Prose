using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;

namespace StreetSamurai.Core.Services;

/// <summary>Lightweight, serialization-friendly view of a <see cref="Universe"/> row.</summary>
public sealed record UniverseInfo(
    Guid Id, string Slug, string Name, string? Theme, string? UniversePrimer, bool IsActive, double SortKey,
    string? WorldFacts = null);

/// <summary>
/// The currently-selected universe for this process / async flow, and the catalog of all
/// universes. Resolution precedence (highest wins):
///   1. flow override   — <see cref="SetFlowUniverse"/> (per async flow; e.g. a single UI render path)
///   2. process override — <see cref="UseUniverse"/> (per CLI/MCP process or the live UI selection)
///   3. global default   — the <c>current_universe</c> KV slug, else the lowest active SortKey
///   4. Guid.Empty       — no universes configured ⇒ scoping is a NO-OP (single-universe / tests)
///
/// Because the DbContext is a singleton factory and the repositories are singletons, the selection
/// can't ride a DI scope — it lives here as an ambient singleton, read by the DbContext query
/// filter via <see cref="UniverseScope"/>. Two CLIs are two OS processes, so their process
/// overrides are naturally isolated; the running web host is its own process whose UI selection is
/// the process override.
/// </summary>
public interface IUniverseContext
{
    Guid CurrentId { get; }
    string CurrentSlug { get; }
    UniverseInfo? CurrentUniverse { get; }
    IReadOnlyList<UniverseInfo> ListUniverses();

    /// <summary>True when the current universe is GLMZ (or no universe is scoped — GLMZ is the
    /// default). Lets a prompt site keep its exact GLMZ wording while branching elsewhere.</summary>
    bool IsGlmz { get; }

    /// <summary>
    /// The world-grounding text for a prompt: returns <paramref name="glmzFallback"/> verbatim when
    /// the current universe is GLMZ (zero drift), otherwise the current universe's UniversePrimer (or a
    /// neutral phrase if it has none). The single seam that segregates the prompt "cards" — a site
    /// keeps its GLMZ string as the fallback and can never feed it to another universe (RFC 0006).
    /// </summary>
    string UniverseGroundingOr(string glmzFallback);

    /// <summary>Set the process-scoped universe (CLI/MCP/UI active selection). Not persisted.</summary>
    void UseUniverse(Guid id);
    /// <summary>Set the process universe by slug. Returns false if the slug is unknown.</summary>
    bool UseUniverseBySlug(string slug);
    /// <summary>Highest-precedence per-async-flow override (null clears it).</summary>
    void SetFlowUniverse(Guid? id);
    /// <summary>Persist <paramref name="id"/>'s slug as the global default (<c>current_universe</c> KV).</summary>
    void PersistAsDefault(Guid id);
    /// <summary>Reload the universe catalog + default from the DB/KV.</summary>
    void Refresh();
}

/// <summary>
/// Process-entry hook for selecting the universe before any service resolves. The Blazor host
/// parses a <c>--universe &lt;slug&gt;</c> CLI flag into <see cref="RequestedSlug"/>; the MCP host
/// does the same. <see cref="UniverseContext"/> also reads the <c>SS_UNIVERSE</c> environment
/// variable (per-terminal) on first load. Either resolves to the process universe so two CLIs in
/// two terminals can target different universes simultaneously.
/// </summary>
public static class UniverseBootstrap
{
    /// <summary>Universe slug requested via the <c>--universe</c> flag (set at process entry).</summary>
    public static string? RequestedSlug { get; set; }

    /// <summary>Extract <c>--universe &lt;slug&gt;</c> (or <c>--universe=&lt;slug&gt;</c>) from argv.</summary>
    public static string? ParseSlug(string[] args)
    {
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == "--universe" && i + 1 < args.Length) return args[i + 1];
            if (args[i].StartsWith("--universe=", StringComparison.Ordinal)) return args[i]["--universe=".Length..];
        }
        return null;
    }

    /// <summary>
    /// The requested slug from the <c>--universe</c> flag or the <c>SS_UNIVERSE</c> env var, mapped
    /// to a well-known seeded-universe id WITHOUT a DB hit. This gives CLI processes correct
    /// scoping + insert-stamping even before anything resolves <see cref="UniverseContext"/> (the
    /// ~60 CLI dispatch blocks don't). Returns null for an unset/unknown slug, in which case scoping
    /// stays a no-op until the full context loads. Covers the two seeded universes; richer/arbitrary
    /// universes resolve once <see cref="UniverseContext"/> is constructed (UI/MCP paths do that).
    /// </summary>
    public static Guid? ResolveWellKnownId()
    {
        var slug = RequestedSlug ?? Environment.GetEnvironmentVariable("SS_UNIVERSE");
        return slug?.Trim().ToLowerInvariant() switch
        {
            "glmz" => StreetSamurai.Core.Data.Entities.Universe.GlmzId,
            "fantasy-steampunk" or "fantasy" or "steampunk" => StreetSamurai.Core.Data.Entities.Universe.FantasyId,
            _ => null,
        };
    }
}

/// <summary>
/// Ambient hook the <see cref="StreetSamuraiDbContext"/> reads in its global query filters and
/// insert-stamping. <see cref="EffectiveId"/> is <c>Guid.Empty</c> when no universe context has
/// been wired (tests / design-time / pre-migration), which makes universe scoping a no-op.
/// </summary>
public static class UniverseScope
{
    /// <summary>The process-wide universe context, set when <see cref="UniverseContext"/> is constructed.</summary>
    public static IUniverseContext? Current { get; set; }

    /// <summary>
    /// Current universe id, or <c>Guid.Empty</c> when scoping is inactive. Precedence: the live
    /// <see cref="IUniverseContext"/> (UI/MCP/any service that resolved it) wins; otherwise a
    /// CLI <c>--universe</c>/<c>SS_UNIVERSE</c> well-known fallback so CLI processes scope correctly
    /// before the full context loads.
    /// </summary>
    public static Guid EffectiveId => Current?.CurrentId ?? UniverseBootstrap.ResolveWellKnownId() ?? Guid.Empty;

    /// <summary>
    /// Monotonic counter bumped whenever the current universe changes. In-memory caches that aren't
    /// universe-keyed (repository GetAll caches, singleton config caches) record the epoch they were
    /// built at and rebuild when it differs — so a mid-process SwitchUniverse (the UI dropdown) never
    /// serves another universe's cached rows. A fresh process starts at 0.
    /// </summary>
    public static int Epoch => epoch;
    private static int epoch;
    public static void BumpEpoch() => System.Threading.Interlocked.Increment(ref epoch);

    /// <summary>
    /// Config keys shared across ALL universes (operational, not world content). Rows for these keys
    /// are stamped with <see cref="Data.Entities.Universe.SharedId"/> so every universe sees the one
    /// copy: LLM action routing, TTS pronunciation rules, and user accounts.
    /// </summary>
    public static readonly HashSet<string> SharedConfigKeys =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "action_configs", "tts.rules", "users.accounts",
            "current_universe", // the global default-universe selector — inherently cross-universe
        };
}

/// <inheritdoc cref="IUniverseContext"/>
public sealed class UniverseContext : IUniverseContext
{
    private const string DefaultKey = "current_universe";

    private readonly IDbContextFactory<StreetSamuraiDbContext> dbFactory;
    private readonly SettingsKvStore kv;
    private readonly ILogger<UniverseContext> log;

    private readonly AsyncLocal<Guid?> flowOverride = new();
    private Guid? processOverride;

    private readonly object gate = new();
    private List<UniverseInfo> catalog = new();
    private Guid defaultId = Guid.Empty;
    private bool loaded;

    public UniverseContext(
        IDbContextFactory<StreetSamuraiDbContext> dbFactory,
        SettingsKvStore kv,
        ILogger<UniverseContext> log)
    {
        this.dbFactory = dbFactory;
        this.kv = kv;
        this.log = log;
        // Expose ourselves to the DbContext query filter immediately. Catalog load is lazy so
        // construction never blocks on / fails against a not-yet-migrated database.
        UniverseScope.Current = this;
    }

    public Guid CurrentId
    {
        get
        {
            EnsureLoaded();
            return flowOverride.Value ?? processOverride ?? defaultId;
        }
    }

    public string CurrentSlug => CurrentUniverse?.Slug ?? "";

    public bool IsGlmz
    {
        get { var id = CurrentId; return id == Universe.GlmzId || id == Guid.Empty; }
    }

    public string UniverseGroundingOr(string glmzFallback)
    {
        if (IsGlmz) return glmzFallback;
        var primer = CurrentUniverse?.UniversePrimer;
        return string.IsNullOrWhiteSpace(primer) ? "a self-contained fictional world" : primer!;
    }

    public UniverseInfo? CurrentUniverse
    {
        get
        {
            var id = CurrentId;
            lock (gate) return catalog.FirstOrDefault(u => u.Id == id);
        }
    }

    public IReadOnlyList<UniverseInfo> ListUniverses()
    {
        EnsureLoaded();
        lock (gate) return catalog.ToList();
    }

    public void UseUniverse(Guid id)
    {
        processOverride = id == Guid.Empty ? null : id;
        UniverseScope.BumpEpoch();
    }

    public bool UseUniverseBySlug(string slug)
    {
        EnsureLoaded();
        UniverseInfo? match;
        lock (gate) match = catalog.FirstOrDefault(u => string.Equals(u.Slug, slug, StringComparison.OrdinalIgnoreCase));
        if (match == null) return false;
        UseUniverse(match.Id);
        return true;
    }

    public void SetFlowUniverse(Guid? id)
    {
        flowOverride.Value = id == Guid.Empty ? null : id;
        UniverseScope.BumpEpoch();
    }

    public void PersistAsDefault(Guid id)
    {
        EnsureLoaded();
        UniverseInfo? match;
        lock (gate) match = catalog.FirstOrDefault(u => u.Id == id);
        if (match == null) return;
        try { kv.Set(DefaultKey, match.Slug); }
        catch (Exception ex) { log.LogWarning(ex, "Failed to persist default universe '{Slug}'", match.Slug); }
        lock (gate) defaultId = id;
    }

    public void Refresh()
    {
        lock (gate) loaded = false;
        EnsureLoaded();
    }

    private void EnsureLoaded()
    {
        lock (gate)
        {
            if (loaded) return;
            loaded = true; // mark first so a failed load doesn't spin every call
            try
            {
                using var db = dbFactory.CreateDbContext();
                catalog = db.Set<Universe>().AsNoTracking()
                    .OrderBy(u => u.SortKey).ThenBy(u => u.Name)
                    .Select(u => new UniverseInfo(u.Id, u.Slug, u.Name, u.Theme, u.UniversePrimer, u.IsActive, u.SortKey, u.WorldFacts))
                    .ToList();
            }
            catch (Exception ex)
            {
                // Table missing (pre-migration / fresh test DB) ⇒ no universes ⇒ scoping no-op.
                log.LogDebug(ex, "Universe catalog unavailable; universe scoping disabled this session");
                catalog = new();
                defaultId = Guid.Empty;
                return;
            }

            if (catalog.Count == 0) { defaultId = Guid.Empty; return; }

            // Default = the persisted current_universe slug if valid, else lowest active SortKey.
            string? slug = null;
            try { slug = kv.Get<string>(DefaultKey); } catch { /* KV optional */ }
            var bySlug = slug == null ? null
                : catalog.FirstOrDefault(u => string.Equals(u.Slug, slug, StringComparison.OrdinalIgnoreCase));
            defaultId = bySlug?.Id
                ?? catalog.FirstOrDefault(u => u.IsActive)?.Id
                ?? catalog[0].Id;

            // Per-process selection: --universe flag (UniverseBootstrap) or SS_UNIVERSE env var
            // (per terminal). Applied once as the process override unless code already set one.
            if (processOverride == null)
            {
                var requested = UniverseBootstrap.RequestedSlug
                    ?? Environment.GetEnvironmentVariable("SS_UNIVERSE");
                if (!string.IsNullOrWhiteSpace(requested))
                {
                    var match = catalog.FirstOrDefault(u =>
                        string.Equals(u.Slug, requested, StringComparison.OrdinalIgnoreCase));
                    if (match != null) processOverride = match.Id;
                }
            }
        }
    }
}
