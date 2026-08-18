using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Interfaces;

namespace Prose.Core.Services;

/// <summary>
/// Phase 0 (repair) of the corpus-trust-recovery plan: the book is canon right now. For ONE
/// book, this service directly syncs each already-linked live Entity's facts against what the
/// book's CURRENT bible actually says about that character, and flags drift — the "Declan
/// Doyle's own entity still says 'disconnected some months later' when the bible has an
/// explicit, dated correction to 'three days'" class of bug. Report-only; never writes.
///
/// Earlier version of this service tried to catch full renames (Soren Rowe -> Declan Doyle) by
/// keyword-matching a bible character against ORPHANED entities (zero live mentions). Retired
/// that approach after live validation against the real Rowe/Doyle case failed twice, even after
/// real bug fixes -- an LLM's "distinctive keywords for this character now" and "what words
/// happened to survive from a totally rewritten old draft" are not reliably the same set, and
/// tuning the prompt to force a match on one known example is overfitting, not a general fix.
/// Per direction: don't guess which orphan used to be whom -- an orphan with zero live
/// connection to any current book simply isn't represented in canon right now, full stop; that's
/// a flat fact worth reporting, not a puzzle worth solving per-book. Fixing entities that ARE
/// already correctly linked to reflect what the book currently says is the reliable, general
/// mechanism, and is what this service now does.
/// </summary>
public class BookEntityReconciliationService(
    IDbContextFactory<ProseDbContext> dbFactory, ILlmService llm, FindingsService findings)
{
    // Bounds the bible excerpt sent to the extraction call. Large enough to cover M-101 (16,560
    // chars) in full; for much larger bibles (VIGL, 117K) this is a head-only excerpt -- same
    // structural spoiler guard as BookDescriptionService, and the same known limitation (may
    // miss a character list buried deep in a huge bible). Good enough for a first validated
    // pass; revisit if a later book's character list lives past this cut.
    private const int MaxBibleExcerptChars = 30000;

    public sealed record BibleCharacter(string Name, string Summary);

    public sealed record DriftFinding(
        string CharacterName, Guid EntityId, bool Drifted, string Explanation, string? SuggestedDescription);

    /// <summary>A character-type entity in this book's universe with zero live BeatEntityMentions
    /// anywhere — not represented in any current book's prose right now. Reported as a flat fact
    /// (see class doc comment on why this service does NOT try to guess which current character
    /// this used to be) with enough detail (name, description snippet) for a human to act on it —
    /// e.g. as a MergeAsync loser once the human has confirmed the winner from real book knowledge.</summary>
    public sealed record OrphanCandidate(Guid Id, string Name, string? DescriptionSnippet);

    public sealed record ReconciliationReport(
        bool BibleTruncated,
        IReadOnlyList<BibleCharacter> BibleCharacters,
        IReadOnlyList<string> LiveRosterNames,
        IReadOnlyList<DriftFinding> DriftFindings,
        IReadOnlyList<string> UnmatchedBibleCharacters,
        IReadOnlyList<OrphanCandidate> OrphansInUniverse);

    public async Task<ReconciliationReport> ReconcileAsync(Guid bookNodeId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        // IgnoreQueryFilters(): explicit bookNodeId, not an ambient scope (same bug class found
        // and fixed in BookArchiveService.ArchiveAsync, 2026-08-17).
        var node = await db.Nodes.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(n => n.Id == bookNodeId, ct)
            ?? throw new InvalidOperationException($"Node {bookNodeId} not found.");

        var bible = node.NodeBible ?? "";
        if (bible.Trim().Length < 1000)
            throw new InvalidOperationException(
                $"Node '{node.Slug}' has no substantive NodeBible content to reconcile against.");

        var truncated = bible.Length > MaxBibleExcerptChars;
        var excerpt = truncated ? bible[..MaxBibleExcerptChars] : bible;

        var characters = await ExtractBibleCharactersAsync(node.Title, excerpt, ct);

        // Live roster: entities with at least one BeatNodes-attached (live) mention under this
        // book's full leaf-descendant tree -- the recursive walk, per this project's hard rule
        // (a flat direct-children query silently misses anything nested deeper).
        var leafIds = await NodeWorkbenchService.GetLeafDescendantIdsAsync(db, bookNodeId, ct);
        var liveEntities = await (
            from bem in db.BeatEntityMentions.AsNoTracking()
            join bn in db.BeatNodes.AsNoTracking() on bem.BeatId equals bn.BeatId
            where leafIds.Contains(bn.NodeId)
            join e in db.Entities.AsNoTracking() on bem.EntityId equals e.Id
            select new { e.Id, e.Name, e.Description }
        ).Distinct().ToListAsync(ct);

        var driftFindings = new List<DriftFinding>();
        var unmatched = new List<string>();

        foreach (var ch in characters)
        {
            var live = liveEntities.FirstOrDefault(e => NamesRoughlyMatch(e.Name, ch.Name));
            if (live == null)
            {
                unmatched.Add(ch.Name);
                continue;
            }

            var drift = await CheckDriftAsync(node.Title, ch, live.Description ?? "", ct);
            if (drift != null)
            {
                var final = drift with { EntityId = live.Id };
                driftFindings.Add(final);

                findings.Upsert(
                    filePath: $"node:{node.Slug}",
                    chapterId: null,
                    category: FindingCategory.EntityDrift,
                    severity: FindingSeverity.Medium,
                    summary: $"Entity \"{final.CharacterName}\" ({final.EntityId}) disagrees with current bible: {final.Explanation}",
                    snippet: live.Description,
                    suggestedFix: final.SuggestedDescription);
            }
        }

        foreach (var name in unmatched)
        {
            findings.Upsert(
                filePath: $"node:{node.Slug}",
                chapterId: null,
                category: FindingCategory.EntityDrift,
                severity: FindingSeverity.Low,
                summary: $"Bible names character \"{name}\" but no live entity (with a live beat mention under this book) matches that name.",
                snippet: null,
                suggestedFix: null);
        }

        var orphans = await db.Entities.AsNoTracking()
            .Where(e => e.UniverseId == node.UniverseId && e.EntityType == "character" && e.OriginNodeId == null)
            .Where(e => !db.BeatEntityMentions.Any(bem =>
                bem.EntityId == e.Id && db.BeatNodes.Any(bn => bn.BeatId == bem.BeatId)))
            .Select(e => new { e.Id, e.Name, e.Description })
            .ToListAsync(ct);

        var orphanCandidates = orphans
            .Select(o => new OrphanCandidate(o.Id, o.Name, o.Description == null ? null : Snippet(o.Description)))
            .ToList();

        // Orphan-ness is a universe-wide fact, not a per-book one -- tag with the universe, not
        // this book's slug, so reconciling two books in the same universe dedupes onto the same
        // finding row instead of creating one duplicate per book that happens to run this check.
        foreach (var o in orphanCandidates)
        {
            findings.Upsert(
                filePath: $"universe:{node.UniverseId}",
                chapterId: null,
                category: FindingCategory.EntityDrift,
                severity: FindingSeverity.Low,
                summary: $"Entity \"{o.Name}\" ({o.Id}) has zero live beat mentions anywhere in this universe — not represented in any current book's prose.",
                snippet: o.DescriptionSnippet,
                suggestedFix: null);
        }

        return new ReconciliationReport(
            truncated, characters, liveEntities.Select(e => e.Name).ToList(), driftFindings, unmatched, orphanCandidates);
    }

    private async Task<IReadOnlyList<BibleCharacter>> ExtractBibleCharactersAsync(
        string bookTitle, string bibleExcerpt, CancellationToken ct)
    {
        const string system = """
            You read a novel's hand-authored story bible and list its named characters as the
            bible describes them RIGHT NOW.

            For each named character, give:
            - name: their proper/birth name if the bible gives one, even if the bible mostly
              refers to them by a nickname, designation, callsign, or codename elsewhere (e.g. if
              the bible says "his birth name, Declan Doyle" but otherwise calls him "M-101",
              output "Declan Doyle" — the proper name, not the designation).
            - summary: 2-4 sentences, everything factual the bible currently says about them —
              identity, key events, current status. Be specific; this will be checked word-for-word
              against an existing database record for factual drift, not just topic overlap.

            Output STRICT JSON only, no prose, no markdown fence:
            {"characters": [{"name": "...", "summary": "..."}]}
            """;
        var user = $"Book: {bookTitle}\n\nBible excerpt:\n{bibleExcerpt}\n\nList the characters now.";

        var raw = await llm.GenerateAsync(system, user, temperature: 0.2, maxTokens: 2048, ct: ct);
        return ParseBibleCharacters(raw);
    }

    private static IReadOnlyList<BibleCharacter> ParseBibleCharacters(string raw)
    {
        try
        {
            using var doc = JsonDocument.Parse(StripCodeFence(raw));
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Array) root = root.EnumerateArray().FirstOrDefault();
            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("characters", out var arr)
                || arr.ValueKind != JsonValueKind.Array)
                return [];

            var result = new List<BibleCharacter>();
            foreach (var c in arr.EnumerateArray())
            {
                if (c.ValueKind != JsonValueKind.Object) continue;
                var name = c.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                var summary = c.TryGetProperty("summary", out var s) ? s.GetString() ?? "" : "";
                if (!string.IsNullOrWhiteSpace(name)) result.Add(new BibleCharacter(name, summary));
            }
            return result;
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException)
        {
            return [];
        }
    }

    /// <summary>Directly compares an already-linked live entity's stored Description against
    /// what the current bible says about that same character, and flags factual drift (a
    /// corrected date, a changed cause of death, a superseded detail) — not just "different
    /// wording." Returns null if the LLM finds no material drift.</summary>
    private async Task<DriftFinding?> CheckDriftAsync(
        string bookTitle, BibleCharacter character, string entityDescription, CancellationToken ct)
    {
        const string system = """
            Compare an existing database Entity record's stored description of a character
            against what that book's CURRENT story bible says about the same character. The bible
            is the more current, corrected source — the entity record may be stale (written
            before a later fix, retcon, or correction to the bible).

            Flag drift ONLY for material factual disagreement: a changed date/timespan, a
            different cause or manner of an event, a superseded status, a contradicted detail.
            Do NOT flag drift for missing detail alone (the entity record being shorter or less
            detailed than the bible is normal and not a problem) or for wording differences that
            don't change any fact.

            Output STRICT JSON only, no prose, no markdown fence:
            {"drifted": bool, "explanation": "...", "suggestedDescription": "...|null"}

            If drifted is true, suggestedDescription should be a corrected version of the entity
            description that fixes the specific drifted fact(s) while preserving everything else
            about the entity record's own voice/detail level — not a full replacement with the
            bible's summary.
            """;
        var user = $"""
            Book: {bookTitle}
            Character: {character.Name}

            Current bible says: {character.Summary}

            Existing entity record says: {(string.IsNullOrWhiteSpace(entityDescription) ? "(empty)" : entityDescription)}

            Compare now.
            """;

        var raw = await llm.GenerateAsync(system, user, temperature: 0.1, maxTokens: 800, ct: ct);
        return ParseDriftFinding(character.Name, raw);
    }

    private static DriftFinding? ParseDriftFinding(string characterName, string raw)
    {
        try
        {
            using var doc = JsonDocument.Parse(StripCodeFence(raw));
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Array) root = root.EnumerateArray().FirstOrDefault();
            if (root.ValueKind != JsonValueKind.Object) return null;

            var drifted = root.TryGetProperty("drifted", out var d) && d.ValueKind == JsonValueKind.True;
            if (!drifted) return null;

            var explanation = root.TryGetProperty("explanation", out var e) ? e.GetString() ?? "" : "";
            var suggested = root.TryGetProperty("suggestedDescription", out var sd) && sd.ValueKind == JsonValueKind.String
                ? sd.GetString() : null;
            return new DriftFinding(characterName, Guid.Empty, true, explanation, suggested);
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException)
        {
            return null;
        }
    }

    private static string Snippet(string description) =>
        description.Length <= 120 ? description : description[..120].TrimEnd() + "…";

    private static bool NamesRoughlyMatch(string a, string b) =>
        string.Equals(Normalize(a), Normalize(b), StringComparison.Ordinal)
        || Normalize(a).Contains(Normalize(b)) || Normalize(b).Contains(Normalize(a));

    private static string Normalize(string s) =>
        string.Join(' ', s.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();

    private static string StripCodeFence(string text)
    {
        var t = text.Trim();
        if (!t.StartsWith("```")) return t;
        var firstNewline = t.IndexOf('\n');
        if (firstNewline >= 0) t = t[(firstNewline + 1)..];
        if (t.EndsWith("```")) t = t[..^3];
        return t.Trim();
    }
}
