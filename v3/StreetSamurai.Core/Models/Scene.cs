namespace StreetSamurai.Core.Models;

public record Scene
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N")[..8];
    public string Goal { get; init; } = "";
    public string? Location { get; init; }
    public List<string> Entities { get; init; } = [];
    public List<string> Themes { get; init; } = [];
    public List<Beat> Beats { get; init; } = [];
    public string CanonStatus { get; init; } = "draft";
    public ValidationReport? Validation { get; init; }
    public DateTime Generated { get; init; } = DateTime.UtcNow;
    public string SourceDir { get; init; } = "";

    public string FullText => string.Join("\n\n", Beats.Select(b => b.Text));
}

public record Beat
{
    public int Index { get; init; }
    public string Goal { get; init; } = "";
    public string LeadFacet { get; init; } = "";
    public List<string> SupportingFacets { get; init; } = [];
    public string Text { get; init; } = "";
    public List<string> ContextTags { get; init; } = [];
}
