using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>prose --tag-entities (--id &lt;guid&gt; | --slug &lt;slug&gt; | --all) [--dry-run] [--force-mentions]</c>
///
/// <c>--force-mentions</c> re-derives <c>BeatEntityMentions</c> for EVERY beat from its current
/// tagged text, not just beats whose <c>Beats.Text</c> this run actually rewrote. Needed to clean
/// up stale mention rows left by the OLD pre-tagging scanner (<c>EntityRamificationService</c>) —
/// found live 2026-08-17: a bogus <c>CharacterAliases.Value = "The"</c> row (see
/// <c>EntityMentionScanner.Stopwords</c>' doc comment) caused the old scanner to mass-mistag
/// EVERY capitalized "The" in the corpus as specific "The X"-named entities, producing 69,792 of
/// 92,606 total <c>BeatEntityMentions</c> rows (75%) as false positives across 35 entities. A plain
/// re-tag run alone can't fix this: a beat with zero genuine entity matches produces identical
/// tagged output before and after, so it's never in this run's "changed" set and its stale bogus
/// mentions would otherwise survive untouched forever.
///
/// Retroactive backfill for inline entity-GUID tagging (corpus-trust-recovery Phase 1a): scans
/// every live beat under the target book(s) with <see cref="EntityMentionScanner"/> and rewrites
/// <c>Beats.Text</c> in place with <c>&lt;entity repo="..." guid="..."&gt;word&lt;/entity&gt;</c>
/// tags, then derives <c>BeatEntityMentions</c> from the tags just written.
///
/// Deliberately does NOT go through <see cref="NodeWorkbenchService.UpdateBeatTextAsync"/> (the
/// normal per-beat edit path) — that method bumps <c>Beat.Version</c>, clears <c>Score</c>/
/// <c>ScoredAt</c>, sets <c>Stale</c>/<c>WasCorrected</c>, and invalidates recorded audio, all of
/// which are correct reactions to an actual PROSE change but wrong here: adding invisible tag
/// markup changes nothing a reader or listener perceives (audio narration already strips tags via
/// <see cref="NarrationText.Clean"/>/<see cref="BeatMarkup"/> before synthesis), so treating a
/// tagging pass as a content edit would spuriously flag every backfilled beat for re-review or
/// re-narration with no actual story change behind it. <c>Beats.Text</c> is once again
/// system-versioned (Phase -1a), so the pre-tag text remains fully recoverable via
/// <c>Beats_History</c> regardless of which path performs the write — on top of which a corpus-wide
/// run should always be preceded by <c>--archive-book --all</c> (a second, independent, human-
/// readable safety net) and a raw <c>Beats</c> table backup, per the corpus-trust-recovery plan.
///
/// Builds each book's candidate index ONCE (not per-beat, unlike a naive per-beat
/// <c>UpdateBeatTextAsync</c> loop) since re-deriving the universe-wide entity/alias list for
/// every beat in a book would be wasted repeated work.
///
/// <c>--all</c> resolves every book-level node across every universe via <c>IgnoreQueryFilters()</c>
/// — this command explicitly targets rows by id/slug/kind, never an ambient universe default, so
/// it is exempt from the <c>--universe</c> requirement (see <c>UniverseAgnosticCommands</c> in
/// Program.cs). Per-beat detail lines are only printed for a single-book run; <c>--all</c> prints
/// one summary line per book to keep corpus-wide output readable.
/// </summary>
public static class TagEntitiesCli
{
    private sealed record BookTagResult(int Tagged, int Unchanged, int TotalMentions);

    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var id = Flag(args, "--id");
        var slug = Flag(args, "--slug");
        var all = args.Contains("--all");
        var dryRun = args.Contains("--dry-run");
        var forceMentions = args.Contains("--force-mentions");

        if (!all && string.IsNullOrWhiteSpace(id) && string.IsNullOrWhiteSpace(slug))
        {
            Console.Error.WriteLine("Usage: prose --tag-entities (--id <guid> | --slug <slug> | --all) [--dry-run] [--force-mentions]");
            return 2;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        List<Node> targets;
        if (all)
        {
            targets = await db.Nodes.IgnoreQueryFilters()
                .Where(n => n.Kind == "book")
                .OrderBy(n => n.Title)
                .ToListAsync();
        }
        else
        {
            var node = !string.IsNullOrWhiteSpace(slug)
                ? await db.Nodes.IgnoreQueryFilters().FirstOrDefaultAsync(n => n.Slug == slug)
                : Guid.TryParse(id, out var g) ? await db.Nodes.IgnoreQueryFilters().FirstOrDefaultAsync(n => n.Id == g) : null;
            if (node == null)
            {
                Console.Error.WriteLine("[tag-entities] Target node not found.");
                return 1;
            }
            if (node.Kind != "book")
            {
                Console.Error.WriteLine($"[tag-entities] Target must be a book-level node (Kind='book'); '{node.Title}' is Kind='{node.Kind}'.");
                return 1;
            }
            targets = [node];
        }

        if (targets.Count == 0)
        {
            Console.Error.WriteLine("[tag-entities] No book-level nodes found.");
            return 1;
        }

        int grandTagged = 0, grandUnchanged = 0, grandMentions = 0, booksTouched = 0;
        foreach (var node in targets)
        {
            var result = await TagBookAsync(db, dbFactory, node, dryRun, verbose: !all, forceMentions);
            grandTagged += result.Tagged;
            grandUnchanged += result.Unchanged;
            grandMentions += result.TotalMentions;
            if (result.Tagged > 0) booksTouched++;

            if (all)
                Console.WriteLine($"[tag-entities] '{node.Title}' ({node.Slug}): tagged={result.Tagged} unchanged={result.Unchanged} mentions={result.TotalMentions}");
        }

        // Single-book mode already prints its own "Done." line inside TagBookAsync (verbose=true).
        if (all)
            Console.WriteLine($"[tag-entities] Done. {targets.Count} book(s), {booksTouched} touched, {grandTagged} beat(s) tagged, {grandUnchanged} unchanged, {grandMentions} total mention(s){(dryRun ? " (dry-run, nothing written)" : "")}.");
        return 0;
    }

    private static async Task<BookTagResult> TagBookAsync(
        ProseDbContext db, IDbContextFactory<ProseDbContext> dbFactory, Node node, bool dryRun, bool verbose,
        bool forceMentions = false)
    {
        var leafIds = await NodeWorkbenchService.GetLeafDescendantIdsAsync(db, node.Id);
        var beatIds = await db.BeatNodes
            .Where(bn => leafIds.Contains(bn.NodeId))
            .OrderBy(bn => bn.SortKey)
            .Select(bn => bn.BeatId)
            .Distinct()
            .ToListAsync();

        if (beatIds.Count == 0) return new BookTagResult(0, 0, 0);

        var candidates = await EntityMentionScanner.BuildCandidateIndexAsync(db, node.UniverseId, node.Id);
        if (verbose)
            Console.WriteLine($"[tag-entities] '{node.Title}' ({node.Slug}): {beatIds.Count} beat(s), {candidates.Count} candidate name(s)/alias(es){(dryRun ? " (dry-run)" : "")}.");

        // One tracked fetch for the whole book instead of N+1 per-beat round-trips.
        var beatsById = (await db.Beats.Where(b => beatIds.Contains(b.Id)).ToListAsync())
            .ToDictionary(b => b.Id);

        int tagged = 0, unchanged = 0, totalMentions = 0;
        var taggedBeatIds = new List<Guid>();
        // Every beat's resolved (possibly unchanged) tagged text — populated regardless of whether
        // this pass actually rewrote Beats.Text, so --force-mentions can re-derive BeatEntityMentions
        // for EVERY beat from current ground truth, not just the ones whose text changed.
        var resolvedTextByBeatId = new Dictionary<Guid, string>();
        foreach (var beatId in beatIds)
        {
            if (!beatsById.TryGetValue(beatId, out var beat)) { unchanged++; continue; }
            if (string.IsNullOrWhiteSpace(beat.Text))
            {
                // Still recorded (as empty) so --force-mentions purges any stale mention rows a
                // now-empty beat has no business still carrying.
                resolvedTextByBeatId[beatId] = "";
                unchanged++;
                continue;
            }

            var plainText = BeatMarkup.StripEntityTags(beat.Text);
            var matches = EntityMentionScanner.Scan(plainText, candidates);
            var retagged = EntityMentionScanner.ApplyTags(plainText, matches);
            resolvedTextByBeatId[beatId] = retagged;

            if (retagged == beat.Text) { unchanged++; continue; }

            if (verbose) Console.WriteLine($"  Beat #{beat.Number}: {matches.Count} mention(s) tagged.");
            tagged++;
            totalMentions += matches.Count;
            if (dryRun) continue;

            beat.Text     = retagged;
            beat.TextHash = NodeWorkbenchService.ComputeTextHash(retagged);
            taggedBeatIds.Add(beat.Id);
        }

        if (!dryRun)
        {
            if (taggedBeatIds.Count > 0) await db.SaveChangesAsync();

            var beatIdsToDerive = forceMentions ? resolvedTextByBeatId.Keys.ToList() : taggedBeatIds;
            foreach (var beatId in beatIdsToDerive)
                await EntityMentionScanner.DeriveAndSaveMentionsAsync(dbFactory, beatId, resolvedTextByBeatId[beatId]);
        }

        if (verbose)
            Console.WriteLine($"[tag-entities] Done. Tagged={tagged} Unchanged={unchanged}{(dryRun ? " (dry-run, nothing written)" : "")}.");

        return new BookTagResult(tagged, unchanged, totalMentions);
    }

    private static string? Flag(string[] args, string name)
    {
        var idx = Array.IndexOf(args, name);
        return idx >= 0 && idx + 1 < args.Length ? args[idx + 1] : null;
    }
}
