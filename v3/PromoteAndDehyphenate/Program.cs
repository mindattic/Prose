using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Extensions;
using StreetSamurai.Core.Models.Canon;
using StreetSamurai.Core.Services;

// ── PromoteAndDehyphenate ──────────────────────────────────────────────────
// Two passes against the canonical Character store:
//
//   Pass 1 — promote candidate characters surfaced by the continuity extractor
//             on "The Voice You Trust": Marcus, Imani, Olu Ferrara, Mira
//             Quintero-Bekele, Joaquim Da Silva-Lerner, Beatrix Ngalula-Vance,
//             Hartfield. Add "Rhea Adesanya-MacGregor" as an alias on Sable.
//
//   Pass 2 — Legion-driven surname dehyphenation. Every character whose Name
//             contains a hyphenated surname AND is not in the protected list
//             (Kyle / Sable / Sasha / Pixel) gets a Legion `ask` vote against
//             the two halves of the hyphen, with the first name as context.
//             Whichever surname Legion picks becomes the new Name. Slug
//             recomputes automatically via CharacterRepository.Save.

const string LegionBin =
    @"D:\Projects\MindAttic\MindAttic.Legion\MindAttic.Legion.Cli\bin\Release\net10.0\legion.exe";

// First names of characters whose surnames must not be touched. The exemption
// is per the user's directive: Kyle Ellen Corbin-Vister, Sable, Sasha Võ, Pixel.
var protectedFirstNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{
    "Kyle", "Sable", "Sasha", "Pixel",
};

// CLI flags: --dry-run lists every candidate rename without invoking Legion or
// touching canon. --skip-promote skips Pass 1 (useful when re-running just the
// dehyphenation sweep). --max-renames N caps the live pass after N renames.
var dryRun = args.Contains("--dry-run");
var skipPromote = args.Contains("--skip-promote");
var maxRenames = int.MaxValue;
for (int i = 0; i < args.Length - 1; i++)
{
    if (args[i] == "--max-renames" && int.TryParse(args[i + 1], out var n)) maxRenames = n;
}

var services = new ServiceCollection();
services.AddLogging(b =>
{
    b.SetMinimumLevel(LogLevel.Warning);
    b.AddSimpleConsole(o =>
    {
        o.SingleLine = true;
        o.TimestampFormat = "HH:mm:ss ";
    });
});
services.AddStreetSamuraiServices();
using var sp = services.BuildServiceProvider();
var characters = sp.GetRequiredService<CharacterRepository>();

// ── Pass 1: promote candidate characters ──────────────────────────────────
if (skipPromote)
{
    Console.WriteLine("=== Pass 1: SKIPPED (--skip-promote) ===");
}
else
{
    Console.WriteLine("=== Pass 1: Promote candidate characters ===");
}

void Upsert(string name, Action<CharacterData> configure)
{
    if (skipPromote) return;
    characters.Reload();
    var existing = characters.GetByName(name);
    var c = existing ?? new CharacterData { Name = name };
    configure(c);
    if (dryRun)
    {
        Console.WriteLine($"  [dry-run] would {(existing == null ? "create" : "update")}: {c.Name}");
        return;
    }
    characters.Save(c);
    Console.WriteLine($"  {(existing == null ? "created" : "updated")}: {c.Name}");
}

Upsert("Marcus", c =>
{
    c.Role = "Tessera Media Group studio director";
    c.Status = "alive";
    c.Species = "human";
    c.Gender = "male";
    c.Pronouns = "he/him";
    c.Location = "Meridian Core — relocated post-acquisition to Axiom Industries Communications and Brand";
    c.Affiliation = "Axiom Industries Communications and Brand (formerly Tessera Media Group)";
    c.Description = "The director who hired Rhea Adesanya-MacGregor at Tessera Media Group. Believes — or has rehearsed believing — that saying 'you have the voice they trust' is part of what makes it true. Brings bad coffee whenever he is about to deliver news the listener should not yet be told. Did not call her once in the fourteen months between the acquisition and her badge declining at the Meridian Core entrance.";
    if (!c.Aliases.Contains("the director")) c.Aliases.Add("the director");
});

Upsert("Imani", c =>
{
    c.Role = "Tessera Media Group floor manager";
    c.Status = "alive";
    c.Species = "human";
    c.Gender = "female";
    c.Pronouns = "she/her";
    c.Location = "Meridian Core, State and Madison (Tessera building, pre-conversion)";
    c.Affiliation = "Tessera Media Group";
    c.Description = "Tessera floor manager on the day of the Axiom acquisition. Two daughters, both in the West Lakeshore school district. Did not meet Rhea's eyes the morning of the announcement because she had been told what the eighth-floor meeting was about and she is not good at small lies. Bad at small lies is, in her line of work, a moral asset and a career ceiling.";
});

Upsert("Olu Ferrara", c =>
{
    c.Role = "Independent journalist (ex-Tessera Media Group)";
    c.Status = "alive";
    c.Species = "human";
    c.Gender = "male";
    c.Pronouns = "he/him";
    c.Location = "The Glooms — operates from one of the two independent outlets still filing coverage on CorpoNation expansion";
    c.Affiliation = "Independent media (post-Tessera resignation, with cause, citing editorial independence)";
    c.Description = "Was a colleague of Rhea Adesanya-MacGregor's at Tessera Media Group before the Axiom Industries acquisition. Walked into the director general's office on the morning of the announcement and resigned in writing, with cause, citing editorial independence. One of four people Rhea envied for a single afternoon. Sable has never made direct contact with him in the years since the restructuring.";
});

Upsert("Mira Quintero-Bekele", c =>
{
    c.Role = "Retired transit dispatcher; community organizer";
    c.Status = "dead";
    c.Species = "human";
    c.Gender = "female";
    c.Pronouns = "she/her";
    c.Age = 52;
    c.Location = "Pilsen (deceased 11 March 2217)";
    c.Affiliation = "Pilsen community assembly (convener); unaffiliated";
    c.Description = "Retired transit dispatcher who convened a Sunday-evening community assembly in Pilsen on 11 March 2217. Was the priority weighting in Axiom Industries Security Division's controlled deployment of OPTIC-7 — dosed at four-point-eight times the median room concentration at her distance from the improvised podium. One of the four attendees who did not recover. Official record: 'stress-related amnestic episode.' Actual cause: targeted ophthalmic disruptor, manufactured for a single client.";
});

Upsert("Joaquim Da Silva-Lerner", c =>
{
    c.Role = "Axiom Industries Security Division analyst";
    c.Status = "alive";
    c.Species = "human";
    c.Gender = "male";
    c.Pronouns = "he/him";
    c.Location = "Meridian Core, Floor 47 — Axiom Industries Security Division, Communications Integration";
    c.Affiliation = "Axiom Industries Security Division";
    c.Description = "A year younger than Rhea Adesanya-MacGregor and two tiers junior at Axiom. Careless with his clipboard; leaves his workstation unlocked when he steps away to the seventh-floor break room. The OPTIC-7 case study Sable read for nineteen minutes was open on his terminal. Internal Affairs has him on logs as the credentialed user; whether he knows what was read off him has never been disclosed.";
});

Upsert("Beatrix Ngalula-Vance", c =>
{
    c.Role = "Axiom Industries Internal Affairs, Communications Integration";
    c.Status = "alive";
    c.Species = "human";
    c.Gender = "female";
    c.Pronouns = "she/her";
    c.Location = "Meridian Core, Floor 47";
    c.Affiliation = "Axiom Industries Security Division — Internal Affairs";
    c.Description = "Conducted Rhea Adesanya-MacGregor's voluntary separation conversation in the eighth-floor conference room. Folds her hands the way Marcus folded his on the talkback at the moment a segment was done. Did not flinch when Rhea named the agent. Stated the package terms cleanly. Raised a hand a half-inch off the table to silence the Security Division colleague when he started to escalate. The kind of operator who has never had to do this any other way and intends to keep it that way.";
});

Upsert("Hartfield", c =>
{
    c.Role = "Meridian Core security guard";
    c.Status = "alive";
    c.Species = "human";
    c.Gender = "male";
    c.Pronouns = "he/him";
    c.Location = "Meridian Core lobby (Axiom Industries Security Division building)";
    c.Affiliation = "Arcturus Civil Security (Axiom Industries contract)";
    c.Description = "Young guard whose nameplate Rhea Adesanya-MacGregor read on the Monday morning her badge first declined. Keyed the override on the third attempt and apologized for the system 'running slow.' His grandmother lived in the Glooms — a detail Sable would learn nine years later in an unrelated context.";
});

// Sable: add Rhea Adesanya-MacGregor as an alias (Sable herself is protected
// from the dehyphenation pass, so the alias keeps its hyphen).
if (!skipPromote)
{
    characters.Reload();
    var sable = characters.GetByName("Sable");
    if (sable != null)
    {
        var rheaAlias = "Rhea Adesanya-MacGregor";
        if (!sable.Aliases.Contains(rheaAlias))
        {
            if (dryRun)
            {
                Console.WriteLine($"  [dry-run] would add alias on Sable: {rheaAlias}");
            }
            else
            {
                sable.Aliases.Add(rheaAlias);
                characters.Save(sable);
                Console.WriteLine($"  alias added on Sable: {rheaAlias}");
            }
        }
        else
        {
            Console.WriteLine($"  Sable already carries alias: {rheaAlias}");
        }
    }
    else
    {
        Console.WriteLine("  WARN: Sable not found — skipping alias add");
    }
}

// ── Pass 2: Legion-driven dehyphenation ────────────────────────────────────
Console.WriteLine();
Console.WriteLine($"=== Pass 2: Legion-driven surname dehyphenation {(dryRun ? "(DRY-RUN)" : "")} ===");
Console.WriteLine($"Legion CLI: {LegionBin}");
if (!dryRun && !File.Exists(LegionBin))
{
    Console.Error.WriteLine($"FATAL: Legion CLI not found at {LegionBin}");
    return 2;
}

// A character's "surname" is the last whitespace-separated token of the Name.
// If that token contains a hyphen, it is a hyphenated surname and a candidate
// for the dehyphenation pass. First-name hyphens (Jean-Paul, Mary-Anne) are
// left alone because the surname is downstream of the last space.
static (string FirstNames, string Surname)? SplitName(string name)
{
    if (string.IsNullOrWhiteSpace(name)) return null;
    var trimmed = name.Trim();
    var lastSpace = trimmed.LastIndexOf(' ');
    if (lastSpace < 0) return null;
    var first = trimmed[..lastSpace].Trim();
    var last  = trimmed[(lastSpace + 1)..].Trim();
    return (first, last);
}

static string? AskLegion(string firstNames, string surnameA, string surnameB)
{
    var question = $"For a character named '{firstNames}', which surname pairs more phonetically appealingly with the given name(s)? Consider syllable flow, stress placement, vowel-consonant balance, and ease of pronunciation. Choose exactly one.";
    var options  = $"{surnameA},{surnameB}";

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
    psi.ArgumentList.Add(options);
    psi.ArgumentList.Add("--tier");
    psi.ArgumentList.Add("low");
    psi.ArgumentList.Add("--no-auto-context");

    using var proc = Process.Start(psi);
    if (proc == null) return null;
    var stdout = proc.StandardOutput.ReadToEnd();
    var stderr = proc.StandardError.ReadToEnd();
    proc.WaitForExit();
    var answer = stdout.Trim();
    if (proc.ExitCode == 0 && (answer == surnameA || answer == surnameB)) return answer;
    Console.Error.WriteLine($"    legion did not return a clean pick (exit {proc.ExitCode}): {answer}");
    if (!string.IsNullOrWhiteSpace(stderr)) Console.Error.WriteLine($"    stderr: {stderr.Trim()}");
    return null;
}

characters.Reload();
var all = characters.GetAll();
Console.WriteLine($"Loaded {all.Count} characters from canon.");

var renamed = new List<(string Before, string After)>();
var skippedProtected = new List<string>();
var skippedNoHyphen = 0;
var legionFailures = new List<string>();

foreach (var c in all)
{
    var split = SplitName(c.Name);
    if (split == null) { skippedNoHyphen++; continue; }
    var (firstNames, surname) = split.Value;
    if (!surname.Contains('-')) { skippedNoHyphen++; continue; }

    // Protect Kyle / Sable / Sasha / Pixel by the first token of the first
    // names. Sable has no surname so SplitName returns null for her — the
    // single-token name is filtered out above. Kyle, Sasha, and Pixel land
    // here on first-name match.
    var firstToken = firstNames.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
    if (protectedFirstNames.Contains(firstToken))
    {
        skippedProtected.Add(c.Name);
        continue;
    }

    var parts = surname.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    if (parts.Length < 2) { skippedNoHyphen++; continue; }
    // For >2-part hyphenated surnames, we only feed Legion the first two and
    // accept the winner. Tri-hyphenated names are vanishingly rare in canon
    // and the n=2 framing keeps the prompt unambiguous.
    var a = parts[0];
    var b = parts[1];

    if (dryRun)
    {
        Console.WriteLine($"  [dry-run] {c.Name}  → would choose between [{a}] vs [{b}]");
        renamed.Add((c.Name, $"{firstNames} {a}|{b}"));
        continue;
    }
    if (renamed.Count >= maxRenames)
    {
        Console.WriteLine($"  hit --max-renames cap ({maxRenames}); stopping");
        break;
    }
    Console.WriteLine($"  Legion: {c.Name}  → choose between [{a}] vs [{b}]");
    var pick = AskLegion(firstNames, a, b);
    if (pick == null)
    {
        legionFailures.Add(c.Name);
        continue;
    }
    var newName = $"{firstNames} {pick}";
    if (string.Equals(newName, c.Name, StringComparison.Ordinal)) continue;

    // Stash the prior full name as an alias so existing references resolve.
    if (!c.Aliases.Contains(c.Name)) c.Aliases.Add(c.Name);
    var oldName = c.Name;
    c.Name = newName;
    characters.Save(c);
    renamed.Add((oldName, newName));
    Console.WriteLine($"    -> {newName}");
}

// ── Report ────────────────────────────────────────────────────────────────
Console.WriteLine();
Console.WriteLine("=== Summary ===");
Console.WriteLine($"Renamed: {renamed.Count}");
foreach (var (before, after) in renamed)
{
    Console.WriteLine($"  {before}  -->  {after}");
}
Console.WriteLine($"Protected (skipped): {skippedProtected.Count}");
foreach (var n in skippedProtected) Console.WriteLine($"  {n}");
Console.WriteLine($"No hyphenated surname (skipped): {skippedNoHyphen}");
Console.WriteLine($"Legion failures: {legionFailures.Count}");
foreach (var n in legionFailures) Console.WriteLine($"  {n}");

return 0;
