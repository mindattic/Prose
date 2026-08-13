using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Core.Services;

/// <summary>
/// Embedding cache + similarity search over the canonical entity corpus.
///
/// <para><b>Storage.</b> One row per entity in <see cref="EntityEmbedding"/>,
/// keyed on <c>EntityId</c>. The vector is JSON-serialised floats so the
/// table works on every SQL Server version (2022+ supports JSON natively;
/// 2025's <c>VECTOR(1536)</c> is a one-shot ALTER + cast away when the
/// preview is GA). Cosine distance runs in C# until the column type is
/// upgraded — at ~10k entities the exact-NN scan is sub-second.</para>
///
/// <para><b>Drift detection.</b> Every row carries the SHA-256 of the
/// source text. <see cref="EnsureFreshAsync"/> recomputes the hash, skips
/// the embed if unchanged, and re-embeds when it differs. Cheap.</para>
///
/// <para><b>Cost.</b> OpenAI <c>text-embedding-3-small</c> is $0.02 / 1M
/// tokens. The full ~10k-entity corpus is ~$0.10 to embed once; per-save
/// re-embeds are sub-cent.</para>
/// </summary>
public class EmbeddingService
{
    private const string Model = "text-embedding-3-small";
    private const int    Dimensions = 1536;

    /// <summary>ProseEmbeddings ScopeKind for a node beat (Beat.Id keyed). Distinct
    /// from 'beat' (which keys ChapterBeat.BeatGuid) so the two content models
    /// never collide in the polymorphic prose table.</summary>
    private const string ScopeBeatNode = "BeatNode";

    private readonly IDbContextFactory<ProseDbContext> dbFactory;
    private readonly SettingsService settings;
    private readonly IHttpClientFactory httpFactory;
    private readonly ILogger<EmbeddingService> log;

    // Schema-bootstrap latch: EnsureSchemaAsync is idempotent but cheap to skip
    // after the first hit. The lazy gate keeps every call site (ReembedCli,
    // ContinuousQualityService, ad-hoc Find/Ensure callers) consistent without
    // each having to remember.
    private int schemaReady; // 0 = not yet, 1 = checked
    private async Task EnsureSchemaOnceAsync(CancellationToken ct)
    {
        if (System.Threading.Interlocked.CompareExchange(ref schemaReady, 1, 0) == 1) return;
        try { await EnsureSchemaAsync(ct); }
        catch { schemaReady = 0; throw; }
    }

    public EmbeddingService(
        IDbContextFactory<ProseDbContext> dbFactory,
        SettingsService settings,
        IHttpClientFactory httpFactory,
        ILogger<EmbeddingService> log)
    {
        this.dbFactory   = dbFactory;
        this.settings    = settings;
        this.httpFactory = httpFactory;
        this.log         = log;
    }

    /// <summary>
    /// Idempotent: creates <c>EntityEmbeddings</c> on a live DB if it isn't
    /// already there. Uses SQL Server 2025's native <c>VECTOR(1536)</c> type;
    /// the database needs <c>PREVIEW_FEATURES = ON</c> to accept it.
    /// Deliberately NOT system-versioned (vector + temporal don't mix) and
    /// no DiskANN index (DiskANN requires a single-INT clustered PK; ours is
    /// a UNIQUEIDENTIFIER for FK alignment with Entities, and exact NN via
    /// VECTOR_DISTANCE is sub-second at this corpus size).
    /// </summary>
    public async Task EnsureSchemaAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        const string ddl = """
            ALTER DATABASE SCOPED CONFIGURATION SET PREVIEW_FEATURES = ON;
            IF OBJECT_ID('dbo.EntityEmbeddings','U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[EntityEmbeddings] (
                    [EntityId]   UNIQUEIDENTIFIER NOT NULL,
                    [UniverseId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_EntityEmbeddings_UniverseId] DEFAULT '0197e9c9-0001-7000-8000-000000000001',
                    [SourceHash] VARBINARY(32)    NOT NULL,
                    [Vector]     VECTOR(1536)     NOT NULL,
                    [Dimensions] INT              NOT NULL,
                    [EmbeddedAt] DATETIME2(7)     NOT NULL,
                    [Model]      NVARCHAR(80)     NOT NULL,
                    CONSTRAINT [PK_EntityEmbeddings] PRIMARY KEY ([EntityId]),
                    CONSTRAINT [FK_EntityEmbeddings_Entities_EntityId]
                        FOREIGN KEY ([EntityId]) REFERENCES [dbo].[Entities]([Id]) ON DELETE CASCADE
                );
                CREATE INDEX [IX_EntityEmbeddings_EmbeddedAt]
                    ON [dbo].[EntityEmbeddings]([EmbeddedAt]);
            END;

            IF OBJECT_ID('dbo.ProseEmbeddings','U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[ProseEmbeddings] (
                    [ScopeKind]  NVARCHAR(20)     NOT NULL,
                    [ScopeId]    UNIQUEIDENTIFIER NOT NULL,
                    [UniverseId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_ProseEmbeddings_UniverseId] DEFAULT '0197e9c9-0001-7000-8000-000000000001',
                    [SourceHash] VARBINARY(32)    NOT NULL,
                    [Vector]     VECTOR(1536)     NOT NULL,
                    [Dimensions] INT              NOT NULL,
                    [EmbeddedAt] DATETIME2(7)     NOT NULL,
                    [Model]      NVARCHAR(80)     NOT NULL,
                    CONSTRAINT [PK_ProseEmbeddings] PRIMARY KEY ([ScopeKind], [ScopeId])
                );
                CREATE INDEX [IX_ProseEmbeddings_EmbeddedAt]
                    ON [dbo].[ProseEmbeddings]([EmbeddedAt]);
                CREATE INDEX [IX_ProseEmbeddings_Scope_EmbeddedAt]
                    ON [dbo].[ProseEmbeddings]([ScopeKind], [EmbeddedAt]);
            END;
            """;
        await db.Database.ExecuteSqlRawAsync(ddl, ct);
    }

    /// <summary>
    /// Build the canonical text we embed for an entity. Uses Name + Description
    /// + tags as a stable, semantically-rich digest. Anything more elaborate
    /// (full Records.Json) inflates token cost without improving retrieval —
    /// embeddings are designed for prose, not structured fields.
    /// </summary>
    public static string BuildSourceText(Entity entity, string? extra = null)
    {
        // text-embedding-3-small caps inputs at 8191 tokens. At ~3.5 chars per
        // token for English prose, 25,000 characters is safely under the limit
        // even for verbose entries. Truncating here is cheap and means a
        // single oversized description never sinks the whole batch.
        const int MaxChars = 25_000;
        var sb = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(entity.Name)) sb.Append(entity.Name).Append('\n');
        if (!string.IsNullOrWhiteSpace(entity.EntityType)) sb.Append('[').Append(entity.EntityType).Append("]\n");
        if (!string.IsNullOrWhiteSpace(entity.Description)) sb.Append(entity.Description).Append('\n');
        if (!string.IsNullOrWhiteSpace(extra)) sb.Append(extra);
        var text = sb.ToString().Trim();
        return text.Length <= MaxChars ? text : text[..MaxChars];
    }

    public static byte[] Hash(string text)
        => SHA256.HashData(Encoding.UTF8.GetBytes(text ?? ""));

    /// <summary>
    /// Cap an arbitrary prose chunk to a token-safe length before sending it to
    /// the embedding API. Same 25,000-char cap as <see cref="BuildSourceText"/>.
    /// </summary>
    public static string TruncateForEmbed(string text)
    {
        const int MaxChars = 25_000;
        if (string.IsNullOrEmpty(text)) return "";
        return text.Length <= MaxChars ? text : text[..MaxChars];
    }

    // ── Prose embedding (chapter / beat) ──────────────────────────────────

    /// <summary>
    /// Drift-skipped upsert for a prose embedding. Same SHA-256 / re-embed-on-
    /// change pattern as <see cref="EnsureFreshAsync"/> but in the polymorphic
    /// <c>ProseEmbeddings</c> table keyed on <paramref name="scopeKind"/>
    /// ('chapter' | 'beat') + <paramref name="scopeId"/>.
    /// </summary>
    public async Task<bool> EnsureProseFreshAsync(
        string scopeKind, Guid scopeId, string sourceText, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(scopeKind) || string.IsNullOrWhiteSpace(sourceText)) return false;
        await EnsureSchemaOnceAsync(ct);
        var hash = Hash(sourceText);

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var existingHash = await db.ProseEmbeddings.AsNoTracking()
            .Where(x => x.ScopeKind == scopeKind && x.ScopeId == scopeId)
            .Select(x => x.SourceHash)
            .FirstOrDefaultAsync(ct);
        if (existingHash != null && existingHash.AsSpan().SequenceEqual(hash))
            return false;

        var vector = await EmbedAsync(TruncateForEmbed(sourceText), ct);
        if (vector.Length == 0) return false;

        await UpsertProseVectorRawAsync(db, scopeKind, scopeId, hash, vector, ct);
        return true;
    }

    /// <summary>
    /// Embed every tracked markdown file under the <c>markdown</c> scope (keyed on
    /// <c>MarkdownFile.Id</c>), drift-skipped by content hash. Backs the Doc Context
    /// Stack's semantic topic triggering. Returns the count of rows written/refreshed.
    /// </summary>
    public async Task<int> ReembedMarkdownAsync(
        IProgress<(int done, int total)>? progress = null, CancellationToken ct = default)
    {
        await EnsureSchemaOnceAsync(ct);
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var docs = await db.MarkdownFiles.AsNoTracking()
            .Select(m => new { m.Id, m.RelativePath, m.Content })
            .ToListAsync(ct);

        int embedded = 0, done = 0;
        foreach (var d in docs)
        {
            // Path carries topic words (e.g. "schism"); body gives semantic depth.
            var source = $"{d.RelativePath}\n{d.Content}";
            if (!string.IsNullOrWhiteSpace(source) && await EnsureProseFreshAsync("markdown", d.Id, source, ct))
                embedded++;
            progress?.Report((++done, docs.Count));
        }
        return embedded;
    }

    private async Task UpsertProseVectorRawAsync(
        ProseDbContext db, string scopeKind, Guid scopeId, byte[] hash, float[] vector, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(vector);
        const string sql = """
            MERGE dbo.ProseEmbeddings AS t
            USING (SELECT @p_kind AS ScopeKind, @p_id AS ScopeId) AS s
            ON t.ScopeKind = s.ScopeKind AND t.ScopeId = s.ScopeId
            WHEN MATCHED THEN
                UPDATE SET SourceHash = @p_hash,
                           Vector     = CAST(@p_json AS VECTOR(1536)),
                           Dimensions = @p_dims,
                           EmbeddedAt = @p_at,
                           Model      = @p_model,
                           UniverseId = @p_universe
            WHEN NOT MATCHED THEN
                INSERT (ScopeKind, ScopeId, UniverseId, SourceHash, Vector, Dimensions, EmbeddedAt, Model)
                VALUES (@p_kind, @p_id, @p_universe, @p_hash, CAST(@p_json AS VECTOR(1536)), @p_dims, @p_at, @p_model);
            """;
        await db.Database.ExecuteSqlRawAsync(sql,
            new Microsoft.Data.SqlClient.SqlParameter("@p_universe", EmbedUniverseId()),
            new Microsoft.Data.SqlClient.SqlParameter("@p_kind", scopeKind),
            new Microsoft.Data.SqlClient.SqlParameter("@p_id", scopeId),
            new Microsoft.Data.SqlClient.SqlParameter("@p_hash", hash),
            new Microsoft.Data.SqlClient.SqlParameter("@p_json", json),
            new Microsoft.Data.SqlClient.SqlParameter("@p_dims", vector.Length),
            new Microsoft.Data.SqlClient.SqlParameter("@p_at", DateTime.UtcNow),
            new Microsoft.Data.SqlClient.SqlParameter("@p_model", EffectiveModel));
    }

    /// <summary>
    /// Find the top-<paramref name="k"/> prose units (chapters or beats) most
    /// semantically similar to <paramref name="queryText"/>, optionally
    /// filtered to one ScopeKind. Server-side cosine via VECTOR_DISTANCE.
    /// </summary>
    public async Task<IReadOnlyList<ProseEmbeddingHit>> FindSimilarProseAsync(
        string queryText,
        int k = 5,
        string? scopeKind = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(queryText)) return Array.Empty<ProseEmbeddingHit>();
        await EnsureSchemaOnceAsync(ct);

        var queryVector = await EmbedAsync(TruncateForEmbed(queryText), ct);
        if (queryVector.Length == 0) return Array.Empty<ProseEmbeddingHit>();
        var queryJson = JsonSerializer.Serialize(queryVector);

        var parameters = new List<Microsoft.Data.SqlClient.SqlParameter>
        {
            new("@p_k", Math.Max(1, k)),
            new("@p_query", queryJson),
        };
        var scopeFilter = "";
        if (!string.IsNullOrWhiteSpace(scopeKind))
        {
            scopeFilter = " WHERE ScopeKind = @p_kind";
            parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@p_kind", scopeKind));
        }

        // NOTE: deliberately NOT filtering on ProseEmbeddings.UniverseId here (unlike
        // FindSimilarAsync / FindSimilarBeatNodesAsync, which join to the authoritative
        // Entities/Nodes table). That column is a drift-prone copy stamped from whatever
        // universe happened to be active when the row was last (re)embedded — trusting it
        // directly caused the SS-A46 cross-universe leak fixed elsewhere in this file.
        // This method has no authoritative per-ScopeKind table to join instead (its one live
        // caller uses ScopeKind="markdown", and MarkdownFile isn't a universe-owned entity —
        // docs like CRAFT.md are intentionally cross-universe). Correctness here comes from
        // the CALLER: DocContextService only keeps a hit whose ScopeId is already present in
        // its own universe/node-scoped candidate set (see the `byId.TryGetValue` gate in
        // PrepareContextAsync step 4) — an unfiltered hit that isn't a valid candidate is
        // simply dropped, never leaked. If a future ScopeKind needs true DB-level universe
        // isolation, join to its authoritative source table instead of adding this filter back.
        var sql = $"""
            SELECT TOP (@p_k)
                ScopeKind, ScopeId,
                1.0 - VECTOR_DISTANCE('cosine', Vector, CAST(@p_query AS VECTOR(1536))) AS Similarity
            FROM dbo.ProseEmbeddings{scopeFilter}
            ORDER BY VECTOR_DISTANCE('cosine', Vector, CAST(@p_query AS VECTOR(1536))) ASC;
            """;

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var rows = await db.Database.SqlQueryRaw<ProseEmbeddingRow>(sql, parameters.ToArray<object>())
            .ToListAsync(ct);
        return rows
            .Select(r => new ProseEmbeddingHit(r.ScopeKind ?? "", r.ScopeId, r.Similarity))
            .ToList();
    }

    // ── Node-beat prose (the live writer/node model) ──────────────────

    /// <summary>
    /// Embed the enabled beats of one node into <c>ProseEmbeddings</c> under
    /// the <see cref="ScopeBeatNode"/> scope (keyed on <c>Beat.Id</c>). This
    /// is the live node/Beats model — distinct from <see cref="ReembedProseCorpusAsync"/>,
    /// which embeds the older Chapter/ChapterBeat model. Drift-skipped; returns
    /// the count newly (re)embedded. Cheap: a novella is a few cents.
    /// </summary>
    public async Task<int> ReembedBeatNodesAsync(Guid nodeId, CancellationToken ct = default)
    {
        await EnsureSchemaOnceAsync(ct);
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var searchIds = await NodeWorkbenchService.GetLeafDescendantIdsAsync(db, nodeId, ct);

        var beats = await (from sb in db.BeatNodes.AsNoTracking()
                           join b in db.Beats.AsNoTracking() on sb.BeatId equals b.Id
                           where searchIds.Contains(sb.NodeId) && true
                           orderby sb.SortKey
                           select new { b.Id, b.Title, b.Description, b.Text }).ToListAsync(ct);
        if (beats.Count == 0) return 0;

        var prepped = beats
            .Select(b => { var t = BuildBeatSourceText(b.Title, b.Description, b.Text); return (b.Id, Text: t, Hash: Hash(t)); })
            .Where(p => !string.IsNullOrWhiteSpace(p.Text))
            .ToList();

        var existing = await db.ProseEmbeddings.AsNoTracking()
            .Where(x => x.ScopeKind == ScopeBeatNode)
            .Select(x => new { x.ScopeId, x.SourceHash })
            .ToListAsync(ct);
        var existingDict = existing.ToDictionary(x => x.ScopeId, x => x.SourceHash);

        var toEmbed = prepped
            .Where(p => !(existingDict.TryGetValue(p.Id, out var h) && h.AsSpan().SequenceEqual(p.Hash)))
            .ToList();
        if (toEmbed.Count == 0) return 0;

        const int BatchSize = 64;
        int touched = 0;
        for (int start = 0; start < toEmbed.Count; start += BatchSize)
        {
            if (ct.IsCancellationRequested) break;
            var slice = toEmbed.Skip(start).Take(BatchSize).ToList();
            var texts = slice.Select(s => TruncateForEmbed(s.Text)).ToList();

            float[][] vectors;
            try { vectors = await EmbedBatchAsync(texts, ct); }
            catch (Exception ex) { log.LogWarning(ex, "Node-beat embed batch failed at offset {Offset}", start); continue; }
            if (vectors.Length != slice.Count) { log.LogWarning("Node-beat batch returned {Got}/{Sent}", vectors.Length, slice.Count); continue; }

            await using var batchDb = await dbFactory.CreateDbContextAsync(ct);
            for (int i = 0; i < slice.Count; i++)
            {
                var v = vectors[i];
                if (v.Length == 0) continue;
                await UpsertProseVectorRawAsync(batchDb, ScopeBeatNode, slice[i].Id, slice[i].Hash, v, ct);
                touched++;
            }
        }
        return touched;
    }

    /// <summary>
    /// Top-<paramref name="k"/> node beats most similar to <paramref name="queryText"/>,
    /// optionally restricted to a single node. Only enabled beats are searched
    /// (the BeatNodes join filters soft-deletes). Returns hits keyed on Beat.Id.
    /// </summary>
    public async Task<IReadOnlyList<ProseEmbeddingHit>> FindSimilarBeatNodesAsync(
        string queryText, int k = 6, Guid? nodeScope = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(queryText)) return Array.Empty<ProseEmbeddingHit>();
        await EnsureSchemaOnceAsync(ct);

        var queryVector = await EmbedAsync(TruncateForEmbed(queryText), ct);
        if (queryVector.Length == 0) return Array.Empty<ProseEmbeddingHit>();
        var queryJson = JsonSerializer.Serialize(queryVector);

        // Pull 2x then dedupe in C# — a beat can live in more than one node,
        // so the BeatNodes join can surface the same Beat.Id twice.
        var parameters = new List<Microsoft.Data.SqlClient.SqlParameter>
        {
            new("@p_k", Math.Max(1, k) * 2),
            new("@p_query", queryJson),
            new("@p_universe", QueryUniverseId()),
        };
        var scopeFilter = "";
        if (nodeScope is Guid sid)
        {
            scopeFilter = " AND sb.NodeId = @p_node";
            parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@p_node", sid));
        }

        var sql = $"""
            SELECT TOP (@p_k)
                pe.ScopeId AS ScopeId,
                1.0 - VECTOR_DISTANCE('cosine', pe.Vector, CAST(@p_query AS VECTOR(1536))) AS Similarity
            FROM dbo.ProseEmbeddings pe
            JOIN dbo.BeatNodes sb ON sb.BeatId = pe.ScopeId AND true = 1
            JOIN dbo.Nodes n ON n.Id = sb.NodeId
            WHERE pe.ScopeKind = '{ScopeBeatNode}'
              -- Filter on the NODE's universe (authoritative), NOT pe.UniverseId: the embedding
              -- tag is a drift-prone copy that silently defaults to GLMZ when a beat is embedded
              -- without an active scope — the same class of bug fixed for EntityEmbeddings in
              -- FindSimilarAsync above (SS-A46). n.UniverseId is the single source of truth for
              -- which universe a beat actually belongs to.
              AND (@p_universe = '00000000-0000-0000-0000-000000000000' OR n.UniverseId = @p_universe){scopeFilter}
            ORDER BY VECTOR_DISTANCE('cosine', pe.Vector, CAST(@p_query AS VECTOR(1536))) ASC;
            """;

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var rows = await db.Database.SqlQueryRaw<ProseScopeRow>(sql, parameters.ToArray<object>())
            .ToListAsync(ct);
        return rows
            .GroupBy(r => r.ScopeId).Select(g => g.First())
            .Take(Math.Max(1, k))
            .Select(r => new ProseEmbeddingHit(ScopeBeatNode, r.ScopeId, r.Similarity))
            .ToList();
    }

    /// <summary>Row shape for the node-beat VECTOR_DISTANCE query.</summary>
    private sealed class ProseScopeRow
    {
        public Guid   ScopeId    { get; set; }
        public double Similarity { get; set; }
    }

    // ── Pairwise similarity ───────────────────────────────────────────────

    /// <summary>
    /// Embed a list of text pairs in a single API batch call and return the
    /// cosine similarity for each pair. Input order is preserved. Returns 0.0
    /// for a pair if the API fails or either text is empty.
    /// </summary>
    public async Task<IReadOnlyList<double>> ComputeSimilaritiesBatchAsync(
        IReadOnlyList<(string A, string B)> pairs,
        CancellationToken ct = default)
    {
        if (pairs.Count == 0) return Array.Empty<double>();
        await EnsureSchemaOnceAsync(ct);

        // Flatten to [a0, b0, a1, b1, ...] so one batch call covers all pairs.
        var texts = pairs
            .SelectMany(p => new[] { TruncateForEmbed(p.A), TruncateForEmbed(p.B) })
            .ToList();
        var vectors = await EmbedBatchAsync(texts, ct);
        if (vectors.Length != texts.Count) return Enumerable.Repeat(0.0, pairs.Count).ToArray();

        var results = new double[pairs.Count];
        for (int i = 0; i < pairs.Count; i++)
            results[i] = CosineSimilarity(vectors[i * 2], vectors[i * 2 + 1]);
        return results;
    }

    /// <summary>
    /// Compute the cosine similarity between two arbitrary text strings.
    /// Delegates to <see cref="ComputeSimilaritiesBatchAsync"/> (one API call).
    /// Useful for comparing intent (Beat.Synopsis) against execution (Beat.Text).
    /// Returns 0 on API failure.
    /// </summary>
    public async Task<double> ComputeSimilarityAsync(string a, string b, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b)) return 0;
        var results = await ComputeSimilaritiesBatchAsync([(a, b)], ct);
        return results.Count > 0 ? results[0] : 0;
    }

    /// <summary>Bulk re-embed every chapter + beat in canon. Idempotent (drift-skipped).</summary>
    public async Task<int> ReembedProseCorpusAsync(
        IProgress<(int done, int total, string current)>? progress = null,
        CancellationToken ct = default)
    {
        await EnsureSchemaAsync(ct);
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        // Pull every chapter (id, title, synopsis, html) and every beat
        // (beat-guid, title, synopsis, text). Pre-compute source text + hash
        // for drift-skip, then re-embed only what changed.
        var chapters = await db.Set<Data.Entities.Chapter>().AsNoTracking()
            .Select(c => new { c.Id, c.Title, c.Synopsis, c.Html })
            .ToListAsync(ct);
        var beats = await db.Set<Data.Entities.ChapterBeat>().AsNoTracking()
            .Select(bt => new { bt.BeatGuid, bt.Title, bt.Synopsis, bt.Text })
            .ToListAsync(ct);

        var prepped = new List<(string Kind, Guid Id, string Text, byte[] Hash, string Label)>(chapters.Count + beats.Count);
        foreach (var c in chapters)
        {
            var txt = BuildChapterSourceText(c.Title, c.Synopsis, c.Html);
            if (string.IsNullOrWhiteSpace(txt)) continue;
            prepped.Add(("chapter", c.Id, txt, Hash(txt), $"chapter:{c.Title}"));
        }
        foreach (var b in beats)
        {
            var txt = BuildBeatSourceText(b.Title, b.Synopsis, b.Text);
            if (string.IsNullOrWhiteSpace(txt)) continue;
            prepped.Add(("beat", b.BeatGuid, txt, Hash(txt), $"beat:{b.Title}"));
        }

        // Drift-skip: pull existing hashes, filter the prepped list down.
        var existing = await db.ProseEmbeddings.AsNoTracking()
            .Select(x => new { x.ScopeKind, x.ScopeId, x.SourceHash })
            .ToListAsync(ct);
        var existingDict = existing.ToDictionary(
            x => (x.ScopeKind, x.ScopeId),
            x => x.SourceHash);
        var toEmbed = prepped
            .Where(p => !(existingDict.TryGetValue((p.Kind, p.Id), out var h) && h.AsSpan().SequenceEqual(p.Hash)))
            .ToList();

        if (toEmbed.Count == 0)
        {
            progress?.Report((prepped.Count, prepped.Count, "all fresh"));
            return 0;
        }

        const int BatchSize = 64; // smaller than entity batches — chapter prose is heavier
        int touched = 0, processed = 0;
        for (int start = 0; start < toEmbed.Count; start += BatchSize)
        {
            if (ct.IsCancellationRequested) break;
            var slice = toEmbed.Skip(start).Take(BatchSize).ToList();
            var texts = slice.Select(s => TruncateForEmbed(s.Text)).ToList();

            float[][] vectors;
            try { vectors = await EmbedBatchAsync(texts, ct); }
            catch (Exception ex)
            {
                log.LogWarning(ex, "Prose embedding batch failed at offset {Offset}", start);
                processed += slice.Count;
                progress?.Report((processed, toEmbed.Count, "batch failed"));
                continue;
            }
            if (vectors.Length != slice.Count)
            {
                log.LogWarning("Prose batch returned {Got}/{Sent} vectors", vectors.Length, slice.Count);
                processed += slice.Count;
                progress?.Report((processed, toEmbed.Count, "incomplete batch"));
                continue;
            }

            await using var batchDb = await dbFactory.CreateDbContextAsync(ct);
            for (int i = 0; i < slice.Count; i++)
            {
                var item = slice[i];
                var v = vectors[i];
                if (v.Length == 0) continue;
                await UpsertProseVectorRawAsync(batchDb, item.Kind, item.Id, item.Hash, v, ct);
                touched++;
            }

            processed += slice.Count;
            progress?.Report((processed, toEmbed.Count, slice.Last().Label));
        }
        return touched;
    }

    /// <summary>Compose a chapter's embedding source: title + synopsis + plain-text body.</summary>
    public static string BuildChapterSourceText(string? title, string? synopsis, string? html)
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(title)) sb.Append(title).Append('\n');
        if (!string.IsNullOrWhiteSpace(synopsis)) sb.Append(synopsis).Append('\n');
        if (!string.IsNullOrWhiteSpace(html))
        {
            // Strip simple HTML tags for the embedding — angle-bracket spans
            // are tokenization noise. Don't bother with a full HTML parser; a
            // regex tag-strip is sufficient for the embed-input corpus.
            var stripped = System.Text.RegularExpressions.Regex.Replace(html, @"<[^>]+>", " ");
            stripped = System.Text.RegularExpressions.Regex.Replace(stripped, @"\s+", " ").Trim();
            sb.Append(stripped);
        }
        return TruncateForEmbed(sb.ToString().Trim());
    }

    /// <summary>Compose a beat's embedding source: title + synopsis + body.</summary>
    public static string BuildBeatSourceText(string? title, string? synopsis, string? text)
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(title)) sb.Append(title).Append('\n');
        if (!string.IsNullOrWhiteSpace(synopsis)) sb.Append(synopsis).Append('\n');
        if (!string.IsNullOrWhiteSpace(text)) sb.Append(text);
        return TruncateForEmbed(sb.ToString().Trim());
    }

    /// <summary>Row shape for the prose VECTOR_DISTANCE query.</summary>
    private sealed class ProseEmbeddingRow
    {
        public string? ScopeKind  { get; set; }
        public Guid    ScopeId    { get; set; }
        public double  Similarity { get; set; }
    }

    /// <summary>
    /// Ensure the entity's embedding row is current. Computes the SHA-256
    /// of <paramref name="sourceText"/>; if it matches the stored hash,
    /// no-ops. Otherwise calls the cloud embedding API and upserts.
    /// </summary>
    public async Task<bool> EnsureFreshAsync(Guid entityId, string sourceText, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(sourceText)) return false;
        await EnsureSchemaOnceAsync(ct);
        var hash = Hash(sourceText);

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var existingHash = await db.EntityEmbeddings.AsNoTracking()
            .Where(x => x.EntityId == entityId)
            .Select(x => x.SourceHash)
            .FirstOrDefaultAsync(ct);
        if (existingHash != null && existingHash.AsSpan().SequenceEqual(hash))
            return false; // already fresh

        var vector = await EmbedAsync(sourceText, ct);
        if (vector.Length == 0) return false; // API failure logged inside

        await UpsertVectorRawAsync(db, entityId, hash, vector, ct);
        return true;
    }

    /// <summary>
    /// Upsert one row's worth of vector + metadata via raw SQL MERGE so the
    /// VECTOR(1536) column is populated server-side via CAST. EF Core can't
    /// bind the VECTOR type natively yet; this is the workaround.
    /// </summary>
    /// <summary>The universe to stamp on a freshly-written embedding (RFC 0006). Embedding runs
    /// under the current universe scope; fall back to GLMZ when no scope is wired.</summary>
    private static Guid EmbedUniverseId()
        => UniverseScope.EffectiveId == Guid.Empty ? Universe.GlmzId : UniverseScope.EffectiveId;

    /// <summary>Universe id used to FILTER a similarity query — the raw <see cref="UniverseScope.EffectiveId"/>;
    /// <c>Guid.Empty</c> means "no scope" and the SQL predicate lets every universe through.</summary>
    private static Guid QueryUniverseId() => UniverseScope.EffectiveId;

    private async Task UpsertVectorRawAsync(
        ProseDbContext db, Guid entityId, byte[] hash, float[] vector, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(vector);
        const string sql = """
            MERGE dbo.EntityEmbeddings AS t
            USING (SELECT @p_id AS EntityId) AS s
            ON t.EntityId = s.EntityId
            WHEN MATCHED THEN
                UPDATE SET SourceHash = @p_hash,
                           Vector     = CAST(@p_json AS VECTOR(1536)),
                           Dimensions = @p_dims,
                           EmbeddedAt = @p_at,
                           Model      = @p_model,
                           UniverseId = @p_universe
            WHEN NOT MATCHED THEN
                INSERT (EntityId, UniverseId, SourceHash, Vector, Dimensions, EmbeddedAt, Model)
                VALUES (@p_id, @p_universe, @p_hash, CAST(@p_json AS VECTOR(1536)), @p_dims, @p_at, @p_model);
            """;
        await db.Database.ExecuteSqlRawAsync(sql,
            new Microsoft.Data.SqlClient.SqlParameter("@p_id", entityId),
            new Microsoft.Data.SqlClient.SqlParameter("@p_universe", EmbedUniverseId()),
            new Microsoft.Data.SqlClient.SqlParameter("@p_hash", hash),
            new Microsoft.Data.SqlClient.SqlParameter("@p_json", json),
            new Microsoft.Data.SqlClient.SqlParameter("@p_dims", vector.Length),
            new Microsoft.Data.SqlClient.SqlParameter("@p_at", DateTime.UtcNow),
            new Microsoft.Data.SqlClient.SqlParameter("@p_model", EffectiveModel));
    }

    /// <summary>
    /// Find the top-<paramref name="k"/> entities whose embeddings are most
    /// similar (cosine distance) to <paramref name="queryText"/>. Optionally
    /// filter to specific entity types. Returns hits sorted best-first.
    /// </summary>
    public async Task<IReadOnlyList<EmbeddingHit>> FindSimilarAsync(
        string queryText,
        int k = 8,
        IReadOnlyCollection<string>? entityTypes = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(queryText)) return Array.Empty<EmbeddingHit>();
        await EnsureSchemaOnceAsync(ct);

        var queryVector = await EmbedAsync(TruncateForEmbed(queryText), ct);
        if (queryVector.Length == 0) return Array.Empty<EmbeddingHit>();

        // Push the cosine math to SQL Server via VECTOR_DISTANCE — at the
        // current corpus (~10k vectors) it's a single sub-second TOP-N seek
        // that doesn't drag a megabyte of JSON over the wire just to score it.
        // Returns distance (0 = identical, 2 = opposite); we convert to
        // similarity = 1 - distance for the existing API contract.
        var queryJson = JsonSerializer.Serialize(queryVector);

        var typeFilter = "";
        var parameters = new List<Microsoft.Data.SqlClient.SqlParameter>
        {
            new("@p_k", Math.Max(1, k)),
            new("@p_query", queryJson),
            new("@p_universe", QueryUniverseId()),
        };
        if (entityTypes is { Count: > 0 })
        {
            // Build a parameterised IN-list. Using positional parameters keeps
            // injection-safe and avoids string concat of user-influenced values.
            var typeParamNames = entityTypes
                .Select((_, i) => "@p_t" + i)
                .ToArray();
            typeFilter = $" AND ent.EntityType IN ({string.Join(", ", typeParamNames)})";
            int idx = 0;
            foreach (var t in entityTypes)
                parameters.Add(new Microsoft.Data.SqlClient.SqlParameter(typeParamNames[idx++], t));
        }

        var sql = $"""
            SELECT TOP (@p_k)
                emb.EntityId AS EntityId,
                ent.Name     AS EntityName,
                ent.EntityType AS EntityType,
                1.0 - VECTOR_DISTANCE('cosine', emb.Vector, CAST(@p_query AS VECTOR(1536))) AS Similarity
            FROM dbo.EntityEmbeddings emb
            JOIN dbo.Entities ent ON ent.Id = emb.EntityId
            WHERE ent.IsActive = 1
              -- Filter on the ENTITY registry's universe (authoritative), NOT emb.UniverseId:
              -- the embedding tag is a drift-prone copy that silently defaults to GLMZ when an
              -- entity is embedded without an active scope, which leaked SCRY quotes (e.g. Wren
              -- Caerglas, Dame Lyra) into GLMZ blueprint anchor searches. ent.UniverseId is the
              -- single source of truth. (SS-A46 cross-universe leak fix.)
              AND (@p_universe = '00000000-0000-0000-0000-000000000000' OR ent.UniverseId = @p_universe){typeFilter}
            ORDER BY VECTOR_DISTANCE('cosine', emb.Vector, CAST(@p_query AS VECTOR(1536))) ASC;
            """;

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var hits = await db.Database.SqlQueryRaw<EmbeddingRow>(sql, parameters.ToArray<object>())
            .ToListAsync(ct);
        return hits
            .Select(h => new EmbeddingHit(h.EntityId, h.EntityName ?? "", h.EntityType ?? "", h.Similarity))
            .ToList();
    }

    /// <summary>Row shape for the VECTOR_DISTANCE TOP-N query.</summary>
    private sealed class EmbeddingRow
    {
        public Guid    EntityId   { get; set; }
        public string? EntityName { get; set; }
        public string? EntityType { get; set; }
        public double  Similarity { get; set; }
    }

    /// <summary>
    /// Bulk re-embed every active entity that has either no row or a stale
    /// hash. Idempotent. Returns the count of rows newly written.
    /// </summary>
    public async Task<int> ReembedCorpusAsync(
        IProgress<(int done, int total)>? progress = null,
        CancellationToken ct = default)
    {
        await EnsureSchemaAsync(ct);
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        // Hygiene: drop embedding rows for archived/inactive entities. Retrieval
        // already joins on Entities.IsActive = 1, so these rows can never surface
        // in a similarity hit — they are pure dead weight, and derived data is
        // rebuildable, so a hard delete is safe here.
        var purged = await db.Database.ExecuteSqlRawAsync(
            "DELETE emb FROM dbo.EntityEmbeddings emb JOIN dbo.Entities ent ON ent.Id = emb.EntityId WHERE ent.IsActive = 0;", ct);
        if (purged > 0)
            log.LogInformation("ReembedCorpus: purged {Count} embedding row(s) for inactive entities.", purged);

        var entities = await db.Entities.AsNoTracking()
            .Where(e => e.IsActive)
            .Select(e => new { e.Id, e.Name, e.EntityType, e.Description })
            .ToListAsync(ct);

        // 1) Compute the source text + hash for every entity so we can drift-skip
        //    in bulk. Prep work is fast (in-memory).
        var prepped = entities.Select(e =>
        {
            var fake = new Entity { Id = e.Id, Name = e.Name, EntityType = e.EntityType, Description = e.Description };
            var text = BuildSourceText(fake);
            return (e.Id, Text: text, Hash: Hash(text));
        }).Where(p => !string.IsNullOrWhiteSpace(p.Text)).ToList();

        // 2) Pull existing hashes in one query so we can skip already-fresh rows.
        var existing = await db.EntityEmbeddings.AsNoTracking()
            .Select(x => new { x.EntityId, x.SourceHash })
            .ToDictionaryAsync(x => x.EntityId, x => x.SourceHash, ct);

        var toEmbed = prepped
            .Where(p => !(existing.TryGetValue(p.Id, out var h) && h.AsSpan().SequenceEqual(p.Hash)))
            .ToList();

        if (toEmbed.Count == 0)
        {
            progress?.Report((entities.Count, entities.Count));
            return 0;
        }

        // 3) Batch the API calls. OpenAI accepts up to 2048 inputs per request, but
        //    Gemini's OpenAI-compat endpoint hard-caps at 100 — 100 is the safe
        //    ceiling across both providers, and a single rate-limit/transient
        //    failure costs ≤100 entities.
        const int BatchSize = 100;
        int touched = 0;
        int processed = 0;
        for (int start = 0; start < toEmbed.Count; start += BatchSize)
        {
            if (ct.IsCancellationRequested) break;
            var slice = toEmbed.Skip(start).Take(BatchSize).ToList();
            var texts = slice.Select(s => s.Text).ToList();

            float[][] vectors;
            try { vectors = await EmbedBatchAsync(texts, ct); }
            catch (Exception ex)
            {
                log.LogWarning(ex, "Embedding batch failed at offset {Offset}", start);
                processed += slice.Count;
                progress?.Report((processed, toEmbed.Count));
                continue;
            }
            if (vectors.Length != slice.Count)
            {
                log.LogWarning("Embedding batch returned {Got} vectors for {Sent} inputs — skipping batch",
                    vectors.Length, slice.Count);
                processed += slice.Count;
                progress?.Report((processed, toEmbed.Count));
                continue;
            }

            // 4) Bulk-upsert this batch via raw SQL. EF can't bind VECTOR yet,
            //    and re-issuing one MERGE per row is fine at batch=128 (sub-second
            //    aggregate at this scale; SQL Server batches the round-trips
            //    when context is reused).
            await using var batchDb = await dbFactory.CreateDbContextAsync(ct);
            for (int i = 0; i < slice.Count; i++)
            {
                var (id, _, hash) = slice[i];
                var vector = vectors[i];
                if (vector.Length == 0) continue;
                await UpsertVectorRawAsync(batchDb, id, hash, vector, ct);
                touched++;
            }

            processed += slice.Count;
            progress?.Report((processed, toEmbed.Count));
        }
        return touched;
    }

    /// <summary>
    /// Send a batch of texts to OpenAI in a single request. Returns vectors in
    /// the input order. Empty array on full-batch failure (caller logs).
    /// </summary>
    private async Task<float[]> EmbedLocalAsync(string text, CancellationToken ct)
    {
        var url   = settings.LocalEmbeddingBaseUrl;
        var key   = settings.LocalEmbeddingApiKey;
        var model = settings.LocalEmbeddingModel;
        var http  = httpFactory.CreateClient(nameof(EmbeddingService) + "Local");
        http.Timeout = TimeSpan.FromSeconds(60);
        var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(new EmbeddingRequest(text, model)),
        };
        if (!string.IsNullOrWhiteSpace(key))
            req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", key);
        try
        {
            var resp = await http.SendAsync(req, ct);
            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync(ct);
                log.LogWarning("Local embedding {Code}: {Body}", (int)resp.StatusCode, Truncate(body, 400));
                return Array.Empty<float>();
            }
            var payload = await resp.Content.ReadFromJsonAsync<EmbeddingResponse>(cancellationToken: ct);
            return NormalizeVector(payload?.Data?.FirstOrDefault()?.Embedding ?? Array.Empty<float>());
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Local embedding call failed");
            return Array.Empty<float>();
        }
    }

    private async Task<float[][]> EmbedBatchLocalAsync(IReadOnlyList<string> texts, CancellationToken ct)
    {
        var url   = settings.LocalEmbeddingBaseUrl;
        var key   = settings.LocalEmbeddingApiKey;
        var model = settings.LocalEmbeddingModel;
        var http  = httpFactory.CreateClient(nameof(EmbeddingService) + "Local");
        http.Timeout = TimeSpan.FromSeconds(120);
        var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(new BatchEmbeddingRequest(texts.ToArray(), model)),
        };
        if (!string.IsNullOrWhiteSpace(key))
            req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", key);
        var resp = await http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(ct);
            log.LogWarning("Local batch embedding {Code}: {Body}", (int)resp.StatusCode, Truncate(body, 400));
            return Array.Empty<float[]>();
        }
        var payload = await resp.Content.ReadFromJsonAsync<EmbeddingResponse>(cancellationToken: ct);
        if (payload?.Data == null) return Array.Empty<float[]>();
        return payload.Data.OrderBy(d => d.Index).Select(d => d.Embedding).ToArray();
    }

    private async Task<float[][]> EmbedBatchAsync(IReadOnlyList<string> texts, CancellationToken ct)
    {
        if (texts.Count == 0) return Array.Empty<float[]>();
        if (!string.IsNullOrWhiteSpace(settings.LocalEmbeddingBaseUrl))
            return await EmbedBatchLocalAsync(texts, ct);
        var key = settings.OpenAiApiKey;
        if (string.IsNullOrWhiteSpace(key))
        {
            log.LogWarning("OpenAI API key missing — embedding batch skipped.");
            return Array.Empty<float[]>();
        }

        var http = httpFactory.CreateClient(nameof(EmbeddingService));
        http.BaseAddress ??= new Uri("https://api.openai.com/");
        http.Timeout = TimeSpan.FromSeconds(120);
        var req = new HttpRequestMessage(HttpMethod.Post, "v1/embeddings")
        {
            Content = JsonContent.Create(new BatchEmbeddingRequest(texts.ToArray(), Model)),
        };
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", key);

        var resp = await http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(ct);
            log.LogWarning("OpenAI batch embedding {Code}: {Body}", (int)resp.StatusCode, Truncate(body, 400));
            return Array.Empty<float[]>();
        }
        var payload = await resp.Content.ReadFromJsonAsync<EmbeddingResponse>(cancellationToken: ct);
        if (payload?.Data == null) return Array.Empty<float[]>();
        // Sort by index to guarantee input-order alignment (OpenAI returns
        // already-ordered, but the docs are explicit that callers shouldn't rely
        // on order — sorting is defensive).
        return payload.Data
            .OrderBy(d => d.Index)
            .Select(d => d.Embedding)
            .ToArray();
    }

    // ── HTTP plumbing ─────────────────────────────────────────────────────

    private async Task<float[]> EmbedAsync(string text, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(settings.LocalEmbeddingBaseUrl))
            return await EmbedLocalAsync(text, ct);
        var key = settings.OpenAiApiKey;
        if (string.IsNullOrWhiteSpace(key))
        {
            log.LogWarning("OpenAI API key missing — embedding skipped.");
            return Array.Empty<float>();
        }

        var http = httpFactory.CreateClient(nameof(EmbeddingService));
        http.BaseAddress ??= new Uri("https://api.openai.com/");
        http.Timeout = TimeSpan.FromSeconds(60);
        var req = new HttpRequestMessage(HttpMethod.Post, "v1/embeddings")
        {
            Content = JsonContent.Create(new EmbeddingRequest(text, Model)),
        };
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", key);

        try
        {
            var resp = await http.SendAsync(req, ct);
            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync(ct);
                log.LogWarning("OpenAI embedding {Code}: {Body}", (int)resp.StatusCode, Truncate(body, 400));
                return Array.Empty<float>();
            }
            var payload = await resp.Content.ReadFromJsonAsync<EmbeddingResponse>(cancellationToken: ct);
            return NormalizeVector(payload?.Data?.FirstOrDefault()?.Embedding ?? Array.Empty<float>());
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Embedding API call failed");
            return Array.Empty<float>();
        }
    }

    /// <summary>Effective model label for stored rows — the configured endpoint's
    /// model when one is set, else the OpenAI default.</summary>
    private string EffectiveModel =>
        !string.IsNullOrWhiteSpace(settings.LocalEmbeddingBaseUrl) && !string.IsNullOrWhiteSpace(settings.LocalEmbeddingModel)
            ? settings.LocalEmbeddingModel
            : Model;

    /// <summary>Fit a provider vector to the VECTOR(1536) schema. MRL-style models
    /// (gemini-embedding-001, text-embedding-3-*) remain valid under truncation +
    /// L2 renormalization; providers that ignore the "dimensions" request param get
    /// clamped here instead of failing the SQL insert. Undersized vectors are
    /// rejected (empty) — padding would fabricate signal.</summary>
    private float[] NormalizeVector(float[] v)
    {
        if (v.Length == 0) return v;
        if (v.Length < Dimensions)
        {
            log.LogWarning("Embedding vector {Len} < {Dim} — provider/model mismatch; discarding", v.Length, Dimensions);
            return Array.Empty<float>();
        }
        if (v.Length == Dimensions) return v;
        var t = new float[Dimensions];
        Array.Copy(v, t, Dimensions);
        double norm = 0; foreach (var x in t) norm += (double)x * x;
        norm = Math.Sqrt(norm);
        if (norm > 1e-9) for (var i = 0; i < t.Length; i++) t[i] = (float)(t[i] / norm);
        return t;
    }

    private static double CosineSimilarity(float[] a, float[] b)
    {
        double dot = 0, na = 0, nb = 0;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            na  += a[i] * a[i];
            nb  += b[i] * b[i];
        }
        if (na == 0 || nb == 0) return 0;
        return dot / (Math.Sqrt(na) * Math.Sqrt(nb));
    }

    private static string Truncate(string s, int n) => s.Length <= n ? s : s[..n] + "…";

    // ── DTOs ──────────────────────────────────────────────────────────────

    // "dimensions" pins the output width to the VECTOR(1536) schema. OpenAI honors
    // it (1536 is text-embedding-3-small's default anyway); Gemini's OpenAI-compat
    // endpoint needs it because gemini-embedding-001's native output is 3072.
    // NormalizeVector below is the defensive net for providers that ignore it.
    private sealed record EmbeddingRequest(
        [property: JsonPropertyName("input")] string Input,
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("dimensions")] int Dimensions = EmbeddingService.Dimensions);

    private sealed record BatchEmbeddingRequest(
        [property: JsonPropertyName("input")] string[] Input,
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("dimensions")] int Dimensions = EmbeddingService.Dimensions);

    private sealed record EmbeddingResponse(
        [property: JsonPropertyName("data")] List<EmbeddingDatum>? Data);

    private sealed record EmbeddingDatum(
        [property: JsonPropertyName("embedding")] float[] Embedding,
        [property: JsonPropertyName("index")] int Index);
}

public sealed record EmbeddingHit(Guid EntityId, string EntityName, string EntityType, double Similarity);

/// <summary>One hit from <see cref="EmbeddingService.FindSimilarProseAsync"/>.</summary>
public sealed record ProseEmbeddingHit(string ScopeKind, Guid ScopeId, double Similarity);
