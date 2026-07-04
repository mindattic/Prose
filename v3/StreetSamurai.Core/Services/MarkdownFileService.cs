using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
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

            // docs/strands/*.md — per-node bibles
            var nodesDir = Path.Combine(docsDir, "nodes");
            if (Directory.Exists(nodesDir))
                foreach (var f in Directory.EnumerateFiles(nodesDir, "*.md"))
                    yield return new(f, "project", ToRelative(projectRoot, f), "node-bible");

            // docs/books/*.md — legacy long-form book spines
            var booksDir = Path.Combine(docsDir, "books");
            if (Directory.Exists(booksDir))
                foreach (var f in Directory.EnumerateFiles(booksDir, "*.md"))
                    yield return new(f, "project", ToRelative(projectRoot, f), "book-spine");

            // docs/universes/*.md — per-universe world facts (source for Universe.WorldFacts)
            var universesDir = Path.Combine(docsDir, "universes");
            if (Directory.Exists(universesDir))
                foreach (var f in Directory.EnumerateFiles(universesDir, "*.md"))
                    yield return new(f, "project", ToRelative(projectRoot, f), "universe-facts");
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
                var cls     = ClassifyFile(f, content);

                // Match on (FileRoot, RelativePath): the project and global CLAUDE.md
                // share RelativePath "CLAUDE.md" and would otherwise clobber each other,
                // leaving only one of them in the DB.
                var existing = await db.MarkdownFiles
                    .FirstOrDefaultAsync(x => x.RelativePath == f.RelativePath && x.FileRoot == f.FileRoot, ct);

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
                            Tier         = cls.Tier,
                            Scope        = cls.Scope,
                            Triggers     = cls.Triggers,
                            AutoTier     = cls.AutoTier,
                        });
                        await db.SaveChangesAsync(ct);
                    }
                    inserted++;
                }
                else
                {
                    var contentChanged = existing.ContentHash != hash;
                    var classChanged   = existing.Tier != cls.Tier || existing.Scope != cls.Scope
                                      || existing.Triggers != cls.Triggers || existing.AutoTier != cls.AutoTier;
                    if (contentChanged || classChanged)
                    {
                        if (!dryRun)
                        {
                            if (contentChanged)
                            {
                                existing.FilePath    = f.FilePath;
                                existing.Content     = content;
                                existing.ContentHash = hash;
                            }
                            existing.Tier         = cls.Tier;
                            existing.Scope        = cls.Scope;
                            existing.Triggers     = cls.Triggers;
                            existing.AutoTier     = cls.AutoTier;
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
            }
            catch (Exception ex)
            {
                errors.Add($"{f.RelativePath}: {ex.Message}");
            }
        }

        return new(inserted, updated, unchanged, errors);
    }

    // ── Doc Context Stack classification (tier / scope / triggers) ──────────

    public readonly record struct DocClassification(string Tier, string Scope, string Triggers, bool AutoTier);

    // Registers are node-scoped — a story uses exactly ONE. Seed each register's
    // scope to the node CODE(s) that use it; unknowns get empty scope (curate via
    // frontmatter). A frontmatter `scope:` always overrides this.
    private static readonly Dictionary<string, string> RegisterScope =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["CODA"]     = "BCODA",
            ["GREY"]     = "ATTE",
            ["VULTURES"] = "VATD",
            // JOY / SORROW: assignment ambiguous — leave empty, curate via frontmatter.
        };

    // The ONLY universal docs (loaded for every context). Keep this list short —
    // anything here costs context budget on every prose write. Promote others by
    // adding `tier: always` to their frontmatter.
    private static readonly HashSet<string> AlwaysFiles =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "BIBLE.digest.md",
        };

    private static readonly HashSet<string> TriggerStopwords =
        new(StringComparer.OrdinalIgnoreCase)
        {
            // structural / project words
            "the","and","for","with","from","that","this","into","story","node",
            "glmz","canon","note","docs","memory","when","what","over","your","their",
            "json","yaml","file","files","rule","rules","page","data",
            // generic common words that produce false topic fires
            "only","read","real","true","anti","also","very","each","just","here",
            "there","then","than","them","they","have","this","that","will","would",
            "holding","more","less","some","most","such","been","were","being",
            "about","after","before","other","under","which","while","these","those",
            // process / meta-doc filename tokens (these docs are about HOW to work, not story
            // canon — they should not fire on generic prose words)
            "reverse","order","audit","self","gateway","pass","draft","plan","fixes",
            "update","status","version","snapshot","workflow","campaign","playbook",
            "pattern","loop","brief","recall","sync","export","deploy","goal","goals",
            "review","quality","engine","system","refactor","subsystem","feedback",
        };

    /// <summary>
    /// Classify a file for the Doc Context Stack. Frontmatter <c>tier:</c>/<c>scope:</c>/<c>triggers:</c>
    /// win (AutoTier=false); otherwise infer from category/path (AutoTier=true):
    /// register → node (scope from RegisterScope) · docs/strands/&lt;CODE&gt;.md → node scope=CODE ·
    /// AlwaysFiles → always · everything else → topic (triggers seeded from file name + description).
    /// Pure function of (file, content) so re-sync is idempotent.
    /// </summary>
    private static DocClassification ClassifyFile(DiscoveredFile f, string content)
    {
        var fm = ParseFrontmatter(content);

        if (fm.TryGetValue("tier", out var fmTier) && !string.IsNullOrWhiteSpace(fmTier))
        {
            var scope    = fm.TryGetValue("scope", out var s) ? NormalizeCsv(s) : "";
            var triggers = fm.TryGetValue("triggers", out var t) && !string.IsNullOrWhiteSpace(t)
                ? NormalizeCsv(t) : SeedTriggers(f, fm);
            return new(NormalizeTier(fmTier), scope, triggers, AutoTier: false);
        }

        var fileName = Path.GetFileName(f.FilePath);

        if (AlwaysFiles.Contains(fileName))
            return new("always", "*", "", AutoTier: true);

        if (f.Category.Equals("register", StringComparison.OrdinalIgnoreCase))
        {
            var reg = Path.GetFileNameWithoutExtension(fileName);
            return new("node", RegisterScope.GetValueOrDefault(reg, ""), "", AutoTier: true);
        }

        if (f.RelativePath.Replace('\\', '/').StartsWith("docs/strands/", StringComparison.OrdinalIgnoreCase))
        {
            var code = Path.GetFileNameWithoutExtension(fileName).ToUpperInvariant();
            return new("node", code, "", AutoTier: true);
        }

        return new("topic", "", SeedTriggers(f, fm), AutoTier: true);
    }

    /// <summary>Parse top-level <c>key: value</c> pairs from a leading YAML frontmatter block.</summary>
    private static Dictionary<string, string> ParseFrontmatter(string content)
    {
        var fm = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrEmpty(content)) return fm;
        var text = content.Replace("\r\n", "\n");
        if (!text.StartsWith("---\n")) return fm;
        var end = text.IndexOf("\n---", 4, StringComparison.Ordinal);
        if (end < 0) return fm;

        foreach (var line in text[4..end].Split('\n'))
        {
            if (line.Length == 0 || line[0] is ' ' or '\t' or '#') continue;  // top-level keys only
            var idx = line.IndexOf(':');
            if (idx <= 0) continue;
            var key = line[..idx].Trim();
            var val = line[(idx + 1)..].Trim().Trim('"', '\'');
            if (key.Length > 0) fm[key] = val;
        }
        return fm;
    }

    /// <summary>Seed topic trigger keywords from the file name + frontmatter description.</summary>
    private static string SeedTriggers(DiscoveredFile f, Dictionary<string, string> fm)
    {
        var terms = new List<string>();

        var baseName = Path.GetFileNameWithoutExtension(f.FilePath);
        baseName = Regex.Replace(baseName, @"^(project|reference|feedback|user)_", "", RegexOptions.IgnoreCase);
        terms.AddRange(baseName.Split(['_', '-', ' '], StringSplitOptions.RemoveEmptyEntries));

        if (fm.TryGetValue("description", out var desc) && !string.IsNullOrWhiteSpace(desc))
        {
            // ALLCAPS acronyms (GLMZ, ELF, RMA, QUANTA, E.L.F.) + distinctive Capitalized proper
            // nouns 5+ chars (Triumvirate, Substrate, Mnemosync). Common-word noise (Holding, Story,
            // Before, …) is filtered by TriggerStopwords below, not by excluding mixed-case outright.
            var head = desc.Length > 240 ? desc[..240] : desc;
            foreach (Match m in Regex.Matches(head, @"\b([A-Z]{2,}|[A-Z](?:\.[A-Z])+\.?|[A-Z][a-z]{4,})\b"))
                terms.Add(m.Value.Replace(".", ""));
        }

        var cleaned = terms
            .Select(t => t.Trim().Trim('.', ',', '"', '\'', '(', ')', ':', ';'))
            .Where(t => t.Length >= 4 && !TriggerStopwords.Contains(t))
            .Select(t => t.ToLowerInvariant())
            .Distinct()
            .Take(12);
        return string.Join(", ", cleaned);
    }

    private static string NormalizeTier(string t)
    {
        t = t.Trim().ToLowerInvariant();
        return t is "always" or "node" or "topic" ? t : "topic";
    }

    private static string NormalizeCsv(string s) =>
        string.Join(", ", (s ?? "").Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                    .Distinct(StringComparer.OrdinalIgnoreCase));

    // ── List ──────────────────────────────────────────────────────────────

    public async Task<List<MarkdownFile>> ListAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.MarkdownFiles
            .OrderBy(x => x.FileRoot)
            .ThenBy(x => x.RelativePath)
            .ToListAsync(ct);
    }

    // ── Search (keyword recall) ────────────────────────────────────────────

    /// <summary>
    /// Find tracked markdown files whose path, file name, or category contains the
    /// keyword (case-insensitive). With <paramref name="includeContent"/> the body
    /// text is searched too. This backs <c>ss --recall &lt;keyword&gt;</c>: it lets a
    /// caller "call up" the select few .md files relevant to a topic from the DB
    /// instead of keeping hundreds of tiny files materialized on disk.
    /// </summary>
    public async Task<List<MarkdownFile>> SearchAsync(string keyword, bool includeContent = false, CancellationToken ct = default)
    {
        var k = (keyword ?? "").Trim();
        if (k.Length == 0) return new List<MarkdownFile>();
        var like = $"%{k}%";

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.MarkdownFiles.AsNoTracking()
            .Where(x => EF.Functions.Like(x.RelativePath, like)
                     || EF.Functions.Like(x.FileName, like)
                     || EF.Functions.Like(x.Category, like)
                     || (includeContent && EF.Functions.Like(x.Content, like)))
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
