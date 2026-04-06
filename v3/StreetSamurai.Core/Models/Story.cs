namespace StreetSamurai.Core.Models;

public record Story
{
    public string Id { get; init; } = Guid.CreateVersion7().ToString("N")[..8];
    public string Title { get; init; } = "Untitled";
    public string Status { get; init; } = "draft";
    public List<string> Characters { get; init; } = [];
    public string? Location { get; init; }
    public DateTime Created { get; init; } = DateTime.UtcNow;
    public DateTime Modified { get; init; } = DateTime.UtcNow;
    public List<string> Tags { get; init; } = [];
    public string MarkdownContent { get; init; } = "";
    public string FilePath { get; init; } = "";
}
