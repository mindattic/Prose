using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Pins the 2026-08-14 numeric-safe comparison fix: ContinuityService's contradiction check
/// used to be bare string equality, so "fifty" vs "50" (same value, different LLM phrasing
/// across sweep rounds) registered as a false CONTRADICTED pair — the exact arithmetic-drift
/// bug class VIGL hit this session. NumericPredicates is an explicit allowlist so every other
/// predicate (location, relationship, etc.) keeps its original string-equality behavior.
/// </summary>
[TestFixture]
public class ContinuityServiceTests
{
    private string tempRoot = "";
    private TestPathProviderWithRoot paths = null!;
    private IDbContextFactory<ProseDbContext> dbFactory = null!;
    private ContinuityService svc = null!;

    [SetUp]
    public void SetUp()
    {
        tempRoot = Path.Combine(Path.GetTempPath(), "ss-continuity-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        paths = new TestPathProviderWithRoot(tempRoot);
        dbFactory = TestDbFactory.For(paths, "nodes");
        svc = new ContinuityService(dbFactory);
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, recursive: true);
    }

    private static ContinuityClaim MakeClaim(string entityId, string predicate, string objectValue) => new()
    {
        EntityId   = entityId,
        EntityName = "Orim Zebulun",
        EntityKind = "person",
        Predicate  = predicate,
        Object     = objectValue,
        SourceType = "prose",
        Snippet    = objectValue,
        Voice      = "narrator",
        Confidence = "high",
    };

    [TestCase("fifty", "50")]
    [TestCase("50", "fifty")]
    [TestCase("fifty-nine", "59")]
    [TestCase("fifty nine", "59 years")]
    public void ObjectsMatch_NumericPredicate_WordAndDigitFormsAgree(string a, string b)
    {
        Assert.That(ContinuityService.ObjectsMatch("career_length_years", a, b), Is.True);
    }

    [Test]
    public void ObjectsMatch_NumericPredicate_GenuinelyDifferentValues_DoNotMatch()
    {
        Assert.That(ContinuityService.ObjectsMatch("career_length_years", "fifty", "fifty-nine"), Is.False);
    }

    [Test]
    public void ObjectsMatch_NonNumericPredicate_UsesStringEquality()
    {
        // Regression guard: predicates NOT in the allowlist keep the original
        // ToLower/Trim string-equality semantics untouched.
        Assert.That(ContinuityService.ObjectsMatch("lives_at", "Bressant Station", "bressant station "), Is.True);
        Assert.That(ContinuityService.ObjectsMatch("lives_at", "Bressant Station", "Caer Glas Moor"), Is.False);
    }

    [Test]
    public void TryParseNumericValue_ParsesDigitsWordsAndCompounds()
    {
        Assert.That(ContinuityService.TryParseNumericValue("50", out var a) && a == 50, Is.True);
        Assert.That(ContinuityService.TryParseNumericValue("fifty", out var b) && b == 50, Is.True);
        Assert.That(ContinuityService.TryParseNumericValue("fifty-nine", out var c) && c == 59, Is.True);
        Assert.That(ContinuityService.TryParseNumericValue("fifty nine years old", out var d) && d == 59, Is.True);
        Assert.That(ContinuityService.TryParseNumericValue("not a number", out _), Is.False);
    }

    [Test]
    public void Upsert_SameNumericFactDifferentPhrasing_ConfirmsNotContradicts()
    {
        var entityId = Guid.NewGuid().ToString("N");
        var first = svc.Upsert(MakeClaim(entityId, "career_length_years", "fifty"));
        Assert.That(first.Outcome, Is.EqualTo("NEW"));

        var second = svc.Upsert(MakeClaim(entityId, "career_length_years", "50"));
        Assert.That(second.Outcome, Is.EqualTo("CONFIRMED"),
            "\"fifty\" and \"50\" are the same fact and must collapse to one claim, not contradict.");
    }

    [Test]
    public void Upsert_GenuinelyDifferentNumericFact_Contradicts()
    {
        var entityId = Guid.NewGuid().ToString("N");
        svc.Upsert(MakeClaim(entityId, "career_length_years", "fifty"));

        var second = svc.Upsert(MakeClaim(entityId, "career_length_years", "sixty"));
        Assert.That(second.Outcome, Is.EqualTo("CONTRADICTED"),
            "fifty vs sixty is a real arithmetic discrepancy and must still be flagged.");
    }

    [Test]
    public void Upsert_DistinctClocksOnDistinctPredicates_NeverCompared()
    {
        // The session's actual bug: a character's career length (50) and a separate
        // catastrophe's age-in-years (59) are two different clocks. As long as they're
        // recorded under DIFFERENT predicate keys, they must never be compared to each other.
        var entityId = Guid.NewGuid().ToString("N");
        svc.Upsert(MakeClaim(entityId, "career_length_years", "fifty"));
        var zoneAge = svc.Upsert(MakeClaim(entityId, "zone_age_years", "fifty-nine"));
        Assert.That(zoneAge.Outcome, Is.EqualTo("NEW"),
            "a different predicate is a different clock — must never contradict career_length_years.");
    }

    [Test]
    public void Upsert_NonNumericPredicate_StillContradictsOnDifferentText()
    {
        var entityId = Guid.NewGuid().ToString("N");
        svc.Upsert(MakeClaim(entityId, "lives_at", "Bressant Station"));
        var second = svc.Upsert(MakeClaim(entityId, "lives_at", "Caer Glas Moor"));
        Assert.That(second.Outcome, Is.EqualTo("CONTRADICTED"));
    }

    [Test]
    public void GetContradictionGroups_BookSlugFilter_ScopesToOneBook()
    {
        var entityA = Guid.NewGuid().ToString("N");
        var entityB = Guid.NewGuid().ToString("N");

        var claimA1 = MakeClaim(entityA, "career_length_years", "fifty");
        claimA1.BookSlug = "VIGL";
        svc.Upsert(claimA1);
        var claimA2 = MakeClaim(entityA, "career_length_years", "sixty");
        claimA2.BookSlug = "VIGL";
        svc.Upsert(claimA2);

        var claimB1 = MakeClaim(entityB, "lives_at", "Bressant Station");
        claimB1.BookSlug = "BCODA";
        svc.Upsert(claimB1);
        var claimB2 = MakeClaim(entityB, "lives_at", "Caer Glas Moor");
        claimB2.BookSlug = "BCODA";
        svc.Upsert(claimB2);

        var viglGroups = svc.GetContradictionGroups("VIGL");
        Assert.That(viglGroups.Select(g => g.EntityId), Does.Contain(entityA));
        Assert.That(viglGroups.Select(g => g.EntityId), Does.Not.Contain(entityB));

        var all = svc.GetContradictionGroups();
        Assert.That(all.Select(g => g.EntityId), Is.SupersetOf(new[] { entityA, entityB }));
    }
}
