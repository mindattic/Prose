using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

public class TextAnalysisService
{
    private readonly ILlmService llm;
    private readonly LoreService canon;
    private readonly WorldGraphService graph;

    public TextAnalysisService(ILlmService llm, LoreService canon, WorldGraphService graph)
    {
        this.llm = llm;
        this.canon = canon;
        this.graph = graph;
    }

    public async Task<string> LoreCheckAsync(string selectedText, string surroundingContext, CancellationToken ct = default)
    {
        var canonContext = BuildCanonContext(selectedText);
        var system = $"""
            You are a lore consistency checker for GLMZ (Great Lakes Metropolitan Zone, also called The Glooms, year 2226).
            You have access to the following canon information:

            {canonContext}

            Analyze the selected text and determine if it matches established canon.
            If there are contradictions, list them specifically with what the canon says.
            If the text introduces new elements not in canon, note them as "new — needs review".
            Be specific and cite sources.
            """;
        var user = $"SURROUNDING CONTEXT:\n{surroundingContext}\n\nSELECTED TEXT TO CHECK:\n{selectedText}";
        return await llm.GenerateAsync(system, user, 0.3, 2048, ct: ct);
    }

    public async Task<string> ClicheCheckAsync(string selectedText, CancellationToken ct = default)
    {
        var system = """
            You are a literary quality checker for a neo-noir novel. The story has strict prohibitions:
            - No generic noir narration
            - No trailer lines or slogans
            - No katana as power fantasy (always a moral problem)
            - No action-movie pacing
            - No samurai cliches or anime dialogue
            - No monologuing about honor
            - No characters explaining their own psychology
            - No clean moral victories
            - Every paragraph must contain an action, sensory detail, or a lie

            Analyze the text for violations. Be specific about what's cliche and why.
            Suggest concrete improvements that maintain the dark, literary tone.
            """;
        return await llm.GenerateAsync(system, selectedText, 0.4, 2048, ct: ct);
    }

    public async Task<string> ExpandAsync(string selectedText, string surroundingContext, CancellationToken ct = default)
    {
        var canonContext = BuildCanonContext(selectedText);
        var system = $"""
            You are continuing a neo-noir novel set in GLMZ (Great Lakes Metropolitan Zone, 2226). Maintain the exact same voice,
            tone, and style. Follow these rules strictly:
            - Every paragraph: action, sensory detail, or a lie
            - No generic noir, no slogans, no samurai cliches
            - Sharp sensory detail, emotional subtext

            World context:
            {canonContext}
            """;
        var user = $"CONTEXT:\n{surroundingContext}\n\nCONTINUE FROM:\n{selectedText}\n\nWrite 2-3 paragraphs continuing this passage.";
        return await llm.GenerateAsync(system, user, 0.85, 2048, ct: ct);
    }

    public async Task<string> RephraseAsync(string selectedText, CancellationToken ct = default)
    {
        var system = """
            Rephrase the following text while maintaining its meaning, tone, and literary quality.
            Rules: sharp sensory detail, no cliches, no generic noir.
            Return ONLY the rephrased text, nothing else.
            """;
        return await llm.GenerateAsync(system, selectedText, 0.7, 1024, ct: ct);
    }

    private string BuildCanonContext(string text)
    {
        var results = canon.Search(text, 5);
        var graphResults = graph.Search(text);

        var lines = new List<string>();
        foreach (var r in results)
            lines.Add($"[{r.FileName}:{r.LineNumber}] {r.Context}");
        foreach (var n in graphResults.Take(5))
            lines.Add($"[GRAPH: {n.NodeType}] {graph.GetContextForNode(n.Id)}");

        return string.Join("\n\n", lines);
    }
}
