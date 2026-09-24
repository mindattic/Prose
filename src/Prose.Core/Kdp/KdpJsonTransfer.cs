using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Services;

namespace Prose.Core.Kdp;

/// <summary>A book's export folder — where its legacy <c>.publish</c> marker lives.</summary>
public sealed record KdpBookFolder(string Code, string FolderPath);

/// <summary>Lists the book folders a legacy-marker import (or an in-place marker export) walks.
/// Production: <see cref="KdpBookFolderLocator"/> (Prose DB + export settings).</summary>
public interface IKdpBookFolderSource
{
    Task<IReadOnlyList<KdpBookFolder>> ListAsync(CancellationToken ct = default);
}

public sealed class KdpTransferOptions
{
    /// <summary>Where the first-run import reads title-ids.json, category-tree-*.json and logs/.</summary>
    public Func<string> ToolsDir { get; init; } = KdpPaths.ResolveToolsDir;
    /// <summary>Off in tests that want an empty store.</summary>
    public bool AutoImport { get; init; } = true;
}

/// <summary>What an import should read.</summary>
public sealed class KdpImportRequest
{
    /// <summary>Folder holding title-ids.json, category-tree-*.json, logs/*.log and (from an
    /// export) publish-markers/. Missing pieces are skipped.</summary>
    public required string FromDir { get; init; }
    /// <summary>Book folders to read legacy <c>.publish</c> markers from, in place. Null skips them.</summary>
    public IReadOnlyList<KdpBookFolder>? LegacyMarkerFolders { get; init; }
    public KdpImportKind Kind { get; init; } = KdpImportKind.Manual;
}

/// <summary>What an export should write.</summary>
public sealed class KdpExportRequest
{
    /// <summary>Folder to write title-ids.json, category-tree-*.json, logs/ and publish-markers/ into.</summary>
    public required string ToDir { get; init; }
    /// <summary>When set, signed-off books' markers are also written back as <c>.publish</c> files
    /// in these book folders (the original layout).</summary>
    public IReadOnlyList<KdpBookFolder>? MarkersInPlace { get; init; }
}

/// <summary>
/// JSON is import/export only: this loads the store from the legacy files and writes them back in
/// the same shapes. Layout of an export folder (and what an import reads):
/// <code>
///   title-ids.json                     the NodeCode → titleId crosswalk, _comment and all
///   category-tree-&lt;slug&gt;.json          one per crawled category subtree
///   logs/kdp-run-*.log                 one per KdpPublish run, in the run log's own text format
///   publish-markers/&lt;CODE&gt;.publish     a signed-off book's .publish marker body
///   publish-markers/held/&lt;CODE&gt;.publish  a book held back (no sign-off) — kept so nothing is lost
/// </code>
/// Imports merge: the file's values win for the fields it carries; nothing in the store is deleted.
/// </summary>
public sealed class KdpJsonTransfer
{
    public const string TitleIdsFile = "title-ids.json";
    public const string CategoryTreePrefix = "category-tree-";
    public const string LogsDir = "logs";
    public const string MarkersDir = "publish-markers";
    public const string HeldMarkersDir = "held";
    public const string MarkerFileName = ".publish";

    /// <summary>The exact options the old writers used: System.Text.Json's default (escaping)
    /// encoder, indented, CRLF (the committed files are CRLF; pinned rather than left to
    /// Environment.NewLine) — so an export of an unchanged import is byte-identical.</summary>
    internal static readonly JsonSerializerOptions FileJson = new()
    {
        WriteIndented = true,
        NewLine = "\r\n",
    };

    public static readonly JsonSerializerOptions CategoryTreeJson = new()
    {
        WriteIndented = true,
        NewLine = "\r\n",
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private static readonly JsonSerializerOptions ReadJson = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private readonly IDbContextFactory<KdpDbContext> factory;
    private readonly IKdpBookFolderSource folders;
    private readonly KdpTransferOptions options;
    private readonly TimeProvider clock;
    private readonly ILogger? log;
    private volatile bool autoImportSettled;
    private bool deferredLogged;
    private readonly SemaphoreSlim autoImportGate = new(1, 1);

    public KdpJsonTransfer(
        IDbContextFactory<KdpDbContext> factory,
        IKdpBookFolderSource folders,
        KdpTransferOptions? options = null,
        TimeProvider? clock = null,
        ILogger<KdpJsonTransfer>? log = null)
    {
        this.factory = factory;
        this.folders = folders;
        this.options = options ?? new KdpTransferOptions();
        this.clock = clock ?? TimeProvider.System;
        this.log = log;
    }

    // ── First run ─────────────────────────────────────────────────────────

    /// <summary>
    /// The one-time automatic import. Runs when no <see cref="KdpImportKind.FirstRun"/> import has
    /// ever been recorded: loads tools/kdp (title-ids.json, category trees, run logs) plus every
    /// book folder's legacy <c>.publish</c> marker, then records a <see cref="KdpImport"/> row so it
    /// never runs again. The files are left exactly as they are — they are the backup.
    /// Deferred (not recorded, retried next time) when tools/kdp/title-ids.json can't be found or
    /// the book folders can't be listed, so a process started from the wrong place can never mark
    /// the import done with nothing imported. Safe across processes: the check and the import run
    /// inside one write transaction.
    /// </summary>
    public async Task AutoImportIfNeededAsync(CancellationToken ct = default)
    {
        if (autoImportSettled || !options.AutoImport) return;
        await autoImportGate.WaitAsync(ct);
        try
        {
            if (autoImportSettled) return;
            await KdpMigrator.EnsureMigratedAsync(factory, ct);

            await using (var probe = await factory.CreateDbContextAsync(ct))
                if (await probe.Imports.AnyAsync(i => i.Kind == KdpImportKind.FirstRun, ct)) { autoImportSettled = true; return; }

            var toolsDir = options.ToolsDir();
            if (!File.Exists(Path.Combine(toolsDir, TitleIdsFile)))
            {
                if (!deferredLogged)
                    log?.LogWarning("KDP store: first-run import deferred — {File} not found in {Dir}.", TitleIdsFile, toolsDir);
                deferredLogged = true;
                return;
            }

            IReadOnlyList<KdpBookFolder> bookFolders;
            try { bookFolders = await folders.ListAsync(ct); }
            catch (Exception ex)
            {
                if (!deferredLogged)
                    log?.LogWarning(ex, "KDP store: first-run import deferred — book folders could not be listed.");
                deferredLogged = true;
                return;
            }

            var counts = await ImportCoreAsync(new KdpImportRequest
            {
                FromDir = toolsDir,
                LegacyMarkerFolders = bookFolders,
                Kind = KdpImportKind.FirstRun,
            }, onlyIfFirstRunMissing: true, ct);

            if (counts != null)
                log?.LogInformation("KDP store: first-run import from {Dir}: {Counts}.", toolsDir, counts);
            autoImportSettled = true;
        }
        finally { autoImportGate.Release(); }
    }

    // ── Import ────────────────────────────────────────────────────────────

    public async Task<KdpTransferCounts> ImportAsync(KdpImportRequest request, CancellationToken ct = default)
    {
        await KdpMigrator.EnsureMigratedAsync(factory, ct);
        return (await ImportCoreAsync(request, onlyIfFirstRunMissing: false, ct))!;
    }

    private async Task<KdpTransferCounts?> ImportCoreAsync(KdpImportRequest request, bool onlyIfFirstRunMissing, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await db.Database.OpenConnectionAsync(ct);
        // BEGIN IMMEDIATE: take the write lock before the check, so two processes starting at
        // once can't both run the first-run import.
        await using var tx = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: false);
        await db.Database.UseTransactionAsync(tx, ct);

        if (onlyIfFirstRunMissing && await db.Imports.AnyAsync(i => i.Kind == KdpImportKind.FirstRun, ct))
            return null;

        var counts = new KdpTransferCounts();
        var now = clock.GetUtcNow();

        await ImportTitleIdsAsync(db, Path.Combine(request.FromDir, TitleIdsFile), counts, now, ct);
        await ImportCategoryTreesAsync(db, request.FromDir, counts, ct);
        await ImportRunLogsAsync(db, Path.Combine(request.FromDir, LogsDir), counts, ct);

        var markers = new List<(string Code, string Path, bool Ready)>();
        var bundle = Path.Combine(request.FromDir, MarkersDir);
        if (Directory.Exists(bundle))
        {
            foreach (var f in Directory.GetFiles(bundle, "*" + MarkerFileName))
                markers.Add((Path.GetFileNameWithoutExtension(f), f, true));
            var held = Path.Combine(bundle, HeldMarkersDir);
            if (Directory.Exists(held))
                foreach (var f in Directory.GetFiles(held, "*" + MarkerFileName))
                    markers.Add((Path.GetFileNameWithoutExtension(f), f, false));
        }
        foreach (var folder in request.LegacyMarkerFolders ?? [])
        {
            var f = Path.Combine(folder.FolderPath, MarkerFileName);
            if (File.Exists(f)) markers.Add((folder.Code, f, true));
        }
        foreach (var (code, path, ready) in markers)
            await ImportMarkerAsync(db, code, path, ready, counts, now, ct);

        db.Imports.Add(new KdpImport
        {
            At = now,
            Kind = request.Kind,
            Source = request.FromDir + (request.LegacyMarkerFolders is { } lf ? $" + {lf.Count} book folder(s)" : ""),
            Counts = counts,
        });
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return counts;
    }

    private static async Task ImportTitleIdsAsync(KdpDbContext db, string path, KdpTransferCounts counts, DateTimeOffset now, CancellationToken ct)
    {
        if (!File.Exists(path)) return;
        var root = JsonNode.Parse(await File.ReadAllTextAsync(path, ct), documentOptions: new JsonDocumentOptions
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        }) as JsonObject ?? throw new InvalidDataException($"{path} is not a JSON object.");

        var next = await KdpStore.NextTitleSortOrderAsync(db, ct);
        foreach (var (key, value) in root)
        {
            if (key.StartsWith('_'))
            {
                var note = await db.TitleNotes.FirstOrDefaultAsync(n => n.Key == key, ct);
                if (note == null) db.TitleNotes.Add(note = new KdpTitleNote { Key = key, SortOrder = next++ });
                note.ValueJson = value?.ToJsonString() ?? "null";
                continue;
            }
            if (value is not JsonObject entry) continue;

            var row = await db.Titles.FirstOrDefaultAsync(t => t.Code == key, ct);
            if (row == null) db.Titles.Add(row = new KdpTitle { Code = key, SortOrder = next++ });

            var extra = new JsonObject();
            foreach (var (prop, propValue) in entry)
            {
                switch (prop)
                {
                    case "titleId": row.TitleId = propValue?.GetValue<string>() ?? row.TitleId; break;
                    case "asin": row.Asin = propValue?.GetValue<string>() ?? row.Asin; break;
                    default: extra[prop] = propValue?.DeepClone(); break;
                }
            }
            row.ExtraJson = extra.Count > 0 ? extra.ToJsonString() : null;
            row.UpdatedAt = now;
            counts.Titles++;
        }
        await db.SaveChangesAsync(ct);
    }

    private static async Task ImportCategoryTreesAsync(KdpDbContext db, string dir, KdpTransferCounts counts, CancellationToken ct)
    {
        if (!Directory.Exists(dir)) return;
        foreach (var file in Directory.GetFiles(dir, CategoryTreePrefix + "*.json").Order(StringComparer.Ordinal))
        {
            var name = Path.GetFileNameWithoutExtension(file);
            var slug = name[CategoryTreePrefix.Length..];
            var tree = JsonSerializer.Deserialize<KdpCategoryNode>(await File.ReadAllTextAsync(file, ct), CategoryTreeJson)
                       ?? throw new InvalidDataException($"{file} holds no category tree.");

            var row = await db.CategoryTrees.FirstOrDefaultAsync(t => t.Slug == slug, ct);
            if (row == null) db.CategoryTrees.Add(new KdpCategoryTree { Slug = slug, Tree = tree });
            else row.Tree = tree;
            counts.CategoryTrees++;
        }
        await db.SaveChangesAsync(ct);
    }

    private static async Task ImportRunLogsAsync(KdpDbContext db, string dir, KdpTransferCounts counts, CancellationToken ct)
    {
        if (!Directory.Exists(dir)) return;
        foreach (var file in Directory.GetFiles(dir, "*.log").Order(StringComparer.Ordinal))
        {
            var fileName = Path.GetFileName(file);
            if (await db.Runs.AnyAsync(r => r.LogFileName == fileName, ct)) continue;

            var parsed = KdpRunLogFormat.Parse(await File.ReadAllTextAsync(file, ct));
            if (parsed.Lines.Count == 0) continue;
            var runId = parsed.RunId ?? Guid.CreateVersion7(parsed.Lines[0].At);
            if (await db.Runs.AnyAsync(r => r.Id == runId, ct)) continue;

            db.Runs.Add(new KdpRun
            {
                Id = runId,
                StartedAt = parsed.Lines[0].At,
                FinishedAt = parsed.Lines[^1].Message == KdpRunLogFormat.FinishedMessage ? parsed.Lines[^1].At : null,
                Codes = parsed.Codes,
                LogFileName = fileName,
                Lines = parsed.Lines.Select(l => new KdpRunLine { RunId = runId, At = l.At, Message = l.Message }).ToList(),
            });
            counts.Runs++;
            counts.RunLines += parsed.Lines.Count;
        }
        await db.SaveChangesAsync(ct);
    }

    private static async Task ImportMarkerAsync(KdpDbContext db, string code, string path, bool ready, KdpTransferCounts counts, DateTimeOffset now, CancellationToken ct)
    {
        var raw = (await File.ReadAllTextAsync(path, ct)).Trim();
        PublishMarker? marker = null;
        if (raw.Length > 0)
        {
            // A malformed or legacy-empty marker still signs the book off — exactly how the
            // manifest treated it.
            try { marker = JsonSerializer.Deserialize<PublishMarker>(raw, ReadJson); }
            catch (JsonException) { }
        }

        var book = db.Books.Local.FirstOrDefault(b => b.Code == code)
                   ?? await db.Books.FirstOrDefaultAsync(b => b.Code == code, ct);
        if (book == null) db.Books.Add(book = new KdpBook { Code = code });

        book.SignOff = new KdpSignOff { Ready = ready, ChangedAt = now, ChangedBy = "import" };
        var snapshot = new KdpPublishSnapshot
        {
            File = marker?.File,
            Version = marker?.Version,
            Asin = marker?.Asin,
            PublishedAt = ParseUtc(marker?.PublishedAtUtc),
        };
        book.LastPublish = snapshot;
        book.PublishingDetectedAt = ParseUtc(marker?.PublishingDetectedAtUtc);
        book.UpdatedAt = now;
        counts.Books++;

        if (snapshot.PublishedAt != null)
        {
            var known = await db.PublishRecords.AnyAsync(r => r.Code == code
                && r.Publish.File == snapshot.File && r.Publish.PublishedAt == snapshot.PublishedAt, ct);
            if (!known)
            {
                db.PublishRecords.Add(new KdpPublishRecord
                {
                    Code = code,
                    Publish = snapshot.Clone(),
                    Source = KdpPublishSource.Import,
                    RecordedAt = snapshot.PublishedAt.Value,
                });
                counts.PublishRecords++;
            }
        }
        await db.SaveChangesAsync(ct);
    }

    internal static DateTimeOffset? ParseUtc(string? s) =>
        DateTimeOffset.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var v)
            ? v.ToUniversalTime()
            : null;

    internal static string? FormatUtc(DateTimeOffset? v) => v?.UtcDateTime.ToString("O", CultureInfo.InvariantCulture);

    // ── Export ────────────────────────────────────────────────────────────

    public async Task<KdpTransferCounts> ExportAsync(KdpExportRequest request, CancellationToken ct = default)
    {
        await KdpMigrator.EnsureMigratedAsync(factory, ct);
        await using var db = await factory.CreateDbContextAsync(ct);
        var counts = new KdpTransferCounts();
        Directory.CreateDirectory(request.ToDir);

        // title-ids.json
        var titles = await db.Titles.AsNoTracking().ToListAsync(ct);
        var notes = await db.TitleNotes.AsNoTracking().ToListAsync(ct);
        if (titles.Count + notes.Count > 0)
        {
            await File.WriteAllTextAsync(Path.Combine(request.ToDir, TitleIdsFile), RenderTitleIds(titles, notes), ct);
            counts.Titles = titles.Count;
        }

        // category-tree-<slug>.json
        foreach (var tree in await db.CategoryTrees.AsNoTracking().ToListAsync(ct))
        {
            await File.WriteAllTextAsync(Path.Combine(request.ToDir, $"{CategoryTreePrefix}{tree.Slug}.json"), RenderCategoryTree(tree.Tree), ct);
            counts.CategoryTrees++;
        }

        // logs/*.log
        var runs = await db.Runs.AsNoTracking().Include(r => r.Lines.OrderBy(l => l.Id)).ToListAsync(ct);
        if (runs.Count > 0)
        {
            var logsDir = Path.Combine(request.ToDir, LogsDir);
            Directory.CreateDirectory(logsDir);
            foreach (var run in runs)
            {
                var name = run.LogFileName ?? $"kdp-run-{run.StartedAt.UtcDateTime:yyyyMMdd-HHmmss}.log";
                await File.WriteAllTextAsync(Path.Combine(logsDir, name), KdpRunLogFormat.Render(run.Lines), ct);
                counts.Runs++;
                counts.RunLines += run.Lines.Count;
            }
        }

        // publish-markers/
        var books = await db.Books.AsNoTracking().OrderBy(b => b.Code).ToListAsync(ct);
        counts.PublishRecords = await db.PublishRecords.CountAsync(ct);
        if (books.Count > 0)
        {
            var markersDir = Path.Combine(request.ToDir, MarkersDir);
            var heldDir = Path.Combine(markersDir, HeldMarkersDir);
            Directory.CreateDirectory(markersDir);
            foreach (var book in books)
            {
                var dir = book.SignOff.Ready ? markersDir : heldDir;
                Directory.CreateDirectory(dir);
                await File.WriteAllTextAsync(Path.Combine(dir, book.Code + MarkerFileName), RenderMarker(book), ct);
                counts.Books++;
            }

            if (request.MarkersInPlace is { } inPlace)
            {
                var byCode = books.Where(b => b.SignOff.Ready).ToDictionary(b => b.Code, StringComparer.Ordinal);
                foreach (var folder in inPlace)
                    if (byCode.TryGetValue(folder.Code, out var book) && Directory.Exists(folder.FolderPath))
                        await File.WriteAllTextAsync(Path.Combine(folder.FolderPath, MarkerFileName), RenderMarker(book), ct);
            }
        }

        return counts;
    }

    internal static string RenderTitleIds(IEnumerable<KdpTitle> titles, IEnumerable<KdpTitleNote> notes)
    {
        var root = new JsonObject();
        var ordered = notes.Select(n => (n.SortOrder, Key: n.Key, Node: JsonNode.Parse(n.ValueJson)))
            .Concat(titles.Select(t => (t.SortOrder, Key: t.Code, Node: (JsonNode?)TitleEntry(t))))
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Key, StringComparer.Ordinal);
        foreach (var (_, key, node) in ordered)
            root[key] = node;
        return root.ToJsonString(FileJson);
    }

    private static JsonObject TitleEntry(KdpTitle t)
    {
        var entry = new JsonObject();
        if (t.TitleId != null) entry["titleId"] = t.TitleId;
        if (t.Asin != null) entry["asin"] = t.Asin;
        if (t.ExtraJson != null && JsonNode.Parse(t.ExtraJson) is JsonObject extra)
            foreach (var (k, v) in extra) entry[k] = v?.DeepClone();
        return entry;
    }

    public static string RenderCategoryTree(KdpCategoryNode tree) => JsonSerializer.Serialize(tree, CategoryTreeJson);

    /// <summary>A book's <c>.publish</c> marker body: empty for a book signed off with no publish
    /// history (the original "presence only" marker), otherwise the PublishMarker JSON.</summary>
    internal static string RenderMarker(KdpBook book)
    {
        if (book.LastPublish.IsEmpty && book.PublishingDetectedAt == null) return "";
        return JsonSerializer.Serialize(ToPublishMarker(book), FileJson);
    }

    /// <summary>The manifest's view of a book's publish state — the same record the marker file
    /// deserialized into, so manifest.json keeps its shape.</summary>
    public static PublishMarker? ToPublishMarker(KdpBook? book)
    {
        if (book == null || (book.LastPublish.IsEmpty && book.PublishingDetectedAt == null)) return null;
        return new PublishMarker(
            File: book.LastPublish.File,
            Asin: book.LastPublish.Asin,
            PublishedAtUtc: FormatUtc(book.LastPublish.PublishedAt),
            Version: book.LastPublish.Version,
            PublishingDetectedAtUtc: FormatUtc(book.PublishingDetectedAt));
    }
}

/// <summary>The text format of a KdpPublish run log: one <c>[yyyy-MM-dd HH:mm:ssZ] message</c>
/// entry per line (a message may itself span lines).</summary>
public static class KdpRunLogFormat
{
    public const string FinishedMessage = "Run finished.";
    public static string NewLine => Environment.NewLine;

    private static readonly Regex EntryStart = new(@"^\[(?<at>\d{4}-\d\d-\d\d \d\d:\d\d:\d\d)Z\] ", RegexOptions.Compiled);
    private static readonly Regex EntrySplit = new(@"\r?\n(?=\[\d{4}-\d\d-\d\d \d\d:\d\d:\d\dZ\] )", RegexOptions.Compiled);
    private static readonly Regex StartedLine = new(@"^=== Run started \((?<id>[0-9a-fA-F-]{36})\): (?<codes>.*) ===$", RegexOptions.Compiled);

    public static string Stamp(DateTimeOffset at, string message) =>
        $"[{at.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)}Z] {message}";

    public static string StartedMessage(Guid runId, IEnumerable<string> codes) =>
        $"=== Run started ({runId}): {string.Join(", ", codes)} ===";

    public static string Render(IEnumerable<KdpRunLine> lines)
    {
        var sb = new StringBuilder();
        foreach (var l in lines) sb.Append(Stamp(l.At, l.Message)).Append(NewLine);
        return sb.ToString();
    }

    public sealed record ParsedLine(DateTimeOffset At, string Message);
    public sealed record ParsedLog(Guid? RunId, List<string> Codes, List<ParsedLine> Lines);

    public static ParsedLog Parse(string text)
    {
        if (text.EndsWith("\r\n", StringComparison.Ordinal)) text = text[..^2];
        else if (text.EndsWith('\n')) text = text[..^1];

        var lines = new List<ParsedLine>();
        foreach (var entry in EntrySplit.Split(text))
        {
            var m = EntryStart.Match(entry);
            if (!m.Success)
            {
                // Text before the first stamp: fold into a line of its own at the run's start.
                if (entry.Length > 0) lines.Add(new ParsedLine(lines.Count > 0 ? lines[^1].At : DateTimeOffset.MinValue, entry));
                continue;
            }
            var at = DateTimeOffset.ParseExact(m.Groups["at"].Value, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
            lines.Add(new ParsedLine(at, entry[m.Length..]));
        }

        Guid? runId = null;
        var codes = new List<string>();
        if (lines.Count > 0 && StartedLine.Match(lines[0].Message) is { Success: true } s)
        {
            runId = Guid.Parse(s.Groups["id"].Value);
            codes = s.Groups["codes"].Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        }
        return new ParsedLog(runId, codes, lines);
    }
}
