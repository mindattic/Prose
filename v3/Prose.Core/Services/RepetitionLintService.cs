using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;

namespace Prose.Core.Services;

public record RepetitionLintReport(
    string NodeCode,
    int BeatsScanned,
    int EchoFindings,
    int PhraseFindings,
    int PetWordFindings,
    int DialogueFindings,
    IReadOnlyList<string> Lines);

/// <summary>
/// Deterministic prose linter — zero LLM cost, pure CPU over a book's beats in reading order.
/// Before 2026-08-28 the engine had NO mechanical repetition detection anywhere: every
/// "don't echo", "vary your phrasing" rule was a prompt-side plea with no verification.
///
/// Checks:
///  1. Echo words — a distinctive word repeated in close proximity within one beat.
///  2. Crutch phrases — a distinctive 3-4-gram recurring within a beat or across a chapter.
///  3. Pet words — a distinctive word appearing in an outsized share of the book's beats.
///  4. Dialogue attribution — long runs of consecutive quoted paragraphs with no tag or
///     action beat (the "reader must know who's speaking" rule, previously prompt-only).
///  5. Airless narration — long runs of consecutive beats with ~zero dialogue, and
///     "floating heads" beats (very high dialogue proportion in a long beat), from the
///     already-computed-but-never-consumed BeatProseMetrics.DialogueProportion.
///
/// Findings are filed under FindingCategory.CraftChecklist with a "LINT " summary prefix on
/// FilePath "node:{slug}" — the exact shape ProseWriterRouter.BuildFindingsGuidanceAsync
/// loops back into future generation (same idiom as READABILITY). Re-runs delete-and-refile
/// (idempotent); character/place names are exempted from word checks via entity names.
/// </summary>
public class RepetitionLintService
{
    private readonly IDbContextFactory<ProseDbContext> dbFactory;
    private readonly FindingsService findings;
    private readonly ILogger<RepetitionLintService> log;

    public RepetitionLintService(
        IDbContextFactory<ProseDbContext> dbFactory,
        FindingsService findings,
        ILogger<RepetitionLintService> log)
    {
        this.dbFactory = dbFactory;
        this.findings = findings;
        this.log = log;
    }

    private const string LintPrefix = "LINT ";

    // Proximity echo: same distinctive word twice within this many tokens.
    private const int EchoWindowTokens = 50;
    // A word must recur at least this many times in one beat to be an echo finding.
    private const int EchoMinOccurrences = 3;
    // Crutch phrase: n-gram length range and thresholds.
    private const int PhraseMinPerBeat = 2;
    private const int PhraseMinPerChapter = 3;
    // Pet word: appears in at least this share of the book's beats (and ≥20 beats scanned).
    private const double PetWordBeatShare = 0.30;
    // Dialogue attribution: this many consecutive quoted-only paragraphs with no tag/action.
    private const int UnattributedRunFloor = 5;
    // Airless narration: consecutive beats below this dialogue proportion.
    private const double AirlessDialogueFloor = 0.02;
    private const int AirlessRunFloor = 6;
    // Floating heads: dialogue proportion above this in a beat this long.
    private const double FloatingHeadsProportion = 0.90;
    private const int FloatingHeadsMinWords = 600;

    private static readonly Regex WordRx = new(@"\b[a-zA-Z''’]+\b", RegexOptions.Compiled);
    private static readonly Regex QuoteRx = new(@"[“”""]", RegexOptions.Compiled);

    private static readonly HashSet<string> Stopwords = new(StringComparer.OrdinalIgnoreCase)
    {
        "the","and","that","with","this","from","they","them","their","there","then","than",
        "have","has","had","was","were","been","being","are","is","not","but","for","you",
        "your","his","her","hers","she","him","its","it's","what","when","where","which",
        "who","whom","whose","will","would","could","should","can","may","might","must",
        "into","onto","over","under","about","after","before","between","through","against",
        "because","while","until","again","also","just","only","even","still","back","down",
        "out","off","up","all","any","some","more","most","other","another","each","every",
        "very","too","how","why","here","now","one","two","like","said","says","did","does",
        "doesn't","don't","didn't","wasn't","isn't","aren't","won't","can't","couldn't",
        "wouldn't","shouldn't","around","across","away","along","behind","beneath","beside",
        "himself","herself","itself","themselves","something","nothing","anything","everything",
        "someone","anyone","everyone","never","always","once","twice","first","last","next",
        "own","same","such","both","few","many","much","those","these","our","ours","mine",
        "yours","theirs","let","lets","let's","get","got","gets","getting","made","make",
        "makes","making","went","gone","going","come","comes","coming","came","know","knows",
        "knew","known","think","thinks","thought","see","sees","saw","seen","look","looks",
        "looked","looking","say","saying","tell","tells","told","asked","asks","ask",
    };

    public async Task<RepetitionLintReport> LintAsync(
        string slugOrCode, bool dryRun = false, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var node = await db.Nodes.AsNoTracking().FirstOrDefaultAsync(
            n => n.Slug == slugOrCode || (n.NodeCode != null && n.NodeCode.ToUpper() == slugOrCode.ToUpper()), ct)
            ?? throw new InvalidOperationException($"Node not found: {slugOrCode}");
        var nodeCode = node.NodeCode?.ToUpperInvariant() ?? node.Slug.ToUpperInvariant();
        var fp = $"node:{node.Slug}";

        var searchIds = await NodeWorkbenchService.GetLeafDescendantIdsAsync(db, node.Id, ct);
        var beats = await (
            from bn in db.BeatNodes.AsNoTracking()
            join b in db.Beats.AsNoTracking() on bn.BeatId equals b.Id
            join c in db.Nodes.AsNoTracking() on bn.NodeId equals c.Id
            where searchIds.Contains(bn.NodeId) && b.Text != null && b.Text != ""
            orderby c.SortKey, bn.SortKey
            select new { b.Id, b.Number, Text = b.Text!, Chapter = c.Title }
        ).ToListAsync(ct);

        // Entity-name exemption: a character/place name repeating is normal prose, not an echo.
        var entityNameTokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var entityNames = await db.Set<Data.Entities.Entity>().AsNoTracking()
            .Where(e => e.UniverseId == node.UniverseId)
            .Select(e => e.Name).ToListAsync(ct);
        foreach (var name in entityNames)
            foreach (Match m in WordRx.Matches(name))
                entityNameTokens.Add(m.Value);

        if (!dryRun) findings.DeleteBySummaryPrefix(fp, LintPrefix);

        var lines = new List<string>();
        int echoCount = 0, phraseCount = 0, petCount = 0, dialogueCount = 0;

        void File(FindingSeverity sev, string summary, string? snippet = null, string? fix = null)
        {
            lines.Add(summary);
            if (!dryRun) findings.Upsert(fp, chapterId: null, FindingCategory.CraftChecklist, sev,
                LintPrefix + summary, snippet, fix);
        }

        // ── per-beat checks ────────────────────────────────────────────────────
        var beatsContainingWord = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var chapterPhrases = new Dictionary<string, Dictionary<string, int>>(); // chapter -> phrase -> count

        foreach (var beat in beats)
        {
            var stripped = BeatMarkup.StripEntityTags(beat.Text);
            var tokens = WordRx.Matches(stripped).Select(m => m.Value).ToList();

            // 1. Echo words in proximity
            var positions = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < tokens.Count; i++)
            {
                var t = tokens[i];
                if (t.Length < 5 || Stopwords.Contains(t) || entityNameTokens.Contains(t)) continue;
                if (!positions.TryGetValue(t, out var list)) positions[t] = list = new List<int>();
                list.Add(i);
            }
            foreach (var (word, pos) in positions)
            {
                if (pos.Count < EchoMinOccurrences) continue;
                var proximityPairs = 0;
                for (int i = 1; i < pos.Count; i++)
                    if (pos[i] - pos[i - 1] <= EchoWindowTokens) proximityPairs++;
                if (proximityPairs >= EchoMinOccurrences - 1)
                {
                    echoCount++;
                    File(FindingSeverity.Low,
                        $"beat #{beat.Number}: \"{word.ToLowerInvariant()}\" echoes {pos.Count}x in close proximity — vary or cut.",
                        fix: "Replace repeats with a synonym, pronoun, or restructure so the word carries once.");
                }
            }

            // 2. Crutch phrases (3-4-grams) within the beat + accumulate per chapter
            var beatPhrases = CountNgrams(tokens);
            foreach (var (phrase, count) in beatPhrases)
            {
                if (count >= PhraseMinPerBeat)
                {
                    phraseCount++;
                    File(FindingSeverity.Low,
                        $"beat #{beat.Number}: phrase \"{phrase}\" appears {count}x in one beat — crutch phrase.",
                        fix: "Keep the strongest instance; rewrite the others.");
                }
                if (!chapterPhrases.TryGetValue(beat.Chapter, out var chMap))
                    chapterPhrases[beat.Chapter] = chMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                chMap[phrase] = chMap.GetValueOrDefault(phrase) + count;
            }

            // 4. Dialogue attribution: consecutive quoted-only paragraphs without a tag/action
            var paragraphs = stripped.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            int run = 0, worstRun = 0;
            foreach (var p in paragraphs)
            {
                bool quotedOnly = QuoteRx.IsMatch(p) && IsQuotedOnly(p);
                run = quotedOnly ? run + 1 : 0;
                worstRun = Math.Max(worstRun, run);
            }
            if (worstRun >= UnattributedRunFloor)
            {
                dialogueCount++;
                File(FindingSeverity.Medium,
                    $"beat #{beat.Number}: {worstRun} consecutive dialogue paragraphs with no attribution or action beat — reader loses the speaker.",
                    fix: "Break the run with an action beat or attribution every 3-4 exchanges.");
            }

            // pet-word accumulation
            foreach (var w in positions.Keys.Distinct(StringComparer.OrdinalIgnoreCase))
                beatsContainingWord[w] = beatsContainingWord.GetValueOrDefault(w) + 1;
        }

        // 2b. Chapter-level crutch phrases
        foreach (var (chapter, phrases) in chapterPhrases)
        {
            foreach (var (phrase, count) in phrases.Where(p => p.Value >= PhraseMinPerChapter).Take(5))
            {
                phraseCount++;
                File(FindingSeverity.Low,
                    $"chapter '{chapter}': phrase \"{phrase}\" appears {count}x across the chapter — crutch phrase.",
                    fix: "Keep at most one instance per chapter.");
            }
        }

        // 3. Book-level pet words
        if (beats.Count >= 20)
        {
            var petWords = beatsContainingWord
                .Where(kv => (double)kv.Value / beats.Count >= PetWordBeatShare)
                .OrderByDescending(kv => kv.Value)
                .Take(10)
                .Select(kv => $"{kv.Key.ToLowerInvariant()} ({kv.Value}/{beats.Count} beats)")
                .ToList();
            if (petWords.Count > 0)
            {
                petCount++;
                File(FindingSeverity.Low,
                    $"pet words — distinctive words appearing in ≥{PetWordBeatShare:P0} of beats: {string.Join(", ", petWords)}.",
                    fix: "These are the book's tics; thin them where two land close together.");
            }
        }

        // 5. Airless narration runs + floating heads (from persisted BeatProseMetrics)
        var beatIds = beats.Select(b => b.Id).ToList();
        var metricsById = await db.BeatProseMetrics.AsNoTracking()
            .Where(m => beatIds.Contains(m.BeatId))
            .ToDictionaryAsync(m => m.BeatId, ct);
        int airlessRun = 0; int airlessStartNumber = 0;
        foreach (var beat in beats)
        {
            if (!metricsById.TryGetValue(beat.Id, out var m)) { airlessRun = 0; continue; }
            if (m.DialogueProportion < AirlessDialogueFloor && m.WordCount > 150)
            {
                if (airlessRun == 0) airlessStartNumber = beat.Number;
                airlessRun++;
            }
            else
            {
                if (airlessRun >= AirlessRunFloor)
                {
                    dialogueCount++;
                    File(FindingSeverity.Low,
                        $"beats #{airlessStartNumber}–#{beat.Number}: {airlessRun} consecutive beats with almost no dialogue — airless narration run.",
                        fix: "Let characters speak — break summary/narration with scene.");
                }
                airlessRun = 0;
            }
            if (m.DialogueProportion >= FloatingHeadsProportion && m.WordCount >= FloatingHeadsMinWords)
            {
                dialogueCount++;
                File(FindingSeverity.Low,
                    $"beat #{beat.Number}: {m.DialogueProportion:P0} dialogue over {m.WordCount} words — floating heads; ground the scene in bodies and place.",
                    fix: "Interleave physical action, setting, and interiority between exchanges.");
            }
        }
        if (airlessRun >= AirlessRunFloor)
        {
            dialogueCount++;
            File(FindingSeverity.Low,
                $"beats #{airlessStartNumber}–(end): {airlessRun} consecutive beats with almost no dialogue — airless narration run.",
                fix: "Let characters speak — break summary/narration with scene.");
        }

        log.LogInformation("[RepetitionLint] {Code}: {Beats} beats, {Echo} echo, {Phrase} phrase, {Pet} pet-word, {Dlg} dialogue findings",
            nodeCode, beats.Count, echoCount, phraseCount, petCount, dialogueCount);
        return new RepetitionLintReport(nodeCode, beats.Count, echoCount, phraseCount, petCount, dialogueCount, lines);
    }

    /// <summary>Distinctive 3-4-grams: every token lowercased; n-grams that are all stopwords
    /// or contain an entity-name token are skipped.</summary>
    private Dictionary<string, int> CountNgrams(List<string> tokens)
    {
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int n = 3; n <= 4; n++)
        {
            for (int i = 0; i + n <= tokens.Count; i++)
            {
                var slice = tokens.Skip(i).Take(n).ToList();
                // Distinctive = at least two non-stopword tokens of length ≥4.
                if (slice.Count(t => t.Length >= 4 && !Stopwords.Contains(t)) < 2) continue;
                var phrase = string.Join(' ', slice.Select(t => t.ToLowerInvariant()));
                counts[phrase] = counts.GetValueOrDefault(phrase) + 1;
            }
        }
        // Only phrases that actually recur are interesting; drop singletons early.
        return counts.Where(kv => kv.Value >= 2).ToDictionary(kv => kv.Key, kv => kv.Value);
    }

    private static bool IsQuotedOnly(string paragraph)
    {
        // A paragraph is "quoted only" when, after removing quoted spans, almost nothing
        // remains (no attribution/action words around the quotes).
        var outside = Regex.Replace(paragraph, @"[“""][^”""]*[”""]", " ");
        var outsideWords = WordRx.Matches(outside).Count;
        return outsideWords <= 1;
    }
}
