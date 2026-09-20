namespace Prose.Core.Models;

public record Corponation
{
    public int Number { get; init; }
    public string Name { get; init; } = "";
    public string Sector { get; init; } = "";
    public string Valuation { get; init; } = "";
    public string Origin { get; init; } = "";
    public string Territory { get; init; } = "";
    public string SecurityForce { get; init; } = "";
    public string KeyDetail { get; init; } = "";
    public string RelationshipToBig20 { get; init; } = "";
    public string SourceFile { get; init; } = "";
    public string FullText { get; init; } = "";
}
