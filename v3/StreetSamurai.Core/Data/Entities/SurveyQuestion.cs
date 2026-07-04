namespace StreetSamurai.Core.Data.Entities;

/// <summary>
/// One question within a <see cref="Survey"/>. Stores the rendered options as
/// JSON, the user's selected answer, and the apply outcome.
///
/// <para><b>QuestionType</b> hints to the apply step what kind of change is
/// required — Claude reads it and calls the appropriate SQL / MCP tool:</para>
/// <list type="bullet">
///   <item>PlaceDescription — UPDATE Places SET Description</item>
///   <item>TechnologyDescription — UPDATE Technologies SET Description (+ optional rename)</item>
///   <item>WeaponRename — UPDATE Weapons + Entities (name/slug + description)</item>
///   <item>FactionDescription — UPDATE Factions SET Description</item>
///   <item>CharacterDescription — UPDATE Characters SET Description</item>
///   <item>BeatText — mcp update_beat_text</item>
///   <item>DocUpdate — edit docs/*.md</item>
///   <item>ContradictionResolve — mcp resolve_continuity_contradiction</item>
///   <item>Custom — apply is manual / ad-hoc</item>
/// </list>
///
/// <para><b>OptionsJson</b> — JSON array of <c>{key, label, description}</c>.
/// Example: <c>[{"key":"a","label":"Auto-fix","description":"Replace Z8 with outer industrial sector"}]</c></para>
/// </summary>
public class SurveyQuestion
{
    public Guid   Id           { get; set; }
    public Guid   SurveyId     { get; set; }

    /// <summary>Short identifier shown in the artifact: "Q-001", "Q-002", etc.</summary>
    public string QuestionKey  { get; set; } = "";

    /// <summary>See XML doc above for valid values.</summary>
    public string QuestionType { get; set; } = "Custom";

    public string  Title       { get; set; } = "";

    /// <summary>Markdown context shown above the options in the artifact.</summary>
    public string? Context     { get; set; }

    /// <summary>JSON array of <c>{key,label,description}</c> option objects.</summary>
    public string  OptionsJson { get; set; } = "[]";

    /// <summary>The letter key the user selected: "a", "b", "c", or "d".</summary>
    public string? SelectedOption { get; set; }

    public DateTime? AnsweredAt { get; set; }

    /// <summary>Pending | Applied | Skipped</summary>
    public string ApplyStatus  { get; set; } = "Pending";

    public string?  ApplyNotes { get; set; }
    public DateTime? AppliedAt { get; set; }

    public int SortOrder { get; set; }

    public Survey Survey { get; set; } = null!;
}
