using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Nodes;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Base for all JSON-file maintenance utilities. Handles parallel scan loop,
/// per-file semaphore locking, progress reporting, and optional file limit.
/// Subclasses implement ProcessFile() to mutate the JsonObject in place.
/// </summary>
public abstract class DataScanUtility
{
    protected readonly string dataDir;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> fileLocks = new();
    protected static readonly JsonSerializerOptions WriteOptions = new() { WriteIndented = true };

    protected DataScanUtility(IPathProvider paths) => dataDir = paths.EngineDataDir;

    /// <summary>Scan files in parallel. processFile mutates obj in place; return change count (0 = no write).</summary>
    protected async Task<UtilityResult> RunScanAsync(
        string[] files,
        Func<string, JsonObject, int> processFile,
        IProgress<UtilityProgress>? progress = null,
        int? limit = null,
        int parallelism = 4,
        CancellationToken ct = default)
    {
        int scanned = 0, modified = 0, changes = 0;
        var warnings = new ConcurrentBag<string>();
        using var limitCts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        try
        {
            await Parallel.ForEachAsync(files,
                new ParallelOptions { MaxDegreeOfParallelism = parallelism, CancellationToken = limitCts.Token },
                async (file, token) =>
                {
                    var sem = fileLocks.GetOrAdd(file, _ => new SemaphoreSlim(1, 1));
                    await sem.WaitAsync(token);
                    try
                    {
                        var json = await File.ReadAllTextAsync(file, token);
                        if (JsonNode.Parse(json) is not JsonObject obj) return;

                        int fileChanges;
                        try { fileChanges = processFile(file, obj); }
                        catch (Exception ex) { warnings.Add($"{Path.GetFileName(file)}: {ex.Message}"); return; }

                        int done = Interlocked.Increment(ref scanned);
                        if (fileChanges > 0)
                        {
                            await File.WriteAllTextAsync(file, obj.ToJsonString(WriteOptions), token);
                            int mod = Interlocked.Increment(ref modified);
                            Interlocked.Add(ref changes, fileChanges);
                            if (limit.HasValue && mod >= limit.Value)
                                limitCts.Cancel();
                        }
                        progress?.Report(new UtilityProgress(done, files.Length, modified, changes));
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception ex) { warnings.Add($"{Path.GetFileName(file)}: {ex.Message}"); }
                    finally { sem.Release(); }
                });
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { }

        return new UtilityResult(scanned, modified, changes, warnings.IsEmpty ? null : [.. warnings]);
    }

    protected string[] GetFiles(string[]? repos = null)
    {
        var files = Directory.GetFiles(dataDir, "*.json", SearchOption.AllDirectories)
            .Where(f =>
                !f.Contains(Path.DirectorySeparatorChar + "archives" + Path.DirectorySeparatorChar) &&
                !f.Contains(Path.DirectorySeparatorChar + "graph"    + Path.DirectorySeparatorChar) &&
                !f.Contains(Path.DirectorySeparatorChar + "logs"     + Path.DirectorySeparatorChar) &&
                !f.Contains(Path.DirectorySeparatorChar + "chapters" + Path.DirectorySeparatorChar))
            .ToArray();

        if (repos is { Length: > 0 })
            files = files.Where(f =>
                repos.Any(r => f.Contains(Path.DirectorySeparatorChar + r + Path.DirectorySeparatorChar)))
                .ToArray();

        return files;
    }

    protected static string? GetStr(JsonObject obj, string key) =>
        obj[key]?.GetValueKind() == System.Text.Json.JsonValueKind.String
            ? obj[key]!.GetValue<string>() : null;

    protected static string CombineText(JsonObject obj, params string[] keys) =>
        string.Join(" ", keys.Select(k => GetStr(obj, k) ?? "").Where(s => s.Length > 0));
}
