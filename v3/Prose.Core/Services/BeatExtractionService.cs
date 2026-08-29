using Microsoft.Extensions.Logging;
using Prose.Core.Interfaces;

namespace Prose.Core.Services;

/// <summary>
/// One consolidated post-write extraction call, replacing five separate per-beat Haiku calls
/// that were each asking the model to look at the same just-written beat and pull out a
/// different slice of structured fact:
///   - ReaderKnowledgeService.ExtractAsync        (reader-knowledge revelations)
///   - NarrativeSummaryService.SummarizeSceneAsync (rolling scene summary)
///   - OpenThreadsService.DetectAndRegisterAsync   (new setups/promises)
///   - OpenThreadsService.MarkResolvedAsync        (which open threads this beat closed)
///   - BookStateLedgerService.ExtractAndRecordAsync (arc-level plot-state transitions)
///
/// This was RFC 0009 §9.4's "item 1" — deliberately deferred in the 2026-08-13 cost-reduction
/// pass until the safer audit-side cuts (items 2-6) had been verified. Implemented 2026-08-13
/// once `--estimate-cost` showed generation itself (not the audit battery) was ~5x the cost of
/// everything else combined, with this five-call cluster as its single largest component.
///
/// Each of the four downstream services keeps its own standalone Extract/Summarize method
/// intact (2026-08-23: the only other caller, SceneGenerationService, was deleted as confirmed
/// dead code — this class is now the sole caller of all four, which is fine, it's the normal
/// case this consolidation was built for) — this class only replaces the FIVE LLM CALLS with
/// one, then fans the parsed response out to each service's existing Persist*-only method (pure
/// DB writes, no LLM).
/// </summary>
public class BeatExtractionService(
    ILlmService llm,
    ReaderKnowledgeService readerKnowledge,
    NarrativeSummaryService narrativeSummary,
    OpenThreadsService openThreads,
    BookStateLedgerService bookStateLedger,
    ILogger<BeatExtractionService> log,
    BeatPlaceService? beatPlace = null,
    MotifLedgerService? motifLedger = null)
{
    private const string ReaderFactsHeader   = "=== READER-FACTS ===";
    private const string SceneSummaryHeader  = "=== SCENE-SUMMARY ===";
    private const string NewThreadsHeader    = "=== NEW-THREADS ===";
    private const string ResolvedHeader      = "=== RESOLVED-THREADS ===";
    private const string PlotEventsHeader    = "=== PLOT-EVENTS ===";
    private const string SceneLocationHeader = "=== SCENE-LOCATION ===";
    private const string MotifsHeader        = "=== MOTIFS ===";

    private static readonly string System = $"""
        You are a story continuity editor. Read the beat of prose below ONCE and extract seven
        different kinds of structured fact from it. Output exactly seven sections, in this order,
        each starting with its own header line exactly as shown, and "NONE" under a section (or
        for SCENE-SUMMARY, an empty line) if that section has nothing to report.

        {ReaderFactsHeader}
        Up to 3 concrete facts the READER now knows that are narratively significant — character
        secrets revealed, plot mechanics exposed, relationship dynamics made explicit, world facts
        established for the first time. Exclude atmosphere/setting description. One per line,
        prefixed "FACT:".

        {SceneSummaryHeader}
        Compress this beat into exactly 3-4 sentences: what happened, to whom, what changed, what
        tension remains. Specific — names, consequences, emotional state. No editorializing.

        {NewThreadsHeader}
        Up to 8 NEW setups, promises, unresolved questions, wounds, or foreshadowing introduced in
        THIS beat that a reader will expect addressed later. Do not list things already resolved
        within this same excerpt. One per line, max 120 chars each.

        {ResolvedHeader}
        Given the OPEN THREADS list below (numbered), output ONLY the 1-based numbers of threads
        that are fully resolved (closed, paid off, definitively answered) by this beat. One number
        per line.

        {PlotEventsHeader}
        Given the CURRENT PLOT STATE below (already recorded — do not repeat), list ONLY NEW
        arc-level plot-state transitions in this beat — crises opening/escalating/resolving,
        dramatic questions posed/answered, objectives established/completed, threats
        emerging/neutralized, alliances forming/breaking, information newly revealed. Do not list
        physical actions or character emotions. Max 6 events, one per line, pipe-delimited:
          StateType|state_key|verb|One-sentence label (max 120 chars)|NewValue
        StateType: Crisis DramaticQuestion Objective Threat Alliance Information
        state_key: snake_case slug, e.g. crisis:behemoth_approach
        verb: open escalate climax resolve reopen defer answer establish achieve fail abandon contain neutralize reveal confirm contest shift
        NewValue: Open Escalated Climaxed Resolved Reopened Answered Deferred Active Achieved Failed Abandoned Contained Neutralized Strained Broken Restored Hidden Revealed Confirmed Contested

        {SceneLocationHeader}
        WHERE this beat's scene takes place — the concrete physical location as the prose
        establishes it (venue plus district/region when stated, e.g. "Doc Stash's clinic, The
        Shelf"). Max ~10 words, named as the prose names it, never invented. One line. "NONE" if
        genuinely indeterminate.

        {MotifsHeader}
        Up to 3 concrete, SPECIFIC recurring-image candidates in this beat — a physical object,
        gesture, or sensory image rendered distinctly enough to recur (e.g. "the cracked
        credstick", "rain on the skylight", "her habit of counting exits"). NOT themes, NOT
        emotions, NOT plot events. One per line, max 8 words each, lowercase except proper nouns.
        "NONE" if the beat has no distinct recurring-image candidate.
        """;

    /// <summary>
    /// Fire one consolidated Haiku call over the just-written beat and persist all five slices
    /// via each service's own Persist*-only method. Fire-and-forget from ProseWriterRouter;
    /// never blocks prose output. Non-fatal end-to-end — any failure is logged and swallowed so
    /// one bad response doesn't cost every downstream service its update for this beat; where
    /// the response parses but only some sections are present, sections that DID parse still get
    /// persisted (see ExtractAllAsync's per-section try/catch below).
    /// </summary>
    public async Task ExtractAllAsync(
        Guid nodeId, Guid beatId, int beatIndex, string prose, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(prose) || nodeId == Guid.Empty) return;

        List<Data.Entities.NodeOpenThread> openList;
        Dictionary<string, Data.Entities.BookPlotEvent> existingState;
        try
        {
            openList     = await openThreads.GetOpenThreadsAsync(nodeId, ct);
            existingState = await bookStateLedger.GetCurrentStateAsync(nodeId, ct);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "BeatExtractionService: context load failed for beat {BeatId}", beatId);
            return;
        }

        var threadListBlock = openList.Count == 0
            ? "None."
            : string.Join("\n", openList.Select((t, i) => $"{i + 1}. {t.Description}"));
        var stateBlock = existingState.Count == 0
            ? "None yet."
            : string.Join("\n", existingState.Values
                .OrderBy(e => e.StateType).ThenBy(e => e.StateKey)
                .Select(e => $"  {e.StateType}|{e.StateKey}|{e.NewValue}: {e.Label}"));

        var user = $"""
            OPEN THREADS (numbered, for RESOLVED-THREADS):
            {threadListBlock}

            CURRENT PLOT STATE (for PLOT-EVENTS — do not repeat these):
            {stateBlock}

            BEAT PROSE:
            {(prose.Length > 4000 ? prose[..4000] : prose)}
            """;

        string raw;
        try
        {
            raw = await llm.GenerateAsync(System, user, temperature: 0.15, maxTokens: 1100, model: LlmModels.Haiku, ct: ct);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "BeatExtractionService: consolidated extraction call failed for beat {BeatId}", beatId);
            return;
        }

        if (string.IsNullOrWhiteSpace(raw)) return;
        var sections = SplitSections(raw);

        if (sections.TryGetValue(ReaderFactsHeader, out var readerBlock))
        {
            var facts = readerBlock.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(l => l.StartsWith("FACT:", StringComparison.OrdinalIgnoreCase))
                .Select(l => l["FACT:".Length..].Trim())
                .Where(f => f.Length > 10)
                .Take(3)
                .ToList();
            if (facts.Count > 0)
            {
                try { await readerKnowledge.PersistFactsAsync(facts, nodeId, ct); }
                catch (Exception ex) { log.LogWarning(ex, "BeatExtractionService: reader-facts persist failed for beat {BeatId}", beatId); }
            }
        }

        if (sections.TryGetValue(SceneSummaryHeader, out var summaryBlock) && !string.IsNullOrWhiteSpace(summaryBlock))
        {
            try { await narrativeSummary.PersistSummaryAsync(summaryBlock.Trim(), nodeId, beatId == Guid.Empty ? null : beatId, ct); }
            catch (Exception ex) { log.LogWarning(ex, "BeatExtractionService: scene-summary persist failed for beat {BeatId}", beatId); }
        }

        if (sections.TryGetValue(NewThreadsHeader, out var newThreadsBlock) && beatId != Guid.Empty)
        {
            var lines = newThreadsBlock.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(l => l.Length > 5 && !l.Equals("NONE", StringComparison.OrdinalIgnoreCase))
                .Take(8)
                .ToList();
            if (lines.Count > 0)
            {
                try { await openThreads.PersistNewThreadsAsync(lines, nodeId, beatId, ct); }
                catch (Exception ex) { log.LogWarning(ex, "BeatExtractionService: new-threads persist failed for beat {BeatId}", beatId); }
            }
        }

        if (sections.TryGetValue(ResolvedHeader, out var resolvedBlock) && openList.Count > 0 && beatId != Guid.Empty)
        {
            var resolvedIds = OpenThreadsService.ParseResolvedNumbers(resolvedBlock, openList);
            if (resolvedIds.Count > 0)
            {
                try { await openThreads.PersistResolutionsAsync(resolvedIds, beatId, ct); }
                catch (Exception ex) { log.LogWarning(ex, "BeatExtractionService: resolved-threads persist failed for beat {BeatId}", beatId); }
            }
        }

        if (sections.TryGetValue(SceneLocationHeader, out var locationBlock) && beatPlace != null && beatId != Guid.Empty)
        {
            var place = locationBlock.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(place) && !place.Equals("NONE", StringComparison.OrdinalIgnoreCase))
            {
                try { await beatPlace.PersistAsync(beatId, place, ct); }
                catch (Exception ex) { log.LogWarning(ex, "BeatExtractionService: scene-location persist failed for beat {BeatId}", beatId); }
            }
        }

        if (sections.TryGetValue(MotifsHeader, out var motifBlock) && motifLedger != null && beatId != Guid.Empty)
        {
            var motifs = motifBlock.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(l => l.Length > 3 && !l.Equals("NONE", StringComparison.OrdinalIgnoreCase))
                .Take(3)
                .ToList();
            if (motifs.Count > 0)
            {
                try { await motifLedger.PersistCandidatesAsync(nodeId, beatId, motifs, ct); }
                catch (Exception ex) { log.LogWarning(ex, "BeatExtractionService: motif persist failed for beat {BeatId}", beatId); }
            }
        }

        if (sections.TryGetValue(PlotEventsHeader, out var plotBlock) && beatId != Guid.Empty)
        {
            var lines = plotBlock.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(l => l.Contains('|') && !l.Equals("NONE", StringComparison.OrdinalIgnoreCase))
                .Take(6)
                .ToList();
            if (lines.Count > 0)
            {
                try { await bookStateLedger.PersistPipeDelimitedEventsAsync(lines, nodeId, beatId, beatIndex, existingState, ct); }
                catch (Exception ex) { log.LogWarning(ex, "BeatExtractionService: plot-events persist failed for beat {BeatId}", beatId); }
            }
        }
    }

    /// <summary>Splits the model's response on the seven known header lines. Tolerant of a
    /// missing section (older/degraded response) — callers TryGetValue and skip what's absent
    /// rather than failing the whole extraction over one missing header.</summary>
    private static Dictionary<string, string> SplitSections(string raw)
    {
        var headers = new[] { ReaderFactsHeader, SceneSummaryHeader, NewThreadsHeader, ResolvedHeader, PlotEventsHeader, SceneLocationHeader, MotifsHeader };
        var result = new Dictionary<string, string>();
        var positions = headers
            .Select(h => (Header: h, Index: raw.IndexOf(h, StringComparison.Ordinal)))
            .Where(x => x.Index >= 0)
            .OrderBy(x => x.Index)
            .ToList();

        for (int i = 0; i < positions.Count; i++)
        {
            var start = positions[i].Index + positions[i].Header.Length;
            var end = i + 1 < positions.Count ? positions[i + 1].Index : raw.Length;
            result[positions[i].Header] = raw[start..end].Trim();
        }
        return result;
    }
}
