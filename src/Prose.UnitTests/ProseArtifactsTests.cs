using System.IO.Compression;
using System.Text;
using MindAttic.Export.Artifacts;
using NUnit.Framework;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// ProseArtifacts is Prose's door to the shared MindAttic.Export ArtifactWriter. Its presets must
/// reproduce what the migrated sites did with File.WriteAllText / File.Create: overwrite in place,
/// keep the exact file name, UTF-8 without BOM (or with it, where a site wrote one), no archive
/// folder, no temp file left behind.
/// </summary>
[TestFixture]
public class ProseArtifactsTests
{
    private string dir = null!;

    [SetUp]
    public void SetUp() => dir = Path.Combine(Path.GetTempPath(), "prose-artifacts-" + Guid.NewGuid().ToString("N"));

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(dir)) Directory.Delete(dir, true);
    }

    [Test]
    public void Overwrite_preset_replaces_in_place_with_exact_name_and_no_bom()
    {
        Assert.That(ProseArtifacts.Overwrite.Existing, Is.EqualTo(ExistingArtifact.Overwrite));
        Assert.That(ProseArtifacts.Overwrite.SanitizeName, Is.False);
        Assert.That(ProseArtifacts.Overwrite.Atomic, Is.True);
        Assert.That(ProseArtifacts.Overwrite.Encoding.GetPreamble(), Is.Empty);
        Assert.That(ProseArtifacts.OverwriteWithBom.Encoding.GetPreamble(), Is.EqualTo(new byte[] { 0xEF, 0xBB, 0xBF }));
        Assert.That(ProseArtifacts.OverwriteWithBom.Existing, Is.EqualTo(ExistingArtifact.Overwrite));
    }

    [Test]
    public void Split_returns_absolute_directory_and_file_name()
    {
        var (d, name) = ProseArtifacts.Split(Path.Combine(dir, "a", "..", "Book V3.epub"));
        Assert.That(d, Is.EqualTo(dir));
        Assert.That(name, Is.EqualTo("Book V3.epub"));

        var (rel, relName) = ProseArtifacts.Split("report.md");
        Assert.That(rel, Is.EqualTo(Path.GetFullPath(".").TrimEnd(Path.DirectorySeparatorChar)));
        Assert.That(relName, Is.EqualTo("report.md"));

        Assert.Throws<ArgumentException>(() => ProseArtifacts.Split(""));
        Assert.Throws<ArgumentException>(() => ProseArtifacts.Split(dir + Path.DirectorySeparatorChar));
    }

    [Test]
    public async Task WriteText_matches_File_WriteAllText_bytes_and_overwrites()
    {
        var path = Path.Combine(dir, "sub", "x.md");
        await ProseArtifacts.WriteTextAsync(path, "first — é");
        await ProseArtifacts.WriteTextAsync(path, "second — é");

        var expected = Path.Combine(dir, "expected.md");
        File.WriteAllText(expected, "second — é");
        Assert.That(File.ReadAllBytes(path), Is.EqualTo(File.ReadAllBytes(expected)));
        Assert.That(Directory.Exists(Path.Combine(dir, "sub", "Archives")), Is.False, "overwrite must not archive");
        Assert.That(Directory.GetFiles(Path.Combine(dir, "sub")), Has.Length.EqualTo(1), "no temp file left behind");
    }

    [Test]
    public void WriteText_with_bom_matches_UTF8Encoding_true()
    {
        var path = Path.Combine(dir, "qa.md");
        ProseArtifacts.WriteText(path, "report", ProseArtifacts.OverwriteWithBom);
        var expected = Path.Combine(dir, "expected.md");
        File.WriteAllText(expected, "report", new UTF8Encoding(true));
        Assert.That(File.ReadAllBytes(path), Is.EqualTo(File.ReadAllBytes(expected)));
    }

    [Test]
    public void Exact_name_is_kept_unsanitized()
    {
        // SanitizeFileName would trim the leading space; the original sites wrote the name as given.
        var path = Path.Combine(dir, " Title V2.numbered.md");
        var written = ProseArtifacts.WriteText(path, "x");
        Assert.That(Path.GetFileName(written), Is.EqualTo(" Title V2.numbered.md"));
        Assert.That(File.Exists(path), Is.True);
    }

    [Test]
    public async Task WriteViaPath_renders_to_temp_then_moves_into_place()
    {
        var path = Path.Combine(dir, "book.pdf");
        string? seen = null;
        await ProseArtifacts.WriteViaPathAsync(path, (temp, _) => { seen = temp; File.WriteAllText(temp, "pdf"); return Task.CompletedTask; });
        Assert.That(seen, Is.Not.EqualTo(path));
        Assert.That(Path.GetDirectoryName(seen), Is.EqualTo(dir));
        Assert.That(File.ReadAllText(path), Is.EqualTo("pdf"));
        Assert.That(File.Exists(seen), Is.False);
    }

    [Test]
    public async Task CopyFile_places_a_copy_over_an_existing_file()
    {
        Directory.CreateDirectory(dir);
        var src = Path.Combine(dir, "scratch.mp4");
        File.WriteAllBytes(src, [1, 2, 3]);
        var dst = Path.Combine(dir, "out", "final.mp4");
        Directory.CreateDirectory(Path.GetDirectoryName(dst)!);
        File.WriteAllBytes(dst, [9]);
        await ProseArtifacts.CopyFileAsync(src, dst);
        Assert.That(File.ReadAllBytes(dst), Is.EqualTo(new byte[] { 1, 2, 3 }));
        Assert.That(File.Exists(src), Is.True);
    }

    [Test]
    public void WriteStream_zip_produces_a_readable_archive()
    {
        var path = Path.Combine(dir, "b.epub");
        ProseArtifacts.WriteStream(path, s =>
        {
            using var zip = new ZipArchive(s, ZipArchiveMode.Create, leaveOpen: true);
            using var w = new StreamWriter(zip.CreateEntry("mimetype", CompressionLevel.NoCompression).Open(), Encoding.ASCII);
            w.Write("application/epub+zip");
        });
        using var read = ZipFile.OpenRead(path);
        Assert.That(read.Entries[0].FullName, Is.EqualTo("mimetype"));
    }

    [Test]
    public async Task Failed_write_leaves_no_partial_file()
    {
        var path = Path.Combine(dir, "x.sql");
        Assert.ThrowsAsync<InvalidOperationException>(() => ProseArtifacts.WriteStreamAsync(path, async (s, c) =>
        {
            await s.WriteAsync(new byte[] { 1 }, c);
            throw new InvalidOperationException("boom");
        }));
        Assert.That(Directory.GetFiles(dir), Is.Empty);
        await Task.CompletedTask;
    }
}
