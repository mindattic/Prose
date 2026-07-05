using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Cli;

/// <summary>
/// ss --plant-audit  --slug &lt;nodeSlug&gt; [--json]
///   Audit all plant/payoff pairs: orphaned plants, transparency violations, coverage.
///
/// ss --list-plants  --slug &lt;nodeSlug&gt; [--json]
///   List all registered plant/payoff pairs for a node.
///
/// ss --add-plant  --slug &lt;nodeSlug&gt;
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

        if (slug == null) { Console.Error.WriteLine("Usage: ss --plant-audit|--list-plants|--add-plant --slug <node> [options]"); return 2; }

        var svc       = services.GetRequiredService<PlantPayoffService>();
        var dbFactory = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        var node = await db.Nodes.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Slug == slug || s.NodeCode == slug);
        if (node == null) { Console.Error.WriteLine($"Node '{slug}' not found."); return 2; }

        // ── ss --list-plants ───────────────────────────────────────────────────

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

        // ── ss --add-plant ─────────────────────────────────────────────────────

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
                Console.Error.WriteLine("Usage: ss --add-plant --slug <node> --plant \"what is seeded\" --payoff \"what re-readers get\" [--cat detail]");
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

        // ── ss --plant-audit ───────────────────────────────────────────────────

        if (!jsonMode) Console.WriteLine($"Auditing plant/payoff pairs for '{node.Title}'…\n");

        var audit = await svc.AuditAsync(node.Id);

        if (jsonMode)
        {
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                node_slug          = audit.NodeSlug,
                total_pairs          = audit.TotalPairs,
                planted              = audit.Planted,
                paid_off             = audit.PaidOff,
                orphaned             = audit.Orphaned,
                not_transparent      = audit.NotTransparentCount,
                gateway_plant_ready  = audit.Orphaned == 0 && audit.NotTransparentCount == 0,
            }, new JsonSerializerOptions { WriteIndented = true }));
            return audit.Orphaned > 0 || audit.NotTransparentCount > 0 ? 1 : 0;
        }

        Console.WriteLine($"Total pairs:   {audit.TotalPairs}");
        Console.WriteLine($"Seeded:        {audit.Planted}");
        Console.WriteLine($"Paid off:      {audit.PaidOff}");
        Console.WriteLine($"Orphaned:      {audit.Orphaned}");
        Console.WriteLine($"Not transparent: {audit.NotTransparentCount}");
        Console.WriteLine();

        if (audit.OrphanedPlants.Count > 0)
        {
            Console.WriteLine("ORPHANED (seeded, no payoff written):");
            foreach (var p in audit.OrphanedPlants)
                Console.WriteLine($"  [{p.Category.ToUpper()}] {p.PlantDescription} → {p.PayoffDescription}");
            Console.WriteLine();
        }

        if (audit.NotTransparentPayoffs.Count > 0)
        {
            Console.WriteLine("TRANSPARENCY ISSUES (payoff opaque without plant — fix before gateway publish):");
            foreach (var p in audit.NotTransparentPayoffs)
                Console.WriteLine($"  [{p.Category.ToUpper()}] {p.PlantDescription} → {p.PayoffDescription}");
            Console.WriteLine();
        }

        if (audit.Orphaned == 0 && audit.NotTransparentCount == 0)
            Console.WriteLine("✅ All plants accounted for and transparent.");

        return audit.Orphaned > 0 || audit.NotTransparentCount > 0 ? 1 : 0;
    }
}
