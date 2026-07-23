using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Cli;

/// <summary>
/// <c>ss (--publish-md | --publish-pdf) (--id &lt;guid|prefix&gt; | --slug &lt;slug&gt;) [--author "Name"]</c>
/// — render a node to Markdown or PDF in the configured publish directory (Desktop fallback).
/// Markdown output embeds <c>&lt;!-- beat:N:id7 --&gt;</c> markers enabling
/// <c>ss --import-md</c> round-trip. The headless twin of the writer page's Export items.
/// </summary>
public static class PublishManuscriptCli
{
    public enum Format { Markdown, Pdf }

    public static async Task<int> RunAsync(string[] args, IServiceProvider services, Format format)
    {
        var (tag, ext) = format switch
        {
            Format.Markdown => ("publish-md", "Markdown .md"),
            _               => ("publish-pdf", "PDF"),
        };

        string? id = null, slug = null, author = null;
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--id":     if (i + 1 < args.Length) id = args[++i]; break;
                case "--slug":   if (i + 1 < args.Length) slug = args[++i]; break;
                case "--author": if (i + 1 < args.Length) author = args[++i]; break;
            }
        }
        if (string.IsNullOrWhiteSpace(id) && string.IsNullOrWhiteSpace(slug))
        {
            Console.Error.WriteLine($"[{tag}] One of --id or --slug is required.");
            return 1;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();
        var export = services.GetRequiredService<ManuscriptExportService>();
        var cleanup = services.GetRequiredService<ExportCleanupService>();

        Guid nodeId; string nodeTitle;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var q = db.Nodes.AsNoTracking();
            Node? node;
            if (!string.IsNullOrWhiteSpace(slug)) node = await q.FirstOrDefaultAsync(s => s.Slug == slug);
            else if (Guid.TryParse(id, out var g)) node = await q.FirstOrDefaultAsync(s => s.Id == g);
            else node = await q.Where(s => s.Id.ToString().StartsWith(id!.ToLower())).Take(2).ToListAsync() switch
            { { Count: 1 } m => m[0], _ => null };
            if (node == null) { Console.Error.WriteLine($"[{tag}] Node not found."); return 1; }
            nodeId = node.Id; nodeTitle = node.Title;
        }

        Console.WriteLine($"[{tag}] Rendering \"{nodeTitle}\" to {ext}…");
        try
        {
            await cleanup.CleanAsync(nodeId);
            var path = format switch
            {
                Format.Markdown => await export.ExportMarkdownAsync(nodeId, author),
                _               => await export.ExportPdfAsync(nodeId, author),
            };
            Console.WriteLine($"[{tag}] Wrote: {path}");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine($"[{tag}] Failed: {ex.Message}"); return 1; }
    }
}
