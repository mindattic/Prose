using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// prose --examine-emotion --slug &lt;nodeSlug&gt; [--effort draft|standard|deep] [--json]
///
/// Emotional Intelligence Examination (SS-A15). Scores prose against an
/// 8-dimension, 0–4 rubric — per beat, character-aware (Want/Need/Wound/Flaw),
/// register-adaptive (CODA vs JOY/SORROW/Fantasy anchors).
///
/// Effort tiers:
///   draft    — Pass 1 only (8 parallel dimension calls)
///   standard — Pass 1 + per-beat emotional curve (default)
///   deep     — Pass 1 + beat curve + ledger refresh + weakest-moment fixes
///
/// Exit codes: 0 = none blocking, 1 = advisory issues, 2 = blocking dimensions open.
/// </summary>
public static class ExamineEmotionCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? slug   = null;
        string  effort = "standard";
        string? model  = null;
        bool    json   = args.Contains("--json");

        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--slug")   { slug   = args[i + 1]; i++; }
            if (args[i] == "--effort") { effort = args[i + 1]; i++; }
            if (args[i] == "--model")  { model  = args[i + 1]; i++; }
        }

        if (slug == null)
        {
            Console.Error.WriteLine("Usage: prose --examine-emotion --slug <nodeSlug> [--effort draft|standard|deep] [--json]");
            return 2;
        }

        var svc       = services.GetRequiredService<EmotionalDepthService>();
        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        var node = await db.Nodes.AsNoTracking().FirstOrDefaultAsync(s => s.Slug == slug || s.NodeCode == slug);
        if (node == null)
        {
            Console.Error.WriteLine($"Node '{slug}' not found.");
            return 2;
        }

        if (!json)
            Console.WriteLine($"Examining '{node.Title}' — effort={effort}{(model != null ? $", model={model}" : "")}…\n");

        // --model retargets the scorer (the default model is rate-limit-sensitive); set it
        // for the run and restore after, mirroring the audit-node orchestrator.
        SettingsService? settings = null;
        string? savedModel = null;
        if (model != null)
        {
            settings = services.GetRequiredService<SettingsService>();
            savedModel = settings.Model;
            settings.Model = model;
        }

        EmotionalExaminationResult result;
        try { result = await svc.ExamineNodeAsync(node.Id, effort); }
        finally { if (settings != null && savedModel != null) settings.Model = savedModel; }

        if (json)
        {
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                node_id           = result.NodeId,
                slug                = result.Slug,
                title               = result.Title,
                emotional_depth     = result.EmotionalDepthScore,
                register            = result.Register,
                blocking_count      = result.BlockingCount,
                recommendation      = result.Recommendation,
                dimensions          = result.Dimensions.Select(d => new
                {
                    dimension       = d.Dimension.ToString(),
                    name            = d.Name,
                    score           = d.Score,
                    is_blocking     = d.IsBlocking,
                    strongest       = d.StrongestEvidence,
                    weakest         = d.WeakestEvidence,
                    weakest_beat    = d.WeakestBeatNumber,
                    fix             = d.Fix,
                    craft_law       = d.CraftLaw,
                }),
                beat_curve          = result.BeatCurve.Select(b => new
                {
                    beat_number = b.BeatNumber,
                    depth       = b.Depth,
                    note        = b.Note,
                }),
                ledgers             = result.Ledgers.Select(l => new
                {
                    character      = l.Character,
                    want           = l.Want,
                    need           = l.Need,
                    wound          = l.Wound,
                    flaw           = l.Flaw,
                    voice_register = l.VoiceRegister,
                    inferred       = l.Inferred,
                }),
            }, new JsonSerializerOptions { WriteIndented = true }));

            return result.BlockingCount > 0 ? 2 : result.Dimensions.Any(d => d.Score <= 1) ? 1 : 0;
        }

        // Human-readable output
        Console.WriteLine($"  Node  : {result.Title}");
        Console.WriteLine($"  Score   : {result.EmotionalDepthScore:F1}/100 emotional depth");
        Console.WriteLine($"  Register: {(result.Register.Length > 0 ? result.Register : "unspecified")}");
        Console.WriteLine($"  Blocking: {result.BlockingCount}");
        Console.WriteLine();

        // Blocking first
        var blocking = result.Dimensions.Where(d => d.IsBlocking && d.Score <= 1).ToList();
        if (blocking.Count > 0)
        {
            Console.WriteLine("BLOCKING DIMENSIONS (resolve before marking publish-ready):");
            foreach (var d in blocking)
            {
                Console.WriteLine($"  ⛔ {d.Name}  [{d.Score}/4]");
                if (!string.IsNullOrWhiteSpace(d.WeakestEvidence))
                    Console.WriteLine($"     Weakest  : {Truncate(d.WeakestEvidence, 120)}");
                Console.WriteLine($"     Fix      : {d.Fix}");
                Console.WriteLine();
            }
        }

        // Non-blocking sorted by score
        Console.WriteLine("DIMENSIONS:");
        foreach (var d in result.Dimensions.OrderBy(d => d.Score))
        {
            var icon = d.Score >= 3 ? "✅" : d.Score == 2 ? "△" : "✗";
            Console.WriteLine($"  {icon} {d.Name,-28} {d.Score}/4  {d.CraftLaw}");
            if (d.Score <= 1 && !string.IsNullOrWhiteSpace(d.Fix))
                Console.WriteLine($"     → {d.Fix}");
        }

        if (result.BeatCurve.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine($"BEAT CURVE ({result.BeatCurve.Count} beats):");
            foreach (var b in result.BeatCurve.OrderBy(x => x.BeatNumber))
            {
                var bar = new string('█', b.Depth) + new string('░', 4 - b.Depth);
                Console.WriteLine($"  Beat {b.BeatNumber,4}: {bar} {b.Depth}/4{(b.Note != null ? $"  — {b.Note}" : "")}");
            }
        }

        if (result.Ledgers.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("CHARACTER LEDGER:");
            foreach (var l in result.Ledgers)
            {
                var inferred = l.Inferred ? " (inferred)" : "";
                Console.WriteLine($"  {l.Character}{inferred}");
                if (!string.IsNullOrWhiteSpace(l.Want)) Console.WriteLine($"    Want : {l.Want}");
                if (!string.IsNullOrWhiteSpace(l.Need)) Console.WriteLine($"    Need : {l.Need}");
            }
        }

        Console.WriteLine();
        Console.WriteLine($"RECOMMENDATION: {result.Recommendation}");

        return result.BlockingCount > 0 ? 2 : result.Dimensions.Any(d => d.Score <= 1) ? 1 : 0;
    }

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..max] + "…";
}
