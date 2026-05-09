using Microsoft.Extensions.DependencyInjection;
using StreetSamurai.Core.Models.Canon;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Blazor.Cli;

/// <summary>
/// CLI surface to insert a worldbuilding Document directly into canon (Entities +
/// Records). Body comes from a file so multi-line essays survive the shell.
///
///   ss --add-doc --title "..." --body-file path.md [--category essay] [--tags "a,b,c"] [--filename slug.md]
/// </summary>
public static class AddDocCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider sp)
    {
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
