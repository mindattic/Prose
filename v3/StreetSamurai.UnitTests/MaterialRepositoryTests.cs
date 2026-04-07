using StreetSamurai.Core.Models.Canon;
using StreetSamurai.Core.Services;

namespace StreetSamurai.UnitTests;

[TestFixture]
public class MaterialRepositoryTests
{
    private string rootDir = "";
    private MaterialRepository repo = null!;

    [SetUp]
    public void Setup()
    {
        rootDir = Path.Combine(Path.GetTempPath(), $"ss_material_{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(rootDir, "engine_data", "materials"));
        repo = new MaterialRepository(new TestPathProviderWithRoot(rootDir));
    }

    [TearDown]
    public void Cleanup() { if (Directory.Exists(rootDir)) Directory.Delete(rootDir, true); }

    [Test]
    public void Save_And_Retrieve()
    {
        var mat = new MaterialData { Name = "Oak", Category = "natural", Tags = ["wood", "natural"] };
        repo.Save(mat);
        repo.Reload();
        Assert.That(repo.GetAll(), Has.Count.EqualTo(1));
        Assert.That(repo.GetAll()[0].Name, Is.EqualTo("Oak"));
    }

    [Test]
    public void DefaultType_IsMaterial()
    {
        Assert.That(new MaterialData().Type, Is.EqualTo("material"));
    }

    [Test]
    public void RepoName_IsMaterials()
    {
        Assert.That(repo.RepoName, Is.EqualTo("materials"));
    }
}
