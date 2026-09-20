namespace Prose.Core.Data.Entities;

/// <summary>
/// Single-document settings table — replaces the four legacy singleton JSON files
/// (tone bible, story bible, literary rules, character profile). Each row is one
/// canonical settings record keyed by a type-derived <see cref="Key"/>; <see cref="Json"/>
/// holds the full document. Updated atomically on save.
/// </summary>
public class Setting
{
    public string Key { get; set; } = "";

    /// <summary>The universe this config row belongs to (part of the composite key, so the same
    /// Key recurs per universe). Operational/shared keys (LLM routing, TTS, user accounts) carry
    /// <see cref="Universe.SharedId"/> and are visible from every universe (RFC 0006).</summary>
    public Guid UniverseId { get; set; }

    public string Json { get; set; } = "";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
