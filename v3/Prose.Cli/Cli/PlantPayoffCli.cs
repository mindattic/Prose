using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// prose --plant-audit  --slug &lt;nodeSlug&gt; [--json]
///   Audit all plant/payoff pairs: orphaned plants, transparency violations, coverage.
///
/// prose --list-plants  --slug &lt;nodeSlug&gt; [--json]
///   List all registered plant/payoff pairs for a node.
///
/// prose --add-plant  --slug &lt;nodeSlug&gt;
///               --plant  "what is seeded"
///               --payoff "what the re-reader gets"
///              [--cat detail|echo|irony|motif|character-truth|structural]
///   Register a new plant/payoff pair.
///
/// Exit codes: 0 = ok / no issues, 1 = advisory, 2 = blocking violations.
/// </summary>
public static class PlantPayoffCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? slug = null;
        bool    jsonMode = args.Contains("--json");

        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == "--slug") { slug = args[i + 1]; i++; }

        if (slug == null) { Console.Error.WriteLine("Usage: prose --plant-audit|--list-plants|--add-plant --slug <node> [options]"); return 2; }

        var svc       = services.GetRequiredService<PlantPayoffService>();
        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        var node = await db.Nodes.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Slug == slug || s.NodeCode == slug);
        if (node == null) { Console.Error.WriteLine($"Node '{slug}' not found."); return 2; }

        // ── prose --list-plants ───────────────────────────────────────────────────

        if (args.Contains("--list-plants"))
        {
            var pairs = await svc.GetByNodeAsync(node.Id);
            if (jsonMode)
            {
                Console.WriteLine(JsonSerializer.Serialize(pairs, new JsonSerializerOptions { WriteIndented = true }));
                return 0;
            }

            Console.WriteLine($"Plants/Payoffs for '{node.Title}' ({pairs.Count} pairs)\n");
            if (pairs.Count == 0) { Console.WriteLine("(none registered)"); return 0; }

            foreach (var p in pairs)
            {
                var status = p.PayoffBeatId != null ? "paid-off" : p.PlantBeatId != null ? "seeded" : "planned";
                var flag   = !p.IsTransparent && p.PayoffBeatId != null ? "  ⚠ NOT TRANSPARENT" : "";
                Console.WriteLine($"  [{p.Category.ToUpper()}] {p.PlantDescription}");
                Console.WriteLine($"           → {p.PayoffDescription}  ({status}){flag}");
                if (p.TransparencyNote != null)
                    Console.WriteLine($"           Re-read layer: {p.TransparencyNote}");
                Console.WriteLine();
            }
            return 0;
        }

        // ── prose --add-plant ─────────────────────────────────────────────────────

        if (args.Contains("--add-plant"))
        {
            string? plant = null, payoff = null, cat = "detail";
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "--plant")  { plant  = args[i + 1]; i++; }
                if (args[i] == "--payoff") { payoff = args[i + 1]; i++; }
                if (args[i] == "--cat")    { cat    = args[i + 1]; i++; }
            }

            if (plant == null || payoff == null)
            {
                Console.Error.WriteLine("Usage: prose --add-plant --slug <node> --plant \"what is seeded\" --payoff \"what re-readers get\" [--cat detail]");
                return 2;
            }

            var pp = await svc.RegisterAsync(node.Id, plant, payoff, cat);
            if (jsonMode)
            {
                Console.WriteLine(JsonSerializer.Serialize(new { id = pp.Id, status = "registered" },
                    new JsonSerializerOptions { WriteIndented = true }));
            }
            else
            {
                Console.WriteLine($"✅ Registered [{cat.ToUpper()}] plant/payoff pair.");
                Console.WriteLine($"   Plant:  {plant}");
                Console.WriteLine($"   Payoff: {payoff}");
                Console.WriteLine($"   ID: {pp.Id}");
            }
            return 0;
        }

        Console.Error.WriteLine("Usage: prose --list-plants --slug <slug> | --add-plant ... (the --plant-audit mode was removed 2026-09-06, RFC 0010)");
        return 2;
    }
}
