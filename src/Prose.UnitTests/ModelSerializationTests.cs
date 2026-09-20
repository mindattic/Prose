using System.Text.Json;
using Prose.Core.Models.Canon;

namespace Prose.UnitTests;

/// <summary>
/// Verifies JSON serialization round-trips for all canon entity models.
/// Ensures generated JSON files will deserialize correctly.
/// </summary>
[TestFixture]
public class ModelSerializationTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    [Test]
    public void AutomatonData_RoundTrips()
    {
        var original = new AutomatonData
        {
            Name = "KS-4 Knitter",
            Classification = "Spider Platform",
            Manufacturer = "ARCTURUS",
            Armament = ["flechette dispersal", "leg spikes"],
            Sensors = ["thermal", "acoustic"],
            Tags = ["automaton", "spider", "lethal"]
        };

        var json = JsonSerializer.Serialize(original, Options);
        var deserialized = JsonSerializer.Deserialize<AutomatonData>(json, Options);

        Assert.That(deserialized, Is.Not.Null);
        Assert.That(deserialized!.Name, Is.EqualTo("KS-4 Knitter"));
        Assert.That(deserialized.Armament, Has.Count.EqualTo(2));
        Assert.That(deserialized.Tags, Has.Count.EqualTo(3));
    }

    [Test]
    public void WeaponryData_RoundTrips()
    {
        var original = new WeaponryData
        {
            Name = "Hearthstone HM-7",
            Category = "pistol",
            Manufacturer = "HEARTHSTONE FIREARMS",
            Tags = ["weapon", "pistol", "lethal", "tier 2"]
        };

        var json = JsonSerializer.Serialize(original, Options);
        var deserialized = JsonSerializer.Deserialize<WeaponryData>(json, Options);

        Assert.That(deserialized, Is.Not.Null);
        Assert.That(deserialized!.Category, Is.EqualTo("pistol"));
        Assert.That(deserialized.Tags, Does.Contain("lethal"));
    }

    [Test]
    public void CharacterData_Tags_SerializesAtTopLevel()
    {
        var character = new CharacterData
        {
            Name = "Kyle",
            Tags = ["protagonist", "runner", "augmented"]
        };

        var json = JsonSerializer.Serialize(character, Options);
        Assert.That(json, Does.Contain("\"tags\""));

        var deserialized = JsonSerializer.Deserialize<CharacterData>(json, Options);
        Assert.That(deserialized!.Tags, Has.Count.EqualTo(3));
        Assert.That(deserialized.Tags, Does.Contain("runner"));
    }

    [Test]
    public void CharacterStats_UsesStatTags_JsonPropertyName()
    {
        var stats = new CharacterStats { StatTags = ["combat", "stealth"] };
        var json = JsonSerializer.Serialize(stats, Options);

        // The JSON property name should be "tags" (matching the attribute) but the C# property is StatTags
        Assert.That(json, Does.Contain("\"tags\""));

        var deserialized = JsonSerializer.Deserialize<CharacterStats>(json, Options);
        Assert.That(deserialized!.StatTags, Has.Count.EqualTo(2));
    }

    [Test]
    public void ApparelData_RoundTrips()
    {
        var original = new ApparelData
        {
            Name = "Steelweave Jacket",
            Category = "jacket",
            Materials = ["carbon fiber", "synth-leather"],
            AugCompatible = true,
            Tags = ["apparel", "jacket", "tier 3"]
        };

        var json = JsonSerializer.Serialize(original, Options);
        var deserialized = JsonSerializer.Deserialize<ApparelData>(json, Options);

        Assert.That(deserialized!.AugCompatible, Is.True);
        Assert.That(deserialized.Materials, Has.Count.EqualTo(2));
    }

    [Test]
    public void WorldbuildingDocument_Tags_RoundTrips()
    {
        var doc = new WorldbuildingDocument
        {
            FileName = "test.json",
            Title = "Test Document",
            Tags = ["document", "classified", "arcturus"]
        };

        var json = JsonSerializer.Serialize(doc, Options);
        var deserialized = JsonSerializer.Deserialize<WorldbuildingDocument>(json, Options);

        Assert.That(deserialized!.Tags, Has.Count.EqualTo(3));
        Assert.That(deserialized.Tags, Does.Contain("classified"));
    }

    [Test]
    public void AllModels_GenerateStableIds()
    {
        var automaton = new AutomatonData();
        var weapon = new WeaponryData();
        var character = new CharacterData();

        Assert.That(automaton.Id, Is.Not.Null.And.Not.Empty);
        Assert.That(weapon.Id, Is.Not.Null.And.Not.Empty);
        Assert.That(character.Id, Is.Not.Null.And.Not.Empty);

        // IDs should be unique
        Assert.That(automaton.Id, Is.Not.EqualTo(weapon.Id));
        Assert.That(weapon.Id, Is.Not.EqualTo(character.Id));
    }

    [Test]
    public void Deserialize_MissingTags_DefaultsToEmpty()
    {
        // Simulate a JSON file without tags field
        var json = """{"name":"Old Entry","type":"weapon","category":"pistol"}""";
        var deserialized = JsonSerializer.Deserialize<WeaponryData>(json, Options);

        Assert.That(deserialized, Is.Not.Null);
        Assert.That(deserialized!.Tags, Is.Not.Null);
        Assert.That(deserialized.Tags, Is.Empty);
    }

    [Test]
    public void Deserialize_ExtraFields_Ignored()
    {
        // JSON with fields not in the model should not throw
        var json = """{"name":"Test","type":"weapon","unknown_field":"value","tags":["test"]}""";
        Assert.DoesNotThrow(() => JsonSerializer.Deserialize<WeaponryData>(json, Options));
    }
}
