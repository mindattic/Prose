using System.Net.Http;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Models;
using StreetSamurai.Core.Models.Canon;

namespace StreetSamurai.Core.Services;

/// <summary>
/// The director — orchestrates fully autonomous story generation.
/// Sits above all other services and runs the complete pipeline:
/// plan → generate → assess → update state → continue → until done.
///
/// World rule: GLMZ is dangerous. Every story must include at least one
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
public class StoryDirectorService : IStoryDirectorService
{
    private readonly ILlmService llm;
    private readonly DatabaseService db;
    private readonly WorldGraphService graph;
    private readonly OutlineService outlineSvc;
    private readonly AgendaEngine agenda;
    private readonly StoryStateService storyState;
    private readonly EventLogService eventLog;
    private readonly KnowledgeMapService knowledge;
    private readonly StoryStarterService starter;
    private readonly SemanticIndexService semanticIndex;
    private readonly InferenceService inference;
    private readonly ILogger<StoryDirectorService> log;
    private readonly IPathProvider paths;
    private readonly ConsequenceEngine consequences;
    private readonly ThematicIndexService thematicIndex;
    private readonly BehaviorPredictionService behaviorPredict;
    private readonly DialogueService dialogue;
    private readonly ArcTrackerService arcTracker;
    private readonly ContinuityValidatorService continuityValidator;
    private readonly SuggestionEngineService suggestions;
    private readonly OutlineReviewService outlineReview;
    private readonly StoryQualityService quality;
    private readonly CanonGroundingService canonGrounding;
    private readonly BookOutlineService? bookOutline;
    private readonly IChapterRepository? chapterRepo;

    public event Action<DirectorProgress>? OnProgress;

    public bool IsGenerating { get; private set; }
    public AutonomousStory? CurrentStory { get; private set; }
    public string ProgressMessage { get; private set; } = "";
    public int ProgressCurrent { get; private set; }
    public int ProgressTotal { get; private set; }

    public StoryDirectorService(
        ILlmService llm, DatabaseService db, WorldGraphService graph,
        OutlineService outline, AgendaEngine agenda,
        StoryStateService storyState, EventLogService eventLog,
        KnowledgeMapService knowledge, StoryStarterService starter,
        SemanticIndexService semanticIndex, InferenceService inference,
        ILogger<StoryDirectorService> log, IPathProvider paths,
        ConsequenceEngine consequences, ThematicIndexService thematicIndex,
        BehaviorPredictionService behaviorPredict,
        DialogueService dialogue, ArcTrackerService arcTracker,
        ContinuityValidatorService continuityValidator, SuggestionEngineService suggestions,
        OutlineReviewService outlineReview, StoryQualityService quality,
        CanonGroundingService canonGrounding,
        BookOutlineService? bookOutline = null, IChapterRepository? chapterRepo = null)
    {
        this.llm = llm;
        this.db = db;
        this.graph = graph;
        this.outlineSvc = outline;
        this.agenda = agenda;
        this.storyState = storyState;
        this.eventLog = eventLog;
        this.knowledge = knowledge;
        this.starter = starter;
        this.semanticIndex = semanticIndex;
        this.inference = inference;
        this.log = log;
        this.paths = paths;
        this.consequences = consequences;
        this.thematicIndex = thematicIndex;
        this.behaviorPredict = behaviorPredict;
        this.dialogue = dialogue;
        this.arcTracker = arcTracker;
        this.continuityValidator = continuityValidator;
        this.suggestions = suggestions;
        this.outlineReview = outlineReview;
        this.quality = quality;
        this.canonGrounding = canonGrounding;
        this.bookOutline = bookOutline;
        this.chapterRepo = chapterRepo;
    }

    /// <summary>
    /// Generate a complete autonomous story from scratch. Picks characters,
    /// plans the arc, writes every beat, maintains continuity.
    /// This is the ONLY public entry point — everything else is internal pipeline.
    /// </summary>
    public async Task<AutonomousStory> SurpriseMeAsync(CancellationToken ct = default)
    {
        log.LogInformation("=== SurpriseMeAsync: Starting autonomous story generation ===");
        var story = new AutonomousStory();
        IsGenerating = true;
        CurrentStory = story;

        try
        {
            // Phase 1: Pick a protagonist and supporting cast from the canon database.
            Report("Choosing protagonist...");
            var cast = await PickCastAsync(ct);
            story.Protagonist = cast.protagonist;
            story.Characters = cast.all;
            log.LogInformation("Phase 1 complete: protagonist={Protagonist}, cast=[{Cast}]",
                cast.protagonist, string.Join(", ", cast.all));

            // Phase 2: Generate a premise from character goals
            Report("Finding conflicts...");
            var premises = await agenda.GenerateScenePremisesAsync(cast.all, ct: ct);
            var premise = premises.FirstOrDefault()?.Premise
                ?? $"{cast.protagonist} receives a contract that forces them to confront something they've been avoiding.";
            story.Premise = premise;
            log.LogInformation("Phase 2 complete: {PremiseCount} premises generated", premises.Count);

            // Phase 3: Pick a location
            var districts = db.Districts;
            var location = districts.Count > 0
                ? districts[Random.Shared.Next(districts.Count)].Name
                : "the Gray Zone";
            story.Location = location;

            // Phase 4: Generate outline with mandatory battle beat + world consequences.
            Report("Architecting story arc...");
            var consequenceContext = consequences.BuildConsequenceContext(cast.protagonist);
            var battlePremise = premise + "\n\nIMPORTANT: GLMZ is a dangerous world. This story MUST include at least one battle/combat/violent conflict scene. Random crime and violence can erupt at any time. Include at least one beat specifically tagged for combat."
                + (consequenceContext.Length > 0 ? $"\n\nWORLD CONSEQUENCES FROM PREVIOUS STORIES:\n{consequenceContext}" : "");
            var outline = await outlineSvc.GenerateOutlineAsync(battlePremise, cast.all, location, 8, ct);
            story.Outline = outline;
            story.Title = outline.Title;
            log.LogInformation("Phase 4 complete: outline title={Title}, acts={ActCount}",
                outline.Title, outline.Acts.Count);

            EnsureBattleBeat(outline);

            // Phase 4b: "The other author" — review the outline before a word is written.
            // Catches clichés, enforces moral ambiguity, validates character arcs, checks pacing.
            Report("Reviewing story arc...");
            try
            {
                var reviewResult = await outlineReview.ReviewAsync(outline, ct);
                story.OutlineReview = reviewResult;
                if (reviewResult.RevisedOutline.Acts.Count > 0)
                {
                    outline = reviewResult.RevisedOutline;
                    story.Outline = outline;
                    log.LogInformation("Outline revised by review: moral={Moral}/10, strength={Strength}/10, cliches={Cliches}",
                        reviewResult.MoralAmbiguityScore, reviewResult.NarrativeStrength, reviewResult.ClicheFlags.Count);
                }
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "Outline review failed — proceeding with original outline");
            }

            // Assign project ID and save the checkpoint with outline (before any beats)
            var projectId = Guid.CreateVersion7().ToString("N");
            story.ProjectId = projectId;
            outlineSvc.Save(projectId, outline);
            if (story.OutlineReview != null) outlineReview.Save(projectId, story.OutlineReview);
            SaveCheckpoint(story);
            log.LogInformation("Outline checkpoint saved: {ProjectId}", projectId);

            // Phase 5: Write beats with per-beat resilience
            storyState.InitializeCharacter(projectId, cast.protagonist, location);
            foreach (var c in cast.all.Where(c => c != cast.protagonist))
                storyState.InitializeCharacter(projectId, c);

            await WritBeatsWithResilience(story, outline, cast, location, ct);
        }
        catch (OperationCanceledException)
        {
            log.LogWarning("Story generation cancelled by user — saving partial story");
            story.FailureReason = "Cancelled by user";
            SaveCheckpoint(story);
            Report("Generation cancelled — partial story saved", story.Beats.Count, story.Outline?.Acts.SelectMany(a => a.Beats).Count() ?? 0);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Story generation failed at phase level — saving whatever we have");
            story.FailureReason = $"Pipeline failure: {ex.Message}";
            SaveCheckpoint(story);
            Report($"Generation failed — partial story saved ({story.Beats.Count} beats)", story.Beats.Count, 0);
        }
        finally
        {
            IsGenerating = false;
            CurrentStory = null;
        }

        return story;
    }

    /// <summary>
    /// Generate a story with a specific character forced as protagonist.
    /// "Drop in on any character and pick up with what they are doing."
    /// All other pipeline logic is identical to SurpriseMeAsync.
    /// </summary>
    public async Task<AutonomousStory> SurpriseMeForAsync(string characterName, CancellationToken ct = default)
    {
        log.LogInformation("=== SurpriseMeForAsync: Starting story for protagonist={Protagonist} ===", characterName);
        var story = new AutonomousStory();
        IsGenerating = true;
        CurrentStory = story;

        try
        {
            Report("Building cast...");
            var allChars = db.Characters;
            var protagonist = allChars.FirstOrDefault(c => c.Name.Equals(characterName, StringComparison.OrdinalIgnoreCase))
                ?? new CharacterData { Name = characterName };

            var supporting = new List<string>();
            foreach (var rel in protagonist.Relationships.Take(3))
            {
                var relChar = allChars.FirstOrDefault(c => c.Name == rel.Name && c.Status != "dead");
                if (relChar != null) supporting.Add(relChar.Name);
            }
            while (supporting.Count < 2)
            {
                var random = allChars
                    .Where(c => c.Name != protagonist.Name && !supporting.Contains(c.Name) && c.Status != "dead")
                    .OrderBy(_ => Random.Shared.Next()).FirstOrDefault();
                if (random == null) break;
                supporting.Add(random.Name);
            }

            var cast = (protagonist: protagonist.Name, all: new List<string> { protagonist.Name }.Concat(supporting).ToList());
            story.Protagonist = cast.protagonist;
            story.Characters = cast.all;

            Report("Finding conflicts...");
            var premises = await agenda.GenerateScenePremisesAsync(cast.all, ct: ct);
            var premise = premises.FirstOrDefault()?.Premise
                ?? $"{cast.protagonist} receives a contract that forces them to confront something they've been avoiding.";
            story.Premise = premise;

            var districts = db.Districts;
            var location = districts.Count > 0
                ? districts[Random.Shared.Next(districts.Count)].Name : "the Gray Zone";
            story.Location = location;

            Report("Architecting story arc...");
            var consequenceContext = consequences.BuildConsequenceContext(cast.protagonist);
            var battlePremise = premise
                + "\n\nIMPORTANT: GLMZ is a dangerous world. This story MUST include at least one battle/combat/violent conflict scene."
                + (consequenceContext.Length > 0 ? $"\n\nWORLD CONSEQUENCES FROM PREVIOUS STORIES:\n{consequenceContext}" : "");
            var outline = await outlineSvc.GenerateOutlineAsync(battlePremise, cast.all, location, 8, ct);
            story.Outline = outline;

            var projectId = Guid.NewGuid().ToString("N")[..12];
            story.ProjectId = projectId;
            if (string.IsNullOrEmpty(story.Title)) story.Title = outline.Title;

            Report("Reviewing story arc...");
            try { story.OutlineReview = await outlineReview.ReviewAsync(outline, ct); }
            catch { /* non-blocking — outline review is best-effort */ }

            SaveCheckpoint(story);

            storyState.InitializeCharacter(projectId, cast.protagonist, location);
            foreach (var c in cast.all.Where(c => c != cast.protagonist))
                storyState.InitializeCharacter(projectId, c);

            await WritBeatsWithResilience(story, outline, cast, location, ct);
        }
        catch (OperationCanceledException)
        {
            log.LogWarning("Story generation cancelled — saving partial story");
            story.FailureReason = "Cancelled by user";
            SaveCheckpoint(story);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Story generation failed for protagonist={Protagonist}", characterName);
            story.FailureReason = $"Pipeline failure: {ex.Message}";
            SaveCheckpoint(story);
        }
        finally
        {
            IsGenerating = false;
            CurrentStory = null;
        }

        return story;
    }

    /// <summary>
    /// Generate a story guided by the user: they supply the protagonist(s), a
    /// synopsis of what the story should be about, and optionally a location.
    /// The synopsis becomes the premise verbatim (plus the standard GLMZ-battle
    /// injection), skipping agenda premise discovery. Supporting cast is filled
    /// in from the primary protagonist's relationships when fewer than two
    /// protagonists are supplied.
    /// </summary>
    public async Task<AutonomousStory> GuidedStoryAsync(
        List<string> protagonists, string synopsis, string? location = null, int targetBeats = 8, CancellationToken ct = default)
    {
        // Clamp to a sane range — 3 is the minimum for a 3-act arc, 16 is the
        // upper bound before context pressure starts degrading beat quality.
        targetBeats = Math.Clamp(targetBeats, 3, 16);

        log.LogInformation("=== GuidedStoryAsync: protagonists=[{Protagonists}], synopsis={SynLen} chars, location={Location}, beats={Beats} ===",
            string.Join(", ", protagonists), synopsis?.Length ?? 0, location ?? "random", targetBeats);

        var story = new AutonomousStory();
        IsGenerating = true;
        CurrentStory = story;

        try
        {
            if (protagonists == null || protagonists.Count == 0)
                throw new ArgumentException("At least one protagonist is required.", nameof(protagonists));
            if (string.IsNullOrWhiteSpace(synopsis))
                throw new ArgumentException("A synopsis is required.", nameof(synopsis));

            Report("Building cast...");
            var allChars = db.Characters;
            var primaryName = protagonists[0];
            var primary = allChars.FirstOrDefault(c => c.Name.Equals(primaryName, StringComparison.OrdinalIgnoreCase))
                ?? new CharacterData { Name = primaryName };

            // Cast = user-supplied protagonists + relationship fill if <2 total
            var cast = new List<string> { primary.Name };
            foreach (var name in protagonists.Skip(1))
            {
                if (!cast.Contains(name, StringComparer.OrdinalIgnoreCase))
                    cast.Add(name);
            }
            if (cast.Count < 2)
            {
                foreach (var rel in primary.Relationships.Take(3))
                {
                    var relChar = allChars.FirstOrDefault(c => c.Name == rel.Name && c.Status != "dead");
                    if (relChar != null && !cast.Contains(relChar.Name, StringComparer.OrdinalIgnoreCase))
                        cast.Add(relChar.Name);
                    if (cast.Count >= 3) break;
                }
            }
            while (cast.Count < 2)
            {
                var random = allChars
                    .Where(c => !cast.Contains(c.Name, StringComparer.OrdinalIgnoreCase) && c.Status != "dead")
                    .OrderBy(_ => Random.Shared.Next()).FirstOrDefault();
                if (random == null) break;
                cast.Add(random.Name);
            }

            story.Protagonist = primary.Name;
            story.Characters = cast;

            // Location: user-supplied (validated loosely) or random fallback
            var districts = db.Districts;
            var chosenLocation = !string.IsNullOrWhiteSpace(location)
                ? location!
                : (districts.Count > 0
                    ? districts[Random.Shared.Next(districts.Count)].Name
                    : "the Gray Zone");
            story.Location = chosenLocation;

            // Synopsis IS the premise — no agenda discovery
            story.Premise = synopsis.Trim();

            Report("Architecting story arc...");
            var consequenceContext = consequences.BuildConsequenceContext(primary.Name);
            var guidedPremise =
                $"USER-SUPPLIED SYNOPSIS (this is the story the user asked for — honor its premise, characters, and direction):\n{synopsis.Trim()}"
                + "\n\nIMPORTANT: GLMZ is a dangerous world. This story MUST include at least one battle/combat/violent conflict scene. Random crime and violence can erupt at any time. Include at least one beat specifically tagged for combat."
                + (consequenceContext.Length > 0 ? $"\n\nWORLD CONSEQUENCES FROM PREVIOUS STORIES:\n{consequenceContext}" : "");

            var outline = await outlineSvc.GenerateOutlineAsync(guidedPremise, cast, chosenLocation, targetBeats, ct);
            story.Outline = outline;
            story.Title = outline.Title;

            EnsureBattleBeat(outline);

            // Assign project ID and save the checkpoint with outline (before any beats)
            var projectId = Guid.CreateVersion7().ToString("N");
            story.ProjectId = projectId;
            outlineSvc.Save(projectId, outline);

            Report("Reviewing story arc...");
            try
            {
                var reviewResult = await outlineReview.ReviewAsync(outline, ct);
                story.OutlineReview = reviewResult;
                if (reviewResult.RevisedOutline.Acts.Count > 0)
                {
                    outline = reviewResult.RevisedOutline;
                    story.Outline = outline;
                    log.LogInformation("Guided outline revised by review: moral={Moral}/10, strength={Strength}/10",
                        reviewResult.MoralAmbiguityScore, reviewResult.NarrativeStrength);
                }
                outlineReview.Save(projectId, reviewResult);
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "Outline review failed on guided story — proceeding with original outline");
            }

            SaveCheckpoint(story);
            log.LogInformation("Guided outline checkpoint saved: {ProjectId}", projectId);

            storyState.InitializeCharacter(projectId, primary.Name, chosenLocation);
            foreach (var c in cast.Where(c => c != primary.Name))
                storyState.InitializeCharacter(projectId, c);

            await WritBeatsWithResilience(story, outline, (primary.Name, cast), chosenLocation, ct);
        }
        catch (OperationCanceledException)
        {
            log.LogWarning("Guided story generation cancelled — saving partial story");
            story.FailureReason = "Cancelled by user";
            SaveCheckpoint(story);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Guided story generation failed");
            story.FailureReason = $"Pipeline failure: {ex.Message}";
            SaveCheckpoint(story);
        }
        finally
        {
            IsGenerating = false;
            CurrentStory = null;
        }

        return story;
    }

    /// <summary>
    /// Resume a previously failed or partially generated story from its last checkpoint.
    /// Loads the saved outline and continues from the first unwritten beat.
    /// </summary>
    public async Task<AutonomousStory> ResumeStoryAsync(AutonomousStory story, string? nextBeatGoalOverride = null, CancellationToken ct = default)
    {
        if (story.Complete)
        {
            log.LogWarning("ResumeStoryAsync called on already-complete story {ProjectId}", story.ProjectId);
            return story;
        }

        log.LogInformation("=== ResumeStoryAsync: Resuming story {ProjectId} from beat {BeatCount} ===",
            story.ProjectId, story.Beats.Count);

        story.FailureReason = null; // Clear previous failure
        IsGenerating = true;
        CurrentStory = story;

        try
        {
            // Load or use existing outline
            var outline = story.Outline ?? outlineSvc.Load(story.ProjectId);
            if (outline == null || outline.Acts.Count == 0)
            {
                log.LogError("Cannot resume — no outline found for {ProjectId}", story.ProjectId);
                story.FailureReason = "No outline available to resume from";
                return story;
            }
            story.Outline = outline;

            // Mark already-written beats
            foreach (var existingBeat in story.Beats)
                outlineSvc.MarkBeatWritten(outline, existingBeat.BeatIndex);

            // Re-initialize story state from existing beats
            var location = story.Location ?? "the Gray Zone";
            storyState.InitializeCharacter(story.ProjectId, story.Protagonist, location);
            foreach (var c in story.Characters.Where(c => c != story.Protagonist))
                storyState.InitializeCharacter(story.ProjectId, c);

            var cast = (protagonist: story.Protagonist, all: story.Characters);
            await WritBeatsWithResilience(story, outline, cast, location, ct, nextBeatGoalOverride);
        }
        finally
        {
            IsGenerating = false;
            CurrentStory = null;
        }

        return story;
    }

    /// <summary>
    /// Core beat-writing loop with full resilience:
    /// - Saves checkpoint after every successful beat
    /// - Retries once on transient failures (timeout, connection)
    /// - On permanent failure, saves partial story and stops gracefully
    /// - Never loses completed work
    /// </summary>
    private async Task WritBeatsWithResilience(
        AutonomousStory story, StoryOutline outline,
        (string protagonist, List<string> all) cast, string location,
        CancellationToken ct, string? nextBeatGoalOverride = null)
    {
        var allText = story.Beats.Select(b => b.Text).ToList();
        var totalBeats = outline.Acts.SelectMany(a => a.Beats).Count();
        var projectId = story.ProjectId;
        var arcValidations = new List<ArcValidation>();
        // Collect suggestion tasks — awaited at the end with a timeout before final checkpoint
        var suggestionTasks = new List<(GeneratedStoryBeat beat, Task<List<BeatSuggestion>> task)>();
        bool firstUnwritten = true;

        foreach (var act in outline.Acts)
        {
            foreach (var beat in act.Beats)
            {
                if (beat.Written) continue; // Skip already-written beats (resume case)

                if (ct.IsCancellationRequested)
                {
                    log.LogWarning("Cancellation requested — saving checkpoint at beat {BeatIndex}", beat.BeatIndex);
                    FinalizePartialStory(story, allText, "Cancelled by user");
                    return;
                }

                Report($"Writing beat {beat.BeatIndex + 1}/{totalBeats}: {beat.Title}");

                string? beatText = null;
                Exception? lastError = null;

                // Apply the user-chosen suggestion direction to the first unwritten beat only
                var goalOverride = (firstUnwritten && !string.IsNullOrEmpty(nextBeatGoalOverride))
                    ? $"CHOSEN DIRECTION: {nextBeatGoalOverride}\n\n{beat.Goal}"
                    : null;
                firstUnwritten = false;

                // Retry loop: try twice on transient failures
                for (int attempt = 1; attempt <= 2; attempt++)
                {
                    try
                    {
                        beatText = await GenerateSingleBeat(
                            story, outline, beat, cast, location, allText, projectId, arcValidations, ct, goalOverride);
                        break; // Success — exit retry loop
                    }
                    catch (TaskCanceledException ex) when (!ct.IsCancellationRequested && attempt < 2)
                    {
                        // HTTP timeout (not user cancellation) — retry
                        log.LogWarning(ex, "Beat {BeatIndex} timed out (attempt {Attempt}/2) — retrying", beat.BeatIndex, attempt);
                        lastError = ex;
                        await Task.Delay(2000, ct);
                    }
                    catch (OperationCanceledException)
                    {
                        // User cancellation — save and exit immediately
                        FinalizePartialStory(story, allText, "Cancelled by user");
                        return;
                    }
                    catch (HttpRequestException ex) when (attempt < 2)
                    {
                        log.LogWarning(ex, "Beat {BeatIndex} failed (attempt {Attempt}/2) — retrying in 3s", beat.BeatIndex, attempt);
                        lastError = ex;
                        await Task.Delay(3000, ct);
                    }
                    catch (Exception ex)
                    {
                        lastError = ex;
                        break; // Non-transient error — don't retry
                    }
                }

                if (beatText == null)
                {
                    // Beat generation failed after retries — save what we have and stop
                    log.LogError(lastError, "Beat {BeatIndex} failed permanently — saving partial story ({BeatsWritten}/{TotalBeats})",
                        beat.BeatIndex, story.Beats.Count, totalBeats);
                    FinalizePartialStory(story, allText, $"Beat {beat.BeatIndex + 1} failed: {lastError?.Message ?? "unknown error"}");
                    Report($"Generation paused — {story.Beats.Count}/{totalBeats} beats saved", story.Beats.Count, totalBeats);
                    return;
                }

                allText.Add(beatText);

                // State extraction (best-effort)
                try
                {
                    var storySoFar = string.Join("\n\n", allText);
                    await storyState.UpdateFromTextAsync(projectId, beatText, storySoFar);
                    await eventLog.ExtractAndLogAsync(projectId, beatText, beat.BeatIndex, ct);
                    var state = storyState.GetState(projectId);
                    knowledge.SyncFromState(projectId, state, eventLog.GetEvents(projectId), beat.BeatIndex);
                }
                catch (Exception ex)
                {
                    log.LogWarning(ex, "State extraction failed for beat {BeatIndex} — continuing with stale state", beat.BeatIndex);
                }

                // (Facet evolution removed 2026-04-26 — facet system retired. Character interior
                // is now sourced from documented psychology fields, not from drifting weights.)

                // Arc validation (best-effort — don't block story on validation failures)
                try
                {
                    var arcResult = await arcTracker.ValidateBeatAsync(beatText, beat, outline, beat.BeatIndex, ct);
                    arcValidations.Add(arcResult);
                    if (arcResult.DriftWarning.Length > 0)
                        log.LogWarning("Arc drift on beat {BeatIndex}: {Warning}", beat.BeatIndex, arcResult.DriftWarning);
                }
                catch (Exception ex)
                {
                    log.LogWarning(ex, "Arc validation failed for beat {BeatIndex} — continuing", beat.BeatIndex);
                }

                // Structural continuity validation (synchronous, fast)
                try
                {
                    var charsInBeat = beat.CharactersPresent.Count > 0 ? beat.CharactersPresent : cast.all;
                    var quickReport = continuityValidator.QuickValidate(beatText, charsInBeat);
                    if (!quickReport.Clean)
                    {
                        foreach (var issue in quickReport.Issues)
                            log.LogWarning("Continuity issue in beat {BeatIndex}: [{Severity}] {Description}",
                                beat.BeatIndex, issue.Severity, issue.Description);
                    }
                }
                catch (Exception ex)
                {
                    log.LogWarning(ex, "Continuity validation failed for beat {BeatIndex} — continuing", beat.BeatIndex);
                }

                // Full LLM continuity check (fire-and-forget — never blocks the beat loop)
                {
                    var beatTextCopy  = beatText;
                    var charsForContinuity = (beat.CharactersPresent.Count > 0 ? beat.CharactersPresent : cast.all).ToList();
                    var locationCopy  = beat.Location ?? location;
                    var priorTextCopy = allText.SkipLast(1).ToList();
                    var beatIdxCopy   = beat.BeatIndex;
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            var report = await continuityValidator.ValidateAsync(
                                beatTextCopy, projectId, charsForContinuity, locationCopy, priorTextCopy);
                            if (!report.Clean)
                            {
                                foreach (var issue in report.Issues)
                                    log.LogWarning("LLM continuity [{Severity}] {Category}: {Description} (beat {BeatIdx})",
                                        issue.Severity, issue.Category, issue.Description, beatIdxCopy);
                            }
                        }
                        catch (Exception ex)
                        {
                            log.LogDebug(ex, "LLM continuity check failed for beat {BeatIndex}", beatIdxCopy);
                        }
                    });
                }

                outlineSvc.MarkBeatWritten(outline, beat.BeatIndex);

                var newBeat = new GeneratedStoryBeat
                {
                    BeatIndex = beat.BeatIndex,
                    Title = beat.Title,
                    Text = beatText,
                    Act = act.ActNumber,
                    StructureRole = beat.StructureRole,
                    SceneType = beat.SceneType,
                };
                story.Beats.Add(newBeat);

                // Queue suggestion generation for this beat (awaited at end with timeout)
                {
                    var beatRef       = newBeat;
                    var storySoFar    = string.Join("\n\n", allText);
                    var castCopy      = cast.all.ToList();
                    var locationCopy  = beat.Location ?? location;
                    var beatIdxCopy   = beat.BeatIndex;
                    suggestionTasks.Add((beatRef, Task.Run(async () =>
                    {
                        try
                        {
                            return await suggestions.SuggestNextBeatsAsync(
                                projectId, outline, beatIdxCopy, castCopy, locationCopy, storySoFar, ct);
                        }
                        catch { return []; }
                    })));
                }

                // CHECKPOINT: Save after every beat — this is the resilience guarantee
                SaveCheckpoint(story);
                log.LogInformation("Beat {BeatIndex}/{Total} saved: {Title}",
                    beat.BeatIndex + 1, totalBeats, beat.Title);

                Report($"Beat {beat.BeatIndex + 1}/{totalBeats} complete", beat.BeatIndex + 1, totalBeats);
            }
        }

        // Collect beat suggestions — await with 10s timeout so they land in the final checkpoint
        Report("Generating next-beat suggestions...");
        foreach (var (beatRef, suggestionTask) in suggestionTasks)
        {
            try
            {
                beatRef.Suggestions = await suggestionTask.WaitAsync(TimeSpan.FromSeconds(10));
            }
            catch
            {
                // Timeout or error — leave Suggestions empty; story is still complete
            }
        }

        // All beats written successfully
        story.FullText = string.Join("\n\n---\n\n", allText);
        story.Complete = true;
        story.FailureReason = null;
        SaveCheckpoint(story);
        Report("Story complete!", totalBeats, totalBeats);

        log.LogInformation("=== Story complete: title={Title}, protagonist={Protagonist}, beats={BeatCount}, chars={TextLen} ===",
            story.Title, story.Protagonist, story.Beats.Count, story.FullText.Length);

        // Quality evaluation + canon grounding — fire-and-forget, do not block the caller
        Report("Evaluating story quality...");
        _ = Task.Run(async () =>
        {
            try
            {
                var report = await quality.EvaluateAsync(story, updatePatternAccumulator: true, ct: default);
                story.QualityReport = report;
                SaveCheckpoint(story);
                log.LogInformation("Quality evaluation complete: overall={Overall}/10 (story: {Title})",
                    report.AggregateScores.GetValueOrDefault("OVERALL"), story.Title);
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "Quality evaluation failed — not blocking story return");
            }

            try
            {
                var grounding = await canonGrounding.AnalyzeAndScaffoldAsync(
                    story.FullText,
                    sourceContext: $"story:{story.ProjectId} \"{story.Title}\"",
                    ct: default);
                story.CanonGrounding = grounding;
                SaveCheckpoint(story);
                if (grounding.EntitiesScaffolded > 0)
                    log.LogInformation(
                        "Canon grounding: scaffolded {Count} new stub(s) from story '{Title}'",
                        grounding.EntitiesScaffolded, story.Title);
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "Canon grounding failed — not blocking story return");
            }
        });
    }

    /// <summary>Generate a single beat's prose.</summary>
    private async Task<string> GenerateSingleBeat(
        AutonomousStory story, StoryOutline outline, OutlineBeat beat,
        (string protagonist, List<string> all) cast, string location,
        List<string> allText, string projectId, List<ArcValidation> arcValidations,
        CancellationToken ct, string? goalOverride = null)
    {
        var storyConstraints = storyState.BuildConstraints(projectId);
        var knowledgeConstraints = knowledge.BuildPovConstraints(projectId, cast.protagonist);
        var eventContext = eventLog.BuildRecentContext(projectId);
        var outlineContext = outlineSvc.BuildBeatContext(outline, beat.BeatIndex);
        var arcGuidance = arcTracker.BuildArcGuidance(outline, beat.BeatIndex, arcValidations);
        var charsForBeat = beat.CharactersPresent.Count > 0 ? beat.CharactersPresent : cast.all;
        var dialogueConstraints = dialogue.BuildDialogueContext(charsForBeat);
        var conversationGoals = dialogue.BuildConversationGoals(charsForBeat, beat.Goal, beat.Tension);
        var physicalContext = BuildPhysicalContext(charsForBeat);
        var povVoiceContext = BuildPovVoiceContext(cast.protagonist);

        // Pacing guidance — tells the LLM how to structure the prose for this beat's position
        var totalBeats = outline.Acts.SelectMany(a => a.Beats).Count();
        var pacing = PacingService.GetPacing(beat.BeatIndex, totalBeats, beat.Goal);

        // Structural role — tells the LLM the beat's named narrative position and scene type
        var methodology = new StoryMethodologyService();
        var structuralGuidance = !string.IsNullOrEmpty(beat.StructureRole)
            ? $"STRUCTURAL ROLE: {beat.StructureRole.ToUpperInvariant()}\nSCENE TYPE: {beat.SceneType?.ToUpperInvariant() ?? "SCENE"} — {(beat.SceneType == "sequel" ? "React → Dilemma → Decision. No action until the decision is made." : "Goal → Conflict → Disaster (yes-but or no-and). Never a clean yes.")}"
            : methodology.GetBeatGenerationGuidance(beat.BeatIndex, totalBeats);

        // Thematic enrichment: pull context snippets, vocabulary, quotes, and motifs from ALL repos
        var thematicContext = thematicIndex.BuildBeatContext(beat.Goal, beat.Title, beat.Location ?? location);

        // Behavior prediction: predict what each character will do based on psychology + state
        var charsInBeat = beat.CharactersPresent.Count > 0 ? beat.CharactersPresent : cast.all;
        var behaviorContext = behaviorPredict.BuildBehaviorContext(
            projectId, charsInBeat, beat.Location ?? location, beat.Goal, beat.Tension);

        var canonFacts = BuildCanonFacts(charsForBeat, beat.Location ?? location);
        var paragraphs = allText.ToList();
        var beatGoal = goalOverride ?? beat.Goal;
        if (!string.IsNullOrEmpty(dialogueConstraints))
            beatGoal += "\n\n" + dialogueConstraints;
        if (!string.IsNullOrEmpty(conversationGoals))
            beatGoal += "\n\n" + conversationGoals;

        if (allText.Count == 0)
        {
            var opening = await starter.GenerateOpeningAsync(new StoryStarterRequest
            {
                Premise = beatGoal,
                Characters = beat.CharactersPresent.Count > 0 ? beat.CharactersPresent : cast.all,
                Location = beat.Location ?? location,
                CanonFacts = canonFacts,
            }, ct);

            if (string.IsNullOrEmpty(story.Title) || story.Title == "Outline generation failed")
                story.Title = opening.Title;

            return opening.Text;
        }

        // Quality improvement directives — accumulated failure patterns from previous stories
        var qualityDirectives = quality.GetImprovementDirectives();

        var fullOutlineContext = outlineContext
            + (structuralGuidance.Length > 0 ? "\n\n" + structuralGuidance : "")
            + (povVoiceContext.Length > 0 ? "\n\n" + povVoiceContext : "")
            + (thematicContext.Length > 0 ? "\n\n" + thematicContext : "")
            + (behaviorContext.Length > 0 ? "\n\n" + behaviorContext : "")
            + (physicalContext.Length > 0 ? "\n\n" + physicalContext : "")
            + (arcGuidance.Length > 0 ? "\n\n" + arcGuidance : "")
            + (qualityDirectives.Length > 0 ? "\n\n" + qualityDirectives : "")
            + (pacing.ProseGuidance.Length > 0 ? "\n\n" + pacing.ProseGuidance : "");
        return await starter.ContinueAsync(
            paragraphs, beatGoal, null, beat.Location ?? location,
            beat.CharactersPresent.Count > 0 ? beat.CharactersPresent : cast.all,
            storyConstraints, knowledgeConstraints, eventContext, fullOutlineContext,
            canonFacts, ct);
    }

    /// <summary>
    /// Build a hard-constraint canon facts block for the given characters and location.
    /// Injected into every beat prompt so the LLM cannot invent undocumented relationships.
    /// </summary>
    private string BuildCanonFacts(List<string> characterNames, string location)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("== CANON FACTS — HARD CONSTRAINTS ==");
        sb.AppendLine("The following is DOCUMENTED CANON for every entity in this scene.");
        sb.AppendLine("Do NOT invent names, family members, relationships, history, or attributes");
        sb.AppendLine("beyond what is listed here. If a relationship is not documented below,");
        sb.AppendLine("it does not exist — write around it rather than fabricating it.");
        sb.AppendLine();

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in characterNames.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var nodeId = graph.ResolveId(name);
            if (nodeId == null || !seen.Add(nodeId)) continue;
            var brief = graph.GetEntityBrief(nodeId);
            if (!string.IsNullOrWhiteSpace(brief))
                sb.AppendLine(brief).AppendLine();
        }

        var locId = graph.ResolveId(location);
        if (locId != null && seen.Add(locId))
        {
            var locBrief = graph.GetEntityBrief(locId);
            if (!string.IsNullOrWhiteSpace(locBrief))
                sb.AppendLine(locBrief).AppendLine();
        }

        return sb.ToString();
    }

    private string StoriesDir => paths.StoriesDir;

    /// <summary>Save a partial or complete story as a checkpoint to disk.</summary>
    private void SaveCheckpoint(AutonomousStory story)
    {
        try
        {
            var path = StoryFolderHelper.GetFilePath(StoriesDir, story.ProjectId, "checkpoint.json", story.Title);
            var json = System.Text.Json.JsonSerializer.Serialize(story,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to save checkpoint for {ProjectId} — partial work may be lost", story.ProjectId);
        }
    }

    /// <summary>Load a checkpoint from disk.</summary>
    public AutonomousStory? LoadCheckpoint(string projectId)
    {
        var path = StoryFolderHelper.FindFile(StoriesDir, projectId, "checkpoint.json");
        if (path == null) return null;
        try
        {
            var json = File.ReadAllText(path);
            return System.Text.Json.JsonSerializer.Deserialize<AutonomousStory>(json);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to load checkpoint for {ProjectId}", projectId);
            return null;
        }
    }

    /// <summary>List all available checkpoints (partial and complete stories).</summary>
    public List<AutonomousStory> ListCheckpoints()
    {
        var dir = StoriesDir;
        if (!Directory.Exists(dir)) return [];
        return Directory.GetDirectories(dir)
            .Select(d => Path.Combine(d, "checkpoint.json"))
            .Where(File.Exists)
            .Select(f => { try { return System.Text.Json.JsonSerializer.Deserialize<AutonomousStory>(File.ReadAllText(f)); } catch { return null; } })
            .Where(s => s != null)
            .ToList()!;
    }

    /// <summary>Archive a failed story folder to archives/.</summary>
    public void ArchiveCheckpoint(string projectId)
    {
        var folder = StoryFolderHelper.FindFolder(StoriesDir, projectId);
        if (folder == null) return;
        var archiveDir = paths.ArchiveDir;
        Directory.CreateDirectory(archiveDir);
        var dest = Path.Combine(archiveDir, Path.GetFileName(folder));
        if (Directory.Exists(dest)) Directory.Delete(dest, true);
        Directory.Move(folder, dest);
    }

    /// <summary>Finalize a partial story — assemble text, set failure reason, save.</summary>
    private void FinalizePartialStory(AutonomousStory story, List<string> allText, string reason)
    {
        story.FullText = string.Join("\n\n---\n\n", allText);
        story.Complete = false;
        story.FailureReason = reason;
        SaveCheckpoint(story);
        log.LogWarning("Partial story saved: {ProjectId}, {BeatCount} beats, reason={Reason}",
            story.ProjectId, story.Beats.Count, reason);
    }

    /// <summary>Pick a protagonist and 2-4 supporting characters from the canon.</summary>
    private async Task<(string protagonist, List<string> all)> PickCastAsync(CancellationToken ct)
    {
        var allChars = db.Characters;
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
            beat.Goal = "BATTLE: " + beat.Goal + ". Violence erupts — GLMZ is a dangerous place. The conflict should feel sudden, visceral, and have consequences for the characters involved.";
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
            var c = db.FindCharacter(name);
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

    /// <summary>
    /// POV voice rubric for the lead character. Different from BuildDialogueConstraints
    /// (which scopes voice differentiation across speakers) — this is the *narrative* voice
    /// the prose itself should sound like. Ensures the system writes the chapter through
    /// THIS character's eyes, not the generic project-default.
    /// </summary>
    private string BuildPovVoiceContext(string leadName)
    {
        var c = db.FindCharacter(leadName);
        if (c == null) return "";

        var lines = new List<string> { $"POV NARRATIVE VOICE — {leadName} is the lens, not just the subject:" };

        // Speech patterns shape narration too — not just dialogue.
        var sp = c.SpeechPatterns;
        if (!string.IsNullOrEmpty(sp.Cadence)) lines.Add($"  cadence: {sp.Cadence}");
        if (!string.IsNullOrEmpty(sp.Vocabulary)) lines.Add($"  vocabulary: {sp.Vocabulary}");
        if (sp.VerbalTics?.Count > 0) lines.Add($"  verbal tics: {string.Join(" | ", sp.VerbalTics.Take(3))}");
        if (sp.ExampleLines?.Count > 0) lines.Add($"  voice example: \"{sp.ExampleLines[0]}\"");

        // Psychology drives WHAT they notice, not just HOW.
        var psy = c.Psychology;
        if (psy.CoreFears?.Count > 0) lines.Add($"  what they fear: {psy.CoreFears[0]}");
        if (psy.CoreDesires?.Count > 0) lines.Add($"  what they want: {psy.CoreDesires[0]}");
        if (psy.CopingMechanisms?.Count > 0) lines.Add($"  how they cope: {psy.CopingMechanisms[0]}");

        // Behavioral cues for what action looks like through their eyes.
        var beh = c.Behavioral;
        if (beh.DecisionRules?.Count > 0) lines.Add($"  decision rule: {beh.DecisionRules[0]}");
        if (beh.Habits?.Count > 0) lines.Add($"  habit: {beh.Habits[0]}");

        // Concrete observation rubric: what they NOTICE first in any space.
        // Fall back to character-agnostic guidance if literary_rules don't have it loaded yet.
        try
        {
            var pov = db.LiteraryRules?.PovVoice;
            if (pov != null && pov.Differentiation?.Count > 0)
                lines.Add($"  observation rubric: every paragraph should reflect THIS character's notice budget — what {leadName} would attend to first, in this order, given who they are. Ref: {pov.Differentiation[0]}");
        }
        catch { /* rules not loaded — skip */ }

        if (lines.Count == 1) return "";  // no usable canon

        lines.Add($"  ANTI-CADENCE: this prose should be unmistakably {leadName}'s. If the same paragraph could appear in another character's chapter, the voice has failed.");
        return string.Join("\n", lines);
    }

    private string BuildPhysicalContext(List<string> charactersInScene)
    {
        var lines = new List<string> { "PHYSICAL DESCRIPTIONS — use these details when describing characters:" };
        foreach (var name in charactersInScene)
        {
            var c = db.FindCharacter(name);
            if (c?.PhysicalDescription == null) continue;
            var p = c.PhysicalDescription;
            var parts = new List<string>();
            if (p.Build.Length > 0) parts.Add(p.Build);
            if (p.SkinTone.Length > 0) parts.Add($"skin: {p.SkinTone}");
            if (p.HairColor.Length > 0 && p.HairStyle.Length > 0) parts.Add($"hair: {p.HairColor}, {p.HairStyle}");
            if (p.EyeColor.Length > 0) parts.Add($"eyes: {p.EyeColor}");
            if (p.VisibleAugmentations.Length > 0) parts.Add($"augmentations: {p.VisibleAugmentations}");
            if (p.DistinguishingMarks.Count > 0) parts.Add($"marks: {string.Join("; ", p.DistinguishingMarks.Take(3))}");
            if (p.ClothingStyle.Length > 0) parts.Add($"clothing: {p.ClothingStyle}");
            if (p.PostureMovement.Length > 0) parts.Add($"carries themselves: {p.PostureMovement}");
            if (parts.Count > 0)
                lines.Add($"  {name}: {string.Join(". ", parts)}");
        }
        return lines.Count <= 1 ? "" : string.Join("\n", lines);
    }

    private void Report(string message, int current = 0, int total = 0)
    {
        ProgressMessage = message;
        ProgressCurrent = current;
        ProgressTotal = total;
        OnProgress?.Invoke(new DirectorProgress { Message = message, CurrentBeat = current, TotalBeats = total });
    }

    /// <summary>
    /// Repair a damaged story checkpoint. Inspects what's present on disk and
    /// re-enters the pipeline at the earliest broken stage — regenerating the
    /// outline if it's empty, re-running the review if it's missing/broken, or
    /// just continuing beat writing. The preserved premise, cast, and location
    /// from the checkpoint are reused so the story keeps its original intent.
    /// </summary>
    public async Task<AutonomousStory> RepairStoryAsync(AutonomousStory story, CancellationToken ct = default)
    {
        if (story.Complete)
        {
            log.LogWarning("RepairStoryAsync called on already-complete story {ProjectId}", story.ProjectId);
            return story;
        }

        var state = StoryDamage.Classify(story);
        log.LogInformation("=== RepairStoryAsync: {ProjectId} damage={State} ===", story.ProjectId, state);

        story.FailureReason = null;
        IsGenerating = true;
        CurrentStory = story;

        try
        {
            var location = string.IsNullOrWhiteSpace(story.Location) ? "the Gray Zone" : story.Location;
            var primary = string.IsNullOrWhiteSpace(story.Protagonist)
                ? (story.Characters.FirstOrDefault() ?? "Kyle")
                : story.Protagonist;
            var cast = story.Characters.Count > 0 ? story.Characters : [primary];
            if (!cast.Contains(primary, StringComparer.OrdinalIgnoreCase))
                cast.Insert(0, primary);
            story.Protagonist = primary;
            story.Characters = cast;

            // Stage 1 — rebuild outline if missing
            if (state == StoryDamageState.OutlineMissing)
            {
                if (string.IsNullOrWhiteSpace(story.Premise))
                {
                    story.FailureReason = "Cannot repair — no premise preserved in checkpoint";
                    SaveCheckpoint(story);
                    return story;
                }

                Report("Rebuilding outline from preserved premise...");
                var consequenceContext = consequences.BuildConsequenceContext(primary);
                var premise =
                    $"USER-SUPPLIED SYNOPSIS (this is the story the user asked for — honor its premise, characters, and direction):\n{story.Premise.Trim()}"
                    + "\n\nIMPORTANT: GLMZ is a dangerous world. This story MUST include at least one battle/combat/violent conflict scene. Random crime and violence can erupt at any time. Include at least one beat specifically tagged for combat."
                    + (consequenceContext.Length > 0 ? $"\n\nWORLD CONSEQUENCES FROM PREVIOUS STORIES:\n{consequenceContext}" : "");

                var outline = await outlineSvc.GenerateOutlineAsync(premise, cast, location, targetBeats: 8, ct);
                if (outline.Acts.Count == 0)
                {
                    story.FailureReason = "Outline rebuild failed — outline still empty";
                    SaveCheckpoint(story);
                    return story;
                }

                story.Outline = outline;
                story.Title = outline.Title;
                EnsureBattleBeat(outline);
                outlineSvc.Save(story.ProjectId, outline);
                SaveCheckpoint(story);
                state = StoryDamageState.OutlineReviewMissing;
            }

            // Stage 2 — re-run outline review if missing or broken
            if (state == StoryDamageState.OutlineReviewMissing)
            {
                Report("Reviewing story arc...");
                try
                {
                    var reviewResult = await outlineReview.ReviewAsync(story.Outline!, ct);
                    story.OutlineReview = reviewResult;
                    if (reviewResult.RevisedOutline.Acts.Count > 0)
                        story.Outline = reviewResult.RevisedOutline;
                    outlineReview.Save(story.ProjectId, reviewResult);
                }
                catch (Exception ex)
                {
                    log.LogWarning(ex, "Outline review failed during repair — proceeding with current outline");
                }
                SaveCheckpoint(story);
            }

            // Stage 3 — continue beat writing from wherever we are
            var finalOutline = story.Outline!;
            foreach (var existing in story.Beats)
                outlineSvc.MarkBeatWritten(finalOutline, existing.BeatIndex);

            storyState.InitializeCharacter(story.ProjectId, primary, location);
            foreach (var c in cast.Where(c => c != primary))
                storyState.InitializeCharacter(story.ProjectId, c);

            await WritBeatsWithResilience(story, finalOutline, (primary, cast), location, ct);
        }
        catch (OperationCanceledException)
        {
            log.LogWarning("Story repair cancelled — saving partial story");
            story.FailureReason = "Cancelled by user";
            SaveCheckpoint(story);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Story repair failed");
            story.FailureReason = $"Repair failure: {ex.Message}";
            SaveCheckpoint(story);
        }
        finally
        {
            IsGenerating = false;
            CurrentStory = null;
        }

        return story;
    }
}

public enum StoryDamageState
{
    Healthy,
    OutlineMissing,
    OutlineReviewMissing,
    BeatsIncomplete
}

public static class StoryDamage
{
    public static StoryDamageState Classify(AutonomousStory s)
    {
        if (s.Complete) return StoryDamageState.Healthy;

        var outline = s.Outline;
        var totalBeats = outline?.Acts.Sum(a => a.Beats.Count) ?? 0;
        if (outline == null || outline.Acts.Count == 0 || totalBeats == 0)
            return StoryDamageState.OutlineMissing;

        if (ReviewIsBroken(s.OutlineReview))
            return StoryDamageState.OutlineReviewMissing;

        return StoryDamageState.BeatsIncomplete;
    }

    public static string Describe(AutonomousStory s)
    {
        var total = s.Outline?.Acts.Sum(a => a.Beats.Count) ?? 0;
        return Classify(s) switch
        {
            StoryDamageState.OutlineMissing => "Outline generation failed — full rebuild from premise",
            StoryDamageState.OutlineReviewMissing => total > 0
                ? $"Outline OK, review incomplete — re-review and write {total} beats"
                : "Outline OK, review incomplete",
            StoryDamageState.BeatsIncomplete => total > 0
                ? $"Writing stopped at beat {s.Beats.Count}/{total} — continue from where it left off"
                : $"Writing stopped at beat {s.Beats.Count} — continue from where it left off",
            _ => "Healthy"
        };
    }

    private static bool ReviewIsBroken(OutlineReviewResult? r)
    {
        if (r == null) return true;
        if (string.IsNullOrWhiteSpace(r.Critique)) return true;
        if (r.Critique.Contains("parse fail", StringComparison.OrdinalIgnoreCase)) return true;
        if (r.Warnings.Any(w => w.Contains("Parse error", StringComparison.OrdinalIgnoreCase))) return true;
        return false;
    }
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
    public OutlineReviewResult? OutlineReview { get; set; }
    public StoryQualityReport? QualityReport { get; set; }
    public CanonGroundingResult? CanonGrounding { get; set; }
    public List<GeneratedStoryBeat> Beats { get; set; } = [];
    public string FullText { get; set; } = "";
    public bool Complete { get; set; }
    public string? FailureReason { get; set; }
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
}

public class GeneratedStoryBeat
{
    public string Id { get; set; } = Guid.CreateVersion7().ToString("N");
    public int BeatIndex { get; set; }
    public string Title { get; set; } = "";
    public string Text { get; set; } = "";
    public int Act { get; set; }
    public string StructureRole { get; set; } = "";
    public string SceneType { get; set; } = "scene";
    /// <summary>Possible next beats — populated after pipeline completes.</summary>
    public List<BeatSuggestion> Suggestions { get; set; } = [];
}
