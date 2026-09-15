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
