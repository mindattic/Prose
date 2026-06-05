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

    /// <summary>Live per-strand progress for an in-flight <see cref="ExportCombinedAsync"/>
    /// (Publish). The UI polls <see cref="GetExportProgress"/> while the export
    /// runs to drive the ring loader. Cleared in the export's finally.</summary>
    private static readonly ConcurrentDictionary<Guid, ExportProgress> exportProgress = new();

    /// <summary>Snapshot of a Publish/combine in progress. <see cref="Current"/>
    /// of <see cref="Total"/> beats stitched; <see cref="Label"/> names the beat
    /// currently being written.</summary>
    public sealed record ExportProgress(int Current, int Total, string? Label);

    /// <summary>Current Publish progress for a strand, or null when no export is
    /// running. Read-only poll target for the ring-loader overlay.</summary>
    public ExportProgress? GetExportProgress(Guid strandId)
        => exportProgress.TryGetValue(strandId, out var p) ? p : null;

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

    /// <summary>Deep-duplicate a strand (and its sub-strand tree) into a brand-new
    /// independent copy. Every beat is cloned into a FRESH Beat row — prose and
    /// narration metadata are preserved, but audio, review scores, and the stale
    /// flag are reset, since a copy has no recordings and no reviews yet. Editing
    /// the duplicate never touches the original (beats are not shared). The root
    /// copy takes <paramref name="newTitle"/>; any child strands keep their own
    /// titles. The duplicate slots in beside the source under the same parent.
    /// Returns the new root strand's id and slug.</summary>
    public async Task<(Guid Id, string Slug)> DuplicateStrandAsync(Guid sourceId, string newTitle, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(newTitle))
            throw new ArgumentException("A title for the duplicate is required.", nameof(newTitle));

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var source = await db.Strands.AsNoTracking().FirstOrDefaultAsync(s => s.Id == sourceId, ct)
            ?? throw new InvalidOperationException($"Strand {sourceId} not found.");

        // Serializable so the sibling-max read + inserts can't race a concurrent
        // create/duplicate under the same parent (mirrors ImportStrandCli).
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

        var siblingMaxSort = source.ParentStrandId.HasValue
            ? await db.Strands.Where(s => s.ParentStrandId == source.ParentStrandId).Select(s => (double?)s.SortKey).MaxAsync(ct) ?? 0
            : await db.Strands.Where(s => s.ParentStrandId == null).Select(s => (double?)s.SortKey).MaxAsync(ct) ?? 0;

        // One contiguous block of Beat.Number values, allocated as we walk the tree.
        var nextNumber = new[] { (await db.Beats.MaxAsync(b => (int?)b.Number, ct) ?? 0) + 1 };

        var (rootId, rootSlug) = await CloneStrandSubtreeAsync(
            db, sourceId, newTitle, source.ParentStrandId, siblingMaxSort + 100.0, nextNumber, ct);

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        log.LogInformation("Duplicated strand {Src} -> {New} ({Slug})", sourceId, rootId, rootSlug);
        return (rootId, rootSlug);
    }

    /// <summary>Recursively clone one strand subtree into newly-added (unsaved)
    /// entities on <paramref name="db"/>. The caller owns the transaction + save.</summary>
    private async Task<(Guid Id, string Slug)> CloneStrandSubtreeAsync(
        StreetSamuraiDbContext db, Guid srcStrandId, string? titleOverride,
        Guid? newParentId, double sortKey, int[] nextNumber, CancellationToken ct)
    {
        var src = await db.Strands.AsNoTracking().FirstAsync(s => s.Id == srcStrandId, ct);
        var newId = Guid.CreateVersion7();
        var title = titleOverride ?? src.Title;
        var slug = $"{Slugify(title)}-{newId.ToString("N")[..8]}";

        db.Strands.Add(new Strand
        {
            Id             = newId,
            Slug           = slug,
            Title          = title,
            Kind           = src.Kind,
            Status         = "draft",
            Synopsis       = src.Synopsis,
            VoiceId        = src.VoiceId,
            ParentStrandId = newParentId,
            SortKey        = sortKey,
        });

        // Direct beats in reading order → fresh Beat rows. Audio
        // (AudioPath/NarratedAt/DurationSec/LastRequestId/GapAfterAudioPath),
        // review (Score/ScoredAt), Stale and WasCorrected are intentionally left
        // at defaults — a duplicate has no recordings, no reviews, nothing stale.
        var srcBeats = await db.StrandBeats
            .Where(sb => sb.StrandId == srcStrandId)
            .OrderBy(sb => sb.SortKey)
            .Join(db.Beats, sb => sb.BeatId, b => b.Id, (sb, b) => new { sb.SortKey, Beat = b })
            .ToListAsync(ct);
        var now = DateTime.UtcNow;
        foreach (var row in srcBeats)
        {
            var s = row.Beat;
            var nb = new Beat
            {
                Id             = Guid.CreateVersion7(),
                Number         = nextNumber[0]++,
                Text           = s.Text,
                TextHash       = s.TextHash,
                BeatTitle      = s.BeatTitle,
                IsChapterStart = s.IsChapterStart,
                Kind           = s.Kind,
                Synopsis       = s.Synopsis,
                StructureRole  = s.StructureRole,
                Act            = s.Act,
                SceneType      = s.SceneType,
                FacetTag       = s.FacetTag,
                EmotionalTone  = s.EmotionalTone,
                PaceHint       = s.PaceHint,
                GapAfterMs     = s.GapAfterMs,
                VoiceId        = s.VoiceId,
                CreatedAt      = now,
                UpdatedAt      = now,
            };
            db.Beats.Add(nb);
            db.StrandBeats.Add(new StrandBeat { StrandId = newId, BeatId = nb.Id, SortKey = row.SortKey });
        }

        // Recurse into child strands, preserving their order.
        var children = await db.Strands.AsNoTracking()
            .Where(s => s.ParentStrandId == srcStrandId)
            .OrderBy(s => s.SortKey)
            .Select(s => new { s.Id, s.SortKey })
            .ToListAsync(ct);
        foreach (var child in children)
            await CloneStrandSubtreeAsync(db, child.Id, null, newId, child.SortKey, nextNumber, ct);

        return (newId, slug);
    }

    /// <summary>Title → URL slug stem (lowercase, non-alphanumerics to hyphens).
    /// Callers append a short id suffix for global uniqueness.</summary>
    private static string Slugify(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "strand";
        var ascii = Regex.Replace(s.ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');
        return string.IsNullOrEmpty(ascii) ? "strand" : ascii;
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

    // ── Version history (system-versioned temporal) ──────────────────────
    // The Beats table is system-versioned (see DbContext.SystemVersionedTables),
    // so every UPDATE/DELETE — from the writer UI, the CLI, or an MCP tool —
    // lands a prior-state row in Beats_History automatically. These two reads
    // back the per-beat version cycler: one row per stored version, newest
    // first, index 0 = the live/current row. No app-side snapshotting; the
    // database is the single source of truth for "what did this beat say
    // before." Returns empty on non-SQL-Server providers (SQLite tests have
    // no temporal history).

    private sealed class BeatVersionCountRow { public Guid Id { get; set; } public int Cnt { get; set; } }

    /// <summary>Count of stored versions (current + history) for every beat in
    /// a strand, keyed by beat id. Drives the cycler arrows' disabled state in
    /// one grouped query. A beat never edited since versioning was enabled has
    /// count 1 (just the current row → both arrows dead).</summary>
    public async Task<Dictionary<Guid, int>> GetBeatVersionCountsAsync(Guid strandId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        if (!db.Database.IsSqlServer()) return new();
        var rows = await db.Database.SqlQueryRaw<BeatVersionCountRow>(
            """
            SELECT b.Id AS Id, COUNT(*) AS Cnt
            FROM [Beats] FOR SYSTEM_TIME ALL AS b
            WHERE b.Id IN (SELECT BeatId FROM [StrandBeats] WHERE StrandId = {0})
            GROUP BY b.Id
            """, strandId).ToListAsync(ct);
        return rows.ToDictionary(r => r.Id, r => r.Cnt);
    }

    /// <summary>The beat's prose at a newest-first version index — 0 = current,
    /// 1 = the version before the last edit, and so on back through history.
    /// Null when the index is past the end or on a non-temporal provider. Used
    /// by the writer's ◀ ▶ cycler to preview a past version in the editor.</summary>
    public async Task<string?> GetBeatVersionTextAsync(Guid beatId, int index, CancellationToken ct = default)
    {
        if (index < 0) return null;
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        if (!db.Database.IsSqlServer()) return null;
        var rows = await db.Database.SqlQueryRaw<string>(
            """
            SELECT b.[Text] AS [Value]
            FROM [Beats] FOR SYSTEM_TIME ALL AS b
            WHERE b.Id = {0}
            ORDER BY b.SysStart DESC
            OFFSET {1} ROWS FETCH NEXT 1 ROWS ONLY
            """, beatId, index).ToListAsync(ct);
        return rows.FirstOrDefault();
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

            // Resolve (and snapshot) the strand's locked voice profile ONCE
            // before the loop and reuse it for every beat. The snapshot is
            // captured on first narration and frozen on the strand, so neither
            // a mid-run settings change NOR a global profile change weeks later
            // can fork the strand into two voices. The bundle (model + voice_id
            // + stability/similarity/style + deterministic seed) is used
            // together — that's the whole point. Beats with their own VoiceId
            // still override (future per-character work).
            var voice = await ResolveStrandVoiceAsync(db, strand, ct);
            var lockedStrandVoice = voice.VoiceId;
            var tagsEnabled = settings?.TtsUseAudioTags ?? true;
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
                // The baseline voice_settings come from the strand's LOCKED
                // snapshot (so every beat — including ones recorded later —
                // shares one tuning + one seed). EmotionalTone / PaceHint nudges
                // still adjust them per beat for dramatic range on v2; on v3
                // they're flattened back to the baseline inside Build.
                var prompt = BeatPromptBuilder.Build(tracked, voice.Model, tagsEnabled,
                    voice.Stability, voice.Similarity, voice.Style, voice.Seed);

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
                    db.StrandAudioEvents.Add(NewAudioEvent(strandId, tracked.Id, null, "beat-recorded",
                        $"{tracked.Text.Length} chars, voice {voiceForBeat}"));
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
            if (failedCount == 0) strand.Error = null; // clear stale failure note on a clean run
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

    /// <summary>
    /// Render the strand's FIRST non-empty beat with an arbitrary voice profile
    /// and return the MP3 bytes — a throwaway preview for the voice studio.
    /// NOTHING is persisted: this never touches the strand's pinned voice, the
    /// beat's stored audio, or the audio-event ledger. Uses the strand's
    /// deterministic seed so the preview matches what a real publish on these
    /// dials would actually sound like.
    /// </summary>
    public async Task<byte[]> AuditionBeatAsync(Guid strandId, Models.VoiceProfile dials, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dials);
        if (string.IsNullOrWhiteSpace(dials.VoiceId))
            throw new InvalidOperationException("Pick a voice to audition.");
        if (!await tts.IsConfiguredAsync())
            throw new InvalidOperationException("TTS is not configured (no ElevenLabs API key).");

        var first = (await GetOrderedBeatsAsync(strandId, ct))
            .Where(o => !string.IsNullOrWhiteSpace(o.Beat.Text))
            .Select(o => o.Beat)
            .FirstOrDefault()
            ?? throw new InvalidOperationException("Strand has no beat text to audition.");

        var tagsEnabled = settings?.TtsUseAudioTags ?? true;
        var prompt = BeatPromptBuilder.Build(first, dials.Model, tagsEnabled,
            dials.Stability, dials.SimilarityBoost, dials.Style, DeriveSeed(strandId));
        var vs = new TtsVoiceSettings(prompt.Stability, prompt.SimilarityBoost, prompt.Style,
            Seed: prompt.Seed, ModelId: prompt.ModelId);
        var result = await tts.SynthesizeWithIdAsync(
            prompt.Text, dials.VoiceId, outputFormat: "mp3_44100_128",
            previousRequestIds: null, previousText: null, nextText: null,
            voiceSettings: vs, ct);
        return result.Bytes;
    }

    /// <summary>
    /// Pin a voice profile's full dial set onto the strand's snapshot columns so
    /// every later (re)record and publish renders through it — the durable
    /// "tweak the voice for THIS strand" path. Overwrites VoiceId/VoiceModel and
    /// the three voice_settings dials; leaves VoiceSeed intact so the strand
    /// stays deterministic across the change.
    /// </summary>
    public async Task SetStrandVoiceAsync(Guid strandId, Models.VoiceProfile dials, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dials);
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var strand = await db.Strands.FirstOrDefaultAsync(s => s.Id == strandId, ct)
            ?? throw new InvalidOperationException($"Strand {strandId} not found.");

        strand.VoiceId         = dials.VoiceId;
        strand.VoiceModel      = dials.Model;
        strand.VoiceStability  = dials.Stability;
        strand.VoiceSimilarity = dials.SimilarityBoost;
        strand.VoiceStyle      = dials.Style;
        strand.VoiceSeed     ??= DeriveSeed(strandId);
        db.StrandAudioEvents.Add(NewAudioEvent(strandId, null, null, "voice-set",
            $"voice {dials.VoiceId}, model {dials.Model}, stability {dials.Stability}"));
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Render a SINGLE beat's audio in isolation — the unit of work
    /// behind Live Broadcast's just-in-time look-ahead buffer. Mirrors one
    /// iteration of <see cref="NarrateAsync"/>'s loop: resolves the active
    /// voice profile, builds the v2/v3 prompt, synthesises lossless WAV
    /// (falling back to mp3 on a 403), and stamps AudioPath / TextHash / etc.
    /// It re-reads the beat's CURRENT text from the DB first, so an edit made
    /// while the broadcast is mid-flight is what gets voiced when the buffer
    /// reaches it. Returns true on success; per-beat failures are logged and
    /// return false so the caller can skip-and-continue. Skips (returns true)
    /// when the beat already has fresh, non-stale audio — idempotent, so the
    /// look-ahead can call it on every tick without re-billing TTS.</summary>
    public async Task<bool> NarrateBeatAsync(Guid strandId, Guid beatId, bool force = false, CancellationToken ct = default)
    {
        if (!await tts.IsConfiguredAsync())
            throw new InvalidOperationException("TTS is not configured. Set ElevenLabs API key in Settings.");

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var strand = await db.Strands.FirstOrDefaultAsync(s => s.Id == strandId, ct)
            ?? throw new InvalidOperationException($"Strand {strandId} not found.");

        var ordered = await GetOrderedBeatsAsync(strandId, ct);
        var idx = ordered.FindIndex(o => o.Beat.Id == beatId);
        if (idx < 0) return false;

        var tracked = await db.Beats.FirstOrDefaultAsync(b => b.Id == beatId, ct);
        if (tracked == null || string.IsNullOrWhiteSpace(tracked.Text)) return false;

        // Idempotent fast-path: already voiced for the current text → nothing to do.
        if (!force && !tracked.Stale && !string.IsNullOrEmpty(tracked.AudioPath))
            return true;

        // Reuse the strand's LOCKED voice snapshot — this is the key to a
        // single-beat re-record sounding like the rest of the strand: same
        // model, same voice, same tuning, same deterministic seed as every
        // other beat, regardless of what the global default profile is now.
        var voice         = await ResolveStrandVoiceAsync(db, strand, ct);
        var lockedStrandVoice = voice.VoiceId;
        var voiceForBeat  = !string.IsNullOrEmpty(tracked.VoiceId) ? tracked.VoiceId : lockedStrandVoice;
        var tagsEnabled   = settings?.TtsUseAudioTags ?? true;
        var prompt = BeatPromptBuilder.Build(tracked, voice.Model, tagsEnabled,
            voice.Stability, voice.Similarity, voice.Style, voice.Seed);

        // v2 continuity context (the TTS layer drops both for v3).
        var prevIds = new List<string>(3);
        for (int j = idx - 1; j >= 0 && prevIds.Count < 3; j--)
        {
            var rid = ordered[j].Beat.LastRequestId;
            if (!string.IsNullOrEmpty(rid)) prevIds.Insert(0, rid);
        }
        var (prevText, nextText) = BuildTextWindow(ordered, idx, contextChars: 1500);

        try
        {
            try
            {
                await SynthesizeAsLosslessWavAsync(tracked, strand, prevIds.ToArray(), prevText, nextText, voiceForBeat, prompt, ct);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                await SynthesizeAsMp3Async(tracked, strand, prevIds.ToArray(), prevText, nextText, voiceForBeat, prompt, ct);
            }
            db.StrandAudioEvents.Add(NewAudioEvent(strandId, beatId, null, "beat-recorded",
                $"{tracked.Text?.Length ?? 0} chars, voice {voiceForBeat}"));
            await db.SaveChangesAsync(ct);
            return true;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            log.LogError(ex, "Live-broadcast render failed on strand {S} beat {B}", strandId, beatId);
            return false;
        }
    }

    /// <summary>
    /// Publish a drift-free audiobook in the FEWEST TTS requests that fit
    /// ElevenLabs' per-request budget, so the narrator is one continuous
    /// performance instead of N separately-recorded beats. Tier 1 = whole strand
    /// in one call; Tier 2 = one call per chapter (split at <c>IsChapterStart</c>);
    /// Tier 3 = split an over-long chapter at the char budget. Intra-segment beat
    /// gaps become inline <c>&lt;break&gt;</c> pauses (one recording keeps its
    /// pacing); segment boundaries get exact PCM silence. The combined MP3 is
    /// written to the audio store AND copied to the user's Downloads folder.
    /// </summary>
    public async Task<string?> PublishAudiobookAsync(Guid strandId, bool retuneRobust = false, CancellationToken ct = default)
    {
        if (!await tts.IsConfiguredAsync())
            throw new InvalidOperationException("TTS is not configured (no ElevenLabs API key).");

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var strand = await db.Strands.FirstOrDefaultAsync(s => s.Id == strandId, ct)
            ?? throw new InvalidOperationException($"Strand {strandId} not found.");

        var ordered = (await GetOrderedBeatsAsync(strandId, ct))
            .Where(o => !string.IsNullOrWhiteSpace(o.Beat.Text)).ToList();
        if (ordered.Count == 0) { log.LogWarning("Strand {S} has no beat text to narrate", strandId); return null; }

        var voice = await ResolveStrandVoiceAsync(db, strand, ct);

        // --robust: deliberately overwrite this strand's frozen stability to
        // Robust (1.0). The snapshot normally pins voice params so re-records
        // can't drift; this is the one explicit, opt-in retune that lets an
        // older strand (snapshotted at Natural 0.5) adopt the Robust default on
        // re-record. Persisted so every later (re)record stays Robust too.
        if (retuneRobust && strand.VoiceStability is not 1.0)
        {
            strand.VoiceStability = 1.0;
            await db.SaveChangesAsync(ct);
            voice = voice with { Stability = 1.0 };
            log.LogInformation("Strand {S} retuned to Robust (stability 1.0)", strandId);
        }
        bool isV3 = voice.Model.Contains("v3", StringComparison.OrdinalIgnoreCase);
        // eleven_v3 caps a request far lower than the v2 family; budget per model.
        int limit = isV3 ? 2800 : 9000;
        var segments = BuildAudiobookSegments(ordered, limit);

        var pub = new StrandPublication
        {
            Id = Guid.CreateVersion7(), StrandId = strandId, StartedAt = DateTime.UtcNow,
            Status = "running", BeatCount = ordered.Count,
        };
        db.StrandPublications.Add(pub);
        db.StrandAudioEvents.Add(NewAudioEvent(strandId, null, pub.Id, "publish-started",
            $"one-pass audiobook: {ordered.Count} beats in {segments.Count} segment(s), model {voice.Model}"));
        await db.SaveChangesAsync(ct);

        exportProgress[strandId] = new ExportProgress(0, segments.Count, "narrating");
        var ffmpeg = ResolveFfmpegPath();
        if (string.IsNullOrEmpty(ffmpeg))
            throw new InvalidOperationException(
                "Audiobook publishing needs ffmpeg on PATH: segments are assembled as PCM and the combined " +
                "track is encoded to MP3 with ffmpeg, and any non-PCM fetch format is decoded back to PCM with it.");
        var tmpWav = Path.Combine(Path.GetTempPath(), $"ss-audiobook-{Guid.CreateVersion7():N}.wav");
        long pcmTotal = 0, chars = 0;
        var prevReqIds = new List<string>();

        // Voice params are frozen per strand, so the bundle is constant for
        // every chunk — build it once.
        var vs = new TtsVoiceSettings(voice.Stability, voice.Similarity, voice.Style, Seed: voice.Seed, ModelId: voice.Model);

        // Negotiate the highest-fidelity output format this account/tier allows,
        // once, then lock it for the whole strand (one consistent encode). The
        // older path always fetched mp3_44100_128 — a lossy 128 k source that
        // then got decoded + re-encoded, capping quality. pcm_44100 is lossless
        // and skips the MP3 round-trip entirely; mp3_192 beats the universal
        // mp3_128. On a tier/format rejection (401/403/422) we drop to the next.
        string? fetchFormat = null;
        async Task<(byte[] Pcm, string? RequestId)> FetchPcmAsync(
            string chunk, IList<string>? stitchIds, string? pText, string? nText)
        {
            var prefs = fetchFormat is not null
                ? new[] { fetchFormat }
                : new[] { "pcm_44100", "mp3_44100_192", "mp3_44100_128" };
            System.Net.Http.HttpRequestException? lastReject = null;
            foreach (var fmt in prefs)
            {
                try
                {
                    var r = await tts.SynthesizeWithIdAsync(chunk, voice.VoiceId, fmt, stitchIds, pText, nText, vs, ct);
                    if (fetchFormat is null)
                    {
                        fetchFormat = fmt;
                        log.LogInformation("Audiobook fetch format negotiated: {Fmt}", fmt);
                        db.StrandAudioEvents.Add(NewAudioEvent(strandId, null, pub.Id, "format-negotiated", fmt));
                    }
                    if (r.Bytes.Length == 0) return (r.Bytes, r.RequestId);
                    var pcm = fmt.StartsWith("pcm", StringComparison.OrdinalIgnoreCase)
                        ? r.Bytes
                        : await DecodeMp3ToPcmAsync(ffmpeg!, r.Bytes, ct);
                    return (pcm, r.RequestId);
                }
                catch (System.Net.Http.HttpRequestException ex) when (fetchFormat is null
                    && ex.StatusCode is System.Net.HttpStatusCode.Unauthorized
                        or System.Net.HttpStatusCode.Forbidden
                        or System.Net.HttpStatusCode.UnprocessableEntity)
                {
                    lastReject = ex;
                    log.LogWarning("Audiobook format {Fmt} rejected ({Code}); trying lower fidelity", fmt, ex.StatusCode);
                }
            }
            if (lastReject is not null) throw lastReject;
            throw new InvalidOperationException("No ElevenLabs output format accepted.");
        }

        try
        {
            await using (var fs = new FileStream(tmpWav, FileMode.Create, FileAccess.ReadWrite, FileShare.None, 64 * 1024, useAsync: true))
            {
                fs.Position = 44; // reserve WAV header; patched after total PCM known
                for (int si = 0; si < segments.Count; si++)
                {
                    ct.ThrowIfCancellationRequested();
                    var seg = segments[si];
                    var text = AssembleSegmentText(seg, ref chars);
                    exportProgress[strandId] = new ExportProgress(si + 1, segments.Count,
                        string.IsNullOrWhiteSpace(seg[0].Beat.BeatTitle) ? $"Segment {si + 1}/{segments.Count}" : seg[0].Beat.BeatTitle);

                    // Last-ditch tier: a segment (e.g. a single over-long beat, or a
                    // chapter that alone exceeds the budget) can still top the per-request
                    // cap — split it on sentence boundaries so no call goes over. Chunks
                    // within a segment are continuous prose, so they're concatenated with
                    // NO silence between them (segment seams keep their PCM silence below).
                    var chunks = SplitToLimit(text, limit);
                    long segBytes = 0;
                    for (int ci = 0; ci < chunks.Count; ci++)
                    {
                        ct.ThrowIfCancellationRequested();
                        // v2 stitches on prior request-ids; v3 does NOT (it would disjoint) —
                        // text conditioning (previous/next) is safe for both.
                        IList<string>? stitch = !isV3 && prevReqIds.Count > 0 ? prevReqIds : null;
                        var prevText = ci > 0 ? Tail(chunks[ci - 1])
                                     : si > 0 ? Tail(segments[si - 1][^1].Beat.Text) : null;
                        var nextText = ci < chunks.Count - 1 ? Head(chunks[ci + 1])
                                     : si < segments.Count - 1 ? Head(segments[si + 1][0].Beat.Text) : null;

                        // Fetch at the best format the tier allows (pcm_44100 lossless →
                        // mp3_192 → mp3_128) and resolve to PCM so the silence-gap assembly
                        // and the single final encode below stay unchanged. When the format
                        // is already PCM the MP3 round-trip is skipped entirely.
                        var res = await FetchPcmAsync(chunks[ci], stitch, prevText, nextText);
                        if (res.Pcm.Length > 0) { await fs.WriteAsync(res.Pcm.AsMemory(), ct); pcmTotal += res.Pcm.Length; segBytes += res.Pcm.Length; }
                        if (!string.IsNullOrEmpty(res.RequestId)) { prevReqIds.Insert(0, res.RequestId!); if (prevReqIds.Count > 3) prevReqIds.RemoveRange(3, prevReqIds.Count - 3); }
                    }
                    db.StrandAudioEvents.Add(NewAudioEvent(strandId, null, pub.Id, "segment-narrated",
                        $"seg {si + 1}/{segments.Count}: {seg.Count} beat(s), {chunks.Count} chunk(s), {segBytes} pcm bytes"));

                    if (si < segments.Count - 1)
                    {
                        var last = seg[^1].Beat; var next = segments[si + 1][0].Beat;
                        var pauseMs = last.GapAfterMs ?? ComputeTrailingSilenceMs(last, next, settings);
                        if (pauseMs > 0)
                        {
                            var sil = GenerateSilencePcm(pauseMs, 44100, 1, 16);
                            if (sil.Length > 0) { await fs.WriteAsync(sil.AsMemory(), ct); pcmTotal += sil.Length; }
                        }
                    }
                }
                fs.Position = 0;
                EpisodeAudioService.WriteWavHeader(fs, checked((int)pcmTotal), 44100, 1, 16);
            }

            byte[] finalBytes; string ext;
            if (!string.IsNullOrEmpty(ffmpeg))
            {
                var tmpMp3 = Path.Combine(Path.GetTempPath(), $"ss-audiobook-{Guid.CreateVersion7():N}.mp3");
                try
                {
                    await TranscodeWavToMp3Async(ffmpeg, tmpWav, tmpMp3, ct);
                    finalBytes = await File.ReadAllBytesAsync(tmpMp3, ct); ext = "mp3";
                }
                finally { try { File.Delete(tmpMp3); } catch { } }
            }
            else { finalBytes = await File.ReadAllBytesAsync(tmpWav, ct); ext = "wav"; }

            await audioStore.WriteCombinedAsync(strand.Slug, ext, finalBytes, ct);

            var dlDir = CanonExportService.DownloadsDir;
            Directory.CreateDirectory(dlDir);
            var dl = Path.Combine(dlDir, $"{strand.Slug}.{strand.Id.ToString("N")[..8]}.{ext}");
            await File.WriteAllBytesAsync(dl, finalBytes, ct);

            strand.CombinedAudioPath = $"{strand.Slug}/strand.{ext}";
            strand.CharsNarrated = (int)Math.Min(chars, int.MaxValue);
            strand.AudioCompletedAt = DateTime.UtcNow;
            pub.Status = "ready";
            db.StrandAudioEvents.Add(NewAudioEvent(strandId, null, pub.Id, "mp3-produced",
                $"{strand.Slug}/strand.{ext}, {finalBytes.Length} bytes; copied to Downloads"));
            await db.SaveChangesAsync(ct);
            exportProgress[strandId] = new ExportProgress(segments.Count, segments.Count, "done");
            log.LogInformation("Published one-pass audiobook for strand {S}: {Seg} segment(s) -> {Path}", strandId, segments.Count, dl);
            return dl;
        }
        catch (Exception ex)
        {
            pub.Status = "error";
            db.StrandAudioEvents.Add(NewAudioEvent(strandId, null, pub.Id, "publish-error", ex.Message));
            try { await db.SaveChangesAsync(CancellationToken.None); } catch { }
            throw;
        }
        finally { try { File.Delete(tmpWav); } catch { } }
    }

    /// <summary>Greedy segmenter: keep beats together up to the char budget,
    /// preferring chapter starts as split points so a chapter never straddles two
    /// recordings unless it alone exceeds the budget.</summary>
    private static List<List<OrderedBeat>> BuildAudiobookSegments(List<OrderedBeat> ordered, int limit)
    {
        var segments = new List<List<OrderedBeat>>();
        var cur = new List<OrderedBeat>();
        int curLen = 0;
        foreach (var ob in ordered)
        {
            int len = (ob.Beat.Text ?? "").Length + 24; // + inline-break/markup headroom
            bool chapterBreak = ob.Beat.IsChapterStart && cur.Count > 0;
            bool overflow = curLen + len > limit && cur.Count > 0;
            if (chapterBreak || overflow) { segments.Add(cur); cur = new List<OrderedBeat>(); curLen = 0; }
            cur.Add(ob);
            curLen += len;
        }
        if (cur.Count > 0) segments.Add(cur);
        return segments;
    }

    /// <summary>Join a segment's beats into one narration block, inserting an
    /// inline pause between beats so the single recording keeps the strand's
    /// pacing. Accumulates the spoken char count.</summary>
    private string AssembleSegmentText(List<OrderedBeat> seg, ref long chars)
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < seg.Count; i++)
        {
            var beat = seg[i].Beat;
            var text = (beat.Text ?? "").Trim();
            if (text.Length == 0) continue;
            sb.Append(text);
            chars += text.Length;
            if (i < seg.Count - 1)
            {
                var next = seg[i + 1].Beat;
                var pauseMs = beat.GapAfterMs ?? ComputeTrailingSilenceMs(beat, next, settings);
                var secs = Math.Clamp(pauseMs / 1000.0, 0.0, 3.0);
                sb.Append(secs >= 0.1 ? $" <break time=\"{secs:0.0}s\" />\n\n" : "\n\n");
            }
        }
        return sb.ToString();
    }

    private static string? Tail(string? t, int n = 200)
    { if (string.IsNullOrWhiteSpace(t)) return null; var s = t.Trim(); return s.Length > n ? s[^n..] : s; }
    private static string? Head(string? t, int n = 200)
    { if (string.IsNullOrWhiteSpace(t)) return null; var s = t.Trim(); return s.Length > n ? s[..n] : s; }

    /// <summary>Split prose into chunks no longer than <paramref name="limit"/>
    /// characters, breaking on sentence boundaries where possible (and hard-cutting
    /// a single sentence that itself exceeds the limit). The last-ditch tier that
    /// guarantees no single TTS request exceeds the model's per-call budget.</summary>
    private static List<string> SplitToLimit(string text, int limit)
    {
        text = (text ?? "").Trim();
        var chunks = new List<string>();
        if (text.Length == 0) return chunks;
        if (text.Length <= limit) { chunks.Add(text); return chunks; }

        // Sentence-ish units: keep terminal punctuation (and a trailing quote/bracket)
        // with the sentence it closes.
        var units = System.Text.RegularExpressions.Regex
            .Split(text, @"(?<=[\.\!\?…][""'\)\]]?)\s+")
            .Where(s => s.Length > 0)
            .ToList();
        var sb = new System.Text.StringBuilder();
        foreach (var u in units)
        {
            var unit = u;
            // A lone sentence longer than the limit: flush what we have, then hard-cut.
            while (unit.Length > limit)
            {
                if (sb.Length > 0) { chunks.Add(sb.ToString().Trim()); sb.Clear(); }
                chunks.Add(unit[..limit].Trim());
                unit = unit[limit..];
            }
            if (sb.Length > 0 && sb.Length + 1 + unit.Length > limit)
            { chunks.Add(sb.ToString().Trim()); sb.Clear(); }
            if (sb.Length > 0) sb.Append(' ');
            sb.Append(unit);
        }
        if (sb.Length > 0) chunks.Add(sb.ToString().Trim());
        return chunks.Where(c => c.Length > 0).ToList();
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

        // Open a Publish run (1:M header) and stamp the process ledger as we go
        // — beat-assembled per beat, wav-exported, mp3-produced — so the whole
        // pipeline has an accurate, queryable history.
        var pub = new StrandPublication
        {
            Id = Guid.CreateVersion7(),
            StrandId = strandId,
            StartedAt = DateTime.UtcNow,
            Status = "running",
            BeatCount = ordered.Count,
        };
        db.StrandPublications.Add(pub);
        db.StrandAudioEvents.Add(NewAudioEvent(strandId, null, pub.Id, "publish-started",
            $"{ordered.Count} beats; source format {(allWav ? "wav" : "mp3")}"));
        await db.SaveChangesAsync(ct);

        // Publish a live progress snapshot the UI polls to drive the ring loader.
        exportProgress[strandId] = new ExportProgress(0, ordered.Count, null);
        var ffmpeg = ResolveFfmpegPath();
        try
        {
            byte[]? combinedBytes = null; // the final published file's bytes
            string finalExt;

            if (allWav)
            {
                // Stitch beats + precise PCM silence into a temp WAV, then
                // transcode that WAV → MP3 as the published artifact (≈10×
                // smaller, universally playable). The temp WAV streams to disk
                // so a long strand's PCM never sits fully in memory.
                var tmp = Path.Combine(Path.GetTempPath(), $"ss-combine-wav-{Guid.CreateVersion7():N}.wav");
                try
                {
                    long pcmTotal = 0;
                    await using (var fs = new FileStream(tmp, FileMode.Create, FileAccess.ReadWrite, FileShare.None, 64 * 1024, useAsync: true))
                    {
                        fs.Position = 44; // reserve header; patched after data size known
                        for (int i = 0; i < ordered.Count; i++)
                        {
                            ct.ThrowIfCancellationRequested();
                            var o = ordered[i];
                            exportProgress[strandId] = new ExportProgress(i + 1, ordered.Count,
                                string.IsNullOrWhiteSpace(o.Beat.BeatTitle) ? $"Beat {i + 1}" : o.Beat.BeatTitle);
                            var bytes = await ReadAllAudioAsync(o.Beat.AudioPath!, ct);
                            if (bytes == null || bytes.Length <= 44) continue;
                            await fs.WriteAsync(bytes.AsMemory(44), ct);
                            pcmTotal += bytes.Length - 44;
                            db.StrandAudioEvents.Add(NewAudioEvent(strandId, o.Beat.Id, pub.Id, "beat-assembled",
                                $"pos {i + 1}/{ordered.Count}, {bytes.Length - 44} pcm bytes"));

                            if (i < ordered.Count - 1)
                            {
                                var next = ordered[i + 1].Beat;
                                var pauseMs = o.Beat.GapAfterMs ?? ComputeTrailingSilenceMs(o.Beat, next, settings);
                                if (pauseMs > 0)
                                {
                                    var silence = GenerateSilencePcm(pauseMs, sampleRate: 44100, channels: 1, bitsPerSample: 16);
                                    if (silence.Length > 0) { await fs.WriteAsync(silence, ct); pcmTotal += silence.Length; }
                                }
                            }
                        }
                        fs.Position = 0;
                        EpisodeAudioService.WriteWavHeader(fs, checked((int)pcmTotal), 44100, 1, 16);
                    }
                    db.StrandAudioEvents.Add(NewAudioEvent(strandId, null, pub.Id, "wav-exported",
                        $"intermediate combined WAV, {new FileInfo(tmp).Length} bytes"));

                    if (!string.IsNullOrEmpty(ffmpeg))
                    {
                        var tmpMp3 = Path.Combine(Path.GetTempPath(), $"ss-combine-mp3-{Guid.CreateVersion7():N}.mp3");
                        try
                        {
                            await TranscodeWavToMp3Async(ffmpeg, tmp, tmpMp3, ct);
                            combinedBytes = await File.ReadAllBytesAsync(tmpMp3, ct);
                            await audioStore.WriteCombinedAsync(strand.Slug, "mp3", combinedBytes, ct);
                            finalExt = "mp3";
                            db.StrandAudioEvents.Add(NewAudioEvent(strandId, null, pub.Id, "mp3-produced",
                                $"{strand.Slug}/strand.mp3, {combinedBytes.Length} bytes"));
                        }
                        finally { try { File.Delete(tmpMp3); } catch { } }
                    }
                    else
                    {
                        // No ffmpeg → publish the lossless WAV as-is.
                        log.LogWarning("ffmpeg not found — publishing lossless WAV instead of MP3 for strand {S}", strandId);
                        combinedBytes = await File.ReadAllBytesAsync(tmp, ct);
                        await audioStore.WriteCombinedAsync(strand.Slug, "wav", combinedBytes, ct);
                        finalExt = "wav";
                    }
                }
                finally { try { File.Delete(tmp); } catch { } }
            }
            else
            {
                // Beats are already MP3 → concat (ffmpeg-paced silence if available).
                if (string.IsNullOrEmpty(ffmpeg))
                {
                    log.LogWarning("ffmpeg not found — naive MP3 concat without paced gaps for strand {S}", strandId);
                    using var msx = new MemoryStream();
                    int k = 0;
                    foreach (var o in ordered)
                    {
                        ct.ThrowIfCancellationRequested();
                        k++;
                        exportProgress[strandId] = new ExportProgress(k, ordered.Count,
                            string.IsNullOrWhiteSpace(o.Beat.BeatTitle) ? $"Beat {k}" : o.Beat.BeatTitle);
                        var bytes = await ReadAllAudioAsync(o.Beat.AudioPath!, ct);
                        if (bytes == null) continue;
                        await msx.WriteAsync(bytes, ct);
                        db.StrandAudioEvents.Add(NewAudioEvent(strandId, o.Beat.Id, pub.Id, "beat-assembled",
                            $"pos {k}/{ordered.Count}, {bytes.Length} bytes (naive concat)"));
                    }
                    combinedBytes = msx.ToArray();
                    await audioStore.WriteCombinedAsync(strand.Slug, "mp3", combinedBytes, ct);
                    finalExt = "mp3";
                    db.StrandAudioEvents.Add(NewAudioEvent(strandId, null, pub.Id, "mp3-produced",
                        $"{strand.Slug}/strand.mp3, {combinedBytes.Length} bytes (no ffmpeg)"));
                }
                else
                {
                    int Pause(Beat a, Beat b) => a.GapAfterMs ?? ComputeTrailingSilenceMs(a, b, settings);
                    var staged = new List<(OrderedBeat Source, string LocalPath)>(ordered.Count);
                    var stagingDir = Path.Combine(Path.GetTempPath(), $"ss-combine-{Guid.CreateVersion7():N}");
                    Directory.CreateDirectory(stagingDir);
                    try
                    {
                        int k = 0;
                        foreach (var o in ordered)
                        {
                            ct.ThrowIfCancellationRequested();
                            k++;
                            exportProgress[strandId] = new ExportProgress(k, ordered.Count,
                                string.IsNullOrWhiteSpace(o.Beat.BeatTitle) ? $"Beat {k}" : o.Beat.BeatTitle);
                            var local = await audioStore.ResolveLocalPathAsync(o.Beat.AudioPath!, ct);
                            if (local == null)
                            {
                                var bytes = await ReadAllAudioAsync(o.Beat.AudioPath!, ct);
                                if (bytes == null) continue;
                                local = Path.Combine(stagingDir, $"{o.Beat.Id:N}.mp3");
                                await File.WriteAllBytesAsync(local, bytes, ct);
                            }
                            staged.Add((o, local));
                            db.StrandAudioEvents.Add(NewAudioEvent(strandId, o.Beat.Id, pub.Id, "beat-assembled",
                                $"pos {k}/{ordered.Count} (mp3 concat)"));
                        }
                        var stagedOut = Path.Combine(stagingDir, "strand.mp3");
                        await ConcatMp3sWithSilenceAsync(ffmpeg, staged, stagedOut, Pause, ct);
                        combinedBytes = await File.ReadAllBytesAsync(stagedOut, ct);
                        await audioStore.WriteCombinedAsync(strand.Slug, "mp3", combinedBytes, ct);
                        finalExt = "mp3";
                        db.StrandAudioEvents.Add(NewAudioEvent(strandId, null, pub.Id, "mp3-produced",
                            $"{strand.Slug}/strand.mp3, {combinedBytes.Length} bytes"));
                    }
                    finally
                    {
                        try { Directory.Delete(stagingDir, recursive: true); }
                        catch (Exception ex) { log.LogDebug(ex, "Could not clean up combine staging dir {Dir}", stagingDir); }
                    }
                }
            }

            var combinedRel = $"{strand.Slug}/strand.{finalExt}";

            // Dual-write: drop a friendly copy in the user's publish dir
            // (Downloads by default) so the file is easy to find on disk. The
            // internal store copy keeps the in-app player + download endpoint
            // working unchanged.
            string? exportedTo = null;
            if (combinedBytes != null)
            {
                try
                {
                    var outDir = settings?.ResolvePublishOutputDirectory()
                                 ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                    var friendly = $"{SafeFileName(string.IsNullOrWhiteSpace(strand.Title) ? strand.Slug : strand.Title)}.{finalExt}";
                    var outPath = Path.Combine(outDir, friendly);
                    await File.WriteAllBytesAsync(outPath, combinedBytes, ct);
                    exportedTo = outPath;
                    db.StrandAudioEvents.Add(NewAudioEvent(strandId, null, pub.Id, "exported-to-folder", outPath));
                    log.LogInformation("Strand {S} published to {Path}", strandId, outPath);
                }
                catch (Exception ex)
                {
                    log.LogWarning(ex, "Strand {S}: combined file written internally but copy to publish dir failed", strandId);
                }
            }

            strand.CombinedAudioPath = combinedRel;
            strand.Error = null; // clear any stale failure note now that we have a clean publish
            pub.Status = "completed";
            pub.CompletedAt = DateTime.UtcNow;
            pub.Format = finalExt;
            pub.Path = exportedTo ?? combinedRel;
            pub.ByteSize = combinedBytes?.LongLength ?? 0;
            db.StrandAudioEvents.Add(NewAudioEvent(strandId, null, pub.Id, "publish-completed",
                $"{finalExt}, {pub.ByteSize} bytes" + (exportedTo != null ? $" -> {exportedTo}" : "")));
            await db.SaveChangesAsync(ct);
            log.LogInformation("Strand {S} published ({Rel}, {Ext}, {Bytes} bytes)", strandId, combinedRel, finalExt, pub.ByteSize);
            return combinedRel;
        }
        catch (Exception ex)
        {
            pub.Status = "failed";
            pub.CompletedAt = DateTime.UtcNow;
            pub.Error = ex.Message;
            try
            {
                db.StrandAudioEvents.Add(NewAudioEvent(strandId, null, pub.Id, "publish-failed", ex.Message));
                await db.SaveChangesAsync(ct);
            }
            catch { /* best-effort audit write */ }
            throw;
        }
        finally
        {
            exportProgress.TryRemove(strandId, out _);
        }
    }

    /// <summary>Build a <see cref="StrandAudioEvent"/> row for the process ledger.</summary>
    private static StrandAudioEvent NewAudioEvent(Guid strandId, Guid? beatId, Guid? publicationId, string kind, string? detail)
        => new()
        {
            Id = Guid.CreateVersion7(),
            StrandId = strandId,
            BeatId = beatId,
            PublicationId = publicationId,
            At = DateTime.UtcNow,
            Kind = kind,
            Detail = detail is { Length: > 1000 } d ? d[..1000] : detail,
        };

    /// <summary>Transcode a combined WAV to MP3 via ffmpeg (libmp3lame, VBR ~q2
    /// ≈190 kbps). Throws on a non-zero exit so the publish run records the
    /// failure rather than silently shipping a missing/partial file.</summary>
    private async Task TranscodeWavToMp3Async(string ffmpegPath, string wavPath, string mp3Path, CancellationToken ct)
    {
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = ffmpegPath,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        psi.ArgumentList.Add("-y");
        psi.ArgumentList.Add("-i"); psi.ArgumentList.Add(wavPath);
        psi.ArgumentList.Add("-codec:a"); psi.ArgumentList.Add("libmp3lame");
        psi.ArgumentList.Add("-qscale:a"); psi.ArgumentList.Add("2");
        psi.ArgumentList.Add(mp3Path);
        using var proc = System.Diagnostics.Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start ffmpeg for WAV->MP3 transcode.");
        // Drain both pipes concurrently before awaiting exit to avoid a deadlock.
        var outTask = proc.StandardOutput.ReadToEndAsync(ct);
        var errTask = proc.StandardError.ReadToEndAsync(ct);
        await proc.WaitForExitAsync(ct);
        await outTask; var stderr = await errTask;
        if (proc.ExitCode != 0)
            throw new InvalidOperationException($"ffmpeg WAV->MP3 failed (exit {proc.ExitCode}): {stderr}");
    }

    /// <summary>Decode an MP3 segment (as returned by ElevenLabs on non-Pro tiers)
    /// to raw signed-16-bit little-endian PCM, mono, 44.1 kHz — the exact shape the
    /// WAV assembler and <see cref="GenerateSilencePcm"/> expect. ffmpeg writes a temp
    /// .pcm we read back; both temps are cleaned up.</summary>
    private async Task<byte[]> DecodeMp3ToPcmAsync(string ffmpegPath, byte[] mp3Bytes, CancellationToken ct)
    {
        var inMp3 = Path.Combine(Path.GetTempPath(), $"ss-seg-{Guid.CreateVersion7():N}.mp3");
        var outPcm = Path.Combine(Path.GetTempPath(), $"ss-seg-{Guid.CreateVersion7():N}.pcm");
        await File.WriteAllBytesAsync(inMp3, mp3Bytes, ct);
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = ffmpegPath,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            psi.ArgumentList.Add("-y");
            psi.ArgumentList.Add("-i"); psi.ArgumentList.Add(inMp3);
            psi.ArgumentList.Add("-f"); psi.ArgumentList.Add("s16le");
            psi.ArgumentList.Add("-acodec"); psi.ArgumentList.Add("pcm_s16le");
            psi.ArgumentList.Add("-ar"); psi.ArgumentList.Add("44100");
            psi.ArgumentList.Add("-ac"); psi.ArgumentList.Add("1");
            psi.ArgumentList.Add(outPcm);
            using var proc = System.Diagnostics.Process.Start(psi)
                ?? throw new InvalidOperationException("Failed to start ffmpeg for MP3->PCM decode.");
            // Drain both pipes concurrently before awaiting exit to avoid a deadlock.
            var outTask = proc.StandardOutput.ReadToEndAsync(ct);
            var errTask = proc.StandardError.ReadToEndAsync(ct);
            await proc.WaitForExitAsync(ct);
            await outTask; var stderr = await errTask;
            if (proc.ExitCode != 0)
                throw new InvalidOperationException($"ffmpeg MP3->PCM failed (exit {proc.ExitCode}): {stderr}");
            return await File.ReadAllBytesAsync(outPcm, ct);
        }
        finally
        {
            try { File.Delete(inMp3); } catch { }
            try { File.Delete(outPcm); } catch { }
        }
    }

    /// <summary>Strip filesystem-hostile characters from a strand title so it
    /// can be a download filename. Collapses to the slug if nothing survives.</summary>
    private static string SafeFileName(string name)
    {
        var cleaned = new string(name.Select(c => Array.IndexOf(Path.GetInvalidFileNameChars(), c) >= 0 ? '_' : c).ToArray()).Trim();
        return string.IsNullOrWhiteSpace(cleaned) ? "strand" : cleaned;
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
                // Drain BOTH pipes concurrently before awaiting exit — otherwise
                // a full stderr/stdout buffer blocks ffmpeg and we deadlock.
                var outTask = proc.StandardOutput.ReadToEndAsync(ct);
                var errTask = proc.StandardError.ReadToEndAsync(ct);
                await proc.WaitForExitAsync(ct);
                await outTask; var stderr = await errTask;
                if (proc.ExitCode != 0)
                    throw new InvalidOperationException($"ffmpeg silence render failed (exit {proc.ExitCode}): {stderr}");
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
            // Drain BOTH pipes concurrently before awaiting exit — otherwise a
            // full stderr/stdout buffer blocks ffmpeg and we deadlock (which is
            // exactly what hung the first real publish).
            var concatOutTask = concatProc.StandardOutput.ReadToEndAsync(ct);
            var concatErrTask = concatProc.StandardError.ReadToEndAsync(ct);
            await concatProc.WaitForExitAsync(ct);
            await concatOutTask; var concatStderr = await concatErrTask;
            if (concatProc.ExitCode != 0)
                throw new InvalidOperationException($"ffmpeg concat failed (exit {concatProc.ExitCode}): {concatStderr}");
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

    /// <summary>The frozen voice parameters for one strand. Either read back
    /// from the strand's snapshot columns or captured from the current default
    /// profile on first narration. Every beat in the strand renders through
    /// this exact bundle so the narrator stays one continuous performance.</summary>
    private readonly record struct ResolvedVoice(
        string Model, string? VoiceId,
        double Stability, double Similarity, double Style, int Seed);

    /// <summary>Resolve (and, on first narration, lazily persist) the immutable
    /// voice profile for a strand. The FIRST time any beat is narrated we
    /// snapshot the then-current default profile + a deterministic seed onto
    /// the strand; every later (re)record reuses the snapshot. This is what
    /// guarantees a beat recorded today sounds like beats recorded last week,
    /// even if the global default profile/model has changed since. The passed
    /// <paramref name="strand"/> must be tracked by <paramref name="db"/> so
    /// the snapshot write persists.</summary>
    private async Task<ResolvedVoice> ResolveStrandVoiceAsync(
        StreetSamuraiDbContext db, Strand strand, CancellationToken ct)
    {
        var profile = settings?.GetDefaultVoiceProfile();
        var dirty = false;

        if (string.IsNullOrEmpty(strand.VoiceModel))
        {
            strand.VoiceModel      = profile?.Model           ?? settings?.TtsModel ?? "eleven_v3";
            strand.VoiceStability  = profile?.Stability        ?? settings?.TtsStability ?? 0.5;
            strand.VoiceSimilarity = profile?.SimilarityBoost  ?? settings?.TtsSimilarityBoost ?? 0.75;
            strand.VoiceStyle      = profile?.Style            ?? settings?.TtsStyle ?? 0.0;
            // Pin the voice too so it can't drift with the global default later.
            if (string.IsNullOrEmpty(strand.VoiceId))
                strand.VoiceId = profile?.VoiceId;
            dirty = true;
        }
        if (strand.VoiceSeed is null)
        {
            strand.VoiceSeed = DeriveSeed(strand.Id);
            dirty = true;
        }
        if (dirty) await db.SaveChangesAsync(ct);

        return new ResolvedVoice(
            strand.VoiceModel ?? "eleven_v3",
            strand.VoiceId,
            strand.VoiceStability  ?? 0.5,
            strand.VoiceSimilarity ?? 0.75,
            strand.VoiceStyle      ?? 0.0,
            strand.VoiceSeed       ?? 0);
    }

    /// <summary>Deterministic, stable per-strand seed in ElevenLabs' accepted
    /// [0, 2^31-1] range. Derived from the strand's Guid bytes so it never
    /// changes for a given strand and never depends on process-level hashing.</summary>
    private static int DeriveSeed(Guid id)
    {
        var b = id.ToByteArray();
        int v = (b[0] << 24) | (b[1] << 16) | (b[2] << 8) | b[3];
        return v & 0x7FFFFFFF;
    }

    private async Task<string?> SynthesizeAsLosslessWavAsync(
        Beat beat, Strand strand,
        string[] previousRequestIds, string? previousText, string? nextText,
        string? voiceForBeat, BeatPrompt prompt,
        CancellationToken ct)
    {
        var voiceSettings = new TtsVoiceSettings(prompt.Stability, prompt.SimilarityBoost, prompt.Style,
            Seed: prompt.Seed, ModelId: prompt.ModelId);
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
        var voiceSettings = new TtsVoiceSettings(prompt.Stability, prompt.SimilarityBoost, prompt.Style,
            Seed: prompt.Seed, ModelId: prompt.ModelId);
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
