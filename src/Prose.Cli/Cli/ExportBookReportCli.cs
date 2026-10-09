using Microsoft.EntityFrameworkCore;
using MindAttic.Export;
using MindAttic.Export.Artifacts;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// prose --export-book-report --slug &lt;slug-or-code&gt; [--formats md,txt,docx,pdf]
///
/// Writes the book's stored BookReports row (the one <c>prose --book-report</c> last overwrote) to
/// <c>{book export dir}\{CODE}_BookReport.md</c> — the same folder the book's .docx/.epub/.pdf
/// exports land in (<see cref="ExportPathResolver"/>) — in its readable markdown form, and renders
/// the same report as <c>{CODE}_BookReport.txt/.docx/.pdf</c> beside it through the shared
/// MindAttic.Export library (Letter document style; the report's first heading is the title page).
/// Reads the row; does not regenerate it. Each file is overwritten in place, as the .md always
/// was. Exits 1 if the book has no stored report yet.
/// </summary>
public static class ExportBookReportCli
{
    private static readonly ExportFormat[] DefaultFormats = [ExportFormat.Md, ExportFormat.Txt, ExportFormat.Docx, ExportFormat.Pdf];

    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var i = Array.IndexOf(args, "--slug");
        var slug = i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        if (slug is null) { Console.WriteLine("Usage: prose --export-book-report --slug <slug-or-code> [--formats md,txt,docx,pdf]"); return 1; }

        var f = Array.IndexOf(args, "--formats");
        ExportFormat[] formats;
        try
        {
            formats = f >= 0 && f + 1 < args.Length
                ? args[f + 1].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(ExportFormatExtensions.Parse).ToArray()
                : DefaultFormats;
        }
        catch (ArgumentException ex) { Console.Error.WriteLine($"[export-book-report] {ex.Message}"); return 1; }

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

        var report = await store.GetAsync(nodeId.Value);
        if (report is null)
        {
            Console.Error.WriteLine($"[export-book-report] no stored report for {node.NodeCode ?? node.Slug}; run `prose --book-report --slug {slug}` first.");
            return 1;
        }

        // The .md goes through the store, which records its path on the report row.
        if (formats.Contains(ExportFormat.Md))
        {
            var mdPath = await store.ExportAsync(nodeId.Value, dir, fileBase);
            Console.WriteLine($"[export-book-report] {node.Title} ({node.NodeCode ?? node.Slug}) -> {mdPath}");
        }

        var rendered = formats.Where(x => x != ExportFormat.Md).ToArray();
        if (rendered.Length > 0)
        {
            var stem = Path.GetFileNameWithoutExtension(BookReportStore.ExportFileName(fileBase));
            var written = await ReportExporter.ExportAsync(report.Markdown, dir, stem, rendered,
                fallbackTitle: $"{node.Title} — Book Report", subtitle: "Book Report",
                artifactOptions: new ArtifactOptions { Existing = ExistingArtifact.Overwrite });
            foreach (var (format, path) in written)
                Console.WriteLine($"[export-book-report] {node.Title} ({node.NodeCode ?? node.Slug}) -> {path}");
        }
        return 0;
    }
}
