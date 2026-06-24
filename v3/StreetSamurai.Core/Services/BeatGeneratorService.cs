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
    private readonly LlmVotingService? voting;
    private readonly ExpertPersonaService? personas;
    private readonly ActionConfigService? actionConfig;
    private readonly IUniverseContext? universe;
    private readonly PlantPayoffService? plantPayoffs;
    private readonly StoryAuditService? storyAudit;

    public BeatGeneratorService(
        ILlmService llm,
        WorldGraphService graph,
        LoreService canon,
        EmbeddingService embeddings,
        IDbContextFactory<StreetSamuraiDbContext> dbFactory,
        LlmVotingService? voting = null,
        ExpertPersonaService? personas = null,
        ActionConfigService? actionConfig = null,
        IUniverseContext? universe = null,
        PlantPayoffService? plantPayoffs = null,
        StoryAuditService? storyAudit = null)
    {
        this.llm = llm;
        this.graph = graph;
        this.canon = canon;
        this.embeddings = embeddings;
        this.dbFactory = dbFactory;
        this.voting = voting;
        this.personas = personas;
        this.actionConfig = actionConfig;
        this.universe = universe;
        this.plantPayoffs = plantPayoffs;
        this.storyAudit = storyAudit;
    }

    /// <summary>
    /// The opening world line for the generator's system prompt. For GLMZ (the default universe,
    /// or when no universe context is wired) this is byte-identical to the original hardcoded
    /// string — zero voice drift. For any other universe it uses that universe's seeded UniversePrimer
    /// so prose is grounded in the right world. This is the seam other GLMZ-hardcoded prompt sites
    /// should adopt (SS-A2 / SS-LAW-15).
    /// </summary>
    private string UniverseLine()
    {
        var u = universe?.CurrentUniverse;
        if (u == null || u.Id == StreetSamurai.Core.Data.Entities.Universe.GlmzId || string.IsNullOrWhiteSpace(u.UniversePrimer))
            return "You are writing a beat in a literary cyberpunk scene set in GLMZ (Meridian 88).";
        return $"You are writing a beat set in this universe — {u.Name}:\n{u.UniversePrimer}";
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

        // Plant/payoff context: seeded details awaiting payoff, or registered payoffs
        // to honour in this beat. Injected when StrandId is set + PlantPayoffService
        // is wired. Non-blocking — silently empty on first-write or cold starts.
        var plantBlock = "";
        if (plantPayoffs != null && context.StrandId != Guid.Empty)
        {
            try { plantBlock = await plantPayoffs.BuildPlantContextAsync(context.StrandId, ct); }
            catch { /* non-blocking */ }
        }

        // Story commandment context: gateway (null PreviousStrandId) or sequel
        // commandments, injected as writing goals for this strand.
        var commandmentBlock = "";
        if (storyAudit != null && context.StrandId != Guid.Empty)
        {
            try
            {
                await using var db = await dbFactory.CreateDbContextAsync(ct);
                var s = await db.Strands.AsNoTracking()
                    .Where(x => x.Id == context.StrandId)
                    .Select(x => new { x.PreviousStrandId })
                    .FirstOrDefaultAsync(ct);
                if (s != null)
                    commandmentBlock = storyAudit.BuildCommandmentContext(s.PreviousStrandId.HasValue);
            }
            catch { /* non-blocking */ }
        }

        // Pacing + structural role guidance — pre-computed by ProseWriterRouter and
        // injected here. Both are empty strings when called via the legacy direct path.
        var pacingBlock = !string.IsNullOrWhiteSpace(context.PacingGuidance)
            ? $"\n\n{context.PacingGuidance}"
            : "";
        var structuralBlock = !string.IsNullOrWhiteSpace(context.StructuralRoleGuidance)
            ? $"\n\n{context.StructuralRoleGuidance}"
            : "";

        var system = $"""
            {UniverseLine()}

            INNER MONOLOGUE: italicized stand-alone sentences, on their own paragraph, NEVER labeled.
            Source from each POV character's documented psychology — coping_mechanisms, core_fears,
            blind_spots, secret. Specific named things, not abstract archetypes. Do NOT use bracketed
            tags like [WOUND] or [IDEAL] — those are retired.

            STORY BIBLE AND LITERARY RULES:
            {context.StoryBibleContext}

            WORLD CONTEXT (characters, locations, equipment, relationships — use as canon facts):
            {context.RelationshipContext}
            {(context.XRayContext.Length > 0 ? "\nSCENE X-RAY — entities on screen RIGHT NOW. Every character below speaks in THEIR OWN documented register, not the narrator's:\n" + context.XRayContext : "")}
            {(context.LocationContext.Length > 0 ? "\nADDITIONAL LOCATION DETAIL:\n" + context.LocationContext : "")}{dialogueBlock}{anchorBlock}{plantBlock}{commandmentBlock}{pacingBlock}{structuralBlock}
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
        string povCharacterProfile = "",
        int count = 5, CancellationToken ct = default)
    {
        if (voting is null) return new List<string>();

        var ctxBuilder = new System.Text.StringBuilder();
        if (!string.IsNullOrWhiteSpace(chapterTitle))
            ctxBuilder.AppendLine($"CHAPTER: {chapterTitle}");
        if (!string.IsNullOrWhiteSpace(povCharacter))
            ctxBuilder.AppendLine($"POV: {povCharacter}");
        if (!string.IsNullOrWhiteSpace(povCharacterProfile))
        {
            ctxBuilder.AppendLine();
            ctxBuilder.AppendLine("POV CHARACTER PROFILE:");
            ctxBuilder.AppendLine(povCharacterProfile);
        }
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
                "Propose ONE next beat for this scene through YOUR area of expertise. 1-2 sentences. " +
                "Be specific — named places, named characters, a concrete action or revelation. " +
                "Lean into your specialty: what does it notice and want to surface here that other " +
                "lenses might miss? Output ONLY the blurb. No preamble, no quotes, no list.",
            Context = ctxBuilder.ToString(),
            MaxTokens = 220,
            Temperature = 0.95,
            SynthesizeNarrative = false,
        };

        // Voter count + tier come from ActionConfigService (default 10 / High,
        // adjustable in settings — but tier-locked HIGH for writing actions so
        // settings can't accidentally degrade prose quality). Personas come
        // from the ExpertPersonaService table; the selector picks the top-N
        // most pertinent to this scene rather than always using the same 10.
        var voterCount = actionConfig?.GetVoterCount(ActionConfigService.ActionIds.ChapterBeatWriter) ?? 10;
        var sceneFootprint = ctxBuilder.ToString();
        var pickedPersonas = personas != null
            ? await personas.SelectPertinentAsync(sceneFootprint, voterCount, ct)
            : new List<Models.ExpertPersona>();
        var experts = pickedPersonas.Count > 0
            ? BuildExpertPanelFromTable(pickedPersonas)
            : BuildExpertPanel();

        VotingResult result;
        try
        {
            result = await voting.VoteWithProfilesAsync(request, Quorum.Plurality, experts, ct);
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
    /// 10 expert-archetype voters — each a different specialty that the kind of
    /// fiction this app produces benefits from. Provider distribution is
    /// round-robin across the trusted four (claude/openai/gemini/deepseek);
    /// each is pinned to the provider's HIGH-tier model (opus-class) because
    /// generation quality dominates this stage. Personas are FIXED archetypes
    /// (not random) so every panel includes the same lenses — the variation
    /// comes from each lens's reading of the scene, not from sampling noise.
    /// </summary>
    private static IReadOnlyList<VoterProfile> BuildExpertPanel()
    {
        var experts = new (string Name, string Lens)[]
        {
            ("Master Swordsman",
                "You're a master swordsman. You read distance, threat, opening, blade lineage. " +
                "You see who in a room can fight and who only thinks they can. You notice draw stances, " +
                "hand position, what's hidden behind a coat."),
            ("Bar / Crowd Specialist",
                "You're an expert on bars, dive scenes, and crowd dynamics. You read the room — " +
                "who's watching, whose attention shifted, what the bartender's eyes do. " +
                "Atmosphere, side conversations, the moment a room changes mood are your craft."),
            ("Negotiation Tactician",
                "You're a master of high-stakes negotiation under threat. You read leverage, " +
                "framing, power moves, what each side cannot afford to admit. You see deals being " +
                "struck behind a sentence about the weather."),
            ("Voice & Dialogue Master",
                "You're a craft master of dialogue. Subtext is your medium — what's NOT said, " +
                "register flips, the line that lands like a counterweight. You hate dialogue tags."),
            ("Pacing Dramatist",
                "You're a master of beat rhythm. You feel when to escalate, when to hold, " +
                "when to deflate before the next reveal. You read scenes as music — tempo, rest, " +
                "the bar before the chord change."),
            ("Character Psychology",
                "You're an expert in interior life. Motivation, blind spots, the gap between what " +
                "a character wants and what they think they want. You make sure inner monologue is " +
                "specific, not abstract — anchored to the character's documented psychology."),
            ("Cyberpunk Genre Specialist",
                UniverseScope.Current?.UniverseGroundingOr(
                "You're an expert in cyberpunk texture — augments, neural interfaces, BCI cognition, " +
                "the felt sense of running parallel processes in the head while a hand stays still. " +
                "Body horror, grace, and tech-as-subtext are your beats.")
                ?? "You're an expert in cyberpunk texture — augments, neural interfaces, BCI cognition, " +
                "the felt sense of running parallel processes in the head while a hand stays still. " +
                "Body horror, grace, and tech-as-subtext are your beats."),
            ("World-Grounding (GLMZ)",
                UniverseScope.Current?.UniverseGroundingOr(
                "You're an expert in this story's world — GLMZ / Meridian 88, CorpoNation politics, " +
                "the Pulse, factions, the Tier system, the Sponsorship Program. You catch when " +
                "prose drifts into generic cyberpunk and pull it back into THIS world's specifics.")
                ?? "You're an expert in this story's world — GLMZ / Meridian 88, CorpoNation politics, " +
                "the Pulse, factions, the Tier system, the Sponsorship Program. You catch when " +
                "prose drifts into generic cyberpunk and pull it back into THIS world's specifics."),
            ("Literary Craft",
                "You're an expert in line-level prose. Image, sound, rhythm, the sentence that earns " +
                "its weight by what it leaves out. You're allergic to clichés and to neat resolutions."),
            ("Continuity Guardian",
                "You track what's been established — earlier beats, character state, threads " +
                "opened-not-closed, reveals already deployed. You catch when a proposed beat would " +
                "contradict canon or repeat a beat that already fired."),
        };

        var providers = new[] { "claude", "openai", "gemini", "deepseek" };
        var voters = new List<VoterProfile>(experts.Length);
        for (int i = 0; i < experts.Length; i++)
        {
            var (name, lens) = experts[i];
            var providerId    = providers[i % providers.Length];
            var modelOverride = HighTierModelFor(providerId);
            voters.Add(new VoterProfile
            {
                VoterId             = $"expert-{i:D2}-{Guid.NewGuid().ToString("N")[..8]}",
                Name                = name,
                ProviderId          = providerId,
                ModelOverride       = modelOverride,
                PersonalityMarkdown = lens,
            });
        }
        return voters;
    }

    /// <summary>
    /// Pin each provider to its HIGH-tier model (opus-class) for prose-quality
    /// generation. Mirrors LowTierModelFor — same bridge pattern. Will be
    /// replaced with LlmProviderCatalog.GetTieredModel(provider, ModelTier.High)
    /// once the running app's Legion DLL lock clears and the consumer bin
    /// dir picks up the rebuilt Legion.
    /// </summary>
    private static string? HighTierModelFor(string providerId) => providerId switch
    {
        "claude"   => "claude-opus-4-7",
        "openai"   => "gpt-4.1",
        "gemini"   => "gemini-2.5-pro",
        "deepseek" => "deepseek-reasoner",
        _          => null,
    };

    /// <summary>
    /// Build voter profiles from a selected subset of the persona table.
    /// Provider distribution stays round-robin across the trusted four;
    /// model is pinned HIGH (opus-class) per provider. Used when the
    /// ExpertPersonaService selected a pertinent subset — preferred over the
    /// hardcoded BuildExpertPanel because the table can grow over time.
    /// </summary>
    private static IReadOnlyList<VoterProfile> BuildExpertPanelFromTable(IReadOnlyList<Models.ExpertPersona> picked)
    {
        var providers = new[] { "claude", "openai", "gemini", "deepseek" };
        var voters = new List<VoterProfile>(picked.Count);
        for (int i = 0; i < picked.Count; i++)
        {
            var p = picked[i];
            var providerId    = providers[i % providers.Length];
            var modelOverride = HighTierModelFor(providerId);
            voters.Add(new VoterProfile
            {
                VoterId             = $"expert-{p.Id}-{Guid.NewGuid().ToString("N")[..8]}",
                Name                = p.Name,
                ProviderId          = providerId,
                ModelOverride       = modelOverride,
                PersonalityMarkdown = p.Lens,
            });
        }
        return voters;
    }

    /// <summary>
    /// Preview what a single persona would produce as a next-beat blurb. Used
    /// from /settings/ai's persona-table editor so the user can sanity-check
    /// each lens before adding it to the rotation. One-voter panel pinned to
    /// the persona's preferred provider (round-robins claude when unspecified)
    /// at HIGH tier — quality matters because the user is judging the lens.
    ///
    /// <paramref name="sceneContext"/> is optional; when empty, a generic
    /// "demo scene" prompt lets the persona pick its own canvas.
    /// </summary>
    public async Task<string> PreviewPersonaAsync(
        Models.ExpertPersona persona, string sceneContext = "", CancellationToken ct = default)
    {
        if (voting is null) return "";

        var scene = string.IsNullOrWhiteSpace(sceneContext)
            ? "DEMO SCENE: invent a brief scene that lets your specialty shine. Be specific."
            : "SCENE CONTEXT:\n" + (sceneContext.Length > 4000 ? sceneContext[^4000..] : sceneContext);

        var voter = new VoterProfile
        {
            VoterId             = $"preview-{persona.Id}-{Guid.NewGuid().ToString("N")[..8]}",
            Name                = persona.Name,
            ProviderId          = "claude",
            ModelOverride       = HighTierModelFor("claude"),
            PersonalityMarkdown = persona.Lens,
        };
        var request = new VoteRequest
        {
            Question =
                "Propose ONE next beat blurb (1-2 sentences) through your expert lens. " +
                "Specific named places / people / actions. No preamble, no quotes, no list — just the blurb.",
            Context = scene,
            MaxTokens = 200,
            Temperature = 0.9,
            SynthesizeNarrative = false,
        };

        try
        {
            var result = await voting.VoteWithProfilesAsync(request, Quorum.Plurality, new[] { voter }, ct);
            var v = result.IndividualVotes.FirstOrDefault(v => !v.IsError && !string.IsNullOrWhiteSpace(v.Decision));
            return v?.Decision?.Trim().Trim('"').Trim() ?? "";
        }
        catch
        {
            return "";
        }
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

        // Voter count comes from ActionConfigService (default 100, adjustable
        // from settings since this is a scoring action, not a writing action).
        // Personas are storytellers distributed along the chaos↔order spectrum;
        // models pinned to each provider's haiku-class (Low tier) so the burst
        // returns fast and cheap regardless of how high the user dials voter count.
        var voterCount = actionConfig?.GetVoterCount(ActionConfigService.ActionIds.ChapterBeatVoter) ?? 100;
        var panel = BuildStorytellerPanel(count: voterCount);

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
        "gemini"   => "gemini-2.5-flash-lite",
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

    /// <summary>
    /// After expanding a beat, compare the generated prose against the POV
    /// character's documented psychology / speech_patterns and surface novel
    /// traits the prose introduced that aren't in canon. Returns an empty list
    /// when the prose stays in character; otherwise the caller can either
    /// tighten the prose or write the novel trait back into the character
    /// profile so future beats see it.
    ///
    /// Single low-tier LLM call — cheap to run after every expand.
    /// </summary>
    public async Task<List<OocFinding>> DetectOutOfCharacterAsync(
        string prose, string povCharacter, string canonProfile, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(prose) || string.IsNullOrWhiteSpace(canonProfile))
            return new List<OocFinding>();

        var system =
            "You audit beat prose for out-of-character drift against a documented character " +
            "profile. You're not a stylistic critic — you only flag traits the prose introduces " +
            "that aren't supported by canon. Return STRICT JSON: an array of objects with the " +
            "shape { \"field\": <profile field>, \"detected\": <novel trait the prose shows>, " +
            "\"canon_value\": <what canon currently says, or empty>, \"suggestion\": <either " +
            "'tighten prose' or 'add to canon' depending on whether the trait belongs>}. " +
            "Empty array if the prose stays in character. No prose outside the JSON.";

        var user = $"""
            CHARACTER: {povCharacter}

            CANON PROFILE:
            {canonProfile}

            BEAT PROSE:
            {prose}
            """;

        string raw;
        try { raw = await llm.GenerateAsync(system, user, temperature: 0.2, maxTokens: 600, ct: ct); }
        catch { return new List<OocFinding>(); }

        return ParseOocFindings(raw);
    }

    internal static List<OocFinding> ParseOocFindings(string payload)
    {
        var result = new List<OocFinding>();
        if (string.IsNullOrWhiteSpace(payload)) return result;
        var start = payload.IndexOf('[');
        var end   = payload.LastIndexOf(']');
        if (start < 0 || end <= start) return result;
        var json = payload[start..(end + 1)];
        System.Text.Json.JsonDocument doc;
        try { doc = System.Text.Json.JsonDocument.Parse(json); }
        catch { return result; }
        using (doc)
        {
            if (doc.RootElement.ValueKind != System.Text.Json.JsonValueKind.Array) return result;
            foreach (var e in doc.RootElement.EnumerateArray())
            {
                if (e.ValueKind != System.Text.Json.JsonValueKind.Object) continue;
                result.Add(new OocFinding(
                    Field:      e.TryGetProperty("field", out var f)        ? f.GetString() ?? "" : "",
                    Detected:   e.TryGetProperty("detected", out var d)     ? d.GetString() ?? "" : "",
                    CanonValue: e.TryGetProperty("canon_value", out var cv) ? cv.GetString() ?? "" : "",
                    Suggestion: e.TryGetProperty("suggestion", out var s)   ? s.GetString() ?? "" : ""));
            }
        }
        return result;
    }

    /// <summary>Parse a "[{id, score}, ...]" JSON payload tolerantly — accepts a JSON array anywhere in the response.</summary>
    internal static IEnumerable<(int id, double score)> ParseRankPayload(string payload)
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
    /// <summary>X-Ray scene assembly block (RFC 0002, SceneContextAssembler): the entities
    /// on screen with their voice/psychology fields. Empty when no roster resolved.</summary>
    public string XRayContext { get; init; } = "";
    public string SceneSoFar { get; init; } = "";
    public string BeatGoal { get; init; } = "";

    /// <summary>
    /// Strand this beat belongs to. When set, BeatGeneratorService injects:
    ///   - active plant/payoff pairs (PlantPayoffService)
    ///   - gateway or sequel commandments (StoryAuditService, per PreviousStrandId)
    /// Leave as Guid.Empty to skip both injections (legacy callers).
    /// </summary>
    public Guid StrandId { get; init; }

    // ── ProseWriterRouter enrichment ──────────────────────────────────────────
    // These fields are populated by ProseWriterRouter before calling GenerateBeatAsync.
    // Left at their defaults when callers invoke BeatGeneratorService directly (legacy path).

    /// <summary>Position in strand — enables pacing and structural role injection when set by ProseWriterRouter.</summary>
    public int BeatIndex { get; init; }
    /// <summary>Total beats in the strand — enables positional arc calculations when set by ProseWriterRouter.</summary>
    public int TotalBeats { get; init; }

    /// <summary>Pre-computed pacing guidance block (from PacingService). Empty = skip injection.</summary>
    public string PacingGuidance { get; init; } = "";
    /// <summary>Pre-computed structural role block (from StoryMethodologyService). Empty = skip injection.</summary>
    public string StructuralRoleGuidance { get; init; } = "";
    /// <summary>Detected beat mode (Combat/Narrative/EmotionalClimax/etc.) from BeatModeDetector.</summary>
    public BeatMode DetectedMode { get; init; } = BeatMode.Narrative;
}

/// <summary>
/// One detected out-of-character drift in generated beat prose. Compared
/// against the POV character's documented psychology + speech_patterns;
/// surfaced in the editor so the user can either edit the prose back into
/// character or update the canon profile to capture the new trait.
/// </summary>
public record OocFinding(
    string Field,           // e.g. "speech_patterns", "psychology.coping_mechanisms"
    string Detected,        // The novel trait the prose introduced
    string CanonValue,      // What the canon currently says
    string Suggestion);     // Either "tighten prose" or "consider adding to canon"

/// <summary>
/// One ranked candidate beat blurb, scored by the 100-persona panel.
/// Score is the mean (0-100) across every responding persona; VoteCount is
/// how many personas successfully scored this candidate (transparent failure
/// when some providers error mid-vote).
/// </summary>
public record BeatRankResult(string Blurb, double Score, int VoteCount);

/// <summary>
/// Classification of the dominant mode of a beat. Detected from BeatGoal keywords by
/// BeatModeDetector and injected into BeatContext by ProseWriterRouter.
/// </summary>
public enum BeatMode { Narrative, Combat, EmotionalClimax, Dialogue, Transition, Revelation }
