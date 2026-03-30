using QuikGraph;

namespace StreetSamurai.Core.Models.Graph;

public record WorldEdge : IEdge<string>
{
    public string Source { get; init; } = "";
    public string Target { get; init; } = "";
    public string RelationType { get; init; } = "";
    public double Weight { get; init; } = 1.0;
    public string Sentiment { get; init; } = "neutral";
    public string Description { get; init; } = "";
    public string CanonStatus { get; init; } = "canon";
    public DateTime LastModified { get; init; } = DateTime.UtcNow;
    public string ModifiedBy { get; init; } = "";
}
