using System.Text.Json;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>ss --kdp-manifest [--out &lt;path&gt;] [--userscript]</c> — thin wrapper over
/// <see cref="KdpManifestService"/> (shared with the KdpPublish app, which consumes the same
/// entries in-process instead of via this CLI). Parses args, calls the service, prints a status
/// table, writes <c>manifest.json</c>, and optionally regenerates the browser userscript.
/// </summary>
public static class KdpManifestCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? outPath = null;
        var writeUserscript = false;
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--out": if (i + 1 < args.Length) outPath = args[++i]; break;
                case "--userscript": writeUserscript = true; break;
            }
        }

        var repoRoot = KdpManifestService.FindRepoRoot();
        var kdpDir = Path.Combine(repoRoot, "tools", "kdp");
        Directory.CreateDirectory(kdpDir);
        outPath ??= Path.Combine(kdpDir, "manifest.json");

        var manifestService = services.GetRequiredService<KdpManifestService>();
        var entries = await manifestService.BuildAsync(repoRoot);

        Console.WriteLine($"\n{"CODE",-8}  {"STATUS",-14}  {"V",-4}  {"FILES",-7}  {"TITLEID",-8}  NOTE");
        Console.WriteLine(new string('-', 90));
        foreach (var e in entries)
        {
            var filesFlag = e.DocxPath != null && e.EpubPath != null ? "ok" : "MISSING";
            Console.WriteLine($"{e.Code,-8}  {e.PublicationStatus,-14}  V{e.Version,-3}  {filesFlag,-7}  {(e.KdpTitleId ?? "—"),-8}  {e.Warning ?? ""}");
        }
        Console.WriteLine(new string('-', 90));
        Console.WriteLine($"[kdp-manifest] {entries.Count} tracked  |  {entries.Count(e => e.NeedsRepublish)} need republish  |  {entries.Count(e => e.DocxPath == null)} missing files  |  {entries.Count(e => e.KdpDirectEditUrl != null)} have direct edit links\n");

        var jsonOpts = new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var json = JsonSerializer.Serialize(entries, jsonOpts);
        await File.WriteAllTextAsync(outPath, json);
        Console.WriteLine($"[kdp-manifest] Wrote {outPath}");

        if (writeUserscript)
        {
            var wrote = KdpUserscriptBuilder.Build(kdpDir, entries, jsonOpts);
            Console.WriteLine(wrote
                ? $"[kdp-manifest] Regenerated {Path.Combine(kdpDir, "kdp-panel.user.js")}"
                : "[kdp-manifest] ⚠ No tools/kdp/kdp-panel.template.js found — skipped userscript regeneration.");
        }

        return 0;
    }
}
