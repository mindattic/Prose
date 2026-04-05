namespace StreetSamurai.UnitTests;

using StreetSamurai.Core.Services;

[TestFixture]
public class LoggingServiceTests
{
    private string tempDir = null!;
    private LoggingService svc = null!;

    [SetUp]
    public void Setup()
    {
        tempDir = Path.Combine(Path.GetTempPath(), "ss_log_test_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(tempDir);
        var paths = new TestPathProviderWithRoot(tempDir);
        svc = new LoggingService(paths);
    }

    [TearDown]
    public void Teardown()
    {
        if (Directory.Exists(tempDir))
            Directory.Delete(tempDir, true);
    }

    private void WriteLogFile(string fileName, string content)
    {
        var logDir = Path.Combine(tempDir, "logs");
        Directory.CreateDirectory(logDir);
        File.WriteAllText(Path.Combine(logDir, fileName), content);
    }

    // ── Search ──────────────────────────────────────────────

    [Test]
    public void Search_NoLogDir_ReturnsEmpty()
    {
        var results = svc.Search(new LogSearchRequest { Since = DateTime.Now.AddDays(-1) });
        Assert.That(results, Is.Empty);
    }

    [Test]
    public void Search_ParsesStandardSerilogFormat()
    {
        WriteLogFile("log-20260405.txt",
            "2026-04-05 02:30:22PM [INF] Application started\n" +
            "2026-04-05 02:30:23PM [WRN] Slow query detected\n");

        var results = svc.Search(new LogSearchRequest
        {
            Since = new DateTime(2026, 4, 5, 0, 0, 0),
            MaxResults = 100
        });

        Assert.That(results, Has.Count.EqualTo(2));
        Assert.That(results[0].Level, Is.EqualTo("Warning")); // newest first
        Assert.That(results[1].Level, Is.EqualTo("Information"));
    }

    [Test]
    public void Search_FiltersBySeverity()
    {
        WriteLogFile("log-20260405.txt",
            "2026-04-05 02:30:22PM [INF] Info message\n" +
            "2026-04-05 02:30:23PM [ERR] Error message\n" +
            "2026-04-05 02:30:24PM [WRN] Warning message\n");

        var results = svc.Search(new LogSearchRequest
        {
            Since = new DateTime(2026, 4, 5, 0, 0, 0),
            MinSeverity = "Warning",
            MaxResults = 100
        });

        Assert.That(results, Has.Count.EqualTo(2));
        Assert.That(results.All(r => r.Level is "Warning" or "Error"), Is.True);
    }

    [Test]
    public void Search_FiltersBySearchText()
    {
        WriteLogFile("log-20260405.txt",
            "2026-04-05 02:30:22PM [INF] Application started\n" +
            "2026-04-05 02:30:23PM [INF] Database connected\n");

        var results = svc.Search(new LogSearchRequest
        {
            Since = new DateTime(2026, 4, 5, 0, 0, 0),
            SearchText = "Database",
            MaxResults = 100
        });

        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0].Message, Does.Contain("Database"));
    }

    [Test]
    public void Search_ParsesMultiLineExceptions()
    {
        WriteLogFile("log-20260405.txt",
            "2026-04-05 02:30:22PM [ERR] Something failed\n" +
            "System.Exception: Bad things\n" +
            "   at Foo.Bar()\n" +
            "2026-04-05 02:30:23PM [INF] Recovered\n");

        var results = svc.Search(new LogSearchRequest
        {
            Since = new DateTime(2026, 4, 5, 0, 0, 0),
            MaxResults = 100
        });

        Assert.That(results, Has.Count.EqualTo(2));
        var error = results.First(r => r.Level == "Error");
        Assert.That(error.Exception, Does.Contain("System.Exception"));
        Assert.That(error.Exception, Does.Contain("Foo.Bar"));
    }

    [Test]
    public void Search_RespectsMaxResults()
    {
        var lines = string.Join("\n", Enumerable.Range(0, 50)
            .Select(i => $"2026-04-05 02:30:{i:D2}PM [INF] Message {i}"));
        WriteLogFile("log-20260405.txt", lines);

        var results = svc.Search(new LogSearchRequest
        {
            Since = new DateTime(2026, 4, 5, 0, 0, 0),
            MaxResults = 10
        });

        Assert.That(results, Has.Count.EqualTo(10));
    }

    // ── Level normalization ─────────────────────────────────

    [TestCase("VRB", "Verbose")]
    [TestCase("DBG", "Debug")]
    [TestCase("INF", "Information")]
    [TestCase("WRN", "Warning")]
    [TestCase("ERR", "Error")]
    [TestCase("FTL", "Fatal")]
    public void Search_NormalizesLevelAbbreviations(string abbrev, string expected)
    {
        WriteLogFile("log-20260405.txt",
            $"2026-04-05 02:30:22PM [{abbrev}] Test message\n");

        var results = svc.Search(new LogSearchRequest
        {
            Since = new DateTime(2026, 4, 5, 0, 0, 0),
            MaxResults = 100
        });

        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0].Level, Is.EqualTo(expected));
    }

    // ── Alternate formats ────────────────────────────────────

    [Test]
    public void Search_Parses24hFormat()
    {
        WriteLogFile("log-20260405.txt",
            "2026-04-05 14:30:22 [INF] 24h message\n");

        var results = svc.Search(new LogSearchRequest
        {
            Since = new DateTime(2026, 4, 5, 0, 0, 0),
            MaxResults = 100
        });

        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0].Message, Is.EqualTo("24h message"));
    }

    // ── GetAvailableDates ───────────────────────────────────

    [Test]
    public void GetAvailableDates_ReturnsCorrectDates()
    {
        WriteLogFile("log-20260405.txt", "2026-04-05 02:30:22PM [INF] test\n");
        WriteLogFile("log-20260404.txt", "2026-04-04 02:30:22PM [INF] test\n");

        var dates = svc.GetAvailableDates();
        Assert.That(dates, Has.Count.EqualTo(2));
        Assert.That(dates[0], Is.EqualTo(new DateTime(2026, 4, 5)));
        Assert.That(dates[1], Is.EqualTo(new DateTime(2026, 4, 4)));
    }

    // ── GetLogSizeBytes ─────────────────────────────────────

    [Test]
    public void GetLogSizeBytes_ExistingFile_ReturnsSize()
    {
        WriteLogFile("log-20260405.txt", "2026-04-05 02:30:22PM [INF] test\n");
        var size = svc.GetLogSizeBytes(new DateTime(2026, 4, 5));
        Assert.That(size, Is.GreaterThan(0));
    }

    [Test]
    public void GetLogSizeBytes_MissingFile_ReturnsZero()
    {
        var size = svc.GetLogSizeBytes(new DateTime(2099, 1, 1));
        Assert.That(size, Is.EqualTo(0));
    }
}
