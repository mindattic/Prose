using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Models;
using StreetSamurai.Core.Models.Canon;

namespace StreetSamurai.Core.Services;

/// <summary>
/// The director — orchestrates fully autonomous story generation.
/// Sits above all other services and runs the complete pipeline:
/// plan → generate → assess → update state → continue → until done.
///
/// World rule: Meridian 88 is dangerous. Every story must include at least one
/// battle/conflict scene to break up the narrative. Random violence can erupt at any time.
///
/// ── WHY ──
/// This is the top-level orchestrator of the autonomous story pipeline. Without it,
/// each service (outline, starter, state, events, knowledge) would need an external
/// caller to wire them together in the correct order. The director encapsulates the
/// entire generation loop so the UI only needs to call SurpriseMeAsync() and wait.
///
/// ── HOW IT CONNECTS ──
/// CALLS: DatabaseService (canon entities), OutlineService (arc planning),
///        AgendaEngine (conflict discovery), StoryStarterService (prose generation),
///        StoryStateService (continuity tracking), EventLogService (event extraction),
///        KnowledgeMapService (POV constraints), FacetService (character facets),
///        WorldGraphService (entity relationships), SemanticIndexService + InferenceService
///        (context enrichment via NarrativeSessionContext).
/// CALLED BY: UI layer (Blazor page) — the single entry point for "generate a story."
/// EMITS: OnProgress events so the UI can show beat-by-beat progress.
///
/// ── WHEN IT RUNS ──
/// On-demand when the user clicks "Surprise Me." Runs once per story generation.
/// Each call produces a complete multi-beat story from cast selection through final prose.
///
/// ── PIPELINE PHASES ──
/// 1. PickCast — select protagonist + supporting characters from canon DB
/// 2. AgendaEngine — discover conflicts from character goals/relationships
/// 3. OutlineService — plan a 3-act arc with ~8 beats, ensure battle beat exists
/// 4. Per-beat loop:
///    a. Build constraints (state, knowledge, events, outline, dialogue voice)
///    b. Generate prose via StoryStarterService (opening or continuation)
///    c. Extract state changes via StoryStateService (LLM-powered)
///    d. Log events via EventLogService
///    e. Sync knowledge map for POV filtering
/// 5. Assemble full text and return AutonomousStory
/// </summary>
public class StoryDirectorService
{
    private readonly ILlmService _llm;
    private readonly DatabaseService _db;
    private readonly WorldGraphService _graph;
    private readonly FacetService _facets;
    private readonly OutlineService _outline;
    private readonly AgendaEngine _agenda;
    private readonly StoryStateService _storyState;
    private readonly EventLogService _eventLog;
    private readonly KnowledgeMapService _knowledge;
    private readonly StoryStarterService _starter;
    private readonly SemanticIndexService _semanticIndex;
    private readonly InferenceService _inference;

    public event Action<DirectorProgress>? OnProgress;

    public StoryDirectorService(
        ILlmService llm, DatabaseService db, WorldGraphService graph,
        FacetService facets, OutlineService outline, AgendaEngine agenda,
        StoryStateService storyState, EventLogService eventLog,
        KnowledgeMapService knowledge, StoryStarterService starter,
        SemanticIndexService semanticIndex, InferenceService inference)
    {
        _llm = llm;
        _db = db;
        _graph = graph;
        _facets = facets;
        _outline = outline;
        _agenda = agenda;
        _storyState = storyState;
        _eventLog = eventLog;
        _knowledge = knowledge;
        _starter = starter;
        _semanticIndex = semanticIndex;
        _inference = inference;
    }

    /// <summary>
    /// Generate a complete autonomous story from scratch. Picks characters,
    /// plans the arc, writes every beat, maintains continuity.
    /// This is the ONLY public entry point — everything else is internal pipeline.
    /// </summary>
    public async Task<AutonomousStory> SurpriseMeAsync(CancellationToken ct = default)
    {
        var story = new AutonomousStory();

        // Phase 1: Pick a protagonist and supporting cast from the canon database.
        // Biased 70/30 toward non-Kyle protagonists for variety.
        Report("Choosing protagonist...");
        var cast = await PickCastAsync(ct);
        story.Protagonist = cast.protagonist;
        story.Characters = cast.all;

        // Phase 2: Generate a premise from character goals
        Report("Finding conflicts...");
        var premises = await _agenda.GenerateScenePremisesAsync(cast.all, ct: ct);
        var premise = premises.FirstOrDefault()?.Premise
            ?? $"{cast.protagonist} receives a contract that forces them to confront something they've been avoiding.";
        story.Premise = premise;

        // Phase 3: Pick a location
        var districts = _db.Districts;
        var location = districts.Count > 0
            ? districts[Random.Shared.Next(districts.Count)].Name
            : "The Shelf";
        story.Location = location;

        // Phase 4: Generate outline with mandatory battle beat.
        // The battle requirement is injected into the premise text so the LLM plans for it.
        // EnsureBattleBeat() is a safety net — if the LLM ignores the instruction, we inject one.
        Report("Architecting story arc...");
        var battlePremise = premise + "\n\nIMPORTANT: Meridian 88 is a dangerous world. This story MUST include at least one battle/combat/violent conflict scene. Random crime and violence can erupt at any time. Include at least one beat specifically tagged for combat.";
        var outline = await _outline.GenerateOutlineAsync(battlePremise, cast.all, location, 8, ct);
        story.Outline = outline;
        story.Title = outline.Title;

        // Ensure at least one beat has combat (inject if the LLM didn't)
        EnsureBattleBeat(outline);

        // Phase 5: Write each beat sequentially. Each beat feeds back into state tracking
        // so the next beat gets accurate constraints (closed-loop generation).
        var projectId = Guid.NewGuid().ToString("N");
        story.ProjectId = projectId;
        // Seed the story state with initial character positions from the world model
        _storyState.InitializeCharacter(projectId, cast.protagonist, location);
        foreach (var c in cast.all.Where(c => c != cast.protagonist))
            _storyState.InitializeCharacter(projectId, c);

        var allText = new List<string>();
        var totalBeats = outline.Acts.SelectMany(a => a.Beats).Count();

        foreach (var act in outline.Acts)
        {
            foreach (var beat in act.Beats)
            {
                ct.ThrowIfCancellationRequested();

                Report($"Writing beat {beat.BeatIndex + 1}/{totalBeats}: {beat.Title}");

                // Build full context from all intelligence services.
                // Each service contributes a different layer of constraints:
                // - storyConstraints: who is where, who is alive, inventory, injuries
                // - knowledgeConstraints: what the POV character knows vs. doesn't
                // - eventContext: recent plot events for continuity
                // - outlineContext: where this beat sits in the arc
                // - dialogueConstraints: speech patterns per character (voice distinction)
                var storyConstraints = _storyState.BuildConstraints(projectId);
                var knowledgeConstraints = _knowledge.BuildPovConstraints(projectId, cast.protagonist);
                var eventContext = _eventLog.BuildRecentContext(projectId);
                var outlineContext = _outline.BuildBeatContext(outline, beat.BeatIndex);
                var dialogueConstraints = BuildDialogueConstraints(beat.CharactersPresent.Count > 0 ? beat.CharactersPresent : cast.all);

                var storySoFar = string.Join("\n\n", allText);
                var paragraphs = allText.ToList();

                // Generate the beat
                var beatGoal = beat.Goal;
                if (!string.IsNullOrEmpty(dialogueConstraints))
                    beatGoal += "\n\n" + dialogueConstraints;

                string beatText;
                if (allText.Count == 0)
                {
                    var opening = await _starter.GenerateOpeningAsync(new StoryStarterRequest
                    {
                        Premise = beatGoal,
                        Characters = beat.CharactersPresent.Count > 0 ? beat.CharactersPresent : cast.all,
                        Location = beat.Location ?? location,
                    }, ct);
                    beatText = opening.Text;
                    if (string.IsNullOrEmpty(story.Title) || story.Title == "Outline generation failed")
                        story.Title = opening.Title;
                }
                else
                {
                    beatText = await _starter.ContinueAsync(
                        paragraphs, beatGoal, null, beat.Location ?? location,
                        beat.CharactersPresent.Count > 0 ? beat.CharactersPresent : cast.all,
                        storyConstraints, knowledgeConstraints, eventContext, outlineContext, ct);
                }

                allText.Add(beatText);

                // Closed-loop feedback: extract what changed in this beat and update all
                // intelligence services. This is what prevents continuity errors — the next
                // beat will know that a character moved, got injured, or learned something.
                // Best-effort: if extraction fails, generation continues with stale state
                // rather than crashing the entire story.
                try
                {
                    await _storyState.UpdateFromTextAsync(projectId, beatText, storySoFar + "\n\n" + beatText);
                    await _eventLog.ExtractAndLogAsync(projectId, beatText, beat.BeatIndex, ct);
                    var state = _storyState.GetState(projectId);
                    _knowledge.SyncFromState(projectId, state, _eventLog.GetEvents(projectId), beat.BeatIndex);
                }
                catch { /* Best effort — stale state is better than a crashed story */ }

                _outline.MarkBeatWritten(outline, beat.BeatIndex);

                story.Beats.Add(new GeneratedStoryBeat
                {
                    BeatIndex = beat.BeatIndex,
                    Title = beat.Title,
                    Text = beatText,
                    Act = act.ActNumber,
                });

                Report($"Beat {beat.BeatIndex + 1}/{totalBeats} complete", beat.BeatIndex + 1, totalBeats);
            }
        }

        story.FullText = string.Join("\n\n---\n\n", allText);
        story.Complete = true;
        Report("Story complete!", totalBeats, totalBeats);

        return story;
    }

    /// <summary>Pick a protagonist and 2-4 supporting characters from the canon.</summary>
    private async Task<(string protagonist, List<string> all)> PickCastAsync(CancellationToken ct)
    {
        var allChars = _db.Characters;
        if (allChars.Count == 0)
            return ("Kyle", ["Kyle", "Sable"]);

        // Pick a protagonist — weight toward non-Kyle characters for variety
        var candidates = allChars
            .Where(c => c.Status != "dead" && !string.IsNullOrWhiteSpace(c.Description))
            .ToList();

        if (candidates.Count == 0)
            return ("Kyle", ["Kyle", "Sable"]);

        // 70% chance of non-Kyle protagonist for variety
        CharacterData protag;
        if (Random.Shared.NextDouble() < 0.7)
        {
            var nonKyle = candidates.Where(c => c.Name != "Kyle").OrderBy(_ => Random.Shared.Next()).FirstOrDefault();
            protag = nonKyle ?? candidates[Random.Shared.Next(candidates.Count)];
        }
        else
        {
            protag = candidates[Random.Shared.Next(candidates.Count)];
        }

        // Pick 2-3 supporting characters with existing relationships to protagonist
        var supporting = new List<string>();
        foreach (var rel in protag.Relationships.Take(3))
        {
            var relChar = candidates.FirstOrDefault(c => c.Name == rel.Name);
            if (relChar != null) supporting.Add(relChar.Name);
        }

        // Fill remaining slots randomly
        while (supporting.Count < 2)
        {
            var random = candidates
                .Where(c => c.Name != protag.Name && !supporting.Contains(c.Name))
                .OrderBy(_ => Random.Shared.Next())
                .FirstOrDefault();
            if (random == null) break;
            supporting.Add(random.Name);
        }

        var all = new List<string> { protag.Name };
        all.AddRange(supporting);
        return (protag.Name, all);
    }

    /// <summary>
    /// Ensure at least one beat in the outline is a combat/battle scene.
    /// Safety net for when the LLM ignores the battle instruction in the premise.
    /// Injects combat into the midpoint of Act 2 (classical dramatic structure).
    /// </summary>
    private static void EnsureBattleBeat(StoryOutline outline)
    {
        var allBeats = outline.Acts.SelectMany(a => a.Beats).ToList();
        var hasBattle = allBeats.Any(b =>
            b.Goal.Contains("fight", StringComparison.OrdinalIgnoreCase) ||
            b.Goal.Contains("combat", StringComparison.OrdinalIgnoreCase) ||
            b.Goal.Contains("battle", StringComparison.OrdinalIgnoreCase) ||
            b.Goal.Contains("attack", StringComparison.OrdinalIgnoreCase) ||
            b.Goal.Contains("violence", StringComparison.OrdinalIgnoreCase) ||
            b.Title.Contains("fight", StringComparison.OrdinalIgnoreCase) ||
            b.Title.Contains("battle", StringComparison.OrdinalIgnoreCase));

        if (hasBattle || allBeats.Count < 3) return;

        // Inject a battle beat in the middle of Act 2
        var act2 = outline.Acts.FirstOrDefault(a => a.ActNumber == 2) ?? outline.Acts.Last();
        var midpoint = act2.Beats.Count / 2;
        if (midpoint < act2.Beats.Count)
        {
            var beat = act2.Beats[midpoint];
            beat.Goal = "BATTLE: " + beat.Goal + ". Violence erupts — Meridian 88 is a dangerous place. The conflict should feel sudden, visceral, and have consequences for the characters involved.";
            beat.Tension = Math.Max(beat.Tension, 8);
            beat.FacetHint = "id";
        }
    }

    /// <summary>
    /// Build dialogue voice constraints for multi-character scenes.
    /// Pulls speech patterns (cadence, vocabulary, example lines) from the canon DB
    /// and formats them as hard constraints so the LLM gives each character a distinct voice.
    /// </summary>
    private string BuildDialogueConstraints(List<string> charactersInScene)
    {
        if (charactersInScene.Count < 2) return "";

        var lines = new List<string> { "DIALOGUE VOICE DISTINCTION — each character must sound DIFFERENT:" };
        foreach (var name in charactersInScene)
        {
            var c = _db.FindCharacter(name);
            if (c == null) continue;
            var sp = c.SpeechPatterns;
            if (sp.Vocabulary.Length > 0 || sp.Cadence.Length > 0)
            {
                var voice = $"  {name}: ";
                if (sp.Cadence.Length > 0) voice += sp.Cadence + " ";
                if (sp.Vocabulary.Length > 0) voice += sp.Vocabulary;
                if (sp.ExampleLines.Count > 0) voice += $" Example: \"{sp.ExampleLines[0]}\"";
                lines.Add(voice);
            }
        }

        if (lines.Count <= 1) return "";
        lines.Add("  These characters must NEVER sound alike. Their voices are their identity.");
        return string.Join("\n", lines);
    }

    private void Report(string message, int current = 0, int total = 0) =>
        OnProgress?.Invoke(new DirectorProgress { Message = message, CurrentBeat = current, TotalBeats = total });
}

public class DirectorProgress
{
    public string Message { get; init; } = "";
    public int CurrentBeat { get; init; }
    public int TotalBeats { get; init; }
}

public class AutonomousStory
{
    public string ProjectId { get; set; } = "";
    public string Title { get; set; } = "";
    public string Protagonist { get; set; } = "";
    public List<string> Characters { get; set; } = [];
    public string Premise { get; set; } = "";
    public string Location { get; set; } = "";
    public StoryOutline? Outline { get; set; }
    public List<GeneratedStoryBeat> Beats { get; set; } = [];
    public string FullText { get; set; } = "";
    public bool Complete { get; set; }
}

public class GeneratedStoryBeat
{
    public int BeatIndex { get; set; }
    public string Title { get; set; } = "";
    public string Text { get; set; } = "";
    public int Act { get; set; }
}
