using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Kdp;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// The KDP store (machine-local SQLite, <see cref="KdpDbContext"/>) — the real migrations against
/// an in-memory database, the crosswalk and publish-record APIs, the JSON import→export round trip
/// against the repo's own tools/kdp files and every legacy .publish marker variant found on disk,
/// and the one-time first-run import.
/// </summary>
[TestFixture]
public class KdpStoreTests
{
    private KdpTestDb db = null!;
    private string temp = null!;

    [SetUp]
    public void SetUp()
    {
        db = new KdpTestDb();
        temp = Path.Combine(Path.GetTempPath(), "kdp-store-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
    }

    [TearDown]
    public void TearDown()
    {
        db.Dispose();
        SqliteConnection.ClearAllPools();
        try { Directory.Delete(temp, recursive: true); } catch { /* best effort */ }
    }

    // ── Schema ────────────────────────────────────────────────────────────

    [Test]
    public void Migrations_apply_to_an_empty_database_and_match_the_model()
    {
        using var ctx = db.CreateDbContext();
        Assert.That(ctx.Database.GetAppliedMigrations(), Has.Some.EndsWith("_InitialKdpStore"));
        Assert.That(ctx.Database.GetPendingMigrations(), Is.Empty);
        Assert.That(ctx.Database.HasPendingModelChanges(), Is.False,
            "The KdpDbContext model has changed since the last migration — add one with " +
            "`dotnet ef migrations add <Name> --context KdpDbContext --output-dir Kdp/Migrations`.");
    }

    [Test]
    public void Native_sqlite_is_patched_against_GHSA_2m69_gcr7_jv3q()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "select sqlite_version()";
        var version = Version.Parse((string)cmd.ExecuteScalar()!);
        Assert.That(version, Is.GreaterThanOrEqualTo(new Version(3, 50, 2)));
    }

    [Test]
    public async Task A_file_database_named_by_the_env_var_is_created_with_its_folder_and_WAL()
    {
        var path = Path.Combine(temp, "nested", "kdp.db");
        var old = Environment.GetEnvironmentVariable(KdpPaths.DbPathEnvVar);
        Environment.SetEnvironmentVariable(KdpPaths.DbPathEnvVar, path);
        try
        {
            Assert.That(KdpPaths.ResolveDbPath(), Is.EqualTo(path));
            var options = new DbContextOptionsBuilder<KdpDbContext>().UseSqlite(KdpPaths.ConnectionString(KdpPaths.ResolveDbPath())).Options;
            var factory = new OptionsFactory(options);
            await KdpMigrator.EnsureMigratedAsync(factory);

            Assert.That(File.Exists(path), Is.True);
            await using var ctx = factory.CreateDbContext();
            var conn = ctx.Database.GetDbConnection();
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "PRAGMA journal_mode;";
            Assert.That(await cmd.ExecuteScalarAsync(), Is.EqualTo("wal"));
        }
        finally
        {
            Environment.SetEnvironmentVariable(KdpPaths.DbPathEnvVar, old);
        }
    }

    [Test]
    public void The_default_database_lives_in_local_app_data()
    {
        Assert.That(KdpPaths.DefaultDbPath, Does.EndWith(Path.Combine("MindAttic", "Prose", "kdp.db")));
        Assert.That(KdpPaths.DefaultDbPath, Does.StartWith(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)));
    }

    // ── Crosswalk ─────────────────────────────────────────────────────────

    [Test]
    public async Task Title_ids_round_trip_and_a_null_argument_keeps_the_stored_value()
    {
        var store = NewStore();
        await store.UpsertTitleAsync("MxG", "A3QU9VRZ6MRE8E", "B0H6PCBVTD");
        await store.UpsertTitleAsync("PXL", "A3UOC59UVPVQA7", null);
        await store.UpsertTitleAsync("MxG", null, "B0NEWASIN1");

        var titles = await store.GetTitlesAsync();
        Assert.That(titles.Keys, Is.EquivalentTo(new[] { "MxG", "PXL" }));
        Assert.That(titles["MxG"].TitleId, Is.EqualTo("A3QU9VRZ6MRE8E"));
        Assert.That(titles["MxG"].Asin, Is.EqualTo("B0NEWASIN1"));
        Assert.That(titles["PXL"].Asin, Is.Null);
        Assert.That(titles["PXL"].SortOrder, Is.GreaterThan(titles["MxG"].SortOrder));

        // NodeCodes are case-sensitive keys, exactly as they were in title-ids.json.
        Assert.That(await store.GetTitleAsync("MXG"), Is.Null);
    }

    // ── Publish records ───────────────────────────────────────────────────

    [Test]
    public async Task A_publish_record_round_trips_signs_off_a_new_book_and_keeps_history()
    {
        var clock = new TestClock(new DateTimeOffset(2026, 9, 23, 12, 0, 0, TimeSpan.Zero));
        var store = NewStore(clock);
        var publishedAt = new DateTimeOffset(2026, 8, 16, 16, 37, 14, TimeSpan.Zero).AddTicks(269879);

        await store.RecordPublishAsync("TFAH", new KdpPublishSnapshot { File = "TFAH V38.epub", Version = 38, Asin = null, PublishedAt = publishedAt }, KdpPublishSource.MarkPublished);

        var book = await store.GetBookAsync("TFAH", withHistory: true);
        Assert.That(book, Is.Not.Null);
        Assert.That(book!.SignOff.Ready, Is.True, "writing a publish record signs a new book off, as writing its .publish marker did");
        Assert.That(book.LastPublish.File, Is.EqualTo("TFAH V38.epub"));
        Assert.That(book.LastPublish.Version, Is.EqualTo(38));
        Assert.That(book.LastPublish.PublishedAt, Is.EqualTo(publishedAt), "timestamps keep full tick precision");
        Assert.That(book.History, Has.Count.EqualTo(1));

        // The operator's post-publish hook confirms the same manuscript seconds later: folded in.
        clock.Now = clock.Now.AddSeconds(5);
        await store.RecordPublishAsync("TFAH", new KdpPublishSnapshot { File = "TFAH V38.epub", Asin = "B0TFAH0001", PublishedAt = clock.Now }, KdpPublishSource.Operator);
        book = await store.GetBookAsync("TFAH", withHistory: true);
        Assert.That(book!.History, Has.Count.EqualTo(1));
        Assert.That(book.LastPublish.Asin, Is.EqualTo("B0TFAH0001"));
        Assert.That(book.LastPublish.Version, Is.EqualTo(38));
        Assert.That(book.LastPublish.PublishedAt, Is.EqualTo(publishedAt), "the first confirmation time stands");

        // KDP's "Updates publishing" window, then the next version goes live and clears it.
        await store.MarkPublishingDetectedAsync("TFAH");
        Assert.That((await store.GetBookAsync("TFAH"))!.PublishingDetectedAt, Is.EqualTo(clock.Now));

        clock.Now = clock.Now.AddDays(3);
        await store.RecordPublishAsync("TFAH", new KdpPublishSnapshot { File = "TFAH V39.epub", Version = 39, PublishedAt = clock.Now }, KdpPublishSource.MarkPublished);
        book = await store.GetBookAsync("TFAH", withHistory: true);
        Assert.That(book!.History.Select(h => h.Publish.File), Is.EqualTo(new[] { "TFAH V38.epub", "TFAH V39.epub" }));
        Assert.That(book.LastPublish.Version, Is.EqualTo(39));
        Assert.That(book.PublishingDetectedAt, Is.Null);
    }

    [Test]
    public async Task Sign_off_and_hold_are_explicit_and_a_publish_does_not_undo_a_hold()
    {
        var store = NewStore();
        Assert.That(await store.SetSignOffAsync(["ATTE", "BLST"], ready: true, "test"), Is.EqualTo(2));
        Assert.That(await store.SetSignOffAsync(["ATTE"], ready: true, "test"), Is.EqualTo(0), "no change, nothing counted");
        await store.SetSignOffAsync(["BLST"], ready: false, "test");

        await store.RecordPublishAsync("BLST", new KdpPublishSnapshot { File = "BLST V26.epub", PublishedAt = DateTimeOffset.UtcNow }, KdpPublishSource.MarkPublished);

        var books = await store.GetBooksAsync();
        Assert.That(books["ATTE"].SignOff.Ready, Is.True);
        Assert.That(books["BLST"].SignOff.Ready, Is.False);
        Assert.That(books["BLST"].SignOff.ChangedBy, Is.EqualTo("test"));
    }

    [Test]
    public void The_manifest_sees_the_store_as_the_same_PublishMarker_shape()
    {
        var book = new KdpBook
        {
            Code = "ATTE",
            LastPublish = new KdpPublishSnapshot { File = "ATTE V74.epub", Asin = "B0H5937163", PublishedAt = KdpJsonTransfer.ParseUtc("2026-08-16T16:04:39.6662280Z") },
        };
        var marker = KdpJsonTransfer.ToPublishMarker(book);
        Assert.That(marker, Is.EqualTo(new PublishMarker("ATTE V74.epub", "B0H5937163", "2026-08-16T16:04:39.6662280Z", null, null)));
        Assert.That(KdpJsonTransfer.ToPublishMarker(new KdpBook { Code = "X" }), Is.Null, "signed off, never published: no marker body");
        Assert.That(KdpJsonTransfer.ToPublishMarker(null), Is.Null);
    }

    // ── Run logs ──────────────────────────────────────────────────────────

    [Test]
    public async Task The_run_log_service_writes_the_run_and_its_lines_to_the_store_and_the_text_log()
    {
        var store = NewStore();
        var service = new KdpRunLogService(store);
        var runId = service.StartRun(temp, ["TLC", "MxG"]);
        await service.LogAsync(runId, "TLC: multi-line\nnote");
        await service.LogAsync(runId, KdpRunLogFormat.FinishedMessage);
        await service.FinishRunAsync(runId);

        await using var ctx = db.CreateDbContext();
        var run = await ctx.Runs.Include(r => r.Lines).SingleAsync();
        Assert.That(run.Id, Is.EqualTo(runId));
        Assert.That(run.Codes, Is.EqualTo(new[] { "TLC", "MxG" }));
        Assert.That(run.FinishedAt, Is.Not.Null);
        Assert.That(run.Lines.OrderBy(l => l.Id).Select(l => l.Message),
            Is.EqualTo(new[] { KdpRunLogFormat.StartedMessage(runId, ["TLC", "MxG"]), "TLC: multi-line\nnote", KdpRunLogFormat.FinishedMessage }));

        var logFile = Path.Combine(temp, "tools", "kdp", "logs", run.LogFileName!);
        var parsed = KdpRunLogFormat.Parse(await File.ReadAllTextAsync(logFile));
        Assert.That(parsed.RunId, Is.EqualTo(runId));
        Assert.That(parsed.Lines.Select(l => l.Message), Is.EqualTo(run.Lines.OrderBy(l => l.Id).Select(l => l.Message)));
    }

    // ── JSON import / export ──────────────────────────────────────────────

    [Test]
    public async Task Import_then_export_reproduces_the_original_files()
    {
        var source = BuildLegacyFixture(out var bookFolders);
        var transfer = NewTransfer();

        var imported = await transfer.ImportAsync(new KdpImportRequest { FromDir = source, LegacyMarkerFolders = bookFolders });
        Assert.That(imported.Titles, Is.EqualTo(RepoTitleIdCount()));
        Assert.That(imported.Books, Is.EqualTo(LegacyMarkers.Count));
        Assert.That(imported.PublishRecords, Is.EqualTo(3), "TWD, ATTE and MxG carry a confirmed PublishedAtUtc");
        Assert.That(imported.CategoryTrees, Is.EqualTo(2));
        Assert.That(imported.Runs, Is.EqualTo(1));

        var export = Path.Combine(temp, "export");
        var inPlace = Path.Combine(temp, "books-out");
        var outFolders = bookFolders.Select(f => new KdpBookFolder(f.Code, Path.Combine(inPlace, f.Code))).ToList();
        outFolders.ForEach(f => Directory.CreateDirectory(f.FolderPath));
        await transfer.ExportAsync(new KdpExportRequest { ToDir = export, MarkersInPlace = outFolders });

        // Byte-for-byte: the hand-maintained crosswalk (with its _comment and escapes), every
        // category tree, and the run log.
        AssertSameBytes(Path.Combine(source, "title-ids.json"), Path.Combine(export, "title-ids.json"));
        foreach (var tree in Directory.GetFiles(source, "category-tree-*.json"))
            AssertSameBytes(tree, Path.Combine(export, Path.GetFileName(tree)));
        AssertSameBytes(Path.Combine(source, "logs", LogFileName), Path.Combine(export, "logs", LogFileName));

        // Markers: same shape (PublishMarker), same values; an empty marker stays empty.
        foreach (var (code, original) in LegacyMarkers)
        {
            foreach (var exported in new[] { Path.Combine(export, "publish-markers", code + ".publish"), Path.Combine(inPlace, code, ".publish") })
            {
                var text = await File.ReadAllTextAsync(exported);
                if (original.Length == 0) { Assert.That(text, Is.Empty, code); continue; }
                Assert.That(ReadMarker(text), Is.EqualTo(ReadMarker(original)), code);
            }
        }

        // And the export imports back to the same state, without duplicating history.
        var before = await Snapshot();
        await NewTransfer().ImportAsync(new KdpImportRequest { FromDir = export });
        Assert.That(await Snapshot(), Is.EqualTo(before));
    }

    [Test]
    public async Task A_held_book_exports_under_held_and_imports_back_held()
    {
        var store = NewStore();
        await store.RecordPublishAsync("QRT", new KdpPublishSnapshot { File = "QRT V5.epub", PublishedAt = DateTimeOffset.UtcNow }, KdpPublishSource.MarkPublished);
        await store.SetSignOffAsync(["QRT"], ready: false, "test");

        var export = Path.Combine(temp, "export");
        await NewTransfer().ExportAsync(new KdpExportRequest { ToDir = export });
        Assert.That(File.Exists(Path.Combine(export, "publish-markers", "held", "QRT.publish")), Is.True);
        Assert.That(File.Exists(Path.Combine(export, "publish-markers", "QRT.publish")), Is.False);

        using var other = new KdpTestDb();
        await new KdpJsonTransfer(other, new FakeFolders([]), new KdpTransferOptions { AutoImport = false })
            .ImportAsync(new KdpImportRequest { FromDir = export });
        await using var ctx = other.CreateDbContext();
        var book = await ctx.Books.SingleAsync();
        Assert.That(book.SignOff.Ready, Is.False);
        Assert.That(book.LastPublish.File, Is.EqualTo("QRT V5.epub"));
    }

    [Test]
    public void Run_logs_parse_multi_line_messages_and_render_back_exactly()
    {
        var text = LegacyLog;
        var parsed = KdpRunLogFormat.Parse(text);
        Assert.That(parsed.RunId, Is.EqualTo(Guid.Parse("019fd01e-bec0-796d-b1d9-a34f6a9c4df4")));
        Assert.That(parsed.Codes, Is.EqualTo(new[] { "TLC" }));
        Assert.That(parsed.Lines, Has.Count.EqualTo(5));
        Assert.That(parsed.Lines[3].Message, Does.Contain("\nRepublish to V26 completed cleanly."));
        var rendered = KdpRunLogFormat.Render(parsed.Lines.Select(l => new KdpRunLine { At = l.At, Message = l.Message }));
        Assert.That(rendered, Is.EqualTo(text));
    }

    // ── First-run import ──────────────────────────────────────────────────

    [Test]
    public async Task The_first_use_of_an_empty_store_imports_the_legacy_files_once_and_leaves_them_untouched()
    {
        var source = BuildLegacyFixture(out var bookFolders);
        var hashesBefore = HashTree(temp);
        var folders = new FakeFolders(bookFolders);
        var options = new KdpTransferOptions { ToolsDir = () => source };

        var store = new KdpStore(db, new KdpJsonTransfer(db, folders, options));
        var titles = await store.GetTitlesAsync();   // any first call brings the store up
        Assert.That(titles, Has.Count.EqualTo(RepoTitleIdCount()));
        Assert.That((await store.GetBooksAsync()).Values.Count(b => b.SignOff.Ready), Is.EqualTo(LegacyMarkers.Count),
            "every book that had a .publish marker is signed off");
        Assert.That(HashTree(temp), Is.EqualTo(hashesBefore), "the legacy files are the backup: never modified");

        var status = await store.GetStatusAsync();
        var firstRun = status.Imports.Single();
        Assert.That(firstRun.Kind, Is.EqualTo(KdpImportKind.FirstRun));
        Assert.That(firstRun.Counts.Titles, Is.EqualTo(RepoTitleIdCount()));
        Assert.That(firstRun.Source, Does.StartWith(source));

        // A later process (fresh store + transfer on the same database) must not import again,
        // even though the files are still there — and even after they change.
        await File.WriteAllTextAsync(Path.Combine(source, "title-ids.json"), """{ "NEWBOOK": { "titleId": "AXXXXXXXXXXXX" } }""");
        var later = new KdpStore(db, new KdpJsonTransfer(db, folders, options));
        await later.EnsureReadyAsync();
        Assert.That((await later.GetTitlesAsync()).ContainsKey("NEWBOOK"), Is.False);
        Assert.That((await later.GetStatusAsync()).Imports, Has.Count.EqualTo(1));
        Assert.That(folders.Calls, Is.EqualTo(1));
    }

    [Test]
    public async Task The_first_run_import_is_deferred_not_recorded_while_the_legacy_files_are_unreachable()
    {
        var source = Path.Combine(temp, "tools", "kdp");
        var options = new KdpTransferOptions { ToolsDir = () => source };
        var store = new KdpStore(db, new KdpJsonTransfer(db, new FakeFolders([]), options));

        await store.EnsureReadyAsync();
        Assert.That((await store.GetStatusAsync()).Imports, Is.Empty, "nothing found: nothing recorded");

        BuildLegacyFixture(out _);
        var restarted = new KdpStore(db, new KdpJsonTransfer(db, new FakeFolders([]), options));
        await restarted.EnsureReadyAsync();
        Assert.That((await restarted.GetStatusAsync()).Imports.Single().Kind, Is.EqualTo(KdpImportKind.FirstRun));
        Assert.That(await restarted.GetTitlesAsync(), Has.Count.EqualTo(RepoTitleIdCount()));
    }

    [Test]
    public async Task The_first_run_import_is_deferred_when_the_book_folders_cannot_be_listed()
    {
        var source = BuildLegacyFixture(out _);
        var store = new KdpStore(db, new KdpJsonTransfer(db, new FakeFolders([]) { Fail = true }, new KdpTransferOptions { ToolsDir = () => source }));
        await store.EnsureReadyAsync();
        Assert.That((await store.GetStatusAsync()).Imports, Is.Empty);
    }

    // ── Fixture ───────────────────────────────────────────────────────────

    private const string LogFileName = "kdp-run-20260805-041143.log";

    /// <summary>Every .publish marker shape found in the export folders on 2026-09-23.</summary>
    private static readonly Dictionary<string, string> LegacyMarkers = new()
    {
        ["TWD"] = """{"File":"TWD V13.epub","Asin":null,"PublishedAtUtc":"2026-08-04T03:05:07.0026343Z"}""",
        ["ATTE"] = """{"File":"ATTE V74.epub","Asin":"B0H5937163","PublishedAtUtc":"2026-08-16T16:04:39.6662280Z","Version":null}""",
        ["MATTHEW"] = "{\n  \"File\": null,\n  \"Asin\": \"B0H8XR1WVF\",\n  \"PublishedAtUtc\": null,\n  \"Version\": null,\n  \"PublishingDetectedAtUtc\": \"2026-08-16T23:11:17.0000000Z\"\n}",
        ["MxG"] = "{\n  \"File\": \"MxG V37.epub\",\n  \"Asin\": \"B0H6PCBVTD\",\n  \"PublishedAtUtc\": \"2026-08-16T16:26:44.9452873Z\",\n  \"Version\": 37\n}",
        ["LDGR"] = "",
    };

    private static readonly string LegacyLog = string.Join("\r\n",
        "[2026-08-05 04:11:43Z] === Run started (019fd01e-bec0-796d-b1d9-a34f6a9c4df4): TLC ===",
        "[2026-08-05 04:11:43Z] Starting run: 1 book(s) — TLC",
        "[2026-08-05 04:11:46Z] TLC: → find_and_open_book({\"title\":\"The Long Cut\",\"known_title_id\":\"A1CG8SZ09ONK9A\"})",
        "[2026-08-05 04:13:30Z] TLC: Done.\n\nRepublish to V26 completed cleanly.",
        "[2026-08-05 04:13:34Z] Run finished.") + "\r\n";

    /// <summary>A tools/kdp folder built from the repo's own committed files (title-ids.json and
    /// both category trees, copied verbatim) plus a run log, and one book folder per legacy marker.</summary>
    private string BuildLegacyFixture(out List<KdpBookFolder> bookFolders)
    {
        var repoKdp = Path.Combine(KdpManifestService.FindRepoRoot(TestContext.CurrentContext.TestDirectory), "tools", "kdp");
        var source = Path.Combine(temp, "tools", "kdp");
        Directory.CreateDirectory(Path.Combine(source, "logs"));
        File.Copy(Path.Combine(repoKdp, "title-ids.json"), Path.Combine(source, "title-ids.json"));
        foreach (var tree in Directory.GetFiles(repoKdp, "category-tree-*.json"))
            File.Copy(tree, Path.Combine(source, Path.GetFileName(tree)));
        File.WriteAllText(Path.Combine(source, "logs", LogFileName), LegacyLog);

        bookFolders = [];
        foreach (var (code, body) in LegacyMarkers)
        {
            var folder = Path.Combine(temp, "books", code);
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, ".publish"), body);
            bookFolders.Add(new KdpBookFolder(code, folder));
        }
        return source;
    }

    private static int RepoTitleIdCount()
    {
        var repoKdp = Path.Combine(KdpManifestService.FindRepoRoot(TestContext.CurrentContext.TestDirectory), "tools", "kdp");
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(repoKdp, "title-ids.json")));
        return doc.RootElement.EnumerateObject().Count(p => !p.Name.StartsWith('_'));
    }

    private static PublishMarker ReadMarker(string json) =>
        JsonSerializer.Deserialize<PublishMarker>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

    private static void AssertSameBytes(string expected, string actual)
    {
        Assert.That(File.Exists(actual), Is.True, actual);
        var a = File.ReadAllBytes(expected);
        var b = File.ReadAllBytes(actual);
        Assert.That(Encoding.UTF8.GetString(b), Is.EqualTo(Encoding.UTF8.GetString(a)), Path.GetFileName(expected));
    }

    private static Dictionary<string, string> HashTree(string root) =>
        Directory.GetFiles(root, "*", SearchOption.AllDirectories)
            .ToDictionary(f => Path.GetRelativePath(root, f), f => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(f))));

    private async Task<string> Snapshot()
    {
        await using var ctx = db.CreateDbContext();
        var titles = await ctx.Titles.OrderBy(t => t.Code).Select(t => new { t.Code, t.TitleId, t.Asin, t.ExtraJson, t.SortOrder }).ToListAsync();
        var books = (await ctx.Books.ToListAsync()).OrderBy(b => b.Code, StringComparer.Ordinal)
            .Select(b => new { b.Code, b.SignOff.Ready, b.LastPublish.File, b.LastPublish.Version, b.LastPublish.Asin, b.LastPublish.PublishedAt, b.PublishingDetectedAt });
        var history = (await ctx.PublishRecords.ToListAsync()).Select(r => new { r.Code, r.Publish.File, r.Publish.PublishedAt }).OrderBy(r => r.Code, StringComparer.Ordinal);
        var trees = (await ctx.CategoryTrees.ToListAsync()).Select(t => new { t.Slug, Tree = KdpJsonTransfer.RenderCategoryTree(t.Tree) });
        var runs = await ctx.Runs.CountAsync();
        var lines = await ctx.RunLines.CountAsync();
        return JsonSerializer.Serialize(new { titles, books, history, trees, runs, lines });
    }

    private KdpStore NewStore(TimeProvider? clock = null) =>
        new(db, new KdpJsonTransfer(db, new FakeFolders([]), new KdpTransferOptions { AutoImport = false }, clock), clock);

    private KdpJsonTransfer NewTransfer() =>
        new(db, new FakeFolders([]), new KdpTransferOptions { AutoImport = false });

    private sealed class FakeFolders(List<KdpBookFolder> folders) : IKdpBookFolderSource
    {
        public int Calls { get; private set; }
        public bool Fail { get; init; }
        public Task<IReadOnlyList<KdpBookFolder>> ListAsync(CancellationToken ct = default)
        {
            Calls++;
            if (Fail) throw new InvalidOperationException("Prose database unreachable");
            return Task.FromResult<IReadOnlyList<KdpBookFolder>>(folders);
        }
    }

    private sealed class OptionsFactory(DbContextOptions<KdpDbContext> options) : IDbContextFactory<KdpDbContext>
    {
        public KdpDbContext CreateDbContext() => new(options);
    }

    private sealed class TestClock(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = now;
        public override DateTimeOffset GetUtcNow() => Now;
    }
}

/// <summary>A real SQLite database in memory, brought up by the store's own migrations — tests
/// exercise the schema KdpPublish actually gets, not a model-only approximation.</summary>
internal sealed class KdpTestDb : IDbContextFactory<KdpDbContext>, IDisposable
{
    private readonly SqliteConnection connection = new("DataSource=:memory:");
    private readonly DbContextOptions<KdpDbContext> options;

    public KdpTestDb()
    {
        connection.Open();
        options = new DbContextOptionsBuilder<KdpDbContext>().UseSqlite(connection).Options;
        using var db = CreateDbContext();
        db.Database.Migrate();
    }

    public KdpDbContext CreateDbContext() => new(options);

    public void Dispose() => connection.Dispose();
}
