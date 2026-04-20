namespace StreetSamurai.Core.Models;

/// <summary>
/// A request to write a combat scene — distinct from a prose scene request.
/// Carries battlefield geometry, sides, loadouts and a combat tone so the
/// writer can produce action prose rather than introspective narration.
/// </summary>
public record CombatSceneRequest
{
    /// <summary>District or place name where the fight unfolds. Used to pull terrain, cover, atmosphere.</summary>
    public string BattlefieldLocation { get; init; } = "";

    /// <summary>Free-form environmental details that shape action — "raining", "flickering neon", "knee-deep water".</summary>
    public string Environment { get; init; } = "";

    /// <summary>The sides fighting. Normally two, but can be three-way (corp security, runners, third party).</summary>
    public List<CombatSide> Sides { get; init; } = [];

    /// <summary>Overall objective the scene is building toward — "extract the courier", "kill the target", "buy time".</summary>
    public string Objective { get; init; } = "";

    /// <summary>Number of exchanges (attack/react cycles) to generate. Each exchange is one written beat.</summary>
    public int NumExchanges { get; init; } = 4;

    /// <summary>Register of the combat prose — shapes word choice, pacing, level of gore.</summary>
    public CombatTone Tone { get; init; } = CombatTone.Brutal;

    /// <summary>Prose that led up to the fight — the writer uses it to transition smoothly from narration into action.</summary>
    public string PrecedingContext { get; init; } = "";

    /// <summary>Optional: specific opening move or inciting action ("the door blows in", "Kyle draws first").</summary>
    public string OpeningBeat { get; init; } = "";
}

/// <summary>
/// One combat faction — usually two or three of these per fight.
/// Combatants reference CharacterData by name; loadout overrides fill in for extras/mooks
/// that may not be canonical characters.
/// </summary>
public record CombatSide
{
    /// <summary>Short label for this side — "runners", "security", "the things in the walls".</summary>
    public string Label { get; init; } = "";

    /// <summary>Names of canonical characters on this side. Cross-referenced against the character repo.</summary>
    public List<string> Combatants { get; init; } = [];

    /// <summary>Starting physical position/posture — "behind the overturned table", "pinned against the east wall".</summary>
    public string InitialPosition { get; init; } = "";

    /// <summary>What this side is trying to achieve — not the overall objective, their view of it.</summary>
    public string Goal { get; init; } = "";

    /// <summary>Anonymous extras that don't exist as canon characters — "three security drones", "two chromed goons".</summary>
    public List<string> UnnamedCombatants { get; init; } = [];

    /// <summary>Fallback loadout for the side — weapons/gear all members share unless overridden by canon character data.</summary>
    public string SharedLoadout { get; init; } = "";
}

/// <summary>
/// The tonal register of the action prose. Each tone shapes sentence length,
/// vocabulary, and how suffering is rendered.
/// </summary>
public enum CombatTone
{
    /// <summary>Short punches, physical detail, no flourish. Violence is work.</summary>
    Brutal,
    /// <summary>Wider shots, choreographed geometry, impossible reflexes — Hong Kong gunplay.</summary>
    Cinematic,
    /// <summary>Fragmented, panicked, visceral — the losing side's POV.</summary>
    Desperate,
    /// <summary>Procedural, detached — mercenary or corporate operative register.</summary>
    Clinical,
    /// <summary>Broken perception — smoke, concussions, cybernetic glitching.</summary>
    Chaotic,
}

/// <summary>
/// Result of writing a combat scene — a sequence of action beats.
/// </summary>
public record GeneratedCombatScene
{
    public string Id { get; init; } = Guid.CreateVersion7().ToString("N")[..8];
    public CombatSceneRequest Request { get; init; } = new();
    public List<CombatBeat> Beats { get; init; } = [];
    public DateTime Generated { get; init; } = DateTime.UtcNow;
    public string FullText => string.Join("\n\n", Beats.Select(b => b.Text));
}

/// <summary>
/// One exchange in a combat scene — an attack, a reaction, or a consequence.
/// </summary>
public record CombatBeat
{
    public int Index { get; init; }
    /// <summary>Short engineered label — "opening shot", "flanking move", "finisher". Used by the prompt, not rendered.</summary>
    public string ActionLabel { get; init; } = "";
    /// <summary>The written action prose for this beat.</summary>
    public string Text { get; init; } = "";
    /// <summary>Which side held the initiative for this beat.</summary>
    public string ActingSide { get; init; } = "";
    /// <summary>Accumulated wound/damage state after this beat — informs the next beat's constraints.</summary>
    public string DamageState { get; init; } = "";
}

/// <summary>Streaming progress event raised as combat beats are generated.</summary>
public record CombatBeatProgress
{
    public int BeatIndex { get; init; }
    public int TotalBeats { get; init; }
    public string ActingSide { get; init; } = "";
    public string Status { get; init; } = "";
}
