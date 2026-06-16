namespace StreetSamurai.Core.Models;

public record WorldRules
{
    public LiteraryRules Literary { get; init; } = new();
    public StoryBible Bible { get; init; } = new();
    public List<Motif> Motifs { get; init; } = [];
}

public record LiteraryRules
{
    public int SentenceMaxWords { get; init; } = 25;
    public List<string> ParagraphRequirements { get; init; } = [];
    public List<string> Prohibitions { get; init; } = [];
    public StructuralRules Structural { get; init; } = new();
}

public record StructuralRules
{
    public string Pov { get; init; } = "";
    public string Location { get; init; } = "";
    public string Choice { get; init; } = "";
    public string Consequence { get; init; } = "";
    public string Ending { get; init; } = "";
    public string Pace { get; init; } = "";
}

public record StoryBible
{
    public string Title { get; init; } = "";
    public string Genre { get; init; } = "";
    public string Tone { get; init; } = "";
    public string CoreTheme { get; init; } = "";
    public string CoreHook { get; init; } = "";
}

public record Motif
{
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";
    public List<MotifAppearance> Appearances { get; init; } = [];
}

public record MotifAppearance
{
    public int Scene { get; init; }
    public string Meaning { get; init; } = "";
}
