using MindAttic.Legion;
using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Models;

namespace StreetSamurai.Core.Services;

public class BeatGeneratorService
{
    private readonly ILlmService llm;
    private readonly WorldGraphService graph;
    private readonly LoreService canon;
    private readonly EmbeddingService embeddings;
    private readonly IDbContextFactory<StreetSamuraiDbContext> dbFactory;
    private readonly LLMVotingService? voting;

    public BeatGeneratorService(
        ILlmService llm,
        WorldGraphService graph,
        LoreService canon,
        EmbeddingService embeddings,
        IDbContextFactory<StreetSamuraiDbContext> dbFactory,
        LLMVotingService? voting = null)
    {
        this.llm = llm;
        this.graph = graph;
        this.canon = canon;
        this.embeddings = embeddings;
        this.dbFactory = dbFactory;
        this.voting = voting;
    }

    public async Task<string> GenerateBeatAsync(
        BeatContext context,
        CancellationToken ct = default)
    {
        var dialogueBlock = !string.IsNullOrWhiteSpace(context.DialogueContext)
            ? $"\n\n{context.DialogueContext}"
            : "";

        // Pull semantically-similar past beats as in-context style anchors. Voice
        // and pacing tend to drift across long writing sessions; injecting 3
        // beats from canon that are *structurally* near this one (same kind of
        // negotiation, same kind of confrontation, same kind of beat-of-quiet)
        // keeps the writer's register consistent without copying any specific
        // scene. Empty when the prose-embedding cache is cold.
        var anchorBlock = await BuildBeatAnchorsAsync(context, ct);

        var system = $"""
            You are writing a beat in a literary cyberpunk scene set in GLMZ (Meridian 88).

            INNER MONOLOGUE: italicized stand-alone sentences, on their own paragraph, NEVER labeled.
            Source from each POV character's documented psychology — coping_mechanisms, core_fears,
            blind_spots, secret. Specific named things, not abstract archetypes. Do NOT use bracketed
            tags like [WOUND] or [IDEAL] — those are retired.

            STORY BIBLE AND LITERARY RULES:
            {context.StoryBibleContext}

            WORLD CONTEXT (characters, locations, equipment, relationships — use as canon facts):
            {context.RelationshipContext}
            {(context.LocationContext.Length > 0 ? "\nADDITIONAL LOCATION DETAIL:\n" + context.LocationContext : "")}{dialogueBlock}{anchorBlock}
            """;

        var hasDialogue = context.DialogueContext.Length > 0;
        var dialogueInstruction = hasDialogue
            ? """

              DIALOGUE DIRECTION:
              Characters speak in their own voice — see profiles above. Each voice must be immediately
              distinct without dialogue tags. Do not name emotions. Do not have characters explain
              themselves. Subtext is load-bearing. What a character says to fill silence reveals
              more than what they say when they mean to speak.
              """
            : "";

        var user = $"""
            SCENE SO FAR:
            {context.SceneSoFar}

            BEAT GOAL: {context.BeatGoal}

            Write the next beat of the scene. Voice comes from the POV character's documented
            speech_patterns and psychology — clipped or warm, deflective or direct, depending on
            whose head we're in. Inner thoughts surface as *italicized stand-alone lines*, never
            labeled — a person arguing with themselves about a specific named thing.{dialogueInstruction}

            Write 2-4 paragraphs. Make every word count.
            """;

        return await llm.GenerateAsync(system, user, temperature: 0.85, maxTokens: 2048, ct: ct);
    }

    /// <summary>
    /// Ask Legion's panel for <paramref name="count"/> diverse next-beat blurbs.
    /// Each trusted provider answers independently with one short blurb at high
    /// temperature, so the returned list is genuinely varied (not converged
    /// consensus). Caller picks one to commit, ignores the rest, or calls again
    /// to append more candidates.
    ///
    /// Each blurb is a 1-2 sentence sketch in the voice of "what should happen
    /// next" — not finished prose. The expand step (<see cref="GenerateBeatAsync"/>)
    /// turns the chosen blurb into prose.
    /// </summary>
    public async Task<List<string>> SuggestNextBeatsAsync(
        string sceneSoFar, string chapterTitle, string povCharacter,
        int count = 5, CancellationToken ct = default)
    {
        if (voting is null) return new List<string>();

        var ctxBuilder = new System.Text.StringBuilder();
        if (!string.IsNullOrWhiteSpace(chapterTitle))
            ctxBuilder.AppendLine($"CHAPTER: {chapterTitle}");
        if (!string.IsNullOrWhiteSpace(povCharacter))
            ctxBuilder.AppendLine($"POV: {povCharacter}");
        ctxBuilder.AppendLine();
        if (!string.IsNullOrWhiteSpace(sceneSoFar))
        {
            ctxBuilder.AppendLine("SCENE SO FAR:");
            ctxBuilder.AppendLine(sceneSoFar.Length > 6000 ? sceneSoFar[^6000..] : sceneSoFar);
        }
        else
        {
            ctxBuilder.AppendLine("SCENE SO FAR: (empty — this is the first beat of the chapter)");
        }

        var request = new VoteRequest
        {
            Question =
                "Propose ONE next beat for this chapter — a 1-2 sentence blurb describing what " +
                "should happen next. Be specific (named places, named characters, a concrete action " +
                "or revelation), not generic. Lean into texture: implications, mood shifts, the " +
                "moment something changes. Output ONLY the blurb, no preamble, no quotes, no list.",
            Context = ctxBuilder.ToString(),
            MaxTokens = 220,
            Temperature = 0.95,
            SynthesizeNarrative = false,
        };

        VotingResult result;
        try
        {
            // Plurality: we don't want consensus, we want every voter's distinct take.
            result = await voting.VoteAsync(request, Quorum.Plurality, ct);
        }
        catch
        {
            return new List<string>();
        }

        var blurbs = result.IndividualVotes
            .Where(v => !v.IsError && !string.IsNullOrWhiteSpace(v.Decision))
            .Select(v => v.Decision.Trim().Trim('"').Trim())
            .Where(s => s.Length >= 8)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(count)
            .ToList();

        return blurbs;
    }

    /// <summary>
    /// Score and rank candidate beat blurbs with a 100-persona panel
    /// distributed evenly across Legion's four trusted providers
    /// (25 claude / 25 openai / 25 gemini / 25 deepseek).
    ///
    /// One prompt per persona — each persona scores ALL candidates 0-100
    /// in a single response, so the call cost is 100 LLM requests total
    /// regardless of how many candidates are being ranked. The aggregate
    /// score per blurb is the mean of every responding persona's score
    /// for it; result is sorted descending so the strongest candidates
    /// rise to the top.
    ///
    /// <para><b>Cost.</b> 100 LLM requests is non-trivial — this is an
    /// opt-in ranking step, NOT auto-fired after every suggest. UI should
    /// gate behind an explicit "Rank with 100 personas" button.</para>
    /// </summary>
    public async Task<List<BeatRankResult>> RankBeatBlurbsAsync(
        IReadOnlyList<string> blurbs, string sceneSoFar,
        string chapterTitle, string povCharacter,
        CancellationToken ct = default)
    {
        if (voting is null || blurbs.Count == 0) return new List<BeatRankResult>();

        var ctxBuilder = new System.Text.StringBuilder();
        if (!string.IsNullOrWhiteSpace(chapterTitle)) ctxBuilder.AppendLine($"CHAPTER: {chapterTitle}");
        if (!string.IsNullOrWhiteSpace(povCharacter)) ctxBuilder.AppendLine($"POV: {povCharacter}");
        if (!string.IsNullOrWhiteSpace(sceneSoFar))
        {
            ctxBuilder.AppendLine();
            ctxBuilder.AppendLine("SCENE SO FAR:");
            ctxBuilder.AppendLine(sceneSoFar.Length > 6000 ? sceneSoFar[^6000..] : sceneSoFar);
        }
        ctxBuilder.AppendLine();
        ctxBuilder.AppendLine("CANDIDATE NEXT BEATS (score each):");
        for (int i = 0; i < blurbs.Count; i++)
            ctxBuilder.AppendLine($"  {i + 1}. {blurbs[i]}");

        var request = new VoteRequest
        {
            Question =
                "Score each candidate next beat 0-100 based on: how compelling and specific it is, " +
                "how well it serves story momentum from the scene so far, and whether it would lead " +
                "to interesting prose that respects the POV character's voice. Output STRICT JSON: " +
                "an array of objects [{\"id\": <int>, \"score\": <0-100>}, ...] — one entry per candidate, " +
                "ids matching the numbered list. No prose outside the JSON.",
            Context = ctxBuilder.ToString(),
            MaxTokens = 512,
            Temperature = 0.4,
            SynthesizeNarrative = false,
        };

        // Build the 100-persona panel with explicit 25/25/25/25 distribution across
        // the four trusted providers. CreatePanel spreads voices across providers
        // round-robin which gives 25/25/25/25 when all four are active and the
        // count is a multiple of 4.
        var panel = voting.CreatePanel(count: 100, fallbackProviderId: "claude");

        VotingResult result;
        try
        {
            // Plurality: we don't want consensus — we want every persona's score for aggregation.
            result = await voting.VoteWithProfilesAsync(request, Quorum.Plurality, panel, ct);
        }
        catch
        {
            return new List<BeatRankResult>();
        }

        var totals = blurbs.Select(_ => (sum: 0.0, n: 0)).ToList();
        foreach (var v in result.IndividualVotes.Where(v => !v.IsError))
        {
            var payload = !string.IsNullOrWhiteSpace(v.Decision) ? v.Decision : v.Reasoning;
            if (string.IsNullOrWhiteSpace(payload)) continue;
            foreach (var (id, score) in ParseRankPayload(payload))
            {
                if (id < 1 || id > blurbs.Count) continue;
                if (score < 0 || score > 100) continue;
                var idx = id - 1;
                var (sum, n) = totals[idx];
                totals[idx] = (sum + score, n + 1);
            }
        }

        return blurbs
            .Select((b, i) => new BeatRankResult(
                Blurb:    b,
                Score:    totals[i].n == 0 ? 0.0 : totals[i].sum / totals[i].n,
                VoteCount: totals[i].n))
            .OrderByDescending(x => x.Score)
            .ToList();
    }

    /// <summary>Parse a "[{id, score}, ...]" JSON payload tolerantly — accepts a JSON array anywhere in the response.</summary>
    private static IEnumerable<(int id, double score)> ParseRankPayload(string payload)
    {
        var start = payload.IndexOf('[');
        var end   = payload.LastIndexOf(']');
        if (start < 0 || end <= start) yield break;
        var json = payload[start..(end + 1)];
        System.Text.Json.JsonDocument doc;
        try { doc = System.Text.Json.JsonDocument.Parse(json); }
        catch { yield break; }
        using (doc)
        {
            if (doc.RootElement.ValueKind != System.Text.Json.JsonValueKind.Array) yield break;
            foreach (var e in doc.RootElement.EnumerateArray())
            {
                if (e.ValueKind != System.Text.Json.JsonValueKind.Object) continue;
                if (!e.TryGetProperty("id",    out var idEl)    || !idEl.TryGetInt32(out var id)) continue;
                if (!e.TryGetProperty("score", out var scoreEl) || !scoreEl.TryGetDouble(out var score)) continue;
                yield return (id, score);
            }
        }
    }

    /// <summary>
    /// Pull the top-3 semantically-similar past beats from the prose-embedding
    /// cache, render them as a short "STYLE ANCHORS" block. The query is the
    /// (BeatGoal + last ~1.5k chars of SceneSoFar) — that's what we want to
    /// match in voice/pacing. Returns empty string when the prose cache is
    /// cold or the embedding API is unavailable.
    /// </summary>
    private async Task<string> BuildBeatAnchorsAsync(BeatContext context, CancellationToken ct)
    {
        var query = (context.BeatGoal ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(context.SceneSoFar))
        {
            var tail = context.SceneSoFar.Length > 1500
                ? context.SceneSoFar[^1500..]
                : context.SceneSoFar;
            query = string.IsNullOrEmpty(query) ? tail : $"{query}\n\n{tail}";
        }
        if (string.IsNullOrWhiteSpace(query)) return "";

        IReadOnlyList<ProseEmbeddingHit> hits;
        try { hits = await embeddings.FindSimilarProseAsync(query, k: 4, scopeKind: "beat", ct); }
        catch { return ""; }
        if (hits.Count == 0) return "";

        var beatIds = hits.Select(h => h.ScopeId).ToHashSet();
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var beats = await db.Set<Data.Entities.ChapterBeat>().AsNoTracking()
            .Where(b => beatIds.Contains(b.BeatGuid))
            .Select(b => new { b.BeatGuid, b.Title, b.Synopsis, b.Text })
            .ToListAsync(ct);

        if (beats.Count == 0) return "";

        var sb = new System.Text.StringBuilder();
        sb.AppendLine().AppendLine();
        sb.AppendLine("STYLE ANCHORS (canon beats with adjacent voice/pacing — match the register, do NOT echo specifics):");
        var hitOrder = hits.Select((h, i) => new { h.ScopeId, Order = i }).ToDictionary(x => x.ScopeId, x => x.Order);
        foreach (var beat in beats.OrderBy(b => hitOrder.TryGetValue(b.BeatGuid, out var o) ? o : int.MaxValue).Take(3))
        {
            if (string.IsNullOrWhiteSpace(beat.Text)) continue;
            // Cap each anchor to ~600 chars so the system prompt stays bounded.
            var excerpt = beat.Text.Length > 600 ? beat.Text[..600].TrimEnd() + "…" : beat.Text;
            if (!string.IsNullOrWhiteSpace(beat.Title)) sb.Append("[").Append(beat.Title).AppendLine("]");
            sb.AppendLine(excerpt).AppendLine();
        }
        return sb.ToString();
    }
}

public record BeatContext
{
    public string StoryBibleContext { get; init; } = "";
    public string RelationshipContext { get; init; } = "";
    public string LocationContext { get; init; } = "";
    /// <summary>Per-character voice profiles and cross-character relationship dynamics from DialogueService.</summary>
    public string DialogueContext { get; init; } = "";
    public string SceneSoFar { get; init; } = "";
    public string BeatGoal { get; init; } = "";
}

/// <summary>
/// One ranked candidate beat blurb, scored by the 100-persona panel.
/// Score is the mean (0-100) across every responding persona; VoteCount is
/// how many personas successfully scored this candidate (transparent failure
/// when some providers error mid-vote).
/// </summary>
public record BeatRankResult(string Blurb, double Score, int VoteCount);
