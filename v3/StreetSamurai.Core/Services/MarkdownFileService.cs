using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

// ── MarkdownFileService ────────────────────────────────────────────────────
// Backs every project-rule, Codex doc, and Claude Code memory file into the
// SQL Server database so they can survive disk loss and be recovered by
// timestamp from the MarkdownFiles_History temporal table.
//
// FileRoot → base directory mapping:
//   "project"               → IPathProvider.DataRoot
//   "claude-user"           → ~/.claude
//   "claude-project-memory" → ~/.claude/projects/{projectSlug}/memory
// ──────────────────────────────────────────────────────────────────────────

public class MarkdownFileService
{
    public record DiscoveredFile(string FilePath, string FileRoot, string RelativePath, string Category);
    public record SyncResult(int Inserted, int Updated, int Unchanged, List<string> Errors);
    public record RestoreResult(int Written, int Skipped, List<string> Errors);

    private readonly IDbContextFactory<StreetSamuraiDbContext> dbFactory;
    private readonly IPathProvider paths;

    public MarkdownFileService(IDbContextFactory<StreetSamuraiDbContext> dbFactory, IPathProvider paths)
    {
        this.dbFactory = dbFactory;
        this.paths     = paths;
    }

    // ── Discovery ─────────────────────────────────────────────────────────

    public IEnumerable<DiscoveredFile> DiscoverFiles()
    {
        var projectRoot  = paths.DataRoot;
        var userHome     = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var claudeUser   = Path.Combine(userHome, ".claude");
        var projectSlug  = DeriveCloudeProjectSlug(projectRoot);
        var memoryRoot   = Path.Combine(claudeUser, "projects", projectSlug, "memory");

        // Project-level CLAUDE.md
        var projectRule = Path.Combine(projectRoot, "CLAUDE.md");
        if (File.Exists(projectRule))
            yield return new(projectRule, "project", "CLAUDE.md", "project-rule");

        // Global ~/.claude/CLAUDE.md
        var globalRule = Path.Combine(claudeUser, "CLAUDE.md");
        if (File.Exists(globalRule))
            yield return new(globalRule, "claude-user", "CLAUDE.md", "project-rule-global");

        // Codex docs: docs/*.md
        var docsDir = Path.Combine(projectRoot, "docs");
        if (Directory.Exists(docsDir))
        {
            foreach (var f in Directory.EnumerateFiles(docsDir, "*.md", SearchOption.TopDirectoryOnly))
                yield return new(f, "project", ToRelative(projectRoot, f), "codex");

            // docs/registers/*.md
            var registersDir = Path.Combine(docsDir, "registers");
            if (Directory.Exists(registersDir))
                foreach (var f in Directory.EnumerateFiles(registersDir, "*.md"))
                    yield return new(f, "project", ToRelative(projectRoot, f), "register");

            // docs/rfc/*.md
            var rfcDir = Path.Combine(docsDir, "rfc");
            if (Directory.Exists(rfcDir))
                foreach (var f in Directory.EnumerateFiles(rfcDir, "*.md"))
                    yield return new(f, "project", ToRelative(projectRoot, f), "rfc");

            // docs/strands/*.md — per-strand bibles
            var strandsDir = Path.Combine(docsDir, "strands");
            if (Directory.Exists(strandsDir))
                foreach (var f in Directory.EnumerateFiles(strandsDir, "*.md"))
                    yield return new(f, "project", ToRelative(projectRoot, f), "strand-bible");

            // docs/books/*.md — legacy long-form book spines
            var booksDir = Path.Combine(docsDir, "books");
            if (Directory.Exists(booksDir))
                foreach (var f in Directory.EnumerateFiles(booksDir, "*.md"))
                    yield return new(f, "project", ToRelative(projectRoot, f), "book-spine");
        }

        // Claude Code project memory files
        if (Directory.Exists(memoryRoot))
        {
            foreach (var f in Directory.EnumerateFiles(memoryRoot, "*.md", SearchOption.TopDirectoryOnly))
            {
                var fname = Path.GetFileName(f);
                var cat   = fname.Equals("MEMORY.md", StringComparison.OrdinalIgnoreCase)
                    ? "memory-index"
                    : "memory";
                yield return new(f, "claude-project-memory", fname, cat);
            }
        }
    }

    // ── Sync: disk → DB ───────────────────────────────────────────────────

    public async Task<SyncResult> SyncAllAsync(bool dryRun = false, CancellationToken ct = default)
    {
        var files  = DiscoverFiles().ToList();
        var errors = new List<string>();
        int inserted = 0, updated = 0, unchanged = 0;

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        foreach (var f in files)
        {
            try
            {
                if (!File.Exists(f.FilePath)) continue;
                var content = await File.ReadAllTextAsync(f.FilePath, ct);
                var hash    = ComputeHash(content);

                var existing = await db.MarkdownFiles
                    .FirstOrDefaultAsync(x => x.RelativePath == f.RelativePath, ct);

                if (existing == null)
                {
                    if (!dryRun)
                    {
                        db.MarkdownFiles.Add(new MarkdownFile
                        {
                            Id           = Guid.NewGuid(),
                            FilePath     = f.FilePath,
                            FileRoot     = f.FileRoot,
                            RelativePath = f.RelativePath,
                            FileName     = Path.GetFileName(f.FilePath),
                            Category     = f.Category,
                            Content      = content,
                            ContentHash  = hash,
                            LastSyncedAt = DateTime.UtcNow,
                            SyncedBy     = "cli",
                        });
                        await db.SaveChangesAsync(ct);
                    }
                    inserted++;
                }
                else if (existing.ContentHash != hash)
                {
                    if (!dryRun)
                    {
                        existing.FilePath     = f.FilePath;
                        existing.Content      = content;
                        existing.ContentHash  = hash;
                        existing.LastSyncedAt = DateTime.UtcNow;
                        existing.SyncedBy     = "cli";
                        await db.SaveChangesAsync(ct);
                    }
                    updated++;
                }
                else
                {
                    unchanged++;
                }
            }
            catch (Exception ex)
            {
                errors.Add($"{f.RelativePath}: {ex.Message}");
            }
        }

        return new(inserted, updated, unchanged, errors);
    }

    // ── List ──────────────────────────────────────────────────────────────

    public async Task<List<MarkdownFile>> ListAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.MarkdownFiles
            .OrderBy(x => x.FileRoot)
            .ThenBy(x => x.RelativePath)
            .ToListAsync(ct);
    }

    // ── Get (with optional point-in-time) ─────────────────────────────────

    public async Task<MarkdownFile?> GetAsync(string relativePath, DateTime? asOf = null, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        if (asOf.HasValue && db.Database.IsSqlServer())
        {
            var ts = asOf.Value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffffff");
            return await db.MarkdownFiles
                .FromSqlRaw(
                    $"SELECT Id, FilePath, FileRoot, RelativePath, FileName, Category, Content, ContentHash, LastSyncedAt, SyncedBy " +
                    $"FROM MarkdownFiles FOR SYSTEM_TIME AS OF '{ts}' " +
                    $"WHERE RelativePath = {{0}}",
                    relativePath)
                .FirstOrDefaultAsync(ct);
        }

        return await db.MarkdownFiles
            .FirstOrDefaultAsync(x => x.RelativePath == relativePath, ct);
    }

    // ── Restore: DB → disk ────────────────────────────────────────────────

    public async Task<RestoreResult> RestoreAsync(
        string?  relativePath = null,
        DateTime? asOf        = null,
        bool     dryRun       = false,
        CancellationToken ct  = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        List<MarkdownFile> rows;
        if (relativePath != null)
        {
            var row = await GetAsync(relativePath, asOf, ct);
            rows = row != null ? new List<MarkdownFile> { row } : new List<MarkdownFile>();
        }
        else if (asOf.HasValue && db.Database.IsSqlServer())
        {
            var ts = asOf.Value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffffff");
            rows = await db.MarkdownFiles
                .FromSqlRaw(
                    $"SELECT Id, FilePath, FileRoot, RelativePath, FileName, Category, Content, ContentHash, LastSyncedAt, SyncedBy " +
                    $"FROM MarkdownFiles FOR SYSTEM_TIME AS OF '{ts}'")
                .ToListAsync(ct);
        }
        else
        {
            rows = await db.MarkdownFiles.ToListAsync(ct);
        }

        var errors  = new List<string>();
        int written = 0, skipped = 0;

        foreach (var row in rows)
        {
            try
            {
                var destPath = ResolveAbsolutePath(row.FileRoot, row.RelativePath);
                if (destPath == null)
                {
                    errors.Add($"Cannot resolve path for root='{row.FileRoot}' rel='{row.RelativePath}'");
                    continue;
                }

                var dir = Path.GetDirectoryName(destPath)!;
                if (!dryRun)
                {
                    Directory.CreateDirectory(dir);
                    await File.WriteAllTextAsync(destPath, row.Content, ct);
                }
                written++;
            }
            catch (Exception ex)
            {
                errors.Add($"{row.RelativePath}: {ex.Message}");
                skipped++;
            }
        }

        return new(written, skipped, errors);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    public string? ResolveAbsolutePath(string fileRoot, string relativePath)
    {
        var userHome   = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var claudeUser = Path.Combine(userHome, ".claude");
        var slug       = DeriveCloudeProjectSlug(paths.DataRoot);

        var baseDir = fileRoot switch
        {
            "project"               => paths.DataRoot,
            "claude-user"           => claudeUser,
            "claude-project-memory" => Path.Combine(claudeUser, "projects", slug, "memory"),
            _                       => null,
        };

        if (baseDir == null) return null;
        return Path.Combine(baseDir, relativePath.Replace('/', Path.DirectorySeparatorChar));
    }

    private static string DeriveCloudeProjectSlug(string projectRoot)
        => projectRoot.Replace(":", "-").Replace("\\", "-").Replace("/", "-");

    private static string ToRelative(string root, string absolute)
        => Path.GetRelativePath(root, absolute).Replace('\\', '/');

    private static string ComputeHash(string content)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
