using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Models.Canon;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Evolves character facet weights based on what they express in each scene.
/// After a beat is written, analyzes the prose to determine which psychological
/// facets the character activated and applies small weight shifts to their canon JSON.
///
/// Facets (from FacetData): wound, ideal, id, shadow, mask, ghost.
/// Each facet is a psychological force that drives character behavior:
///   WOUND  — Defining trauma; active when defensive, triggered, or cornered
///   IDEAL  — Highest aspiration; active when principled, hopeful, self-sacrificing
///   ID     — Raw desire/instinct; active when unguarded, desperate, or hungry
///   SHADOW — Dark mirror of ideal; active when hypocritical or self-deceiving
///   MASK   — Social persona; active in social performance, deception, or professional mode
///   GHOST  — Haunting past; active when memory intrudes or history repeats
///
/// Shifts are tiny (max ±0.05 per beat) so psychology changes slowly across a story arc.
/// Weights are clamped to [0.0, 1.0] independently (they are relative influences, not a sum-to-1 simplex).
/// </summary>
public class FacetEvolutionService
{
    private readonly ILlmService llm;
    private readonly CharacterRepository charRepo;
    private readonly ILogger<FacetEvolutionService> log;

    private const double MaxShiftPerBeat = 0.05;

    public FacetEvolutionService(
        ILlmService llm,
        CharacterRepository charRepo,
        ILogger<FacetEvolutionService> log)
    {
        this.llm = llm;
        this.charRepo = charRepo;
        this.log = log;
    }

    /// <summary>
    /// Analyze a beat's prose and apply facet weight shifts to the character's canonical JSON.
    /// No-ops silently if the character is not in the DB.
    /// </summary>
    public async Task<FacetShiftResult> AnalyzeAndApplyAsync(
        string beatText,
        string characterName,
        string beatGoal,
        CancellationToken ct = default)
    {
        var character = charRepo.GetByName(characterName);
        if (character == null)
            return new FacetShiftResult { CharacterName = characterName };

        var shifts = await AnalyzeBeatAsync(beatText, characterName, character.Psychology.FacetWeights, beatGoal, ct);

        if (shifts.HasSignificantShift)
        {
            ApplyShifts(character, shifts);
            charRepo.Save(character);
            log.LogInformation(
                "Facet shift applied to {Character}: wound={W:+0.00;-0.00} ideal={I:+0.00;-0.00} id={Id:+0.00;-0.00} shadow={Sh:+0.00;-0.00} mask={M:+0.00;-0.00} ghost={G:+0.00;-0.00} — {Rationale}",
                characterName,
                shifts.WoundDelta, shifts.IdealDelta, shifts.IdDelta,
                shifts.ShadowDelta, shifts.MaskDelta, shifts.GhostDelta,
                shifts.Rationale);
        }

        return shifts;
    }

    /// <summary>
    /// Analyze beat prose and propose facet weight deltas for a character.
    /// Returns zero shifts if the LLM call fails.
    /// </summary>
    public async Task<FacetShiftResult> AnalyzeBeatAsync(
        string beatText,
        string characterName,
        FacetWeights current,
        string beatGoal,
        CancellationToken ct = default)
    {
        var system = """
            You are a psychological story analyst tracking character facet evolution across scenes.
            Each character has six psychological facets that influence their behavior:

              WOUND  — Defining trauma; active when defensive, triggered, cornered, or lashing out
              IDEAL  — Highest aspiration; active when principled, hopeful, self-sacrificing
              ID     — Raw desire/instinct/appetite; active when unguarded, desperate, or hungry
              SHADOW — Dark mirror of the ideal; active when hypocritical, self-deceiving, compromising values
              MASK   — Social persona; active in social performance, deception, or professional roles
              GHOST  — Haunting past; active when memory intrudes or history seems to repeat

            Given a scene, assess which facets the character expressed most strongly vs. suppressed.
            Suggest tiny weight adjustments reflecting how this specific scene shaped them.

            Rules:
            - Small scenes: ±0.01 to ±0.02 per facet
            - Significant character moments: ±0.03 to ±0.05 per facet
            - Never exceed ±0.05 on any single facet in one scene
            - Most scenes: most deltas should be 0.0 (not every facet is relevant in every scene)

            Return ONLY this JSON object (no commentary, no markdown fence):
            {
              "wound":  <float -0.05 to 0.05>,
              "ideal":  <float -0.05 to 0.05>,
              "id":     <float -0.05 to 0.05>,
              "shadow": <float -0.05 to 0.05>,
              "mask":   <float -0.05 to 0.05>,
              "ghost":  <float -0.05 to 0.05>,
              "rationale": "<one sentence: which facets dominated and why>"
            }
            """;

        var user = $"""
            CHARACTER: {characterName}
            SCENE GOAL: {beatGoal}

            CURRENT FACET WEIGHTS (for reference):
              Wound:  {current.Wound:F2}
              Ideal:  {current.Ideal:F2}
              Id:     {current.Id:F2}
              Shadow: {current.Shadow:F2}
              Mask:   {current.Mask:F2}
              Ghost:  {current.Ghost:F2}

            SCENE TEXT:
            {(beatText.Length > 2500 ? beatText[^2500..] : beatText)}

            Analyze which psychological facets {characterName} expressed in this scene.
            """;

        try
        {
            var response = await llm.GenerateAsync(system, user, 0.2, 512, null, ct);
            var json = ExtractJson(response);
            var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;

            double Clamp(string key) =>
                Math.Clamp(root.TryGetProperty(key, out var v) ? v.GetDouble() : 0.0, -MaxShiftPerBeat, MaxShiftPerBeat);

            return new FacetShiftResult
            {
                CharacterName = characterName,
                WoundDelta  = Clamp("wound"),
                IdealDelta  = Clamp("ideal"),
                IdDelta     = Clamp("id"),
                ShadowDelta = Clamp("shadow"),
                MaskDelta   = Clamp("mask"),
                GhostDelta  = Clamp("ghost"),
                Rationale   = root.TryGetProperty("rationale", out var r) ? r.GetString() ?? "" : "",
            };
        }
        catch (Exception ex)
        {
            log.LogDebug(ex, "Facet analysis failed for {Character} — no shifts applied", characterName);
            return new FacetShiftResult { CharacterName = characterName };
        }
    }

    private static void ApplyShifts(CharacterData character, FacetShiftResult shifts)
    {
        var w = character.Psychology.FacetWeights;
        w.Wound  = Math.Clamp(w.Wound  + shifts.WoundDelta,  0.0, 1.0);
        w.Ideal  = Math.Clamp(w.Ideal  + shifts.IdealDelta,  0.0, 1.0);
        w.Id     = Math.Clamp(w.Id     + shifts.IdDelta,     0.0, 1.0);
        w.Shadow = Math.Clamp(w.Shadow + shifts.ShadowDelta, 0.0, 1.0);
        w.Mask   = Math.Clamp(w.Mask   + shifts.MaskDelta,   0.0, 1.0);
        w.Ghost  = Math.Clamp(w.Ghost  + shifts.GhostDelta,  0.0, 1.0);
    }

    private static string ExtractJson(string text)
    {
        var start = text.IndexOf('{');
        var end   = text.LastIndexOf('}');
        return start >= 0 && end > start ? text[start..(end + 1)] : "{}";
    }
}

/// <summary>
/// Proposed facet weight deltas from a single beat's psychological analysis.
/// All deltas are pre-clamped to [-0.05, 0.05].
/// </summary>
public class FacetShiftResult
{
    public string CharacterName { get; init; } = "";
    public double WoundDelta    { get; init; }
    public double IdealDelta    { get; init; }
    public double IdDelta       { get; init; }
    public double ShadowDelta   { get; init; }
    public double MaskDelta     { get; init; }
    public double GhostDelta    { get; init; }
    public string Rationale     { get; init; } = "";

    /// <summary>True when at least one facet shifted by more than noise threshold.</summary>
    public bool HasSignificantShift =>
        Math.Abs(WoundDelta)  > 0.005 ||
        Math.Abs(IdealDelta)  > 0.005 ||
        Math.Abs(IdDelta)     > 0.005 ||
        Math.Abs(ShadowDelta) > 0.005 ||
        Math.Abs(MaskDelta)   > 0.005 ||
        Math.Abs(GhostDelta)  > 0.005;
}
