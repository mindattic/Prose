using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Tests for SceneCollisionService.ParseCollisionResponse — the JSON-parsing half of the
/// causal collision engine (2026-08-10, see memory: project_causal_collision_engine_vision).
/// Extracted from ComputeAsync into an internal static method specifically so this logic is
/// directly testable without an LLM dependency, matching the established pattern
/// (EmotionalDepthService.ParseBeatCurve).
/// </summary>
[TestFixture]
public class SceneCollisionServiceTests
{
    [Test]
    public void ParseCollisionResponse_ValidJson_ParsesAllFields()
    {
        var raw = """
            {
              "mechanics": "Kaeric's hand finds the axe before his mind catches up to why.",
              "reactions": [
                {"name": "Kaeric", "reaction": "goes still, jaw tight, the old reflex overriding thought"},
                {"name": "Kressida", "reaction": "steps between him and the door without being asked"}
              ],
              "new_consequence": "Kaeric no longer trusts the runner boy's errands.",
              "rationale": "Kaeric's documented wound (betrayed by a trusted courier once before) fires before conscious judgment does."
            }
            """;

        var result = SceneCollisionService.ParseCollisionResponse(raw);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Mechanics, Does.Contain("axe"));
        Assert.That(result.Reactions, Has.Count.EqualTo(2));
        Assert.That(result.Reactions[0].Name, Is.EqualTo("Kaeric"));
        Assert.That(result.Reactions[1].Name, Is.EqualTo("Kressida"));
        Assert.That(result.NewConsequence, Does.Contain("runner boy"));
        Assert.That(result.Rationale, Does.Contain("wound"));
    }

    [Test]
    public void ParseCollisionResponse_MissingMechanics_ReturnsNull()
    {
        var raw = """{"reactions": [], "rationale": "no mechanics field at all"}""";
        Assert.That(SceneCollisionService.ParseCollisionResponse(raw), Is.Null);
    }

    [Test]
    public void ParseCollisionResponse_EmptyMechanics_ReturnsNull()
    {
        var raw = """{"mechanics": "", "reactions": [], "rationale": ""}""";
        Assert.That(SceneCollisionService.ParseCollisionResponse(raw), Is.Null);
    }

    [Test]
    public void ParseCollisionResponse_MissingReactionsArray_DefaultsToEmpty()
    {
        var raw = """{"mechanics": "Something happens.", "rationale": "because"}""";
        var result = SceneCollisionService.ParseCollisionResponse(raw);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Reactions, Is.Empty);
    }

    [Test]
    public void ParseCollisionResponse_ReactionMissingName_IsSkippedNotThrown()
    {
        var raw = """
            {
              "mechanics": "Something happens.",
              "reactions": [{"reaction": "no name given"}, {"name": "Kaeric", "reaction": "reacts"}],
              "rationale": "because"
            }
            """;
        var result = SceneCollisionService.ParseCollisionResponse(raw);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Reactions, Has.Count.EqualTo(1));
        Assert.That(result.Reactions[0].Name, Is.EqualTo("Kaeric"));
    }

    [Test]
    public void ParseCollisionResponse_NewConsequenceNull_IsPreservedAsNull()
    {
        var raw = """{"mechanics": "Something happens.", "reactions": [], "new_consequence": null, "rationale": "because"}""";
        var result = SceneCollisionService.ParseCollisionResponse(raw);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.NewConsequence, Is.Null);
    }

    [Test]
    public void ParseCollisionResponse_WrappedInMarkdownFences_StillExtractsJson()
    {
        // LLMs occasionally wrap "JSON only" responses in fences anyway — ExtractJson
        // brace-slices from the first '{' to the last '}', same tolerance as
        // EmotionalDepthService's own ExtractJson helper.
        var raw = "```json\n{\"mechanics\": \"Something happens.\", \"reactions\": [], \"rationale\": \"because\"}\n```";
        var result = SceneCollisionService.ParseCollisionResponse(raw);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Mechanics, Is.EqualTo("Something happens."));
    }

    [Test]
    public void FormatForPrompt_IncludesMechanicsReactionsAndConsequence()
    {
        var collision = new SceneCollisionService.SceneCollision(
            Mechanics: "The door doesn't open the way it's supposed to.",
            Reactions: [new SceneCollisionService.CharacterReaction("Kaeric", "freezes")],
            NewConsequence: "The lock is now flagged as tampered.",
            Rationale: "because trust is the load-bearing wall here");

        var formatted = SceneCollisionService.FormatForPrompt(collision);

        Assert.That(formatted, Does.Contain("The door doesn't open"));
        Assert.That(formatted, Does.Contain("Kaeric: freezes"));
        Assert.That(formatted, Does.Contain("The lock is now flagged as tampered."));
    }

    [Test]
    public void FormatForPrompt_NoReactionsOrConsequence_OmitsThoseSections()
    {
        var collision = new SceneCollisionService.SceneCollision(
            Mechanics: "Something happens.",
            Reactions: [],
            NewConsequence: null,
            Rationale: "because");

        var formatted = SceneCollisionService.FormatForPrompt(collision);

        Assert.That(formatted, Does.Not.Contain("Per-character reaction"));
        Assert.That(formatted, Does.Not.Contain("New consequence"));
    }

    [Test]
    public async Task ComputeAsync_FewerThanTwoCharacters_ReturnsNullWithoutCallingLlm()
    {
        var svc = new SceneCollisionService(new ThrowingLlmService(), NullLoggerFor<SceneCollisionService>());
        var result = await svc.ComputeAsync(
            charactersInScene: ["Kaeric"],
            xRayContext: "some roster text",
            worldStateContext: "", consequenceContext: "",
            beatGoal: "Kaeric confronts the runner.", locationContext: "");

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task ComputeAsync_NoXRayContext_ReturnsNullWithoutCallingLlm()
    {
        var svc = new SceneCollisionService(new ThrowingLlmService(), NullLoggerFor<SceneCollisionService>());
        var result = await svc.ComputeAsync(
            charactersInScene: ["Kaeric", "Kressida"],
            xRayContext: "",
            worldStateContext: "", consequenceContext: "",
            beatGoal: "Kaeric confronts the runner.", locationContext: "");

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task ComputeAsync_NoBeatGoal_ReturnsNullWithoutCallingLlm()
    {
        var svc = new SceneCollisionService(new ThrowingLlmService(), NullLoggerFor<SceneCollisionService>());
        var result = await svc.ComputeAsync(
            charactersInScene: ["Kaeric", "Kressida"],
            xRayContext: "some roster text",
            worldStateContext: "", consequenceContext: "",
            beatGoal: "", locationContext: "");

        Assert.That(result, Is.Null);
    }

    /// <summary>Fails the test loudly if ComputeAsync's gate logic doesn't short-circuit before
    /// the LLM call, rather than silently returning null for the wrong reason.</summary>
    private sealed class ThrowingLlmService : Prose.Core.Interfaces.ILlmService
    {
        public Task<bool> IsConfiguredAsync() => Task.FromResult(true);
        public Task<string> GenerateAsync(string system, string user, double temperature = 0.8,
            int maxTokens = 4096, string? model = null, CancellationToken ct = default)
            => throw new InvalidOperationException("Gate should have short-circuited before calling the LLM.");
    }

    private static Microsoft.Extensions.Logging.ILogger<T> NullLoggerFor<T>() =>
        Microsoft.Extensions.Logging.Abstractions.NullLogger<T>.Instance;
}
