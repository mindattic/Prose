namespace StreetSamurai.Core.Data.Entities;

// ─────────────────────────────────────────────────────────────────────────────
// The remaining entity types — each fully relational, no DataJson columns.
// Order: Archetype, Quote, News, Contract, Document, Vocabulary, LabSpecimen,
//        Psionic, Technology, Facet, Motif, Entertainment, FlyoverEntity (Wasteland).
// Ceramic Men were folded into SyntheticLives (Type == "ceramic_man") — the
// dedicated CeramicMan tables and entity classes were retired 2026-05-06.
// ─────────────────────────────────────────────────────────────────────────────

// ── Archetype ──────────────────────────────────────────────────────────────

public class ArchetypeRow
{
    public Guid Id { get; set; }
    /// <summary>Canonical name. Mirrors Entity.Name.</summary>
    public string Name { get; set; } = "";
    public string Family { get; set; } = "";
    public string Category { get; set; } = "";
    public string Description { get; set; } = "";
    public string BehavioralSignature { get; set; } = "";
    public string UnderStress { get; set; } = "";
    public string AtRest { get; set; } = "";
    public Entity? Entity { get; set; }
    public ICollection<ArchetypeWillAlways> WillAlways  { get; set; } = new List<ArchetypeWillAlways>();
    public ICollection<ArchetypeWillNever>  WillNever   { get; set; } = new List<ArchetypeWillNever>();
    public ICollection<ArchetypeUnless>     Unless      { get; set; } = new List<ArchetypeUnless>();
    public ICollection<ArchetypeSimilar>    SimilarTo   { get; set; } = new List<ArchetypeSimilar>();
    public ICollection<ArchetypeOpposite>   OppositeOf  { get; set; } = new List<ArchetypeOpposite>();
}
public class ArchetypeWillAlways { public long Id { get; set; } public Guid ArchetypeId { get; set; } public int Position { get; set; } public string Rule { get; set; } = ""; public ArchetypeRow? Archetype { get; set; } }
public class ArchetypeWillNever  { public long Id { get; set; } public Guid ArchetypeId { get; set; } public int Position { get; set; } public string Rule { get; set; } = ""; public ArchetypeRow? Archetype { get; set; } }
public class ArchetypeUnless     { public long Id { get; set; } public Guid ArchetypeId { get; set; } public int Position { get; set; } public string Condition { get; set; } = ""; public ArchetypeRow? Archetype { get; set; } }
public class ArchetypeSimilar
{
    public long Id { get; set; }
    public Guid ArchetypeId { get; set; }
    public int Position { get; set; }
    public Guid? SimilarArchetypeId { get; set; }
    public string Alias { get; set; } = "";
    public double Threshold { get; set; }
    public string Context { get; set; } = "";
    public ArchetypeRow? Archetype { get; set; }
    public Entity? Similar { get; set; }
}
public class ArchetypeOpposite
{
    public long Id { get; set; }
    public Guid ArchetypeId { get; set; }
    public int Position { get; set; }
    public Guid? OppositeArchetypeId { get; set; }
    public string Alias { get; set; } = "";
    public ArchetypeRow? Archetype { get; set; }
    public Entity? Opposite { get; set; }
}

// ── Quote ──────────────────────────────────────────────────────────────────

public class Quote
{
    public Guid Id { get; set; }
    /// <summary>The quote text — used as Entity.Name for display in lists.</summary>
    public string Name { get; set; } = "";
    public string Attribution { get; set; } = "";
    public string Theme { get; set; } = "";
    public string QuoteText { get; set; } = "";
    public string Source { get; set; } = "";
    public string Context { get; set; } = "";
    public string Category { get; set; } = "";
    public bool   InWorld { get; set; }
    public Entity? Entity { get; set; }
}

// ── News ───────────────────────────────────────────────────────────────────

public class News
{
    public Guid Id { get; set; }
    /// <summary>Headline. Mirrors Entity.Name.</summary>
    public string Name { get; set; } = "";
    public string Outlet { get; set; } = "";
    public DateTime? PublishedDate { get; set; }
    public string DateText { get; set; } = "";
    public string Category { get; set; } = "";
    public string Source { get; set; } = "";
    public string Reporter { get; set; } = "";
    public string Body { get; set; } = "";
    public string Aftermath { get; set; } = "";
    public string Casualties { get; set; } = "";
    public string RunnerRelevance { get; set; } = "";
    public double Rating { get; set; }
    public int    VoteCount { get; set; }
    public string MidjourneyPrompt { get; set; } = "";
    public string Dalle3Prompt { get; set; } = "";
    public Entity? Entity { get; set; }
    public ICollection<NewsEntityInvolved> EntitiesInvolved { get; set; } = new List<NewsEntityInvolved>();
    public ICollection<NewsLocation>       Locations        { get; set; } = new List<NewsLocation>();
}
public class NewsEntityInvolved
{
    public long Id { get; set; }
    public Guid NewsId { get; set; }
    public int Position { get; set; }
    public Guid? InvolvedEntityId { get; set; }
    public string Alias { get; set; } = "";
    public News? News { get; set; }
    public Entity? InvolvedEntity { get; set; }
}
public class NewsLocation
{
    public long Id { get; set; }
    public Guid NewsId { get; set; }
    public int Position { get; set; }
    public Guid? PlaceId { get; set; }
    public string Alias { get; set; } = "";
    public News? News { get; set; }
    public Entity? Place { get; set; }
}

// ── Contract ───────────────────────────────────────────────────────────────

public class Contract
{
    public Guid Id { get; set; }
    /// <summary>Codename. Mirrors Entity.Name.</summary>
    public string Name { get; set; } = "";
    public string Codename { get; set; } = "";
    public string ContractStatus { get; set; } = "open";
    public string Tier { get; set; } = "";
    public string Client { get; set; } = "";
    public Guid?  ClientEntityId { get; set; }
    public string ClientTier { get; set; } = "";
    public string Category { get; set; } = "";
    public string Description { get; set; } = "";
    public string Objective { get; set; } = "";
    public string Location { get; set; } = "";
    public Guid?  LocationPlaceId { get; set; }
    public string Target { get; set; } = "";
    public string Opposition { get; set; } = "";
    public string Payout { get; set; } = "";
    public string CrewSize { get; set; } = "";
    public string Difficulty { get; set; } = "";
    public string TimeLimit { get; set; } = "";
    public string Outcome { get; set; } = "";
    // Required-capability scalars (CrewCapabilities flattened).
    public int CapabilityCombat { get; set; }
    public int CapabilityStealth { get; set; }
    public int CapabilityHacking { get; set; }
    public int CapabilitySocial { get; set; }
    public int CapabilityMedical { get; set; }
    public int CapabilityTech { get; set; }
    public int CapabilityTransport { get; set; }
    public int CapabilityDemolitions { get; set; }
    public int CapabilitySurveillance { get; set; }
    public int CapabilityLinguistics { get; set; }
    public double Rating { get; set; }
    public int    VoteCount { get; set; }
    public string MidjourneyPrompt { get; set; } = "";
    public string Dalle3Prompt { get; set; } = "";
    public Entity? Entity { get; set; }
    public Entity? ClientEntity { get; set; }
    public Entity? LocationPlace { get; set; }
    public ICollection<ContractBonusRow>     Bonuses        { get; set; } = new List<ContractBonusRow>();
    public ICollection<ContractComplication> Complications  { get; set; } = new List<ContractComplication>();
}
public class ContractBonusRow
{
    public long Id { get; set; }
    public Guid ContractId { get; set; }
    public int Position { get; set; }
    public string BonusType { get; set; } = "";
    public string Amount { get; set; } = "";
    public string Condition { get; set; } = "";
    public Contract? Contract { get; set; }
}
public class ContractComplication
{
    public long Id { get; set; }
    public Guid ContractId { get; set; }
    public int Position { get; set; }
    public string Description { get; set; } = "";
    public Contract? Contract { get; set; }
}

// ── Document (worldbuilding) ───────────────────────────────────────────────

public class Document
{
    public Guid Id { get; set; }
    /// <summary>Title. Mirrors Entity.Name.</summary>
    public string Name { get; set; } = "";
    public string Title { get; set; } = "";
    public string FileName { get; set; } = "";
    public string Category { get; set; } = "";
    public string Body { get; set; } = "";
    public int    LineCount { get; set; }
    public double Rating { get; set; }
    public int    VoteCount { get; set; }
    public string MidjourneyPrompt { get; set; } = "";
    public string Dalle3Prompt { get; set; } = "";
    public Entity? Entity { get; set; }
    public ICollection<DocumentHeading> Headings { get; set; } = new List<DocumentHeading>();
}
public class DocumentHeading
{
    public long Id { get; set; }
    public Guid DocumentId { get; set; }
    public int Position { get; set; }
    public string HeadingText { get; set; } = "";
    public Document? Document { get; set; }
}

// ── MarkdownFile (config + memory backup) ─────────────────────────────────
// Tracks every .md file that the LLM toolchain depends on — project rules,
// Codex docs, and Claude Code memory files. System-versioned so any version
// of any file can be recovered by timestamp. FilePath stores the absolute
// path at sync time; RelativePath is the stable logical key (relative to
// FileRoot) used to reconstruct the path on any machine.
//
// FileRoot values:  "project"               → IPathProvider.DataRoot
//                   "claude-user"           → ~/.claude
//                   "claude-project-memory" → ~/.claude/projects/{slug}/memory
//
// Category values:  "project-rule" | "project-rule-global" | "codex"
//                   | "register" | "rfc" | "memory" | "memory-index"

public class MarkdownFile
{
    public Guid     Id            { get; set; }
    public string   FilePath      { get; set; } = "";  // absolute path at sync time
    public string   FileRoot      { get; set; } = "";  // logical root label
    public string   RelativePath  { get; set; } = "";  // unique key: path relative to FileRoot
    public string   FileName      { get; set; } = "";  // basename only
    public string   Category      { get; set; } = "";
    public string   Content       { get; set; } = "";
    public string   ContentHash   { get; set; } = "";  // SHA-256 hex of Content
    public DateTime LastSyncedAt  { get; set; }
    public string   SyncedBy      { get; set; } = "";  // "cli" | "mcp"

    // ── Doc Context Stack (dynamic .md working-set engine) ────────────────────
    // Classify each file for the rotating-context engine (DocContextService).
    //   Tier:     "always" (universal, every context) | "node" (one story) | "topic" (triggered)
    //   Scope:    CSV of node CODEs the file applies to (node tier), or "*". Empty = none.
    //   Triggers: CSV of keywords/aliases that load a topic file when they appear in scene text.
    //   AutoTier: true = tier/scope/triggers were auto-inferred; false = set from frontmatter.
    public string   Tier          { get; set; } = "topic";
    public string   Scope         { get; set; } = "";
    public string   Triggers      { get; set; } = "";
    public bool     AutoTier      { get; set; } = true;
}

// ── Vocabulary ─────────────────────────────────────────────────────────────

public class Vocabulary
{
    public Guid Id { get; set; }
    /// <summary>Term. Mirrors Entity.Name.</summary>
    public string Name { get; set; } = "";
    public string Term { get; set; } = "";
    public string Domain { get; set; } = "";
    public string Definition { get; set; } = "";
    public string Origin { get; set; } = "";
    public string Usage { get; set; } = "";
    public string Tier { get; set; } = "";
    public string Category { get; set; } = "";
    public string Example { get; set; } = "";
    public Entity? Entity { get; set; }
}

// ── LabSpecimen ────────────────────────────────────────────────────────────

public class LabSpecimen
{
    public Guid Id { get; set; }
    /// <summary>Canonical name. Mirrors Entity.Name.</summary>
    public string Name { get; set; } = "";
    public string Classification { get; set; } = "";
    public string Origin { get; set; } = "";
    public string OriginLab { get; set; } = "";
    public string OriginMethod { get; set; } = "";
    public string Substrate { get; set; } = "";
    public string PhysicalDescription { get; set; } = "";
    public string BehavioralProfile { get; set; } = "";
    public string ThreatLevel { get; set; } = "";
    public string ContainmentStatus { get; set; } = "";
    public string ContaminationRisk { get; set; } = "";
    public string PacificationProtocol { get; set; } = "";
    public string PitiableQualities { get; set; } = "";
    public double Rating { get; set; }
    public int    VoteCount { get; set; }
    public string MidjourneyPrompt { get; set; } = "";
    public string Dalle3Prompt { get; set; } = "";
    public Entity? Entity { get; set; }
    public ICollection<LabSpecimenAlias>          Aliases        { get; set; } = new List<LabSpecimenAlias>();
    public ICollection<LabSpecimenKnownLocation>  KnownLocations { get; set; } = new List<LabSpecimenKnownLocation>();
    public ICollection<LabSpecimenStoryHook>      StoryHooks     { get; set; } = new List<LabSpecimenStoryHook>();
}
public class LabSpecimenAlias { public long Id { get; set; } public Guid LabSpecimenId { get; set; } public int Position { get; set; } public string Value { get; set; } = ""; public LabSpecimen? LabSpecimen { get; set; } }
public class LabSpecimenKnownLocation
{
    public long Id { get; set; }
    public Guid LabSpecimenId { get; set; }
    public int Position { get; set; }
    public Guid? PlaceId { get; set; }
    public string Alias { get; set; } = "";
    public LabSpecimen? LabSpecimen { get; set; }
    public Entity? Place { get; set; }
}
public class LabSpecimenStoryHook { public long Id { get; set; } public Guid LabSpecimenId { get; set; } public int Position { get; set; } public string Hook { get; set; } = ""; public LabSpecimen? LabSpecimen { get; set; } }

// ── Psionic ────────────────────────────────────────────────────────────────

public class Psionic
{
    public Guid Id { get; set; }
    /// <summary>Canonical name. Mirrors Entity.Name.</summary>
    public string Name { get; set; } = "";
    public string Discipline { get; set; } = "";
    public string Tier { get; set; } = "";
    public string Classification { get; set; } = "";
    public string EnhancementType { get; set; } = "";
    public string Mechanism { get; set; } = "";
    public string Abilities { get; set; } = "";
    public string SideEffects { get; set; } = "";
    public string AcquisitionMethod { get; set; } = "";
    public string DetectionRisk { get; set; } = "";
    public string CorporateInterest { get; set; } = "";
    public double Rating { get; set; }
    public int    VoteCount { get; set; }
    public string MidjourneyPrompt { get; set; } = "";
    public string Dalle3Prompt { get; set; } = "";
    public Entity? Entity { get; set; }
    public ICollection<PsionicAlias>             Aliases            { get; set; } = new List<PsionicAlias>();
    public ICollection<PsionicKnownPractitioner> KnownPractitioners { get; set; } = new List<PsionicKnownPractitioner>();
    public ICollection<PsionicStoryHook>         StoryHooks         { get; set; } = new List<PsionicStoryHook>();
}
public class PsionicAlias { public long Id { get; set; } public Guid PsionicId { get; set; } public int Position { get; set; } public string Value { get; set; } = ""; public Psionic? Psionic { get; set; } }
public class PsionicKnownPractitioner
{
    public long Id { get; set; }
    public Guid PsionicId { get; set; }
    public int Position { get; set; }
    public Guid? CharacterId { get; set; }
    public string Alias { get; set; } = "";
    public Psionic? Psionic { get; set; }
    public Entity? Character { get; set; }
}
public class PsionicStoryHook { public long Id { get; set; } public Guid PsionicId { get; set; } public int Position { get; set; } public string Hook { get; set; } = ""; public Psionic? Psionic { get; set; } }

// ── Technology ─────────────────────────────────────────────────────────────

public class Technology
{
    public Guid Id { get; set; }
    /// <summary>Canonical name. Mirrors Entity.Name.</summary>
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public string Tier { get; set; } = "";
    public string Subcategory { get; set; } = "";
    public string BrandName { get; set; } = "";
    public string ProductName { get; set; } = "";
    public string Description { get; set; } = "";
    public string SocialImpact { get; set; } = "";
    public double Rating { get; set; }
    public int    VoteCount { get; set; }
    public string MidjourneyPrompt { get; set; } = "";
    public string Dalle3Prompt { get; set; } = "";
    public Entity? Entity { get; set; }
    public ICollection<TechnologyAlias>          Aliases          { get; set; } = new List<TechnologyAlias>();
    public ICollection<TechnologyDeveloper>      Developers       { get; set; } = new List<TechnologyDeveloper>();
    public ICollection<TechnologyBaseTechnology> BaseTechnologies { get; set; } = new List<TechnologyBaseTechnology>();
    public ICollection<TechnologyEnables>        Enables          { get; set; } = new List<TechnologyEnables>();
    public ICollection<TechnologyStoryHook>      StoryHooks       { get; set; } = new List<TechnologyStoryHook>();
}
public class TechnologyAlias { public long Id { get; set; } public Guid TechnologyId { get; set; } public int Position { get; set; } public string Value { get; set; } = ""; public Technology? Technology { get; set; } }
public class TechnologyDeveloper
{
    public long Id { get; set; }
    public Guid TechnologyId { get; set; }
    public int Position { get; set; }
    public Guid? DeveloperEntityId { get; set; }
    public string Alias { get; set; } = "";
    public Technology? Technology { get; set; }
    public Entity? Developer { get; set; }
}
public class TechnologyBaseTechnology
{
    public long Id { get; set; }
    public Guid TechnologyId { get; set; }
    public int Position { get; set; }
    public Guid? BaseTechnologyId { get; set; }
    public string Alias { get; set; } = "";
    public Technology? Technology { get; set; }
    public Entity? BaseTechnology { get; set; }
}
public class TechnologyEnables
{
    public long Id { get; set; }
    public Guid TechnologyId { get; set; }
    public int Position { get; set; }
    public Guid? EnabledEntityId { get; set; }
    public string Alias { get; set; } = "";
    public Technology? Technology { get; set; }
    public Entity? Enabled { get; set; }
}
public class TechnologyStoryHook { public long Id { get; set; } public Guid TechnologyId { get; set; } public int Position { get; set; } public string Hook { get; set; } = ""; public Technology? Technology { get; set; } }


// ── Motif ──────────────────────────────────────────────────────────────────

public class Motif
{
    public Guid Id { get; set; }
    /// <summary>Canonical name. Mirrors Entity.Name.</summary>
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public Entity? Entity { get; set; }
    public ICollection<MotifAppearance> Appearances { get; set; } = new List<MotifAppearance>();
}
public class MotifAppearance
{
    public long Id { get; set; }
    public Guid MotifId { get; set; }
    public int Position { get; set; }
    public int Scene { get; set; }
    public string Meaning { get; set; } = "";
    public Motif? Motif { get; set; }
}

// ── Entertainment ──────────────────────────────────────────────────────────

public class Entertainment
{
    public Guid Id { get; set; }
    /// <summary>Canonical name. Mirrors Entity.Name.</summary>
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public string Tier { get; set; } = "";
    public string Subcategory { get; set; } = "";
    public string Description { get; set; } = "";
    public string Creator { get; set; } = "";
    public string Distributor { get; set; } = "";
    public string Legality { get; set; } = "";
    public string Genre { get; set; } = "";
    public string Medium { get; set; } = "";
    public string Audience { get; set; } = "";
    public string CulturalImpact { get; set; } = "";
    public double Rating { get; set; }
    public int    VoteCount { get; set; }
    public string MidjourneyPrompt { get; set; } = "";
    public string Dalle3Prompt { get; set; } = "";
    public Entity? Entity { get; set; }
    public ICollection<EntertainmentAlias>     Aliases    { get; set; } = new List<EntertainmentAlias>();
    public ICollection<EntertainmentKnownFan>  KnownFans  { get; set; } = new List<EntertainmentKnownFan>();
    public ICollection<EntertainmentStoryHook> StoryHooks { get; set; } = new List<EntertainmentStoryHook>();
}
public class EntertainmentAlias { public long Id { get; set; } public Guid EntertainmentId { get; set; } public int Position { get; set; } public string Value { get; set; } = ""; public Entertainment? Entertainment { get; set; } }
public class EntertainmentKnownFan
{
    public long Id { get; set; }
    public Guid EntertainmentId { get; set; }
    public int Position { get; set; }
    public Guid? CharacterId { get; set; }
    public string Alias { get; set; } = "";
    public Entertainment? Entertainment { get; set; }
    public Entity? Character { get; set; }
}
public class EntertainmentStoryHook { public long Id { get; set; } public Guid EntertainmentId { get; set; } public int Position { get; set; } public string Hook { get; set; } = ""; public Entertainment? Entertainment { get; set; } }

// ── FlyoverEntity (Wasteland) ──────────────────────────────────────────────

public class FlyoverEntity
{
    public Guid Id { get; set; }
    /// <summary>Canonical name. Mirrors Entity.Name.</summary>
    public string Name { get; set; } = "";
    public string Classification { get; set; } = "";
    public string Origin { get; set; } = "";
    public string Substrate { get; set; } = "";
    public string Territory { get; set; } = "";
    public string PhysicalDescription { get; set; } = "";
    public string BehavioralProfile { get; set; } = "";
    public string ThreatLevel { get; set; } = "";
    public string HumanRemnants { get; set; } = "";
    public string GlmzMigrationRisk { get; set; } = "";
    public double Rating { get; set; }
    public int    VoteCount { get; set; }
    public string MidjourneyPrompt { get; set; } = "";
    public string Dalle3Prompt { get; set; } = "";
    public Entity? Entity { get; set; }
    public ICollection<FlyoverEntityAlias>          Aliases        { get; set; } = new List<FlyoverEntityAlias>();
    public ICollection<FlyoverEntityKnownLocation>  KnownLocations { get; set; } = new List<FlyoverEntityKnownLocation>();
    public ICollection<FlyoverEntityStoryHook>      StoryHooks     { get; set; } = new List<FlyoverEntityStoryHook>();
}
public class FlyoverEntityAlias { public long Id { get; set; } public Guid FlyoverEntityId { get; set; } public int Position { get; set; } public string Value { get; set; } = ""; public FlyoverEntity? FlyoverEntity { get; set; } }
public class FlyoverEntityKnownLocation
{
    public long Id { get; set; }
    public Guid FlyoverEntityId { get; set; }
    public int Position { get; set; }
    public Guid? PlaceId { get; set; }
    public string Alias { get; set; } = "";
    public FlyoverEntity? FlyoverEntity { get; set; }
    public Entity? Place { get; set; }
}
public class FlyoverEntityStoryHook { public long Id { get; set; } public Guid FlyoverEntityId { get; set; } public int Position { get; set; } public string Hook { get; set; } = ""; public FlyoverEntity? FlyoverEntity { get; set; } }

// ── SyntheticLife (ELFs / rogue AI / firmware-evolved entities) ───────────

public class SyntheticLife
{
    public Guid Id { get; set; }
    /// <summary>Canonical name. Mirrors Entity.Name.</summary>
    public string Name { get; set; } = "";

    public string KindOfBeing       { get; set; } = "";
    public string Manufacturer      { get; set; } = "";
    public string Tier              { get; set; } = "";
    public double Rating            { get; set; }
    public int    VoteCount         { get; set; }

    public string Classification    { get; set; } = "";
    public string Disposition       { get; set; } = "";
    public string Habitat           { get; set; } = "";
    public string Origin            { get; set; } = "";
    public string LifeStatus        { get; set; } = "";
    public string Description       { get; set; } = "";
    public string ObservedBehavior  { get; set; } = "";
    public string EncounterFrequency{ get; set; } = "";
    public int    ConfirmedSightings{ get; set; }
    public string Location          { get; set; } = "";
    public double DtiRating         { get; set; }
    public bool   Paratechnological { get; set; }

    public string? KnownAge            { get; set; }
    public string? CrackPattern        { get; set; }
    public string? CurrentRole         { get; set; }
    public string? KnownLocation       { get; set; }
    public string? DiplomaticSpecialty { get; set; }
    public string? OperatingHistory    { get; set; }
    public string? BehavioralNotes     { get; set; }
    public string? DamageHistory       { get; set; }
    public string? FaceDecoration      { get; set; }

    public string MidjourneyPrompt { get; set; } = "";
    public string Dalle3Prompt     { get; set; } = "";

    public Entity? Entity { get; set; }
    public ICollection<SyntheticLifeAlias>            Aliases            { get; set; } = new List<SyntheticLifeAlias>();
    public ICollection<SyntheticLifeKnownAssociation> KnownAssociations  { get; set; } = new List<SyntheticLifeKnownAssociation>();
    public ICollection<SyntheticLifeStoryHook>        StoryHooks         { get; set; } = new List<SyntheticLifeStoryHook>();
}

public class SyntheticLifeAlias
{
    public long Id             { get; set; }
    public Guid SyntheticLifeId { get; set; }
    public int  Position       { get; set; }
    public string Value        { get; set; } = "";
    public SyntheticLife? SyntheticLife { get; set; }
}

public class SyntheticLifeKnownAssociation
{
    public long Id              { get; set; }
    public Guid SyntheticLifeId { get; set; }
    public int  Position        { get; set; }
    /// <summary>Alias string preserved from blob; resolved to FK when entity found.</summary>
    public string Alias         { get; set; } = "";
    /// <summary>Nullable FK to an Entity (character, faction, etc.) when resolved.</summary>
    public Guid? AssociateEntityId { get; set; }
    public SyntheticLife? SyntheticLife { get; set; }
    public Entity? Associate    { get; set; }
}

public class SyntheticLifeStoryHook
{
    public long Id              { get; set; }
    public Guid SyntheticLifeId { get; set; }
    public int  Position        { get; set; }
    public string Hook          { get; set; } = "";
    public SyntheticLife? SyntheticLife { get; set; }
}

