using Microsoft.Extensions.DependencyInjection;
using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Models;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Cli;

/// <summary>
/// Idempotent stub-creator for the "Vultures on the Doorstep" book — the
/// future story seeded in memory <c>project_vultures</c>. Creates a Book
/// row with placeholder fields and a Draft <see cref="BookOutline"/> with
/// chapter slots, but writes no prose. Run when the user is ready to
/// start the book; on subsequent runs it's a no-op (matched by title).
///
/// Usage:
///   ss --seed-vultures
/// </summary>
public static class VulturesSeedCli
{
    private const string Title = "Vultures on the Doorstep";

    public static int Run(IReadOnlyList<string> args, IServiceProvider sp)
    {
        var books = sp.GetRequiredService<IBookRepository>();
        var outlineSvc = sp.GetRequiredService<BookOutlineService>();

        var existing = books.ListBooks().FirstOrDefault(b =>
            string.Equals(b.Title, Title, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            Console.WriteLine($"Book already exists (id={existing.Id}, title=\"{existing.Title}\") — no action.");
            return 0;
        }

        var book = new Book
        {
            Title = Title,
            Tagline = "",                              // user fills
            Premise =
                "[STUB — refine before publishing] " +
                "The Vultures — body-pickup-after-shootouts service, " +
                "organ-repossession technicians on the side — show up " +
                "somewhere they weren't called. Seeds in memory project_vultures.md " +
                "and in Street Meat where Hua delivers Kyle's \"corpse\".",
            ArcTarget = "",                            // user fills
            Protagonists = new List<string>(),         // user fills
            Status = "draft",
        };
        books.SaveBook(book);
        Console.WriteLine($"Created book stub: id={book.Id}, title=\"{book.Title}\"");

        // Outline starts in Draft per the OutlineStatus contract — chapter
        // generation gate (BookOutlineService.EnsureApprovedForGeneration)
        // will refuse to run against this book until the user approves it.
        var outline = outlineSvc.Load(book.Id);
        outline.Premise = book.Premise;
        outline.Theme   = "[STUB] consequences of organ economy / who decides who's already dead";
        outline.Structure = "freeform";
        outline.Status = OutlineStatus.Draft;
        outlineSvc.Save(outline);
        Console.WriteLine($"Created Draft outline with status=Draft (gate is enforced).");

        Console.WriteLine();
        Console.WriteLine("Next steps:");
        Console.WriteLine($"  1. Open /books, pick \"{Title}\", refine premise/tagline/protagonists.");
        Console.WriteLine($"  2. Open /books/{{id}}/outline, add chapter outlines, mark Approved.");
        Console.WriteLine($"  3. Then chapter generation is unlocked.");
        return 0;
    }
}
