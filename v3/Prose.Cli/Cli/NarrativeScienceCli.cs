using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// prose --narrative-science &lt;subcommand&gt; [options]
///
/// Subcommands (Will Storr "Science of Storytelling" frameworks):
///
///   sacred-flaw        Analyze or scaffold a character's theory of control.
///     --character &lt;slug|id&gt;   Character slug or GUID. Required.
///     --scaffold              Generate a plausible flaw from description.
///
///   dramatic-question  Score how well a beat poses "who is this person really?"
///     --slug &lt;nodeSlug&gt;     Evaluate every beat in the node.
///     --id &lt;beatId&gt;           Evaluate a single beat.
///     --character &lt;slug|id&gt;   Optional: provide character context.
///
///   scene-anatomy      6-point scene engagement audit.
///     --slug &lt;nodeSlug&gt;     Audit every beat in the node.
///     --id &lt;beatId&gt;           Audit a single beat.
///
///   five-act           Map a node's beats to Storr's 5-act arc.
///     --slug &lt;nodeSlug&gt;     Required.
///
/// Global flags:
///   --json             Emit raw JSON output.
///   --effort draft|standard|deep
///                      Cost tier (default: deep).
///                        draft    — skip analysis entirely (zero LLM calls).
///                        standard — run only dramatic-question + scene-anatomy (cheapest, most actionable).
///                        deep     — run all five analyzers (current default behavior).
///   --no-persist       Do not save results as Findings in the database.
/// </summary>
public static class NarrativeScienceCli
{
    // ── Effort tiers (mirrors ReviewEffortProfile spirit) ─────────────────────
    // draft    → skip entirely
    // standard → dramatic-question + scene-anatomy only
    // deep     → all five analyzers (default)

    private static string ResolveEffort(string[] args)
    {
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == "--effort") return args[i + 1].Trim().ToLowerInvariant();
        return "deep";
    }

    private static bool ShouldPersist(string[] args) => !args.Contains("--no-persist");

    private const int ParallelCap = 8;

    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        // Find subcommand (first non-flag arg after --narrative-science)
        string? subcommand = null;
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--narrative-science" && i + 1 < args.Length && !args[i + 1].StartsWith("--"))
            {
                subcommand = args[i + 1];
                break;
            }
        }

        if (subcommand == null)
        {
            PrintUsage();
            return 1;
        }

        var effort = ResolveEffort(args);

        // draft tier: skip all analysis at zero cost
        if (effort == "draft")
        {
            Console.WriteLine($"[narrative-science] skipped at draft tier — no LLM calls made.");
            return 0;
        }

        return subcommand.ToLowerInvariant() switch
        {
            "sacred-flaw"       => effort == "standard"
                                    ? SkipForStandard("sacred-flaw")
                                    : await RunSacredFlawAsync(args, services),
            "dramatic-question" => await RunDramaticQuestionAsync(args, services, effort),
            "scene-anatomy"     => await RunSceneAnatomyAsync(args, services, effort),
            "five-act"          => effort == "standard"
                                    ? SkipForStandard("five-act")
                                    : await RunFiveActAsync(args, services),
            _ => PrintUsage($"Unknown subcommand '{subcommand}'"),
        };
    }

    private static int SkipForStandard(string name)
    {
        Console.WriteLine($"[narrative-science] {name} skipped at standard tier (only dramatic-question + scene-anatomy run at standard).");
        return 0;
    }

    // ── sacred-flaw ───────────────────────────────────────────────────────────

    static async Task<int> RunSacredFlawAsync(string[] args, IServiceProvider services)
    {
        string? characterArg = null;
        bool scaffold = args.Contains("--scaffold");
        bool json = args.Contains("--json");
        bool persist = ShouldPersist(args);

        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == "--character") { characterArg = args[i + 1]; i++; }

        if (characterArg == null)
            return PrintUsage("--character <slug|id> is required for sacred-flaw");

        var svc = services.GetRequiredService<NarrativeScienceService>();
        var findingsSvc = services.GetRequiredService<FindingsService>();
        var charId = await ResolveCharacterAsync(characterArg, services);
        if (charId == null)
        {
            Console.Error.WriteLine($"Character '{characterArg}' not found.");
            return 1;
        }

        Console.WriteLine($"Analyzing sacred flaw for character {characterArg}…");
        var result = await svc.AnalyzeSacredFlawAsync(charId.Value, scaffold);

        if (persist)
        {
            const string prefix = "NARRATIVE-SCIENCE [sacred-flaw]:";
            var summary = $"{prefix} {characterArg} — theory: {result.TheoryOfControl}";
            findingsSvc.Upsert(
                filePath: $"character:{charId.Value:N}",
                chapterId: null,
                category: FindingCategory.Other,
                severity: FindingSeverity.Low,
                summary: summary,
                snippet: result.Diagnosis,
                suggestedFix: result.OriginDamage);
        }

        if (json)
        {
            Console.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
            return 0;
        }

        Console.WriteLine();
        Console.WriteLine("═══ SACRED FLAW ANALYSIS ═══");
        Console.WriteLine($"Confidence: {result.Confidence}");
        Console.WriteLine();
        Console.WriteLine($"Theory of Control:");
        Console.WriteLine($"  {result.TheoryOfControl}");
        Console.WriteLine();
        Console.WriteLine($"Origin Damage:");
        Console.WriteLine($"  {result.OriginDamage}");
        Console.WriteLine();
        Console.WriteLine($"Secret Dread:");
        Console.WriteLine($"  {result.SecretDread}");
        Console.WriteLine();
        Console.WriteLine($"Hero-Maker Narrative:");
        Console.WriteLine($"  {result.HeroMakerNarrative}");
        Console.WriteLine();
        Console.WriteLine($"Material Gains (why change is terrifying):");
        Console.WriteLine($"  {result.MaterialGains}");
        Console.WriteLine();
        Console.WriteLine($"Diagnosis:");
        Console.WriteLine($"  {result.Diagnosis}");
        return 0;
    }

    // ── dramatic-question ─────────────────────────────────────────────────────

    static async Task<int> RunDramaticQuestionAsync(string[] args, IServiceProvider services, string effort = "deep")
    {
        string? nodeSlug = null;
        Guid? beatId = null;
        string? characterArg = null;
        bool json = args.Contains("--json");
        bool persist = ShouldPersist(args);

        for (int i = 0; i < args.Length - 1; i++)
        {
            switch (args[i])
            {
                case "--slug":      nodeSlug = args[i + 1]; i++; break;
                case "--id":        if (Guid.TryParse(args[i + 1], out var g)) { beatId = g; i++; } break;
                case "--character": characterArg = args[i + 1]; i++; break;
            }
        }

        if (nodeSlug == null && beatId == null)
            return PrintUsage("--slug <nodeSlug> or --id <beatId> required for dramatic-question");

        var svc = services.GetRequiredService<NarrativeScienceService>();
        var findingsSvc = services.GetRequiredService<FindingsService>();
        Guid? charId = null;
        if (characterArg != null) charId = await ResolveCharacterAsync(characterArg, services);

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        if (beatId.HasValue)
        {
            var beat = await db.Beats.AsNoTracking().FirstOrDefaultAsync(b => b.Id == beatId.Value);
            if (beat == null) { Console.Error.WriteLine($"Beat {beatId} not found."); return 1; }
            var r = await svc.CheckDramaticQuestionAsync(beat.Text ?? "", charId);
            PrintDramaticQuestionResult($"Beat #{beat.Number}", r, json);
            if (persist)
            {
                PurgeNarrativeScienceFindings(findingsSvc, [beat.Id], "NARRATIVE-SCIENCE [dramatic-question]:");
                PersistDramaticQuestion(findingsSvc, beat.Id, beat.Number, r);
            }
            return r.DramaticQuestionActive ? 0 : 1;
        }
        else
        {
            var node = await db.Nodes.AsNoTracking().FirstOrDefaultAsync(s => s.Slug == nodeSlug);
            if (node == null) { Console.Error.WriteLine($"Node '{nodeSlug}' not found."); return 1; }

            var beats = await (
                from sb in db.BeatNodes
                join b in db.Beats on sb.BeatId equals b.Id
                where sb.NodeId == node.Id && sb.IsEnabled
                orderby sb.SortKey
                select new { b.Id, b.Number, b.Text }
            ).ToListAsync();

            if (beats.Count == 0) { Console.Error.WriteLine("No beats found."); return 1; }
            Console.WriteLine($"Checking dramatic question in {beats.Count} beats of '{nodeSlug}'…");

            // Delete stale NARRATIVE-SCIENCE findings for this node before writing fresh ones.
            if (persist)
                PurgeNarrativeScienceFindings(findingsSvc, beats.Select(b => b.Id).ToList(), "NARRATIVE-SCIENCE [dramatic-question]:");

            // Parallel execution: analyzers are independent (no shared mutable state).
            var sem = new SemaphoreSlim(ParallelCap);
            var bag = new ConcurrentBag<(int Number, Guid Id, DramaticQuestionResult Result)>();

            await Task.WhenAll(beats.Select(beat => Task.Run(async () =>
            {
                await sem.WaitAsync();
                try
                {
                    var r = await svc.CheckDramaticQuestionAsync(beat.Text ?? "", charId);
                    bag.Add((beat.Number, beat.Id, r));
                }
                finally { sem.Release(); }
            })));

            // Sort results by beat number to preserve display ordering.
            var ordered = bag.OrderBy(x => x.Number).ToList();
            int weak = 0;
            foreach (var (num, id, r) in ordered)
            {
                PrintDramaticQuestionResult($"Beat #{num}", r, json);
                if (!r.DramaticQuestionActive) weak++;
                if (persist) PersistDramaticQuestion(findingsSvc, id, num, r);
            }

            if (!json) Console.WriteLine($"\n{beats.Count - weak}/{beats.Count} beats have an active dramatic question.");
            return 0;
        }
    }

    static void PersistDramaticQuestion(FindingsService findingsSvc, Guid beatId, int beatNumber, DramaticQuestionResult r)
    {
        const string prefix = "NARRATIVE-SCIENCE [dramatic-question]:";
        var summary = $"{prefix} Beat #{beatNumber} — DQ {r.OverallScore}/10 (surface {r.SurfaceScore}, sub {r.SubconsciousScore}). {r.SubconsciousSummary}";
        var fix = r.DramaticQuestionActive ? null : r.ImprovementHint;
        findingsSvc.Upsert(
            filePath: $"beat:{beatId:N}",
            chapterId: null,
            category: FindingCategory.Other,
            severity: r.DramaticQuestionActive ? FindingSeverity.Low : FindingSeverity.Medium,
            summary: summary,
            snippet: r.SurfaceSummary,
            suggestedFix: fix);
    }

    static void PrintDramaticQuestionResult(string label, DramaticQuestionResult r, bool json)
    {
        if (json)
        {
            Console.WriteLine(JsonSerializer.Serialize(new { beat = label, result = r },
                new JsonSerializerOptions { WriteIndented = true }));
            return;
        }
        var flag = r.DramaticQuestionActive ? "✔" : "✗";
        Console.WriteLine($"\n{flag} {label} — DQ: {r.OverallScore}/10 " +
                          $"(surface {r.SurfaceScore}, subconscious {r.SubconsciousScore})");
        Console.WriteLine($"  Surface:       {r.SurfaceSummary}");
        Console.WriteLine($"  Subconscious:  {r.SubconsciousSummary}");
        if (!r.DramaticQuestionActive)
            Console.WriteLine($"  Hint: {r.ImprovementHint}");
    }

    // ── scene-anatomy ─────────────────────────────────────────────────────────

    static async Task<int> RunSceneAnatomyAsync(string[] args, IServiceProvider services, string effort = "deep")
    {
        string? nodeSlug = null;
        Guid? beatId = null;
        bool json = args.Contains("--json");
        bool persist = ShouldPersist(args);

        for (int i = 0; i < args.Length - 1; i++)
        {
            switch (args[i])
            {
                case "--slug": nodeSlug = args[i + 1]; i++; break;
                case "--id":   if (Guid.TryParse(args[i + 1], out var g)) { beatId = g; i++; } break;
            }
        }

        if (nodeSlug == null && beatId == null)
            return PrintUsage("--slug <nodeSlug> or --id <beatId> required for scene-anatomy");

        var svc = services.GetRequiredService<NarrativeScienceService>();
        var findingsSvc = services.GetRequiredService<FindingsService>();
        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        if (beatId.HasValue)
        {
            var beat = await db.Beats.AsNoTracking().FirstOrDefaultAsync(b => b.Id == beatId.Value);
            if (beat == null) { Console.Error.WriteLine($"Beat {beatId} not found."); return 1; }
            var r = await svc.AuditSceneEngagementAsync(beat.Text ?? "");
            PrintSceneAuditResult($"Beat #{beat.Number}", r, json);
            if (persist)
            {
                PurgeNarrativeScienceFindings(findingsSvc, [beat.Id], "NARRATIVE-SCIENCE [scene-engagement]:");
                PersistSceneEngagement(findingsSvc, beat.Id, beat.Number, r);
            }
            return r.BeatPasses ? 0 : 1;
        }
        else
        {
            var node = await db.Nodes.AsNoTracking().FirstOrDefaultAsync(s => s.Slug == nodeSlug);
            if (node == null) { Console.Error.WriteLine($"Node '{nodeSlug}' not found."); return 1; }

            var beats = await (
                from sb in db.BeatNodes
                join b in db.Beats on sb.BeatId equals b.Id
                where sb.NodeId == node.Id && sb.IsEnabled
                orderby sb.SortKey
                select new { b.Id, b.Number, b.Text }
            ).ToListAsync();

            if (beats.Count == 0) { Console.Error.WriteLine("No beats found."); return 1; }
            Console.WriteLine($"Scene anatomy of {beats.Count} beats in '{nodeSlug}'…");

            // Delete stale NARRATIVE-SCIENCE findings for this node before writing fresh ones.
            if (persist)
                PurgeNarrativeScienceFindings(findingsSvc, beats.Select(b => b.Id).ToList(), "NARRATIVE-SCIENCE [scene-engagement]:");

            // Parallel execution: analyzers are independent (no shared mutable state).
            var sem = new SemaphoreSlim(ParallelCap);
            var bag = new ConcurrentBag<(int Number, Guid Id, SceneEngagementReport Result)>();

            await Task.WhenAll(beats.Select(beat => Task.Run(async () =>
            {
                await sem.WaitAsync();
                try
                {
                    var r = await svc.AuditSceneEngagementAsync(beat.Text ?? "");
                    bag.Add((beat.Number, beat.Id, r));
                }
                finally { sem.Release(); }
            })));

            // Sort results by beat number to preserve display ordering.
            var ordered = bag.OrderBy(x => x.Number).ToList();
            int passing = 0;
            foreach (var (num, id, r) in ordered)
            {
                PrintSceneAuditResult($"Beat #{num}", r, json);
                if (r.BeatPasses) passing++;
                if (persist) PersistSceneEngagement(findingsSvc, id, num, r);
            }

            if (!json) Console.WriteLine($"\n{passing}/{beats.Count} beats pass (≥4/6 mechanisms).");
            return 0;
        }
    }

    static void PersistSceneEngagement(FindingsService findingsSvc, Guid beatId, int beatNumber, SceneEngagementReport r)
    {
        const string prefix = "NARRATIVE-SCIENCE [scene-engagement]:";
        var summary = $"{prefix} Beat #{beatNumber} — {r.MechanismsPassing}/6 mechanisms{(r.BeatPasses ? "" : $". Weakness: {r.TopWeakness}")}";
        findingsSvc.Upsert(
            filePath: $"beat:{beatId:N}",
            chapterId: null,
            category: FindingCategory.Other,
            severity: r.BeatPasses ? FindingSeverity.Low : FindingSeverity.Medium,
            summary: summary,
            snippet: null,
            suggestedFix: r.BeatPasses ? null : r.Fix);
    }

    static void PrintSceneAuditResult(string label, SceneEngagementReport r, bool json)
    {
        if (json)
        {
            Console.WriteLine(JsonSerializer.Serialize(new { beat = label, result = r },
                new JsonSerializerOptions { WriteIndented = true }));
            return;
        }
        var flag = r.BeatPasses ? "✔" : "✗";
        Console.WriteLine($"\n{flag} {label} — {r.MechanismsPassing}/6 mechanisms");
        foreach (var (k, m) in r.Mechanisms)
        {
            var present = m.Present ? "✔" : "✗";
            Console.WriteLine($"  {present} {k,-22} {(m.Present ? m.Evidence : "(missing)")}");
        }
        if (!r.BeatPasses)
        {
            Console.WriteLine($"  Weakness: {r.TopWeakness}");
            Console.WriteLine($"  Fix:      {r.Fix}");
        }
    }

    // ── five-act ──────────────────────────────────────────────────────────────

    static async Task<int> RunFiveActAsync(string[] args, IServiceProvider services)
    {
        string? nodeSlug = null;
        bool json = args.Contains("--json");
        bool persist = ShouldPersist(args);

        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == "--slug") { nodeSlug = args[i + 1]; i++; }

        if (nodeSlug == null)
            return PrintUsage("--slug <nodeSlug> required for five-act");

        var svc = services.GetRequiredService<NarrativeScienceService>();
        var findingsSvc = services.GetRequiredService<FindingsService>();
        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        var node = await db.Nodes.AsNoTracking().FirstOrDefaultAsync(s => s.Slug == nodeSlug);
        if (node == null) { Console.Error.WriteLine($"Node '{nodeSlug}' not found."); return 1; }

        Console.WriteLine($"Mapping five-act structure for '{nodeSlug}'…");
        var result = await svc.MapFiveActStructureAsync(node.Id);

        if (result.Error != null)
        {
            Console.Error.WriteLine($"Error: {result.Error}");
            return 1;
        }

        if (json)
        {
            Console.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
            return 0;
        }

        Console.WriteLine();
        Console.WriteLine($"═══ FIVE-ACT MAP: {result.NodeTitle} ({result.BeatCount} beats) ═══");
        Console.WriteLine();

        var actNames = new Dictionary<string, string>
        {
            ["act_I"]   = "I  — ESTABLISH + IGNITE",
            ["act_II"]  = "II — OLD THEORY TESTED",
            ["act_III"] = "III— TRANSFORMATION TRIGGER",
            ["act_IV"]  = "IV — DARK NIGHT",
            ["act_V"]   = "V  — GOD MOMENT",
        };

        foreach (var (key, name) in actNames)
        {
            if (!result.Acts.TryGetValue(key, out var act)) continue;
            var beatList = act.BeatNumbers.Count > 0
                ? string.Join(", ", act.BeatNumbers.Select(n => $"#{n}"))
                : "(none assigned)";
            Console.WriteLine($"{name}  [{beatList}]");
            if (act.IgnitionBeat.HasValue)  Console.WriteLine($"  Ignition:   Beat #{act.IgnitionBeat}");
            if (act.TriggerBeat.HasValue)   Console.WriteLine($"  Trigger:    Beat #{act.TriggerBeat}");
            if (act.GodMomentBeat.HasValue) Console.WriteLine($"  God Moment: Beat #{act.GodMomentBeat} ({act.Resolution})");
            Console.WriteLine($"  {act.Assessment}");
            Console.WriteLine();
        }

        if (result.StructuralGaps.Count > 0)
        {
            Console.WriteLine("Structural Gaps:");
            foreach (var g in result.StructuralGaps) Console.WriteLine($"  ✗ {g}");
            Console.WriteLine();
        }

        if (result.StructuralStrengths.Count > 0)
        {
            Console.WriteLine("Structural Strengths:");
            foreach (var s in result.StructuralStrengths) Console.WriteLine($"  ✔ {s}");
            Console.WriteLine();
        }

        Console.WriteLine("Assessment:");
        Console.WriteLine($"  {result.OverallAssessment}");

        if (persist)
        {
            const string prefix = "NARRATIVE-SCIENCE [five-act]:";
            // Supersede semantics: the summary embeds the live gap list, so a changed
            // gap count would otherwise mint a new DedupKey and orphan the old row forever.
            findingsSvc.DeleteBySummaryPrefix($"node:{nodeSlug}", prefix);
            var gaps = result.StructuralGaps.Count > 0
                ? string.Join("; ", result.StructuralGaps.Take(3))
                : "none";
            var summary = $"{prefix} {nodeSlug} — {result.BeatCount} beats. Gaps: {gaps}";
            findingsSvc.Upsert(
                filePath: $"node:{nodeSlug}",
                chapterId: null,
                category: FindingCategory.Other,
                severity: result.StructuralGaps.Count > 0 ? FindingSeverity.Medium : FindingSeverity.Low,
                summary: summary,
                snippet: result.OverallAssessment,
                suggestedFix: result.StructuralGaps.Count > 0 ? string.Join("\n", result.StructuralGaps) : null);
        }

        return 0;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Delete stale NARRATIVE-SCIENCE findings for a set of beat IDs before writing
    /// fresh ones — ensures supersede semantics without duplicates per beat+analyzer.
    /// </summary>
    static void PurgeNarrativeScienceFindings(FindingsService findingsSvc, IEnumerable<Guid> beatIds, string summaryPrefix)
    {
        foreach (var beatId in beatIds)
            findingsSvc.DeleteBySummaryPrefix($"beat:{beatId:N}", summaryPrefix);
    }

    static async Task<Guid?> ResolveCharacterAsync(string idOrSlug, IServiceProvider services)
    {
        if (Guid.TryParse(idOrSlug, out var g)) return g;
        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();
        var id = await db.Entities.AsNoTracking()
            .Where(e => e.Slug == idOrSlug && e.EntityType == "character")
            .Select(e => e.Id)
            .FirstOrDefaultAsync();
        return id == Guid.Empty ? null : id;
    }

    static int PrintUsage(string? error = null)
    {
        if (error != null) Console.Error.WriteLine($"Error: {error}");
        Console.Error.WriteLine("""
            Usage: prose --narrative-science <subcommand> [options]

            Subcommands:
              sacred-flaw        Analyze a character's theory of control (Sacred Flaw).
                --character <slug|id>   Required. Character slug or GUID.
                --scaffold              Generate a plausible flaw from existing description.

              dramatic-question  Score how well a beat asks "who is this person really?"
                --slug <nodeSlug>     Evaluate all beats in the node (parallel, up to 8 at once).
                --id <beatId>           Evaluate a single beat.
                --character <slug|id>   Optional. Provide character context.

              scene-anatomy      6-point scene engagement audit.
                --slug <nodeSlug>     Audit all beats in the node (parallel, up to 8 at once).
                --id <beatId>           Audit a single beat.

              five-act           Map a node's beats to Storr's 5-act arc.
                --slug <nodeSlug>     Required.

            Global flags:
              --json             Emit raw JSON output.
              --effort draft|standard|deep
                                 Cost tier (default: deep).
                                   draft    — skip all analysis (zero LLM calls, exit 0).
                                   standard — dramatic-question + scene-anatomy only.
                                   deep     — all five analyzers (default).
              --no-persist       Do not save results as Findings in the database.
                                 By default, results are written with prefix NARRATIVE-SCIENCE [analyzer]:
                                 and can be read by the prose generator to guide writing.
            """);
        return 1;
    }
}
