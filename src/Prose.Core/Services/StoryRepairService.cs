using Microsoft.Extensions.Logging;
using Prose.Core.Interfaces;
using Prose.Core.Models;
using Prose.Core.Models.Canon;

namespace Prose.Core.Services;

/// <summary>
/// One-pass repair across every existing chapter that brings character entity
/// records into alignment with what the prose actually says. Two phases:
///
/// 1. Timeline (cheap, deterministic): for every chapter a character appears in,
///    ensure a <see cref="TimelineEvent"/> exists keyed by the chapter id. Pulls
///    appearance summaries from chapter synopsis or first beat.
///
/// 2. Continuity (expensive, LLM-driven): runs <see cref="ContinuityExtractionService"/>
///    against every chapter so the SQLite claim store reflects everything that's
///    been written. Subsequent dossier reads see those facts.
///
/// Idempotent — re-running after new chapters land only touches the deltas.
/// </summary>
public class StoryRepairService
{
    private readonly IChapterRepository chapters;
    private readonly CharacterRepository characters;
    private readonly ContinuityExtractionService extraction;
    private readonly BeatFactExtractionService beatFacts;
    private readonly ILogger<StoryRepairService> log;

    public StoryRepairService(
        IChapterRepository chapters,
        CharacterRepository characters,
        ContinuityExtractionService extraction,
        BeatFactExtractionService beatFacts,
        ILogger<StoryRepairService> log)
    {
        this.chapters = chapters;
        this.characters = characters;
        this.extraction = extraction;
        this.beatFacts = beatFacts;
        this.log = log;
    }

    /// <summary>
    /// Cheap pass — no LLM. Walks all chapters, finds appearances by name/alias
    /// substring match, ensures every (character, chapter) pair has a TimelineEvent
    /// entry. Returns the number of timeline rows added.
    /// </summary>
    public RepairTimelineResult RepairTimelines(CancellationToken ct = default)
    {
        var result = new RepairTimelineResult();
        var allCharacters = characters.GetAll();
        var allChapters = chapters.ListChapters()
            .OrderBy(c => c.Number ?? int.MaxValue)
            .ThenBy(c => c.Created)
            .ToList();

        result.ChaptersScanned = allChapters.Count;
        result.CharactersScanned = allCharacters.Count;

        foreach (var character in allCharacters)
        {
            if (ct.IsCancellationRequested) break;
            if (string.IsNullOrWhiteSpace(character.Name)) continue;

            var nameTokens = BuildSearchTokens(character);
            bool dirty = false;

            foreach (var chapter in allChapters)
            {
                if (!ChapterMentions(chapter, nameTokens)) continue;
                if (HasTimelineEntry(character, chapter.Id)) continue;

                character.Timeline.Add(BuildTimelineEntry(chapter));
                dirty = true;
                result.TimelineEntriesAdded++;
            }

            if (dirty)
            {
                try
                {
                    character.Timeline = character.Timeline
                        .OrderBy(t => ParseChapterNumber(t.Date) ?? int.MaxValue)
                        .ThenBy(t => t.Date, StringComparer.Ordinal)
                        .ToList();
                    characters.Save(character);
                    result.CharactersUpdated++;
                }
                catch (Exception ex)
                {
                    log.LogWarning(ex, "Failed to save character '{Name}' during timeline repair", character.Name);
                    result.Errors.Add($"{character.Name}: {ex.Message}");
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Expensive pass — LLM-driven. Runs ContinuityExtractionService against every
    /// chapter so canonical facts in the SQLite store reflect the prose. Filters
    /// out chapters whose extraction run already completed (cheap idempotence).
    /// </summary>
    public async Task<RepairContinuityResult> RepairContinuityAsync(
        bool forceReExtract = false,
        CancellationToken ct = default)
    {
        var result = new RepairContinuityResult();
        var allChapters = chapters.ListChapters()
            .Where(c => !string.IsNullOrEmpty(c.Id))
            .OrderBy(c => c.Number ?? int.MaxValue)
            .ToList();

        result.ChaptersScanned = allChapters.Count;

        foreach (var chapter in allChapters)
        {
            if (ct.IsCancellationRequested) break;
            try
            {
                var run = await extraction.ExtractFromChapterAsync(chapter.Id, ct: ct);
                result.ChaptersExtracted++;
                result.NewClaims        += run.NewClaims;
                result.ConfirmedClaims  += run.ConfirmedClaims;
                result.ContradictedClaims += run.ContradictedClaims;
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "Continuity extraction failed for chapter {Id}", chapter.Id);
                result.Errors.Add($"{chapter.Title} ({chapter.Id}): {ex.Message}");
            }
        }

        return result;
    }

    /// <summary>
    /// Walk every chapter beat. For each character that appears in a beat, ask the
    /// LLM to extract Knowledge claims (what they learned) and Conditions (addictions,
    /// allergies, prescriptions, chronic, mental, injury). Merge into the character
    /// record, deduping on (topic) and (kind+name). Idempotent — re-runs only add new.
    /// </summary>
    public async Task<RepairBeatFactsResult> RepairBeatFactsAsync(CancellationToken ct = default)
    {
        var result = new RepairBeatFactsResult();
        var allCharacters = characters.GetAll();
        var allChapters = chapters.ListChapters()
            .OrderBy(c => c.Number ?? int.MaxValue)
            .ThenBy(c => c.Created)
            .ToList();

        result.ChaptersScanned = allChapters.Count;

        foreach (var chapter in allCharacters.Count == 0 ? new List<Chapter>() : allChapters)
        {
            if (ct.IsCancellationRequested) break;

            foreach (var beat in chapter.Beats.OrderBy(b => b.Index))
            {
                if (ct.IsCancellationRequested) break;

                var presentCharacters = allCharacters
                    .Where(c => CharacterInBeat(c, beat))
                    .ToList();
                if (presentCharacters.Count == 0) continue;

                BeatFactExtractionResult facts;
                try { facts = await beatFacts.ExtractAsync(chapter, beat, presentCharacters.Select(c => c.Name).ToList(), ct); }
                catch (Exception ex) { log.LogWarning(ex, "Beat fact extract failed for Ch{N} §{B}", chapter.Number, beat.Index); result.Errors.Add($"Ch{chapter.Number} §{beat.Index}: {ex.Message}"); continue; }

                result.BeatsScanned++;

                foreach (var (name, k) in facts.Knowledge)
                {
                    var c = MatchCharacter(presentCharacters, name);
                    if (c == null) continue;
                    if (c.Knowledge.Any(existing => string.Equals(existing.Topic, k.Topic, StringComparison.OrdinalIgnoreCase)
                                                  && (existing.LearnedChapter ?? -1) == (k.LearnedChapter ?? -1))) continue;
                    c.Knowledge.Add(k);
                    result.KnowledgeAdded++;
                    result.TouchedCharacters.Add(c.Name);
                }

                foreach (var (name, cnd) in facts.Conditions)
                {
                    var c = MatchCharacter(presentCharacters, name);
                    if (c == null) continue;
                    if (c.Conditions.Any(existing =>
                            string.Equals(existing.Kind, cnd.Kind, StringComparison.OrdinalIgnoreCase)
                         && string.Equals(existing.Name, cnd.Name, StringComparison.OrdinalIgnoreCase))) continue;
                    c.Conditions.Add(cnd);
                    result.ConditionsAdded++;
                    result.TouchedCharacters.Add(c.Name);
                }
            }
        }

        // Persist every character we touched — use the in-memory (mutated) copy from
        // allCharacters, not a fresh fetch which would discard the accumulated mutations.
        var byName = allCharacters.ToDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);
        foreach (var name in result.TouchedCharacters)
        {
            try
            {
                if (byName.TryGetValue(name, out var c)) characters.Save(c);
            }
            catch (Exception ex) { log.LogWarning(ex, "Failed to save character '{Name}' after beat-fact merge", name); result.Errors.Add($"{name}: {ex.Message}"); }
        }

        return result;
    }

    private static bool CharacterInBeat(CharacterData c, ChapterBeat beat)
    {
        if (string.IsNullOrWhiteSpace(c.Name)) return false;
        var hay = $"{beat.Title} {beat.Synopsis} {beat.Text}";
        if (string.IsNullOrWhiteSpace(hay)) return false;
        if (hay.IndexOf(c.Name, StringComparison.OrdinalIgnoreCase) >= 0) return true;
        foreach (var alias in c.Aliases)
        {
            if (string.IsNullOrWhiteSpace(alias) || alias.Length < 3) continue;
            if (hay.IndexOf(alias, StringComparison.OrdinalIgnoreCase) >= 0) return true;
        }
        return false;
    }

    private static CharacterData? MatchCharacter(List<CharacterData> candidates, string name)
    {
        var direct = candidates.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
        if (direct != null) return direct;
        return candidates.FirstOrDefault(c => c.Aliases.Any(a => string.Equals(a, name, StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>
    /// Run all phases sequentially. Caller can inspect partial results when
    /// any LLM phase fails — the cheap timeline pass always completes first.
    /// </summary>
    public async Task<FullRepairResult> RepairAllAsync(
        bool forceReExtract = false,
        bool withBeatFacts = false,
        CancellationToken ct = default)
    {
        var timeline = RepairTimelines(ct);
        var continuity = await RepairContinuityAsync(forceReExtract, ct);
        var beats = withBeatFacts ? await RepairBeatFactsAsync(ct) : new RepairBeatFactsResult();
        return new FullRepairResult(timeline, continuity, beats);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static List<string> BuildSearchTokens(CharacterData c)
    {
        var tokens = new List<string> { c.Name };
        tokens.AddRange(c.Aliases.Where(a => !string.IsNullOrWhiteSpace(a)));
        // Cheap hygiene: drop anything shorter than 3 chars (false positives like "Hua" are OK,
        // but "Vô" or "Hu" would noisily match unrelated words).
        return tokens
            .Select(t => t.Trim())
            .Where(t => t.Length >= 3)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool ChapterMentions(Chapter chapter, List<string> tokens)
    {
        if (tokens.Count == 0) return false;
        // Combine title + synopsis + every beat's text/synopsis. Cheap allocation
        // up front beats re-walking the beat list per token.
        var hay = new System.Text.StringBuilder(chapter.Title).Append(' ').Append(chapter.Synopsis);
        foreach (var beat in chapter.Beats)
            hay.Append(' ').Append(beat.Title).Append(' ').Append(beat.Synopsis).Append(' ').Append(beat.Text);
        var s = hay.ToString();
        foreach (var t in tokens)
            if (s.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0) return true;
        return false;
    }

    private static bool HasTimelineEntry(CharacterData c, string chapterId)
        => c.Timeline.Any(t => string.Equals(t.StoryId, chapterId, StringComparison.OrdinalIgnoreCase));

    private static TimelineEvent BuildTimelineEntry(Chapter chapter)
    {
        var summary = !string.IsNullOrWhiteSpace(chapter.Synopsis)
            ? chapter.Synopsis
            : chapter.Beats.FirstOrDefault()?.Synopsis ?? chapter.Title;

        return new TimelineEvent
        {
            Date    = chapter.Number.HasValue ? $"Ch{chapter.Number}" : chapter.Title,
            StoryId = chapter.Id,
            Event   = Truncate(summary, 320),
        };
    }

    private static int? ParseChapterNumber(string date)
    {
        if (string.IsNullOrEmpty(date)) return null;
        if (date.StartsWith("Ch", StringComparison.OrdinalIgnoreCase)
            && int.TryParse(date[2..].Trim(), out var n)) return n;
        return null;
    }

    private static string Truncate(string s, int n) => s.Length <= n ? s : s[..(n - 1)] + "…";
}

public sealed class RepairTimelineResult
{
    public int ChaptersScanned { get; set; }
    public int CharactersScanned { get; set; }
    public int TimelineEntriesAdded { get; set; }
    public int CharactersUpdated { get; set; }
    public List<string> Errors { get; set; } = new();
}

public sealed class RepairContinuityResult
{
    public int ChaptersScanned { get; set; }
    public int ChaptersExtracted { get; set; }
    public int NewClaims { get; set; }
    public int ConfirmedClaims { get; set; }
    public int ContradictedClaims { get; set; }
    public List<string> Errors { get; set; } = new();
}

public sealed class RepairBeatFactsResult
{
    public int ChaptersScanned { get; set; }
    public int BeatsScanned { get; set; }
    public int KnowledgeAdded { get; set; }
    public int ConditionsAdded { get; set; }
    public HashSet<string> TouchedCharacters { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<string> Errors { get; } = new();
}

public sealed record FullRepairResult(
    RepairTimelineResult Timeline,
    RepairContinuityResult Continuity,
    RepairBeatFactsResult BeatFacts);
