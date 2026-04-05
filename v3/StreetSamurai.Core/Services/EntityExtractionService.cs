using System.Text.Json;
using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Models.Graph;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Extracts entities (characters, places, weapons, organizations, etc.) and their
/// relationships from story text using the LLM. Results are structured as graph
/// nodes and edges ready for merging into the world graph.
/// </summary>
public class EntityExtractionService
{
    private readonly ILlmService llm;
    private readonly WorldGraphService graph;

    public EntityExtractionService(ILlmService llm, WorldGraphService graph)
    {
        this.llm = llm;
        this.graph = graph;
    }

    /// <summary>
    /// Extract entities and relationships from story text.
    /// </summary>
    public async Task<ExtractionResult> ExtractAsync(string storyText, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(storyText)) return new ExtractionResult();

        // Provide existing entity names so the LLM can match rather than duplicate
        var existingNames = graph.AllNodes()
            .Select(n => $"{n.Name} ({n.NodeType})")
            .Take(100);
        var existingContext = string.Join("\n", existingNames);

        var system = """
            You are an entity extraction engine for a cyberpunk fiction world database.
            You read story prose and identify every named entity and relationship.

            ENTITY TYPES (use exactly these):
            - character: any named person (protagonist, NPC, mentioned person)
            - place: any named location (district, building, room, street, city)
            - organization: any corporation, company, institution
            - faction: any gang, group, crew, militia, movement
            - weapon: any named weapon or weapon type
            - equipment: any gear, tool, implant, augmentation, device
            - technology: any named tech, software, system, protocol
            - event: any named incident, battle, operation, historical event
            - fact: any world rule, detail, or established truth
            - lore: any history, legend, backstory element

            RULES:
            - Extract ONLY entities explicitly named or described in the text
            - Do NOT invent entities not present in the text
            - If an entity matches an existing one in the database, use the EXACT same name
            - For properties, include only what the text reveals (description, role, status, etc.)
            - For relationships, identify how entities relate to each other in this text
            - Sentiment: "positive", "negative", "neutral", or "mixed"
            - Be thorough — even brief mentions count

            RESPOND WITH ONLY VALID JSON matching this schema:
            {
              "entities": [
                {
                  "name": "Entity Name",
                  "type": "character|place|organization|faction|weapon|equipment|technology|event|fact|lore",
                  "description": "Brief description from the text",
                  "properties": { "key": "value" }
                }
              ],
              "relationships": [
                {
                  "source": "Entity A Name",
                  "target": "Entity B Name",
                  "type": "relationship type (e.g. ally_of, located_in, wields, member_of)",
                  "description": "How they relate in this text",
                  "sentiment": "positive|negative|neutral|mixed"
                }
              ]
            }
            """;

        var user = $"""
            EXISTING ENTITIES IN THE DATABASE (match these names if applicable):
            {existingContext}

            STORY TEXT TO EXTRACT FROM:
            {storyText}
            """;

        var response = await llm.GenerateAsync(system, user, 0.2, 4096, ct: ct);
        return ParseExtraction(response);
    }

    /// <summary>
    /// Extract entities from story text and merge them into the world graph.
    /// Returns the number of new entities and relationships added.
    /// storyPoint identifies where in the story this happened (e.g. "chapter:3" or "SS_00045").
    /// </summary>
    public async Task<(int entities, int relationships)> ExtractAndMergeAsync(
        string storyText, string storyId, string storyPoint = "", CancellationToken ct = default)
    {
        var result = await ExtractAsync(storyText, ct);
        return MergeIntoGraph(result, storyId, storyPoint);
    }

    /// <summary>
    /// Merge extraction results into the world graph.
    /// </summary>
    public (int entities, int relationships) MergeIntoGraph(ExtractionResult result, string storyId, string storyPoint = "")
    {
        graph.EnsureLoaded();
        int newEntities = 0, newRelationships = 0;

        // Merge entities
        foreach (var entity in result.Entities)
        {
            var id = WorldGraphService.Slugify(entity.Name);
            if (string.IsNullOrEmpty(id)) continue;

            var nodeType = EntityTypes.Normalize(entity.Type);
            var existing = graph.GetNode(id);

            if (existing != null)
            {
                // Merge properties — track changes temporally for key properties
                var mergedProps = new Dictionary<string, string>(existing.Properties);
                var trackableProps = new HashSet<string> { "status", "location", "affiliation" };

                foreach (var (key, value) in entity.Properties)
                {
                    if (string.IsNullOrEmpty(value)) continue;
                    var oldVal = mergedProps.GetValueOrDefault(key, "");

                    if (trackableProps.Contains(key) && oldVal.Length > 0 && oldVal != value && !string.IsNullOrEmpty(storyPoint))
                    {
                        // Temporal change — record in history
                        graph.RecordPropertyChange(id, key, value, storyPoint, storyId);
                    }
                    else if (!mergedProps.ContainsKey(key))
                    {
                        mergedProps[key] = value;
                    }
                }

                // Update description if the existing one is empty
                if (!mergedProps.ContainsKey("description") && !string.IsNullOrEmpty(entity.Description))
                    mergedProps["description"] = entity.Description;

                // Update node type if it was "unknown"
                var updatedType = existing.NodeType == EntityTypes.Unknown ? nodeType : existing.NodeType;

                graph.AddNode(existing with { NodeType = updatedType, Properties = mergedProps });
            }
            else
            {
                // New entity
                var props = new Dictionary<string, string>(entity.Properties);
                if (!string.IsNullOrEmpty(entity.Description))
                    props["description"] = entity.Description;

                graph.AddNode(new WorldNode
                {
                    Id = id,
                    Name = entity.Name,
                    NodeType = nodeType,
                    Status = "extracted",
                    Properties = props,
                    ExtractedFrom = storyId,
                });
                newEntities++;
            }
        }

        // Merge relationships
        foreach (var rel in result.Relationships)
        {
            var sourceId = WorldGraphService.Slugify(rel.Source);
            var targetId = WorldGraphService.Slugify(rel.Target);
            if (string.IsNullOrEmpty(sourceId) || string.IsNullOrEmpty(targetId)) continue;
            if (sourceId == targetId) continue;

            // Ensure both nodes exist (create stubs if not)
            if (graph.GetNode(sourceId) == null)
            {
                graph.AddNode(new WorldNode
                {
                    Id = sourceId, Name = rel.Source,
                    NodeType = EntityTypes.Unknown, Status = "extracted",
                    ExtractedFrom = storyId,
                });
                newEntities++;
            }

            if (graph.GetNode(targetId) == null)
            {
                graph.AddNode(new WorldNode
                {
                    Id = targetId, Name = rel.Target,
                    NodeType = EntityTypes.Unknown, Status = "extracted",
                    ExtractedFrom = storyId,
                });
                newEntities++;
            }

            graph.EvolveRelationship(sourceId, targetId, storyId,
                rel.Type, rel.Description, 1.0, rel.Sentiment, storyPoint);
            newRelationships++;
        }

        graph.Save();
        return (newEntities, newRelationships);
    }

    private static ExtractionResult ParseExtraction(string response)
    {
        // Strip markdown code fences if present
        var json = response.Trim();
        if (json.StartsWith("```"))
        {
            var firstNewline = json.IndexOf('\n');
            if (firstNewline > 0) json = json[(firstNewline + 1)..];
            if (json.EndsWith("```")) json = json[..^3];
            json = json.Trim();
        }

        try
        {
            return JsonSerializer.Deserialize<ExtractionResult>(json, JsonDefaults.LlmParsing) ?? new ExtractionResult();
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "Entity extraction failed — LLM returned malformed JSON");
            return new ExtractionResult();
        }
    }
}
