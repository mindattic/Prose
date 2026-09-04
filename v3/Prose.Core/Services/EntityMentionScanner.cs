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
    /// <summary>
    /// Entity types that must never be tagging anchors.
    ///
    /// <para><b><c>quote</c> added 2026-09-03.</b> A quote row is stored as an Entity whose
    /// <c>Name</c> is its SPEAKER's name — "Kressida Haun" has 46 of them
    /// (<c>kressida-haun-q002</c>, <c>q005</c>, …). Left in the candidate pool, every quoted
    /// character's own name is claimed by dozens of entities at once, so the ambiguity rule below
    /// drops the name entirely and the character can never be tagged — and an older scanner
    /// without that rule picked one arbitrarily, which is how TRUCE beat #16174 ended up tagging a
    /// QUOTE row as the character. It also made <c>--backfill-character-relationships</c> report
    /// "Kressida Haun matches 46 entities in the same universe", which reads like a duplicate-data
    /// disaster and is really just this. A quote is a record ABOUT a speaker, never a thing prose
    /// refers to by name.</para>
    /// </summary>
    private static readonly HashSet<string> ExcludedTypes =
        new(StringComparer.OrdinalIgnoreCase) { "chapter", "book", "node", "series", "beat", "quote" };

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
    //
    // "first" joined this set 2026-08-22 (VIGL logic sweep): the given-name/surname derivation
    // below takes tokens[0] of ANY multi-word character name as a candidate, with no way to tell
    // a personal given name from a leading rank/title word. "First Archivist Aurel Verlaine" derived
    // bare "First" as a standalone tag, so every ordinary occurrence of the word "First" anywhere
    // in the book's prose (e.g. "First light") got mistagged as that character. Same failure class
    // as the "The X"-alias bug above, just reached through name-derivation instead of a curated
    // alias row -- any future title-word/ordinal found doing this should be added here the same way.
    //
    // "sunday"/"unit"/"last"/"patient"/"can" joined 2026-08-22 (BCODA logic sweep), same exact
    // failure mode as "first": each is tokens[0] of a real multi-word character name -- "Sunday
    // Alarcon", "Unit 7-Gamma", "Last Word", "Patient Zero", "Can Zaragoza" -- so every ordinary-
    // English use of that word ("last week", "Can you pull...", "Unit 3", "Patient. Correct.")
    // mistagged as that character corpus-wide. NOT added here (different bug, needs a data fix
    // instead -- see plan): single-word full entity Names that are themselves ordinary words
    // ("Green", "Cut", "drifting" -- character/vocabulary entities whose canonical Name IS the
    // common word, not a derived token) and curated-alias mismatches ("the wall"/"the face"/
    // "the counter" registered as a character alias, "Eight" as an alias for "Sumi Okeke") --
    // those need per-entity alias/name cleanup, a stopword can't fix a full canonical Name match.
    //
    // "gate" joined 2026-08-24 (Kofi alias investigation), and is the first entry here derived from
    // the TRAILING token rather than tokens[0]: the character "Judas Gate" derives bare "Gate", so
    // every "Gate 3" in BCODA Ch24-25 -- a freight-yard reconciliation office, not a person -- tagged
    // as him. All 3 of that character's beat mentions corpus-wide were this false positive; he has no
    // real on-page appearance, so the derived token had nothing to lose and 5 spans to stop mistagging.
    // The full name "Judas Gate" still tags normally; only the bare derived token is suppressed.
    private static readonly HashSet<string> Stopwords =
        new(StringComparer.OrdinalIgnoreCase) { "the", "a", "an", "of", "von", "van", "de", "der", "la", "le", "el", "al", "first", "sunday", "unit", "last", "patient", "can", "gate" };

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
            .Select(e => new { e.Id, e.Name, e.EntityType, e.OriginNodeId })
            .ToListAsync(ct);
        var bookScopedIds = bookNodeId is Guid bnid
            ? entities.Where(e => e.OriginNodeId == bnid).Select(e => e.Id).ToHashSet()
            : [];

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
        var namesById = entities.Where(e => seenIds.Contains(e.Id)).ToDictionary(e => e.Id, e => e.Name);
        var aliases = await db.Set<CharacterAlias>().AsNoTracking()
            .Where(a => a.Value.Length >= 3)
            .Select(a => new { a.CharacterId, a.Value })
            .ToListAsync(ct);
        foreach (var a in aliases)
            if (namesById.TryGetValue(a.CharacterId, out var canonical))
                candidates.Add(new MentionCandidate(a.Value, a.CharacterId, canonical, "character", RequiresStrictCase(a.Value)));

        // 2026-08-19: place/faction/corponation aliases were NEVER read here — only CharacterAlias
        // was. Confirmed live: "ArcSec" was already registered as a Corporation CommonName on
        // "Arcturus Defense Solutions," yet 3 independent tagging passes across 3 different books
        // still found it unmatched, because this method had no path to ever see it. Every
        // non-character entity type has its own alias bridge table (PlaceAliases, FactionAliases,
        // CorponationCommonNames, WeaponAliases, ...) — this wires in the three this fix's driving
        // case actually needs; the rest are a known, separate follow-up (not yet audited for the
        // same gap).
        var placeAliases = await db.Set<PlaceAlias>().AsNoTracking()
            .Where(a => a.Value.Length >= 3)
            .Select(a => new { Id = a.PlaceId, a.Value })
            .ToListAsync(ct);
        foreach (var a in placeAliases)
            if (namesById.TryGetValue(a.Id, out var canonical))
                candidates.Add(new MentionCandidate(a.Value, a.Id, canonical, "place", RequiresStrictCase(a.Value)));

        var factionAliases = await db.Set<FactionAlias>().AsNoTracking()
            .Where(a => a.Value.Length >= 3)
            .Select(a => new { Id = a.FactionId, a.Value })
            .ToListAsync(ct);
        foreach (var a in factionAliases)
            if (namesById.TryGetValue(a.Id, out var canonical))
                candidates.Add(new MentionCandidate(a.Value, a.Id, canonical, "faction", RequiresStrictCase(a.Value)));

        var corponationCommonNames = await db.Set<CorponationCommonName>().AsNoTracking()
            .Where(a => a.Value.Length >= 3)
            .Select(a => new { Id = a.CorponationId, a.Value })
            .ToListAsync(ct);
        foreach (var a in corponationCommonNames)
            if (namesById.TryGetValue(a.Id, out var canonical))
                candidates.Add(new MentionCandidate(a.Value, a.Id, canonical, "corponation", RequiresStrictCase(a.Value)));

        // 2026-08-19, same day as the Place/Faction/Corponation fix above: a second sweep found
        // the identical gap on Weapon ("Wolfpack" never resolving to the seeded "Fenris Ballistics
        // Howl FB-7 'Wolfpack'") and Pharmaceutical ("Tears" never resolving to "Lethedol (street:
        // \"Tears\")") — confirming this was never a Place/Faction/Corponation-specific bug, it's
        // structural to every non-Character alias table. Wiring these two in now; the other ~15
        // remaining alias tables (technology, cyberware, apparel, ...) are still an open follow-up.
        var weaponAliases = await db.Set<WeaponAlias>().AsNoTracking()
            .Where(a => a.Value.Length >= 3)
            .Select(a => new { Id = a.WeaponId, a.Value })
            .ToListAsync(ct);
        foreach (var a in weaponAliases)
            if (namesById.TryGetValue(a.Id, out var canonical))
                candidates.Add(new MentionCandidate(a.Value, a.Id, canonical, "weapon", RequiresStrictCase(a.Value)));

        var pharmAliases = await db.Set<PharmAlias>().AsNoTracking()
            .Where(a => a.Value.Length >= 3)
            .Select(a => new { Id = a.PharmaceuticalId, a.Value })
            .ToListAsync(ct);
        foreach (var a in pharmAliases)
            if (namesById.TryGetValue(a.Id, out var canonical))
                candidates.Add(new MentionCandidate(a.Value, a.Id, canonical, "pharmaceutical", RequiresStrictCase(a.Value)));

        // Derived given-name/surname candidates for multi-word character names ("Declan Doyle" also
        // tags bare "Declan"/"Doyle"). Ambiguity arbitration used to happen right here (a token was
        // only kept when exactly one entity claimed it) — moved to the unified cross-source pass
        // below (2026-08-24, BTL logic sweep) because doing it here meant a token claimed by two
        // entities was dropped unconditionally, with no chance for the book-scope-preference rule
        // to save it: "Kovac" claimed by both a book-scoped "Idris Kovac" and a universe-wide "Ivet
        // Kovac" was discarded right here, before the later pass ever saw a "Kovac" candidate to
        // arbitrate. Every derived token is now added unconditionally (`ClaimToken` still records
        // ownership for the unified pass to read); a book's cast can legitimately collide on a
        // token with no book-scoped tiebreaker available (found live: "Aelwyn Croft"/"Aderyn Croft"
        // both derive "Croft") — the unified pass drops those exactly as before.
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
            if (!namesById.TryGetValue(id, out var canonical)) continue;
            foreach (var tok in tokens)
                candidates.Add(new MentionCandidate(tok, id, canonical, "character", RequiresStrictCase(tok)));
        }

        // Cross-source ambiguity guard (2026-08-24, BTL logic sweep): the derived given-name/
        // surname loop above only arbitrates collisions AMONG derived tokens. It never sees a
        // collision between a derived token and an alias-table row, or between two alias-table
        // rows for two different entities — those are added to `candidates` directly, with no
        // ClaimToken call at all. Confirmed live: "Farai" was a curated CharacterAlias on BOTH
        // "Farai Karimi" (universe-wide) and "Farai Kessler" (BTL-scoped, the character actually
        // introduced in that book) — since alias rows never call ClaimToken, the derived-token
        // ambiguity check never saw the collision, and every bare "Farai" mention in BTL silently
        // resolved to whichever of the two candidates happened to sort first, misattributing a
        // book's own protagonist's records-clerk contact to an unrelated universe-wide character.
        // Generalize the same "ambiguous token dropped, never guessed at" rule to the WHOLE
        // candidate pool regardless of source: any Text claimed by more than one distinct entity
        // is unsafe to anchor a tag to, full stop.
        // A collision isn't always an unresolvable guess: if exactly one contender for a shared
        // Text is scoped to THIS book (OriginNodeId == bookNodeId) and the rest are universe-wide
        // or scoped elsewhere, the book-scoped one wins — that's not guessing, it's the whole
        // point of book-scoping (Farai Kessler, introduced by name in BTL, correctly outranks the
        // unrelated universe-wide "Farai Karimi" for every bare "Farai" mention in BTL's own
        // beats). Only drop the text entirely when the collision can't be broken that way (two+
        // book-scoped contenders, or none).
        var textOwners = new Dictionary<string, HashSet<Guid>>(StringComparer.OrdinalIgnoreCase);
        foreach (var c in candidates)
        {
            if (!textOwners.TryGetValue(c.Text, out var owners))
                textOwners[c.Text] = owners = [];
            owners.Add(c.EntityId);
        }
        candidates.RemoveAll(c =>
        {
            if (!textOwners.TryGetValue(c.Text, out var owners) || owners.Count <= 1) return false;
            var bookScopedOwners = owners.Where(bookScopedIds.Contains).ToList();
            if (bookScopedOwners.Count == 1) return c.EntityId != bookScopedOwners[0];
            return true; // 0 or 2+ book-scoped contenders — genuinely ambiguous, drop all.
        });

        // Final guard, independent of source: a candidate whose entire text is nothing but a bare
        // article/connective can never safely anchor a tag. See the Stopwords doc comment for why
        // this is a real, not hypothetical, live data hazard.
        candidates.RemoveAll(c => Stopwords.Contains(c.Text.Trim()));

        return candidates;
    }

    /// <summary>
    /// Adds the caller's own already-tagged mentions to <paramref name="candidates"/> as pinned
    /// candidates, so a tag on an AMBIGUOUS name survives a save instead of being silently lost.
    ///
    /// <para>The ambiguity rule in <see cref="BuildCandidateIndexAsync"/> is not weakened by this
    /// and must not be: it refuses to GUESS which of five Marisols a bare "Marisol" means. A
    /// pinned mention is not a guess — it is the guid a human (or a prior tagging pass) already
    /// committed to in the markup, which is precisely the fact a name scan cannot recover. So the
    /// pinned entry is added AFTER the purge and replaces any purged entry for the same text.</para>
    ///
    /// <para>Two guards keep the staleness property that made re-derivation strip-first in the
    /// first place: a pinned guid is honoured only if it is <paramref name="liveEntities"/> (a
    /// live, non-archived entity in this book's universe), and the tag's canonical Name/EntityType
    /// come from that lookup rather than from the tag's own inner text, so a renamed entity still
    /// re-renders under its current identity.</para>
    /// </summary>
    public static List<MentionCandidate> WithPinnedMentions(
        List<MentionCandidate> candidates,
        IReadOnlyList<BeatMarkup.TaggedMention> pinned,
        IReadOnlyDictionary<Guid, (string Name, string EntityType)> liveEntities)
    {
        if (pinned.Count == 0) return candidates;

        foreach (var p in pinned)
        {
            if (!liveEntities.TryGetValue(p.EntityId, out var live)) continue;   // stale tag — still dropped
            var text = p.Text.Trim();
            if (text.Length == 0 || Stopwords.Contains(text)) continue;

            // Already resolvable to this same entity by name: nothing to pin.
            if (candidates.Any(c => string.Equals(c.Text, text, StringComparison.OrdinalIgnoreCase)
                                 && c.EntityId == p.EntityId))
                continue;

            // Any other claim on this surface text loses to the explicit one.
            candidates.RemoveAll(c => string.Equals(c.Text, text, StringComparison.OrdinalIgnoreCase));
            candidates.Add(new MentionCandidate(
                text, p.EntityId, live.Name, live.EntityType, RequiresStrictCase(text)));
        }
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

    // Outline entity-tag round-trip (Bible→Outline refactor Phase 4b): capitalized word-groups
    // that LOOK like proper nouns but a completed Scan() pass left untagged — the residue a
    // human should either seed as a real entity or fix as a typo. Deliberately the same coarse
    // heuristic as ProseWriterRouter.FindUnknownEntityNames (capitalized-phrase regex + a
    // common-word stoplist), independently scoped here to Scan()'s own matches rather than to
    // UniverseGraphService.AllNodes() — this method has no graph dependency, only text + matches.
    private static readonly Regex ProperNounResiduePattern =
        new(@"\b([A-Z][a-z]{2,}(?:\s+[A-Z][a-z]{2,})*)\b", RegexOptions.Compiled);

    private static readonly HashSet<string> ResidueCommonWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "The", "This", "That", "Here", "There", "When", "What", "Where", "Who", "Which",
        "And", "But", "Yet", "For", "Nor", "So", "Then", "Now", "Just", "Still",
        "He", "She", "They", "His", "Her", "Their", "Its", "Our", "Your", "My",
        "After", "Before", "During", "Inside", "Outside", "Through", "Against",
        // Outline-format labels, not names.
        "Chapter", "Beat", "Book", "Outline",
    };

    /// <summary>Every distinct capitalized phrase in <paramref name="text"/> whose span was NOT
    /// claimed by <paramref name="matches"/> (a completed <see cref="Scan"/> pass) — the "resolves
    /// to no entity record" residue filed as an <c>EntityDrift</c> finding by
    /// <c>NodeDocService.GenerateAsync</c> / <c>CanonDocumentService.SetNodeOutlineSectionAsync</c>.
    /// Not a grammar check: a name that legitimately isn't an entity (a one-off descriptive phrase)
    /// still surfaces here for a human to dismiss — the mechanism's job is recall, not precision.</summary>
    public static List<string> FindUnresolvedProperNouns(string text, IReadOnlyList<MentionMatch> matches)
    {
        if (string.IsNullOrWhiteSpace(text)) return [];
        var claimed = matches.Select(m => (m.Start, End: m.Start + m.Length)).ToList();
        var found = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match m in ProperNounResiduePattern.Matches(text))
        {
            var name = m.Value.Trim();
            if (name.Length <= 3 || ResidueCommonWords.Contains(name)) continue;
            if (claimed.Any(r => m.Index >= r.Start && m.Index < r.End)) continue;
            if (seen.Add(name)) found.Add(name);
        }
        return found;
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
