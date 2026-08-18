using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Core.Services;

/// <summary>
/// Deterministic, zero-LLM span-aware entity-mention detector — the core mechanism behind inline
/// <c>&lt;entity guid="..."&gt;word&lt;/entity&gt;</c> tagging (corpus-trust-recovery Phase 1a).
/// Extends the word-boundary + case-sensitivity heuristic already proven in
/// <see cref="SceneContextAssembler"/>'s <c>ScanNames</c>/<c>RequiresStrictCase</c> (which exists
/// specifically to stop bare common-noun-shaped names like "Echo" or "The Ledger" from firing on
/// ordinary lowercase prose — the "BLST contamination vector") to also capture match OFFSETS, not
/// just presence, and to pick a single non-overlapping tagging over the whole text.
///
/// Deliberately scoped to explicit textual naming only — no pronoun/coreference resolution ("she
/// stood by the door" with no literal name is never tagged). That is the correct, honest scope
/// boundary for a mechanism whose whole value proposition is being a FACT (an exact link to a
/// real Entities.Id row), not an inference.
/// </summary>
public static class EntityMentionScanner
{
    private static readonly HashSet<string> ExcludedTypes =
        new(StringComparer.OrdinalIgnoreCase) { "chapter", "book", "node", "series", "beat" };

    // A bare article/connective is never a valid standalone tagging anchor, no matter which source
    // offers it as a candidate: given-name/surname derivation skips these when splitting a full name
    // ("Aldric von Hess" derives "Aldric"/"Hess", never "von"), AND the final candidate-list filter
    // below drops any candidate whose entire Text is one of these outright. The latter exists
    // because live data proved this isn't hypothetical: a 2026-08-11 batch process left dozens of
    // "The X"-named entities (e.g. "The Petrified Colossus") with a literal `CharacterAliases.Value
    // = "The"` row. Without this filter that bad alias makes EVERY capitalized "The" in a book's
    // prose tag as that entity — the exact "Silence" false-positive class this whole scanner exists
    // to prevent, just reached through a curated-alias source instead of a common noun. The bad rows
    // themselves are a separate, tracked data cleanup (see plan's "Also track"); this filter is the
    // root-cause code fix that stays correct regardless of whether that cleanup ever runs.
    private static readonly HashSet<string> Stopwords =
        new(StringComparer.OrdinalIgnoreCase) { "the", "a", "an", "of", "von", "van", "de", "der", "la", "le", "el", "al" };

    public sealed record MentionCandidate(string Text, Guid EntityId, string Name, string EntityType, bool RequiresStrictCase);

    public sealed record MentionMatch(int Start, int Length, Guid EntityId, string Name, string EntityType);

    /// <summary>Every entity/alias name this book's beats may reference: universe-wide entities
    /// (<c>OriginNodeId == null</c>) plus entities scoped specifically to this book — mirrors
    /// <see cref="EntityDisambiguationService"/>'s resolution semantics rather than inventing a
    /// new one. Explicit <paramref name="universeId"/> filter (with <c>IgnoreQueryFilters</c>) so
    /// this doesn't silently depend on whatever universe happens to be ambient on the caller's
    /// <see cref="ProseDbContext"/>.</summary>
    public static async Task<List<MentionCandidate>> BuildCandidateIndexAsync(
        ProseDbContext db, Guid universeId, Guid? bookNodeId, CancellationToken ct = default)
    {
        var entities = await db.Set<Entity>().AsNoTracking().IgnoreQueryFilters()
            .Where(e => e.UniverseId == universeId
                && e.Status != "archived"
                && e.Name.Length >= 3
                && (e.OriginNodeId == null || e.OriginNodeId == bookNodeId))
            .Select(e => new { e.Id, e.Name, e.EntityType })
            .ToListAsync(ct);

        var candidates = new List<MentionCandidate>();
        var seenIds = new HashSet<Guid>();
        var entityTypes = new Dictionary<Guid, string>();
        foreach (var e in entities)
        {
            if (ExcludedTypes.Contains(e.EntityType) || e.Name.StartsWith('(')) continue;
            seenIds.Add(e.Id);
            entityTypes[e.Id] = e.EntityType;
            candidates.Add(new MentionCandidate(e.Name, e.Id, e.Name, e.EntityType, RequiresStrictCase(e.Name)));
        }

        // Character aliases ("Pixel" for a character whose canonical name differs, etc.) — same
        // alias table and same "scope to the already-universe-filtered entity id set" guard
        // SceneContextAssembler uses, since Character/CharacterAlias carry no UniverseId of their
        // own (Character.Id IS the parent Entity.Id).
        var characterNames = entities.Where(e => seenIds.Contains(e.Id)).ToDictionary(e => e.Id, e => e.Name);
        var aliases = await db.Set<CharacterAlias>().AsNoTracking()
            .Where(a => a.Value.Length >= 3)
            .Select(a => new { a.CharacterId, a.Value })
            .ToListAsync(ct);
        foreach (var a in aliases)
            if (characterNames.TryGetValue(a.CharacterId, out var canonical))
                candidates.Add(new MentionCandidate(a.Value, a.CharacterId, canonical, "character", RequiresStrictCase(a.Value)));

        // Derived given-name/surname candidates for multi-word character names ("Declan Doyle" also
        // tags bare "Declan"/"Doyle"). A token is only added when it is NOT shared with any other
        // entity's full name or derived token in this same candidate pool — a book's cast can
        // legitimately collide on a token (found live: "Aelwyn Croft"/"Aderyn Croft" both derive
        // "Croft"), and silently mistagging one character's name onto another is worse than leaving
        // the bare form untagged, so an ambiguous token is dropped rather than guessed at.
        var tokenOwners = new Dictionary<string, HashSet<Guid>>(StringComparer.OrdinalIgnoreCase);
        void ClaimToken(string token, Guid id)
        {
            if (!tokenOwners.TryGetValue(token, out var owners))
                tokenOwners[token] = owners = [];
            owners.Add(id);
        }
        foreach (var e in entities)
            if (seenIds.Contains(e.Id))
                ClaimToken(e.Name, e.Id);

        var derivedByEntity = new Dictionary<Guid, List<string>>();
        foreach (var e in entities)
        {
            if (!seenIds.Contains(e.Id) || !string.Equals(e.EntityType, "character", StringComparison.OrdinalIgnoreCase))
                continue;
            var tokens = e.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(t => t.Length >= 3 && !Stopwords.Contains(t))
                .ToList();
            if (tokens.Count < 2) continue;

            foreach (var tok in new[] { tokens[0], tokens[^1] }.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                ClaimToken(tok, e.Id);
                if (!derivedByEntity.TryGetValue(e.Id, out var list))
                    derivedByEntity[e.Id] = list = [];
                list.Add(tok);
            }
        }
        foreach (var (id, tokens) in derivedByEntity)
        {
            if (!characterNames.TryGetValue(id, out var canonical)) continue;
            foreach (var tok in tokens)
                if (tokenOwners.TryGetValue(tok, out var owners) && owners.Count == 1 && owners.Contains(id))
                    candidates.Add(new MentionCandidate(tok, id, canonical, "character", RequiresStrictCase(tok)));
        }

        // Final guard, independent of source: a candidate whose entire text is nothing but a bare
        // article/connective can never safely anchor a tag. See the Stopwords doc comment for why
        // this is a real, not hypothetical, live data hazard.
        candidates.RemoveAll(c => Stopwords.Contains(c.Text.Trim()));

        return candidates;
    }

    // Bare single tokens AND article+noun names ("The Ledger", "The Spine") hold to
    // case-sensitive matching: their nouns are ordinary prose words ("the ledger is open") and
    // ignore-case containment attaches the wrong entity to the scene. Multi-word proper names
    // keep ignore-case. Identical rule to SceneContextAssembler.RequiresStrictCase by design —
    // a future pass should collapse both onto one shared implementation rather than let them
    // drift the way SceneContextAssembler's and EntityRamificationService's name indexes already
    // have (found live 2026-08-17).
    private static bool RequiresStrictCase(string name)
    {
        var tokens = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return tokens.Length == 1
            || (tokens.Length == 2 && tokens[0].ToLowerInvariant() is "the" or "a" or "an");
    }

    /// <summary>Scans <paramref name="text"/> for every candidate's word-boundary-safe occurrence,
    /// then greedily selects the longest, leftmost, non-overlapping set — so "Declan Doyle" claims
    /// its span before a bare "Doyle" alias is even considered for the same characters, while a
    /// genuinely separate "Doyle" occurrence elsewhere in the text is still tagged on its own.</summary>
    public static List<MentionMatch> Scan(string text, IReadOnlyList<MentionCandidate> candidates)
    {
        if (string.IsNullOrEmpty(text) || candidates.Count == 0) return [];

        var raw = new List<MentionMatch>();
        foreach (var c in candidates)
        {
            var cmp = c.RequiresStrictCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            if (text.IndexOf(c.Text, cmp) < 0) continue; // cheap containment pre-filter

            var pattern = $@"\b{Regex.Escape(c.Text)}\b";
            var opts = c.RequiresStrictCase ? RegexOptions.None : RegexOptions.IgnoreCase;
            foreach (Match m in Regex.Matches(text, pattern, opts))
                raw.Add(new MentionMatch(m.Index, m.Length, c.EntityId, c.Name, c.EntityType));
        }

        if (raw.Count == 0) return [];

        var claimed = new bool[text.Length];
        var accepted = new List<MentionMatch>();
        foreach (var m in raw.OrderByDescending(x => x.Length).ThenBy(x => x.Start))
        {
            var overlaps = false;
            for (var i = m.Start; i < m.Start + m.Length; i++)
                if (claimed[i]) { overlaps = true; break; }
            if (overlaps) continue;

            for (var i = m.Start; i < m.Start + m.Length; i++) claimed[i] = true;
            accepted.Add(m);
        }

        return accepted.OrderBy(x => x.Start).ToList();
    }

    /// <summary>Wraps each accepted match in
    /// <c>&lt;entity repo="..." guid="..."&gt;matchedText&lt;/entity&gt;</c>, working right-to-left
    /// so earlier offsets stay valid as the string grows. <c>repo</c> carries the entity's
    /// <c>EntityType</c> (e.g. "character", "place") purely as a lookup-speed hint for future
    /// consumers — resolving a tag no longer requires a blind scan across every entity subtype
    /// table, just the one <c>repo</c> names.</summary>
    public static string ApplyTags(string text, IReadOnlyList<MentionMatch> matches)
    {
        if (matches.Count == 0) return text;

        var sb = new StringBuilder(text);
        foreach (var m in matches.OrderByDescending(x => x.Start))
        {
            var inner = sb.ToString(m.Start, m.Length);
            sb.Remove(m.Start, m.Length);
            sb.Insert(m.Start, $"""<entity repo="{m.EntityType}" guid="{m.EntityId}">{inner}</entity>""");
        }
        return sb.ToString();
    }

    /// <summary>Derives <c>BeatEntityMentions</c> for one beat directly from the tags already
    /// present in its (already-tagged) text — parsing, not re-scanning. Replaces the entity's
    /// canonical Name/EntityType from a fresh <c>Entities</c> lookup rather than trusting the
    /// tag's own inner text, since a renamed entity's tag can carry stale display text between
    /// edits (the tag's `guid` is the permanent fact; its inner text is not). Same
    /// replace-all-for-beat semantics as the name/alias-scan mechanism this supersedes
    /// (<see cref="EntityRamificationService.IndexBeatMentionsAsync"/>, left intact for
    /// not-yet-retagged beats via <c>--scan-entity-mentions</c>).</summary>
    public static async Task DeriveAndSaveMentionsAsync(
        IDbContextFactory<ProseDbContext> dbFactory, Guid beatId, string taggedText, CancellationToken ct = default)
    {
        var guids = BeatMarkup.ExtractEntityGuids(taggedText).ToList();

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var entities = guids.Count == 0
            ? []
            : await db.Set<Entity>().AsNoTracking().IgnoreQueryFilters()
                .Where(e => guids.Contains(e.Id))
                .Select(e => new { e.Id, e.Name, e.EntityType })
                .ToListAsync(ct);

        var mentions = entities.Select(e => new BeatEntityMention
        {
            BeatId     = beatId,
            EntityId   = e.Id,
            EntityName = e.Name,
            EntityType = e.EntityType,
            CreatedAt  = DateTime.UtcNow,
        }).ToList();

        await db.BeatEntityMentions.Where(m => m.BeatId == beatId).ExecuteDeleteAsync(ct);
        if (mentions.Count > 0)
        {
            db.BeatEntityMentions.AddRange(mentions);
            await db.SaveChangesAsync(ct);
        }
    }
}
