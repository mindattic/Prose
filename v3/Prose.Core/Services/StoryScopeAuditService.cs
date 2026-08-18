using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Interfaces;
using Prose.Core.Services.Audit;

namespace Prose.Core.Services;

// ─────────────────────────────────────────────────────────────────────────────
// StoryScopeAuditService
//
// Verifies a story against the measurable structural tells of AI fiction
// (StoryScope, UMD/Google DeepMind 2025 — arXiv 2604.03136: 61,608 stories;
// narrative-structure classifiers detect AI fiction at 93.2% with zero style
// signals; prose editing barely moves it). The tells are decisions, not
// sentences — so this audit checks decisions:
//
//   Deterministic layer (zero LLM cost): blueprint-vs-execution drift, beat-mode
//   run-length, emotional-depth plateaus, social-network breadth, deviation
//   surfacing (quiet ending / clear moral polarity — logged, not failed).
//
//   LLM-graded layer (parallel, one call per check): a progressive per-beat
//   reading (stakes / event type / revelation mode) that authoritatively
//   detects flat escalation + event monoculture + information-dynamics
//   flatline, plus holistic checks: narrator moral gloss, embodied-vs-labeled
//   emotion ratio, character-introduction method, dialogue-as-philosophy,
//   resolution mode as written, intertextual anchor presence, TTCW originality
//   checks, plot-function characters, subtext, single-track causality, LAMP
//   line mechanics, and a consensus-cliché scan against the running blocklist.
//
// Findings use the canonical logic-sweep triage (BLOCKER / MODERATE / MINOR —
// docs/LOGIC.md) plus DEVIATION for surfaced-but-legal blueprint escape
// hatches. BLOCKER/MODERATE findings are written to the Findings table with a
// "STORYSCOPE " summary prefix; ProseWriterRouter folds them into future beat
// prompts, so the audit corrects subsequent writing instead of just reporting.
// ─────────────────────────────────────────────────────────────────────────────

public class StoryScopeAuditService(
    ILlmService llm,
    StructuralBlueprintService blueprints,
    NodeWorkbenchService workbench,
    FindingsService findings,
    IDbContextFactory<ProseDbContext> dbFactory,
    ILogger<StoryScopeAuditService> log)
{
    public const string FindingPrefix = "STORYSCOPE";

    // ── Public API ────────────────────────────────────────────────────────────

    public async Task<StoryScopeAuditReport> AuditAsync(Guid nodeId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        // IgnoreQueryFilters(): explicit nodeId, not an ambient scope (same bug class found and
        // fixed in BookArchiveService.ArchiveAsync/WalkAsync, 2026-08-17).
        var node = await db.Nodes.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(n => n.Id == nodeId, ct)
            ?? throw new InvalidOperationException($"Node {nodeId} not found.");

        var ordered = await workbench.GetOrderedBeatsAsync(nodeId, ct);
        var beats = ordered
            .Where(b => !string.IsNullOrWhiteSpace(b.Beat.Text))
            .ToList();
        if (beats.Count == 0)
            throw new InvalidOperationException($"Node '{node.Title}' has no written prose to audit.");

        var blueprint = await blueprints.GetAsync(nodeId, ct);
        var prose = string.Join("\n\n", beats.Select(b => b.Beat.Text));

        var checks = new List<StoryScopeCheck>();

        // ── Deterministic layer ───────────────────────────────────────────────
        checks.AddRange(await RunDeterministicChecksAsync(db, node, beats, blueprint, ct));

        // ── LLM-graded layer ─────────────────────────────────────────────────
        // The bible excerpt travels with every holistic check so the judge's fix
        // suggestions don't contradict the book's own deliberate design (narrative
        // locks, register choices) — the ATTE pilot showed judges recommending
        // against locked decisions when blind to them.
        var bibleExcerpt = string.IsNullOrWhiteSpace(node.NodeBible)
            ? ""
            : node.NodeBible.Length <= 6000 ? node.NodeBible : node.NodeBible[..6000] + " …[clamped]";

        var progressiveTask = ReadProgressiveAsync(beats, ct);
        var holisticTasks = BuildHolisticChecks(node, prose, blueprint)
            .Select(c => RunHolisticCheckAsync(c, bibleExcerpt, ct))
            .ToList();
        var clicheTask = RunConsensusClicheScanAsync(db, node, prose, ct);

        var progressive = await progressiveTask;
        if (progressive != null)
            checks.AddRange(DeriveProgressiveChecks(progressive, beats.Count));
        else
            // ERROR, not MINOR: this is StoryScope's core structural signal (escalation curve +
            // event-type diversity — "the #1 AI-fiction fingerprint") failing to run at all, not
            // a real finding. A MINOR severity let Ready stay true when the single most load-
            // bearing check silently never executed.
            checks.Add(new StoryScopeCheck("progressive_reading", "Per-beat stakes/event reading",
                "ERROR", "Progressive reading failed — escalation and event-diversity checks were skipped.", null, null, null));

        checks.AddRange(await Task.WhenAll(holisticTasks));
        checks.Add(await clicheTask);

        // ── Findings loop-back ────────────────────────────────────────────────
        WriteFindings(node, checks);

        return new StoryScopeAuditReport(
            NodeSlug:       node.Slug,
            NodeTitle:      node.Title,
            HasBlueprint:   blueprint != null,
            BeatCount:      beats.Count,
            Checks:         checks,
            BlockerCount:   checks.Count(c => c.Severity == "BLOCKER"),
            ModerateCount:  checks.Count(c => c.Severity == "MODERATE"),
            MinorCount:     checks.Count(c => c.Severity == "MINOR"),
            DeviationCount: checks.Count(c => c.Severity == "DEVIATION"),
            ErrorCount:     checks.Count(c => c.Severity == "ERROR"),
            // ERROR (a check that never actually ran) must block Ready exactly like a real
            // BLOCKER — see the two ERROR-producing catch blocks above for why.
            Ready:          checks.All(c => c.Severity is not ("BLOCKER" or "ERROR")));
    }

    // ── Deterministic layer ───────────────────────────────────────────────────

    async Task<List<StoryScopeCheck>> RunDeterministicChecksAsync(
        ProseDbContext db,
        Node node,
        List<NodeWorkbenchService.OrderedBeat> beats,
        NodeStructuralBlueprint? blueprint,
        CancellationToken ct)
    {
        var results = new List<StoryScopeCheck>();
        var beatIds = beats.Select(b => b.Beat.Id).ToList();

        // 0. Blueprint exists at all
        if (blueprint == null)
        {
            results.Add(new StoryScopeCheck("blueprint_missing", "Structural blueprint exists",
                "MODERATE",
                "No StructuralBlueprint exists for this node — structural commitments were never made, so the story defaults are whatever the model chose.",
                "Run prose --generate-blueprint --slug <slug> --retrofit, review the inferred structure, then re-audit.",
                "insert", null));
        }
        else
        {
            // 1. Subplot planned but never executed
            if (blueprint.HasSubplot)
            {
                var subplotBeatIds = blueprint.BeatTags
                    .Where(t => t.TagType == "subplot")
                    .Select(t => t.BeatId)
                    .ToHashSet();
                var executed = beats.Count(b => subplotBeatIds.Contains(b.Beat.Id));
                if (executed == 0)
                    results.Add(new StoryScopeCheck("subplot_not_executed", "Subplot executed",
                        "BLOCKER",
                        $"Blueprint commits to a subplot (\"{blueprint.SubplotSummary}\") but none of its carrier beats have written prose. 79% of AI stories have zero subplots vs 57% of human ones — an unexecuted subplot plan is the tell unfixed.",
                        "Write the subplot carrier beats, or regenerate the blueprint without a subplot if the story genuinely can't hold one.",
                        "insert", null));
                else
                    results.Add(Pass("subplot_not_executed", "Subplot executed",
                        $"Subplot carrier beats written: {executed}/{subplotBeatIds.Count}."));
            }

            // 2. Deviation surfacing — legal escape hatches, reported for human judgment
            if (blueprint.MoralPolarity == "clear")
                results.Add(new StoryScopeCheck("moral_polarity_deviation", "Moral polarity deviation",
                    "DEVIATION",
                    $"Blueprint commits to CLEAR moral polarity (default is ambivalent — 59% of human stories are morally mixed vs 38% of AI). Note: {blueprint.MoralPolarityNote ?? "(no justification recorded)"}",
                    null, null, null));
            if (blueprint.EndingStyle == "quiet")
                results.Add(new StoryScopeCheck("ending_style_deviation", "Ending style deviation",
                    "DEVIATION",
                    $"Blueprint commits to a QUIET ending (default is avalanche — quiet endings are a Claude fingerprint). Note: {blueprint.EndingNote ?? "(no justification recorded)"}",
                    null, null, null));
        }

        // 3. Beat-mode run-length (coarse pre-filter; the LLM progressive reading is authoritative)
        var modeRows = await db.BeatModeLogs.AsNoTracking()
            .Where(m => beatIds.Contains(m.BeatId))
            .ToListAsync(ct);
        var modeById = modeRows.ToDictionary(m => m.BeatId, m => m.Mode);
        var modesInOrder = beats
            .Select(b => modeById.GetValueOrDefault(b.Beat.Id))
            .Where(m => m != null)
            .ToList();
        var (runMode, runLength, runStart) = LongestRun(modesInOrder!);
        if (runLength >= 4)
            results.Add(new StoryScopeCheck("beat_mode_monoculture", "Beat-mode variety",
                "MODERATE",
                $"{runLength} consecutive beats classified as {runMode} starting around beat {runStart}. Event-type monoculture is Claude's #2 measured fingerprint.",
                "Vary the event type across the run — the blueprint's event palette assigns one per beat.",
                "replace", null));
        else
            results.Add(Pass("beat_mode_monoculture", "Beat-mode variety",
                modesInOrder.Count > 0
                    ? $"Longest same-mode run: {runLength} ({runMode ?? "n/a"}). Coverage: {modesInOrder.Count}/{beats.Count} beats have mode logs."
                    : "No beat-mode logs for this story — monoculture check skipped."));

        // 4. Emotional-depth plateau (weak secondary signal — labeled as such)
        var exam = await db.EmotionalExaminations.AsNoTracking()
            .Where(e => e.NodeId == node.Id)
            .OrderByDescending(e => e.ExaminedAt)
            .FirstOrDefaultAsync(ct);
        if (exam != null)
        {
            var depths = await db.EmotionalBeatScores.AsNoTracking()
                .Where(s => s.ExaminationId == exam.Id)
                .OrderBy(s => s.BeatNumber)
                .Select(s => s.Depth)
                .ToListAsync(ct);
            var (_, depthRun, depthStart) = LongestRun(depths.Select(d => d.ToString()).ToList());
            if (depthRun >= 4)
                results.Add(new StoryScopeCheck("emotional_plateau", "Emotional-depth escalation",
                    "MINOR",
                    $"{depthRun} consecutive beats at identical emotional depth starting around beat {depthStart} (weak signal — depth measures interiority, not stakes; the LLM stakes reading below is authoritative).",
                    null, null, null));
        }

        // 5. Social-network breadth from the persisted X-Ray index (BeatEntities).
        //    A two-hander SCENE is fine; a two-hander STORY is a network-poverty tell
        //    (Nonaka & Perry 2025: AI fiction has measurably simpler social structures).
        try
        {
            var idParams = string.Join(",", beatIds.Select((_, i) => $"@p{i}"));
            var sql = $"SELECT [EntityId], COUNT(DISTINCT [BeatId]) AS Beats FROM [dbo].[BeatEntities] " +
                      $"WHERE [EntityType] = 'character' AND [BeatId] IN ({idParams}) GROUP BY [EntityId]";
            var parameters = beatIds.Select((id, i) =>
                new Microsoft.Data.SqlClient.SqlParameter($"@p{i}", id)).ToArray();
            var counts = new List<int>();
            var conn = db.Database.GetDbConnection();
            await db.Database.OpenConnectionAsync(ct);
            try
            {
                await using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = sql;
                    cmd.Parameters.AddRange(parameters);
                    await using var reader = await cmd.ExecuteReaderAsync(ct);
                    while (await reader.ReadAsync(ct))
                        counts.Add(reader.GetInt32(1));
                }
            }
            finally
            {
                await db.Database.CloseConnectionAsync();
            }
            var recurring = counts.Count(c => c >= 2);
            if (recurring < 3)
                results.Add(new StoryScopeCheck("social_network_breadth", "Social-network breadth",
                    "MINOR",
                    $"Only {recurring} character(s) appear in 2+ beats. AI fiction has measurably simpler social structures than human fiction.",
                    "Give a secondary character a recurring on-page presence — the subplot carrier is the natural host.",
                    "insert", null));
            else if (recurring >= 3)
                results.Add(Pass("social_network_breadth", "Social-network breadth",
                    $"{recurring} characters recur across 2+ beats."));
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "[storyscope] BeatEntities social-network check skipped");
        }

        return results;
    }

    static StoryScopeCheck Pass(string key, string title, string evidence) =>
        new(key, title, "PASS", evidence, null, null, null);

    internal static (string? Value, int Length, int StartIndex) LongestRun(List<string> values)
    {
        string? bestVal = null; int bestLen = 0, bestStart = 0;
        int i = 0;
        while (i < values.Count)
        {
            int j = i;
            while (j < values.Count && values[j] == values[i]) j++;
            if (j - i > bestLen) { bestLen = j - i; bestVal = values[i]; bestStart = i; }
            i = j;
        }
        return (bestVal, bestLen, bestStart);
    }

    // ── LLM layer: progressive per-beat reading ───────────────────────────────

    async Task<List<BeatReading>?> ReadProgressiveAsync(
        List<NodeWorkbenchService.OrderedBeat> beats, CancellationToken ct)
    {
        // Book-scale nodes read at chapter granularity — same unit rule as the blueprint.
        var (granularity, units) = StructuralBlueprintService.GroupUnits(beats);
        var unitLabel = granularity == "chapter" ? "Chapter" : "Beat";

        // Hash cache: reuse prior readings for units whose prose is unchanged
        // (mirrors Legion's BeatTextHash ballot caching). A re-audit after a
        // one-beat splice re-reads one unit, not the whole story.
        var unitHashes = units.ToDictionary(
            u => u.Index,
            u => NodeWorkbenchService.ComputeTextHash(string.Join("\n\n", u.Beats.Select(b => b.Beat.Text ?? ""))));
        var unitFirstBeatIds = units.ToDictionary(u => u.Index, u => u.Beats[0].Beat.Id);

        var cached = new Dictionary<int, BeatReading>();
        try
        {
            await using var cacheDb = await dbFactory.CreateDbContextAsync(ct);
            var firstIds = unitFirstBeatIds.Values.ToList();
            var rows = await cacheDb.StructuralReadings.AsNoTracking()
                .Where(r => firstIds.Contains(r.BeatId))
                .ToListAsync(ct);
            var byBeatId = rows.ToDictionary(r => r.BeatId);
            foreach (var u in units)
                if (byBeatId.TryGetValue(unitFirstBeatIds[u.Index], out var row)
                    && row.UnitHash == unitHashes[u.Index])
                    cached[u.Index] = new BeatReading
                    {
                        Index = u.Index, Stakes = row.Stakes,
                        EventType = row.EventType, RevelationMode = row.RevelationMode,
                    };
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "[storyscope] reading cache load failed — reading everything fresh");
        }

        var toRead = units.Where(u => !cached.ContainsKey(u.Index)).ToList();
        log.LogInformation("[storyscope] progressive reading: {Cached}/{Total} units from cache, {Fresh} to read",
            cached.Count, units.Count, toRead.Count);

        if (toRead.Count == 0)
            return cached.Values.OrderBy(r => r.Index).ToList();

        var system = $$"""
            You are reading a story {{unitLabel.ToLowerInvariant()}}-by-{{unitLabel.ToLowerInvariant()}} and scoring each {{unitLabel.ToLowerInvariant()}} on three axes.
            For EVERY {{unitLabel.ToLowerInvariant()}}, return:
              - stakes: 1-10 — how large/costly/irreversible the events feel in context
              - eventType: one word — the dominant plot event (confrontation, discovery, chase,
                confession, ceremony, negotiation, ambush, loss, betrayal, arrival, departure,
                exchange, vigil, repair, filing, surveillance ... choose the truest word)
              - revelationMode: suspense | curiosity | surprise | none — the information dynamics
                (suspense = reader knows what a character doesn't; curiosity = effect shown, cause
                withheld; surprise = sudden recontextualizing disclosure; none = information flat)
            Return STRICT JSON only: { "beats": [ { "index": 0, "stakes": 3, "eventType": "arrival", "revelationMode": "curiosity" }, ... ] }
            """;

        var perUnitClamp = granularity == "chapter"
            ? Math.Clamp(60000 / Math.Max(units.Count, 1), 700, 2000)
            : 900;
        var sb = new System.Text.StringBuilder();

        // When reading a subset, give the model the cached neighbors' readings so
        // subset stakes stay calibrated against the rest of the story.
        if (cached.Count > 0)
        {
            sb.AppendLine($"CONTEXT — already-scored {unitLabel.ToLowerInvariant()}s (unchanged since last read; do NOT re-score these):");
            foreach (var r in cached.Values.OrderBy(r => r.Index))
                sb.AppendLine($"  {unitLabel} {r.Index}: stakes {r.Stakes}/10, {r.EventType}, {r.RevelationMode}");
            sb.AppendLine();
            sb.AppendLine($"Score ONLY the {unitLabel.ToLowerInvariant()}s below, keeping stakes consistent with the context above:");
        }

        foreach (var u in toRead)
        {
            sb.AppendLine($"--- {unitLabel} {u.Index} ---");
            var text = string.Join("\n\n", u.Beats.Select(b => b.Beat.Text ?? ""));
            sb.AppendLine(text.Length <= perUnitClamp
                ? text
                : text[..(int)(perUnitClamp * 0.65)] + "\n…[middle elided]…\n" + text[^(perUnitClamp - (int)(perUnitClamp * 0.65))..]);
        }

        try
        {
            var raw = await llm.GenerateAsync(system, sb.ToString(), temperature: 0.2, maxTokens: 4096, ct: ct);
            var parsed = ParseJson<ProgressiveRaw>(raw);
            var fresh = parsed?.Beats;
            if (fresh == null) return cached.Count > 0 ? cached.Values.OrderBy(r => r.Index).ToList() : null;

            // Persist fresh readings to the cache (upsert by unit-first BeatId).
            // Bulk-load existing rows to avoid N+1 FindAsync inside the loop.
            try
            {
                await using var writeDb = await dbFactory.CreateDbContextAsync(ct);
                var freshToWrite = fresh.Where(r => unitFirstBeatIds.ContainsKey(r.Index)).ToList();
                var beatIdsToLoad = freshToWrite.Select(r => unitFirstBeatIds[r.Index]).ToList();
                var existingRows = await writeDb.StructuralReadings
                    .Where(sr => beatIdsToLoad.Contains(sr.BeatId))
                    .ToListAsync(ct);
                var rowsByBeatId = existingRows.ToDictionary(sr => sr.BeatId);

                foreach (var r in freshToWrite)
                {
                    var beatId = unitFirstBeatIds[r.Index];
                    if (!rowsByBeatId.TryGetValue(beatId, out var row))
                    {
                        row = new StructuralReading { BeatId = beatId };
                        writeDb.StructuralReadings.Add(row);
                    }
                    row.UnitHash       = unitHashes[r.Index];
                    row.Stakes         = r.Stakes;
                    row.EventType      = r.EventType ?? "";
                    row.RevelationMode = r.RevelationMode ?? "";
                    row.ReadAt         = DateTime.UtcNow;
                }
                await writeDb.SaveChangesAsync(ct);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                log.LogWarning(ex, "[storyscope] reading cache write failed — results still returned");
            }

            return cached.Values.Concat(fresh).OrderBy(r => r.Index).ToList();
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            log.LogWarning(ex, "[storyscope] progressive reading failed");
            return cached.Count > 0 ? cached.Values.OrderBy(r => r.Index).ToList() : null;
        }
    }

    internal static List<StoryScopeCheck> DeriveProgressiveChecks(List<BeatReading> readings, int beatCount)
    {
        var results = new List<StoryScopeCheck>();
        var ordered = readings.OrderBy(r => r.Index).ToList();

        // Flat escalation — Claude's #1 fingerprint (SHAP 0.402). Window is 4+
        // consecutive equal scores: stakes readings carry ±1 run-to-run noise, and
        // 3-in-a-row exact equality fires on that noise (two cold reads of the same
        // story produced different 3-plateau sets in the Sparrow fix pass).
        var stakes = ordered.Select(r => r.Stakes).ToList();
        var plateaus = new List<string>();
        for (int i = 0; i + 3 < stakes.Count; i++)
            if (stakes[i] == stakes[i + 1] && stakes[i] == stakes[i + 2] && stakes[i] == stakes[i + 3])
                plateaus.Add($"beats {i}-{i + 3} at {stakes[i]}/10");
        var climaxZoneStart = (int)(stakes.Count * 0.6);
        var maxStakes = stakes.Count > 0 ? stakes.Max() : 0;
        var peakIndex = stakes.IndexOf(maxStakes);
        // IndexOf finds the FIRST beat tying the max — if an early beat and the true climax
        // both hit the same top stakes value, that alone doesn't mean the story de-escalates;
        // the climax is still correctly placed. Only flag early_peak when NO occurrence of the
        // max reaches the climax zone at all.
        var peakReachedInClimaxZone = climaxZoneStart < stakes.Count
            && stakes.Skip(climaxZoneStart).Any(s => s == maxStakes);
        if (plateaus.Count > 0)
            results.Add(new StoryScopeCheck("flat_escalation", "Event escalation",
                "MODERATE",
                $"Stakes plateau at: {string.Join("; ", plateaus.Take(4))}. Flat escalation is the single strongest AI-fiction fingerprint measured (Claude's #1, SHAP 0.402).",
                "Raise or drop the stakes across each plateau — every beat must feel larger, more costly, or more irreversible than the last.",
                "replace", null));
        else
            results.Add(Pass("flat_escalation", "Event escalation",
                $"No 3-beat stakes plateaus. Peak {maxStakes}/10 at beat {peakIndex}."));
        if (peakIndex >= 0 && !peakReachedInClimaxZone && stakes.Count >= 5)
            results.Add(new StoryScopeCheck("early_peak", "Escalation peak placement",
                "MODERATE",
                $"The stakes peak ({maxStakes}/10) lands at beat {peakIndex} of {stakes.Count} — before the final 40%. The story de-escalates into its own ending.",
                "Move the largest consequence into the climax zone, or escalate past the current peak in the final act.",
                "replace", null));
        else if (stakes.Count >= 5)
            results.Add(Pass("early_peak", "Escalation peak placement",
                $"Escalation peak correctly placed in climax zone (beat {peakIndex} of {stakes.Count})."));

        // Event monoculture — Claude's #2 fingerprint.
        var types = ordered.Select(r => r.EventType?.ToLowerInvariant() ?? "").ToList();
        var repeats = new List<string>();
        for (int i = 0; i + 1 < types.Count; i++)
            if (types[i].Length > 0 && types[i] == types[i + 1])
                repeats.Add($"beats {i}-{i + 1} ({types[i]})");
        var distinctRatio = types.Count > 0 ? types.Where(t => t.Length > 0).Distinct().Count() / (double)types.Count : 0;
        if (repeats.Count >= 2 || distinctRatio < 0.4)
            results.Add(new StoryScopeCheck("event_monoculture", "Event-type diversity",
                "MODERATE",
                $"Back-to-back repeated event types: {(repeats.Count > 0 ? string.Join("; ", repeats.Take(4)) : "none")}. Distinct-type ratio: {distinctRatio:P0}. Low event-type diversity is Claude's #2 measured fingerprint.",
                "Re-type the repeated beats — confession, negotiation, ceremony, loss are underused alternatives to another confrontation.",
                "replace", null));
        else
            results.Add(Pass("event_monoculture", "Event-type diversity",
                $"Distinct-type ratio {distinctRatio:P0}; back-to-back repeats: {repeats.Count}."));

        // Information-dynamics flatline (NarraBench Revelation dimension).
        var noneShare = ordered.Count > 0
            ? ordered.Count(r => string.Equals(r.RevelationMode, "none", StringComparison.OrdinalIgnoreCase)) / (double)ordered.Count
            : 0;
        if (noneShare > 0.6)
            results.Add(new StoryScopeCheck("revelation_flatline", "Information dynamics",
                "MODERATE",
                $"{noneShare:P0} of beats carry no information dynamics (no suspense, curiosity, or surprise). This is the discourse-level version of flat escalation.",
                "Give flat beats a withheld cause or an asymmetry between what the reader and a character know.",
                "insert", null));
        else
            results.Add(Pass("revelation_flatline", "Information dynamics",
                $"{1 - noneShare:P0} of beats carry active information dynamics."));

        return results;
    }

    // ── LLM layer: holistic checks ────────────────────────────────────────────

    record HolisticCheck(string Key, string Title, string Question, string ProseSlice, string SeverityOnFail, bool Perspectival);

    IEnumerable<HolisticCheck> BuildHolisticChecks(Node node, string prose, NodeStructuralBlueprint? blueprint)
    {
        var full = AuditProseUtils.ClampProse(prose);
        var tail = prose.Length <= 25000 ? prose : prose[^25000..];

        yield return new HolisticCheck("moral_gloss", "Narrator moral gloss",
            "Does the narrator explicitly state the story's theme, moral, or what events 'meant' — a lesson-learned paragraph, an 'and in that moment she understood that...' epiphany, a closing summary of significance? CALIBRATION: human stories do this 52% of the time (AI: 77%) — some thematic naming is normal, not a defect. FAIL only for unambiguous, explicit theme-statement in the narrator's voice. NOT gloss: interiority rendered in the POV character's own documented register; a character weighing a concrete situation; concrete imagery and action (closing image fragments are the OPPOSITE of gloss — never flag them); plot facts restated. If the strongest candidate is borderline or in-register, status=pass and say why. Do not escalate to the next-most-reflective passage in a story whose worst gloss is already mild.",
            tail, "MODERATE", false);   // MODERATE, not BLOCKER: verdicts on interiority-heavy registers
                                        // drift between runs (ATTE-sweep ratchet) — blocking authority
                                        // stays with deterministic checks; findings still loop back.

        yield return new HolisticCheck("emotion_ratio", "Embodied-vs-labeled emotion",
            "How is emotion rendered? AI fiction routes 81% of emotion through body sensations (tightening chest, cold sweat, held breath) and uses explicit labels ('she was afraid') only 8% of the time; humans label 29% of the time. FAIL if the story exclusively uses embodied rendering with zero (or near-zero) direct emotion labels; PASS if there's a working mix. Count roughly.",
            full, "MODERATE", false);

        yield return new HolisticCheck("char_intro", "Character-introduction method",
            "How are new characters introduced? Description-first entry (a physical inventory before the character acts or speaks) appears in 52% of AI stories vs 30% of human ones; humans most distinctively introduce through dialogue and in-action. FAIL if most named characters arrive as static description; cite the worst example.",
            full, "MODERATE", false);

        yield return new HolisticCheck("dialogue_philosophy", "Dialogue-as-philosophy",
            "Does dialogue function as philosophical debate — characters trading abstract positions — rather than as a status battle between people who want different things? AI: 59%, humans: 34%. PASS means dialogue is transactional/status-driven (the human-like state — its ABSENCE of philosophy is good, not a gap); FAIL means characters conduct seminars. Quote the most seminar-like exchange if present. Do NOT recommend adding philosophical debate.",
            full, "MODERATE", false);

        var resolutionCommitment = blueprint != null
            ? $"The blueprint commits to resolution mode '{blueprint.ResolutionMode}'{(blueprint.ResolutionNote != null ? $" ({blueprint.ResolutionNote})" : "")} and ending style '{blueprint.EndingStyle}' with noEpilogue={blueprint.NoEpilogue}. "
            : "";
        yield return new HolisticCheck("resolution_mode", "Resolution mode as written",
            resolutionCommitment +
            "How does the story actually exit? FAIL if the resolution is the protagonist achieving NARRATED internal understanding/acceptance/peace (AI: 47%, humans: 27%) — an epiphany the prose spells out — or if a retrospective epilogue narrates significance after the last event. PASS if the outcome is decided externally, stays genuinely open, or is mixed — and the story ends on its last event. CALIBRATION: a protagonist making an external CHOICE (staying, leaving, sending, refusing) rendered as action or image is an external resolution, not internal understanding — never flag a choice shown through action. Humans resolve internally 27% of the time; only the explicit narrated-peace exit is the tell.",
            tail, "MODERATE", false);  // MODERATE, not BLOCKER — same rationale as moral_gloss.

        if (blueprint != null && blueprint.IntertextualAnchorsJson.Length > 4)
            yield return new HolisticCheck("anchors_presence", "Intertextual anchors present",
                $"The blueprint commits to these named in-world references: {blueprint.IntertextualAnchorsJson}. Are they actually touched in the prose — named, specific, in-voice? FAIL if none appear; WARN if fewer than half.",
                full, "MODERATE", false);

        yield return new HolisticCheck("originality_form", "Originality in form",
            "Does the story show any formal or structural originality — a document interleave, timeline play, an unusual narrative frame, any deliberate shape beyond scene-after-scene? Professional writers pass this 64% of the time; LLMs 0-8% (the worst gap in the TTCW study). This is perspectival — include a confidence 0-1 in your evidence.",
            full, "MINOR", true);

        yield return new HolisticCheck("unique_takeaway", "Unique takeaway",
            "Would an average reader take away a unique or original idea from this story — something they haven't gotten from other stories with this premise? (TTCW Originality-in-Thought: pros 75%, LLMs 0-19%.) Perspectival — include a confidence 0-1.",
            full, "MINOR", true);

        yield return new HolisticCheck("plot_function_character", "Plot-function characters",
            "Does any named character exist merely to satisfy a plot requirement — a courier of information or capability with no want, fear, or interiority of their own? Name them if so. (TTCW Character Development: pros 61%, LLMs 8-17%.)",
            full, "MODERATE", false);

        yield return new HolisticCheck("subtext", "Surface + subtext",
            "Does the story operate at two levels — surface action plus a second level of meaning the prose never names? Perspectival — include a confidence 0-1. FAIL only if the story is single-layer 'light and sound'.",
            full, "MINOR", true);

        yield return new HolisticCheck("single_track_causality", "Causal-chain threading",
            "Reconstruct the causal chain. Is it 100% single-track — one thread of cause and effect with zero parallel or interleaved threads? A subplot that never interleaves isn't a subplot. FAIL if strictly single-track; cite where a second thread could interleave.",
            full, "MODERATE", false);

        yield return new HolisticCheck("line_mechanics", "Tense + pronoun mechanics",
            "Scan for (a) tense inconsistency — unmotivated shifts between past/present within a passage — and (b) unclear pronoun antecedents. These are the two LAMP idiosyncrasy categories not covered elsewhere. Cite up to 3 instances with rough locations.",
            full, "MINOR", false);
    }

    async Task<StoryScopeCheck> RunHolisticCheckAsync(HolisticCheck check, string bibleExcerpt, CancellationToken ct)
    {
        var system = """
            You are auditing one structural property of a story — a measurable tell that separates
            AI fiction from human fiction. Be specific: cite prose, name beats, count when asked.
            If the book's design notes (bible) show a flagged property is a deliberate, locked
            choice, say so in the evidence and never propose a fix that contradicts a lock —
            deliberate design is reported, not corrected.
            Respond as JSON only:
            {
              "status":     "pass" | "warn" | "fail",
              "evidence":   "1-3 sentence specific observation, quoting the prose where possible",
              "fix":        "one concrete sentence, or null if passing",
              "operation":  "replace" | "delete" | "insert" | null,
              "confidence": 0.0-1.0 (only for perspectival checks; else null)
            }
            """;

        var bibleBlock = bibleExcerpt.Length > 0
            ? $"\nBOOK DESIGN NOTES (the book's own bible — locks and register choices are deliberate):\n{bibleExcerpt}\n"
            : "";

        var user = $"""
            CHECK: {check.Title}
            QUESTION: {check.Question}
            {bibleBlock}
            STORY PROSE:
            {check.ProseSlice}
            """;

        try
        {
            var raw = await llm.GenerateAsync(system, user, temperature: 0.2, maxTokens: 500, ct: ct);
            var parsed = ParseJson<HolisticRaw>(raw);
            var status = parsed?.Status?.ToLowerInvariant() ?? "warn";

            // Severity mapping: fail → the check's fail tier; warn → one tier below.
            // Perspectival checks with low confidence never exceed MINOR (NarraBench
            // variance rule: subjective properties don't get deterministic authority).
            var severity = status switch
            {
                "pass" => "PASS",
                "fail" => check.SeverityOnFail,
                _      => Downgrade(check.SeverityOnFail),
            };
            if (check.Perspectival && severity != "PASS" && (parsed?.Confidence ?? 0) < 0.7)
                severity = "MINOR";

            return new StoryScopeCheck(check.Key, check.Title, severity,
                parsed?.Evidence ?? "(evaluation returned no evidence)",
                parsed?.Fix, parsed?.Operation, parsed?.Confidence);
        }
        catch (Exception ex)
        {
            // ERROR, not MINOR: this check never actually ran (LLM timeout, provider outage,
            // malformed response) — folding that into MINOR let Ready stay true even when every
            // holistic check failed to evaluate, same false-"clean" bug as BookAuditService's
            // GatewayReady (2026-08-09).
            return new StoryScopeCheck(check.Key, check.Title, "ERROR",
                $"Evaluation failed: {ex.Message}", null, null, null);
        }
    }

    static string Downgrade(string severity) => severity switch
    {
        "BLOCKER"  => "MODERATE",
        "MODERATE" => "MINOR",
        _           => "MINOR",
    };

    /// <summary>
    /// Run ONLY the consensus-cliché scan for a node — one LLM call. Used by
    /// `prose --storyscope-audit --cliches-only` for cheap blocklist-corroboration
    /// sweeps (the full audit is ~15 calls; promotion only needs this one).
    /// Updates the ConsensusCliches table exactly like the full audit.
    /// </summary>
    public async Task<StoryScopeCheck> ScanClichesAsync(Guid nodeId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        // IgnoreQueryFilters(): explicit nodeId, not an ambient scope (same bug class found and
        // fixed in BookArchiveService.ArchiveAsync/WalkAsync, 2026-08-17).
        var node = await db.Nodes.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(n => n.Id == nodeId, ct)
            ?? throw new InvalidOperationException($"Node {nodeId} not found.");

        var beats = (await workbench.GetOrderedBeatsAsync(nodeId, ct))
            .Where(b => !string.IsNullOrWhiteSpace(b.Beat.Text))
            .ToList();
        if (beats.Count == 0)
            throw new InvalidOperationException($"Node '{node.Title}' has no written prose to scan.");

        var prose = string.Join("\n\n", beats.Select(b => b.Beat.Text));
        return await RunConsensusClicheScanAsync(db, node, prose, ct);
    }

    // ── LLM layer: consensus-cliché scan ─────────────────────────────────────

    async Task<StoryScopeCheck> RunConsensusClicheScanAsync(
        ProseDbContext db, Node node, string prose, CancellationToken ct)
    {
        var blocked = await db.ConsensusCliches.AsNoTracking()
            .Where(c => c.UniverseId == node.UniverseId && c.FlagCount >= 2)
            .OrderByDescending(c => c.FlagCount)
            .Take(25)
            .Select(c => c.Device)
            .ToListAsync(ct);

        // Provisional devices (FlagCount = 1) are shown so the judge REUSES their exact
        // wording when the same device recurs — free-text re-phrasing means exact-match
        // corroboration never fires and nothing ever gets promoted to the active blocklist.
        var provisional = await db.ConsensusCliches.AsNoTracking()
            .Where(c => c.UniverseId == node.UniverseId && c.FlagCount == 1)
            .OrderByDescending(c => c.AddedAt)
            .Take(60)
            .Select(c => c.Device)
            .ToListAsync(ct);

        var blockedList = blocked.Count > 0
            ? "KNOWN CONSENSUS CLICHÉS for this universe (LLMs converge on these — flag any that appear):\n" +
              string.Join("\n", blocked.Select(d => $"  - {d}"))
            : "No active blocklist exists yet for this universe.";

        var provisionalList = provisional.Count > 0
            ? "\n\nPROVISIONAL DEVICES already flagged once in this universe. If a device you see in " +
              "this story matches one below IN SUBSTANCE, return the wording below VERBATIM in your " +
              "devices array (corroboration is exact-match); only invent new wording for genuinely new devices:\n" +
              string.Join("\n", provisional.Select(d => $"  - {d}"))
            : "";

        var system = """
            You are scanning a story for CONSENSUS CLICHÉS — concrete narrative devices that language
            models statistically converge on across independent generations (not phrases; plot-level
            devices, e.g. "the mentor dies passing on one last clue", "the protagonist watches the
            hand-off from a parked car", "time is a river" metaphors). Different model families
            collapse onto the same devices, so the presence of one is a measurable tell.

            STRICT BAR for flagging a device: it must be a genre-generic choice you would expect
            most LLMs to produce for ANY story of this type — NOT the story's own premise, its
            locked design choices, or elements specific to its world. "The investigator follows a
            paper trail" in a procedural is the genre, not a cliché. Maximum 5 devices; fewer is
            better; an empty list is a valid answer.
            Respond as JSON only:
            {
              "status": "pass" | "warn" | "fail",
              "evidence": "which devices appear, where",
              "devices": [ "device stated concretely", ... ],
              "fix": "one concrete sentence or null"
            }
            """;

        var user = $"""
            {blockedList}{provisionalList}

            Also flag NEW stock devices (max 5, strict bar above) this story leans on that you
            would expect most LLMs to produce for this premise — the statistically safe choices.

            STORY PROSE:
            {AuditProseUtils.ClampProse(prose)}
            """;

        try
        {
            var raw = await llm.GenerateAsync(system, user, temperature: 0.3, maxTokens: 600, ct: ct);
            var parsed = ParseJson<ClicheRaw>(raw);
            var status = parsed?.Status?.ToLowerInvariant() ?? "warn";

            // Record newly-flagged devices into the running blocklist.
            foreach (var device in parsed?.Devices ?? [])
            {
                if (string.IsNullOrWhiteSpace(device)) continue;
                var trimmed = device.Trim();
                var existing = await db.ConsensusCliches
                    .FirstOrDefaultAsync(c => c.UniverseId == node.UniverseId && c.Device == trimmed, ct);
                if (existing != null)
                {
                    // One corroboration per story: skip if this slug already counted
                    // (either as the first flagger or a prior corroborator in Notes).
                    var alreadyCounted = existing.FirstFlaggedInSlug == node.Slug
                        || (existing.Notes?.Contains(node.Slug) ?? false);
                    if (!alreadyCounted)
                    {
                        existing.FlagCount++;
                        existing.UpdatedAt = DateTime.UtcNow;
                        existing.Notes = $"{existing.Notes} | also flagged in {node.Slug}".TrimStart(' ', '|');
                    }
                }
                else
                {
                    db.ConsensusCliches.Add(new ConsensusCliche
                    {
                        UniverseId = node.UniverseId,
                        Device = trimmed,
                        FirstFlaggedInSlug = node.Slug,
                        Notes = $"Flagged by storyscope-audit of {node.Slug}",
                    });
                }
            }
            await db.SaveChangesAsync(ct);

            return new StoryScopeCheck("consensus_cliches", "Consensus-cliché scan",
                status == "pass" ? "PASS" : status == "fail" ? "MODERATE" : "MINOR",
                parsed?.Evidence ?? "(no evidence returned)",
                parsed?.Fix, "replace", null);
        }
        catch (Exception ex)
        {
            return new StoryScopeCheck("consensus_cliches", "Consensus-cliché scan", "MINOR",
                $"Evaluation failed: {ex.Message}", null, null, null);
        }
    }

    // ── Findings loop-back ────────────────────────────────────────────────────

    void WriteFindings(Node node, List<StoryScopeCheck> checks)
    {
        // Auto-heal: a check that now passes retires any stale finding it wrote on a
        // prior run — otherwise superseded findings sit at Status=New forever and
        // eventually pollute the beat-prompt loop-back.
        foreach (var check in checks.Where(c => c.Severity == "PASS"))
        {
            try { findings.DeleteBySummaryPrefix($"node:{node.Slug}", $"{FindingPrefix} {check.Key}:"); }
            catch (Exception ex)
            {
                log.LogWarning(ex, "[storyscope] stale finding cleanup failed for {Key}", check.Key);
            }
        }

        foreach (var check in checks.Where(c => c.Severity is "BLOCKER" or "MODERATE"))
        {
            try
            {
                // Replace semantics per check key: derived checks' evidence text moves
                // between runs (plateau locations, counts), which changes the dedup key —
                // without this, superseded findings accumulate at Status=New.
                findings.DeleteBySummaryPrefix($"node:{node.Slug}", $"{FindingPrefix} {check.Key}:");
                findings.Upsert(
                    filePath:     $"node:{node.Slug}",
                    chapterId:    null,
                    category:     FindingCategory.StoryScope,
                    severity:     check.Severity == "BLOCKER" ? FindingSeverity.High : FindingSeverity.Medium,
                    summary:      $"{FindingPrefix} {check.Key}: {Truncate(check.Evidence, 300)}",
                    snippet:      null,
                    suggestedFix: check.Fix);
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "[storyscope] finding write failed for {Key}", check.Key);
            }
        }
    }

    static string Truncate(string s, int max) => s.Length <= max ? s : s[..max];

    // ── Shared helpers ────────────────────────────────────────────────────────

    internal static T? ParseJson<T>(string raw)
    {
        try
        {
            var start = raw.IndexOf('{');
            var end   = raw.LastIndexOf('}');
            if (start < 0 || end < start) return default;
            return JsonSerializer.Deserialize<T>(raw[start..(end + 1)], new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });
        }
        catch { return default; }
    }

    class ProgressiveRaw
    {
        [JsonPropertyName("beats")] public List<BeatReading>? Beats { get; set; }
    }

    public class BeatReading
    {
        [JsonPropertyName("index")]          public int Index { get; set; }
        [JsonPropertyName("stakes")]         public int Stakes { get; set; }
        [JsonPropertyName("eventType")]      public string? EventType { get; set; }
        [JsonPropertyName("revelationMode")] public string? RevelationMode { get; set; }
    }

    class HolisticRaw
    {
        [JsonPropertyName("status")]     public string? Status { get; set; }
        [JsonPropertyName("evidence")]   public string? Evidence { get; set; }
        [JsonPropertyName("fix")]        public string? Fix { get; set; }
        [JsonPropertyName("operation")]  public string? Operation { get; set; }
        [JsonPropertyName("confidence")] public double? Confidence { get; set; }
    }

    class ClicheRaw
    {
        [JsonPropertyName("status")]   public string? Status { get; set; }
        [JsonPropertyName("evidence")] public string? Evidence { get; set; }
        [JsonPropertyName("devices")]  public List<string>? Devices { get; set; }
        [JsonPropertyName("fix")]      public string? Fix { get; set; }
    }
}

// ── Result models ─────────────────────────────────────────────────────────────

/// <summary>One StoryScope audit check. Severity uses the canonical logic-sweep
/// triage (BLOCKER / MODERATE / MINOR — docs/LOGIC.md) plus PASS and DEVIATION
/// (a legal blueprint escape hatch surfaced for human judgment, not a failure).
/// FixOperation labels the LAMP edit taxonomy: replace | delete | insert.</summary>
public record StoryScopeCheck(
    string  Key,
    string  Title,
    string  Severity,      // "PASS" | "BLOCKER" | "MODERATE" | "MINOR" | "DEVIATION"
    string  Evidence,
    string? Fix,
    string? FixOperation,  // "replace" | "delete" | "insert" | null
    double? Confidence);   // set on perspectival checks only

public record StoryScopeAuditReport(
    string NodeSlug,
    string NodeTitle,
    bool   HasBlueprint,
    int    BeatCount,
    List<StoryScopeCheck> Checks,
    int    BlockerCount,
    int    ModerateCount,
    int    MinorCount,
    int    DeviationCount,
    int    ErrorCount,
    bool   Ready);
