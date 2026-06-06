using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

/// <summary>
/// The "other author" — a second-pass story critic that evaluates a generated outline
/// BEFORE a single word of prose is written.
///
/// This is the structural gatekeeper. It doesn't write the story; it stress-tests the
/// architecture. A weak outline produces weak prose no matter how good the sentence-level
/// generation is. This service enforces:
///
/// — Moral ambiguity: no clean villains, no clean heroes, no easy answers
/// — Earned arcs: character change must cost something real
/// — Anti-cliché: detected tropes get flagged and optionally excised
/// — World specificity: GLMZ mechanics (tiers, corponations, Sponsorship Program) used
/// — Pacing logic: tension should curve, not flatline
/// — "Unapologetic" tone: this world is brutal, don't sanitize it
///
/// ── PIPELINE POSITION ──
/// Called after OutlineService.GenerateOutlineAsync() and before beat writing begins.
/// The revised outline replaces the original — all beats are improved before prose starts.
/// </summary>
public class OutlineReviewService
{
    private const string FailurePatternsKey = "quality_patterns";

    private readonly ILlmService llm;
    private readonly DatabaseService db;
    private readonly SettingsKvStore kv;
    private readonly ILogger<OutlineReviewService> log;

    public OutlineReviewService(
        ILlmService llm, DatabaseService db, SettingsKvStore kv,
        ILogger<OutlineReviewService> log)
    {
        this.llm = llm;
        this.db = db;
        this.kv = kv;
        this.log = log;
    }

    /// <summary>
    /// Review and improve a story outline before any prose is written.
    /// The LLM acts as a hard-nosed story editor who knows the world cold.
    /// Returns a revised outline and notes on what was changed and why.
    /// </summary>
    public async Task<OutlineReviewResult> ReviewAsync(
        StoryOutline outline, CancellationToken ct = default)
    {
        var totalBeats = outline.Acts.SelectMany(a => a.Beats).Count();
        log.LogInformation("OutlineReview starting: title={Title}, acts={Acts}, beats={Beats}",
            outline.Title, outline.Acts.Count, totalBeats);

        var outlineJson = JsonSerializer.Serialize(outline, JsonDefaults.Indented);
        var worldContext = BuildWorldContext(outline.Characters);
        var knownFailures = LoadKnownFailurePatterns();

        // Non-interpolated raw strings for JSON templates — prevents brace conflicts in $"""..."""
        var outputFormat = """
            OUTPUT FORMAT (JSON — return ONLY this, no markdown, no preamble):
            {
              "critique": "overall structural assessment — be specific and harsh",
              "cliche_flags": ["each specific cliché detected with beat reference"],
              "moral_ambiguity_score": <1-10 where 10 is maximum moral complexity>,
              "narrative_strength": <1-10 where 10 is compelling>,
              "pacing_notes": "where tension sags or spikes incorrectly",
              "improvements_made": ["each change made and why"],
              "warnings": ["remaining concerns that couldn't be fully resolved"],
              "revised_outline": <full revised StoryOutline JSON>
            }
            """;

        var schemaExample = """
            The revised_outline must match this schema exactly:
            {"title":"","logline":"","theme":"","premise":"","characters":[],"acts":[{"act_number":1,"name":"","purpose":"","beats":[{"beat_index":0,"title":"","goal":"","characters_present":[],"location":"","emotional_arc":"","stakes":"","seeds":[],"payoffs":[],"tension":5}]}],"character_arcs":[{"character":"","start_state":"","end_state":"","turning_point":"","cost":""}],"seeds_and_payoffs":[{"seed":"","planted_in_beat":0,"payoff":"","payoff_in_beat":0}]}
            """;

        var system = $"""
            You are a ruthless story editor for neo-noir literary fiction set in GLMZ
            (formerly Meridian City — after the Behemoth arrived, the old name became a joke).

            WORLD RULES YOU MUST ENFORCE:
            - There are NO city police. Arcturus Civil Security is the closest equivalent,
              but they're a private contractor with jurisdiction only in contracted zones.
              Writing "cops showed up" or "called the police" is a world violation.
            - Corponations (Libation Corp, Arcturus, Iron Lotus, etc.) are sovereign entities.
              Their borders are bureaucratic gaps — freelancers exploit the seams.
            - The Sponsorship Program: lower-tier citizens can gain mobility by submitting to
              degrading corporate branding. It is humiliating by design. It works because
              people are desperate. Never make it a joke.
            - Tier 1 is lowest (poorest, most marginalized). Tier 5 is highest.
            - The Iowan Behemoth (Meridian 88) is an autonomous machine. NOT synthetic life.
              It is not alive. It does not feel. It processes.
            - GLMZ is dangerous. Violence erupts without narrative permission.
            - The Φ symbol is the QUANTA currency, NOT the Greek letter phi.

            YOUR JOB:
            1. DETECT CLICHÉS and flag them with specific language. Then REWRITE the beat
               to preserve the intent but eliminate the cliché mechanism.
            2. ENFORCE MORAL AMBIGUITY. If the villain is purely evil with no coherent
               worldview, rewrite their motivation. If the protagonist "wins" without cost,
               add the cost. If there's a clean answer to a moral question, corrupt it.
            3. VALIDATE CHARACTER ARCS. Each character's arc must be CAUSED by their
               decisions, not imposed by plot convenience. Check: does each character
               have a turning point that is the inevitable consequence of who they are?
            4. CHECK PACING. The tension curve should build, peak at Act 2 midpoint,
               complicate in Act 2 close, and resolve (not reset) in Act 3.
               Identify flat zones and inject complication.
            5. ENFORCE "UNAPOLOGETIC" TONE. This world doesn't reward good behavior.
               Violence has lasting consequences. Intimacy is complicated.
               Corporate control is pervasive but invisible. Poverty is textured.
               Check every beat: is this sanitized? If so, unsanitize it.
            6. SEED/PAYOFF INTEGRITY. Every planted seed must have a payoff.
               Every payoff must have a planted seed. No orphaned threads.

            {(worldContext.Length > 0 ? $"CHARACTERS IN THIS STORY:\n{worldContext}" : "")}

            {(knownFailures.Length > 0 ? $"KNOWN FAILURE PATTERNS TO AVOID (from previous stories):\n{knownFailures}" : "")}

            CLICHÉS TO SPECIFICALLY WATCH FOR:
            - "Chosen one" narrative — protagonist is special because they're the protagonist
            - Villain explains their entire plan before the climax
            - Convenient coincidences that save the protagonist at the last moment
            - Characters who exist only to die and motivate the protagonist
            - Unearned redemption — villain suddenly remembers they have a heart
            - Clean victories — protagonist wins without losing something real
            - "The whole thing was a test" reveals
            - Tragedy because someone didn't say the thing they needed to say
            - Final speech that perfectly articulates the theme
            - Violence that resets at scene end (no lasting consequence)

            {outputFormat}

            {schemaExample}
            """;

        var user = $"Review and improve this story outline:\n\n{outlineJson}";

        string response;
        try
        {
            response = await llm.GenerateAsync(system, user, 0.7, 16384, ct: ct);
            log.LogDebug("OutlineReview LLM response: {Len} chars", response.Length);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Expected — user cancelled (navigated away, clicked Cancel, etc). No stack-trace noise.
            log.LogDebug("OutlineReview cancelled");
            return new OutlineReviewResult
            {
                RevisedOutline = outline,
                Critique = "Review cancelled",
                Warnings = ["Cancelled"]
            };
        }
        catch (Exception ex)
        {
            log.LogError(ex, "OutlineReview LLM call failed — using original outline");
            return new OutlineReviewResult
            {
                RevisedOutline = outline,
                Critique = "Review failed — using original outline",
                Warnings = ["LLM call failed: " + ex.Message]
            };
        }

        var json = response.Trim();
        json = JsonDefaults.StripCodeFences(json);
        json = json.Trim();

        try
        {
            var raw = JsonDocument.Parse(json).RootElement;
            var result = new OutlineReviewResult
            {
                Critique = raw.TryGetProperty("critique", out var c) ? c.GetString() ?? "" : "",
                MoralAmbiguityScore = raw.TryGetProperty("moral_ambiguity_score", out var ms) ? ms.GetInt32() : 5,
                NarrativeStrength = raw.TryGetProperty("narrative_strength", out var ns) ? ns.GetInt32() : 5,
                PacingNotes = raw.TryGetProperty("pacing_notes", out var pn) ? pn.GetString() ?? "" : "",
            };

            if (raw.TryGetProperty("cliche_flags", out var cf))
                result.ClicheFlags = cf.EnumerateArray().Select(x => x.GetString() ?? "").Where(s => s.Length > 0).ToList();

            if (raw.TryGetProperty("improvements_made", out var im))
                result.ImprovementsMade = im.EnumerateArray().Select(x => x.GetString() ?? "").Where(s => s.Length > 0).ToList();

            if (raw.TryGetProperty("warnings", out var w))
                result.Warnings = w.EnumerateArray().Select(x => x.GetString() ?? "").Where(s => s.Length > 0).ToList();

            // Parse revised outline
            if (raw.TryGetProperty("revised_outline", out var ro))
            {
                try
                {
                    var revised = JsonSerializer.Deserialize<StoryOutline>(
                        ro.GetRawText(), JsonDefaults.LlmParsing);
                    if (revised != null && revised.Acts.Count > 0)
                    {
                        // Preserve premise and character list from original
                        revised.Premise = outline.Premise;
                        if (revised.Characters.Count == 0) revised.Characters = outline.Characters;
                        result.RevisedOutline = revised;
                    }
                    else
                    {
                        result.RevisedOutline = outline;
                        result.Warnings.Add("Revised outline parse returned empty acts — using original");
                    }
                }
                catch (Exception ex)
                {
                    log.LogWarning(ex, "Failed to parse revised_outline — using original");
                    result.RevisedOutline = outline;
                    result.Warnings.Add("Revised outline could not be parsed — using original");
                }
            }
            else
            {
                result.RevisedOutline = outline;
                result.Warnings.Add("No revised_outline in response — using original");
            }

            log.LogInformation("OutlineReview complete: moral={Moral}/10, strength={Strength}/10, cliches={Cliches}, improvements={Improvements}",
                result.MoralAmbiguityScore, result.NarrativeStrength,
                result.ClicheFlags.Count, result.ImprovementsMade.Count);

            if (result.ClicheFlags.Count > 0)
                log.LogWarning("Clichés detected: {Flags}", string.Join("; ", result.ClicheFlags));

            return result;
        }
        catch (Exception ex)
        {
            log.LogError(ex, "OutlineReview JSON parse failed — using original outline");
            return new OutlineReviewResult
            {
                RevisedOutline = outline,
                Critique = "Review JSON parse failed",
                Warnings = ["Parse error: " + ex.Message]
            };
        }
    }

    /// <summary>
    /// Save the review result keyed by chapter/project id. Pre-cutover this wrote
    /// next to the chapter folder; now it lives in Settings('outline_review:{id}')
    /// so the SQL store is the single source of truth.
    /// </summary>
    public void Save(string projectId, OutlineReviewResult result)
    {
        kv.Set($"outline_review:{projectId}", result);
    }

    /// <summary>
    /// Load accumulated failure patterns from the Settings table. These are patterns
    /// that scored poorly in quality evaluations across previous stories. Injected
    /// into the review prompt so the editor knows what to avoid.
    /// </summary>
    private string LoadKnownFailurePatterns()
    {
        var doc = kv.Get<QualityPatternsDoc>(FailurePatternsKey);
        if (doc?.FailurePatterns == null || doc.FailurePatterns.Count == 0) return "";

        var lines = doc.FailurePatterns
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => $"- {p}")
            .Take(20) // Cap to keep prompt from ballooning
            .ToList();

        return lines.Count > 0 ? string.Join("\n", lines) : "";
    }

    private sealed class QualityPatternsDoc
    {
        public List<string>? FailurePatterns { get; set; }
    }

    /// <summary>Build character context for the review system prompt.</summary>
    private string BuildWorldContext(List<string> characters)
    {
        if (characters.Count == 0) return "";

        var lines = new List<string>();
        foreach (var name in characters)
        {
            var c = db.FindCharacter(name);
            if (c == null) continue;

            var desc = c.Description.Length > 200 ? c.Description[..200] + "..." : c.Description;
            var desires = c.Psychology.CoreDesires.Count > 0
                ? $" Wants: {string.Join(", ", c.Psychology.CoreDesires.Take(2))}." : "";
            var fears = c.Psychology.CoreFears.Count > 0
                ? $" Fears: {string.Join(", ", c.Psychology.CoreFears.Take(2))}." : "";

            lines.Add($"  {name}: {desc}{desires}{fears}");
        }

        return string.Join("\n", lines);
    }
}

public class OutlineReviewResult
{
    [JsonPropertyName("critique")] public string Critique { get; set; } = "";
    [JsonPropertyName("cliche_flags")] public List<string> ClicheFlags { get; set; } = [];
    [JsonPropertyName("moral_ambiguity_score")] public int MoralAmbiguityScore { get; set; }
    [JsonPropertyName("narrative_strength")] public int NarrativeStrength { get; set; }
    [JsonPropertyName("pacing_notes")] public string PacingNotes { get; set; } = "";
    [JsonPropertyName("improvements_made")] public List<string> ImprovementsMade { get; set; } = [];
    [JsonPropertyName("warnings")] public List<string> Warnings { get; set; } = [];
    [JsonPropertyName("revised_outline")] public StoryOutline RevisedOutline { get; set; } = new();
}
