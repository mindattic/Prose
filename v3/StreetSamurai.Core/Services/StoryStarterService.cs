using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Models;

namespace StreetSamurai.Core.Services;

public class StoryStarterService
{
    private readonly ILlmService llm;
    private readonly WorldGraphService graph;
    private readonly LoreService canon;
    private readonly DatabaseService canonDb;
    private readonly FacetService facets;
    private readonly IPathProvider paths;
    private readonly SemanticIndexService semanticIndex;
    private readonly InferenceService inference;
    private readonly ILogger<StoryStarterService> log;

    // Seed premises for zero-input generation — drawn from the world's actual tensions
    private static readonly string[] SeedPremises =
    [
        "A routine contract goes wrong when the target turns out to be someone Kyle owes a debt to.",
        "Pixel's salvaged scanner eye starts showing data from a corporate network that was supposed to be dead.",
        "Sable offers a job that pays triple the normal rate. The catch: the client is Axiom Industries.",
        "Someone is leaving Ghost Ronin symbols in the Shelf — carved into walls, painted on doors. Real or provocation?",
        "A rogue AI in the Circuit starts asking about Kyle by name.",
        "Mrs. Chen's shop is targeted for demolition. The corporate order has Sable's cipher on it.",
        "A child from the Grey Stacks shows up with military-grade augments and no memory of how they got them.",
        "The Collective intercepts a data shipment that contains employee records from a facility in the Mindanao Economic Zone.",
        "Seo's blade — the one Kyle carries — is identified by a Corporate Wars veteran who claims it was taken from a battlefield in the Upper Peninsula.",
        "A blackout hits the Shelf. When power returns, three people are missing and a door that was always locked is open.",
        "Sable cooks dinner for two. The second plate is a negotiation tactic.",
        "Pixel builds something that works too well. Corporate scouts arrive within hours.",
        "An old augment in Kyle's body — one he forgot about — activates and starts transmitting.",
    ];

    public StoryStarterService(
        ILlmService llm, WorldGraphService graph, LoreService canon,
        DatabaseService canonDb, FacetService facets, IPathProvider paths,
        SemanticIndexService semanticIndex, InferenceService inference,
        ILogger<StoryStarterService> log)
    {
        this.llm = llm;
        this.graph = graph;
        this.canon = canon;
        this.canonDb = canonDb;
        this.facets = facets;
        this.paths = paths;
        this.semanticIndex = semanticIndex;
        this.inference = inference;
        this.log = log;
    }

    /// <summary>
    /// Zero-input generation: picks random characters, location, and premise from the canon.
    /// </summary>
    public Task<GeneratedOpening> GenerateRandomAsync(CancellationToken ct = default)
    {
        var allChars = canonDb.Characters;
        var allDistricts = canonDb.Districts;

        // Pick 1-3 characters at random
        var charCount = Random.Shared.Next(1, Math.Min(4, allChars.Count + 1));
        var chars = allChars.OrderBy(_ => Random.Shared.Next()).Take(charCount).Select(c => c.Name).ToList();

        // Pick a location
        var location = allDistricts.Any()
            ? allDistricts[Random.Shared.Next(allDistricts.Count)].Name
            : null;

        // Pick a seed premise
        var premise = SeedPremises[Random.Shared.Next(SeedPremises.Length)];

        // Pick a mood
        string[] moods = ["desperate and claustrophobic", "slow burn dread", "electric and kinetic",
            "melancholic and beautiful", "paranoid and fractured", "tender amid violence", "dark humor and irony"];
        var mood = moods[Random.Shared.Next(moods.Length)];

        return GenerateOpeningAsync(new StoryStarterRequest
        {
            Premise = premise,
            Mood = mood,
            Location = location,
            Characters = chars,
        }, ct);
    }

    public async Task<GeneratedOpening> GenerateOpeningAsync(
        StoryStarterRequest request, CancellationToken ct = default)
    {
        log.LogInformation("GenerateOpeningAsync: characters=[{Characters}], location={Location}, premise={PremisePreview}",
            string.Join(", ", request.Characters), request.Location ?? "none",
            request.Premise.Length > 80 ? request.Premise[..80] + "..." : request.Premise);
        graph.EnsureLoaded();

        // Build world context — graph-first, typed JSON as fallback
        var literaryRules = canonDb.GetLiteraryRulesPrompt();
        var toneBible = canonDb.GetToneBiblePrompt();
        var sensoryPalette = canonDb.GetSensoryPalettePrompt(request.Location);
        var storyBible = JsonStoryBible();

        // Pull full scene context from graph: characters (with gender, pronouns,
        // psychology, speech), locations (with atmosphere), equipment, weapons,
        // affiliations, and 1-hop neighbors for relationship web
        var entityNames = new List<string>(request.Characters);
        if (request.Location != null) entityNames.Add(request.Location);
        var sceneContext = graph.GetSceneContext(entityNames);

        // Fall back to typed JSON if graph is empty
        string locationContext, characterContext;
        if (!string.IsNullOrWhiteSpace(sceneContext))
        {
            characterContext = sceneContext;
            locationContext = ""; // Already included in scene context
        }
        else
        {
            locationContext = BuildLocationContext(request.Location);
            characterContext = BuildCharacterContext(request.Characters);
        }

        var worldFlavor = BuildWorldFlavor();

        // Select lead facet based on character behavioral baselines
        var blended = canonDb.GetBlendedWeights(request.Characters);
        var weights = new FacetState
        {
            Wound = blended.Wound, Ideal = blended.Ideal, Id = blended.Id,
            Shadow = blended.Shadow, Mask = blended.Mask, Ghost = blended.Ghost,
        };
        var seedTriggers = InferTriggers(request);
        var (lead, supporting) = facets.SelectFacets(weights, seedTriggers, []);

        var supportingVoices = string.Join("\n", supporting.Select(f =>
            $"- {f.Label}: {f.VoiceTone}"));

        var system = $"""
            You are a literary fiction author writing the opening of a cyberpunk story
            set in Meridian City — a near-future megacity where corporations hold sovereignty,
            neural interfaces are ubiquitous, and the line between human and machine dissolves
            a little more every day.

            Your lead voice is {lead.Label} — {lead.VoiceTone}.
            {lead.SystemPrompt}

            SUPPORTING FACETS (may surface as brief interior interjections):
            {supportingVoices}

            STORY BIBLE:
            {storyBible}

            {toneBible}

            LITERARY RULES — THESE ARE NON-NEGOTIABLE:
            {literaryRules}

            {sensoryPalette}

            LOCATION:
            {locationContext}

            CHARACTERS:
            {characterContext}

            WORLD DETAILS:
            {worldFlavor}
            """;

        var moodLine = string.IsNullOrWhiteSpace(request.Mood) ? "" : $"\nMOOD/TONE: {request.Mood}";

        var user = $"""
            Write the opening of a story.{moodLine}

            PREMISE: {request.Premise}

            Write 3-5 paragraphs. Drop us into the middle of something.
            The opening should feel like a wound that just started bleeding —
            we don't know what happened yet, but we can't look away.

            End at a moment of tension, ambiguity, or choice. The reader (or another AI)
            will continue from wherever you stop. Leave threads hanging. Leave doors open.

            Write ONLY the story text. No titles, no headers, no metadata.
            """;

        var text = await llm.GenerateAsync(system, user, lead.Temperature, 2048, lead.Model, ct);

        // Generate a title
        var titlePrompt = $"Given this story opening, generate a short, evocative title (2-5 words, no quotes). The title should feel like graffiti on a wall — raw, cryptic, beautiful:\n\n{text}";
        var title = await llm.GenerateAsync(
            "You generate short, evocative titles for cyberpunk fiction. Respond with ONLY the title, nothing else.",
            titlePrompt, 0.9, 50, ct: ct);
        title = title.Trim().Trim('"').Trim('\'');

        log.LogInformation("Opening generated: title={Title}, facet={LeadFacet}, textLen={TextLen}",
            title, lead.Name, text.Length);

        return new GeneratedOpening
        {
            Title = title,
            Text = text,
            LeadFacet = lead.Name,
            SupportingFacets = supporting.Select(f => f.Name).ToList(),
            Characters = request.Characters,
            Location = request.Location,
        };
    }

    private string JsonStoryBible()
    {
        var sb = canonDb.StoryBible;
        var lines = new List<string>();
        if (sb.Title.Length > 0) lines.Add($"Title: {sb.Title}");
        if (sb.Genre.Length > 0) lines.Add($"Genre: {sb.Genre}");
        if (sb.Tone.Length > 0) lines.Add($"Tone: {sb.Tone}");
        if (sb.CoreTheme.Length > 0) lines.Add($"Core Theme: {sb.CoreTheme}");
        if (sb.CoreHook.Length > 0) lines.Add($"Core Hook: {sb.CoreHook}");
        if (sb.Arc.Length > 0) lines.Add($"Arc: {sb.Arc}");
        if (sb.Protagonist.Length > 0) lines.Add($"Protagonist: {sb.Protagonist}");
        return string.Join("\n", lines);
    }

    private string BuildLocationContext(string? location)
    {
        if (string.IsNullOrEmpty(location)) return "Location not specified — choose one that fits.";

        // Get rich context from typed JSON database
        var districtContext = canonDb.GetDistrictContext(location);

        // Supplement with graph relationships
        var id = WorldGraphService.Slugify(location);
        var graphContext = graph.GetContextForNode(id);

        if (districtContext.Length > 0)
        {
            if (graphContext.Length > 0)
                districtContext += $"\n\nRELATIONSHIPS:\n{graphContext}";
            return districtContext;
        }

        return graphContext.Length > 0 ? graphContext : $"Location: {location}";
    }

    private string BuildCharacterContext(List<string> characters)
    {
        if (!characters.Any()) return "No specific characters — introduce someone new from this world.";

        var contexts = new List<string>();
        foreach (var name in characters)
        {
            // Primary: typed JSON data with full psychology
            var ctx = canonDb.GetCharacterContext(name);
            if (ctx.Length > 0)
            {
                // Supplement with graph relationships (broader world connections)
                var id = WorldGraphService.Slugify(name);
                var edges = graph.GetAllEdges(id);
                if (edges.Any())
                {
                    ctx += "\n\nWORLD GRAPH CONNECTIONS:\n";
                    foreach (var edge in edges)
                    {
                        var other = edge.Source == id ? edge.Target : edge.Source;
                        var otherNode = graph.GetNode(other);
                        ctx += $"  [{edge.RelationType}] {otherNode?.Name ?? other}: {edge.Description}\n";
                    }
                }
                contexts.Add(ctx);
            }
            else
            {
                // Fallback: graph-only context
                var id = WorldGraphService.Slugify(name);
                var graphCtx = graph.GetContextForNode(id);
                if (graphCtx.Length > 0) contexts.Add(graphCtx);
            }
        }

        return string.Join("\n\n---\n\n", contexts);
    }

    private string BuildWorldFlavor()
    {
        var lines = new List<string>();

        // Typed corponation data
        var corps = canonDb.Corponations;
        if (corps.Any())
        {
            foreach (var c in corps.OrderBy(_ => Random.Shared.Next()).Take(2))
                lines.Add($"Corponation: {c.Name} — {c.Sector}, territory: {c.SovereignTerritory}");
        }

        // Profile context
        var profile = canonDb.CharacterProfile;
        if (profile.CoreContradiction.Length > 0)
            lines.Add($"PROTAGONIST CONTRADICTION: {profile.CoreContradiction}");

        return string.Join("\n", lines);
    }

    private FacetState LoadBlendedWeights(List<string> characters)
    {
        var allChars = canon.ListCharacters();
        var weights = new List<FacetState>();

        foreach (var name in characters)
        {
            // Match by name or alias
            var match = allChars.FirstOrDefault(c =>
                c.Name.Equals(name, StringComparison.OrdinalIgnoreCase)
                || c.Aliases.Any(a => a.Equals(name, StringComparison.OrdinalIgnoreCase)));

            if (match?.Facets != null && (match.Facets.Wound > 0 || match.Facets.Ideal > 0))
                weights.Add(match.Facets);
        }

        if (weights.Count == 0)
            return new FacetState { Wound = 0.6, Ideal = 0.5, Id = 0.4, Shadow = 0.55, Mask = 0.45, Ghost = 0.5 };

        return new FacetState
        {
            Wound = weights.Average(w => w.Wound),
            Ideal = weights.Average(w => w.Ideal),
            Id = weights.Average(w => w.Id),
            Shadow = weights.Average(w => w.Shadow),
            Mask = weights.Average(w => w.Mask),
            Ghost = weights.Average(w => w.Ghost),
        };
    }

    /// <summary>
    /// Continue a story from existing blocks + a user prompt.
    /// </summary>
    public async Task<string> ContinueAsync(
        List<string> existingParagraphs, string prompt, string? mood, string? location,
        List<string> characters, string? storyConstraints = null,
        string? knowledgeConstraints = null, string? eventContext = null,
        string? outlineContext = null, CancellationToken ct = default)
    {
        log.LogInformation("ContinueAsync: paragraphs={ParagraphCount}, characters=[{Characters}], location={Location}",
            existingParagraphs.Count, string.Join(", ", characters), location ?? "none");
        graph.EnsureLoaded();

        var literaryRules = canonDb.GetLiteraryRulesPrompt();
        var toneBibleCont = canonDb.GetToneBiblePrompt();
        var sensoryPaletteCont = canonDb.GetSensoryPalettePrompt(location);
        var storyBible = JsonStoryBible();

        var session = new NarrativeSessionContext(graph, semanticIndex, inference);
        session.TouchAll(characters);
        if (location != null) session.Touch(location);

        var storySoFar = string.Join("\n\n", existingParagraphs);
        session.ScanText(storySoFar);
        session.ScanTextSemantic(storySoFar);

        var characterContext = session.BuildContext();
        var locationContext = "";

        var blended = canonDb.GetBlendedWeights(characters);
        var weights = new FacetState
        {
            Wound = blended.Wound, Ideal = blended.Ideal, Id = blended.Id,
            Shadow = blended.Shadow, Mask = blended.Mask, Ghost = blended.Ghost,
        };
        var seedTriggers = InferTriggers(new StoryStarterRequest { Premise = prompt, Mood = mood });
        var (lead, _) = facets.SelectFacets(weights, seedTriggers, []);

        var system = $"""
            You are a literary fiction author continuing a cyberpunk story
            set in Meridian City.

            Your voice is {lead.Label} — {lead.VoiceTone}.
            {lead.SystemPrompt}

            STORY BIBLE:
            {storyBible}

            {toneBibleCont}

            LITERARY RULES — THESE ARE NON-NEGOTIABLE:
            {literaryRules}

            {sensoryPaletteCont}

            {(locationContext.Length > 0 ? $"LOCATION:\n{locationContext}" : "")}

            {(characterContext.Length > 0 ? $"CHARACTERS:\n{characterContext}" : "")}

            {(storyConstraints?.Length > 0 ? storyConstraints : "")}

            {(knowledgeConstraints?.Length > 0 ? knowledgeConstraints : "")}

            {(eventContext?.Length > 0 ? eventContext : "")}

            {(outlineContext?.Length > 0 ? outlineContext : "")}
            """;

        var moodLine = string.IsNullOrWhiteSpace(mood) ? "" : $"\nMOOD/TONE: {mood}";

        var user = $"""
            THE STORY SO FAR:
            {storySoFar}

            CONTINUE THE STORY.{moodLine}
            Direction: {prompt}

            Write 2-4 paragraphs continuing from where the story left off.
            Maintain voice, tension, and momentum. Leave threads hanging.
            Write ONLY the story text. No titles, no headers, no metadata.
            """;

        return await llm.GenerateAsync(system, user, lead.Temperature, 2048, lead.Model, ct);
    }

    /// <summary>
    /// Polish/clean up unlocked paragraphs while preserving locked ones.
    /// </summary>
    public async Task<List<string>> PolishAsync(
        List<(string text, bool locked)> blocks, string? mood, string? location,
        List<string> characters, CancellationToken ct = default)
    {
        var literaryRules = canonDb.GetLiteraryRulesPrompt();

        // Build the text with markers so the LLM knows what to touch
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < blocks.Count; i++)
        {
            var (text, locked) = blocks[i];
            if (locked)
                sb.AppendLine($"[LOCKED PARAGRAPH {i + 1} — DO NOT MODIFY]");
            else
                sb.AppendLine($"[PARAGRAPH {i + 1} — POLISH THIS]");
            sb.AppendLine(text);
            sb.AppendLine();
        }

        var system = $"""
            You are a literary editor polishing cyberpunk fiction set in Meridian City.
            You refine prose — tighten sentences, sharpen imagery, fix awkward phrasing,
            remove cliches — without changing the story, characters, or events.

            LITERARY RULES — THESE ARE NON-NEGOTIABLE:
            {literaryRules}
            """;

        var user = $"""
            Polish the following story paragraphs. Paragraphs marked LOCKED must be returned
            EXACTLY as they are — do not change a single word. Paragraphs marked POLISH should
            be refined for clarity, voice, and literary quality.

            Return ONLY the paragraphs separated by blank lines. Same number of paragraphs,
            same order. No labels, no headers, no commentary.

            {sb}
            """;

        var result = await llm.GenerateAsync(system, user, 0.4, 4096, ct: ct);

        // Split back into paragraphs
        var polished = result
            .Split(["\n\n", "\r\n\r\n"], StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim())
            .Where(p => p.Length > 0)
            .ToList();

        return polished;
    }

    /// <summary>
    /// Rewrite a selected passage with an optional direction from the user.
    /// </summary>
    public async Task<string> RewriteAsync(
        string selectedText, string? direction, string? mood, string? location,
        List<string> characters, CancellationToken ct = default)
    {
        graph.EnsureLoaded();
        var literaryRules = canonDb.GetLiteraryRulesPrompt();

        // Graph context for rewrites — ensures character facts stay consistent
        var entityNames = new List<string>(characters);
        if (location != null) entityNames.Add(location);
        var characterContext = graph.GetSceneContext(entityNames);
        if (string.IsNullOrWhiteSpace(characterContext))
            characterContext = BuildCharacterContext(characters);

        var directionLine = string.IsNullOrWhiteSpace(direction)
            ? "Polish and refine — tighten prose, sharpen imagery, fix awkward phrasing. Keep the same events and meaning."
            : direction;

        var moodLine = string.IsNullOrWhiteSpace(mood) ? "" : $"\nMOOD/TONE: {mood}";

        var system = $"""
            You are a literary editor rewriting cyberpunk fiction set in Meridian City.
            You rewrite passages according to the author's direction while maintaining
            consistency with the world, characters, and story.

            LITERARY RULES — THESE ARE NON-NEGOTIABLE:
            {literaryRules}

            {(characterContext.Length > 0 ? $"CHARACTERS:\n{characterContext}" : "")}
            """;

        var user = $"""
            Rewrite the following passage.{moodLine}

            DIRECTION: {directionLine}

            PASSAGE TO REWRITE:
            {selectedText}

            Return ONLY the rewritten text. No labels, no headers, no commentary.
            Maintain the same approximate length unless the direction says otherwise.
            """;

        return await llm.GenerateAsync(system, user, 0.5, 4096, ct: ct);
    }

    private static List<string> InferTriggers(StoryStarterRequest request)
    {
        var triggers = new List<string>();
        var premise = (request.Premise + " " + (request.Mood ?? "")).ToLowerInvariant();

        if (premise.Contains("betray") || premise.Contains("trust")) triggers.Add("betrayal");
        if (premise.Contains("fight") || premise.Contains("violence") || premise.Contains("kill")) triggers.Add("violence");
        if (premise.Contains("loss") || premise.Contains("grief") || premise.Contains("death")) triggers.Add("loss");
        if (premise.Contains("choice") || premise.Contains("moral") || premise.Contains("decision")) triggers.Add("moral_choice");
        if (premise.Contains("memory") || premise.Contains("past") || premise.Contains("remember")) triggers.Add("memory");
        if (premise.Contains("identity") || premise.Contains("who am i") || premise.Contains("self")) triggers.Add("identity_crisis");
        if (premise.Contains("corporate") || premise.Contains("corp") || premise.Contains("power")) triggers.Add("corporate_oppression");
        if (premise.Contains("augment") || premise.Contains("chrome") || premise.Contains("machine")) triggers.Add("transhumanism");
        if (premise.Contains("contract") || premise.Contains("job") || premise.Contains("hire")) triggers.Add("desperation");
        if (premise.Contains("rogue") || premise.Contains("ai") || premise.Contains("machine")) triggers.Add("transhumanism");
        if (premise.Contains("debt") || premise.Contains("owe")) triggers.Add("debt");
        if (premise.Contains("child") || premise.Contains("kid") || premise.Contains("young")) triggers.Add("children_in_danger");

        if (!triggers.Any()) triggers.AddRange(["unknown_danger", "moral_choice"]);
        return triggers;
    }

}

public record StoryStarterRequest
{
    public string Premise { get; init; } = "";
    public string? Mood { get; init; }
    public string? Location { get; init; }
    public List<string> Characters { get; init; } = [];
}

public record GeneratedOpening
{
    public string Title { get; init; } = "";
    public string Text { get; init; } = "";
    public string LeadFacet { get; init; } = "";
    public List<string> SupportingFacets { get; init; } = [];
    public List<string> Characters { get; init; } = [];
    public string? Location { get; init; }
}
