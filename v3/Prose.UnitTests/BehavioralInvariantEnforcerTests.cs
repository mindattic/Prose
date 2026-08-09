using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Interfaces;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Regression cover for the 2026-08-09 fix to BehavioralInvariantEnforcer.EnforceAsync/
/// ParseViolations. Both used to swallow every evaluation failure (LLM exception, empty
/// response, unparseable JSON) into an empty violations list — indistinguishable from "checked,
/// found nothing." BookHealthService.BehaviorCheckAsync's purge-then-refile Findings cycle
/// could not tell the difference, so an LLM outage silently deleted real prior BEHAVIOR findings
/// and never re-added them. Same defect family as the SwainAuditService fix earlier this
/// session — "the check never ran" must never be indistinguishable from "the check ran and
/// found nothing." Fixed by letting both methods throw on genuine failure instead.
/// </summary>
[TestFixture]
public class BehavioralInvariantEnforcerTests
{
    private string tempRoot = "";
    private TestPathProviderWithRoot paths = null!;
    private IDbContextFactory<ProseDbContext> dbFactory = null!;

    [SetUp]
    public void SetUp()
    {
        tempRoot = Path.Combine(Path.GetTempPath(), "ss-behavenforce-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        paths = new TestPathProviderWithRoot(tempRoot);
        dbFactory = TestDbFactory.For(paths, "behavior");
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        try { Directory.Delete(tempRoot, recursive: true); } catch { }
    }

    private async Task<Guid> SeedCharacterWithRuleAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var id = Guid.CreateVersion7();
        db.Entities.Add(new Entity
        {
            Id = id,
            EntityType = "character",
            Name = "Kessa",
            Slug = "kessa-" + id.ToString("N")[..8],
            Status = "canon",
        });
        db.Characters.Add(new Character { Id = id, Name = "Kessa" }); // TPT subtype row — CharacterBehavioralRules FKs here, not Entities
        db.CharacterBehavioralRules.Add(new CharacterBehavioralRule
        {
            CharacterId = id,
            Bucket = "decision_rules",
            Position = 0,
            Rule = "Never abandons a job partner mid-run.",
        });
        await db.SaveChangesAsync();
        return id;
    }

    private sealed class ThrowingLlm : ILlmService
    {
        public Task<bool> IsConfiguredAsync() => Task.FromResult(true);
        public Task<string> GenerateAsync(string system, string user,
            double temperature = 0.8, int maxTokens = 4096, string? model = null, CancellationToken ct = default) =>
            throw new InvalidOperationException("Circuit breaker open for provider 'claude-api'.");
    }

    private sealed class FixedResponseLlm(string response) : ILlmService
    {
        public Task<bool> IsConfiguredAsync() => Task.FromResult(true);
        public Task<string> GenerateAsync(string system, string user,
            double temperature = 0.8, int maxTokens = 4096, string? model = null, CancellationToken ct = default) =>
            Task.FromResult(response);
    }

    [Test]
    public async Task EnforceAsync_LlmThrows_PropagatesRatherThanReturningEmpty()
    {
        var charId = await SeedCharacterWithRuleAsync();
        var enforcer = new BehavioralInvariantEnforcer(dbFactory, new ThrowingLlm());

        Assert.That(async () => await enforcer.EnforceAsync("Some beat prose.", charId),
            Throws.InstanceOf<InvalidOperationException>());
    }

    [Test]
    public async Task EnforceAsync_EmptyLlmResponse_ThrowsRatherThanReturningEmpty()
    {
        var charId = await SeedCharacterWithRuleAsync();
        var enforcer = new BehavioralInvariantEnforcer(dbFactory, new FixedResponseLlm("   "));

        Assert.That(async () => await enforcer.EnforceAsync("Some beat prose.", charId),
            Throws.InvalidOperationException);
    }

    [Test]
    public async Task EnforceAsync_NoJsonArrayInResponse_ThrowsRatherThanReturningEmpty()
    {
        var charId = await SeedCharacterWithRuleAsync();
        var enforcer = new BehavioralInvariantEnforcer(dbFactory, new FixedResponseLlm("I cannot evaluate this."));

        Assert.That(async () => await enforcer.EnforceAsync("Some beat prose.", charId),
            Throws.InvalidOperationException);
    }

    [Test]
    public async Task EnforceAsync_MalformedJson_ThrowsRatherThanReturningEmpty()
    {
        var charId = await SeedCharacterWithRuleAsync();
        var enforcer = new BehavioralInvariantEnforcer(dbFactory, new FixedResponseLlm("[{\"bucket\": oops}]"));

        Assert.That(async () => await enforcer.EnforceAsync("Some beat prose.", charId),
            Throws.Exception); // JsonException specifically, but any exception proves it didn't swallow to [].
    }

    [Test]
    public async Task EnforceAsync_ValidEmptyArray_ReturnsEmptyListWithoutThrowing()
    {
        // A well-behaved model saying "no violations" (literal "[]") must NOT be treated as a
        // failure — only genuinely unparseable/empty/exception cases should throw.
        var charId = await SeedCharacterWithRuleAsync();
        var enforcer = new BehavioralInvariantEnforcer(dbFactory, new FixedResponseLlm("[]"));

        var violations = await enforcer.EnforceAsync("Some beat prose.", charId);
        Assert.That(violations, Is.Empty);
    }

    [Test]
    public async Task EnforceAsync_ValidViolation_ParsesCorrectly()
    {
        var charId = await SeedCharacterWithRuleAsync();
        var raw = """[{"bucket":"decision_rules","rule":"Never abandons a job partner","explanation":"Kessa leaves Dren behind at the door."}]""";
        var enforcer = new BehavioralInvariantEnforcer(dbFactory, new FixedResponseLlm(raw));

        var violations = await enforcer.EnforceAsync("Some beat prose.", charId);
        Assert.That(violations, Has.Count.EqualTo(1));
        Assert.That(violations[0].CharacterName, Is.EqualTo("Kessa"));
        Assert.That(violations[0].RuleBucket, Is.EqualTo("decision_rules"));
    }

    [Test]
    public async Task EnforceAsync_NoRulesForCharacter_ReturnsEmptyWithoutCallingLlm()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var id = Guid.CreateVersion7();
        db.Entities.Add(new Entity { Id = id, UniverseId = Guid.CreateVersion7(), EntityType = "character", Name = "NoRules", Slug = "no-rules", Status = "canon" });
        await db.SaveChangesAsync();

        var enforcer = new BehavioralInvariantEnforcer(dbFactory, new ThrowingLlm()); // would throw if called
        var violations = await enforcer.EnforceAsync("Some beat prose.", id);
        Assert.That(violations, Is.Empty);
    }
}
