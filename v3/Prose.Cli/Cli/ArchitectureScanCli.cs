using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>prose --architecture-scan [--json] [--out &lt;file&gt;] [--top &lt;n&gt;] [--force]</c>
///
/// Automated, repeatable version of the manual 8-agent "Engine Manifest" service audit run
/// 2026-09-01 (services, DI registrations, CLI verbs, MCP tools, standalone scripts, and
/// name-overlap clusters worth a second look). Thin CLI wrapper over
/// <see cref="ProjectArchitectureService"/> — see that class for the actual scan logic and for
/// why it went dead (and was resurrected) in the first place.
///
/// Default output is a console summary plus the top duplicate-name clusters. <c>--json</c>
/// prints the full <see cref="ArchitectureSnapshot"/> instead (to stdout, or to <c>--out</c> if
/// given). <c>--force</c> bypasses the service's in-memory cache and rescans from disk.
/// </summary>
public static class ArchitectureScanCli
{
    public static Task<int> RunAsync(string[] args, IServiceProvider sp)
    {
        var asJson = args.Contains("--json");
        var force  = args.Contains("--force");
        var outArg = ArgValue(args, "--out");
        var top    = int.TryParse(ArgValue(args, "--top"), out var t) ? t : 15;

        var svc = sp.GetRequiredService<ProjectArchitectureService>();
        var snap = svc.Scan(force);

        if (asJson)
        {
            var json = JsonSerializer.Serialize(snap, new JsonSerializerOptions { WriteIndented = true });
            if (!string.IsNullOrWhiteSpace(outArg))
            {
                File.WriteAllText(outArg, json);
                Console.Error.WriteLine($"[architecture-scan] wrote {outArg}");
            }
            else
            {
                Console.WriteLine(json);
            }
            return Task.FromResult(0);
        }

        Console.WriteLine($"[architecture-scan] {snap.RepoRoot}  (scanned {snap.ScannedAt:u})");
        Console.WriteLine($"  Services:         {snap.Services.Count}");
        Console.WriteLine($"  DI registrations: {snap.DiRegistrations.Count}");
        Console.WriteLine($"  CLI commands:     {snap.CliCommands.Count}");
        Console.WriteLine($"  MCP tools:        {snap.McpTools.Count}");
        Console.WriteLine($"  Scripts:          {snap.Scripts.Count}");
        Console.WriteLine();

        if (snap.DuplicateClusters.Count == 0)
        {
            Console.WriteLine("No name-overlap clusters found.");
            return Task.FromResult(0);
        }

        Console.WriteLine($"Top {Math.Min(top, snap.DuplicateClusters.Count)} name-overlap clusters (token → members — a lead, not a verdict; read each member before assuming it's a real duplicate):");
        foreach (var cluster in snap.DuplicateClusters.Take(top))
        {
            Console.WriteLine($"  '{cluster.Token}' ({cluster.Members.Count}):");
            foreach (var m in cluster.Members)
                Console.WriteLine($"    [{m.Source,-7}] {m.Name,-40} {m.File}");
        }

        return Task.FromResult(0);
    }

    private static string? ArgValue(string[] args, string flag)
    {
        var i = Array.IndexOf(args, flag);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }
}
