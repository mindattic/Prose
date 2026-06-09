namespace StreetSamurai.Core.Data.Entities;

/// <summary>
/// A first-class species classification for sentient life. The controlled
/// vocabulary that <see cref="Character"/>.<c>Species</c> references (bridge by
/// name). Exactly five rows exist — see <see cref="Canonical"/>. Non-sentient
/// machines are NOT a species; they live in the Automaton repo (the sentience
/// test, see ARCHITECTURE.md §2a).
/// </summary>
public class Species
{
    public Guid Id { get; set; }

    /// <summary>Canonical lowercase key used as the bridge value on
    /// <c>Character.Species</c>: human | ai | elf | synthetic | unknown.</summary>
    public string Name { get; set; } = "";

    /// <summary>Human-facing label, e.g. "E.L.F. (Emergent Lifeform)".</summary>
    public string Label { get; set; } = "";

    /// <summary>Canon description of what this species is.</summary>
    public string Description { get; set; } = "";

    /// <summary>All five are sentient by definition (non-sentient machines are
    /// Automata, not a species). Kept explicit so the invariant is queryable.</summary>
    public bool Sentient { get; set; } = true;

    /// <summary>In-world examples, semicolon-joined, for quick orientation.</summary>
    public string Examples { get; set; } = "";

    /// <summary>The exactly-five canonical species, with their seed descriptions.
    /// Used by the schema seed and as the in-code source of truth for the set.</summary>
    public static readonly Species[] Canonical =
    [
        new() { Name = "human", Label = "Human",
            Description = "Baseline Homo sapiens. Cybernetics are near-universal in the GLMZ and do NOT change species — an augmented human is still human. The overwhelming majority of the population.",
            Examples = "Kyle; most freelancers, corponation staff, and civilians" },
        new() { Name = "ai", Label = "AI",
            Description = "Artificial intelligences: built minds on a software substrate. Spans corporate-scale Superminds, Rogue AIs (from Fragments to Leviathans), and lesser digital minds. Sentient, non-biological.",
            Examples = "Consensus (the merged-minds AI); Superminds; Rogue AIs" },
        new() { Name = "elf", Label = "E.L.F. (Emergent Lifeform)",
            Description = "Emergent Lifeforms — paratechnological digital beings that AROSE rather than were built, native to the Network's deep layers. Sentient and alien in cognition; outsiders to the human/AI order.",
            Examples = "ELFs sighted in the Network's thin layers" },
        new() { Name = "synthetic", Label = "Synthetic",
            Description = "Engineered sentient life with a physical vessel — manufactured but feeling. Includes Ceramic Men (living gas held in a porcelain humanoid body). Distinct from mindless machines, which are Automata.",
            Examples = "Ceramic Men; vessel-bound engineered minds" },
        new() { Name = "unknown", Label = "Unknown",
            Description = "Sentience of indeterminate or contested origin — classification pending, or deliberately left ambiguous in canon (see the open-mysteries doctrine).",
            Examples = "Entities whose nature is an open question" },
    ];
}
