using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// prose --export-book-report --slug &lt;slug-or-code&gt;
///
/// Writes the book's stored BookReports row (the one <c>prose --book-report</c> last overwrote) to
/// <c>{book export dir}\{CODE}_BookReport.md</c> — the same folder the book's .docx/.epub/.pdf
/// exports land in (<see cref="ExportPathResolver"/>) — in its readable markdown form. Reads the
/// row; does not regenerate it. Exits 1 if the book has no stored report yet.
/// </summary>
public static class ExportBookReportCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var i = Array.IndexOf(args, "--slug");
        var slug = i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        if (slug is null) { Console.WriteLine("Usage: prose --export-book-report --slug <slug-or-code>"); return 1; }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        var settings = services.GetRequiredService<SettingsService>();
        var store = services.GetRequiredService<BookReportStore>();

        await using var db = await dbFactory.CreateDbContextAsync();
        var nodeId = await NodeRefResolver.ResolveAsync(db, slug, CancellationToken.None);
        if (nodeId is null) { Console.Error.WriteLine($"[export-book-report] node not found: {slug}"); return 1; }
        var node = await db.Nodes.IgnoreQueryFilters().AsNoTracking().FirstAsync(n => n.Id == nodeId);

        var universeSlug = await db.Set<Prose.Core.Data.Entities.Universe>().AsNoTracking()
            .Where(u => u.Id == node.UniverseId).Select(u => u.Slug).FirstOrDefaultAsync() ?? "glmz";
        var (dir, fileBase) = await ExportPathResolver.ResolveAsync(db, node, settings.GetExportDirectory(universeSlug), CancellationToken.None);

        var path = await store.ExportAsync(nodeId.Value, dir, fileBase);
        if (path is null)
        {
            Console.Error.WriteLine($"[export-book-report] no stored report for {node.NodeCode ?? node.Slug}; run `prose --book-report --slug {slug}` first.");
            return 1;
        }
        Console.WriteLine($"[export-book-report] {node.Title} ({node.NodeCode ?? node.Slug}) -> {path}");
        return 0;
    }
}
