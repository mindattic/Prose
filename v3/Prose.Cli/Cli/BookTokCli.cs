using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;
using Prose.Core.Interfaces;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>prose --booktok</c> — composite a book cover onto a 3D mockup template, generate a
/// short AI image-to-video clip (hand shows the cover, opens it, flips pages) via a chosen
/// video provider, and assemble a vertical 1080x1920 #booktok MP4. Costs real money per
/// call unless <c>--dry-run</c>, which stops after the local ImageMagick mockup + payload
/// validation. Note: the AI clip's page-flip motion is generic/blurred — there is no real
/// interior page-spread art to render.
///
/// DB mode (pulls cover + title from the node):
///   prose --booktok --slug &lt;slug&gt; --provider kling|runway|sora [--duration 8] [--template default] [--prompt "..."] [--dry-run] [--yes]
///
/// Standalone mode (no DB — takes a cover file directly):
///   prose --booktok --standalone --cover-path &lt;path&gt; --title "&lt;title&gt;" --provider kling|runway|sora [...]
/// </summary>
public static class BookTokCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? slug = null, provider = null, template = "default", prompt = null, title = null, coverPath = null;
        var duration = 8;
        var dryRun = args.Contains("--dry-run");
        var standalone = args.Contains("--standalone");

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--slug":       if (i + 1 < args.Length) slug      = args[++i]; break;
                case "--provider":   if (i + 1 < args.Length) provider  = args[++i]; break;
                case "--duration":   if (i + 1 < args.Length && int.TryParse(args[++i], out var d)) duration = d; break;
                case "--template":   if (i + 1 < args.Length) template  = args[++i]; break;
                case "--prompt":     if (i + 1 < args.Length) prompt    = args[++i]; break;
                case "--cover-path": if (i + 1 < args.Length) coverPath = args[++i]; break;
                case "--title":      if (i + 1 < args.Length) title     = args[++i]; break;
            }
        }

        if (string.IsNullOrWhiteSpace(provider))
        {
            PrintUsage();
            return 2;
        }

        string effectiveCoverPath;
        string effectiveSlug;

        if (standalone)
        {
            if (string.IsNullOrWhiteSpace(coverPath) || !File.Exists(coverPath))
            {
                Console.Error.WriteLine($"[booktok] --cover-path '{coverPath}' not found.");
                return 1;
            }
            effectiveCoverPath = coverPath;
            effectiveSlug = Slugify(!string.IsNullOrWhiteSpace(title) ? title : Path.GetFileNameWithoutExtension(coverPath));
        }
        else
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                PrintUsage();
                return 2;
            }

            var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
            var paths     = services.GetRequiredService<IPathProvider>();
            await using var db = await dbFactory.CreateDbContextAsync();
            // IgnoreQueryFilters(): explicit id/slug, not ambient scope (2026-08-17).
            var node = await db.Nodes.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(n => n.Slug == slug || n.NodeCode == slug);
            if (node == null)
            {
                Console.Error.WriteLine($"[booktok] Node '{slug}' not found.");
                return 1;
            }
            if (string.IsNullOrWhiteSpace(node.CoverImagePath))
            {
                Console.Error.WriteLine($"[booktok] Node '{slug}' has no CoverImagePath yet — run --generate-cover-image first.");
                return 1;
            }

            effectiveCoverPath = Path.Combine(paths.MediaDir, node.CoverImagePath);
            effectiveSlug      = node.Slug;
            title ??= node.Title;
        }

        var videoSvc = services.GetRequiredService<BookTokVideoService>();

        try
        {
            var result = await videoSvc.GenerateAsync(new BookTokVideoService.Options(
                CoverPath: effectiveCoverPath,
                Slug: effectiveSlug,
                ProviderId: provider,
                DurationSeconds: duration,
                TemplateName: template ?? "default",
                Prompt: prompt,
                Title: title,
                DryRun: dryRun));

            Console.WriteLine($"[booktok] mockup : {result.MockupPath}");
            if (result.ClipPath != null)
                Console.WriteLine($"[booktok] clip   : {result.ClipPath}");
            if (result.FinalVideoPath != null)
                Console.WriteLine($"[booktok] final  : {result.FinalVideoPath}");
            else if (dryRun)
                Console.WriteLine("[booktok] dry-run — stopped before submitting a paid job.");

            if (!standalone && result.FinalVideoPath != null)
                await StampNodeAsync(services, effectiveSlug, provider, result.FinalVideoPath);

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[booktok] Failed: {ex.Message}");
            return 1;
        }
    }

    private static async Task StampNodeAsync(IServiceProvider services, string slug, string provider, string finalVideoPath)
    {
        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        var paths     = services.GetRequiredService<IPathProvider>();
        await using var db = await dbFactory.CreateDbContextAsync();
        // IgnoreQueryFilters(): explicit id/slug, not ambient scope (2026-08-17).
        var row = await db.Nodes.IgnoreQueryFilters().FirstOrDefaultAsync(n => n.Slug == slug);
        if (row == null) return;

        row.BookTokVideoPath        = Path.GetRelativePath(paths.MediaDir, finalVideoPath);
        row.BookTokVideoProvider    = provider;
        row.BookTokVideoGeneratedAt = DateTime.UtcNow;
        row.UpdatedAt               = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    private static string Slugify(string input)
    {
        var sb = new StringBuilder();
        var lastDash = false;
        foreach (var c in input.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(c)) { sb.Append(c); lastDash = false; }
            else if (!lastDash) { sb.Append('-'); lastDash = true; }
        }
        return sb.ToString().Trim('-');
    }

    private static void PrintUsage()
    {
        Console.Error.WriteLine("Usage: prose --booktok --slug <slug> --provider kling|runway|sora [--duration 8] [--template default] [--prompt \"...\"] [--dry-run] [--yes]");
        Console.Error.WriteLine("       prose --booktok --standalone --cover-path <path> --title \"<title>\" --provider kling|runway|sora [...]");
    }
}
