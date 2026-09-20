namespace Prose.Core.Data.Entities;

/// <summary>
/// Records that a named entity appears in a beat's prose. Populated by
/// <c>EntityRamificationService.IndexBeatMentionsAsync</c> after every beat write.
/// When an entity is updated, the service queries this table to find every beat
/// that mentions it and flags those beats <see cref="Beat.EntityStale"/>.
/// </summary>
public class BeatEntityMention
{
    public Guid BeatId     { get; set; }
    public Guid EntityId   { get; set; }
    public string EntityName { get; set; } = "";
    public string EntityType { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Beat?   Beat   { get; set; }
    public Entity? Entity { get; set; }
}
