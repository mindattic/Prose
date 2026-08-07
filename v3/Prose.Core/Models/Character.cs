namespace Prose.Core.Models;

public record Character
{
    public string Name { get; init; } = "";
    public List<string> Aliases { get; init; } = [];
    public int Tier { get; init; }
    public string Status { get; init; } = "";
    public string Origin { get; init; } = "";
    public int Age { get; init; }
    public string Augmentation { get; init; } = "";
    public string Occupation { get; init; } = "";
    public string Affiliation { get; init; } = "";
    public List<CascadeOverride> CascadeOverrides { get; init; } = [];
    public List<Modifier> ActiveModifiers { get; init; } = [];
    public List<HistoryBeat> History { get; init; } = [];
    public List<Relationship> Relationships { get; init; } = [];
    public string VoiceNotes { get; init; } = "";
    public string SourceFile { get; init; } = "";
}

public record CascadeOverride
{
    public string From { get; init; } = "";
    public string To { get; init; } = "";
    public double ModifierValue { get; init; }
    public string Reason { get; init; } = "";
}

public record Modifier
{
    public string Name { get; init; } = "";
    public string Trigger { get; init; } = "";
    public double Intensity { get; init; }
    public string Duration { get; init; } = "";
    public string Decay { get; init; } = "";
    public bool Cascade { get; init; }
    public string Notes { get; init; } = "";
}

public record HistoryBeat
{
    public int Age { get; init; }
    public string Event { get; init; } = "";
}

public record Relationship
{
    public string Name { get; init; } = "";
    public string Status { get; init; } = "";
    public string Notes { get; init; } = "";
}
