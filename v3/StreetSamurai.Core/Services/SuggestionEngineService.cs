using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Models;

namespace StreetSamurai.Core.Services;

/// <summary>
/// "What happens next" engine. After each beat, proposes 2-3 possible next beats
/// based on story state, character agendas, unresolved seeds, and consequence pressure.
/// User picks one or lets the engine choose autonomously.
/// </summary>
public class SuggestionEngineService
{
    private readonly ILlmService llm;
    private readonly IDatabaseService db;
    private readonly ConsequenceEngine consequences;
    private readonly StoryStateService storyState;
    private readonly EmbeddingService? embeddings;

    public SuggestionEngineService(
        ILlmService llm, IDatabaseService db,
        ConsequenceEngine consequences, StoryStateService storyState,
        EmbeddingService? embeddings = null)
    {
        this.llm = llm;
        this.db = db;
        this.consequences = consequences;
        this.storyState = storyState;
        this.embeddings = embeddings;
    }

    /// <summary>
    /// Generate 2-3 possible next beats based on current story state.
    /// </summary>
    public async Task<List<BeatSuggestion>> SuggestNextBeatsAsync(
        string projectId,
        StoryOutline outline,
        int currentBeatIndex,
        List<string> cast,
        string location,
        string storySoFar,
        CancellationToken ct = default)
    {
        var stateContext = storyState.BuildConstraints(projectId);
        var consequenceContext = consequences.BuildConsequenceContext(projectId);

        // Find unresolved seeds from the outline
        var allBeats = outline.Acts.SelectMany(a => a.Beats).ToList();
        var completedBeats = allBeats.Take(currentBeatIndex + 1).ToList();
        var remainingBeats = allBeats.Skip(currentBeatIndex + 1).ToList();

        var plantedSeeds = completedBeats.SelectMany(b => b.Seeds).Distinct().ToList();
        var resolvedPayoffs = completedBeats.SelectMany(b => b.Payoffs).Distinct().ToList();
        var unresolvedSeeds = plantedSeeds.Except(resolvedPayoffs).ToList();

        // Next planned beat (if outline exists)
        var nextPlanned = remainingBeats.FirstOrDefault();

        // Character agendas
        var agendas = new List<string>();
        foreach (var name in cast.Take(4))
        {
            var c = db.FindCharacter(name);
            if (c?.Description.Length > 0)
                agendas.Add($"  {name}: {c.Description[..Math.Min(200, c.Description.Length)]}");
        }

        // Audit Priority-2: surface thematically-pressing characters whose
        // agendas align with the current tension. The cast list contains who's
        // physically present; the embedding query (story state + unresolved
        // seeds) finds characters whose canon tension matches what the story
        // is RIGHT NOW. Hits become "OFF-CAST RELEVANT" hints in the prompt
        // so the LLM can suggest beats that bring those characters into play.
        var offCastRelevant = await BuildOffCastRelevantAsync(stateContext, unresolvedSeeds, cast, ct);

        var system = """
            You are a story direction engine for a neo-noir narrative.
            Given the current story state, suggest 2-3 possible next beats.
            Each suggestion should be a different narrative direction — not variations of the same idea.

            Consider:
            - Unresolved narrative seeds that need payoff
            - Character motivations and unfinished business
            - Consequences from prior actions
            - Pacing (if recent beats were high-tension, suggest a breather option)
            - The planned outline (honor it if possible, but don't force it if the story has naturally diverged)

            Return ONLY a JSON array of suggestions:
            [
              {
                "title": "Short beat title",
                "description": "2-3 sentences describing what happens",
                "tone": "tense|reflective|action|intimate|suspense|revelation",
                "tension": 1-10,
                "characters_involved": ["character names"],
                "seeds_resolved": ["any seeds this beat would pay off"],
                "new_seeds": ["any new narrative seeds this would plant"],
                "rationale": "Why this beat makes sense right now"
              }
            ]
            """;

        var user = $"""
            STORY SO FAR (last 1500 chars):
            {(storySoFar.Length > 1500 ? storySoFar[^1500..] : storySoFar)}

            CAST: {string.Join(", ", cast)}
            LOCATION: {location}

            CHARACTER AGENDAS:
            {(agendas.Count > 0 ? string.Join("\n", agendas) : "  No specific agendas available.")}

            UNRESOLVED SEEDS: {(unresolvedSeeds.Count > 0 ? string.Join(", ", unresolvedSeeds) : "none")}
            {offCastRelevant}

            STORY STATE:
            {stateContext}

            WORLD CONSEQUENCES IN PLAY:
            {consequenceContext}

            OUTLINE'S NEXT PLANNED BEAT: {(nextPlanned != null ? $"{nextPlanned.Goal} (tension: {nextPlanned.Tension}/10)" : "No outline — freeform.")}

            Suggest 2-3 possible next beats.
            """;

        try
        {
            var response = await llm.GenerateAsync(system, user, 0.7f, 1500, null, ct);
            var json = ExtractJsonArray(response);
            var arr = System.Text.Json.JsonDocument.Parse(json);

            var suggestions = new List<BeatSuggestion>();
            foreach (var item in arr.RootElement.EnumerateArray())
            {
                suggestions.Add(new BeatSuggestion
                {
                    Title = item.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "",
                    Description = item.TryGetProperty("description", out var d) ? d.GetString() ?? "" : "",
                    Tone = item.TryGetProperty("tone", out var tn) ? tn.GetString() ?? "tense" : "tense",
                    Tension = item.TryGetProperty("tension", out var te) ? te.GetInt32() : 5,
                    CharactersInvolved = item.TryGetProperty("characters_involved", out var ci)
                        ? ci.EnumerateArray().Select(e => e.GetString() ?? "").Where(s => s.Length > 0).ToList()
                        : cast,
                    SeedsResolved = item.TryGetProperty("seeds_resolved", out var sr)
                        ? sr.EnumerateArray().Select(e => e.GetString() ?? "").Where(s => s.Length > 0).ToList()
                        : [],
                    NewSeeds = item.TryGetProperty("new_seeds", out var ns)
                        ? ns.EnumerateArray().Select(e => e.GetString() ?? "").Where(s => s.Length > 0).ToList()
                        : [],
                    Rationale = item.TryGetProperty("rationale", out var r) ? r.GetString() ?? "" : "",
                });
            }

            return suggestions;
        }
        catch
        {
            return [];
        }
    }

    /// <summary>
    /// Embedding-grounded "characters not on stage who could matter right now"
    /// hint. Query is (story state + unresolved seeds) — finds canon characters
    /// whose embedded profile overlaps thematically with what's pressing in the
    /// scene. Excludes the current cast so the suggestion isn't "the same
    /// people" again. Empty string when EmbeddingService isn't available so
    /// the prompt section drops out cleanly.
    /// </summary>
    private async Task<string> BuildOffCastRelevantAsync(
        string stateContext, List<string> unresolvedSeeds, List<string> cast, CancellationToken ct)
    {
        if (embeddings == null) return "";
        var query = $"{stateContext}\n{string.Join(" / ", unresolvedSeeds)}";
        if (string.IsNullOrWhiteSpace(query)) return "";
        try
        {
            var hits = await embeddings.FindSimilarAsync(query, k: 5, entityTypes: new[] { "character" }, ct: ct);
            if (hits.Count == 0) return "";
            // Drop characters already on cast — those are covered by AGENDAS above.
            var castSet = new HashSet<string>(cast, StringComparer.OrdinalIgnoreCase);
            var off = hits.Where(h => !castSet.Contains(h.EntityName)).Take(3).ToList();
            if (off.Count == 0) return "";
            return "\nOFF-CAST RELEVANT (canon characters whose tensions match the current scene — bringing one in could land):\n"
                 + string.Join("\n", off.Select(h => $"  - {h.EntityName}"));
        }
        catch
        {
            return "";
        }
    }

    private static string ExtractJsonArray(string text)
    {
        var start = text.IndexOf('[');
        var end = text.LastIndexOf(']');
        return start >= 0 && end > start ? text[start..(end + 1)] : "[]";
    }
}

public class BeatSuggestion
{
    public string Title { get; init; } = "";
    public string Description { get; init; } = "";
    public string Tone { get; init; } = "";
    public int Tension { get; init; }
    public List<string> CharactersInvolved { get; init; } = [];
    public List<string> SeedsResolved { get; init; } = [];
    public List<string> NewSeeds { get; init; } = [];
    public string Rationale { get; init; } = "";
}
