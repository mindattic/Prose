using StreetSamurai.Core.Services;

namespace StreetSamurai.UnitTests;

[TestFixture]
public class FtpPublishServiceTests
{
    [Test]
    public void IsConfigured_False_WhenNoHost()
    {
        var rootDir = Path.Combine(Path.GetTempPath(), $"ss_ftp_{Guid.NewGuid():N}");
        Directory.CreateDirectory(rootDir);
        try
        {
            using var settings = new SettingsService(rootDir);
            settings.FtpHost = "";
            var svc = new FtpPublishService(settings);
            Assert.That(svc.IsConfigured, Is.False);
        }
        finally { Directory.Delete(rootDir, true); }
    }

    [Test]
    public void IsConfigured_True_WhenHostAndUsername()
    {
        var rootDir = Path.Combine(Path.GetTempPath(), $"ss_ftp_{Guid.NewGuid():N}");
        Directory.CreateDirectory(rootDir);
        try
        {
            using var settings = new SettingsService(rootDir);
            settings.FtpHost = "example.com";
            settings.FtpUsername = "user";
            var svc = new FtpPublishService(settings);
            Assert.That(svc.IsConfigured, Is.True);
        }
        finally { Directory.Delete(rootDir, true); }
    }

    [Test]
    public async Task Publish_FailsGracefully_WhenNoExportDir()
    {
        var rootDir = Path.Combine(Path.GetTempPath(), $"ss_ftp_{Guid.NewGuid():N}");
        Directory.CreateDirectory(rootDir);
        try
        {
            using var settings = new SettingsService(rootDir);
            settings.FtpHost = "example.com";
            settings.FtpUsername = "user";
            var svc = new FtpPublishService(settings);
            var (ok, msg) = await svc.PublishAsync("/nonexistent/path");
            Assert.That(ok, Is.False);
            Assert.That(msg, Does.Contain("not found"));
        }
        finally { Directory.Delete(rootDir, true); }
    }

    [Test]
    public async Task Publish_FailsGracefully_WhenNotConfigured()
    {
        var rootDir = Path.Combine(Path.GetTempPath(), $"ss_ftp_{Guid.NewGuid():N}");
        Directory.CreateDirectory(rootDir);
        try
        {
            using var settings = new SettingsService(rootDir);
            settings.FtpHost = "";
            var svc = new FtpPublishService(settings);
            var (ok, msg) = await svc.PublishAsync("/some/path");
            Assert.That(ok, Is.False);
            Assert.That(msg, Does.Contain("not configured"));
        }
        finally { Directory.Delete(rootDir, true); }
    }
}
