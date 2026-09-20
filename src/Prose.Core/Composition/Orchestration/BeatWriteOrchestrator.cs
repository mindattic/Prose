using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Interfaces;
using Prose.Core.Services;
using Prose.Core.Services.Contradiction;
using Prose.Core.Composition.Ledger;
using Prose.Core.Composition.Prompt;
using Prose.Core.Composition.Window;

namespace Prose.Core.Composition.Orchestration;

/// <summary>Result of a preview generation — deliberately carries the assembled prompt back so a
/// caller (or a human) can inspect exactly what the model saw, not just what it produced.</summary>
public sealed record PreviewResult(AssembledPrompt Prompt, string GeneratedText);

/// <summary>One declared state change the writer reported about the beat it just wrote.</summary>
public sealed record DeclaredDelta(Guid EntityId, string EntityName, string Aspect, string Value);

/// <summary>Result of an actual generate-and-save call (Phase 2-3). <see cref="GateRetried"/> is
/// true when the first attempt was rejected and a second, corrected attempt is what got saved.</summary>
public sealed record SavedBeatResult(Guid NewBeatId, PreviewResult Preview, IReadOnlyList<DeclaredDelta> DeclaredDeltas, bool GateRetried);

/// <summary>Both candidate texts and the checker's reasoning when a contradiction survives a retry.
/// Per the v4 plan's Decisions: this is a hard stop, not a Finding-and-continue — the caller (an
/// --auto-run-style batch) is expected to halt on this, not swallow it.</summary>
public sealed class NarrativeContradictionRejectedException(ContradictionVerdict firstVerdict, string firstAttemptText, ContradictionVerdict secondVerdict, string secondAttemptText)
    : Exception($"Contradiction confirmed after retry: {secondVerdict.ViolatedFact ?? secondVerdict.Reasoning}")
{
    public ContradictionVerdict FirstVerdict { get; } = firstVerdict;
    public string FirstAttemptText { get; } = firstAttemptText;
    public ContradictionVerdict SecondVerdict { get; } = secondVerdict;
    public string SecondAttemptText { get; } = secondAttemptText;
}

/// <summary>
/// v4 plan Phases 1-2: windowed generation. <see cref="PreviewGenerateAsync"/> (Phase 1) writes
/// nothing to any database — it assembles the ≤8-block prompt (<see cref="PromptAssembler"/>) from
/// a real sliding window (<see cref="SceneWindowService"/>) and real on-screen facts
/// (<see cref="StoryStateQuery"/>), calls the LLM once, and returns the result for inspection.
/// <see cref="GenerateAndSaveAsync"/> (Phase 2) does the same but actually inserts the generated
/// beat and records its declared state deltas — used ONLY against the disposable BCODA2 sandbox
/// (see the v4 plan memory), never the live book, until Phase 5 is reviewed and passed.
/// </summary>
public sealed class BeatWriteOrchestrator
{
    private readonly SceneWindowService window;
    private readonly StoryStateQuery storyState;
    private readonly ILlmService llm;
    private readonly NodeWorkbenchService workbench;
    private readonly WorldStateLedger worldState;
    private readonly IDbContextFactory<ProseDbContext> dbFactory;
    private readonly NarrativeContradictionChecker gate;

    public BeatWriteOrchestrator(
        SceneWindowService window, StoryStateQuery storyState, ILlmService llm,
        NodeWorkbenchService workbench, WorldStateLedger worldState, IDbContextFactory<ProseDbContext> dbFactory,
        NarrativeContradictionChecker gate)
    {
        this.window = window;
        this.storyState = storyState;
        this.llm = llm;
        this.workbench = workbench;
        this.worldState = worldState;
        this.dbFactory = dbFactory;
        this.gate = gate;
    }

    public async Task<PreviewResult> PreviewGenerateAsync(
        Guid bookNodeId,
        Guid afterBeatId,
        IReadOnlyList<Guid> charactersInScene,
        int asOfStoryPosition,
        string povCharacter,
        string location,
        string beatGoal,
        string universeLine,
        int windowSizeBeats = 15,
        CancellationToken ct = default)
    {
        var win = await this.window.GetWindowAsync(bookNodeId, afterBeatId, windowSizeBeats, ct);
        var facts = await storyState.GetOnScreenFactsForEntitiesAsync(bookNodeId, charactersInScene, asOfStoryPosition, ct);
        var prompt = PromptAssembler.Assemble(universeLine, win, facts, povCharacter, location, beatGoal);

        var generated = await llm.GenerateAsync(prompt.System, prompt.User, temperature: 0.8, maxTokens: 1200, ct: ct);
        return new PreviewResult(prompt, generated);
    }

    /// <summary>
    /// Phase 2-3: generate, run the write gate (Phase 3 — retry once on a confirmed contradiction,
    /// hard-stop on a second), INSERT the beat (via the same <c>NodeWorkbenchService</c> every other
    /// write path uses), stamp a StoryPosition so later queries can order against it (v3's
    /// InsertBeatAsync leaves it null), then run one narrow follow-up LLM call asking the writer to
    /// self-report what it just changed — the "declared" write path the v4 plan's Story State
    /// section describes, distinct from the blanket LLM-mined obligation extractor that measured a
    /// 96% false-positive rate this same session: this call is scoped to the one beat just written,
    /// not arbitrary already-written prose.
    /// </summary>
    /// <exception cref="NarrativeContradictionRejectedException">A contradiction was confirmed on
    /// both attempts. Nothing is saved. Per the v4 plan's Decisions, an --auto-run-style caller is
    /// expected to let this propagate and halt the batch, not swallow it.</exception>
    public async Task<SavedBeatResult> GenerateAndSaveAsync(
        Guid bookNodeId,
        Guid afterBeatId,
        IReadOnlyDictionary<Guid, string> charactersInScene,
        int asOfStoryPosition,
        string povCharacter,
        string location,
        string beatGoal,
        string universeLine,
        int windowSizeBeats = 15,
        CancellationToken ct = default)
    {
        var charIds = charactersInScene.Keys.ToList();
        var facts = await storyState.GetOnScreenFactsForEntitiesAsync(bookNodeId, charIds, asOfStoryPosition, ct);

        var preview = await PreviewGenerateAsync(
            bookNodeId, afterBeatId, charIds, asOfStoryPosition,
            povCharacter, location, beatGoal, universeLine, windowSizeBeats, ct);

        var verdict = await gate.CheckAsync(facts.EntityStateFacts.Concat(facts.ContinuityFacts).ToList(), preview.GeneratedText, ct);
        var gateRetried = false;
        if (verdict.Contradicts)
        {
            gateRetried = true;
            var correctedGoal = $"{beatGoal}\n\nIMPORTANT — a prior attempt at this beat contradicted an established fact ({verdict.ViolatedFact ?? verdict.Reasoning}). Do not repeat that contradiction.";
            var retryPreview = await PreviewGenerateAsync(
                bookNodeId, afterBeatId, charIds, asOfStoryPosition,
                povCharacter, location, correctedGoal, universeLine, windowSizeBeats, ct);
            var retryVerdict = await gate.CheckAsync(facts.EntityStateFacts.Concat(facts.ContinuityFacts).ToList(), retryPreview.GeneratedText, ct);
            if (retryVerdict.Contradicts)
                throw new NarrativeContradictionRejectedException(verdict, preview.GeneratedText, retryVerdict, retryPreview.GeneratedText);
            preview = retryPreview;
        }

        // InsertBeatAsync's nodeId is the BEAT'S OWN chapter (the BeatNode.NodeId it's actually a
        // member of), not the book root — a book-level node holds no BeatNodes rows of its own once
        // beats live under chapters. bookNodeId above is only ever used for the window/facts/
        // obligations reads, which are correctly scoped to the whole book.
        Guid chapterNodeId;
        await using (var lookupDb = await dbFactory.CreateDbContextAsync(ct))
        {
            chapterNodeId = await lookupDb.BeatNodes.AsNoTracking()
                .Where(bn => bn.BeatId == afterBeatId).Select(bn => bn.NodeId).FirstAsync(ct);
        }

        var newBeat = await workbench.InsertBeatAsync(chapterNodeId, afterBeatId, preview.GeneratedText, ct);

        // v3's InsertBeatAsync doesn't stamp StoryPosition (it's normally backfilled by a separate
        // extraction pass). Stamping it here — same value as the beat this one follows — keeps the
        // new beat visible to StoryStateQuery's asOf join immediately, without waiting on that pass.
        await using (var db = await dbFactory.CreateDbContextAsync(ct))
        {
            var b = await db.Beats.FirstAsync(x => x.Id == newBeat.Id, ct);
            b.StoryPosition = asOfStoryPosition;
            await db.SaveChangesAsync(ct);
        }

        var deltas = await ExtractDeclaredDeltasAsync(preview.GeneratedText, charactersInScene, ct);
        if (deltas.Count > 0)
        {
            var events = deltas.Select(d => new EntityStateEvent
            {
                EntityId = d.EntityId,
                AspectKey = d.Aspect,
                Verb = "set",
                NewValue = d.Value,
                BeatGuid = newBeat.Id,
                Source = "v4:declared",
                Snippet = preview.GeneratedText.Length > 300 ? preview.GeneratedText[..300] : preview.GeneratedText,
            }).ToList();
            await worldState.RecordManyAsync(events, ct);
        }

        return new SavedBeatResult(newBeat.Id, preview, deltas, gateRetried);
    }

    private async Task<IReadOnlyList<DeclaredDelta>> ExtractDeclaredDeltasAsync(
        string beatText, IReadOnlyDictionary<Guid, string> charactersInScene, CancellationToken ct)
    {
        if (charactersInScene.Count == 0) return [];

        var names = string.Join(", ", charactersInScene.Values);
        var system = "You extract structured state changes from a scene. You report only what the text actually states — never invent a fact the text doesn't support.";
        var user = $"""
            CHARACTERS IN THIS SCENE: {names}

            SCENE TEXT:
            {beatText}

            List ONLY concrete state changes this scene establishes about these characters — a new
            location, a new possession or loss, a new piece of knowledge, a change in a relationship,
            a physical condition. One per line, in EXACTLY this format:
            CharacterName | aspect_key | new value

            Use short aspect_key names such as: location, knowledge.<topic>, relations.<name>,
            condition.<name>, possession.<item>. Output NOTHING if the scene establishes no new state
            for these characters. Maximum 5 lines. No commentary, no header, just the lines (or nothing).
            """;

        var raw = await llm.GenerateAsync(system, user, temperature: 0.0, maxTokens: 300, ct: ct);
        var nameToId = charactersInScene.ToDictionary(kv => kv.Value, kv => kv.Key, StringComparer.OrdinalIgnoreCase);

        var results = new List<DeclaredDelta>();
        foreach (var line in raw.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = line.Split('|', StringSplitOptions.TrimEntries);
            if (parts.Length != 3) continue;
            if (!nameToId.TryGetValue(parts[0], out var entityId)) continue;
            results.Add(new DeclaredDelta(entityId, parts[0], parts[1], parts[2]));
        }
        return results;
    }
}
