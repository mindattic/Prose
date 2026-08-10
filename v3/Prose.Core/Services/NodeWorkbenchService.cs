using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Interfaces;

namespace Prose.Core.Services;

/// <summary>
/// The one service that drives the unified <c>/node/{id}</c> writer +
/// recorder + listener page. CRUD on beats (edit, insert, split, delete),
/// narration (TTS with stitching, MP3 fallback, cancellation), and
/// combined-audio export. Replaces the
/// <c>EpisodeAudioService</c> + <c>ChapterRecordingService</c> pair: those
/// stay alive for legacy /listen and /recordings pages during the
/// transition, but new code paths flow through here.
///
/// Operates on the unified <see cref="Beat"/> / <see cref="Node"/> /
/// <see cref="BeatNode"/> schema. A Beat appearing in multiple nodes
/// edits in one place; one audio rendering per beat.
/// </summary>
public class NodeWorkbenchService
{
    private readonly IDbContextFactory<ProseDbContext> dbFactory;
    private readonly ElevenLabsTtsService tts;
    private readonly IPathProvider paths;
    private readonly IAudioStore audioStore;
    private readonly SettingsService? settings;
    private readonly ILogger<NodeWorkbenchService> log;
    private static readonly ConcurrentDictionary<Guid, CancellationTokenSource> cancelTokens = new();

    /// <summary>Live per-node progress for an in-flight <see cref="ExportCombinedAsync"/>
    /// (Publish). The UI polls <see cref="GetExportProgress"/> while the export
    /// runs to drive the ring loader. Cleared in the export's finally.</summary>
    private static readonly ConcurrentDictionary<Guid, ExportProgress> exportProgress = new();

    /// <summary>Snapshot of a Publish/combine in progress. <see cref="Current"/>
    /// of <see cref="Total"/> beats stitched; <see cref="Label"/> names the beat
    /// currently being written.</summary>
    public sealed record ExportProgress(int Current, int Total, string? Label);

    /// <summary>Current Publish progress for a node, or null when no export is
    /// running. Read-only poll target for the ring-loader overlay.</summary>
    public ExportProgress? GetExportProgress(Guid nodeId)
        => exportProgress.TryGetValue(nodeId, out var p) ? p : null;

    public NodeWorkbenchService(
        IDbContextFactory<ProseDbContext> dbFactory,
        ElevenLabsTtsService tts,
        IPathProvider paths,
        IAudioStore audioStore,
        ILogger<NodeWorkbenchService> log,
        SettingsService? settings = null,
        EntityRamificationService? ramification = null,
        PostBeatValidationService? postBeatValidator = null,
        SemanticFidelityService? semanticFidelity = null,
        EditSessionService? editSession = null)
    {
        this.dbFactory = dbFactory;
        this.tts = tts;
        this.paths = paths;
        this.audioStore = audioStore;
        this.settings = settings;
        this.log = log;
        this.ramification = ramification;
        this.postBeatValidator = postBeatValidator;
        this.semanticFidelity = semanticFidelity;
        this.editSession = editSession;
    }

    private readonly EntityRamificationService? ramification;
    private readonly PostBeatValidationService? postBeatValidator;
    private readonly SemanticFidelityService? semanticFidelity;
    private readonly EditSessionService? editSession;

    // ── Reads ────────────────────────────────────────────────────────────

    /// <summary>Walk this node's tree (recursing into sub-nodes) and
    /// return its beats in reading order. Each entry includes its source
    /// node so the UI can group beats under sub-node headers when the
    /// caller wants to render a multi-level page.</summary>
    /// <param name="includeDisabled">When true, soft-deleted (IsEnabled=false) beats
    /// are included in the result with <see cref="OrderedBeat.IsEnabled"/> = false.
    /// Default false — the normal writing view only shows live beats.</param>
    public async Task<List<OrderedBeat>> GetOrderedBeatsAsync(Guid nodeId, CancellationToken ct = default, bool includeDisabled = false)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var result = new List<OrderedBeat>();
        // Cycle guard: ParentNodeId is supposed to form a DAG, but a bad
        // data import could close a loop. We track visited nodes so we
        // bail out cleanly instead of blowing the stack.
        var visited = new HashSet<Guid>();
        await WalkAsync(db, nodeId, result, visited, includeDisabled, ct);
        return result;
    }

    private static async Task WalkAsync(ProseDbContext db, Guid nodeId, List<OrderedBeat> acc, HashSet<Guid> visited, bool includeDisabled, CancellationToken ct)
    {
        if (!visited.Add(nodeId)) return; // cycle — already walked this node once.

        // Direct beats first, in SortKey order. Soft-deleted beats are excluded
        // by default; pass includeDisabled=true to make them visible (grey + restore button).
        var direct = await db.BeatNodes
            .Where(sb => sb.NodeId == nodeId && (includeDisabled || sb.IsEnabled))
            .OrderBy(sb => sb.SortKey)
            .Join(db.Beats, sb => sb.BeatId, b => b.Id, (sb, b) => new { sb.SortKey, sb.IsEnabled, Beat = b })
            .ToListAsync(ct);
        foreach (var d in direct)
            acc.Add(new OrderedBeat(d.Beat, nodeId, d.SortKey, d.IsEnabled));

        // Then child nodes in SortKey order — skip book-kind nodes (draft buckets).
        var children = await db.Nodes
            .Where(s => s.ParentNodeId == nodeId && s.Kind != "book")
            .OrderBy(s => s.SortKey)
            .Select(s => s.Id)
            .ToListAsync(ct);
        foreach (var c in children)
            await WalkAsync(db, c, acc, visited, includeDisabled, ct);
    }

    /// <summary>
    /// Recursively resolve every LEAF descendant node id under <paramref name="rootId"/> —
    /// the nodes that actually hold <c>BeatNodes</c> rows directly, at ANY depth. A "leaf" is
    /// any node with no children; a node WITH children is never itself included (its beats,
    /// if any, would have been moved off during a split — see SplitIntoCollectionAsync).
    /// Returns just <c>[rootId]</c> for a flat node with no children.
    ///
    /// 2026-08-09: added after discovering the pervasive one-level idiom
    /// <c>Nodes.Where(n => n.ParentNodeId == nodeId).Select(n => n.Id)</c> — copy-pasted
    /// across dozens of services to gather "the beats under this book" — silently misses
    /// everything once a book contains a nested Collection (a mid-book chapter split into its
    /// own bounded sub-chapters, an existing, tested, documented pattern per
    /// ARCHITECTURE.md §2c, first actually exercised live on 2026-08-09 splitting two
    /// 150-300 beat mega-chapters). Any NEW code that needs "every beat under this node"
    /// should call this instead of reinventing the one-level version.
    ///
    /// Deliberately does NOT apply <see cref="WalkAsync"/>'s Drafts-bucket exclusion
    /// (skipping Kind=="book" children) — that exclusion is specific to assembling the
    /// reader-facing manuscript order via <see cref="GetOrderedBeatsAsync"/>. Audit/analysis
    /// code that wants "every beat under this node, unconditionally, including any Drafts
    /// bucket" should use this method; code that wants the reader's actual assembled text
    /// should keep using GetOrderedBeatsAsync.
    ///
    /// Returns leaves in proper reading order (depth-first, SortKey-ordered at each level) —
    /// callers that need "chapter position" for sorting (e.g. OutlineAdherenceService.
    /// RecalibrateAsync's chapter-then-beat ordering) can rely on list position directly
    /// instead of re-deriving it.
    /// </summary>
    public static async Task<List<Guid>> GetLeafDescendantIdsAsync(
        ProseDbContext db, Guid rootId, CancellationToken ct = default)
    {
        var result = new List<Guid>();
        var visited = new HashSet<Guid>();
        await CollectLeavesAsync(db, rootId, result, visited, ct);
        return result;
    }

    private static async Task CollectLeavesAsync(
        ProseDbContext db, Guid nodeId, List<Guid> result, HashSet<Guid> visited, CancellationToken ct)
    {
        if (!visited.Add(nodeId)) return; // cycle guard

        // IgnoreQueryFilters(): Node has a global HasQueryFilter on ScopedUniverseId
        // (ProseDbContext.OnModelCreating). rootId/nodeId here is always a specific,
        // already-resolved id by the time any caller reaches this helper, so the ambient
        // universe scope is irrelevant to "does this node have children" — a real bug
        // otherwise: a caller invoked with no/mismatched ambient universe scope (e.g. a
        // cross-universe sweep, or a book-agnostic diagnostic command) would see this
        // query silently return 0 children for a node in a different universe than the
        // scope, making CollectLeavesAsync treat that node as a leaf and stop descending —
        // found 2026-08-09 while wiring a new caller (WorkflowMonitorService) that
        // explicitly needs cross-universe traversal.
        var children = await db.Nodes.AsNoTracking().IgnoreQueryFilters()
            .Where(n => n.ParentNodeId == nodeId)
            .OrderBy(n => n.SortKey)
            .Select(n => n.Id)
            .ToListAsync(ct);

        if (children.Count == 0) { result.Add(nodeId); return; }

        foreach (var childId in children)
            await CollectLeavesAsync(db, childId, result, visited, ct);
    }

    /// <summary>Cheap count without loading the beats — for tile/badge displays.
    /// Only counts enabled beats (soft-deleted excluded).</summary>
    public async Task<int> CountBeatsAsync(Guid nodeId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.BeatNodes.CountAsync(sb => sb.NodeId == nodeId && sb.IsEnabled, ct);
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

        var priorVersion = beat.Version;
        var priorHash    = beat.TextHash;
        var trimmed = TextSanitizerService.Sanitize((newText ?? "").Trim());
        if (beat.Text == trimmed) return; // no-op — don't bump UpdatedAt for nothing

        beat.Text          = trimmed;
        beat.TextHash      = ComputeTextHash(trimmed);
        beat.WasCorrected  = true;
        beat.Stale         = true;
        beat.Score         = null;  // text changed → prior score is for the old version
        beat.ScoredAt      = null;
        beat.Version++;
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

        // Fire-and-forget: log this prose edit to the active edit session.
        if (editSession != null)
            _ = Task.Run(() => editSession.TryLogBeatAsync(beatId, priorVersion, priorHash), CancellationToken.None)
                .ContinueWith(t => log.LogError(t.Exception, "EditSession.TryLogBeatAsync background task failed"),
                    TaskContinuationOptions.OnlyOnFaulted);

        // Fire-and-forget: re-index which entities this beat mentions so
        // future entity saves can propagate EntityStale to this beat.
        if (ramification != null)
            _ = Task.Run(() => ramification.IndexBeatMentionsAsync(beatId, trimmed), CancellationToken.None)
                .ContinueWith(t => log.LogError(t.Exception, "IndexBeatMentionsAsync background task failed"),
                    TaskContinuationOptions.OnlyOnFaulted);

        // Fire-and-forget: auto-engage prose quality checks. Resolve slug
        // here while the db context is still open; the validator only needs
        // the slug string + text (no DB access in QuickValidateAsync).
        string? beatSlug = null;
        if (postBeatValidator != null || semanticFidelity != null)
        {
            beatSlug = await db.BeatNodes.AsNoTracking()
                .Where(sb => sb.BeatId == beatId && sb.IsEnabled)
                .Join(db.Nodes, sb => sb.NodeId, s => s.Id, (_, s) => s.Slug)
                .FirstOrDefaultAsync(ct);
        }

        if (postBeatValidator != null && beatSlug != null)
            _ = Task.Run(() => postBeatValidator.QuickValidateAsync(beatSlug, trimmed, beatId), CancellationToken.None)
                .ContinueWith(t => log.LogError(t.Exception, "QuickValidateAsync background task failed"),
                    TaskContinuationOptions.OnlyOnFaulted);

        if (semanticFidelity != null && beatSlug != null && !string.IsNullOrWhiteSpace(beat.Description))
        {
            var number   = beat.Number;
            var slug2    = beatSlug;
            var synopsis = beat.Description!;
            _ = Task.Run(
                    () => semanticFidelity.CheckBeatIntentDriftAsync(number, slug2, trimmed, synopsis),
                    CancellationToken.None)
                .ContinueWith(t => log.LogError(t.Exception, "CheckBeatIntentDriftAsync background task failed"),
                    TaskContinuationOptions.OnlyOnFaulted);
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
        beat.Title          = string.IsNullOrWhiteSpace(update.Title)          ? null : update.Title.Trim();
        beat.Description    = string.IsNullOrWhiteSpace(update.Description)   ? null : update.Description.Trim();
        beat.Subtext        = string.IsNullOrWhiteSpace(update.Subtext)       ? null : update.Subtext.Trim();
        beat.EmotionalTone  = string.IsNullOrWhiteSpace(update.EmotionalTone) ? null : update.EmotionalTone.Trim().ToLowerInvariant();
        beat.PaceHint       = string.IsNullOrWhiteSpace(update.PaceHint)      ? null : update.PaceHint.Trim().ToLowerInvariant();
        beat.StructureRole  = string.IsNullOrWhiteSpace(update.StructureRole) ? null : update.StructureRole.Trim();
        beat.Act            = update.Act;
        beat.SceneType      = string.IsNullOrWhiteSpace(update.SceneType)     ? "scene" : update.SceneType.Trim();
        beat.IsChapterStart = update.IsChapterStart;
        beat.Kind           = string.IsNullOrWhiteSpace(update.Kind)          ? "prose" : update.Kind.Trim().ToLowerInvariant();
        beat.UpdatedAt      = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Deep-duplicate a node (and its sub-node tree) into a brand-new
    /// independent copy. Every beat is cloned into a FRESH Beat row — prose and
    /// narration metadata are preserved, but audio, review scores, and the stale
    /// flag are reset, since a copy has no recordings and no reviews yet. Editing
    /// the duplicate never touches the original (beats are not shared). The root
    /// copy takes <paramref name="newTitle"/>; any child nodes keep their own
    /// titles. The duplicate slots in beside the source under the same parent.
    /// Returns the new root node's id and slug.</summary>
    public async Task<(Guid Id, string Slug)> DuplicateNodeAsync(Guid sourceId, string newTitle, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(newTitle))
            throw new ArgumentException("A title for the duplicate is required.", nameof(newTitle));

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var source = await db.Nodes.AsNoTracking().FirstOrDefaultAsync(s => s.Id == sourceId, ct)
            ?? throw new InvalidOperationException($"Node {sourceId} not found.");

        // Serializable so the sibling-max read + inserts can't race a concurrent
        // create/duplicate under the same parent (mirrors ImportNodeCli).
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

        var siblingMaxSort = source.ParentNodeId.HasValue
            ? await db.Nodes.Where(s => s.ParentNodeId == source.ParentNodeId).Select(s => (double?)s.SortKey).MaxAsync(ct) ?? 0
            : await db.Nodes.Where(s => s.ParentNodeId == null).Select(s => (double?)s.SortKey).MaxAsync(ct) ?? 0;

        // One contiguous block of Beat.Number values, allocated as we walk the tree.
        var nextNumber = new[] { (await db.Beats.MaxAsync(b => (int?)b.Number, ct) ?? 0) + 1 };

        var (rootId, rootSlug) = await CloneNodeSubtreeAsync(
            db, sourceId, newTitle, source.ParentNodeId, siblingMaxSort + 100.0, nextNumber, ct);

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        log.LogInformation("Duplicated node {Src} -> {New} ({Slug})", sourceId, rootId, rootSlug);
        return (rootId, rootSlug);
    }

    /// <summary>Recursively clone one node subtree into newly-added (unsaved)
    /// entities on <paramref name="db"/>. The caller owns the transaction + save.</summary>
    private async Task<(Guid Id, string Slug)> CloneNodeSubtreeAsync(
        ProseDbContext db, Guid srcNodeId, string? titleOverride,
        Guid? newParentId, double sortKey, int[] nextNumber, CancellationToken ct)
    {
        var src = await db.Nodes.AsNoTracking().FirstAsync(s => s.Id == srcNodeId, ct);
        var newId = Guid.CreateVersion7();
        var title = titleOverride ?? src.Title;
        var slug = $"{Slugify(title)}-{newId.ToString("N")[..8]}";

        var clone = NodeFactory.CreateLike(src);
        clone.Id           = newId;
        clone.Slug         = slug;
        clone.Title        = title;
        clone.Kind         = src.Kind;
        clone.Status       = "draft";
        clone.Description  = src.Description;
        clone.VoiceId      = src.VoiceId;
        clone.ParentNodeId = newParentId;
        clone.SortKey      = sortKey;
        db.Nodes.Add(clone);

        // Direct beats in reading order → fresh Beat rows. Audio
        // (AudioPath/NarratedAt/DurationSec/LastRequestId/GapAfterAudioPath),
        // review (Score/ScoredAt), Stale and WasCorrected are intentionally left
        // at defaults — a duplicate has no recordings, no reviews, nothing stale.
        var srcBeats = await db.BeatNodes
            .Where(sb => sb.NodeId == srcNodeId && sb.IsEnabled)
            .OrderBy(sb => sb.SortKey)
            .Join(db.Beats, sb => sb.BeatId, b => b.Id, (sb, b) => new { sb.SortKey, sb.IsEnabled, Beat = b })
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
                Title          = s.Title,
                IsChapterStart = s.IsChapterStart,
                Kind           = s.Kind,
                Description    = s.Description,
                StructureRole  = s.StructureRole,
                Act            = s.Act,
                SceneType      = s.SceneType,
                EmotionalTone  = s.EmotionalTone,
                PaceHint       = s.PaceHint,
                GapAfterMs     = s.GapAfterMs,
                VoiceId        = s.VoiceId,
                CreatedAt      = now,
                UpdatedAt      = now,
            };
            db.Beats.Add(nb);
            db.BeatNodes.Add(new BeatNode { NodeId = newId, BeatId = nb.Id, SortKey = row.SortKey, IsEnabled = row.IsEnabled });
        }

        // Recurse into child nodes, preserving their order.
        var children = await db.Nodes.AsNoTracking()
            .Where(s => s.ParentNodeId == srcNodeId)
            .OrderBy(s => s.SortKey)
            .Select(s => new { s.Id, s.SortKey })
            .ToListAsync(ct);
        foreach (var child in children)
            await CloneNodeSubtreeAsync(db, child.Id, null, newId, child.SortKey, nextNumber, ct);

        return (newId, slug);
    }

    /// <summary>Title → URL slug stem (lowercase, non-alphanumerics to hyphens).
    /// Callers append a short id suffix for global uniqueness.</summary>
    private static string Slugify(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "node";
        var ascii = Regex.Replace(s.ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');
        return string.IsNullOrEmpty(ascii) ? "node" : ascii;
    }

    /// <summary>
    /// Persist a generated story as a first-class <see cref="Node"/> of
    /// <see cref="Beat"/>s — the single story representation. The autonomous
    /// generator and the writer both land here, so everything downstream
    /// (validate → review → harvest → publish) operates on one model. Returns the
    /// new node id. Each non-empty text becomes one beat in order; the first
    /// beat is marked a chapter start when <paramref name="chapterStartFirst"/>.
    /// </summary>
    public async Task<Guid> CreateNodeFromBeatsAsync(
        string title, IReadOnlyList<string> beatTexts, string? description = null,
        string kind = "book", string? seed = null, bool chapterStartFirst = false, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        // Serializable transaction guards the two MaxAsync calls (Node.SortKey and
        // Beat.Number) against concurrent story-creation races producing duplicates.
        await using var tx = await db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable, ct);
        try
        {

        var nodeId = Guid.CreateVersion7();
        var slug = $"{Slugify(string.IsNullOrWhiteSpace(title) ? "untitled" : title)}-{nodeId.ToString("N")[..8]}";

        var maxSort = await db.Nodes.Where(s => s.ParentNodeId == null)
            .Select(s => (double?)s.SortKey).MaxAsync(ct) ?? 0;
        var node = NodeFactory.Create(kind);
        node.Id = nodeId; node.Slug = slug; node.Title = title; node.Status = "draft";
        node.Description = description; node.Seed = seed; node.SortKey = maxSort + 100.0;
        db.Nodes.Add(node);

        var baseNumber = (await db.Beats.MaxAsync(b => (int?)b.Number, ct) ?? 0) + 1;
        double sortKey = 100.0;
        int i = 0;
        foreach (var raw in beatTexts)
        {
            var text = TextSanitizerService.Sanitize((raw ?? "").Trim());
            if (text.Length == 0) continue;
            var beat = new Beat
            {
                Id = Guid.CreateVersion7(),
                Number = baseNumber + i,
                Text = text,
                TextHash = ComputeTextHash(text),
                Kind = "prose",
                SceneType = "scene",
                IsChapterStart = chapterStartFirst && i == 0,
            };
            db.Beats.Add(beat);
            db.BeatNodes.Add(new BeatNode { NodeId = nodeId, BeatId = beat.Id, SortKey = sortKey });
            sortKey += 100.0;
            i++;
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        log.LogInformation("Persisted generated story '{Title}' as node {Slug} ({Beats} beats)", title, slug, i);
        return nodeId;

        } // end try
        catch
        {
            await tx.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    /// <summary>
    /// Create an empty root node (no beats) — the bible-first entry point for a
    /// brand-new story. The author writes/imports the bible and beats afterward
    /// (via the UI, <c>--edit-beat</c>, or <c>--write-story</c>). UniverseId is
    /// stamped to the current universe on save (GLMZ in headless/CLI). Returns the
    /// new id + slug.
    /// </summary>
    /// <param name="title">Display title (required).</param>
    /// <param name="kind">Free-form category — "book" (root), "chapter", etc.</param>
    /// <param name="description">Optional back-of-book description.</param>
    /// <param name="seed">Optional one-line generator seed / logline.</param>
    /// <param name="nodeCode">Optional short reference code (e.g. "SRZR"). Upper-cased;
    /// rejected if already in use by another node in this universe.</param>
    /// <param name="previousNodeId">Optional prior node this one continues (sequel commandments).</param>
    /// <param name="parentNodeId">Optional parent (makes this a sub-node under a book/saga).</param>
    public async Task<(Guid Id, string Slug)> CreateNodeAsync(
        string title, string kind = "book", string? description = null, string? seed = null,
        string? nodeCode = null, Guid? previousNodeId = null, Guid? parentNodeId = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Node title is required.", nameof(title));

        var code = string.IsNullOrWhiteSpace(nodeCode) ? null : nodeCode.Trim().ToUpperInvariant();

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        if (code != null)
        {
            // NodeCode is a GLOBAL namespace, not per-universe: IX_Nodes_NodeCode is a plain
            // unique index on NodeCode alone (no UniverseId), so IgnoreQueryFilters() here
            // matches the real DB constraint. Without it, this check only sees codes within
            // whichever universe is currently scoped and would report a cross-universe clash
            // as "available" — the insert would then fail at SaveChangesAsync with a raw,
            // unhandled DbUpdateException instead of this clean error.
            var clash = await db.Nodes.AsNoTracking().IgnoreQueryFilters()
                .FirstOrDefaultAsync(s => s.NodeCode == code, ct);
            if (clash != null)
                throw new InvalidOperationException(
                    $"NodeCode '{code}' is already in use by '{clash.Title}' ({clash.Slug}).");
        }

        if (parentNodeId is { } pid && !await db.Nodes.AnyAsync(s => s.Id == pid, ct))
            throw new InvalidOperationException($"Parent node {pid} not found.");
        if (previousNodeId is { } prev && !await db.Nodes.AnyAsync(s => s.Id == prev, ct))
            throw new InvalidOperationException($"Previous node {prev} not found.");

        var nodeId = Guid.CreateVersion7();
        var slug = $"{Slugify(title)}-{nodeId.ToString("N")[..8]}";

        await using var sortTx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        var siblingMaxSort = parentNodeId is { } p
            ? await db.Nodes.Where(s => s.ParentNodeId == p).Select(s => (double?)s.SortKey).MaxAsync(ct) ?? 0
            : await db.Nodes.Where(s => s.ParentNodeId == null).Select(s => (double?)s.SortKey).MaxAsync(ct) ?? 0;

        var now = DateTime.UtcNow;
        var node = NodeFactory.Create(kind);
        node.Id             = nodeId;
        node.Slug           = slug;
        node.Title          = title;
        node.NodeCode       = code;
        node.Status         = "draft";
        node.Description    = description;
        node.Seed           = seed;
        node.ParentNodeId   = parentNodeId;
        node.PreviousNodeId = previousNodeId;
        node.SortKey        = siblingMaxSort + 100.0;
        node.CreatedAt      = now;
        node.UpdatedAt      = now;
        db.Nodes.Add(node);
        await db.SaveChangesAsync(ct);
        await sortTx.CommitAsync(ct);
        log.LogInformation("Created empty node '{Title}' ({Slug}) code={Code} kind={Kind}",
            title, slug, code ?? "-", kind);
        return (nodeId, slug);
    }

    /// <summary>
    /// Convert a monolithic node into a Collection (ARCHITECTURE.md §2c): split
    /// its beats at <c>IsChapterStart</c> boundaries into child nodes parented
    /// under it via <c>ParentNodeId</c>. Beats are MOVED (re-pointed), never
    /// copied or rewritten. The parent keeps its identity and its existing Kind
    /// (Kind is left untouched — see the 2026-08-09 fix note inline below for
    /// why forcing it to "book" is wrong for a non-root split); each chapter
    /// becomes a child node with its own 100-step SortKey ladder. Any lead-in
    /// beats before the first chapter mark form an implicit first child. Safe
    /// to call on EITHER a flat top-level book OR an oversized mid-book chapter
    /// (splitting a mega-chapter into bounded sub-chapters) — both are real,
    /// exercised use cases. Returns (childNodes, beatsMoved).
    /// </summary>
    public async Task<(int Children, int Beats)> SplitIntoCollectionAsync(Guid nodeId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var parent = await db.Nodes.FirstOrDefaultAsync(s => s.Id == nodeId, ct)
            ?? throw new InvalidOperationException($"Node {nodeId} not found.");

        // Guard: refuse to split a node that is ALREADY a Collection (has child
        // nodes). Splitting its direct beats too would create a second, parallel
        // set of chapters alongside the existing children — reconcile first.
        var existingChildren = await db.Nodes.CountAsync(s => s.ParentNodeId == nodeId, ct);
        if (existingChildren > 0)
            throw new InvalidOperationException(
                $"'{parent.Title}' already has {existingChildren} child node(s) — it's already a Collection. " +
                "Splitting its direct beats would duplicate chapters. Reconcile the existing children first.");

        var rows = await db.BeatNodes.Where(sb => sb.NodeId == nodeId && sb.IsEnabled)
            .OrderBy(sb => sb.SortKey)
            .Join(db.Beats, sb => sb.BeatId, b => b.Id,
                  (sb, b) => new { sb.BeatId, sb.SortKey, b.IsChapterStart, b.Title })
            .ToListAsync(ct);
        if (rows.Count == 0) throw new InvalidOperationException("Node has no beats to split.");

        // Segment by chapter starts; lead-in beats (before the first mark) become chapter 1.
        var segments = new List<(string Title, List<Guid> Beats)>();
        foreach (var r in rows)
        {
            if (r.IsChapterStart || segments.Count == 0)
            {
                // 2026-08-09 bug fix: this used to take the beat's own Title verbatim as
                // the new chapter node's Title (no "Chapter N —" prefix at all — violates
                // feedback_chapter_title_standard: every chapter node must be "Chapter N"
                // or "Chapter N — Subtitle"), and its blank-title fallback put the words in
                // the wrong order ("{ParentTitle} — Chapter N" instead of "Chapter N — ...").
                // Found while splitting Vigil's End: the 25 new chapters all needed a manual
                // rename afterward. Always emit the canonical format now so no post-split
                // rename is ever needed again.
                var subtitle = (r.Title ?? "").Trim();
                var chapterNum = segments.Count + 1;
                var t = subtitle.Length == 0 ? $"Chapter {chapterNum}" : $"Chapter {chapterNum} — {subtitle}";
                segments.Add((t, new List<Guid>()));
            }
            segments[^1].Beats.Add(r.BeatId);
        }
        if (segments.Count < 2)
            throw new InvalidOperationException($"Node has {segments.Count} chapter segment(s) — nothing to split. Mark IsChapterStart on beats first.");

        // Drop only enabled beat links — disabled (soft-deleted) rows stay on the parent so they remain restorable.
        var oldLinks = await db.BeatNodes.Where(sb => sb.NodeId == nodeId && sb.IsEnabled).ToListAsync(ct);
        db.BeatNodes.RemoveRange(oldLinks);

        double parentSort = 100.0;
        int beatCount = 0;
        foreach (var (title, beatIds) in segments)
        {
            var childId = Guid.CreateVersion7();
            var slug = $"{Slugify(title)}-{childId.ToString("N")[..8]}";
            db.Nodes.Add(new ChapterNode
            {
                Id = childId, Slug = slug, Title = title, Status = "draft",
                ParentNodeId = nodeId, SortKey = parentSort,
            });
            parentSort += 100.0;
            double sk = 100.0;
            foreach (var bid in beatIds)
            {
                db.BeatNodes.Add(new BeatNode { NodeId = childId, BeatId = bid, SortKey = sk });
                sk += 100.0;
                beatCount++;
            }
        }

        // Display label only — the TPH discriminator (NodeType) is fixed at
        // creation, so a split leaf keeps its concrete type. This method's own
        // original assumption ("splitting is only offered on book-level nodes,
        // where type and label already agree") meant this line was always a
        // no-op for its intended use case. 2026-08-09 bug fix: it was NOT a
        // no-op when applied to a CHAPTER (e.g. splitting an oversized mega-chapter
        // into bounded sub-chapters) — forcing Kind="book" there collided with two
        // unrelated pieces of code that give "book" a completely different meaning
        // on a non-root node: (1) NodeWorkbenchService.WalkAsync explicitly SKIPS
        // any child whose Kind=="book" as a deliberate "Drafts bucket" exclusion
        // (see GetOrderedBeats_SkipsDraftChildSubtrees) — this made the split
        // chapter's entire content silently invisible to every reader-facing path
        // that walks from the book root (narration, export, EPUB/PDF/DOCX — nearly
        // everything), (2) BeatGranularityService/SwainAuditService's "list every
        // book in the corpus" queries would incorrectly start including the split
        // chapter as if it were its own top-level book. Only reassign Kind when the
        // node was ALREADY "book" (the one case this was ever meant to affect);
        // otherwise leave it as whatever it already was so it stays a transparent,
        // walked-through interior node. (If it was already "book" this is a true
        // no-op, matching the original code's own stated assumption — nothing to
        // assign in either branch.)
        parent.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        log.LogInformation("Split '{Title}' into a Collection: {Children} child nodes, {Beats} beats.", parent.Title, segments.Count, beatCount);
        return (segments.Count, beatCount);
    }

    /// <summary>
    /// Enforce the "every story has at least one chapter" invariant on a flat
    /// (chapterless) story: wrap ALL of its direct beats into a single new
    /// <see cref="ChapterNode"/> child, re-pointing the beats (never copied or
    /// rewritten) and preserving their reading order. No-op (returns null) if the
    /// story already has chapter children. The lone chapter takes the story's own
    /// title; renderers suppress the heading when a story resolves to one chapter.
    /// Returns the new chapter's id + slug and the number of beats moved.
    /// </summary>
    public async Task<(Guid ChapterId, string Slug, int Beats)?> WrapInSingleChapterAsync(Guid storyId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var story = await db.Nodes.FirstOrDefaultAsync(s => s.Id == storyId, ct)
            ?? throw new InvalidOperationException($"Node {storyId} not found.");

        var existingChildren = await db.Nodes.CountAsync(s => s.ParentNodeId == storyId, ct);
        if (existingChildren > 0) return null; // already chaptered — nothing to do

        var enabled = await db.BeatNodes.Where(sb => sb.NodeId == storyId && sb.IsEnabled)
            .OrderBy(sb => sb.SortKey).ToListAsync(ct);
        if (enabled.Count == 0) throw new InvalidOperationException($"'{story.Title}' has no direct beats to wrap.");

        var childId = Guid.CreateVersion7();
        var slug = $"{Slugify(story.Title)}-{childId.ToString("N")[..8]}";
        db.Nodes.Add(new ChapterNode
        {
            Id = childId, Slug = slug, Title = story.Title, Status = "draft",
            UniverseId = story.UniverseId, ParentNodeId = storyId, SortKey = 100.0,
        });

        // Re-point the enabled beat links onto the new chapter, preserving order.
        db.BeatNodes.RemoveRange(enabled);
        double sk = 100.0;
        foreach (var link in enabled)
        {
            db.BeatNodes.Add(new BeatNode { NodeId = childId, BeatId = link.BeatId, SortKey = sk });
            sk += 100.0;
        }

        story.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        log.LogInformation("Wrapped '{Title}' into a single chapter ({Slug}): {Beats} beats moved.", story.Title, slug, enabled.Count);
        return (childId, slug, enabled.Count);
    }

    /// <summary>Mark a node Canon (or clear it) — the author-only trust gate
    /// (ARCHITECTURE.md §2c): "strong enough to draw conclusions about the
    /// characters and events." Stamps <see cref="Node.CanonAt"/> when set.
    /// Returns false if the node isn't found.</summary>
    public async Task<bool> SetCanonAsync(Guid nodeId, bool canon, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var node = await db.Nodes.FirstOrDefaultAsync(s => s.Id == nodeId, ct);
        if (node == null) return false;
        node.IsCanon = canon;
        node.CanonAt = canon ? DateTime.UtcNow : null;
        node.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        log.LogInformation("Node {Slug} canon = {Canon}", node.Slug, canon);
        return true;
    }

    /// <summary>Insert a brand-new empty beat into <paramref name="nodeId"/>
    /// at a fractional SortKey just after <paramref name="afterBeatId"/>.
    /// Pass <c>null</c> for <paramref name="afterBeatId"/> to insert at the
    /// very top of the node.</summary>
    public async Task<Beat> InsertBeatAsync(Guid nodeId, Guid? afterBeatId, string initialText = "", CancellationToken ct = default)
    {
        initialText = TextSanitizerService.Sanitize(initialText ?? "");
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var ordered = await db.BeatNodes
            .Where(sb => sb.NodeId == nodeId && sb.IsEnabled)
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
            if (pos < 0) throw new InvalidOperationException($"Beat {afterBeatId} not in node {nodeId}.");
            prevSk = ordered[pos].SortKey;
            nextSk = pos + 1 < ordered.Count ? ordered[pos + 1].SortKey : prevSk + 100.0;
        }

        // Auto-restripe before the gap shrinks into IEEE-754 territory. After
        // restripe the targets get fresh 100-step spacing; recompute prevSk
        // and nextSk against the new ladder. Cheap (O(N) one-time) and only
        // triggers after many midpoint inserts between the same two siblings.
        if (nextSk - prevSk < MinSortKeyGap)
        {
            await RestripeSortKeysAsync(nodeId, ct);
            // Restripe ran on its own DbContext and committed fresh SortKeys.
            // Our local `db` still has the old BeatNode instances tracked
            // with their pre-restripe values — a re-query would return those
            // same tracked instances (EF identity resolution), not the new
            // DB values. Detach so the next ToListAsync materialises fresh
            // rows with the post-restripe ladder.
            db.ChangeTracker.Clear();
            ordered = await db.BeatNodes
                .Where(sb => sb.NodeId == nodeId && sb.IsEnabled)
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
                if (pos < 0)
                {
                    prevSk = ordered.Count > 0 ? ordered[^1].SortKey : 0.0;
                    nextSk = prevSk + 100.0;
                }
                else
                {
                    prevSk = ordered[pos].SortKey;
                    nextSk = pos + 1 < ordered.Count ? ordered[pos + 1].SortKey : prevSk + 100.0;
                }
            }
        }

        await using var insertTx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
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
        db.BeatNodes.Add(new BeatNode
        {
            NodeId = nodeId,
            BeatId   = beat.Id,
            SortKey  = (prevSk + nextSk) / 2.0,
        });
        await db.SaveChangesAsync(ct);
        await insertTx.CommitAsync(ct);
        log.LogInformation("Inserted beat {BeatId} into node {NodeId} between SortKey {Prev} and {Next}",
            beat.Id, nodeId, prevSk, nextSk);
        return beat;
    }

    /// <summary>Below this fractional-SortKey gap, an insert/move would
    /// halve the spacing into IEEE-754 territory where subsequent midpoints
    /// stop producing strictly-ordered values. When InsertBeat/MoveBeat
    /// would push below this, we restripe the whole node first so the
    /// new insertion has clean breathing room. 0.001 is empirically safe
    /// across thousands of subdivisions on a 100-step initial spacing.</summary>
    private const double MinSortKeyGap = 0.001;

    /// <summary>Rewrite every <see cref="BeatNode.SortKey"/> in this node
    /// to a fresh 100-step ladder (100, 200, 300, …). Preserves the current
    /// reading order. O(N) and runs in a single transaction. Audio stays
    /// valid — only the junction's SortKey changes.</summary>
    public async Task<int> RestripeSortKeysAsync(Guid nodeId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var siblings = await db.BeatNodes
            .Where(sb => sb.NodeId == nodeId && sb.IsEnabled)
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
        log.LogInformation("Restriped {N} BeatNode rows in node {Node}", siblings.Count, nodeId);
        return siblings.Count;
    }

    /// <summary>Re-slot a beat within its node. Pass <paramref name="afterBeatId"/>=null
    /// to move to the very top; otherwise the beat lands directly after that
    /// sibling. Uses fractional SortKey midpoints so no neighbouring rows need
    /// to be touched. Audio is preserved — only the membership SortKey changes,
    /// the beat's prose and recording stay valid.
    ///
    /// No-op when the beat is already in the requested position. Throws when
    /// the beat is not a member of the node or when <paramref name="afterBeatId"/>
    /// refers to the beat being moved (would create a self-loop).</summary>
    public async Task MoveBeatAsync(Guid nodeId, Guid beatId, Guid? afterBeatId, CancellationToken ct = default)
    {
        if (afterBeatId == beatId)
            throw new InvalidOperationException("Cannot move a beat to a position after itself.");

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var siblings = await db.BeatNodes
            .Where(sb => sb.NodeId == nodeId && sb.IsEnabled)
            .OrderBy(sb => sb.SortKey)
            .ToListAsync(ct);
        var subject = siblings.FirstOrDefault(sb => sb.BeatId == beatId)
            ?? throw new InvalidOperationException($"Beat {beatId} not in node {nodeId}.");

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
            if (pos < 0) throw new InvalidOperationException($"Anchor beat {afterBeatId} not in node {nodeId}.");
            prevSk = others[pos].SortKey;
            nextSk = pos + 1 < others.Count ? others[pos + 1].SortKey : prevSk + 100.0;
        }

        // Same precision guard as InsertBeatAsync — a move that targets a
        // gap below the threshold restripes first, then recomputes against
        // the fresh ladder.
        if (nextSk - prevSk < MinSortKeyGap)
        {
            await RestripeSortKeysAsync(nodeId, ct);
            // Restripe used a separate DbContext; clear ours so the re-read
            // returns fresh post-restripe SortKeys, not the tracked stale
            // values from the first ToListAsync above.
            db.ChangeTracker.Clear();
            siblings = await db.BeatNodes
                .Where(sb => sb.NodeId == nodeId && sb.IsEnabled)
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
        log.LogInformation("Moved beat {Beat} in node {Node} to SortKey {Sk} (after {After})",
            beatId, nodeId, newSortKey, afterBeatId?.ToString() ?? "(top)");
    }

    /// <summary>Enable or disable a beat's membership in a node's reading order, without
    /// touching the Beat row itself (its prose, audio, and any OTHER node's membership of the
    /// same beat all survive untouched — this only affects whether IT reads as part of THIS
    /// node). Reversible: pass enabled=true to restore it. Added 2026-08-09 for a real case —
    /// a beat found sorted into a chapter its content had no connection to (no causal or
    /// thematic link anywhere in that chapter), where forcing a position (top, middle, or end)
    /// would only trade one confusing placement for another. Disabling is the honest fix when
    /// no correct position can be found with the evidence at hand, rather than leaving a beat
    /// wherever it happened to land.</summary>
    public async Task SetBeatMembershipEnabledAsync(Guid nodeId, Guid beatId, bool enabled, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var membership = await db.BeatNodes.FirstOrDefaultAsync(
            bn => bn.NodeId == nodeId && bn.BeatId == beatId, ct)
            ?? throw new InvalidOperationException($"Beat {beatId} has no membership row in node {nodeId}.");
        if (membership.IsEnabled == enabled) return;
        membership.IsEnabled = enabled;
        await db.SaveChangesAsync(ct);
        log.LogInformation("Beat {Beat} membership in node {Node} set to IsEnabled={Enabled}", beatId, nodeId, enabled);
    }

    /// <summary>Split a beat at an explicit character position — what the
    /// writer wants when their cursor is inside the prose. Same shape as
    /// <see cref="SplitBeatAsync"/> but skips the midpoint-search and uses
    /// the caller's split index directly. Snaps to the nearest word
    /// boundary so we never break a word in two.</summary>
    public async Task<Beat> SplitBeatAtAsync(Guid nodeId, Guid beatId, int splitPosition, CancellationToken ct = default)
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

        var siblings = await db.BeatNodes
            .Where(sb => sb.NodeId == nodeId && sb.IsEnabled)
            .OrderBy(sb => sb.SortKey)
            .ToListAsync(ct);
        var pos = siblings.FindIndex(sb => sb.BeatId == beatId);
        if (pos < 0) throw new InvalidOperationException($"Beat {beatId} not in node {nodeId}.");
        var prevSk = siblings[pos].SortKey;
        var nextSk = pos + 1 < siblings.Count ? siblings[pos + 1].SortKey : prevSk + 100.0;

        target.Text         = firstHalf;
        target.TextHash     = ComputeTextHash(firstHalf);
        target.WasCorrected = true;
        target.Stale        = true;
        InvalidateAudioOnBeat(target);
        target.UpdatedAt    = DateTime.UtcNow;

        await using var splitPosTx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        var second = new Beat
        {
            Id            = Guid.CreateVersion7(),
            Number        = await NextBeatNumberAsync(db, ct),
            Text          = secondHalf,
            TextHash      = ComputeTextHash(secondHalf),
            SceneType     = target.SceneType,
            EmotionalTone = target.EmotionalTone,
            PaceHint      = target.PaceHint,
            Act           = target.Act,
            StructureRole = target.StructureRole,
            WasCorrected  = true,
        };
        db.Beats.Add(second);
        db.BeatNodes.Add(new BeatNode
        {
            NodeId = nodeId,
            BeatId   = second.Id,
            SortKey  = (prevSk + nextSk) / 2.0,
        });
        await db.SaveChangesAsync(ct);
        await splitPosTx.CommitAsync(ct);
        log.LogInformation("Split beat {BeatId} at position {Pos} (snapped to {Snap}) → ({First}|{Second}) in node {NodeId}",
            beatId, splitPosition, snapped, firstHalf.Length, secondHalf.Length, nodeId);
        return second;
    }

    /// <summary>Split one beat into two at the nearest sentence boundary
    /// closest to its midpoint. The second half goes into a fresh Beat with
    /// a fractional SortKey between the original and the next sibling. Both
    /// halves lose their audio because the text-boundaries changed; the next
    /// narration pass re-records them.</summary>
    public async Task<Beat> SplitBeatAsync(Guid nodeId, Guid beatId, CancellationToken ct = default)
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

        // Find the target's SortKey in this node to slot the new beat.
        var siblings = await db.BeatNodes
            .Where(sb => sb.NodeId == nodeId && sb.IsEnabled)
            .OrderBy(sb => sb.SortKey)
            .ToListAsync(ct);
        var pos = siblings.FindIndex(sb => sb.BeatId == beatId);
        if (pos < 0) throw new InvalidOperationException($"Beat {beatId} not in node {nodeId}.");
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
            EmotionalTone = target.EmotionalTone,
            PaceHint      = target.PaceHint,
            Act           = target.Act,
            StructureRole = target.StructureRole,
            WasCorrected  = true,
        };
        db.Beats.Add(second);
        db.BeatNodes.Add(new BeatNode
        {
            NodeId = nodeId,
            BeatId   = second.Id,
            SortKey  = (prevSk + nextSk) / 2.0,
        });
        await db.SaveChangesAsync(ct);
        log.LogInformation("Split beat {BeatId} → ({First}|{Second}) in node {NodeId}", beatId, firstHalf.Length, secondHalf.Length, nodeId);
        return second;
    }

    /// <summary>Burst one oversized beat into N beats — one per paragraph.
    /// Splits on blank lines (matches the prose convention used everywhere
    /// else in the engine); falls back to single newlines when an entire
    /// chapter was pasted without blank-line separators. The first paragraph
    /// stays in the original beat; paragraphs 2..N become new beats slotted
    /// into <paramref name="nodeId"/> between the original's SortKey and
    /// the next sibling's. All resulting beats have audio invalidated and
    /// <see cref="Beat.Stale"/>=true. No-ops (returns empty) if the beat is
    /// already a single paragraph.
    ///
    /// Per-node by design: a beat shared across multiple nodes would
    /// only have its new siblings appear in this node. Callers running a
    /// bulk migration over old books should pre-filter to non-shared beats.</summary>
    public async Task<List<Guid>> SplitBeatByParagraphsAsync(Guid nodeId, Guid beatId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var target = await db.Beats.FirstOrDefaultAsync(b => b.Id == beatId, ct)
            ?? throw new InvalidOperationException($"Beat {beatId} not found.");

        var paragraphs = SplitIntoParagraphs(target.Text ?? "");
        if (paragraphs.Count < 2) return new List<Guid>();

        var siblings = await db.BeatNodes
            .Where(sb => sb.NodeId == nodeId && sb.IsEnabled)
            .OrderBy(sb => sb.SortKey)
            .ToListAsync(ct);
        var pos = siblings.FindIndex(sb => sb.BeatId == beatId);
        if (pos < 0) throw new InvalidOperationException($"Beat {beatId} not in node {nodeId}.");
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
                EmotionalTone = target.EmotionalTone,
                PaceHint      = target.PaceHint,
                Act           = target.Act,
                StructureRole = target.StructureRole,
                WasCorrected  = true,
                Stale         = true,
            };
            db.Beats.Add(b);
            db.BeatNodes.Add(new BeatNode
            {
                NodeId = nodeId,
                BeatId   = b.Id,
                SortKey  = prevSk + stride * i,
            });
            newIds.Add(b.Id);
        }

        await db.SaveChangesAsync(ct);
        log.LogInformation("Burst beat {Beat} into {N} paragraphs in node {Node}", beatId, paragraphs.Count, nodeId);
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
    /// Take a chapter node whose prose is sitting in the legacy
    /// <c>Chapter.Html</c> / <c>Chapter.Markdown</c> blob (because it was written
    /// before the Node+Beat schema landed) and burst it into one Beat per
    /// paragraph, attached to the chapter node via BeatNode junctions.
    ///
    /// Idempotent: if the chapter node already has any beats, returns 0 and
    /// leaves them alone. Parses Markdown-flavoured prose conventions:
    /// <list type="bullet">
    /// <item>First <c>#</c> chapter-title line is dropped (already on Node.Title).</item>
    /// <item><c>*Protagonist: …*</c> front-matter line is dropped.</item>
    /// <item><c>## Section Heading</c> becomes the next paragraph beat's
    ///   <see cref="Beat.Title"/>, and the preceding paragraph beat's
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
    public async Task<int> MaterializeChapterFromHtmlAsync(Guid chapterNodeId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var node = await db.Nodes.FirstOrDefaultAsync(s => s.Id == chapterNodeId, ct)
            ?? throw new InvalidOperationException($"Node {chapterNodeId} not found.");

        var existingCount = await db.BeatNodes.CountAsync(sb => sb.NodeId == chapterNodeId && sb.IsEnabled, ct);
        if (existingCount > 0)
        {
            log.LogInformation("Node {S} ({T}) already has {N} beats; not materialising.",
                chapterNodeId, node.Title, existingCount);
            return 0;
        }

        // The legacy Chapter blob is stored as a Records row hanging off the
        // matching Entity (same Guid). Pull the JSON directly so this method
        // doesn't take a dep on IChapterRepository.
        var recordJson = await db.Records.AsNoTracking()
            .Where(r => r.EntityId == chapterNodeId)
            .Select(r => r.Json)
            .FirstOrDefaultAsync(ct);
        if (string.IsNullOrWhiteSpace(recordJson))
        {
            log.LogWarning("Node {S} ({T}): no Chapter record found in Records; skipping.",
                chapterNodeId, node.Title);
            return 0;
        }

        Models.Chapter? chapter;
        try
        {
            chapter = JsonSerializer.Deserialize<Models.Chapter>(recordJson, ChapterJsonOpts);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Node {S} ({T}): Chapter record JSON failed to deserialise; skipping.",
                chapterNodeId, node.Title);
            return 0;
        }
        if (chapter == null) return 0;

        var body = !string.IsNullOrWhiteSpace(chapter.Markdown) ? chapter.Markdown : chapter.Html;
        if (string.IsNullOrWhiteSpace(body))
        {
            log.LogInformation("Node {S} ({T}) has no prose body to materialise.",
                chapterNodeId, node.Title);
            return 0;
        }

        var parsed = ParseChapterBodyIntoBeats(body);
        if (parsed.Count == 0)
        {
            log.LogInformation("Node {S} ({T}) body produced zero paragraphs after parse.",
                chapterNodeId, node.Title);
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
                Title = pb.Title,
                SceneType = pb.SceneType,
                CreatedAt = now,
                UpdatedAt = now,
            };
            db.Beats.Add(beat);
            db.BeatNodes.Add(new BeatNode
            {
                NodeId = chapterNodeId,
                BeatId   = beat.Id,
                SortKey  = sortKey,
            });
            sortKey += 100.0;
        }
        await db.SaveChangesAsync(ct);
        log.LogInformation("Node {S} ({T}): materialised {N} beats from chapter body.",
            chapterNodeId, node.Title, parsed.Count);
        return parsed.Count;
    }

    private static readonly JsonSerializerOptions ChapterJsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private static readonly Regex ChapterBodyBlankLineSplit = new(@"\r?\n\s*\r?\n+", RegexOptions.Compiled);
    private static readonly Regex ChapterBodyProtagonistLine = new(@"^\s*\*Protagonist:\s*[^*]+\*\s*$", RegexOptions.Compiled);
    private static readonly Regex ChapterBodySceneBreak = new(@"^\s*(?:---+|\*\*\*+|[-*]\s*[-*]\s*[-*][-*\s]*)\s*$", RegexOptions.Compiled);

    private record ParsedBeat(string Text, string? Title, string SceneType);

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

            // First H1 line is the chapter title — already on Node.Title.
            if (!firstH1Skipped && firstLine.StartsWith("# ") && !firstLine.StartsWith("## "))
            {
                firstH1Skipped = true;
                continue;
            }

            // Protagonist marker — front matter, drop.
            if (ChapterBodyProtagonistLine.IsMatch(firstLine)) continue;

            // ## Section heading — capture for next beat's Title; mark
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

    /// <summary>Merge this beat's text into the previous beat in the node
    /// (joined by a space), then remove this beat from the node. The
    /// survivor's audio is invalidated because the text grew; the now-empty
    /// beat row is removed if no other node references it.</summary>
    public async Task JoinBeatWithPreviousAsync(Guid nodeId, Guid beatId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var siblings = await db.BeatNodes
            .Where(sb => sb.NodeId == nodeId && sb.IsEnabled)
            .OrderBy(sb => sb.SortKey)
            .ToListAsync(ct);
        var pos = siblings.FindIndex(sb => sb.BeatId == beatId);
        if (pos < 0) throw new InvalidOperationException($"Beat {beatId} not in node {nodeId}.");
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
        db.BeatNodes.Remove(siblings[pos]);

        // Delete the absorbed beat row if no other node still holds it.
        var otherMemberships = await db.BeatNodes
            .Where(sb => sb.BeatId == beatId && sb.NodeId != nodeId && sb.IsEnabled)
            .AnyAsync(ct);
        if (!otherMemberships)
        {
            InvalidateAudioOnBeat(target);
            db.Beats.Remove(target);
        }
        await db.SaveChangesAsync(ct);
        log.LogInformation("Joined beat {Beat} into {Prev} in node {Node}", beatId, prevId, nodeId);
    }

    /// <summary>Soft-delete a beat from a node: sets <c>BeatNode.IsEnabled = false</c>
    /// on the junction row. The Beat row and all its temporal history are preserved;
    /// <see cref="RestoreBeatAsync"/> can un-hide it. Audio is invalidated so a restore
    /// triggers re-narration rather than playing stale audio.</summary>
    public async Task DeleteBeatAsync(Guid nodeId, Guid beatId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var junction = await db.BeatNodes
            .FirstOrDefaultAsync(sb => sb.NodeId == nodeId && sb.BeatId == beatId, ct);
        if (junction == null) return;
        if (!junction.IsEnabled) return; // already disabled — no-op

        junction.IsEnabled = false;

        // Invalidate audio so a future restore triggers fresh narration.
        var beat = await db.Beats.FirstOrDefaultAsync(b => b.Id == beatId, ct);
        if (beat != null) InvalidateAudioOnBeat(beat);

        await db.SaveChangesAsync(ct);
    }

    /// <summary>Restore a previously soft-deleted beat: sets <c>BeatNode.IsEnabled = true</c>.
    /// The beat re-appears in the normal (non-disabled) view. Audio remains stale until
    /// re-narrated.</summary>
    public async Task RestoreBeatAsync(Guid nodeId, Guid beatId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var junction = await db.BeatNodes
            .FirstOrDefaultAsync(sb => sb.NodeId == nodeId && sb.BeatId == beatId, ct);
        if (junction == null || junction.IsEnabled) return;
        junction.IsEnabled = true;
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
    /// a node, keyed by beat id. Drives the cycler arrows' disabled state in
    /// one grouped query. A beat never edited since versioning was enabled has
    /// count 1 (just the current row → both arrows dead).</summary>
    public async Task<Dictionary<Guid, int>> GetBeatVersionCountsAsync(Guid nodeId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        if (!db.Database.IsSqlServer()) return new();
        var rows = await db.Database.SqlQueryRaw<BeatVersionCountRow>(
            """
            SELECT b.Id AS Id, COUNT(*) AS Cnt
            FROM [Beats] FOR SYSTEM_TIME ALL AS b
            WHERE b.Id IN (SELECT BeatId FROM [BeatNodes] WHERE NodeId = {0} AND IsEnabled = 1)
            GROUP BY b.Id
            """, nodeId).ToListAsync(ct);
        return rows.ToDictionary(r => r.Id, r => r.Cnt);
    }

    /// <summary>Like <see cref="GetBeatVersionCountsAsync"/> but scoped to an explicit set of beat IDs.
    /// Use this for book-mode nodes where beats live on ChapterNode children (SS-A43).</summary>
    public async Task<Dictionary<Guid, int>> GetBeatVersionCountsByIdsAsync(IEnumerable<Guid> beatIds, CancellationToken ct = default)
    {
        var idList = beatIds.ToList();
        if (idList.Count == 0) return new();
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        if (!db.Database.IsSqlServer()) return new();
        // Batch into chunks of 500 to keep the IN clause within safe SQL text limits.
        const int batchSize = 500;
        var result = new Dictionary<Guid, int>();
        for (int offset = 0; offset < idList.Count; offset += batchSize)
        {
            var batch = idList.Skip(offset).Take(batchSize).ToList();
            var inClause = string.Join(",", batch.Select(id => $"'{id:D}'"));
            var rows = await db.Database.SqlQueryRaw<BeatVersionCountRow>(
                $"""
                SELECT b.Id AS Id, COUNT(*) AS Cnt
                FROM [Beats] FOR SYSTEM_TIME ALL AS b
                WHERE b.Id IN ({inClause})
                GROUP BY b.Id
                """).ToListAsync(ct);
            foreach (var row in rows)
                result[row.Id] = row.Cnt;
        }
        return result;
    }

    /// <summary>The beat's prose at a newest-first version index — 0 = current,
    /// 1 = the version before the last edit, and so on back through history.
    /// Null when the index is past the end or on a non-temporal provider. Used
    /// by the writer's ◀ ▶ cycler to preview a past version in the editor.</summary>
    public async Task<string?> GetBeatVersionTextAsync(Guid beatId, int index, CancellationToken ct = default)
    {
        var v = await GetBeatVersionAsync(beatId, index, ct);
        return v?.Text;
    }

    /// <summary>The beat's prose AND its <c>SysStart</c> timestamp at a newest-first
    /// version index. Null when the index is past the end or on a non-temporal provider.
    /// Used by the version cycler to show "last edited at …" alongside the preview.</summary>
    public async Task<BeatVersion?> GetBeatVersionAsync(Guid beatId, int index, CancellationToken ct = default)
    {
        if (index < 0) return null;
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        if (!db.Database.IsSqlServer()) return null;
        var rows = await db.Database.SqlQueryRaw<BeatVersionRow>(
            """
            SELECT b.[Text] AS [Text], b.[SysStart] AS [ValidFrom]
            FROM [Beats] FOR SYSTEM_TIME ALL AS b
            WHERE b.Id = {0}
            ORDER BY b.SysStart DESC
            OFFSET {1} ROWS FETCH NEXT 1 ROWS ONLY
            """, beatId, index).ToListAsync(ct);
        var r = rows.FirstOrDefault();
        return r == null ? null : new BeatVersion(r.Text ?? "", r.ValidFrom);
    }

    public sealed record BeatVersion(string Text, DateTime ValidFrom);
    private sealed class BeatVersionRow { public string? Text { get; set; } public DateTime ValidFrom { get; set; } }

    // ── Audio ────────────────────────────────────────────────────────────

    /// <summary>Re-fire narration on every beat in this node (and its
    /// children) that's missing an audio file. Stitches request-ids across
    /// adjacent beats for prosodic continuity. Cancellation supported via
    /// <see cref="CancelNarration"/>.</summary>
    public async Task NarrateAsync(Guid nodeId, CancellationToken ct = default)
    {
        // 1. Validate config BEFORE mutating node state — a misconfigured
        //    account shouldn't leave the node stuck in narrating.
        if (!await tts.IsConfiguredAsync())
            throw new InvalidOperationException("TTS is not configured. Set ElevenLabs API key in Settings.");

        // 2. If a prior narration is already running for this node, cancel
        //    it before starting a new one. Otherwise the old loop keeps
        //    writing audio files for stale beat text alongside the new run.
        if (cancelTokens.TryGetValue(nodeId, out var prior))
        {
            try { prior.Cancel(); } catch { /* prior may already be disposed */ }
            // Give the old loop a beat to roll up and persist its cancelled status.
            await Task.Delay(50, ct);
        }

        using var cancelCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cancelTokens[nodeId] = cancelCts;
        ct = cancelCts.Token;

        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            var node = await db.Nodes.FirstOrDefaultAsync(s => s.Id == nodeId, ct)
                ?? throw new InvalidOperationException($"Node {nodeId} not found.");
            node.Status = "narrating";
            await db.SaveChangesAsync(ct);

            var ordered = await GetOrderedBeatsAsync(nodeId, ct);
            // Reset the per-run progress counters so the polling UI shows
            // current-run state, not a stale lifetime total. Stamp the
            // denominator from this snapshot so it stays stable even if
            // beats are added/removed mid-run.
            node.NarratedBeatCount = 0;
            node.TotalBeatsToNarrate = ordered.Count;
            await db.SaveChangesAsync(ct);
            // Audio bytes are written through IAudioStore — the synth helpers
            // hand the bytes to audioStore.WriteBeatAsync which knows where
            // they live (local disk vs blob). No filesystem prep needed here.

            // Resolve (and snapshot) the node's locked voice profile ONCE
            // before the loop and reuse it for every beat. The snapshot is
            // captured on first narration and frozen on the node, so neither
            // a mid-run settings change NOR a global profile change weeks later
            // can fork the node into two voices. The bundle (model + voice_id
            // + stability/similarity/style + deterministic seed) is used
            // together — that's the whole point. Beats with their own VoiceId
            // still override (future per-character work).
            var voice = await ResolveNodeVoiceAsync(db, node, ct);
            var lockedNodeVoice = voice.VoiceId;
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
                // node sound continuous with its neighbours, instead of
                // pulling from the node's tail.
                var prevIds = new List<string>(3);
                for (int j = idx - 1; j >= 0 && prevIds.Count < 3; j--)
                {
                    var rid = ordered[j].Beat.LastRequestId;
                    if (!string.IsNullOrEmpty(rid)) prevIds.Insert(0, rid);
                }

                var (prevText, nextText) = BuildTextWindow(ordered, idx, contextChars: 1500);
                var tracked = await db.Beats.FirstAsync(b => b.Id == beat.Id, ct);

                // Pick the voice: beat override → node lock → tts default
                // (resolved inside the TTS service). Lock takes precedence
                // even if node.VoiceId was mutated mid-run.
                var voiceForBeat = !string.IsNullOrEmpty(tracked.VoiceId) ? tracked.VoiceId : lockedNodeVoice;

                // Map beat metadata → ElevenLabs prompt + per-request voice_settings.
                // The baseline voice_settings come from the node's LOCKED
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
                            newReqId = await SynthesizeAsLosslessWavAsync(tracked, node, prevIds.ToArray(), prevText, nextText, voiceForBeat, prompt, ct);
                        }
                        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
                        {
                            log.LogWarning("Node {S}: pcm_44100 forbidden — falling back to mp3", node.Slug);
                            useLossless = false;
                            newReqId = await SynthesizeAsMp3Async(tracked, node, prevIds.ToArray(), prevText, nextText, voiceForBeat, prompt, ct);
                        }
                    }
                    else
                    {
                        newReqId = await SynthesizeAsMp3Async(tracked, node, prevIds.ToArray(), prevText, nextText, voiceForBeat, prompt, ct);
                    }
                    // Update the in-memory snapshot so the next iteration's
                    // backward look sees the just-stamped id without an
                    // extra DB round-trip.
                    if (!string.IsNullOrEmpty(newReqId))
                        ordered[idx].Beat.LastRequestId = newReqId;
                    node.CharsNarrated += tracked.Text?.Length ?? 0;
                    // Bump the progress counter so the polling UI reads a
                    // single int instead of scanning the beats collection.
                    node.NarratedBeatCount++;
                    db.NodeAudioEvents.Add(NewAudioEvent(nodeId, tracked.Id, null, "beat-recorded",
                        $"{tracked.Text?.Length ?? 0} chars, voice {voiceForBeat}"));
                    await db.SaveChangesAsync(ct);
                }
                catch (OperationCanceledException)
                {
                    // Cancellation propagates — outer handler rolls the node
                    // into "stopped". Don't count as a failure or eat the token.
                    throw;
                }
                catch (Exception ex)
                {
                    // Per-beat failure: log, record the message on the node
                    // so the UI can surface it, and CONTINUE the loop. One bad
                    // beat (content filter, timeout, weird unicode) used to
                    // abort the whole node and lock every later beat out of
                    // narration; now we keep going and report the partial
                    // result at the end.
                    failedCount++;
                    failedBeatIds.Add(beat.Id);
                    log.LogError(ex, "Narration failed on node {S} beat {B} — skipping and continuing", nodeId, beat.Id);
                    node.Error = failedCount == 1
                        ? $"Beat {beat.Id}: {ex.Message}"
                        : $"{failedCount} beats failed (latest {beat.Id}): {ex.Message}";
                    await db.SaveChangesAsync(ct);
                }
            }

            // Node outcome reflects the per-beat tally:
            //   all beats rendered → "ready"
            //   some beats failed  → "failed" (Error already populated above)
            // Either way AudioCompletedAt stamps so callers can see the run finished.
            node.Status = failedCount == 0 ? "ready" : "failed";
            if (failedCount == 0) node.Error = null; // clear stale failure note on a clean run
            node.AudioCompletedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);

            try { await ExportCombinedAsync(nodeId, ct); }
            catch (Exception ex) { log.LogWarning(ex, "Node {S} combined export failed (non-fatal)", nodeId); }
        }
        catch (OperationCanceledException)
        {
            log.LogInformation("Node {S} narration cancelled", nodeId);
            await using var db2 = await dbFactory.CreateDbContextAsync(CancellationToken.None);
            var st = await db2.Nodes.FirstOrDefaultAsync(s => s.Id == nodeId, CancellationToken.None);
            if (st != null) { st.Status = "stopped"; await db2.SaveChangesAsync(CancellationToken.None); }
        }
        catch (Exception ex)
        {
            // Top-level failure (DB unreachable, TTS service constructor blew
            // up, anything else not caught per-beat). Without this catch the
            // exception would escape NarrateAsync — and every caller is
            // fire-and-forget Task.Run, so the node would stay stuck in
            // "narrating" forever with no signal to the UI. Flip status to
            // "failed" with the exception message so the polling page can
            // recover.
            log.LogError(ex, "Node {S} narration crashed at top level", nodeId);
            try
            {
                await using var db2 = await dbFactory.CreateDbContextAsync(CancellationToken.None);
                var st = await db2.Nodes.FirstOrDefaultAsync(s => s.Id == nodeId, CancellationToken.None);
                if (st != null)
                {
                    st.Status = "failed";
                    st.Error = ex.Message;
                    st.AudioCompletedAt = DateTime.UtcNow;
                    await db2.SaveChangesAsync(CancellationToken.None);
                }
            }
            catch (Exception inner) { log.LogError(inner, "Node {S} failed-status write also failed", nodeId); }
        }
        finally
        {
            cancelTokens.TryRemove(nodeId, out _);
        }
    }

    public bool CancelNarration(Guid nodeId)
    {
        if (cancelTokens.TryGetValue(nodeId, out var cts))
        {
            try { cts.Cancel(); return true; } catch { return false; }
        }
        return false;
    }

    /// <summary>Quick pangram for the Audition button — exercises a broad phoneme
    /// spread in one short clip.</summary>
    public const string AuditionSampleText = "The quick brown fox jumped over the lazy dog.";

    /// <summary>Longer dramatic passage for the Demo button — exercises normal
    /// narration, tension, urgency, and a beat of joy in about 150 words so the
    /// listener hears the voice's full emotional range, not just its timbre.</summary>
    public const string DemoSampleText =
        "The city breathed at 3 AM with a sound no one had named yet. " +
        "Kyle moved through it the way water moves through cracks — not forcing, just finding. " +
        "The job was simple: retrieve the drive before anyone else knew it was missing. " +
        "Simple. He almost laughed. Forty feet up, balanced on a ledge that had no business " +
        "supporting his weight, with a sweep team two blocks out and closing — simple was not " +
        "the word. But then the lock clicked open, the data was in his hand, and somewhere below " +
        "his partner sent a single pulse through his neuretics: got them. Run. " +
        "He ran. And for exactly three seconds, before the shooting started, it felt like joy.";

    /// <summary>
    /// Synthesize a sample passage with an arbitrary voice profile and return
    /// the MP3 bytes — a throwaway preview for the voice studio. NOTHING is
    /// persisted. Uses the node's deterministic seed. Pass <paramref name="text"/>
    /// to override the default <see cref="AuditionSampleText"/>.
    /// </summary>
    public async Task<byte[]> AuditionVoiceAsync(Guid nodeId, Models.VoiceProfile dials,
        string? text = null, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dials);
        if (string.IsNullOrWhiteSpace(dials.VoiceId))
            throw new InvalidOperationException("Pick a voice to audition.");
        if (!await tts.IsConfiguredAsync())
            throw new InvalidOperationException("TTS is not configured (no ElevenLabs API key).");

        var vs = new TtsVoiceSettings(dials.Stability, dials.SimilarityBoost, dials.Style,
            Seed: DeriveSeed(nodeId), ModelId: dials.Model);
        var result = await tts.SynthesizeWithIdAsync(
            text ?? AuditionSampleText, dials.VoiceId, outputFormat: "mp3_44100_128",
            previousRequestIds: null, previousText: null, nextText: null,
            voiceSettings: vs, ct);
        return result.Bytes;
    }

    /// <summary>
    /// Synthesize <paramref name="text"/> through a local engine (kokoro/piper)
    /// and return the result as a WAV byte array (44.1 kHz mono 16-bit).
    /// Used by the node editor for per-beat preview and Voice Studio demo when
    /// the node's TtsEngine is set to a local engine.
    /// Returns null when ffmpeg is not found; throws when the engine is not installed.
    /// </summary>
    public async Task<byte[]?> SynthesizeLocalBeatAsync(string text, string engine, CancellationToken ct = default)
    {
        var local = LocalTts.Resolve(engine, log)
            ?? throw new InvalidOperationException(
                $"Unknown local TTS engine '{engine}'. Options: {string.Join(", ", LocalTts.KnownEngines)}.");
        if (!local.IsAvailable)
            throw new InvalidOperationException(
                $"Local TTS engine '{engine}' is not installed. " +
                $"See tools\\{engine}\\README for one-time setup.");

        var ffmpeg = ResolveFfmpegPath();
        if (string.IsNullOrEmpty(ffmpeg))
            return null; // caller shows friendly message

        var pcm = await local.SynthesizeToPcmAsync(text, ffmpeg, ct);
        return EpisodeAudioService.WrapPcmAsWav(pcm, 44100, 1, 16);
    }

    /// <summary>
    /// Pin a voice profile's full dial set onto the node's snapshot columns so
    /// every later (re)record and publish renders through it — the durable
    /// "tweak the voice for THIS node" path. Overwrites VoiceId/VoiceModel and
    /// the three voice_settings dials; leaves VoiceSeed intact so the node
    /// stays deterministic across the change.
    /// </summary>
    public async Task SetNodeVoiceAsync(Guid nodeId, Models.VoiceProfile dials, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dials);
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var node = await db.Nodes.FirstOrDefaultAsync(s => s.Id == nodeId, ct)
            ?? throw new InvalidOperationException($"Node {nodeId} not found.");

        node.VoiceId         = dials.VoiceId;
        node.VoiceModel      = dials.Model;
        node.VoiceStability  = dials.Stability;
        node.VoiceSimilarity = dials.SimilarityBoost;
        node.VoiceStyle      = dials.Style;
        node.VoiceSeed     ??= DeriveSeed(nodeId);
        db.NodeAudioEvents.Add(NewAudioEvent(nodeId, null, null, "voice-set",
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
    public async Task<bool> NarrateBeatAsync(Guid nodeId, Guid beatId, bool force = false, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var node = await db.Nodes.FirstOrDefaultAsync(s => s.Id == nodeId, ct)
            ?? throw new InvalidOperationException($"Node {nodeId} not found.");

        var ordered = await GetOrderedBeatsAsync(nodeId, ct);
        var idx = ordered.FindIndex(o => o.Beat.Id == beatId);
        if (idx < 0) return false;

        var tracked = await db.Beats.FirstOrDefaultAsync(b => b.Id == beatId, ct);
        if (tracked == null || string.IsNullOrWhiteSpace(tracked.Text)) return false;

        // Idempotent fast-path: already voiced for the current text → nothing to do.
        if (!force && !tracked.Stale && !string.IsNullOrEmpty(tracked.AudioPath))
            return true;

        // ── Local engine path (kokoro / piper) ─────────────────────────────
        var engineName = node.TtsEngine;
        bool isLocal = !string.IsNullOrEmpty(engineName)
            && !string.Equals(engineName, "elevenlabs", StringComparison.OrdinalIgnoreCase);
        if (isLocal)
        {
            var wav = await SynthesizeLocalBeatAsync(tracked.Text, engineName!, ct)
                ?? throw new InvalidOperationException(
                    "ffmpeg is required for local TTS preview but was not found on PATH.");
            var rel = await audioStore.WriteBeatAsync(node.Slug, tracked.Id, "wav", wav, ct);
            tracked.AudioPath  = rel;
            tracked.NarratedAt = DateTime.UtcNow;
            tracked.Stale      = false;
            tracked.TextHash   = ComputeTextHash(tracked.Text);
            db.NodeAudioEvents.Add(NewAudioEvent(nodeId, beatId, null, "beat-recorded",
                $"{tracked.Text.Length} chars, engine {engineName}"));
            await db.SaveChangesAsync(ct);
            return true;
        }

        // ── ElevenLabs path ────────────────────────────────────────────────
        if (!await tts.IsConfiguredAsync())
            throw new InvalidOperationException("TTS is not configured. Set ElevenLabs API key in Settings.");

        // Reuse the node's LOCKED voice snapshot — this is the key to a
        // single-beat re-record sounding like the rest of the node: same
        // model, same voice, same tuning, same deterministic seed as every
        // other beat, regardless of what the global default profile is now.
        var voice         = await ResolveNodeVoiceAsync(db, node, ct);
        var lockedNodeVoice = voice.VoiceId;
        var voiceForBeat  = !string.IsNullOrEmpty(tracked.VoiceId) ? tracked.VoiceId : lockedNodeVoice;
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
                await SynthesizeAsLosslessWavAsync(tracked, node, prevIds.ToArray(), prevText, nextText, voiceForBeat, prompt, ct);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                await SynthesizeAsMp3Async(tracked, node, prevIds.ToArray(), prevText, nextText, voiceForBeat, prompt, ct);
            }
            db.NodeAudioEvents.Add(NewAudioEvent(nodeId, beatId, null, "beat-recorded",
                $"{tracked.Text?.Length ?? 0} chars, voice {voiceForBeat}"));
            await db.SaveChangesAsync(ct);
            return true;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            log.LogError(ex, "Live-broadcast render failed on node {S} beat {B}", nodeId, beatId);
            return false;
        }
    }

    /// <summary>
    /// Export a drift-free audiobook in the FEWEST TTS requests that fit
    /// ElevenLabs' per-request budget, so the narrator is one continuous
    /// performance instead of N separately-recorded beats. Tier 1 = whole node
    /// in one call; Tier 2 = one call per chapter (split at <c>IsChapterStart</c>);
    /// Tier 3 = split an over-long chapter at the char budget. Intra-segment beat
    /// gaps become inline <c>&lt;break&gt;</c> pauses (one recording keeps its
    /// pacing); segment boundaries get exact PCM silence. The combined MP3 is
    /// written to the audio store AND copied to the user's Downloads folder.
    /// Local file rendering only — no KDP/Audible API integration.
    /// </summary>
    public async Task<string?> ExportAudiobookAsync(Guid nodeId, bool retuneRobust = false, string? ttsProvider = null, CancellationToken ct = default)
    {
        // --tts <engine>: free, fully-local narration (no API key, no per-char cost) —
        // piper | kokoro | chatterbox. Same segment/silence/encode assembly; the local
        // engine supplies the PCM. Omitted (or "elevenlabs") = the ElevenLabs path.
        ILocalTtsEngine? local = null;
        if (!string.IsNullOrWhiteSpace(ttsProvider) &&
            !string.Equals(ttsProvider, "elevenlabs", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(ttsProvider, "eleven", StringComparison.OrdinalIgnoreCase))
        {
            local = LocalTts.Resolve(ttsProvider!, log)
                ?? throw new InvalidOperationException(
                    $"Unknown TTS engine '{ttsProvider}'. Options: elevenlabs, {string.Join(", ", LocalTts.KnownEngines)}.");
            if (!local.IsAvailable)
                throw new InvalidOperationException(
                    $"TTS engine '{ttsProvider}' is not installed. See tools\\{ttsProvider}\\README for one-time setup.");
        }
        if (local is null && !await tts.IsConfiguredAsync())
            throw new InvalidOperationException(
                "TTS is not configured (no ElevenLabs API key). For a free local narrator, pass --tts piper|kokoro|chatterbox.");

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var node = await db.Nodes.FirstOrDefaultAsync(s => s.Id == nodeId, ct)
            ?? throw new InvalidOperationException($"Node {nodeId} not found.");

        var ordered = (await GetOrderedBeatsAsync(nodeId, ct))
            .Where(o => !string.IsNullOrWhiteSpace(o.Beat.Text)).ToList();
        if (ordered.Count == 0) { log.LogWarning("Node {S} has no beat text to narrate", nodeId); return null; }

        var voice = await ResolveNodeVoiceAsync(db, node, ct);

        // --robust: deliberately overwrite this node's frozen stability to
        // Robust (1.0). The snapshot normally pins voice params so re-records
        // can't drift; this is the one explicit, opt-in retune that lets an
        // older node (snapshotted at Natural 0.5) adopt the Robust default on
        // re-record. Persisted so every later (re)record stays Robust too.
        if (retuneRobust && node.VoiceStability is not 1.0)
        {
            node.VoiceStability = 1.0;
            await db.SaveChangesAsync(ct);
            voice = voice with { Stability = 1.0 };
            log.LogInformation("Node {S} retuned to Robust (stability 1.0)", nodeId);
        }
        bool isV3 = local is null && voice.Model.Contains("v3", StringComparison.OrdinalIgnoreCase);
        // eleven_v3 caps a request far lower than the v2 family; budget per model.
        // Local engines are uncapped — chapter-sized segments keep memory sane.
        int limit = local != null ? local.CharBudget : isV3 ? 4800 : 9000;
        var segments = BuildAudiobookSegments(ordered, limit);

        var pub = new NodePublication
        {
            Id = Guid.CreateVersion7(), NodeId = nodeId, StartedAt = DateTime.UtcNow,
            Status = "running", BeatCount = ordered.Count,
        };
        db.NodePublications.Add(pub);
        db.NodeAudioEvents.Add(NewAudioEvent(nodeId, null, pub.Id, "publish-started",
            $"one-pass audiobook: {ordered.Count} beats in {segments.Count} segment(s), model {(local != null ? local.Label : voice.Model)}"));
        await db.SaveChangesAsync(ct);

        exportProgress[nodeId] = new ExportProgress(0, segments.Count, "narrating");
        var ffmpeg = ResolveFfmpegPath();
        if (string.IsNullOrEmpty(ffmpeg))
            throw new InvalidOperationException(
                "Audiobook publishing needs ffmpeg on PATH: segments are assembled as PCM and the combined " +
                "track is encoded to MP3 with ffmpeg, and any non-PCM fetch format is decoded back to PCM with it.");
        var tmpWav = Path.Combine(Path.GetTempPath(), $"ss-audiobook-{Guid.CreateVersion7():N}.wav");
        long pcmTotal = 0, chars = 0;
        var prevReqIds = new List<string>();

        // Voice params are frozen per node, so the bundle is constant for
        // every chunk — build it once.
        var vs = new TtsVoiceSettings(voice.Stability, voice.Similarity, voice.Style, Seed: voice.Seed, ModelId: voice.Model);

        // Negotiate the highest-fidelity output format this account/tier allows,
        // once, then lock it for the whole node (one consistent encode). The
        // older path always fetched mp3_44100_128 — a lossy 128 k source that
        // then got decoded + re-encoded, capping quality. pcm_44100 is lossless
        // and skips the MP3 round-trip entirely; mp3_192 beats the universal
        // mp3_128. On a tier/format rejection (401/403/422) we drop to the next.
        string? fetchFormat = null;
        async Task<(byte[] Pcm, string? RequestId)> FetchPcmAsync(
            string chunk, IList<string>? stitchIds, string? pText, string? nText)
        {
            if (local != null)
                return (await local.SynthesizeToPcmAsync(chunk, ffmpeg!, ct), null);
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
                        db.NodeAudioEvents.Add(NewAudioEvent(nodeId, null, pub.Id, "format-negotiated", fmt));
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
                    exportProgress[nodeId] = new ExportProgress(si + 1, segments.Count,
                        string.IsNullOrWhiteSpace(seg[0].Beat.Title) ? $"Segment {si + 1}/{segments.Count}" : seg[0].Beat.Title);

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
                    db.NodeAudioEvents.Add(NewAudioEvent(nodeId, null, pub.Id, "segment-narrated",
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

            // Encode the assembled lossless WAV to the user's configured delivery
            // format (Settings → Audiobook quality; default 320 kbps MP3). Falls
            // back to the lossless WAV when ffmpeg is unavailable.
            var (finalBytes, ext) = await EncodeAudiobookAsync(ffmpeg, tmpWav, ct);

            await audioStore.WriteCombinedAsync(node.Slug, ext, finalBytes, ct);

            // Write to the same per-universe publish dir/subdir as DocxExportService:
            //   {ExportDir(universe)}/{SanitizedTitle}/{SanitizedTitle} {EngineLabel} V{N}.{ext}
            var universeSlug = await db.Universes.AsNoTracking()
                .Where(u => u.Id == node.UniverseId)
                .Select(u => u.Slug)
                .FirstOrDefaultAsync(ct);
            var publishBase = settings is null
                ? Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                : settings.GetExportDirectory(universeSlug);
            var safeTitle = SafeFileName(string.IsNullOrWhiteSpace(node.Title) ? node.Slug : node.Title);
            var nodePubDir = Path.Combine(publishBase, safeTitle);
            Directory.CreateDirectory(nodePubDir);
            var engineLabel = ResolveAudioEngineLabel(ttsProvider);
            var dl = Path.Combine(nodePubDir, $"{safeTitle} {engineLabel} V{node.Version}.{ext}");
            await File.WriteAllBytesAsync(dl, finalBytes, ct);

            node.CombinedAudioPath = $"{node.Slug}/node.{ext}";
            node.CharsNarrated = (int)Math.Min(chars, int.MaxValue);
            node.AudioCompletedAt = DateTime.UtcNow;
            pub.Status = "ready";
            db.NodeAudioEvents.Add(NewAudioEvent(nodeId, null, pub.Id, "mp3-produced",
                $"{node.Slug}/node.{ext}, {finalBytes.Length} bytes; copied to publish dir"));
            await db.SaveChangesAsync(ct);
            exportProgress[nodeId] = new ExportProgress(segments.Count, segments.Count, "done");
            log.LogInformation("Published one-pass audiobook for node {S}: {Seg} segment(s) -> {Path}", nodeId, segments.Count, dl);
            return dl;
        }
        catch (Exception ex)
        {
            pub.Status = "error";
            db.NodeAudioEvents.Add(NewAudioEvent(nodeId, null, pub.Id, "publish-error", ex.Message));
            try { await db.SaveChangesAsync(CancellationToken.None); } catch (Exception dbEx) { log.LogWarning(dbEx, "Failed to save audio error event"); }
            throw;
        }
        finally { try { File.Delete(tmpWav); } catch (Exception delEx) { log.LogWarning(delEx, "Failed to delete temporary audio file"); } }
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
    /// inline pause between beats so the single recording keeps the node's
    /// pacing. Accumulates the spoken char count.
    ///
    /// Each beat's prose is passed through <see cref="NarrationText.Clean"/> to
    /// strip markdown/beat-markers and normalise punctuation, then the assembled
    /// segment receives <see cref="NarrationText.ApplySpeechPronunciation"/> to
    /// fix world-term pronunciations for the TTS engine. Beat entities are never
    /// mutated — only the transient local strings are transformed.</summary>
    private string AssembleSegmentText(List<OrderedBeat> seg, ref long chars)
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < seg.Count; i++)
        {
            var beat = seg[i].Beat;
            // Clean the transient copy — beat.Text (the EF-tracked entity field) is never touched.
            var text = NarrationText.Clean((beat.Text ?? "").Trim());
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
        // Apply spoken-only pronunciation substitutions to the whole assembled segment.
        return NarrationText.ApplySpeechPronunciation(sb.ToString());
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
    /// across child nodes) into one WAV or MP3 at
    /// <c>engine/strands/{slug}/node.wav|mp3</c>.</summary>
    public async Task<string?> ExportCombinedAsync(Guid nodeId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var node = await db.Nodes.FirstOrDefaultAsync(s => s.Id == nodeId, ct)
            ?? throw new InvalidOperationException($"Node {nodeId} not found.");

        var ordered = (await GetOrderedBeatsAsync(nodeId, ct))
            .Where(o => !string.IsNullOrEmpty(o.Beat.AudioPath))
            .ToList();
        if (ordered.Count == 0)
        {
            log.LogWarning("Node {S} has no narrated beats to combine", nodeId);
            return null;
        }
        bool allWav = ordered.All(o => o.Beat.AudioPath!.EndsWith(".wav", StringComparison.OrdinalIgnoreCase));
        bool allMp3 = ordered.All(o => o.Beat.AudioPath!.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase));
        if (!allWav && !allMp3)
        {
            log.LogInformation("Node {S} has mixed-format beats; skipping combined audio", nodeId);
            return null;
        }

        // Gap-after-beat is now a property of the upper beat
        // (Beat.GapAfterMs). Null = "use the auto-computed default" from
        // ComputeTrailingSilenceMs; a value (including 0) is an explicit
        // override the user set in the UI.

        // Open a Publish run (1:M header) and stamp the process ledger as we go
        // — beat-assembled per beat, wav-exported, mp3-produced — so the whole
        // pipeline has an accurate, queryable history.
        var pub = new NodePublication
        {
            Id = Guid.CreateVersion7(),
            NodeId = nodeId,
            StartedAt = DateTime.UtcNow,
            Status = "running",
            BeatCount = ordered.Count,
        };
        db.NodePublications.Add(pub);
        db.NodeAudioEvents.Add(NewAudioEvent(nodeId, null, pub.Id, "publish-started",
            $"{ordered.Count} beats; source format {(allWav ? "wav" : "mp3")}"));
        await db.SaveChangesAsync(ct);

        // Publish a live progress snapshot the UI polls to drive the ring loader.
        exportProgress[nodeId] = new ExportProgress(0, ordered.Count, null);
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
                // so a long node's PCM never sits fully in memory.
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
                            exportProgress[nodeId] = new ExportProgress(i + 1, ordered.Count,
                                string.IsNullOrWhiteSpace(o.Beat.Title) ? $"Beat {i + 1}" : o.Beat.Title);
                            var bytes = await ReadAllAudioAsync(o.Beat.AudioPath!, ct);
                            if (bytes == null || bytes.Length <= 44) continue;
                            await fs.WriteAsync(bytes.AsMemory(44), ct);
                            pcmTotal += bytes.Length - 44;
                            db.NodeAudioEvents.Add(NewAudioEvent(nodeId, o.Beat.Id, pub.Id, "beat-assembled",
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
                    db.NodeAudioEvents.Add(NewAudioEvent(nodeId, null, pub.Id, "wav-exported",
                        $"intermediate combined WAV, {new FileInfo(tmp).Length} bytes"));

                    // Encode to the configured delivery format (default 320 kbps
                    // MP3), or ship the lossless WAV when ffmpeg is unavailable or
                    // WAV is the selected format.
                    (combinedBytes, finalExt) = await EncodeAudiobookAsync(ffmpeg, tmp, ct);
                    await audioStore.WriteCombinedAsync(node.Slug, finalExt, combinedBytes, ct);
                    db.NodeAudioEvents.Add(NewAudioEvent(nodeId, null, pub.Id, "audio-produced",
                        $"{node.Slug}/node.{finalExt}, {combinedBytes.Length} bytes"));
                }
                finally { try { File.Delete(tmp); } catch { } }
            }
            else
            {
                // Beats are already MP3 → concat (ffmpeg-paced silence if available).
                if (string.IsNullOrEmpty(ffmpeg))
                {
                    log.LogWarning("ffmpeg not found — naive MP3 concat without paced gaps for node {S}", nodeId);
                    using var msx = new MemoryStream();
                    int k = 0;
                    foreach (var o in ordered)
                    {
                        ct.ThrowIfCancellationRequested();
                        k++;
                        exportProgress[nodeId] = new ExportProgress(k, ordered.Count,
                            string.IsNullOrWhiteSpace(o.Beat.Title) ? $"Beat {k}" : o.Beat.Title);
                        var bytes = await ReadAllAudioAsync(o.Beat.AudioPath!, ct);
                        if (bytes == null) continue;
                        await msx.WriteAsync(bytes, ct);
                        db.NodeAudioEvents.Add(NewAudioEvent(nodeId, o.Beat.Id, pub.Id, "beat-assembled",
                            $"pos {k}/{ordered.Count}, {bytes.Length} bytes (naive concat)"));
                    }
                    combinedBytes = msx.ToArray();
                    await audioStore.WriteCombinedAsync(node.Slug, "mp3", combinedBytes, ct);
                    finalExt = "mp3";
                    db.NodeAudioEvents.Add(NewAudioEvent(nodeId, null, pub.Id, "mp3-produced",
                        $"{node.Slug}/node.mp3, {combinedBytes.Length} bytes (no ffmpeg)"));
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
                            exportProgress[nodeId] = new ExportProgress(k, ordered.Count,
                                string.IsNullOrWhiteSpace(o.Beat.Title) ? $"Beat {k}" : o.Beat.Title);
                            var local = await audioStore.ResolveLocalPathAsync(o.Beat.AudioPath!, ct);
                            if (local == null)
                            {
                                var bytes = await ReadAllAudioAsync(o.Beat.AudioPath!, ct);
                                if (bytes == null) continue;
                                local = Path.Combine(stagingDir, $"{o.Beat.Id:N}.mp3");
                                await File.WriteAllBytesAsync(local, bytes, ct);
                            }
                            staged.Add((o, local));
                            db.NodeAudioEvents.Add(NewAudioEvent(nodeId, o.Beat.Id, pub.Id, "beat-assembled",
                                $"pos {k}/{ordered.Count} (mp3 concat)"));
                        }
                        var stagedOut = Path.Combine(stagingDir, "node.mp3");
                        await ConcatMp3sWithSilenceAsync(ffmpeg, staged, stagedOut, Pause, ct);
                        combinedBytes = await File.ReadAllBytesAsync(stagedOut, ct);
                        await audioStore.WriteCombinedAsync(node.Slug, "mp3", combinedBytes, ct);
                        finalExt = "mp3";
                        db.NodeAudioEvents.Add(NewAudioEvent(nodeId, null, pub.Id, "mp3-produced",
                            $"{node.Slug}/node.mp3, {combinedBytes.Length} bytes"));
                    }
                    finally
                    {
                        try { Directory.Delete(stagingDir, recursive: true); }
                        catch (Exception ex) { log.LogDebug(ex, "Could not clean up combine staging dir {Dir}", stagingDir); }
                    }
                }
            }

            var combinedRel = $"{node.Slug}/node.{finalExt}";

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
                    var friendly = $"{SafeFileName(string.IsNullOrWhiteSpace(node.Title) ? node.Slug : node.Title)} V{node.Version}.{finalExt}";
                    var outPath = Path.Combine(outDir, friendly);
                    await File.WriteAllBytesAsync(outPath, combinedBytes, ct);
                    exportedTo = outPath;
                    db.NodeAudioEvents.Add(NewAudioEvent(nodeId, null, pub.Id, "exported-to-folder", outPath));
                    log.LogInformation("Node {S} published to {Path}", nodeId, outPath);
                }
                catch (Exception ex)
                {
                    log.LogWarning(ex, "Node {S}: combined file written internally but copy to publish dir failed", nodeId);
                }
            }

            node.CombinedAudioPath = combinedRel;
            node.Error = null; // clear any stale failure note now that we have a clean publish
            pub.Status = "completed";
            pub.CompletedAt = DateTime.UtcNow;
            pub.Format = finalExt;
            pub.Path = exportedTo ?? combinedRel;
            pub.ByteSize = combinedBytes?.LongLength ?? 0;
            db.NodeAudioEvents.Add(NewAudioEvent(nodeId, null, pub.Id, "publish-completed",
                $"{finalExt}, {pub.ByteSize} bytes" + (exportedTo != null ? $" -> {exportedTo}" : "")));
            await db.SaveChangesAsync(ct);
            log.LogInformation("Node {S} published ({Rel}, {Ext}, {Bytes} bytes)", nodeId, combinedRel, finalExt, pub.ByteSize);
            return combinedRel;
        }
        catch (Exception ex)
        {
            pub.Status = "failed";
            pub.CompletedAt = DateTime.UtcNow;
            pub.Error = ex.Message;
            try
            {
                db.NodeAudioEvents.Add(NewAudioEvent(nodeId, null, pub.Id, "publish-failed", ex.Message));
                await db.SaveChangesAsync(ct);
            }
            catch { /* best-effort audit write */ }
            throw;
        }
        finally
        {
            exportProgress.TryRemove(nodeId, out _);
        }
    }

    /// <summary>Build a <see cref="NodeAudioEvent"/> row for the process ledger.</summary>
    private static NodeAudioEvent NewAudioEvent(Guid nodeId, Guid? beatId, Guid? publicationId, string kind, string? detail)
        => new()
        {
            Id = Guid.CreateVersion7(),
            NodeId = nodeId,
            BeatId = beatId,
            PublicationId = publicationId,
            At = DateTime.UtcNow,
            Kind = kind,
            Detail = detail is { Length: > 1000 } d ? d[..1000] : detail,
        };

    /// <summary>Encode the assembled combined WAV to the user's configured
    /// audiobook delivery format (Settings → <see cref="SettingsService.AudiobookFormat"/>;
    /// default 320 kbps MP3). Returns the encoded bytes and the file extension.
    /// When ffmpeg is missing — or the chosen format is lossless WAV — the WAV is
    /// delivered as-is with no re-encode. The ElevenLabs <em>source</em> is always
    /// fetched at the best fidelity the tier allows; this controls only the final
    /// container/bitrate the listener receives.</summary>
    private async Task<(byte[] Bytes, string Ext)> EncodeAudiobookAsync(string? ffmpegPath, string wavPath, CancellationToken ct)
    {
        var (ext, args) = settings?.ResolveAudiobookEncode()
            ?? ("mp3", new[] { "-codec:a", "libmp3lame", "-b:a", "320k" });

        if (string.IsNullOrEmpty(ffmpegPath) || args is null)
        {
            // No encoder available, or lossless-WAV requested → ship the WAV.
            if (string.IsNullOrEmpty(ffmpegPath) && ext != "wav")
                log.LogWarning("ffmpeg not found — delivering lossless WAV instead of {Ext}", ext);
            return (await File.ReadAllBytesAsync(wavPath, ct), "wav");
        }

        var outPath = Path.Combine(Path.GetTempPath(), $"ss-audiobook-{Guid.CreateVersion7():N}.{ext}");
        try
        {
            await EncodeWavAsync(ffmpegPath, wavPath, outPath, args, ct);
            return (await File.ReadAllBytesAsync(outPath, ct), ext);
        }
        finally { try { File.Delete(outPath); } catch { } }
    }

    /// <summary>Run ffmpeg to encode <paramref name="wavPath"/> into
    /// <paramref name="outPath"/> with the given audio-codec argument list (e.g.
    /// <c>-codec:a libmp3lame -b:a 320k</c>). Throws on a non-zero exit so the
    /// publish run records the failure rather than silently shipping a
    /// missing/partial file.</summary>
    private async Task EncodeWavAsync(string ffmpegPath, string wavPath, string outPath, string[] codecArgs, CancellationToken ct)
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
        foreach (var a in codecArgs) psi.ArgumentList.Add(a);
        psi.ArgumentList.Add(outPath);
        using var proc = System.Diagnostics.Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start ffmpeg for audiobook encode.");
        // Drain both pipes concurrently before awaiting exit to avoid a deadlock.
        var outTask = proc.StandardOutput.ReadToEndAsync(ct);
        var errTask = proc.StandardError.ReadToEndAsync(ct);
        await proc.WaitForExitAsync(ct);
        await outTask; var stderr = await errTask;
        if (proc.ExitCode != 0)
            throw new InvalidOperationException($"ffmpeg audiobook encode failed (exit {proc.ExitCode}): {stderr}");
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

    /// <summary>Strip filesystem-hostile characters from a node title so it
    /// can be a download filename. Collapses to the slug if nothing survives.</summary>
    private static string SafeFileName(string name)
    {
        var cleaned = new string(name.Select(c => Array.IndexOf(Path.GetInvalidFileNameChars(), c) >= 0 ? '_' : c).ToArray()).Trim();
        return string.IsNullOrWhiteSpace(cleaned) ? "node" : cleaned;
    }

    /// <summary>Map a raw TTS provider name to a Title-cased label for the
    /// audiobook filename: null/"elevenlabs"/"eleven" → "ElevenLabs",
    /// "piper" → "Piper", "kokoro" → "Kokoro", "chatterbox" → "Chatterbox".
    /// Any unrecognised value is Title-cased as-is.</summary>
    private static string ResolveAudioEngineLabel(string? provider)
    {
        if (string.IsNullOrWhiteSpace(provider) ||
            provider.Equals("elevenlabs", StringComparison.OrdinalIgnoreCase) ||
            provider.Equals("eleven", StringComparison.OrdinalIgnoreCase))
            return "ElevenLabs";
        if (provider.Equals("piper", StringComparison.OrdinalIgnoreCase))      return "Piper";
        if (provider.Equals("kokoro", StringComparison.OrdinalIgnoreCase))     return "Kokoro";
        if (provider.Equals("chatterbox", StringComparison.OrdinalIgnoreCase)) return "Chatterbox";
        // Fallback: capitalise the first letter.
        return char.ToUpperInvariant(provider[0]) + provider[1..];
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
    // honours PROSE_MUTABLE_DATA_ROOT (set to D:\home\data\Prose on
    // Azure) so audio survives deploys and stays writable. On local dev with
    // no env var, MutableDataDir falls back to the same engine/data path as
    // before, so the dev experience doesn't change.
    public string GetAudioRoot() => Path.Combine(paths.MutableDataDir, "nodes");
    public string GetNodeRoot(string slug) => Path.Combine(paths.MutableDataDir, "nodes", slug);

    /// <summary>Resolve a relative audio path to an absolute file path. Tries
    /// the new MutableDataDir-rooted nodes tree first, then falls back to
    /// (a) the pre-2026-05-24 nodes location at <c>{DataRoot}/engine/strands/</c>
    /// and (b) the even older episode-era location at <c>{DataRoot}/engine/episodes/</c>.
    /// Files migrate forward as they're re-recorded; nothing physically moves
    /// from the legacy locations. Returns the primary path even when no file
    /// exists anywhere — callers check <see cref="File.Exists"/> and 404 from there.</summary>
    public string ResolveAudioFile(string relativePath)
    {
        var rel = relativePath.Replace('/', Path.DirectorySeparatorChar);
        var primary = Path.Combine(GetAudioRoot(), rel);
        if (File.Exists(primary)) return primary;
        var legacyNodes = Path.Combine(paths.DataRoot, "engine", "nodes", rel);
        if (File.Exists(legacyNodes)) return legacyNodes;
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
    private static async Task<int> NextBeatNumberAsync(ProseDbContext db, CancellationToken ct)
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
    /// node.</summary>
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
        var tmpDir = Path.Combine(Path.GetTempPath(), $"prose-concat-{Guid.CreateVersion7():N}");
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

    /// <summary>The frozen voice parameters for one node. Either read back
    /// from the node's snapshot columns or captured from the current default
    /// profile on first narration. Every beat in the node renders through
    /// this exact bundle so the narrator stays one continuous performance.</summary>
    private readonly record struct ResolvedVoice(
        string Model, string? VoiceId,
        double Stability, double Similarity, double Style, int Seed);

    /// <summary>Resolve (and, on first narration, lazily persist) the immutable
    /// voice profile for a node. The FIRST time any beat is narrated we
    /// snapshot the then-current default profile + a deterministic seed onto
    /// the node; every later (re)record reuses the snapshot. This is what
    /// guarantees a beat recorded today sounds like beats recorded last week,
    /// even if the global default profile/model has changed since. The passed
    /// <paramref name="node"/> must be tracked by <paramref name="db"/> so
    /// the snapshot write persists.</summary>
    private async Task<ResolvedVoice> ResolveNodeVoiceAsync(
        ProseDbContext db, Node node, CancellationToken ct)
    {
        var profile = settings?.GetDefaultVoiceProfile();
        var dirty = false;

        if (string.IsNullOrEmpty(node.VoiceModel))
        {
            node.VoiceModel      = profile?.Model           ?? settings?.TtsModel ?? "eleven_v3";
            node.VoiceStability  = profile?.Stability        ?? settings?.TtsStability ?? 0.5;
            node.VoiceSimilarity = profile?.SimilarityBoost  ?? settings?.TtsSimilarityBoost ?? 0.75;
            node.VoiceStyle      = profile?.Style            ?? settings?.TtsStyle ?? 0.0;
            // Pin the voice too so it can't drift with the global default later.
            if (string.IsNullOrEmpty(node.VoiceId))
                node.VoiceId = profile?.VoiceId;
            dirty = true;
        }
        if (node.VoiceSeed is null)
        {
            node.VoiceSeed = DeriveSeed(node.Id);
            dirty = true;
        }
        if (dirty) await db.SaveChangesAsync(ct);

        return new ResolvedVoice(
            node.VoiceModel ?? "eleven_v3",
            node.VoiceId,
            node.VoiceStability  ?? 0.5,
            node.VoiceSimilarity ?? 0.75,
            node.VoiceStyle      ?? 0.0,
            node.VoiceSeed       ?? 0);
    }

    /// <summary>Deterministic, stable per-node seed in ElevenLabs' accepted
    /// [0, 2^31-1] range. Derived from the node's Guid bytes so it never
    /// changes for a given node and never depends on process-level hashing.</summary>
    private static int DeriveSeed(Guid id)
    {
        var b = id.ToByteArray();
        int v = (b[0] << 24) | (b[1] << 16) | (b[2] << 8) | b[3];
        return v & 0x7FFFFFFF;
    }

    private async Task<string?> SynthesizeAsLosslessWavAsync(
        Beat beat, Node node,
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
        var rel = await audioStore.WriteBeatAsync(node.Slug, beat.Id, "wav", wav, ct);

        beat.AudioPath     = rel;
        beat.NarratedAt    = DateTime.UtcNow;
        beat.DurationSec   = result.Bytes.Length / 88200.0;
        beat.TextHash      = ComputeTextHash(beat.Text);
        beat.LastRequestId = result.RequestId;
        beat.Stale         = false;
        return result.RequestId;
    }

    private async Task<string?> SynthesizeAsMp3Async(
        Beat beat, Node node,
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

        var rel = await audioStore.WriteBeatAsync(node.Slug, beat.Id, "mp3", result.Bytes, ct);

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

    /// <summary>A beat in reading-order context. Carries the parent node id
    /// so multi-level UIs can group beats by source.</summary>
    public record OrderedBeat(Beat Beat, Guid NodeId, double SortKey, bool IsEnabled = true);

    /// <summary>The fields the UI's per-beat "details" panel can edit. None
    /// of these touch prose or audio — they just steer the narration's
    /// tone the next time the beat is re-recorded.</summary>
    public record BeatMetadataUpdate(
        string? Title,
        string? Description,
        string? Subtext,
        string? EmotionalTone,
        string? PaceHint,
        string? StructureRole,
        int Act,
        string? SceneType,
        bool IsChapterStart,
        string? Kind);

    /// <summary>Map a Node.Status value to a Bootstrap chip color name.
    /// Single source of truth — used by /node/{id}, /nodes, any
    /// future node-aware view. Keeps colors consistent so the user
    /// learns one visual language.</summary>
    public static string StatusColor(string status)
    {
        if (string.IsNullOrEmpty(status)) return "secondary";
        // Fixed system states
        return status switch
        {
            "ready"           => "success",
            "ready_for_audio" => "info",
            "narrating"       => "primary",
            "generating"      => "primary",
            "failed"          => "danger",
            "stopped"         => "warning",
            "draft"           => "secondary",
            "archived"        => "dark",
            _ => status.StartsWith("Complete", StringComparison.OrdinalIgnoreCase) ||
                 status.StartsWith("Canon",    StringComparison.OrdinalIgnoreCase)
                    ? "success"
                    : status.StartsWith("Act ", StringComparison.OrdinalIgnoreCase) ||
                      status.StartsWith("In progress", StringComparison.OrdinalIgnoreCase)
                        ? "info"
                        : "secondary",
        };
    }

    /// <summary>Human-readable rendering of a Node.Status value. Underscores
    /// become spaces; status names are kept lowercase so the badge's
    /// text-uppercase CSS gives them a consistent look. Single helper so
    /// both /nodes and /node/{id} render statuses identically.</summary>
    public static string StatusLabel(string status) =>
        string.IsNullOrEmpty(status) ? "draft" : status.Replace('_', ' ');
}
