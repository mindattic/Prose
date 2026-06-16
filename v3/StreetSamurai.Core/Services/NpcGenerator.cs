using System.Text.Json;
using System.Text.Json.Serialization;
using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Models.Canon;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Generates NPCs for contracts and encounters. Every NPC is a FULL character —
/// no disposable throwaways. Generated characters are saved to the character
/// repository and become part of the world permanently.
///
/// This means a random guard from contract #3 might become a recurring ally in
/// contract #7 because the system remembers them.
/// </summary>
public class NpcGenerator
{
    private readonly ILlmService llm;
    private readonly DatabaseService db;
    private readonly CharacterRepository charRepo;
    private readonly WorldGraphService graph;
    private readonly NamePoolService namePool;
    private readonly EmbeddingService embeddings;

    public NpcGenerator(ILlmService llm, DatabaseService db, CharacterRepository charRepo,
        WorldGraphService graph, NamePoolService namePool, EmbeddingService embeddings)
    {
        this.llm = llm;
        this.db = db;
        this.charRepo = charRepo;
        this.graph = graph;
        this.namePool = namePool;
        this.embeddings = embeddings;
    }

    /// <summary>
    /// Pull a handful of canon characters semantically closest to the role +
    /// context, format their (name, role, voice) as a short in-context example
    /// block. Anchors generated NPCs to canon voice without listing every
    /// character. Returns "" when the embedding cache is cold.
    /// </summary>
    private async Task<string> BuildCanonExamplesAsync(
        string role, string context, string? location, string? affiliation, CancellationToken ct)
    {
        var query = string.Join(" ", new[] { role, context, location, affiliation }
            .Where(s => !string.IsNullOrWhiteSpace(s)));
        if (string.IsNullOrWhiteSpace(query)) return "";

        IReadOnlyList<EmbeddingHit> hits;
        try { hits = await embeddings.FindSimilarAsync(query, k: 5, entityTypes: new[] { "character" }, ct); }
        catch { return ""; }
        if (hits.Count == 0) return "";

        var sb = new System.Text.StringBuilder();
        sb.AppendLine();
        sb.AppendLine("CANON EXAMPLES (existing characters semantically related to this role — write your new character in similar voice but DISTINCT from these specific people):");
        foreach (var hit in hits)
        {
            var ch = charRepo.GetByName(hit.EntityName);
            if (ch == null) continue;
            sb.Append("  • ").Append(ch.Name);
            if (!string.IsNullOrWhiteSpace(ch.Role)) sb.Append(" — ").Append(ch.Role);
            if (!string.IsNullOrWhiteSpace(ch.Affiliation)) sb.Append(" (").Append(ch.Affiliation).Append(')');
            sb.AppendLine();
            if (!string.IsNullOrWhiteSpace(ch.Description))
            {
                var trimmed = ch.Description.Length > 240 ? ch.Description[..237] + "…" : ch.Description;
                sb.Append("    ").AppendLine(trimmed.Replace("\n", " "));
            }
        }
        return sb.ToString();
    }

    /// <summary>
    /// Generate a full character for a specific narrative role and save to the repo.
    /// Returns the character name for use in story generation.
    /// </summary>
    public async Task<string> GenerateAndSaveAsync(
        string role, string context, string? location = null,
        string? affiliation = null, CancellationToken ct = default)
    {
        var existingNames = db.Characters.Select(c => c.Name).ToHashSet();
        var preferredNames = namePool.SamplePreferredNames(40);
        var usedFirstNames = namePool.SampleUsedFirstNames(60);

        var systemIdentity = UniverseScope.Current?.UniverseGroundingOr("You are a character designer for near-future fiction set in GLMZ (2100).")
            ?? "You are a character designer for near-future fiction set in GLMZ (2100).";
        var system = systemIdentity + """

            Create a COMPLETE character. This person is not disposable — they will persist
            in the world and may recur in future stories.

            NAMING RULES — NON-NEGOTIABLE:
              • GLMZ follows the Ubiquitous Diaspora pattern: mixed heritage from unexpected
                global combinations. Pair a first name and a surname from DIFFERENT cultural
                lineages (e.g., Slavic × Vietnamese, Yoruba × Georgian, Tamil × Finnish).
              • The first name must be UNIQUE in canon — do not reuse any name listed below
                under USED FIRST NAMES.
              • FORBIDDEN first names (never use): Sarah, Lee, Bekka, Karen.
              • Avoid the cyberpunk-generator defaults: Zara, Slate, Echo, Nova, Haze, Atlas,
                Nyx, Flux, Remi, Kit, Kai, Sage, Rio, Phoenix, Juno, Reed, Rowan, Soren, Mika,
                Wren, Rune, Orion, Onyx, Lark, Drift, Ash, Ember, Vega, Indigo, Arden, Zephyr,
                Quinn, Vale, Sterling, Briar. These have been overused.
              • Prefer names from the PREFERRED NAMES list below, or use other authentic
                diaspora names of comparable specificity.

            Return a JSON object matching this EXACT structure:
            {
              "type": "character",
              "name": "First Surname — first name unique in canon, mixed-heritage surname",
              "aliases": ["street name or alias"],
              "gender": "male/female/nonbinary",
              "pronouns": "he/him, she/her, they/them",
              "role": "their function in the world",
              "age": 25,
              "status": "alive",
              "location": "where they operate",
              "description": "2-3 paragraphs. Physical appearance, augmentations, how they carry themselves. Be specific and visual.",
              "affiliation": "who they work for or with",
              "augmentations": "what hardware they carry",
              "daily_life": "what a normal day looks like",
              "narrative_function": "what role they play in stories",
              "psychology": {
                "core_fears": ["2-3 fears"],
                "core_desires": ["2-3 desires"],
                "coping_mechanisms": ["2-3 mechanisms"],
                "blind_spots": ["2-3 blind spots"],
                "secret": "one secret they keep"
              },
              "speech_patterns": {
                "vocabulary": "how they talk",
                "cadence": "rhythm of speech",
                "verbal_tics": ["2-3 speech habits"],
                "example_lines": ["2-3 lines of dialogue"]
              },
              "relationships": [],
              "story_hooks": ["2-3 narrative threads"],
              "behavioral": {
                "decision_rules": ["4-5 rules"],
                "escalation_ladder": ["3-4 steps"],
                "interpersonal_modes": {},
                "stress_responses": {"low": "", "medium": "", "high": "", "critical": ""},
                "contradictions": ["2-3"],
                "habits": ["3-4"],
                "breaking_points": ["2-3"]
              }
            }

            Return ONLY the JSON. No markdown, no explanation.
            """;

        var canonExamples = await BuildCanonExamplesAsync(role, context, location, affiliation, ct);

        var user = $"Create a character for this role: {role}\nContext: {context}" +
            (location != null ? $"\nLocation: {location}" : "") +
            (affiliation != null ? $"\nAffiliation: {affiliation}" : "") +
            canonExamples +
            $"\n\nUSED FIRST NAMES (do NOT reuse any of these as the character's first name):\n  {string.Join(", ", usedFirstNames)}" +
            (preferredNames.Count > 0
                ? $"\n\nPREFERRED FIRST NAMES (pick one of these, or a diaspora name of similar specificity):\n  {string.Join(", ", preferredNames)}"
                : "");

        try
        {
            var response = await llm.GenerateAsync(system, user, 0.9, 3072, ct: ct);
            var json = response.Trim();
            json = JsonDefaults.StripCodeFences(json);

            var character = JsonSerializer.Deserialize<CharacterData>(json.Trim(),
                JsonDefaults.LlmParsing);

            if (character != null && !string.IsNullOrWhiteSpace(character.Name))
            {
                // Guarantee first-name uniqueness — swap from pool if LLM picked a colliding
                // or forbidden first name. Preserves surname, logs swap in aliases[].
                namePool.EnsureUniqueFirstName(character);

                // Belt-and-suspenders: if the full name still collides (shouldn't, after the
                // first-name swap above, unless an identical surname is in play), disambiguate.
                if (existingNames.Contains(character.Name))
                    character.Name += $" ({Random.Shared.Next(100, 999)})";

                // Save to repository — this character is now permanent canon
                charRepo.Save(character);

                // Add to world graph
                var nodeId = WorldGraphService.Slugify(character.Name);
                graph.AddNode(new Models.Graph.WorldNode
                {
                    Id = nodeId,
                    Name = character.Name,
                    NodeType = "character",
                    Properties = new()
                    {
                        ["role"] = character.Role,
                        ["gender"] = character.Gender,
                        ["pronouns"] = character.Pronouns,
                        ["description"] = character.Description.Length > 400 ? character.Description[..397] + "..." : character.Description,
                        ["affiliation"] = character.Affiliation,
                        ["location"] = character.Location,
                    },
                    SourceFile = "npc_generator",
                });

                return character.Name;
            }
        }
        catch (Exception ex) { Serilog.Log.Warning(ex, "NPC generation failed, falling through to fallback"); }

        return "Unknown Operator";
    }

    /// <summary>Generate multiple NPCs for a contract (client, target, complication NPC).</summary>
    public async Task<List<string>> GenerateContractNpcsAsync(Contract contract, CancellationToken ct = default)
    {
        var names = new List<string>();

        // Generate client if not an existing character
        if (!string.IsNullOrWhiteSpace(contract.ClientName) && db.FindCharacter(contract.ClientName) == null)
        {
            var name = await GenerateAndSaveAsync(
                $"Contract client — {contract.ClientName}",
                $"Hiring a freelancer for a {contract.JobType} job. Motivation: {contract.ClientMotivation}",
                contract.TargetLocation, contract.ClientAffiliation, ct);
            names.Add(name);
        }

        // Generate secondary antagonist if needed
        if (!string.IsNullOrWhiteSpace(contract.SecondaryAntagonist) && db.FindCharacter(contract.SecondaryAntagonist) == null)
        {
            var name = await GenerateAndSaveAsync(
                $"Antagonist — {contract.SecondaryAntagonist}",
                $"Opposes the freelancer during a {contract.JobType} job. {contract.Complication}",
                contract.TargetLocation, null, ct);
            names.Add(name);
        }

        return names;
    }
}
