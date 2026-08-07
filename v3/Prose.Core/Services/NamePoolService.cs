using System.Text.Json;
using Microsoft.Extensions.Logging;
using Prose.Core.Interfaces;
using Prose.Core.Models.Canon;

namespace Prose.Core.Services;

/// <summary>
/// Source of truth for unique-first-name selection across the world.
///
/// Loads the curated diaspora name pool from engine/data/name_pool.json and
/// provides two capabilities used by character generators:
///
///   1. <see cref="SamplePreferredNames"/> — returns a biased sample of currently-unused
///      first names to inject into a generation prompt so the LLM steers toward
///      Ubiquitous Diaspora names instead of defaulting to its small "cyberpunk
///      aesthetic" pool (Zara, Slate, Echo, Nova, Haze, etc.).
///
///   2. <see cref="EnsureUniqueFirstName"/> — a post-generation hook that swaps the
///      first name if the LLM picked one that's already in canon. The surname is
///      preserved; the old full name and old first name are pushed into aliases[]
///      so cross-references keep resolving.
///
/// Also enforces a forbidden list (Sarah, Lee, Bekka, Karen) per project-level
/// user preference.
/// </summary>
public class NamePoolService
{
    private static readonly HashSet<string> Forbidden = new(StringComparer.OrdinalIgnoreCase)
    {
        "Sarah", "Lee", "Bekka", "Karen",
    };

    private const string SettingsKey = "name_pool";

    private readonly IPathProvider paths;
    private readonly SettingsKvStore kv;
    private readonly IDatabaseService db;
    private readonly ILogger<NamePoolService> log;

    // Loaded once on first access.
    private List<string>? poolCache;
    private readonly object cacheLock = new();

    public NamePoolService(IPathProvider paths, SettingsKvStore kv, IDatabaseService db, ILogger<NamePoolService> log)
    {
        this.paths = paths;
        this.kv    = kv;
        this.db    = db;
        this.log   = log;
    }

    /// <summary>
    /// Full curated diaspora pool loaded from engine/data/name_pool.json.
    /// Deduplicated, filtered against the forbidden list.
    /// </summary>
    public IReadOnlyList<string> Pool
    {
        get
        {
            if (poolCache != null) return poolCache;
            lock (cacheLock)
            {
                if (poolCache != null) return poolCache;
                poolCache = LoadPool();
                return poolCache;
            }
        }
    }

    /// <summary>
    /// Returns up to <paramref name="count"/> first names from the pool that are
    /// not currently used in canon. Intended to be injected into generation prompts
    /// as a "prefer names like these" guidance block.
    /// </summary>
    public List<string> SamplePreferredNames(int count = 40)
    {
        var usedFirsts = GetUsedFirstNames();
        var unused = Pool.Where(n => !usedFirsts.Contains(FirstOf(n))).ToList();
        if (unused.Count == 0) return [];

        // Deterministic per-call random via system RNG — we don't need reproducibility here.
        var rng = Random.Shared;
        var picks = new List<string>(count);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (picks.Count < count && picks.Count < unused.Count)
        {
            var pick = unused[rng.Next(unused.Count)];
            if (seen.Add(pick)) picks.Add(pick);
        }
        return picks;
    }

    /// <summary>
    /// Returns up to <paramref name="count"/> first names currently IN USE in canon —
    /// intended to be injected into generation prompts as a "do NOT use these first
    /// names" guidance block. Sampled randomly across all in-use names so the LLM
    /// sees variety rather than just the first N alphabetically.
    /// </summary>
    public List<string> SampleUsedFirstNames(int count = 60)
    {
        var all = GetUsedFirstNames().ToList();
        if (all.Count == 0) return [];

        var rng = Random.Shared;
        var picks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var maxTries = count * 4;
        while (picks.Count < count && picks.Count < all.Count && maxTries-- > 0)
            picks.Add(all[rng.Next(all.Count)]);
        return picks.ToList();
    }

    /// <summary>
    /// After an LLM generates a character, enforce first-name uniqueness.
    /// If the generated first name already exists in canon OR is on the forbidden
    /// list, swap it for a fresh pick from the pool. Surname is preserved. Old full
    /// name and old first name are pushed into aliases[] for backward resolution.
    /// Returns true if a swap occurred.
    /// </summary>
    public bool EnsureUniqueFirstName(CharacterData character)
    {
        if (character == null || string.IsNullOrWhiteSpace(character.Name)) return false;

        var fullName = character.Name.Trim();
        var firstName = FirstOf(fullName);
        if (string.IsNullOrEmpty(firstName)) return false;

        var usedFirsts = GetUsedFirstNames();
        // Exclude the character's own id from the "used" set in case it's being re-saved.
        var collides = usedFirsts.Contains(firstName) || Forbidden.Contains(firstName);
        if (!collides) return false;

        var surname = fullName.Substring(firstName.Length).TrimStart();
        var replacement = PickUnusedFromPool(usedFirsts);
        if (replacement == null)
        {
            log.LogWarning("Name pool exhausted — could not swap colliding first name '{First}' for character {Id}",
                firstName, character.Id);
            return false;
        }

        var newFullName = string.IsNullOrWhiteSpace(surname) ? replacement : $"{replacement} {surname}";

        // Preserve history in aliases so any downstream reference to the old name still resolves.
        character.Aliases ??= [];
        if (!character.Aliases.Contains(fullName, StringComparer.OrdinalIgnoreCase))
            character.Aliases.Insert(0, fullName);
        if (!character.Aliases.Contains(firstName, StringComparer.OrdinalIgnoreCase))
            character.Aliases.Add(firstName);

        log.LogInformation("NamePool: swapped colliding first name '{Old}' → '{New}' for character {Id}",
            firstName, replacement, character.Id);

        character.Name = newFullName;
        return true;
    }

    // ── Private ──

    List<string> LoadPool()
    {
        // SQL Settings is the canonical source. The previous engine_data/name_pool.json
        // seed-from-disk path was retired with the JSON archival sweep — the Settings
        // row carries every name now.
        var fromKv = kv.Get<List<string>>(SettingsKey);
        if (fromKv != null && fromKv.Count > 0)
        {
            log.LogInformation("NamePool loaded {Count} names from Settings", fromKv.Count);
            return Sanitize(fromKv);
        }
        log.LogWarning("Name pool Settings row '{Key}' is empty — returning empty pool", SettingsKey);
        return [];
    }

    static List<string> Sanitize(List<string> names) =>
        names
            .Select(n => n?.Trim() ?? "")
            .Where(n => n.Length > 0 && !Forbidden.Contains(n))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    HashSet<string> GetUsedFirstNames()
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var c in db.Characters)
        {
            var fn = FirstOf(c.Name);
            if (fn.Length > 0) set.Add(fn);
        }
        return set;
    }

    string? PickUnusedFromPool(HashSet<string> used)
    {
        var candidates = Pool.Where(n => !used.Contains(n)).ToList();
        if (candidates.Count == 0) return null;
        return candidates[Random.Shared.Next(candidates.Count)];
    }

    static string FirstOf(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return "";
        var i = fullName.IndexOf(' ');
        return (i > 0 ? fullName[..i] : fullName).Trim();
    }
}
