using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Blazor.Cli;

/// <summary>
/// <c>ss --import-md --file path.md [--dry-run]</c> — reimport an edited
/// <c>ss --publish-md</c> Markdown file back into the database, updating each
/// beat's prose in-place.
///
/// The file must contain <c>&lt;!-- beat:N:id7 --&gt;</c> markers (written by
/// <c>--publish-md</c>). Each marker identifies a beat by the first 7 hex chars
/// of its GUID. All text between a marker and the next marker (or EOF) — minus
/// any Markdown heading lines — becomes the new <c>Beat.Text</c>. Chapter
/// structure, metadata, and beat order are not changed; only prose is updated.
///
/// Args:
///   --file PATH    Required. Path to the .md file (or "-" for stdin).
///   --dry-run      Parse and show what would change without writing.
///
/// Exit codes: 0 = success, 1 = error, 2 = bad args.
/// </summary>
public static class ImportMarkdownCli
{
    // Matches <!-- beat:1:019ea02 --> (case-insensitive on hex)
    private static readonly Regex BeatMarker =
        new(@"^<!--\s*beat:(\d+):([0-9a-f]{7})\s*-->$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Matches markdown heading lines: ## Title, # Title, etc.
    private static readonly Regex HeadingLine =
        new(@"^#+\s", RegexOptions.Compiled);

    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? file = null;
        bool dryRun = false;
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--file":    if (i + 1 < args.Length) file = args[++i]; break;
                case "--dry-run": dryRun = true; break;
            }
        }

        if (string.IsNullOrWhiteSpace(file))
        {
            Console.Error.WriteLine("[import-md] --file is required (or '-' for stdin).");
            Console.Error.WriteLine("Usage: ss --import-md --file path.md [--dry-run]");
            return 2;
        }

        string raw;
        if (file == "-")
            raw = await Console.In.ReadToEndAsync();
        else if (!System.IO.File.Exists(file))
        {
            Console.Error.WriteLine($"[import-md] File not found: {file}");
            return 1;
        }
        else
            raw = await System.IO.File.ReadAllTextAsync(file);

        var beats = ParseBeats(raw);
        if (beats.Count == 0)
        {
            Console.Error.WriteLine("[import-md] No <!-- beat:N:id7 --> markers found. Is this a --publish-md file?");
            return 1;
        }

        Console.WriteLine($"[import-md] Found {beats.Count} beat(s) in file{(dryRun ? " (dry-run)" : "")}.");

        var workbench = services.GetRequiredService<StrandWorkbenchService>();
        var dbFactory = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();

        int updated = 0, skipped = 0, notFound = 0;
        foreach (var (beatNo, id7, newText) in beats)
        {
            Guid? beatId = null;
            await using (var db = await dbFactory.CreateDbContextAsync())
            {
                // Prefix match: first 7 hex chars are identical in dashed and N GUID formats.
                var candidates = await db.Beats
                    .Where(b => b.Id.ToString().StartsWith(id7))
                    .Take(2)
                    .Select(b => new { b.Id, b.Text })
                    .ToListAsync();

                if (candidates.Count == 0)
                {
                    Console.WriteLine($"  Beat #{beatNo} ({id7}): NOT FOUND — skipping.");
                    notFound++;
                    continue;
                }
                if (candidates.Count > 1)
                {
                    Console.WriteLine($"  Beat #{beatNo} ({id7}): ambiguous prefix (matched {candidates.Count}) — skipping.");
                    notFound++;
                    continue;
                }

                var current = candidates[0];
                beatId = current.Id;

                if (current.Text?.Trim() == newText)
                {
                    Console.WriteLine($"  Beat #{beatNo} ({id7}): unchanged.");
                    skipped++;
                    continue;
                }
            }

            if (dryRun)
            {
                Console.WriteLine($"  Beat #{beatNo} ({id7}): would update ({newText.Length} chars).");
                updated++;
                continue;
            }

            await workbench.UpdateBeatTextAsync(beatId!.Value, newText, expectedUpdatedAt: null);
            Console.WriteLine($"  Beat #{beatNo} ({id7}): updated ({newText.Length} chars).");
            updated++;
        }

        Console.WriteLine($"[import-md] Done. Updated={updated} Unchanged={skipped} NotFound={notFound}");
        return notFound > 0 && updated == 0 ? 1 : 0;
    }

    /// <summary>
    /// Parse <c>&lt;!-- beat:N:id7 --&gt;</c> markers from the file content.
    /// Text between consecutive markers (stripped of heading lines) becomes each beat's prose.
    /// </summary>
    private static List<(int BeatNo, string Id7, string Text)> ParseBeats(string content)
    {
        var result = new List<(int, string, string)>();

        int currentNo = 0;
        string? currentId7 = null;
        var currentLines = new List<string>();

        foreach (var line in content.Replace("\r\n", "\n").Split('\n'))
        {
            var m = BeatMarker.Match(line.Trim());
            if (m.Success)
            {
                if (currentId7 != null)
                    result.Add((currentNo, currentId7, BuildBeatText(currentLines)));

                currentNo = int.Parse(m.Groups[1].Value);
                currentId7 = m.Groups[2].Value.ToLower();
                currentLines.Clear();
            }
            else if (currentId7 != null)
            {
                currentLines.Add(line);
            }
        }

        if (currentId7 != null)
            result.Add((currentNo, currentId7, BuildBeatText(currentLines)));

        return result;
    }

    private static string BuildBeatText(List<string> lines)
    {
        var sb = new StringBuilder();
        foreach (var line in lines)
        {
            // Drop heading lines that appear between beats (chapter markers from export).
            if (HeadingLine.IsMatch(line)) continue;
            sb.AppendLine(line);
        }
        return sb.ToString().Trim();
    }
}
