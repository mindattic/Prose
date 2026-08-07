using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Prose.Core.Interfaces;
using Prose.Core.Models;
using Prose.Core.Models.Canon;

namespace Prose.Core.Services;

/// <summary>
/// Heuristic writing-quality checks that don't require an LLM call. Runs as a
/// pre-LLM pass during book review and surfaces deterministic findings — first-line
/// strength, paragraph-serves checks, tension delta tracking, voice cadence drift.
/// Cheap, fast, complementary to the multi-LLM Quorum review. Findings produced
/// here use the same <see cref="ReviewFinding"/> shape so the UI doesn't have to
/// distinguish heuristic vs LLM origin.
/// </summary>
public class WritingQualityService
{
    private readonly DatabaseService db;
    private readonly ILogger<WritingQualityService> log;

    // Tokens that indicate concrete sensory detail in a chapter opener.
    private static readonly HashSet<string> SenseTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "light","dark","cold","warm","hot","wet","dry","loud","quiet","silence","smoke","smell","scent","stink",
        "taste","touch","sound","music","noise","crash","whisper","glow","shine","shadow","flicker","heat","chill",
        "wind","rain","snow","dust","mud","blood","sweat","steel","glass","brick","concrete","fluorescent","neon"
    };

    // Generic openers that betray a flat first line.
    private static readonly Regex GenericOpenerRx = new(
        @"^(it was|there was|he was|she was|they were|the day was|once upon|in the beginning)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public WritingQualityService(DatabaseService db, ILogger<WritingQualityService> log)
    {
        this.db = db;
        this.log = log;
    }

    /// <summary>
    /// Run every heuristic check across the book's ordered chapters and return findings.
    /// Caller is responsible for distributing them into the report's BookFindings/ChapterFindings/SeamFindings buckets.
    /// </summary>
    public List<ReviewFinding> Analyze(Book book, List<Chapter> ordered, MotifInventory? motifs = null)
    {
        var findings = new List<ReviewFinding>();

        for (int i = 0; i < ordered.Count; i++)
        {
            var c = ordered[i];
            var plain = StripHtmlAndMarkdown(c.Html ?? "");

            findings.AddRange(CheckFirstLine(c, plain));
            findings.AddRange(CheckTensionDelta(c, ordered, i));
            findings.AddRange(CheckParagraphService(c, plain));

            if (motifs != null)
                findings.AddRange(CheckMotifReuse(c, plain, motifs));

            // Exclude the current chapter from its own fingerprint — including it
            // makes the comparison circular (it will always match itself).
            var fingerprints = BuildVoiceFingerprints(book, ordered, excludeId: c.Id);
            if (fingerprints.Count > 1)
                findings.AddRange(CheckVoiceCadence(c, plain, fingerprints));
        }

        return findings;
    }

    // ── First-line audit ─────────────────────────────────────────────────

    private static List<ReviewFinding> CheckFirstLine(Chapter c, string plain)
    {
        // Strip leading title heading if present.
        var head = StripChapterHeading(plain);
        if (string.IsNullOrWhiteSpace(head)) return [];

        // First sentence ~ first 200 chars or to first '.' / '!' / '?'.
        var firstSentenceEnd = head.IndexOfAny(['.', '!', '?']);
        var firstSentence = firstSentenceEnd > 0 ? head[..firstSentenceEnd] : head[..Math.Min(head.Length, 200)];

        var hasSense = HasSenseToken(firstSentence);
        var hasGenericOpener = GenericOpenerRx.IsMatch(firstSentence.TrimStart());
        var tooShort = firstSentence.Trim().Length < 8;

        if (hasGenericOpener || (!hasSense && tooShort))
        {
            return
            [
                new ReviewFinding
                {
                    Layer = ReviewLayer.Chapter,
                    Kind = ReviewKind.FirstLine,
                    Severity = ReviewSeverity.Warning,
                    ChapterId = c.Id,
                    Title = "Chapter opens with a generic first line",
                    Rationale = hasGenericOpener
                        ? "First sentence starts with a generic opener (\"It was\", \"He was\", etc.). Open with concrete sensory detail or a specific observation only this POV would make."
                        : "First sentence is short and lacks concrete sensory grounding. The opening should anchor the reader in the POV character's perception of THIS specific moment.",
                    VoterAgreement = 1,
                }
            ];
        }
        return [];
    }

    // ── Tension delta tracking ───────────────────────────────────────────

    private static List<ReviewFinding> CheckTensionDelta(Chapter c, List<Chapter> ordered, int idx)
    {
        // Use Beats if present — that's where structure_role lives. Otherwise skip.
        if (c.Beats.Count < 4) return [];

        // Map structure_role to a coarse tension score. Anything resembling setup/breath = 1, rising = 2, climax/turn = 3, denouement = 1.
        var tensions = c.Beats.Select(b => TensionFor(b.StructureRole)).ToList();

        // Detect 4+ consecutive low-tension beats — the pacing collapse the user warned about.
        int run = 1;
        for (int i = 1; i < tensions.Count; i++)
        {
            if (tensions[i] == 1 && tensions[i - 1] == 1) run++;
            else run = 1;

            if (run >= 4)
            {
                return
                [
                    new ReviewFinding
                    {
                        Layer = ReviewLayer.Chapter,
                        Kind = ReviewKind.TensionDelta,
                        Severity = ReviewSeverity.Warning,
                        ChapterId = c.Id,
                        Title = $"Pacing flat — {run} consecutive low-tension beats",
                        Rationale = $"Beats {i - run + 2} through {i + 1} all read as setup/breath. The reader's attention budget is finite. Inject a stake, a small reveal, or a tension turn somewhere in this run.",
                        VoterAgreement = 1,
                    }
                ];
            }
        }
        return [];
    }

    private static int TensionFor(string role) => (role ?? "").ToLowerInvariant() switch
    {
        var r when r.Contains("climax") || r.Contains("turn") || r.Contains("crisis") => 3,
        var r when r.Contains("rising") || r.Contains("escalat") || r.Contains("complic") => 2,
        var r when r.Contains("setup") || r.Contains("establish") || r.Contains("breath") || r.Contains("denou") => 1,
        _ => 2,
    };

    // ── Paragraph-serves audit ───────────────────────────────────────────

    private static List<ReviewFinding> CheckParagraphService(Chapter c, string plain)
    {
        // Split on blank lines into paragraphs; ignore very short pieces (likely scene breaks or single italics).
        var paragraphs = Regex.Split(plain, @"\n\s*\n")
            .Select(p => p.Trim())
            .Where(p => p.Length > 0)
            .ToList();

        var findings = new List<ReviewFinding>();
        foreach (var p in paragraphs)
        {
            if (p.Length < 25) continue;  // scene break / single line / italic aside — fine

            // Trivial paragraph: short, lacks any of: concrete noun, action verb, dialogue, sensory token.
            var hasDialogue = p.Contains('"') || p.Contains('“') || p.Contains('”');
            var hasSense = HasSenseToken(p);
            var hasActionVerb = ActionVerbRx.IsMatch(p);
            var hasNumber = Regex.IsMatch(p, @"\d");
            var hasCapitalizedNoun = Regex.IsMatch(p, @"\b[A-Z][a-z]{2,}\b");

            int signals = (hasDialogue ? 1 : 0) + (hasSense ? 1 : 0) + (hasActionVerb ? 1 : 0)
                        + (hasNumber ? 1 : 0) + (hasCapitalizedNoun ? 1 : 0);

            if (signals == 0 && p.Length > 60)
            {
                findings.Add(new ReviewFinding
                {
                    Layer = ReviewLayer.Chapter,
                    Kind = ReviewKind.ParagraphService,
                    Severity = ReviewSeverity.Suggestion,
                    ChapterId = c.Id,
                    Title = "Paragraph carries no specific signals",
                    Rationale = "This paragraph has no dialogue, no sensory token, no action verb, no number, and no proper noun. Either it's pure exposition (rewrite as something a character does or notices) or it's filler (cut it).",
                    BeforeText = p,
                    AfterText = "",  // no LLM-generated rewrite — diagnostic only
                    VoterAgreement = 1,
                });
                if (findings.Count >= 5) break;  // cap noise per chapter
            }
        }
        return findings;
    }

    private static readonly Regex ActionVerbRx = new(
        @"\b(walks?|walked|runs?|ran|sat|sits?|stood|stands?|moved|reached|looked|grabbed|pulled|pushed|opened|closed|drew|drove|threw|caught|hit|struck|dropped|fell|knelt|leaned|turned|slid|stepped|set|picked|carried|broke|lifted|tied|wiped|cut|slipped|knocked|laughed|cried|whispered|shouted|breathed|swallowed|nodded|shook)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // ── Motif reuse ──────────────────────────────────────────────────────

    private static List<ReviewFinding> CheckMotifReuse(Chapter c, string plain, MotifInventory motifs)
    {
        // Only check for motifs registered before this chapter's position.
        var availableForCallback = motifs.Motifs
            .Where(m => string.IsNullOrEmpty(m.IntroducedInChapterId) || m.IntroducedInChapterId != c.Id)
            .ToList();
        if (availableForCallback.Count == 0) return [];

        var referenced = availableForCallback.Where(m => plain.Contains(m.Name, StringComparison.Ordinal)).ToList();
        if (referenced.Count == 0 && availableForCallback.Count >= 3)
        {
            // Chapter doesn't reference any registered motif — flag if there are 3+ available.
            var sample = string.Join(", ", availableForCallback.Take(5).Select(m => m.Name));
            return
            [
                new ReviewFinding
                {
                    Layer = ReviewLayer.Chapter,
                    Kind = ReviewKind.Motif,
                    Severity = ReviewSeverity.Suggestion,
                    ChapterId = c.Id,
                    Title = "Chapter references no established motifs",
                    Rationale = $"This book has motifs registered ({sample}). None appears in this chapter. Anaphoric callbacks to one or two would thread the chapter into the book.",
                    VoterAgreement = 1,
                }
            ];
        }
        return [];
    }

    // ── Voice cadence drift ──────────────────────────────────────────────

    /// <summary>
    /// Builds a per-protagonist vocabulary fingerprint from their existing prose
    /// (chapters where they're the lead). When a new chapter arrives, we can compare
    /// its fingerprint to the expected one — drift means the prose sounds like
    /// a different character than the chapter claims.
    /// </summary>
    private Dictionary<string, HashSet<string>> BuildVoiceFingerprints(Book book, List<Chapter> ordered, string? excludeId = null)
    {
        var fingerprints = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var protagonist in book.Protagonists)
        {
            var chars = ordered
                .Where(c => c.Id != excludeId && c.Characters.Contains(protagonist, StringComparer.OrdinalIgnoreCase))
                .SelectMany(c => DistinctiveTokens(StripHtmlAndMarkdown(c.Html ?? "")));

            // Augment with canonical speech_patterns.example_lines if available — even
            // if the book has no chapters for this protagonist yet, the character file
            // gives us a vocabulary baseline.
            var canonical = db.FindCharacter(protagonist);
            if (canonical?.SpeechPatterns?.ExampleLines != null)
            {
                chars = chars.Concat(canonical.SpeechPatterns.ExampleLines.SelectMany(DistinctiveTokens));
            }

            var set = chars.ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (set.Count > 20) fingerprints[protagonist] = set;  // skip thin fingerprints
        }
        return fingerprints;
    }

    private static List<ReviewFinding> CheckVoiceCadence(
        Chapter c, string plain, Dictionary<string, HashSet<string>> fingerprints)
    {
        if (c.Characters.Count == 0) return [];
        var lead = c.Characters[0];
        if (!fingerprints.ContainsKey(lead)) return [];

        var chapterTokens = DistinctiveTokens(plain).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (chapterTokens.Count < 30) return [];

        // For each protagonist we have a fingerprint for, compute Jaccard overlap with this chapter.
        var scores = fingerprints
            .Select(kv => new
            {
                Character = kv.Key,
                Score = (double)chapterTokens.Intersect(kv.Value).Count() /
                        Math.Max(1, chapterTokens.Union(kv.Value).Count())
            })
            .OrderByDescending(x => x.Score)
            .ToList();

        // If the chapter's lead character is NOT the top match, the voice has drifted.
        if (!string.Equals(scores[0].Character, lead, StringComparison.OrdinalIgnoreCase))
        {
            // Match the lead case-insensitively — the fingerprint dictionary key
            // can differ in casing from c.Characters[0], and a case-sensitive
            // First() here would throw InvalidOperationException on that mismatch.
            var leadScore = scores.FirstOrDefault(s => string.Equals(s.Character, lead, StringComparison.OrdinalIgnoreCase))?.Score ?? 0.0;
            return
            [
                new ReviewFinding
                {
                    Layer = ReviewLayer.Chapter,
                    Kind = ReviewKind.VoiceCadence,
                    Severity = ReviewSeverity.Warning,
                    ChapterId = c.Id,
                    Title = $"Voice fingerprint matches {scores[0].Character} more than {lead}",
                    Rationale = $"This chapter's vocabulary is closer to {scores[0].Character}'s established voice ({scores[0].Score:F2}) than to the chapter's lead character {lead} ({leadScore:F2}). Push the prose harder toward {lead}'s specific cadence — what they notice first, what vocabulary they use, what they joke about.",
                    VoterAgreement = 1,
                }
            ];
        }
        return [];
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private static bool HasSenseToken(string s)
    {
        var tokens = Regex.Matches(s, @"\b[a-zA-Z]{3,}\b").Select(m => m.Value);
        return tokens.Any(t => SenseTokens.Contains(t));
    }

    private static IEnumerable<string> DistinctiveTokens(string text)
    {
        // Keep tokens 4+ chars, lowercase, drop common stopwords. Crude but functions as a fingerprint.
        return Regex.Matches(text, @"\b[a-zA-Z]{4,}\b")
            .Select(m => m.Value.ToLowerInvariant())
            .Where(t => !CommonStopwords.Contains(t));
    }

    private static readonly HashSet<string> CommonStopwords = new(StringComparer.OrdinalIgnoreCase)
    {
        "this","that","with","from","have","were","been","they","them","their","what","when","then",
        "there","would","could","should","because","about","into","through","before","after","over",
        "under","than","each","other","some","much","many","just","like","said","says","made","make",
        "going","gone","took","take","gave","give","know","knew","think","came","come","want","wanted"
    };

    private static string StripHtmlAndMarkdown(string s) =>
        Regex.Replace(Regex.Replace(s, @"<[^>]+>", " "), @"[#*_`]+", "");

    private static string StripChapterHeading(string s)
    {
        // Drop first line if it looks like a heading or chapter title.
        var firstNl = s.IndexOf('\n');
        if (firstNl < 0) return s;
        var first = s[..firstNl].Trim();
        if (first.Length < 80 && (first.StartsWith('#') || first == first.ToUpperInvariant() || first.Length < 40))
            return s[(firstNl + 1)..].TrimStart();
        return s;
    }
}
