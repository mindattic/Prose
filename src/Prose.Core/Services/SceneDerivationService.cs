using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Core.Services;

/// <summary>
/// Groups a chapter's existing beats into scenes, without altering a word of prose.
///
/// <para>A scene is the run of beats playing in one continuous time and place. That definition is
/// directly measurable from fields every beat already carries — where it happens
/// (<see cref="Beat.PlaceEntityId"/> / <see cref="Beat.PlaceName"/>) and how long after the previous
/// one (<see cref="Beat.ElapsedMinutesSincePrevious"/>, <see cref="Beat.InWorldDate"/>) — so this is
/// deterministic, free, and repeatable. No LLM call, no judgement about the writing, nothing that
/// could produce a different answer on a second run.</para>
///
/// <para><b>It derives, it never re-beats.</b> Beats are not split, merged, reworded or reordered.
/// The only change on apply is that a <see cref="SceneNode"/> is inserted between the chapter and
/// its beats, and the beats' <see cref="BeatNode.NodeId"/> is repointed at it. Reading order is
/// preserved exactly, because scenes inherit the SortKey of the first beat they contain.</para>
///
/// <para><b>Coverage is reported before findings.</b> A chapter whose beats carry no place and no
/// timing data yields one scene per chapter — which is not a derivation, it is the absence of one.
/// Saying so is the difference between "this chapter is a single scene" and "I could not tell".</para>
/// </summary>
public class SceneDerivationService(
    IDbContextFactory<ProseDbContext> dbFactory,
    ILogger<SceneDerivationService> log)
{
    /// <summary>
    /// A gap at or above this many in-world minutes ends a scene even when the place is unchanged —
    /// the "later that evening, same bar" cut. An hour is deliberately conservative: it will merge
    /// two scenes that are genuinely separate rather than split one that is not, and a missed split
    /// is easier for an author to spot than a spurious one.
    /// </summary>
    public const int DefaultTimeGapMinutes = 60;

    public sealed record BeatRef(Guid BeatId, int Number, double SortKey, string? PlaceName, Guid? PlaceEntityId);

    public sealed record ProposedScene(
        string Title, string Reason, IReadOnlyList<BeatRef> Beats)
    {
        public int BeatCount => Beats.Count;
    }

    public sealed record ChapterPlan(
        Guid ChapterNodeId, string ChapterTitle, IReadOnlyList<ProposedScene> Scenes,
        int BeatsTotal, int BeatsWithPlace, int BeatsWithTiming, bool AlreadyHasScenes)
    {
        /// <summary>No beat carried either signal, so every split rule was inert.</summary>
        public bool CouldNotLook => BeatsWithPlace == 0 && BeatsWithTiming == 0;
    }

    public sealed record DerivationReport(
        Guid NodeId, string NodeTitle, IReadOnlyList<ChapterPlan> Chapters, bool Applied)
    {
        public int ScenesProposed => Chapters.Sum(c => c.Scenes.Count);
        public int ChaptersCouldNotLook => Chapters.Count(c => c.CouldNotLook);
    }

    /// <summary>
    /// Plan (and optionally apply) scene derivation for every chapter beneath <paramref name="nodeId"/>.
    /// </summary>
    /// <param name="apply">False plans only and writes nothing.</param>
    public async Task<DerivationReport> DeriveAsync(
        Guid nodeId, bool apply = false, int timeGapMinutes = DefaultTimeGapMinutes,
        CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        // IgnoreQueryFilters: an explicit id the caller already holds.
        var root = await db.Nodes.AsNoTracking().IgnoreQueryFilters()
            .FirstOrDefaultAsync(n => n.Id == nodeId, ct)
            ?? throw new InvalidOperationException($"Node not found: {nodeId}");

        var leafIds = await NodeWorkbenchService.GetLeafDescendantIdsAsync(db, nodeId, ct);
        var plans = new List<ChapterPlan>();

        foreach (var leafId in leafIds)
        {
            // leafIds includes the root itself when the root holds beats directly — which is the
            // normal case when a single chapter is passed in to derive just that chapter. Only a
            // root with no beats of its own (a book, a series) has nothing to derive here, and the
            // beat lookup below already yields nothing for those.
            var leaf = await db.Nodes.AsNoTracking().IgnoreQueryFilters()
                .FirstOrDefaultAsync(n => n.Id == leafId, ct);
            if (leaf == null) continue;

            // A node that is already a scene has nothing to derive; re-running must be a no-op.
            if (leaf is SceneNode) continue;

            var beats = await (
                from bn in db.BeatNodes.AsNoTracking().IgnoreQueryFilters()
                join b in db.Beats.AsNoTracking() on bn.BeatId equals b.Id
                where bn.NodeId == leafId
                orderby bn.SortKey
                select new
                {
                    b.Id, b.Number, bn.SortKey, b.PlaceName, b.PlaceEntityId,
                    b.ElapsedMinutesSincePrevious, b.InWorldDate,
                }).ToListAsync(ct);

            if (beats.Count == 0) continue;

            var withPlace = beats.Count(x => x.PlaceEntityId != null || !string.IsNullOrWhiteSpace(x.PlaceName));
            var withTiming = beats.Count(x => x.ElapsedMinutesSincePrevious != null || x.InWorldDate != null);

            var scenes = new List<ProposedScene>();
            var current = new List<BeatRef>();
            var currentReason = "chapter opens";

            void Flush()
            {
                if (current.Count == 0) return;
                var place = current.Select(b => b.PlaceName).FirstOrDefault(p => !string.IsNullOrWhiteSpace(p));
                var title = string.IsNullOrWhiteSpace(place) ? $"Scene {scenes.Count + 1}" : place!.Trim();
                scenes.Add(new ProposedScene(title, currentReason, current.ToList()));
                current.Clear();
            }

            for (var i = 0; i < beats.Count; i++)
            {
                var b = beats[i];
                if (i > 0)
                {
                    var prev = beats[i - 1];
                    string? reason = null;

                    // Place change. Prefer the resolved entity id; fall back to the free-text name
                    // only when both sides have one, so an unlabelled beat does not read as a move.
                    if (b.PlaceEntityId != null && prev.PlaceEntityId != null && b.PlaceEntityId != prev.PlaceEntityId)
                        reason = $"place changes to {b.PlaceName ?? b.PlaceEntityId.ToString()}";
                    else if (b.PlaceEntityId == null && prev.PlaceEntityId == null
                             && !string.IsNullOrWhiteSpace(b.PlaceName) && !string.IsNullOrWhiteSpace(prev.PlaceName)
                             && !SamePlace(prev.PlaceName, b.PlaceName))
                        reason = $"place changes to {b.PlaceName.Trim()}";

                    // Time gap, same place — the "later that night" cut.
                    if (reason == null && b.ElapsedMinutesSincePrevious is { } gap && gap >= timeGapMinutes)
                        reason = $"{gap} minutes elapse";

                    // Calendar day changes.
                    if (reason == null && b.InWorldDate is { } now && prev.InWorldDate is { } before
                        && now.Date != before.Date)
                        reason = $"day changes to {now:yyyy-MM-dd}";

                    if (reason != null) { Flush(); currentReason = reason; }
                }

                current.Add(new BeatRef(b.Id, b.Number, b.SortKey, b.PlaceName, b.PlaceEntityId));
            }
            Flush();

            plans.Add(new ChapterPlan(leafId, leaf.Title, scenes, beats.Count, withPlace, withTiming,
                AlreadyHasScenes: false));
        }

        if (apply)
            await ApplyAsync(db, plans, ct);

        return new DerivationReport(root.Id, root.Title, plans, apply);
    }

    /// <summary>
    /// Insert the planned SceneNodes and repoint their beats. Skips any chapter that would yield a
    /// single scene — wrapping a whole chapter in one scene adds a tree level and says nothing.
    /// </summary>
    private async Task ApplyAsync(ProseDbContext db, List<ChapterPlan> plans, CancellationToken ct)
    {
        foreach (var plan in plans)
        {
            if (plan.Scenes.Count < 2) continue;

            var chapter = await db.Nodes.IgnoreQueryFilters().FirstAsync(n => n.Id == plan.ChapterNodeId, ct);

            // One transaction per chapter: an interruption after the first scene left the chapter
            // with a child node AND beats of its own, and the leaf walk stops at a node with
            // children — those beats dropped out of reading order and export.
            await using var tx = db.Database.CurrentTransaction == null ? await db.Database.BeginTransactionAsync(ct) : null;

            for (var i = 0; i < plan.Scenes.Count; i++)
            {
                var proposed = plan.Scenes[i];
                var scene = new SceneNode
                {
                    Id = Guid.CreateVersion7(),
                    UniverseId = chapter.UniverseId,
                    ParentNodeId = chapter.Id,
                    Title = proposed.Title,
                    // Scenes inherit the SortKey of their first beat, so reading order is preserved
                    // exactly rather than renumbered.
                    SortKey = proposed.Beats[0].SortKey,
                    Slug = await UniqueSlugAsync(db, chapter.UniverseId, proposed.Title, ct),
                    Status = chapter.Status,
                };
                db.Nodes.Add(scene);
                await db.SaveChangesAsync(ct);

                // BeatNode.NodeId is part of the composite key, so a membership cannot be
                // repointed in place — it is removed and re-added, the same way
                // NodeWorkbenchService.MoveBeatToNodeAsync does it. The original SortKey is carried
                // over unchanged: this moves a beat one level down the tree, not along the page.
                var beatIds = proposed.Beats.Select(b => b.BeatId).ToList();
                var rows = await db.BeatNodes.IgnoreQueryFilters()
                    .Where(bn => bn.NodeId == plan.ChapterNodeId && beatIds.Contains(bn.BeatId))
                    .ToListAsync(ct);
                foreach (var row in rows)
                {
                    db.BeatNodes.Remove(row);
                    db.BeatNodes.Add(new BeatNode { NodeId = scene.Id, BeatId = row.BeatId, SortKey = row.SortKey });
                }
                await db.SaveChangesAsync(ct);

                log.LogInformation("[derive-scenes] {Chapter}: scene '{Scene}' took {Count} beat(s) ({Reason})",
                    chapter.Title, scene.Title, rows.Count, proposed.Reason);
            }
            if (tx != null) await tx.CommitAsync(ct);
        }
    }

    /// <summary>
    /// Whether two free-text place labels describe the same dramatic location.
    ///
    /// <para>A scene is continuous in place, but "place" means where the scene is set, not where the
    /// camera is standing. Location extraction records the camera: a single infiltration yields
    /// "building exterior, fire escape", then "building interior, floor", then "building interior,
    /// room". Treating each as a move turns one continuous sequence into three scenes, which is how
    /// a first run of this produced 293 scenes for 521 beats — 1.8 beats per scene, i.e. beats with
    /// extra steps.</para>
    ///
    /// <para>Two labels continue the same scene when one contains the other ("dock" ⊂ "dock, forty
    /// stories up") or when they open on the same significant word ("building exterior…" /
    /// "building interior…"). The leading word is what these labels use for the location proper,
    /// with the qualifier after it. This deliberately errs toward merging: a missed split leaves a
    /// long scene an author can see and divide, while a spurious one fragments a sequence that read
    /// as continuous, and is much harder to spot.</para>
    /// </summary>
    public static bool SamePlace(string? a, string? b)
    {
        var x = Normalize(a);
        var y = Normalize(b);
        if (x.Length == 0 || y.Length == 0) return true;   // unlabelled is not evidence of a move
        if (x == y) return true;
        if (x.Contains(y, StringComparison.Ordinal) || y.Contains(x, StringComparison.Ordinal)) return true;

        var xf = FirstSignificantWord(x);
        var yf = FirstSignificantWord(y);
        return xf.Length > 0 && xf == yf;

        static string Normalize(string? s) =>
            new string((s ?? "").ToLowerInvariant().Where(c => char.IsLetterOrDigit(c) || c == ' ' || c == ',').ToArray())
                .Trim();

        static string FirstSignificantWord(string s)
        {
            foreach (var w in s.Split([' ', ','], StringSplitOptions.RemoveEmptyEntries))
                if (w.Length > 2 && w is not ("the" or "a" or "an" or "of" or "at" or "in" or "on"))
                    return w;
            return "";
        }
    }

    private static async Task<string> UniqueSlugAsync(ProseDbContext db, Guid universeId, string title, CancellationToken ct)
    {
        var baseSlug = new string(title.ToLowerInvariant()
            .Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray());
        baseSlug = string.Join('-', baseSlug.Split('-', StringSplitOptions.RemoveEmptyEntries));
        if (baseSlug.Length == 0) baseSlug = "scene";
        if (baseSlug.Length > 150) baseSlug = baseSlug[..150];

        var slug = baseSlug;
        for (var n = 2; await db.Nodes.IgnoreQueryFilters()
                 .AnyAsync(x => x.UniverseId == universeId && x.Slug == slug, ct); n++)
            slug = $"{baseSlug}-{n}";
        return slug;
    }
}
