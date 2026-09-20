using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Core.Services;

/// <summary>
/// Turns the raw Serilog files into a triage queue: groups errors into distinct faults, lets the
/// author queue the ones worth fixing, and re-checks "fixed" against the logs rather than trusting
/// the flag.
///
/// <para><b>Why grouping matters.</b> One broken code path produces hundreds of identical log
/// lines. Reading the file gives you volume; what you want is the handful of distinct faults, each
/// with how often and how recently it happened. Grouping is what turns "2,000 lines since Tuesday"
/// into "four things are broken".</para>
///
/// <para><b>Why "resolved" is not taken at face value.</b> An issue marked resolved is reported as
/// <see cref="IssueState.Regressed"/> as soon as a matching line appears with a timestamp later
/// than its <c>ResolvedAt</c>. Nothing has to remember to reopen it, and a fix that did not
/// actually work cannot sit in the queue looking done.</para>
///
/// <para><b>The one real limit:</b> the Hub keeps 14 days of daily log files. Beyond that window
/// there is nothing left to check against, so an old resolved issue reads as resolved because the
/// evidence has rolled off — not because it was proven fixed. <see cref="LogIssueView.Verifiable"/>
/// says which of the two you are looking at, rather than letting the distinction go unnoticed.</para>
/// </summary>
public class LogIssueService(LoggingService logs, IDbContextFactory<ProseDbContext> dbFactory)
{
    /// <summary>How far back the daily log files are retained (Program.cs: retainedFileCountLimit).
    /// Past this, absence of evidence is not evidence of a fix.</summary>
    public const int RetentionDays = 14;

    // ── Signature ───────────────────────────────────────────────────────────

    // Order matters: GUIDs before the generic number rule, or a GUID's digits get eaten first and
    // two different GUIDs stop collapsing to the same template.
    private static readonly (Regex Re, string Token)[] Normalisers =
    [
        (new Regex(@"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}", RegexOptions.Compiled), "{guid}"),
        (new Regex(@"\b[0-9a-fA-F]{32}\b", RegexOptions.Compiled), "{guid}"),
        (new Regex(@"[A-Za-z]:\\[^\s""']+", RegexOptions.Compiled), "{path}"),
        // Opaque identifiers that are not GUIDs: Blazor circuit ids, tokens, base64 hashes.
        // Found against the real logs 2026-09-12 — the single most common error in this corpus is
        // "Unhandled exception in circuit 'z-OMai2TwVd_wLJLaAS2uIV2yYw0tMfznr7d7PZKC9A'", and
        // without this rule each circuit id made its own "distinct fault", which is precisely the
        // fragmentation this service exists to prevent. Requires a digit so ordinary long words
        // and namespaced type names (Prose.Core.Services.SomethingVeryLong) are left alone.
        (new Regex(@"(?=[A-Za-z0-9_+/=-]*\d)[A-Za-z0-9_+/=-]{16,}", RegexOptions.Compiled), "{id}"),
        (new Regex(@"\b\d{4}-\d{2}-\d{2}[T ]\d{2}:\d{2}:\d{2}(\.\d+)?\b", RegexOptions.Compiled), "{time}"),
        // NOT \b\d+\b: there is no word boundary between a digit and a letter, so that pattern
        // silently fails on the most common shape in these logs — "failed after 812ms" kept its
        // timing and every occurrence became its own fault. The lookbehind instead means "a number
        // that does not continue an identifier", so 812ms normalises while SHA256 and UTF8 do not
        // lose their digits and collapse into each other. The digit in the lookbehind matters as
        // much as the letter: excluding only letters still matched "56" inside SHA256.
        (new Regex(@"(?<![A-Za-z0-9])\d+(\.\d+)?", RegexOptions.Compiled), "{n}"),
    ];

    /// <summary>
    /// Collapses one message to its template, so the same fault about different data is one fault.
    /// "Beat 41f2… failed after 812ms" and "Beat 9c07… failed after 47ms" both become
    /// "Beat {guid} failed after {n}ms".
    /// </summary>
    public static string Normalise(string message)
    {
        var s = message ?? "";
        foreach (var (re, token) in Normalisers) s = re.Replace(s, token);
        return Regex.Replace(s, @"\s+", " ").Trim();
    }

    /// <summary>Stable short hash of (level, normalised message, exception type). Short enough to
    /// type on the command line, long enough not to collide in a personal corpus.</summary>
    public static string SignatureOf(LogEntry entry)
    {
        var exceptionType = "";
        if (!string.IsNullOrWhiteSpace(entry.Exception))
        {
            // Serilog writes the exception block starting with "Namespace.TypeName: message".
            var firstLine = entry.Exception.Split('\n')[0].Trim();
            var colon = firstLine.IndexOf(':');
            exceptionType = colon > 0 ? firstLine[..colon] : firstLine;
        }

        var payload = $"{entry.Level}|{Normalise(entry.Message)}|{exceptionType}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash)[..10].ToLowerInvariant();
    }

    // ── Grouping ────────────────────────────────────────────────────────────

    /// <summary>
    /// Distinct faults in the window, worst-first (most recent wins ties). <paramref name="minLevel"/>
    /// defaults to Error — warnings are noise for a fix queue.
    /// </summary>
    public List<LogFault> GroupFaults(DateTime? since = null, string minLevel = "Error",
                                      string? searchText = null, int maxEntries = 20000)
    {
        var entries = logs.Search(new LogSearchRequest
        {
            Since = since ?? DateTime.Now.AddDays(-RetentionDays),
            MinSeverity = minLevel,
            SearchText = searchText,
            MaxResults = maxEntries,
        });

        return entries
            .GroupBy(SignatureOf)
            .Select(g =>
            {
                var newest = g.OrderByDescending(e => e.Timestamp).First();
                return new LogFault(
                    Signature: g.Key,
                    Title: Clip(newest.Message, 200),
                    Level: newest.Level,
                    Count: g.Count(),
                    FirstSeen: g.Min(e => e.Timestamp),
                    LastSeen: g.Max(e => e.Timestamp),
                    Sample: newest);
            })
            .OrderByDescending(f => f.LastSeen)
            .ToList();
    }

    // ── The queue ───────────────────────────────────────────────────────────

    /// <summary>Queues a fault to fix. Re-tracking a signature already queued reopens it rather
    /// than creating a second row.</summary>
    public async Task<LogIssue> TrackAsync(LogFault fault, string? note = null, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var issue = await db.LogIssues.FirstOrDefaultAsync(i => i.Signature == fault.Signature, ct);
        if (issue == null)
        {
            issue = new LogIssue { Signature = fault.Signature };
            db.LogIssues.Add(issue);
        }

        issue.Title  = fault.Title;
        issue.Level  = fault.Level;
        issue.Status = "Open";
        issue.ResolvedAt = null;
        if (note != null) issue.Note = note;

        await db.SaveChangesAsync(ct);
        return issue;
    }

    /// <summary>Marks an issue fixed. The claim is re-checked against the logs on every read —
    /// see <see cref="ListAsync"/>.</summary>
    public async Task<LogIssue?> ResolveAsync(long id, string? note = null, CancellationToken ct = default)
        => await SetStatusAsync(id, "Resolved", DateTime.UtcNow, note, ct);

    /// <summary>Stops reporting an issue without claiming it is fixed — for noise you have decided
    /// to live with. Kept distinct from Resolved so a deliberate shrug is never mistaken for a fix.</summary>
    public async Task<LogIssue?> IgnoreAsync(long id, string? note = null, CancellationToken ct = default)
        => await SetStatusAsync(id, "Ignored", DateTime.UtcNow, note, ct);

    public async Task<LogIssue?> ReopenAsync(long id, string? note = null, CancellationToken ct = default)
        => await SetStatusAsync(id, "Open", null, note, ct);

    private async Task<LogIssue?> SetStatusAsync(long id, string status, DateTime? resolvedAt,
                                                 string? note, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var issue = await db.LogIssues.FirstOrDefaultAsync(i => i.Id == id, ct);
        if (issue == null) return null;

        issue.Status = status;
        issue.ResolvedAt = resolvedAt;
        if (note != null) issue.Note = note;

        await db.SaveChangesAsync(ct);
        return issue;
    }

    /// <summary>
    /// The queue, each row re-checked against the log files. This is the method that answers
    /// "which of these did I actually fix?" — a resolved issue whose fault has been logged again
    /// since comes back as <see cref="IssueState.Regressed"/>.
    /// </summary>
    public async Task<List<LogIssueView>> ListAsync(bool includeClosed = false, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var issues = await db.LogIssues.AsNoTracking()
            .OrderByDescending(i => i.TrackedAt)
            .ToListAsync(ct);

        if (issues.Count == 0) return [];

        // One pass over the whole retention window, then look each issue up - rather than
        // re-reading the log files once per issue.
        var faults = GroupFaults(since: DateTime.Now.AddDays(-RetentionDays), minLevel: "Warning")
            .ToDictionary(f => f.Signature);

        var views = new List<LogIssueView>();
        foreach (var i in issues)
        {
            faults.TryGetValue(i.Signature, out var fault);

            var regressed = i.Status == "Resolved" && i.ResolvedAt != null
                         && fault != null && fault.LastSeen > i.ResolvedAt.Value.ToLocalTime();

            var state = regressed        ? IssueState.Regressed
                      : i.Status switch { "Resolved" => IssueState.Resolved,
                                          "Ignored"  => IssueState.Ignored,
                                          _          => IssueState.Open };

            // A resolved issue can only be *proven* fixed while the evidence window still covers
            // the moment it was resolved. Older than that and silence means the logs rolled off.
            var verifiable = i.ResolvedAt == null
                          || i.ResolvedAt.Value.ToLocalTime() >= DateTime.Now.AddDays(-RetentionDays);

            if (!includeClosed && state is IssueState.Resolved or IssueState.Ignored) continue;

            views.Add(new LogIssueView(i, state, fault, verifiable));
        }

        // Regressions first: a fix that did not hold is the most urgent thing in the list.
        return views
            .OrderBy(v => v.State switch { IssueState.Regressed => 0, IssueState.Open => 1, _ => 2 })
            .ThenByDescending(v => v.Fault?.LastSeen ?? v.Issue.TrackedAt)
            .ToList();
    }

    private static string Clip(string s, int max) =>
        string.IsNullOrEmpty(s) ? "" :
        s.Length <= max ? s.ReplaceLineEndings(" ") : s.ReplaceLineEndings(" ")[..max] + "…";
}

/// <summary>One distinct fault in the logs, with how often and how recently it happened.</summary>
public record LogFault(string Signature, string Title, string Level, int Count,
                       DateTime FirstSeen, DateTime LastSeen, LogEntry Sample);

public enum IssueState { Open, Resolved, Regressed, Ignored }

/// <summary>A queued issue as it stands right now: the stored triage state, plus what the log
/// files currently say about it. <paramref name="Fault"/> is null when nothing matching appears in
/// the retention window — which for an open issue means it has stopped happening.</summary>
public record LogIssueView(LogIssue Issue, IssueState State, LogFault? Fault, bool Verifiable);
