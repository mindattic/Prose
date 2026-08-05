using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Cli;

/// <summary>
/// <c>ss --generate-cover-prompt</c> — (re)generate Node.CoverPrompt, the visual
/// image-model prompt for a book's cover, from its Title/Summary/Description/universe.
///
/// Args:
///   --slug &lt;slug&gt;   Target book node by slug or NodeCode. Required unless --all is set.
///   --all            Process every BookNode (Kind == "book").
/// </summary>
public static class GenerateCoverPromptCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? slug = null;
        bool all = args.Contains("--all");

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--slug" && i + 1 < args.Length) slug = args[++i];
        }

        if (!all && string.IsNullOrWhiteSpace(slug))
        {
            Console.Error.WriteLine("[generate-cover-prompt] --slug <slug> or --all is required.");
            Console.Error.WriteLine("Usage: ss --generate-cover-prompt --slug <slug>");
            Console.Error.WriteLine("       ss --generate-cover-prompt --all");
            return 2;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();
        var coverSvc  = services.GetRequiredService<CoverPromptService>();

        if (all)
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            var nodes = await db.Nodes
                .Where(n => n.Kind == "book")
                .OrderBy(n => n.NodeCode ?? n.Title)
                .Select(n => new { n.Id, n.NodeCode, n.Title, n.Slug })
                .ToListAsync();

            if (nodes.Count == 0)
            {
                Console.Error.WriteLine("[generate-cover-prompt] No book nodes found.");
                return 1;
            }

            Console.WriteLine($"[generate-cover-prompt] Processing {nodes.Count} books…");
            int ok = 0, fail = 0;
            foreach (var n in nodes)
            {
                try
                {
                    var prompt = await coverSvc.GenerateAndSaveAsync(n.Id);
                    Console.WriteLine($"  ✓ {(n.NodeCode ?? n.Slug),-14} {n.Title}");
                    Console.WriteLine($"      {prompt}");
                    ok++;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"  ✗ {(n.NodeCode ?? n.Slug),-14} {n.Title} — {ex.Message}");
                    fail++;
                }
            }

            Console.WriteLine($"[generate-cover-prompt] Done: {ok} succeeded, {fail} failed.");
            return fail > 0 ? 1 : 0;
        }
        else
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            var node = await db.Nodes.AsNoTracking()
                .FirstOrDefaultAsync(n => n.Slug == slug || n.NodeCode == slug);
            if (node == null)
            {
                Console.Error.WriteLine($"[generate-cover-prompt] Node '{slug}' not found.");
                return 1;
            }

            var prompt = await coverSvc.GenerateAndSaveAsync(node.Id);
            Console.WriteLine($"[generate-cover-prompt] {node.Title} ({node.Slug})");
            Console.WriteLine();
            Console.WriteLine(prompt);
            return 0;
        }
    }
}

/// <summary>
/// <c>ss --generate-cover-image</c> — render Node.CoverPrompt through a chosen image
/// provider and save the result under the media dir. Generates a CoverPrompt first
/// if the node doesn't have one yet. Costs real money per call — requires that
/// provider's API key configured in Settings.
///
/// Args:
///   --slug &lt;slug&gt;        Target book node by slug or NodeCode. Required.
///   --provider &lt;id&gt;      "openai" | "stability" | "google". Required.
/// </summary>
public static class GenerateCoverImageCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? slug = null;
        string? provider = null;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--slug":     if (i + 1 < args.Length) slug     = args[++i]; break;
                case "--provider": if (i + 1 < args.Length) provider = args[++i]; break;
            }
        }

        if (string.IsNullOrWhiteSpace(slug) || string.IsNullOrWhiteSpace(provider))
        {
            Console.Error.WriteLine("Usage: ss --generate-cover-image --slug <slug> --provider openai|stability|google");
            return 2;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();
        var coverSvc  = services.GetRequiredService<CoverImageService>();

        await using var db = await dbFactory.CreateDbContextAsync();
        var node = await db.Nodes.AsNoTracking()
            .FirstOrDefaultAsync(n => n.Slug == slug || n.NodeCode == slug);
        if (node == null)
        {
            Console.Error.WriteLine($"[generate-cover-image] Node '{slug}' not found.");
            return 1;
        }

        try
        {
            var path = await coverSvc.GenerateAndSaveAsync(node.Id, provider);
            Console.WriteLine($"[generate-cover-image] {node.Title} ({node.Slug}) — saved {path} via {provider}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[generate-cover-image] Failed: {ex.Message}");
            return 1;
        }
    }
}

/// <summary>
/// <c>ss --composite-cover-title</c> — (re)draw the book title onto an already-saved
/// cover image file in place, without calling an image-generation API again. Useful
/// when the title-compositing step needs a tweak (or ran on a cover saved before it
/// existed) and you don't want to pay for a fresh render.
///
/// Args:
///   --slug &lt;slug&gt;   Target book node by slug or NodeCode. Required.
/// </summary>
public static class CompositeCoverTitleCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? slug = null;
        for (int i = 0; i < args.Length; i++)
            if (args[i] == "--slug" && i + 1 < args.Length) slug = args[++i];

        if (string.IsNullOrWhiteSpace(slug))
        {
            Console.Error.WriteLine("Usage: ss --composite-cover-title --slug <slug>");
            return 2;
        }

        var dbFactory   = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();
        var compositor  = services.GetRequiredService<CoverTitleCompositorService>();
        var paths       = services.GetRequiredService<StreetSamurai.Core.Interfaces.IPathProvider>();

        await using var db = await dbFactory.CreateDbContextAsync();
        var node = await db.Nodes.AsNoTracking()
            .FirstOrDefaultAsync(n => n.Slug == slug || n.NodeCode == slug);
        if (node == null)
        {
            Console.Error.WriteLine($"[composite-cover-title] Node '{slug}' not found.");
            return 1;
        }
        if (string.IsNullOrWhiteSpace(node.CoverImagePath))
        {
            Console.Error.WriteLine($"[composite-cover-title] Node '{slug}' has no CoverImagePath yet — run --generate-cover-image first.");
            return 1;
        }

        var fullPath  = Path.Combine(paths.MediaDir, node.CoverImagePath);
        var extension = Path.GetExtension(fullPath).TrimStart('.');
        var bytes     = await File.ReadAllBytesAsync(fullPath);

        var composited = await compositor.CompositeTitleAsync(bytes, node.Title, extension);
        await File.WriteAllBytesAsync(fullPath, composited);

        Console.WriteLine($"[composite-cover-title] {node.Title} ({node.Slug}) — updated {fullPath}");
        return 0;
    }
}
