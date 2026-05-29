using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Models.Canon;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Post-generation canon consistency checker.
/// Extracts named entities and relationship claims from any generated text,
/// resolves them against the Xref index, and auto-scaffolds stub CharacterData
/// records for anything not yet in the world — so canon grows to match the stories.
///
/// Design: never blocks generation. Call fire-and-forget after any text is finalized.
/// Works on stories, character descriptions, place entries, any generated content.
/// </summary>
public class CanonGroundingService
{
    private readonly ILlmService llm;
    private readonly XrefService xref;
    private readonly CharacterRepository characters;
    private readonly IPathProvider paths;
    private readonly ILogger<CanonGroundingService> log;

    public CanonGroundingService(
        ILlmService llm, XrefService xref, CharacterRepository characters,
        IPathProvider paths, ILogger<CanonGroundingService> log)
    {
        this.llm = llm;
        this.xref = xref;
        this.characters = characters;
        this.paths = paths;
        this.log = log;
    }

    /// <summary>
    /// Analyze text, resolve all named entities against canon, auto-scaffold stubs
    /// for unresolved character-type entities, and return the full grounding report.
    /// </summary>
    public async Task<CanonGroundingResult> AnalyzeAndScaffoldAsync(
        string text,
        string sourceContext = "",
        CancellationToken ct = default)
    {
        xref.EnsureBuilt();

        var extracted = await ExtractEntitiesAsync(text, sourceContext, ct);
        var result = new CanonGroundingResult { SourceContext = sourceContext };

        foreach (var entity in extracted)
        {
            var entry = xref.Resolve(entity.Name);
            if (entry != null)
            {
                result.Resolved.Add(new ResolvedReference
                {
                    Name = entity.Name,
                    EntityType = entry.Type,
                    EntityId = entry.Id
                });
            }
            else
            {
                if (entity.InferredType is "character" or "person" or "unknown")
                {
                    var stub = ScaffoldCharacter(entity);
                    characters.Save(stub);
                    entity.Scaffolded = true;
                    entity.ScaffoldedId = stub.Id;
                    result.EntitiesScaffolded++;
                    log.LogInformation(
                        "Canon grounding: scaffolded stub for '{Name}' (id={Id}, source='{Source}')",
                        entity.Name, stub.Id, sourceContext);
                }
                result.Unresolved.Add(entity);
            }
        }

        SaveLog(result);
        return result;
    }

    private async Task<List<ProposedEntity>> ExtractEntitiesAsync(
        string text, string context, CancellationToken ct)
    {
        const string system = """
            You are a canon consistency analyzer for a neo-noir worldbuilding project set in 2250s GLMZ.
            Extract all named entities and relationship claims from the provided text.

            Return a JSON array — no other text. Each element:
            {
              "name": "exact proper noun as written",
              "inferred_type": "character" | "place" | "faction" | "synthetic" | "technology" | "unknown",
              "context_snippet": "the sentence or clause where this name appears (max 120 chars)",
              "relationship_claims": ["nephew of Barber Vasquez", "works for Arcturus", ...]
            }

            Rules:
            - Named proper nouns only: people, places, organizations, machines with names
            - Skip common nouns, generic terms, and pronouns
            - Include every person mentioned by name, even briefly
            - Relationship claims describe how this entity relates to other named entities
            - Human-sounding names with no clear type → use "character"
            - Return an empty array [] if no named entities are found
            """;

        var user = string.IsNullOrWhiteSpace(context)
            ? text
            : $"Source context: {context}\n\n{text}";

        try
        {
            var response = await llm.GenerateAsync(system, user, 0.1, 4096, ct: ct);
            var json = response.Trim();
            json = JsonDefaults.StripCodeFences(json);

            var raw = JsonSerializer.Deserialize<List<ExtractedEntity>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return raw?.Select(e => new ProposedEntity
            {
                Name = e.Name ?? "",
                InferredType = e.InferredType ?? "unknown",
                ContextSnippet = e.ContextSnippet ?? "",
                RelationshipClaims = e.RelationshipClaims ?? []
            }).Where(e => e.Name.Length >= 2).ToList() ?? [];
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Canon grounding: entity extraction failed for source '{Context}'", context);
            return [];
        }
    }

    private static CharacterData ScaffoldCharacter(ProposedEntity entity)
    {
        var stub = new CharacterData
        {
            Name = entity.Name,
            Description = $"Auto-scaffolded. First mentioned: \"{entity.ContextSnippet}\"",
            Status = "alive",
            Tags = ["auto-scaffolded", "needs-review"]
        };

        foreach (var claim in entity.RelationshipClaims)
        {
            // Parse "X of Y" pattern to extract relationship type and counterpart
            var parts = claim.Split(" of ", 2, StringSplitOptions.TrimEntries);
            stub.Relationships.Add(new CharacterRelationship
            {
                Name = parts.Length == 2 ? parts[1] : "",
                Type = parts[0],
                Description = claim
            });
        }

        return stub;
    }

    private void SaveLog(CanonGroundingResult result)
    {
        try
        {
            var logsDir = Path.Combine(paths.EngineDataDir, "logs", "canon_grounding");
            Directory.CreateDirectory(logsDir);
            var fileName = $"{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.CreateVersion7().ToString("N")[..8]}.json";
            File.WriteAllText(
                Path.Combine(logsDir, fileName),
                JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Canon grounding: failed to save log");
        }
    }

    private sealed class ExtractedEntity
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("inferred_type")] public string? InferredType { get; set; }
        [JsonPropertyName("context_snippet")] public string? ContextSnippet { get; set; }
        [JsonPropertyName("relationship_claims")] public List<string>? RelationshipClaims { get; set; }
    }
}

public class CanonGroundingResult
{
    [JsonPropertyName("source_context")] public string SourceContext { get; set; } = "";
    [JsonPropertyName("analyzed_at")] public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
    [JsonPropertyName("resolved")] public List<ResolvedReference> Resolved { get; set; } = [];
    [JsonPropertyName("unresolved")] public List<ProposedEntity> Unresolved { get; set; } = [];
    [JsonPropertyName("entities_scaffolded")] public int EntitiesScaffolded { get; set; }
}

public class ResolvedReference
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("entity_type")] public string EntityType { get; set; } = "";
    [JsonPropertyName("entity_id")] public string EntityId { get; set; } = "";
}

public class ProposedEntity
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("inferred_type")] public string InferredType { get; set; } = "";
    [JsonPropertyName("context_snippet")] public string ContextSnippet { get; set; } = "";
    [JsonPropertyName("relationship_claims")] public List<string> RelationshipClaims { get; set; } = [];
    [JsonPropertyName("scaffolded")] public bool Scaffolded { get; set; }
    [JsonPropertyName("scaffolded_id")] public string? ScaffoldedId { get; set; }
}
