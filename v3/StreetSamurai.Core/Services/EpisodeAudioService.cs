using System.Net;
using System.Net.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Narrates a generated episode. Iterates beats in order, calls ElevenLabs at
/// pcm_44100 (16-bit mono PCM @ 44.1 kHz), wraps the raw PCM in a standard
/// 44-byte RIFF WAV header, and writes the lossless .wav into
/// engine/audio/episodes/{episodeId}/. The /listen page plays the WAV files
/// directly (browser audio elements support WAV natively) and the same files
/// are editable in any DAW.
///
/// On TTS failure the service stops and marks the episode "failed". Already
/// narrated beats remain on disk, so partial listens are preserved.
/// </summary>
public class EpisodeAudioService
{
    private readonly IDbContextFactory<StreetSamuraiDbContext> dbFactory;
    private readonly ElevenLabsTtsService tts;
    private readonly IPathProvider paths;
    private readonly ILogger<EpisodeAudioService> log;
    private readonly IServiceProvider sp;

    // Per-episode CancellationTokenSource so the /listen Stop button can abort a
    // narration mid-flight (saves ElevenLabs credits when output is going badly).
    // Registry is process-local; if the host restarts, in-flight narrations were
    // already lost anyway. Keyed by Episode.Id.
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, CancellationTokenSource>
        cancelTokens = new();

    public EpisodeAudioService(
        IDbContextFactory<StreetSamuraiDbContext> dbFactory,
        ElevenLabsTtsService tts,
        IPathProvider paths,
        ILogger<EpisodeAudioService> log,
        IServiceProvider sp)
    {
        this.dbFactory = dbFactory;
        this.tts = tts;
        this.paths = paths;
        this.log = log;
        // EpisodeExportService is resolved via IServiceProvider to break the
        // circular reference (EpisodeExportService depends on EpisodeAudioService
        // for path resolution).
        this.sp = sp;
    }

    /// <summary>
    /// Narrate every un-narrated beat for the episode. Saves MP3s and
    /// updates EpisodeBeat.AudioPath as each completes, so the /listen page
    /// can start playing the first one before the rest are ready.
    /// </summary>
    /// <summary>Cancel any in-flight narration for the given episode. Idempotent;
    /// no-op if no narration is running. Beats already on disk stay; the
    /// in-progress beat may produce a partial file which the next retry will
    /// overwrite.</summary>
    public bool CancelNarration(Guid episodeId)
    {
        if (cancelTokens.TryRemove(episodeId, out var cts))
        {
            try { cts.Cancel(); cts.Dispose(); } catch { }
            log.LogInformation("Episode #{Ep} narration cancellation requested", episodeId);
            return true;
        }
        return false;
    }

    /// <summary>Apply a transcript edit to one beat. If the new text contains
    /// blank-line paragraph breaks, the beat splits into N consecutive beats
    /// (one per paragraph) — the natural Markdown-like UX where the user
    /// presses Enter twice to create a new beat. All touched beats have their
    /// audio invalidated; the next narration pass will re-record exactly those.
    /// Subsequent beats shift up by (newCount - 1).</summary>
    public async Task UpdateBeatTextAsync(Guid episodeId, int beatIndex, string newText, CancellationToken ct = default)
    {
        // Detect paragraph splits in the new text.
        var paragraphs = System.Text.RegularExpressions.Regex
            .Split(newText, @"\r?\n\s*\r?\n")
            .Select(p => p.Trim())
            .Where(p => p.Length > 0)
            .ToList();
        if (paragraphs.Count == 0) paragraphs.Add("");

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var beats = await db.EpisodeBeats
            .Where(b => b.EpisodeId == episodeId)
            .OrderBy(b => b.SortKey)
            .ToListAsync(ct);
        var pos = beats.FindIndex(b => b.Index == beatIndex);
        if (pos < 0) throw new InvalidOperationException($"Beat {beatIndex} of episode {episodeId} not found.");
        var target = beats[pos];

        // Update the target with the first paragraph.
        target.Text = paragraphs[0];
        target.WasCorrected = true;
        InvalidateAudioOnBeat(target);
        // Hash now reflects what *will be* narrated next — keeps the
        // chapter-save desync sweep idempotent: ChapterBeat.Text will be
        // propagated to match, the hashes will line up, no false Stale flap.
        // Mark Stale so the UI shows "needs re-record" until the user kicks
        // narration. LastRequestId is meaningless once audio is invalidated.
        target.TextHash      = ComputeTextHash(target.Text);
        target.Stale         = true;
        target.LastRequestId = null;

        // For extra paragraphs, slot each in via fractional indexing between
        // the prior beat's SortKey and the next sibling's SortKey. No
        // downstream renumbering — that's the whole point of SortKey.
        if (paragraphs.Count > 1)
        {
            var nextSortKey = pos + 1 < beats.Count
                ? beats[pos + 1].SortKey
                : target.SortKey + 100.0;
            var nextIndex = beats.Max(b => b.Index) + 1;

            // Distribute (paragraphs.Count - 1) new keys uniformly between
            // target.SortKey and nextSortKey.
            var step = (nextSortKey - target.SortKey) / paragraphs.Count;
            for (int k = 1; k < paragraphs.Count; k++)
            {
                db.EpisodeBeats.Add(new EpisodeBeat
                {
                    EpisodeId    = episodeId,
                    Index        = nextIndex++,
                    SortKey      = target.SortKey + step * k,
                    Text         = paragraphs[k],
                    WasCorrected = true,
                    TextHash     = ComputeTextHash(paragraphs[k]),
                    Stale        = true,
                });
            }
        }

        await db.SaveChangesAsync(ct);
        if (paragraphs.Count == 1)
            log.LogInformation("Episode #{Ep} beat #{Idx} text updated ({Chars} chars); audio invalidated.",
                episodeId, beatIndex, newText.Length);
        else
            log.LogInformation("Episode #{Ep} beat #{Idx} split into {N} beats on paragraph breaks.",
                episodeId, beatIndex, paragraphs.Count);
    }

    /// <summary>Split one beat into two at the nearest sentence boundary closest
    /// to the midpoint of its text. The new beat takes the next Index, every
    /// subsequent beat shifts up by one. Both touched beats lose their audio
    /// because the text chunking changed; the next narration pass will re-record.
    /// </summary>
    public async Task SplitBeatAsync(Guid episodeId, int beatIndex, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var beats = await db.EpisodeBeats
            .Where(b => b.EpisodeId == episodeId)
            .OrderBy(b => b.SortKey)
            .ToListAsync(ct);
        var targetPos = beats.FindIndex(b => b.Index == beatIndex);
        if (targetPos < 0) throw new InvalidOperationException($"Beat {beatIndex} not found.");
        var target = beats[targetPos];

        var text = target.Text;
        if (text.Length < 40)
            throw new InvalidOperationException("Beat is too short to split sensibly.");
        int split = FindSentenceSplit(text);

        var firstHalf  = text[..split].TrimEnd();
        var secondHalf = text[split..].TrimStart();
        if (firstHalf.Length == 0 || secondHalf.Length == 0)
            throw new InvalidOperationException("Could not find a clean split point.");

        // Fractional indexing: new SortKey is half-way between this beat and
        // the next sibling. No renumbering of other rows.
        var nextSortKey = targetPos + 1 < beats.Count
            ? beats[targetPos + 1].SortKey
            : target.SortKey + 100.0;
        var newSortKey = (target.SortKey + nextSortKey) / 2.0;

        target.Text = firstHalf;
        InvalidateAudioOnBeat(target);
        target.WasCorrected = true;

        // Stable Index for the new beat: max(Index) + 1 within this episode.
        var newIndex = beats.Max(b => b.Index) + 1;

        db.EpisodeBeats.Add(new EpisodeBeat
        {
            EpisodeId    = episodeId,
            Index        = newIndex,
            SortKey      = newSortKey,
            Text         = secondHalf,
            WasCorrected = true,
        });

        await db.SaveChangesAsync(ct);
        log.LogInformation("Episode #{Ep} beat #{Idx} split at SortKey {SortKey} (new beat Index #{NewIdx})",
            episodeId, beatIndex, newSortKey, newIndex);
    }

    /// <summary>Insert a brand-new empty beat immediately after the target beat.
    /// Uses fractional indexing — the new beat's SortKey lands halfway between
    /// the target and the next sibling. Zero downstream renumbering.</summary>
    public async Task<int> InsertBeatAfterAsync(Guid episodeId, int afterBeatIndex, string initialText = "", CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var beats = await db.EpisodeBeats
            .Where(b => b.EpisodeId == episodeId)
            .OrderBy(b => b.SortKey)
            .ToListAsync(ct);

        // Special case: afterBeatIndex == -1 inserts at the very top.
        double prevSk, nextSk;
        int newIndex;
        if (afterBeatIndex < 0)
        {
            prevSk = beats.Count > 0 ? beats[0].SortKey - 100.0 : 0.0;
            nextSk = beats.Count > 0 ? beats[0].SortKey         : 100.0;
        }
        else
        {
            var pos = beats.FindIndex(b => b.Index == afterBeatIndex);
            if (pos < 0) throw new InvalidOperationException($"Beat {afterBeatIndex} not found.");
            prevSk = beats[pos].SortKey;
            nextSk = pos + 1 < beats.Count ? beats[pos + 1].SortKey : prevSk + 100.0;
        }
        newIndex = (beats.Count == 0 ? 0 : beats.Max(b => b.Index) + 1);

        var newBeat = new EpisodeBeat
        {
            EpisodeId    = episodeId,
            Index        = newIndex,
            SortKey      = (prevSk + nextSk) / 2.0,
            Text         = initialText,
            WasCorrected = true,
            Stale        = false,
        };
        db.EpisodeBeats.Add(newBeat);

        await db.SaveChangesAsync(ct);
        log.LogInformation("Episode #{Ep} inserted new beat Index #{Idx} after Index #{After} (SortKey {SortKey})",
            episodeId, newIndex, afterBeatIndex, newBeat.SortKey);
        return newIndex;
    }

    /// <summary>Compare a beat's stored TextHash against the supplied current
    /// text. On mismatch: nulls AudioPath, sets Stale=true, deletes the audio
    /// file on disk. Idempotent — calling repeatedly when hashes match is a
    /// no-op. The writer's chapter-save handler calls this for every
    /// ChapterBeat against its mapped EpisodeBeat (lookup by SourceBeatGuid).</summary>
    public async Task MarkStaleIfDriftedAsync(Guid episodeId, string sourceBeatGuid, string currentText, CancellationToken ct = default)
    {
        var hash = ComputeTextHash(currentText);
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var beat = await db.EpisodeBeats
            .FirstOrDefaultAsync(b => b.EpisodeId == episodeId && b.SourceBeatGuid == sourceBeatGuid, ct);
        if (beat == null) return;
        if (beat.TextHash == hash) return;

        // Drift detected — invalidate.
        if (!string.IsNullOrEmpty(beat.AudioPath))
        {
            try
            {
                var full = ResolveAudioFile(beat.AudioPath);
                if (File.Exists(full)) File.Delete(full);
            }
            catch (Exception ex) { log.LogWarning(ex, "Could not delete stale audio at {Path}", beat.AudioPath); }
        }
        beat.AudioPath    = null;
        beat.NarratedAt   = null;
        beat.DurationSec  = null;
        beat.Stale        = true;
        beat.WasCorrected = true;
        beat.Text         = currentText; // adopt the writer's canonical prose
        await db.SaveChangesAsync(ct);
        log.LogInformation("Episode #{Ep} beat {SrcId} marked stale (text drifted past recording)",
            episodeId, sourceBeatGuid);
    }

    /// <summary>Merge a beat into its predecessor. The target beat's text is
    /// appended to the previous beat's text (with a blank-line separator), the
    /// target row is deleted, every subsequent beat shifts down by one. The
    /// merged beat's audio is invalidated.</summary>
    public async Task JoinBeatWithPreviousAsync(Guid episodeId, int beatIndex, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var beats = await db.EpisodeBeats
            .Where(b => b.EpisodeId == episodeId)
            .OrderBy(b => b.SortKey)
            .ToListAsync(ct);
        var pos = beats.FindIndex(b => b.Index == beatIndex);
        if (pos < 0) throw new InvalidOperationException($"Beat {beatIndex} not found.");
        if (pos == 0) throw new InvalidOperationException("Cannot join the first beat — there is nothing before it.");

        var target   = beats[pos];
        var previous = beats[pos - 1];

        previous.Text = $"{previous.Text.TrimEnd()}\n\n{target.Text.TrimStart()}";
        InvalidateAudioOnBeat(previous);
        previous.WasCorrected = true;

        // Delete the target row outright — no shifting needed because SortKey
        // ordering doesn't depend on contiguous values.
        db.EpisodeBeats.Remove(target);

        await db.SaveChangesAsync(ct);
        log.LogInformation("Episode #{Ep} beat (Index #{Idx}) merged into the prior beat (Index #{Prev}); no renumber.",
            episodeId, beatIndex, previous.Index);
    }

    /// <summary>Clear audio fields on a beat in memory and delete the file on disk.</summary>
    private void InvalidateAudioOnBeat(EpisodeBeat beat)
    {
        if (!string.IsNullOrEmpty(beat.AudioPath))
        {
            try
            {
                var full = ResolveAudioFile(beat.AudioPath);
                if (File.Exists(full)) File.Delete(full);
            }
            catch (Exception ex) { log.LogWarning(ex, "Could not delete audio at {Path}", beat.AudioPath); }
        }
        beat.AudioPath  = null;
        beat.NarratedAt = null;
        beat.DurationSec = null;
    }

    /// <summary>Find a sentence-boundary split point near the midpoint of the
    /// text. Walks outward from the middle searching for terminator+space.</summary>
    private static int FindSentenceSplit(string text)
    {
        int mid = text.Length / 2;
        // Bias search radius to text length so very long beats still find a hit.
        int radius = Math.Max(80, text.Length / 3);
        for (int offset = 0; offset <= radius; offset++)
        {
            foreach (var dir in new[] { -1, +1 })
            {
                int i = mid + dir * offset;
                if (i < 4 || i >= text.Length - 4) continue;
                // Accept ". " "? " "! " — typical sentence terminators followed by a space.
                if ((text[i] == '.' || text[i] == '?' || text[i] == '!') && char.IsWhiteSpace(text[i + 1]))
                    return i + 1; // split AFTER the space so the next beat starts cleanly
            }
        }
        // Fallback: split at the nearest word boundary near midpoint.
        for (int i = mid; i < text.Length; i++)
            if (char.IsWhiteSpace(text[i])) return i + 1;
        return mid;
    }

    /// <summary>Discard the audio for one beat so NarrateAsync re-records it on
    /// the next pass. Used by the /listen "Re-record this beat" button when
    /// the user dislikes the take but the text is fine.</summary>
    public async Task InvalidateBeatAudioAsync(Guid episodeId, int beatIndex, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var beat = await db.EpisodeBeats
            .FirstOrDefaultAsync(b => b.EpisodeId == episodeId && b.Index == beatIndex, ct);
        if (beat == null || string.IsNullOrEmpty(beat.AudioPath)) return;

        var fullPath = ResolveAudioFile(beat.AudioPath);
        try { if (File.Exists(fullPath)) File.Delete(fullPath); }
        catch (Exception ex) { log.LogWarning(ex, "Could not delete audio at {Path}", fullPath); }
        beat.AudioPath  = null;
        beat.NarratedAt = null;
        beat.DurationSec = null;
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Resume narration on an episode that previously failed mid-way.
    /// Clears the failed status + error message and re-enters the same beat
    /// loop. Beats with AudioPath set are skipped, so narration picks up at
    /// the first unrendered beat.</summary>
    public async Task RetryAsync(Guid episodeId, CancellationToken ct = default)
    {
        await using (var db = await dbFactory.CreateDbContextAsync(ct))
        {
            var episode = await db.Episodes.FirstOrDefaultAsync(e => e.Id == episodeId, ct);
            if (episode == null) throw new InvalidOperationException($"Episode {episodeId} not found.");
            // Reset failure state so the polling UI flips out of "failed" — narration
            // will set it back to "narrating" then "ready".
            if (episode.Status == "failed")
            {
                episode.Status = "narrating";
                episode.Error = null;
                await db.SaveChangesAsync(ct);
            }
        }
        log.LogInformation("Retrying episode #{Ep} narration", episodeId);
        await NarrateAsync(episodeId, ct);
    }

    public async Task NarrateAsync(Guid episodeId, CancellationToken ct = default)
    {
        if (!await tts.IsConfiguredAsync())
            throw new InvalidOperationException("TTS is not configured. Set ElevenLabs API key in Settings.");

        // Register a per-episode CTS so the /listen Stop button can cancel us.
        // The link combines the caller's ct with the cancel-button's source.
        using var cancelCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cancelTokens[episodeId] = cancelCts;
        ct = cancelCts.Token;

        try
        {

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var episode = await db.Episodes
            .Include(e => e.Beats)
            .FirstOrDefaultAsync(e => e.Id == episodeId, ct)
            ?? throw new InvalidOperationException($"Episode {episodeId} not found.");

        episode.Status = "narrating";
        episode.VoiceId ??= null; // default voice falls back inside the TTS service
        await db.SaveChangesAsync(ct);

        var slug = !string.IsNullOrWhiteSpace(episode.Slug) ? episode.Slug : episode.Id.ToString();
        var outDir = Path.Combine(GetEpisodeRoot(slug), "audio");
        Directory.CreateDirectory(outDir);

        var ordered = episode.Beats.OrderBy(b => b.SortKey).ThenBy(b => b.Index).ToList();
        int totalChars = episode.CharsNarrated;
        // pcm_44100 is gated to higher ElevenLabs subscription tiers; lower tiers
        // 403 on the request. We try lossless once, and on Forbidden we flip to
        // mp3_44100_128 for the rest of the episode. The chosen format is
        // remembered in this loop only (next episode will probe again).
        bool useLossless = true;
        // Sliding window of the last 3 ElevenLabs request-ids. Passing these as
        // previous_request_ids on each subsequent call conditions the new
        // generation on the prior audio, so prosody, timbre, and pace carry
        // across beat boundaries. Without this, adjacent beats sound like
        // different takes by different readers.
        var stitchWindow = new System.Collections.Generic.Queue<string>(3);
        // Cross-session seed: if some beats are already narrated and have
        // persisted LastRequestId values, seed the window from the most recent
        // three. Critical for single-beat re-records — without this the lone
        // re-rendered beat would have no context and would sound off from its
        // neighbours.
        foreach (var b in ordered.Where(b => !string.IsNullOrEmpty(b.LastRequestId)).TakeLast(3))
            stitchWindow.Enqueue(b.LastRequestId!);

        foreach (var beat in ordered)
        {
            ct.ThrowIfCancellationRequested();
            if (!string.IsNullOrEmpty(beat.AudioPath)) continue; // already narrated

            try
            {
                log.LogDebug("Narrating episode #{Ep} beat #{Idx} ({Chars} chars, lossless={Lossless}, stitch={StitchCount})",
                    episodeId, beat.Index, beat.Text.Length, useLossless, stitchWindow.Count);

                // Build text context windows: up to ~1500 chars before and
                // after, taken from neighbouring beats. ElevenLabs uses these
                // to keep intonation / sentence-continuation coherent across
                // paragraph boundaries.
                var (prevText, nextText) = BuildTextWindow(ordered, beat.Index, contextChars: 1500);

                string? newRequestId;
                if (useLossless)
                {
                    try
                    {
                        newRequestId = await SynthesizeBeatAsLosslessWavAsync(episode, beat, slug, outDir, stitchWindow.ToArray(), prevText, nextText, ct);
                    }
                    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    {
                        // Tier-gated format. Fall back to MP3 for this and all subsequent beats.
                        log.LogWarning("Episode #{Ep}: pcm_44100 forbidden by ElevenLabs tier — falling back to mp3_44100_128 for remaining beats", episodeId);
                        useLossless = false;
                        newRequestId = await SynthesizeBeatAsMp3Async(episode, beat, slug, outDir, stitchWindow.ToArray(), prevText, nextText, ct);
                    }
                }
                else
                {
                    newRequestId = await SynthesizeBeatAsMp3Async(episode, beat, slug, outDir, stitchWindow.ToArray(), prevText, nextText, ct);
                }

                // Roll the window forward. ElevenLabs allows up to 3 ids.
                if (!string.IsNullOrEmpty(newRequestId))
                {
                    stitchWindow.Enqueue(newRequestId);
                    while (stitchWindow.Count > 3) stitchWindow.Dequeue();
                }

                totalChars += beat.Text.Length;
                episode.CharsNarrated = totalChars;
                await db.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "TTS failed on episode #{Ep} beat #{Idx}", episodeId, beat.Index);
                episode.Status = "failed";
                episode.Error = $"TTS failed on beat {beat.Index}: {ex.Message}";
                await db.SaveChangesAsync(ct);
                throw;
            }
        }

        episode.Status = "ready";
        episode.AudioCompletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        log.LogInformation("Episode #{Ep} narration complete ({Beats} beats, {Chars} chars)",
            episodeId, ordered.Count, totalChars);

        // File the combined audio. Non-fatal — even if concat fails the per-beat
        // WAVs are still on disk and the user can grab them individually.
        try
        {
            var exporter = sp.GetService(typeof(EpisodeExportService)) as EpisodeExportService;
            if (exporter != null)
                await exporter.ExportCombinedAudioAsync(episodeId, ct);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Episode #{Ep} combined audio export failed (non-fatal)", episodeId);
        }
        }
        catch (OperationCanceledException)
        {
            log.LogInformation("Episode #{Ep} narration cancelled by user", episodeId);
            await using var db = await dbFactory.CreateDbContextAsync(CancellationToken.None);
            var ep = await db.Episodes.FirstOrDefaultAsync(e => e.Id == episodeId);
            if (ep != null && ep.Status == "narrating")
            {
                ep.Status = "stopped";
                ep.Error  = "Narration cancelled by user.";
                await db.SaveChangesAsync();
            }
        }
        finally
        {
            cancelTokens.TryRemove(episodeId, out _);
        }
    }

    /// <summary>The on-disk root that EpisodeBeat.AudioPath is relative to.
    /// All per-episode artifacts live under here:
    ///   engine/episodes/{slug}/audio/{idx:D3}.wav  (per-beat WAVs)
    ///   engine/episodes/{slug}/script.md           (Markdown script)
    ///   engine/episodes/{slug}/script.pdf          (PDF script)
    ///   engine/episodes/{slug}/episode.wav         (combined narration)
    /// </summary>
    public string GetAudioRoot() =>
        Path.Combine(paths.DataRoot, "engine", "episodes");

    /// <summary>Directory for one specific episode's files, by slug.</summary>
    public string GetEpisodeRoot(string slug) =>
        Path.Combine(paths.DataRoot, "engine", "episodes", slug);

    /// <summary>Absolute path on disk for a given relative audio path. Use to
    /// stream a file from the /audio static endpoint.</summary>
    public string ResolveAudioFile(string relativePath) =>
        Path.Combine(GetAudioRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar));

    /// <summary>Build the previous / next text-context strings the TTS sees when
    /// synthesizing one beat. Concatenates surrounding paragraphs walking
    /// outward from the target until <paramref name="contextChars"/> is reached
    /// in each direction. Truncated on a paragraph boundary so partial
    /// sentences don't confuse the model.</summary>
    private static (string? previous, string? next) BuildTextWindow(
        List<EpisodeBeat> ordered, int targetBeatIndex, int contextChars)
    {
        string? prev = null;
        string? next = null;
        var prevBuf = new System.Text.StringBuilder();
        var nextBuf = new System.Text.StringBuilder();

        // Walk backwards: append more recent first, but produce reading-order
        // text. Easiest: collect into a list then join in original order.
        var prevParts = new List<string>();
        for (int i = targetBeatIndex - 1; i >= 0; i--)
        {
            var t = ordered[i].Text;
            if (string.IsNullOrEmpty(t)) continue;
            if (prevBuf.Length + t.Length > contextChars) break;
            prevBuf.Append(t).Append('\n');
            prevParts.Insert(0, t);
        }
        if (prevParts.Count > 0)
            prev = string.Join("\n\n", prevParts);

        for (int i = targetBeatIndex + 1; i < ordered.Count; i++)
        {
            var t = ordered[i].Text;
            if (string.IsNullOrEmpty(t)) continue;
            if (nextBuf.Length + t.Length > contextChars) break;
            nextBuf.Append(t).Append('\n');
        }
        if (nextBuf.Length > 0)
            next = nextBuf.ToString().TrimEnd();

        return (prev, next);
    }

    // ── Beat synthesis (lossless WAV / fallback MP3) ────────────────────

    /// <summary>Request pcm_44100 with optional stitching context, wrap in WAV
    /// header, write .wav, stamp the beat. Returns the new ElevenLabs
    /// request-id (or null) for the caller's stitching window. Throws
    /// HttpRequestException(403) if the user's tier blocks the format —
    /// the caller is expected to catch and fall back to MP3.</summary>
    private async Task<string?> SynthesizeBeatAsLosslessWavAsync(
        Episode episode, EpisodeBeat beat, string slug, string outDir,
        string[] previousRequestIds, string? previousText, string? nextText,
        CancellationToken ct)
    {
        var result = await tts.SynthesizeWithIdAsync(
            beat.Text, episode.VoiceId, outputFormat: "pcm_44100",
            previousRequestIds: previousRequestIds,
            previousText: previousText, nextText: nextText, ct);

        var wav = WrapPcmAsWav(result.Bytes, sampleRate: 44100, channels: 1, bitsPerSample: 16);

        var fileName = $"{beat.Index:D3}.wav";
        var fullPath = Path.Combine(outDir, fileName);
        await File.WriteAllBytesAsync(fullPath, wav, ct);

        beat.AudioPath  = $"{slug}/audio/{fileName}";
        beat.NarratedAt = DateTime.UtcNow;
        // 16-bit mono PCM @ 44.1 kHz → 88200 bytes/sec.
        beat.DurationSec = result.Bytes.Length / 88200.0;
        beat.TextHash      = ComputeTextHash(beat.Text);
        beat.LastRequestId = result.RequestId;
        beat.Stale         = false; // freshly rendered against current text
        return result.RequestId;
    }

    /// <summary>Request mp3_44100_128 (ElevenLabs default; available on every tier)
    /// with optional stitching context. Returns the new ElevenLabs request-id.</summary>
    private async Task<string?> SynthesizeBeatAsMp3Async(
        Episode episode, EpisodeBeat beat, string slug, string outDir,
        string[] previousRequestIds, string? previousText, string? nextText,
        CancellationToken ct)
    {
        var result = await tts.SynthesizeWithIdAsync(
            beat.Text, episode.VoiceId, outputFormat: null,
            previousRequestIds: previousRequestIds,
            previousText: previousText, nextText: nextText, ct);

        var fileName = $"{beat.Index:D3}.mp3";
        var fullPath = Path.Combine(outDir, fileName);
        await File.WriteAllBytesAsync(fullPath, result.Bytes, ct);

        beat.AudioPath  = $"{slug}/audio/{fileName}";
        beat.NarratedAt = DateTime.UtcNow;
        // ~15 chars/sec narration is the rough industry rule; better than null
        // for the progress bar's virtual timeline.
        beat.DurationSec = Math.Max(1.0, beat.Text.Length / 15.0);
        beat.TextHash      = ComputeTextHash(beat.Text);
        beat.LastRequestId = result.RequestId;
        beat.Stale         = false;
        return result.RequestId;
    }

    /// <summary>SHA-256 hex of normalized prose. Whitespace at the edges is
    /// trimmed before hashing so trailing-newline edits don't churn the hash.</summary>
    public static string ComputeTextHash(string text)
    {
        var normalized = (text ?? "").Trim();
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>
    /// Prepend a standard RIFF / WAVE header to a raw PCM byte stream so the
    /// result is a valid .wav file readable by browsers, ffmpeg, Audacity,
    /// Reaper, Pro Tools, etc.
    ///
    /// The 44-byte header for 16-bit mono PCM at 44100 Hz is well-defined: see
    /// http://soundfile.sapp.org/doc/WaveFormat/. Multi-channel and other bit
    /// depths require trivial parameter changes.
    /// </summary>
    public static byte[] WrapPcmAsWav(byte[] pcm, int sampleRate, short channels, short bitsPerSample)
    {
        int dataChunkSize = pcm.Length;
        using var ms = new MemoryStream(44 + dataChunkSize);
        WriteWavHeader(ms, dataChunkSize, sampleRate, channels, bitsPerSample);
        ms.Write(pcm, 0, pcm.Length);
        return ms.ToArray();
    }

    /// <summary>Write a 44-byte RIFF/WAVE header to <paramref name="dst"/>
    /// describing a PCM payload of <paramref name="dataChunkSize"/> bytes.
    /// Caller is responsible for writing the PCM bytes that follow. Used by
    /// the streaming concat path so we don't have to materialize the whole
    /// node's PCM in memory before stamping the header.</summary>
    public static void WriteWavHeader(Stream dst, int dataChunkSize, int sampleRate, short channels, short bitsPerSample)
    {
        int byteRate = sampleRate * channels * bitsPerSample / 8;
        short blockAlign = (short)(channels * bitsPerSample / 8);
        int riffChunkSize = 36 + dataChunkSize;
        using var w = new BinaryWriter(dst, System.Text.Encoding.ASCII, leaveOpen: true);
        w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
        w.Write(riffChunkSize);
        w.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
        w.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
        w.Write(16);
        w.Write((short)1);
        w.Write(channels);
        w.Write(sampleRate);
        w.Write(byteRate);
        w.Write(blockAlign);
        w.Write(bitsPerSample);
        w.Write(System.Text.Encoding.ASCII.GetBytes("data"));
        w.Write(dataChunkSize);
    }
}
