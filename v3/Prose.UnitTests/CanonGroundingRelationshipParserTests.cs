using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Regression tests for <see cref="CanonGroundingService.TryParseRelationshipClaim"/>.
///
/// Origin (2026-09-02): the parser was a bare <c>claim.Split(" of ", 2)</c> whose result was
/// written to CharacterRelationships unconditionally. A claim containing no " of " produced a row
/// with <c>Name = ""</c> and the entire raw sentence duplicated into both Type and Description.
/// Seven such rows — sentences about Kyle from BCODA — ended up grafted onto "Seo Jisun", an
/// unrelated she/her character belonging to a different book (Testament), including a row reading
/// "his funeral". The UnparsedClaims fixture below is those seven literal strings, read off the
/// live record. They are the acceptance criteria: none of them may ever become a relationship row.
/// </summary>
[TestFixture]
public class CanonGroundingRelationshipParserTests
{
    /// <summary>The seven claims found contaminating the live Seo Jisun record.</summary>
    private static readonly string[] SeoJisunContamination =
    [
        "trained Kyle in blade work",
        "gave Kyle his katana",
        "listed as program consultant on Ferrogate BioSystems manifest",
        "contribution record active from 2189 through 2193",
        "status listed as deceased, file closed",
        "Kyle was not informed of his funeral",
        "his funeral"
    ];

    [Test]
    public void SeoJisunContamination_IsRejectedEntirely()
    {
        foreach (var claim in SeoJisunContamination)
        {
            var parsed = CanonGroundingService.TryParseRelationshipClaim(claim);
            Assert.That(parsed, Is.Null, $"Claim should not have parsed into a relationship: '{claim}'");
        }
    }

    // ── The invariant: never an empty target ─────────────────────────────────

    [Test]
    public void AnyParsedClaim_AlwaysHasNonEmptyTypeAndTarget()
    {
        string[] everything = [.. SeoJisunContamination, "nephew of Barber Vasquez", "works for Arcturus",
            "of", " of ", "of Arcturus", "member of", "", "   ", "allied with the Lotus Syndicate"];

        foreach (var claim in everything)
        {
            var parsed = CanonGroundingService.TryParseRelationshipClaim(claim);
            if (parsed is null) continue;
            Assert.That(parsed.Name, Is.Not.Empty, $"Empty target from '{claim}'");
            Assert.That(parsed.Type, Is.Not.Empty, $"Empty type from '{claim}'");
        }
    }

    // ── Genuine relationship claims still parse ──────────────────────────────

    [Test]
    public void NephewOf_ParsesTypeAndTarget()
    {
        var parsed = CanonGroundingService.TryParseRelationshipClaim("nephew of Barber Vasquez");
        Assert.That(parsed, Is.Not.Null);
        Assert.That(parsed!.Type, Is.EqualTo("nephew"));
        Assert.That(parsed.Name, Is.EqualTo("Barber Vasquez"));
        Assert.That(parsed.Description, Is.EqualTo("nephew of Barber Vasquez"));
    }

    [Test]
    public void WorksFor_ParsesTypeAndTarget()
    {
        var parsed = CanonGroundingService.TryParseRelationshipClaim("works for Arcturus");
        Assert.That(parsed, Is.Not.Null);
        Assert.That(parsed!.Type, Is.EqualTo("works"));
        Assert.That(parsed.Name, Is.EqualTo("Arcturus"));
    }

    [Test]
    public void DefiniteDescriptionTarget_IsAccepted()
    {
        var parsed = CanonGroundingService.TryParseRelationshipClaim("allied with the Lotus Syndicate");
        Assert.That(parsed, Is.Not.Null);
        Assert.That(parsed!.Name, Is.EqualTo("the Lotus Syndicate"));
    }

    // ── The specific shapes that let the contamination through ───────────────

    [Test]
    public void SentenceFragmentType_IsRejected()
    {
        // "Kyle was not informed" is four words — a clause, not a relation type.
        Assert.That(CanonGroundingService.TryParseRelationshipClaim("Kyle was not informed of his funeral"), Is.Null);
    }

    [Test]
    public void DateRangeTarget_IsRejected()
    {
        // A target starting with a digit is a date/quantity, never a named entity.
        Assert.That(CanonGroundingService.TryParseRelationshipClaim("active from 2189 through 2193"), Is.Null);
    }

    [Test]
    public void PossessiveTarget_IsRejected()
    {
        Assert.That(CanonGroundingService.TryParseRelationshipClaim("informed of his funeral"), Is.Null);
    }

    [Test]
    public void ClaimWithNoConnector_IsRejected()
    {
        Assert.That(CanonGroundingService.TryParseRelationshipClaim("gave Kyle his katana"), Is.Null);
    }

    [Test]
    public void NullOrWhitespace_IsRejected()
    {
        Assert.That(CanonGroundingService.TryParseRelationshipClaim(null!), Is.Null);
        Assert.That(CanonGroundingService.TryParseRelationshipClaim(""), Is.Null);
        Assert.That(CanonGroundingService.TryParseRelationshipClaim("   "), Is.Null);
    }

    [Test]
    public void ConnectorWithEmptySide_IsRejected()
    {
        Assert.That(CanonGroundingService.TryParseRelationshipClaim("of Arcturus"), Is.Null, "no type");
        Assert.That(CanonGroundingService.TryParseRelationshipClaim("member of"), Is.Null, "no target");
    }
}
