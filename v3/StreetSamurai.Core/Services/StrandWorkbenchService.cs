using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

/// <summary>
/// The one service that drives the unified <c>/strand/{id}</c> writer +
/// recorder + listener page. CRUD on beats (edit, insert, split, delete),
/// narration (TTS with stitching, MP3 fallback, cancellation), and
/// combined-audio export. Replaces the
/// <c>EpisodeAudioService</c> + <c>ChapterRecordingService</c> pair: those
/// stay alive for legacy /listen and /recordings pages during the
/// transition, but new code paths flow through here.
///
/// Operates on the unified <see cref="Beat"/> / <see cref="Strand"/> /
/// <see cref="StrandBeat"/> schema. A Beat appearing in multiple strands
/// edits in one place; one audio rendering per beat.
/// </summary>
public class StrandWorkbenchService
{
    private readonly IDbContextFactory<StreetSamuraiDbContext> dbFactory;
    private readonly ElevenLabsTtsService tts;
    private readonly IPathProvider paths;
    private readonly SettingsService? settings;
    private readonly ILogger<StrandWorkbenchService> log;
    private static readonly ConcurrentDictionary<Guid, CancellationTokenSource> cancelTokens = new();

    public StrandWorkbenchService(
        IDbContextFactory<StreetSamuraiDbContext> dbFactory,
        ElevenLabsTtsService tts,
        IPathProvider paths,
        ILogger<StrandWorkbenchService> log,
        SettingsService? settings = null)
    {
        this.dbFactory = dbFactory;
        this.tts = tts;
        this.paths = paths;
        this.settings = settings;
        this.log = log;
    }

    // ── Reads ────────────────────────────────────────────────────────────

    /// <summary>Walk this strand's tree (recursing into sub-strands) and
    /// return its beats in reading order. Each entry includes its source
    /// strand so the UI can group beats under sub-strand headers when the
    /// caller wants to render a multi-level page.</summary>
    public async Task<List<OrderedBeat>> GetOrderedBeatsAsync(Guid strandId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var result = new List<OrderedBeat>();
        // Cycle guard: ParentStrandId is supposed to form a DAG, but a bad
        // data import could close a loop. We track visited strands so we
        // bail out cleanly instead of blowing the stack.
        var visited = new HashSet<Guid>();
        await WalkAsync(db, strandId, result, visited, ct);
        return result;
    }

    private static async Task WalkAsync(StreetSamuraiDbContext db, Guid strandId, List<OrderedBeat> acc, HashSet<Guid> visited, CancellationToken ct)
    {
        if (!visited.Add(strandId)) return; // cycle — already walked this strand once.

        // Direct beats first, in SortKey order.
        var direct = await db.StrandBeats
            .Where(sb => sb.StrandId == strandId)
            .OrderBy(sb => sb.SortKey)
            .Join(db.Beats, sb => sb.BeatId, b => b.Id, (sb, b) => new { sb.SortKey, Beat = b })
            .ToListAsync(ct);
        foreach (var d in direct)
            acc.Add(new OrderedBeat(d.Beat, strandId, d.SortKey));

        // Then child strands in SortKey order (recursive).
        var children = await db.Strands
            .Where(s => s.ParentStrandId == strandId)
            .OrderBy(s => s.SortKey)
            .Select(s => s.Id)
            .ToListAsync(ct);
        foreach (var c in children)
            await WalkAsync(db, c, acc, visited, ct);
    }

    /// <summary>Cheap count without loading the beats — for tile/badge displays.</summary>
    public async Task<int> CountBeatsAsync(Guid strandId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.StrandBeats.CountAsync(sb => sb.StrandId == strandId, ct);
    }

    // ── Edits ────────────────────────────────────────────────────────────

    /// <summary>Update one beat's prose. Recomputes the hash, marks the beat
    /// Stale, nulls AudioPath, and deletes the on-disk audio file. The next
    /// narration pass re-records it.
    ///
    /// <para><paramref name="expectedUpdatedAt"/> is the long-window
    /// concurrency check: pass the <c>UpdatedAt</c> the caller saw when it
    /// loaded the beat. If the row was modified since (another tab edited
    /// it; an MCP tool wrote to it), this throws
    /// <see cref="BeatConflictException"/> carrying the freshly-loaded
    /// text so the UI can surface a "keep yours or reload?" choice. Pass
    /// <c>null</c> to skip the check (fire-and-forget callers, migrations).</para>
    /// </summary>
    public async Task UpdateBeatTextAsync(Guid beatId, string newText, DateTime? expectedUpdatedAt = null, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var beat = await db.Beats.FirstOrDefaultAsync(b => b.Id == beatId, ct)
            ?? throw new InvalidOperationException($"Beat {beatId} not found.");

        // Long-window check: the row may have been modified between when the
        // caller loaded it and now. Surfaces as a typed conflict the UI knows
        // how to handle.
        if (expectedUpdatedAt is { } expected
            && Math.Abs((beat.UpdatedAt - expected).TotalMilliseconds) > 1.0)
        {
            throw new BeatConflictException(beatId, expected, beat.UpdatedAt, beat.Text ?? "");
        }

        var trimmed = (newText ?? "").Trim();
        if (beat.Text == trimmed) return; // no-op — don't bump UpdatedAt for nothing

        beat.Text          = trimmed;
        beat.TextHash      = ComputeTextHash(trimmed);
        beat.WasCorrected  = true;
        beat.Stale         = true;
        InvalidateAudioOnBeat(beat);
        beat.UpdatedAt = DateTime.UtcNow;
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Short-window race: another writer SaveChanges'd between our
            // load and our save. Re-fetch and surface the same typed
            // conflict so the UI handles both windows identically.
            await using var probe = await dbFactory.CreateDbContextAsync(ct);
            var fresh = await probe.Beats.AsNoTracking().FirstOrDefaultAsync(b => b.Id == beatId, ct);
            throw new BeatConflictException(beatId,
                expectedUpdatedAt ?? default,
                fresh?.UpdatedAt ?? DateTime.UtcNow,
                fresh?.Text ?? "");
        }
    }

    /// <summary>Update a beat's narrative metadata — the fields that drive
    /// <see cref="BeatPromptBuilder"/> at narration time. Does NOT touch
    /// the prose, the audio, or the hash; the user can tune tone without
    /// invalidating the existing recording. The next re-record picks up
    /// the new tone via the prompt builder.</summary>
    public async Task UpdateBeatMetadataAsync(Guid beatId, BeatMetadataUpdate update, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var beat = await db.Beats.FirstOrDefaultAsync(b => b.Id == beatId, ct)
            ?? throw new InvalidOperationException($"Beat {beatId} not found.");
        beat.BeatTitle     = string.IsNullOrWhiteSpace(update.BeatTitle)     ? null : update.BeatTitle.Trim();
        beat.Synopsis      = string.IsNullOrWhiteSpace(update.Synopsis)      ? null : update.Synopsis.Trim();
        beat.EmotionalTone = string.IsNullOrWhiteSpace(update.EmotionalTone) ? null : update.EmotionalTone.Trim().ToLowerInvariant();
        beat.PaceHint      = string.IsNullOrWhiteSpace(update.PaceHint)      ? null : update.PaceHint.Trim().ToLowerInvariant();
        beat.FacetTag      = string.IsNullOrWhiteSpace(update.FacetTag)      ? null : update.FacetTag.Trim().ToUpperInvariant();
        beat.StructureRole = string.IsNullOrWhiteSpace(update.StructureRole) ? null : update.StructureRole.Trim();
        beat.Act           = update.Act;
        beat.SceneType     = string.IsNullOrWhiteSpace(update.SceneType)     ? "scene" : update.SceneType.Trim();
        beat.UpdatedAt     = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Insert a brand-new empty beat into <paramref name="strandId"/>
    /// at a fractional SortKey just after <paramref name="afterBeatId"/>.
    /// Pass <c>null</c> for <paramref name="afterBeatId"/> to insert at the
    /// very top of the strand.</summary>
    public async Task<Beat> InsertBeatAsync(Guid strandId, Guid? afterBeatId, string initialText = "", CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var ordered = await db.StrandBeats
            .Where(sb => sb.StrandId == strandId)
            .OrderBy(sb => sb.SortKey)
            .ToListAsync(ct);

        double prevSk, nextSk;
        if (afterBeatId == null)
        {
            prevSk = ordered.Count > 0 ? ordered[0].SortKey - 100.0 : 0.0;
            nextSk = ordered.Count > 0 ? ordered[0].SortKey         : 100.0;
        }
        else
        {
            var pos = ordered.FindIndex(sb => sb.BeatId == afterBeatId.Value);
            if (pos < 0) throw new InvalidOperationException($"Beat {afterBeatId} not in strand {strandId}.");
            prevSk = ordered[pos].SortKey;
            nextSk = pos + 1 < ordered.Count ? ordered[pos + 1].SortKey : prevSk + 100.0;
        }

        var beat = new Beat
        {
            Id           = Guid.CreateVersion7(),
            Text         = initialText,
            TextHash     = string.IsNullOrEmpty(initialText) ? null : ComputeTextHash(initialText),
            SceneType    = "scene",
            WasCorrected = true,
            Stale        = false,
        };
        db.Beats.Add(beat);
        db.StrandBeats.Add(new StrandBeat
        {
            StrandId = strandId,
            BeatId   = beat.Id,
            SortKey  = (prevSk + nextSk) / 2.0,
        });
        await db.SaveChangesAsync(ct);
        log.LogInformation("Inserted beat {BeatId} into strand {StrandId} between SortKey {Prev} and {Next}",
            beat.Id, strandId, prevSk, nextSk);
        return beat;
    }

    /// <summary>Split one beat into two at the nearest sentence boundary
    /// closest to its midpoint. The second half goes into a fresh Beat with
    /// a fractional SortKey between the original and the next sibling. Both
    /// halves lose their audio because the text-boundaries changed; the next
    /// narration pass re-records them.</summary>
    public async Task<Beat> SplitBeatAsync(Guid strandId, Guid beatId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var target = await db.Beats.FirstOrDefaultAsync(b => b.Id == beatId, ct)
            ?? throw new InvalidOperationException($"Beat {beatId} not found.");

        var text = target.Text ?? "";
        if (text.Length < 40)
            throw new InvalidOperationException("Beat is too short to split sensibly.");
        int split = FindSentenceSplit(text);
        var firstHalf  = text[..split].TrimEnd();
        var secondHalf = text[split..].TrimStart();
        if (firstHalf.Length == 0 || secondHalf.Length == 0)
            throw new InvalidOperationException("Could not find a clean split point.");

        // Find the target's SortKey in this strand to slot the new beat.
        var siblings = await db.StrandBeats
            .Where(sb => sb.StrandId == strandId)
            .OrderBy(sb => sb.SortKey)
            .ToListAsync(ct);
        var pos = siblings.FindIndex(sb => sb.BeatId == beatId);
        if (pos < 0) throw new InvalidOperationException($"Beat {beatId} not in strand {strandId}.");
        var prevSk = siblings[pos].SortKey;
        var nextSk = pos + 1 < siblings.Count ? siblings[pos + 1].SortKey : prevSk + 100.0;

        // Shrink target.
        target.Text          = firstHalf;
        target.TextHash      = ComputeTextHash(firstHalf);
        target.WasCorrected  = true;
        target.Stale         = true;
        InvalidateAudioOnBeat(target);
        target.UpdatedAt     = DateTime.UtcNow;

        // Add second-half beat.
        var second = new Beat
        {
            Id            = Guid.CreateVersion7(),
            Text          = secondHalf,
            TextHash      = ComputeTextHash(secondHalf),
            SceneType     = target.SceneType,
            FacetTag      = target.FacetTag,
            EmotionalTone = target.EmotionalTone,
            PaceHint      = target.PaceHint,
            Act           = target.Act,
            StructureRole = target.StructureRole,
            WasCorrected  = true,
        };
        db.Beats.Add(second);
        db.StrandBeats.Add(new StrandBeat
        {
            StrandId = strandId,
            BeatId   = second.Id,
            SortKey  = (prevSk + nextSk) / 2.0,
        });
        await db.SaveChangesAsync(ct);
        log.LogInformation("Split beat {BeatId} → ({First}|{Second}) in strand {StrandId}", beatId, firstHalf.Length, secondHalf.Length, strandId);
        return second;
    }

    /// <summary>Burst one oversized beat into N beats — one per paragraph.
    /// Splits on blank lines (matches the prose convention used everywhere
    /// else in the engine); falls back to single newlines when an entire
    /// chapter was pasted without blank-line separators. The first paragraph
    /// stays in the original beat; paragraphs 2..N become new beats slotted
    /// into <paramref name="strandId"/> between the original's SortKey and
    /// the next sibling's. All resulting beats have audio invalidated and
    /// <see cref="Beat.Stale"/>=true. No-ops (returns empty) if the beat is
    /// already a single paragraph.
    ///
    /// Per-strand by design: a beat shared across multiple strands would
    /// only have its new siblings appear in this strand. Callers running a
    /// bulk migration over old books should pre-filter to non-shared beats.</summary>
    public async Task<List<Guid>> SplitBeatByParagraphsAsync(Guid strandId, Guid beatId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var target = await db.Beats.FirstOrDefaultAsync(b => b.Id == beatId, ct)
            ?? throw new InvalidOperationException($"Beat {beatId} not found.");

        var paragraphs = SplitIntoParagraphs(target.Text ?? "");
        if (paragraphs.Count < 2) return new List<Guid>();

        var siblings = await db.StrandBeats
            .Where(sb => sb.StrandId == strandId)
            .OrderBy(sb => sb.SortKey)
            .ToListAsync(ct);
        var pos = siblings.FindIndex(sb => sb.BeatId == beatId);
        if (pos < 0) throw new InvalidOperationException($"Beat {beatId} not in strand {strandId}.");
        var prevSk = siblings[pos].SortKey;
        var nextSk = pos + 1 < siblings.Count ? siblings[pos + 1].SortKey : prevSk + 100.0;

        // First paragraph stays in target.
        target.Text         = paragraphs[0];
        target.TextHash     = ComputeTextHash(paragraphs[0]);
        target.WasCorrected = true;
        target.Stale        = true;
        InvalidateAudioOnBeat(target);
        target.UpdatedAt    = DateTime.UtcNow;

        // Paragraphs 2..N → new beats. Evenly stride between prevSk and nextSk
        // so each new beat slots between the previous one and the next sibling.
        // N paragraphs means N-1 new beats; stride = gap / N gives clean spacing.
        var newIds = new List<Guid>(paragraphs.Count - 1);
        double stride = (nextSk - prevSk) / paragraphs.Count;
        for (int i = 1; i < paragraphs.Count; i++)
        {
            var b = new Beat
            {
                Id            = Guid.CreateVersion7(),
                Text          = paragraphs[i],
                TextHash      = ComputeTextHash(paragraphs[i]),
                SceneType     = target.SceneType,
                FacetTag      = target.FacetTag,
                EmotionalTone = target.EmotionalTone,
                PaceHint      = target.PaceHint,
                Act           = target.Act,
                StructureRole = target.StructureRole,
                WasCorrected  = true,
                Stale         = true,
            };
            db.Beats.Add(b);
            db.StrandBeats.Add(new StrandBeat
            {
                StrandId = strandId,
                BeatId   = b.Id,
                SortKey  = prevSk + stride * i,
            });
            newIds.Add(b.Id);
        }

        await db.SaveChangesAsync(ct);
        log.LogInformation("Burst beat {Beat} into {N} paragraphs in strand {Strand}", beatId, paragraphs.Count, strandId);
        return newIds;
    }

    private static readonly Regex BlankLineSplit = new(@"\r?\n\s*\r?\n+", RegexOptions.Compiled);

    /// <summary>Split prose into paragraphs. Prefers blank-line separators;
    /// falls back to single newlines when the source was pasted as a wall
    /// of single-newline-delimited paragraphs (common in old book imports).
    /// Returns the original text as a single-element list if neither pattern
    /// applies — the caller treats that as "nothing to split."</summary>
    public static List<string> SplitIntoParagraphs(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return new List<string>();
        var byBlank = BlankLineSplit.Split(text)
            .Select(p => p.Trim())
            .Where(p => p.Length > 0)
            .ToList();
        if (byBlank.Count > 1) return byBlank;

        var byNewline = text.Split('\n')
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .ToList();
        if (byNewline.Count > 1) return byNewline;

        return new List<string> { text.Trim() };
    }

    /// <summary>Merge this beat's text into the previous beat in the strand
    /// (joined by a space), then remove this beat from the strand. The
    /// survivor's audio is invalidated because the text grew; the now-empty
    /// beat row is removed if no other strand references it.</summary>
    public async Task JoinBeatWithPreviousAsync(Guid strandId, Guid beatId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var siblings = await db.StrandBeats
            .Where(sb => sb.StrandId == strandId)
            .OrderBy(sb => sb.SortKey)
            .ToListAsync(ct);
        var pos = siblings.FindIndex(sb => sb.BeatId == beatId);
        if (pos < 0) throw new InvalidOperationException($"Beat {beatId} not in strand {strandId}.");
        if (pos == 0) throw new InvalidOperationException("First beat has no predecessor to join with.");

        var prevId = siblings[pos - 1].BeatId;
        var prev = await db.Beats.FirstAsync(b => b.Id == prevId, ct);
        var target = await db.Beats.FirstAsync(b => b.Id == beatId, ct);

        prev.Text         = string.Concat((prev.Text ?? "").TrimEnd(), " ", (target.Text ?? "").TrimStart()).Trim();
        prev.TextHash     = ComputeTextHash(prev.Text);
        prev.WasCorrected = true;
        prev.Stale        = true;
        InvalidateAudioOnBeat(prev);
        prev.UpdatedAt    = DateTime.UtcNow;

        // Drop the merged junction.
        db.StrandBeats.Remove(siblings[pos]);

        // Delete the absorbed beat row if no other strand still holds it.
        var otherMemberships = await db.StrandBeats
            .Where(sb => sb.BeatId == beatId && sb.StrandId != strandId)
            .AnyAsync(ct);
        if (!otherMemberships)
        {
            InvalidateAudioOnBeat(target);
            db.Beats.Remove(target);
        }
        await db.SaveChangesAsync(ct);
        log.LogInformation("Joined beat {Beat} into {Prev} in strand {Strand}", beatId, prevId, strandId);
    }

    /// <summary>Remove a beat from a strand. If the beat is not in any other
    /// strand, delete it entirely (and its audio file). Otherwise leave the
    /// Beat row alone — other strands still reference it.</summary>
    public async Task DeleteBeatAsync(Guid strandId, Guid beatId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var junction = await db.StrandBeats
            .FirstOrDefaultAsync(sb => sb.StrandId == strandId && sb.BeatId == beatId, ct);
        if (junction == null) return;
        db.StrandBeats.Remove(junction);

        var otherMemberships = await db.StrandBeats
            .Where(sb => sb.BeatId == beatId && sb.StrandId != strandId)
            .AnyAsync(ct);
        if (!otherMemberships)
        {
            var beat = await db.Beats.FirstAsync(b => b.Id == beatId, ct);
            InvalidateAudioOnBeat(beat);
            db.Beats.Remove(beat);
        }
        await db.SaveChangesAsync(ct);
    }

    // ── Audio ────────────────────────────────────────────────────────────

    /// <summary>Re-fire narration on every beat in this strand (and its
    /// children) that's missing an audio file. Stitches request-ids across
    /// adjacent beats for prosodic continuity. Cancellation supported via
    /// <see cref="CancelNarration"/>.</summary>
    public async Task NarrateAsync(Guid strandId, CancellationToken ct = default)
    {
        // 1. Validate config BEFORE mutating strand state — a misconfigured
        //    account shouldn't leave the strand stuck in narrating.
        if (!await tts.IsConfiguredAsync())
            throw new InvalidOperationException("TTS is not configured. Set ElevenLabs API key in Settings.");

        // 2. If a prior narration is already running for this strand, cancel
        //    it before starting a new one. Otherwise the old loop keeps
        //    writing audio files for stale beat text alongside the new run.
        if (cancelTokens.TryGetValue(strandId, out var prior))
        {
            try { prior.Cancel(); } catch { /* prior may already be disposed */ }
            // Give the old loop a beat to roll up and persist its cancelled status.
            await Task.Delay(50, ct);
        }

        using var cancelCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cancelTokens[strandId] = cancelCts;
        ct = cancelCts.Token;

        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            var strand = await db.Strands.FirstOrDefaultAsync(s => s.Id == strandId, ct)
                ?? throw new InvalidOperationException($"Strand {strandId} not found.");
            strand.Status = "narrating";
            await db.SaveChangesAsync(ct);

            var ordered = await GetOrderedBeatsAsync(strandId, ct);
            var outDir = Path.Combine(GetStrandRoot(strand.Slug), "audio");
            Directory.CreateDirectory(outDir);

            // Lock the voice for the whole run. If the UI's voice picker
            // changes mid-narration, the change applies to the NEXT run,
            // not this one — otherwise a single strand would render in two
            // voices. Beats with their own VoiceId still override (rare,
            // future use: per-character voices).
            var lockedStrandVoice = strand.VoiceId;
            bool useLossless = true;

            for (int idx = 0; idx < ordered.Count; idx++)
            {
                ct.ThrowIfCancellationRequested();
                var beat = ordered[idx].Beat;
                if (!string.IsNullOrEmpty(beat.AudioPath)) continue;

                // Per-beat stitch context: the up-to-3 most-recent in-memory
                // LastRequestIds from BEATS THAT COME BEFORE this one. This
                // is what makes a single-beat re-record in the middle of a
                // strand sound continuous with its neighbours, instead of
                // pulling from the strand's tail.
                var prevIds = new List<string>(3);
                for (int j = idx - 1; j >= 0 && prevIds.Count < 3; j--)
                {
                    var rid = ordered[j].Beat.LastRequestId;
                    if (!string.IsNullOrEmpty(rid)) prevIds.Insert(0, rid);
                }

                var (prevText, nextText) = BuildTextWindow(ordered, idx, contextChars: 1500);
                var tracked = await db.Beats.FirstAsync(b => b.Id == beat.Id, ct);

                // Pick the voice: beat override → strand lock → tts default
                // (resolved inside the TTS service). Lock takes precedence
                // even if strand.VoiceId was mutated mid-run.
                var voiceForBeat = !string.IsNullOrEmpty(tracked.VoiceId) ? tracked.VoiceId : lockedStrandVoice;

                // Map beat metadata → ElevenLabs prompt + per-request voice_settings.
                // Tag injection only happens when the model is v3-class AND
                // the global toggle is on; otherwise we still get the
                // voice_settings tuning from EmotionalTone/PaceHint.
                var modelId         = settings?.TtsModel ?? "eleven_v3";
                var tagsEnabled     = settings?.TtsUseAudioTags ?? true;
                var baseStability   = settings?.TtsStability ?? 0.5;
                var baseSimilarity  = settings?.TtsSimilarityBoost ?? 0.75;
                var baseStyle       = settings?.TtsStyle ?? 0.0;
                var prompt = BeatPromptBuilder.Build(tracked, modelId, tagsEnabled,
                    baseStability, baseSimilarity, baseStyle);

                string? newReqId = null;
                try
                {
                    if (useLossless)
                    {
                        try
                        {
                            newReqId = await SynthesizeAsLosslessWavAsync(tracked, strand, outDir, prevIds.ToArray(), prevText, nextText, voiceForBeat, prompt, ct);
                        }
                        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
                        {
                            log.LogWarning("Strand {S}: pcm_44100 forbidden — falling back to mp3", strand.Slug);
                            useLossless = false;
                            newReqId = await SynthesizeAsMp3Async(tracked, strand, outDir, prevIds.ToArray(), prevText, nextText, voiceForBeat, prompt, ct);
                        }
                    }
                    else
                    {
                        newReqId = await SynthesizeAsMp3Async(tracked, strand, outDir, prevIds.ToArray(), prevText, nextText, voiceForBeat, prompt, ct);
                    }
                    // Update the in-memory snapshot so the next iteration's
                    // backward look sees the just-stamped id without an
                    // extra DB round-trip.
                    if (!string.IsNullOrEmpty(newReqId))
                        ordered[idx].Beat.LastRequestId = newReqId;
                    strand.CharsNarrated += tracked.Text.Length;
                    await db.SaveChangesAsync(ct);
                }
                catch (Exception ex)
                {
                    log.LogError(ex, "Narration failed on strand {S} beat {B}", strandId, beat.Id);
                    strand.Status = "failed";
                    strand.Error = $"Beat {beat.Id}: {ex.Message}";
                    await db.SaveChangesAsync(ct);
                    throw;
                }
            }

            strand.Status = "ready";
            strand.AudioCompletedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);

            try { await ExportCombinedAsync(strandId, ct); }
            catch (Exception ex) { log.LogWarning(ex, "Strand {S} combined export failed (non-fatal)", strandId); }
        }
        catch (OperationCanceledException)
        {
            log.LogInformation("Strand {S} narration cancelled", strandId);
            await using var db2 = await dbFactory.CreateDbContextAsync(CancellationToken.None);
            var st = await db2.Strands.FirstOrDefaultAsync(s => s.Id == strandId, CancellationToken.None);
            if (st != null) { st.Status = "stopped"; await db2.SaveChangesAsync(CancellationToken.None); }
        }
        finally
        {
            cancelTokens.TryRemove(strandId, out _);
        }
    }

    public bool CancelNarration(Guid strandId)
    {
        if (cancelTokens.TryGetValue(strandId, out var cts))
        {
            try { cts.Cancel(); return true; } catch { return false; }
        }
        return false;
    }

    /// <summary>Concatenate every beat's audio (in reading order, recursively
    /// across child strands) into one WAV or MP3 at
    /// <c>engine/strands/{slug}/strand.wav|mp3</c>.</summary>
    public async Task<string?> ExportCombinedAsync(Guid strandId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var strand = await db.Strands.FirstOrDefaultAsync(s => s.Id == strandId, ct)
            ?? throw new InvalidOperationException($"Strand {strandId} not found.");

        var ordered = (await GetOrderedBeatsAsync(strandId, ct))
            .Where(o => !string.IsNullOrEmpty(o.Beat.AudioPath))
            .ToList();
        if (ordered.Count == 0)
        {
            log.LogWarning("Strand {S} has no narrated beats to combine", strandId);
            return null;
        }
        bool allWav = ordered.All(o => o.Beat.AudioPath!.EndsWith(".wav", StringComparison.OrdinalIgnoreCase));
        bool allMp3 = ordered.All(o => o.Beat.AudioPath!.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase));
        if (!allWav && !allMp3)
        {
            log.LogInformation("Strand {S} has mixed-format beats; skipping combined audio", strandId);
            return null;
        }

        var dir = GetStrandRoot(strand.Slug);
        Directory.CreateDirectory(dir);
        var ext = allWav ? "wav" : "mp3";
        var outPath = Path.Combine(dir, $"strand.{ext}");

        if (allWav)
        {
            var pcmParts = new List<byte[]>();
            foreach (var o in ordered)
            {
                ct.ThrowIfCancellationRequested();
                var full = ResolveAudioFile(o.Beat.AudioPath!);
                if (!File.Exists(full)) continue;
                var bytes = await File.ReadAllBytesAsync(full, ct);
                if (bytes.Length <= 44) continue;
                pcmParts.Add(bytes[44..]);
            }
            var total = pcmParts.Sum(p => p.Length);
            var all = new byte[total];
            int off = 0;
            foreach (var p in pcmParts) { Buffer.BlockCopy(p, 0, all, off, p.Length); off += p.Length; }
            var wav = EpisodeAudioService.WrapPcmAsWav(all, 44100, 1, 16);
            await File.WriteAllBytesAsync(outPath, wav, ct);
        }
        else
        {
            await using var outFs = File.Create(outPath);
            foreach (var o in ordered)
            {
                ct.ThrowIfCancellationRequested();
                var full = ResolveAudioFile(o.Beat.AudioPath!);
                if (!File.Exists(full)) continue;
                var bytes = await File.ReadAllBytesAsync(full, ct);
                await outFs.WriteAsync(bytes, ct);
            }
        }

        strand.CombinedAudioPath = $"{strand.Slug}/strand.{ext}";
        await db.SaveChangesAsync(ct);
        log.LogInformation("Strand {S} combined audio written to {Path}", strandId, outPath);
        return outPath;
    }

    /// <summary>Drop a single beat's audio (file + db fields) so the next
    /// narration pass re-records it. Use for "re-record this beat".</summary>
    public async Task InvalidateBeatAudioAsync(Guid beatId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var beat = await db.Beats.FirstOrDefaultAsync(b => b.Id == beatId, ct);
        if (beat == null) return;
        InvalidateAudioOnBeat(beat);
        beat.Stale = true;
        beat.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    // ── Paths / helpers ──────────────────────────────────────────────────

    public string GetAudioRoot() => Path.Combine(paths.DataRoot, "engine", "strands");
    public string GetStrandRoot(string slug) => Path.Combine(paths.DataRoot, "engine", "strands", slug);

    /// <summary>Resolve a relative audio path to an absolute file path. Tries
    /// the new strands root first; on miss, falls back to the legacy
    /// engine/episodes root so migrated content keeps playing without
    /// physically moving files. Returns the strands-root path even when no
    /// file exists at either location — callers check <see cref="File.Exists"/>
    /// and 404 from there.</summary>
    public string ResolveAudioFile(string relativePath)
    {
        var rel = relativePath.Replace('/', Path.DirectorySeparatorChar);
        var primary = Path.Combine(GetAudioRoot(), rel);
        if (File.Exists(primary)) return primary;
        var legacy = Path.Combine(paths.DataRoot, "engine", "episodes", rel);
        return File.Exists(legacy) ? legacy : primary;
    }

    public static string ComputeTextHash(string text)
    {
        var normalized = (text ?? "").Trim();
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private void InvalidateAudioOnBeat(Beat beat)
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
        beat.AudioPath    = null;
        beat.NarratedAt   = null;
        beat.DurationSec  = null;
        beat.LastRequestId = null;
    }

    private static int FindSentenceSplit(string text)
    {
        int mid = text.Length / 2;
        int radius = Math.Max(80, text.Length / 3);
        for (int offset = 0; offset <= radius; offset++)
        {
            foreach (var dir in new[] { -1, +1 })
            {
                int i = mid + offset * dir;
                if (i < 1 || i >= text.Length - 1) continue;
                char c = text[i];
                if ((c == '.' || c == '!' || c == '?') && (i + 1 < text.Length && char.IsWhiteSpace(text[i + 1])))
                    return i + 1;
            }
        }
        return mid;
    }

    private static (string? prev, string? next) BuildTextWindow(List<OrderedBeat> ordered, int targetIndex, int contextChars)
    {
        string? prev = null, next = null;
        var prevBuf = new StringBuilder();
        var nextBuf = new StringBuilder();
        var prevParts = new List<string>();
        for (int i = targetIndex - 1; i >= 0; i--)
        {
            var t = ordered[i].Beat.Text;
            if (string.IsNullOrEmpty(t)) continue;
            if (prevBuf.Length + t.Length > contextChars) break;
            prevBuf.Append(t).Append('\n');
            prevParts.Insert(0, t);
        }
        if (prevParts.Count > 0) prev = string.Join("\n\n", prevParts);

        for (int i = targetIndex + 1; i < ordered.Count; i++)
        {
            var t = ordered[i].Beat.Text;
            if (string.IsNullOrEmpty(t)) continue;
            if (nextBuf.Length + t.Length > contextChars) break;
            nextBuf.Append(t).Append('\n');
        }
        if (nextBuf.Length > 0) next = nextBuf.ToString().TrimEnd();
        return (prev, next);
    }

    private async Task<string?> SynthesizeAsLosslessWavAsync(
        Beat beat, Strand strand, string outDir,
        string[] previousRequestIds, string? previousText, string? nextText,
        string? voiceForBeat, BeatPrompt prompt,
        CancellationToken ct)
    {
        var voiceSettings = new TtsVoiceSettings(prompt.Stability, prompt.SimilarityBoost, prompt.Style);
        var result = await tts.SynthesizeWithIdAsync(
            prompt.Text, voiceForBeat, outputFormat: "pcm_44100",
            previousRequestIds: previousRequestIds,
            previousText: previousText, nextText: nextText,
            voiceSettings: voiceSettings, ct);

        var wav = EpisodeAudioService.WrapPcmAsWav(result.Bytes, 44100, 1, 16);
        var fileName = $"{beat.Id:N}.wav";
        await File.WriteAllBytesAsync(Path.Combine(outDir, fileName), wav, ct);

        beat.AudioPath     = $"{strand.Slug}/audio/{fileName}";
        beat.NarratedAt    = DateTime.UtcNow;
        beat.DurationSec   = result.Bytes.Length / 88200.0;
        beat.TextHash      = ComputeTextHash(beat.Text);
        beat.LastRequestId = result.RequestId;
        beat.Stale         = false;
        return result.RequestId;
    }

    private async Task<string?> SynthesizeAsMp3Async(
        Beat beat, Strand strand, string outDir,
        string[] previousRequestIds, string? previousText, string? nextText,
        string? voiceForBeat, BeatPrompt prompt,
        CancellationToken ct)
    {
        var voiceSettings = new TtsVoiceSettings(prompt.Stability, prompt.SimilarityBoost, prompt.Style);
        var result = await tts.SynthesizeWithIdAsync(
            prompt.Text, voiceForBeat, outputFormat: "mp3_44100_128",
            previousRequestIds: previousRequestIds,
            previousText: previousText, nextText: nextText,
            voiceSettings: voiceSettings, ct);

        var fileName = $"{beat.Id:N}.mp3";
        await File.WriteAllBytesAsync(Path.Combine(outDir, fileName), result.Bytes, ct);

        beat.AudioPath     = $"{strand.Slug}/audio/{fileName}";
        beat.NarratedAt    = DateTime.UtcNow;
        beat.DurationSec   = Math.Max(1.0, beat.Text.Length / 15.0);
        beat.TextHash      = ComputeTextHash(beat.Text);
        beat.LastRequestId = result.RequestId;
        beat.Stale         = false;
        return result.RequestId;
    }

    /// <summary>A beat in reading-order context. Carries the parent strand id
    /// so multi-level UIs can group beats by source.</summary>
    public record OrderedBeat(Beat Beat, Guid StrandId, double SortKey);

    /// <summary>The fields the UI's per-beat "details" panel can edit. None
    /// of these touch prose or audio — they just steer the narration's
    /// tone the next time the beat is re-recorded.</summary>
    public record BeatMetadataUpdate(
        string? BeatTitle,
        string? Synopsis,
        string? EmotionalTone,
        string? PaceHint,
        string? FacetTag,
        string? StructureRole,
        int Act,
        string? SceneType);

    /// <summary>Map a Strand.Status value to a Bootstrap chip color name.
    /// Single source of truth — used by /strand/{id}, /strands, any
    /// future strand-aware view. Keeps colors consistent so the user
    /// learns one visual language.</summary>
    public static string StatusColor(string status) => status switch
    {
        "ready"           => "success",
        "ready_for_audio" => "info",
        "narrating"       => "primary",
        "generating"      => "primary",
        "failed"          => "danger",
        "stopped"         => "warning",
        _                 => "secondary",
    };

    /// <summary>Human-readable rendering of a Strand.Status value. Underscores
    /// become spaces; status names are kept lowercase so the badge's
    /// text-uppercase CSS gives them a consistent look. Single helper so
    /// both /strands and /strand/{id} render statuses identically.</summary>
    public static string StatusLabel(string status) =>
        string.IsNullOrEmpty(status) ? "draft" : status.Replace('_', ' ');
}
