using Prose.Core.Services;

namespace Prose.UnitTests;

[TestFixture]
public class MediaServiceTests
{
    private string tempDir = null!;
    private string mediaDir = null!;
    private string archiveDir = null!;
    private MediaService svc = null!;

    [SetUp]
    public void SetUp()
    {
        tempDir = Path.Combine(Path.GetTempPath(), $"ss_media_{Guid.NewGuid():N}");
        var paths = new TestPathProviderWithRoot(tempDir);
        mediaDir = paths.MediaDir;
        archiveDir = paths.MediaArchiveDir;
        Directory.CreateDirectory(mediaDir);
        Directory.CreateDirectory(archiveDir);
        svc = new MediaService(paths);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
    }

    private string CreateFile(string filename, string content = "data")
    {
        var path = Path.Combine(mediaDir, filename);
        File.WriteAllText(path, content);
        return path;
    }

    // ── GetFilesForEntity ─────────────────────────────────────────────────────

    [Test]
    public void GetFilesForEntity_EmptyDir_ReturnsEmpty()
    {
        Assert.That(svc.GetFilesForEntity("abc123"), Is.Empty);
    }

    [Test]
    public void GetFilesForEntity_EmptyEntityId_ReturnsEmpty()
    {
        Assert.That(svc.GetFilesForEntity(""), Is.Empty);
    }

    [Test]
    public void GetFilesForEntity_MatchesPrefix()
    {
        CreateFile("entity1.00.png");
        CreateFile("entity1.01.png");
        CreateFile("other.00.png");

        var files = svc.GetFilesForEntity("entity1");
        Assert.That(files, Has.Count.EqualTo(2));
        Assert.That(files, Does.Contain("entity1.00.png"));
        Assert.That(files, Does.Contain("entity1.01.png"));
    }

    [Test]
    public void GetFilesForEntity_SortedByName()
    {
        CreateFile("entity1.02.png");
        CreateFile("entity1.00.png");
        CreateFile("entity1.01.jpg");

        var files = svc.GetFilesForEntity("entity1");
        Assert.That(files[0], Is.EqualTo("entity1.00.png"));
        Assert.That(files[1], Is.EqualTo("entity1.01.jpg"));
        Assert.That(files[2], Is.EqualTo("entity1.02.png"));
    }

    [Test]
    public void GetFilesForEntity_FiltersUnknownExtensions()
    {
        CreateFile("entity1.00.png");
        CreateFile("entity1.01.txt");  // not a media file
        CreateFile("entity1.02.exe");  // not a media file

        var files = svc.GetFilesForEntity("entity1");
        Assert.That(files, Has.Count.EqualTo(1));
        Assert.That(files[0], Is.EqualTo("entity1.00.png"));
    }

    [Test]
    public void GetFilesForEntity_AcceptsAllMediaExtensions()
    {
        foreach (var ext in new[] { ".png", ".jpg", ".jpeg", ".webp", ".gif", ".mp4", ".webm", ".glb", ".gltf" })
            CreateFile($"entity1.00{ext}");

        var files = svc.GetFilesForEntity("entity1");
        Assert.That(files, Has.Count.EqualTo(9));
    }

    // ── GetPrimaryImage ───────────────────────────────────────────────────────

    [Test]
    public void GetPrimaryImage_WhenNoFiles_ReturnsNull()
    {
        Assert.That(svc.GetPrimaryImage("entity1"), Is.Null);
    }

    [Test]
    public void GetPrimaryImage_WhenVideoOnly_ReturnsNull()
    {
        CreateFile("entity1.00.mp4");
        Assert.That(svc.GetPrimaryImage("entity1"), Is.Null);
    }

    [Test]
    public void GetPrimaryImage_ReturnsFirstImage()
    {
        CreateFile("entity1.00.png");
        CreateFile("entity1.01.png");
        Assert.That(svc.GetPrimaryImage("entity1"), Is.EqualTo("entity1.00.png"));
    }

    // ── HasMedia ──────────────────────────────────────────────────────────────

    [Test]
    public void HasMedia_WhenEmpty_ReturnsFalse()
    {
        Assert.That(svc.HasMedia("entity1"), Is.False);
    }

    [Test]
    public void HasMedia_WhenFileExists_ReturnsTrue()
    {
        CreateFile("entity1.00.png");
        Assert.That(svc.HasMedia("entity1"), Is.True);
    }

    // ── GetPath ───────────────────────────────────────────────────────────────

    [Test]
    public void GetPath_ExistingFile_ReturnsFull()
    {
        CreateFile("entity1.00.png");
        var path = svc.GetPath("entity1.00.png");
        Assert.That(path, Is.Not.Null);
        Assert.That(File.Exists(path), Is.True);
    }

    [Test]
    public void GetPath_NonexistentFile_ReturnsNull()
    {
        Assert.That(svc.GetPath("ghost.00.png"), Is.Null);
    }

    [Test]
    public void GetPath_EmptyFilename_ReturnsNull()
    {
        Assert.That(svc.GetPath(""), Is.Null);
    }

    [Test]
    public void GetPath_PathTraversalAttempt_DoesNotEscape()
    {
        // Path.GetFileName strips directory parts — should not resolve to parent
        var path = svc.GetPath("../../../etc/passwd");
        Assert.That(path, Is.Null);
    }

    // ── GetMimeType ───────────────────────────────────────────────────────────

    [TestCase("image.png",  "image/png")]
    [TestCase("image.jpg",  "image/jpeg")]
    [TestCase("image.jpeg", "image/jpeg")]
    [TestCase("image.webp", "image/webp")]
    [TestCase("image.gif",  "image/gif")]
    [TestCase("video.mp4",  "video/mp4")]
    [TestCase("video.webm", "video/webm")]
    [TestCase("video.mov",  "video/quicktime")]
    [TestCase("model.glb",  "model/gltf-binary")]
    [TestCase("model.gltf", "model/gltf+json")]
    [TestCase("file.bin",   "application/octet-stream")]
    public void GetMimeType_ReturnsCorrectMimeType(string filename, string expected)
    {
        Assert.That(MediaService.GetMimeType(filename), Is.EqualTo(expected));
    }

    [Test]
    public void GetMimeType_CaseInsensitiveExtension()
    {
        Assert.That(MediaService.GetMimeType("IMAGE.PNG"), Is.EqualTo("image/png"));
        Assert.That(MediaService.GetMimeType("IMAGE.JPG"), Is.EqualTo("image/jpeg"));
    }

    // ── GetEntityIdsWithImages ────────────────────────────────────────────────

    [Test]
    public void GetEntityIdsWithImages_Empty_ReturnsEmpty()
    {
        Assert.That(svc.GetEntityIdsWithImages(), Is.Empty);
    }

    [Test]
    public void GetEntityIdsWithImages_DeduplicatesById()
    {
        CreateFile("abc.00.png");
        CreateFile("abc.01.png");
        CreateFile("def.00.jpg");

        var ids = svc.GetEntityIdsWithImages();
        Assert.That(ids, Has.Count.EqualTo(2));
        Assert.That(ids, Does.Contain("abc"));
        Assert.That(ids, Does.Contain("def"));
    }

    [Test]
    public void GetEntityIdsWithImages_ExcludesVideoOnly()
    {
        CreateFile("abc.00.mp4");
        Assert.That(svc.GetEntityIdsWithImages(), Is.Empty);
    }

    // ── Archive ───────────────────────────────────────────────────────────────

    [Test]
    public void Archive_ExistingFile_MovesToArchive()
    {
        CreateFile("entity1.00.png");
        var result = svc.Archive("entity1.00.png");
        Assert.That(result, Is.True);
        Assert.That(File.Exists(Path.Combine(mediaDir, "entity1.00.png")), Is.False);
        Assert.That(File.Exists(Path.Combine(archiveDir, "entity1.00.png")), Is.True);
    }

    [Test]
    public void Archive_NonexistentFile_ReturnsFalse()
    {
        var result = svc.Archive("ghost.00.png");
        Assert.That(result, Is.False);
    }

    [Test]
    public void Archive_FileNoLongerReturnedByGetFilesForEntity()
    {
        CreateFile("entity1.00.png");
        svc.Archive("entity1.00.png");
        Assert.That(svc.GetFilesForEntity("entity1"), Is.Empty);
    }

    // ── GetRandomImage ────────────────────────────────────────────────────────

    [Test]
    public void GetRandomImage_EmptyDir_ReturnsNull()
    {
        Assert.That(svc.GetRandomImage(), Is.Null);
    }

    [Test]
    public void GetRandomImage_WithImages_ReturnsFilename()
    {
        CreateFile("entity1.00.png");
        CreateFile("entity2.00.jpg");
        var result = svc.GetRandomImage();
        Assert.That(result, Is.Not.Null);
        Assert.That(new[] { "entity1.00.png", "entity2.00.jpg" }, Does.Contain(result));
    }
}
