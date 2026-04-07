using StreetSamurai.Core.Models.Canon;

namespace StreetSamurai.UnitTests;

/// <summary>
/// Verifies all canon entity models have a Tags property that defaults to empty.
/// This ensures tag filtering works across all repository pages.
/// </summary>
[TestFixture]
public class ModelTagTests
{
    [Test]
    public void WeaponryData_HasTags() => AssertHasTags(new WeaponryData());

    [Test]
    public void AutomatonData_HasTags() => AssertHasTags(new AutomatonData());

    [Test]
    public void CharacterData_HasTags() => AssertHasTags(new CharacterData());

    [Test]
    public void FactionData_HasTags() => AssertHasTags(new FactionData());

    [Test]
    public void TechnologyData_HasTags() => AssertHasTags(new TechnologyData());

    [Test]
    public void EquipmentData_HasTags() => AssertHasTags(new EquipmentData());

    [Test]
    public void CyberwareData_HasTags() => AssertHasTags(new CyberwareData());

    [Test]
    public void AmmunitionData_HasTags() => AssertHasTags(new AmmunitionData());

    [Test]
    public void ConsumerGoodData_HasTags() => AssertHasTags(new ConsumerGoodData());

    [Test]
    public void PharmaceuticalData_HasTags() => AssertHasTags(new PharmaceuticalData());

    [Test]
    public void MaterialData_HasTags() => AssertHasTags(new MaterialData());

    [Test]
    public void SyntheticLifeData_HasTags() => AssertHasTags(new SyntheticLifeData());

    [Test]
    public void GenewareData_HasTags() => AssertHasTags(new GenewareData());

    [Test]
    public void TransportationData_HasTags() => AssertHasTags(new TransportationData());

    [Test]
    public void ApparelData_HasTags() => AssertHasTags(new ApparelData());

    [Test]
    public void ArchetypeData_HasTags() => AssertHasTags(new ArchetypeData());

    [Test]
    public void VocabularyEntry_HasTags() => AssertHasTags(new VocabularyEntry());

    [Test]
    public void QuoteData_HasTags() => AssertHasTags(new QuoteData());

    [Test]
    public void NewsData_HasTags() => AssertHasTags(new NewsData());

    [Test]
    public void ContractData_HasTags() => AssertHasTags(new ContractData());

    [Test]
    public void WorldbuildingDocument_HasTags()
    {
        var doc = new WorldbuildingDocument();
        Assert.That(doc.Tags, Is.Not.Null);
        Assert.That(doc.Tags, Is.Empty);
    }

    [Test]
    public void CharacterStats_UsesStatTags_NotTags()
    {
        var stats = new CharacterStats();
        Assert.That(stats.StatTags, Is.Not.Null);
        Assert.That(stats.StatTags, Is.Empty);
        // Ensure the property is named StatTags, not Tags, to avoid collision with CharacterData.Tags
        var prop = typeof(CharacterStats).GetProperty("StatTags");
        Assert.That(prop, Is.Not.Null);
        var tagsProp = typeof(CharacterStats).GetProperty("Tags");
        Assert.That(tagsProp, Is.Null, "CharacterStats should use StatTags, not Tags");
    }

    private static void AssertHasTags(object entity)
    {
        var prop = entity.GetType().GetProperty("Tags");
        Assert.That(prop, Is.Not.Null, $"{entity.GetType().Name} must have a Tags property");
        var value = prop!.GetValue(entity) as List<string>;
        Assert.That(value, Is.Not.Null);
        Assert.That(value, Is.Empty);
    }
}
