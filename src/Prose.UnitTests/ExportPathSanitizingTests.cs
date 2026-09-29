using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Titles become export folder and file names. A title of ".." climbed out of the export root,
/// "Con" opened the console device, a trailing "..." was silently dropped by Windows, and a
/// control character pasted into a chapter made the whole EPUB invalid XML.
/// </summary>
[TestFixture]
public class ExportPathSanitizingTests
{
    [TestCase("..", "untitled")]
    [TestCase(".", "untitled")]
    [TestCase("And Then...", "And Then")]
    [TestCase("What: Now?", "What Now")]
    [TestCase("a/b\\c", "abc")]
    [TestCase("Bushido Coda", "Bushido Coda")]
    [TestCase("The Devil's Hour", "The Devils Hour")]
    [TestCase("Con", "_Con")]
    [TestCase("nul", "_nul")]
    [TestCase("COM1", "_COM1")]
    [TestCase("Line\u0007Bell", "LineBell")]
    public void SanitizeTitle_yields_a_safe_single_segment(string title, string expected)
    {
        Assert.That(ExportPathResolver.SanitizeTitle(title), Is.EqualTo(expected));
    }

    [TestCase("CON", true)]
    [TestCase("aux.txt", true)]
    [TestCase("LPT9", true)]
    [TestCase("Console", false)]
    [TestCase("COM", false)]
    [TestCase("Nullity", false)]
    public void IsReservedDeviceName_matches_only_device_names(string name, bool expected)
    {
        Assert.That(ExportPathResolver.IsReservedDeviceName(name), Is.EqualTo(expected));
    }

    [Test]
    public void SanitizeTitle_never_escapes_the_base_directory()
    {
        var root = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "prose-export-root"));
        foreach (var title in new[] { "..", "../..", "..\\..", "C:\\Windows", "/etc/passwd", "...", " .. " })
        {
            var full = Path.GetFullPath(Path.Combine(root, ExportPathResolver.SanitizeTitle(title)));
            Assert.That(full.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase),
                Is.True, $"'{title}' resolved to {full}");
        }
    }

    [Test]
    public void StripXmlIllegalChars_removes_forbidden_controls_and_keeps_whitespace()
    {
        var s = "a\u000Bb\u000Cc\u0001d\te\nf\rg";
        Assert.That(BookExportService.StripXmlIllegalChars(s), Is.EqualTo("abcd\te\nf\rg"));
    }
}
