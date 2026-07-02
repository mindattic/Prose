using Microsoft.EntityFrameworkCore;
using MindAttic.Media;
using StreetSamurai.Core.Data;

namespace StreetSamurai.Blazor.Cli;

/// <summary>
/// <c>ss --import-cover</c> — import a local image file into the Media table.
///
/// Usage:
///   ss --import-cover --file PATH [--strand-code CODE] [--type TYPE] [--notes TEXT]
///
/// Arguments:
///   --file PATH          Required. Path to the image file (png, jpg, webp).
///   --strand-code CODE   Associate with a strand by its StrandCode (e.g. ATTE, VATD).
///                        Omit for global assets like logos or watermarks.
///   --type TYPE          Media type. Default: cover_image.
///                        Values: cover_image | logo | watermark | banner | thumbnail | promotional
///   --notes TEXT         Optional free-text note.
///   --dry-run            Parse and validate only — do not write to DB.
///
/// Examples:
///   ss --import-cover --file "R:\Desktop\EPub\MindAttic\GLMZ\Sparrow\cover.jpg" --strand-code SPRW
///   ss --import-cover --file "R:\Desktop\EPub\MindAttic\GLMZ\M.png" --type logo
///   ss --import-cover --file "R:\Desktop\EPub\MindAttic\GLMZ\RedBand.png" --type watermark
/// </summary>
public static class ImportCoverImageCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider sp)
    {
        var file       = Arg(args, "--file");
        var strandCode = Arg(args, "--strand-code");
        var type       = Arg(args, "--type") ?? "cover_image";
        var notes      = Arg(args, "--notes");
        var dryRun     = args.Contains("--dry-run");

        if (string.IsNullOrWhiteSpace(file))
        {
            Console.Error.WriteLine("ERROR: --file PATH is required.");
            return 1;
        }

        if (!File.Exists(file))
        {
            Console.Error.WriteLine($"ERROR: file not found: {file}");
            return 1;
        }

        var validTypes = new[] { "cover_image", "logo", "watermark", "banner", "thumbnail", "promotional" };
        if (!validTypes.Contains(type))
        {
            Console.Error.WriteLine($"ERROR: --type must be one of: {string.Join(", ", validTypes)}");
            return 1;
        }

        var ext         = Path.GetExtension(file).ToLowerInvariant();
        var contentType = ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png"            => "image/png",
            ".webp"           => "image/webp",
            _                 => "application/octet-stream",
        };

        var fileName = Path.GetFileName(file);
        var fileInfo = new FileInfo(file);

        Console.WriteLine($"File:    {file}");
        Console.WriteLine($"Size:    {fileInfo.Length:N0} bytes ({fileInfo.Length / 1024.0:F1} KB)");
        Console.WriteLine($"Type:    {type}");
        Console.WriteLine($"MIME:    {contentType}");
        if (strandCode is not null) Console.WriteLine($"Strand:  {strandCode}");
        if (dryRun) { Console.WriteLine("DRY-RUN: no DB write."); return 0; }

        var factory = sp.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();
        await using var db = await factory.CreateDbContextAsync();

        int? tenantId = null;

        if (!string.IsNullOrWhiteSpace(strandCode))
        {
            var strand = await db.Strands
                .Where(s => s.StrandCode == strandCode)
                .Select(s => new { s.Id, s.Title })
                .FirstOrDefaultAsync();

            if (strand is null)
            {
                Console.Error.WriteLine($"ERROR: no strand found with StrandCode = '{strandCode}'");
                return 1;
            }

            Console.WriteLine($"Strand:  {strand.Title} ({strand.Id})");
        }

        await using var scope = sp.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<IMediaStore>();
        await using var stream = File.OpenRead(file);
        var item = await store.UploadAsync(
            stream, fileName, contentType,
            tenantId: tenantId,
            mediaType: type,
            notes: notes);

        Console.WriteLine($"Saved:   {item.Uid}  ({item.FileName}, {item.SizeBytes:N0} bytes)");
        return 0;
    }

    static string? Arg(string[] args, string flag)
    {
        var idx = Array.IndexOf(args, flag);
        return idx >= 0 && idx + 1 < args.Length ? args[idx + 1] : null;
    }
}
