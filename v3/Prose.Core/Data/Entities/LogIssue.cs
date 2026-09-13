namespace Prose.Core.Data.Entities;

/// <summary>
/// One engine error the author has queued to fix, identified by a stable signature so the same
/// fault recurring a hundred times is one row, not a hundred.
///
/// <para><b>This table holds triage state only</b> — status, when it was queued, when it was
/// called fixed. It deliberately does NOT store occurrence counts or last-seen timestamps: the
/// Serilog files are the source of truth for what actually happened, and those are recomputed on
/// every read. A stored "last seen" would be a second copy of a fact that can go stale, and the
/// whole point of this table is to answer "is it still happening?" honestly.</para>
///
/// <para>That is also what makes "fixed" verifiable rather than a claim. An issue marked resolved
/// is reported as REGRESSED the moment a matching line appears in the logs with a later timestamp
/// than <see cref="ResolvedAt"/> — nobody has to remember to reopen it.</para>
///
/// <para>Deliberately NOT the <c>Findings</c> table, which carries story defects (contradictions,
/// clichés, voice flags) and whose open counts feed SII grading and publish-readiness. An engine
/// stack trace is not a story defect, and mixing the two would corrupt a measure the author
/// relies on.</para>
///
/// <para>Not system-versioned (see <c>ProseDbContext.SystemVersionedTables</c>): this is
/// operational bookkeeping, like <c>Findings</c>, not canon.</para>
/// </summary>
public class LogIssue
{
    public long Id { get; set; }

    /// <summary>
    /// Stable hash of the normalised message — GUIDs, numbers, paths and timestamps replaced with
    /// placeholders — so "beat 41f2… failed" and "beat 9c07… failed" are recognised as the same
    /// fault. Unique: one row per distinct fault.
    /// </summary>
    public string Signature { get; set; } = "";

    /// <summary>A representative message, for display. The logs hold the full text.</summary>
    public string Title { get; set; } = "";

    /// <summary>Serilog level of the sample that was queued (Error, Fatal, Warning).</summary>
    public string Level { get; set; } = "";

    /// <summary>Open | Resolved | Ignored. "Resolved" is a claim the log files re-check.</summary>
    public string Status { get; set; } = "Open";

    public DateTime TrackedAt { get; set; } = DateTime.UtcNow;

    /// <summary>When it was called fixed. Any matching log line after this reopens it on sight.</summary>
    public DateTime? ResolvedAt { get; set; }

    /// <summary>Free text — what the fix was, or why it is being ignored.</summary>
    public string? Note { get; set; }
}
