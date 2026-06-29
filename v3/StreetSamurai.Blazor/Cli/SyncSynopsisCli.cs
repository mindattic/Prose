using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Services;
using System.Text;
using System.Text.RegularExpressions;

namespace StreetSamurai.Blazor.Cli;

/// <summary>
/// Two-way sync between <c>synopsis.txt</c> in each strand's publish folder and
/// <c>Strands.Synopsis</c> in the database.
///
///   ss --sync-synopsis                  # sync all non-draft strands
///   ss --sync-synopsis --slug &lt;slug&gt;    # sync one strand
///   ss --sync-synopsis --dry-run        # report what would change, no writes
///
/// Rules (file wins on conflict):
///   file exists, DB empty     → update DB from file
///   DB has value, file absent → write file from DB
///   both exist, differ        → file wins, update DB
///   both match or both empty  → no-op
/// </summary>
public static class SyncSynopsisCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider sp)
    {
        var dryRun = args.Contains("--dry-run");
        string? slug = null;
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == "--slug") { slug = args[i + 1]; break; }

        var settings = sp.GetRequiredService<SettingsService>();
        var dbFactory = sp.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();

        var baseDir = (settings.PublishExportDirectory ?? "").Trim().Trim('"', '\'').Trim();
        if (string.IsNullOrWhiteSpace(baseDir))
            baseDir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        if (dryRun) Console.WriteLine("[sync-synopsis] dry-run — no writes");
        Console.WriteLine($"[sync-synopsis] publish dir: {baseDir}");

        await using var db = await dbFactory.CreateDbContextAsync();

        var query = db.Strands.Where(s => !s.IsDraft);
        if (slug != null)
            query = query.Where(s => s.Slug == slug);

        var strands = await query.OrderBy(s => s.Title).ToListAsync();

        int fileToDb = 0, dbToFile = 0, conflicts = 0, noOp = 0, errors = 0;

        foreach (var strand in strands)
        {
            var dir = BuildFolderPath(baseDir, strand.Title, strand.ParentStrandId, db);
            var filePath = Path.Combine(dir, "synopsis.txt");

            var fileText = File.Exists(filePath)
                ? File.ReadAllText(filePath, new UTF8Encoding(false)).Trim()
                : null;
            var dbText = string.IsNullOrWhiteSpace(strand.Synopsis) ? null : strand.Synopsis!.Trim();

            if (fileText == null && dbText == null) { noOp++; continue; }
            if (fileText == dbText) { noOp++; continue; }

            if (fileText != null && dbText == null)
            {
                Console.WriteLine($"  [file→db]  {strand.Title}");
                if (!dryRun) { strand.Synopsis = fileText; strand.UpdatedAt = DateTime.UtcNow; }
                fileToDb++;
            }
            else if (fileText == null && dbText != null)
            {
                Console.WriteLine($"  [db→file]  {strand.Title}");
                if (!dryRun)
                {
                    Directory.CreateDirectory(dir);
                    File.WriteAllText(filePath, dbText, new UTF8Encoding(false));
                }
                dbToFile++;
            }
            else
            {
                // Both exist and differ — file wins.
                Console.WriteLine($"  [conflict→file wins] {strand.Title}");
                if (!dryRun) { strand.Synopsis = fileText!; strand.UpdatedAt = DateTime.UtcNow; }
                conflicts++;
            }
        }

        if (!dryRun)
        {
            try { await db.SaveChangesAsync(); }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[sync-synopsis] DB save failed: {ex.Message}");
                errors++;
            }
        }

        Console.WriteLine();
        Console.WriteLine($"[sync-synopsis] file→db={fileToDb}  db→file={dbToFile}  conflict(file wins)={conflicts}  no-op={noOp}  errors={errors}");
        return errors > 0 ? 1 : 0;
    }

    private static string BuildFolderPath(string baseDir, string strandTitle, Guid? parentId, StreetSamuraiDbContext db)
    {
        var ancestors = new List<string>();
        var pid = parentId;
        for (var guard = 0; pid is Guid p && guard < 8; guard++)
        {
            var parent = db.Strands.AsNoTracking()
                .Where(s => s.Id == p)
                .Select(s => new { s.Title, s.ParentStrandId })
                .FirstOrDefault();
            if (parent is null) break;
            ancestors.Insert(0, Sanitize(parent.Title));
            pid = parent.ParentStrandId;
        }
        var parts = new List<string> { baseDir };
        parts.AddRange(ancestors);
        parts.Add(Sanitize(strandTitle));
        return Path.Combine(parts.ToArray());
    }

    private static string Sanitize(string title)
    {
        var invalid = Path.GetInvalidFileNameChars().ToHashSet();
        invalid.Add('\''); invalid.Add('’');
        var kept = new string((title ?? "").Where(c => !invalid.Contains(c)).ToArray()).Trim();
        kept = Regex.Replace(kept, @"\s+", " ").Trim();
        return string.IsNullOrWhiteSpace(kept) ? "untitled" : kept;
    }
}
