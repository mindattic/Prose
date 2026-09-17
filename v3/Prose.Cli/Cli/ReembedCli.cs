using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// CLI surface for <see cref="EmbeddingService"/>. Bootstraps the
/// EntityEmbeddings table from the active entity corpus.
///
///   prose --reembed                     full corpus pass (drift-skipped — only
///                                    entities whose source text changed get
///                                    a new API call)
///   prose --reembed --force             same as above but invalidates every
///                                    cached hash (use after model upgrade)
///   prose --reembed --beats --slug S    backfill ProseEmbeddings for ONE book's beats
///
/// <para><b>Why <c>--beats</c> exists</b> (RFC 0013, 2026-09-16). Nothing maintains the beat
/// embedding index on beat creation. The only production path that refreshes it is
/// <c>SemanticFidelityService</c>, which <c>NodeWorkbenchService.UpdateBeatTextAsync</c> runs only
/// when <c>!deferAnalysis</c> AND the beat carries a non-empty <c>Description</c>. Beats made by
/// <c>split_beat</c> have neither, so they are never embedded. The GCSH calibration fixture was
/// split from 12 chapter-sized beats to 96 and the index kept the original 12: a <c>k=400</c>
/// similarity sweep over the whole <c>gutenberg</c> universe returned twelve rows, and the
/// obligation resurfacing judge has been retrieving candidates from that stub index for the entire
/// calibration programme. A retrieval tier serving 12 of 116 beats reported no error to anyone.</para>
/// </summary>
public static class ReembedCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider sp)
    {
        var svc = sp.GetRequiredService<EmbeddingService>();
        var force = args.Contains("--force");
        var prose = args.Contains("--prose");
        var markdown = args.Contains("--markdown");
        var beats = args.Contains("--beats");

        if (beats)
        {
            var slugIdx = Array.IndexOf(args, "--slug");
            if (slugIdx < 0 || slugIdx + 1 >= args.Length)
            {
                Console.Error.WriteLine("[reembed] --beats requires --slug <book>.");
                return 2;
            }
            var slug = args[slugIdx + 1];
            var dbFactory = sp.GetRequiredService<IDbContextFactory<Prose.Core.Data.ProseDbContext>>();
            Guid bookId;
            await using (var db = await dbFactory.CreateDbContextAsync())
            {
                var resolved = await NodeRefResolver.ResolveAsync(db, slug);
                if (resolved == null) { Console.Error.WriteLine($"[reembed] {NodeRefResolver.NotFoundMessage(slug)}"); return 1; }
                bookId = resolved.Value;
            }

            var bsw = System.Diagnostics.Stopwatch.StartNew();
            int embedded;
            try { embedded = await svc.ReembedBeatNodesAsync(bookId); }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[reembed] beat pass failed: {ex.Message}");
                return 1;
            }
            bsw.Stop();
            Console.WriteLine($"[reembed] {slug} — {embedded} beat embedding(s) written/refreshed in {bsw.Elapsed:mm\\:ss}.");
            Console.WriteLine("  Drift-skipped: a beat whose source text is unchanged costs nothing.");
            Console.WriteLine("  Verify with: prose --obligations coverage --slug " + slug);
            return 0;
        }

        if (markdown)
        {
            Console.WriteLine($"[reembed] MARKDOWN corpus pass (Doc Context Stack)  force={force}");
            if (force)
            {
                await using var scope = sp.CreateAsyncScope();
                var dbf = scope.ServiceProvider
                    .GetRequiredService<Microsoft.EntityFrameworkCore.IDbContextFactory<Prose.Core.Data.ProseDbContext>>();
                await using var ctx = await dbf.CreateDbContextAsync();
                var n = await ctx.Database.ExecuteSqlRawAsync("DELETE FROM dbo.ProseEmbeddings WHERE ScopeKind = 'markdown';");
                Console.WriteLine($"[reembed] cleared {n} existing markdown rows");
            }
            var lastMd = -1;
            var mdProgress = new Progress<(int done, int total)>(p =>
            {
                var pct = p.total > 0 ? (int)(100.0 * p.done / p.total) : 0;
                if (pct == lastMd) return;
                lastMd = pct;
                Console.Write($"\r[reembed] [{p.done,5}/{p.total,5}] {pct,3}%");
            });
            var mdsw = System.Diagnostics.Stopwatch.StartNew();
            int mdTouched;
            try { mdTouched = await svc.ReembedMarkdownAsync(mdProgress); }
            catch (Exception ex)
            {
                Console.Error.WriteLine();
                Console.Error.WriteLine($"[reembed] markdown pass failed: {ex.Message}");
                return 1;
            }
            mdsw.Stop();
            Console.WriteLine();
            Console.WriteLine($"=== Markdown reembed done in {mdsw.Elapsed:mm\\:ss} ===  rows written/refreshed: {mdTouched}");
            return 0;
        }

        if (prose)
        {
            Console.WriteLine($"[reembed] PROSE corpus pass (chapters + beats)  force={force}");
            if (force)
            {
                await using var scope = sp.CreateAsyncScope();
                var db = scope.ServiceProvider
                    .GetRequiredService<Microsoft.EntityFrameworkCore.IDbContextFactory<Prose.Core.Data.ProseDbContext>>();
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
                .GetRequiredService<Microsoft.EntityFrameworkCore.IDbContextFactory<Prose.Core.Data.ProseDbContext>>();
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
