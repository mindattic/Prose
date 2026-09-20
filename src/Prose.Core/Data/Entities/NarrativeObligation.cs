namespace Prose.Core.Data.Entities;

/// <summary>
/// A narrative promise the story owes the reader, logged at the moment it is made (RFC 0013).
///
/// <para>The unit of the continuity ledger. A row is opened when prose introduces a setup,
/// question, unnamed referent, wound, or foreshadowing — with the verbatim sentence that made
/// the promise and the beat it came from — and is closed only by a quote from the beat that pays
/// it. The trial balance (<c>NarrativeObligationService.TrialBalanceAsync</c>) ages rows by
/// <see cref="DueByKind"/>/<see cref="DueByValue"/> against <c>Beat.StoryPosition</c>; a row
/// still Open past its due point with no author decision is a finding, and so is a payoff with
/// no origin. An empty ledger fails, it does not pass.</para>
///
/// <para>Replaces <c>NodeOpenThreads</c> (free-text promises with no quote, no due-by, no entity,
/// no read path — migrated in as <see cref="Provenance"/> = inferred) and bridges
/// <see cref="PlantPayoff"/> (a hand-curated plant pair is a specialised annotation on a
/// <c>plant</c>-kind row via <c>PlantPayoff.ObligationId</c>).</para>
///
/// <para>Ledgers reverse entries; they never erase them. The system may create rows and move
/// unlocked rows between states, but a row an author has touched (<see cref="AuthorLocked"/>) is
/// immutable to every automated path.</para>
/// </summary>
public class NarrativeObligation
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>The BOOK root — same scoping as <c>BeatContext.NodeId</c>, so a promise made in
    /// chapter 1 is visible when chapter 34 is written.</summary>
    public Guid  NodeId { get; set; }
    public Node? Node   { get; set; }

    /// <summary>One of <see cref="ObligationKind"/>.</summary>
    public string Kind { get; set; } = ObligationKind.Promise;

    /// <summary>What is owed, in one line.</summary>
    public string Description { get; set; } = "";

    /// <summary>One of <c>ClaimProvenance</c> — authored (outline / MCP / CLI / bible import),
    /// observed (extracted, quote verified against the beat text), inferred (no verifying quote;
    /// the migrated NodeOpenThreads rows).</summary>
    public string Provenance { get; set; } = Services.ClaimProvenance.Observed;

    /// <summary>Beat that made the promise. NoAction FK — cleared explicitly by
    /// <c>NodeWorkbenchService.ClearEdgeBeatBoundsAsync</c>, never blocks a delete.</summary>
    public Guid?  OriginBeatId { get; set; }
    public Beat?  OriginBeat   { get; set; }

    /// <summary>Verbatim, whitespace-normalised sentence from the origin beat. The mechanical
    /// grounding: a row without one is never <c>observed</c>.</summary>
    public string? OriginQuote { get; set; }

    /// <summary><c>Beat.TextHash</c> at registration; staleness is
    /// <c>Beat.SummaryTrustState(OriginQuote, OriginTextHash, beat.TextHash)</c>.</summary>
    public string? OriginTextHash { get; set; }

    /// <summary>The referent this promise is about — a canon entity or a
    /// <c>Status="stub"</c> one named <c>"(unnamed) …"</c> created by the extractor.</summary>
    public Guid?    EntityId { get; set; }
    public Entity?  Entity   { get; set; }

    /// <summary>CFPG's "trigger" — the narrative condition under which paying this off becomes
    /// natural ("when Kyle next visits Mrs. Chen's"). Shown in the Brief; not machine-evaluated.</summary>
    public string? TriggerCondition { get; set; }

    /// <summary>One of <see cref="ObligationDueKind"/>: chapter (leaf ordinal, 1-based), beats
    /// (count after the origin beat's StoryPosition), or book-end (value unused).</summary>
    public string DueByKind  { get; set; } = ObligationDueKind.BookEnd;
    public int?   DueByValue { get; set; }

    /// <summary>One of <see cref="ObligationState"/>.</summary>
    public string State { get; set; } = ObligationState.Open;

    public Guid?   ClosingBeatId   { get; set; }
    public Beat?   ClosingBeat     { get; set; }
    public string? ClosingQuote    { get; set; }
    public string? ClosingTextHash { get; set; }

    /// <summary>Required for Dropped / Deferred — the author's reason, in their words.</summary>
    public string? AuthorNote { get; set; }

    /// <summary>One of <see cref="ObligationDroppedReason"/> when State is Dropped. Feeds the
    /// extractor's negative few-shots; <c>false-extraction</c> is the one that trains it.</summary>
    public string? DroppedReason { get; set; }

    /// <summary>Set by any author write. A locked row is never deleted, withdrawn, re-stated or
    /// re-anchored by the system.</summary>
    public bool AuthorLocked { get; set; }

    /// <summary>SHA1 of <c>NodeId|Kind|normalised Description</c>; unique per node so imports
    /// and re-extraction are idempotent.</summary>
    public string DedupKey { get; set; } = "";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// The journal behind <see cref="NarrativeObligation"/> — every state change with who did it and,
/// where a beat was involved, the quote and text hash that justified it. Advances are
/// multi-valued, so this is where the provenance trail lives; the head table is not
/// system-versioned.
/// </summary>
public class NarrativeObligationEvent
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid                 ObligationId { get; set; }
    public NarrativeObligation? Obligation   { get; set; }

    /// <summary>One of <see cref="ObligationEventAction"/>.</summary>
    public string Action { get; set; } = ObligationEventAction.Open;

    public Guid?   BeatId       { get; set; }
    public string? Quote        { get; set; }
    public string? BeatTextHash { get; set; }

    /// <summary>One of <see cref="ObligationActor"/>.</summary>
    public string Actor { get; set; } = ObligationActor.SystemExtract;

    public string?  Note      { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public static class ObligationKind
{
    public const string Promise            = "promise";
    public const string Plant              = "plant";
    public const string Question           = "question";
    public const string Wound              = "wound";
    public const string Foreshadow         = "foreshadow";
    /// <summary>An unnamed person/object given narrative weight — "the girl behind the curtain".</summary>
    public const string IntroducedReferent = "introduced-referent";
    /// <summary>Someone present with no stated reason to be — the child in the ductwork.</summary>
    public const string UnexplainedPresence = "unexplained-presence";

    public static readonly string[] All =
        [Promise, Plant, Question, Wound, Foreshadow, IntroducedReferent, UnexplainedPresence];

    public static bool IsValid(string? kind) => kind != null && All.Contains(kind);
}

public static class ObligationState
{
    public const string Open      = "Open";
    public const string Advanced  = "Advanced";
    public const string Closed    = "Closed";
    public const string Dropped   = "Dropped";
    public const string Deferred  = "Deferred";
    /// <summary>System-only terminal state: the origin text vanished before anything relied on
    /// the row. Never applied to a locked or advanced row.</summary>
    public const string Withdrawn = "Withdrawn";

    public static readonly string[] All = [Open, Advanced, Closed, Dropped, Deferred, Withdrawn];

    /// <summary>States that still owe the reader something.</summary>
    public static bool IsOutstanding(string state) => state is Open or Advanced;
}

public static class ObligationDueKind
{
    public const string Chapter = "chapter";
    public const string Beats   = "beats";
    public const string BookEnd = "book-end";
}

public static class ObligationEventAction
{
    public const string Open       = "open";
    public const string Advance    = "advance";
    public const string Close      = "close";
    public const string Reopen     = "reopen";
    public const string Drop       = "drop";
    public const string Defer      = "defer";
    public const string Withdraw   = "withdraw";
    public const string Reanchor   = "reanchor";
    public const string DueChanged = "due-changed";
    public const string Lock       = "lock";
}

public static class ObligationActor
{
    public const string SystemExtract = "system:extract";
    public const string SystemRescan  = "system:rescan";
    public const string SystemDeep    = "system:deep";
    public const string AuthorMcp     = "author:mcp";
    public const string AuthorCli     = "author:cli";
    public const string ImportBible   = "import:bible";
    public const string Migration     = "migration";

    public static bool IsAuthor(string actor) => actor.StartsWith("author:", StringComparison.Ordinal);
}

public static class ObligationDroppedReason
{
    public const string BackgroundTexture  = "background-texture";
    public const string PovLimited         = "pov-limited";
    public const string GenreConvention    = "genre-convention";
    public const string IntentionalMystery = "intentional-mystery";
    public const string SeriesDeferred     = "series-deferred";
    /// <summary>The extractor invented an obligation the prose never made — the one reason that
    /// re-tunes the extractor.</summary>
    public const string FalseExtraction    = "false-extraction";
    /// <summary>Prefix form: <c>duplicate-of:&lt;obligation id&gt;</c>.</summary>
    public const string DuplicateOfPrefix  = "duplicate-of:";
    /// <summary>The bible's own §14b "dropped findings" table, imported verbatim.</summary>
    public const string AuthorNote         = "author-note";

    public static bool IsValid(string? reason) =>
        reason != null && (reason.StartsWith(DuplicateOfPrefix, StringComparison.Ordinal)
            || reason is BackgroundTexture or PovLimited or GenreConvention or IntentionalMystery
                      or SeriesDeferred or FalseExtraction or AuthorNote);
}
