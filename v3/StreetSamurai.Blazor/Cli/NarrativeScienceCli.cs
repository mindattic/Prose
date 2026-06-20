using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Blazor.Cli;

/// <summary>
/// ss --narrative-science &lt;subcommand&gt; [options]
///
/// Subcommands (Will Storr "Science of Storytelling" frameworks):
///
///   sacred-flaw        Analyze or scaffold a character's theory of control.
///     --character &lt;slug|id&gt;   Character slug or GUID. Required.
///     --scaffold              Generate a plausible flaw from description.
///
///   dramatic-question  Score how well a beat poses "who is this person really?"
///     --slug &lt;strandSlug&gt;     Evaluate every beat in the strand.
///     --id &lt;beatId&gt;           Evaluate a single beat.
///     --character &lt;slug|id&gt;   Optional: provide character context.
///
///   scene-anatomy      6-point scene engagement audit.
///     --slug &lt;strandSlug&gt;     Audit every beat in the strand.
///     --id &lt;beatId&gt;           Audit a single beat.
///
///   five-act           Map a strand's beats to Storr's 5-act arc.
///     --slug &lt;strandSlug&gt;     Required.
///
/// Global flags: --json (raw JSON output)
/// </summary>
public static class NarrativeScienceCli
{
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

        return subcommand.ToLowerInvariant() switch
        {
            "sacred-flaw"       => await RunSacredFlawAsync(args, services),
            "dramatic-question" => await RunDramaticQuestionAsync(args, services),
            "scene-anatomy"     => await RunSceneAnatomyAsync(args, services),
            "five-act"          => await RunFiveActAsync(args, services),
            _ => PrintUsage($"Unknown subcommand '{subcommand}'"),
        };
    }

    // ── sacred-flaw ───────────────────────────────────────────────────────────

    static async Task<int> RunSacredFlawAsync(string[] args, IServiceProvider services)
    {
        string? characterArg = null;
        bool scaffold = args.Contains("--scaffold");
        bool json = args.Contains("--json");

        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == "--character") { characterArg = args[i + 1]; i++; }

        if (characterArg == null)
            return PrintUsage("--character <slug|id> is required for sacred-flaw");

        var svc = services.GetRequiredService<NarrativeScienceService>();
        var charId = await ResolveCharacterAsync(characterArg, services);
        if (charId == null)
        {
            Console.Error.WriteLine($"Character '{characterArg}' not found.");
            return 1;
        }

        Console.WriteLine($"Analyzing sacred flaw for character {characterArg}…");
        var result = await svc.AnalyzeSacredFlawAsync(charId.Value, scaffold);

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

    static async Task<int> RunDramaticQuestionAsync(string[] args, IServiceProvider services)
    {
        string? strandSlug = null;
        Guid? beatId = null;
        string? characterArg = null;
        bool json = args.Contains("--json");

        for (int i = 0; i < args.Length - 1; i++)
        {
            switch (args[i])
            {
                case "--slug":      strandSlug = args[i + 1]; i++; break;
                case "--id":        if (Guid.TryParse(args[i + 1], out var g)) { beatId = g; i++; } break;
                case "--character": characterArg = args[i + 1]; i++; break;
            }
        }

        if (strandSlug == null && beatId == null)
            return PrintUsage("--slug <strandSlug> or --id <beatId> required for dramatic-question");

        var svc = services.GetRequiredService<NarrativeScienceService>();
        Guid? charId = null;
        if (characterArg != null) charId = await ResolveCharacterAsync(characterArg, services);

        var dbFactory = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();
        using var db = dbFactory.CreateDbContext();

        if (beatId.HasValue)
        {
            var beat = await db.Beats.AsNoTracking().FirstOrDefaultAsync(b => b.Id == beatId.Value);
            if (beat == null) { Console.Error.WriteLine($"Beat {beatId} not found."); return 1; }
            var r = await svc.CheckDramaticQuestionAsync(beat.Text ?? "", charId);
            PrintDramaticQuestionResult($"Beat #{beat.Number}", r, json);
            return r.DramaticQuestionActive ? 0 : 1;
        }
        else
        {
            var strand = await db.Strands.AsNoTracking().FirstOrDefaultAsync(s => s.Slug == strandSlug);
            if (strand == null) { Console.Error.WriteLine($"Strand '{strandSlug}' not found."); return 1; }

            var beats = await (
                from sb in db.StrandBeats
                join b in db.Beats on sb.BeatId equals b.Id
                where sb.StrandId == strand.Id
                orderby sb.SortKey
                select new { b.Id, b.Number, b.Text }
            ).ToListAsync();

            if (beats.Count == 0) { Console.Error.WriteLine("No beats found."); return 1; }
            Console.WriteLine($"Checking dramatic question in {beats.Count} beats of '{strandSlug}'…");

            int weak = 0;
            foreach (var beat in beats)
            {
                var r = await svc.CheckDramaticQuestionAsync(beat.Text ?? "", charId);
                PrintDramaticQuestionResult($"Beat #{beat.Number}", r, json);
                if (!r.DramaticQuestionActive) weak++;
            }

            if (!json) Console.WriteLine($"\n{beats.Count - weak}/{beats.Count} beats have an active dramatic question.");
            return 0;
        }
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

    static async Task<int> RunSceneAnatomyAsync(string[] args, IServiceProvider services)
    {
        string? strandSlug = null;
        Guid? beatId = null;
        bool json = args.Contains("--json");

        for (int i = 0; i < args.Length - 1; i++)
        {
            switch (args[i])
            {
                case "--slug": strandSlug = args[i + 1]; i++; break;
                case "--id":   if (Guid.TryParse(args[i + 1], out var g)) { beatId = g; i++; } break;
            }
        }

        if (strandSlug == null && beatId == null)
            return PrintUsage("--slug <strandSlug> or --id <beatId> required for scene-anatomy");

        var svc = services.GetRequiredService<NarrativeScienceService>();
        var dbFactory = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();
        using var db = dbFactory.CreateDbContext();

        if (beatId.HasValue)
        {
            var beat = await db.Beats.AsNoTracking().FirstOrDefaultAsync(b => b.Id == beatId.Value);
            if (beat == null) { Console.Error.WriteLine($"Beat {beatId} not found."); return 1; }
            var r = await svc.AuditSceneEngagementAsync(beat.Text ?? "");
            PrintSceneAuditResult($"Beat #{beat.Number}", r, json);
            return r.BeatPasses ? 0 : 1;
        }
        else
        {
            var strand = await db.Strands.AsNoTracking().FirstOrDefaultAsync(s => s.Slug == strandSlug);
            if (strand == null) { Console.Error.WriteLine($"Strand '{strandSlug}' not found."); return 1; }

            var beats = await (
                from sb in db.StrandBeats
                join b in db.Beats on sb.BeatId equals b.Id
                where sb.StrandId == strand.Id
                orderby sb.SortKey
                select new { b.Id, b.Number, b.Text }
            ).ToListAsync();

            if (beats.Count == 0) { Console.Error.WriteLine("No beats found."); return 1; }
            Console.WriteLine($"Scene anatomy of {beats.Count} beats in '{strandSlug}'…");

            int passing = 0;
            foreach (var beat in beats)
            {
                var r = await svc.AuditSceneEngagementAsync(beat.Text ?? "");
                PrintSceneAuditResult($"Beat #{beat.Number}", r, json);
                if (r.BeatPasses) passing++;
            }

            if (!json) Console.WriteLine($"\n{passing}/{beats.Count} beats pass (≥4/6 mechanisms).");
            return 0;
        }
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
        string? strandSlug = null;
        bool json = args.Contains("--json");

        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == "--slug") { strandSlug = args[i + 1]; i++; }

        if (strandSlug == null)
            return PrintUsage("--slug <strandSlug> required for five-act");

        var svc = services.GetRequiredService<NarrativeScienceService>();
        var dbFactory = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();
        using var db = dbFactory.CreateDbContext();

        var strand = await db.Strands.AsNoTracking().FirstOrDefaultAsync(s => s.Slug == strandSlug);
        if (strand == null) { Console.Error.WriteLine($"Strand '{strandSlug}' not found."); return 1; }

        Console.WriteLine($"Mapping five-act structure for '{strandSlug}'…");
        var result = await svc.MapFiveActStructureAsync(strand.Id);

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
        Console.WriteLine($"═══ FIVE-ACT MAP: {result.StrandTitle} ({result.BeatCount} beats) ═══");
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
        return 0;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    static async Task<Guid?> ResolveCharacterAsync(string idOrSlug, IServiceProvider services)
    {
        if (Guid.TryParse(idOrSlug, out var g)) return g;
        var dbFactory = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();
        using var db = dbFactory.CreateDbContext();
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
            Usage: ss --narrative-science <subcommand> [options]

            Subcommands:
              sacred-flaw        Analyze a character's theory of control (Sacred Flaw).
                --character <slug|id>   Required. Character slug or GUID.
                --scaffold              Generate a plausible flaw from existing description.

              dramatic-question  Score how well a beat asks "who is this person really?"
                --slug <strandSlug>     Evaluate all beats in the strand.
                --id <beatId>           Evaluate a single beat.
                --character <slug|id>   Optional. Provide character context.

              scene-anatomy      6-point scene engagement audit.
                --slug <strandSlug>     Audit all beats in the strand.
                --id <beatId>           Audit a single beat.

              five-act           Map a strand's beats to Storr's 5-act arc.
                --slug <strandSlug>     Required.

            Global flags:
              --json             Emit raw JSON output.
            """);
        return 1;
    }
}
