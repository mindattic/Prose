using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Exports a <see cref="Data.Entities.Strand"/> to readable markdown — the text
/// an LLM reader (or a human) consumes. Renders the ordered beats under a title
/// heading, with chapter dividers where a beat is a chapter start, and computes
/// a stable content fingerprint of the prose so a review can be tied to the
/// exact version it read. Also writes a <c>.md</c> file under
/// <see cref="IPathProvider.ExportDir"/> (mirrors <c>BookExportService</c>).
/// </summary>
public class StrandMarkdownExporter
{
    private readonly IDbContextFactory<StreetSamuraiDbContext> dbFactory;
    private readonly StrandWorkbenchService workbench;
    private readonly IPathProvider paths;

    public StrandMarkdownExporter(
        IDbContextFactory<StreetSamuraiDbContext> dbFactory,
        StrandWorkbenchService workbench,
        IPathProvider paths)
    {
        this.dbFactory = dbFactory;
        this.workbench = workbench;
        this.paths = paths;
    }

    /// <summary>The exported strand: the markdown a reader reads, the SHA-256
    /// fingerprint of the ordered prose (which VERSION this is), the beat count,
    /// the strand's title, and the written .md path.</summary>
    public record StrandExport(string Markdown, string ContentHash, int BeatCount, string Title, string Path);

    /// <param name="numberBeats">When true, prefixes each rendered beat with a
    /// 1-based <c>[Beat N]</c> marker (reading order) so a reviewer can micro-score
    /// each beat by number. The number matches the strand's positional beat index
    /// (ROW_NUMBER over SortKey), so scores join cleanly to the beats.</param>
    public async Task<StrandExport> ExportAsync(Guid strandId, bool numberBeats = false, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var strand = await db.Strands.AsNoTracking().FirstOrDefaultAsync(s => s.Id == strandId, ct)
            ?? throw new InvalidOperationException($"Strand {strandId} not found.");

        var ordered = await workbench.GetOrderedBeatsAsync(strandId, ct);

        var md = new StringBuilder();
        md.AppendLine($"# {strand.Title}");
        md.AppendLine();
        if (!string.IsNullOrWhiteSpace(strand.Synopsis))
        {
            md.AppendLine($"_{strand.Synopsis.Trim()}_");
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
            if (beat.IsChapterStart && !string.IsNullOrWhiteSpace(beat.BeatTitle))
            {
                md.AppendLine($"## {beat.BeatTitle.Trim()}");
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
        var path = Path.Combine(paths.ExportDir, $"{strand.Slug}.{strand.Id.ToString("N")[..8]}{suffix}.md");
        await File.WriteAllTextAsync(path, markdown, ct);

        return new StrandExport(markdown, contentHash, n, strand.Title, path);
    }

    private static string Sha256Hex(string text)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(text ?? ""));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
