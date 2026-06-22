using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Blazor.Cli;

/// <summary>
/// <c>ss --publish-docx (--id &lt;guid|prefix&gt; | --slug &lt;slug&gt;) [--author "Name"] [--export-dir &lt;path&gt;]</c>
/// — render a strand to a KDP-ready Word .docx AND a matching PDF in the same location.
/// With no <c>--export-dir</c> (and no configured PublishExportDirectory) both files write
/// to the user's Downloads folder. With <c>--export-dir</c> the docx is copied there and
/// the PDF lands in Downloads alongside it.
/// </summary>
public static class PublishDocxCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? id = null, slug = null, author = null, exportDir = null;
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--id":         if (i + 1 < args.Length) id = args[++i]; break;
                case "--slug":       if (i + 1 < args.Length) slug = args[++i]; break;
                case "--author":     if (i + 1 < args.Length) author = args[++i]; break;
                case "--export-dir": if (i + 1 < args.Length) exportDir = args[++i]; break;
            }
        }
        if (string.IsNullOrWhiteSpace(id) && string.IsNullOrWhiteSpace(slug))
        {
            Console.Error.WriteLine("[publish-docx] One of --id or --slug is required.");
            return 1;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();
        var docx = services.GetRequiredService<DocxExportService>();
        var manuscript = services.GetRequiredService<ManuscriptExportService>();

        if (!string.IsNullOrWhiteSpace(exportDir))
        {
            var settings = services.GetRequiredService<SettingsService>();
            settings.PublishExportDirectory = exportDir!;
            settings.Flush();
            Console.WriteLine($"[publish-docx] PublishExportDirectory set to: {exportDir}");
        }

        Guid strandId; string strandTitle;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var q = db.Strands.AsNoTracking();
            Strand? strand;
            if (!string.IsNullOrWhiteSpace(slug)) strand = await q.FirstOrDefaultAsync(s => s.Slug == slug);
            else if (Guid.TryParse(id, out var g)) strand = await q.FirstOrDefaultAsync(s => s.Id == g);
            else strand = await q.Where(s => s.Id.ToString().StartsWith(id!.ToLower())).Take(2).ToListAsync() switch
            { { Count: 1 } m => m[0], _ => null };
            if (strand == null) { Console.Error.WriteLine("[publish-docx] Strand not found."); return 1; }
            strandId = strand.Id; strandTitle = strand.Title;
        }

        Console.WriteLine($"[publish-docx] Rendering \"{strandTitle}\" to .epub + .docx + .pdf…");
        try
        {
            var epubPath = await manuscript.ExportEpubAsync(strandId, author);
            Console.WriteLine($"[publish-docx] Wrote epub: {epubPath}");
            var docxPath = await docx.ExportStrandAsync(strandId, author);
            Console.WriteLine($"[publish-docx] Wrote docx: {docxPath}");
            var pdfPath = await manuscript.ExportPdfAsync(strandId, author);
            Console.WriteLine($"[publish-docx] Wrote pdf:  {pdfPath}");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine($"[publish-docx] Failed: {ex.Message}"); return 1; }
    }
}
