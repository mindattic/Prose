using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// prose --chapters --slug &lt;slug-or-code-or-id&gt; [--json]
///
/// Lists a book's chapter units in true reading order — index, title, beat count, prose size.
/// The 100 ft rung of the Three Altitudes (CLAUDE.md), made executable from the CLI for the
/// first time.
///
/// <para><b>Why this exists (Story Ledger Phase 1).</b> Every read path was flat:
/// <c>read_beats</c>, <c>print_book</c> and <c>get_book</c> all returned an undifferentiated
/// beat list, and <c>get_chapter</c> serves only the legacy pre-Nodes shelf. Faced with a
/// 1.9M-char book and no chapter-scoped path, a reading agent fell back to the one-line
/// <c>Beat.Description</c> spine and reported fabricated detail as established fact. A book you
/// cannot address chapter by chapter is a book that gets read by summary.</para>
///
/// <para><b>Segmentation is deliberately borrowed, not reinvented.</b> This calls
/// <see cref="SynopsisExportService.GetChapterSourcesAsync"/> — the same
/// <c>LoadChapterUnitsAsync</c> walk that builds <c>story-synopsis.txt</c> and feeds
/// <c>ComprehensionProbeService</c>. So this listing's count and order match
/// <c>story-synopsis.txt</c> by construction rather than by luck, and a book with a nested
/// sub-chapter collection segments identically in all three. No LLM calls: chapter sources are
/// pure DB reads.</para>
///
/// <para>Note that <c>read_beats groupByChapter:true</c> groups by
/// <c>GetOrderedBeatsAsync</c>'s source nodes instead, which is the reader's assembled
/// manuscript order and therefore excludes <c>Kind=="book"</c> drafts buckets. The two can
/// legitimately differ for a book that has a drafts bucket; each is correct for its own
/// question, so do not "unify" them without deciding which question you are answering.</para>
/// </summary>
public static class ChaptersCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        var synopsis  = services.GetRequiredService<SynopsisExportService>();

        var slug = Flag(args, "--slug") ?? Flag(args, "--code") ?? Flag(args, "--id");
        if (string.IsNullOrWhiteSpace(slug))
        {
            Console.Error.WriteLine("Usage: prose --chapters --slug <slug-or-code-or-id> [--json]");
            return 2;
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        var nodeId = await NodeRefResolver.ResolveAsync(db, slug);
        if (nodeId == null)
        {
            Console.Error.WriteLine($"[chapters] No node matched '{slug}'.");
            return 2;
        }

        // IgnoreQueryFilters(): nodeId is already resolved explicitly (NodeRefResolver), so the
        // ambient universe scope can only suppress the right answer here.
        var book = await db.Nodes.IgnoreQueryFilters().AsNoTracking()
            .Where(n => n.Id == nodeId.Value)
            .Select(n => new { n.Id, n.Slug, n.NodeCode, n.Title })
            .FirstAsync();

        var units = await synopsis.GetChapterSourcesAsync(book.Id);

        if (args.Contains("--json"))
        {
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new
            {
                node_id = book.Id,
                slug = book.Slug,
                code = book.NodeCode,
                title = book.Title,
                chapter_count = units.Count,
                total_beats = units.Sum(u => u.BeatCount),
                chapters = units.Select(u => new
                {
                    index = u.Index + 1,
                    chapter_node_id = u.NodeId,
                    title = u.Title,
                    beat_count = u.BeatCount,
                    prose_chars = u.SourceText.Length,
                }),
            }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            return 0;
        }

        Console.WriteLine($"{book.Title}  [{book.NodeCode ?? book.Slug}]");
        Console.WriteLine($"{units.Count} chapter unit(s), {units.Sum(u => u.BeatCount)} beat(s), " +
                          $"{units.Sum(u => (long)u.SourceText.Length):N0} chars of prose");
        Console.WriteLine(new string('-', 78));
        if (units.Count == 0)
        {
            // Not an error: a structured-but-unwritten book has chapters and no prose. The
            // segmentation walk skips units with no text at all (see LoadChapterUnitsAsync), so
            // an empty result means "no prose yet", never "no chapters".
            Console.WriteLine("(no chapter unit has prose yet — chapters may exist but be unwritten)");
            return 0;
        }

        foreach (var u in units)
            Console.WriteLine($"{u.Index + 1,4}. {Truncate(u.Title, 52),-52} {u.BeatCount,5} beat(s) {u.SourceText.Length,9:N0} chars");

        return 0;
    }

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..(max - 1)] + "…";

    private static string? Flag(string[] args, string name)
    {
        var i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }
}
