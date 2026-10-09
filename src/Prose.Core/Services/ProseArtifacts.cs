using System.Text;
using MindAttic.Export.Artifacts;

namespace Prose.Core.Services;

/// <summary>
/// Prose's door to the shared <see cref="ArtifactWriter"/> (MindAttic.Export): every user-facing
/// file Prose exports is written through it, so each one gets the library's folder creation and
/// atomic temp-then-move write.
///
/// <para>Prose's export sites take a full path and historically overwrote in place with an exact
/// file name, so the presets here reproduce that: <see cref="Overwrite"/> replaces the file, keeps
/// the name byte for byte (<c>SanitizeName = false</c>) and writes UTF-8 without a BOM — what
/// <c>File.WriteAllText(path, text)</c> did. <see cref="OverwriteWithBom"/> is for the sites that
/// deliberately wrote a BOM. The bundle folders that archive old versions keep doing so through
/// <see cref="ExportCleanupService"/> before writing; the write itself then overwrites.</para>
/// </summary>
public static class ProseArtifacts
{
    /// <summary>Replace in place, exact name, UTF-8 without BOM.</summary>
    public static ArtifactOptions Overwrite { get; } = new()
    {
        Existing = ExistingArtifact.Overwrite,
        SanitizeName = false,
        Encoding = new UTF8Encoding(false),
    };

    /// <summary>Replace in place, exact name, UTF-8 with BOM (<c>Encoding.UTF8</c>).</summary>
    public static ArtifactOptions OverwriteWithBom { get; } = Overwrite with { Encoding = new UTF8Encoding(true) };

    /// <summary>Splits a file path into the (absolute) directory and file name ArtifactWriter takes.
    /// A bare file name resolves against the current directory, as File.WriteAllText would.</summary>
    public static (string Directory, string FileName) Split(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("An artifact needs a path.", nameof(path));
        var full = Path.GetFullPath(path);
        var fileName = Path.GetFileName(full);
        if (fileName.Length == 0) throw new ArgumentException($"Not a file path: {path}", nameof(path));
        return (Path.GetDirectoryName(full)!, fileName);
    }

    public static Task<string> WriteTextAsync(string path, string content, ArtifactOptions? options = null, CancellationToken ct = default)
    {
        var (dir, name) = Split(path);
        return ArtifactWriter.WriteTextAsync(dir, name, content, options ?? Overwrite, ct);
    }

    public static Task<string> WriteBytesAsync(string path, byte[] bytes, ArtifactOptions? options = null, CancellationToken ct = default)
    {
        var (dir, name) = Split(path);
        return ArtifactWriter.WriteBytesAsync(dir, name, bytes, options ?? Overwrite, ct);
    }

    public static Task<string> WriteStreamAsync(string path, Func<Stream, CancellationToken, Task> write, ArtifactOptions? options = null, CancellationToken ct = default)
    {
        var (dir, name) = Split(path);
        return ArtifactWriter.WriteStreamAsync(dir, name, write, options ?? Overwrite, ct);
    }

    public static Task<string> WriteZipAsync(string path, Func<System.IO.Compression.ZipArchive, CancellationToken, Task> build, ArtifactOptions? options = null, CancellationToken ct = default)
    {
        var (dir, name) = Split(path);
        return ArtifactWriter.WriteZipAsync(dir, name, build, options ?? Overwrite, ct);
    }

    public static Task<string> WriteViaPathAsync(string path, Func<string, CancellationToken, Task> render, ArtifactOptions? options = null, CancellationToken ct = default)
    {
        var (dir, name) = Split(path);
        return ArtifactWriter.WriteViaPathAsync(dir, name, render, options ?? Overwrite, ct);
    }

    /// <summary>Copies an existing file (a staged manuscript, a rendered scratch file) into place as
    /// an artifact.</summary>
    public static Task<string> CopyFileAsync(string sourcePath, string path, ArtifactOptions? options = null, CancellationToken ct = default) =>
        WriteStreamAsync(path, async (target, c) =>
        {
            await using var source = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            await source.CopyToAsync(target, c);
        }, options, ct);

    // Synchronous forms for call sites that are not async. The write runs on the thread pool so a
    // caller holding a synchronization context (Blazor, WPF) cannot deadlock on it.

    public static string WriteText(string path, string content, ArtifactOptions? options = null) =>
        Task.Run(() => WriteTextAsync(path, content, options)).GetAwaiter().GetResult();

    public static string WriteViaPath(string path, Action<string> render, ArtifactOptions? options = null) =>
        Task.Run(() => WriteViaPathAsync(path, (p, _) => { render(p); return Task.CompletedTask; }, options)).GetAwaiter().GetResult();

    public static string WriteStream(string path, Action<Stream> write, ArtifactOptions? options = null) =>
        Task.Run(() => WriteStreamAsync(path, (s, _) => { write(s); return Task.CompletedTask; }, options)).GetAwaiter().GetResult();
}
