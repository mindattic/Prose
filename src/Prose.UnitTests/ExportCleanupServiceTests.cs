using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// ArchiveDirFor/RemotePathFor are internal statics (InternalsVisibleTo already covers this
/// project) exactly so the "where does the sibling Archives/public-mirror path land" math is
/// directly testable without a database or the file system (PRS-T942: Archives moved from
/// nested under each book's export folder to a sibling tree).
/// </summary>
[TestFixture]
public class ExportCleanupServiceTests
{
    [Test]
    public void ArchiveDirFor_flat_NodeCode_book_is_a_sibling_of_the_universe_folder()
    {
        var baseDir = @"D:\Projects\MindAttic\ePub\MindAttic\GLMZ";
        var nodeDir = @"D:\Projects\MindAttic\ePub\MindAttic\GLMZ\ATTE";
        Assert.That(ExportCleanupService.ArchiveDirFor(baseDir, nodeDir),
            Is.EqualTo(@"D:\Projects\MindAttic\ePub\MindAttic\Archives\GLMZ\ATTE"));
    }

    [Test]
    public void ArchiveDirFor_mirrors_legacy_series_nesting()
    {
        var baseDir = @"D:\Projects\MindAttic\ePub\MindAttic\SET";
        var nodeDir = @"D:\Projects\MindAttic\ePub\MindAttic\SET\Philosophy and Technology\Manuscript";
        Assert.That(ExportCleanupService.ArchiveDirFor(baseDir, nodeDir),
            Is.EqualTo(@"D:\Projects\MindAttic\ePub\MindAttic\Archives\SET\Philosophy and Technology\Manuscript"));
    }

    [Test]
    public void ArchiveDirFor_tolerates_a_trailing_separator_on_baseDir()
    {
        var baseDir = @"D:\Projects\MindAttic\ePub\MindAttic\GLMZ\";
        var nodeDir = @"D:\Projects\MindAttic\ePub\MindAttic\GLMZ\ATTE";
        Assert.That(ExportCleanupService.ArchiveDirFor(baseDir, nodeDir),
            Is.EqualTo(@"D:\Projects\MindAttic\ePub\MindAttic\Archives\GLMZ\ATTE"));
    }

    [Test]
    public void RemotePathFor_mirrors_ArchiveDirFor_as_a_forward_slash_ftp_path()
    {
        var baseDir = @"D:\Projects\MindAttic\ePub\MindAttic\GLMZ";
        var nodeDir = @"D:\Projects\MindAttic\ePub\MindAttic\GLMZ\ATTE";
        Assert.That(WebPublishService.RemotePathFor(baseDir, nodeDir), Is.EqualTo("/mindattic.com/GLMZ/ATTE"));
    }

    [Test]
    public void RemotePathFor_mirrors_legacy_series_nesting_as_forward_slashes()
    {
        var baseDir = @"D:\Projects\MindAttic\ePub\MindAttic\SET";
        var nodeDir = @"D:\Projects\MindAttic\ePub\MindAttic\SET\Philosophy and Technology\Manuscript";
        Assert.That(WebPublishService.RemotePathFor(baseDir, nodeDir), Is.EqualTo("/mindattic.com/SET/Philosophy and Technology/Manuscript"));
    }
}
