using Microsoft.Extensions.DependencyInjection;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Cli;

/// <summary>
/// CLI surface for <see cref="FactInterpreterService"/>.
///
///   ss --interpret --text "..."
///   ss --interpret --file path.txt
///       Run extraction in DRY-RUN mode (default). Prints what entities + edges
///       WOULD be created. Nothing committed.
///
///   ss --interpret --file path.txt --commit
///       Actually wire the resolved entities + edges into canon.
///
///   Optional flags:
///       --auto-create        create stub entities for unresolved names
///       --tag &lt;source&gt;   override Edges.Source / EntityStateEvents.Source
///       --no-ledger          skip writing EntityStateEvent rows
/// </summary>
public static class InterpretCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider sp)
    {
        var text = ArgValue(args, "--text");
        var file = ArgValue(args, "--file");
        if (string.IsNullOrWhiteSpace(text) && string.IsNullOrWhiteSpace(file))
        {
            PrintUsage();
            return 1;
        }

        var description = !string.IsNullOrWhiteSpace(text)
            ? text!
            : await File.ReadAllTextAsync(file!);

        var dryRun     = !args.Contains("--commit");
        var autoCreate = args.Contains("--auto-create");
        var noLedger   = args.Contains("--no-ledger");
        var tag        = ArgValue(args, "--tag") ?? "interpret:cli";

        var svc = sp.GetRequiredService<FactInterpreterService>();
        var opts = new FactInterpreterService.InterpretationOptions(
            DryRun:       dryRun,
            AutoCreate:   autoCreate,
            RecordLedger: !noLedger,
            SourceTag:    tag);

        Console.WriteLine($"[interpret] mode={(dryRun ? "DRY-RUN" : "COMMIT")}  auto-create={autoCreate}  ledger={!noLedger}");
        var progress = new Progress<string>(Console.WriteLine);
        var result = await svc.InterpretAsync(description, opts, progress);

        Console.WriteLine();
        Console.WriteLine($"  ── ENTITIES ({result.Entities.Count}) ──");
        foreach (var e in result.Entities)
        {
            var status = e.WasCreated ? "NEW " :
                         e.ResolvedId.HasValue ? "✓ existing" :
                         "(unresolved)";
            Console.WriteLine($"  [{e.EntityType,-12}] {e.Name,-40} {status}");
        }

        Console.WriteLine();
        Console.WriteLine($"  ── RELATIONS ({result.Relations.Count}) ──");
        foreach (var r in result.Relations)
        {
            var marker = r.Skipped != null ? "✘ " + r.Skipped :
                         r.Wired ? (dryRun ? "would write" : "✓ wired") :
                         "(skipped)";
            Console.WriteLine($"  {r.FromName,-30} --[{r.RelationType}]--> {r.ToName,-30}   {marker}");
        }

        Console.WriteLine();
        Console.WriteLine($"  entities created : {result.EntitiesCreated}");
        Console.WriteLine($"  edges written    : {result.EdgesWritten}");
        Console.WriteLine($"  ledger events    : {result.LedgerEvents}");
        if (dryRun)
            Console.WriteLine();
            Console.WriteLine("  (dry-run — re-run with --commit to apply)");
        return 0;
    }

    private static string? ArgValue(string[] args, string flag)
    {
        var i = Array.IndexOf(args, flag);
        return (i >= 0 && i + 1 < args.Length) ? args[i + 1] : null;
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  ss --interpret --text \"...\"   [--commit] [--auto-create] [--no-ledger] [--tag <source>]");
        Console.WriteLine("  ss --interpret --file path.txt  [--commit] [--auto-create] [--no-ledger] [--tag <source>]");
    }
}
