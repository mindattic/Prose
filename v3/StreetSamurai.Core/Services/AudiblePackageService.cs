using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Produces an Audible hand-off package for a strand: a narration-clean manuscript
/// (.audible.txt), a pronunciation guide (.pronunciation.md), and a human-readable
/// hand-off README. All three land in {publishDir}/{SanitizedTitle}/Audible/ so they
/// sit beside existing KDP exports without colliding.
///
/// Audible's AI narration is a closed publisher/ACX program — no API is called here.
/// The author uploads the .audible.txt directly to the ACX/Audible submission portal.
///
/// Narration cleaning is delegated to <see cref="NarrationText.Clean"/> — the single
/// canonical implementation shared with the audiobook pipeline.
/// </summary>
public class AudiblePackageService
{
    // Collapse excess blank lines after Clean() joins beats (NarrationText.Clean
    // handles the per-beat pass; this cleans the joined manuscript).
    private static readonly Regex excessBlankLines =
        new(@"\n{3,}", RegexOptions.Compiled);

    private readonly IDbContextFactory<StreetSamuraiDbContext> dbFactory;
    private readonly StrandWorkbenchService workbench;
    private readonly SettingsService settings;
    private readonly ILogger<AudiblePackageService> log;
    private readonly ILlmService? llm;

    public AudiblePackageService(
        IDbContextFactory<StreetSamuraiDbContext> dbFactory,
        StrandWorkbenchService workbench,
        SettingsService settings,
        ILogger<AudiblePackageService> log,
        ILlmService? llm = null)
    {
        this.dbFactory = dbFactory;
        this.workbench = workbench;
        this.settings  = settings;
        this.log       = log;
        this.llm       = llm;
    }

    // ── public surface ─────────────────────────────────────────────────────────

    public async Task<AudiblePackageResult> BuildAsync(
        Guid strandId,
        bool withPhonetics = true,
        CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var strand = await db.Strands.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == strandId, ct)
            ?? throw new InvalidOperationException($"Strand {strandId} not found.");

        var ordered = await workbench.GetOrderedBeatsAsync(strandId, ct);
        var beatIds = ordered.Select(ob => ob.Beat.Id).ToList();

        // Collect distinct entity names from beat mentions (capped at 200).
        var entityNames = await db.BeatEntityMentions
            .AsNoTracking()
            .Where(m => beatIds.Contains(m.BeatId))
            .Select(m => new { m.EntityName, m.EntityType })
            .Distinct()
            .OrderBy(m => m.EntityName)
            .Take(200)
            .ToListAsync(ct);

        var distinctTerms = entityNames
            .Select(m => m.EntityName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n)
            .ToList();

        // Resolve output directory.
        var publishRoot = ResolveExportDir();
        var safeTitle   = SanitizeTitle(strand.Title);
        var audibleDir  = Path.Combine(publishRoot, safeTitle, "Audible");
        Directory.CreateDirectory(audibleDir);

        // ── (a) narration manuscript ───────────────────────────────────────────
        var (manuscriptText, wordCount) = BuildManuscript(strand.Title, strand.Slug, ordered);
        var manuscriptPath = Path.Combine(audibleDir, $"{strand.Slug}.audible.txt");
        await File.WriteAllTextAsync(manuscriptPath, manuscriptText, new UTF8Encoding(false), ct);

        // ── (b) pronunciation guide ────────────────────────────────────────────
        Dictionary<string, (string SayAs, string Note)> phonetics = new();
        bool phoneticsApplied = false;

        if (withPhonetics && llm != null)
        {
            phonetics = await TryGetPhoneticsAsync(distinctTerms, ct);
            phoneticsApplied = phonetics.Count > 0;
        }

        var pronunciationText = BuildPronunciationGuide(distinctTerms, phonetics);
        var pronunciationPath = Path.Combine(audibleDir, $"{strand.Slug}.pronunciation.md");
        await File.WriteAllTextAsync(pronunciationPath, pronunciationText, new UTF8Encoding(false), ct);

        // ── (c) README ────────────────────────────────────────────────────────
        var readmePath = Path.Combine(audibleDir, "AUDIBLE_README.md");
        var readmeText = BuildReadme(strand.Title, strand.Slug);
        await File.WriteAllTextAsync(readmePath, readmeText, new UTF8Encoding(false), ct);

        log.LogInformation(
            "Audible package for strand {Slug}: manuscript={MS}, lexicon={LEX}, readme={RME}",
            strand.Slug, manuscriptPath, pronunciationPath, readmePath);

        return new AudiblePackageResult(
            ManuscriptPath:    manuscriptPath,
            LexiconPath:       pronunciationPath,
            ReadmePath:        readmePath,
            WordCount:         wordCount,
            TermCount:         distinctTerms.Count,
            PhoneticsApplied:  phoneticsApplied);
    }

    // ── manuscript builder ─────────────────────────────────────────────────────

    private static (string Text, int WordCount) BuildManuscript(
        string title,
        string slug,
        IReadOnlyList<StrandWorkbenchService.OrderedBeat> ordered)
    {
        var sb = new StringBuilder();
        sb.AppendLine(title);
        sb.AppendLine();

        int chapterNo    = 0;
        bool hadContent  = false;

        foreach (var ob in ordered)
        {
            var beat = ob.Beat;
            if (beat.IsChapterStart)
            {
                if (hadContent) sb.AppendLine();   // blank line before new chapter
                chapterNo++;
                var heading = !string.IsNullOrWhiteSpace(beat.BeatTitle)
                    ? beat.BeatTitle!.Trim()
                    : $"Chapter {chapterNo}";
                sb.AppendLine(heading);
                sb.AppendLine();
            }

            var raw = (beat.Text ?? "").Trim();
            if (raw.Length == 0) continue;

            var clean = CleanForNarration(raw);
            if (string.IsNullOrWhiteSpace(clean)) continue;

            sb.AppendLine(clean);
            sb.AppendLine();
            hadContent = true;
        }

        var finalText = excessBlankLines
            .Replace(sb.ToString().TrimEnd(), "\n\n")
            .TrimEnd() + "\n";

        // Word count: split on whitespace
        var wordCount = finalText
            .Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
            .Length;

        return (finalText, wordCount);
    }

    // Narration cleaning is now in NarrationText.Clean (single source of truth).
    // The manuscript uses Clean only (no speech-pronunciation substitution) so the
    // written output keeps correct spelling while the TTS path adds ApplySpeechPronunciation.
    private static string CleanForNarration(string text) => NarrationText.Clean(text);

    // ── pronunciation guide builder ────────────────────────────────────────────

    private static string BuildPronunciationGuide(
        List<string> terms,
        Dictionary<string, (string SayAs, string Note)> phonetics)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Pronunciation Guide");
        sb.AppendLine();
        sb.AppendLine("Submit this file alongside your manuscript to guide Audible's AI narrator");
        sb.AppendLine("on invented names, world-specific terms, and currency.");
        sb.AppendLine();
        sb.AppendLine("| Term | Say it as | Type | Note |");
        sb.AppendLine("|------|-----------|------|------|");

        // ── Canon constants always first ──────────────────────────────────────
        sb.AppendLine("| Φ | QUANTA | currency | The QUANTA currency symbol; never read as \"phi\" |");
        sb.AppendLine("| CorpoNation | corpo nation | world term | Two-word pronunciation |");
        sb.AppendLine("| GLMZ | G-L-M-Z | acronym | Greater Lake Michigan Zone |");

        // ── Entity names from the strand ─────────────────────────────────────
        foreach (var term in terms)
        {
            phonetics.TryGetValue(term, out var ph);
            var sayAs = EscapeTable(ph.SayAs ?? "");
            var note  = EscapeTable(ph.Note  ?? "");
            sb.AppendLine($"| {EscapeTable(term)} | {sayAs} | entity | {note} |");
        }

        return sb.ToString().TrimEnd() + "\n";
    }

    // ── README builder ─────────────────────────────────────────────────────────

    private static string BuildReadme(string title, string slug)
    {
        return $"""
            # Audible AI Narration — Hand-off Package

            ## What these files are

            This folder contains three files produced by the StreetSamurai engine for the
            strand **{title}**:

            | File | Purpose |
            |------|---------|
            | `{slug}.audible.txt` | Narration-clean manuscript — the file you submit to Audible |
            | `{slug}.pronunciation.md` | Pronunciation guide — share with Audible to correct name readings |
            | `AUDIBLE_README.md` | This file |

            ## Audible AI Narration

            Audible's AI narration is a **closed publisher program** — Audible generates the
            audio on their side; there is no public API.  To enroll your title, visit:

            https://www.audible.com/about/newsroom/audible-expands-catalog-with-ai-narration-and-translation-for-publishers

            ## How to submit

            1. **Enroll your title** via the ACX / Audible publisher portal.
            2. **Submit `{slug}.audible.txt`** as your manuscript.  The file is plain UTF-8
               text with clean chapter headings and no markdown artifacts.
            3. **Share `{slug}.pronunciation.md`** with Audible so their system reads invented
               names and world-specific terms correctly.  In particular, ensure `Φ` is read
               as **"QUANTA"** (the in-world currency), not as the Greek letter "phi".
            4. **Choose a voice** from Audible's 100+ narrator voices on their side.

            > Note: The `.audible.txt` manuscript transforms `Φ` to `QUANTA` and strips
            > markdown formatting for narration clarity only.  Your canonical prose in the
            > StreetSamurai database is never modified.

            """;
    }

    // ── phonetics via LLM ─────────────────────────────────────────────────────

    private async Task<Dictionary<string, (string SayAs, string Note)>> TryGetPhoneticsAsync(
        List<string> terms,
        CancellationToken ct)
    {
        if (terms.Count == 0 || llm == null)
            return new Dictionary<string, (string SayAs, string Note)>();

        try
        {
            var termList = string.Join(", ", terms.Take(150));

            var system = """
                You are a pronunciation specialist for audiobook production.
                Given a list of invented or potentially ambiguous proper nouns from a
                cyberpunk fiction work, return a JSON array where each entry has:
                  "term"   — the original term (copy it exactly)
                  "say_as" — a plain-English respelling for an AI narrator
                             (e.g. "KY-luh", "koh-POR-nay-shun", "glee-mahz")
                  "note"   — brief note if helpful (e.g. "stress first syllable")

                Rules:
                - Only include terms that are genuinely ambiguous or invented.
                - Skip common English words and standard first names.
                - Do NOT modify the "term" value.
                - Output JSON only, no prose wrapper.
                """;

            var user = $"Terms: {termList}";

            var raw = await llm.GenerateAsync(system, user, temperature: 0.2, maxTokens: 1200, ct: ct);

            // Parse JSON array
            var jsonStart = raw.IndexOf('[');
            var jsonEnd   = raw.LastIndexOf(']');
            if (jsonStart < 0 || jsonEnd < jsonStart)
                return new Dictionary<string, (string SayAs, string Note)>();

            var jsonSlice = raw[jsonStart..(jsonEnd + 1)];
            var entries   = JsonSerializer.Deserialize<List<PhoneticsEntry>>(jsonSlice,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (entries == null)
                return new Dictionary<string, (string SayAs, string Note)>();

            return entries
                .Where(e => !string.IsNullOrWhiteSpace(e.Term))
                .ToDictionary(
                    e => e.Term!,
                    e => (SayAs: e.SayAs ?? "", Note: e.Note ?? ""),
                    StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            // Graceful degradation: leave Say it as blank for the author to fill.
            log.LogWarning("Phonetics LLM pass failed ({Msg}); pronunciation table will have empty Say-it-as column.", ex.Message);
            return new Dictionary<string, (string SayAs, string Note)>();
        }
    }

    // ── helpers ────────────────────────────────────────────────────────────────

    private string ResolveExportDir()
    {
        var dir = (settings.PublishExportDirectory ?? string.Empty).Trim().Trim('"', '\'').Trim();
        if (string.IsNullOrWhiteSpace(dir))
            dir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        return dir;
    }

    private static string SanitizeTitle(string title)
    {
        var invalid = Path.GetInvalidFileNameChars().ToHashSet();
        invalid.Add('\''); invalid.Add('’');
        var kept = new string((title ?? "").Where(c => !invalid.Contains(c)).ToArray()).Trim();
        kept = Regex.Replace(kept, @"\s+", " ").Trim();
        return string.IsNullOrWhiteSpace(kept) ? "untitled" : kept;
    }

    private static string EscapeTable(string s) =>
        (s ?? "").Replace("|", "\\|").Replace("\n", " ").Replace("\r", "");

    // ── nested types ───────────────────────────────────────────────────────────

    private sealed class PhoneticsEntry
    {
        public string? Term   { get; set; }
        public string? SayAs  { get; set; }
        public string? Note   { get; set; }
    }
}

/// <summary>Result returned by <see cref="AudiblePackageService.BuildAsync"/>.</summary>
public sealed record AudiblePackageResult(
    string ManuscriptPath,
    string LexiconPath,
    string ReadmePath,
    int    WordCount,
    int    TermCount,
    bool   PhoneticsApplied);
