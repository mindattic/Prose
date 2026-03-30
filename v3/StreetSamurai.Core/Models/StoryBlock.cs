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
/// A story project — the container for blocks. Each project has a unique prefix
/// that namespaces its blocks (e.g., "RVN" → RVN_00001, RVN_00002).
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

    [JsonPropertyName("created")]
    public DateTime Created { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("modified")]
    public DateTime Modified { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("blocks")]
    public List<StoryBlock> Blocks { get; set; } = [];

    /// <summary>Next available sequence number based on existing blocks.</summary>
    [JsonIgnore]
    public int NextSequence => Blocks.Count > 0 ? Blocks.Max(b => b.Sequence) + 1 : 1;

    /// <summary>Full story text — all blocks concatenated.</summary>
    [JsonIgnore]
    public string FullText => string.Join("\n\n", Blocks.OrderBy(b => b.Sequence).Select(b => b.Text));

    /// <summary>Rename the prefix and update all block IDs.</summary>
    public void RenamePrefix(string newPrefix)
    {
        newPrefix = SanitizePrefix(newPrefix);
        Prefix = newPrefix;
        foreach (var block in Blocks)
            block.Id = $"{newPrefix}_{block.Sequence:D5}";
        Modified = DateTime.UtcNow;
    }

    /// <summary>Add new blocks from generated text, auto-sequenced.</summary>
    public List<StoryBlock> AddBlocksFromText(string text)
    {
        var newBlocks = StoryBlock.FromText(text, Prefix, NextSequence);
        Blocks.AddRange(newBlocks);
        Modified = DateTime.UtcNow;
        return newBlocks;
    }

    public static string SanitizePrefix(string raw) =>
        new string(raw.Trim().ToUpperInvariant().Where(c => char.IsLetterOrDigit(c)).Take(6).ToArray());
}
