using Prose.Core.Services;

namespace Prose.UnitTests.Fixtures;

/// <summary>
/// An <see cref="IUniverseContext"/> pinned to one universe, for driving the ambient
/// <see cref="UniverseScope"/> in tests.
///
/// <para>Several fixtures carry their own private <c>FakeUniverseContext</c> with this same body.
/// This is the shared one; new tests should use it rather than adding another copy.</para>
/// </summary>
internal sealed class FixedUniverseContext(Guid universeId, string slug = "test-universe") : IUniverseContext
{
    public Guid CurrentId { get; private set; } = universeId;

    /// <summary>
    /// A context pinned to a universe HAS named it — which is what an explicit scope means to
    /// <c>UnscopedUniverseWriteCheck</c>. <c>Guid.Empty</c> means no universe is wired at all, and
    /// there scoping is a no-op that nothing gates on.
    /// </summary>
    public bool IsExplicitlyScoped => CurrentId != Guid.Empty;

    public string CurrentSlug => slug;
    public UniverseInfo? CurrentUniverse => new(CurrentId, slug, "Test", null, null, true, 100);
    public IReadOnlyList<UniverseInfo> ListUniverses() => [CurrentUniverse!];
    public bool IsGlmz => false;
    public string UniverseGroundingOr(string glmzFallback) => glmzFallback;
    public void UseUniverse(Guid newId) => CurrentId = newId;
    public bool UseUniverseBySlug(string s) => false;
    public void SetFlowUniverse(Guid? newId) { }
    public void PersistAsDefault(Guid newId) { }
    public void Refresh() { }
}
