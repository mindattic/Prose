using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Regression cover for the truncated-array salvage in <c>ContinuityExtractionService</c>.
///
/// Found live 2026-08-24: a beat-save extraction logged "[continuity] extraction produced 0
/// candidates" while its own raw response plainly contained four complete, well-formed claims.
/// The whole-array parse needs a closing ']' and the regex fallback needs a literal "}]", so a
/// response cut off at maxTokens mid-object matched neither and the entire batch was discarded —
/// meaning that source contributed nothing to the fact ledger, which is point 2 of the
/// docs/LOGIC.md §9 publish gate. Salvage keeps every complete object and drops only the
/// half-written tail.
/// </summary>
[TestFixture]
public class ContinuityExtractionSalvageTests
{
    [Test]
    public void Salvage_TruncatedMidObject_KeepsTheCompleteObjects()
    {
        // Two complete claims, then the model runs out of tokens mid-snippet.
        var raw = """
            ```json
            [
              {"entity_name":"Lyra","predicate":"rank","object":"Lieutenant","confidence":"high"},
              {"entity_name":"Lyra","predicate":"organization_affiliation","object":"Vigil","confidence":"high"},
              {"entity_name":"Lyra","predicate":"weapon_carry","object":"shield","snippet":"She stepped into its path rather than away, shield u
            """;

        var salvaged = ContinuityExtractionService.SalvageCompleteObjects(raw);

        Assert.That(salvaged, Is.Not.Null, "the two complete claims must survive the truncated third");
        using var doc = System.Text.Json.JsonDocument.Parse(salvaged!);
        Assert.That(doc.RootElement.GetArrayLength(), Is.EqualTo(2));
        Assert.That(doc.RootElement[0].GetProperty("predicate").GetString(), Is.EqualTo("rank"));
        Assert.That(doc.RootElement[1].GetProperty("object").GetString(), Is.EqualTo("Vigil"));
    }

    [Test]
    public void Salvage_BracesAndBracketsInsideStrings_DoNotDesynchroniseTheScan()
    {
        // A prose snippet is free to contain braces, brackets and escaped quotes. If the scanner
        // counted those as structure it would mis-slice every object after the first one.
        var raw = """
            [
              {"entity_name":"Orim","predicate":"quote","object":"a }{ brace [bracket] mess","snippet":"he said \"stop\" once"},
              {"entity_name":"Doyle","predicate":"origin","object":"Sphere 31"}
            """;

        var salvaged = ContinuityExtractionService.SalvageCompleteObjects(raw);

        Assert.That(salvaged, Is.Not.Null);
        using var doc = System.Text.Json.JsonDocument.Parse(salvaged!);
        Assert.That(doc.RootElement.GetArrayLength(), Is.EqualTo(2));
        Assert.That(doc.RootElement[0].GetProperty("object").GetString(), Is.EqualTo("a }{ brace [bracket] mess"));
        Assert.That(doc.RootElement[1].GetProperty("object").GetString(), Is.EqualTo("Sphere 31"));
    }

    [Test]
    public void Salvage_NestedObjects_AreKeptWhole()
    {
        var raw = """
            [
              {"entity_name":"Vega","predicate":"role","object":"Scribe","meta":{"nested":{"deep":true}}},
              {"entity_name":"Wren","predicate":"skill","obj
            """;

        var salvaged = ContinuityExtractionService.SalvageCompleteObjects(raw);

        using var doc = System.Text.Json.JsonDocument.Parse(salvaged!);
        Assert.That(doc.RootElement.GetArrayLength(), Is.EqualTo(1));
        Assert.That(doc.RootElement[0].GetProperty("meta").GetProperty("nested").GetProperty("deep").GetBoolean(), Is.True);
    }

    [Test]
    public void Salvage_WellFormedArray_StillReturnsEveryObject()
    {
        // Salvage must be a superset of the happy path, not a special case for damaged input.
        var raw = """[{"a":1},{"b":2},{"c":3}]""";

        var salvaged = ContinuityExtractionService.SalvageCompleteObjects(raw);

        using var doc = System.Text.Json.JsonDocument.Parse(salvaged!);
        Assert.That(doc.RootElement.GetArrayLength(), Is.EqualTo(3));
    }

    [Test]
    public void Salvage_StopsAtTheClosingBracket_AndIgnoresTrailingProse()
    {
        var raw = """
            Here are the claims:
            [{"a":1},{"b":2}]
            I also noticed {"this":"is not a claim"} in the text.
            """;

        var salvaged = ContinuityExtractionService.SalvageCompleteObjects(raw);

        using var doc = System.Text.Json.JsonDocument.Parse(salvaged!);
        Assert.That(doc.RootElement.GetArrayLength(), Is.EqualTo(2),
            "objects after the array closed are commentary, not claims");
    }

    [Test]
    public void Salvage_NoCompleteObject_ReturnsNull()
    {
        Assert.That(ContinuityExtractionService.SalvageCompleteObjects("""[{"entity_name":"Lyr"""), Is.Null);
        Assert.That(ContinuityExtractionService.SalvageCompleteObjects("no json here at all"), Is.Null);
        Assert.That(ContinuityExtractionService.SalvageCompleteObjects(""), Is.Null);
    }

    [Test]
    public void Salvage_MalformedObjectAmongGoodOnes_DoesNotCostTheOthers()
    {
        // The middle object has a bare (unquoted) key and won't parse; it must be dropped alone.
        var raw = """[{"a":1},{bad:},{"c":3}]""";

        var salvaged = ContinuityExtractionService.SalvageCompleteObjects(raw);

        using var doc = System.Text.Json.JsonDocument.Parse(salvaged!);
        Assert.That(doc.RootElement.GetArrayLength(), Is.EqualTo(2));
    }
}
