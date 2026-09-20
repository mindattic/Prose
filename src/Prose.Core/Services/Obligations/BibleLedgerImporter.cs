using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Core.Services.Obligations;

/// <summary>
/// Imports a node bible's hand-kept plant/payoff ledger (the BCODA §14 tables) into the Narrative
/// Obligation Ledger (RFC 0013, BCODA runbook step 3).
///
/// <para>§14a "Closed plants (payoff written)" rows — <c>| Plant | Plant location | Payoff | Payoff
/// location | Closed |</c> — become <c>plant</c>-kind rows: <c>authored</c>, author-locked,
/// <c>Closed</c>, mirrored into <see cref="PlantPayoff"/> so the existing plant/payoff surfaces see
/// them. §14b "Dropped findings" rows — <c>| Finding | Reason dropped | Date |</c> — become
/// <c>Dropped</c> rows with <c>DroppedReason = author-note</c> and the bible's own reason as the
/// author note, verbatim.</para>
///
/// <para>Anchors are resolved from the bible's <c>Ch&lt;n&gt; SK:&lt;k&gt;</c> labels: leaf ordinal
/// <c>n</c> in the current reading order, then the beat in that chapter whose <c>BeatNodes.SortKey</c>
/// is EXACTLY <c>k</c>. Anything else — a label the tree no longer matches (the "Ch21 SK80000"
/// case that predates reparenting), a place name instead of a chapter, a chapter past the end —
/// is imported with a null anchor and reported under <b>NeedsAnchor</b>, listed first for the
/// author. A lossy anchor is never guessed at. Rows carry no verbatim quote: the bible is the
/// author's word, not the page's, and <c>NarrativeObligationService.TrialBalanceAsync</c> treats
/// an authored row without a quote as neither stale nor dangling.</para>
///
/// <para>Idempotent through <see cref="NarrativeObligation.DedupKey"/>: a re-run reports every
/// existing row as <c>exists</c> and writes nothing. Dry-run parses and resolves without a write.
/// Nothing here touches prose.</para>
/// </summary>
public class BibleLedgerImporter(IDbContextFactory<ProseDbContext> dbFactory, ILogger<BibleLedgerImporter> log)
{
    // ── Parsed shapes ─────────────────────────────────────────────────────────────

    /// <summary>A bible location label. <see cref="Chapter"/>/<see cref="SortKey"/> are set only
    /// when the label is the canonical <c>Ch&lt;n&gt; SK:&lt;k&gt;</c> form.</summary>
    public sealed record Location(string Raw, int? Chapter, double? SortKey)
    {
        public bool IsCanonical => Chapter is not null && SortKey is not null;
    }

    public sealed record ClosedPlantRow(string Plant, Location PlantLocation, string Payoff, Location PayoffLocation, string ClosedOn);
    public sealed record DroppedRow(string Finding, string Reason, string Date);

    public sealed record ParsedLedger(IReadOnlyList<ClosedPlantRow> Closed, IReadOnlyList<DroppedRow> Dropped, IReadOnlyList<string> Warnings)
    {
        public bool IsEmpty => Closed.Count == 0 && Dropped.Count == 0;
    }

    // ── Import report ─────────────────────────────────────────────────────────────

    public static class RowAction
    {
        public const string Created = "created";
        public const string Exists  = "exists";
        public const string DryRun  = "dry-run";
    }

    /// <summary>One bible row's fate. <see cref="NeedsAnchor"/> lists every location label that
    /// could not be resolved to a beat, with the reason.</summary>
    public sealed record RowOutcome(
        string Table, string Kind, string State, string Description, string Action,
        Guid? ObligationId, Guid? OriginBeatId, Guid? ClosingBeatId,
        IReadOnlyList<string> NeedsAnchor, string? Warning);

    public sealed record ImportReport(Guid NodeId, string? Source, bool DryRun, IReadOnlyList<RowOutcome> Outcomes, IReadOnlyList<string> Warnings)
    {
        public int ClosedRows      => Outcomes.Count(o => o.Table == "14a");
        public int DroppedRows     => Outcomes.Count(o => o.Table == "14b");
        public int Created         => Outcomes.Count(o => o.Action == RowAction.Created);
        public int Existing        => Outcomes.Count(o => o.Action == RowAction.Exists);
        public int NeedsAnchorRows => Outcomes.Count(o => o.NeedsAnchor.Count > 0);
        /// <summary>No §14 ledger in the bible at all — the author has to know that is why nothing landed.</summary>
        public bool CouldNotLook   => Source == null;
    }

    // ── Parsing (pure, testable) ──────────────────────────────────────────────────

    private static readonly Regex HeadingLine   = new(@"^\s{0,3}#{1,6}\s+(.*)$", RegexOptions.Compiled);
    private static readonly Regex Section14a    = new(@"^\s{0,3}#{1,6}\s+14a\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex Section14b    = new(@"^\s{0,3}#{1,6}\s+14b\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex SeparatorRow  = new(@"^\s*\|?\s*:?-{3,}:?\s*(\|\s*:?-{3,}:?\s*)*\|?\s*$", RegexOptions.Compiled);
    private static readonly Regex CanonicalLoc  = new(@"\bCh\s*(\d+)\b[^\d]{0,4}?SK\s*:?\s*(\d+(?:\.\d+)?)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex Bold          = new(@"\*\*(.*?)\*\*", RegexOptions.Compiled | RegexOptions.Singleline);
    private static readonly Regex Whitespace    = new(@"\s+", RegexOptions.Compiled);

    /// <summary>Does this markdown carry a §14a or §14b table at all?</summary>
    public static bool HasLedger(string? markdown) =>
        !string.IsNullOrWhiteSpace(markdown) &&
        markdown.Split('\n').Any(l => Section14a.IsMatch(l.TrimEnd('\r')) || Section14b.IsMatch(l.TrimEnd('\r')));

    public static ParsedLedger Parse(string? bibleMarkdown)
    {
        var closed = new List<ClosedPlantRow>();
        var dropped = new List<DroppedRow>();
        var warnings = new List<string>();
        if (string.IsNullOrWhiteSpace(bibleMarkdown)) return new(closed, dropped, warnings);

        // Entity tags carry guids the author never typed; the ledger wants the visible text.
        var text = BeatMarkup.StripEntityTags(bibleMarkdown);
        var lines = text.Split('\n').Select(l => l.TrimEnd('\r')).ToList();

        foreach (var row in TableRows(lines, Section14a))
        {
            if (row.Count < 4)
            {
                warnings.Add($"§14a row with {row.Count} cell(s) skipped: \"{Trunc(string.Join(" | ", row), 80)}\"");
                continue;
            }
            closed.Add(new ClosedPlantRow(
                Plant: row[0], PlantLocation: ParseLocation(row[1]),
                Payoff: row[2], PayoffLocation: ParseLocation(row[3]),
                ClosedOn: row.Count > 4 ? row[4] : ""));
        }

        foreach (var row in TableRows(lines, Section14b))
        {
            if (row.Count < 2)
            {
                warnings.Add($"§14b row with {row.Count} cell(s) skipped: \"{Trunc(string.Join(" | ", row), 80)}\"");
                continue;
            }
            dropped.Add(new DroppedRow(Finding: row[0], Reason: row[1], Date: row.Count > 2 ? row[2] : ""));
        }

        return new(closed, dropped, warnings);
    }

    /// <summary><c>Ch14 SK:18000</c> → (14, 18000). <c>35th and Halsted SK:5000</c> → raw only.</summary>
    public static Location ParseLocation(string cell)
    {
        var raw = Clean(cell);
        var m = CanonicalLoc.Match(raw);
        if (!m.Success) return new Location(raw, null, null);
        var chapter = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
        var sortKey = double.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
        return new Location(raw, chapter, sortKey);
    }

    /// <summary>Data rows (header and separator dropped) of the first table under the heading
    /// <paramref name="heading"/> matches; the section ends at the next heading of any level.</summary>
    private static List<List<string>> TableRows(List<string> lines, Regex heading)
    {
        var rows = new List<List<string>>();
        var start = lines.FindIndex(heading.IsMatch);
        if (start < 0) return rows;

        var sawHeader = false;
        for (var i = start + 1; i < lines.Count; i++)
        {
            var line = lines[i];
            if (HeadingLine.IsMatch(line)) break;
            if (!line.TrimStart().StartsWith('|')) continue;
            if (SeparatorRow.IsMatch(line)) continue;
            var cells = SplitCells(line);
            if (!sawHeader) { sawHeader = true; continue; }   // the column-title row
            rows.Add(cells);
        }
        return rows;
    }

    private static List<string> SplitCells(string line)
    {
        // Split on unescaped pipes; drop the empty edge cells a leading/trailing "|" produces.
        var parts = Regex.Split(line.Trim(), @"(?<!\\)\|").Select(c => c.Replace(@"\|", "|")).ToList();
        if (parts.Count > 0 && parts[0].Trim().Length == 0) parts.RemoveAt(0);
        if (parts.Count > 0 && parts[^1].Trim().Length == 0) parts.RemoveAt(parts.Count - 1);
        return parts.Select(Clean).ToList();
    }

    private static string Clean(string cell)
    {
        var s = Bold.Replace(cell, "$1");
        return Whitespace.Replace(s, " ").Trim();
    }

    private static string Trunc(string s, int max) => s.Length <= max ? s : s[..(max - 1)] + "…";

    // ── Import ────────────────────────────────────────────────────────────────────

    public async Task<ImportReport> ImportAsync(Guid bookNodeId, bool dryRun, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var (source, markdown) = await LoadBibleAsync(db, bookNodeId, ct);
        if (markdown == null)
            return new ImportReport(bookNodeId, null, dryRun, [], ["no §14a/§14b ledger table found in this node's outline sections or NodeOutline"]);

        var parsed = Parse(markdown);
        var warnings = new List<string>(parsed.Warnings);
        var clock = await NarrativeObligationService.LoadClockAsync(db, bookNodeId, ct);
        var universeId = await db.Nodes.AsNoTracking().IgnoreQueryFilters().Where(n => n.Id == bookNodeId).Select(n => n.UniverseId).FirstAsync(ct);

        // SortKeys per chapter, loaded once: anchors need an exact match against them.
        var sortKeys = await db.BeatNodes.AsNoTracking()
            .Where(bn => clock.ChapterIds.Contains(bn.NodeId))
            .Select(bn => new BeatSortKey(bn.NodeId, bn.BeatId, bn.SortKey))
            .ToListAsync(ct);
        var byChapter = sortKeys.ToLookup(x => x.NodeId);

        var outcomes = new List<RowOutcome>();
        var plantSortKey = await db.PlantPayoffs.Where(p => p.NodeId == bookNodeId).MaxAsync(p => (double?)p.SortKey, ct) ?? 0;

        foreach (var row in parsed.Closed)
        {
            var description = Trunc($"{row.Plant} → {row.Payoff}", 500);
            var dedup = NarrativeObligationService.DedupKey(bookNodeId, ObligationKind.Plant, description);
            var needs = new List<string>();
            var origin = Resolve(row.PlantLocation, "plant", clock, byChapter, needs);
            var closing = Resolve(row.PayoffLocation, "payoff", clock, byChapter, needs);

            var existing = await db.NarrativeObligations.AsNoTracking().FirstOrDefaultAsync(o => o.NodeId == bookNodeId && o.DedupKey == dedup, ct);
            if (existing != null)
            {
                outcomes.Add(new RowOutcome("14a", ObligationKind.Plant, existing.State, description, RowAction.Exists, existing.Id, existing.OriginBeatId, existing.ClosingBeatId, needs, null));
                continue;
            }
            if (dryRun)
            {
                outcomes.Add(new RowOutcome("14a", ObligationKind.Plant, ObligationState.Closed, description, RowAction.DryRun, null, origin?.BeatId, closing?.BeatId, needs, null));
                continue;
            }

            var note = Trunc($"§14a closed {row.ClosedOn}; plant @ {row.PlantLocation.Raw}; payoff @ {row.PayoffLocation.Raw}"
                             + (needs.Count > 0 ? $"; NEEDS ANCHOR: {string.Join("; ", needs)}" : ""), 1000);
            var ob = new NarrativeObligation
            {
                NodeId = bookNodeId, Kind = ObligationKind.Plant, Description = description,
                Provenance = ClaimProvenance.Authored, AuthorLocked = true, State = ObligationState.Closed,
                OriginBeatId = origin?.BeatId, OriginTextHash = origin?.TextHash,
                ClosingBeatId = closing?.BeatId, ClosingTextHash = closing?.TextHash,
                DueByKind = ObligationDueKind.BookEnd, AuthorNote = note, DedupKey = dedup,
            };
            db.NarrativeObligations.Add(ob);
            db.NarrativeObligationEvents.Add(new NarrativeObligationEvent
            {
                ObligationId = ob.Id, Action = ObligationEventAction.Open, BeatId = origin?.BeatId, BeatTextHash = origin?.TextHash,
                Actor = ObligationActor.ImportBible, Note = $"§14a plant @ {row.PlantLocation.Raw}",
            });
            db.NarrativeObligationEvents.Add(new NarrativeObligationEvent
            {
                ObligationId = ob.Id, Action = ObligationEventAction.Close, BeatId = closing?.BeatId, BeatTextHash = closing?.TextHash,
                Actor = ObligationActor.ImportBible, Note = $"§14a payoff @ {row.PayoffLocation.Raw}; closed {row.ClosedOn}",
            });

            // Mirror into PlantPayoffs so the plant/payoff surfaces (audit, Brief) see the pair.
            plantSortKey += 100;
            db.PlantPayoffs.Add(new PlantPayoff
            {
                NodeId = bookNodeId, UniverseId = universeId,
                PlantDescription = Trunc(row.Plant, 2000), PayoffDescription = Trunc(row.Payoff, 2000),
                Category = "detail", PlantBeatId = origin?.BeatId, PayoffBeatId = closing?.BeatId,
                SortKey = plantSortKey, ObligationId = ob.Id,
            });
            await db.SaveChangesAsync(ct);
            outcomes.Add(new RowOutcome("14a", ObligationKind.Plant, ObligationState.Closed, description, RowAction.Created, ob.Id, ob.OriginBeatId, ob.ClosingBeatId, needs, null));
        }

        foreach (var row in parsed.Dropped)
        {
            var description = Trunc(row.Finding, 500);
            var dedup = NarrativeObligationService.DedupKey(bookNodeId, ObligationKind.Promise, description);
            // The bible keeps a row in "Dropped findings" even after it was later closed on the
            // page; surface that so the author can reopen/close it by hand rather than trust a drop.
            var warning = row.Reason.Contains("CLOSED", StringComparison.Ordinal)
                ? "reason text says CLOSED — the finding may have been paid on the page since it was dropped; review"
                : null;

            var existing = await db.NarrativeObligations.AsNoTracking().FirstOrDefaultAsync(o => o.NodeId == bookNodeId && o.DedupKey == dedup, ct);
            if (existing != null)
            {
                outcomes.Add(new RowOutcome("14b", ObligationKind.Promise, existing.State, description, RowAction.Exists, existing.Id, existing.OriginBeatId, existing.ClosingBeatId, [], warning));
                continue;
            }
            if (dryRun)
            {
                outcomes.Add(new RowOutcome("14b", ObligationKind.Promise, ObligationState.Dropped, description, RowAction.DryRun, null, null, null, [], warning));
                continue;
            }

            var note = Trunc(string.IsNullOrWhiteSpace(row.Date) ? row.Reason : $"{row.Reason} ({row.Date})", 1000);
            var ob = new NarrativeObligation
            {
                NodeId = bookNodeId, Kind = ObligationKind.Promise, Description = description,
                Provenance = ClaimProvenance.Authored, AuthorLocked = true, State = ObligationState.Dropped,
                DroppedReason = ObligationDroppedReason.AuthorNote, AuthorNote = note,
                DueByKind = ObligationDueKind.BookEnd, DedupKey = dedup,
            };
            db.NarrativeObligations.Add(ob);
            db.NarrativeObligationEvents.Add(new NarrativeObligationEvent { ObligationId = ob.Id, Action = ObligationEventAction.Open, Actor = ObligationActor.ImportBible, Note = "§14b dropped finding" });
            db.NarrativeObligationEvents.Add(new NarrativeObligationEvent { ObligationId = ob.Id, Action = ObligationEventAction.Drop, Actor = ObligationActor.ImportBible, Note = $"{ObligationDroppedReason.AuthorNote}: {note}" });
            await db.SaveChangesAsync(ct);
            outcomes.Add(new RowOutcome("14b", ObligationKind.Promise, ObligationState.Dropped, description, RowAction.Created, ob.Id, null, null, [], warning));
        }

        if (parsed.IsEmpty) warnings.Add($"§14 heading found in {source} but its tables held no data rows");
        log.LogInformation("Bible ledger import for {Node} from {Source}: {Created} created, {Existing} existing, {NeedsAnchor} need an anchor{DryRun}",
            bookNodeId, source, outcomes.Count(o => o.Action == RowAction.Created), outcomes.Count(o => o.Action == RowAction.Exists),
            outcomes.Count(o => o.NeedsAnchor.Count > 0), dryRun ? " (dry run)" : "");

        // NeedsAnchor rows first — they are the author's work.
        var ordered = outcomes.OrderByDescending(o => o.NeedsAnchor.Count > 0).ThenBy(o => o.Table).ToList();
        return new ImportReport(bookNodeId, source, dryRun, ordered, warnings);
    }

    private sealed record BeatSortKey(Guid NodeId, Guid BeatId, double SortKey);
    private sealed record Anchor(Guid BeatId, string? TextHash);

    private static Anchor? Resolve(Location loc, string role, NarrativeObligationService.BookClock clock,
        ILookup<Guid, BeatSortKey> byChapter, List<string> needs)
    {
        if (!loc.IsCanonical)
        {
            needs.Add($"{role} \"{loc.Raw}\" is not a Ch<n> SK:<k> label");
            return null;
        }
        var ch = loc.Chapter!.Value;
        if (ch < 1 || ch > clock.ChapterIds.Count)
        {
            needs.Add($"{role} \"{loc.Raw}\": chapter {ch} is outside the current {clock.ChapterIds.Count}-chapter reading order");
            return null;
        }
        var chapterId = clock.ChapterIds[ch - 1];
        var candidates = byChapter[chapterId].ToList();
        var exact = candidates.FirstOrDefault(c => Math.Abs(c.SortKey - loc.SortKey!.Value) < 1e-6);
        if (exact == null)
        {
            var nearest = candidates.Count == 0 ? null : candidates.OrderBy(c => Math.Abs(c.SortKey - loc.SortKey!.Value)).First();
            needs.Add(nearest == null
                ? $"{role} \"{loc.Raw}\": chapter {ch} has no beats"
                : $"{role} \"{loc.Raw}\": no beat in chapter {ch} has SortKey {loc.SortKey}; nearest is {nearest.SortKey:0.###} — not guessed");
            return null;
        }
        return new Anchor(exact.BeatId, null);
    }

    /// <summary>The node's bible text that carries the §14 ledger: the outline sections first (the
    /// canonical, DB-backed store), then the legacy <c>Nodes.NodeOutline</c> blob.</summary>
    private static async Task<(string? Source, string? Markdown)> LoadBibleAsync(ProseDbContext db, Guid bookNodeId, CancellationToken ct)
    {
        var sections = await db.NodeOutlineSections.AsNoTracking()
            .Where(s => s.NodeId == bookNodeId)
            .Select(s => new { s.SectionType, s.Content })
            .ToListAsync(ct);
        foreach (var s in sections.OrderBy(s => s.SectionType == "Full" ? 0 : 1))
            if (HasLedger(s.Content)) return ($"NodeOutlineSections.{s.SectionType}", s.Content);

        var blob = await db.Nodes.AsNoTracking().IgnoreQueryFilters().Where(n => n.Id == bookNodeId).Select(n => n.NodeOutline).FirstOrDefaultAsync(ct);
        if (HasLedger(blob)) return ("Nodes.NodeOutline", blob);
        return (null, null);
    }
}
