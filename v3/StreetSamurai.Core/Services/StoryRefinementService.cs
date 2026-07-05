using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MindAttic.Legion;
using MindAttic.Legion.Providers;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Models;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Produces structured refinement notes on a completed first-draft story.
///
/// Human-in-the-loop, on-demand: this service only proposes notes, it never
/// rewrites the draft. Each note is keyed to a beat and a specific quote,
/// with a rationale and a suggestion the author can accept, edit, or skip.
///
/// Notes fall into five kinds (see <see cref="RefinementKind"/>):
///   Impactful       — moments doing the most work; consider expanding
///   Cluttered       — too much at once; needs breathing room
///   Underdeveloped  — promise not paid off
///   ContextGap      — draft assumes canon facts the reader hasn't been told
///   PacingMismatch  — sentence rhythm does not match the beat's register
///
/// ContextGap is the killer feature: the analyzer is told about each referenced
/// character's canonical cyberware, stats, belongings and relationships, and
/// asked "what does the reader need to know to understand this scene that
/// isn't on the page?" — e.g. "Kyle's blade electrifies under repeated kinetic
/// impact" is canon but a reader won't know it unless the draft says so.
/// </summary>
public class StoryRefinementService
{
    private readonly LlmVotingProvider provider;
    private readonly VotingConfiguration voting;
    private readonly IDatabaseService db;
    private readonly IPathProvider paths;
    private readonly IDbContextFactory<StreetSamuraiDbContext> dbFactory;
    private readonly ILogger<StoryRefinementService> log;

    public StoryRefinementService(
        LlmVotingProvider provider,
        VotingConfiguration voting,
        IDatabaseService db,
        IPathProvider paths,
        IDbContextFactory<StreetSamuraiDbContext> dbFactory,
        ILogger<StoryRefinementService> log)
    {
        this.provider  = provider;
        this.voting    = voting;
        this.db        = db;
        this.paths     = paths;
        this.dbFactory = dbFactory;
        this.log       = log;
    }

    /// <summary>
    /// Idempotent column-add. Called from <c>--repair</c>'s schema-bootstrap.
    /// </summary>
    public async Task EnsureRefinementReportColumnAsync(CancellationToken ct = default)
    {
        await using var ctx = await dbFactory.CreateDbContextAsync(ct);
        const string ddl = """
            IF COL_LENGTH('dbo.Chapters', 'RefinementReportJson') IS NULL
                ALTER TABLE [dbo].[Chapters] ADD [RefinementReportJson] NVARCHAR(MAX) NULL;
            """;
        await ctx.Database.ExecuteSqlRawAsync(ddl, ct);
    }

    /// <summary>
    /// Analyze a completed story beat-by-beat and produce a refinement report.
    /// Saves the report to the story folder as refinement_report.json.
    /// </summary>
    public async Task<RefinementReport> AnalyzeAsync(AutonomousStory story, CancellationToken ct = default)
    {
        log.LogInformation("Refinement analysis starting: projectId={ProjectId}, beats={Beats}",
            story.ProjectId, story.Beats.Count);

        var report = new RefinementReport
        {
            ProjectId     = story.ProjectId,
            Title         = story.Title,
            AnalyzedAt    = DateTime.UtcNow,
            BeatsAnalyzed = 0,
        };

        if (story.Beats.Count == 0)
        {
            report.Error = "Story has no beats to analyze";
            Save(story.ProjectId, report);
            return report;
        }

        var judgeId = voting.JudgeProviderId;
        if (string.IsNullOrWhiteSpace(judgeId) || !voting.ActiveProviderIds.Contains(judgeId))
        {
            report.Error = $"Judge provider '{judgeId}' is not configured";
            Save(story.ProjectId, report);
            return report;
        }

        foreach (var beat in story.Beats)
        {
            if (ct.IsCancellationRequested) break;

            try
            {
                var notes = await AnalyzeBeatAsync(story, beat, judgeId, ct);
                report.Notes.AddRange(notes);
                report.BeatsAnalyzed++;
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "Refinement analysis failed for beat {Index}", beat.BeatIndex);
            }
        }

        Save(story.ProjectId, report);
        log.LogInformation("Refinement analysis complete: {Notes} notes across {Beats} beats",
            report.Notes.Count, report.BeatsAnalyzed);
        return report;
    }

    /// <summary>
    /// Load a saved refinement report for a story, if present. Reads from
    /// <c>Chapters.RefinementReportJson</c>; on first miss with a legacy disk
    /// file present, migrates the file content into the column and deletes
    /// the source.
    /// </summary>
    public RefinementReport? LoadReport(string projectId)
    {
        if (Guid.TryParse(projectId, out var chapterId)
            || Guid.TryParseExact(projectId, "N", out chapterId))
        {
            try
            {
                using var ctx = dbFactory.CreateDbContext();
                var row = ctx.Chapters.FirstOrDefault(c => c.Id == chapterId);
                if (row != null)
                {
                    if (string.IsNullOrEmpty(row.RefinementReportJson))
                    {
                        var fromDisk = TryReadLegacyDiskFile(projectId, deleteAfterRead: true);
                        if (fromDisk != null)
                        {
                            row.RefinementReportJson = JsonSerializer.Serialize(fromDisk, JsonDefaults.Indented);
                            row.ModifiedAt = DateTime.UtcNow;
                            ctx.SaveChanges();
                            return fromDisk;
                        }
                        return null;
                    }
                    return JsonSerializer.Deserialize<RefinementReport>(row.RefinementReportJson);
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Refinement: load failed for {ProjectId}", projectId);
                return null;
            }
        }
        return TryReadLegacyDiskFile(projectId);
    }

    private RefinementReport? TryReadLegacyDiskFile(string projectId, bool deleteAfterRead = false)
    {
        var path = StoryFolderHelper.FindFile(paths.ChaptersDir, projectId, "refinement_report.json");
        if (path == null) return null;
        try
        {
            var report = JsonSerializer.Deserialize<RefinementReport>(File.ReadAllText(path));
            if (deleteAfterRead && report != null)
            {
                try { File.Delete(path); }
                catch (Exception ex) { log.LogDebug(ex, "Refinement: legacy file delete failed for {Path}", path); }
            }
            return report;
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to load refinement report for {ProjectId}", projectId);
            return null;
        }
    }

    /// <summary>Persist a report after the author accepts/edits/skips notes.</summary>
    public void SaveReport(RefinementReport report) => Save(report.ProjectId, report);

    /// <summary>
    /// Rewrite a paragraph to incorporate a refinement note's suggestion.
    /// Returns the new paragraph text. Voice, tone, and approximate length are preserved —
    /// the LLM is instructed to perform the minimum edit, not a reinvention.
    /// </summary>
    public async Task<string> ApplyNoteAsync(
        string paragraph, RefinementNote note, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(paragraph))
            throw new ArgumentException("paragraph must be non-empty", nameof(paragraph));

        var judgeId = voting.JudgeProviderId;
        if (string.IsNullOrWhiteSpace(judgeId) || !voting.ActiveProviderIds.Contains(judgeId))
            throw new InvalidOperationException($"Judge provider '{judgeId}' is not configured");

        var system = """
            You are a developmental editor applying a targeted, minimum-viable edit to one paragraph
            of neo-noir fiction. Preserve the author's voice, sentence rhythm, and approximate length.
            Do not add plot information the suggestion does not call for. Do not summarize.
            Return ONLY the rewritten paragraph — no preamble, no quotes, no commentary.
            """;

        var canon = string.IsNullOrWhiteSpace(note.CanonFact)
            ? ""
            : $"\nCANON FACT TO GROUND (if the suggestion requires it):\n{note.CanonFact}\n";

        var user = $"""
            NOTE KIND: {note.Kind}
            WHY: {note.Rationale}
            SUGGESTION: {note.Suggestion}
            {canon}
            ORIGINAL PARAGRAPH:
            {paragraph}

            REWRITTEN PARAGRAPH:
            """;

        var result = await provider.CallAsync(
            providerId:   judgeId,
            systemPrompt: system,
            userMessage:  user,
            maxTokens:    1024,
            temperature:  0.4,
            ct:           ct);

        return CleanRewrite(result);
    }

    // LLMs sometimes wrap responses in quotes or code fences despite the instruction.
    static string CleanRewrite(string raw)
    {
        var s = raw.Trim();
        if (s.StartsWith("```"))
        {
            var nl = s.IndexOf('\n');
            if (nl > 0) s = s[(nl + 1)..];
            var fence = s.LastIndexOf("```", StringComparison.Ordinal);
            if (fence >= 0) s = s[..fence];
            s = s.Trim();
        }
        if (s.Length >= 2 && s[0] == '"' && s[^1] == '"') s = s[1..^1];
        return s.Trim();
    }

    // ── Private ────────────────────────────────────────────────────────

    private const string SystemPromptSuffix = """
         Your job is to flag
        specific moments the author should reconsider. You do not rewrite. You
        propose. The author decides.

        Return ONLY a JSON object in this exact shape:

        {
          "impactful":      [ { "quote": "", "rationale": "", "suggestion": "" } ],
          "cluttered":      [ { "quote": "", "rationale": "", "suggestion": "" } ],
          "underdeveloped": [ { "quote": "", "rationale": "", "suggestion": "" } ],
          "context_gaps":   [ { "quote": "", "rationale": "", "suggestion": "", "canon_fact": "" } ],
          "pacing_mismatch":[ { "quote": "", "rationale": "", "suggestion": "" } ]
        }

        Rules:
        - "quote" must be an exact substring from the beat text (5-25 words).
        - Each list may be empty. Do not fabricate notes to fill them.
        - Keep "rationale" and "suggestion" to one sentence each.
        - For context_gaps, only flag canon facts supplied in CHARACTER CANON that
          a first-time reader would need to fully understand the scene but which
          the draft has not established. "canon_fact" must quote or paraphrase
          the specific canon fact. One grounding sentence suggestion max —
          never heavy-handed exposition.
        - Impactful: the single moment in this beat doing the most emotional or
          thematic work. Usually 0 or 1 per beat.
        - Cluttered: three or more distinct actions/images jammed in one paragraph.
        - Underdeveloped: a tension or promise raised but not landed on the page.
        - PacingMismatch: sentence rhythm is wrong for the register (long winding
          clauses during action; choppy fragments during contemplation).

        Be sparing. Five notes across all kinds is plenty. Silence is fine.
        """;

    async Task<List<RefinementNote>> AnalyzeBeatAsync(
        AutonomousStory story, GeneratedStoryBeat beat, string judgeId, CancellationToken ct)
    {
        var systemPrompt = BuildSystemPrompt();
        var userMessage  = BuildBeatPrompt(story, beat);

        var raw = await provider.CallAsync(
            providerId:   judgeId,
            systemPrompt: systemPrompt,
            userMessage:  userMessage,
            maxTokens:    2048,
            temperature:  0.3,
            ct:           ct);

        return ParseNotes(beat.BeatIndex, raw);
    }

    static string BuildSystemPrompt()
    {
        var identity = UniverseScope.Current?.UniverseGroundingOr(
            "You are a developmental editor reviewing one beat of a neo-noir short story set in GLMZ (also called The Glooms).")
            ?? "You are a developmental editor reviewing one beat of a neo-noir short story set in GLMZ (also called The Glooms).";
        return identity + SystemPromptSuffix;
    }

    string BuildBeatPrompt(AutonomousStory story, GeneratedStoryBeat beat)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"STORY TITLE: {story.Title}");
        sb.AppendLine($"PROTAGONIST: {story.Protagonist}");
        sb.AppendLine($"BEAT {beat.BeatIndex + 1} — Act {beat.Act}: {beat.Title}");
        sb.AppendLine($"STRUCTURE ROLE: {beat.StructureRole}");
        sb.AppendLine();

        // Canon projection — only for characters actually referenced in this beat.
        var mentioned = story.Characters
            .Where(name => beat.Text.Contains(name, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (mentioned.Count > 0)
        {
            sb.AppendLine("CHARACTER CANON — facts the reader may or may not know:");
            foreach (var name in mentioned)
            {
                var canon = db.GetCharacterContext(name);
                if (!string.IsNullOrWhiteSpace(canon))
                {
                    sb.AppendLine(canon);
                    sb.AppendLine();
                }
            }
        }

        sb.AppendLine("BEAT TEXT:");
        sb.AppendLine(beat.Text);
        return sb.ToString();
    }

    List<RefinementNote> ParseNotes(int beatIndex, string raw)
    {
        var notes = new List<RefinementNote>();
        var json = ExtractJsonObject(raw);
        if (json == null)
        {
            log.LogDebug("No JSON object in LLM response for beat {Index}", beatIndex);
            return notes;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            AppendKind(notes, beatIndex, root, "impactful",       RefinementKind.Impactful);
            AppendKind(notes, beatIndex, root, "cluttered",       RefinementKind.Cluttered);
            AppendKind(notes, beatIndex, root, "underdeveloped",  RefinementKind.Underdeveloped);
            AppendKind(notes, beatIndex, root, "context_gaps",    RefinementKind.ContextGap);
            AppendKind(notes, beatIndex, root, "pacing_mismatch", RefinementKind.PacingMismatch);
        }
        catch (JsonException ex)
        {
            log.LogDebug(ex, "Malformed JSON from LLM for beat {Index}", beatIndex);
        }

        return notes;
    }

    static void AppendKind(List<RefinementNote> notes, int beatIndex, JsonElement root, string prop, RefinementKind kind)
    {
        if (!root.TryGetProperty(prop, out var arr) || arr.ValueKind != JsonValueKind.Array) return;
        foreach (var item in arr.EnumerateArray())
        {
            var note = new RefinementNote
            {
                BeatIndex  = beatIndex,
                Kind       = kind,
                Quote      = Str(item, "quote"),
                Rationale  = Str(item, "rationale"),
                Suggestion = Str(item, "suggestion"),
                CanonFact  = kind == RefinementKind.ContextGap ? NullableStr(item, "canon_fact") : null,
            };
            if (note.Quote.Length > 0) notes.Add(note);
        }
    }

    static string Str(JsonElement el, string prop) =>
        el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";

    static string? NullableStr(JsonElement el, string prop)
    {
        var s = Str(el, prop);
        return s.Length == 0 ? null : s;
    }

    // LLMs occasionally wrap JSON in markdown code fences or chatter. Extract the outermost object.
    static string? ExtractJsonObject(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var start = raw.IndexOf('{');
        var end   = raw.LastIndexOf('}');
        return start >= 0 && end > start ? raw[start..(end + 1)] : null;
    }

    void Save(string projectId, RefinementReport report)
    {
        if (!Guid.TryParse(projectId, out var chapterId)
            && !Guid.TryParseExact(projectId, "N", out chapterId))
        {
            log.LogWarning("Refinement: project id is not a Guid, skipping save: {ProjectId}", projectId);
            return;
        }

        try
        {
            using var ctx = dbFactory.CreateDbContext();
            var row = ctx.Chapters.FirstOrDefault(c => c.Id == chapterId);
            if (row == null)
            {
                log.LogDebug("Refinement: no Chapters row for {ProjectId}; report not persisted", projectId);
                return;
            }
            row.RefinementReportJson = JsonSerializer.Serialize(report, JsonDefaults.Indented);
            row.ModifiedAt = DateTime.UtcNow;
            ctx.SaveChanges();
            log.LogDebug("Refinement report saved to Chapters.RefinementReportJson for {ProjectId}", projectId);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to save refinement report for {ProjectId}", projectId);
        }
    }
}
