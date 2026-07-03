using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Single C# code path for the canonical SQL seeds under
/// <c>v3/StreetSamurai.Core/Data/Sql/*.sql</c>. Replaces the old
/// "run sqlcmd against the file by hand" workflow — every seed now goes through
/// <see cref="RunAsync"/>, which:
/// <list type="bullet">
/// <item>resolves the file from the project's Data/Sql folder via
///   <see cref="IPathProvider"/>;</item>
/// <item>checks the <c>SeedRuns</c> audit table to skip already-applied
///   seeds (idempotent);</item>
/// <item>executes the raw SQL inside a transaction with the SET options the
///   seeds expect (<c>QUOTED_IDENTIFIER ON</c>, <c>ANSI_NULLS ON</c>);</item>
/// <item>writes a <c>SeedRuns</c> row on success so the next call is a no-op.</item>
/// </list>
/// CLI surface: <c>ss --seed &lt;name&gt;</c> (see <c>SeedCli</c>).
/// </summary>
public class SqlSeedService
{
    private readonly IDbContextFactory<StreetSamuraiDbContext> dbFactory;
    private readonly IPathProvider paths;

    public SqlSeedService(
        IDbContextFactory<StreetSamuraiDbContext> dbFactory,
        IPathProvider paths)
    {
        this.dbFactory = dbFactory;
        this.paths     = paths;
    }

    /// <summary>
    /// Catalogue of known seed names → relative-to-project SQL filenames.
    /// Add a new entry when introducing a new seed; the <c>SeedRuns</c> audit
    /// table keys on the seed name, so renaming an existing entry causes the
    /// seed to re-run.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> Seeds =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["ammunition_45_acp"]            = "insert_ammunition_45_acp.sql",
            ["weapon_sw_governor_2211"]      = "insert_weapon_sw_governor_2211.sql",
            ["weapon_sw_governor_2211_fks"]  = "fix_weapon_sw_governor_2211_fks.sql",
            ["performance_indexes_20260516"] = "add_performance_indexes_20260516.sql",
            ["episode_tables_20260518"]      = "create_episode_tables_20260518.sql",
            ["episodebeat_duration_20260518"] = "add_episodebeat_duration_20260518.sql",
            ["episode_export_paths_20260518"] = "add_episode_export_paths_20260518.sql",
            ["episode_slug_20260518"]         = "add_episode_slug_20260518.sql",
            ["episode_guid_id_20260518"]      = "rebuild_episode_tables_as_guid_20260518.sql",
            ["episode_resume_columns_20260518"] = "add_episode_resume_columns_20260518.sql",
            ["episode_book_chapter_20260518"]   = "add_episode_book_chapter_20260518.sql",
            ["episodebeat_sortkey_20260518"]    = "add_episodebeat_sortkey_20260518.sql",
            ["beat_narrative_metadata_20260518"] = "add_beat_narrative_metadata_20260518.sql",
            ["chapter_beats_table_20260518"]    = "create_chapter_beats_table_20260518.sql",
            ["extend_chapter_beats_for_recording_20260518"] = "extend_chapter_beats_for_recording_20260518.sql",
            ["episodebeat_hash_stale_20260518"] = "add_episodebeat_hash_stale_20260518.sql",
            ["node_schema_20260518"]          = "create_node_schema_20260518.sql",
            ["beat_number_20260522"]            = "add_beat_number_20260522.sql",
            ["gaps_table_20260522"]             = "add_gaps_table_20260522.sql",
        };

    public class SeedResult
    {
        public string Name        { get; set; } = "";
        public bool   AlreadyRan  { get; set; }
        public bool   Success     { get; set; }
        public string Message     { get; set; } = "";
    }

    public async Task<SeedResult> RunAsync(string name, bool force = false, CancellationToken ct = default)
    {
        var result = new SeedResult { Name = name };

        if (!Seeds.TryGetValue(name, out var fileName))
        {
            result.Message = $"Unknown seed '{name}'. Known: {string.Join(", ", Seeds.Keys)}";
            return result;
        }

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        await EnsureSeedRunsTableAsync(db, ct);

        if (!force && await HasRunAsync(db, name, ct))
        {
            result.AlreadyRan = true;
            result.Success    = true;
            result.Message    = $"Seed '{name}' already applied — skipping (pass --force to re-run).";
            return result;
        }

        var sqlPath = ResolveSqlPath(fileName);
        if (!File.Exists(sqlPath))
        {
            result.Message = $"Seed file not found: {sqlPath}";
            return result;
        }

        var script = await File.ReadAllTextAsync(sqlPath, ct);
        // Strip GO batch separators — ExecuteSqlRawAsync runs everything as one
        // batch. Existing seeds use GO at end-of-file for sqlcmd; safe to remove.
        script = StripGo(script);
        var prelude = "SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON; SET XACT_ABORT ON;\n";

        try
        {
            await db.Database.ExecuteSqlRawAsync(prelude + script, ct);
            await RecordRunAsync(db, name, ct);
            result.Success = true;
            result.Message = $"Seed '{name}' applied.";
        }
        catch (Exception ex)
        {
            result.Message = $"Seed '{name}' failed: {ex.Message}";
        }
        return result;
    }

    private string ResolveSqlPath(string fileName)
    {
        // Seed .sql files live in the source tree at
        // <repo>/v3/StreetSamurai.Core/Data/Sql/. The runtime entry point
        // varies — Blazor host, Core test runner, one-shot CLI — so try a
        // handful of candidate roots. Each candidate is a directory we hope
        // ends in (…/StreetSamurai.Core), to which we append Data/Sql/X.
        var assemblyDir = Path.GetDirectoryName(typeof(SqlSeedService).Assembly.Location) ?? "";
        var candidateRoots = new List<string>();

        // 1. Walk up from the assembly. From any v3/<ProjectName>/bin/.../
        //    going up 3 finds <ProjectName>. From there step sideways into
        //    ../StreetSamurai.Core. Also try walking up further (some host
        //    layouts publish to bin/Debug/net10.0/win-x64/publish/).
        var dir = assemblyDir;
        for (int up = 0; up < 6 && !string.IsNullOrEmpty(dir); up++)
        {
            candidateRoots.Add(dir); // case: dll is already inside Core/
            candidateRoots.Add(Path.Combine(dir, "..", "StreetSamurai.Core"));
            candidateRoots.Add(Path.Combine(dir, "StreetSamurai.Core"));
            dir = Path.GetDirectoryName(dir) ?? "";
        }

        // 2. Source-tree fall-back relative to current working dir, so a
        //    "dotnet run --project v3/X" from the repo root still resolves.
        var cwd = Directory.GetCurrentDirectory();
        candidateRoots.Add(Path.Combine(cwd, "v3", "StreetSamurai.Core"));
        candidateRoots.Add(Path.Combine(cwd, "StreetSamurai.Core"));
        candidateRoots.Add(cwd);

        foreach (var root in candidateRoots)
        {
            var probe = Path.GetFullPath(Path.Combine(root, "Data", "Sql", fileName));
            if (File.Exists(probe)) return probe;
        }
        // Return the first candidate so the error message points somewhere
        // useful when the file truly is missing.
        return Path.GetFullPath(Path.Combine(candidateRoots[0], "Data", "Sql", fileName));
    }

    private static string StripGo(string sql)
    {
        var lines = sql.Split('\n');
        var keep = lines.Where(l => !l.Trim().Equals("GO", StringComparison.OrdinalIgnoreCase));
        return string.Join('\n', keep);
    }

    // ── SeedRuns audit table ────────────────────────────────────────────────

    private static async Task EnsureSeedRunsTableAsync(StreetSamuraiDbContext db, CancellationToken ct)
    {
        await db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'[dbo].[SeedRuns]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[SeedRuns] (
                    [Name]    NVARCHAR(200) NOT NULL PRIMARY KEY,
                    [RanAt]   DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME()
                );
            END;
            """, ct);
    }

    private static async Task<bool> HasRunAsync(StreetSamuraiDbContext db, string name, CancellationToken ct)
    {
        var rows = await db.Database
            .SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM [dbo].[SeedRuns] WHERE [Name] = {0};", name)
            .ToListAsync(ct);
        return rows.FirstOrDefault() > 0;
    }

    private static async Task RecordRunAsync(StreetSamuraiDbContext db, string name, CancellationToken ct)
    {
        await db.Database.ExecuteSqlRawAsync(
            "MERGE [dbo].[SeedRuns] AS t USING (SELECT {0} AS [Name]) AS s ON t.[Name] = s.[Name] " +
            "WHEN MATCHED THEN UPDATE SET RanAt = SYSUTCDATETIME() " +
            "WHEN NOT MATCHED THEN INSERT ([Name], [RanAt]) VALUES (s.[Name], SYSUTCDATETIME());",
            ct, name);
    }
}
