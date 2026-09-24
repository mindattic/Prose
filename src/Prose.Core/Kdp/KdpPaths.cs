using System.Collections.Concurrent;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Prose.Core.Kdp;

/// <summary>Where the KDP store and its legacy JSON sources live.</summary>
public static class KdpPaths
{
    /// <summary>Full path of the SQLite file to use instead of the default — tests point it at a
    /// temp file.</summary>
    public const string DbPathEnvVar = "PROSE_KDP_DB";

    /// <summary>Folder holding title-ids.json, category-tree-*.json and logs/ — defaults to the
    /// repo's <c>tools/kdp</c>.</summary>
    public const string ToolsDirEnvVar = "PROSE_KDP_TOOLS_DIR";

    /// <summary>%LocalAppData%\MindAttic\Prose\kdp.db — machine-local, beside Settings.json. Not in
    /// the repo (it is live state, not source) and not in C:\Apps (a build output that can be
    /// wiped by a redeploy).</summary>
    public static string DefaultDbPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MindAttic", "Prose", "kdp.db");

    public static string ResolveDbPath() =>
        Environment.GetEnvironmentVariable(DbPathEnvVar) is { Length: > 0 } p ? p : DefaultDbPath;

    public static string ConnectionString(string dbPath) =>
        new SqliteConnectionStringBuilder { DataSource = dbPath, DefaultTimeout = 30 }.ToString();

    /// <summary>The repo's <c>tools/kdp</c> folder (override with <see cref="ToolsDirEnvVar"/>).</summary>
    public static string ResolveToolsDir() =>
        Environment.GetEnvironmentVariable(ToolsDirEnvVar) is { Length: > 0 } p
            ? p
            : Path.Combine(Services.KdpManifestService.FindRepoRoot(), "tools", "kdp");
}

/// <summary>Brings a KDP database up to the current schema, once per process per database.</summary>
public static class KdpMigrator
{
    private static readonly ConcurrentDictionary<string, Lazy<Task>> Done = new();

    public static Task EnsureMigratedAsync(IDbContextFactory<KdpDbContext> factory, CancellationToken ct = default)
    {
        string key;
        using (var probe = factory.CreateDbContext())
            key = probe.Database.GetConnectionString() ?? "";

        // In-memory test databases share a connection string but not a database — never cache them.
        if (key.Contains(":memory:", StringComparison.OrdinalIgnoreCase) || key.Length == 0)
            return MigrateAsync(factory, ct);

        var lazy = Done.GetOrAdd(key, _ => new Lazy<Task>(() => MigrateAsync(factory, CancellationToken.None)));
        var task = lazy.Value;
        // A failed attempt must not poison the process: drop it so the next call retries.
        if (task.IsFaulted || task.IsCanceled) Done.TryRemove(key, out _);
        return task;
    }

    private static async Task MigrateAsync(IDbContextFactory<KdpDbContext> factory, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        if (db.Database.GetConnectionString() is { } cs
            && new SqliteConnectionStringBuilder(cs).DataSource is { Length: > 0 } file
            && !file.Contains(":memory:", StringComparison.OrdinalIgnoreCase)
            && Path.GetDirectoryName(Path.GetFullPath(file)) is { } dir)
            Directory.CreateDirectory(dir);

        await db.Database.MigrateAsync(ct);
        // WAL lets KdpPublish and the Hub (which runs the prose --kdp-* commands) read while the
        // other writes. Persistent: set once, stays on the file.
        await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;", ct);
    }
}

/// <summary>Lets <c>dotnet ef migrations add --context KdpDbContext</c> build the context without a host.</summary>
internal sealed class KdpDesignTimeDbFactory : IDesignTimeDbContextFactory<KdpDbContext>
{
    public KdpDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<KdpDbContext>().UseSqlite("Data Source=kdp-design-time.db").Options);
}
