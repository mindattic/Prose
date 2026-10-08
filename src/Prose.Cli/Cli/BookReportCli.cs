using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// prose --book-report --slug &lt;slug-or-code&gt; [--out &lt;dir&gt;] [--code &lt;CODE&gt;] [--complete]
///
/// Deterministic report pull (see <see cref="BookReportService"/>'s doc comment) — findings by
/// status/category, logic-sweep convergence, and any Decision Ledger entries mentioning this book,
/// rendered to one markdown file. Writes to Downloads\GLMZ_Book_Reports\{CODE}[_complete].md by
/// default, matching the existing drafts/{CODE}.md -&gt; make_pdfs.py -&gt; {CODE}.pdf pipeline (the
/// author runs make_pdfs.py by hand afterward; this command does not invoke Python).
///
/// --code overrides the filename stem (defaults to the resolved node's own NodeCode, falling back
/// to its Slug when the book has none on file yet). --complete appends _complete — pass it once
/// this book's backlog has actually been read through and corrected, same convention as this
/// session's BLST_complete.md/.pdf; omitting it writes the plain {CODE}.md draft name.
/// </summary>
public static class BookReportCli
{
    private const string DefaultOutDir = @"C:\Users\ryand\Downloads\GLMZ_Book_Reports";

    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var slug = ArgValue(args, "--slug");
        if (slug is null) { PrintUsage(); return 1; }

        var outDir   = ArgValue(args, "--out") ?? DefaultOutDir;
        var codeArg  = ArgValue(args, "--code");
        var complete = args.Contains("--complete");

        var reportService = services.GetRequiredService<BookReportService>();
        var result = await reportService.GenerateAsync(slug);
        if (result is null) { Console.Error.WriteLine($"[book-report] node not found: {slug}"); return 1; }

        var code = codeArg ?? result.NodeCode ?? result.NodeSlug;
        var draftsDir = Path.Combine(outDir, "drafts");
        Directory.CreateDirectory(draftsDir);

        var stem = complete ? $"{code}_complete" : code;
        var mdPath = Path.Combine(draftsDir, $"{stem}.md");
        await File.WriteAllTextAsync(mdPath, result.Markdown);

        Console.WriteLine($"[book-report] {result.Title} ({code}) -> {mdPath}");
        Console.WriteLine("[book-report] run make_pdfs.py in that folder to refresh the PDF.");
        return 0;
    }

    private static string? ArgValue(string[] args, string flag)
    {
        var i = Array.IndexOf(args, flag);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Usage: prose --book-report --slug <slug-or-code> [--out <dir>] [--code <CODE>] [--complete]");
    }
}
