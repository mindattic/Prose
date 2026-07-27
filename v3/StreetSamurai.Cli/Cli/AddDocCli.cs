using Microsoft.Extensions.DependencyInjection;
using StreetSamurai.Core.Models.Canon;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Cli;

/// <summary>
/// CLI surface to insert a worldbuilding Document directly into canon (Entities +
/// Records). Body comes from a file so multi-line essays survive the shell.
///
///   ss --add-doc --title "..." --body-file path.md [--category essay] [--tags "a,b,c"] [--filename slug.md]
///   ss --add-doc --dir path/to/folder
///     One .md file per document, simple frontmatter (all optional except title):
///       ---
///       title: Tel Dan Stele (1993)
///       category: source
///       tags: archaeology, tel-dan, house-of-david
///       ---
///       (body — everything after the closing --- line; a "Source:"/"Accessed:" line
///       naming the URL and access date belongs here too, since WorldbuildingDocument
///       has no dedicated Url/Author/Date columns — it's kept as durable body text
///       rather than lost the moment the source page disappears.)
/// </summary>
public static class AddDocCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider sp)
    {
        var dir = ArgValue(args, "--dir");
        if (!string.IsNullOrWhiteSpace(dir))
            return await RunDirAsync(dir, sp);

        var title    = ArgValue(args, "--title");
        var bodyFile = ArgValue(args, "--body-file");
        var category = ArgValue(args, "--category") ?? "essay";
        var tagsCsv  = ArgValue(args, "--tags") ?? "";
        var fileName = ArgValue(args, "--filename");

        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(bodyFile))
        {
            Console.Error.WriteLine("usage: ss --add-doc --title \"…\" --body-file path.md [--category essay] [--tags \"a,b,c\"] [--filename slug.md]");
            return 1;
        }
        if (!File.Exists(bodyFile))
        {
            Console.Error.WriteLine($"body file not found: {bodyFile}");
            return 1;
        }

        var body = await File.ReadAllTextAsync(bodyFile);
        var lineCount = body.Split('\n').Length;
        var tags = tagsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        if (string.IsNullOrWhiteSpace(fileName))
            fileName = Slugify(title!) + ".md";

        var headings = body.Split('\n')
            .Select(l => l.TrimEnd('\r'))
            .Where(l => l.StartsWith("#"))
            .Select(l => l.TrimStart('#').Trim())
            .ToList();

        var doc = new WorldbuildingDocument
        {
            Title     = title!,
            FileName  = fileName,
            Category  = category,
            Body      = body,
            LineCount = lineCount,
            Headings  = headings,
            Tags      = tags,
        };

        var repo = sp.GetRequiredService<WorldbuildingDocRepository>();
        repo.Save(doc);

        Console.WriteLine($"[add-doc] saved id={doc.Id} title=\"{doc.Title}\" file={doc.FileName} category={doc.Category} lines={doc.LineCount} tags=[{string.Join(", ", tags)}]");
        return 0;
    }

    private static async Task<int> RunDirAsync(string dir, IServiceProvider sp)
    {
        if (!Directory.Exists(dir))
        {
            Console.Error.WriteLine($"[add-doc] directory not found: {dir}");
            return 1;
        }

        var repo = sp.GetRequiredService<WorldbuildingDocRepository>();
        int ok = 0, failed = 0;
        foreach (var file in Directory.EnumerateFiles(dir, "*.md").OrderBy(f => f))
        {
            try
            {
                var raw = await File.ReadAllTextAsync(file);
                var (fm, body) = ParseFrontmatter(raw);

                if (!fm.TryGetValue("title", out var title) || string.IsNullOrWhiteSpace(title))
                {
                    Console.Error.WriteLine($"  FAIL  {Path.GetFileName(file)} — missing 'title' in frontmatter");
                    failed++;
                    continue;
                }
                var category = fm.GetValueOrDefault("category", "essay");
                var tags = (fm.GetValueOrDefault("tags", "") ?? "")
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
                var lineCount = body.Split('\n').Length;
                var headings = body.Split('\n')
                    .Select(l => l.TrimEnd('\r'))
                    .Where(l => l.StartsWith("#"))
                    .Select(l => l.TrimStart('#').Trim())
                    .ToList();

                var doc = new WorldbuildingDocument
                {
                    Title     = title,
                    FileName  = Path.GetFileName(file),
                    Category  = category!,
                    Body      = body,
                    LineCount = lineCount,
                    Headings  = headings,
                    Tags      = tags,
                };
                repo.Save(doc);
                Console.WriteLine($"  ok    {Path.GetFileName(file)} — id={doc.Id} title=\"{doc.Title}\"");
                ok++;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"  FAIL  {Path.GetFileName(file)} — {ex.Message}");
                failed++;
            }
        }
        Console.WriteLine($"[add-doc] {ok} saved, {failed} failed");
        return failed > 0 ? 1 : 0;
    }

    private static (Dictionary<string, string> fm, string body) ParseFrontmatter(string content)
    {
        var text = content.Replace("\r\n", "\n");
        var fm = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!text.StartsWith("---\n")) return (fm, text.Trim());

        var end = text.IndexOf("\n---", 4, StringComparison.Ordinal);
        if (end < 0) return (fm, text.Trim());

        var header = text[4..end];
        foreach (var line in header.Split('\n'))
        {
            var idx = line.IndexOf(':');
            if (idx <= 0) continue;
            fm[line[..idx].Trim()] = line[(idx + 1)..].Trim();
        }

        var afterIdx = end + 4;
        var body = afterIdx < text.Length ? text[afterIdx..].TrimStart('\n') : "";
        return (fm, body.Trim());
    }

    private static string? ArgValue(string[] args, string flag)
    {
        var i = Array.IndexOf(args, flag);
        return (i >= 0 && i + 1 < args.Length) ? args[i + 1] : null;
    }

    private static string Slugify(string s)
    {
        var lower = s.ToLowerInvariant();
        var sb = new System.Text.StringBuilder();
        foreach (var ch in lower)
        {
            if (char.IsLetterOrDigit(ch)) sb.Append(ch);
            else if (ch == ' ' || ch == '-' || ch == '_') sb.Append('-');
        }
        var raw = sb.ToString();
        while (raw.Contains("--")) raw = raw.Replace("--", "-");
        return raw.Trim('-');
    }
}
