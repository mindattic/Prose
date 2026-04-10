using System.Text.Json;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

public class ContextAnalyzerService
{
    private readonly ILlmService llm;
    private readonly WorldGraphService graph;

    public ContextAnalyzerService(ILlmService llm, WorldGraphService graph)
    {
        this.llm = llm;
        this.graph = graph;
    }

    public async Task<ContextAnalysis> AnalyzeAsync(string sceneContext, List<string> characterIds, CancellationToken ct = default)
    {
        // Pull full entity briefs from the graph — includes gender, pronouns,
        // psychology, relationships, equipment, and 1-hop neighbors
        var relationshipContext = string.Join("\n\n", characterIds
            .Select(id => graph.GetEntityBrief(id))
            .Where(c => !string.IsNullOrEmpty(c)));

        var system = """
            You are a psychological context analyzer for a neo-noir narrative engine.
            Given a scene description and character relationships, extract:
            1. psychological_triggers: tags that activate character facets (e.g., "violence", "betrayal", "moral_choice")
            2. dominant_emotion: the primary emotional tone
            3. stakes: what's at risk in this scene
            4. tension_source: where the conflict comes from

            Respond in JSON format ONLY:
            {"psychological_triggers": ["tag1", "tag2"], "dominant_emotion": "string", "stakes": "string", "tension_source": "string"}
            """;

        var user = $"SCENE:\n{sceneContext}\n\nRELATIONSHIPS:\n{relationshipContext}";

        var response = await llm.GenerateAsync(system, user, 0.3, 1024, ct: ct);

        try
        {
            return JsonSerializer.Deserialize<ContextAnalysis>(response,
                JsonDefaults.LlmParsing) ?? new();
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "Context analysis failed, returning default");
            return new ContextAnalysis { PsychologicalTriggers = ["unknown"] };
        }
    }
}

public record ContextAnalysis
{
    public List<string> PsychologicalTriggers { get; init; } = [];
    public string DominantEmotion { get; init; } = "";
    public string Stakes { get; init; } = "";
    public string TensionSource { get; init; } = "";
}
