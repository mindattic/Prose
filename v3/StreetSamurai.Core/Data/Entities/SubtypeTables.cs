namespace StreetSamurai.Core.Data.Entities;

/// <summary>
/// Lightweight per-type tables. Each row's <c>Id</c> is also FK to <c>Entity.Id</c>
/// (TPT-style). Strongly-typed columns are limited to the fields the codebase
/// actually filters on (Manufacturer, Tier, Sector, Legality, Category, etc.) —
/// all hot for indexed reads. Everything else lands in <c>DataJson</c>, the
/// "lift and shift" payload that lets us migrate now and normalize later as
/// queries demand. This is the "super flexible — new fields tackable on without
/// schema changes" path the user asked for.
/// </summary>

// ── Geographic / organizational ────────────────────────────────────────────────
// Place and Faction moved to their own files (Place.cs / Faction.cs) and are now
// fully relational — no DataJson column. The remaining types in this file still
// use the DataJson "lift and shift" path until each one is decomposed in turn.

// Corponation / Subsidiary / SyntheticLife / Automaton moved to dedicated files
// (Corponation.cs / Subsidiary.cs / SyntheticLife.cs / Automaton.cs) — fully
// relational, no DataJson column.

// Gear cluster (Weapon / Equipment / Cyberware / Apparel / Ammunition /
// Pharmaceutical / Genemod / Material / Transportation / ConsumerGood) moved
// to Gear.cs — fully relational, no DataJson columns.

// Story / canon content (Archetype / Quote / News / Contract / Document /
// Vocabulary / LabSpecimen / Psionic / Technology / Facet / Motif /
// Entertainment / FlyoverEntity) moved to Misc.cs — fully relational, no
// DataJson columns. Ceramic Men live on SyntheticLives (Type == "ceramic_man").

// ── Books / chapters / beats ────────────────────────────────────────────────

public class Book
{
    public Guid Id { get; set; }
    public string Title       { get; set; } = "";
    public string Slug        { get; set; } = "";
    public Guid? SeriesId     { get; set; }
    public string Tagline     { get; set; } = "";
    public string Premise     { get; set; } = "";
    public string ArcTarget   { get; set; } = "";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;

    public ICollection<BookProtagonist> Protagonists { get; set; } = new List<BookProtagonist>();
    public ICollection<BookChapterOrder> ChapterOrder { get; set; } = new List<BookChapterOrder>();
}

/// <summary>Book protagonist by name + resolved Character FK. Replaces Book.ProtagonistsJson.</summary>
public class BookProtagonist
{
    public long Id { get; set; }
    public Guid BookId { get; set; }
    public int Position { get; set; }
    public Guid? CharacterId { get; set; }
    public string Alias { get; set; } = "";
    public Book? Book { get; set; }
    public Entity? Character { get; set; }
}

/// <summary>Explicit ordering of a book's chapters. Replaces Book.ChapterIdsJson.</summary>
public class BookChapterOrder
{
    public long Id { get; set; }
    public Guid BookId { get; set; }
    public int Position { get; set; }
    public Guid ChapterId { get; set; }
    public Book? Book { get; set; }
    public Chapter? Chapter { get; set; }
}

public class Series
{
    public Guid Id { get; set; }
    public string Name        { get; set; } = "";
    public string Title       { get; set; } = "";
    public string Slug        { get; set; } = "";
    public string Description { get; set; } = "";
}

public class Chapter
{
    public Guid Id { get; set; }
    public Guid? BookId       { get; set; }
    public int? Number        { get; set; }
    public string Title       { get; set; } = "";
    public string Synopsis    { get; set; } = "";
    public string Status      { get; set; } = "draft";
    public string Html        { get; set; } = "";

    /// <summary>23rd-century in-world date the chapter takes place on. Used as the dossier asOf cursor.</summary>
    public DateTime? InWorldDate { get; set; }

    /// <summary>
    /// JSON array of structured StoryEvent records (what happened, who was
    /// involved, where, what changed). Replaces the legacy
    /// <c>engine/data/chapters/&lt;id&gt;/events.json</c> file. Null/empty
    /// when no events have been extracted yet. Owned and serialized by
    /// <see cref="StreetSamurai.Core.Services.EventLogService"/>.
    /// </summary>
    public string? EventsJson { get; set; }

    /// <summary>
    /// Serialized KnowledgeMap (information asymmetry — what each character
    /// knows, what the reader knows, when learned). Replaces the legacy
    /// <c>engine/data/chapters/&lt;id&gt;/knowledge.json</c> file. Owned by
    /// <see cref="StreetSamurai.Core.Services.KnowledgeMapService"/>.
    /// </summary>
    public string? KnowledgeJson { get; set; }

    /// <summary>
    /// Serialized StoryOutline — beat sheet with act structure, character
    /// arcs, seeds and payoffs. Replaces the legacy
    /// <c>engine/data/chapters/&lt;id&gt;/outline.json</c> file. Owned by
    /// <see cref="StreetSamurai.Core.Services.OutlineService"/>.
    /// </summary>
    public string? OutlineJson { get; set; }

    /// <summary>
    /// Serialized RefinementReport — beat-by-beat refinement notes from the
    /// human-in-the-loop refinement pass. Replaces the legacy
    /// <c>engine/data/chapters/&lt;id&gt;/refinement_report.json</c> file.
    /// Owned by <see cref="StreetSamurai.Core.Services.StoryRefinementService"/>.
    /// </summary>
    public string? RefinementReportJson { get; set; }

    /// <summary>
    /// Serialized StoryQualityReport — per-chapter quality scores + flagged
    /// clichés/strengths from the LLMVoting Quorum pass. Replaces the legacy
    /// <c>engine/data/chapters/&lt;id&gt;/quality_report.json</c> file.
    /// Owned by <see cref="StreetSamurai.Core.Services.StoryQualityService"/>.
    /// </summary>
    public string? QualityReportJson { get; set; }

    /// <summary>
    /// Serialized AutonomousStory — full pipeline checkpoint (beats, state,
    /// outline progress) for StoryDirectorService resume-from-failure. Replaces
    /// the legacy <c>engine/data/chapters/&lt;id&gt;/checkpoint.json</c> file.
    /// Owned by <see cref="StreetSamurai.Core.Services.StoryDirectorService"/>.
    /// </summary>
    public string? CheckpointJson { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ChapterBeat> Beats { get; set; } = new List<ChapterBeat>();
    public ICollection<ChapterCharacter> CharactersMentioned { get; set; } = new List<ChapterCharacter>();
}

/// <summary>Character mentioned in a chapter, alias + resolved Character FK. Replaces Chapter.CharactersJson.</summary>
public class ChapterCharacter
{
    public long Id { get; set; }
    public Guid ChapterId { get; set; }
    public int Position { get; set; }
    public Guid? CharacterId { get; set; }
    public string Alias { get; set; } = "";
    public Chapter? Chapter { get; set; }
    public Entity? Character { get; set; }
}

public class ChapterBeat
{
    public long Id { get; set; }
    public Guid BeatGuid      { get; set; }
    public Guid ChapterId     { get; set; }

    /// <summary>Stable index issued at creation; embedded in audio file paths.</summary>
    public int Index          { get; set; }

    /// <summary>Mutable ordering. Insertions / splits write here without
    /// renumbering siblings. UI sorts by SortKey ASC.</summary>
    public double SortKey     { get; set; }

    public string Title       { get; set; } = "";
    public string Synopsis    { get; set; } = "";
    public string Text        { get; set; } = "";
    public int Act            { get; set; }
    public string StructureRole { get; set; } = "";
    public string SceneType   { get; set; } = "scene";

    /// <summary>"tense" / "wry" / "tender" / "violent" / "quiet" — optional tone hint.</summary>
    public string? EmotionalTone { get; set; }

    /// <summary>"clipped" / "languorous" / "staccato" / "flowing" — optional cadence hint.</summary>
    public string? PaceHint { get; set; }

    /// <summary>23rd-century in-world date the beat takes place on (when known).</summary>
    public DateTime? InWorldDate { get; set; }

    // ── Recording state ──────────────────────────────────────────────
    public string? AudioPath     { get; set; }
    public double? DurationSec   { get; set; }
    public DateTime? NarratedAt  { get; set; }
    public string? LastRequestId { get; set; }
    public bool WasCorrected     { get; set; }

    public Chapter? Chapter   { get; set; }
}
