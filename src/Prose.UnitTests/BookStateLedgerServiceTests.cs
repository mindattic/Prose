using Prose.Core.Services;
using Prose.UnitTests.Fixtures;

namespace Prose.UnitTests;

/// <summary>
/// Covers <see cref="BookStateLedgerService"/>'s write/read surface: <c>RecordAsync</c> and
/// <c>GetCurrentStateAsync</c> keep only the latest event per <c>StateKey</c>; the per-<c>StateType</c>
/// transition chains documented on the service (Crisis, DramaticQuestion, Objective, Threat,
/// Alliance, Information) track correctly as later events supersede earlier ones;
/// <c>PersistPipeDelimitedEventsAsync</c> parses well-formed pipe-delimited lines and skips
/// malformed/blank ones, and refuses to knock a terminal state (e.g. a Resolved crisis) out of its
/// terminal value unless the verb is explicitly "reopen" — the mechanism that stops the engine
/// re-resolving a closed crisis; and <c>BuildContextAsync</c> renders the BOOK PLOT STATE block,
/// including the empty-node case.
///
/// <c>ExtractAndRecordAsync</c> (the LLM-extraction entry point) is intentionally not covered here:
/// it is a fire-and-forget call to an LLM and this project tests no LLM judges — only the
/// deterministic parse/persist path it hands off to, <see cref="BookStateLedgerService.PersistPipeDelimitedEventsAsync"/>,
/// is covered.
/// </summary>
[TestFixture, NonParallelizable]
public class BookStateLedgerServiceTests
{
    static BookStateLedgerService MakeService(CanonFixture fixture) =>
        new(fixture.Factory, new FakeLlmService(), NullLoggers.For<BookStateLedgerService>());

    // ── RecordAsync + GetCurrentStateAsync: latest wins per StateKey ─────────

    [Test]
    public async Task RecordAsync_SingleEvent_GetCurrentStateReturnsIt()
    {
        using var fixture = CanonFixture.Create();
        using var _ = CanonFixture.ScopeTo(CanonFixture.UniverseA);
        var svc = MakeService(fixture);

        await svc.RecordAsync(fixture.BookA, beatId: null, beatIndex: -1,
            "crisis:ambush", "Crisis", "open", "The convoy is ambushed.", "Open");

        var state = await svc.GetCurrentStateAsync(fixture.BookA);

        Assert.That(state, Has.Count.EqualTo(1));
        var ev = state["crisis:ambush"];
        Assert.That(ev.StateType, Is.EqualTo("Crisis"));
        Assert.That(ev.Verb, Is.EqualTo("open"));
        Assert.That(ev.Label, Is.EqualTo("The convoy is ambushed."));
        Assert.That(ev.NewValue, Is.EqualTo("Open"));
        Assert.That(ev.Source, Is.EqualTo("manual"));
    }

    [Test]
    public async Task RecordAsync_SameStateKeyTwice_GetCurrentStateReturnsLatestOnly()
    {
        using var fixture = CanonFixture.Create();
        using var _ = CanonFixture.ScopeTo(CanonFixture.UniverseA);
        var svc = MakeService(fixture);

        await svc.RecordAsync(fixture.BookA, null, -1, "crisis:ambush", "Crisis", "open", "Opens.", "Open");
        await Task.Delay(5);
        await svc.RecordAsync(fixture.BookA, null, -1, "crisis:ambush", "Crisis", "escalate", "Escalates.", "Escalated");

        var state = await svc.GetCurrentStateAsync(fixture.BookA);

        Assert.That(state, Has.Count.EqualTo(1), "one row per StateKey, not one per event");
        Assert.That(state["crisis:ambush"].NewValue, Is.EqualTo("Escalated"));
        Assert.That(state["crisis:ambush"].Label, Is.EqualTo("Escalates."));
    }

    [Test]
    public async Task GetCurrentStateAsync_DifferentStateKeys_ReturnsOneEntryEach()
    {
        using var fixture = CanonFixture.Create();
        using var _ = CanonFixture.ScopeTo(CanonFixture.UniverseA);
        var svc = MakeService(fixture);

        await svc.RecordAsync(fixture.BookA, null, -1, "crisis:ambush", "Crisis", "open", "Opens.", "Open");
        await svc.RecordAsync(fixture.BookA, null, -1, "objective:evacuate", "Objective", "establish", "Evacuate.", "Active");

        var state = await svc.GetCurrentStateAsync(fixture.BookA);

        Assert.That(state, Has.Count.EqualTo(2));
        Assert.That(state.Keys, Is.EquivalentTo(new[] { "crisis:ambush", "objective:evacuate" }));
    }

    [Test]
    public async Task GetCurrentStateAsync_StateKeyCaseInsensitive_TreatsAsSameKey()
    {
        using var fixture = CanonFixture.Create();
        using var _ = CanonFixture.ScopeTo(CanonFixture.UniverseA);
        var svc = MakeService(fixture);

        await svc.RecordAsync(fixture.BookA, null, -1, "Crisis:Ambush", "Crisis", "open", "Opens.", "Open");
        await Task.Delay(5);
        await svc.RecordAsync(fixture.BookA, null, -1, "crisis:ambush", "Crisis", "escalate", "Escalates.", "Escalated");

        var state = await svc.GetCurrentStateAsync(fixture.BookA);

        Assert.That(state, Has.Count.EqualTo(1));
        Assert.That(state["CRISIS:AMBUSH"].NewValue, Is.EqualTo("Escalated"));
    }

    [Test]
    public async Task GetCurrentStateAsync_ScopedToNode_DoesNotLeakOtherNodesEvents()
    {
        using var fixture = CanonFixture.Create();
        using var _ = CanonFixture.ScopeTo(CanonFixture.UniverseA);
        var svc = MakeService(fixture);

        await svc.RecordAsync(fixture.BookA, null, -1, "crisis:ambush", "Crisis", "open", "Book A's crisis.", "Open");
        await svc.RecordAsync(fixture.FlatChapter, null, -1, "crisis:ambush", "Crisis", "open", "A different node's crisis.", "Open");

        var state = await svc.GetCurrentStateAsync(fixture.BookA);

        Assert.That(state, Has.Count.EqualTo(1));
        Assert.That(state["crisis:ambush"].Label, Is.EqualTo("Book A's crisis."));
    }

    // ── Per-StateType transition chains ──────────────────────────────────────

    [Test]
    public async Task Crisis_OpenEscalatedClimaxedResolvedReopened_TracksLatestAtEachStep()
    {
        using var fixture = CanonFixture.Create();
        using var _ = CanonFixture.ScopeTo(CanonFixture.UniverseA);
        var svc = MakeService(fixture);
        const string key = "crisis:behemoth_approach";

        async Task Step(string verb, string value)
        {
            await svc.RecordAsync(fixture.BookA, null, -1, key, "Crisis", verb, $"{verb} step", value);
            await Task.Delay(5);
        }

        await Step("open", "Open");
        Assert.That((await svc.GetCurrentStateAsync(fixture.BookA))[key].NewValue, Is.EqualTo("Open"));

        await Step("escalate", "Escalated");
        Assert.That((await svc.GetCurrentStateAsync(fixture.BookA))[key].NewValue, Is.EqualTo("Escalated"));

        await Step("climax", "Climaxed");
        Assert.That((await svc.GetCurrentStateAsync(fixture.BookA))[key].NewValue, Is.EqualTo("Climaxed"));

        await Step("resolve", "Resolved");
        Assert.That((await svc.GetCurrentStateAsync(fixture.BookA))[key].NewValue, Is.EqualTo("Resolved"));

        await Step("reopen", "Reopened");
        Assert.That((await svc.GetCurrentStateAsync(fixture.BookA))[key].NewValue, Is.EqualTo("Reopened"));
    }

    [Test]
    public async Task DramaticQuestion_OpenThenAnswered_LatestIsAnswered()
    {
        using var fixture = CanonFixture.Create();
        using var _ = CanonFixture.ScopeTo(CanonFixture.UniverseA);
        var svc = MakeService(fixture);
        const string key = "question:who_controls_it";

        await svc.RecordAsync(fixture.BookA, null, -1, key, "DramaticQuestion", "open", "Posed.", "Open");
        await Task.Delay(5);
        await svc.RecordAsync(fixture.BookA, null, -1, key, "DramaticQuestion", "answer", "Answered.", "Answered");

        Assert.That((await svc.GetCurrentStateAsync(fixture.BookA))[key].NewValue, Is.EqualTo("Answered"));
    }

    [Test]
    public async Task Objective_ActiveThenAchieved_LatestIsAchieved()
    {
        using var fixture = CanonFixture.Create();
        using var _ = CanonFixture.ScopeTo(CanonFixture.UniverseA);
        var svc = MakeService(fixture);
        const string key = "objective:civilian_evacuation";

        await svc.RecordAsync(fixture.BookA, null, -1, key, "Objective", "establish", "Established.", "Active");
        await Task.Delay(5);
        await svc.RecordAsync(fixture.BookA, null, -1, key, "Objective", "achieve", "Achieved.", "Achieved");

        Assert.That((await svc.GetCurrentStateAsync(fixture.BookA))[key].NewValue, Is.EqualTo("Achieved"));
    }

    [Test]
    public async Task Threat_ActiveContainedNeutralized_TracksLatestAtEachStep()
    {
        using var fixture = CanonFixture.Create();
        using var _ = CanonFixture.ScopeTo(CanonFixture.UniverseA);
        var svc = MakeService(fixture);
        const string key = "threat:arcsec_neutralize";

        await svc.RecordAsync(fixture.BookA, null, -1, key, "Threat", "shift", "Emerges.", "Active");
        await Task.Delay(5);
        await svc.RecordAsync(fixture.BookA, null, -1, key, "Threat", "contain", "Contained.", "Contained");
        await Task.Delay(5);
        await svc.RecordAsync(fixture.BookA, null, -1, key, "Threat", "neutralize", "Neutralized.", "Neutralized");

        Assert.That((await svc.GetCurrentStateAsync(fixture.BookA))[key].NewValue, Is.EqualTo("Neutralized"));
    }

    [Test]
    public async Task Alliance_ActiveStrainedBroken_TracksLatestAtEachStep()
    {
        using var fixture = CanonFixture.Create();
        using var _ = CanonFixture.ScopeTo(CanonFixture.UniverseA);
        var svc = MakeService(fixture);
        const string key = "alliance:kyle_pixel";

        await svc.RecordAsync(fixture.BookA, null, -1, key, "Alliance", "shift", "Forms.", "Active");
        await Task.Delay(5);
        await svc.RecordAsync(fixture.BookA, null, -1, key, "Alliance", "shift", "Strains.", "Strained");
        await Task.Delay(5);
        await svc.RecordAsync(fixture.BookA, null, -1, key, "Alliance", "shift", "Breaks.", "Broken");

        Assert.That((await svc.GetCurrentStateAsync(fixture.BookA))[key].NewValue, Is.EqualTo("Broken"));
    }

    [Test]
    public async Task Information_HiddenRevealedConfirmed_TracksLatestAtEachStep()
    {
        using var fixture = CanonFixture.Create();
        using var _ = CanonFixture.ScopeTo(CanonFixture.UniverseA);
        var svc = MakeService(fixture);
        const string key = "information:true_identity";

        await svc.RecordAsync(fixture.BookA, null, -1, key, "Information", "shift", "Hidden.", "Hidden");
        await Task.Delay(5);
        await svc.RecordAsync(fixture.BookA, null, -1, key, "Information", "reveal", "Revealed.", "Revealed");
        await Task.Delay(5);
        await svc.RecordAsync(fixture.BookA, null, -1, key, "Information", "confirm", "Confirmed.", "Confirmed");

        Assert.That((await svc.GetCurrentStateAsync(fixture.BookA))[key].NewValue, Is.EqualTo("Confirmed"));
    }

    // ── PersistPipeDelimitedEventsAsync: parsing ──────────────────────────────

    [Test]
    public async Task Persist_WellFormedLines_ParsesFieldsInOrderAndPersistsAll()
    {
        using var fixture = CanonFixture.Create();
        using var _ = CanonFixture.ScopeTo(CanonFixture.UniverseA);
        var svc = MakeService(fixture);
        var beatId = Guid.NewGuid();

        var lines = new[]
        {
            "Crisis|crisis:x|open|Something happens|Open",
            "Objective|objective:y|establish|New objective|Active",
        };
        var existing = await svc.GetCurrentStateAsync(fixture.BookA);

        await svc.PersistPipeDelimitedEventsAsync(lines, fixture.BookA, beatId, beatIndex: 3, existing);

        var state = await svc.GetCurrentStateAsync(fixture.BookA);
        Assert.That(state, Has.Count.EqualTo(2));
        Assert.That(state["crisis:x"].StateType, Is.EqualTo("Crisis"));
        Assert.That(state["crisis:x"].Verb, Is.EqualTo("open"));
        Assert.That(state["crisis:x"].Label, Is.EqualTo("Something happens"));
        Assert.That(state["crisis:x"].NewValue, Is.EqualTo("Open"));
        Assert.That(state["crisis:x"].BeatId, Is.EqualTo(beatId));
        Assert.That(state["crisis:x"].BeatIndex, Is.EqualTo(3));
        Assert.That(state["crisis:x"].Source, Is.EqualTo("auto"));
        Assert.That(state["objective:y"].NewValue, Is.EqualTo("Active"));
    }

    [Test]
    public async Task Persist_BlankAndTooFewFieldLines_SkippedButWellFormedLineStillPersists()
    {
        using var fixture = CanonFixture.Create();
        using var _ = CanonFixture.ScopeTo(CanonFixture.UniverseA);
        var svc = MakeService(fixture);

        var lines = new[]
        {
            "",
            "   ",
            "Crisis|crisis:bad|open|Missing new value", // only 4 fields
            "Crisis|crisis:good|open|Label here|Open",
        };
        var existing = await svc.GetCurrentStateAsync(fixture.BookA);

        await svc.PersistPipeDelimitedEventsAsync(lines, fixture.BookA, Guid.NewGuid(), 0, existing);

        var state = await svc.GetCurrentStateAsync(fixture.BookA);
        Assert.That(state, Has.Count.EqualTo(1));
        Assert.That(state.ContainsKey("crisis:good"), Is.True);
        Assert.That(state.ContainsKey("crisis:bad"), Is.False);
    }

    [Test]
    public async Task Persist_EmptyStateKeyOrEmptyNewValue_LineSkipped()
    {
        using var fixture = CanonFixture.Create();
        using var _ = CanonFixture.ScopeTo(CanonFixture.UniverseA);
        var svc = MakeService(fixture);

        var lines = new[]
        {
            "Crisis||open|Label|Open",      // empty state_key
            "Crisis|crisis:z|open|Label|",  // empty NewValue
        };
        var existing = await svc.GetCurrentStateAsync(fixture.BookA);

        await svc.PersistPipeDelimitedEventsAsync(lines, fixture.BookA, Guid.NewGuid(), 0, existing);

        Assert.That(await svc.GetCurrentStateAsync(fixture.BookA), Is.Empty);
    }

    [Test]
    public async Task Persist_MoreThanSixLines_OnlyFirstSixPersisted()
    {
        using var fixture = CanonFixture.Create();
        using var _ = CanonFixture.ScopeTo(CanonFixture.UniverseA);
        var svc = MakeService(fixture);

        var lines = Enumerable.Range(1, 8)
            .Select(i => $"Crisis|crisis:{i}|open|Label {i}|Open")
            .ToArray();
        var existing = await svc.GetCurrentStateAsync(fixture.BookA);

        await svc.PersistPipeDelimitedEventsAsync(lines, fixture.BookA, Guid.NewGuid(), 0, existing);

        var state = await svc.GetCurrentStateAsync(fixture.BookA);
        Assert.That(state, Has.Count.EqualTo(6));
        for (var i = 1; i <= 6; i++) Assert.That(state.ContainsKey($"crisis:{i}"), Is.True);
        for (var i = 7; i <= 8; i++) Assert.That(state.ContainsKey($"crisis:{i}"), Is.False);
    }

    [Test]
    public async Task Persist_EmptyLinesList_NoOp()
    {
        using var fixture = CanonFixture.Create();
        using var _ = CanonFixture.ScopeTo(CanonFixture.UniverseA);
        var svc = MakeService(fixture);

        await svc.PersistPipeDelimitedEventsAsync(
            Array.Empty<string>(), fixture.BookA, Guid.NewGuid(), 0, new Dictionary<string, Prose.Core.Data.Entities.BookPlotEvent>());

        Assert.That(await svc.GetCurrentStateAsync(fixture.BookA), Is.Empty);
    }

    // ── Terminal-state guard: never re-resolve a closed crisis ───────────────

    [Test]
    public async Task Persist_TerminalStateWithoutReopenVerb_SkipsUpdate_StaysResolved()
    {
        using var fixture = CanonFixture.Create();
        using var _ = CanonFixture.ScopeTo(CanonFixture.UniverseA);
        var svc = MakeService(fixture);
        const string key = "crisis:behemoth_approach";

        await svc.RecordAsync(fixture.BookA, null, -1, key, "Crisis", "resolve", "Resolved already.", "Resolved");
        var existing = await svc.GetCurrentStateAsync(fixture.BookA);

        await svc.PersistPipeDelimitedEventsAsync(
            [$"Crisis|{key}|escalate|Tries to re-escalate a closed crisis|Escalated"],
            fixture.BookA, Guid.NewGuid(), 0, existing);

        var state = await svc.GetCurrentStateAsync(fixture.BookA);
        Assert.That(state[key].NewValue, Is.EqualTo("Resolved"), "a terminal state must not be knocked out by a non-reopen verb");
        Assert.That(state[key].Label, Is.EqualTo("Resolved already."));
    }

    [Test]
    public async Task Persist_TerminalStateWithReopenVerb_AllowsUpdate()
    {
        using var fixture = CanonFixture.Create();
        using var _ = CanonFixture.ScopeTo(CanonFixture.UniverseA);
        var svc = MakeService(fixture);
        const string key = "crisis:behemoth_approach";

        await svc.RecordAsync(fixture.BookA, null, -1, key, "Crisis", "resolve", "Resolved already.", "Resolved");
        var existing = await svc.GetCurrentStateAsync(fixture.BookA);

        await svc.PersistPipeDelimitedEventsAsync(
            [$"Crisis|{key}|reopen|The crisis returns|Reopened"],
            fixture.BookA, Guid.NewGuid(), 0, existing);

        var state = await svc.GetCurrentStateAsync(fixture.BookA);
        Assert.That(state[key].NewValue, Is.EqualTo("Reopened"));
        Assert.That(state[key].Label, Is.EqualTo("The crisis returns"));
    }

    // ── BuildContextAsync: BOOK PLOT STATE block ──────────────────────────────

    [Test]
    public async Task BuildContextAsync_NoEvents_ReturnsEmptyString()
    {
        using var fixture = CanonFixture.Create();
        using var _ = CanonFixture.ScopeTo(CanonFixture.UniverseA);
        var svc = MakeService(fixture);

        var block = await svc.BuildContextAsync(fixture.BookA);

        Assert.That(block, Is.EqualTo(""));
    }

    [Test]
    public async Task BuildContextAsync_NonTerminalEvent_RendersHeaderAndStateLineWithoutCheckmark()
    {
        using var fixture = CanonFixture.Create();
        using var _ = CanonFixture.ScopeTo(CanonFixture.UniverseA);
        var svc = MakeService(fixture);

        await svc.RecordAsync(fixture.BookA, null, -1, "crisis:ambush", "Crisis", "escalate", "The ambush worsens.", "Escalated");

        var block = await svc.BuildContextAsync(fixture.BookA);

        Assert.That(block, Does.Contain("BOOK PLOT STATE"));
        Assert.That(block, Does.Contain("[Crisis — Escalated]"));
        Assert.That(block, Does.Contain("crisis:ambush: The ambush worsens."));
        Assert.That(block, Does.Not.Contain("Escalated ✓"));
    }

    [Test]
    public async Task BuildContextAsync_TerminalEvent_MarkedWithCheckmarkAndDoNotReopenFooter()
    {
        using var fixture = CanonFixture.Create();
        using var _ = CanonFixture.ScopeTo(CanonFixture.UniverseA);
        var svc = MakeService(fixture);

        await svc.RecordAsync(fixture.BookA, null, -1, "crisis:ambush", "Crisis", "resolve", "The ambush ends.", "Resolved");

        var block = await svc.BuildContextAsync(fixture.BookA);

        Assert.That(block, Does.Contain("[Crisis — Resolved ✓]"));
        Assert.That(block, Does.Contain("Do NOT re-open or re-establish states marked"));
        Assert.That(block, Does.Contain("Do not re-introduce crises already Resolved."));
    }

    [Test]
    public async Task BuildContextAsync_MultipleStateTypes_GroupedAndOrderedByStateType()
    {
        using var fixture = CanonFixture.Create();
        using var _ = CanonFixture.ScopeTo(CanonFixture.UniverseA);
        var svc = MakeService(fixture);

        await svc.RecordAsync(fixture.BookA, null, -1, "objective:evacuate", "Objective", "establish", "Evacuate the block.", "Active");
        await svc.RecordAsync(fixture.BookA, null, -1, "crisis:ambush", "Crisis", "open", "The ambush begins.", "Open");

        var block = await svc.BuildContextAsync(fixture.BookA);

        var crisisPos = block.IndexOf("[Crisis", StringComparison.Ordinal);
        var objectivePos = block.IndexOf("[Objective", StringComparison.Ordinal);
        Assert.That(crisisPos, Is.GreaterThan(-1));
        Assert.That(objectivePos, Is.GreaterThan(-1));
        Assert.That(crisisPos, Is.LessThan(objectivePos), "StateType groups render alphabetically (Crisis before Objective)");
    }
}
