using System.Text;

namespace StreetSamurai.Core.Services;

/// <summary>
/// This process's identity for namespacing scratch files during generated-.md writes. One CLI
/// invocation, one MCP server process, one web host — each gets a distinct id for as long as it
/// runs. Computed once (a process's PID cannot change), so every write from this process uses
/// the same scratch namespace without needing to thread a session id through DI.
/// </summary>
public static class SessionContext
{
    public static readonly string Id = Environment.ProcessId.ToString();
}

/// <summary>
/// Writes a generated, read-only <c>.md</c> mirror (docs/nodes/&lt;CODE&gt;.md,
/// docs/BIBLE.md, etc.) so two concurrent processes regenerating the SAME file can never
/// observe or produce a torn/half-written copy of it.
///
/// Previously every generator (NodeDocService, CanonDocumentService) hand-rolled its own
/// "clear ReadOnly, delete, write, set ReadOnly" sequence directly against the destination path.
/// Between the delete and the new write completing, a concurrent reader could hit a
/// FileNotFoundException; two concurrent writers could interleave their writes into the same
/// file. Writing to a per-process scratch file first and then doing a single atomic
/// <see cref="File.Move(string, string, bool)"/> onto the destination (same directory, same
/// volume, so the OS-level rename is atomic) closes both holes: a reader always sees either the
/// old complete file or the new complete file, never a partial one — whichever writer's rename
/// lands last simply wins, which is the accepted, sufficient guarantee here (there is one DB
/// row of record either way; this only protects the disk mirror from corruption, it does not
/// give two processes independent "views" of the same generated doc).
/// </summary>
public static class GeneratedFileWriter
{
    public static async Task WriteReadOnlyAsync(string destPath, string content, CancellationToken ct = default)
    {
        var dir = Path.GetDirectoryName(destPath)!;
        Directory.CreateDirectory(dir);

        var tempPath = Path.Combine(dir, $".{SessionContext.Id}-{Path.GetFileName(destPath)}.tmp");
        await File.WriteAllTextAsync(tempPath, content, new UTF8Encoding(false), ct);

        try
        {
            // A ReadOnly destination blocks File.Move's replace — clear it first. This does not
            // touch the destination's content, only its attribute, so a concurrent reader mid-way
            // through this still sees the old file's real, complete bytes.
            if (File.Exists(destPath))
                File.SetAttributes(destPath, FileAttributes.Normal);
            File.Move(tempPath, destPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }

        File.SetAttributes(destPath, FileAttributes.ReadOnly);
    }
}
