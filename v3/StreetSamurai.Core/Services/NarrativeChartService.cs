using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Generates XKCD-style narrative chart data for a book node.
///
/// Inspired by Randall Munroe's "Movie Narrative Charts" — each character is a flowing
/// line through the book's timeline. Lines that are close together (or touching) mean
/// the characters share a scene. Lines that diverge mean characters are separated.
/// Events are labeled at key inflection points.
///
/// At each beat (a temporal cross-section of the book):
///   ONSCREEN characters  — active in the current scene; their lines converge.
///   OFFSCREEN characters — doing things in parallel that build toward their next
///                          emergence into an onscreen moment. Their line flows
///                          in a separate track, labeled with what they're implied to
///                          be preparing.
///
/// The chart data is consumed by:
///   1. The frontend (SVG/Canvas render of character proximity over time).
///   2. The prose generation pipeline — "what are offscreen characters doing right now"
///      can be injected as subtext context (what is happening off-camera that the
///      reader will recognize when those characters reappear).
///   3. Logic-sweep audits — verify that offscreen character continuity is coherent
///      (characters cannot teleport between scenes; travel time must be plausible).
/// </summary>
public class NarrativeChartService(IDbContextFactory<StreetSamuraiDbContext> dbFactory)
{
    // ── Chart data model ──────────────────────────────────────────────────────

    /// <summary>
    /// A character's presence state at a single beat.
    /// </summary>
    public record CharacterPresence(
        string CharacterName,
        bool IsOnscreen,
        string Location,
        /// <summary>What this character is doing when OFFSCREEN — implied preparation for next emergence.</summary>
        string OffscreenActivity,
        /// <summary>Track index — offscreen characters fan out into separate tracks; onscreen converge.</summary>
        int TrackIndex);

    /// <summary>
    /// One labeled event on the narrative chart — a major inflection point in the book.
    /// </summary>
    public record ChartEvent(
        int BeatIndex,
        string Label,
        string EventType, // "death" | "meeting" | "separation" | "revelation" | "confrontation" | "arc-stage"
        IReadOnlyList<string> AffectedCharacters);

    /// <summary>
    /// A single beat's cross-section through the narrative chart.
    /// Shows the state of every tracked character at this moment in book time.
    /// </summary>
    public record BeatCrossSection(
        int BeatIndex,
        string BeatGoal,
        string Location,
        StoryScienceService.ChangeArcStage ArcStage,
        IReadOnlyList<CharacterPresence> Characters,
        IReadOnlyList<ChartEvent> Events);

    /// <summary>
    /// Complete narrative chart data for a book node.
    /// Contains the full timeline of cross-sections — one per beat.
    /// </summary>
    public record NarrativeChart(
        Guid NodeId,
        string NodeTitle,
        IReadOnlyList<BeatCrossSection> Beats,
        IReadOnlyList<string> TrackedCharacters,
        IReadOnlyList<ChartEvent> AllEvents);

    // ── DB query ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Build the full narrative chart for a node.
    /// Reads beats in SortKey order and extracts character presence from:
    ///   1. EntityStateEvents keyed by BeatGuid (character location events logged by the engine)
    ///   2. Beat.Description text analysis (character name mentions as a fallback)
    /// </summary>
    public async Task<NarrativeChart> BuildChartAsync(Guid nodeId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var node = await db.Nodes
            .AsNoTracking()
            .Where(n => n.Id == nodeId)
            .Select(n => new { n.Id, n.Title, n.DefaultLocation })
            .FirstOrDefaultAsync(ct)
            ?? throw new ArgumentException($"Node {nodeId} not found.");

        // SS-A43: beats live on chapter children for book-mode books.
        var childIds = await db.Nodes.AsNoTracking()
            .Where(n => n.ParentNodeId == nodeId)
            .Select(n => n.Id).ToListAsync(ct);
        var beatNodeIds = childIds.Count > 0 ? childIds : new List<Guid> { nodeId };

        // Load beats for this node in book order (via BeatNode join to Beat)
        var beatRows = await db.BeatNodes
            .AsNoTracking()
            .Where(nb => beatNodeIds.Contains(nb.NodeId) && nb.IsEnabled)
            .OrderBy(nb => nb.SortKey)
            .Include(nb => nb.Beat)
            .Select(nb => new
            {
                nb.BeatId,
                nb.SortKey,
                Description = nb.Beat != null ? nb.Beat.Description : null,
                Location = (string?)null, // Beat has no Location field; use node default
            })
            .ToListAsync(ct);

        // Collect all beat IDs for this node to query EntityStateEvents
        var beatIds = beatRows.Select(b => b.BeatId).ToHashSet();

        // Load EntityStateEvents for these beats — character location/presence events.
        // The key events are: AspectKey = "location" | "companion.with" | "present" | "on_screen"
        // Entity must be joined to get the name and type.
        var presenceEventRows = await db.EntityStateEvents
            .AsNoTracking()
            .Where(e => e.BeatGuid != null
                     && beatIds.Contains(e.BeatGuid!.Value)
                     && (e.AspectKey == "location" || e.AspectKey == "companion.with"
                         || e.AspectKey == "on_screen" || e.AspectKey == "present"))
            .Include(e => e.Entity)
            .Select(e => new
            {
                BeatId = e.BeatGuid!.Value,
                EntityId = e.EntityId,
                EntityName = e.Entity != null ? e.Entity.Name : "",
                EntityType = e.Entity != null ? e.Entity.EntityType : "",
                AspectKey = e.AspectKey,
                NewValue = e.NewValue,
            })
            .ToListAsync(ct);

        // Build a lookup: beatId → character names that are "present"
        var presenceByBeat = presenceEventRows
            .Where(e => string.Equals(e.EntityType, "character", StringComparison.OrdinalIgnoreCase)
                     && !string.IsNullOrEmpty(e.EntityName))
            .GroupBy(e => e.BeatId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.EntityName).Distinct(StringComparer.OrdinalIgnoreCase).ToHashSet(StringComparer.OrdinalIgnoreCase));

        // Discover all tracked characters across the whole node
        var allCharacters = presenceEventRows
            .Where(e => string.Equals(e.EntityType, "character", StringComparison.OrdinalIgnoreCase)
                     && !string.IsNullOrEmpty(e.EntityName))
            .Select(e => e.EntityName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n)
            .ToList();

        // If no state events, fall back to name-mention scan of beat descriptions
        if (allCharacters.Count == 0)
            allCharacters = ExtractCharactersFromBeatGoals(beatRows.Select(b => b.Description ?? "").ToList());

        // Bigram extraction misses single-word handles (Kyle, Sasha, Bear).
        // Supplement from the entity DB: character-type entities in this universe
        // whose name (or first name) appears in any beat goal.
        if (allCharacters.Count < 3)
        {
            var universeId = await db.Nodes.AsNoTracking()
                .Where(n => n.Id == nodeId)
                .Select(n => n.UniverseId)
                .FirstOrDefaultAsync(ct);
            var allGoalsLower = string.Join(" ", beatRows.Select(b => b.Description ?? "")).ToLowerInvariant();
            var entityChars = await db.Entities.AsNoTracking()
                .Where(e => e.IsActive && e.UniverseId == universeId
                         && (e.EntityType == "character" || e.EntityType == "person"))
                .Select(e => e.Name)
                .ToListAsync(ct);
            var mentioned = entityChars
                .Where(n => !string.IsNullOrEmpty(n)
                         && allGoalsLower.Contains(n.Split(' ')[0].ToLowerInvariant()))
                .Take(15)
                .ToList();
            allCharacters = allCharacters.Union(mentioned, StringComparer.OrdinalIgnoreCase).ToList();
        }

        // Build chart events and cross-sections
        var chartEvents = new List<ChartEvent>();
        var crossSections = new List<BeatCrossSection>();

        for (var i = 0; i < beatRows.Count; i++)
        {
            var beat = beatRows[i];
            var arcStage = StoryScienceService.ClassifyArcStage(i, beatRows.Count);
            var beatGoal = beat.Description ?? "";
            var location = beat.Location ?? node.DefaultLocation ?? "";

            // Determine which characters are onscreen at this beat
            var onscreenChars = presenceByBeat.TryGetValue(beat.BeatId, out var present)
                ? present
                : InferOnscreenFromBeatGoal(beatGoal, allCharacters);

            // Detect events at this beat
            var beatEvents = DetectEvents(i, beatGoal, onscreenChars, allCharacters, arcStage);
            chartEvents.AddRange(beatEvents);

            // Assign track indices: onscreen chars cluster at track 0, offscreen fan out
            var onscreenList = onscreenChars.ToList();
            var offscreenList = allCharacters.Except(onscreenChars, StringComparer.OrdinalIgnoreCase).ToList();

            var presences = new List<CharacterPresence>();
            for (var t = 0; t < onscreenList.Count; t++)
                presences.Add(new CharacterPresence(onscreenList[t], true, location, "", t));

            for (var t = 0; t < offscreenList.Count; t++)
            {
                var activity = InferOffscreenActivity(offscreenList[t], beatGoal, i, beatRows.Count, new Dictionary<string, string[]>());
                presences.Add(new CharacterPresence(offscreenList[t], false, "", activity, onscreenList.Count + t));
            }

            crossSections.Add(new BeatCrossSection(
                i,
                beatGoal,
                location,
                arcStage,
                presences,
                beatEvents));
        }

        return new NarrativeChart(
            nodeId,
            node.Title,
            crossSections,
            allCharacters,
            chartEvents);
    }

    // ── Offscreen activity inference ──────────────────────────────────────────

    /// <summary>
    /// Builds the implied offscreen activity for a character at a given arc position.
    /// This is the story-science heart of the chart: offscreen characters are NOT idle.
    /// They are doing something that explains their state when they reappear.
    /// The description is injected as subtext context during prose generation.
    /// </summary>
    public static string InferOffscreenActivity(
        string characterName,
        string currentBeatGoal,
        int beatIndex,
        int totalBeats,
        Dictionary<string, string[]> offscreenLibrary)
    {
        if (offscreenLibrary.TryGetValue(characterName, out var activities) && activities.Length > 0)
        {
            var pos = (float)beatIndex / Math.Max(totalBeats - 1, 1);
            var idx = Math.Min((int)(pos * activities.Length), activities.Length - 1);
            return activities[idx];
        }

        // Generic arc-position based inference when no specific library entry exists
        var stage = StoryScienceService.ClassifyArcStage(beatIndex, totalBeats);
        return stage switch
        {
            StoryScienceService.ChangeArcStage.FlawEnthroned    => $"{characterName} is operating in their comfortable pattern — the flaw is working for them.",
            StoryScienceService.ChangeArcStage.IgnitionPoint     => $"{characterName} has felt the same shockwave that hit the protagonist — reacting privately, building toward confrontation.",
            StoryScienceService.ChangeArcStage.FlawTested        => $"{characterName} is maneuvering — they have an agenda in relation to the protagonist's crisis that hasn't surfaced yet.",
            StoryScienceService.ChangeArcStage.MidpointCommitment => $"{characterName} is at their own decision point — what they decide here shapes how they emerge in Act IV.",
            StoryScienceService.ChangeArcStage.WorstCaseRealised => $"{characterName} is either the cause of the protagonist's worst-case or will be present to witness it.",
            StoryScienceService.ChangeArcStage.GodMoment         => $"{characterName} is positioned for the final confrontation — their agenda and the protagonist's arc are about to collide.",
            _                                                     => $"{characterName} is offscreen."
        };
    }

    // ── Onscreen inference from beat goal text ────────────────────────────────

    private static HashSet<string> InferOnscreenFromBeatGoal(string beatGoal, List<string> allCharacters)
    {
        if (string.IsNullOrEmpty(beatGoal) || allCharacters.Count == 0)
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var goalLower = beatGoal.ToLowerInvariant();
        return allCharacters
            .Where(c =>
            {
                var nameLower = c.ToLowerInvariant();
                var firstNameLower = nameLower.Split(' ')[0];
                return goalLower.Contains(nameLower) || goalLower.Contains(firstNameLower);
            })
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    // ── Event detection ───────────────────────────────────────────────────────

    private static List<ChartEvent> DetectEvents(
        int beatIndex,
        string? beatGoal,
        HashSet<string> onscreen,
        List<string> allChars,
        StoryScienceService.ChangeArcStage arcStage)
    {
        var events = new List<ChartEvent>();
        var goal = beatGoal?.ToLowerInvariant() ?? "";

        if (goal.Contains("death") || goal.Contains("dies") || goal.Contains("killed"))
            events.Add(new ChartEvent(beatIndex, "Death", "death", onscreen.ToList()));

        if (goal.Contains("reveal") || goal.Contains("discover") || goal.Contains("truth"))
            events.Add(new ChartEvent(beatIndex, "Revelation", "revelation", onscreen.ToList()));

        if (goal.Contains("confront") || goal.Contains("standoff") || goal.Contains("face"))
            events.Add(new ChartEvent(beatIndex, "Confrontation", "confrontation", onscreen.ToList()));

        if (goal.Contains("separat") || goal.Contains("leaves") || goal.Contains("depart"))
            events.Add(new ChartEvent(beatIndex, "Separation", "separation", onscreen.ToList()));

        // Arc stage transitions are always charted
        var stageLabel = arcStage switch
        {
            StoryScienceService.ChangeArcStage.IgnitionPoint      => "Ignition Point",
            StoryScienceService.ChangeArcStage.MidpointCommitment => "Midpoint",
            StoryScienceService.ChangeArcStage.WorstCaseRealised  => "All Is Lost",
            StoryScienceService.ChangeArcStage.GodMoment          => "God Moment",
            _ => ""
        };
        if (stageLabel.Length > 0)
            events.Add(new ChartEvent(beatIndex, stageLabel, "arc-stage", onscreen.ToList()));

        return events;
    }

    // ── Fallback character extraction from beat goal text ────────────────────

    private static List<string> ExtractCharactersFromBeatGoals(List<string> beatGoals)
    {
        // Simple: collect capitalized multi-word tokens that appear in multiple beats.
        var candidates = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var goal in beatGoals)
        {
            if (string.IsNullOrEmpty(goal)) continue;
            var words = goal.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < words.Length - 1; i++)
            {
                var w1 = words[i].Trim(',', '.', ';');
                var w2 = words[i + 1].Trim(',', '.', ';');
                if (w1.Length > 2 && w2.Length > 2 && char.IsUpper(w1[0]) && char.IsUpper(w2[0]))
                {
                    var candidate = $"{w1} {w2}";
                    candidates[candidate] = candidates.GetValueOrDefault(candidate) + 1;
                }
            }
        }

        return candidates.Where(kv => kv.Value >= 2).Select(kv => kv.Key).Take(10).ToList();
    }

    // ── Context block for prose generation ───────────────────────────────────

    /// <summary>
    /// Returns a formatted offscreen-activity context block for injection into the
    /// prose generation prompt. Tells the LLM what characters NOT in this scene
    /// are doing in parallel — keeping the world continuous and non-idle.
    ///
    /// This is the "narrative chart subtext" feature: even when Sasha isn't onscreen,
    /// the reader (and the prose engine) knows she is preparing something. When she
    /// reappears, the world feels alive rather than summoned from nowhere.
    /// </summary>
    public static string BuildOffscreenContextBlock(BeatCrossSection crossSection)
    {
        var offscreen = crossSection.Characters
            .Where(c => !c.IsOnscreen && !string.IsNullOrEmpty(c.OffscreenActivity))
            .ToList();

        if (offscreen.Count == 0) return "";

        var lines = new List<string>
        {
            "## PARALLEL WORLD — characters offscreen at this beat:",
            "(Not in this scene — but NOT idle. Their activity now explains their state when they return.)"
        };

        foreach (var c in offscreen)
            lines.Add($"• {c.CharacterName}: {c.OffscreenActivity}");

        lines.Add("Do not reference offscreen characters directly unless the POV character has a way to know about them.");
        lines.Add("This context is for the WRITER, not the narrator. Infuse tone and subtext; do not name the offscreen activity.");

        return string.Join("\n", lines);
    }

    /// <summary>
    /// Returns a compact chart summary for display in the workbench UI.
    /// Format: one line per beat showing onscreen vs offscreen character streams.
    /// Matches the XKCD narrative chart model: each beat is a temporal cross-section.
    /// </summary>
    public static string FormatChartSummary(NarrativeChart chart)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"NARRATIVE CHART — {chart.NodeTitle}");
        sb.AppendLine($"{chart.TrackedCharacters.Count} characters tracked across {chart.Beats.Count} beats.");
        sb.AppendLine();

        foreach (var beat in chart.Beats)
        {
            var onscreen = beat.Characters.Where(c => c.IsOnscreen).Select(c => c.CharacterName).ToList();
            var offscreen = beat.Characters.Where(c => !c.IsOnscreen).Select(c => c.CharacterName).ToList();

            var events = beat.Events.Any()
                ? $" [{string.Join(", ", beat.Events.Select(e => e.Label))}]"
                : "";

            sb.AppendLine($"Beat {beat.BeatIndex + 1:D3} [{beat.ArcStage,-20}]{events}");
            sb.AppendLine($"  ONSCREEN:  {(onscreen.Count > 0 ? string.Join(", ", onscreen) : "(no tracked characters)")}");
            if (offscreen.Count > 0)
                sb.AppendLine($"  OFFSCREEN: {string.Join(", ", offscreen)}");
        }

        return sb.ToString();
    }
}
