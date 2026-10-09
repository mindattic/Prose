using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// prose --book-report --slug &lt;slug-or-code&gt; [--out &lt;dir&gt;] [--code &lt;CODE&gt;] [--complete]
///
/// Deterministic report pull (see <see cref="BookReportService"/>'s doc comment) — findings by
/// status/category, logic-sweep convergence, and any Decision Ledger entries mentioning this book,
/// rendered to one markdown file. Writes to Downloads\GLMZ_Book_Reports\{CODE}[_complete].md by
/// default, matching the existing drafts/{CODE}.md -&gt; make_pdfs.py -&gt; {CODE}.pdf pipeline (the
/// author runs make_pdfs.py by hand afterward; this command does not invoke Python).
///
/// --code overrides the filename stem (defaults to the resolved node's own NodeCode, falling back
/// to its Slug when the book has none on file yet). --complete appends _complete — pass it once
/// this book's backlog has actually been read through and corrected, same convention as this
/// session's BLST_complete.md/.pdf; omitting it writes the plain {CODE}.md draft name.
///
/// Every run ALSO (a) appends a row to the BookReports table and (b) rewrites
/// {book export dir}\{CODE}_BookReport.md, so the newest report is always in the book's own export
/// directory and every earlier one stays queryable (see <see cref="BookReportStore"/>). The
/// Downloads draft above is unchanged. --no-export skips the export-dir file (DB row only).
/// </summary>
public static class BookReportCli
{
    private const string DefaultOutDir = @"C:\Users\ryand\Downloads\GLMZ_Book_Reports";

    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var slug = ArgValue(args, "--slug");
        if (slug is null) { PrintUsage(); return 1; }

        var outDir   = ArgValue(args, "--out") ?? DefaultOutDir;
        var codeArg  = ArgValue(args, "--code");
        var complete = args.Contains("--complete");

        var reportService = services.GetRequiredService<BookReportService>();
        var result = await reportService.GenerateAsync(slug, complete);
        if (result is null) { Console.Error.WriteLine($"[book-report] node not found: {slug}"); return 1; }

        var code = codeArg ?? result.NodeCode ?? result.NodeSlug;
        var draftsDir = Path.Combine(outDir, "drafts");
        Directory.CreateDirectory(draftsDir);

        var stem = complete ? $"{code}_complete" : code;
        var mdPath = Path.Combine(draftsDir, $"{stem}.md");
        await File.WriteAllTextAsync(mdPath, result.Markdown);

        Console.WriteLine($"[book-report] {result.Title} ({code}) -> {mdPath}");

        string? exportDir = null, fileBase = null;
        if (!args.Contains("--no-export"))
        {
            (exportDir, fileBase) = await ResolveExportDirAsync(services, result.NodeId);
            Console.WriteLine(exportDir is null
                ? "[book-report] export directory could not be resolved; recording the DB row only."
                : $"[book-report] export dir: {exportDir}");
        }

        var store = services.GetRequiredService<BookReportStore>();
        var saved = await store.SaveAsync(result, complete, exportDir, fileBase);
        Console.WriteLine($"[book-report] BookReports row {saved.Id}" +
            (saved.ExportFilePath is null ? "" : $"; latest report -> {saved.ExportFilePath}"));
        Console.WriteLine("[book-report] run make_pdfs.py in the Downloads folder to refresh the PDF.");
        return 0;
    }

    private static async Task<(string? Dir, string? FileBase)> ResolveExportDirAsync(IServiceProvider services, Guid nodeId)
    {
        try
        {
            var settings = services.GetRequiredService<SettingsService>();
            var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
            await using var db = await dbFactory.CreateDbContextAsync();
            var node = await db.Nodes.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(n => n.Id == nodeId);
            if (node is null) return (null, null);
            var universeSlug = await db.Set<Prose.Core.Data.Entities.Universe>().AsNoTracking()
                .Where(u => u.Id == node.UniverseId).Select(u => u.Slug).FirstOrDefaultAsync() ?? "glmz";
            var baseDir = settings.GetExportDirectory(universeSlug);
            var (dir, fileBase) = await ExportPathResolver.ResolveAsync(db, node, baseDir, CancellationToken.None);
            return (dir, fileBase);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[book-report] export dir resolve failed: {ex.Message}");
            return (null, null);
        }
    }

    private static string? ArgValue(string[] args, string flag)
    {
        var i = Array.IndexOf(args, flag);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Usage: prose --book-report --slug <slug-or-code> [--out <dir>] [--code <CODE>] [--complete] [--no-export]");
    }
}
