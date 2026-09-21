using Prose.Core.Interfaces;

namespace Prose.Core.Composition.Obligations;

/// <summary>
/// One plant the writer reported immediately after generating the beat that made it, split at the
/// em dash the prompt asks for: the concrete thing, and the question left open about it.
/// </summary>
/// <param name="Referent">The one concrete object, person, place or posed question.</param>
/// <param name="Question">What the text leaves unresolved about it. Empty when the model gave no dash.</param>
public sealed record SelfReportedPlant(string Referent, string Question)
{
    /// <summary>The whole line as reported, for display and for storing as an obligation.</summary>
    public string Description => string.IsNullOrEmpty(Question) ? Referent : $"{Referent} — {Question}";

    /// <summary>Split a reported line on the first em/en dash or double hyphen. A model that
    /// ignores the format still yields a usable referent rather than being dropped.</summary>
    public static SelfReportedPlant Parse(string line)
    {
        var idx = line.IndexOfAny(['—', '–']);
        if (idx < 0)
        {
            var dbl = line.IndexOf("--", StringComparison.Ordinal);
            if (dbl >= 0) return new SelfReportedPlant(line[..dbl].Trim(), line[(dbl + 2)..].Trim());
            return new SelfReportedPlant(line.Trim(), "");
        }
        return new SelfReportedPlant(line[..idx].Trim(), line[(idx + 1)..].Trim());
    }
}

/// <summary>
/// One distinct plant after the same referent reported in several beats has been collapsed.
/// </summary>
/// <param name="Referent">The referent as first reported.</param>
/// <param name="Question">The question as first reported.</param>
/// <param name="ReportedInBeats">Every beat that reported it, in the order encountered.</param>
public sealed record MergedPlant(string Referent, string Question, IReadOnlyList<Guid> ReportedInBeats)
{
    public int ReportCount => ReportedInBeats.Count;
    public string Description => string.IsNullOrEmpty(Question) ? Referent : $"{Referent} — {Question}";
}

/// <summary>
/// v4 plan Phase 4, the "self-reported" half of explicit obligations. This is a deliberately
/// different task shape from <c>NarrativeObligationExtractor</c> (v3's blanket LLM-mined obligation
/// miner, measured THIS SAME SESSION at a ~96% false-positive rate on GCOBN — see RFC 0013 §8,
/// 2026-09-17): that extractor reads arbitrary already-written prose and guesses what it "sets up,"
/// with no operating notion of salience. This service asks ONE narrow question scoped to a single
/// beat, immediately after it's generated: "did THIS beat plant something, yes or no, name it or
/// say none" — closer to asking the author what they just did than mining a stranger's paragraph
/// for hidden meaning. Whether that difference in task shape actually produces a different
/// false-positive rate is a hypothesis, not a fact — it must clear the same GCTOC/GCSH/GCNEG/GCOBN
/// calibration before being trusted, exactly like every other instrument in this project.
/// </summary>
public sealed class SelfReportedPlantService
{
    private readonly ILlmService llm;

    public SelfReportedPlantService(ILlmService llm)
    {
        this.llm = llm;
    }

    public async Task<IReadOnlyList<SelfReportedPlant>> ExtractAsync(string beatText, CancellationToken ct = default)
    {
        // Tightened 2026-09-17 after GCTOC/GCSH calibration measured real, repeated over-triggering
        // (~3.1-3.2 plants/beat, vs. GCOBN's ~0.05) on classic/mystery-register prose — the false
        // positives read were consistently broad thematic/allegorical/backdrop commentary with no
        // single concrete, nameable referent (e.g. "the unresolved political tension between England
        // and France," "the Woodman and Farmer as Fate and Death"), never a texture-vs-plant error of
        // the GCOBN kind. The two-part HARD RULE + contrastive examples below target that specific
        // failure shape rather than re-deriving the whole prompt from scratch.
        const string system = "You identify narrative promises a piece of prose makes to its reader — nothing else. Most beats promise nothing; say so plainly when that's true.";
        var user = $"""
            BEAT TEXT (one scene from a story):
            {beatText}

            Question: did THIS beat introduce something a reader will actively expect to see
            explained, used, or resolved LATER in the story?

            HARD RULE, both parts must hold: (1) the plant must be a SPECIFIC, CONCRETE thing — one
            named or nameable object, person, place, or an explicit question the text itself poses in
            those terms ("what did it mean?", "who was she?") — never a theme, mood, historical
            backdrop, or authorial commentary with no single referent; and (2) the text must mark that
            specific thing as unresolved or mysterious RIGHT NOW, not merely mention it while telling
            the story.

            EXAMPLE — a real plant: "A message arrives reading only 'RECALLED TO LIFE.' The messenger
            frowns — he doesn't understand it either." → PLANT (one concrete message, explicitly
            marked as not understood).

            EXAMPLE — NOT a plant: "The chapter opens on how both England and France were ruled by
            kings convinced their nations were untroubled, while want and revolution gathered beneath
            them." → NOT a plant (a thematic/historical backdrop with no single object, person, or
            posed question to track and resolve).

            EXAMPLE — NOT a plant: "A room being described, a character doing something mundane, or
            an object being used and set back down" is texture, not a promise, even if described at
            length.

            List each real plant on its own line, in the form:
            PLANT: <the one concrete referent> — <the specific unresolved question about it>

            If this beat plants nothing meeting BOTH parts of the rule, output exactly:
            NONE

            Do not pad the list to find something. Most beats — including most beats in classic or
            densely-plotted fiction — plant nothing by this stricter standard.
            """;

        var raw = await llm.GenerateAsync(system, user, temperature: 0.0, maxTokens: 400, ct: ct);
        return raw.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(l => l.StartsWith("PLANT:", StringComparison.OrdinalIgnoreCase))
            .Select(l => SelfReportedPlant.Parse(l[6..].Trim()))
            .ToList();
    }

    // ── merging repeat reports of one plant ──────────────────────────────────────────────────

    /// <summary>
    /// Words that carry no identity. Dropped before comparing two referents so "the message" and
    /// "a message" are the same thing.
    /// </summary>
    private static readonly HashSet<string> Ignorable = new(StringComparer.OrdinalIgnoreCase)
    {
        "a", "an", "the", "of", "to", "in", "on", "at", "by", "for", "from", "with", "and", "or",
        "that", "which", "who", "whom", "whose", "this", "these", "those", "it", "its",
        "his", "her", "hers", "their", "theirs", "him", "them", "he", "she", "they",
        "is", "was", "are", "were", "be", "been", "being", "as", "about",
    };

    /// <summary>
    /// How much two referents' significant words must overlap to be judged the same plant.
    ///
    /// <para>Set high on purpose. Over-merging is the more dangerous error here: this number is
    /// used to measure a FALSE-POSITIVE rate, and collapsing two distinct false positives into one
    /// row makes the instrument look better than it is. Under-merging only leaves the count
    /// pessimistic, which is the safe direction to be wrong in. 0.6 is a starting point chosen to
    /// be conservative, not a calibrated constant — it is itself subject to measurement.</para>
    /// </summary>
    public const double MergeSimilarityThreshold = 0.6;

    private static HashSet<string> Significant(string referent) =>
        new(referent
                .Split([' ', '\t', ',', '.', ';', ':', '"', '\'', '(', ')', '[', ']', '“', '”', '‘', '’', '/', '\\'],
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(w => w.Trim().ToLowerInvariant())
                .Where(w => w.Length > 0 && !Ignorable.Contains(w)),
            StringComparer.Ordinal);

    /// <summary>Jaccard overlap of two referents' significant words. 1.0 when identical.</summary>
    internal static double Similarity(string a, string b)
    {
        var sa = Significant(a);
        var sb = Significant(b);
        if (sa.Count == 0 || sb.Count == 0) return string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase) ? 1.0 : 0.0;
        var intersection = sa.Count(sb.Contains);
        var union = sa.Count + sb.Count - intersection;
        return union == 0 ? 0.0 : (double)intersection / union;
    }

    /// <summary>
    /// Collapse repeat reports of one plant into a single row.
    ///
    /// <para><b>Why this is not optional.</b> A plant introduced in one beat is frequently reported
    /// again by every later beat that touches it, so the raw per-beat total counts one promise many
    /// times. Compared against an answer key of DISTINCT plants, that raw number is not a
    /// false-positive rate — it is a false-positive rate plus an unknown amount of repetition, and
    /// the two cannot be separated after the fact. Calibration numbers taken before this existed
    /// (GCTOC 53, then 34 after the prompt was tightened) are raw counts and were never comparable
    /// to the 15-row key.</para>
    ///
    /// <para>Deterministic and free: no second LLM call. Reports arrive in beat order and the first
    /// wording of a referent wins, so the row reads as the plant was first stated.</para>
    /// </summary>
    public static IReadOnlyList<MergedPlant> Merge(IEnumerable<(Guid BeatId, SelfReportedPlant Plant)> reports)
    {
        var merged = new List<(string Referent, string Question, List<Guid> Beats)>();

        foreach (var (beatId, plant) in reports)
        {
            var hit = merged.FirstOrDefault(m => Similarity(m.Referent, plant.Referent) >= MergeSimilarityThreshold);
            if (hit.Beats is null)
                merged.Add((plant.Referent, plant.Question, [beatId]));
            else if (!hit.Beats.Contains(beatId))
                hit.Beats.Add(beatId);
        }

        return merged.Select(m => new MergedPlant(m.Referent, m.Question, m.Beats)).ToList();
    }
}
