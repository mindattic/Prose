namespace Prose.Core.Models.Graph;

/// <summary>
/// An entity in the world graph with temporal state tracking.
/// Properties that change over time (status, location, affiliations) are tracked
/// via the PropertyHistory list — each entry records what changed and when.
/// </summary>
public record UniverseNode
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string NodeType { get; init; } = "";
    public string Status { get; init; } = "canon";
    public Dictionary<string, string> Properties { get; init; } = new();
    public string SourceFile { get; init; } = "";
    public string ExtractedFrom { get; init; } = "";

    // ── Temporal state ──
    /// <summary>History of property changes (e.g. status changed from "alive" to "dead" at chapter 12).</summary>
    public List<PropertyChange> History { get; init; } = [];

    /// <summary>Get the value of a property at a specific story point, falling back to current value.</summary>
    public string GetPropertyAt(string key, string storyPoint)
    {
        // Find the most recent change to this property that is <= storyPoint
        var change = History
            .Where(h => h.Property == key && string.Compare(h.StoryPoint, storyPoint, StringComparison.Ordinal) <= 0)
            .OrderByDescending(h => h.StoryPoint)
            .FirstOrDefault();
        return change?.NewValue ?? Properties.GetValueOrDefault(key, "");
    }
}

/// <summary>
/// A recorded change to a node's property. This is how we know that
/// "Sable was alive until chapter 12, then died" without deleting the alive state.
/// </summary>
public record PropertyChange
{
    /// <summary>Which property changed (e.g. "status", "location", "affiliation").</summary>
    public string Property { get; init; } = "";
    /// <summary>The old value.</summary>
    public string OldValue { get; init; } = "";
    /// <summary>The new value.</summary>
    public string NewValue { get; init; } = "";
    /// <summary>When in the story this happened (e.g. "chapter:12", "SS_00045").</summary>
    public string StoryPoint { get; init; } = "";
    /// <summary>Which story/source caused this change.</summary>
    public string Source { get; init; } = "";
    /// <summary>When this was recorded in the database.</summary>
    public DateTime RecordedAt { get; init; } = DateTime.UtcNow;
}
