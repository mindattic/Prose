using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Generates and serves the per-story StructuralBlueprint — the pre-prose layer of
/// the StoryScope countermeasures (arXiv 2604.03136: AI fiction is detectable at
/// 93.2% from narrative-structure decisions alone; editing prose doesn't remove the
/// tells, so the counters must be decided BEFORE prose exists).
///
/// One LLM call decides all structural commitments together (they interact: ending
/// style and resolution mode must agree). The prompt opens with an outlier-seeking
/// step — list the three most obvious resolutions, then forbid them — because
/// ideas-layer divergence is the only layer where divergence works (Echoes in AI,
/// PNAS 2025; Artificial Hivemind, NeurIPS 2025: sampling and ensembles do NOT
/// break semantic homogeneity).
///
/// Ordering contract: node bible first (NodeBibleService), blueprint second,
/// prose last. BuildBeatInjectionAsync feeds the per-beat slice into the
/// ProseWriterRouter enrichment chain; StoryScopeAuditService verifies the
/// commitments held after writing.
/// </summary>
public class StructuralBlueprintService
{
    private readonly ILlmService llm;
    private readonly IDbContextFactory<StreetSamuraiDbContext> dbFactory;
    private readonly NodeWorkbenchService workbench;
    private readonly EmbeddingService embeddings;
    private readonly ILogger<StructuralBlueprintService> log;

    private static readonly string[] AnchorEntityTypes = ["entertainment", "document", "news", "quote"];

    /// <summary>Above this beat count, blueprints plan at CHAPTER granularity — per-beat
    /// escalation/event arrays don't fit a response window at book scale, and the
    /// structural decisions live at chapter level anyway for long works.</summary>
    public const int ChapterGranularityThreshold = 60;

    /// <summary>One planning unit: a beat (short works) or a chapter's run of beats (books).</summary>
    public sealed record StructuralUnit(int Index, Guid OwnerNodeId, string? Title, List<NodeWorkbenchService.OrderedBeat> Beats);

    /// <summary>Group ordered beats into planning units. Short works: one unit per beat.
    /// Book-scale works (beats > threshold): one unit per consecutive same-owner run
    /// (i.e., per chapter, in reading order).</summary>
    public static (string Granularity, List<StructuralUnit> Units) GroupUnits(
        List<NodeWorkbenchService.OrderedBeat> beats,
        IReadOnlyDictionary<Guid, string>? ownerTitles = null,
        bool forceChapter = false)
    {
        if (!forceChapter && beats.Count <= ChapterGranularityThreshold)
            return ("beat", beats
                .Select((b, i) => new StructuralUnit(i, b.NodeId, b.Beat.Title, [b]))
                .ToList());

        var units = new List<StructuralUnit>();
        var i = 0;
        while (i < beats.Count)
        {
            var owner = beats[i].NodeId;
            var run = new List<NodeWorkbenchService.OrderedBeat>();
            while (i < beats.Count && beats[i].NodeId == owner) { run.Add(beats[i]); i++; }
            var title = ownerTitles != null && ownerTitles.TryGetValue(owner, out var t) ? t : null;
            units.Add(new StructuralUnit(units.Count, owner, title, run));
        }
        return ("chapter", units);
    }

    public StructuralBlueprintService(
        ILlmService llm,
        IDbContextFactory<StreetSamuraiDbContext> dbFactory,
        NodeWorkbenchService workbench,
        EmbeddingService embeddings,
        ILogger<StructuralBlueprintService> log)
    {
        this.llm        = llm;
        this.dbFactory  = dbFactory;
        this.workbench  = workbench;
        this.embeddings = embeddings;
        this.log        = log;
    }

    // ── Retrieval ─────────────────────────────────────────────────────────

    public async Task<NodeStructuralBlueprint?> GetAsync(Guid nodeId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.NodeStructuralBlueprints
            .Include(b => b.BeatTags)
            .FirstOrDefaultAsync(b => b.NodeId == nodeId, ct);
    }

    // ── Generation ────────────────────────────────────────────────────────

    /// <summary>
    /// Generate a blueprint for a story that has a bible but no (or little) prose.
    /// Replaces any existing blueprint for the node.
    /// </summary>
    public async Task<NodeStructuralBlueprint> GenerateAndSaveAsync(Guid nodeId, CancellationToken ct = default)
        => await GenerateCoreAsync(nodeId, retrofit: false, ct);

    /// <summary>
    /// Infer a blueprint from a story's already-written prose — for existing
    /// stories that predate the blueprint system. The audit then reports where
    /// the story deviates from its own inferred structure.
    /// </summary>
    public async Task<NodeStructuralBlueprint> RetrofitAsync(Guid nodeId, CancellationToken ct = default)
        => await GenerateCoreAsync(nodeId, retrofit: true, ct);

    private async Task<NodeStructuralBlueprint> GenerateCoreAsync(Guid nodeId, bool retrofit, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var node = await db.Nodes.FindAsync([nodeId], ct)
            ?? throw new InvalidOperationException($"Node {nodeId} not found.");

        if (string.IsNullOrWhiteSpace(node.NodeBible) && !retrofit)
            throw new InvalidOperationException(
                $"Node '{node.Title}' has no bible. Generate the bible first (bible → blueprint → prose).");

        var beats = await workbench.GetOrderedBeatsAsync(nodeId, ct);
        if (beats.Count == 0)
            throw new InvalidOperationException(
                $"Node '{node.Title}' has no beats — the blueprint tags planned beats, so create the spine first.");

        // Intertextual anchor candidates: named in-world works/brands/places
        // semantically adjacent to this story's premise. The LLM decides HOW to
        // reference them, not WHAT the candidates are.
        var anchorQuery = node.Seed is { Length: > 0 } ? node.Seed : node.Title;
        IReadOnlyList<EmbeddingHit> anchorCandidates = [];
        try
        {
            // Scope the anchor search to THIS story's universe for the duration of the query, so a
            // blueprint generated from a process defaulted to another universe still only pulls
            // in-universe anchors. Combined with the entity-universe filter in FindSimilarAsync,
            // this prevents cross-universe intertextual leaks (e.g. a SCRY quote in a GLMZ story).
            UniverseScope.Current?.SetFlowUniverse(node.UniverseId);
            try
            {
                anchorCandidates = await embeddings.FindSimilarAsync(anchorQuery, k: 8, AnchorEntityTypes, ct);
            }
            finally
            {
                UniverseScope.Current?.SetFlowUniverse(null);
            }
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "[blueprint] anchor candidate lookup failed for {NodeId} — continuing without", nodeId);
        }

        // Book-scale nodes plan at chapter granularity — one unit per chapter.
        var ownerIds = beats.Select(b => b.NodeId).Distinct().ToList();
        var ownerTitles = await db.Nodes.AsNoTracking()
            .Where(n => ownerIds.Contains(n.Id))
            .ToDictionaryAsync(n => n.Id, n => n.Title, ct);
        var (granularity, units) = GroupUnits(beats, ownerTitles);

        var system = BuildSystemPrompt(retrofit, granularity);
        var user   = BuildUserPrompt(node, units, granularity, anchorCandidates, retrofit);

        log.LogInformation("[blueprint] Generating ({Mode}, {Granularity}) for node {Title} — {Beats} beats / {Units} units",
            retrofit ? "retrofit" : "pre-prose", granularity, node.Title, beats.Count, units.Count);

        // Response budget scales with unit count — per-unit escalation + event entries
        // overflow 4k tokens at ~45+ units and truncate the JSON mid-array.
        var maxTokens = Math.Clamp(units.Count * 130 + 2000, 4096, 16000);
        var raw = await llm.GenerateAsync(system, user, temperature: 0.8, maxTokens: maxTokens, ct: ct);
        var parsed = ParseResponse(raw, units.Count);

        // Replace any existing blueprint (cascade removes beat tags).
        var existing = await db.NodeStructuralBlueprints
            .Where(b => b.NodeId == nodeId)
            .ToListAsync(ct);
        if (existing.Count > 0)
            db.NodeStructuralBlueprints.RemoveRange(existing);

        // Clamp free-text fields to their column caps — the LLM doesn't know the schema.
        static string? Cap(string? s, int max) =>
            string.IsNullOrWhiteSpace(s) ? null : (s.Length <= max ? s : s[..max]);

        var blueprint = new NodeStructuralBlueprint
        {
            NodeId       = nodeId,
            UniverseId   = node.UniverseId,
            HasSubplot   = parsed.Subplot?.Summary is { Length: > 0 },
            SubplotSummary = Cap(parsed.Subplot?.Summary, 1000),
            SubplotTheme   = Cap(parsed.Subplot?.ThematicParallel, 500),
            TemporalScheme = NormalizeChoice(parsed.Temporal?.Scheme, ["linear", "frame", "nonlinear"], "linear"),
            AnachronyPlan  = Cap(parsed.Temporal?.AnachronyPlan, 1000),
            ResolutionMode = NormalizeChoice(parsed.Resolution?.Mode, ["external", "unresolved", "mixed"], "external"),
            ResolutionNote = Cap(parsed.Resolution?.Note, 500),
            MoralPolarity  = NormalizeChoice(parsed.Moral?.Polarity, ["ambivalent", "clear"], "ambivalent"),
            MoralPolarityNote = Cap(parsed.Moral?.Note, 500),
            EscalationCurveJson = JsonSerializer.Serialize(parsed.EscalationCurve ?? []),
            EventTypePaletteJson = JsonSerializer.Serialize(parsed.Events ?? []),
            FormDevice  = Cap(parsed.FormDevice, 200),
            EndingStyle = NormalizeChoice(parsed.Ending?.Style, ["avalanche", "quiet"], "avalanche"),
            NoEpilogue  = parsed.Ending?.NoEpilogue ?? true,
            EndingNote  = Cap(parsed.Ending?.Note, 500),
            IntertextualAnchorsJson = JsonSerializer.Serialize(parsed.IntertextualAnchors ?? []),
            Granularity = granularity,
            GeneratedBy = retrofit ? "retrofit" : "llm",
        };
        db.NodeStructuralBlueprints.Add(blueprint);

        // Beat tags: subplot carriers, anachrony cut, anchor touch-points.
        // Chapter granularity: a unit index resolves to that chapter's FIRST beat.
        foreach (var tag in BuildBeatTags(parsed, units, blueprint.Id))
            db.NodeStructuralBlueprintBeatTags.Add(tag);

        // Per-beat blueprint decisions (Track B): one row per beat, replacing the
        // JSON blob columns with queryable, verifiable rows. Remove any prior rows
        // for beats in this node before inserting (same lifecycle as the blueprint row).
        var beatIds = units.SelectMany(u => u.Beats.Select(b => b.Beat.Id)).ToHashSet();
        var priorDecisions = await db.BeatBlueprintDecisions
            .Where(d => beatIds.Contains(d.BeatId))
            .ToListAsync(ct);
        if (priorDecisions.Count > 0)
            db.BeatBlueprintDecisions.RemoveRange(priorDecisions);

        foreach (var decision in BuildBeatDecisions(parsed, units, blueprint.Id))
            db.BeatBlueprintDecisions.Add(decision);

        await db.SaveChangesAsync(ct);
        log.LogInformation("[blueprint] Saved for {Title}: subplot={HasSubplot}, temporal={Scheme}, resolution={Res}, ending={End}, decisions={Decisions}",
            node.Title, blueprint.HasSubplot, blueprint.TemporalScheme, blueprint.ResolutionMode, blueprint.EndingStyle, units.Count);
        return blueprint;
    }

    private static IEnumerable<BeatBlueprintDecision> BuildBeatDecisions(
        BlueprintResponse parsed, List<StructuralUnit> units, Guid blueprintId)
    {
        var subplotIndexes = (parsed.Subplot?.BeatIndexes ?? []).ToHashSet();
        var anachronyCut   = parsed.Temporal?.CutBeatIndex;
        var eventByIndex   = (parsed.Events ?? []).ToDictionary(e => e.BeatIndex, e => e);
        var curve          = parsed.EscalationCurve ?? [];

        for (int i = 0; i < units.Count; i++)
        {
            var beatId   = units[i].Beats[0].Beat.Id;
            var beatDesc = units[i].Beats[0].Beat.Description;

            eventByIndex.TryGetValue(i, out var evt);

            yield return new BeatBlueprintDecision
            {
                BeatId          = beatId,
                BlueprintId     = blueprintId,
                EventType       = evt?.EventType,
                EscalationFloor = i < curve.Count ? (decimal?)curve[i] : null,
                SubplotCarrier  = subplotIndexes.Contains(i),
                // AnachronyType is nvarchar(40) — a short label, not the plan. The full
                // anachronyPlan prose lives on the beat tag Note (BuildBeatTags) instead.
                AnachronyType   = (i == anachronyCut) ? Truncate(parsed.Temporal?.Scheme ?? "Flashback", 40) : null,
                DeclaredPurpose = beatDesc,  // seeded from existing Description; author refines via set_beat_blueprint
                CreatedAt       = DateTime.UtcNow,
                UpdatedAt       = DateTime.UtcNow,
            };
        }
    }

    private static IEnumerable<NodeStructuralBlueprintBeatTag> BuildBeatTags(
        BlueprintResponse parsed, List<StructuralUnit> units, Guid blueprintId)
    {
        Guid? BeatIdAt(int index) =>
            index >= 0 && index < units.Count ? units[index].Beats[0].Beat.Id : null;

        foreach (var idx in parsed.Subplot?.BeatIndexes ?? [])
            if (BeatIdAt(idx) is { } id)
                yield return new NodeStructuralBlueprintBeatTag
                {
                    BlueprintId = blueprintId, BeatId = id, TagType = "subplot",
                    Note = parsed.Subplot?.Summary is { Length: > 0 } s ? $"Carries the B-story: {Truncate(s, 200)}" : "Carries the B-story",
                };

        if (parsed.Temporal?.CutBeatIndex is { } cut && BeatIdAt(cut) is { } cutId)
            yield return new NodeStructuralBlueprintBeatTag
            {
                BlueprintId = blueprintId, BeatId = cutId, TagType = "anachrony-cut",
                Note = parsed.Temporal?.AnachronyPlan is { Length: > 0 } p ? Truncate(p, 400) : null,
            };

        foreach (var anchor in parsed.IntertextualAnchors ?? [])
            if (anchor.BeatIndex is { } bi && BeatIdAt(bi) is { } anchorBeatId)
                yield return new NodeStructuralBlueprintBeatTag
                {
                    BlueprintId = blueprintId, BeatId = anchorBeatId, TagType = "intertextual-touchpoint",
                    Note = $"{anchor.Name}: {Truncate(anchor.HowReferenced ?? "", 300)}",
                };
    }

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max];

    private static string NormalizeChoice(string? value, string[] allowed, string fallback)
    {
        var v = value?.Trim().ToLowerInvariant() ?? "";
        return allowed.Contains(v) ? v : fallback;
    }

    // ── Per-beat injection (Component 2) ──────────────────────────────────

    /// <summary>
    /// The blueprint slice relevant to one beat, formatted for the generation
    /// prompt. Returns "" when the node has no blueprint — never blocks writing.
    /// </summary>
    public async Task<string> BuildBeatInjectionAsync(
        Guid nodeId, Guid beatId, int beatIndex, int totalBeats, CancellationToken ct = default)
    {
        var blueprint = await GetAsync(nodeId, ct);
        if (blueprint == null)
        {
            log.LogWarning("[blueprint] No structural blueprint for node {NodeId} — StoryScope anti-tell layer inactive. Run 'ss --generate-blueprint --slug <slug>'.", nodeId);
            return "";
        }

        // Chapter-granular blueprints index chapters, not beats — map this beat to
        // its unit so the curve/palette lookups read the right entry.
        var unitIndex = beatIndex;
        var totalUnits = totalBeats;
        var unitLabel = "beat";
        if (blueprint.Granularity == "chapter")
        {
            var ordered = await workbench.GetOrderedBeatsAsync(nodeId, ct);
            var (_, units) = GroupUnits(ordered, forceChapter: true);
            var containing = units.FirstOrDefault(u => u.Beats.Any(b => b.Beat.Id == beatId));
            if (containing == null) return "";  // new beat not yet in snapshot — don't inject from wrong chapter
            unitIndex = containing.Index;
            totalUnits = units.Count;
            unitLabel = "chapter";
        }

        var lines = new List<string>
        {
            "[STRUCTURAL BLUEPRINT — this story's pre-committed anti-tell decisions]"
        };

        // Per-beat tags
        foreach (var tag in blueprint.BeatTags.Where(t => t.BeatId == beatId))
        {
            var line = tag.TagType switch
            {
                "subplot" => $"SUBPLOT: {tag.Note ?? "This beat carries the B-story."} The B-story must echo — not restate — the A-plot's thematic question.",
                "anachrony-cut" => $"TEMPORAL CUT: {tag.Note ?? "This beat breaks chronology."} Land the cut cleanly; do not caption it for the reader.",
                "intertextual-touchpoint" => $"INTERTEXTUAL ANCHOR: {tag.Note ?? "Reference the committed in-world work here."} Named, specific, in-voice.",
                _ => null,
            };
            if (line != null) lines.Add(line);
        }

        // Escalation floor from the curve
        var curve = TryDeserialize<List<int>>(blueprint.EscalationCurveJson);
        if (curve is { Count: > 0 } && unitIndex < curve.Count)
        {
            var target = curve[unitIndex];
            if (unitIndex > 0 && unitIndex - 1 < curve.Count)
            {
                var prev = curve[unitIndex - 1];
                lines.Add($"ESCALATION: this {unitLabel}'s stakes target is {target}/10 (previous {unitLabel}: {prev}/10). " +
                          "It must feel more costly or more irreversible than what came before — flat escalation is the strongest measurable AI-fiction signal. " +
                          "Escalation is FELT COST — what a choice destroys, forecloses, or makes permanent — never physical danger the premise doesn't authorize. " +
                          "Do not invent peril, deadlines, or threats to life to hit the number; deepen what the events already cost the people in them.");
            }
            else
            {
                lines.Add($"ESCALATION: this {unitLabel}'s stakes target is {target}/10.");
            }
        }

        // Event type + revelation mode from the palette
        var palette = TryDeserialize<List<EventPaletteEntry>>(blueprint.EventTypePaletteJson);
        var entry = palette?.FirstOrDefault(e => e.BeatIndex == unitIndex);
        if (entry != null)
        {
            var prevTypes = palette!
                .Where(e => e.BeatIndex == unitIndex - 1 || e.BeatIndex == unitIndex - 2)
                .Select(e => e.EventType)
                .Where(t => !string.IsNullOrEmpty(t))
                .ToList();
            var prevNote = prevTypes.Count > 0 ? $" (recent {unitLabel}s were: {string.Join(", ", prevTypes)} — do not repeat)" : "";
            lines.Add($"EVENT TYPE: this {unitLabel} is a {entry.EventType?.ToUpperInvariant()}{prevNote}.");
            if (!string.IsNullOrEmpty(entry.RevelationMode) && entry.RevelationMode != "none")
                lines.Add($"INFORMATION DYNAMICS: {entry.RevelationMode} — " + entry.RevelationMode switch
                {
                    "suspense"  => "the reader knows something a character doesn't; let that gap do the work.",
                    "curiosity" => "show the effect, withhold the cause; the missing 'why' is the hook.",
                    "surprise"  => "a sudden disclosure lands here; it must recontextualize, not just startle.",
                    _ => "",
                });
        }

        // Ending guidance on the final ~15% of units
        if (totalUnits > 0 && unitIndex >= totalUnits * 0.85)
        {
            var epilogue = blueprint.NoEpilogue
                ? " No epilogue, no retrospective narration of what it all meant — end on the last event itself."
                : "";
            lines.Add($"ENDING ({blueprint.EndingStyle.ToUpperInvariant()}): " + (blueprint.EndingStyle == "avalanche"
                ? "multiple consequences land in the close; do not resolve them one at a time into quiet."
                : "a deliberate quiet ending — chosen, not defaulted.") + epilogue);
            lines.Add($"RESOLUTION MODE ({blueprint.ResolutionMode.ToUpperInvariant()}): " + blueprint.ResolutionMode switch
            {
                "external"   => "the outcome is decided by an outside force or another character — not by the protagonist achieving internal peace.",
                "unresolved" => "the central question stays open on the page. Do not close it as a courtesy to the reader.",
                "mixed"      => "part of the situation resolves externally; part stays open. No internal-understanding exit.",
                _ => "",
            });
            if (blueprint.ResolutionNote is { Length: > 0 })
                lines.Add($"RESOLUTION NOTE: {blueprint.ResolutionNote}");
        }

        // Moral polarity is story-wide; remind mid-story and at the end.
        if (blueprint.MoralPolarity == "ambivalent" && (unitIndex >= totalUnits * 0.5))
            lines.Add("MORAL POLARITY: ambivalent — the protagonist's choices carry genuine cost on the path not taken. Do not resolve who was right.");

        // Promoted consensus clichés (FlagCount >= 2): devices LLMs converge on in this
        // universe, corroborated across 2+ stories by audits. Blocked at write time.
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            var blockedDevices = await db.ConsensusCliches.AsNoTracking()
                .Where(c => c.UniverseId == blueprint.UniverseId && c.FlagCount >= 2)
                .OrderByDescending(c => c.FlagCount)
                .Take(8)
                .Select(c => c.Device)
                .ToListAsync(ct);
            if (blockedDevices.Count > 0)
            {
                lines.Add("CONSENSUS CLICHÉS — these devices recur across this universe's stories because " +
                          "models converge on them; do NOT reach for them here:");
                lines.AddRange(blockedDevices.Select(d => $"• {d}"));
            }
        }
        catch { /* non-blocking */ }

        return lines.Count > 1 ? string.Join("\n", lines) : "";
    }

    private static T? TryDeserialize<T>(string json) where T : class
    {
        try { return JsonSerializer.Deserialize<T>(json, JsonOpts); }
        catch { return null; }
    }

    // ── Prompts ───────────────────────────────────────────────────────────

    private static string BuildSystemPrompt(bool retrofit, string granularity) => $$"""
        You are a story architect making STRUCTURAL decisions {{(retrofit ? "by reading a finished story and inferring the structure it actually has" : "BEFORE any prose is written")}}.
        {{(granularity == "chapter" ? "GRANULARITY: this is a book-scale work — every per-unit decision below (beatIndexes, cutBeatIndex, escalationCurve, events, anchor beatIndex) indexes CHAPTERS (0-based, in reading order), not individual beats." : "")}}

        These decisions counter measurable AI-fiction tells (StoryScope, UMD/Google DeepMind 2025 —
        61,608 stories; narrative-structure classifiers detect AI fiction at 93.2% without reading
        a word of prose). The tells are decisions, not sentences. Your job is to make the decisions
        a human author would make.

        STEP 0 — OUTLIER SEEKING (do this first, in your head): list the three most likely/obvious
        ways this premise plays out and resolves. Those three are FORBIDDEN. Design from what
        remains. Human stories are the statistical outlier among alternatives 57.8% of the time;
        the obvious version is the machine version.

        Then decide, with a 1-2 sentence justification each:

        1. SUBPLOT — a thematically-PARALLEL B-story (echoes the A-plot's question in a different
           key; not a random side quest). Name which beats carry it (0-based indexes). Humans use
           subplots in 43% of stories; AI in 21%. If the story is genuinely too short for one,
           say so — a forced subplot is worse than none.
        2. TEMPORAL SCHEME — linear, frame, or nonlinear. If frame/nonlinear, say where the cut
           lands (0-based beat index) and what it withholds. Don't force one; but know that pure
           first-clue-to-reveal chronology is the AI default.
        3. RESOLUTION MODE — external (an outside force or other character decides), unresolved,
           or mixed. NEVER "the protagonist achieves internal peace/understanding" — that exit
           appears in 47% of AI stories vs 27% of human ones.
        4. MORAL POLARITY — ambivalent is the default (59% of human stories; 38% of AI). Choose
           "clear" only if the premise demands it, and say why.
        5. ESCALATION CURVE — one intensity integer 1-10 per beat, in beat order. Non-decreasing
           act-over-act to the climax; never three identical values in a row. Flat escalation is
           the single strongest AI fingerprint measured. Intensity measures the COST and
           irreversibility of what happens to the people in the beat — not physical danger.
           If the bible's premise or narrative locks exclude danger-to-life, the curve must be
           achievable through social, moral, and material cost alone.
        6. EVENT-TYPE PALETTE — assign each beat one event type (confrontation, discovery, chase,
           confession, ceremony, negotiation, ambush, loss, betrayal, escape, vigil, exchange,
           repair, arrival, departure — or a better word of your own). No type twice in a row.
           Also assign each beat a revelationMode: suspense | curiosity | surprise | none —
           at most half the beats may be "none". HARD OVERRIDE: if the bible's narrative locks
           declare that nothing is hidden / nothing is revealed / no reveal recontextualizes,
           then "surprise" and "suspense" are FORBIDDEN, "revelation" is not a valid event type,
           and the half-"none" cap does not apply — use "curiosity" (effect shown, cause
           withheld) or "none" only. The bible's locks outrank every statistical target above.
        7. FORM DEVICE (optional) — one formal/structural originality choice: document interleave,
           dual timeline, epistolary fragment, second-person interlude, inventory-as-narrative,
           or null for conventional form. Professional writers show formal originality 64% of the
           time; LLMs 0-8%. Only commit to one the story can sustain.
        8. ENDING — avalanche (multiple consequences land in the final beats) or quiet (must be
           justified). noEpilogue: true unless the premise demands a coda. End on the last event,
           not on narration about its significance.
        9. INTERTEXTUAL ANCHORS — pick 3-5 from the CANDIDATE ANCHORS list (named in-world works,
           documents, broadcasts). For each, say how the prose references it (a character quotes
           it, it plays in a scene, it's cited in a report) and optionally which beat (0-based,
           in the beatIndex field ONLY — never write beat numbers inside howReferenced prose;
           readers count beats 1-based and embedded numbers drift).
           Named specific references are a human marker (47% vs 24%).

        Return STRICT JSON only, no markdown fence, matching:
        {
          "subplot": { "summary": "...", "thematicParallel": "...", "beatIndexes": [2,5,9] } | null,
          "temporal": { "scheme": "linear|frame|nonlinear", "anachronyPlan": "..."|null, "cutBeatIndex": 3|null },
          "resolution": { "mode": "external|unresolved|mixed", "note": "..." },
          "moral": { "polarity": "ambivalent|clear", "note": "..." },
          "escalationCurve": [3,4,4,5,6,7,7,8,9,10,10,6],
          "events": [ { "beatIndex": 0, "eventType": "arrival", "revelationMode": "curiosity" }, ... ],
          "formDevice": "..."|null,
          "ending": { "style": "avalanche|quiet", "noEpilogue": true, "note": "..." },
          "intertextualAnchors": [ { "entityId": "guid"|null, "name": "...", "entityType": "...", "howReferenced": "...", "beatIndex": 4|null } ]
        }
        """;

    private static string BuildUserPrompt(
        Node node, List<StructuralUnit> units, string granularity,
        IReadOnlyList<EmbeddingHit> anchorCandidates, bool retrofit)
    {
        var unitLabel = granularity == "chapter" ? "CHAPTER" : "Beat";
        var parts = new List<string>
        {
            $"STORY: {node.Title}",
            node.Seed is { Length: > 0 } ? $"SEED: {node.Seed}" : "",
            $"{unitLabel.ToUpperInvariant()} COUNT: {units.Count} (0-based indexes 0..{units.Count - 1})",
            "",
        };

        if (!string.IsNullOrWhiteSpace(node.NodeBible))
        {
            parts.Add("NODE BIBLE:");
            parts.Add(ClampText(node.NodeBible, 12000));
            parts.Add("");
        }

        // Per-unit text budget: keep the whole prompt bounded regardless of scale.
        var perUnitClamp = granularity == "chapter"
            ? Math.Clamp(60000 / Math.Max(units.Count, 1), 800, 2400)
            : (retrofit ? 1200 : 400);

        if (retrofit)
        {
            parts.Add($"WRITTEN {unitLabel.ToUpperInvariant()}S (infer the structure the story actually has; where a decision was never made, propose the one that best fits what's on the page):");
            foreach (var u in units)
            {
                var text = granularity == "chapter"
                    ? HeadAndTail(string.Join("\n\n", u.Beats.Select(b => b.Beat.Text ?? "")), perUnitClamp)
                    : ClampText(u.Beats[0].Beat.Text ?? u.Beats[0].Beat.Description ?? "", perUnitClamp);
                parts.Add($"--- {unitLabel} {u.Index}{(TitleOf(u) is { Length: > 0 } t ? $" ({t})" : "")} ---");
                parts.Add(text);
            }
        }
        else
        {
            parts.Add($"{unitLabel.ToUpperInvariant()} SPINE (planned synopses — prose does not exist yet):");
            foreach (var u in units)
                parts.Add($"{u.Index}. {(TitleOf(u) is { Length: > 0 } t ? $"[{t}] " : "")}{ClampText(u.Beats[0].Beat.Description ?? u.Beats[0].Beat.Text ?? "(no synopsis)", perUnitClamp)}");
        }
        parts.Add("");

        if (anchorCandidates.Count > 0)
        {
            parts.Add("CANDIDATE ANCHORS (named in-world works/documents from the canon DB — pick 3-5):");
            foreach (var hit in anchorCandidates)
                parts.Add($"- {hit.EntityName} ({hit.EntityType}, id {hit.EntityId})");
        }
        else
        {
            parts.Add("CANDIDATE ANCHORS: none found — propose intertextualAnchors with entityId null, naming plausible in-world works consistent with the bible.");
        }
        parts.Add("");
        parts.Add("Make the structural decisions now. STRICT JSON only.");

        return string.Join("\n", parts.Where(p => p != null));
    }

    private static string ClampText(string text, int max) =>
        text.Length <= max ? text : text[..max] + " …[clamped]";

    /// <summary>Chapter clamp keeps head AND tail so chapter endings register.</summary>
    private static string HeadAndTail(string text, int max)
    {
        if (text.Length <= max) return text;
        var head = (int)(max * 0.65);
        var tailLen = max - head;
        return text[..head] + "\n…[chapter middle elided]…\n" + text[^tailLen..];
    }

    private static string? TitleOf(StructuralUnit u) =>
        u.Title ?? u.Beats[0].Beat.Title;

    // ── Response parsing ──────────────────────────────────────────────────

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    internal static BlueprintResponse ParseResponse(string raw, int beatCount)
    {
        // Strip a markdown fence if the model added one despite instructions.
        var text = raw.Trim();
        if (text.StartsWith("```"))
        {
            var firstNewline = text.IndexOf('\n');
            var lastFence = text.LastIndexOf("```", StringComparison.Ordinal);
            if (firstNewline >= 0 && lastFence > firstNewline)
                text = text[(firstNewline + 1)..lastFence].Trim();
        }

        // The model sometimes emits reasoning fragments with braces, or several JSON
        // objects, around the real payload — first-brace..last-brace gluing produces
        // invalid JSON. Scan for COMPLETE balanced top-level objects (string-aware)
        // and take the largest that deserializes.
        var candidates = ExtractBalancedObjects(text);
        if (candidates.Count == 0)
            throw new InvalidOperationException("Blueprint LLM response contained no complete JSON object.");

        BlueprintResponse? parsed = null;
        string? lastError = null;
        foreach (var candidate in candidates.OrderByDescending(c => c.Length))
        {
            try
            {
                parsed = JsonSerializer.Deserialize<BlueprintResponse>(candidate, JsonOpts);
                if (parsed != null) break;
            }
            catch (JsonException ex) { lastError = ex.Message; }
        }
        if (parsed == null)
            throw new InvalidOperationException(
                $"No JSON candidate deserialized ({candidates.Count} found; likely truncated response): {lastError}");

        // Clamp the escalation curve to the beat count; pad by repeating the last value.
        if (parsed.EscalationCurve is { Count: > 0 } curve)
        {
            if (curve.Count > beatCount) parsed.EscalationCurve = curve.Take(beatCount).ToList();
            else while (parsed.EscalationCurve.Count < beatCount)
                parsed.EscalationCurve.Add(parsed.EscalationCurve[^1]);
        }

        return parsed;
    }

    /// <summary>All complete top-level {...} spans in the text, string-aware
    /// (braces inside JSON strings don't count). Incomplete trailing objects
    /// (truncation) are simply not returned.</summary>
    internal static List<string> ExtractBalancedObjects(string text)
    {
        var results = new List<string>();
        int i = 0;
        while (i < text.Length)
        {
            if (text[i] != '{') { i++; continue; }
            int depth = 0, start = i;
            bool inString = false, escaped = false, closed = false;
            int j = i;
            for (; j < text.Length; j++)
            {
                var ch = text[j];
                if (escaped) { escaped = false; continue; }
                if (ch == '\\' && inString) { escaped = true; continue; }
                if (ch == '"') { inString = !inString; continue; }
                if (inString) continue;
                if (ch == '{') depth++;
                else if (ch == '}')
                {
                    depth--;
                    if (depth == 0) { results.Add(text[start..(j + 1)]); closed = true; break; }
                }
            }
            i = closed ? j + 1 : start + 1;
        }
        return results;
    }

    // ── Response DTOs (internal for unit tests) ───────────────────────────

    internal sealed class BlueprintResponse
    {
        public SubplotPlan? Subplot { get; set; }
        public TemporalPlan? Temporal { get; set; }
        public ResolutionPlan? Resolution { get; set; }
        public MoralPlan? Moral { get; set; }
        public List<int>? EscalationCurve { get; set; }
        public List<EventPaletteEntry>? Events { get; set; }
        public string? FormDevice { get; set; }
        public EndingPlan? Ending { get; set; }
        public List<AnchorPlan>? IntertextualAnchors { get; set; }
    }

    internal sealed class SubplotPlan
    {
        public string? Summary { get; set; }
        public string? ThematicParallel { get; set; }
        public List<int>? BeatIndexes { get; set; }
    }

    internal sealed class TemporalPlan
    {
        public string? Scheme { get; set; }
        public string? AnachronyPlan { get; set; }
        public int? CutBeatIndex { get; set; }
    }

    internal sealed class ResolutionPlan
    {
        public string? Mode { get; set; }
        public string? Note { get; set; }
    }

    internal sealed class MoralPlan
    {
        public string? Polarity { get; set; }
        public string? Note { get; set; }
    }

    internal sealed class EndingPlan
    {
        public string? Style { get; set; }
        public bool? NoEpilogue { get; set; }
        public string? Note { get; set; }
    }

    internal sealed class AnchorPlan
    {
        public Guid? EntityId { get; set; }
        public string? Name { get; set; }
        public string? EntityType { get; set; }
        public string? HowReferenced { get; set; }
        public int? BeatIndex { get; set; }
    }

    public sealed class EventPaletteEntry
    {
        public int BeatIndex { get; set; }
        public string? EventType { get; set; }
        public string? RevelationMode { get; set; }
    }
}
