using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
    private readonly IAudioStore audioStore;
    private readonly SettingsService? settings;
    private readonly ILogger<StrandWorkbenchService> log;
    private static readonly ConcurrentDictionary<Guid, CancellationTokenSource> cancelTokens = new();

    public StrandWorkbenchService(
        IDbContextFactory<StreetSamuraiDbContext> dbFactory,
        ElevenLabsTtsService tts,
        IPathProvider paths,
        IAudioStore audioStore,
        ILogger<StrandWorkbenchService> log,
        SettingsService? settings = null)
    {
        this.dbFactory = dbFactory;
        this.tts = tts;
        this.paths = paths;
        this.audioStore = audioStore;
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
        beat.BeatTitle      = string.IsNullOrWhiteSpace(update.BeatTitle)     ? null : update.BeatTitle.Trim();
        beat.Synopsis       = string.IsNullOrWhiteSpace(update.Synopsis)      ? null : update.Synopsis.Trim();
        beat.EmotionalTone  = string.IsNullOrWhiteSpace(update.EmotionalTone) ? null : update.EmotionalTone.Trim().ToLowerInvariant();
        beat.PaceHint       = string.IsNullOrWhiteSpace(update.PaceHint)      ? null : update.PaceHint.Trim().ToLowerInvariant();
        beat.FacetTag       = string.IsNullOrWhiteSpace(update.FacetTag)      ? null : update.FacetTag.Trim().ToUpperInvariant();
        beat.StructureRole  = string.IsNullOrWhiteSpace(update.StructureRole) ? null : update.StructureRole.Trim();
        beat.Act            = update.Act;
        beat.SceneType      = string.IsNullOrWhiteSpace(update.SceneType)     ? "scene" : update.SceneType.Trim();
        beat.IsChapterStart = update.IsChapterStart;
        beat.Kind           = string.IsNullOrWhiteSpace(update.Kind)          ? "prose" : update.Kind.Trim().ToLowerInvariant();
        beat.UpdatedAt      = DateTime.UtcNow;
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

        // Auto-restripe before the gap shrinks into IEEE-754 territory. After
        // restripe the targets get fresh 100-step spacing; recompute prevSk
        // and nextSk against the new ladder. Cheap (O(N) one-time) and only
        // triggers after many midpoint inserts between the same two siblings.
        if (nextSk - prevSk < MinSortKeyGap)
        {
            await RestripeSortKeysAsync(strandId, ct);
            // Restripe ran on its own DbContext and committed fresh SortKeys.
            // Our local `db` still has the old StrandBeat instances tracked
            // with their pre-restripe values — a re-query would return those
            // same tracked instances (EF identity resolution), not the new
            // DB values. Detach so the next ToListAsync materialises fresh
            // rows with the post-restripe ladder.
            db.ChangeTracker.Clear();
            ordered = await db.StrandBeats
                .Where(sb => sb.StrandId == strandId)
                .OrderBy(sb => sb.SortKey)
                .ToListAsync(ct);
            if (afterBeatId == null)
            {
                prevSk = ordered.Count > 0 ? ordered[0].SortKey - 100.0 : 0.0;
                nextSk = ordered.Count > 0 ? ordered[0].SortKey         : 100.0;
            }
            else
            {
                var pos = ordered.FindIndex(sb => sb.BeatId == afterBeatId.Value);
                prevSk = ordered[pos].SortKey;
                nextSk = pos + 1 < ordered.Count ? ordered[pos + 1].SortKey : prevSk + 100.0;
            }
        }

        var beat = new Beat
        {
            Id           = Guid.CreateVersion7(),
            Number       = await NextBeatNumberAsync(db, ct),
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

    /// <summary>Below this fractional-SortKey gap, an insert/move would
    /// halve the spacing into IEEE-754 territory where subsequent midpoints
    /// stop producing strictly-ordered values. When InsertBeat/MoveBeat
    /// would push below this, we restripe the whole strand first so the
    /// new insertion has clean breathing room. 0.001 is empirically safe
    /// across thousands of subdivisions on a 100-step initial spacing.</summary>
    private const double MinSortKeyGap = 0.001;

    /// <summary>Rewrite every <see cref="StrandBeat.SortKey"/> in this strand
    /// to a fresh 100-step ladder (100, 200, 300, …). Preserves the current
    /// reading order. O(N) and runs in a single transaction. Audio stays
    /// valid — only the junction's SortKey changes.</summary>
    public async Task<int> RestripeSortKeysAsync(Guid strandId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var siblings = await db.StrandBeats
            .Where(sb => sb.StrandId == strandId)
            .OrderBy(sb => sb.SortKey)
            .ToListAsync(ct);
        if (siblings.Count == 0) return 0;
        double sk = 100.0;
        foreach (var sb in siblings)
        {
            sb.SortKey = sk;
            sk += 100.0;
        }
        await db.SaveChangesAsync(ct);
        log.LogInformation("Restriped {N} StrandBeat rows in strand {Strand}", siblings.Count, strandId);
        return siblings.Count;
    }

    /// <summary>Re-slot a beat within its strand. Pass <paramref name="afterBeatId"/>=null
    /// to move to the very top; otherwise the beat lands directly after that
    /// sibling. Uses fractional SortKey midpoints so no neighbouring rows need
    /// to be touched. Audio is preserved — only the membership SortKey changes,
    /// the beat's prose and recording stay valid.
    ///
    /// No-op when the beat is already in the requested position. Throws when
    /// the beat is not a member of the strand or when <paramref name="afterBeatId"/>
    /// refers to the beat being moved (would create a self-loop).</summary>
    public async Task MoveBeatAsync(Guid strandId, Guid beatId, Guid? afterBeatId, CancellationToken ct = default)
    {
        if (afterBeatId == beatId)
            throw new InvalidOperationException("Cannot move a beat to a position after itself.");

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var siblings = await db.StrandBeats
            .Where(sb => sb.StrandId == strandId)
            .OrderBy(sb => sb.SortKey)
            .ToListAsync(ct);
        var subject = siblings.FirstOrDefault(sb => sb.BeatId == beatId)
            ?? throw new InvalidOperationException($"Beat {beatId} not in strand {strandId}.");

        var others = siblings.Where(sb => sb.BeatId != beatId).ToList();
        double prevSk, nextSk;
        if (afterBeatId == null)
        {
            prevSk = others.Count > 0 ? others[0].SortKey - 100.0 : 0.0;
            nextSk = others.Count > 0 ? others[0].SortKey         : 100.0;
        }
        else
        {
            var pos = others.FindIndex(sb => sb.BeatId == afterBeatId.Value);
            if (pos < 0) throw new InvalidOperationException($"Anchor beat {afterBeatId} not in strand {strandId}.");
            prevSk = others[pos].SortKey;
            nextSk = pos + 1 < others.Count ? others[pos + 1].SortKey : prevSk + 100.0;
        }

        // Same precision guard as InsertBeatAsync — a move that targets a
        // gap below the threshold restripes first, then recomputes against
        // the fresh ladder.
        if (nextSk - prevSk < MinSortKeyGap)
        {
            await RestripeSortKeysAsync(strandId, ct);
            // Restripe used a separate DbContext; clear ours so the re-read
            // returns fresh post-restripe SortKeys, not the tracked stale
            // values from the first ToListAsync above.
            db.ChangeTracker.Clear();
            siblings = await db.StrandBeats
                .Where(sb => sb.StrandId == strandId)
                .OrderBy(sb => sb.SortKey)
                .ToListAsync(ct);
            subject = siblings.First(sb => sb.BeatId == beatId);
            others = siblings.Where(sb => sb.BeatId != beatId).ToList();
            if (afterBeatId == null)
            {
                prevSk = others.Count > 0 ? others[0].SortKey - 100.0 : 0.0;
                nextSk = others.Count > 0 ? others[0].SortKey         : 100.0;
            }
            else
            {
                var pos = others.FindIndex(sb => sb.BeatId == afterBeatId.Value);
                prevSk = others[pos].SortKey;
                nextSk = pos + 1 < others.Count ? others[pos + 1].SortKey : prevSk + 100.0;
            }
        }

        var newSortKey = (prevSk + nextSk) / 2.0;
        // No-op short-circuit: same SortKey ± 1e-9 means the move would land
        // exactly where the beat already is (drag onto self / drag onto the
        // immediately-preceding sibling).
        if (Math.Abs(newSortKey - subject.SortKey) < 1e-9) return;

        subject.SortKey = newSortKey;
        await db.SaveChangesAsync(ct);
        log.LogInformation("Moved beat {Beat} in strand {Strand} to SortKey {Sk} (after {After})",
            beatId, strandId, newSortKey, afterBeatId?.ToString() ?? "(top)");
    }

    /// <summary>Split a beat at an explicit character position — what the
    /// writer wants when their cursor is inside the prose. Same shape as
    /// <see cref="SplitBeatAsync"/> but skips the midpoint-search and uses
    /// the caller's split index directly. Snaps to the nearest word
    /// boundary so we never break a word in two.</summary>
    public async Task<Beat> SplitBeatAtAsync(Guid strandId, Guid beatId, int splitPosition, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var target = await db.Beats.FirstOrDefaultAsync(b => b.Id == beatId, ct)
            ?? throw new InvalidOperationException($"Beat {beatId} not found.");

        var text = target.Text ?? "";
        if (splitPosition <= 0 || splitPosition >= text.Length)
            throw new InvalidOperationException("Split position must land inside the prose, not at the start or end.");

        // Snap to a word boundary if the cursor landed mid-word — keeps
        // narration sane (we don't want the first half to end on a half-word).
        int snapped = splitPosition;
        if (!char.IsWhiteSpace(text[snapped - 1]) && !char.IsWhiteSpace(text[snapped]))
        {
            // Walk forward to the next space, capped by the rest of the text.
            int fwd = snapped;
            while (fwd < text.Length && !char.IsWhiteSpace(text[fwd])) fwd++;
            // Also walk backward.
            int bwd = snapped;
            while (bwd > 0 && !char.IsWhiteSpace(text[bwd - 1])) bwd--;
            // Pick whichever is closer to the original cursor.
            snapped = (snapped - bwd) <= (fwd - snapped) ? bwd : fwd;
        }

        var firstHalf  = text[..snapped].TrimEnd();
        var secondHalf = text[snapped..].TrimStart();
        if (firstHalf.Length == 0 || secondHalf.Length == 0)
            throw new InvalidOperationException("Split would leave one half empty — pick a different cursor position.");

        var siblings = await db.StrandBeats
            .Where(sb => sb.StrandId == strandId)
            .OrderBy(sb => sb.SortKey)
            .ToListAsync(ct);
        var pos = siblings.FindIndex(sb => sb.BeatId == beatId);
        if (pos < 0) throw new InvalidOperationException($"Beat {beatId} not in strand {strandId}.");
        var prevSk = siblings[pos].SortKey;
        var nextSk = pos + 1 < siblings.Count ? siblings[pos + 1].SortKey : prevSk + 100.0;

        target.Text         = firstHalf;
        target.TextHash     = ComputeTextHash(firstHalf);
        target.WasCorrected = true;
        target.Stale        = true;
        InvalidateAudioOnBeat(target);
        target.UpdatedAt    = DateTime.UtcNow;

        var second = new Beat
        {
            Id            = Guid.CreateVersion7(),
            Number        = await NextBeatNumberAsync(db, ct),
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
        log.LogInformation("Split beat {BeatId} at position {Pos} (snapped to {Snap}) → ({First}|{Second}) in strand {StrandId}",
            beatId, splitPosition, snapped, firstHalf.Length, secondHalf.Length, strandId);
        return second;
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
            Number        = await NextBeatNumberAsync(db, ct),
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
        // Pre-allocate a contiguous block of beat numbers in one round-trip
        // rather than calling MAX(Number)+1 inside the loop (which would
        // re-read uncommitted inserts and produce a sequence). Saves N-1
        // queries on big paragraph splits.
        var baseNumber = await NextBeatNumberAsync(db, ct);
        for (int i = 1; i < paragraphs.Count; i++)
        {
            var b = new Beat
            {
                Id            = Guid.CreateVersion7(),
                Number        = baseNumber + (i - 1),
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

    /// <summary>
    /// Take a chapter strand whose prose is sitting in the legacy
    /// <c>Chapter.Html</c> / <c>Chapter.Markdown</c> blob (because it was written
    /// before the Strand+Beat schema landed) and burst it into one Beat per
    /// paragraph, attached to the chapter strand via StrandBeat junctions.
    ///
    /// Idempotent: if the chapter strand already has any beats, returns 0 and
    /// leaves them alone. Parses Markdown-flavoured prose conventions:
    /// <list type="bullet">
    /// <item>First <c>#</c> chapter-title line is dropped (already on Strand.Title).</item>
    /// <item><c>*Protagonist: …*</c> front-matter line is dropped.</item>
    /// <item><c>## Section Heading</c> becomes the next paragraph beat's
    ///   <see cref="Beat.BeatTitle"/>, and the preceding paragraph beat's
    ///   <see cref="Beat.SceneType"/> is upgraded to <c>"section-end"</c>.</item>
    /// <item><c>---</c> scene breaks mark the preceding paragraph beat's
    ///   SceneType as <c>"scene-end"</c>.</item>
    /// </list>
    /// SceneType is consumed by the combined-audio export's silence pacer to
    /// drop longer gaps between sections and scenes than between mid-scene
    /// paragraphs.
    /// </summary>
    /// <returns>Beat count created. Zero means already populated, or the
    /// chapter has no body to materialise.</returns>
    /// <remarks>
    /// LEGACY MIGRATION ONLY. Reads from the retired Records.Json table —
    /// the project rule [NO new JSON files] supersedes that storage path
    /// for everything else. The only sanctioned caller is the standalone
    /// <c>v3/MaterializeChapters</c> one-shot tool. New runtime code paths
    /// (UI, MCP tools, narration loop, generation pipeline) must not call
    /// this; insert beats via <see cref="InsertBeatAsync"/> or
    /// <see cref="SplitBeatByParagraphsAsync"/> instead.
    /// </remarks>
    [Obsolete("Legacy Records.Json migration only — see v3/MaterializeChapters. Use InsertBeatAsync / SplitBeatByParagraphsAsync for new code paths.", error: false)]
    public async Task<int> MaterializeChapterFromHtmlAsync(Guid chapterStrandId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var strand = await db.Strands.FirstOrDefaultAsync(s => s.Id == chapterStrandId, ct)
            ?? throw new InvalidOperationException($"Strand {chapterStrandId} not found.");

        var existingCount = await db.StrandBeats.CountAsync(sb => sb.StrandId == chapterStrandId, ct);
        if (existingCount > 0)
        {
            log.LogInformation("Strand {S} ({T}) already has {N} beats; not materialising.",
                chapterStrandId, strand.Title, existingCount);
            return 0;
        }

        // The legacy Chapter blob is stored as a Records row hanging off the
        // matching Entity (same Guid). Pull the JSON directly so this method
        // doesn't take a dep on IChapterRepository.
        var recordJson = await db.Records.AsNoTracking()
            .Where(r => r.EntityId == chapterStrandId)
            .Select(r => r.Json)
            .FirstOrDefaultAsync(ct);
        if (string.IsNullOrWhiteSpace(recordJson))
        {
            log.LogWarning("Strand {S} ({T}): no Chapter record found in Records; skipping.",
                chapterStrandId, strand.Title);
            return 0;
        }

        Models.Chapter? chapter;
        try
        {
            chapter = JsonSerializer.Deserialize<Models.Chapter>(recordJson, ChapterJsonOpts);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Strand {S} ({T}): Chapter record JSON failed to deserialise; skipping.",
                chapterStrandId, strand.Title);
            return 0;
        }
        if (chapter == null) return 0;

        var body = !string.IsNullOrWhiteSpace(chapter.Markdown) ? chapter.Markdown : chapter.Html;
        if (string.IsNullOrWhiteSpace(body))
        {
            log.LogInformation("Strand {S} ({T}) has no prose body to materialise.",
                chapterStrandId, strand.Title);
            return 0;
        }

        var parsed = ParseChapterBodyIntoBeats(body);
        if (parsed.Count == 0)
        {
            log.LogInformation("Strand {S} ({T}) body produced zero paragraphs after parse.",
                chapterStrandId, strand.Title);
            return 0;
        }

        var now = DateTime.UtcNow;
        double sortKey = 100.0;
        // Pre-allocate the whole block of beat numbers once. Cheaper than
        // re-querying MAX(Number) per beat — and avoids racey reads against
        // the uncommitted inserts in our own transaction.
        var baseNumber = await NextBeatNumberAsync(db, ct);
        int numberOffset = 0;
        foreach (var pb in parsed)
        {
            var beat = new Beat
            {
                Id        = Guid.CreateVersion7(),
                Number    = baseNumber + numberOffset++,
                Text      = pb.Text,
                TextHash  = ComputeTextHash(pb.Text),
                BeatTitle = pb.BeatTitle,
                SceneType = pb.SceneType,
                CreatedAt = now,
                UpdatedAt = now,
            };
            db.Beats.Add(beat);
            db.StrandBeats.Add(new StrandBeat
            {
                StrandId = chapterStrandId,
                BeatId   = beat.Id,
                SortKey  = sortKey,
            });
            sortKey += 100.0;
        }
        await db.SaveChangesAsync(ct);
        log.LogInformation("Strand {S} ({T}): materialised {N} beats from chapter body.",
            chapterStrandId, strand.Title, parsed.Count);
        return parsed.Count;
    }

    private static readonly JsonSerializerOptions ChapterJsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private static readonly Regex ChapterBodyBlankLineSplit = new(@"\r?\n\s*\r?\n+", RegexOptions.Compiled);
    private static readonly Regex ChapterBodyProtagonistLine = new(@"^\s*\*Protagonist:\s*[^*]+\*\s*$", RegexOptions.Compiled);
    private static readonly Regex ChapterBodySceneBreak = new(@"^\s*(?:---+|\*\*\*+|[-*]\s*[-*]\s*[-*][-*\s]*)\s*$", RegexOptions.Compiled);

    private record ParsedBeat(string Text, string? BeatTitle, string SceneType);

    private static List<ParsedBeat> ParseChapterBodyIntoBeats(string body)
    {
        var blocks = ChapterBodyBlankLineSplit.Split(body)
            .Select(p => p.Trim())
            .Where(p => p.Length > 0)
            .ToList();

        var beats = new List<ParsedBeat>();
        string? pendingTitle = null;
        bool firstH1Skipped = false;

        foreach (var raw in blocks)
        {
            var firstLine = raw.Split('\n', 2)[0].Trim();

            // First H1 line is the chapter title — already on Strand.Title.
            if (!firstH1Skipped && firstLine.StartsWith("# ") && !firstLine.StartsWith("## "))
            {
                firstH1Skipped = true;
                continue;
            }

            // Protagonist marker — front matter, drop.
            if (ChapterBodyProtagonistLine.IsMatch(firstLine)) continue;

            // ## Section heading — capture for next beat's BeatTitle; mark
            // the prior beat as section-end so the silence pacer drops a
            // longer gap before the section opener.
            if (firstLine.StartsWith("## "))
            {
                pendingTitle = firstLine.Substring(3).Trim();
                if (beats.Count > 0)
                {
                    var prev = beats[^1];
                    if (prev.SceneType == "scene" || prev.SceneType == "scene-end")
                        beats[^1] = prev with { SceneType = "section-end" };
                }
                // If the block also carries body lines under the header, take
                // them as the section opener immediately so we don't lose them.
                var idx = raw.IndexOf('\n');
                if (idx > 0)
                {
                    var bodyText = raw[(idx + 1)..].Trim();
                    if (!string.IsNullOrEmpty(bodyText))
                    {
                        beats.Add(new ParsedBeat(bodyText, pendingTitle, "scene"));
                        pendingTitle = null;
                    }
                }
                continue;
            }

            // --- scene break — upgrade the prior beat to scene-end.
            if (ChapterBodySceneBreak.IsMatch(firstLine))
            {
                if (beats.Count > 0)
                {
                    var prev = beats[^1];
                    if (prev.SceneType == "scene")
                        beats[^1] = prev with { SceneType = "scene-end" };
                }
                continue;
            }

            // Regular paragraph block.
            var title = pendingTitle;
            pendingTitle = null;
            beats.Add(new ParsedBeat(raw, title, "scene"));
        }
        return beats;
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
            // Reset the per-run progress counters so the polling UI shows
            // current-run state, not a stale lifetime total. Stamp the
            // denominator from this snapshot so it stays stable even if
            // beats are added/removed mid-run.
            strand.NarratedBeatCount = 0;
            strand.TotalBeatsToNarrate = ordered.Count;
            await db.SaveChangesAsync(ct);
            // Audio bytes are written through IAudioStore — the synth helpers
            // hand the bytes to audioStore.WriteBeatAsync which knows where
            // they live (local disk vs blob). No filesystem prep needed here.

            // Resolve the active voice profile ONCE before the loop and reuse
            // it for every beat. Two reasons: (1) the default-profile lookup
            // is keyed on a string id stored in settings, so resolving once
            // means a mid-run settings change doesn't fork the strand into
            // two voices, (2) the profile's voice_id + voice_settings are a
            // bundle — using them together is the whole point of profiles
            // (otherwise sliders drift). Beats with their own VoiceId still
            // override (future per-character work).
            var activeProfile = settings?.GetDefaultVoiceProfile();
            var lockedStrandVoice = !string.IsNullOrEmpty(strand.VoiceId)
                ? strand.VoiceId
                : activeProfile?.VoiceId;
            bool useLossless = true;
            int failedCount = 0;
            var failedBeatIds = new List<Guid>();

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
                // The baseline voice_settings come from the active VoiceProfile
                // (so a profile change applies consistently to every beat in
                // the run). EmotionalTone / PaceHint nudges still adjust them
                // per beat for dramatic range, but they bias FROM the profile,
                // not from free-floating settings sliders.
                var modelId         = activeProfile?.Model              ?? settings?.TtsModel ?? "eleven_v3";
                var tagsEnabled     = settings?.TtsUseAudioTags         ?? true;
                var baseStability   = activeProfile?.Stability          ?? settings?.TtsStability ?? 0.5;
                var baseSimilarity  = activeProfile?.SimilarityBoost    ?? settings?.TtsSimilarityBoost ?? 0.75;
                var baseStyle       = activeProfile?.Style              ?? settings?.TtsStyle ?? 0.0;
                var prompt = BeatPromptBuilder.Build(tracked, modelId, tagsEnabled,
                    baseStability, baseSimilarity, baseStyle);

                string? newReqId = null;
                try
                {
                    if (useLossless)
                    {
                        try
                        {
                            newReqId = await SynthesizeAsLosslessWavAsync(tracked, strand, prevIds.ToArray(), prevText, nextText, voiceForBeat, prompt, ct);
                        }
                        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
                        {
                            log.LogWarning("Strand {S}: pcm_44100 forbidden — falling back to mp3", strand.Slug);
                            useLossless = false;
                            newReqId = await SynthesizeAsMp3Async(tracked, strand, prevIds.ToArray(), prevText, nextText, voiceForBeat, prompt, ct);
                        }
                    }
                    else
                    {
                        newReqId = await SynthesizeAsMp3Async(tracked, strand, prevIds.ToArray(), prevText, nextText, voiceForBeat, prompt, ct);
                    }
                    // Update the in-memory snapshot so the next iteration's
                    // backward look sees the just-stamped id without an
                    // extra DB round-trip.
                    if (!string.IsNullOrEmpty(newReqId))
                        ordered[idx].Beat.LastRequestId = newReqId;
                    strand.CharsNarrated += tracked.Text.Length;
                    // Bump the progress counter so the polling UI reads a
                    // single int instead of scanning the beats collection.
                    strand.NarratedBeatCount++;
                    await db.SaveChangesAsync(ct);
                }
                catch (OperationCanceledException)
                {
                    // Cancellation propagates — outer handler rolls the strand
                    // into "stopped". Don't count as a failure or eat the token.
                    throw;
                }
                catch (Exception ex)
                {
                    // Per-beat failure: log, record the message on the strand
                    // so the UI can surface it, and CONTINUE the loop. One bad
                    // beat (content filter, timeout, weird unicode) used to
                    // abort the whole strand and lock every later beat out of
                    // narration; now we keep going and report the partial
                    // result at the end.
                    failedCount++;
                    failedBeatIds.Add(beat.Id);
                    log.LogError(ex, "Narration failed on strand {S} beat {B} — skipping and continuing", strandId, beat.Id);
                    strand.Error = failedCount == 1
                        ? $"Beat {beat.Id}: {ex.Message}"
                        : $"{failedCount} beats failed (latest {beat.Id}): {ex.Message}";
                    await db.SaveChangesAsync(ct);
                }
            }

            // Strand outcome reflects the per-beat tally:
            //   all beats rendered → "ready"
            //   some beats failed  → "failed" (Error already populated above)
            // Either way AudioCompletedAt stamps so callers can see the run finished.
            strand.Status = failedCount == 0 ? "ready" : "failed";
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
        catch (Exception ex)
        {
            // Top-level failure (DB unreachable, TTS service constructor blew
            // up, anything else not caught per-beat). Without this catch the
            // exception would escape NarrateAsync — and every caller is
            // fire-and-forget Task.Run, so the strand would stay stuck in
            // "narrating" forever with no signal to the UI. Flip status to
            // "failed" with the exception message so the polling page can
            // recover.
            log.LogError(ex, "Strand {S} narration crashed at top level", strandId);
            try
            {
                await using var db2 = await dbFactory.CreateDbContextAsync(CancellationToken.None);
                var st = await db2.Strands.FirstOrDefaultAsync(s => s.Id == strandId, CancellationToken.None);
                if (st != null)
                {
                    st.Status = "failed";
                    st.Error = ex.Message;
                    st.AudioCompletedAt = DateTime.UtcNow;
                    await db2.SaveChangesAsync(CancellationToken.None);
                }
            }
            catch (Exception inner) { log.LogError(inner, "Strand {S} failed-status write also failed", strandId); }
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

        // Gap-after-beat is now a property of the upper beat
        // (Beat.GapAfterMs). Null = "use the auto-computed default" from
        // ComputeTrailingSilenceMs; a value (including 0) is an explicit
        // override the user set in the UI.

        var ext = allWav ? "wav" : "mp3";

        if (allWav)
        {
            // Stream straight to a temp WAV file so the strand's PCM never
            // sits in memory all at once. For a 100-beat strand at 44.1 kHz
            // mono 16-bit, the old List<byte[]> + contiguous-array pattern
            // pinned ~30-50 MB on the LOH twice (list + merged buffer +
            // header-wrap allocation), routinely OOMing the B1 worker. Now
            // each beat's bytes are written and released; the WAV header is
            // patched in after the data chunk size is known.
            var tmp = Path.Combine(Path.GetTempPath(), $"ss-combine-wav-{Guid.CreateVersion7():N}.wav");
            try
            {
                await using (var fs = new FileStream(tmp, FileMode.Create, FileAccess.ReadWrite, FileShare.None, 64 * 1024, useAsync: true))
                {
                    // Reserve 44 bytes for the header — written after we
                    // know the data chunk size.
                    fs.Position = 44;
                    long pcmTotal = 0;
                    for (int i = 0; i < ordered.Count; i++)
                    {
                        ct.ThrowIfCancellationRequested();
                        var o = ordered[i];
                        var bytes = await ReadAllAudioAsync(o.Beat.AudioPath!, ct);
                        if (bytes == null || bytes.Length <= 44) continue;
                        await fs.WriteAsync(bytes.AsMemory(44), ct);
                        pcmTotal += bytes.Length - 44;

                        if (i < ordered.Count - 1)
                        {
                            var next = ordered[i + 1].Beat;
                            var pauseMs = o.Beat.GapAfterMs ?? ComputeTrailingSilenceMs(o.Beat, next, settings);
                            if (pauseMs > 0)
                            {
                                var silence = GenerateSilencePcm(pauseMs, sampleRate: 44100, channels: 1, bitsPerSample: 16);
                                if (silence.Length > 0)
                                {
                                    await fs.WriteAsync(silence, ct);
                                    pcmTotal += silence.Length;
                                }
                            }
                        }
                    }
                    fs.Position = 0;
                    EpisodeAudioService.WriteWavHeader(fs, checked((int)pcmTotal), 44100, 1, 16);
                    fs.Position = 0;
                    await audioStore.WriteCombinedFromStreamAsync(strand.Slug, "wav", fs, ct);
                }
            }
            finally
            {
                try { File.Delete(tmp); } catch { /* best-effort */ }
            }
        }
        else
        {
            // MP3 concat with ffmpeg-injected silence. Strategy unchanged:
            // render one silence-MP3 per distinct pause length we need,
            // then ffmpeg concat demuxer with -c copy (no re-encode).
            //
            // Blob-backed deployments don't have local file paths, so we
            // stage every beat's MP3 to a temp dir first, run ffmpeg
            // against the staged copies, then upload the result through
            // the store. Local-disk just uses ResolveLocalPathAsync directly.
            var ffmpeg = ResolveFfmpegPath();
            if (string.IsNullOrEmpty(ffmpeg))
            {
                log.LogWarning("ffmpeg not found on PATH — falling back to naive MP3 concat without inter-beat silence pacing. Install ffmpeg to enable paced gaps.");
                using var ms = new MemoryStream();
                foreach (var o in ordered)
                {
                    ct.ThrowIfCancellationRequested();
                    var bytes = await ReadAllAudioAsync(o.Beat.AudioPath!, ct);
                    if (bytes == null) continue;
                    await ms.WriteAsync(bytes, ct);
                }
                await audioStore.WriteCombinedAsync(strand.Slug, "mp3", ms.ToArray(), ct);
            }
            else
            {
                int Pause(Beat a, Beat b) => a.GapAfterMs ?? ComputeTrailingSilenceMs(a, b, settings);
                // Stage every beat to a local temp file (no-op rename for
                // local-disk; download from blob otherwise), run ffmpeg
                // against a temp output, then hand the bytes to the store.
                var staged = new List<(OrderedBeat Source, string LocalPath)>(ordered.Count);
                var stagingDir = Path.Combine(Path.GetTempPath(), $"ss-combine-{Guid.CreateVersion7():N}");
                Directory.CreateDirectory(stagingDir);
                try
                {
                    foreach (var o in ordered)
                    {
                        ct.ThrowIfCancellationRequested();
                        var local = await audioStore.ResolveLocalPathAsync(o.Beat.AudioPath!, ct);
                        if (local == null)
                        {
                            var bytes = await ReadAllAudioAsync(o.Beat.AudioPath!, ct);
                            if (bytes == null) continue;
                            local = Path.Combine(stagingDir, $"{o.Beat.Id:N}.mp3");
                            await File.WriteAllBytesAsync(local, bytes, ct);
                        }
                        staged.Add((o, local));
                    }
                    var stagedOut = Path.Combine(stagingDir, "strand.mp3");
                    await ConcatMp3sWithSilenceAsync(ffmpeg, staged, stagedOut, Pause, ct);
                    var combined = await File.ReadAllBytesAsync(stagedOut, ct);
                    await audioStore.WriteCombinedAsync(strand.Slug, "mp3", combined, ct);
                }
                finally
                {
                    try { Directory.Delete(stagingDir, recursive: true); }
                    catch (Exception ex) { log.LogDebug(ex, "Could not clean up combine staging dir {Dir}", stagingDir); }
                }
            }
        }

        var combinedRel = $"{strand.Slug}/strand.{ext}";
        strand.CombinedAudioPath = combinedRel;
        await db.SaveChangesAsync(ct);
        log.LogInformation("Strand {S} combined audio written ({Rel})", strandId, combinedRel);
        return combinedRel;
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

    // Audio files MUST live under MutableDataDir, not DataRoot. On Azure App
    // Service, DataRoot is on the read-only deployment slot — writes there
    // either fail at runtime or get wiped on the next deploy. MutableDataDir
    // honours SS_MUTABLE_DATA_ROOT (set to D:\home\data\StreetSamurai on
    // Azure) so audio survives deploys and stays writable. On local dev with
    // no env var, MutableDataDir falls back to the same engine/data path as
    // before, so the dev experience doesn't change.
    public string GetAudioRoot() => Path.Combine(paths.MutableDataDir, "strands");
    public string GetStrandRoot(string slug) => Path.Combine(paths.MutableDataDir, "strands", slug);

    /// <summary>Resolve a relative audio path to an absolute file path. Tries
    /// the new MutableDataDir-rooted strands tree first, then falls back to
    /// (a) the pre-2026-05-24 strands location at <c>{DataRoot}/engine/strands/</c>
    /// and (b) the even older episode-era location at <c>{DataRoot}/engine/episodes/</c>.
    /// Files migrate forward as they're re-recorded; nothing physically moves
    /// from the legacy locations. Returns the primary path even when no file
    /// exists anywhere — callers check <see cref="File.Exists"/> and 404 from there.</summary>
    public string ResolveAudioFile(string relativePath)
    {
        var rel = relativePath.Replace('/', Path.DirectorySeparatorChar);
        var primary = Path.Combine(GetAudioRoot(), rel);
        if (File.Exists(primary)) return primary;
        var legacyStrands = Path.Combine(paths.DataRoot, "engine", "strands", rel);
        if (File.Exists(legacyStrands)) return legacyStrands;
        var legacyEpisodes = Path.Combine(paths.DataRoot, "engine", "episodes", rel);
        return File.Exists(legacyEpisodes) ? legacyEpisodes : primary;
    }

    public static string ComputeTextHash(string text)
    {
        var normalized = (text ?? "").Trim();
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    // ── Inter-beat silence (combined-audio export) ──────────────────────
    // Two helpers used by ExportCombinedAsync: ComputeTrailingSilenceMs
    // decides how much pause to insert after a beat, and GenerateSilencePcm
    // produces the raw little-endian PCM bytes for that many ms. Pause
    // length is a function of (a) SceneType (the parser-assigned label
    // describing whether this beat ends a scene or a section), (b) the
    // beat's terminating punctuation (a paragraph that ends mid-sentence
    // gets less gap than one that lands on '.'/'?'/'!'). Settings carry
    // the per-tier budgets so the user can adjust pacing globally.

    /// <summary>Pick the silence in milliseconds to insert after <paramref name="beat"/>
    /// and before <paramref name="next"/>. If <paramref name="settings"/> is null
    /// (test harness, MCP-only paths), defaults are 1800 / 1000 / 400 / 200.</summary>
    public static int ComputeTrailingSilenceMs(Beat beat, Beat? next, SettingsService? settings)
    {
        var sectionMs       = settings?.TtsPauseSectionMs      ?? 1800;
        var sceneMs         = settings?.TtsPauseSceneMs        ?? 1000;
        var paragraphMs     = settings?.TtsPauseParagraphMs    ?? 400;
        var continuationMs  = settings?.TtsPauseContinuationMs ?? 200;

        // SceneType is the strongest signal — set during chapter materialisation.
        switch (beat.SceneType?.ToLowerInvariant())
        {
            case "section-end": return sectionMs;
            case "scene-end":   return sceneMs;
        }

        // Otherwise fall back to terminator punctuation. Hard terminators
        // suggest the sentence finished cleanly; comma/em-dash/no-mark
        // suggest the prose continues into the next paragraph.
        var trimmed = (beat.Text ?? "").TrimEnd();
        // Walk back across trailing markdown emphasis markers so '**Likes me.**'
        // and '*__Likes me.__*' still read as '.' terminated. Strip * and _ only;
        // these are the four markers BeatFormatter renders.
        int tail = trimmed.Length - 1;
        while (tail >= 0 && (trimmed[tail] == '*' || trimmed[tail] == '_')) tail--;
        if (tail < 0) return continuationMs;
        var last = trimmed[tail];
        return last switch
        {
            '.' or '!' or '?' or '"' or '”' => paragraphMs,
            _                               => continuationMs,
        };
    }

    /// <summary>Generate <paramref name="ms"/> milliseconds of digital silence
    /// at the given PCM format. 16-bit signed PCM silence is just zero bytes,
    /// so this is a cheap allocation. Returns an empty array for ms ≤ 0.</summary>
    public static byte[] GenerateSilencePcm(int ms, int sampleRate, short channels, short bitsPerSample)
    {
        if (ms <= 0) return Array.Empty<byte>();
        long samples = (long)sampleRate * ms / 1000L;
        long bytes = samples * channels * (bitsPerSample / 8);
        return new byte[bytes];
    }

    /// <summary>Allocate the next globally-unique <see cref="Beat.Number"/>.
    /// Reads MAX+1 inside the active DbContext so it sees uncommitted inserts
    /// from this same transaction. The UNIQUE index on Beats.Number is the
    /// safety net — if two concurrent inserts pick the same number, one
    /// SaveChanges will fail with a duplicate-key error.</summary>
    private static async Task<int> NextBeatNumberAsync(StreetSamuraiDbContext db, CancellationToken ct)
    {
        var max = await db.Beats.MaxAsync(b => (int?)b.Number, ct) ?? 0;
        return max + 1;
    }

    // ── Gap-after-beat CRUD ─────────────────────────────────────────────
    // The gap that follows a beat lives on that beat: Beat.GapAfterMs is the
    // explicit override (null = "use the computed default from SceneType +
    // terminator punctuation"). These helpers let the UI set or clear the
    // override without exposing the column directly.

    /// <summary>Set an explicit silence-after-this-beat override. 0 means
    /// "no silence"; null callers should use <see cref="ClearGapAfterAsync"/>
    /// to revert to the auto-computed default.</summary>
    public async Task SetGapAfterAsync(Guid beatId, int durationMs, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var beat = await db.Beats.FirstOrDefaultAsync(b => b.Id == beatId, ct)
            ?? throw new InvalidOperationException($"Beat {beatId} not found.");
        beat.GapAfterMs = Math.Max(0, durationMs);
        beat.UpdatedAt  = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Clear the explicit override, letting the silence engine fall
    /// back to the computed default for that beat.</summary>
    public async Task ClearGapAfterAsync(Guid beatId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var beat = await db.Beats.FirstOrDefaultAsync(b => b.Id == beatId, ct);
        if (beat == null || beat.GapAfterMs == null) return;
        beat.GapAfterMs        = null;
        beat.GapAfterAudioPath = null;
        beat.UpdatedAt         = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Locate the ffmpeg executable on PATH. Returns the full path on
    /// success, or null when ffmpeg isn't installed. Used by the MP3 combined
    /// export path to inject precise digital silence between beats — the only
    /// way to do that cleanly in an MP3 stream without re-encoding the whole
    /// strand.</summary>
    private static string? ResolveFfmpegPath()
    {
        var name = OperatingSystem.IsWindows() ? "ffmpeg.exe" : "ffmpeg";
        var pathVar = Environment.GetEnvironmentVariable("PATH") ?? "";
        var sep = OperatingSystem.IsWindows() ? ';' : ':';
        foreach (var dir in pathVar.Split(sep, StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                var candidate = Path.Combine(dir, name);
                if (File.Exists(candidate)) return candidate;
            }
            catch { /* malformed PATH entry — skip */ }
        }
        return null;
    }

    /// <summary>Read a beat's audio bytes through the configured store.
    /// Returns null when the relative path can't be resolved (missing file,
    /// blob 404, etc) — caller treats null as "skip this beat" so a single
    /// missing file doesn't blow up a combined export.</summary>
    private async Task<byte[]?> ReadAllAudioAsync(string relativePath, CancellationToken ct)
    {
        try
        {
            await using var src = await audioStore.OpenReadAsync(relativePath, ct);
            if (src == null) return null;
            using var ms = new MemoryStream();
            await src.CopyToAsync(ms, ct);
            return ms.ToArray();
        }
        catch (Exception ex) { log.LogWarning(ex, "Audio read failed for {Path}", relativePath); return null; }
    }

    /// <summary>Concat each beat's already-staged-local MP3 file into
    /// <paramref name="outPath"/>, inserting precise digital silence between
    /// each beat per <see cref="ComputeTrailingSilenceMs"/>. Silence MP3s
    /// are rendered once per distinct pause length (cached in a temp dir)
    /// and reused via ffmpeg's <c>-f concat</c> demuxer with <c>-c copy</c>
    /// (no re-encode). Inputs are paired with their source OrderedBeat so
    /// the per-beat gap computation has access to the same Beat metadata
    /// the rest of the workbench works against.</summary>
    private async Task ConcatMp3sWithSilenceAsync(string ffmpegPath, List<(OrderedBeat Source, string LocalPath)> ordered, string outPath, Func<Beat, Beat, int> pauseMsFor, CancellationToken ct)
    {
        var tmpDir = Path.Combine(Path.GetTempPath(), $"streetsamurai-concat-{Guid.CreateVersion7():N}");
        Directory.CreateDirectory(tmpDir);
        try
        {
            // Render any silence MP3 lengths we need, keyed by ms. Cache so a
            // 400ms gap that repeats 50 times only renders once.
            var silenceCache = new Dictionary<int, string>();
            async Task<string> SilenceFor(int ms)
            {
                if (silenceCache.TryGetValue(ms, out var existing)) return existing;
                var file = Path.Combine(tmpDir, $"silence_{ms}.mp3");
                var args = $"-hide_banner -loglevel error -y -f lavfi -i anullsrc=channel_layout=mono:sample_rate=44100 -t {ms / 1000.0:F3} -b:a 128k \"{file}\"";
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = args,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                using var proc = System.Diagnostics.Process.Start(psi)
                    ?? throw new InvalidOperationException("Failed to spawn ffmpeg for silence render.");
                await proc.WaitForExitAsync(ct);
                if (proc.ExitCode != 0)
                {
                    var stderr = await proc.StandardError.ReadToEndAsync(ct);
                    throw new InvalidOperationException($"ffmpeg silence render failed (exit {proc.ExitCode}): {stderr}");
                }
                silenceCache[ms] = file;
                return file;
            }

            // Build the concat list using the already-staged local paths.
            var listLines = new List<string>();
            for (int i = 0; i < ordered.Count; i++)
            {
                var (source, beatAudio) = ordered[i];
                if (!File.Exists(beatAudio)) continue;
                listLines.Add($"file '{beatAudio.Replace("'", "'\\''")}'");
                if (i < ordered.Count - 1)
                {
                    var pauseMs = pauseMsFor(source.Beat, ordered[i + 1].Source.Beat);
                    if (pauseMs > 0)
                    {
                        var silenceFile = await SilenceFor(pauseMs);
                        listLines.Add($"file '{silenceFile.Replace("'", "'\\''")}'");
                    }
                }
            }

            if (listLines.Count == 0)
            {
                log.LogWarning("ConcatMp3sWithSilenceAsync: no beat audio files exist; not writing combined.");
                return;
            }

            var listPath = Path.Combine(tmpDir, "concat.txt");
            await File.WriteAllLinesAsync(listPath, listLines, ct);

            var concatArgs = $"-hide_banner -loglevel error -y -f concat -safe 0 -i \"{listPath}\" -c copy \"{outPath}\"";
            var concatPsi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = concatArgs,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            using var concatProc = System.Diagnostics.Process.Start(concatPsi)
                ?? throw new InvalidOperationException("Failed to spawn ffmpeg for MP3 concat.");
            await concatProc.WaitForExitAsync(ct);
            if (concatProc.ExitCode != 0)
            {
                var stderr = await concatProc.StandardError.ReadToEndAsync(ct);
                throw new InvalidOperationException($"ffmpeg concat failed (exit {concatProc.ExitCode}): {stderr}");
            }
            log.LogInformation("ffmpeg concat wrote {Path} ({Beats} beats, {Silences} silences)",
                outPath, ordered.Count, silenceCache.Count);
        }
        finally
        {
            try { Directory.Delete(tmpDir, recursive: true); }
            catch (Exception ex) { log.LogDebug(ex, "Could not clean up tmp concat dir {Dir}", tmpDir); }
        }
    }

    private void InvalidateAudioOnBeat(Beat beat)
    {
        if (!string.IsNullOrEmpty(beat.AudioPath))
        {
            // Fire-and-forget the delete via the store. Sync caller, so we
            // can't await — the store's own try/catch keeps a transient
            // blob/disk failure from cascading into a beat-edit failure.
            // The DB row update below is the authoritative "audio is gone"
            // signal regardless of whether the bytes actually deleted.
            var path = beat.AudioPath;
            _ = audioStore.DeleteAsync(path).ContinueWith(t =>
            {
                if (t.Exception != null) log.LogWarning(t.Exception.Flatten(), "Audio delete failed for {Path}", path);
            }, TaskScheduler.Default);
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
        Beat beat, Strand strand,
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
        // Persist bytes through the audio-store abstraction so a blob-backed
        // deployment writes to Azure storage without the workbench knowing.
        // The relative path stamped onto Beat.AudioPath is canonical across
        // backends ("{slug}/audio/{beatId:N}.wav").
        var rel = await audioStore.WriteBeatAsync(strand.Slug, beat.Id, "wav", wav, ct);

        beat.AudioPath     = rel;
        beat.NarratedAt    = DateTime.UtcNow;
        beat.DurationSec   = result.Bytes.Length / 88200.0;
        beat.TextHash      = ComputeTextHash(beat.Text);
        beat.LastRequestId = result.RequestId;
        beat.Stale         = false;
        return result.RequestId;
    }

    private async Task<string?> SynthesizeAsMp3Async(
        Beat beat, Strand strand,
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

        var rel = await audioStore.WriteBeatAsync(strand.Slug, beat.Id, "mp3", result.Bytes, ct);

        // Real duration: prefer ffprobe (already required for MP3 silence
        // pacing on the export path), then a frame-header scan as a pure-C#
        // fallback. The old code used `Text.Length / 15.0` which was off by
        // 30-60% on short or punctuation-heavy beats and broke the listener's
        // progress bar. ffprobe needs a local path; on blob backends the
        // local lookup returns null and we fall back to the byte scan.
        var localPathForProbe = await audioStore.ResolveLocalPathAsync(rel, ct);
        var duration = await ProbeMp3DurationAsync(localPathForProbe, result.Bytes, ct);

        beat.AudioPath     = rel;
        beat.NarratedAt    = DateTime.UtcNow;
        beat.DurationSec   = duration;
        beat.TextHash      = ComputeTextHash(beat.Text);
        beat.LastRequestId = result.RequestId;
        beat.Stale         = false;
        return result.RequestId;
    }

    /// <summary>Return the duration of an MP3 file in seconds. Tries ffprobe
    /// first (precise, fast — needs a local path); falls back to a frame-
    /// header byte scan for VBR safety; last resort is a CBR estimate
    /// (file-size ÷ 16 KB/s ≈ 128 kbps). Never throws — bad audio just
    /// yields a 1.0s sentinel so the UI's progress bar still moves.</summary>
    private async Task<double> ProbeMp3DurationAsync(string? path, byte[] bytes, CancellationToken ct)
    {
        var ffprobe = string.IsNullOrEmpty(path) ? null : ResolveFfprobePath();
        if (!string.IsNullOrEmpty(ffprobe))
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = ffprobe,
                    Arguments = $"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 \"{path}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                using var proc = System.Diagnostics.Process.Start(psi);
                if (proc != null)
                {
                    // Drain stdout AND stderr concurrently. With both pipes
                    // redirected, a child that writes >4 KB to stderr will
                    // block on the unread pipe — and since stdout doesn't
                    // close until the process exits, awaiting stdout first
                    // hangs forever. The hard 10s timeout caps the worst-
                    // case wedge so a misbehaving ffprobe can't pin a
                    // narration thread indefinitely.
                    using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    timeoutCts.CancelAfter(TimeSpan.FromSeconds(10));
                    var stdoutTask = proc.StandardOutput.ReadToEndAsync(timeoutCts.Token);
                    var stderrTask = proc.StandardError.ReadToEndAsync(timeoutCts.Token);
                    try
                    {
                        await proc.WaitForExitAsync(timeoutCts.Token);
                        var stdout = await stdoutTask;
                        _ = await stderrTask;
                        if (proc.ExitCode == 0
                            && double.TryParse(stdout.Trim(), System.Globalization.NumberStyles.Float,
                                System.Globalization.CultureInfo.InvariantCulture, out var sec)
                            && sec > 0)
                        {
                            return Math.Round(sec, 3);
                        }
                    }
                    catch (OperationCanceledException) when (!ct.IsCancellationRequested)
                    {
                        try { if (!proc.HasExited) proc.Kill(entireProcessTree: true); } catch { }
                        log.LogWarning("ffprobe timed out for {Path}; falling back to byte scan", path);
                    }
                }
            }
            catch (Exception ex) { log.LogDebug(ex, "ffprobe duration parse failed for {Path}", path); }
        }
        // Pure-C# fallback: ElevenLabs returns CBR mp3_44100_128 (128 kbps).
        // 128 kbps = 16,000 bytes/sec. Skip the (small) ID3v2 header if present.
        int offset = 0;
        if (bytes.Length > 10 && bytes[0] == 'I' && bytes[1] == 'D' && bytes[2] == '3')
        {
            int size = ((bytes[6] & 0x7F) << 21) | ((bytes[7] & 0x7F) << 14)
                     | ((bytes[8] & 0x7F) << 7)  | (bytes[9]  & 0x7F);
            offset = 10 + size;
        }
        var audioBytes = Math.Max(0, bytes.Length - offset);
        return Math.Max(1.0, Math.Round(audioBytes / 16000.0, 3));
    }

    private static string? ResolveFfprobePath()
    {
        var name = OperatingSystem.IsWindows() ? "ffprobe.exe" : "ffprobe";
        var pathVar = Environment.GetEnvironmentVariable("PATH") ?? "";
        var sep = OperatingSystem.IsWindows() ? ';' : ':';
        foreach (var dir in pathVar.Split(sep, StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                var candidate = Path.Combine(dir, name);
                if (File.Exists(candidate)) return candidate;
            }
            catch { /* malformed PATH entry — skip */ }
        }
        return null;
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
        string? SceneType,
        bool IsChapterStart,
        string? Kind);

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
