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

        // 100 expert-storyteller personas distributed evenly across the trusted
        // four providers (25 each — round-robin assignment). Each persona occupies
        // a distinct position on the chaos↔order narrative-craft spectrum so the
        // resulting score is the mean reading across that whole aesthetic range,
        // not a single voice's bias. Low-tier models keep the 100-call burst fast
        // and cheap (haiku-class everywhere).
        var panel = BuildStorytellerPanel(count: 100);

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

    /// <summary>
    /// Build a panel of N expert-storyteller voters. Providers are round-robin
    /// across the trusted four (claude / openai / gemini / deepseek), so a 100-
    /// voter panel ends up 25/25/25/25 when all four are active. Each voter
    /// occupies a distinct position on the chaos↔order narrative-craft spectrum
    /// (position p / count) — the persona prompt anchors them at that point so
    /// the aggregate score is the mean reading across the full range, not a
    /// single voice's preference. Models are pinned to <see cref="ModelTier.Low"/>
    /// per provider via <see cref="LlmProviderCatalog.GetTieredModel"/> so the
    /// burst stays fast and cheap.
    /// </summary>
    private static IReadOnlyList<VoterProfile> BuildStorytellerPanel(int count)
    {
        var providers = new[] { "claude", "openai", "gemini", "deepseek" };
        var voters = new List<VoterProfile>(count);
        for (int i = 0; i < count; i++)
        {
            // Position on the chaos↔order spectrum, 0.0 (pure chaos) → 1.0 (pure order).
            var t = count <= 1 ? 0.5 : (double)i / (count - 1);
            var providerId = providers[i % providers.Length];
            var modelOverride = LowTierModelFor(providerId);
            voters.Add(new VoterProfile
            {
                VoterId             = $"storyteller-{i:D3}-{Guid.NewGuid().ToString("N")[..8]}",
                Name                = $"Storyteller {i + 1} ({SpectrumLabel(t)})",
                ProviderId          = providerId,
                ModelOverride       = modelOverride,
                PersonalityMarkdown = BuildStorytellerPersona(t),
            });
        }
        return voters;
    }

    /// <summary>
    /// Pin each persona to the cheapest fastest model the provider exposes —
    /// haiku-class everywhere — so the 100-call burst returns in seconds rather
    /// than minutes. The Legion-side tier catalog is the long-term home; this
    /// inline map is the bridge until consumers can resolve via
    /// LlmProviderCatalog.GetTieredModel from the Legion DLL.
    /// </summary>
    private static string? LowTierModelFor(string providerId) => providerId switch
    {
        "claude"   => "claude-haiku-4-5-20251001",
        "openai"   => "gpt-4.1-nano",
        "gemini"   => "gemini-2.0-flash",
        "deepseek" => "deepseek-chat",
        _          => null,
    };

    /// <summary>
    /// One short label for the chaos↔order position so panel members are readable
    /// in voting-result UIs (e.g., "0.32 → tilted-chaos").
    /// </summary>
    private static string SpectrumLabel(double t) => t switch
    {
        < 0.10 => "pure chaos",
        < 0.30 => "tilted chaos",
        < 0.45 => "balanced (lean chaos)",
        < 0.55 => "balanced",
        < 0.70 => "balanced (lean order)",
        < 0.90 => "tilted order",
        _      => "pure order",
    };

    /// <summary>
    /// Persona prompt for an expert storyteller occupying a specific point on
    /// the chaos↔order spectrum. The persona is an EXPERT in narrative craft —
    /// they all share the toolkit; what differs is the aesthetic position from
    /// which they evaluate. Anchored prose so the LLM commits to the bias.
    /// </summary>
    private static string BuildStorytellerPersona(double t)
    {
        var pct = (int)Math.Round(t * 100);
        var stance = t switch
        {
            < 0.10 => "You favor narrative CHAOS in its purest form — surprising reversals, broken expectations, anti-tidy endings, unresolved tension as virtue. Causation that never lands. Beats that defy story shape on principle.",
            < 0.30 => "You favor controlled CHAOS — most rules can be broken, but the breaking should land. Surprise is the highest virtue; structure is a foil to subvert.",
            < 0.45 => "You lean chaos but respect craft — surprise matters more than satisfaction, but a broken expectation must MEAN something. You distrust tidy resolutions.",
            < 0.55 => "You stand at the BALANCE point — story is the negotiation between expectation and surprise. You judge a beat by whether it serves the whole arc, neither over-tidy nor over-fractured.",
            < 0.70 => "You lean order but value rupture — structure is a covenant, but the covenant has to bend at the right beats. Earned reversals over surprise for surprise's sake.",
            < 0.90 => "You favor narrative ORDER — satisfying causation, every promise paid, every gun fired. The pleasure of a clockwork story assembled with intent.",
            _      => "You favor pure ORDER — cause-and-effect rigor, structural inevitability, the catharsis of a perfectly closed loop. Chaos is failure of craft.",
        };

        return $$"""
            You are an EXPERT in narrative craft — you know story structure, beat theory,
            voice, pacing, dialogue, subtext, and how each element compounds. You evaluate
            fiction by craft principles, not personal taste in subject matter.

            YOUR AESTHETIC POSITION on the chaos↔order spectrum: **{{pct}}/100** (100 = pure order, 0 = pure chaos).

            {{stance}}

            Score candidate beats by how compelling YOU find them through this lens. Two
            experts at different ends of the spectrum can rate the same beat very
            differently — that's the point. Your score is a vote, not a consensus.
            """;
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
