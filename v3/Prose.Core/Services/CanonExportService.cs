using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;

namespace Prose.Core.Services;

/// <summary>
/// Writes raw canon JSON to the configured publish directory. Three scopes:
///   <list type="bullet">
///   <item>per-entity — single &lt;Name&gt;.json (sourced from <c>Records.Json</c>)</item>
///   <item>per-repo — &lt;RepoName&gt;.zip of every entity in that repo</item>
///   <item>global — prose-export-YYYYMMDD.zip with one folder per repo</item>
///   </list>
/// Repo + global rely on <see cref="ExportDiscoveryService"/> so a new
/// <see cref="Interfaces.IExportableRepository"/> picks itself up automatically.
/// Distinct from <see cref="ExportService"/>, which renders chapter HTML to
/// TXT/MD/print-HTML, and from <see cref="HtmlExportService"/>, which renders
/// the canon as a static HTML encyclopedia.
/// </summary>
public class CanonExportService
{
    private readonly IDbContextFactory<ProseDbContext> dbFactory;
    private readonly ExportDiscoveryService discovery;
    private readonly SettingsService settings;
    private readonly IUniverseContext universe;
    private readonly ILogger<CanonExportService> log;
    private static readonly JsonSerializerOptions PrettyOpts = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    public CanonExportService(
        IDbContextFactory<ProseDbContext> dbFactory,
        ExportDiscoveryService discovery,
        SettingsService settings,
        IUniverseContext universe,
        ILogger<CanonExportService> log)
    {
        this.dbFactory = dbFactory;
        this.discovery = discovery;
        this.settings = settings;
        this.universe = universe;
        this.log = log;
    }

    /// <summary>
    /// Resolve a user-supplied token to an EntityId. Accepts a Guid in any
    /// format, the dashless 32-char form, or an <c>Entity.Slug</c>. Returns
    /// null if no entity matches.
    /// </summary>
    public async Task<Guid?> ResolveEntityIdAsync(string token, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        if (Guid.TryParse(token, out var g)) return g;
        if (token.Length == 32 && Guid.TryParseExact(token, "N", out g)) return g;

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var hit = await db.Entities.AsNoTracking()
            .Where(e => e.Slug == token)
            .Select(e => (Guid?)e.Id)
            .FirstOrDefaultAsync(ct);
        return hit;
    }

    /// <summary>Resolved publish directory for canon exports — created if missing.
    /// Canon queries are scoped to the ambient universe, so exports land in that
    /// universe's folder (e.g. …\GLMZ) via the same per-universe resolution the
    /// manuscript exporters use.</summary>
    private string PublishDir
    {
        get
        {
            var dir = settings.GetExportDirectory(universe.CurrentSlug);
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    public sealed record ExportResult(string Path, int EntryCount, long Bytes);

    /// <summary>
    /// Write a single &lt;Name&gt;.json to the publish directory. Source is <c>Records.Json</c>
    /// — the canonical blob — pretty-printed for human readability.
    /// </summary>
    public async Task<ExportResult> ExportEntityAsync(Guid entityId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var row = await db.Records.AsNoTracking()
            .Where(r => r.EntityId == entityId)
            .Select(r => new { r.Json, r.Entity!.Name, r.Entity.EntityType, r.Entity.Slug })
            .FirstOrDefaultAsync(ct);
        if (row == null)
            throw new InvalidOperationException($"No Record found for EntityId={entityId}.");

        var fileName = $"{ResolveSlug(row.Slug, row.Name)}-{DateTime.Now:yyyyMMdd-HHmmss}.json";
        var path = Path.Combine(PublishDir, fileName);
        var pretty = TryPrettyPrint(row.Json);
        await File.WriteAllTextAsync(path, pretty, new UTF8Encoding(false), ct);

        log.LogInformation("Exported entity {Name} ({Type}) → {Path}", row.Name, row.EntityType, path);
        return new ExportResult(path, 1, new FileInfo(path).Length);
    }

    /// <summary>
    /// Like <see cref="ExportEntityAsync"/> but also bundles every cross-repo
    /// record this entity names. Output is &lt;Name&gt;-bundle.zip with the root
    /// entity at the top and references nested under <c>references/&lt;Repo&gt;/</c>.
    /// References are resolved by name lookup across every
    /// <see cref="Interfaces.IExportableRepository"/> — string values inside the
    /// blob that match a known entity name are pulled in.
    /// </summary>
    public async Task<ExportResult> ExportEntityDeepAsync(Guid entityId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var row = await db.Records.AsNoTracking()
            .Where(r => r.EntityId == entityId)
            .Select(r => new { r.Json, r.Entity!.Name, r.Entity.EntityType, r.Entity.Slug })
            .FirstOrDefaultAsync(ct);
        if (row == null)
            throw new InvalidOperationException($"No Record found for EntityId={entityId}.");

        // Build a (repoName, entityName) → (repo, json, slug) index from every
        // exportable repo. Case-insensitive on the entity name; collisions
        // resolve to the first hit.
        var repos = discovery.GetAllRepos();
        var nameIndex = new Dictionary<string, (string Repo, string Json, string Slug)>(StringComparer.OrdinalIgnoreCase);
        foreach (var (repoName, entries) in repos)
            foreach (var (name, json) in entries)
                if (!string.IsNullOrWhiteSpace(name) && !nameIndex.ContainsKey(name))
                    nameIndex[name] = (repoName, json, Slugify(name));

        // Walk the JSON tree, collect every string-valued leaf that names a
        // known entity (excluding the root entity itself).
        var refs = new Dictionary<string, (string Repo, string Json, string Slug)>(StringComparer.OrdinalIgnoreCase);
        try
        {
            using var doc = JsonDocument.Parse(row.Json);
            CollectStringLeaves(doc.RootElement, leaf =>
            {
                if (string.IsNullOrWhiteSpace(leaf)) return;
                if (string.Equals(leaf, row.Name, StringComparison.OrdinalIgnoreCase)) return;
                if (refs.ContainsKey(leaf)) return;
                if (nameIndex.TryGetValue(leaf, out var hit)) refs[leaf] = hit;
            });
        }
        catch
        {
            // Bad JSON on the root — write what we have, skip ref-walk.
        }

        var rootSlug = ResolveSlug(row.Slug, row.Name);
        var fileName = $"{rootSlug}-bundle-{DateTime.Now:yyyyMMdd-HHmmss}.zip";
        var path = Path.Combine(PublishDir, fileName);
        if (File.Exists(path)) File.Delete(path);
        using (var fs = File.Create(path))
        using (var zip = new ZipArchive(fs, ZipArchiveMode.Create))
        {
            AddJsonEntry(zip, $"{rootSlug}.json", row.Json);
            foreach (var (_, hit) in refs)
            {
                ct.ThrowIfCancellationRequested();
                var folder = Slugify(hit.Repo);
                AddJsonEntry(zip, $"references/{folder}/{hit.Slug}.json", hit.Json);
            }
        }

        log.LogInformation("Deep-exported {Name} ({Type}) + {RefCount} references → {Path}",
            row.Name, row.EntityType, refs.Count, path);
        return new ExportResult(path, 1 + refs.Count, new FileInfo(path).Length);
    }

    private static void CollectStringLeaves(JsonElement el, Action<string> sink)
    {
        switch (el.ValueKind)
        {
            case JsonValueKind.String:
                sink(el.GetString() ?? "");
                break;
            case JsonValueKind.Array:
                foreach (var child in el.EnumerateArray()) CollectStringLeaves(child, sink);
                break;
            case JsonValueKind.Object:
                foreach (var prop in el.EnumerateObject()) CollectStringLeaves(prop.Value, sink);
                break;
        }
    }

    /// <summary>
    /// Zip every entity in <paramref name="repoName"/> into &lt;RepoName&gt;.zip
    /// under the publish directory. Repo name match is case-insensitive against
    /// <see cref="ExportDiscoveryService.GetAllRepos"/>.
    /// </summary>
    public Task<ExportResult> ExportRepoAsync(string repoName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(repoName))
            throw new ArgumentException("Repo name required.", nameof(repoName));

        var repos = discovery.GetAllRepos();
        var matched = repos.FirstOrDefault(kv =>
            string.Equals(kv.Key, repoName, StringComparison.OrdinalIgnoreCase));
        if (matched.Key == null)
            throw new InvalidOperationException(
                $"Repo '{repoName}' not found. Known: {string.Join(", ", repos.Keys.OrderBy(k => k))}");

        var entries = matched.Value;
        var fileName = $"{Slugify(matched.Key)}-{DateTime.Now:yyyyMMdd-HHmmss}.zip";
        var path = Path.Combine(PublishDir, fileName);

        if (File.Exists(path)) File.Delete(path);
        using (var fs = File.Create(path))
        using (var zip = new ZipArchive(fs, ZipArchiveMode.Create))
        {
            foreach (var (name, json) in entries)
            {
                ct.ThrowIfCancellationRequested();
                AddJsonEntry(zip, $"{Slugify(name)}.json", json);
            }
        }

        log.LogInformation("Exported repo {Repo} ({N} entries) → {Path}", matched.Key, entries.Count, path);
        return Task.FromResult(new ExportResult(path, entries.Count, new FileInfo(path).Length));
    }

    /// <summary>
    /// Zip every repo into a single timestamped archive under the publish directory.
    /// Entries are namespaced as <c>&lt;RepoName&gt;/&lt;EntityName&gt;.json</c>.
    /// </summary>
    public Task<ExportResult> ExportAllAsync(CancellationToken ct = default)
    {
        var repos = discovery.GetAllRepos();
        var fileName = $"prose-export-{DateTime.Now:yyyyMMdd-HHmmss}.zip";
        var path = Path.Combine(PublishDir, fileName);

        int total = 0;
        if (File.Exists(path)) File.Delete(path);
        using (var fs = File.Create(path))
        using (var zip = new ZipArchive(fs, ZipArchiveMode.Create))
        {
            foreach (var (repoName, entries) in repos.OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase))
            {
                var folder = Slugify(repoName);
                foreach (var (name, json) in entries)
                {
                    ct.ThrowIfCancellationRequested();
                    AddJsonEntry(zip, $"{folder}/{Slugify(name)}.json", json);
                    total++;
                }
            }
        }

        log.LogInformation("Exported global archive ({Repos} repos, {N} entries) → {Path}",
            repos.Count, total, path);
        return Task.FromResult(new ExportResult(path, total, new FileInfo(path).Length));
    }

    private static void AddJsonEntry(ZipArchive zip, string entryName, string json)
    {
        var entry = zip.CreateEntry(entryName, CompressionLevel.Optimal);
        using var stream = entry.Open();
        var bytes = new UTF8Encoding(false).GetBytes(TryPrettyPrint(json));
        stream.Write(bytes, 0, bytes.Length);
    }

    private static string TryPrettyPrint(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(doc.RootElement, PrettyOpts);
        }
        catch
        {
            return json;
        }
    }

    /// <summary>
    /// Prefer the entity's stored <c>Entity.Slug</c> (canonical, generated by
    /// <see cref="UniverseGraphService.Slugify"/> as <c>lowercase_with_underscores</c>),
    /// falling back to a fresh slug from the name. Either way, the result is
    /// re-folded through <see cref="Slugify"/> so accented stored slugs (if any
    /// ever existed) are reduced to ASCII and Windows-illegal chars are dropped.
    /// </summary>
    private static string ResolveSlug(string? entitySlug, string name)
    {
        var raw = !string.IsNullOrWhiteSpace(entitySlug) ? entitySlug! : name;
        return Slugify(raw);
    }

    /// <summary>
    /// Filename-safe slug. Pipeline:
    ///   1. ASCII-fold accents — Č → c, á → a, ï → i, ñ → n — via NFD normalize +
    ///      <see cref="UnicodeCategory.NonSpacingMark"/> strip, plus a manual map
    ///      for characters that don't decompose (ø, æ, ß, þ, …).
    ///   2. Lowercase.
    ///   3. Replace runs of non-[a-z0-9] with a single underscore (matches
    ///      <see cref="UniverseGraphService.Slugify"/> shape).
    ///   4. Trim leading/trailing underscores.
    ///   5. Defensive Windows-illegal filter (cheap belt-and-suspenders; the
    ///      step-3 filter already ensures the output is <c>[a-z0-9_]+</c>).
    /// </summary>
    private static string Slugify(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "unnamed";

        var folded = AsciiFold(raw.Trim()).ToLowerInvariant();

        var sb = new StringBuilder(folded.Length);
        var prevUnder = true;
        foreach (var ch in folded)
        {
            if ((ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9'))
            {
                sb.Append(ch);
                prevUnder = false;
            }
            else if (!prevUnder)
            {
                sb.Append('_');
                prevUnder = true;
            }
        }
        var s = sb.ToString().Trim('_');
        s = StripWindowsIllegal(s);
        return s.Length == 0 ? "unnamed" : s;
    }

    /// <summary>
    /// NFD normalize, drop non-spacing marks, plus a tiny map for characters
    /// that don't decompose into base letter + combining mark. Mirrors
    /// <c>JsonDirectoryRepository.StripDiacritics</c>.
    /// </summary>
    private static string AsciiFold(string text)
    {
        var sb = new StringBuilder(text.Length + 4);
        foreach (var c in text)
        {
            if (DiacriticMap.TryGetValue(c, out var mapped)) sb.Append(mapped);
            else sb.Append(c);
        }
        var normalized = sb.ToString().Normalize(System.Text.NormalizationForm.FormD);
        sb.Clear();
        foreach (var c in normalized)
        {
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c)
                != System.Globalization.UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        return sb.ToString().Normalize(System.Text.NormalizationForm.FormC);
    }

    private static readonly Dictionary<char, string> DiacriticMap = new()
    {
        ['ø'] = "o", ['Ø'] = "o", ['ð'] = "d", ['Ð'] = "d", ['þ'] = "th", ['Þ'] = "th",
        ['æ'] = "ae", ['Æ'] = "ae", ['œ'] = "oe", ['Œ'] = "oe", ['ß'] = "ss",
        ['ł'] = "l", ['Ł'] = "l", ['ı'] = "i", ['ĸ'] = "k", ['ŉ'] = "n",
    };

    /// <summary>
    /// Strip the Windows reserved filename characters <c>&lt; &gt; : " / \ | ? *</c>
    /// plus control chars and trailing dot/space. Defensive — the slug pipeline
    /// already constrains output to <c>[a-z0-9_]+</c>, but the input path also
    /// uses this for direct user-supplied repo names if a future caller passes
    /// one through unsanitised.
    /// </summary>
    private static string StripWindowsIllegal(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        var sb = new StringBuilder(s.Length);
        foreach (var ch in s)
        {
            if (ch is '<' or '>' or ':' or '"' or '/' or '\\' or '|' or '?' or '*') continue;
            if (ch < 32) continue;
            sb.Append(ch);
        }
        return sb.ToString().TrimEnd('.', ' ');
    }
}
