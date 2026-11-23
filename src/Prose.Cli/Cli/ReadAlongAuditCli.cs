using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;
using Prose.Core.Services;
using Prose.Core.Services.ReadAlong;

namespace Prose.Cli;

/// <summary>
/// The read-along precision audit (RFC 0019, RA-S3.1.1). Read-only: the replay runs in memory and writes
/// nothing to the database.
///
///   prose --read-along-audit --slug &lt;book&gt; [--sample 100] --out &lt;file.csv&gt;     sample for the author to mark
///   prose --read-along-audit --score &lt;file.csv&gt; [--out &lt;table.md&gt;] [--apply]    precision per pattern
///
/// Mark the <c>verdict</c> column y (right) or n (wrong). <c>--apply</c> adds every pattern under 80 %
/// (with at least 5 verdicts) to the <c>readalong.patterns.disabled</c> setting.
/// </summary>
public static class ReadAlongAuditCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider sp)
    {
        string? Arg(string name) { var i = Array.IndexOf(args, name); return i >= 0 && i + 1 < args.Length && !args[i + 1].StartsWith("--") ? args[i + 1] : null; }

        if (Arg("--score") is { } csvPath)
        {
            if (!File.Exists(csvPath)) { Console.Error.WriteLine($"No file {csvPath}"); return 1; }
            var scores = ReadAlongAudit.Score(ReadAlongAudit.FromCsv(await File.ReadAllTextAsync(csvPath)));
            var md = ReadAlongAudit.ToMarkdown(scores, Path.GetFileName(csvPath));
            if (Arg("--out") is { } outMd) { await File.WriteAllTextAsync(outMd, md); Console.WriteLine($"Wrote {outMd}"); }
            else Console.Write(md);
            if (args.Contains("--apply"))
            {
                var settings = sp.GetRequiredService<SettingsKvStore>();
                var off = (settings.Get<List<string>>(ReadAlongService.DisabledPatternsKey) ?? []).ToHashSet();
                off.UnionWith(scores.Where(s => s.SwitchOff).Select(s => s.Pattern));
                settings.Set(ReadAlongService.DisabledPatternsKey, off.OrderBy(x => x).ToList());
                Console.WriteLine($"Disabled patterns now: {(off.Count == 0 ? "none" : string.Join(", ", off.OrderBy(x => x)))}");
            }
            return 0;
        }

        var slug = Arg("--slug") ?? Arg("--book");
        var outCsv = Arg("--out");
        if (slug is null || outCsv is null)
        {
            Console.Error.WriteLine("usage: prose --read-along-audit --slug <book> [--sample 100] --out <file.csv>   |   --score <file.csv> [--out <md>] [--apply]");
            return 2;
        }
        Guid? bookId;
        await using (var db = await sp.GetRequiredService<IDbContextFactory<ProseDbContext>>().CreateDbContextAsync())
            bookId = await NodeRefResolver.ResolveAsync(db, slug);
        if (bookId is null) { Console.Error.WriteLine($"No book '{slug}'."); return 1; }

        var chapters = await sp.GetRequiredService<ReadAlongService>().GetChaptersAsync(bookId.Value);
        var rows = ReadAlongAudit.Replay(chapters);
        var sample = ReadAlongAudit.Sample(rows, int.TryParse(Arg("--sample"), out var n) ? n : 100);
        await File.WriteAllTextAsync(outCsv, ReadAlongAudit.ToCsv(sample));
        Console.WriteLine($"Replayed {chapters.Count} chapters: {rows.Count(r => r.Kind == "name")} names, {rows.Count(r => r.Kind == "fact")} details, " +
                          $"{rows.Count(r => r.Kind == "relation")} relations, {rows.Count(r => r.Kind == "card")} would-be cards.");
        Console.WriteLine($"Wrote {sample.Count} rows to {outCsv}. Mark the verdict column y or n, then: prose --read-along-audit --score {outCsv}");
        return 0;
    }
}
