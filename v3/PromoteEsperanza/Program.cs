using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Extensions;
using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Models.Canon;
using StreetSamurai.Core.Services;

// ── PromoteEsperanza ───────────────────────────────────────────────────────
// One-shot follow-up to PromoteAndDehyphenate. Esperanza Halberd-Iwu is
// mentioned by name in Sable origin chapter 3 (the Open Eyes advocacy-group
// lawyer who received the anonymous OPTIC-7 tip) but was never promoted to a
// Character record, so the bulk dehyphenation sweep skipped her. This script:
//
//   1. Creates the Esperanza Halberd-Iwu Character record (with hyphenated
//      surname, matching the prose as it stands).
//   2. Asks Legion to pick between "Halberd" and "Iwu" on phonetic appeal.
//   3. Renames the record, preserving the original full name as an alias.
//   4. Updates "The Voice You Trust" chapter 3 to use the new canonical name
//      and book state-at-end's Open Threads.

const string LegionBin =
    @"D:\Projects\MindAttic\MindAttic.Legion\MindAttic.Legion.Cli\bin\Release\net10.0\legion.exe";
const string BookId = "5ab1e000000000000000000000000001";
const string OriginalName = "Esperanza Halberd-Iwu";

var services = new ServiceCollection();
services.AddLogging(b =>
{
    b.SetMinimumLevel(LogLevel.Warning);
    b.AddSimpleConsole(o => { o.SingleLine = true; o.TimestampFormat = "HH:mm:ss "; });
});
services.AddStreetSamuraiServices();
using var sp = services.BuildServiceProvider();

var characters = sp.GetRequiredService<CharacterRepository>();
var books      = sp.GetRequiredService<IBookRepository>();
var chapters   = sp.GetRequiredService<IChapterRepository>();

// Step 1 — upsert the Character record with the hyphenated name as it appears
// in chapter 3 of "The Voice You Trust."
Console.WriteLine("=== Step 1: Promote Esperanza Halberd-Iwu ===");
characters.Reload();
var esperanza = characters.GetByName(OriginalName) ?? new CharacterData { Name = OriginalName };
esperanza.Role        = "Lawyer at Open Eyes (advocacy group); disbarred by Axiom-affiliated bar association";
esperanza.Status      = "alive";
esperanza.Species     = "human";
esperanza.Gender      = "female";
esperanza.Pronouns    = "she/her";
esperanza.Location    = "The Glooms — Open Eyes operates from a converted bakery";
esperanza.Affiliation = "Open Eyes (advocacy group)";
esperanza.Description =
    "Disbarred by the Axiom-affiliated bar association nineteen years before the events of "
    + "'The Voice You Trust' for representing a Z3 plaintiff in a class action that ended in "
    + "a six-figure settlement and a permanent injunction Axiom quietly violated within eight "
    + "months. Now works out of a converted bakery in the Glooms under the Open Eyes banner. "
    + "Rhea MacGregor interviewed her once at Tessera for a piece that did not air "
    + "because Marcus took it off the schedule two days before broadcast. Received an "
    + "anonymous letter on plain paper, in a deliberately non-attributable hand, containing "
    + "the OPTIC-7 lot number and four medical-record case identifiers — the tip Sable dropped "
    + "into a Glooms mailbox on a Wednesday in 2217 and has never confirmed receipt of since.";
characters.Save(esperanza);
Console.WriteLine($"  upserted: {esperanza.Name}");

// Step 2 — Legion vote on the surname.
Console.WriteLine();
Console.WriteLine("=== Step 2: Legion vote on surname ===");
if (!File.Exists(LegionBin))
{
    Console.Error.WriteLine($"FATAL: Legion CLI not found at {LegionBin}");
    return 2;
}

var question =
    "For a character named 'Esperanza', which surname pairs more phonetically appealingly "
    + "with the given name? Consider syllable flow, stress placement, vowel-consonant balance, "
    + "and ease of pronunciation. Choose exactly one.";

var psi = new ProcessStartInfo
{
    FileName = LegionBin,
    UseShellExecute = false,
    RedirectStandardOutput = true,
    RedirectStandardError = true,
    CreateNoWindow = true,
};
psi.ArgumentList.Add("ask");
psi.ArgumentList.Add(question);
psi.ArgumentList.Add("--options");
psi.ArgumentList.Add("Halberd,Iwu");
psi.ArgumentList.Add("--tier");
psi.ArgumentList.Add("low");
psi.ArgumentList.Add("--no-auto-context");

using var proc = Process.Start(psi);
if (proc == null) { Console.Error.WriteLine("legion process failed to start"); return 2; }
var stdout = proc.StandardOutput.ReadToEnd();
var stderr = proc.StandardError.ReadToEnd();
proc.WaitForExit();
var pick = stdout.Trim();
Console.WriteLine($"  legion exit {proc.ExitCode}, pick: '{pick}'");
if (!string.IsNullOrWhiteSpace(stderr)) Console.WriteLine($"  stderr: {stderr.Trim()}");
if (pick != "Halberd" && pick != "Iwu")
{
    Console.Error.WriteLine("FATAL: legion did not return a clean pick");
    return 1;
}

var newName = $"Esperanza {pick}";

// Step 3 — apply the rename.
Console.WriteLine();
Console.WriteLine("=== Step 3: Apply rename ===");
characters.Reload();
esperanza = characters.GetByName(OriginalName)!;
if (!esperanza.Aliases.Contains(OriginalName)) esperanza.Aliases.Add(OriginalName);
esperanza.Name = newName;
characters.Save(esperanza);
Console.WriteLine($"  renamed: {OriginalName} -> {newName}");

// Step 4 — sync prose + book state-at-end.
Console.WriteLine();
Console.WriteLine("=== Step 4: Sync prose ===");
var book = books.LoadBook(BookId);
if (book == null)
{
    Console.Error.WriteLine($"WARN: book {BookId} not found — skipping prose sync");
    return 0;
}

var renames = new (string Old, string New)[]
{
    (OriginalName, newName),                   // "Esperanza Halberd-Iwu" -> "Esperanza Halberd|Iwu"
    ("Halberd-Iwu", pick),                     // bare surname references
};

string Apply(string s)
{
    if (string.IsNullOrEmpty(s)) return s;
    foreach (var (o, n) in renames) s = s.Replace(o, n, StringComparison.Ordinal);
    return s;
}

var bookChanged = false;
var newPremise = Apply(book.Premise);
if (newPremise != book.Premise) { book.Premise = newPremise; bookChanged = true; }
if (book.StateAtEnd != null)
{
    for (int i = 0; i < book.StateAtEnd.OpenThreads.Count; i++)
    {
        var u = Apply(book.StateAtEnd.OpenThreads[i]);
        if (u != book.StateAtEnd.OpenThreads[i])
        {
            book.StateAtEnd.OpenThreads[i] = u;
            bookChanged = true;
        }
    }
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
    var newHtml = Apply(c.Html);
    var newMd   = Apply(c.Markdown);
    var newSyn  = Apply(c.Synopsis);
    if (newHtml == c.Html && newMd == c.Markdown && newSyn == c.Synopsis)
    {
        Console.WriteLine($"  ch{c.Number} unchanged: {c.Title}");
        continue;
    }
    c.Html = newHtml;
    c.Markdown = newMd;
    c.Synopsis = newSyn;
    chapters.SaveChapter(c);
    Console.WriteLine($"  ch{c.Number} updated: {c.Title}");
}

Console.WriteLine();
Console.WriteLine("Done.");
return 0;
