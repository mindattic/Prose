namespace StreetSamurai.Core.Models;

public record CanonDocument
{
    public string FileName { get; init; } = "";
    public string Title { get; init; } = "";
    public string Category { get; init; } = "";
    public int LineCount { get; init; }
    public string FullPath { get; init; } = "";
}

public record SearchResult
{
    public string FileName { get; init; } = "";
    public string Heading { get; init; } = "";
    public int LineNumber { get; init; }
    public string Context { get; init; } = "";
    public double Relevance { get; init; }
}

public record CanonChunk
{
    public string Text { get; init; } = "";
    public string Source { get; init; } = "";
    public string Heading { get; init; } = "";
    public int ChunkIndex { get; init; }
}

public record ValidationReport
{
    public string Status { get; init; } = "draft"; // green, yellow, red
    public List<Contradiction> Contradictions { get; init; } = [];
    public List<NewEntity> NewEntities { get; init; } = [];
    public List<ToneViolation> ToneViolations { get; init; } = [];
    public string Summary { get; init; } = "";
}

public record Contradiction
{
    public string Claim { get; init; } = "";
    public string Canon { get; init; } = "";
    public string Source { get; init; } = "";
    public string Severity { get; init; } = "";
}

public record NewEntity
{
    public string Name { get; init; } = "";
    public string Type { get; init; } = "";
    public string Context { get; init; } = "";
}

public record ToneViolation
{
    public string Issue { get; init; } = "";
    public string Excerpt { get; init; } = "";
}

public record CanonQueueEntry
{
    public string Name { get; init; } = "";
    public string Type { get; init; } = "";
    public string Context { get; init; } = "";
    public string SourceScene { get; init; } = "";
    public DateTime Submitted { get; init; }
    public string Status { get; init; } = "pending";
    public string Notes { get; init; } = "";
    public string FilePath { get; init; } = "";
}
