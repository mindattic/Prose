using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;
using Prose.Core.Interfaces;

namespace Prose.Core.Services;

/// <summary>
/// Full-coverage bible↔blueprint↔beat coordination for a book node.
///
/// Produces a 3-coordinate record for EVERY enabled beat:
///   • MEANING      — what the beat means            (Beat.Description + bible)
///   • CONSTRUCTION — how the beat is to be built     (blueprint chapter slice + beat tags)
///   • PROSE        — what actually accomplishes both (Beat.Text)
///
/// Unlike the session-scoped BibleSync/BlueprintSync services, this correlates
/// the whole node at once and FLAGS every beat missing a coordinate — the gap
/// list is the first tranche of gripe/contradiction/cliché candidates. Read-only
/// over the DB; the only write is a regenerable "## Beat Coordination Index"
/// section in docs/nodes/&lt;CODE&gt;.md plus a JSON report artifact.
/// </summary>
public class BeatCoordinationService
{
    private readonly IDbContextFactory<ProseDbContext> dbFactory;
    private readonly IPathProvider paths;
    private readonly MarkdownFileService markdown;
    private readonly BeatModeDetector beatMode;
    private readonly ILogger<BeatCoordinationService> log;

    private const int StubProseThreshold = 200;
    private const string IndexHeading = "## Beat Coordination Index";

    // Characters that mark a beat as a complete, deliberate short fragment (not a truncation).
    private static readonly char[] TerminalPunctuation = { '.', '!', '?', '"', '”', '*', ')', ']', '…' };

    public BeatCoordinationService(
        IDbContextFactory<ProseDbContext> dbFactory,
        IPathProvider paths,
        MarkdownFileService markdown,
        BeatModeDetector beatMode,
        ILogger<BeatCoordinationService> log)
    {
        this.dbFactory = dbFactory;
        this.paths = paths;
        this.markdown = markdown;
        this.beatMode = beatMode;
        this.log = log;
    }

    /// <summary>
    /// Role-aware: a short beat is a real STUB gap only if it is thin NARRATIVE prose. Combat,
    /// dialogue, transition, emotional, and revelation beats are terse by design; so are interior
    /// monologue beats and any complete short fragment. This keeps intentional staccato (the
    /// combat/duel register, interior thought) from being read as coverage gaps.
    /// </summary>
    private bool IsTerseByDesign(string? meaning, string prose)
    {
        var trimmedStart = prose.TrimStart();
        if (trimmedStart.StartsWith("*")) return true;                       // interior monologue
        if (prose.IndexOf('"') >= 0 || prose.IndexOf('“') >= 0) return true;  // carries dialogue
        if (beatMode.Detect(meaning, prose).Mode != BeatMode.Narrative) return true;  // combat/dialogue/transition/emotional/revelation
        var trimmedEnd = prose.TrimEnd();
        if (trimmedEnd.Length > 0 && TerminalPunctuation.Contains(trimmedEnd[^1])) return true;  // complete deliberate fragment
        return false;
    }

    public async Task<CoordinationReport> CoordinateAsync(
        string slugOrCode, string? jsonPath = null, bool stamp = true,
        CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var node = await db.Nodes.AsNoTracking().FirstOrDefaultAsync(
            n => n.Slug == slugOrCode || (n.NodeCode != null && n.NodeCode.ToUpper() == slugOrCode.ToUpper()), ct)
            ?? throw new InvalidOperationException($"Node not found: {slugOrCode}");

        var nodeCode = node.NodeCode?.ToUpperInvariant() ?? node.Slug.ToUpperInvariant();

        // Chapters in spine order → chapter index (blueprint arrays are chapter-parallel).
        // Descend to LEAF nodes, not just direct children — a split-collection book (Book ->
        // "Chapter N" container with 0 direct beats -> real chapters -> beats, e.g.
        // BLST/ICFI/RTR/VIGL) has its real chapters two levels down; direct-children-only
        // found just the empty container, reporting totalBeats:0 for these books (confirmed
        // live 2026-08-10 — a --audit-book run regenerated VIGL.coordination.json with an
        // empty beats array where 318 real beats existed). Preserve GetLeafDescendantIdsAsync's
        // own return order rather than re-sorting by Node.SortKey, which is only comparable
        // within one parent's sibling group.
        var leafIds = await NodeWorkbenchService.GetLeafDescendantIdsAsync(db, node.Id, ct);
        var chapterMeta = await db.Nodes.AsNoTracking().IgnoreQueryFilters()
            .Where(c => leafIds.Contains(c.Id))
            .Select(c => new { c.Id, c.NodeCode, c.Title, c.SortKey })
            .ToDictionaryAsync(c => c.Id, ct);
        var chapters = leafIds.Where(chapterMeta.ContainsKey).Select(id => chapterMeta[id]).ToList();

        // Direct beats (single-node books) count as one "chapter 0"
        var chapterIndex = new Dictionary<Guid, int>();
        for (int i = 0; i < chapters.Count; i++) chapterIndex[chapters[i].Id] = i;
        var chapterLabel = chapters.ToDictionary(
            c => c.Id, c => c.NodeCode ?? (c.Title.Length > 24 ? c.Title[..24] : c.Title));

        // Blueprint: chapter-parallel escalation curve + event palette + per-beat tags
        var bp = await db.NodeStructuralBlueprints.AsNoTracking()
            .Include(x => x.BeatTags)
            .FirstOrDefaultAsync(x => x.NodeId == node.Id, ct);

        int[] escalation = ParseIntArray(bp?.EscalationCurveJson);
        var events = ParseEventPalette(bp?.EventTypePaletteJson);
        var tagsByBeat = bp?.BeatTags
            .GroupBy(t => t.BeatId)
            .ToDictionary(g => g.Key, g => g.ToList())
            ?? new Dictionary<Guid, List<Data.Entities.NodeStructuralBlueprintBeatTag>>();
        bool chapterGranular = string.Equals(bp?.Granularity, "chapter", StringComparison.OrdinalIgnoreCase);

        // All enabled beats across the node's chapters, in reading order. Query per leaf, in
        // leaf order (already correct depth-first order from GetLeafDescendantIdsAsync), then
        // concatenate — a single flat "orderby c.SortKey, bn.SortKey" would be wrong the
        // moment there's more than one leaf: Node.SortKey is only comparable within one
        // parent's sibling group, and BeatNodes.SortKey restarts at 100 within each chapter
        // (same fix applied elsewhere, e.g. DcmVizCli, NarrativeForkService).
        var rows = new List<(Guid Id, int Number, string? Title, string? StructureRole, int Act,
            string? Description, string? Subtext, double? Score, string Prose, Guid ChapterId)>();
        foreach (var leafId in leafIds)
        {
            var leafRows = await (
                from bn in db.BeatNodes.AsNoTracking()
                join b in db.Beats.AsNoTracking() on bn.BeatId equals b.Id
                where bn.NodeId == leafId && bn.IsEnabled
                orderby bn.SortKey
                select new
                {
                    b.Id, b.Number, b.Title, b.StructureRole, b.Act,
                    b.Description, b.Subtext, b.Score,
                    Prose = b.Text,
                }).ToListAsync(ct);
            rows.AddRange(leafRows.Select(r => (r.Id, r.Number, (string?)r.Title, (string?)r.StructureRole, r.Act,
                (string?)r.Description, (string?)r.Subtext, r.Score, r.Prose, ChapterId: leafId)));
        }

        // The blueprint arrays are indexed by the blueprint's own granularity:
        // chapter-granular blueprints are chapter-parallel (one entry per chapter),
        // beat-granular blueprints are beat-parallel (one entry per beat in reading
        // order) — regardless of whether the book is chaptered or flat. Key the
        // construction slice by the matching index so every beat reads its OWN slice.
        int ordinal = -1;

        var coords = new List<BeatCoordinate>(rows.Count);
        foreach (var r in rows)
        {
            ordinal++;
            int chIdx = chapterIndex.TryGetValue(r.ChapterId, out var ci) ? ci : -1;
            int constrIdx = chapterGranular ? chIdx : ordinal;
            var flags = new List<string>();

            var meaning = string.IsNullOrWhiteSpace(r.Description) ? null : r.Description!.Trim();
            if (meaning == null) flags.Add("MISSING_MEANING");

            var proseText = r.Prose ?? "";
            int proseLen = proseText.Length;
            if (proseLen == 0) flags.Add("NO_PROSE");
            else if (proseLen < StubProseThreshold && !IsTerseByDesign(meaning, proseText))
                flags.Add("STUB_PROSE");

            // UNSCORED was retired 2026-08-10: Beat.Score is written only by the legacy
            // dual-review panel (NodeReviewService), which SS-A44 (author ruling 2026-08-03,
            // "remove scores; they mean nothing") quarantined behind an explicit opt-in and
            // retired from the default pipeline. Every beat written through the normal
            // ProseWriterRouter path has Score==null by design, not by defect — flagging it
            // made every single beat in every book permanently "unscored" for no fixable
            // reason. BookHealthService.BeatCoordinationAsync already special-cased a
            // UNSCORED-only beat as Covered (see its own comment: "UNSCORED is expected
            // noise"); this makes that the actual truth instead of a downstream patch.

            // Construction slice (chapter-parallel for chaptered books, beat-parallel for flat ones)
            string? esc = constrIdx >= 0 && constrIdx < escalation.Length
                ? escalation[constrIdx].ToString() : null;
            var evt = constrIdx >= 0 ? events.FirstOrDefault(e => e.BeatIndex == constrIdx) : null;
            tagsByBeat.TryGetValue(r.Id, out var beatTags);
            var tagList = beatTags?.Select(t => $"{t.TagType}{(t.Confirmed ? "✓" : "")}").ToList() ?? new();

            bool hasConstruction = esc != null || evt != null || tagList.Count > 0;
            if (!hasConstruction) flags.Add("NO_CONSTRUCTION");

            coords.Add(new BeatCoordinate
            {
                BeatId          = r.Id,
                Number          = r.Number,
                Chapter         = chapterLabel.TryGetValue(r.ChapterId, out var lbl) ? lbl : "—",
                ChapterIndex    = chIdx,
                StructureRole   = r.StructureRole,
                Act             = r.Act,
                Meaning         = meaning,
                Subtext         = string.IsNullOrWhiteSpace(r.Subtext) ? null : r.Subtext!.Trim(),
                EscalationTarget = esc,
                EventType       = evt?.EventType,
                RevelationMode  = evt?.RevelationMode,
                BeatTags        = tagList,
                ProseLength     = proseLen,
                Score           = r.Score,
                Flags           = flags,
                Covered         = flags.Count == 0,
            });
        }

        // How many beat-slots the blueprint's escalation/event arrays actually cover —
        // beat-granular blueprints are sized to the beat count AT GENERATION TIME and are
        // never resized when beats are later split, so a book that's grown well past this
        // capacity will show NO_CONSTRUCTION on every beat beyond it. Exposed so callers
        // (e.g. BookHealthService) can tell "blueprint is stale/undersized" apart from a
        // genuine per-beat gap inside the blueprint's covered range.
        // Bug fixed 2026-08-10: this used to report chapters.Count for chapter-granular books —
        // how many chapters the BOOK currently has, not how many the BLUEPRINT's escalation/
        // event arrays actually cover (the field's own doc comment always specified the latter).
        // Caught live: VIGL and BLST both have chapter-granular blueprints with escalation
        // curves of length 1 (e.g. "[8]") and a single event-palette entry, covering only
        // chapter index 0 — but this formula reported ConstructionCapacity=25 (VIGL) / =21
        // (BLST), the book's real chapter count, hiding the staleness completely and letting
        // BookHealthService's beat-granular-only consolidation check (which compares this value
        // against the book's size to decide whether to file ONE "blueprint stale" finding
        // instead of one per beat) never fire for chapter-granular books. The same Math.Max
        // formula already used for beat-granular is the correct one for both — it measures the
        // blueprint's own array size regardless of granularity.
        int constructionCapacity =
            Math.Max(escalation.Length, events.Count == 0 ? 0 : events.Max(e => e.BeatIndex) + 1);

        // Book-wide construction context (applies to every beat)
        var bookScope = new BookScopeContext
        {
            TemporalScheme = bp?.TemporalScheme,
            ResolutionMode = bp?.ResolutionMode,
            MoralPolarity  = bp?.MoralPolarity,
            EndingStyle    = bp?.EndingStyle,
            HasSubplot     = bp?.HasSubplot ?? false,
            Granularity    = bp?.Granularity ?? "beat",
            HasBlueprint   = bp != null,
            ConstructionCapacity = constructionCapacity,
        };

        var flagCounts = coords
            .SelectMany(c => c.Flags)
            .GroupBy(f => f)
            .ToDictionary(g => g.Key, g => g.Count());

        // ── Write JSON report artifact ──
        var jsonOut = new
        {
            nodeCode,
            slug = node.Slug,
            title = node.Title,
            generatedAtUtc = DateTime.UtcNow.ToString("u"),
            totalBeats = coords.Count,
            covered = coords.Count(c => c.Covered),
            chapterGranular,
            bookScope,
            flagCounts,
            beats = coords,
        };

        var jsonOpts = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };
        var json = JsonSerializer.Serialize(jsonOut, jsonOpts);

        jsonPath ??= Path.Combine(paths.DataRoot, "reports", "coordination", $"{nodeCode}.coordination.json");
        Directory.CreateDirectory(Path.GetDirectoryName(jsonPath)!);
        await File.WriteAllTextAsync(jsonPath, json, ct);

        // ── Stamp the ## Beat Coordination Index into the bible ──
        string? stampedTo = null;
        if (stamp)
        {
            var bibleFile = Path.Combine(paths.DataRoot, "docs", "nodes", $"{nodeCode}.md");
            if (File.Exists(bibleFile))
            {
                var section = BuildIndexSection(nodeCode, coords, bookScope, flagCounts, jsonPath);
                await ReplaceSectionAsync(bibleFile, section, ct);
                await markdown.SyncAllAsync(ct: ct);
                stampedTo = bibleFile;
            }
            else
            {
                log.LogWarning("Bible file not found at {Path} — index not stamped", bibleFile);
            }
        }

        return new CoordinationReport(
            nodeCode, coords.Count, coords.Count(c => c.Covered),
            flagCounts, bookScope, coords, jsonPath, stampedTo);
    }

    // ── Index section builder (idempotent; grouped by chapter) ──
    private static string BuildIndexSection(
        string nodeCode, List<BeatCoordinate> coords, BookScopeContext scope,
        Dictionary<string, int> flagCounts, string jsonPath)
    {
        var sb = new StringBuilder();
        sb.AppendLine(IndexHeading + $" {{#SS-{nodeCode}-COORD}}");
        sb.AppendLine();
        sb.AppendLine("> GENERATED by `prose --coordinate --slug <slug>`. Do not hand-edit — regenerated");
        sb.AppendLine("> on demand. This is the three-coordinate map: each beat's **meaning** (bible),");
        sb.AppendLine("> **construction** (blueprint), and **prose** (DB) correlated by beat ID + number.");
        sb.AppendLine($"> Full record: `{RelPath(jsonPath)}`.");
        sb.AppendLine();
        sb.AppendLine($"**Coverage:** {coords.Count(c => c.Covered)}/{coords.Count} beats fully covered. "
            + $"Book-wide construction: temporal={scope.TemporalScheme}, resolution={scope.ResolutionMode}, "
            + $"moral={scope.MoralPolarity}, ending={scope.EndingStyle}, granularity={scope.Granularity}.");
        sb.AppendLine();
        if (flagCounts.Count > 0)
        {
            sb.AppendLine("**Gap counts:** " + string.Join(" · ",
                flagCounts.OrderByDescending(kv => kv.Value).Select(kv => $"{kv.Key}={kv.Value}")));
            sb.AppendLine();
        }

        foreach (var chapter in coords.GroupBy(c => c.Chapter))
        {
            sb.AppendLine($"### {chapter.Key}");
            sb.AppendLine();
            sb.AppendLine("| # | Beat ID | Role | Meaning | Constr (esc/event) | Prose | Flags |");
            sb.AppendLine("|---|---------|------|---------|--------------------|-------|-------|");
            foreach (var c in chapter)
            {
                var meaning = c.Meaning == null ? "—" : Clip(c.Meaning, 56);
                var constr = $"{c.EscalationTarget ?? "?"}/{c.EventType ?? "?"}"
                    + (c.BeatTags.Count > 0 ? " +" + string.Join(",", c.BeatTags) : "");
                var prose = c.ProseLength == 0 ? "0" : $"{c.ProseLength}c";
                var flags = c.Flags.Count == 0 ? "✓" : string.Join(",", c.Flags);
                sb.AppendLine($"| {c.Number} | `{c.BeatId}` | {c.StructureRole ?? "—"} | {meaning} | {constr} | {prose} | {flags} |");
            }
            sb.AppendLine();
        }
        return sb.ToString();
    }

    /// <summary>Replace an existing "## Beat Coordination Index" section (heading to next
    /// top-level "## " or EOF) with the new one, or append if absent.</summary>
    private static async Task ReplaceSectionAsync(string file, string section, CancellationToken ct)
    {
        var text = await File.ReadAllTextAsync(file, ct);
        var lines = text.Replace("\r\n", "\n").Split('\n');
        int start = -1, end = lines.Length;
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].StartsWith(IndexHeading))
            {
                start = i;
                for (int j = i + 1; j < lines.Length; j++)
                {
                    if (lines[j].StartsWith("## ")) { end = j; break; }
                }
                break;
            }
        }

        string body;
        if (start >= 0)
        {
            var before = string.Join("\n", lines[..start]).TrimEnd();
            var after = end < lines.Length ? string.Join("\n", lines[end..]).TrimStart('\n') : "";
            body = before + "\n\n" + section.TrimEnd() + "\n\n" + after;
        }
        else
        {
            body = text.TrimEnd() + "\n\n" + section.TrimEnd() + "\n";
        }
        await GeneratedFileWriter.WriteReadOnlyAsync(file, body.TrimEnd() + "\n", ct);
    }

    private static int[] ParseIntArray(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Array.Empty<int>();
        try { return JsonSerializer.Deserialize<int[]>(json) ?? Array.Empty<int>(); }
        catch { return Array.Empty<int>(); }
    }

    private static List<EventPaletteEntry> ParseEventPalette(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new();
        try { return JsonSerializer.Deserialize<List<EventPaletteEntry>>(json) ?? new(); }
        catch { return new(); }
    }

    private static string Clip(string s, int n)
    {
        s = s.Replace("\n", " ").Replace("|", "\\|").Trim();
        return s.Length <= n ? s : s[..(n - 1)] + "…";
    }

    private static string RelPath(string p)
    {
        var idx = p.Replace('\\', '/').IndexOf("/reports/", StringComparison.OrdinalIgnoreCase);
        return idx >= 0 ? p.Replace('\\', '/')[(idx + 1)..] : Path.GetFileName(p);
    }

    private class EventPaletteEntry
    {
        public int BeatIndex { get; set; }
        public string? EventType { get; set; }
        public string? RevelationMode { get; set; }
    }
}

// ── DTOs ──

public class BeatCoordinate
{
    public Guid BeatId { get; set; }
    public int Number { get; set; }
    public string Chapter { get; set; } = "";
    public int ChapterIndex { get; set; }
    public string? StructureRole { get; set; }
    public int Act { get; set; }

    /// <summary>MEANING coordinate — what the beat means (Beat.Description).</summary>
    public string? Meaning { get; set; }
    public string? Subtext { get; set; }

    /// <summary>CONSTRUCTION coordinate — how the beat is built (blueprint chapter slice).</summary>
    public string? EscalationTarget { get; set; }
    public string? EventType { get; set; }
    public string? RevelationMode { get; set; }
    public List<string> BeatTags { get; set; } = new();

    /// <summary>PROSE coordinate — what accomplishes it (Beat.Text length; text lives in DB).</summary>
    public int ProseLength { get; set; }
    public double? Score { get; set; }

    public List<string> Flags { get; set; } = new();
    public bool Covered { get; set; }
}

public class BookScopeContext
{
    public string? TemporalScheme { get; set; }
    public string? ResolutionMode { get; set; }
    public string? MoralPolarity { get; set; }
    public string? EndingStyle { get; set; }
    public bool HasSubplot { get; set; }
    public string Granularity { get; set; } = "beat";
    public bool HasBlueprint { get; set; }

    /// <summary>Number of beat-slots (beat-granular) or chapters (chapter-granular) the
    /// blueprint's construction arrays actually cover. Compare against the book's live
    /// beat count to detect a blueprint frozen before later beat-splits grew the book.</summary>
    public int ConstructionCapacity { get; set; }
}

public record CoordinationReport(
    string NodeCode,
    int TotalBeats,
    int Covered,
    Dictionary<string, int> FlagCounts,
    BookScopeContext BookScope,
    List<BeatCoordinate> Beats,
    string JsonPath,
    string? StampedTo);
