using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Models;
using StreetSamurai.Core.Models.Canon;

namespace StreetSamurai.Core.Services;

/// <summary>
/// LLM-driven extractor for the two character-side fields the dossier has been
/// designed for but the existing ContinuityExtractionService doesn't fill:
/// <see cref="CharacterKnowledge"/> (what a character learned and when) and
/// <see cref="CharacterCondition"/> (addictions, allergies, prescriptions, chronic
/// conditions, injuries). Operates per-beat so the chapter cursor on each finding
/// stays accurate.
/// </summary>
public class BeatFactExtractionService
{
    private readonly ILlmService llm;
    private readonly ILogger<BeatFactExtractionService> log;

    public BeatFactExtractionService(ILlmService llm, ILogger<BeatFactExtractionService> log)
    {
        this.llm = llm;
        this.log = log;
    }

    public async Task<BeatFactExtractionResult> ExtractAsync(
        Chapter chapter,
        ChapterBeat beat,
        IReadOnlyList<string> characterNames,
        CancellationToken ct = default)
    {
        var result = new BeatFactExtractionResult();
        if (characterNames.Count == 0) return result;
        if (string.IsNullOrWhiteSpace(beat.Text) && string.IsNullOrWhiteSpace(beat.Synopsis)) return result;

        var system = """
            You extract two kinds of facts from a beat of cyberpunk fiction:

            1) KNOWLEDGE — a discrete thing a named character learned or knew at this beat.
               Examples: "Kyle learned Hua's tab is 24 hours overdue", "Sasha knows the strop charges Silence".
            2) CONDITIONS — persistent body/mind states a named character has at this beat.
               Examples: { kind: "addiction", name: "Methylin", severity: "moderate" }, { kind: "injury", name: "left wrist sprain", severity: "mild" }.
               Kinds: addiction, allergy, prescription, chronic, mental, injury.

            Return STRICT JSON: { "knowledge": [...], "conditions": [...] } — no prose, no fences.
            Each knowledge item: { character, topic, summary, entities? }
            Each condition item: { character, kind, name, severity?, notes? }
            Use the character's exact name as it appears. If nothing applies, return empty arrays.
            Do NOT invent facts. Only extract what the text plainly says or directly implies.
            """;

        var sb = new StringBuilder();
        sb.AppendLine($"CHAPTER {chapter.Number?.ToString() ?? "?"} \"{chapter.Title}\"");
        sb.AppendLine($"BEAT {beat.Index}: {beat.Title}");
        if (!string.IsNullOrWhiteSpace(beat.Synopsis)) sb.AppendLine($"SYNOPSIS: {beat.Synopsis}");
        sb.AppendLine();
        sb.AppendLine("CHARACTERS IN SCOPE:");
        foreach (var n in characterNames) sb.AppendLine($"  - {n}");
        sb.AppendLine();
        sb.AppendLine("BEAT TEXT:");
        sb.AppendLine(beat.Text);

        string raw;
        try { raw = await llm.GenerateAsync(system, sb.ToString(), 0.1, 1500, ct: ct); }
        catch (Exception ex) { log.LogWarning(ex, "Beat fact extraction LLM call failed"); return result; }

        var json = StripFences(raw);
        ExtractedPayload? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<ExtractedPayload>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (Exception ex) { log.LogDebug(ex, "Beat fact JSON parse failed: {Json}", json); return result; }
        if (parsed == null) return result;

        var asOfChapter = chapter.Number;
        foreach (var k in parsed.Knowledge ?? new())
        {
            if (string.IsNullOrWhiteSpace(k.Character) || string.IsNullOrWhiteSpace(k.Topic)) continue;
            result.Knowledge.Add((k.Character, new CharacterKnowledge
            {
                Topic            = k.Topic,
                Summary          = k.Summary ?? "",
                LearnedChapter   = asOfChapter,
                LearnedChapterId = chapter.Id,
                SourceBeat       = beat.Index,
                SourceSnippet    = string.IsNullOrEmpty(beat.Synopsis) ? Truncate(beat.Text, 240) : beat.Synopsis,
                Entities         = k.Entities ?? new(),
            }));
        }

        foreach (var c in parsed.Conditions ?? new())
        {
            if (string.IsNullOrWhiteSpace(c.Character) || string.IsNullOrWhiteSpace(c.Name) || string.IsNullOrWhiteSpace(c.Kind)) continue;
            result.Conditions.Add((c.Character, new CharacterCondition
            {
                Kind         = c.Kind,
                Name         = c.Name,
                Severity     = c.Severity ?? "",
                Notes        = c.Notes ?? "",
                SinceChapter = asOfChapter,
            }));
        }

        return result;
    }

    private static string StripFences(string s)
    {
        var t = s.Trim();
        if (t.StartsWith("```")) t = t[(t.IndexOf('\n') + 1)..];
        if (t.EndsWith("```")) t = t[..^3].TrimEnd();
        return t;
    }

    private static string Truncate(string s, int n) => string.IsNullOrEmpty(s) || s.Length <= n ? s ?? "" : s[..(n - 1)] + "…";

    private sealed class ExtractedPayload
    {
        [JsonPropertyName("knowledge")] public List<KnowledgeRow>? Knowledge { get; set; }
        [JsonPropertyName("conditions")] public List<ConditionRow>? Conditions { get; set; }
    }

    private sealed class KnowledgeRow
    {
        [JsonPropertyName("character")] public string? Character { get; set; }
        [JsonPropertyName("topic")] public string? Topic { get; set; }
        [JsonPropertyName("summary")] public string? Summary { get; set; }
        [JsonPropertyName("entities")] public List<string>? Entities { get; set; }
    }

    private sealed class ConditionRow
    {
        [JsonPropertyName("character")] public string? Character { get; set; }
        [JsonPropertyName("kind")] public string? Kind { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("severity")] public string? Severity { get; set; }
        [JsonPropertyName("notes")] public string? Notes { get; set; }
    }
}

public sealed class BeatFactExtractionResult
{
    public List<(string CharacterName, CharacterKnowledge Item)> Knowledge { get; } = new();
    public List<(string CharacterName, CharacterCondition Item)> Conditions { get; } = new();
}
