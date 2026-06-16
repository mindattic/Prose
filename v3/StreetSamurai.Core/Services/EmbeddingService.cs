using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;

namespace StreetSamurai.Core.Services;

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

    /// <summary>ProseEmbeddings ScopeKind for a strand beat (Beat.Id keyed). Distinct
    /// from 'beat' (which keys ChapterBeat.BeatGuid) so the two content models
    /// never collide in the polymorphic prose table.</summary>
    private const string ScopeStrandBeat = "strandbeat";

    private readonly IDbContextFactory<StreetSamuraiDbContext> dbFactory;
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
        IDbContextFactory<StreetSamuraiDbContext> dbFactory,
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

    private static async Task UpsertProseVectorRawAsync(
        StreetSamuraiDbContext db, string scopeKind, Guid scopeId, byte[] hash, float[] vector, CancellationToken ct)
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
            new Microsoft.Data.SqlClient.SqlParameter("@p_model", Model));
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
            new("@p_universe", QueryUniverseId()),
        };
        var scopeFilter = "";
        if (!string.IsNullOrWhiteSpace(scopeKind))
        {
            scopeFilter = " WHERE ScopeKind = @p_kind";
            parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@p_kind", scopeKind));
        }

        // Universe scope is always applied; the optional ScopeKind filter is ANDed onto it.
        var universeClause = scopeFilter.Length > 0
            ? scopeFilter + " AND (@p_universe = '00000000-0000-0000-0000-000000000000' OR UniverseId = @p_universe)"
            : " WHERE (@p_universe = '00000000-0000-0000-0000-000000000000' OR UniverseId = @p_universe)";
        var sql = $"""
            SELECT TOP (@p_k)
                ScopeKind, ScopeId,
                1.0 - VECTOR_DISTANCE('cosine', Vector, CAST(@p_query AS VECTOR(1536))) AS Similarity
            FROM dbo.ProseEmbeddings{universeClause}
            ORDER BY VECTOR_DISTANCE('cosine', Vector, CAST(@p_query AS VECTOR(1536))) ASC;
            """;

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var rows = await db.Database.SqlQueryRaw<ProseEmbeddingRow>(sql, parameters.ToArray<object>())
            .ToListAsync(ct);
        return rows
            .Select(r => new ProseEmbeddingHit(r.ScopeKind ?? "", r.ScopeId, r.Similarity))
            .ToList();
    }

    // ── Strand-beat prose (the live writer/strand model) ──────────────────

    /// <summary>
    /// Embed the enabled beats of one strand into <c>ProseEmbeddings</c> under
    /// the <see cref="ScopeStrandBeat"/> scope (keyed on <c>Beat.Id</c>). This
    /// is the live strand/Beats model — distinct from <see cref="ReembedProseCorpusAsync"/>,
    /// which embeds the older Chapter/ChapterBeat model. Drift-skipped; returns
    /// the count newly (re)embedded. Cheap: a novella is a few cents.
    /// </summary>
    public async Task<int> ReembedStrandBeatsAsync(Guid strandId, CancellationToken ct = default)
    {
        await EnsureSchemaOnceAsync(ct);
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var beats = await (from sb in db.StrandBeats.AsNoTracking()
                           join b in db.Beats.AsNoTracking() on sb.BeatId equals b.Id
                           where sb.StrandId == strandId && sb.IsEnabled
                           orderby sb.SortKey
                           select new { b.Id, b.BeatTitle, b.Synopsis, b.Text }).ToListAsync(ct);
        if (beats.Count == 0) return 0;

        var prepped = beats
            .Select(b => { var t = BuildBeatSourceText(b.BeatTitle, b.Synopsis, b.Text); return (b.Id, Text: t, Hash: Hash(t)); })
            .Where(p => !string.IsNullOrWhiteSpace(p.Text))
            .ToList();

        var existing = await db.ProseEmbeddings.AsNoTracking()
            .Where(x => x.ScopeKind == ScopeStrandBeat)
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
            catch (Exception ex) { log.LogWarning(ex, "Strand-beat embed batch failed at offset {Offset}", start); continue; }
            if (vectors.Length != slice.Count) { log.LogWarning("Strand-beat batch returned {Got}/{Sent}", vectors.Length, slice.Count); continue; }

            await using var batchDb = await dbFactory.CreateDbContextAsync(ct);
            for (int i = 0; i < slice.Count; i++)
            {
                var v = vectors[i];
                if (v.Length == 0) continue;
                await UpsertProseVectorRawAsync(batchDb, ScopeStrandBeat, slice[i].Id, slice[i].Hash, v, ct);
                touched++;
            }
        }
        return touched;
    }

    /// <summary>
    /// Top-<paramref name="k"/> strand beats most similar to <paramref name="queryText"/>,
    /// optionally restricted to a single strand. Only enabled beats are searched
    /// (the StrandBeats join filters soft-deletes). Returns hits keyed on Beat.Id.
    /// </summary>
    public async Task<IReadOnlyList<ProseEmbeddingHit>> FindSimilarStrandBeatsAsync(
        string queryText, int k = 6, Guid? strandScope = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(queryText)) return Array.Empty<ProseEmbeddingHit>();
        await EnsureSchemaOnceAsync(ct);

        var queryVector = await EmbedAsync(TruncateForEmbed(queryText), ct);
        if (queryVector.Length == 0) return Array.Empty<ProseEmbeddingHit>();
        var queryJson = JsonSerializer.Serialize(queryVector);

        // Pull 2x then dedupe in C# — a beat can live in more than one strand,
        // so the StrandBeats join can surface the same Beat.Id twice.
        var parameters = new List<Microsoft.Data.SqlClient.SqlParameter>
        {
            new("@p_k", Math.Max(1, k) * 2),
            new("@p_query", queryJson),
            new("@p_universe", QueryUniverseId()),
        };
        var scopeFilter = "";
        if (strandScope is Guid sid)
        {
            scopeFilter = " AND sb.StrandId = @p_strand";
            parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@p_strand", sid));
        }

        var sql = $"""
            SELECT TOP (@p_k)
                pe.ScopeId AS ScopeId,
                1.0 - VECTOR_DISTANCE('cosine', pe.Vector, CAST(@p_query AS VECTOR(1536))) AS Similarity
            FROM dbo.ProseEmbeddings pe
            JOIN dbo.StrandBeats sb ON sb.BeatId = pe.ScopeId AND sb.IsEnabled = 1
            WHERE pe.ScopeKind = '{ScopeStrandBeat}'
              AND (@p_universe = '00000000-0000-0000-0000-000000000000' OR pe.UniverseId = @p_universe){scopeFilter}
            ORDER BY VECTOR_DISTANCE('cosine', pe.Vector, CAST(@p_query AS VECTOR(1536))) ASC;
            """;

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var rows = await db.Database.SqlQueryRaw<ProseScopeRow>(sql, parameters.ToArray<object>())
            .ToListAsync(ct);
        return rows
            .GroupBy(r => r.ScopeId).Select(g => g.First())
            .Take(Math.Max(1, k))
            .Select(r => new ProseEmbeddingHit(ScopeStrandBeat, r.ScopeId, r.Similarity))
            .ToList();
    }

    /// <summary>Row shape for the strand-beat VECTOR_DISTANCE query.</summary>
    private sealed class ProseScopeRow
    {
        public Guid   ScopeId    { get; set; }
        public double Similarity { get; set; }
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

    private static async Task UpsertVectorRawAsync(
        StreetSamuraiDbContext db, Guid entityId, byte[] hash, float[] vector, CancellationToken ct)
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
            new Microsoft.Data.SqlClient.SqlParameter("@p_model", Model));
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

        var queryVector = await EmbedAsync(queryText, ct);
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
              AND (@p_universe = '00000000-0000-0000-0000-000000000000' OR emb.UniverseId = @p_universe){typeFilter}
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

        // 3) Batch the API calls. OpenAI text-embedding-3-small accepts up to 2048
        //    inputs per request; per-input cap is 8191 tokens. We chunk at 128 to
        //    keep total wire time bounded and parallelism manageable, and so a
        //    single rate-limit/transient failure costs ≤128 entities.
        const int BatchSize = 128;
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
    private async Task<float[][]> EmbedBatchAsync(IReadOnlyList<string> texts, CancellationToken ct)
    {
        if (texts.Count == 0) return Array.Empty<float[]>();
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
            return payload?.Data?.FirstOrDefault()?.Embedding ?? Array.Empty<float>();
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Embedding API call failed");
            return Array.Empty<float>();
        }
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

    private sealed record EmbeddingRequest(
        [property: JsonPropertyName("input")] string Input,
        [property: JsonPropertyName("model")] string Model);

    private sealed record BatchEmbeddingRequest(
        [property: JsonPropertyName("input")] string[] Input,
        [property: JsonPropertyName("model")] string Model);

    private sealed record EmbeddingResponse(
        [property: JsonPropertyName("data")] List<EmbeddingDatum>? Data);

    private sealed record EmbeddingDatum(
        [property: JsonPropertyName("embedding")] float[] Embedding,
        [property: JsonPropertyName("index")] int Index);
}

public sealed record EmbeddingHit(Guid EntityId, string EntityName, string EntityType, double Similarity);

/// <summary>One hit from <see cref="EmbeddingService.FindSimilarProseAsync"/>.</summary>
public sealed record ProseEmbeddingHit(string ScopeKind, Guid ScopeId, double Similarity);
