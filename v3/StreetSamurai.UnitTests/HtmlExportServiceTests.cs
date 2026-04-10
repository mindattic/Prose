using StreetSamurai.Core.Services;

namespace StreetSamurai.UnitTests;

[TestFixture]
public class HtmlExportServiceTests
{
    private string rootDir = "";
    private HtmlExportService svc = null!;

    [SetUp]
    public void Setup()
    {
        rootDir = Path.Combine(Path.GetTempPath(), $"ss_export_{Guid.NewGuid():N}");
        Directory.CreateDirectory(rootDir);
        var paths = new TestPathProviderWithRoot(rootDir);
        var settingsDir = Path.Combine(rootDir, "settings");
        Directory.CreateDirectory(settingsDir);
        var settings = new SettingsService(settingsDir);
        svc = new HtmlExportService(paths, settings);
    }

    [TearDown]
    public void Cleanup()
    {
        if (Directory.Exists(rootDir)) Directory.Delete(rootDir, true);
    }

    [Test]
    public void ExportEntry_CreatesSingleFile()
    {
        var json = """{"name":"Test Weapon","type":"weapon","tags":["lethal","pistol"]}""";
        var path = svc.ExportEntry("Weaponry", "Test Weapon", json);

        Assert.That(File.Exists(path));
        var html = File.ReadAllText(path);
        Assert.That(html, Does.Contain("Test Weapon"));
        Assert.That(html, Does.Contain("export.css"));
        Assert.That(html, Does.Contain("export.js"));
    }

    [Test]
    public void ExportEntry_CreatesExternalCssAndJs()
    {
        var json = """{"name":"Test","type":"weapon","tags":[]}""";
        svc.ExportEntry("Test", "Test", json);

        Assert.That(File.Exists(Path.Combine(svc.ExportDir, "export.css")));
        Assert.That(File.Exists(Path.Combine(svc.ExportDir, "export.js")));
    }

    [Test]
    public void ExportCss_UsesOutfitFont()
    {
        var json = """{"name":"Test","type":"weapon","tags":[]}""";
        svc.ExportEntry("Test", "Test", json);

        var css = File.ReadAllText(Path.Combine(svc.ExportDir, "export.css"));
        Assert.That(css, Does.Contain("Outfit"));
    }

    [Test]
    public void ExportCss_UsesAppColors()
    {
        var json = """{"name":"Test","type":"weapon","tags":[]}""";
        svc.ExportEntry("Test", "Test", json);

        var css = File.ReadAllText(Path.Combine(svc.ExportDir, "export.css"));
        Assert.That(css, Does.Contain("#0d1117")); // bg
        Assert.That(css, Does.Contain("#dc3545")); // accent
        Assert.That(css, Does.Contain("#e6edf3")); // text
    }

    [Test]
    public void ExportJs_SupportsTagFiltering()
    {
        var json = """{"name":"Test","type":"weapon","tags":[]}""";
        svc.ExportEntry("Test", "Test", json);

        var js = File.ReadAllText(Path.Combine(svc.ExportDir, "export.js"));
        Assert.That(js, Does.Contain("activeTags"));
        Assert.That(js, Does.Contain("toggleTag"));
        Assert.That(js, Does.Contain("data-tags"));
    }

    [Test]
    public void ExportRepo_CreatesFileWithTOC()
    {
        var entries = new List<(string name, string json)>
        {
            ("Alpha", """{"name":"Alpha","type":"weapon","tags":["pistol"]}"""),
            ("Beta", """{"name":"Beta","type":"weapon","tags":["rifle"]}"""),
        };

        var path = svc.ExportRepo("Weaponry", entries);
        Assert.That(File.Exists(path));

        var html = File.ReadAllText(path);
        Assert.That(html, Does.Contain("Alpha"));
        Assert.That(html, Does.Contain("Beta"));
        Assert.That(html, Does.Contain("toc"));
        Assert.That(html, Does.Contain("filterInput")); // has filter bar
        Assert.That(html, Does.Contain("tagBar")); // has tag bar
    }

    [Test]
    public void ExportRepo_EmbedsTags_InDataAttribute()
    {
        var entries = new List<(string name, string json)>
        {
            ("Test Gun", """{"name":"Test Gun","type":"weapon","tags":["lethal","pistol","tier 3"]}"""),
        };

        var path = svc.ExportRepo("Weaponry", entries);
        var html = File.ReadAllText(path);
        Assert.That(html, Does.Contain("data-tags=\"lethal,pistol,tier 3\""));
    }

    [Test]
    public void ExportRepo_HasBackToIndexLink()
    {
        var entries = new List<(string name, string json)>
        {
            ("Test", """{"name":"Test","type":"weapon","tags":[]}"""),
        };

        var path = svc.ExportRepo("Weaponry", entries);
        var html = File.ReadAllText(path);
        Assert.That(html, Does.Contain("index.htm"));
    }

    [Test]
    public void ExportRepo_HasScrollToTopButton()
    {
        var entries = new List<(string name, string json)>
        {
            ("Test", """{"name":"Test","type":"weapon","tags":[]}"""),
        };

        var path = svc.ExportRepo("Weaponry", entries);
        var html = File.ReadAllText(path);
        Assert.That(html, Does.Contain("scrollTop"));
        Assert.That(html, Does.Contain("scroll-top"));
    }

    [Test]
    public void ExportAll_CreatesIndexAndRepoFiles()
    {
        var repos = new Dictionary<string, List<(string name, string json)>>
        {
            ["Weaponry"] = [("Gun", """{"name":"Gun","type":"weapon","tags":["lethal"]}""")],
            ["Automata"] = [("Bot", """{"name":"Bot","type":"automaton","tags":["spider"]}""")],
        };

        var indexPath = svc.ExportAll(repos);
        Assert.That(File.Exists(indexPath));

        var indexHtml = File.ReadAllText(indexPath);
        Assert.That(indexHtml, Does.Contain("Weaponry"));
        Assert.That(indexHtml, Does.Contain("Automata"));
        Assert.That(indexHtml, Does.Contain("Encyclopedia"));
    }

    [Test]
    public void ExportAll_IndexHasBootstrapIcons()
    {
        var repos = new Dictionary<string, List<(string name, string json)>>
        {
            ["Weaponry"] = [("Gun", """{"name":"Gun","tags":["lethal"]}""")],
        };

        var indexPath = svc.ExportAll(repos);
        var html = File.ReadAllText(indexPath);
        Assert.That(html, Does.Contain("bootstrap-icons"));
        Assert.That(html, Does.Contain("bi bi-"));
    }

    [Test]
    public void ExportAll_RepoIconMapping_ReturnsCorrectIcons()
    {
        var repos = new Dictionary<string, List<(string name, string json)>>
        {
            ["Weaponry"] = [("Gun", """{"name":"Gun","tags":[]}""")],
            ["Automata"] = [("Bot", """{"name":"Bot","tags":[]}""")],
            ["People"] = [("Kyle", """{"name":"Kyle","tags":[]}""")],
        };

        var indexPath = svc.ExportAll(repos);
        var html = File.ReadAllText(indexPath);
        Assert.That(html, Does.Contain("bi-crosshair")); // weaponry
        Assert.That(html, Does.Contain("bi-robot")); // automata
        Assert.That(html, Does.Contain("bi-people")); // people
    }

    [Test]
    public void ExtractTags_ParsesJsonTags()
    {
        var json = """{"name":"Test","tags":["alpha","beta","gamma"]}""";
        var entries = new List<(string name, string json)> { ("Test", json) };
        var path = svc.ExportRepo("Test", entries);
        var html = File.ReadAllText(path);
        Assert.That(html, Does.Contain("data-tags=\"alpha,beta,gamma\""));
    }

    [Test]
    public void ExtractTags_HandlesNoTags()
    {
        var json = """{"name":"Test"}""";
        var entries = new List<(string name, string json)> { ("Test", json) };
        var path = svc.ExportRepo("Test", entries);
        var html = File.ReadAllText(path);
        Assert.That(html, Does.Contain("data-tags=\"\""));
    }
}
