using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Exports a <see cref="Data.Entities.Node"/> to readable markdown — the text
/// an LLM reader (or a human) consumes. Renders the ordered beats under a title
/// heading, with chapter dividers where a beat is a chapter start, and computes
/// a stable content fingerprint of the prose so a review can be tied to the
/// exact version it read. Also writes a <c>.md</c> file under
/// <see cref="IPathProvider.ExportDir"/> (mirrors <c>BookExportService</c>).
/// </summary>
public class NodeMarkdownExporter
{
    private readonly IDbContextFactory<StreetSamuraiDbContext> dbFactory;
    private readonly NodeWorkbenchService workbench;
    private readonly IPathProvider paths;

    public NodeMarkdownExporter(
        IDbContextFactory<StreetSamuraiDbContext> dbFactory,
        NodeWorkbenchService workbench,
        IPathProvider paths)
    {
        this.dbFactory = dbFactory;
        this.workbench = workbench;
        this.paths = paths;
    }

    /// <summary>The exported node: the markdown a reader reads, the SHA-256
    /// fingerprint of the ordered prose (which VERSION this is), the beat count,
    /// the node's title, and the written .md path.</summary>
    public record NodeExport(string Markdown, string ContentHash, int BeatCount, string Title, string Path);

    /// <param name="numberBeats">When true, prefixes each rendered beat with a
    /// 1-based <c>[Beat N]</c> marker (reading order) so a reviewer can micro-score
    /// each beat by number. The number matches the node's positional beat index
    /// (ROW_NUMBER over SortKey), so scores join cleanly to the beats.</param>
    public async Task<NodeExport> ExportAsync(Guid nodeId, bool numberBeats = false, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var node = await db.Nodes.AsNoTracking().FirstOrDefaultAsync(s => s.Id == nodeId, ct)
            ?? throw new InvalidOperationException($"Node {nodeId} not found.");

        var ordered = await workbench.GetOrderedBeatsAsync(nodeId, ct);

        var md = new StringBuilder();
        md.AppendLine($"# {node.Title}");
        md.AppendLine();
        if (!string.IsNullOrWhiteSpace(node.Description))
        {
            md.AppendLine($"_{node.Description.Trim()}_");
            md.AppendLine();
        }

        // The fingerprint is computed over the raw ordered beat text only (no
        // headings/synopsis/markers) so it stays stable regardless of export
        // framing and matches "the prose the reader actually read".
        var proseForHash = new StringBuilder();
        var n = 0;

        foreach (var ob in ordered)
        {
            var beat = ob.Beat;
            if (beat.IsChapterStart && !string.IsNullOrWhiteSpace(beat.Title))
            {
                md.AppendLine($"## {beat.Title.Trim()}");
                md.AppendLine();
            }
            var text = (beat.Text ?? "").Trim();
            if (text.Length == 0) continue;
            n++;
            if (numberBeats) md.AppendLine($"[Beat {n}]");
            md.AppendLine(text);
            md.AppendLine();
            proseForHash.Append(text).Append('\n');
        }

        var contentHash = Sha256Hex(proseForHash.ToString().Trim());
        var markdown = md.ToString();

        Directory.CreateDirectory(paths.ExportDir);
        var suffix = numberBeats ? ".numbered" : "";
        var path = Path.Combine(paths.ExportDir, $"{node.Slug}.{node.Id.ToString("N")[..8]}{suffix}.md");
        await File.WriteAllTextAsync(path, markdown, ct);

        return new NodeExport(markdown, contentHash, n, node.Title, path);
    }

    /// <summary>One contiguous review segment ("act"): a run of beats small enough
    /// to review in a single reliable pass, with GLOBAL beat numbers preserved so
    /// per-beat scores still join to the node's positional index.</summary>
    public record NodeSegment(int Index, int Total, int FirstBeat, int LastBeat, int Chars, string Markdown);

    /// <summary>Split the node into review segments for large books that can't be
    /// reviewed reliably in one pass. Breaks at chapter boundaries once the running
    /// segment reaches <paramref name="targetChars"/>; for nodes with no chapter
    /// starts (flat beat lists) it falls back to a hard size cap so each segment
    /// stays within a single reliable ballot. Beat numbering is GLOBAL (1..N over
    /// the whole node) so segment ballots' per-beat scores join cleanly.</summary>
    public async Task<(string Title, string ContentHash, int BeatCount, IReadOnlyList<NodeSegment> Segments)>
        ExportSegmentsAsync(Guid nodeId, int targetChars = 90000, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var node = await db.Nodes.AsNoTracking().FirstOrDefaultAsync(s => s.Id == nodeId, ct)
            ?? throw new InvalidOperationException($"Node {nodeId} not found.");
        var ordered = await workbench.GetOrderedBeatsAsync(nodeId, ct);

        // Render once with global numbering; keep chapter-start metadata for splitting.
        var rendered = new List<(int N, string Text, bool ChapterStart, string? ChapterTitle)>();
        var proseForHash = new StringBuilder();
        var n = 0;
        foreach (var ob in ordered)
        {
            var text = (ob.Beat.Text ?? "").Trim();
            if (text.Length == 0) continue;
            n++;
            var chStart = ob.Beat.IsChapterStart;
            rendered.Add((n, text, chStart, chStart ? ob.Beat.Title?.Trim() : null));
            proseForHash.Append(text).Append('\n');
        }
        var contentHash = Sha256Hex(proseForHash.ToString().Trim());
        var beatCount = n;

        // Group beats into segments. Prefer breaking at a chapter start once the
        // running segment has reached the target; a hard cap (1.5x) keeps no-chapter
        // nodes bounded too.
        var groups = new List<List<(int N, string Text, bool ChapterStart, string? ChapterTitle)>>();
        var cur = new List<(int N, string Text, bool ChapterStart, string? ChapterTitle)>();
        var curChars = 0;
        var hardCap = (int)(targetChars * 1.5);
        foreach (var r in rendered)
        {
            var breakHere = cur.Count > 0 &&
                ((r.ChapterStart && curChars >= targetChars) || curChars >= hardCap);
            if (breakHere) { groups.Add(cur); cur = new(); curChars = 0; }
            cur.Add(r);
            curChars += r.Text.Length;
        }
        if (cur.Count > 0) groups.Add(cur);

        var total = groups.Count;
        var segments = new List<NodeSegment>(total);
        for (var gi = 0; gi < total; gi++)
        {
            var g = groups[gi];
            var md = new StringBuilder();
            md.AppendLine($"# {node.Title} — Part {gi + 1} of {total} (Beats {g[0].N}–{g[^1].N})");
            md.AppendLine();
            var chars = 0;
            foreach (var r in g)
            {
                if (r.ChapterStart && !string.IsNullOrWhiteSpace(r.ChapterTitle))
                {
                    md.AppendLine($"## {r.ChapterTitle}");
                    md.AppendLine();
                }
                md.AppendLine($"[Beat {r.N}]");
                md.AppendLine(r.Text);
                md.AppendLine();
                chars += r.Text.Length;
            }
            segments.Add(new NodeSegment(gi + 1, total, g[0].N, g[^1].N, chars, md.ToString()));
        }

        return (node.Title, contentHash, beatCount, segments);
    }

    private static string Sha256Hex(string text)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(text ?? ""));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
