using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// prose --validate-chapters (--slug &lt;book&gt; | --universe &lt;slug&gt; | --all) [--json]
///
/// Report-only, zero-LLM, zero-cost. Answers the questions nothing in the engine could answer
/// before <see cref="BookSpineService"/> and <see cref="ChapterTitle"/> existed: does this book's
/// chapter structure hold up, and do its titles match the house standard
/// (<c>Chapter N</c> / <c>Chapter N — Subtitle</c>)?
///
/// <para>Reports; never repairs. A chapter title is authored text and a numbering gap can be
/// deliberate, so every row here is something for the author to decide on (RFC 0009). The fixes,
/// when wanted, are existing operations: <c>WrapInSingleChapterAsync</c> for unfiled beats,
/// <c>SplitIntoCollectionAsync</c> for a flat book, a rename for a title.</para>
///
/// Exit codes: 0 clean, 1 findings, 2 usage error.
/// </summary>
public static class ValidateChaptersCli
{
    private sealed record Finding(string Book, string Code, string Severity, string Message);

    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var slug = Flag(args, "--slug");
        var universeSlug = Flag(args, "--universe");
        var all = args.Contains("--all");
        var jsonMode = args.Contains("--json");

        if (string.IsNullOrWhiteSpace(slug) && string.IsNullOrWhiteSpace(universeSlug) && !all)
        {
            Console.Error.WriteLine(
                "Usage: prose --validate-chapters (--slug <book> | --universe <slug> | --all) [--json]");
            return 2;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        var spines = services.GetRequiredService<BookSpineService>();
        await using var db = await dbFactory.CreateDbContextAsync();

        // IgnoreQueryFilters throughout: --all is deliberately corpus-wide across every universe,
        // and a book named by --slug must resolve whatever the ambient scope happens to be.
        var booksQuery = db.BookNodes.AsNoTracking().IgnoreQueryFilters();

        if (!string.IsNullOrWhiteSpace(slug))
        {
            booksQuery = booksQuery.Where(b => b.Slug == slug || b.NodeCode == slug);
        }
        else if (!string.IsNullOrWhiteSpace(universeSlug))
        {
            var canonDocs = services.GetRequiredService<CanonDocumentService>();
            var universeId = await canonDocs.ResolveUniverseIdAsync(universeSlug);
            if (universeId == null)
            {
                Console.Error.WriteLine($"[validate-chapters] Unknown universe '{universeSlug}'.");
                return 2;
            }
            booksQuery = booksQuery.Where(b => b.UniverseId == universeId.Value);
        }

        var books = await booksQuery
            .OrderBy(b => b.Title)
            .Select(b => new { b.Id, b.Slug, b.Title, b.NodeCode })
            .ToListAsync();

        if (books.Count == 0)
        {
            Console.Error.WriteLine("[validate-chapters] No book matched.");
            return 2;
        }

        var findings = new List<Finding>();
        var scanned = 0;

        foreach (var book in books)
        {
            var label = book.NodeCode ?? book.Slug;
            var spine = await spines.GetAsync(book.Id);
            scanned++;

            if (spine.BeatCount == 0)
            {
                findings.Add(new Finding(label, "no_beats", "warn", "Book has no beats."));
                continue;
            }

            // A chapter node that holds no beats anywhere beneath it is invisible to every
            // exporter — it simply never appears in the spine, so it can only be found by
            // comparing the node tree against the walk.
            var present = spine.Chapters.Select(c => c.NodeId).ToHashSet();
            var declared = await db.Nodes.AsNoTracking().IgnoreQueryFilters()
                .Where(n => n.ParentNodeId == book.Id && n.Kind != "book")
                .Select(n => new { n.Id, n.Title })
                .ToListAsync();
            foreach (var node in declared.Where(n => !present.Contains(n.Id)))
                findings.Add(new Finding(label, "empty_chapter", "warn",
                    $"Chapter node '{node.Title}' holds no beats — it prints in no export."));

            foreach (var chapter in spine.Chapters)
            {
                if (chapter.IsBookRoot)
                    findings.Add(new Finding(label, "unfiled_beats", "blocker",
                        $"{chapter.Beats.Count} beat(s) hang off the book node itself, not a chapter. " +
                        "Fix with WrapInSingleChapterAsync / --wrap-in-chapter."));

                if (chapter.OpenedByBeatMarker)
                    findings.Add(new Finding(label, "flat_book", "blocker",
                        $"Unit {chapter.Ordinal} ('{chapter.Heading}') is opened by a beat's IsChapterStart, " +
                        "not a node — this book is flat. Every export decides its chapter structure " +
                        "differently in that shape. Fix with SplitIntoCollectionAsync / --split-node."));

                if (chapter.Title.Trim().Length == 0 && !chapter.IsBookRoot)
                    findings.Add(new Finding(label, "blank_title", "warn",
                        $"Unit {chapter.Ordinal} has a blank node title; the export falls back to '{chapter.Heading}'."));
                else if (chapter.Parsed.Kind == ChapterTitleKind.Chapter && !chapter.Parsed.IsStandard)
                    findings.Add(new Finding(label, "nonstandard_title", "warn",
                        $"'{chapter.Title}' is not the house format " +
                        (chapter.Parsed.Number is null
                            ? "(no chapter number)."
                            : $"(separator is '{chapter.Parsed.FoundSeparator}', standard is '{ChapterTitle.Separator}') — " +
                              $"should be '{ChapterTitle.Format(chapter.Parsed.Number.Value, chapter.Parsed.Subtitle)}'.")));
                else if (chapter.Parsed.Kind == ChapterTitleKind.Other && chapter.Title.Trim().Length > 0)
                    findings.Add(new Finding(label, "untitled_unit", "warn",
                        $"Unit {chapter.Ordinal} is titled '{chapter.Title}' — not Chapter/Interlude/Prologue/Epilogue."));

                // A heading that has leaked into the prose of a chapter's FIRST beat is almost
                // always draft debris duplicating the node title. Mid-chapter it is far more often
                // a sentence that happens to start with the word, so only the first beat is flagged.
                var first = chapter.Beats.FirstOrDefault();
                if (first is { LooksLikeStrayHeading: true })
                    findings.Add(new Finding(label, "heading_in_prose", "warn",
                        $"Beat #{first.Number} (unit {chapter.Ordinal}) opens with a heading line: \"{first.Preview}\"."));
            }

            // Numbering is checked across the Chapter-kind units only, in order: an Interlude
            // between chapters is not a numbering gap, it is an interlude.
            var numbered = spine.Chapters
                .Where(c => c.Parsed.Kind == ChapterTitleKind.Chapter && c.Parsed.Number is not null)
                .ToList();

            var expected = 1;
            foreach (var chapter in numbered)
            {
                if (chapter.Parsed.Number!.Value != expected)
                {
                    findings.Add(new Finding(label, "number_gap", "warn",
                        $"'{chapter.Title}' sits at chapter position {expected} — the numbering skips or repeats here."));
                    expected = chapter.Parsed.Number.Value;
                }
                expected++;
            }

            foreach (var dup in numbered.GroupBy(c => c.Parsed.Number!.Value).Where(g => g.Count() > 1))
                findings.Add(new Finding(label, "duplicate_number", "blocker",
                    $"Chapter {dup.Key} is claimed by {dup.Count()} units: " +
                    string.Join(", ", dup.Select(c => $"'{c.Title}'"))));
        }

        if (jsonMode)
        {
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                books_scanned = scanned,
                finding_count = findings.Count,
                findings = findings.Select(f => new
                {
                    book = f.Book, code = f.Code, severity = f.Severity, message = f.Message,
                }),
            }, new JsonSerializerOptions { WriteIndented = true }));
            return findings.Count > 0 ? 1 : 0;
        }

        Console.WriteLine($"Chapter validation — {scanned} book(s) scanned.");
        Console.WriteLine();

        if (findings.Count == 0)
        {
            Console.WriteLine("✅ Every chapter is node-delimited and every title is the house format.");
            return 0;
        }

        foreach (var group in findings.GroupBy(f => f.Book))
        {
            Console.WriteLine(group.Key);
            foreach (var f in group.OrderBy(f => f.Severity == "blocker" ? 0 : 1))
                Console.WriteLine($"    [{(f.Severity == "blocker" ? "BLOCKER" : "warn   ")}] {f.Code}: {f.Message}");
            Console.WriteLine();
        }

        var blockers = findings.Count(f => f.Severity == "blocker");
        Console.WriteLine($"{findings.Count} finding(s), {blockers} blocker(s), across {findings.Select(f => f.Book).Distinct().Count()} book(s).");
        Console.WriteLine("Nothing was changed — every fix here is an author decision.");
        return 1;
    }

    static string? Flag(string[] args, string name)
    {
        var idx = Array.IndexOf(args, name);
        return idx >= 0 && idx + 1 < args.Length ? args[idx + 1] : null;
    }
}
