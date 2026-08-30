using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;

namespace Prose.Core.Services;

public class TextIntegrityFinding
{
    public string Table { get; set; } = "";
    public Guid RowId { get; set; }
    public string Column { get; set; } = "";
    public string RowLabel { get; set; } = "";
    public int Position { get; set; }
    public string Context { get; set; } = "";
    public char? SuggestedFix { get; set; }
    public string? SuggestedFixReason { get; set; }
    public int FoundCodepoint { get; set; }
}

/// <summary>
/// Finds and repairs U+FFFD (the Unicode replacement character) sitting where a real character —
/// almost always Φ, the QUANTA currency symbol — was silently lost during some past write.
///
/// Root cause of the detection gap this service exists to close (found 2026-08-15, while
/// sequential-reading Ballast): SQL Server's <c>REPLACE</c>/<c>CHARINDEX</c> gave FALSE NEGATIVES
/// for U+FFFD under this DB's collation — <c>CHARINDEX(NCHAR(65533), text)</c> returned 0 even
/// when a direct <c>UNICODE(SUBSTRING(text, pos, 1))</c> at that exact position confirmed 65533.
/// Any code (or ad-hoc SQL) that used those functions to check for corruption would silently miss
/// it. This service never touches SQL string-matching functions for the check itself — it pulls
/// text into memory via EF Core and does a plain C# <c>char == '�'</c> comparison, which has
/// no collation involved and cannot have the same false-negative bug.
///
/// Confirmed scope of the original bug (Ballast): 8 instances in <c>Nodes.NodeOutline</c>, all
/// immediately before a formatted number (Φ8,400, Φ2,100, etc.) — 0 instances in that book's 339
/// `Beats.Text` rows. The corruption is specific to Φ's 2-byte UTF-8 encoding being mangled by
/// some past non-UTF-8 write path.
///
/// Extended 2026-08-15 (same day) after finding a SECOND, distinct corruption class in Between
/// the Lines' bible: stray low-range control characters (codepoints 1-31, excluding tab/LF/CR)
/// sitting where an em-dash (—, 8212) or section symbol (§, 167) had been silently lost — 12
/// instances, all in <c>Nodes.NodeOutline</c>. Same failure family as the Φ→U+FFFD bug (a non-UTF-8
/// write path mangling a multi-byte character), different garbage byte value. This scanner now
/// flags BOTH U+FFFD and any stray control character in that range — do not narrow it back to
/// U+FFFD only. Treat any future new garbage-codepoint discovery the same way: extend
/// <see cref="IsSuspect"/> rather than writing a one-off fix and moving on.
/// </summary>
public class TextIntegrityService(IDbContextFactory<ProseDbContext> dbFactory)
{
    private const char ReplacementChar = '�';
    private const char Phi = 'Φ';
    private const char EmDash = '—';
    private const char SectionSign = '§';

    private static bool IsSuspect(char c) =>
        c == ReplacementChar || (c < 32 && c != '\t' && c != '\n' && c != '\r');

    public async Task<List<TextIntegrityFinding>> ScanAsync(CancellationToken ct = default)
    {
        var findings = new List<TextIntegrityFinding>();
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        // IgnoreQueryFilters: a data-integrity scan must see every row in every universe in one
        // pass, never scoped to whatever --universe happened to be passed to this process — the
        // Ballast corruption would have stayed invisible to a GLMZ-only scan forever otherwise.
        var beats = await db.Beats.AsNoTracking().IgnoreQueryFilters()
            .Select(b => new { b.Id, b.Number, b.Text })
            .ToListAsync(ct);
        foreach (var beat in beats)
            ScanText(beat.Text, "Beats", beat.Id, "Text", $"Beat #{beat.Number}", findings);

        var books = await db.Nodes.AsNoTracking().IgnoreQueryFilters()
            .Where(n => n.Kind == "book" && n.NodeOutline != null)
            .Select(n => new { n.Id, n.Title, n.NodeOutline })
            .ToListAsync(ct);
        foreach (var book in books)
            ScanText(book.NodeOutline, "Nodes", book.Id, "NodeOutline", book.Title, findings);

        return findings;
    }

    private static void ScanText(string? text, string table, Guid rowId, string column, string rowLabel,
        List<TextIntegrityFinding> findings)
    {
        if (string.IsNullOrEmpty(text)) return;

        for (var i = 0; i < text.Length; i++)
        {
            if (!IsSuspect(text[i])) continue;

            var start = Math.Max(0, i - 25);
            var len = Math.Min(50, text.Length - start);
            var context = text.Substring(start, len);

            char? suggested = null;
            string? reason = null;
            var isReplacementChar = text[i] == ReplacementChar;

            // U+FFFD immediately followed by a digit, in this project's established "Φ precedes
            // the number" convention (feedback_phi_always_before_number) → almost certainly Φ.
            // This rule is scoped to U+FFFD specifically — it must NOT fire for the other stray
            // control characters below, which are a different corruption (see class doc comment).
            if (isReplacementChar && i + 1 < text.Length && char.IsDigit(text[i + 1]))
            {
                suggested = Phi;
                reason = "immediately followed by a digit — matches this project's Φ-precedes-number currency convention";
            }
            // A stray (non-U+FFFD) control char with a space on both sides, between two word
            // characters, is almost always a lost em-dash used as a separator (e.g.
            // "Idris — Bishop") — the pattern found corrupted 11 times in Between the Lines' bible.
            else if (!isReplacementChar && i > 0 && i + 1 < text.Length
                     && text[i - 1] == ' ' && text[i + 1] == ' ')
            {
                suggested = EmDash;
                reason = "stray control char surrounded by spaces between words — almost certainly a lost em-dash separator";
            }
            // A stray (non-U+FFFD) control char immediately before a digit with no space of its
            // own before it, in a "see brief §N" style reference, is almost always a lost section
            // symbol — found corrupted twice in Between the Lines' bible.
            else if (!isReplacementChar && i + 1 < text.Length && char.IsDigit(text[i + 1]))
            {
                suggested = SectionSign;
                reason = "stray control char immediately before a digit — likely a lost section symbol (§N)";
            }

            findings.Add(new TextIntegrityFinding
            {
                Table = table,
                RowId = rowId,
                Column = column,
                RowLabel = rowLabel,
                Position = i,
                Context = context,
                SuggestedFix = suggested,
                SuggestedFixReason = reason,
                FoundCodepoint = text[i],
            });
        }
    }

    /// <summary>
    /// Repairs one finding via a direct positional single-character replace (raw SQL STUFF at the
    /// finding's exact character position) — never a bulk REPLACE, since that function's
    /// unreliable matching against U+FFFD is the whole reason this service exists.
    /// </summary>
    public async Task ApplyFixAsync(TextIntegrityFinding finding, char replacement, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var pos = finding.Position + 1;
        var repl = replacement.ToString();
        if (finding.Table == "Beats")
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE Beats SET Text = STUFF(Text, {pos}, 1, {repl}) WHERE Id = {finding.RowId}", ct);
        else
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE Nodes SET NodeOutline = STUFF(NodeOutline, {pos}, 1, {repl}) WHERE Id = {finding.RowId}", ct);
    }
}
