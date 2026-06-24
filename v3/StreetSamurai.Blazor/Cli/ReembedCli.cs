using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Blazor.Cli;

/// <summary>
/// CLI surface for <see cref="EmbeddingService"/>. Bootstraps the
/// EntityEmbeddings table from the active entity corpus.
///
///   ss --reembed                     full corpus pass (drift-skipped — only
///                                    entities whose source text changed get
///                                    a new API call)
///   ss --reembed --force             same as above but invalidates every
///                                    cached hash (use after model upgrade)
/// </summary>
public static class ReembedCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider sp)
    {
        var svc = sp.GetRequiredService<EmbeddingService>();
        var force = args.Contains("--force");
        var prose = args.Contains("--prose");

        if (prose)
        {
            Console.WriteLine($"[reembed] PROSE corpus pass (chapters + beats)  force={force}");
            if (force)
            {
                await using var scope = sp.CreateAsyncScope();
                var db = scope.ServiceProvider
                    .GetRequiredService<Microsoft.EntityFrameworkCore.IDbContextFactory<StreetSamurai.Core.Data.StreetSamuraiDbContext>>();
                await using var ctx = await db.CreateDbContextAsync();
                var n = await ctx.Database.ExecuteSqlRawAsync("DELETE FROM dbo.ProseEmbeddings;");
                Console.WriteLine($"[reembed] cleared {n} existing prose rows");
            }
            int last = -1;
            var pp = new Progress<(int done, int total, string current)>(p =>
            {
                var pct = p.total > 0 ? (int)(100.0 * p.done / p.total) : 0;
                if (pct == last) return;
                last = pct;
                var label = (p.current ?? "");
                var truncated = label.Length > 40 ? label.Substring(0, 37) + "…" : label.PadRight(40);
                Console.Write($"\r[reembed] [{p.done,5}/{p.total,5}] {pct,3}%  {truncated}");
            });
            var psw = System.Diagnostics.Stopwatch.StartNew();
            int proseTouched;
            try { proseTouched = await svc.ReembedProseCorpusAsync(pp); }
            catch (Exception ex)
            {
                Console.Error.WriteLine();
                Console.Error.WriteLine($"[reembed] prose pass failed: {ex.Message}");
                return 1;
            }
            psw.Stop();
            Console.WriteLine();
            Console.WriteLine($"=== Prose reembed done in {psw.Elapsed:mm\\:ss} ===  rows written/refreshed: {proseTouched}");
            return 0;
        }

        Console.WriteLine($"[reembed] starting full corpus pass  force={force}");
        if (force)
        {
            // Quick way to force-re-embed: clear the table; EnsureFreshAsync will
            // re-create every row. Cheaper than a hash-update query.
            await using var scope = sp.CreateAsyncScope();
            var db = scope.ServiceProvider
                .GetRequiredService<Microsoft.EntityFrameworkCore.IDbContextFactory<StreetSamurai.Core.Data.StreetSamuraiDbContext>>();
            await using var ctx = await db.CreateDbContextAsync();
            var n = await ctx.Database.ExecuteSqlRawAsync("DELETE FROM dbo.EntityEmbeddings;");
            Console.WriteLine($"[reembed] cleared {n} existing rows");
        }

        var lastPct = -1;
        var progress = new Progress<(int done, int total)>(p =>
        {
            var pct = p.total > 0 ? (int)(100.0 * p.done / p.total) : 0;
            if (pct == lastPct) return;
            lastPct = pct;
            Console.Write($"\r[reembed] [{p.done,6}/{p.total,6}] {pct,3}%");
        });

        var sw = System.Diagnostics.Stopwatch.StartNew();
        int touched;
        try { touched = await svc.ReembedCorpusAsync(progress); }
        catch (Exception ex)
        {
            Console.Error.WriteLine();
            Console.Error.WriteLine($"[reembed] failed: {ex.Message}");
            return 1;
        }
        sw.Stop();

        Console.WriteLine();
        Console.WriteLine($"=== Reembed done in {sw.Elapsed:mm\\:ss} ===");
        Console.WriteLine($"  rows written/refreshed: {touched}");
        return 0;
    }
}
