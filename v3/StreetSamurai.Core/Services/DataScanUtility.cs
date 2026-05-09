using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Base for batch maintenance utilities that mutate canon entity records.
/// Originally walked <c>engine/data/{folder}/*.json</c>; after the JSON
/// archival sweep the source of truth is SQL — this base now scans
/// <c>Records.Json</c> blobs and writes mutations back to the same row.
///
/// The <c>processFile</c> callback signature is preserved so the six
/// subclasses (CrossReferenceService, AssignTiersService,
/// TagWeaponLethalityService, TagNormalizerService, FixIdentityCorruptionService,
/// FixPhiService) continue to compile unchanged. The "path" passed to the
/// callback is a synthetic <c>{folderEquiv}/{entityId}.json</c> string so any
/// folder-name extraction (e.g. <c>Path.GetFileName(Path.GetDirectoryName(file))</c>)
/// returns the same value it always did.
/// </summary>
public abstract class DataScanUtility
{
    private readonly IDbContextFactory<StreetSamuraiDbContext> dbFactory;
    protected static readonly JsonSerializerOptions WriteOptions = new() { WriteIndented = false };

    // Mirror of EfRepository.RepoNameMap (EntityType → legacy folder name).
    // Used to synthesize the path string passed to the processFile callback
    // so subclasses' folder-based heuristics continue to work.
    private static readonly Dictionary<string, string> EntityTypeToFolder =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["character"]      = "people",
            ["place"]          = "places",
            ["faction"]        = "factions",
            ["corponation"]    = "corponations",
            ["subsidiary"]     = "subsidiaries",
            ["synthetic"]      = "synthetics",
            ["automaton"]      = "automata",
            ["weapon"]         = "weaponry",
            ["equipment"]      = "equipment",
            ["cyberware"]      = "cyberware",
            ["apparel"]        = "apparel",
            ["ammunition"]     = "ammunition",
            ["pharmaceutical"] = "pharmaceuticals",
            ["genemod"]        = "genemods",
            ["material"]       = "materials",
            ["transportation"] = "transportation",
            ["consumer_good"]  = "consumer_goods",
            ["archetype"]      = "archetypes",
            ["quote"]          = "quotes",
            ["news"]           = "news",
            ["contract"]       = "contracts",
            ["document"]       = "documents",
            ["lab_specimen"]   = "lab_specimens",
            ["psionic"]        = "psionics",
            ["technology"]     = "technology",
            ["entertainment"]  = "entertainment",
            ["flyover_entity"] = "flyover_entities",
            ["creature"]       = "creatures",
        };

    private static readonly Dictionary<string, string> FolderToEntityType =
        EntityTypeToFolder.ToDictionary(kv => kv.Value, kv => kv.Key, StringComparer.OrdinalIgnoreCase);

    protected DataScanUtility(IDbContextFactory<StreetSamuraiDbContext> dbFactory)
    {
        this.dbFactory = dbFactory;
    }

    /// <summary>
    /// Scan entity records in parallel. <paramref name="processFile"/>
    /// receives a synthetic <c>{folder}/{entityId}.json</c> path and the
    /// parsed <see cref="Records.Json"/> blob; mutate the JsonObject in
    /// place and return the change count (0 = no write).
    /// </summary>
    protected async Task<UtilityResult> RunScanAsync(
        Guid[] entityIds,
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
            await Parallel.ForEachAsync(entityIds,
                new ParallelOptions { MaxDegreeOfParallelism = parallelism, CancellationToken = limitCts.Token },
                async (entityId, token) =>
                {
                    try
                    {
                        await using var db = await dbFactory.CreateDbContextAsync(token);
                        var row = await db.Records.Include(r => r.Entity)
                            .FirstOrDefaultAsync(r => r.EntityId == entityId, token);
                        if (row == null || string.IsNullOrEmpty(row.Json)) return;
                        var entityType = row.Entity?.EntityType ?? "";
                        var folder     = EntityTypeToFolder.TryGetValue(entityType, out var f) ? f : entityType;
                        var fakePath   = $"{folder}/{entityId:N}.json";

                        if (JsonNode.Parse(row.Json) is not JsonObject obj) return;

                        int fileChanges;
                        try { fileChanges = processFile(fakePath, obj); }
                        catch (Exception ex) { warnings.Add($"{entityId:N}: {ex.Message}"); return; }

                        int done = Interlocked.Increment(ref scanned);
                        if (fileChanges > 0)
                        {
                            row.Json      = obj.ToJsonString(WriteOptions);
                            row.UpdatedAt = DateTime.UtcNow;
                            if (row.Entity != null) row.Entity.ModifiedAt = row.UpdatedAt;
                            await db.SaveChangesAsync(token);
                            int mod = Interlocked.Increment(ref modified);
                            Interlocked.Add(ref changes, fileChanges);
                            if (limit.HasValue && mod >= limit.Value)
                                limitCts.Cancel();
                        }
                        progress?.Report(new UtilityProgress(done, entityIds.Length, modified, changes));
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception ex) { warnings.Add($"{entityId:N}: {ex.Message}"); }
                });
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { }

        return new UtilityResult(scanned, modified, changes, warnings.IsEmpty ? null : [.. warnings]);
    }

    /// <summary>
    /// Build the EntityId list to scan. <paramref name="repos"/> is the same
    /// folder-name list the legacy file-walker accepted (e.g.
    /// <c>["people", "synthetics"]</c>); each is translated to its canonical
    /// <see cref="Data.Entities.Entity.EntityType"/> and the matching active
    /// rows are returned.
    /// </summary>
    protected Guid[] GetFiles(string[]? repos = null)
    {
        using var db = dbFactory.CreateDbContext();

        if (repos is { Length: > 0 })
        {
            var types = repos
                .Select(r => FolderToEntityType.TryGetValue(r, out var t) ? t : r)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            return db.Entities.AsNoTracking()
                .Where(e => e.IsActive && types.Contains(e.EntityType))
                .Select(e => e.Id)
                .ToArray();
        }

        // No filter: every active entity that has a Records.Json blob.
        return db.Entities.AsNoTracking()
            .Where(e => e.IsActive)
            .Select(e => e.Id)
            .ToArray();
    }

    protected static string? GetStr(JsonObject obj, string key) =>
        obj[key]?.GetValueKind() == System.Text.Json.JsonValueKind.String
            ? obj[key]!.GetValue<string>() : null;

    protected static string CombineText(JsonObject obj, params string[] keys) =>
        string.Join(" ", keys.Select(k => GetStr(obj, k) ?? "").Where(s => s.Length > 0));
}
