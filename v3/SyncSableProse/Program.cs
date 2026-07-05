using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Extensions;
using StreetSamurai.Core.Interfaces;

// ── SyncSableProse ─────────────────────────────────────────────────────────
// Updates "The Voice You Trust" chapters + book record so the prose matches
// the post-dehyphenation canon. Three characters in the book had hyphenated
// surnames before the Legion sweep; Legion's verdicts:
//
//   Mira Quintero-Bekele       → Mira Quintero
//   Joaquim Da Silva-Lerner    → Joaquim Da Silva
//   Beatrix Ngalula-Vance      → Beatrix Vance
//
// Esperanza Halberd-Iwu is intentionally left untouched: she is mentioned in
// the prose but was never promoted to a Character record, so the sweep didn't
// touch her, and renaming her unilaterally would manufacture canon we haven't
// committed to yet. Sable's own "Rhea MacGregor" alias is protected.

const string BookId = "5ab1e000000000000000000000000001";

// Ordered replacements — longer/more-specific forms first so the surname-only
// substitution at the end can't munge the full-name forms produced above it.
var renames = new (string Old, string New)[]
{
    ("Mira Quintero-Bekele",    "Mira Quintero"),
    ("M. Quintero-Bekele",      "M. Quintero"),
    ("Quintero-Bekele",         "Quintero"),

    ("Joaquim Da Silva-Lerner", "Joaquim Da Silva"),
    ("Da Silva-Lerner",         "Da Silva"),

    ("Beatrix Ngalula-Vance",   "Beatrix Vance"),
    ("Ngalula-Vance",           "Vance"),
};

string Apply(string input)
{
    if (string.IsNullOrEmpty(input)) return input;
    var s = input;
    foreach (var (oldText, newText) in renames)
    {
        s = s.Replace(oldText, newText, StringComparison.Ordinal);
    }
    return s;
}

bool ApplyList(IList<string> list)
{
    var changed = false;
    for (int i = 0; i < list.Count; i++)
    {
        var updated = Apply(list[i]);
        if (!ReferenceEquals(updated, list[i]) && updated != list[i])
        {
            list[i] = updated;
            changed = true;
        }
    }
    return changed;
}

var services = new ServiceCollection();
services.AddLogging(b =>
{
    b.SetMinimumLevel(LogLevel.Warning);
    b.AddSimpleConsole(o => { o.SingleLine = true; o.TimestampFormat = "HH:mm:ss "; });
});
services.AddStreetSamuraiServices();
using var sp = services.BuildServiceProvider();

var books    = sp.GetRequiredService<IBookRepository>();
var chapters = sp.GetRequiredService<IChapterRepository>();

Console.WriteLine("=== SyncSableProse ===");

var book = books.LoadBook(BookId);
if (book == null)
{
    Console.Error.WriteLine($"FATAL: book not found: {BookId}");
    return 2;
}

var bookChanged = false;
var newPremise = Apply(book.Premise);
if (newPremise != book.Premise) { book.Premise = newPremise; bookChanged = true; }
var newArc = Apply(book.ArcTarget);
if (newArc != book.ArcTarget) { book.ArcTarget = newArc; bookChanged = true; }

if (book.StateAtEnd != null)
{
    foreach (var key in book.StateAtEnd.CharacterStatus.Keys.ToList())
    {
        var v = book.StateAtEnd.CharacterStatus[key];
        var nv = Apply(v);
        if (nv != v) { book.StateAtEnd.CharacterStatus[key] = nv; bookChanged = true; }
    }
    if (ApplyList(book.StateAtEnd.OpenThreads))  bookChanged = true;
    if (ApplyList(book.StateAtEnd.CanonChanges)) bookChanged = true;
}

if (bookChanged)
{
    books.SaveBook(book);
    Console.WriteLine($"  book updated: {book.Title}");
}
else
{
    Console.WriteLine($"  book unchanged: {book.Title}");
}

foreach (var cid in book.ChapterIds)
{
    var c = chapters.LoadChapter(cid);
    if (c == null) continue;
    var newHtml     = Apply(c.Html);
    var newMd       = Apply(c.Markdown);
    var newSynopsis = Apply(c.Synopsis);
    var changed = newHtml != c.Html || newMd != c.Markdown || newSynopsis != c.Synopsis;
    if (!changed)
    {
        Console.WriteLine($"  ch{c.Number} unchanged: {c.Title}");
        continue;
    }
    c.Html     = newHtml;
    c.Markdown = newMd;
    c.Synopsis = newSynopsis;
    chapters.SaveChapter(c);
    Console.WriteLine($"  ch{c.Number} updated: {c.Title}");
}

Console.WriteLine();
Console.WriteLine("Done.");
return 0;
