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
/// Assembles the unified Book Context Document — hand-authored NodeBible content
/// plus generated Structural Blueprint and Beat Spine sections — and writes it to
/// both <c>Nodes.NodeBible</c> (DB) and <c>docs/nodes/{CODE}.md</c> (disk mirror).
///
/// The disk file is a generated read-only view; never hand-edit it.
/// Edit hand-authored content via <c>set_book_bible</c> MCP, then re-run.
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
    private readonly ILogger<NodeDocService> log;

    public NodeDocService(
        IDbContextFactory<ProseDbContext> dbFactory,
        StructuralBlueprintService blueprintService,
        IPathProvider paths,
        ILogger<NodeDocService> log)
    {
        this.dbFactory = dbFactory;
        this.blueprintService = blueprintService;
        this.paths = paths;
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
        var node = await db.Nodes.FirstOrDefaultAsync(n => n.Id == nodeId, ct)
            ?? throw new InvalidOperationException($"Node {nodeId} not found.");

        var nodeCode = (node.NodeCode ?? node.Slug).ToUpperInvariant();
        var now = DateTime.UtcNow;

        // Preserve hand-authored content; strip any prior generated sections AND any prior
        // frontmatter block (the frontmatter is recomputed fresh every run from UniverseId,
        // never hand-edited, so it must never be carried forward as "hand-authored" — otherwise
        // it accumulates a new block on top of the old one every regenerate).
        var handAuthored = StripFrontmatter(ExtractHandAuthored(node.NodeBible));
        if (string.IsNullOrWhiteSpace(handAuthored))
            handAuthored = $"# Book Context: {node.Title} ({nodeCode})\n\n" +
                           $"_No hand-authored content yet. Use `set_book_bible` MCP to add arc, characters, voice register, and narrative locks._\n";

        var universeSlug = await db.Universes.AsNoTracking()
            .Where(u => u.Id == node.UniverseId).Select(u => u.Slug).FirstOrDefaultAsync(ct);
        var frontmatter = universeSlug != null && UniverseRelatedDoc.TryGetValue(universeSlug, out var relatedDoc)
            ? $"---\nrelated: {relatedDoc}\n---\n\n"
            : "";

        // Load generated data
        var blueprint = await blueprintService.GetAsync(nodeId, ct);
        var (beatSpine, beatCount) = await BuildBeatSpineAsync(db, nodeId, ct);

        // Build generated portion (blueprint + beat spine) separately so we can checksum it.
        // Checksum detects hand-edits to the generated sections (checked by codex doctor).
        var genPart = new StringBuilder();
        if (blueprint != null)
        {
            genPart.AppendLine();
            genPart.AppendLine(BuildBlueprintSection(blueprint, now));
        }
        if (!string.IsNullOrWhiteSpace(beatSpine))
        {
            genPart.AppendLine();
            genPart.AppendLine(BuildBeatSpineSection(beatSpine, now));
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

        // Save to DB
        node.NodeBible = docText;
        node.NodeBibleGeneratedAt = now;
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

    // ── Beat spine section ────────────────────────────────────────────────────

    private static string BuildBeatSpineSection(string beatSpine, DateTime now) =>
        $"## Beat Spine\n<!-- generated {now:O} from Beats table — edit via MCP beat tools -->\n\n{beatSpine.TrimEnd()}";

    /// <summary>
    /// Opens/closes preview label for the compressed (&gt;60 beat) Beat Spine view. Post-Swain-rebeat
    /// books never populate Beat.Title (only Description, via the MeaningBackfillService), so a bare
    /// "Title ?? '—'" fallback rendered every compressed spine as a content-free "— — opens" stub.
    /// Falls back to a clipped Description, which IS populated for those books.
    /// </summary>
    internal static string BeatLabel(string? title, string? desc)
    {
        if (!string.IsNullOrWhiteSpace(title)) return title;
        if (!string.IsNullOrWhiteSpace(desc)) return TruncateAtWordBoundary(desc, 80);
        return "—";
    }

    /// <summary>
    /// Truncates to at most maxLength characters, backing up to the last space so a word is
    /// never cut in half (e.g. "...will for…" instead of "...will force impossible...").
    /// If no space exists before maxLength, falls back to a hard cut rather than returning an
    /// oversized string.
    /// </summary>
    internal static string TruncateAtWordBoundary(string text, int maxLength)
    {
        if (text.Length <= maxLength) return text;
        var cut = text.LastIndexOf(' ', maxLength - 1);
        var slice = cut > 0 ? text[..cut] : text[..maxLength];
        return slice.TrimEnd() + "…";
    }

    private static async Task<(string SpineText, int BeatCount)> BuildBeatSpineAsync(
        ProseDbContext db, Guid nodeId, CancellationToken ct)
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

        var sb = new StringBuilder();
        int totalBeats = 0;

        if (chapters.Count > 0)
        {
            // Two-pass: count first, then render at appropriate detail level
            var chapterData = new List<(string Title, List<(int Pos, string? Title, string? Desc)> Beats)>();
            int pos = 1;

            foreach (var ch in chapters)
            {
                var beats = await db.BeatNodes
                    .Where(bn => bn.NodeId == ch.Id && bn.IsEnabled)
                    .OrderBy(bn => bn.SortKey)
                    .Join(db.Beats, bn => bn.BeatId, b => b.Id,
                          (bn, b) => new { b.Title, b.Description })
                    .ToListAsync(ct);

                var list = beats.Select(b => (pos++, b.Title, b.Description)).ToList();
                chapterData.Add((ch.Title ?? "Chapter", list));
                totalBeats += list.Count;
            }

            bool full = totalBeats <= 60;

            foreach (var (chTitle, beats) in chapterData)
            {
                sb.AppendLine($"### {chTitle}");
                if (full)
                {
                    foreach (var (p, t, d) in beats)
                    {
                        var desc = string.IsNullOrWhiteSpace(d) ? "" : $" — {d}";
                        sb.AppendLine($"- B{p:D2} · {t ?? "—"}{desc}");
                    }
                }
                else
                {
                    sb.AppendLine($"_({beats.Count} beats)_");
                    if (beats.Count >= 2)
                    {
                        var first = beats.First();
                        var last  = beats.Last();
                        sb.AppendLine($"- B{first.Pos:D2} · {BeatLabel(first.Title, first.Desc)} — opens");
                        sb.AppendLine($"- B{last.Pos:D2}  · {BeatLabel(last.Title, last.Desc)} — closes");
                    }
                }
                sb.AppendLine();
            }
        }
        else
        {
            // Direct beats on book node — use IsChapterStart to detect chapter boundaries
            var beats = await db.BeatNodes
                .Where(bn => bn.NodeId == nodeId && bn.IsEnabled)
                .OrderBy(bn => bn.SortKey)
                .Join(db.Beats, bn => bn.BeatId, b => b.Id,
                      (bn, b) => new { b.Title, b.Description, b.IsChapterStart })
                .ToListAsync(ct);

            totalBeats = beats.Count;
            bool full = totalBeats <= 60;
            int pos = 1;

            foreach (var b in beats)
            {
                if (b.IsChapterStart)
                    sb.AppendLine($"\n### {b.Title ?? $"Chapter"}");

                if (full)
                {
                    var desc = string.IsNullOrWhiteSpace(b.Description) ? "" : $" — {b.Description}";
                    sb.AppendLine($"- B{pos:D2} · {b.Title ?? "—"}{desc}");
                }
                pos++;
            }

            if (!full)
                sb.AppendLine($"\n_({totalBeats} beats total — use `get_book_outline` for chapter summaries)_");
        }

        return (sb.ToString(), totalBeats);
    }
}
