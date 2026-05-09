namespace StreetSamurai.Core.Data.Entities;

/// <summary>
/// Polymorphic embedding cache for prose units — chapters AND beats live here
/// keyed by <c>(ScopeKind, ScopeId)</c>. Mirrors <see cref="EntityEmbedding"/>
/// in shape (drift hash, VECTOR(1536), drift-skipped re-embed) but for
/// prose retrieval rather than entity retrieval. Unified table avoids two
/// near-duplicate schemas and lets a future "scene" or "dialogue" scope
/// land without a migration.
///
/// <para><b>Granularity rationale</b> (Legion 4-voter quorum 2026-05-08):
/// chapter-level for thematic / arc retrieval (whole-book Q&amp;A,
/// "what chapters dealt with the rogue-AI thread?"), beat-level for prose
/// / voice anchoring (BeatGeneratorService pulling similar past beats as
/// in-context style examples). The table holds both; consumers filter by
/// <see cref="ScopeKind"/> via <c>FindSimilarAsync</c> overloads.</para>
///
/// <para><b>Why no FK.</b> ScopeId can point at <see cref="Chapter"/>.Id
/// when ScopeKind='chapter' or <see cref="ChapterBeat"/>.BeatGuid when
/// ScopeKind='beat' — two different parent tables. EF can't model that
/// natively; <see cref="EmbeddingService"/> validates the link in
/// application code instead.</para>
/// </summary>
public class ProseEmbedding
{
    /// <summary>'chapter' or 'beat' (lower-case slug; new scopes added by string).</summary>
    public string ScopeKind { get; set; } = "";

    /// <summary>Chapter.Id when ScopeKind='chapter'; ChapterBeat.BeatGuid when ScopeKind='beat'.</summary>
    public Guid ScopeId { get; set; }

    /// <summary>SHA-256 of source prose; cheap drift detection.</summary>
    public byte[] SourceHash { get; set; } = Array.Empty<byte>();

    /// <summary>1536 for OpenAI text-embedding-3-small.</summary>
    public int Dimensions { get; set; }

    public DateTime EmbeddedAt { get; set; }

    public string Model { get; set; } = "text-embedding-3-small";

    // The Vector column lives in SQL as VECTOR(1536) and is managed via raw
    // SQL by EmbeddingService — same pattern as EntityEmbedding. EF doesn't
    // need to map it because we never read/write it via change tracking.
}
