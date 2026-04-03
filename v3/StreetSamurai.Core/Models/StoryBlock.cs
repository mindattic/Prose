using System.Text.Json.Serialization;

namespace StreetSamurai.Core.Models;

/// <summary>
/// A single paragraph-level block in a story. IDs are derived from the
/// parent StoryProject's prefix (e.g., RVN_00001, RVN_00002).
/// </summary>
public class StoryBlock
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("sequence")]
    public int Sequence { get; set; }

    [JsonPropertyName("text")]
    public string Text { get; set; } = "";

    [JsonPropertyName("locked")]
    public bool Locked { get; set; }

    [JsonPropertyName("created")]
    public DateTime Created { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("modified")]
    public DateTime Modified { get; set; } = DateTime.UtcNow;

    /// <summary>Split raw text into StoryBlocks using the given prefix and starting sequence.</summary>
    public static List<StoryBlock> FromText(string text, string prefix, int startSequence = 1)
    {
        var paragraphs = text
            .Split(["\n\n", "\r\n\r\n"], StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim())
            .Where(p => p.Length > 0)
            .ToList();

        return paragraphs.Select((p, i) =>
        {
            var seq = startSequence + i;
            return new StoryBlock
            {
                Id = $"{prefix}_{seq:D5}",
                Sequence = seq,
                Text = p,
            };
        }).ToList();
    }
}

/// <summary>
/// A chapter in a story. Each chapter has its own text and a synopsis
/// that summarizes it for context in subsequent chapters — this keeps
/// API token usage manageable for long stories.
/// </summary>
public class StoryChapter
{
    [JsonPropertyName("number")]
    public int Number { get; set; } = 1;

    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("text")]
    public string Text { get; set; } = "";

    /// <summary>Detailed chapter synopsis — every event, character, location, thread.</summary>
    [JsonPropertyName("synopsis")]
    public string Synopsis { get; set; } = "";

    /// <summary>Key plot beats in this chapter — 3-5 bullet points of major events.</summary>
    [JsonPropertyName("beats")]
    public string Beats { get; set; } = "";

    /// <summary>Perspective character for this chapter (if applicable).</summary>
    [JsonPropertyName("perspective")]
    public string Perspective { get; set; } = "";

    /// <summary>Characters that appear in this chapter — auto-detected from entity extraction.</summary>
    [JsonPropertyName("characters")]
    public List<string> Characters { get; set; } = [];

    /// <summary>Locations that appear in this chapter — auto-detected from entity extraction.</summary>
    [JsonPropertyName("locations")]
    public List<string> Locations { get; set; } = [];

    [JsonPropertyName("created")]
    public DateTime Created { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("modified")]
    public DateTime Modified { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// A story project — the container for chapters. Each project has chapters
/// that can be individually written, reviewed, and narrated. Synopses chain
/// chapters together without requiring the full text of previous chapters.
/// Designed for easy migration to a database: flat fields, no markdown parsing.
/// </summary>
public class StoryProject
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];

    [JsonPropertyName("prefix")]
    public string Prefix { get; set; } = "SS";

    [JsonPropertyName("title")]
    public string Title { get; set; } = "Untitled";

    [JsonPropertyName("mood")]
    public string? Mood { get; set; }

    [JsonPropertyName("location")]
    public string? Location { get; set; }

    [JsonPropertyName("characters")]
    public List<string> Characters { get; set; } = [];

    [JsonPropertyName("status")]
    public string Status { get; set; } = "draft";

    /// <summary>High-level story synopsis — the entire arc in 2-3 paragraphs. Updated as chapters are completed.</summary>
    [JsonPropertyName("story_synopsis")]
    public string StorySynopsis { get; set; } = "";

    /// <summary>Plot arc outline — the major turning points and where the story is heading.</summary>
    [JsonPropertyName("plot_arc")]
    public string PlotArc { get; set; } = "";

    /// <summary>Full structured outline — chapter-by-chapter breakdown with continuity checks. Updated by Outline button.</summary>
    [JsonPropertyName("outline")]
    public string Outline { get; set; } = "";

    [JsonPropertyName("created")]
    public DateTime Created { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("modified")]
    public DateTime Modified { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("chapters")]
    public List<StoryChapter> Chapters { get; set; } = [];

    // Legacy — kept for backward compat with existing story_blocks/*.json files
    [JsonPropertyName("blocks")]
    public List<StoryBlock> Blocks { get; set; } = [];

    /// <summary>Next available sequence number based on existing blocks.</summary>
    [JsonIgnore]
    public int NextSequence => Blocks.Count > 0 ? Blocks.Max(b => b.Sequence) + 1 : 1;

    /// <summary>Full story text — all chapters concatenated, falling back to blocks.</summary>
    [JsonIgnore]
    public string FullText => Chapters.Count > 0
        ? string.Join("\n\n", Chapters.OrderBy(c => c.Number).Select(c => c.Text).Where(t => t.Length > 0))
        : string.Join("\n\n", Blocks.OrderBy(b => b.Sequence).Select(b => b.Text));

    /// <summary>Ensure at least one chapter exists. Migrates legacy blocks if needed.</summary>
    public void EnsureChapters()
    {
        if (Chapters.Count > 0) return;

        // Migrate from legacy blocks
        var text = Blocks.Count > 0
            ? string.Join("\n\n", Blocks.OrderBy(b => b.Sequence).Select(b => b.Text))
            : "";

        Chapters.Add(new StoryChapter { Number = 1, Title = "Chapter 1", Text = text });
    }

    /// <summary>Add new blocks from generated text, auto-sequenced.</summary>
    public List<StoryBlock> AddBlocksFromText(string text)
    {
        var newBlocks = StoryBlock.FromText(text, Prefix, NextSequence);
        Blocks.AddRange(newBlocks);
        Modified = DateTime.UtcNow;
        return newBlocks;
    }

    /// <summary>Rebuild blocks from chapter text (for backward compat with services that use blocks).</summary>
    public void SyncBlocksFromChapters()
    {
        Blocks.Clear();
        var allText = string.Join("\n\n", Chapters.OrderBy(c => c.Number).Select(c => c.Text).Where(t => t.Length > 0));
        if (!string.IsNullOrWhiteSpace(allText))
            AddBlocksFromText(allText);
    }

    /// <summary>
    /// Build multi-level synopsis context for a chapter.
    /// Layers: story-level arc → chapter synopses → beat-level detail for recent chapters.
    /// </summary>
    public string GetPriorContext(int chapterNumber)
    {
        var parts = new List<string>();

        // Layer 1: Story-level synopsis (the big picture)
        if (!string.IsNullOrWhiteSpace(StorySynopsis))
            parts.Add($"STORY OVERVIEW:\n{StorySynopsis}");

        if (!string.IsNullOrWhiteSpace(PlotArc))
            parts.Add($"PLOT ARC:\n{PlotArc}");

        // Layer 2: Chapter synopses (older chapters get synopsis only)
        var priors = Chapters
            .Where(c => c.Number < chapterNumber)
            .OrderBy(c => c.Number)
            .ToList();

        if (priors.Count > 0)
        {
            var chapterLines = new List<string>();
            foreach (var c in priors)
            {
                var perspective = string.IsNullOrWhiteSpace(c.Perspective) ? "" : $" [POV: {c.Perspective}]";
                var synopsis = !string.IsNullOrWhiteSpace(c.Synopsis) ? c.Synopsis : "(no synopsis)";

                // Layer 3: Recent chapters get beat-level detail too
                var isRecent = c.Number >= chapterNumber - 2;
                if (isRecent && !string.IsNullOrWhiteSpace(c.Beats))
                    chapterLines.Add($"CHAPTER {c.Number} ({c.Title}){perspective}:\n{synopsis}\nKEY BEATS:\n{c.Beats}");
                else
                    chapterLines.Add($"CHAPTER {c.Number} ({c.Title}){perspective}: {synopsis}");
            }
            parts.Add(string.Join("\n\n", chapterLines));
        }

        return string.Join("\n\n---\n\n", parts);
    }

    /// <summary>Rename the prefix and update all block IDs.</summary>
    public void RenamePrefix(string newPrefix)
    {
        newPrefix = SanitizePrefix(newPrefix);
        Prefix = newPrefix;
        foreach (var block in Blocks)
            block.Id = $"{newPrefix}_{block.Sequence:D5}";
        Modified = DateTime.UtcNow;
    }

    public static string SanitizePrefix(string raw) =>
        new string(raw.Trim().ToUpperInvariant().Where(c => char.IsLetterOrDigit(c)).Take(6).ToArray());
}
