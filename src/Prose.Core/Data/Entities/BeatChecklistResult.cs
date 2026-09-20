namespace Prose.Core.Data.Entities;

/// <summary>
/// Reader-Proxy QA Instrument 2 cache — one row per (beat, rule-set version): the
/// binary craft/delight checklist verdicts for that beat's exact text.
///
/// <para>The gate that <c>AuditRunner</c> lacks: a beat whose <see cref="BeatTextHash"/>
/// still matches <c>Beat.TextHash</c> AND whose <see cref="RuleSetVersion"/> still
/// matches the current parsed CRAFT/DELIGHT rule text is a pure cache hit — no LLM
/// call, no bill. Editing a beat re-evaluates ONE row; editing CRAFT.md §8 or a
/// DELIGHT move re-evaluates the book (the rules themselves changed).</para>
/// </summary>
public class BeatChecklistResult
{
    public Guid Id { get; set; }
    public Guid BeatId { get; set; }
    public Beat? Beat { get; set; }

    /// <summary>The book node this evaluation ran under (a beat shared across nodes
    /// is evaluated per book — chapter context differs).</summary>
    public Guid NodeId { get; set; }

    /// <summary>Beat.TextHash at evaluation time — the re-bill gate.</summary>
    public string BeatTextHash { get; set; } = "";

    /// <summary>Hash of the parsed CRAFT DON'T + DELIGHT move rule text + prompt
    /// version — editing the source docs invalidates the cache too.</summary>
    public string RuleSetVersion { get; set; } = "";

    /// <summary>Full verdicts JSON:
    /// { dontViolations:[{key,title,evidence}], delightMovesLanded:["SS-DELIGHT-3",…],
    ///   beatJob:"…", wordCount:N }.</summary>
    public string ResultsJson { get; set; } = "[]";

    /// <summary>Fraction of binary checks passed (DON'Ts not violated + ≥1 delight
    /// move landed where applicable). Reproducible: same text + same rules → same
    /// number. NOT an opinion score.</summary>
    public double PassFraction { get; set; }

    public DateTime EvaluatedAt { get; set; } = DateTime.UtcNow;
}
