namespace Prose.Core.Data.Entities;

/// <summary>
/// One recurring image/object/gesture in one book — the motif ledger (2026-08-28). The legacy
/// KV-backed MotifService only ever served the retired pre-Node review stack; the live Node
/// pipeline had no motif tracking at all. Rows are written by the motif slice of
/// BeatExtractionService's consolidated post-write call (via MotifLedgerService) and read back
/// as a "MOTIFS IN PLAY" generation-guidance block, the same surface shape as open threads.
/// </summary>
public class BookMotif
{
    public Guid Id { get; set; }

    /// <summary>The book node this motif belongs to.</summary>
    public Guid NodeId { get; set; }
    public Node? Node { get; set; }

    /// <summary>Normalized motif key — lowercase, trimmed (e.g. "the cracked credstick").
    /// Uniqueness per node is enforced by upsert on this key, not a DB constraint.</summary>
    public string MotifKey { get; set; } = "";

    /// <summary>Display form as first extracted.</summary>
    public string Display { get; set; } = "";

    /// <summary>How many beats the motif has been sighted in.</summary>
    public int Occurrences { get; set; }

    /// <summary>Beat of the first sighting (soft reference).</summary>
    public Guid? FirstBeatId { get; set; }

    /// <summary>Beat of the most recent sighting (soft reference).</summary>
    public Guid? LastBeatId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
