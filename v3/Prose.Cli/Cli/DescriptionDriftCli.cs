using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// prose --description-drift --slug &lt;slug-or-code-or-id&gt; [--json]
/// prose --description-drift --all --universe &lt;slug&gt;          (every book in one universe)
///
/// Reports beats whose <see cref="Beat.Description"/> was verified against prose that has since
/// changed — <c>DescriptionHash != TextHash</c>. Purely deterministic: no LLM call, no embedding
/// call, no cost at all.
///
/// <para><b>Why this exists (Story Ledger Phase 1).</b> <c>Beat.Description</c> is the
/// authorial-intent line ("what this beat is DOING"), and it was the only per-beat summary the
/// read tools exposed. Unlike <c>Beat.EventSummary</c>, which has <c>EventSummaryHash</c>,
/// nothing bound Description to the prose — so it could drift from <c>Beat.Text</c> permanently
/// and silently, and a reader relying on the Description spine had no way to know. That is
/// exactly how a full-book read reported fabricated detail as established fact.
/// <see cref="Beat.DescriptionHash"/> is the binding; this command is the report over it.</para>
///
/// <para><b>Report-only, by law.</b> docs/LOGIC.md §4 — audits never write prose. Findings
/// deliberately carry no <c>Snippet</c>, so nothing can auto-splice a "fix" over a beat: a
/// stale Description is repaired by regenerating it from the prose
/// (<c>prose --backfill-meaning</c>) or by an author edit, never by this command. Same posture
/// the logic-sweep findings take.</para>
///
/// <para><b>Three states, and only one is a finding</b> (see Beat.DescriptionHash):
/// <c>current</c> (hash matches — trustworthy), <c>stale</c> (prose changed after the
/// description was written — filed as a finding), and <c>unverified</c> (a Description exists
/// that was never bound to prose: an outline/intent line, or a legacy row predating the hash).
/// Unverified rows are reported as ONE aggregate count, never one finding per beat — every
/// pre-existing beat corpus-wide is unverified, so per-beat findings there would be pure noise
/// and would bury the real ones. That is the same grandfather-then-flag posture the Story Ledger
/// plan takes for provenance.</para>
/// </summary>
public static class DescriptionDriftCli
{
    private const string SummaryPrefix = "DESCRIPTION-DRIFT";

    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        var findings  = services.GetRequiredService<FindingsService>();

        var slug = Flag(args, "--slug") ?? Flag(args, "--code") ?? Flag(args, "--id");
        var all  = args.Contains("--all");
        if (string.IsNullOrWhiteSpace(slug) && !all)
        {
            Console.Error.WriteLine(
                "Usage: prose --description-drift --slug <slug-or-code-or-id> [--json]\n" +
                "       prose --description-drift --all --universe <slug>");
            return 2;
        }

        await using var db = await dbFactory.CreateDbContextAsync();

        List<(Guid Id, string Slug, string? Code, string Title)> books;
        if (all)
        {
            // Ambient universe scope on purpose: --all means "every book in THIS universe", so
            // this stays out of Program.cs's UniverseAgnosticCommands and --universe is required
            // by the outer gate, per the universe-division hard rule.
            books = (await db.Nodes.OfType<BookNode>().AsNoTracking()
                    .OrderBy(n => n.Title)
                    .Select(n => new { n.Id, n.Slug, n.NodeCode, n.Title })
                    .ToListAsync())
                .Select(n => (n.Id, n.Slug, n.NodeCode, n.Title)).ToList();
        }
        else
        {
            var nodeId = await NodeRefResolver.ResolveAsync(db, slug);
            if (nodeId == null)
            {
                Console.Error.WriteLine($"[description-drift] No node matched '{slug}'.");
                return 2;
            }
            // IgnoreQueryFilters(): nodeId came from NodeRefResolver — explicitly named, so
            // ambient scope can only suppress the right answer.
            var one = await db.Nodes.IgnoreQueryFilters().AsNoTracking()
                .Where(n => n.Id == nodeId.Value)
                .Select(n => new { n.Id, n.Slug, n.NodeCode, n.Title })
                .FirstAsync();
            books = [(one.Id, one.Slug, one.NodeCode, one.Title)];
        }

        var reports = new List<BookReport>();
        foreach (var book in books)
        {
            var report = await AnalyzeAsync(db, book.Id, book.Slug, book.Code, book.Title);
            if (all && report.WithDescription == 0) continue;   // nothing to say about an unwritten book
            reports.Add(report);
            FileFindings(findings, report);
        }

        if (args.Contains("--json"))
        {
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(
                reports, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            return 0;
        }

        foreach (var r in reports)
        {
            Console.WriteLine($"{r.Title}  [{r.Code ?? r.Slug}]");
            Console.WriteLine($"  beats {r.TotalBeats}  ·  with description {r.WithDescription}  ·  " +
                              $"current {r.Current}  ·  STALE {r.Stale}  ·  unverified {r.Unverified}" +
                              (r.NoTextHash > 0 ? $"  ·  no-text-hash {r.NoTextHash}" : ""));
            foreach (var d in r.StaleBeats.Take(40))
                Console.WriteLine($"    stale  Beat #{d.Number}  {Truncate(d.Description, 84)}");
            if (r.StaleBeats.Count > 40)
                Console.WriteLine($"    … and {r.StaleBeats.Count - 40} more");
        }

        var totalStale = reports.Sum(r => r.Stale);
        Console.WriteLine();
        Console.WriteLine($"[description-drift] {reports.Count} book(s), {totalStale} stale description(s), " +
                          $"{reports.Sum(r => r.Unverified)} unverified. Findings filed under \"{SummaryPrefix}\".");
        if (totalStale > 0)
            Console.WriteLine("  Regenerate a stale description from its prose with: prose --backfill-meaning --slug <slug> --overwrite");
        return 0;
    }

    // ── analysis ─────────────────────────────────────────────────────────────

    public sealed record DriftedBeat(Guid BeatId, int Number, string? Title, string Description);

    public sealed record BookReport(
        Guid NodeId, string Slug, string? Code, string Title,
        int TotalBeats, int WithDescription, int Current, int Stale, int Unverified, int NoTextHash,
        List<DriftedBeat> StaleBeats);

    /// <summary>internal, not private: pinned directly by DescriptionDriftTests, which is the
    /// only way to exercise the "stale" branch without editing real prose (the grandfathered
    /// corpus is entirely "unverified", so a live run can never reach it).</summary>
    internal static async Task<BookReport> AnalyzeAsync(
        ProseDbContext db, Guid bookId, string slug, string? code, string title)
    {
        // GetLeafDescendantIdsAsync, not a one-level ParentNodeId query: a book with a nested
        // sub-chapter collection silently drops those beats otherwise (CLAUDE.md's recursive
        // descendant-walk hard rule).
        var leafIds = await NodeWorkbenchService.GetLeafDescendantIdsAsync(db, bookId);

        var beats = await (
            from bn in db.BeatNodes.AsNoTracking()
            join b in db.Beats.AsNoTracking() on bn.BeatId equals b.Id
            where leafIds.Contains(bn.NodeId)
            orderby bn.SortKey
            select new { b.Id, b.Number, b.Title, b.Description, b.DescriptionHash, b.TextHash }
        ).ToListAsync();

        int current = 0, stale = 0, unverified = 0, noTextHash = 0;
        var staleBeats = new List<DriftedBeat>();

        foreach (var b in beats)
        {
            if (string.IsNullOrWhiteSpace(b.Description)) continue;

            // A beat with no TextHash cannot be judged either way — calling it "stale" would be
            // an assertion about prose we have no fingerprint for. Counted separately so the
            // number is visible rather than folded into "unverified", but never filed.
            if (!string.IsNullOrWhiteSpace(b.DescriptionHash) && string.IsNullOrWhiteSpace(b.TextHash))
            {
                noTextHash++;
                continue;
            }

            // One definition of the three states, shared with the read payloads (Beat.cs).
            switch (Beat.SummaryTrustState(b.Description, b.DescriptionHash, b.TextHash))
            {
                case "current": current++; break;
                case "stale":
                    stale++;
                    staleBeats.Add(new DriftedBeat(b.Id, b.Number, b.Title, b.Description!));
                    break;
                default: unverified++; break;
            }
        }

        var withDescription = beats.Count(b => !string.IsNullOrWhiteSpace(b.Description));
        return new BookReport(bookId, slug, code, title,
            beats.Count, withDescription, current, stale, unverified, noTextHash, staleBeats);
    }

    // ── findings ─────────────────────────────────────────────────────────────

    private static void FileFindings(FindingsService findings, BookReport r)
    {
        // Delete-then-recreate at book scope, the standard findings lifecycle: a description
        // that has since been regenerated must lose its finding even though nothing re-emits
        // for it this run (Upsert alone never removes a row whose condition stopped holding).
        findings.DeleteBySummaryPrefix($"node:{r.Slug}", SummaryPrefix);

        foreach (var d in r.StaleBeats)
        {
            findings.Upsert(
                filePath: $"node:{r.Slug}/beat:{d.Number}",
                chapterId: null,
                category: FindingCategory.SemanticDrift,
                severity: FindingSeverity.Medium,
                summary: $"{SummaryPrefix}: Beat #{d.Number}'s description was written against prose that has since changed — " +
                         $"\"{Truncate(d.Description, 140)}\" no longer describes the beat's current text.",
                // No snippet, deliberately: with no Snippet/SuggestedFix pair there is nothing
                // for an apply path to splice into the prose. docs/LOGIC.md §4.
                snippet: null,
                suggestedFix: $"Re-derive Beat #{d.Number}'s description from its current prose " +
                              $"(prose --backfill-meaning --slug {r.Slug} --overwrite), or edit it by hand. " +
                              "Do not edit the prose to match the description.");
        }

        if (r.Unverified > 0)
        {
            findings.Upsert(
                filePath: $"node:{r.Slug}",
                chapterId: null,
                category: FindingCategory.SemanticDrift,
                severity: FindingSeverity.Low,
                summary: $"{SummaryPrefix} [unverified]: {r.Unverified} of {r.WithDescription} beat description(s) in " +
                         $"'{r.Title}' have never been verified against prose (no DescriptionHash).",
                snippet: null,
                suggestedFix: "These are outline/intent lines or rows predating hash stamping. Treat them as plans, " +
                              "not as summaries of what the prose says. Running prose --backfill-meaning --overwrite " +
                              "re-derives them from the prose and stamps them.");
        }
    }

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..(max - 1)] + "…";

    private static string? Flag(string[] args, string name)
    {
        var i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }
}
