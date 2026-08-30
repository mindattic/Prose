using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Interfaces;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Prose.Core.Services;

public record NodeDocResult(string NodeCode, int BeatCount, bool HasBlueprint, string Path, DateTime GeneratedAt);

/// <summary>
/// Assembles the unified Book Context Document — hand-authored NodeOutline content
/// plus generated Structural Blueprint and Event Sequence sections — and writes it to
/// both <c>Nodes.NodeOutline</c> (DB) and <c>docs/nodes/{CODE}.md</c> (disk mirror).
///
/// The disk file is a generated read-only view; never hand-edit it.
/// Edit hand-authored content via <c>set_book_outline</c> MCP, then re-run.
/// </summary>
public class NodeDocService
{
    internal const string GeneratedMarker =
        "<!-- ==== GENERATED SECTIONS — do not hand-edit below this line ==== -->";

    // Tolerant of the marker's em-dash getting mangled by an encoding mismatch upstream
    // (seen in the wild — a stray non-UTF8 write turned "—" into a mojibake byte). Matching
    // loosely here means a corrupted marker still gets recognized and stripped as generated
    // content on the next regenerate, instead of permanently freezing into "hand-authored".
    private static readonly Regex GeneratedMarkerPattern = new(
        @"<!-- ==== GENERATED SECTIONS .{0,4} do not hand-edit below this line ==== -->",
        RegexOptions.Compiled);

    private readonly IDbContextFactory<ProseDbContext> dbFactory;
    private readonly StructuralBlueprintService blueprintService;
    private readonly IPathProvider paths;
    private readonly FindingsService findings;
    private readonly ILogger<NodeDocService> log;

    public NodeDocService(
        IDbContextFactory<ProseDbContext> dbFactory,
        StructuralBlueprintService blueprintService,
        IPathProvider paths,
        FindingsService findings,
        ILogger<NodeDocService> log)
    {
        this.dbFactory = dbFactory;
        this.blueprintService = blueprintService;
        this.paths = paths;
        this.findings = findings;
        this.log = log;
    }

    // Universe slug → the one canon doc every book bible in that universe should cascade to via
    // `related:` (DCM relational graph, step 5 of DocContextService.PrepareContextAsync). Only
    // universes with an established world-facts doc get one; others fall through with no link.
    private static readonly Dictionary<string, string> UniverseRelatedDoc = new(StringComparer.OrdinalIgnoreCase)
    {
        ["glmz"] = "docs/WORLD.md",
        ["scry"] = "docs/universes/ENTOS.md",
        ["caul"] = "docs/universes/ENTOS.md",
        ["fantasy"] = "docs/universes/ENTOS.md",
    };

    public async Task<NodeDocResult> GenerateAsync(Guid nodeId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        // IgnoreQueryFilters(): explicit nodeId, not an ambient scope (same bug class found and
        // fixed in BookArchiveService.ArchiveAsync, 2026-08-17).
        var node = await db.Nodes.IgnoreQueryFilters().FirstOrDefaultAsync(n => n.Id == nodeId, ct)
            ?? throw new InvalidOperationException($"Node {nodeId} not found.");

        var nodeCode = (node.NodeCode ?? node.Slug).ToUpperInvariant();
        var now = DateTime.UtcNow;

        // Preserve hand-authored content; strip any prior generated sections AND any prior
        // frontmatter block (the frontmatter is recomputed fresh every run from UniverseId,
        // never hand-edited, so it must never be carried forward as "hand-authored" — otherwise
        // it accumulates a new block on top of the old one every regenerate).
        var handAuthored = StripFrontmatter(ExtractHandAuthored(node.NodeOutline));
        if (string.IsNullOrWhiteSpace(handAuthored))
            handAuthored = $"# Book Context: {node.Title} ({nodeCode})\n\n" +
                           $"_No hand-authored content yet. Use `set_book_outline` MCP to add arc, characters, voice register, and narrative locks._\n";

        var universeSlug = await db.Universes.AsNoTracking()
            .Where(u => u.Id == node.UniverseId).Select(u => u.Slug).FirstOrDefaultAsync(ct);
        var frontmatter = universeSlug != null && UniverseRelatedDoc.TryGetValue(universeSlug, out var relatedDoc)
            ? $"---\nrelated: {relatedDoc}\n---\n\n"
            : "";

        // Load generated data
        var blueprint = await blueprintService.GetAsync(nodeId, ct);
        var (eventSequence, beatCount, unresolvedNames) =
            await BuildEventSequenceAsync(db, nodeId, node.UniverseId, ct);

        // Stage-1 "seed before reference" enforced at the outline layer (Phase 4b): a generated
        // Event Sequence line naming something that resolves to no entity record is a finding, not
        // a silent gap — same rule that already gates hand-authored outline sections (see
        // CanonDocumentService.SetNodeOutlineSectionAsync, which files under its own "#section:"
        // scope). Full re-generation is authoritative for the Event Sequence specifically: purge
        // just THIS scope (not the hand-authored sections' own findings) and refile, so a name
        // later seeded/fixed clears its own finding without erasing an unrelated section's.
        var sequenceScope = $"story:{node.Slug}#sequence";
        findings.DeleteBySummaryPrefix(sequenceScope, "[outline-entity] ");
        foreach (var name in unresolvedNames)
            findings.Upsert(
                filePath: sequenceScope,
                chapterId: null,
                category: FindingCategory.EntityDrift,
                severity: FindingSeverity.Low,
                summary: $"[outline-entity] \"{name}\" resolves to no entity record",
                snippet: null,
                suggestedFix: $"Seed \"{name}\" as an entity, or fix the name if it's a typo/renamed reference.");

        // Build generated portion (blueprint + event sequence) separately so we can checksum it.
        // Checksum detects hand-edits to the generated sections (checked by codex doctor).
        var genPart = new StringBuilder();
        if (blueprint != null)
        {
            genPart.AppendLine();
            genPart.AppendLine(BuildBlueprintSection(blueprint, now));
        }
        if (!string.IsNullOrWhiteSpace(eventSequence))
        {
            genPart.AppendLine();
            genPart.AppendLine(BuildEventSequenceSection(eventSequence, now));
        }
        var genText = genPart.ToString();
        // Normalize to LF before hashing so the checksum is stable across platforms.
        var genNorm = genText.Replace("\r\n", "\n").Replace("\r", "\n");
        var checksum = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(genNorm))).ToLower();

        // Assemble final document
        var doc = new StringBuilder();
        doc.Append(frontmatter);
        doc.Append(handAuthored.TrimEnd());
        doc.AppendLine();
        doc.AppendLine();
        doc.AppendLine(GeneratedMarker);
        doc.AppendLine($"<!-- GENERATED-CHECKSUM: {checksum} -->");
        doc.Append(genText);

        var docText = doc.ToString().TrimEnd() + "\n";

        // Save to DB — NodeOutline stays PURE hand-authored content, never the merged blob.
        // Previously this wrote the full docText (frontmatter + hand-authored + blueprint +
        // beat spine) back into Nodes.NodeOutline, so the column named "the bible" stopped
        // meaning only the bible after the first regenerate — any code or person reading
        // NodeOutline directly got a blend under a name that promised one of its three
        // ingredients. The merged view belongs only on the disk mirror (docs/nodes/{CODE}.md)
        // and MarkdownFiles, which is what DocContextService actually injects at generation
        // time anyway. ExtractHandAuthored still strips a legacy marker/blueprint/spine tail
        // from any NodeOutline value saved before this fix, so every book self-heals back to a
        // pure bible the next time its doc is regenerated.
        node.NodeOutline = handAuthored.TrimEnd() + "\n";
        node.NodeOutlineGeneratedAt = now;
        node.UpdatedAt = now;
        await db.SaveChangesAsync(ct);

        // Write disk mirror — atomic (per-process scratch file + rename) so two CLI/MCP
        // processes regenerating the same node's doc concurrently can't corrupt or race it.
        var filePath = Path.Combine(paths.DataRoot, "docs", "nodes", $"{nodeCode}.md");
        await GeneratedFileWriter.WriteReadOnlyAsync(filePath, docText, ct);

        log.LogInformation(
            "[generate-node-doc] {NodeCode} — {BeatCount} beats, blueprint={HasBlueprint}, file={Path}",
            nodeCode, beatCount, blueprint != null, filePath);

        return new NodeDocResult(nodeCode, beatCount, blueprint != null, filePath, now);
    }

    // ── Hand-authored extraction ──────────────────────────────────────────────

    internal static string ExtractHandAuthored(string? nodeBible)
    {
        if (string.IsNullOrWhiteSpace(nodeBible)) return "";
        var match = GeneratedMarkerPattern.Match(nodeBible);
        return match.Success ? nodeBible[..match.Index].TrimEnd() : nodeBible.TrimEnd();
    }

    /// <summary>Strip a leading `---\n...\n---` frontmatter block, if present. The frontmatter
    /// (currently just `related:`) is always recomputed fresh from live DB state — it must never
    /// be preserved as part of "hand-authored" content or it accumulates a block per regenerate.</summary>
    internal static string StripFrontmatter(string content)
    {
        if (!content.StartsWith("---\n")) return content;
        var end = content.IndexOf("\n---", 4, StringComparison.Ordinal);
        if (end < 0) return content;
        var afterClose = content.IndexOf('\n', end + 1);
        return (afterClose < 0 ? "" : content[(afterClose + 1)..]).TrimStart('\n');
    }

    // ── Blueprint section ─────────────────────────────────────────────────────

    private static string BuildBlueprintSection(NodeStructuralBlueprint bp, DateTime now)
    {
        var sb = new StringBuilder();
        sb.AppendLine("## Structural Blueprint");
        sb.AppendLine($"<!-- generated {now:O} from NodeStructuralBlueprints — edit via prose --generate-blueprint -->");
        sb.AppendLine();

        if (bp.HasSubplot && !string.IsNullOrWhiteSpace(bp.SubplotSummary))
            sb.AppendLine($"- **Subplot**: {bp.SubplotSummary}");

        sb.AppendLine($"- **Temporal scheme**: {bp.TemporalScheme}");
        if (!string.IsNullOrWhiteSpace(bp.AnachronyPlan))
            sb.AppendLine($"  - _{bp.AnachronyPlan}_");

        sb.AppendLine($"- **Resolution**: {bp.ResolutionMode}");
        sb.AppendLine($"- **Moral polarity**: {bp.MoralPolarity}");

        var ending = bp.EndingStyle + (bp.NoEpilogue ? ", no epilogue" : "");
        sb.AppendLine($"- **Ending**: {ending}");

        if (!string.IsNullOrWhiteSpace(bp.FormDevice))
            sb.AppendLine($"- **Form device**: {bp.FormDevice}");

        // Escalation curve — summarise as "N-beat arc, peak X/10 at beat Y"
        if (!string.IsNullOrWhiteSpace(bp.EscalationCurveJson))
        {
            try
            {
                var curve = JsonSerializer.Deserialize<int[]>(bp.EscalationCurveJson) ?? [];
                if (curve.Length > 0)
                {
                    var max = curve.Max();
                    var peakIdx = Array.IndexOf(curve, max) + 1;
                    sb.AppendLine($"- **Escalation**: {curve.Length}-beat arc, peak {max}/10 at beat {peakIdx}");
                }
            }
            catch { /* malformed JSON — skip */ }
        }

        // Intertextual anchors
        if (!string.IsNullOrWhiteSpace(bp.IntertextualAnchorsJson))
        {
            try
            {
                var anchors = JsonSerializer.Deserialize<JsonElement[]>(bp.IntertextualAnchorsJson) ?? [];
                if (anchors.Length > 0)
                {
                    sb.AppendLine("- **Intertextual anchors**:");
                    foreach (var a in anchors)
                    {
                        var name = a.TryGetProperty("Name", out var n) ? n.GetString() ?? "" : "";
                        var type = a.TryGetProperty("EntityType", out var t) ? t.GetString() ?? "" : "";
                        var how  = a.TryGetProperty("HowReferenced", out var h) ? h.GetString() ?? "" : "";
                        // BeatIndex is stored 0-based (blueprint contract); render 1-based for readers.
                        var beat = a.TryGetProperty("BeatIndex", out var b) && b.ValueKind == JsonValueKind.Number
                            ? $" (beat {b.GetInt32() + 1})" : "";
                        sb.AppendLine($"  - **{name}** ({type}) — {how}{beat}");
                    }
                }
            }
            catch { /* malformed JSON — skip */ }
        }

        return sb.ToString().TrimEnd();
    }

    // ── Event Sequence section ────────────────────────────────────────────────
    //
    // Phase 4a of the Bible→Outline refactor: the Event Sequence is every beat in the book, in
    // reading order, with its plant/payoff status — the actual outline (Little-Red-Riding-Hood
    // framing: "the core beats in order" — see the plan). Replaces the old opens/closes-only
    // Beat Spine, which compressed any book over 60 beats down to two lines per chapter and hid
    // everything in between; a full event list is the entire point of an outline, so there is no
    // compression gate here regardless of book length.

    private static string BuildEventSequenceSection(string eventSequence, DateTime now) =>
        $"## Event Sequence\n<!-- generated {now:O} from Beats table — edit via MCP beat tools -->\n\n{eventSequence.TrimEnd()}";

    private static async Task<(string SequenceText, int BeatCount, List<string> UnresolvedNames)> BuildEventSequenceAsync(
        ProseDbContext db, Guid nodeId, Guid universeId, CancellationToken ct)
    {
        // Check for ChapterNode children (SS-A43 book-mode: beats live on chapter children).
        // Recurses past any nested Collection (a mid-book chapter split into its own bounded
        // sub-chapters) to the actual leaf chapters — a direct-children-only query here
        // silently reported "0 beats" for a book with a split chapter (found 2026-08-09).
        // IMPORTANT: leafIds is already in correct global reading order (depth-first,
        // SortKey-per-level) — do NOT re-sort the fetched rows by raw SortKey, since SortKey is
        // only comparable among siblings under the SAME parent, not globally across branches
        // (a second bug found the same day as the first, while fixing a sibling service).
        var leafIds = await NodeWorkbenchService.GetLeafDescendantIdsAsync(db, nodeId, ct);
        var isFlatNode = leafIds.Count == 1 && leafIds[0] == nodeId;
        List<(Guid Id, string? Title)> chapters = [];
        if (!isFlatNode)
        {
            var titleById = await db.Nodes.AsNoTracking()
                .Where(n => leafIds.Contains(n.Id))
                .Select(n => new { n.Id, n.Title })
                .ToDictionaryAsync(n => n.Id, n => n.Title, ct);
            chapters = leafIds.Select(id => (id, titleById.GetValueOrDefault(id))).ToList();
        }

        // Flat list of every beat in true reading order, whatever shape the book is in.
        var ordered = new List<(int Pos, string Chapter, Guid BeatId, string? Title, string? Description)>();

        if (chapters.Count > 0)
        {
            int pos = 1;
            foreach (var ch in chapters)
            {
                var beats = await db.BeatNodes
                    .Where(bn => bn.NodeId == ch.Id)
                    .OrderBy(bn => bn.SortKey)
                    .Join(db.Beats, bn => bn.BeatId, b => b.Id,
                          (bn, b) => new { b.Id, b.Title, b.Description })
                    .ToListAsync(ct);
                foreach (var b in beats)
                    ordered.Add((pos++, ch.Title ?? "Chapter", b.Id, b.Title, b.Description));
            }
        }
        else
        {
            // Direct beats on book node — use IsChapterStart to detect chapter boundaries.
            var beats = await db.BeatNodes
                .Where(bn => bn.NodeId == nodeId)
                .OrderBy(bn => bn.SortKey)
                .Join(db.Beats, bn => bn.BeatId, b => b.Id,
                      (bn, b) => new { b.Id, b.Title, b.Description, b.IsChapterStart })
                .ToListAsync(ct);

            var currentChapter = "Chapter";
            int pos = 1;
            foreach (var b in beats)
            {
                if (b.IsChapterStart && !string.IsNullOrWhiteSpace(b.Title)) currentChapter = b.Title;
                ordered.Add((pos++, currentChapter, b.Id, b.Title, b.Description));
            }
        }

        var totalBeats = ordered.Count;
        if (totalBeats == 0) return ("", 0, []);

        // Plant/payoff annotations (Phase 4a): same query shape as PlantPayoffService.AuditAsync
        // — Orphaned (planted, no payoff yet) / Unplanted (paid off, no plant on record) — but
        // keyed per-beat here instead of aggregated, since each event line names its own status.
        var posByBeatId = ordered.ToDictionary(o => o.BeatId, o => o.Pos);
        var searchIds = isFlatNode ? [nodeId] : chapters.Select(c => c.Id).Append(nodeId).ToList();
        var plantPayoffs = await db.PlantPayoffs.AsNoTracking()
            .Where(p => searchIds.Contains(p.NodeId))
            .ToListAsync(ct);
        var annotationByBeatId = new Dictionary<Guid, string>();
        foreach (var p in plantPayoffs)
        {
            if (p.PlantBeatId is Guid plantId && posByBeatId.ContainsKey(plantId))
                annotationByBeatId[plantId] = p.PayoffBeatId is Guid payoffId && posByBeatId.TryGetValue(payoffId, out var m)
                    ? $"[x] pays off at B{m:D2}"
                    : "[ ] ORPHANED";
            if (p.PayoffBeatId is Guid payId && posByBeatId.ContainsKey(payId) && p.PlantBeatId == null)
                annotationByBeatId[payId] = "[ ] UNPLANTED";
        }

        // Entity tags (Phase 4b): built once per node, applied per rendered line at RENDER time —
        // never persisted back into Beat.Title/Description (see the plan's round-trip-safety
        // note). Same 3-call sequence as NodeWorkbenchService.SaveBeatDraftAsync.
        var candidates = await EntityMentionScanner.BuildCandidateIndexAsync(db, universeId, nodeId, ct);
        var unresolvedNames = new List<string>();
        var seenUnresolved = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var sb = new StringBuilder();
        string? lastChapter = null;
        foreach (var (pos, chapter, beatId, title, description) in ordered)
        {
            if (chapter != lastChapter)
            {
                if (lastChapter != null) sb.AppendLine();
                sb.AppendLine($"### {chapter}");
                lastChapter = chapter;
            }

            var body = string.IsNullOrWhiteSpace(description) ? (title ?? "—") : $"{title ?? "—"}: {description}";

            // Skip re-tagging a line that (unexpectedly) already carries tags — Title/Description
            // never persist tags themselves, but this keeps the render idempotent regardless.
            if (!BeatMarkup.ExtractEntityGuids(body).Any())
            {
                var matches = EntityMentionScanner.Scan(body, candidates);
                foreach (var name in EntityMentionScanner.FindUnresolvedProperNouns(body, matches))
                    if (seenUnresolved.Add(name)) unresolvedNames.Add(name);
                body = EntityMentionScanner.ApplyTags(body, matches);
            }

            var annotation = annotationByBeatId.TryGetValue(beatId, out var a) ? $"  {a}" : "";
            sb.AppendLine($"- B{pos:D2} · {body}{annotation}");
        }

        return (sb.ToString(), totalBeats, unresolvedNames);
    }
}
