namespace Prose.Core.Data.Entities;

// ─────────────────────────────────────────────────────────────────────────────
// Automaton — fully relational. Lists (Aliases / Armament / Sensors /
// KnownDeployments / StoryHooks) become bridges; KnownDeployments resolves to
// Place / Faction / Corponation FKs since deployments name where the machine
// has been used.
// ─────────────────────────────────────────────────────────────────────────────

public class Automaton
{
    public Guid Id { get; set; }
    /// <summary>Canonical name. Mirrors Entity.Name so the Automata table is queryable standalone.</summary>
    public string Name { get; set; } = "";

    // Indexed classification.
    public string KindOfBeing  { get; set; } = "automaton";
    public string Manufacturer { get; set; } = "";
    public string Tier         { get; set; } = "";
    public string Operator     { get; set; } = "";

    // Top-level scalars.
    public double Rating { get; set; }
    public int    VoteCount { get; set; }
    public string Classification { get; set; } = "";
    public string Description { get; set; } = "";
    public string Legality { get; set; } = "";
    public string AutonomyLevel { get; set; } = "";
    public string Dimensions { get; set; } = "";
    public string Weight { get; set; } = "";
    public string PowerSource { get; set; } = "";
    public string Locomotion { get; set; } = "";
    public string Countermeasures { get; set; } = "";
    public string CulturalContext { get; set; } = "";
    public string MidjourneyPrompt { get; set; } = "";
    public string Dalle3Prompt { get; set; } = "";

    public Entity? Entity { get; set; }
    public ICollection<AutomatonAlias>      Aliases          { get; set; } = new List<AutomatonAlias>();
    public ICollection<AutomatonArmament>   Armament         { get; set; } = new List<AutomatonArmament>();
    public ICollection<AutomatonSensor>     Sensors          { get; set; } = new List<AutomatonSensor>();
    public ICollection<AutomatonDeployment> KnownDeployments { get; set; } = new List<AutomatonDeployment>();
    public ICollection<AutomatonStoryHook>  StoryHooks       { get; set; } = new List<AutomatonStoryHook>();
}

public class AutomatonAlias
{
    public long Id { get; set; }
    public Guid AutomatonId { get; set; }
    public int Position { get; set; }
    public string Value { get; set; } = "";
    public Automaton? Automaton { get; set; }
}

public class AutomatonArmament
{
    public long Id { get; set; }
    public Guid AutomatonId { get; set; }
    public int Position { get; set; }
    /// <summary>Optional FK to a Weapon entity when the armament is a canon weapon.</summary>
    public Guid? WeaponId { get; set; }
    public string Alias { get; set; } = "";
    public Automaton? Automaton { get; set; }
    public Entity? Weapon { get; set; }
}

public class AutomatonSensor
{
    public long Id { get; set; }
    public Guid AutomatonId { get; set; }
    public int Position { get; set; }
    public string SensorName { get; set; } = "";
    public Automaton? Automaton { get; set; }
}

public class AutomatonDeployment
{
    public long Id { get; set; }
    public Guid AutomatonId { get; set; }
    public int Position { get; set; }
    /// <summary>Place / Faction / Corponation entity where this automaton was deployed.</summary>
    public Guid? DeploymentEntityId { get; set; }
    public string Alias { get; set; } = "";
    public Automaton? Automaton { get; set; }
    public Entity? DeploymentEntity { get; set; }
}

public class AutomatonStoryHook
{
    public long Id { get; set; }
    public Guid AutomatonId { get; set; }
    public int Position { get; set; }
    public string Hook { get; set; } = "";
    public Automaton? Automaton { get; set; }
}
