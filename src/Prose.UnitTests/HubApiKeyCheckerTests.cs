using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Locks in the fail-closed contract of <see cref="HubApiKeyChecker"/> — the comparison logic
/// behind the Hub's shared-secret gate (portable-writing-service plan, Phase 1).
/// </summary>
[TestFixture]
public class HubApiKeyCheckerTests
{
    [Test]
    public void IsAuthorized_MissingProvidedHeader_ReturnsFalse()
    {
        Assert.That(HubApiKeyChecker.IsAuthorized(null, "expected-key"), Is.False);
        Assert.That(HubApiKeyChecker.IsAuthorized("", "expected-key"), Is.False);
    }

    [Test]
    public void IsAuthorized_ExpectedKeyUnconfigured_ReturnsFalse()
    {
        // Should not happen in practice (the Hub generates a key before it starts listening),
        // but an empty expected key must never be treated as "anything goes."
        Assert.That(HubApiKeyChecker.IsAuthorized("some-key", null), Is.False);
        Assert.That(HubApiKeyChecker.IsAuthorized("some-key", ""), Is.False);
    }

    [Test]
    public void IsAuthorized_WrongKey_ReturnsFalse()
    {
        Assert.That(HubApiKeyChecker.IsAuthorized("wrong-key", "expected-key"), Is.False);
    }

    [Test]
    public void IsAuthorized_DifferentLengthKey_ReturnsFalse()
    {
        Assert.That(HubApiKeyChecker.IsAuthorized("short", "a-much-longer-expected-key"), Is.False);
    }

    [Test]
    public void IsAuthorized_CorrectKey_ReturnsTrue()
    {
        Assert.That(HubApiKeyChecker.IsAuthorized("the-real-key", "the-real-key"), Is.True);
    }

    [Test]
    public void IsAuthorized_IsCaseSensitive()
    {
        Assert.That(HubApiKeyChecker.IsAuthorized("The-Real-Key", "the-real-key"), Is.False);
    }
}
