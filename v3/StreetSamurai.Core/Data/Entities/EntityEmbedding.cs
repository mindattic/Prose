namespace StreetSamurai.Core.Data.Entities;

/// <summary>
/// Cached cloud-LLM embedding for an entity's canonical text. One row per
/// <see cref="Entity"/> — semantic-search index + drift detector.
///
/// <para><b>Why a separate non-temporal table.</b> SQL Server 2025 vector
/// indexes are incompatible with system-versioned tables (vector index
/// maintenance conflicts with the temporal history-row insertion mechanism;
/// no official Microsoft documentation supports the combination). Keeping
/// embeddings on their own non-temporal table sidesteps that constraint
/// while leaving the canonical <see cref="Entity"/> + <see cref="Record"/>
/// rows free to remain system-versioned for editor audit.</para>
///
/// <para><b>Drift detection.</b> Each row carries a SHA-256 hash of the
/// source text the vector was computed from. On save, recompute the hash;
/// if it doesn't match, re-embed. No "stale embedding" surprises.</para>
///
/// <para><b>Indexing strategy.</b> Below 50,000 vectors, exact NN via
/// <c>VECTOR_DISTANCE()</c> is fast enough and avoids the
/// <c>PREVIEW_FEATURES</c> rough edges around DiskANN. Add the vector
/// index when the corpus crosses that threshold.</para>
/// </summary>
public class EntityEmbedding
{
    /// <summary>FK <c>Entities.Id</c>; also the PK so each entity has at most one current embedding.</summary>
    public Guid EntityId { get; set; }

    /// <summary>SHA-256 of the source text the vector was computed from. 32 bytes.</summary>
    public byte[] SourceHash { get; set; } = Array.Empty<byte>();

    // The actual embedding lives in a [Vector] VECTOR(1536) column on the
    // table — server-side, queryable via VECTOR_DISTANCE('cosine', ...). EF
    // Core has no native VECTOR mapping yet, so we deliberately omit a
    // matching C# property here: <see cref="EmbeddingService"/> manages the
    // column entirely via raw SQL (MERGE for writes, SqlQueryRaw with
    // VECTOR_DISTANCE for reads). EF only tracks the metadata columns so
    // change-tracking and migrations stay simple.

    /// <summary>Number of dimensions (1536 for OpenAI text-embedding-3-small).</summary>
    public int Dimensions { get; set; }

    /// <summary>UTC timestamp the embedding was last generated.</summary>
    public DateTime EmbeddedAt { get; set; }

    /// <summary>Embedding model id, so a column-wide re-embed isn't needed when the model changes mid-corpus.</summary>
    public string Model { get; set; } = "text-embedding-3-small";

    public Entity? Entity { get; set; }
}
