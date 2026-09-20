using System.ComponentModel.DataAnnotations;

namespace Prose.Core.Data.Entities;

/// <summary>
/// Durable, structured record of a higher-level decision or piece of reasoning — not a
/// mechanical command invocation (see <see cref="CommandLedgerEntry"/> for that), but the
/// "why" behind one or more of them. Written explicitly via <c>prose --log-decision</c> /
/// the <c>log_decision</c> MCP tool, by this assistant or any other LLM/human working
/// against the Hub. Exists so a totally fresh session — with zero conversation memory —
/// can query the Hub directly and reconstruct not just what ran, but why, instead of
/// depending on a chat transcript or an external memory file that only one assistant reads.
/// </summary>
public class DecisionLedgerEntry
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime At { get; set; } = DateTime.UtcNow;

    [MaxLength(128)]
    public string? SessionId { get; set; }

    [MaxLength(256)]
    public string Summary { get; set; } = "";

    public string? Rationale { get; set; }

    [MaxLength(64)]
    public string? Category { get; set; } // "architecture" | "bugfix" | "canon-change" | ...

    [MaxLength(128)]
    public string? Actor { get; set; } // "claude-code", "human", a model name, etc.

    /// <summary>JSON array of <see cref="CommandLedgerEntry.Id"/>s this decision grew out
    /// of or explains — ties the "why" back to the concrete commands that implemented it.</summary>
    public string? RelatedCommandIdsJson { get; set; }
}
