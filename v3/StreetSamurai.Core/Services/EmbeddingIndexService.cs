using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

public record IndexedChunk(
    long Id, string FilePath, int ChunkIndex, string Text, float[] Embedding);

public record SearchHit(string FilePath, int ChunkIndex, string Text, float Score);

public record IndexStats(int FileCount, int ChunkCount, DateTime? LastIndexed, bool OllamaReachable);

/// <summary>
/// RAG corpus index over engine/data/. Walks every JSON file, embeds chunks via
/// Ollama (bge-m3), persists vectors in embeddings.db, and keeps an in-memory
/// copy for cosine-similarity search. A FileSystemWatcher re-embeds files on
/// any save so the index always reflects the live corpus — the closed-loop
/// "truth on disk → LLM corpus" pipeline.
/// </summary>
public class EmbeddingIndexService : IDisposable
{
    private const int ChunkSize    = 1500;
    private const int ChunkOverlap = 200;
    private const int DebounceMs   = 500;
    private const int EmbedBatch   = 16;

    private readonly string dbPath;
    private readonly string dataRoot;
    private readonly OllamaClient ollama;
    private readonly OllamaProcessManager ollamaProc;
    private readonly ILogger<EmbeddingIndexService> log;

    private readonly List<IndexedChunk> memory = new();
    private readonly ReaderWriterLockSlim memLock = new();

    private readonly ConcurrentDictionary<string, DateTime> pending = new();
    private readonly Timer flushTimer;
    private FileSystemWatcher? watcher;

    private DateTime? lastIndexedAt;

    /// <summary>Fires after a file has been re-embedded and the in-memory index updated.</summary>
    public event Action<string>? FileReindexed;

    private const string SchemaSql = """
        CREATE TABLE IF NOT EXISTS chunks (
            id           INTEGER PRIMARY KEY AUTOINCREMENT,
            file_path    TEXT NOT NULL,
            content_hash TEXT NOT NULL,
            chunk_index  INTEGER NOT NULL,
            text         TEXT NOT NULL,
            embedding    BLOB NOT NULL
        );
        CREATE INDEX IF NOT EXISTS idx_chunks_file ON chunks(file_path);

        CREATE TABLE IF NOT EXISTS files (
            path         TEXT PRIMARY KEY,
            last_hash    TEXT NOT NULL,
            last_indexed TEXT NOT NULL
        );
        """;

    public EmbeddingIndexService(IPathProvider paths, OllamaClient ollama, OllamaProcessManager ollamaProc, ILogger<EmbeddingIndexService> log)
    {
        this.ollama     = ollama;
        this.ollamaProc = ollamaProc;
        this.log        = log;
        this.dataRoot   = paths.MutableDataDir;
        this.dbPath   = Path.Combine(dataRoot, "embeddings.db");

        EnsureSchema();
        LoadIntoMemory();

        flushTimer = new Timer(_ => _ = FlushPendingAsync(), null, Timeout.Infinite, Timeout.Infinite);
        StartWatcher();

        // Initial reindex disabled — was spawning Ollama on startup and hanging the host.
        // Run reindex manually via the /ask page or CLI when Ollama is available.
    }

    public IndexStats GetStats()
    {
        memLock.EnterReadLock();
        try
        {
            return new IndexStats(
                FileCount:  memory.Select(c => c.FilePath).Distinct().Count(),
                ChunkCount: memory.Count,
                LastIndexed: lastIndexedAt,
                OllamaReachable: false);
        }
        finally { memLock.ExitReadLock(); }
    }

    public async Task<bool> OllamaReachableAsync(CancellationToken ct = default)
        => await ollama.IsReachableAsync(ct);

    /// <summary>Cosine-similarity search over the in-memory index.</summary>
    public async Task<IReadOnlyList<SearchHit>> SearchAsync(
        string query, int k = 8, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query) || k <= 0) return Array.Empty<SearchHit>();

        var queryEmbeddings = await ollama.EmbedAsync(new[] { query }, ct);
        if (queryEmbeddings.Count == 0) return Array.Empty<SearchHit>();
        var qv = queryEmbeddings[0];

        memLock.EnterReadLock();
        try
        {
            var hits = memory
                .Select(c => (c, score: Cosine(qv, c.Embedding)))
                .OrderByDescending(x => x.score)
                .Take(k)
                .Select(x => new SearchHit(x.c.FilePath, x.c.ChunkIndex, x.c.Text, x.score))
                .ToList();
            return hits;
        }
        finally { memLock.ExitReadLock(); }
    }

    /// <summary>Walk the data tree and re-embed any file whose hash has changed.</summary>
    public async Task<int> ReindexAllAsync(CancellationToken ct = default)
    {
        // Spin up Ollama if it's not already running AND wait for the embed model
        // to finish loading. Without the warmup-await, the first ~30 embed calls
        // race ahead of bge-m3 loading into VRAM and 404 from /api/embed. On hosts
        // without Ollama installed (Azure) this returns false and we fall through.
        await ollamaProc.EnsureWarmAsync(ct);

        if (!await ollama.IsReachableAsync(ct))
        {
            log.LogInformation("Reindex skipped: Ollama not reachable at {Url}", ollama.BaseUrl);
            return 0;
        }

        var files = EnumerateIndexableFiles().ToList();
        var hashes = LoadFileHashes();
        var toIndex = new List<string>();
        foreach (var f in files)
        {
            if (ct.IsCancellationRequested) break;
            var h = HashFile(f);
            if (!hashes.TryGetValue(f, out var prev) || prev != h)
                toIndex.Add(f);
        }

        if (toIndex.Count == 0)
        {
            lastIndexedAt = DateTime.UtcNow;
            return 0;
        }

        log.LogInformation("Reindexing {N} changed file(s)…", toIndex.Count);
        int n = 0;
        foreach (var f in toIndex)
        {
            if (ct.IsCancellationRequested) break;
            try
            {
                await ReindexFileAsync(f, ct);
                n++;
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "Failed to index {File}", f);
            }
        }
        lastIndexedAt = DateTime.UtcNow;
        log.LogInformation("Reindex complete: {N} file(s) embedded", n);
        return n;
    }

    public async Task ReindexFileAsync(string filePath, CancellationToken ct = default)
    {
        if (!File.Exists(filePath)) { RemoveFile(filePath); return; }

        var content = await File.ReadAllTextAsync(filePath, ct);
        var hash = HashString(content);
        if (FileHashEquals(filePath, hash)) return;

        var chunks = Chunk(content).ToList();
        if (chunks.Count == 0) return;

        var embeddings = await EmbedBatchedAsync(chunks, ct);

        ReplaceChunks(filePath, hash, chunks, embeddings);
    }

    private async Task<List<float[]>> EmbedBatchedAsync(
        List<string> chunks, CancellationToken ct)
    {
        var output = new List<float[]>(chunks.Count);
        for (int i = 0; i < chunks.Count; i += EmbedBatch)
        {
            ct.ThrowIfCancellationRequested();
            var slice = chunks.GetRange(i, Math.Min(EmbedBatch, chunks.Count - i));
            var vectors = await ollama.EmbedAsync(slice, ct);
            output.AddRange(vectors);
        }
        return output;
    }

    private void ReplaceChunks(string filePath, string contentHash,
        List<string> chunks, List<float[]> embeddings)
    {
        using var conn = Open();
        using var tx = conn.BeginTransaction();

        using (var del = conn.CreateCommand())
        {
            del.Transaction = tx;
            del.CommandText = "DELETE FROM chunks WHERE file_path = $p";
            del.Parameters.AddWithValue("$p", filePath);
            del.ExecuteNonQuery();
        }

        var newRows = new List<IndexedChunk>(chunks.Count);
        for (int i = 0; i < chunks.Count; i++)
        {
            using var ins = conn.CreateCommand();
            ins.Transaction = tx;
            ins.CommandText = """
                INSERT INTO chunks (file_path, content_hash, chunk_index, text, embedding)
                VALUES ($p, $h, $i, $t, $e);
                SELECT last_insert_rowid();
                """;
            ins.Parameters.AddWithValue("$p", filePath);
            ins.Parameters.AddWithValue("$h", contentHash);
            ins.Parameters.AddWithValue("$i", i);
            ins.Parameters.AddWithValue("$t", chunks[i]);
            ins.Parameters.AddWithValue("$e", FloatsToBytes(embeddings[i]));
            var id = (long)ins.ExecuteScalar()!;
            newRows.Add(new IndexedChunk(id, filePath, i, chunks[i], embeddings[i]));
        }

        using (var upd = conn.CreateCommand())
        {
            upd.Transaction = tx;
            upd.CommandText = """
                INSERT INTO files (path, last_hash, last_indexed) VALUES ($p, $h, $t)
                ON CONFLICT(path) DO UPDATE SET last_hash = excluded.last_hash, last_indexed = excluded.last_indexed;
                """;
            upd.Parameters.AddWithValue("$p", filePath);
            upd.Parameters.AddWithValue("$h", contentHash);
            upd.Parameters.AddWithValue("$t", DateTime.UtcNow.ToString("O"));
            upd.ExecuteNonQuery();
        }

        tx.Commit();

        memLock.EnterWriteLock();
        try
        {
            memory.RemoveAll(c => c.FilePath == filePath);
            memory.AddRange(newRows);
        }
        finally { memLock.ExitWriteLock(); }

        try { FileReindexed?.Invoke(filePath); }
        catch (Exception ex) { log.LogWarning(ex, "FileReindexed handler threw for {Path}", filePath); }
    }

    private void RemoveFile(string filePath)
    {
        using var conn = Open();
        using (var del = conn.CreateCommand())
        {
            del.CommandText = "DELETE FROM chunks WHERE file_path = $p; DELETE FROM files WHERE path = $p;";
            del.Parameters.AddWithValue("$p", filePath);
            del.ExecuteNonQuery();
        }
        memLock.EnterWriteLock();
        try { memory.RemoveAll(c => c.FilePath == filePath); }
        finally { memLock.ExitWriteLock(); }
    }

    // ── Filesystem watcher ──────────────────────────────────────────────────────

    private void StartWatcher()
    {
        try
        {
            watcher = new FileSystemWatcher(dataRoot)
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime,
                Filter = "*.json",
            };
            watcher.Changed += OnFs;
            watcher.Created += OnFs;
            watcher.Renamed += (_, e) => Enqueue(e.FullPath);
            watcher.Deleted += (_, e) => RemoveFile(e.FullPath);
            watcher.EnableRaisingEvents = true;
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Could not start FileSystemWatcher on {Root}", dataRoot);
        }
    }

    private void OnFs(object _, FileSystemEventArgs e) => Enqueue(e.FullPath);

    private void Enqueue(string path)
    {
        if (!IsIndexable(path)) return;
        pending[path] = DateTime.UtcNow;
        flushTimer.Change(DebounceMs, Timeout.Infinite);
    }

    private async Task FlushPendingAsync()
    {
        if (pending.IsEmpty) return;
        var snapshot = pending.ToArray();
        foreach (var (path, _) in snapshot)
        {
            pending.TryRemove(path, out _);
            try { await ReindexFileAsync(path); }
            catch (Exception ex) { log.LogWarning(ex, "Watcher reindex failed for {Path}", path); }
        }
        lastIndexedAt = DateTime.UtcNow;
    }

    // ── Indexable-file rules ────────────────────────────────────────────────────

    private static readonly string[] SkipSegments =
    {
        $"{Path.DirectorySeparatorChar}archives{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}logs{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}exports{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}media{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}.cdiag{Path.DirectorySeparatorChar}",
    };

    private static readonly string[] SkipFiles =
    {
        "world_graph.json",
        "embeddings.db",
        "continuity.db",
    };

    private bool IsIndexable(string path)
    {
        if (!path.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) return false;
        var norm = Path.DirectorySeparatorChar + path.Replace('/', Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (SkipSegments.Any(s => norm.Contains(s, StringComparison.OrdinalIgnoreCase))) return false;
        var name = Path.GetFileName(path);
        if (SkipFiles.Any(f => string.Equals(f, name, StringComparison.OrdinalIgnoreCase))) return false;
        return true;
    }

    private IEnumerable<string> EnumerateIndexableFiles()
        => Directory.EnumerateFiles(dataRoot, "*.json", SearchOption.AllDirectories)
            .Where(IsIndexable);

    // ── Chunking ────────────────────────────────────────────────────────────────

    private static IEnumerable<string> Chunk(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) yield break;
        int start = 0;
        while (start < text.Length)
        {
            int len = Math.Min(ChunkSize, text.Length - start);
            yield return text.Substring(start, len).Trim();
            if (start + len >= text.Length) break;
            start += ChunkSize - ChunkOverlap;
        }
    }

    // ── Persistence ─────────────────────────────────────────────────────────────

    private SqliteConnection Open()
    {
        var conn = new SqliteConnection($"Data Source={dbPath}");
        conn.Open();
        return conn;
    }

    private void EnsureSchema()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = SchemaSql;
        cmd.ExecuteNonQuery();
    }

    private void LoadIntoMemory()
    {
        memLock.EnterWriteLock();
        try
        {
            memory.Clear();
            using var conn = Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT id, file_path, chunk_index, text, embedding FROM chunks";
            using var rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                var id    = rdr.GetInt64(0);
                var path  = rdr.GetString(1);
                var idx   = rdr.GetInt32(2);
                var text  = rdr.GetString(3);
                var blob  = (byte[])rdr["embedding"];
                memory.Add(new IndexedChunk(id, path, idx, text, BytesToFloats(blob)));
            }

            using var fcmd = conn.CreateCommand();
            fcmd.CommandText = "SELECT MAX(last_indexed) FROM files";
            var v = fcmd.ExecuteScalar();
            if (v is string s && DateTime.TryParse(s, out var dt))
                lastIndexedAt = dt;
        }
        finally { memLock.ExitWriteLock(); }
    }

    private Dictionary<string, string> LoadFileHashes()
    {
        var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT path, last_hash FROM files";
        using var rdr = cmd.ExecuteReader();
        while (rdr.Read()) d[rdr.GetString(0)] = rdr.GetString(1);
        return d;
    }

    private bool FileHashEquals(string path, string hash)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT last_hash FROM files WHERE path = $p";
        cmd.Parameters.AddWithValue("$p", path);
        return cmd.ExecuteScalar() is string s && s == hash;
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────

    private static string HashFile(string path)
    {
        using var stream = File.OpenRead(path);
        var bytes = SHA256.HashData(stream);
        return Convert.ToHexString(bytes);
    }

    private static string HashString(string s)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(s)));

    private static byte[] FloatsToBytes(float[] v)
    {
        var b = new byte[v.Length * 4];
        Buffer.BlockCopy(v, 0, b, 0, b.Length);
        return b;
    }

    private static float[] BytesToFloats(byte[] b)
    {
        var v = new float[b.Length / 4];
        Buffer.BlockCopy(b, 0, v, 0, b.Length);
        return v;
    }

    private static float Cosine(float[] a, float[] b)
    {
        if (a.Length != b.Length) return 0f;
        double dot = 0, na = 0, nb = 0;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            na  += a[i] * a[i];
            nb  += b[i] * b[i];
        }
        var denom = Math.Sqrt(na) * Math.Sqrt(nb);
        return denom == 0 ? 0f : (float)(dot / denom);
    }

    public void Dispose()
    {
        watcher?.Dispose();
        flushTimer.Dispose();
        memLock.Dispose();
    }
}
