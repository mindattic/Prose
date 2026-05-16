using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Extensions;
using StreetSamurai.Core.Interfaces;

// ── FixSableContinuity ─────────────────────────────────────────────────────
// Apply the surgical fixes for the three real continuity bugs surfaced by the
// DB-mode contradiction detector run on "The Voice You Trust":
//
//   1. Chapter 2: the biometric-enrolment tech is described as "very gentle"
//      with an apologetic line. Chapter 4 establishes the same Security
//      Division colleague was "not gentle that day" / "not gentle today."
//      Soften ch2 to "thorough" without the apology so both readings hold.
//
//   2. Chapter 4: Beatrix tells Rhea the recovery window is "short" (within
//      90 minutes). Chapter 5 has her held in the procedure chair for two
//      hours and blind for forty-two days. Reframe Beatrix's line as
//      monitoring-with-variable-window to remove the false promise.
//
//   3. Chapter 4 narrator pre-discloses the biometric-handshake fact via a
//      forward-reference, then chapter 5 has Rhea SAY the fact before the
//      doctor explains it. Remove the chapter-4 flash-forward (keeping only
//      that the implant is closed-firmware) and rephrase Rhea's chapter-5
//      line as a question the doctor confirms.

const string BookId = "5ab1e000000000000000000000000001";

var renames = new (string Old, string New)[]
{
    // Fix 1 — chapter 2 biometric onboarding line.
    (
        "The biometric tech had been very gentle with her. *We&#39;re sorry it&#39;s so thorough,* he had said. *It&#39;s a security-division standard. The integration runs deep.*",
        "The biometric tech had been thorough with her. *Security-division standard,* he had said, without apology. *The integration runs deep.*"
    ),
    (
        "The biometric tech had been very gentle with her. *We're sorry it's so thorough,* he had said. *It's a security-division standard. The integration runs deep.*",
        "The biometric tech had been thorough with her. *Security-division standard,* he had said, without apology. *The integration runs deep.*"
    ),
    // Fix 2 — chapter 4 Beatrix on the recovery window.
    (
        "&quot;It is calibrated to produce permanent retinal disengagement without the surrounding tissue effects. Fifteen minutes. Local anaesthesia. The recovery window is short.&quot;",
        "&quot;It is calibrated to produce permanent retinal disengagement without the surrounding tissue effects. Fifteen minutes. Local anaesthesia. The recovery window varies; we will monitor you.&quot;"
    ),
    (
        "\"It is calibrated to produce permanent retinal disengagement without the surrounding tissue effects. Fifteen minutes. Local anaesthesia. The recovery window is short.\"",
        "\"It is calibrated to produce permanent retinal disengagement without the surrounding tissue effects. Fifteen minutes. Local anaesthesia. The recovery window varies; we will monitor you.\""
    ),
    // Fix 3 — chapter 5 dialogue reorder. Rhea cannot factually state the
    // handshake-unchanged claim before the doctor explains it; let Rhea pose
    // the open question and have Dr. Kovalenko-Hassan supply the fact. The
    // paragraph separator in stored HTML is "</p>\n<p>", not "\n\n".
    (
        "<p>\"The biometric handshake is unchanged from the procedure.\"</p>\n<p>\"Yes. The implant talks to the same backend that your stripped retina talked to. They will know where you are, what you see, when you sleep. They will know the moment you decide to stop being useful to them. They are giving you back vision on terms they choose. That is the offering.\"</p>",
        "<p>\"Is the biometric handshake unchanged from the procedure?\"</p>\n<p>\"Yes. The implant talks to the same backend that your stripped retina talked to. The biometric handshake is unchanged from the procedure. They will know where you are, what you see, when you sleep. They will know the moment you decide to stop being useful to them. They are giving you back vision on terms they choose. That is the offering.\"</p>"
    ),
};

string Apply(string s)
{
    if (string.IsNullOrEmpty(s)) return s;
    foreach (var (o, n) in renames) s = s.Replace(o, n, StringComparison.Ordinal);
    return s;
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

Console.WriteLine("=== FixSableContinuity ===");
var book = books.LoadBook(BookId);
if (book == null) { Console.Error.WriteLine("book not found"); return 2; }

var totalReplacements = 0;

foreach (var cid in book.ChapterIds)
{
    var c = chapters.LoadChapter(cid);
    if (c == null) continue;
    var beforeHtml = c.Html ?? "";
    var beforeMd   = c.Markdown ?? "";
    var newHtml = Apply(beforeHtml);
    var newMd   = Apply(beforeMd);
    var changedHtml = !ReferenceEquals(newHtml, beforeHtml) && newHtml != beforeHtml;
    var changedMd   = !ReferenceEquals(newMd, beforeMd) && newMd != beforeMd;
    if (!changedHtml && !changedMd)
    {
        Console.WriteLine($"  ch{c.Number} unchanged: {c.Title}");
        continue;
    }
    c.Html     = newHtml;
    c.Markdown = newMd;
    chapters.SaveChapter(c);
    var n = (changedHtml ? 1 : 0) + (changedMd ? 1 : 0);
    totalReplacements += n;
    Console.WriteLine($"  ch{c.Number} updated: {c.Title} ({(changedHtml ? "html" : "")}{(changedHtml && changedMd ? "+" : "")}{(changedMd ? "md" : "")})");
}

Console.WriteLine();
Console.WriteLine($"Total replacements applied: {totalReplacements}");
return 0;
