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
        "AI", "HVAC", "VTOL", "ARGUS", "UV", "PD", "NGRA"
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
        if (orderedBeats.Count == 0)
        {
            var childIds = await db.Nodes.AsNoTracking()
                .Where(n => n.ParentNodeId == nodeId)
                .Select(n => n.Id).ToListAsync(ct);
            if (childIds.Count > 0)
                orderedBeats = await (
                    from sb in db.BeatNodes.AsNoTracking()
                    join b in db.Beats.AsNoTracking() on sb.BeatId equals b.Id
                    where childIds.Contains(sb.NodeId) && sb.IsEnabled
                    orderby sb.SortKey
                    select b
                ).ToListAsync(ct);
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

        // Pre-build a set of upper-case [A-Z]{3,6} tokens from entity names
        var knownTokens = new HashSet<string>(StringComparer.Ordinal);
        foreach (var name in entityNames)
        {
            if (string.IsNullOrWhiteSpace(name)) continue;
            var upper = name.ToUpperInvariant();
            if (Regex.IsMatch(upper, @"^[A-Z]{3,6}$")) knownTokens.Add(upper);
            foreach (var word in upper.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                if (Regex.IsMatch(word, @"^[A-Z]{3,6}$")) knownTokens.Add(word);
        }

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

    static string Snippet(string text, int matchIndex, int radius)
    {
        int start = Math.Max(0, matchIndex - radius / 2);
        int end   = Math.Min(text.Length, matchIndex + radius / 2);
        var raw   = text[start..end].Replace('\n', ' ').Replace('\r', ' ');
        return (start > 0 ? "..." : "") + raw + (end < text.Length ? "..." : "");
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
