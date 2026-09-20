namespace Prose.Core.Data.Entities;

/// <summary>
/// Cache for the resurfacing judge (RFC 0013 `--reconcile-obligations --deep`): one verdict per
/// (obligation, candidate beat, candidate text hash, prompt version). A candidate whose text has
/// not changed is never re-judged, so a re-run of the deep pass over an unchanged book is free.
/// </summary>
public class ObligationJudgeCache
{
    public Guid     Id                { get; set; } = Guid.CreateVersion7();
    public Guid     ObligationId      { get; set; }
    public Guid     CandidateBeatId   { get; set; }
    public string   CandidateTextHash { get; set; } = "";
    public string   PromptVersion     { get; set; } = "";
    /// <summary>closes | advances | not_addressed | ungrounded (the model claimed a quote the text did not contain).</summary>
    public string   Relation          { get; set; } = "not_addressed";
    public string?  Quote             { get; set; }
    public DateTime CreatedAt         { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// One row per reconciliation run with something examined — the numbers the author watches over
/// time (RFC 0013 D6f). Written only when the ledger had rows and beats to look at; a run that
/// COULD NOT LOOK writes nothing rather than a row of zeros that reads as health.
/// </summary>
public class NarrativeHealthSnapshot
{
    public Guid     Id                          { get; set; } = Guid.CreateVersion7();
    public Guid     NodeId                      { get; set; }
    public DateTime TakenAt                     { get; set; } = DateTime.UtcNow;
    /// <summary>Hash over every beat's TextHash in reading order — two snapshots with the same
    /// value describe the same manuscript.</summary>
    public string   BookTextHash                { get; set; } = "";
    public int      WordCount                   { get; set; }
    public int      TotalBeats                  { get; set; }
    public int      ExaminedBeats               { get; set; }
    public int      Open                        { get; set; }
    public int      Overdue                     { get; set; }
    public int      Closed                      { get; set; }
    public int      Dropped                     { get; set; }
    public int      Deferred                    { get; set; }
    /// <summary>Closed / (Closed + Overdue). 1.0 when nothing is overdue.</summary>
    public double   PayoffCoveragePct           { get; set; }
    /// <summary>Unnamed referents the deterministic scanner flags, per 10k words.</summary>
    public double   UnnamedReferentDensity10k   { get; set; }
    /// <summary>Open High/Medium findings from LOGICSWEEP, FACT-LEDGER, TUNEDREAD and OBLIGATION
    /// per 10k words. Comparable to the literature's best (0.113, GPT-5 on *Lost in Stories*) only
    /// once the calibration harness has passed for this instrument.</summary>
    public double   ConsistencyErrorDensity10k  { get; set; }
    /// <summary>From the last entity-record grounding run for this book; null until one has run.</summary>
    public double?  UnentailedRecordClaimPct    { get; set; }
    public string   InstrumentVersion           { get; set; } = "";
}

/// <summary>
/// What happened the last time the extractor tried to read one beat (RFC 0013, 2026-09-16).
///
/// <para><b>Why this exists.</b> The only durable evidence an unread beat left was the *absence* of
/// <c>Beat.ObligationScanHash</c>, and the reason for the failure reached an <c>ILogger</c> and
/// stopped there. That made two very different beats identical in the database: one that was read
/// and owed nothing, and one the extractor could not read at all. Worse, a third case had no
/// representation anywhere — a beat that WAS read, found debts, and had every one of them thrown
/// out by the quote gate, which reports <c>opened = 0</c> exactly like a beat that owed nothing.
/// That is the leading explanation for the GCTOC beat-3 blind spot (the beat carrying
/// <c>RECALLED TO LIFE</c>, which opened nothing at all).</para>
///
/// <para>One row per attempt, not per beat: the history is the point. A beat that needed a retry,
/// or that only read after being halved, is a beat whose budget is marginal.</para>
/// </summary>
public class ObligationScanAttempt
{
    public Guid     Id                  { get; set; } = Guid.CreateVersion7();
    /// <summary>The BOOK node, matching every other obligation row's scoping.</summary>
    public Guid     NodeId              { get; set; }
    public Guid     BeatId              { get; set; }
    /// <summary>Extractor <c>PromptVersion</c> this attempt ran under. A number is only comparable
    /// to another computed under the same rules.</summary>
    public string   PromptVersion       { get; set; } = "";
    public int      BeatChars           { get; set; }
    public int      WindowsTotal        { get; set; }
    public int      WindowsRead         { get; set; }
    /// <summary>See <see cref="ObligationScanOutcome"/>.</summary>
    public string   Outcome             { get; set; } = ObligationScanOutcome.Read;
    /// <summary>Why, when the read failed or was partial. Null on a clean read.</summary>
    public string?  Failure             { get; set; }
    public int      Opened              { get; set; }
    public int      Advanced            { get; set; }
    public int      Closed              { get; set; }
    /// <summary>Items the model produced that could not quote the beat and were thrown away. When
    /// this is positive and <see cref="Opened"/> is zero, the beat was read and its entire harvest
    /// discarded — which is NOT the same as a beat that owed nothing.</summary>
    public int      DiscardedUngrounded { get; set; }
    public DateTime CreatedAt           { get; set; } = DateTime.UtcNow;
}

/// <summary>The four distinguishable outcomes of trying to read one beat. Before these existed the
/// database could only say "stamped" or "not stamped".</summary>
public static class ObligationScanOutcome
{
    /// <summary>Every window read. The beat is stamped.</summary>
    public const string Read = "read";
    /// <summary>Every window read, the model produced items, and the quote gate discarded all of
    /// them. Stamped — the read was real — but this is a fidelity defect, not an empty beat.</summary>
    public const string ReadAllDiscarded = "read-all-discarded";
    /// <summary>Some windows read and are banked; at least one could not be read after retry and
    /// halving. NOT stamped — the beat is re-read.</summary>
    public const string Partial = "partial";
    /// <summary>No window could be read. NOT stamped.</summary>
    public const string Unread = "unread";

    public static bool CouldNotLook(string outcome) => outcome is Partial or Unread;
}

/// <summary>
/// A synthetic defect the calibration harness injected into a gutenberg-universe book (RFC 0013
/// D7): the sentence appended, where, the exact text it replaced (so the revert is byte-exact),
/// and for a "resolved" injection the payoff sentence and its beat. The harness scores the
/// instrument against these rows — TP/FP/FN are computed from them, never eyeballed.
/// </summary>
public class CalibrationInjection
{
    public Guid     Id               { get; set; } = Guid.CreateVersion7();
    public Guid     NodeId           { get; set; }
    public Guid     BeatId           { get; set; }
    public string   PriorText        { get; set; } = "";
    public string   Sentence         { get; set; } = "";
    /// <summary>abandoned | resolved</summary>
    public string   Kind             { get; set; } = "abandoned";
    public Guid?    PayoffBeatId     { get; set; }
    public string?  PayoffPriorText  { get; set; }
    public string?  PayoffSentence   { get; set; }
    public int      Seed             { get; set; }
    public DateTime CreatedAt        { get; set; } = DateTime.UtcNow;
}
