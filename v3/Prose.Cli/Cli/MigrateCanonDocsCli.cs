using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// ss --migrate-canon-docs [--dry-run]                                          (legacy bulk mode)
/// ss --migrate-canon-docs --type &lt;type&gt; --universe &lt;slug&gt; --file &lt;relPath&gt; [--dry-run]   (single-doc mode)
///
/// Step A2: migrates hand-editable canon .md files into CanonDocument +
/// CanonDocumentSection rows. After this runs, the .md files become generated
/// read-only artifacts; all edits go through set_canon_section MCP.
///
/// Bulk mode (no args) replays the 4 original migrations exactly as they shipped —
/// docs/BIBLE.md→WorldBible(GLMZ), docs/WORLD.md→WorldMaster(GLMZ),
/// docs/FRANCHISE.md→Franchise(GLMZ), docs/universes/CAUL.md→UniverseCanon(Fantasy) — kept
/// as a fixed historical list rather than derived from CanonDocumentTypes, because a
/// Scope="universe" type with a literal (non-templated) PathTemplate doesn't itself encode
/// which universe it belongs to; auto-crossing every active universe against every such type
/// would try to re-import the same literal file under the wrong universe. All 4 are already
/// migrated, so this mode is a no-op today (every spec hits the exists-check and is skipped).
///
/// Single-doc mode (--type/--universe/--file) is the path for migrating a NEW file — it
/// resolves title/scope from the CanonDocumentTypes row via CanonDocumentTypeRegistry, so
/// there is no new hardcoded copy to add per file.
///
/// Idempotent either way: skips any document whose (UniverseId, DocumentType) already exists.
/// </summary>
public static class MigrateCanonDocsCli
{
    private static readonly string RepoRoot = FindRepoRoot();

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "docs")) &&
                File.Exists(Path.Combine(dir.FullName, "docs", "BIBLE.md")))
                return dir.FullName;
            dir = dir.Parent;
        }
        return Directory.GetCurrentDirectory();
    }

    private record DocSpec(string FilePath, string DocumentType, Guid UniverseId, string Title);

    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        bool    dryRun       = args.Contains("--dry-run");
        string? typeArg      = GetArg(args, "--type");
        string? universeArg  = GetArg(args, "--universe");
        string? fileArg      = GetArg(args, "--file");

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        DocSpec[] specs;
        if (typeArg != null || universeArg != null || fileArg != null)
        {
            if (typeArg == null || universeArg == null || fileArg == null)
            {
                Console.Error.WriteLine("[migrate-canon-docs] Single-doc mode requires all of --type, --universe, --file.");
                return 2;
            }

            var universeId = CanonDocumentService.ResolveUniverseId(universeArg);
            if (universeId == null)
            {
                Console.Error.WriteLine($"[migrate-canon-docs] Unknown universe '{universeArg}'.");
                return 2;
            }

            var registry = services.GetRequiredService<CanonDocumentTypeRegistry>();
            var effectiveUniverseId = await registry.ResolveEffectiveUniverseIdAsync(typeArg, universeId.Value);
            var title = await registry.GetTitleAsync(typeArg, effectiveUniverseId);
            if (title == null)
            {
                var validTypes = string.Join(", ", await registry.ListActiveTypeNamesAsync());
                Console.Error.WriteLine($"[migrate-canon-docs] Unknown document type '{typeArg}'. Valid: {validTypes}.");
                return 2;
            }

            specs = [new DocSpec(Path.Combine(RepoRoot, fileArg.Replace('/', Path.DirectorySeparatorChar)),
                typeArg, effectiveUniverseId, title)];
        }
        else
        {
            specs =
            [
                new DocSpec(Path.Combine(RepoRoot, "docs", "BIBLE.md"),
                    "WorldBible", Universe.GlmzId, "GLMZ World Bible"),
                new DocSpec(Path.Combine(RepoRoot, "docs", "WORLD.md"),
                    "WorldMaster", Universe.GlmzId, "GLMZ World Master"),
                new DocSpec(Path.Combine(RepoRoot, "docs", "FRANCHISE.md"),
                    "Franchise", Universe.GlmzId, "GLMZ Franchise Bible"),
                new DocSpec(Path.Combine(RepoRoot, "docs", "universes", "CAUL.md"),
                    "UniverseCanon", Universe.FantasyId, "Caul Universe Canon"),
            ];
        }

        int docsCreated = 0, sectionsCreated = 0, skipped = 0;

        foreach (var spec in specs)
        {
            if (!File.Exists(spec.FilePath))
            {
                Console.WriteLine($"  SKIP  {spec.DocumentType} — file not found: {spec.FilePath}");
                skipped++;
                continue;
            }

            var exists = await db.CanonDocuments
                .AnyAsync(d => d.UniverseId == spec.UniverseId && d.DocumentType == spec.DocumentType);
            if (exists)
            {
                Console.WriteLine($"  SKIP  {spec.DocumentType} — already migrated");
                skipped++;
                continue;
            }

            var content = StripLeadingFrontMatter(await File.ReadAllTextAsync(spec.FilePath));
            var sections = ParseMarkdownSections(content);

            Console.WriteLine($"  {(dryRun ? "DRY " : "")}CREATE  {spec.DocumentType} ({sections.Count} sections) ← {Path.GetFileName(spec.FilePath)}");

            if (!dryRun)
            {
                var doc = new CanonDocument
                {
                    UniverseId   = spec.UniverseId,
                    DocumentType = spec.DocumentType,
                    Title        = spec.Title,
                    LastChecksum = ComputeChecksum(content),
                    CreatedAt    = DateTime.UtcNow,
                    UpdatedAt    = DateTime.UtcNow,
                };
                db.CanonDocuments.Add(doc);
                await db.SaveChangesAsync();
                docsCreated++;

                int sortKey = 0;
                foreach (var (key, title, sectionContent) in sections)
                {
                    db.CanonDocumentSections.Add(new CanonDocumentSection
                    {
                        DocumentId   = doc.Id,
                        SectionKey   = key,
                        SectionTitle = title,
                        Content      = sectionContent,
                        SortKey      = sortKey++,
                        UpdatedAt    = DateTime.UtcNow,
                    });
                    sectionsCreated++;
                }
                await db.SaveChangesAsync();
            }
        }

        Console.WriteLine();
        Console.WriteLine($"Done. docs={docsCreated} sections={sectionsCreated} skipped={skipped}{(dryRun ? " (dry run — nothing written)" : "")}");
        return 0;
    }

    // Parse a markdown file by ## headings. Returns (sectionKey, sectionTitle, content) tuples.
    // Pre-heading content becomes section "preamble" / sort 0.
    private static List<(string Key, string? Title, string Content)> ParseMarkdownSections(string markdown)
    {
        var results = new List<(string, string?, string)>();
        var headingPattern = new Regex(@"^(#{1,3})\s+(.+?)(?:\s+\{#([^\}]+)\})?$", RegexOptions.Multiline);
        var matches = headingPattern.Matches(markdown);

        int index = 0;

        // Capture preamble (content before first heading)
        int firstMatchStart = matches.Count > 0 ? matches[0].Index : markdown.Length;
        if (firstMatchStart > 0)
        {
            var preamble = markdown[..firstMatchStart].Trim();
            if (preamble.Length > 0)
                results.Add(("preamble", null, preamble));
        }

        for (int i = 0; i < matches.Count; i++)
        {
            var match = matches[i];
            int contentStart = match.Index + match.Length;
            int contentEnd = i + 1 < matches.Count ? matches[i + 1].Index : markdown.Length;
            var body = markdown[contentStart..contentEnd].Trim();

            var headingText = match.Groups[2].Value.Trim();
            var anchorId    = match.Groups[3].Value.Trim();

            // Prefer the explicit {#anchor} id, fall back to slugified heading
            var key = !string.IsNullOrEmpty(anchorId)
                ? anchorId
                : $"section-{SlugifyHeading(headingText)}-{index}";

            results.Add((key, headingText, body));
            index++;
        }

        return results;
    }

    /// <summary>
    /// Drop a leading YAML frontmatter block before section-parsing. Without this, the original
    /// migration's <c>##</c>-heading regex swallowed frontmatter whole into the "preamble"
    /// section (frontmatter has no heading), and <c>GenerateMdAsync</c> then prepended a SECOND,
    /// freshly-computed frontmatter block ahead of it on every regeneration — doubled headers in
    /// the generated file. Mirrors the boundary detection in
    /// <c>MarkdownFileService.ParseFrontmatter</c>.
    /// </summary>
    private static string StripLeadingFrontMatter(string markdown)
    {
        var text = markdown.Replace("\r\n", "\n");
        if (!text.StartsWith("---\n")) return markdown;
        var end = text.IndexOf("\n---", 4, StringComparison.Ordinal);
        if (end < 0) return markdown;
        var closeLineEnd = text.IndexOf('\n', end + 1);
        return closeLineEnd < 0 ? "" : text[(closeLineEnd + 1)..].TrimStart('\n');
    }

    private static string? GetArg(string[] args, string flag)
    {
        var i = Array.IndexOf(args, flag);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }

    private static string SlugifyHeading(string heading)
        => Regex.Replace(heading.ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');

    private static string ComputeChecksum(string content)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
