using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Models;

namespace StreetSamurai.Core.Services;

public class StoryStarterService
{
    private readonly ILlmService _llm;
    private readonly WorldGraphService _graph;
    private readonly CanonService _canon;
    private readonly CanonDatabaseService _canonDb;
    private readonly FacetService _facets;
    private readonly ICanonPathProvider _paths;
    private readonly YamlService _yaml;

    // Seed premises for zero-input generation — drawn from the world's actual tensions
    private static readonly string[] SeedPremises =
    [
        "A routine contract goes wrong when the target turns out to be someone Kael owes a debt to.",
        "Pixel's salvaged scanner eye starts showing data from a corporate network that was supposed to be dead.",
        "Sable offers a job that pays triple the normal rate. The catch: the client is Axiom Industries.",
        "Someone is leaving Ghost Ronin symbols in the Shelf — carved into walls, painted on doors. Real or provocation?",
        "A rogue AI in the Circuit starts asking about Kael by name.",
        "Mrs. Chen's shop is targeted for demolition. The corporate order has Sable's cipher on it.",
        "A child from the Grey Stacks shows up with military-grade augments and no memory of how they got them.",
        "The Collective intercepts a data shipment that contains employee records from a facility in the Mindanao Economic Zone.",
        "Tanaka's sword — the one Kael carries — is identified by a Corporate Wars veteran who says it was stolen from a dead ronin.",
        "A blackout hits the Shelf. When power returns, three people are missing and a door that was always locked is open.",
        "Sable cooks dinner for two. The second plate is a negotiation tactic.",
        "Pixel builds something that works too well. Corporate scouts arrive within hours.",
        "An old augment in Kael's body — one he forgot about — activates and starts transmitting.",
    ];

    public StoryStarterService(
        ILlmService llm, WorldGraphService graph, CanonService canon,
        CanonDatabaseService canonDb, FacetService facets, ICanonPathProvider paths, YamlService yaml)
    {
        _llm = llm;
        _graph = graph;
        _canon = canon;
        _canonDb = canonDb;
        _facets = facets;
        _paths = paths;
        _yaml = yaml;
    }

    /// <summary>
    /// Zero-input generation: picks random characters, location, and premise from the canon.
    /// </summary>
    public Task<GeneratedOpening> GenerateRandomAsync(CancellationToken ct = default)
    {
        var allChars = _canonDb.Characters;
        var allDistricts = _canonDb.Districts;

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
        _graph.EnsureLoaded();

        // Build world context from typed JSON database
        var literaryRules = _canonDb.GetLiteraryRulesPrompt();
        var storyBible = JsonStoryBible();
        var locationContext = request.Location != null ? _canonDb.GetDistrictContext(request.Location) : "Location not specified — choose one that fits.";
        var characterContext = BuildCharacterContext(request.Characters);
        var worldFlavor = BuildWorldFlavor();

        // Select lead facet based on character behavioral baselines
        var blended = _canonDb.GetBlendedWeights(request.Characters);
        var weights = new FacetState
        {
            Wound = blended.Wound, Ideal = blended.Ideal, Id = blended.Id,
            Shadow = blended.Shadow, Mask = blended.Mask, Ghost = blended.Ghost,
        };
        var seedTriggers = InferTriggers(request);
        var (lead, supporting) = _facets.SelectFacets(weights, seedTriggers, []);

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

            LITERARY RULES — THESE ARE NON-NEGOTIABLE:
            {literaryRules}

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

        var text = await _llm.GenerateAsync(system, user, lead.Temperature, 2048, lead.Model, ct);

        // Generate a title
        var titlePrompt = $"Given this story opening, generate a short, evocative title (2-5 words, no quotes). The title should feel like graffiti on a wall — raw, cryptic, beautiful:\n\n{text}";
        var title = await _llm.GenerateAsync(
            "You generate short, evocative titles for cyberpunk fiction. Respond with ONLY the title, nothing else.",
            titlePrompt, 0.9, 50, ct: ct);
        title = title.Trim().Trim('"').Trim('\'');

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
        var sb = _canonDb.StoryBible;
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

    private string LoadLiteraryRules()
    {
        var lines = new List<string>();

        // Load literary_rules.yaml
        var rulesPath = Path.Combine(_paths.WorldDir, "literary_rules.yaml");
        if (File.Exists(rulesPath))
        {
            var content = File.ReadAllText(rulesPath);
            lines.Add(content);
        }

        // Load motifs.yaml
        var motifsPath = Path.Combine(_paths.WorldDir, "motifs.yaml");
        if (File.Exists(motifsPath))
        {
            lines.Add("\nMOTIFS:");
            lines.Add(File.ReadAllText(motifsPath));
        }

        if (!lines.Any())
        {
            // Fallback hardcoded rules
            lines.Add("""
                - Sentence max: 25 words
                - Every paragraph: action, sensory detail, or a lie
                - No generic noir, no slogans, no samurai cliches, no anime dialogue
                - No clean moral victories
                - Characters reveal themselves through action, not introspection
                """);
        }

        return string.Join("\n", lines);
    }

    private string LoadStoryBible()
    {
        var path = Path.Combine(_paths.WorldDir, "story_bible.yaml");
        if (!File.Exists(path)) return "";
        return File.ReadAllText(path);
    }

    private string BuildLocationContext(string? location)
    {
        if (string.IsNullOrEmpty(location)) return "Location not specified — choose one that fits.";

        // First get graph context (relationships)
        var id = WorldGraphService.Slugify(location);
        var graphContext = _graph.GetContextForNode(id);

        // Then get the full YAML for rich sensory detail
        var dir = Path.Combine(_paths.EssencesDir, "world", "districts");
        if (Directory.Exists(dir))
        {
            var match = Directory.GetFiles(dir, "*.yaml")
                .FirstOrDefault(f => Path.GetFileNameWithoutExtension(f)
                    .Contains(location.Replace(" ", "_").ToLowerInvariant(), StringComparison.OrdinalIgnoreCase));
            if (match != null)
            {
                var yaml = File.ReadAllText(match);
                var desc = ExtractBlock(yaml, "description");
                var sights = ExtractList(yaml, "sights");
                var sounds = ExtractList(yaml, "sounds");
                var smells = ExtractList(yaml, "smells");
                var feel = ExtractBlock(yaml, "feel");
                var dangers = ExtractList(yaml, "dangers");

                var parts = new List<string> { $"LOCATION: {location}" };
                if (desc.Length > 0) parts.Add(desc);
                if (sights.Any()) parts.Add("SIGHTS: " + string.Join("; ", sights.Take(4)));
                if (sounds.Any()) parts.Add("SOUNDS: " + string.Join("; ", sounds.Take(4)));
                if (smells.Any()) parts.Add("SMELLS: " + string.Join("; ", smells.Take(3)));
                if (feel.Length > 0) parts.Add("FEEL: " + feel);
                if (dangers.Any()) parts.Add("DANGERS: " + string.Join("; ", dangers.Take(3)));
                if (graphContext.Length > 0) parts.Add("\nRELATIONSHIPS:\n" + graphContext);

                return string.Join("\n", parts);
            }
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
            var ctx = _canonDb.GetCharacterContext(name);
            if (ctx.Length > 0)
            {
                // Supplement with graph relationships (broader world connections)
                var id = WorldGraphService.Slugify(name);
                var edges = _graph.GetAllEdges(id);
                if (edges.Any())
                {
                    ctx += "\n\nWORLD GRAPH CONNECTIONS:\n";
                    foreach (var edge in edges)
                    {
                        var other = edge.Source == id ? edge.Target : edge.Source;
                        var otherNode = _graph.GetNode(other);
                        ctx += $"  [{edge.RelationType}] {otherNode?.Name ?? other}: {edge.Description}\n";
                    }
                }
                contexts.Add(ctx);
            }
            else
            {
                // Fallback: graph-only context
                var id = WorldGraphService.Slugify(name);
                var graphCtx = _graph.GetContextForNode(id);
                if (graphCtx.Length > 0) contexts.Add(graphCtx);
            }
        }

        return string.Join("\n\n---\n\n", contexts);
    }

    private string BuildWorldFlavor()
    {
        var lines = new List<string>();

        // Typed corponation data
        var corps = _canonDb.Corponations;
        if (corps.Any())
        {
            foreach (var c in corps.OrderBy(_ => Random.Shared.Next()).Take(2))
                lines.Add($"Corponation: {c.Name} — {c.Sector}, territory: {c.SovereignTerritory}");
        }

        // Profile context
        var profile = _canonDb.CharacterProfile;
        if (profile.CoreContradiction.Length > 0)
            lines.Add($"PROTAGONIST CONTRADICTION: {profile.CoreContradiction}");

        return string.Join("\n", lines);
    }

    private FacetState LoadBlendedWeights(List<string> characters)
    {
        var allChars = _canon.ListCharacters();
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

    // YAML extraction helpers (work on raw text, not parsed dictionaries)
    private static string ExtractBlock(string yaml, string field)
    {
        var m = System.Text.RegularExpressions.Regex.Match(yaml,
            $@"^  {System.Text.RegularExpressions.Regex.Escape(field)}:\s*\|?\s*\n((?:\s{{4,}}.+\n?)+)",
            System.Text.RegularExpressions.RegexOptions.Multiline);
        if (!m.Success) return "";
        return string.Join("\n", m.Groups[1].Value.Split('\n')
            .Select(l => l.TrimStart()).Where(l => l.Length > 0));
    }

    private static List<string> ExtractList(string yaml, string field)
    {
        var m = System.Text.RegularExpressions.Regex.Match(yaml,
            $@"^  {System.Text.RegularExpressions.Regex.Escape(field)}:\s*\n((?:\s+- .+\n?)+)",
            System.Text.RegularExpressions.RegexOptions.Multiline);
        if (!m.Success) return [];
        return m.Groups[1].Value.Split('\n')
            .Select(l => l.Trim().TrimStart('-').Trim().Trim('"'))
            .Where(l => l.Length > 0).ToList();
    }

    private static string Truncate(string s, int max) =>
        s.Length > max ? s[..(max - 3)] + "..." : s;
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
