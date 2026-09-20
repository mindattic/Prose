using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Prose.Core.Interfaces;
using Prose.Core.Models.Canon;

namespace Prose.Core.Services;

/// <summary>
/// Post-generation canon consistency checker.
/// Extracts named entities and relationship claims from any generated text,
/// resolves them against the Xref index, and auto-scaffolds stub CharacterData
/// records for anything not yet in the world — so canon grows to match the stories.
///
/// Design: never blocks generation. Call fire-and-forget after any text is finalized.
/// Works on stories, character descriptions, place entries, any generated content.
/// </summary>
public class CanonGroundingService
{
    private readonly ILlmService llm;
    private readonly XrefService xref;
    private readonly CharacterRepository characters;
    private readonly IPathProvider paths;
    private readonly FindingsService findings;
    private readonly Audit.ProvenanceService provenance;
    private readonly ILogger<CanonGroundingService> log;

    public CanonGroundingService(
        ILlmService llm, XrefService xref, CharacterRepository characters,
        IPathProvider paths, FindingsService findings, Audit.ProvenanceService provenance,
        ILogger<CanonGroundingService> log)
    {
        this.llm = llm;
        this.xref = xref;
        this.characters = characters;
        this.paths = paths;
        this.findings = findings;
        this.provenance = provenance;
        this.log = log;
    }

    /// <summary>
    /// Analyze text, resolve all named entities against canon, auto-scaffold stubs
    /// for unresolved character-type entities, and return the full grounding report.
    /// </summary>
    public async Task<CanonGroundingResult> AnalyzeAndScaffoldAsync(
        string text,
        string sourceContext = "",
        CancellationToken ct = default)
    {
        xref.EnsureBuilt();

        var extracted = await ExtractEntitiesAsync(text, sourceContext, ct);
        var result = new CanonGroundingResult { SourceContext = sourceContext };

        foreach (var entity in extracted)
        {
            var entry = xref.Resolve(entity.Name);
            if (entry != null)
            {
                result.Resolved.Add(new ResolvedReference
                {
                    Name = entity.Name,
                    EntityType = entry.Type,
                    EntityId = entry.Id
                });
            }
            else
            {
                if (entity.InferredType is "character" or "person" or "unknown")
                {
                    var stub = ScaffoldCharacter(entity, out var unparsedClaims);
                    characters.Save(stub);
                    // Grade the entity row itself scaffolded (Story Ledger Phase 3). The
                    // "auto-scaffolded"/"needs-review" TAGS above already said this, but tags are
                    // free text nothing queries or enforces — a grade is a column
                    // --provenance-audit counts, and the one thing that separates "a model
                    // invented this while writing a beat" from author-approved canon after the
                    // fact. Done here rather than in CharacterRepository.Save because the SAVE is
                    // not what makes a record provisional; this call site is.
                    await GradeScaffoldedAsync(stub.Id, entity.Name, ct);
                    entity.Scaffolded = true;
                    entity.ScaffoldedId = stub.Id;
                    result.EntitiesScaffolded++;
                    log.LogInformation(
                        "Canon grounding: scaffolded PROVISIONAL stub for '{Name}' (id={Id}, source='{Source}')",
                        entity.Name, stub.Id, sourceContext);
                    // Don't grow canon silently — flag the provisional stub for review.
                    TryFlag(entity.Name, sourceContext,
                        $"PROVISIONAL-ENTITY [{entity.InferredType}] '{entity.Name}' was auto-created as a needs-review stub from prose. Confirm, merge into an existing entity, or remove.");
                    // A claim that isn't relationship-shaped is dropped rather than written as a
                    // malformed row — but it is never dropped SILENTLY, or the prose that produced
                    // it goes unexamined.
                    if (unparsedClaims.Count > 0)
                    {
                        TryFlag(entity.Name, sourceContext,
                            $"UNPARSED-RELATIONSHIP '{entity.Name}': {unparsedClaims.Count} claim(s) were not relationship-shaped and were NOT written to canon — {string.Join(" | ", unparsedClaims.Take(5))}. Either the prose asserts something that needs a real relationship, or the claim is noise.");
                    }
                }
                else
                {
                    // A non-character entity named in prose that isn't in canon — surface
                    // it (previously dropped silently) so it can be created or corrected.
                    TryFlag(entity.Name, sourceContext,
                        $"PROVISIONAL-ENTITY [{entity.InferredType}] '{entity.Name}' appears in prose but isn't in canon. Add it, or fix the prose to use an existing entity.");
                }
                result.Unresolved.Add(entity);
            }
        }

        SaveLog(result);
        return result;
    }

    /// <summary>
    /// Mark a freshly-scaffolded stub's Entity row <see cref="ClaimProvenance.Scaffolded"/>.
    /// Never throws into the grounding pass: this service's contract is "never blocks generation",
    /// so a failed grade is logged and the stub survives ungraded rather than losing the record.
    /// </summary>
    private async Task GradeScaffoldedAsync(string? stubId, string name, CancellationToken ct)
    {
        if (!Guid.TryParse(stubId, out var id)) return;
        try { await provenance.SetEntityProvenanceAsync(id, ClaimProvenance.Scaffolded, ct); }
        catch (Exception ex) { log.LogWarning(ex, "Failed to grade scaffolded stub '{Name}' ({Id})", name, id); }
    }

    /// <summary>Raise a low-severity PROVISIONAL-ENTITY finding so unknown
    /// entities are surfaced for review instead of silently scaffolded/dropped.</summary>
    private void TryFlag(string name, string source, string summary)
    {
        try
        {
            findings.Upsert(
                filePath:     string.IsNullOrWhiteSpace(source) ? "canon-grounding" : source,
                chapterId:    null,
                category:     FindingCategory.Other,
                severity:     FindingSeverity.Low,
                summary:      summary,
                snippet:      null,
                suggestedFix: "Review in /findings: confirm, merge into an existing entity, create it, or correct the prose.");
        }
        catch (Exception ex) { log.LogWarning(ex, "Failed to flag provisional entity {Name}", name); }
    }

    private async Task<List<ProposedEntity>> ExtractEntitiesAsync(
        string text, string context, CancellationToken ct)
    {
        var systemIdentity = UniverseScope.Current?.UniverseGroundingOr(
            "You are a canon consistency analyzer for a neo-noir worldbuilding project set in 2250s GLMZ.")
            ?? "You are a canon consistency analyzer for a neo-noir worldbuilding project set in 2250s GLMZ.";
        const string system = """
            Extract all named entities and relationship claims from the provided text.

            Return a JSON array — no other text. Each element:
            {
              "name": "exact proper noun as written",
              "inferred_type": "character" | "place" | "faction" | "synthetic" | "technology" | "unknown",
              "context_snippet": "the sentence or clause where this name appears (max 120 chars)",
              "relationship_claims": ["nephew of Barber Vasquez", "works for Arcturus", ...]
            }

            Rules:
            - Named proper nouns only: people, places, organizations, machines with names
            - Skip common nouns, generic terms, and pronouns
            - Include every person mentioned by name, even briefly
            - Relationship claims describe how this entity relates to other named entities
            - Human-sounding names with no clear type → use "character"
            - Return an empty array [] if no named entities are found
            """;
        var systemPrompt = systemIdentity + "\n            " + system.TrimStart();

        var user = string.IsNullOrWhiteSpace(context)
            ? text
            : $"Source context: {context}\n\n{text}";

        try
        {
            var response = await llm.GenerateAsync(systemPrompt, user, 0.1, 4096, ct: ct);
            var json = response.Trim();
            json = JsonDefaults.StripCodeFences(json);

            var raw = JsonSerializer.Deserialize<List<ExtractedEntity>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return raw?.Select(e => new ProposedEntity
            {
                Name = e.Name ?? "",
                InferredType = e.InferredType ?? "unknown",
                ContextSnippet = e.ContextSnippet ?? "",
                RelationshipClaims = e.RelationshipClaims ?? []
            }).Where(e => e.Name.Length >= 2).ToList() ?? [];
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Canon grounding: entity extraction failed for source '{Context}'", context);
            return [];
        }
    }

    /// <summary>
    /// Connectors a relationship claim may hinge on, longest-first so " of " never
    /// pre-empts a longer match. Deliberately short and conservative: a connector that
    /// mostly introduces a date or a place (" from ", " on ", " in ") produces garbage
    /// relationships far more often than real ones.
    /// </summary>
    private static readonly string[] RelationshipConnectors = [" with ", " for ", " of ", " to ", " by ", " at "];

    /// <summary>
    /// Parse one LLM-proposed relationship claim ("nephew of Barber Vasquez",
    /// "works for Arcturus") into a typed relationship, or return null if the claim
    /// is not relationship-shaped.
    ///
    /// This used to be a bare <c>claim.Split(" of ", 2)</c> that wrote a row no matter
    /// what came back — a claim with no " of " produced <c>Name = ""</c> and dumped the
    /// whole raw sentence into both Type and Description. That is how a set of BCODA
    /// sentences ("gave Kyle his katana", "his funeral") ended up as relationship rows on
    /// an unrelated character in another book. The invariant now: a relationship row is
    /// only ever written when BOTH a plausible type and a non-empty, proper-noun-shaped
    /// target were actually recovered. Everything else is dropped and reported.
    /// </summary>
    internal static CharacterRelationship? TryParseRelationshipClaim(string claim)
    {
        if (string.IsNullOrWhiteSpace(claim)) return null;
        claim = claim.Trim();

        foreach (var connector in RelationshipConnectors)
        {
            var idx = claim.IndexOf(connector, StringComparison.OrdinalIgnoreCase);
            if (idx <= 0) continue;

            var type = claim[..idx].Trim();
            var target = claim[(idx + connector.Length)..].Trim();
            if (type.Length == 0 || target.Length == 0) continue;

            // A relationship TYPE is a word or two ("nephew", "works", "allied"), never a
            // clause. "Kyle was not informed" is a sentence fragment, not a relation.
            if (type.Length > 30 || type.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length > 3) continue;

            // A TARGET is a named thing: a proper noun, or a definite description.
            // This rejects dates ("2189 through 2193") and possessives ("his funeral").
            var looksNamed = char.IsUpper(target[0])
                             || target.StartsWith("the ", StringComparison.OrdinalIgnoreCase);
            if (!looksNamed || target.Length > 80) continue;

            // Graded scaffolded, never canon — this parser's output is a candidate a human has
            // not seen (Story Ledger Phase 3). The seven contaminating rows were undetectable as
            // machine-authored once written; the grade is what keeps that distinction.
            return new CharacterRelationship
            {
                Name = target, Type = type, Description = claim,
                Provenance = ClaimProvenance.Scaffolded,
            };
        }

        return null;
    }

    private static CharacterData ScaffoldCharacter(ProposedEntity entity, out List<string> unparsedClaims)
    {
        var stub = new CharacterData
        {
            Name = entity.Name,
            Description = $"Auto-scaffolded. First mentioned: \"{entity.ContextSnippet}\"",
            Status = "alive",
            Tags = ["auto-scaffolded", "needs-review"]
        };

        unparsedClaims = [];
        foreach (var claim in entity.RelationshipClaims)
        {
            var parsed = TryParseRelationshipClaim(claim);
            if (parsed is null) { unparsedClaims.Add(claim); continue; }
            stub.Relationships.Add(parsed);
        }

        return stub;
    }

    private void SaveLog(CanonGroundingResult result)
    {
        try
        {
            var logsDir = Path.Combine(paths.EngineDataDir, "logs", "canon_grounding");
            Directory.CreateDirectory(logsDir);
            var fileName = $"{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.CreateVersion7().ToString("N")[..8]}.json";
            File.WriteAllText(
                Path.Combine(logsDir, fileName),
                JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Canon grounding: failed to save log");
        }
    }

    private sealed class ExtractedEntity
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("inferred_type")] public string? InferredType { get; set; }
        [JsonPropertyName("context_snippet")] public string? ContextSnippet { get; set; }
        [JsonPropertyName("relationship_claims")] public List<string>? RelationshipClaims { get; set; }
    }
}

public class CanonGroundingResult
{
    [JsonPropertyName("source_context")] public string SourceContext { get; set; } = "";
    [JsonPropertyName("analyzed_at")] public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
    [JsonPropertyName("resolved")] public List<ResolvedReference> Resolved { get; set; } = [];
    [JsonPropertyName("unresolved")] public List<ProposedEntity> Unresolved { get; set; } = [];
    [JsonPropertyName("entities_scaffolded")] public int EntitiesScaffolded { get; set; }
}

public class ResolvedReference
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("entity_type")] public string EntityType { get; set; } = "";
    [JsonPropertyName("entity_id")] public string EntityId { get; set; } = "";
}

public class ProposedEntity
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("inferred_type")] public string InferredType { get; set; } = "";
    [JsonPropertyName("context_snippet")] public string ContextSnippet { get; set; } = "";
    [JsonPropertyName("relationship_claims")] public List<string> RelationshipClaims { get; set; } = [];
    [JsonPropertyName("scaffolded")] public bool Scaffolded { get; set; }
    [JsonPropertyName("scaffolded_id")] public string? ScaffoldedId { get; set; }
}
