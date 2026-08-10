using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;

namespace Prose.Core.Services;

// ─────────────────────────────────────────────────────────────────────────────
// SanityScanService
//
// Runs a battery of deterministic (no-LLM) checks over a finished node's
// prose to catch problems that reviewers miss because they're invisible to a
// reader who doesn't know the internal authoring codes.
//
// Checks:
//   A) Internal node-code leak  -- "NRST" / "BCODA" / etc. in prose
//   B) Undefined all-caps acronym -- \b[A-Z]{3,6}\b not in whitelist or entity DB
//   C) Heft / length floor        -- total word count -> PDF page estimate
//   D) Mojibake detector          -- UTF-8 -> codepage corruption artifacts
//
// No LLM calls; fast enough to run in CI / as a pre-publish gate.
// ─────────────────────────────────────────────────────────────────────────────

public class SanityScanService(IDbContextFactory<ProseDbContext> dbFactory)
{
    // ── Whitelisted in-world all-caps terms ───────────────────────────────────
    // These are legitimate terms that appear in prose and must NEVER be flagged.
    static readonly HashSet<string> Whitelist = new(StringComparer.Ordinal)
    {
        "GLMZ", "QCE", "QUANTA", "PEREGRINE", "E.L.F", "ELF", "BCI",
        "AI", "HVAC", "VTOL", "ARGUS", "UV", "PD", "NGRA",
        // 2026-08-09: standard citation/scholarly abbreviations from real academic
        // publishing — NONFICTION's Gospel books cite real sources this way, and none of
        // these could plausibly ever BE a leaked internal dev code (they're established,
        // external, real-world abbreviations, not invented GLMZ/SCRY-style short codes).
        // Found via a corpus-wide sanity-scan sweep: MATTHEW alone had 15+ of these firing
        // as "possible placeholder or leaked code" false positives.
        "BCE", "CE", "ICC", "SBL", "JBL", "JSOT", "BDAG", "JPS", "UBS", "IVP",
        "RINAP", "SCM", "UNESCO", "IAA", "BYU",
        // Common Gospel/biblical person names that are ALSO, coincidentally, sibling
        // NONFICTION book NodeCodes (MATTHEW/MARK/LUKE/JOHN — each Gospel is its own
        // book, named after its traditional author). "John was baptized" or "see Mark's
        // account" is ordinary nonfiction narration, not a leak of another book's project
        // code — the risk of a genuine cross-book leak using exactly these 4 common
        // English names is effectively nil, unlike an invented abbreviation like
        // "BCODA"/"NRST" that has no meaning outside this project.
        "JOHN", "MARK", "LUKE", "MATTHEW",
        // Genuine content, found via the same sweep: "LEVI" is Matthew's other biblical
        // name (Mark 2:14); "DNA" is a universal, non-jargon term; "PONTIF"/"MAXIM" are a
        // real, deliberately-quoted historical Roman coin legend ("PONTIF MAXIM" = Pontifex
        // Maximus, abbreviated exactly the way period Roman numismatics did) — 2 words is
        // below IsInsideCapsRun's minNeighborCapsWords=2 threshold for recognizing an
        // embedded inscription/quote, so this narrow whitelist entry covers the gap without
        // loosening that general heuristic.
        "LEVI", "DNA", "PONTIF", "MAXIM",
        // 2026-08-09 follow-up: continued the same corpus-wide sweep across every remaining
        // NONFICTION book (NEPH, LUKE, JOHN, MARK, IREOUT) — every single warning checked by
        // hand across all five turned out to be a real term, not a leak. Rather than
        // re-litigate each one in a future session, whitelisted the confirmed-genuine set:
        //
        // Bible-translation abbreviations (NRSV, RSV, NIV, KJV, NKJV, ESV, NABRE — all real,
        // standard scholarly shorthand for named English Bible translations):
        "NRSV", "RSV", "NIV", "KJV", "NKJV", "ESV", "NABRE",
        // Academic bodies/publishers/journals/reference works (ERC = European Research
        // Council; CNRS = a French national research org; SPCK/EBSCO/WUNT/HTS/CSCO/BDB/DDD/
        // HALOT/KTU/OTP/JST = real publishers, databases, monograph series, and lexicons a
        // citation-grounded nonfiction book is expected to cite):
        "ERC", "CNRS", "SPCK", "EBSCO", "WUNT", "HTS", "CSCO", "BDB", "DDD",
        "HALOT", "KTU", "OTP", "JST",
        // Real historical/mythological/biblical terms and figures (GAMLA = a real
        // archaeological site; DROPSY/MAMMON/LOT/MINA/TYRIAN/SHEKEL = biblical-era terms and
        // figures; BORR = a Norse mythological figure, father of Odin; AL-SUDDI/TABARI = real
        // early Islamic scholars; STYX/ASTRAL = comparative-mythology vocabulary; BAAM = a
        // real museum shelfmark, Biblioteca Alexandrina's Antiquities Museum; BEZAE = Codex
        // Bezae, a real ancient biblical manuscript):
        "GAMLA", "DROPSY", "MAMMON", "LOT", "MINA", "TYRIAN", "SHEKEL", "BORR",
        "SUDDI", "TABARI", "STYX", "ASTRAL", "BAAM", "BEZAE",
        // Real Irish-history institutions/organizations (IREOUT): RIC = Royal Irish
        // Constabulary; IRA = Irish Republican Army; GHQ = General Headquarters; GPO =
        // General Post Office (the 1916 Easter Rising site); HMSO = Her Majesty's Stationery
        // Office; DIB = Dictionary of Irish Biography; DBE = Dame Commander of the British
        // Empire; MSPC = Military Service Pensions Collection; OED = Oxford English
        // Dictionary:
        "RIC", "IRA", "GHQ", "GPO", "HMSO", "DIB", "DBE", "MSPC", "OED",
        // Universal real-world terms/orgs with no plausible leaked-code reading: USA, NASA,
        // UFO, PEN (the international writers' association and literary prize), PRIEST
        // (a section heading, not a placeholder):
        "USA", "NASA", "UFO", "PEN", "PRIEST",
        // Ordinary short English words used in isolated ALL CAPS for emphasis — a common,
        // legitimate device in BOTH nonfiction ("It was NOT her fault") and fiction dialogue/
        // narration alike, not specific to one universe:
        "NOT", "DAY", "COIN", "HAND", "HIRED",
        // Non-standard but historically real Roman-numeral form (old clock-face "IIII" for 4,
        // instead of "IV") — narrower than extending IsRomanNumeral's grammar for one rare
        // inscription-style usage, same reasoning as the PONTIF/MAXIM entry above.
        "IIII",
        // Final round of the same sweep (1381, JOAN): HEDGE is part of "hedge-priest," a real
        // historical term for an unlicensed itinerant priest (John Ball); UCSF/UPI are real
        // institutions (Univ. of California San Francisco; United Press International); IPEAF
        // is a real, defined medical diagnosis term (Idiopathic Partial Epilepsy with Auditory
        // Features) spelled out in full in the same sentence it appears in.
        "HEDGE", "UCSF", "UPI", "IPEAF",
        // HORROR universe: QRT is a ham-radio horror story (its own title, "QRT", is a real
        // amateur-radio Q-code meaning "I am ceasing transmission" — deliberate wordplay, not
        // a leak). The rest are the real ham-radio vocabulary the story is built on: QRP (low
        // power operation), RST (signal report system), UTC (Coordinated Universal Time), PTT
        // (push-to-talk), QSO (a radio contact), FCC (Federal Communications Commission), SWR
        // (standing wave ratio), QSL (confirmation of contact).
        "QRT", "QRP", "RST", "UTC", "PTT", "QSO", "FCC", "SWR", "QSL",
        // GLMZ / BCODA sweep: two established, deliberate stylistic devices, not leaks.
        // (1) The mystery "entity" Kyle contracts with communicates entirely in all-caps
        // contract-format messages (CONTRACT NUMBER/STATUS/PAYMENT/NOTES fields, e.g.
        // "GRATUITY WITHHELD THIS PERIOD - ITEMIZED: MORALE.") — established, recurring
        // in-fiction formatting representing a machine/AI voice, not a placeholder. (2) Short
        // embedded physical-sign/logbook/sensor-readout quotes (a hand-painted tally board,
        // "LOG VOL 7" on a logbook spine, "TIMING VARIANCE"/"AMBIENT INTERFERENCE" sensor
        // labels) — the same found-document category IsInsideCapsRun already exempts for
        // LONGER runs, these are just short enough (1-2 words) to fall under its
        // minNeighborCapsWords=2 threshold, same shape as the earlier PONTIF/MAXIM case.
        // Also "TEST" — the Testament book's own NodeCode, but here always the ordinary
        // English word ("a TEST of whether...", "EQUIPMENT TEST: SUCCESSFUL"), same
        // effectively-zero-leak-risk reasoning as JOHN/MARK/LUKE/MATTHEW above.
        "SINCE", "LIKES", "SURE", "MORALE", "DENIED", "LIST", "AGREED",
        "TIMING", "ACCEPT", "LOG", "NOTED", "VOL", "TEST",
        // "RTD-6" — an in-world freight-placard destination code ("the Rotterdam-bound
        // slug... the placard at the nose read 10:15 RTD-6"), the same real-world-style
        // shipping-manifest shorthand as an airport code, not a leak.
        "RTD",
        // "OUSE" — deliberate wordplay, explained in the same sentence: a noodle counter's
        // broken sign ("The H's out, so it just says 'OUSE.'"), not a leak at all.
        "OUSE",
        // Sparrow (SPRW): real orbital-mechanics terms (GEO/MEO = Geostationary/Medium Earth
        // Orbit), a real isotope-geochemistry reference standard (VSMOW = Vienna Standard
        // Mean Ocean Water), and the orbital-tracking AI's own established all-caps
        // structured-report device (PERIOD/LAYERS/LOGGED — the same category as BCODA's
        // all-caps contract-format entity, a different character using the same device).
        "GEO", "MEO", "VSMOW", "PERIOD", "LAYERS", "LOGGED",
        // The Long Cut (TLC): NREN is defined in the very same sentence it appears in ("the
        // NREN relay - a person-to-person package network"); DOA (Dead On Arrival) and ICD
        // (International Classification of Diseases) are universal real-world medical/legal
        // terms, fitting for an underground-surgery setting.
        "NREN", "DOA", "ICD",
        // Testament (TEST): NCO (Non-Commissioned Officer) is a universal, real military rank
        // term, fitting a court-martial book.
        "NCO",
        // Iron & Silk (IxS): ETA (Estimated Time of Arrival) is a universal real-world term
        // used completely naturally ("ETA to Nari's building is sixteen minutes"). NOT
        // whitelisted: "CSE" (beat #11231, "coerced Grade 4 CSE - unlocated") — reads as a
        // deliberate narrative mystery marker for an active, unresolved plot thread ("A
        // thing that was out there somewhere and hadn't been found"), not a defect to fix.
        "ETA"
    };

    // ── Built-in alias codes (supplement codes pulled from DB) ────────────────
    static readonly HashSet<string> BuiltinCodes = new(StringComparer.Ordinal)
    {
        "MGUN","NRST","NRSTC","NRSTQ","MCRM","BCODA","DWIACE","VATD","ATTE",
        "SPRW","SRZR","MNEMO","TDIU","UNDR","TEST","SS","MxG","NxR","CxC"
    };

    // Codes that are clearly not English words -> block severity
    // Ambiguous word-like codes ("TEST","SS","ATTE") are warn severity
    static readonly HashSet<string> BlockCodes = new(StringComparer.Ordinal)
    {
        "MGUN","NRST","NRSTC","NRSTQ","MCRM","BCODA","DWIACE","VATD","SPRW",
        "SRZR","MNEMO","TDIU","UNDR","MxG","NxR","CxC"
    };

    // ── Mojibake substrings ───────────────────────────────────────────────────
    // UTF-8 byte sequences decoded as Windows-1252 (the classic mojibake pattern).
    // All non-ASCII characters are written as \u escapes to keep the source safe.
    //
    //  Pattern        | UTF-8 bytes | What it was
    //  ---------------+-------------+----------------------------
    //  â€   | E2 80       | start of most 3-byte seqs
    //  Ã    | C3 A0       | a-grave (U+00E0)
    //  Ã©   | C3 A9       | e-acute (U+00E9)
    //  ...™       | E2 80 99    | right single quote (U+2019)
    //  ...”       | E2 80 94    | em dash (U+2014)
    //  Â          | C2          | stray two-byte prefix
    static readonly string[] MojiSubstrings =
    [
        "â€",                         // a-tilde + euro = UTF-8 3-byte prefix
        "Ã ",                         // a-grave misread
        "Ã©",                         // e-acute misread
        "â€™",                   // right single quote misread
        "â€”",                   // em dash misread
        "Â",                               // stray C2 prefix byte
    ];

    // ── Public API ────────────────────────────────────────────────────────────

    public async Task<SanityReport> ScanAsync(Guid nodeId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        // Load the node + its ordered beats (same pattern as BookAuditService)
        var node = await db.Nodes
            .AsNoTracking()
            .Include(s => s.BeatNodes)
            .ThenInclude(sb => sb.Beat)
            .FirstOrDefaultAsync(s => s.Id == nodeId, ct)
            ?? throw new InvalidOperationException($"Node {nodeId} not found.");

        var orderedBeats = node.BeatNodes
            .Where(sb => sb.IsEnabled && sb.Beat != null)
            .OrderBy(sb => sb.SortKey)
            .Select(sb => sb.Beat!)
            .ToList();

        // SS-A43: book-mode nodes have beats on chapter children, not directly on the story node.
        // Aggregate from children whenever any exist — NOT only when orderedBeats is empty.
        // Confirmed live 2026-08-09: a GLMZ book had exactly one enabled direct BeatNode of its
        // own (an orphan/stray row, architecturally not expected for a chaptered book) sitting
        // alongside 547 real beats spread across its 6 chapters. The old "only fall back when
        // count == 0" check saw a non-empty (but tiny) direct list and never looked at the
        // children at all — the whole book's sanity scan silently saw one 20-word beat instead
        // of the real ~14,000-word book, hiding every real finding and firing a bogus
        // "6 words -- below the 50-page floor" warning. Concatenating instead of replacing keeps
        // any genuine direct beats AND the children's beats — ordering between the two groups is
        // not narratively exact, but none of this service's checks (word count, code-leak scan,
        // acronym scan, mojibake scan) depend on cross-node reading order, only on not silently
        // dropping real content.
        // Recurses past any nested Collection, not just one level of direct children
        // (2026-08-09 follow-up fix — same "don't silently drop real content" principle
        // as the fix described above, extended to cover a split mega-chapter's grandchildren).
        var leafIds = await NodeWorkbenchService.GetLeafDescendantIdsAsync(db, nodeId, ct);
        var childIds = leafIds.Count == 1 && leafIds[0] == nodeId ? [] : leafIds;
        if (childIds.Count > 0)
        {
            var childBeats = await (
                from sb in db.BeatNodes.AsNoTracking()
                join b in db.Beats.AsNoTracking() on sb.BeatId equals b.Id
                where childIds.Contains(sb.NodeId) && sb.IsEnabled
                orderby sb.SortKey
                select b
            ).ToListAsync(ct);
            orderedBeats = orderedBeats.Concat(childBeats).ToList();
        }

        // ── Load all node codes from DB ─────────────────────────────────────
        var dbCodes = await db.Nodes
            .AsNoTracking()
            .Where(s => s.NodeCode != null)
            .Select(s => s.NodeCode!)
            .ToListAsync(ct);

        var allCodes = BuiltinCodes
            .Union(dbCodes, StringComparer.Ordinal)
            .Where(c => !Whitelist.Contains(c))
            // Purely numeric codes (e.g. "1381", a NONFICTION book coded by its historical year)
            // can never be a "leaked internal dev code" — a number appearing in prose is a date
            // or quantity, not jargon. And a node's OWN code appearing in ITS OWN prose isn't a
            // leak at all (nothing left the book it belongs to) — confirmed live 2026-08-09:
            // together these two rules account for 164 of 183 (89.6%) InternalCodeLeak findings
            // in NONFICTION, all false positives from books whose NodeCode is a plain historical
            // year or Gospel-author name (e.g. node "1381-the-peasants-revolt" coded "1381" citing
            // its own subject year 109 times; node "matthew-..." coded "MATTHEW" citing its own
            // Gospel's name 13 times) — content GLMZ's deliberately-obscure abbreviation codes
            // ("BCODA", "ATTE") never needed this exemption for, since those never legitimately
            // appear as ordinary prose words.
            .Where(c => !(c.Length > 0 && c.All(char.IsDigit)))
            .Where(c => c != node.NodeCode)
            .ToHashSet(StringComparer.Ordinal);

        // Severity: codes in BlockCodes -> "block", word-like ambiguous ones -> "warn"
        string CodeSeverity(string code) =>
            BlockCodes.Contains(code) ? "block" : "warn";

        // ── Load entity names for check B ─────────────────────────────────────
        // A token is "known" if any entity Name equals or contains it as a standalone word.
        var entityNames = await db.Entities
            .AsNoTracking()
            .Where(e => e.IsActive)
            .Select(e => e.Name)
            .ToListAsync(ct);

        // Also load Glossary terms for this node's universe — an acronym that already has a
        // back-matter Glossary entry (SS-LAW-20: the Glossary, not in-voice explanation, is the
        // designated fix for exactly this "unglossed acronym" problem) is a defined, legitimate
        // in-world term, not a placeholder/leaked code. Without this, this check re-flags every
        // single mention of every properly-glossaried acronym as "possible placeholder or leaked
        // code" forever — confirmed live: on the ATTE book alone, "ARCSEC" and "AAMA" (both
        // properly defined GlossaryTerms rows) accounted for half of this check's findings.
        var glossaryTerms = await db.GlossaryTerms
            .IgnoreQueryFilters()
            .Where(g => g.UniverseId == node.UniverseId)
            .Select(g => g.Term)
            .ToListAsync(ct);

        // Pre-build a set of upper-case [A-Z]{3,6} tokens from entity names + glossary terms
        var knownTokens = new HashSet<string>(StringComparer.Ordinal);
        foreach (var name in entityNames.Concat(glossaryTerms))
        {
            if (string.IsNullOrWhiteSpace(name)) continue;
            var upper = name.ToUpperInvariant();
            if (Regex.IsMatch(upper, @"^[A-Z]{3,6}$")) knownTokens.Add(upper);
            foreach (var word in upper.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                if (Regex.IsMatch(word, @"^[A-Z]{3,6}$")) knownTokens.Add(word);
                // 2026-08-09: also recognize the letter-only PREFIX of a hyphenated model
                // designator (e.g. "TSS-3" from the seeded entity "Torii Security Group
                // Shotgun Revolver TSS-3 'Cacophony'") — the whole-word match above requires
                // the ENTIRE space-separated word to be 3-6 bare letters, so "TSS-3" never
                // matched even though "TSS" is exactly what the checker should recognize as
                // this weapon's own established name. The lookahead requires the letter run
                // be immediately followed by a non-letter or end-of-string, so a genuinely
                // long word like "REVOLVER" (8 letters) is correctly never truncated into a
                // false 6-letter match.
                var prefixMatch = Regex.Match(word, @"^[A-Z]{3,6}(?=[^A-Z]|$)");
                if (prefixMatch.Success) knownTokens.Add(prefixMatch.Value);
            }
        }

        // 2026-08-09: an acronym the author explicitly parenthesizes anywhere in the book
        // ("the Freelancer Coordination Authority. The FCA was...") is, by construction, a
        // deliberately self-introduced term, not a placeholder or leaked code — found while
        // triaging BCODA: "FCA" was correctly never a real issue (introduced this way in the
        // very same beat), but "CFR" was a genuine miss (used repeatedly with no introduction
        // anywhere in the book) until fixed by adding the same "(CFR)" self-definition prose
        // itself. A book-wide scan for the "(ABBR)" shape recognizes both cases the same way
        // going forward, instead of requiring a whitelist entry for every acronym an author
        // properly introduces. Scoped to Check B (undefined-acronym) only — Check A's
        // internal-code-leak detection is untouched, so a coincidental "(NRST)" parenthetical
        // elsewhere could never accidentally launder a real leaked project code.
        var selfIntroducedAcronyms = new HashSet<string>(StringComparer.Ordinal);
        foreach (var beat in orderedBeats)
            foreach (Match pm in Regex.Matches(beat.Text ?? "", @"\(([A-Z]{2,8})\)"))
                selfIntroducedAcronyms.Add(pm.Groups[1].Value);

        // ── Enumerate beats ───────────────────────────────────────────────────
        var rawFindings = new List<SanityFinding>();

        // Track deduplication state for check B
        var seenUnknownTokens = new Dictionary<string, (int BeatNumber, int Count)>(StringComparer.Ordinal);
        // Track codes flagged by check A so check B skips them
        var flaggedByCodes = new HashSet<string>(StringComparer.Ordinal);

        int totalWords = 0;
        int beatIndex  = 0;

        foreach (var beat in orderedBeats)
        {
            beatIndex++;
            var text = beat.Text ?? "";

            // Word count
            totalWords += text.Split((char[])null!, StringSplitOptions.RemoveEmptyEntries).Length;

            // ── Check A: internal-code leak ───────────────────────────────────
            foreach (var code in allCodes)
            {
                string? snippet = FindCodeInText(text, code);
                if (snippet == null) continue;

                flaggedByCodes.Add(code);
                rawFindings.Add(new SanityFinding(
                    Severity:   CodeSeverity(code),
                    Kind:       "InternalCodeLeak",
                    BeatNumber: beat.Number,
                    Message:    $"Internal node code \"{code}\" appears in prose -- must be an in-world name.",
                    Snippet:    snippet));
            }

            // ── Check B: undefined all-caps acronym ───────────────────────────
            foreach (Match m in Regex.Matches(text, @"\b([A-Z]{3,6})\b"))
            {
                var token = m.Groups[1].Value;
                if (Whitelist.Contains(token)) continue;
                if (flaggedByCodes.Contains(token)) continue;
                if (allCodes.Contains(token)) continue;
                if (knownTokens.Contains(token)) continue;
                if (IsInsideCapsRun(text, m.Index, m.Length)) continue;
                if (IsRomanNumeral(token)) continue;
                if (selfIntroducedAcronyms.Contains(token)) continue;

                if (seenUnknownTokens.TryGetValue(token, out var existing))
                    seenUnknownTokens[token] = (existing.BeatNumber, existing.Count + 1);
                else
                    seenUnknownTokens[token] = (beat.Number, 1);
            }

            // ── Check D: mojibake ─────────────────────────────────────────────
            foreach (var moji in MojiSubstrings)
            {
                int idx = text.IndexOf(moji, StringComparison.Ordinal);
                if (idx < 0) continue;

                rawFindings.Add(new SanityFinding(
                    Severity:   "warn",
                    Kind:       "Mojibake",
                    BeatNumber: beat.Number,
                    Message:    "Possible mojibake (encoding corruption) detected.",
                    Snippet:    Snippet(text, idx, 80)));
                break; // one finding per beat
            }
        }

        // ── Emit check B deduped findings ─────────────────────────────────────
        foreach (var (token, (firstBeat, count)) in seenUnknownTokens)
        {
            rawFindings.Add(new SanityFinding(
                Severity:   "warn",
                Kind:       "UndefinedAcronym",
                BeatNumber: firstBeat,
                Message:    $"All-caps token \"{token}\" is not a known entity or world term -- possible placeholder or leaked code. ({count} occurrence{(count == 1 ? "" : "s")})",
                Snippet:    null));
        }

        // ── Check C: heft / length floor ─────────────────────────────────────
        int estimatedPages = (int)Math.Ceiling(totalWords / 300.0);
        if (estimatedPages < 50)
        {
            rawFindings.Add(new SanityFinding(
                Severity:   "warn",
                Kind:       "BelowLengthFloor",
                BeatNumber: null,
                Message:    $"Story is ~{estimatedPages} PDF pages (~{totalWords} words) -- below the 50-page book floor. " +
                            "Expand ORGANICALLY: more texture, observation, lived-in detail, moments that show the reader " +
                            "something cool, while advancing plot or deepening the world. Never pad; never drop prose quality.",
                Snippet:    null));
        }

        // ── Order: blocks first, then warns, then infos ───────────────────────
        static int SevOrder(string s) => s switch { "block" => 0, "warn" => 1, _ => 2 };
        var ordered = rawFindings
            .OrderBy(f => SevOrder(f.Severity))
            .ThenBy(f => f.BeatNumber ?? int.MaxValue)
            .ToList();

        return new SanityReport(
            NodeTitle:       node.Title,
            NodeSlug:        node.Slug,
            NodeCode:        node.NodeCode,
            BeatCount:         beatIndex,
            WordCount:         totalWords,
            EstimatedPdfPages: estimatedPages,
            Findings:          ordered);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Search for a code token in text. For codes containing '&amp;' (like "M&amp;G"),
    /// does a literal contains with surrounding non-letter check. Otherwise uses
    /// word-boundary regex. Returns null if not found, or a ~80-char snippet if found.
    /// </summary>
    static string? FindCodeInText(string text, string code)
    {
        if (code.Contains('&'))
        {
            // Literal match with non-letter check on both sides
            int idx = text.IndexOf(code, StringComparison.Ordinal);
            while (idx >= 0)
            {
                bool leftOk  = idx == 0 || !char.IsLetter(text[idx - 1]);
                bool rightOk = idx + code.Length >= text.Length || !char.IsLetter(text[idx + code.Length]);
                if (leftOk && rightOk)
                    return Snippet(text, idx, 80);
                idx = text.IndexOf(code, idx + 1, StringComparison.Ordinal);
            }
            return null;
        }

        // Standard word-boundary regex (case-sensitive)
        var m = Regex.Match(text, $@"\b{Regex.Escape(code)}\b");
        return m.Success ? Snippet(text, m.Index, 80) : null;
    }

    /// <summary>
    /// True for a valid Roman numeral (I, III, VIII, XII, XIV, XVIII, XIX, XXIV, XXVIII...).
    /// Citation-heavy nonfiction (chapter/verse/volume numbering, footnote markers) writes
    /// these constantly, and they match the same [A-Z]{3,6} shape Check B looks for — found
    /// via a corpus-wide sanity-scan sweep: MATTHEW alone had "III"/"XVIII"/"XII"/"XIV"/
    /// "XIX"/"XXVIII"/"XXIV"/"VIII" all firing as "possible placeholder or leaked code".
    /// Validates real numeral grammar (not just "every letter is in {I,V,X,L,C,D,M}") so an
    /// ordinary word that happens to draw only from those seven letters — e.g. "CIVIC" — is
    /// correctly left for the normal acronym check rather than silently exempted.
    /// </summary>
    internal static bool IsRomanNumeral(string token) =>
        Regex.IsMatch(token, @"^M{0,4}(CM|CD|D?C{0,3})(XC|XL|L?X{0,3})(IX|IV|V?I{0,3})$")
        && token.Length > 0;

    static string Snippet(string text, int matchIndex, int radius)
    {
        int start = Math.Max(0, matchIndex - radius / 2);
        int end   = Math.Min(text.Length, matchIndex + radius / 2);
        var raw   = text[start..end].Replace('\n', ' ').Replace('\r', ' ');
        return (start > 0 ? "..." : "") + raw + (end < text.Length ? "..." : "");
    }

    /// <summary>
    /// True when the token at [matchStart, matchStart+matchLength) sits inside a run of
    /// consecutive all-caps "words" — the shape of an embedded found-document/log/contract
    /// insert written in sustained capitals for in-world flavor, not a standalone acronym.
    /// Confirmed live: a GLMZ "morning report" security-log insert mid-beat ("06:55 - PIGEON ON
    /// THE RAIL AGAIN. SAME PIGEON. WE HAVE NAMED IT... 07:31 - DENTS SAYS TOO CLEAN MEANS
    /// CORPO...") flagged ordinary words (LOG, DENTS, BEEN, PARTY, MORALE...) as "undefined
    /// acronyms" purely because the author wrote that passage in capitals — the single largest
    /// contributor to this check's remaining false-positive volume after the glossary fix (2026-
    /// 08-09). A plaque inscription ("PRINCIPAL FUNDER: ALDISS-MWANGI CAPITAL PARTNERS") and a
    /// contract clause ("CONTRACT 14-S. RESERVED BAND: 17-19 HZ...", the same legitimate insert
    /// already documented in NightlyHealthService's caps-header exemption) hit the identical
    /// pattern. Numbers/punctuation-only tokens (timestamps, dashes, colons) don't break the
    /// run — a log line like "06:55 - PIGEON" mixes them freely with caps words.
    /// </summary>
    internal static bool IsInsideCapsRun(string text, int matchStart, int matchLength, int minNeighborCapsWords = 2)
    {
        var before = CountAdjacentCapsWords(text, matchStart, forward: false);
        var after  = CountAdjacentCapsWords(text, matchStart + matchLength, forward: true);
        return before + after >= minNeighborCapsWords;
    }

    static int CountAdjacentCapsWords(string text, int pos, bool forward)
    {
        int count = 0;
        int i = pos;
        while (true)
        {
            if (forward) { while (i < text.Length && char.IsWhiteSpace(text[i])) i++; }
            else         { while (i > 0 && char.IsWhiteSpace(text[i - 1])) i--; }

            int start, end;
            if (forward)
            {
                start = i;
                while (i < text.Length && !char.IsWhiteSpace(text[i])) i++;
                end = i;
            }
            else
            {
                end = i;
                while (i > 0 && !char.IsWhiteSpace(text[i - 1])) i--;
                start = i;
            }
            if (start == end) break; // ran off the end of the text — no more tokens

            var letters = new string(text[start..end].Where(char.IsLetter).ToArray());
            if (letters.Length == 0) continue; // pure punctuation/number token (timestamp, dash) — skip, don't break the run
            if (letters.All(char.IsUpper)) { count++; continue; }
            break; // a lowercase letter means the caps run ended here
        }
        return count;
    }
}

// ── Result models ─────────────────────────────────────────────────────────────

public sealed record SanityFinding(
    string  Severity,    // "block" | "warn" | "info"
    string  Kind,
    int?    BeatNumber,
    string  Message,
    string? Snippet);

public sealed record SanityReport(
    string                       NodeTitle,
    string                       NodeSlug,
    string?                      NodeCode,
    int                          BeatCount,
    int                          WordCount,
    int                          EstimatedPdfPages,
    IReadOnlyList<SanityFinding> Findings);
