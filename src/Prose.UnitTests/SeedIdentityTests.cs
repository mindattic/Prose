using NUnit.Framework;
using Prose.Cli;

namespace Prose.UnitTests;

/// <summary>
/// Guards seed-import identity resolution.
///
/// Why this is guarded by tests: canon models self-assign <c>Id</c> inline
/// (<c>= Guid.CreateVersion7().ToString("N")</c>), so a seed file that omits <c>"id"</c>
/// deserializes into an object holding a brand-new id. The repositories upsert by id, so that
/// INSERTS — and re-running the same import silently produces a second entity with the same name.
/// This actually happened: re-importing one corrected character file created a duplicate
/// "Anne Devlin" that only surfaced when WorldValidationTests.NoSameTypeNameCollisions failed.
///
/// Hand-authored seed files in this repo routinely omit "id", so this is the default path.
/// </summary>
[TestFixture]
public class SeedIdentityTests
{
    // ── HasExplicitId: must read the RAW JSON, not the deserialized object ────

    [Test]
    public void HasExplicitId_TrueWhenIdPresent()
        => Assert.That(SeedIdentity.HasExplicitId("""{"id":"abc123","name":"Anne Devlin"}"""), Is.True);

    [Test]
    public void HasExplicitId_FalseWhenIdAbsent()
        => Assert.That(SeedIdentity.HasExplicitId("""{"name":"Anne Devlin"}"""), Is.False);

    [Test]
    public void HasExplicitId_FalseWhenIdBlank()
    {
        Assert.Multiple(() =>
        {
            Assert.That(SeedIdentity.HasExplicitId("""{"id":"","name":"X"}"""), Is.False);
            Assert.That(SeedIdentity.HasExplicitId("""{"id":"   ","name":"X"}"""), Is.False);
        });
    }

    [Test]
    public void HasExplicitId_FalseWhenIdIsNullLiteral()
        => Assert.That(SeedIdentity.HasExplicitId("""{"id":null,"name":"X"}"""), Is.False);

    [Test]
    public void HasExplicitId_MatchesCaseInsensitively_LikeTheDeserializer()
    {
        // The importers deserialize with PropertyNameCaseInsensitive = true, so "ID" binds to Id.
        // If this check were case-sensitive it would disagree with the object it is reasoning about.
        Assert.That(SeedIdentity.HasExplicitId("""{"ID":"abc123","name":"X"}"""), Is.True);
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase("not json at all")]
    [TestCase("[1,2,3]")]
    public void HasExplicitId_FalseForUnusableInput(string raw)
        => Assert.That(SeedIdentity.HasExplicitId(raw), Is.False);

    // ── ResolveId: the actual duplicate-prevention behaviour ─────────────────

    private static string Slug(string name) => name.ToLowerInvariant().Replace(' ', '-');

    [Test]
    public void ResolveId_AdoptsExistingId_WhenSeedOmitsIdAndEntityExists()
    {
        var freshId = "brand-new-uuid";
        var resolved = SeedIdentity.ResolveId(
            rawJson: """{"name":"Anne Devlin"}""",
            currentId: freshId,
            name: "Anne Devlin",
            findIdBySlug: slug => slug == "anne-devlin" ? "existing-id-0001" : null,
            toSlug: Slug,
            out var wasExisting);

        Assert.Multiple(() =>
        {
            Assert.That(resolved, Is.EqualTo("existing-id-0001"),
                "Re-importing a seed file must update the existing row, not insert a duplicate.");
            Assert.That(wasExisting, Is.True);
        });
    }

    [Test]
    public void ResolveId_KeepsFreshId_WhenSeedOmitsIdAndEntityIsNew()
    {
        var freshId = "brand-new-uuid";
        var resolved = SeedIdentity.ResolveId(
            rawJson: """{"name":"Somebody New"}""",
            currentId: freshId,
            name: "Somebody New",
            findIdBySlug: _ => null,
            toSlug: Slug,
            out var wasExisting);

        Assert.Multiple(() =>
        {
            Assert.That(resolved, Is.EqualTo(freshId), "Genuinely new content must still insert.");
            Assert.That(wasExisting, Is.False);
        });
    }

    [Test]
    public void ResolveId_HonoursExplicitId_EvenWhenNameCollides()
    {
        // An explicit id is a deliberate instruction — including "retarget this name to that row".
        // It must win over the slug lookup.
        var resolved = SeedIdentity.ResolveId(
            rawJson: """{"id":"explicit-0009","name":"Anne Devlin"}""",
            currentId: "explicit-0009",
            name: "Anne Devlin",
            findIdBySlug: _ => "existing-id-0001",
            toSlug: Slug,
            out var wasExisting);

        Assert.Multiple(() =>
        {
            Assert.That(resolved, Is.EqualTo("explicit-0009"));
            Assert.That(wasExisting, Is.False, "An explicit id is not an adopted id.");
        });
    }

    [Test]
    public void ResolveId_DoesNotLookUp_WhenNameIsMissing()
    {
        var called = false;
        var resolved = SeedIdentity.ResolveId(
            rawJson: """{"description":"no name here"}""",
            currentId: "fresh",
            name: "",
            findIdBySlug: _ => { called = true; return "should-not-be-used"; },
            toSlug: Slug,
            out var wasExisting);

        Assert.Multiple(() =>
        {
            Assert.That(resolved, Is.EqualTo("fresh"));
            Assert.That(wasExisting, Is.False);
            Assert.That(called, Is.False, "A nameless seed has no slug to resolve; don't query.");
        });
    }

    [Test]
    public void ResolveId_IsIdempotent_AcrossRepeatedImports()
    {
        // The regression in one assertion: importing the same file three times must converge on
        // one id rather than minting a new one each pass.
        string? stored = null;
        var ids = new List<string>();
        for (var i = 0; i < 3; i++)
        {
            var id = SeedIdentity.ResolveId(
                rawJson: """{"name":"Anne Devlin"}""",
                currentId: $"fresh-uuid-{i}",
                name: "Anne Devlin",
                findIdBySlug: _ => stored,
                toSlug: Slug,
                out _);
            stored ??= id;          // first import creates the row
            ids.Add(id);
        }

        Assert.That(ids.Distinct().Count(), Is.EqualTo(1),
            $"Three imports produced {ids.Distinct().Count()} distinct ids: {string.Join(", ", ids)}");
    }
}
