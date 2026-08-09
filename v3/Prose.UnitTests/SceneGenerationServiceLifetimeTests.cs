using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Extensions;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Regression cover for the 2026-08-09 cross-user/cross-session event-leak fix.
/// <see cref="SceneGenerationService"/> exposes instance events (OnBeatProgress/OnBeatCompleted)
/// that GenerateScene.razor subscribes to per page visit. Registered Singleton, it was shared by
/// every Blazor Server circuit — two open /generate tabs (same user or different users) were
/// subscribed to the SAME event source, so clicking "Generate" in one tab fired the callback in
/// every other open tab, silently populating an unrelated session's UI with someone else's
/// generated content. This can't be exercised through a real Blazor circuit without a component
/// test harness (none exists in this project), but the actual defect was the DI *lifetime*, not
/// any rendering behavior — so pinning the lifetime here directly protects the fix: a future
/// refactor reverting to AddSingleton (or any lifetime broader than Scoped) reintroduces the
/// exact leak, and this test fails the moment that happens, without needing bUnit at all.
/// </summary>
[TestFixture]
public class SceneGenerationServiceLifetimeTests
{
    [Test]
    public void SceneGenerationService_IsRegisteredScoped_NotSingletonOrTransient()
    {
        var services = new ServiceCollection();
        services.AddProseServices();

        var descriptor = services.LastOrDefault(d => d.ServiceType == typeof(SceneGenerationService));

        Assert.That(descriptor, Is.Not.Null, "SceneGenerationService must be registered");
        Assert.That(descriptor!.Lifetime, Is.EqualTo(ServiceLifetime.Scoped),
            "SceneGenerationService exposes per-request instance events (OnBeatProgress/" +
            "OnBeatCompleted) that a Razor page subscribes to — anything broader than Scoped " +
            "(Singleton) shares that event source across every Blazor Server circuit, leaking " +
            "one user's/tab's generated content into every other open session's UI.");
    }
}
