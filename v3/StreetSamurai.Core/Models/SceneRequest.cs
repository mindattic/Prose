namespace StreetSamurai.Core.Models;

public record SceneRequest
{
    public string Goal { get; init; } = "";
    public string? Location { get; init; }
    public List<string> Characters { get; init; } = [];
    public List<string> Themes { get; init; } = [];
    public int NumBeats { get; init; } = 5;
    public string? ForcedLeadFacet { get; init; }
}

public record GeneratedScene
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N")[..8];
    public SceneRequest Request { get; init; } = new();
    public List<GeneratedBeat> Beats { get; init; } = [];
    public DateTime Generated { get; init; } = DateTime.UtcNow;
    public string FullText => string.Join("\n\n", Beats.Select(b => b.Text));
}

public record GeneratedBeat
{
    public int Index { get; init; }
    public string Goal { get; init; } = "";
    public string LeadFacet { get; init; } = "";
    public List<string> SupportingFacets { get; init; } = [];
    public string Text { get; init; } = "";
    public List<string> ContextTags { get; init; } = [];
}

public record BeatGenerationProgress
{
    public int BeatIndex { get; init; }
    public int TotalBeats { get; init; }
    public string LeadFacet { get; init; } = "";
    public string Status { get; init; } = "";
}
