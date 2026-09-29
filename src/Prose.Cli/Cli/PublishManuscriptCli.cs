using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>prose (--publish-md | --publish-pdf) (--id &lt;guid|prefix&gt; | --slug &lt;slug&gt;) [--author "Name"]</c>
/// — render a node to Markdown or PDF in the configured publish directory (Desktop fallback).
/// Markdown output embeds <c>&lt;!-- beat:N:id7 --&gt;</c> markers enabling
/// <c>prose --import-md</c> round-trip. The headless twin of the writer page's Export items.
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

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        var export = services.GetRequiredService<ManuscriptExportService>();
        var cleanup = services.GetRequiredService<ExportCleanupService>();

        Guid nodeId; string nodeTitle;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            // The shared resolver: NodeCode, books outside the current universe, unique prefixes,
            // ambiguity refused. The private copy applied the ambient universe filter to every
            // branch (a book in another universe was "not found") and took the first same-slug row.
            Node? node = await NodeRefResolver.ResolveNodeAsync(db, !string.IsNullOrWhiteSpace(slug) ? slug : id);
            if (node == null) { Console.Error.WriteLine($"[{tag}] Node not found."); return 1; }
            nodeId = node.Id; nodeTitle = node.Title;
        }

        Console.WriteLine($"[{tag}] Rendering \"{nodeTitle}\" to {ext}…");
        try
        {
            var nodeDir = await cleanup.CleanAsync(nodeId);
            var path = format switch
            {
                Format.Markdown => await export.ExportMarkdownAsync(nodeId, author),
                _               => await export.ExportPdfAsync(nodeId, author),
            };
            await using var dbVersion = await dbFactory.CreateDbContextAsync();
            var version = await dbVersion.Nodes.AsNoTracking().IgnoreQueryFilters().Where(n => n.Id == nodeId)
                .Select(n => n.Version).FirstOrDefaultAsync();
            cleanup.ArchiveCurrent(nodeDir, version);
            Console.WriteLine($"[{tag}] Wrote: {path}");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine($"[{tag}] Failed: {ex.Message}"); return 1; }
    }
}
