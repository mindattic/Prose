using Prose.Core.Services;
using Prose.Core.Services.Operator.KdpTools;

namespace Prose.UnitTests;

/// <summary>
/// Pins two input-sanitization fixes: KDP's description box receives HTML (CKEditor.setData), so
/// plain text must be escaped and its paragraphs kept; and MediaService.Archive must sanitize the
/// destination name exactly as it sanitizes the source.
/// </summary>
[TestFixture]
public class DescriptionHtmlAndMediaArchiveTests
{
    [Test]
    public void PlainTextToHtml_KeepsParagraphsAndLineBreaks()
    {
        var html = SetDescriptionTool.PlainTextToHtml("First para.\r\nSame para.\r\n\r\nApproximately 12 pages and 1 hr to read.");
        Assert.That(html, Is.EqualTo("<p>First para.<br>Same para.</p><p>Approximately 12 pages and 1 hr to read.</p>"));
    }

    [Test]
    public void PlainTextToHtml_EscapesMarkupCharacters()
    {
        var html = SetDescriptionTool.PlainTextToHtml("Kyle & Sasha <3");
        Assert.That(html, Is.EqualTo("<p>Kyle &amp; Sasha &lt;3</p>"));
    }

    [Test]
    public void PlainTextToHtml_EmptyText_IsEmpty()
    {
        Assert.That(SetDescriptionTool.PlainTextToHtml("   \n\n  "), Is.EqualTo(""));
    }

    [Test]
    public void MediaArchive_TraversalName_LandsInsideArchiveDir()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"ss_media_trav_{Guid.NewGuid():N}");
        try
        {
            var paths = new TestPathProviderWithRoot(tempDir);
            Directory.CreateDirectory(paths.MediaDir);
            File.WriteAllText(Path.Combine(paths.MediaDir, "x.00.png"), "data");
            var svc = new MediaService(paths);

            Assert.That(svc.Archive("../x.00.png"), Is.True);
            Assert.That(File.Exists(Path.Combine(paths.MediaArchiveDir, "x.00.png")), Is.True);
            Assert.That(File.Exists(Path.Combine(Path.GetDirectoryName(paths.MediaArchiveDir)!, "x.00.png")), Is.False);
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
        }
    }
}
