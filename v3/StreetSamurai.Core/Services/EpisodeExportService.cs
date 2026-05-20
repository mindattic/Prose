using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Files every artifact produced by an episode generation/narration run. Three
/// outputs land in <c>engine/episodes/{id}/</c>:
/// <list type="bullet">
///   <item><c>script.md</c> — Markdown script with frontmatter (seed, voice, dates).</item>
///   <item><c>script.pdf</c> — QuestPDF render of the same content.</item>
///   <item><c>episode.wav</c> — single combined WAV produced by concatenating
///     the per-beat PCM data and rewrapping with a fresh header.</item>
/// </list>
/// The DB's <c>Episodes.ScriptMarkdownPath</c> / <c>ScriptPdfPath</c> /
/// <c>CombinedAudioPath</c> columns hold the relative-to-data-root paths.
/// </summary>
public class EpisodeExportService
{
    private readonly IDbContextFactory<StreetSamuraiDbContext> dbFactory;
    private readonly IPathProvider paths;
    private readonly EpisodeAudioService audio;
    private readonly ILogger<EpisodeExportService> log;

    public EpisodeExportService(
        IDbContextFactory<StreetSamuraiDbContext> dbFactory,
        IPathProvider paths,
        EpisodeAudioService audio,
        ILogger<EpisodeExportService> log)
    {
        this.dbFactory = dbFactory;
        this.paths = paths;
        this.audio = audio;
        this.log = log;
    }

    /// <summary>Write the Markdown script + PDF rendition. Called when generation
    /// finishes, before narration begins. Either output failing is logged but
    /// does not throw — the audio pipeline must keep moving regardless.</summary>
    public async Task ExportScriptAsync(Guid episodeId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var episode = await db.Episodes
            .Include(e => e.Beats)
            .FirstOrDefaultAsync(e => e.Id == episodeId, ct)
            ?? throw new InvalidOperationException($"Episode {episodeId} not found.");

        var slug = !string.IsNullOrWhiteSpace(episode.Slug) ? episode.Slug : episode.Id.ToString();
        var dir = audio.GetEpisodeRoot(slug);
        Directory.CreateDirectory(dir);

        try
        {
            var mdRelative = await WriteMarkdownAsync(episode, slug, dir, ct);
            episode.ScriptMarkdownPath = mdRelative;
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Episode #{Ep} markdown export failed", episodeId);
        }

        try
        {
            var pdfRelative = WritePdf(episode, slug, dir);
            episode.ScriptPdfPath = pdfRelative;
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Episode #{Ep} pdf export failed", episodeId);
        }

        await db.SaveChangesAsync(ct);
        log.LogInformation("Episode #{Ep} script artifacts filed at {Dir}", episodeId, dir);
    }

    /// <summary>Concatenate every per-beat WAV's PCM into one combined episode.wav.
    /// All inputs share the same format (16-bit mono 44.1 kHz) so we just strip
    /// the 44-byte headers, concat the raw PCM, and rewrap.</summary>
    public async Task ExportCombinedAudioAsync(Guid episodeId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var episode = await db.Episodes
            .Include(e => e.Beats)
            .FirstOrDefaultAsync(e => e.Id == episodeId, ct)
            ?? throw new InvalidOperationException($"Episode {episodeId} not found.");

        var ordered = episode.Beats
            .Where(b => !string.IsNullOrEmpty(b.AudioPath))
            .OrderBy(b => b.SortKey)
            .ToList();
        if (ordered.Count == 0)
        {
            log.LogWarning("Episode #{Ep} has no narrated beats to combine", episodeId);
            return;
        }

        // Pick the dominant format. If every beat is the same kind we get a
        // clean concatenation. Mixed-format episodes are rare (would only happen
        // if the tier flipped mid-run) and we skip those.
        bool allWav = ordered.All(b => b.AudioPath!.EndsWith(".wav", StringComparison.OrdinalIgnoreCase));
        bool allMp3 = ordered.All(b => b.AudioPath!.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase));
        if (!allWav && !allMp3)
        {
            log.LogInformation("Episode #{Ep} has mixed-format beats; skipping combined audio. Per-beat files remain available.", episodeId);
            return;
        }

        var slug = !string.IsNullOrWhiteSpace(episode.Slug) ? episode.Slug : episode.Id.ToString();
        var dir = audio.GetEpisodeRoot(slug);
        Directory.CreateDirectory(dir);

        string combinedPath;
        long combinedLen;
        if (allWav)
        {
            // WAV: strip the 44-byte header from each, concat raw PCM, rewrap.
            var pcmParts = new List<byte[]>(ordered.Count);
            foreach (var beat in ordered)
            {
                ct.ThrowIfCancellationRequested();
                var fullPath = audio.ResolveAudioFile(beat.AudioPath!);
                if (!File.Exists(fullPath)) continue;
                var bytes = await File.ReadAllBytesAsync(fullPath, ct);
                if (bytes.Length <= 44) continue;
                pcmParts.Add(bytes[44..]);
            }
            var totalLen = pcmParts.Sum(p => p.Length);
            var allPcm = new byte[totalLen];
            var offset = 0;
            foreach (var part in pcmParts)
            {
                Buffer.BlockCopy(part, 0, allPcm, offset, part.Length);
                offset += part.Length;
            }
            var combined = EpisodeAudioService.WrapPcmAsWav(allPcm, sampleRate: 44100, channels: 1, bitsPerSample: 16);
            combinedPath = Path.Combine(dir, "episode.wav");
            await File.WriteAllBytesAsync(combinedPath, combined, ct);
            combinedLen = combined.Length;
            episode.CombinedAudioPath = $"{slug}/episode.wav";
        }
        else // allMp3
        {
            // MP3 frame-level concat: ElevenLabs returns CBR 44.1 kHz / 128 kbps
            // for every beat, so concatenating the byte streams produces a
            // playable composite. Decoders walk frame headers; the only
            // observable artifact is a slightly imprecise duration estimate in
            // some players, which is acceptable for bedtime listening.
            combinedPath = Path.Combine(dir, "episode.mp3");
            await using var output = File.Create(combinedPath);
            foreach (var beat in ordered)
            {
                ct.ThrowIfCancellationRequested();
                var fullPath = audio.ResolveAudioFile(beat.AudioPath!);
                if (!File.Exists(fullPath)) continue;
                var bytes = await File.ReadAllBytesAsync(fullPath, ct);
                if (bytes.Length == 0) continue;
                await output.WriteAsync(bytes, ct);
            }
            combinedLen = output.Length;
            episode.CombinedAudioPath = $"{slug}/episode.mp3";
        }

        await db.SaveChangesAsync(ct);
        log.LogInformation("Episode #{Ep} combined audio written: {Path} ({Bytes:N0} bytes, {Beats} beats, format={Fmt})",
            episodeId, combinedPath, combinedLen, ordered.Count, allWav ? "wav" : "mp3");
    }

    // ── Markdown ────────────────────────────────────────────────────────────

    private static async Task<string> WriteMarkdownAsync(Episode episode, string slug, string dir, CancellationToken ct)
    {
        var path = Path.Combine(dir, "script.md");

        var sb = new StringBuilder();
        sb.AppendLine("---");
        sb.AppendLine($"title: {episode.Title}");
        sb.AppendLine($"episode_id: {episode.Id}");
        sb.AppendLine($"slug: {slug}");
        sb.AppendLine($"seed: \"{episode.Seed.Replace("\"", "\\\"")}\"");
        sb.AppendLine($"voice_id: {episode.VoiceId ?? "(default)"}");
        sb.AppendLine($"started_at: {episode.StartedAt:o}");
        if (episode.GenerationCompletedAt.HasValue)
            sb.AppendLine($"generation_completed_at: {episode.GenerationCompletedAt.Value:o}");
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine($"# {episode.Title}");
        sb.AppendLine();
        sb.AppendLine($"> {episode.Seed}");
        sb.AppendLine();

        foreach (var beat in episode.Beats.OrderBy(b => b.SortKey))
        {
            sb.AppendLine(beat.Text);
            sb.AppendLine();
        }

        await File.WriteAllTextAsync(path, sb.ToString(), ct);
        return $"{slug}/script.md";
    }

    // ── PDF (QuestPDF) ──────────────────────────────────────────────────────

    private static string WritePdf(Episode episode, string slug, string dir)
    {
        var path = Path.Combine(dir, "script.pdf");

        QuestPDF.Fluent.Document.Create(container =>
        {
            // Title page
            container.Page(p =>
            {
                p.Size(PageSizes.Letter);
                p.Margin(72);
                p.PageColor(Colors.White);
                p.DefaultTextStyle(t => t.FontFamily("Georgia").FontSize(12).FontColor(Colors.Black));
                p.Content().AlignCenter().AlignMiddle().Column(col =>
                {
                    col.Item().Text(episode.Title).FontSize(28).Bold();
                    col.Item().PaddingTop(24).Text(episode.Seed)
                        .FontSize(12).Italic().FontColor(Colors.Grey.Darken1);
                    col.Item().PaddingTop(56).Text($"Episode #{episode.Id} · {episode.StartedAt:yyyy-MM-dd}")
                        .FontSize(10).FontColor(Colors.Grey.Medium);
                });
            });

            // Prose
            container.Page(p =>
            {
                p.Size(PageSizes.Letter);
                p.Margin(72);
                p.PageColor(Colors.White);
                p.DefaultTextStyle(t => t.FontFamily("Georgia").FontSize(11.5f).LineHeight(1.45f).FontColor(Colors.Black));
                p.Header().PaddingBottom(20).Column(h =>
                {
                    h.Item().Text(episode.Title).FontSize(14).Bold();
                });
                p.Content().Column(col =>
                {
                    foreach (var beat in episode.Beats.OrderBy(b => b.SortKey))
                    {
                        col.Item().PaddingBottom(8).Text(beat.Text).Justify();
                    }
                });
                p.Footer().AlignCenter().Text(t =>
                {
                    t.Span($"— Page ").FontSize(9).FontColor(Colors.Grey.Medium);
                    t.CurrentPageNumber().FontSize(9).FontColor(Colors.Grey.Medium);
                    t.Span(" —").FontSize(9).FontColor(Colors.Grey.Medium);
                });
            });
        }).GeneratePdf(path);

        return $"{slug}/script.pdf";
    }
}
