using Prose.Core.Composition.Ledger;
using Prose.Core.Composition.Prompt;
using Prose.Core.Composition.Window;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// The prompt is a contract with context around it, not context with an instruction at the end.
///
/// <para>These tests exist because of a measured failure, not a hypothetical one. On a real BCODA5
/// beat the assembled prompt was 42,612 characters of which the instruction was 219 — 0.5% — and
/// it sat LAST, after 40KB of prior prose. The draft that came back invented a character and a
/// vehicle, emitted a markdown heading and a scene break, explained its own subtext, and ran three
/// times the length of the beat it replaced. Every one of those is forbidden by a line
/// <see cref="BeatBrief.ToPromptBlock"/> renders; the assembler simply never asked for it.</para>
///
/// <para>So the assertions below are mostly about ORDER and CEILINGS — the two things that were
/// wrong — and they are deliberately blunt: the brief must come first, the restatement must come
/// last, and no block may exceed its tier. No LLM: this is string assembly, and a fake model would
/// only hide what is being checked.</para>
/// </summary>
[TestFixture]
public class PromptAssemblerContractTests
{
    private static readonly OnScreenSnapshot EmptyFacts = new();

    private static IReadOnlyList<WindowedBeat> Window(params string[] texts) =>
        texts.Select((t, i) => new WindowedBeat(Guid.CreateVersion7(), Guid.CreateVersion7(), "Chapter 1", t, i + 1)).ToList();

    private static BeatBrief Brief(string goal = "Kyle counts what the evening cost him.", int targetWords = 400) =>
        new() { Goal = goal, TargetWords = targetWords, StopBefore = "He reaches the apartment." };

    // ── order: the two things that were wrong ────────────────────────────────

    [Test]
    public void TheBriefIsTheFirstThingInThePrompt()
    {
        var p = PromptAssembler.Assemble(
            "universe", Window("prior prose"), EmptyFacts, "Kyle", "The Narrows", "goal text", Brief());

        Assert.That(p.User.TrimStart(), Does.StartWith("THE BRIEF"),
            "the job must be the first thing the model reads, not the last");
    }

    [Test]
    public void TheJobIsRestatedAsTheFinalLine()
    {
        var p = PromptAssembler.Assemble(
            "universe", Window("prior prose"), EmptyFacts, "Kyle", "The Narrows", "goal text", Brief());

        var lastLine = p.User.TrimEnd().Split('\n')[^1];
        Assert.That(lastLine, Does.Contain("Write exactly"),
            "an instruction stated once, 40KB ago, is an instruction the model has stopped attending to");
    }

    [Test]
    public void ThePriorProseSitsBetweenTheBriefAndItsRestatement()
    {
        var p = PromptAssembler.Assemble(
            "universe", Window("THE_WINDOW_MARKER"), EmptyFacts, "Kyle", "The Narrows", "goal", Brief());

        var briefAt = p.User.IndexOf("THE BRIEF", StringComparison.Ordinal);
        var windowAt = p.User.IndexOf("THE_WINDOW_MARKER", StringComparison.Ordinal);
        var closingAt = p.User.IndexOf("Write exactly", StringComparison.Ordinal);

        Assert.That(briefAt, Is.GreaterThanOrEqualTo(0));
        Assert.That(windowAt, Is.GreaterThan(briefAt));
        Assert.That(closingAt, Is.GreaterThan(windowAt),
            "the last thing read before writing should be what just happened, then what to do");
    }

    // ── the rules that were absent ───────────────────────────────────────────

    [Test]
    public void TheBriefCarriesTheProhibitionsTheDraftBroke()
    {
        var p = PromptAssembler.Assemble(
            "universe", Window("prior"), EmptyFacts, "Kyle", "The Narrows", "goal", Brief());

        Assert.Multiple(() =>
        {
            Assert.That(p.User, Does.Contain("NO NEW NAMES"), "the draft invented a character called Cho");
            Assert.That(p.User, Does.Contain("NO NEW PLOT"));
            Assert.That(p.User, Does.Contain("OUTPUT: prose only"), "the draft emitted a markdown heading");
            Assert.That(p.User, Does.Contain("STOP BEFORE"), "the draft ran past the beat it was asked for");
            Assert.That(p.User, Does.Contain("LENGTH: about 400 words"));
        });
    }

    // ── ceilings: characters, not item counts ────────────────────────────────

    [Test]
    public void TheWindowIsCappedInCharactersAndKeepsTheEnd()
    {
        // The old cap was 15 BEATS, which on ~2,700-character beats is 40KB of prompt.
        var huge = new string('x', 20_000) + "CLOSEST_TO_THIS_BEAT";
        var p = PromptAssembler.Assemble(
            "universe", Window(huge), EmptyFacts, "Kyle", "The Narrows", "goal", Brief());

        Assert.That(p.BlockLengths["window"], Is.LessThanOrEqualTo(PromptAssembler.WindowCeiling + 200),
            "a beat count is not a size");
        Assert.That(p.User, Does.Contain("CLOSEST_TO_THIS_BEAT"),
            "when prior prose must be trimmed, the lines nearest this beat are the ones to keep");
    }

    [Test]
    public void TheVoiceBlockIsCapped()
    {
        var p = PromptAssembler.Assemble(
            "universe", Window("prior"), EmptyFacts, "Kyle", "The Narrows", "goal", Brief(),
            voiceAnchorBlock: new string('v', 10_000));

        Assert.That(p.BlockLengths["voice"], Is.LessThanOrEqualTo(PromptAssembler.VoiceCeiling));
    }

    [Test]
    public void BlockLengthsAreReportedForEveryBlock()
    {
        var p = PromptAssembler.Assemble(
            "universe", Window("prior"), EmptyFacts, "Kyle", "The Narrows", "goal", Brief(),
            voiceAnchorBlock: "voice");

        // Being able to SEE that the instruction was 0.5% of the prompt is what made the defect
        // findable at all; it must stay visible without adding log lines and redeploying.
        Assert.That(p.BlockLengths.Keys, Is.SupersetOf(new[] { "brief", "facts", "obligations", "scene", "window", "voice", "closing" }));
        Assert.That(p.BlockLengths.ContainsKey("spine"), Is.False, "the outline slice was removed 2026-09-22; nothing may re-add it");
        Assert.That(p.BlockLengths["brief"], Is.GreaterThan(0));
    }

    // ── the fallback is a fallback, and is visible as one ────────────────────

    [Test]
    public void WithoutABriefItFallsBackToTheBareGoal_TheShapeThatFailed()
    {
        var p = PromptAssembler.Assemble(
            "universe", Window("prior"), EmptyFacts, "Kyle", "The Narrows", "just the goal");

        Assert.That(p.User, Does.StartWith("BEAT GOAL:"));
        Assert.That(p.User, Does.Not.Contain("NO NEW NAMES"),
            "documents the cost of a missing brief: no prohibitions travel with it, and no gate runs");
    }
}
