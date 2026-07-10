using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Cli;

/// <summary>
/// CLI entry for the dossier-driven story repair pass. Walks every chapter,
/// fills in character timelines, and (optionally) runs LLM-driven extraction.
///
///   ss --repair                  # cheap timeline-only pass, no LLM
///   ss --repair --continuity     # also run continuity extraction (LLM-heavy)
///   ss --repair --beat-facts     # also run Knowledge + Conditions extraction (LLM-heavy)
///   ss --repair --backfill-dates # populate Chapter.InWorldDate (and beats) via LLM
///   ss --repair --force          # re-extract even chapters that already ran
/// </summary>
public static class RepairCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider sp)
    {
        var withContinuity    = args.Contains("--continuity");
        var withBeatFacts     = args.Contains("--beat-facts");
        var withDates         = args.Contains("--backfill-dates");
        var withMojibake      = args.Contains("--fix-mojibake");
        var withState         = args.Contains("--extract-state");
        var withCacophonySeed = args.Contains("--seed-cacophony");
        var withLinkAmmo      = args.Contains("--link-ammunition");
        var withNormKinds     = args.Contains("--normalize-kinds");
        var withOrphans       = args.Contains("--orphan-chapters");
        // --prune-json-* / --prune-types retired 2026-05-08 with JsonPruneService;
        // engine/data/*.json no longer exists, so there's nothing to prune.
        var force             = args.Contains("--force");

        var repair = sp.GetRequiredService<StoryRepairService>();
        var ct = CancellationToken.None;
        var failures = 0;

        Console.WriteLine("=== StreetSamurai story repair ===");

        // Idempotent schema bootstrap — applies any new columns/tables this
        // build expects so subsequent EF queries don't trip on a missing
        // column (e.g. CharacterBelongingsGear.GearEntityId, WeaponSpecs).
        var ammoLinker = sp.GetRequiredService<StreetSamurai.Core.Services.AmmunitionLinkerService>();
        await ammoLinker.EnsureWeaponSpecsSchemaAsync(ct);
        await ammoLinker.EnsureGearEntityIdColumnAsync(ct);
        var stateLedger = sp.GetRequiredService<StreetSamurai.Core.Services.WorldStateLedger>();
        await stateLedger.EnsureSchemaAsync(ct);
        var eventLog = sp.GetRequiredService<StreetSamurai.Core.Services.EventLogService>();
        await eventLog.EnsureEventsJsonColumnAsync(ct);
        var knowledgeMap = sp.GetRequiredService<StreetSamurai.Core.Services.KnowledgeMapService>();
        await knowledgeMap.EnsureKnowledgeJsonColumnAsync(ct);
        var outlineSvc = sp.GetRequiredService<StreetSamurai.Core.Services.OutlineService>();
        await outlineSvc.EnsureOutlineJsonColumnAsync(ct);
        var refinementSvc = sp.GetRequiredService<StreetSamurai.Core.Services.StoryRefinementService>();
        await refinementSvc.EnsureRefinementReportColumnAsync(ct);
        var qualitySvc = sp.GetRequiredService<StreetSamurai.Core.Services.StoryQualityService>();
        await qualitySvc.EnsureQualityReportColumnAsync(ct);
        var directorSvc = sp.GetRequiredService<StreetSamurai.Core.Services.StoryDirectorService>();
        await directorSvc.EnsureCheckpointColumnAsync(ct);

        var timeline = repair.RepairTimelines(ct);
        Console.WriteLine();
        Console.WriteLine("[timelines]");
        Console.WriteLine($"  chapters scanned   : {timeline.ChaptersScanned}");
        Console.WriteLine($"  characters scanned : {timeline.CharactersScanned}");
        Console.WriteLine($"  entries added      : {timeline.TimelineEntriesAdded}");
        Console.WriteLine($"  characters updated : {timeline.CharactersUpdated}");
        if (timeline.Errors.Count > 0)
        {
            Console.WriteLine($"  errors             : {timeline.Errors.Count}");
            foreach (var e in timeline.Errors.Take(10)) Console.WriteLine($"    - {e}");
            failures++;
        }

        if (withNormKinds)
        {
            Console.WriteLine();
            Console.WriteLine("[normalize-kinds]");
            await using var db = await sp.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>().CreateDbContextAsync();
            // series/story: root level (ParentNodeId IS NULL), except explicit series nodes which stay "series"
            // story: null-parent nodes that aren't already "series"
            var storyRows = await db.Database.ExecuteSqlRawAsync(
                "UPDATE Nodes SET Kind = 'story' WHERE ParentNodeId IS NULL AND Kind <> 'series'");
            var chapterRows = await db.Database.ExecuteSqlRawAsync(
                "UPDATE Nodes SET Kind = 'chapter' WHERE ParentNodeId IS NOT NULL AND Kind NOT IN ('story','series')");
            Console.WriteLine($"  root nodes set to story  : {storyRows}");
            Console.WriteLine($"  child nodes set to chapter: {chapterRows}");
        }

        if (withOrphans)
        {
            Console.WriteLine();
            Console.WriteLine("[orphan-chapters]");
            await using var db = await sp.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>().CreateDbContextAsync();
            var orphans = await db.Nodes
                .Where(s => s is ChapterNode && s.ParentNodeId == null)
                .ToListAsync();
            Console.WriteLine($"  orphan chapters found: {orphans.Count}");
            if (orphans.Count > 0)
            {
                // Group by UniverseId and create one "Drafts" story per universe.
                foreach (var grp in orphans.GroupBy(o => o.UniverseId))
                {
                    var uid = grp.Key;
                    var drafts = await db.Nodes.FirstOrDefaultAsync(s =>
                        s.Title == "Drafts" && s is StoryNode && s.ParentNodeId == null && s.UniverseId == uid);
                    if (drafts == null)
                    {
                        await using var repairTx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
                        var maxSort = await db.Nodes.Where(s => s.ParentNodeId == null)
                            .MaxAsync(s => (double?)s.SortKey) ?? 0;
                        drafts = new StreetSamurai.Core.Data.Entities.StoryNode
                        {
                            Id = Guid.CreateVersion7(),
                            Slug = $"drafts-{Guid.CreateVersion7().ToString("N")[..8]}",
                            Title = "Drafts",
                            Kind = "story",
                            Status = "draft",
                            SortKey = maxSort + 100.0,
                            UniverseId = uid,
                        };
                        db.Nodes.Add(drafts);
                        await db.SaveChangesAsync();
                        await repairTx.CommitAsync();
                        Console.WriteLine($"  created Drafts story (universe {uid})");
                    }
                    foreach (var o in grp)
                    {
                        o.ParentNodeId = drafts.Id;
                        Console.WriteLine($"    → reparented '{o.Title}' to Drafts");
                    }
                }
                await db.SaveChangesAsync();
                Console.WriteLine($"  reparented {orphans.Count} orphan(s)");
                failures = 0; // orphan repair succeeded
            }
        }

        if (!withContinuity && !withBeatFacts && !withDates && !withMojibake
            && !withState && !withCacophonySeed && !withLinkAmmo
            && !withNormKinds && !withOrphans)
        {
            Console.WriteLine();
            Console.WriteLine("Skipping LLM/repair phases. Add one of:");
            Console.WriteLine("  --normalize-kinds       set root nodes→story, child nodes→chapter (idempotent)");
            Console.WriteLine("  --orphan-chapters       reparent Kind=chapter/no-parent nodes to a 'Drafts' story");
            Console.WriteLine("  --continuity            LLM continuity-claim extraction");
            Console.WriteLine("  --beat-facts            knowledge + conditions extraction");
            Console.WriteLine("  --backfill-dates        populate Chapter/Beat InWorldDate via LLM");
            Console.WriteLine("  --fix-mojibake          reverse double-encoded UTF-8 in every NVARCHAR column");
            Console.WriteLine("  --extract-state         emit EntityStateEvents from chapter beats");
            Console.WriteLine("  --seed-cacophony        insert canonical specs + ammo + Kyle link for Cacophony");
            Console.WriteLine("  --link-ammunition       bulk LLM pass: tie every firearm to compatible ammunition");
            return failures > 0 ? 1 : 0;
        }

        if (withCacophonySeed)
        {
            Console.WriteLine();
            Console.WriteLine("[seed-cacophony]");
            var linker = sp.GetRequiredService<StreetSamurai.Core.Services.AmmunitionLinkerService>();
            var rs = await linker.SeedCacophonyAsync(ct);
            Console.WriteLine($"  ammunitions created    : {rs.AmmunitionsCreated}");
            Console.WriteLine($"  weapon→ammo rows added : {rs.CompatibilityRowsAdded}");
            Console.WriteLine($"  weapon specs written   : {rs.SpecsWritten}");
            if (rs.Errors.Count > 0) { foreach (var e in rs.Errors) Console.WriteLine($"    ✘ {e}"); failures++; }
        }

        if (withLinkAmmo)
        {
            Console.WriteLine();
            Console.WriteLine("[link-ammunition]");
            var linker = sp.GetRequiredService<StreetSamurai.Core.Services.AmmunitionLinkerService>();
            var progress = new Progress<string>(Console.WriteLine);
            var rs = await linker.LinkAllFirearmsAsync(progress, ct);
            Console.WriteLine();
            Console.WriteLine($"  firearms scanned       : {rs.WeaponsScanned}");
            Console.WriteLine($"  ammunitions created    : {rs.AmmunitionsCreated}");
            Console.WriteLine($"  weapon→ammo rows added : {rs.CompatibilityRowsAdded}");
            if (rs.Errors.Count > 0) { Console.WriteLine($"  errors: {rs.Errors.Count}"); foreach (var e in rs.Errors.Take(10)) Console.WriteLine($"    ✘ {e}"); failures++; }
        }

        if (withState)
        {
            Console.WriteLine();
            Console.WriteLine("[extract-state]");
            var ledger    = sp.GetRequiredService<StreetSamurai.Core.Services.WorldStateLedger>();
            var extractor = sp.GetRequiredService<StreetSamurai.Core.Services.BeatStateExtractor>();
            var chapters  = sp.GetRequiredService<StreetSamurai.Core.Interfaces.IChapterRepository>();

            // Idempotent DDL: ensures EntityStateEvents exists on a live DB
            // even when --rebuild hasn't been run since the feature landed.
            await ledger.EnsureSchemaAsync(ct);

            extractor.AutoOnChapterSaved = false; // we drive the loop ourselves
            int totalEvents = 0, totalBeats = 0, totalErrors = 0;
            var allChapters = chapters.ListChapters().OrderBy(c => c.Number ?? int.MaxValue).ToList();
            for (int i = 0; i < allChapters.Count; i++)
            {
                var ch = allChapters[i];
                Console.WriteLine($"  [{i + 1}/{allChapters.Count}] Ch{ch.Number} '{ch.Title}'");
                var rs = await extractor.ExtractAsync(ch, ct);
                totalBeats  += rs.BeatsScanned;
                totalEvents += rs.EventsRecorded;
                totalErrors += rs.Errors.Count;
                Console.WriteLine($"    beats {rs.BeatsScanned,3}  events {rs.EventsRecorded,4}  errors {rs.Errors.Count,2}");
            }
            Console.WriteLine();
            Console.WriteLine($"  beats scanned   : {totalBeats}");
            Console.WriteLine($"  events recorded : {totalEvents}");
            Console.WriteLine($"  errors          : {totalErrors}");
            if (totalErrors > 0) failures++;
        }

        if (withMojibake)
        {
            Console.WriteLine();
            Console.WriteLine("[fix-mojibake]");
            var fixer = sp.GetRequiredService<StreetSamurai.Core.Services.MojibakeRepairService>();
            var progress = new Progress<string>(Console.WriteLine);
            var rm = await fixer.RepairAllAsync(progress, ct);
            Console.WriteLine($"  tables scanned   : {rm.TablesScanned}");
            Console.WriteLine($"  columns scanned  : {rm.ColumnsScanned}");
            Console.WriteLine($"  rows scanned     : {rm.RowsScanned}");
            Console.WriteLine($"  cells repaired   : {rm.CellsRepaired}");
            Console.WriteLine($"  cells left alone : {rm.CellsLeftAlone}");
            if (rm.Errors.Count > 0)
            {
                Console.WriteLine($"  errors           : {rm.Errors.Count}");
                foreach (var e in rm.Errors.Take(10)) Console.WriteLine($"    - {e}");
                failures++;
            }
        }

        if (withDates)
        {
            Console.WriteLine();
            Console.WriteLine("[backfill-dates]");
            var backfill = sp.GetRequiredService<DateBackfillService>();
            var progress = new Progress<string>(Console.WriteLine);
            var dr = await backfill.RunAsync(force: force, includeBeats: true, progress: progress, ct: ct);
            Console.WriteLine($"  chapters scanned : {dr.ChaptersScanned}");
            Console.WriteLine($"  chapters dated   : {dr.ChaptersDated}");
            Console.WriteLine($"  chapters skipped : {dr.ChaptersSkipped} (already had a date; use --force to override)");
            Console.WriteLine($"  beats scanned    : {dr.BeatsScanned}");
            Console.WriteLine($"  beats dated      : {dr.BeatsDated}");
            if (dr.Errors.Count > 0)
            {
                Console.WriteLine($"  errors           : {dr.Errors.Count}");
                foreach (var e in dr.Errors.Take(10)) Console.WriteLine($"    - {e}");
                failures++;
            }
        }

        if (withContinuity)
        {
            Console.WriteLine();
            Console.WriteLine("[continuity]");
            var continuity = await repair.RepairContinuityAsync(force, ct);
            Console.WriteLine($"  chapters scanned    : {continuity.ChaptersScanned}");
            Console.WriteLine($"  chapters extracted  : {continuity.ChaptersExtracted}");
            Console.WriteLine($"  new claims          : {continuity.NewClaims}");
            Console.WriteLine($"  confirmed claims    : {continuity.ConfirmedClaims}");
            Console.WriteLine($"  contradicted claims : {continuity.ContradictedClaims}");
            if (continuity.Errors.Count > 0)
            {
                Console.WriteLine($"  errors              : {continuity.Errors.Count}");
                foreach (var e in continuity.Errors.Take(10)) Console.WriteLine($"    - {e}");
                failures++;
            }
        }

        if (withBeatFacts)
        {
            Console.WriteLine();
            Console.WriteLine("[beat-facts (Knowledge + Conditions)]");
            var bf = await repair.RepairBeatFactsAsync(ct);
            Console.WriteLine($"  chapters scanned   : {bf.ChaptersScanned}");
            Console.WriteLine($"  beats scanned      : {bf.BeatsScanned}");
            Console.WriteLine($"  knowledge added    : {bf.KnowledgeAdded}");
            Console.WriteLine($"  conditions added   : {bf.ConditionsAdded}");
            Console.WriteLine($"  characters touched : {bf.TouchedCharacters.Count}");
            if (bf.Errors.Count > 0)
            {
                Console.WriteLine($"  errors             : {bf.Errors.Count}");
                foreach (var e in bf.Errors.Take(10)) Console.WriteLine($"    - {e}");
                failures++;
            }
        }

        return failures > 0 ? 1 : 0;
    }
}
