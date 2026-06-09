using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Materializes a chapter from a book into an <see cref="Episode"/> tagged
/// with its source <c>BookId</c> + <c>ChapterId</c>. From that point on, the
/// chapter recording reuses the entire existing narration pipeline — TTS
/// stitching, MP3 fallback, cancel, per-beat edit, re-record, combined export.
///
/// The "pearl necklace" string: each chapter becomes one Episode whose beats
/// are the chapter's paragraphs. Restringing all of a book's chapter
/// recordings produces the full book audio. Per-beat re-records swap
/// individual pearls without breaking the chain.
/// </summary>
public class ChapterRecordingService
{
    private readonly IDbContextFactory<StreetSamuraiDbContext> dbFactory;
    private readonly IChapterRepository chapters;
    private readonly IBookRepository books;
    private readonly MarkdownService markdown;
    private readonly EpisodeAudioService audio;
    private readonly EpisodeExportService export;
    private readonly ILogger<ChapterRecordingService> log;

    public ChapterRecordingService(
        IDbContextFactory<StreetSamuraiDbContext> dbFactory,
        IChapterRepository chapters,
        IBookRepository books,
        MarkdownService markdown,
        EpisodeAudioService audio,
        EpisodeExportService export,
        ILogger<ChapterRecordingService> log)
    {
        this.dbFactory = dbFactory;
        this.chapters = chapters;
        this.books = books;
        this.markdown = markdown;
        this.audio = audio;
        this.export = export;
        this.log = log;
    }

    /// <summary>Materialize a chapter into a new Episode and kick off narration.
    /// Returns the new Episode id. If a recording already exists for this
    /// chapter, throws — the caller should use <see cref="ReRecordChapterAsync"/>
    /// to replace it.</summary>
    public async Task<Guid> RecordChapterAsync(string chapterId, string? voiceId = null, CancellationToken ct = default)
    {
        var chapter = chapters.LoadChapter(chapterId)
            ?? throw new InvalidOperationException($"Chapter {chapterId} not found.");
        if (string.IsNullOrEmpty(chapter.BookId))
            throw new InvalidOperationException($"Chapter {chapterId} has no BookId — cannot record an unattached chapter.");

        var bookGuid = Guid.Parse(chapter.BookId);

        // Already recorded? Caller should delete + redo or call ReRecordChapterAsync.
        await using (var db = await dbFactory.CreateDbContextAsync(ct))
        {
            var existing = await db.Episodes
                .AsNoTracking()
                .Where(e => e.ChapterId == chapter.Id)
                .Select(e => e.Id)
                .FirstOrDefaultAsync(ct);
            if (existing != Guid.Empty)
                throw new InvalidOperationException(
                    $"Chapter {chapter.Title} already has a recording (episode {existing}). " +
                    "Delete it or call ReRecordChapterAsync to replace.");
        }

        // Build the recording's beat list. Two sources of paragraphs:
        //   1. If chapter.Beats[] is populated (the writer's narrative-beat
        //      list), use it — we already have rich metadata per beat.
        //   2. Otherwise fall back to splitting chapter.Html on blank lines.
        //
        // The first path is the better one because it carries the narrative
        // metadata into the recording (Synopsis, StructureRole, Act,
        // SceneType) which then flows into BeatContext briefs and
        // ElevenLabs tone hints.
        var inputs = new List<RecordingBeatInput>();
        if (chapter.Beats is { Count: > 0 } && chapter.Beats.Any(b => !string.IsNullOrWhiteSpace(b.Text)))
        {
            foreach (var cb in chapter.Beats.OrderBy(b => b.Index))
            {
                if (string.IsNullOrWhiteSpace(cb.Text)) continue;
                inputs.Add(new RecordingBeatInput(
                    Text:           cb.Text.Trim(),
                    SourceBeatGuid: cb.Id,
                    BeatTitle:      string.IsNullOrWhiteSpace(cb.Title) ? null : cb.Title,
                    Synopsis:       string.IsNullOrWhiteSpace(cb.Synopsis) ? null : cb.Synopsis,
                    StructureRole:  string.IsNullOrWhiteSpace(cb.StructureRole) ? null : cb.StructureRole,
                    Act:            cb.Act,
                    SceneType:      string.IsNullOrWhiteSpace(cb.SceneType) ? "scene" : cb.SceneType));
            }
        }
        if (inputs.Count == 0)
        {
            var plain = markdown.StripToPlainText(chapter.Html ?? "");
            foreach (var p in SplitToParagraphs(plain))
                inputs.Add(new RecordingBeatInput(p, null, null, null, null, 0, "scene"));
        }
        if (inputs.Count == 0)
            throw new InvalidOperationException($"Chapter {chapter.Title} has no prose to record.");

        await using var dbWrite = await dbFactory.CreateDbContextAsync(ct);

        var episode = new Episode
        {
            Id = Guid.CreateVersion7(),
            Seed = $"Chapter recording: {chapter.Title}",
            Title = chapter.Title,
            Slug  = ToSlug(chapter.Title) + "-" + chapter.Id[..8],
            Status = "ready_for_audio",
            StartedAt = DateTime.UtcNow,
            GenerationCompletedAt = DateTime.UtcNow,
            VoiceId = voiceId,
            BookId = bookGuid,
            ChapterId = chapter.Id,
        };
        dbWrite.Episodes.Add(episode);

        for (int i = 0; i < inputs.Count; i++)
        {
            var input = inputs[i];
            dbWrite.EpisodeBeats.Add(new EpisodeBeat
            {
                EpisodeId      = episode.Id,
                Index          = i,
                SortKey        = i * 100.0, // big gaps for future splits
                Text           = input.Text,
                SourceBeatGuid = input.SourceBeatGuid,
                TextHash       = EpisodeAudioService.ComputeTextHash(input.Text),
                BeatTitle      = input.BeatTitle,
                Synopsis       = input.Synopsis,
                StructureRole  = input.StructureRole,
                Act            = input.Act,
                SceneType      = input.SceneType,
            });
        }

        await dbWrite.SaveChangesAsync(ct);

        log.LogInformation("Chapter {ChapterId} materialized as episode {EpId} ({Beats} beats)",
            chapter.Id, episode.Id, inputs.Count);

        // Script artifacts immediately; audio fires after.
        try { await export.ExportScriptAsync(episode.Id, ct); }
        catch (Exception ex) { log.LogWarning(ex, "Chapter recording script export failed (non-fatal)"); }

        // Narration in the background — caller's UI polls Episode.Status.
        _ = Task.Run(() => audio.NarrateAsync(episode.Id));

        return episode.Id;
    }

    /// <summary>Delete the existing recording for a chapter and create a fresh
    /// one. Used when the chapter's prose has been edited in the writer.</summary>
    public async Task<Guid> ReRecordChapterAsync(string chapterId, string? voiceId = null, CancellationToken ct = default)
    {
        await using (var db = await dbFactory.CreateDbContextAsync(ct))
        {
            var existing = await db.Episodes
                .Where(e => e.ChapterId == chapterId)
                .ToListAsync(ct);
            foreach (var e in existing)
            {
                // Best-effort: delete audio files on disk before nuking the row.
                try
                {
                    var dir = audio.GetEpisodeRoot(string.IsNullOrEmpty(e.Slug) ? e.Id.ToString() : e.Slug);
                    if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
                }
                catch (Exception ex) { log.LogWarning(ex, "Could not delete audio dir for episode {Id}", e.Id); }
                db.Episodes.Remove(e);
            }
            if (existing.Count > 0) await db.SaveChangesAsync(ct);
        }
        return await RecordChapterAsync(chapterId, voiceId, ct);
    }

    /// <summary>Check every beat of this chapter's recording against the
    /// current Chapter.Beats[].Text and flip Stale=true (clearing AudioPath)
    /// on any drift. Call this from the writer's chapter-save handler — it
    /// is the canonical close on the writer-recording desync gap. Idempotent
    /// and cheap when nothing drifted.</summary>
    public async Task SyncRecordingAfterChapterSaveAsync(string chapterId, CancellationToken ct = default)
    {
        var chapter = chapters.LoadChapter(chapterId);
        if (chapter == null) return;
        if (chapter.Beats is not { Count: > 0 }) return;

        var recording = await GetRecordingAsync(chapterId, ct);
        if (recording == null) return; // never recorded — nothing to invalidate

        foreach (var cb in chapter.Beats)
        {
            if (string.IsNullOrWhiteSpace(cb.Text)) continue;
            await audio.MarkStaleIfDriftedAsync(recording.Id, cb.Id, cb.Text, ct);
        }
    }

    /// <summary>Update one beat's text from the recording panel, then push the
    /// change back to the source ChapterBeat so the writer's prose stays in
    /// sync. Without this propagation, the next chapter-save desync sweep
    /// would clobber the recording-side edit (ChapterBeat still holds the
    /// pre-edit text, hash mismatch flips Stale and adopts the ChapterBeat
    /// text back over the user's edit).
    ///
    /// Skip-cases (recording was built from raw HTML with no source beats,
    /// or a single edit produced multiple paragraphs that don't map 1:1) fall
    /// back to a best-effort behaviour: the EpisodeBeat is still updated;
    /// only the canon-side write is skipped or joins the paragraphs.</summary>
    public async Task UpdateBeatTextWithCanonSyncAsync(Guid episodeId, int beatIndex, string newText, CancellationToken ct = default)
    {
        // Capture the SourceBeatGuid before the edit — UpdateBeatTextAsync may
        // restructure beats on paragraph splits, but the target's identity
        // doesn't change.
        string? sourceBeatGuid;
        string? chapterId;
        await using (var db = await dbFactory.CreateDbContextAsync(ct))
        {
            var rec = await db.Episodes
                .Where(e => e.Id == episodeId)
                .Select(e => new { e.ChapterId })
                .FirstOrDefaultAsync(ct);
            chapterId = rec?.ChapterId;
            sourceBeatGuid = await db.EpisodeBeats
                .Where(b => b.EpisodeId == episodeId && b.Index == beatIndex)
                .Select(b => b.SourceBeatGuid)
                .FirstOrDefaultAsync(ct);
        }

        await audio.UpdateBeatTextAsync(episodeId, beatIndex, newText, ct);

        if (string.IsNullOrEmpty(chapterId) || string.IsNullOrEmpty(sourceBeatGuid))
        {
            // Recording was synthesized from raw HTML — nothing to propagate.
            return;
        }

        var chapter = chapters.LoadChapter(chapterId);
        if (chapter == null) return;
        if (chapter.Beats is not { Count: > 0 }) return;
        var canon = chapter.Beats.FirstOrDefault(b => b.Id == sourceBeatGuid);
        if (canon == null) return;

        // Propagate the FULL edited text (including any blank-line splits)
        // into the canon ChapterBeat. The writer will see the unified prose
        // exactly as edited in the recording panel; the next chapter-save
        // desync sweep will be a no-op because TextHash already matches.
        canon.Text = newText;
        chapters.SaveChapter(chapter);
        log.LogInformation("Chapter {ChapterId} beat {BeatGuid} propagated from recording-panel edit", chapterId, sourceBeatGuid);
    }

    /// <summary>Get the recording episode for a chapter, if one exists.</summary>
    public async Task<Episode?> GetRecordingAsync(string chapterId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.Episodes
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.ChapterId == chapterId, ct);
    }

    /// <summary>Concatenate every chapter recording in a book in chapter order
    /// into one combined audio file at engine/books/{book-slug}/book.wav|mp3.
    /// </summary>
    public async Task<string?> ExportBookAudioAsync(Guid bookId, CancellationToken ct = default)
    {
        var book = books.LoadBook(bookId.ToString("N"))
            ?? throw new InvalidOperationException($"Book {bookId} not found.");

        var orderedChapterIds = book.ChapterIds; // canonical chapter order
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var recordings = await db.Episodes
            .Include(e => e.Beats)
            .Where(e => e.BookId == bookId && e.Status == "ready" && e.CombinedAudioPath != null)
            .AsNoTracking()
            .ToListAsync(ct);

        // Sort by the book's canonical chapter order.
        var byChapterId = recordings.ToDictionary(r => r.ChapterId ?? "", r => r);
        var ordered = orderedChapterIds
            .Select(cid => byChapterId.TryGetValue(cid, out var ep) ? ep : null)
            .Where(ep => ep != null)
            .ToList();

        if (ordered.Count == 0)
        {
            log.LogWarning("Book {BookId} has no completed chapter recordings yet", bookId);
            return null;
        }

        // Same format-detection as combined-episode export: WAV / MP3 / mixed.
        bool allWav = ordered.All(ep => ep!.CombinedAudioPath!.EndsWith(".wav", StringComparison.OrdinalIgnoreCase));
        bool allMp3 = ordered.All(ep => ep!.CombinedAudioPath!.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase));
        if (!allWav && !allMp3)
        {
            log.LogInformation("Book {BookId} chapter recordings have mixed formats — skipping book-level concat", bookId);
            return null;
        }

        var bookDir = Path.Combine(audio.GetAudioRoot(), "..", "books", ToSlug(book.Title) + "-" + bookId.ToString("N")[..8]);
        Directory.CreateDirectory(bookDir);

        var ext = allWav ? "wav" : "mp3";
        var outPath = Path.Combine(bookDir, $"book.{ext}");

        if (allWav)
        {
            var pcmParts = new List<byte[]>();
            foreach (var ep in ordered)
            {
                ct.ThrowIfCancellationRequested();
                var src = Path.Combine(audio.GetAudioRoot(), ep!.CombinedAudioPath!);
                if (!File.Exists(src)) continue;
                var bytes = await File.ReadAllBytesAsync(src, ct);
                if (bytes.Length <= 44) continue;
                pcmParts.Add(bytes[44..]);
            }
            var total = pcmParts.Sum(p => p.Length);
            var all = new byte[total];
            var off = 0;
            foreach (var p in pcmParts) { Buffer.BlockCopy(p, 0, all, off, p.Length); off += p.Length; }
            var wav = EpisodeAudioService.WrapPcmAsWav(all, 44100, 1, 16);
            await File.WriteAllBytesAsync(outPath, wav, ct);
        }
        else
        {
            await using var output = File.Create(outPath);
            foreach (var ep in ordered)
            {
                ct.ThrowIfCancellationRequested();
                var src = Path.Combine(audio.GetAudioRoot(), ep!.CombinedAudioPath!);
                if (!File.Exists(src)) continue;
                var bytes = await File.ReadAllBytesAsync(src, ct);
                await output.WriteAsync(bytes, ct);
            }
        }

        log.LogInformation("Book {BookId} combined audio written to {Path} ({Chapters} chapters)",
            bookId, outPath, ordered.Count);
        return outPath;
    }

    // ── Helpers ────────────────────────────────────────────────────────

    private static IEnumerable<string> SplitToParagraphs(string text)
    {
        // Blank-line separated; drop lone roman numeral section markers.
        var paras = Regex.Split(text, @"\r?\n\s*\r?\n");
        foreach (var raw in paras)
        {
            var p = raw.Trim();
            if (string.IsNullOrWhiteSpace(p)) continue;
            // Drop section markers like "I", "II.", "III"
            var t = p.TrimEnd('.').Trim();
            if (t.Length <= 4 && t.All(c => "IVXivx".Contains(c))) continue;
            yield return p;
        }
    }

    private static string ToSlug(string title)
        => EpisodeGeneratorService.Slugify(title);
}

/// <summary>One paragraph's worth of input when materializing a chapter into
/// an episode. Holds the prose plus the narrative metadata copied off the
/// source ChapterBeat (or default values when synthesizing from raw prose).</summary>
internal record RecordingBeatInput(
    string Text,
    string? SourceBeatGuid,
    string? BeatTitle,
    string? Synopsis,
    string? StructureRole,
    int Act,
    string SceneType);

