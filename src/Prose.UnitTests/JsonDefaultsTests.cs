namespace Prose.UnitTests;

using System.Text.Json;
using Prose.Core;

[TestFixture]
public class JsonDefaultsTests
{
    [Test]
    public void LlmParsing_IsCaseInsensitive()
    {
        var json = """{"Name":"test","Value":42}""";
        var result = JsonSerializer.Deserialize<TestDto>(json, JsonDefaults.LlmParsing);
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.name, Is.EqualTo("test"));
        Assert.That(result.value, Is.EqualTo(42));
    }

    [Test]
    public void Indented_ProducesFormattedOutput()
    {
        var obj = new { name = "test" };
        var json = JsonSerializer.Serialize(obj, JsonDefaults.Indented);
        Assert.That(json, Does.Contain("\n"));
        Assert.That(json, Does.Contain("  "));
    }

    [Test]
    public void LlmParsing_IgnoresNullOnWrite()
    {
        var obj = new TestDto { name = "test", value = 0 };
        var json = JsonSerializer.Serialize(obj, JsonDefaults.LlmParsing);
        Assert.That(json, Does.Not.Contain("extra"));
    }

    [Test]
    public void SnakeCase_ProducesSnakeCaseKeys()
    {
        var obj = new { myField = "val" };
        var json = JsonSerializer.Serialize(obj, JsonDefaults.SnakeCase);
        Assert.That(json, Does.Contain("my_field"));
    }

    private class TestDto
    {
        public string name { get; set; } = "";
        public int value { get; set; }
        public string? extra { get; set; }
    }
}
