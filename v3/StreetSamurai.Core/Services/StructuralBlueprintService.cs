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
            anchorCandidates = await embeddings.FindSimilarAsync(anchorQuery, k: 8, AnchorEntityTypes, ct);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "[blueprint] anchor candidate lookup failed for {NodeId} — continuing without", nodeId);
        }

        var system = BuildSystemPrompt(retrofit);
        var user   = await BuildUserPromptAsync(db, node, beats, anchorCandidates, retrofit, ct);

        log.LogInformation("[blueprint] Generating ({Mode}) for node {Title} — {Beats} beats",
            retrofit ? "retrofit" : "pre-prose", node.Title, beats.Count);

        var raw = await llm.GenerateAsync(system, user, temperature: 0.8, maxTokens: 4096, ct: ct);
        var parsed = ParseResponse(raw, beats.Count);

        // Replace any existing blueprint (cascade removes beat tags).
        var existing = await db.NodeStructuralBlueprints
            .Where(b => b.NodeId == nodeId)
            .ToListAsync(ct);
        if (existing.Count > 0)
            db.NodeStructuralBlueprints.RemoveRange(existing);

        var blueprint = new NodeStructuralBlueprint
        {
            NodeId       = nodeId,
            UniverseId   = node.UniverseId,
            HasSubplot   = parsed.Subplot?.Summary is { Length: > 0 },
            SubplotSummary = parsed.Subplot?.Summary,
            SubplotTheme   = parsed.Subplot?.ThematicParallel,
            TemporalScheme = NormalizeChoice(parsed.Temporal?.Scheme, ["linear", "frame", "nonlinear"], "linear"),
            AnachronyPlan  = parsed.Temporal?.AnachronyPlan,
            ResolutionMode = NormalizeChoice(parsed.Resolution?.Mode, ["external", "unresolved", "mixed"], "external"),
            ResolutionNote = parsed.Resolution?.Note,
            MoralPolarity  = NormalizeChoice(parsed.Moral?.Polarity, ["ambivalent", "clear"], "ambivalent"),
            MoralPolarityNote = parsed.Moral?.Note,
            EscalationCurveJson = JsonSerializer.Serialize(parsed.EscalationCurve ?? []),
            EventTypePaletteJson = JsonSerializer.Serialize(parsed.Events ?? []),
            FormDevice  = string.IsNullOrWhiteSpace(parsed.FormDevice) ? null : parsed.FormDevice,
            EndingStyle = NormalizeChoice(parsed.Ending?.Style, ["avalanche", "quiet"], "avalanche"),
            NoEpilogue  = parsed.Ending?.NoEpilogue ?? true,
            EndingNote  = parsed.Ending?.Note,
            IntertextualAnchorsJson = JsonSerializer.Serialize(parsed.IntertextualAnchors ?? []),
            GeneratedBy = retrofit ? "retrofit" : "llm",
        };
        db.NodeStructuralBlueprints.Add(blueprint);

        // Beat tags: subplot carriers, anachrony cut, anchor touch-points.
        foreach (var tag in BuildBeatTags(parsed, beats, blueprint.Id))
            db.NodeStructuralBlueprintBeatTags.Add(tag);

        await db.SaveChangesAsync(ct);
        log.LogInformation("[blueprint] Saved for {Title}: subplot={HasSubplot}, temporal={Scheme}, resolution={Res}, ending={End}",
            node.Title, blueprint.HasSubplot, blueprint.TemporalScheme, blueprint.ResolutionMode, blueprint.EndingStyle);
        return blueprint;
    }

    private static IEnumerable<NodeStructuralBlueprintBeatTag> BuildBeatTags(
        BlueprintResponse parsed, List<NodeWorkbenchService.OrderedBeat> beats, Guid blueprintId)
    {
        Guid? BeatIdAt(int index) =>
            index >= 0 && index < beats.Count ? beats[index].Beat.Id : null;

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
        if (blueprint == null) return "";

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
        if (curve is { Count: > 0 } && beatIndex < curve.Count)
        {
            var target = curve[beatIndex];
            if (beatIndex > 0 && beatIndex - 1 < curve.Count)
            {
                var prev = curve[beatIndex - 1];
                lines.Add($"ESCALATION: this beat's stakes target is {target}/10 (previous beat: {prev}/10). " +
                          "It must feel larger, more costly, or more irreversible than what came before — flat escalation is the strongest measurable AI-fiction signal.");
            }
            else
            {
                lines.Add($"ESCALATION: this beat's stakes target is {target}/10.");
            }
        }

        // Event type + revelation mode from the palette
        var palette = TryDeserialize<List<EventPaletteEntry>>(blueprint.EventTypePaletteJson);
        var entry = palette?.FirstOrDefault(e => e.BeatIndex == beatIndex);
        if (entry != null)
        {
            var prevTypes = palette!
                .Where(e => e.BeatIndex == beatIndex - 1 || e.BeatIndex == beatIndex - 2)
                .Select(e => e.EventType)
                .Where(t => !string.IsNullOrEmpty(t))
                .ToList();
            var prevNote = prevTypes.Count > 0 ? $" (recent beats were: {string.Join(", ", prevTypes)} — do not repeat)" : "";
            lines.Add($"EVENT TYPE: this beat is a {entry.EventType?.ToUpperInvariant()}{prevNote}.");
            if (!string.IsNullOrEmpty(entry.RevelationMode) && entry.RevelationMode != "none")
                lines.Add($"INFORMATION DYNAMICS: {entry.RevelationMode} — " + entry.RevelationMode switch
                {
                    "suspense"  => "the reader knows something a character doesn't; let that gap do the work.",
                    "curiosity" => "show the effect, withhold the cause; the missing 'why' is the hook.",
                    "surprise"  => "a sudden disclosure lands here; it must recontextualize, not just startle.",
                    _ => "",
                });
        }

        // Ending guidance on the final ~15% of beats
        if (totalBeats > 0 && beatIndex >= totalBeats * 0.85)
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
        if (blueprint.MoralPolarity == "ambivalent" && (beatIndex >= totalBeats * 0.5))
            lines.Add("MORAL POLARITY: ambivalent — the protagonist's choices carry genuine cost on the path not taken. Do not resolve who was right.");

        return lines.Count > 1 ? string.Join("\n", lines) : "";
    }

    private static T? TryDeserialize<T>(string json) where T : class
    {
        try { return JsonSerializer.Deserialize<T>(json, JsonOpts); }
        catch { return null; }
    }

    // ── Prompts ───────────────────────────────────────────────────────────

    private static string BuildSystemPrompt(bool retrofit) => $$"""
        You are a story architect making STRUCTURAL decisions {{(retrofit ? "by reading a finished story and inferring the structure it actually has" : "BEFORE any prose is written")}}.

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
           the single strongest AI fingerprint measured.
        6. EVENT-TYPE PALETTE — assign each beat one event type (confrontation, discovery, chase,
           confession, ceremony, negotiation, ambush, loss, betrayal, escape, vigil, exchange,
           repair, arrival, departure — or a better word of your own). No type twice in a row.
           Also assign each beat a revelationMode: suspense | curiosity | surprise | none —
           at most half the beats may be "none".
        7. FORM DEVICE (optional) — one formal/structural originality choice: document interleave,
           dual timeline, epistolary fragment, second-person interlude, inventory-as-narrative,
           or null for conventional form. Professional writers show formal originality 64% of the
           time; LLMs 0-8%. Only commit to one the story can sustain.
        8. ENDING — avalanche (multiple consequences land in the final beats) or quiet (must be
           justified). noEpilogue: true unless the premise demands a coda. End on the last event,
           not on narration about its significance.
        9. INTERTEXTUAL ANCHORS — pick 3-5 from the CANDIDATE ANCHORS list (named in-world works,
           documents, broadcasts). For each, say how the prose references it (a character quotes
           it, it plays in a scene, it's cited in a report) and optionally which beat (0-based).
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

    private async Task<string> BuildUserPromptAsync(
        StreetSamuraiDbContext db, Node node, List<NodeWorkbenchService.OrderedBeat> beats,
        IReadOnlyList<EmbeddingHit> anchorCandidates, bool retrofit, CancellationToken ct)
    {
        var parts = new List<string>
        {
            $"STORY: {node.Title}",
            node.Seed is { Length: > 0 } ? $"SEED: {node.Seed}" : "",
            $"BEAT COUNT: {beats.Count} (0-based indexes 0..{beats.Count - 1})",
            "",
        };

        if (!string.IsNullOrWhiteSpace(node.NodeBible))
        {
            parts.Add("NODE BIBLE:");
            parts.Add(ClampText(node.NodeBible, 12000));
            parts.Add("");
        }

        if (retrofit)
        {
            parts.Add("WRITTEN BEATS (infer the structure the story actually has; where a decision was never made, propose the one that best fits what's on the page):");
            foreach (var (b, i) in beats.Select((b, i) => (b, i)))
            {
                var text = b.Beat.Text ?? b.Beat.Description ?? "";
                parts.Add($"--- Beat {i}{(b.Beat.Title is { Length: > 0 } t ? $" ({t})" : "")} ---");
                parts.Add(ClampText(text, 1200));
            }
        }
        else
        {
            parts.Add("BEAT SPINE (planned synopses — prose does not exist yet):");
            foreach (var (b, i) in beats.Select((b, i) => (b, i)))
                parts.Add($"{i}. {(b.Beat.Title is { Length: > 0 } t ? $"[{t}] " : "")}{ClampText(b.Beat.Description ?? b.Beat.Text ?? "(no synopsis)", 400)}");
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

        var start = text.IndexOf('{');
        var end   = text.LastIndexOf('}');
        if (start < 0 || end <= start)
            throw new InvalidOperationException("Blueprint LLM response contained no JSON object.");
        text = text[start..(end + 1)];

        var parsed = JsonSerializer.Deserialize<BlueprintResponse>(text, JsonOpts)
            ?? throw new InvalidOperationException("Blueprint JSON deserialized to null.");

        // Clamp the escalation curve to the beat count; pad by repeating the last value.
        if (parsed.EscalationCurve is { Count: > 0 } curve)
        {
            if (curve.Count > beatCount) parsed.EscalationCurve = curve.Take(beatCount).ToList();
            else while (parsed.EscalationCurve.Count < beatCount)
                parsed.EscalationCurve.Add(parsed.EscalationCurve[^1]);
        }

        return parsed;
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
