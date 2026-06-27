using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Services;

namespace StreetSamurai.UnitTests;

sealed class SqliteWoundLedgerService : WoundLedgerService
{
    readonly IDbContextFactory<StreetSamuraiDbContext> ownFactory;

    public SqliteWoundLedgerService(IDbContextFactory<StreetSamuraiDbContext> dbFactory)
        : base(dbFactory, NullLogger<WoundLedgerService>.Instance)
    {
        ownFactory = dbFactory;
    }

    protected override async Task EnsureSchemaAsync(CancellationToken ct)
    {
        if (schemaEnsured) return;
        await using var db = await ownFactory.CreateDbContextAsync(ct);
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS WoundLedger (
                Id                  INTEGER PRIMARY KEY AUTOINCREMENT,
                CharacterId         TEXT    NOT NULL,
                BodyLocation        TEXT    NOT NULL,
                Description         TEXT    NOT NULL,
                Severity            TEXT    NOT NULL,
                SourceStrandSlug    TEXT    NULL,
                SourceBeatId        TEXT    NULL,
                InWorldDate         TEXT    NULL,
                ExpectedHealingDays INTEGER NOT NULL DEFAULT 14,
                Status              TEXT    NOT NULL DEFAULT 'fresh',
                ResidualEffect      TEXT    NOT NULL DEFAULT '',
                CreatedAt           TEXT    NOT NULL DEFAULT (datetime('now'))
            )
            """, ct);
        schemaEnsured = true;
    }

    public override async Task<long> AddAsync(
        Guid characterId, string bodyLocation, string description, string severity,
        string? sourceStrandSlug = null, Guid? sourceBeatId = null, DateTime? inWorldDate = null,
        int expectedHealingDays = 14, string status = "fresh", string residualEffect = "",
        CancellationToken ct = default)
    {
        await EnsureSchemaAsync(ct);
        await using var db = await ownFactory.CreateDbContextAsync(ct);
        await using var cmd = db.Database.GetDbConnection().CreateCommand();
        if (cmd.Connection!.State != System.Data.ConnectionState.Open)
            await cmd.Connection.OpenAsync(ct);

        cmd.CommandText = """
            INSERT INTO WoundLedger
                (CharacterId, BodyLocation, Description, Severity, SourceStrandSlug, SourceBeatId, InWorldDate, ExpectedHealingDays, Status, ResidualEffect)
            VALUES
                (@charId, @loc, @desc, @sev, @slug, @beatId, @date, @days, @status, @residual)
            """;

        void AddParam(string name, object? value)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = name;
            p.Value = value ?? DBNull.Value;
            cmd.Parameters.Add(p);
        }

        AddParam("@charId", characterId.ToString());
        AddParam("@loc", bodyLocation);
        AddParam("@desc", description);
        AddParam("@sev", severity);
        AddParam("@slug", sourceStrandSlug);
        AddParam("@beatId", sourceBeatId?.ToString());
        AddParam("@date", inWorldDate?.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
        AddParam("@days", expectedHealingDays);
        AddParam("@status", status);
        AddParam("@residual", residualEffect);

        await cmd.ExecuteNonQueryAsync(ct);

        cmd.CommandText = "SELECT last_insert_rowid()";
        cmd.Parameters.Clear();
        return (long)(await cmd.ExecuteScalarAsync(ct))!;
    }

    public override async Task<List<WoundRow>> GetActiveAsync(Guid characterId, DateTime? atInWorldDate = null, CancellationToken ct = default)
    {
        await EnsureSchemaAsync(ct);
        await using var db = await ownFactory.CreateDbContextAsync(ct);
        await using var cmd = db.Database.GetDbConnection().CreateCommand();
        if (cmd.Connection!.State != System.Data.ConnectionState.Open)
            await cmd.Connection.OpenAsync(ct);

        cmd.CommandText = """
            SELECT Id, CharacterId, BodyLocation, Description, Severity,
                   SourceStrandSlug, SourceBeatId, InWorldDate, ExpectedHealingDays, Status, ResidualEffect
            FROM WoundLedger
            WHERE CharacterId = @charId AND Status NOT IN ('scarred','healed')
            """;
        var p = cmd.CreateParameter();
        p.ParameterName = "@charId";
        p.Value = characterId.ToString();
        cmd.Parameters.Add(p);

        var rows = new List<WoundRow>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var idWound = reader.GetInt64(0);
            var charIdVal = Guid.Parse(reader.GetString(1));
            var loc = reader.GetString(2);
            var desc = reader.GetString(3);
            var sev = reader.GetString(4);
            var slug = reader.IsDBNull(5) ? null : reader.GetString(5);
            Guid? beatId = reader.IsDBNull(6) ? null : Guid.Parse(reader.GetString(6));
            DateTime? date = reader.IsDBNull(7) ? null
                : DateTime.Parse(reader.GetString(7), null, System.Globalization.DateTimeStyles.RoundtripKind);
            var days = reader.GetInt32(8);
            var stat = reader.GetString(9);
            var res = reader.GetString(10);
            rows.Add(new WoundRow(idWound, charIdVal, loc, desc, sev, slug, beatId, date, days, stat, res));
        }

        if (atInWorldDate.HasValue)
            rows = rows.Where(w => w.InWorldDate == null
                || (atInWorldDate.Value >= w.InWorldDate.Value
                    && atInWorldDate.Value <= w.InWorldDate.Value.AddDays(w.ExpectedHealingDays))).ToList();

        return rows;
    }
}

[TestFixture]
public class WoundLedgerServiceTests
{
    SqliteConnection connection = null!;
    IDbContextFactory<StreetSamuraiDbContext> factory = null!;
    SqliteWoundLedgerService svc = null!;

    static readonly Guid CharId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public WoundLedgerServiceTests()
    {
        connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<StreetSamuraiDbContext>()
            .UseSqlite(connection)
            .Options;

        using var ctx = new StreetSamuraiDbContext(options);
        ctx.Database.EnsureCreated();

        factory = new PinnedSqliteFactory(options);
        svc = new SqliteWoundLedgerService(factory);
    }

    [OneTimeTearDown]
    public void TearDownAll()
    {
        connection.Close();
        connection.Dispose();
    }

    async Task<long> AddFresh(
        string bodyLocation = "left arm",
        string description = "gunshot wound",
        string severity = "serious",
        string? sourceStrandSlug = null,
        DateTime? inWorldDate = null,
        int expectedHealingDays = 14,
        string status = "fresh",
        string residualEffect = "") =>
        await svc.AddAsync(CharId, bodyLocation, description, severity,
            sourceStrandSlug, sourceBeatId: null, inWorldDate, expectedHealingDays, status, residualEffect);

    sealed class PinnedSqliteFactory(DbContextOptions<StreetSamuraiDbContext> options)
        : IDbContextFactory<StreetSamuraiDbContext>
    {
        public StreetSamuraiDbContext CreateDbContext() => new(options);
        public Task<StreetSamuraiDbContext> CreateDbContextAsync(CancellationToken ct = default)
            => Task.FromResult(new StreetSamuraiDbContext(options));
    }

    [Test]
    public async Task NoWounds_BuildPromptBlock_ReturnsEmpty()
    {
        var charId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var result = await svc.BuildPromptBlockAsync(charId);
        Assert.That(result, Is.EqualTo(""));
    }

    [Test]
    public async Task FreshWound_NoSourceNoResidual_BlockContainsLocationAndDescription()
    {
        var charId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        await svc.AddAsync(charId, "right knee", "deep laceration", "moderate");

        var block = await svc.BuildPromptBlockAsync(charId);

        Assert.That(block, Does.Contain("right knee"));
        Assert.That(block, Does.Contain("deep laceration"));
        var woundLine = block.Split('\n').First(l => l.Contains("right knee"));
        Assert.That(woundLine, Does.Not.Contain("from "));
        Assert.That(woundLine, Does.Not.Contain(" — "));
    }

    [Test]
    public async Task WoundWithSourceStrandSlug_BlockContainsSource()
    {
        var charId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        await svc.AddAsync(charId, "left shoulder", "shrapnel", "severe", sourceStrandSlug: "BCODA");

        var block = await svc.BuildPromptBlockAsync(charId);

        Assert.That(block, Does.Contain("from BCODA"));
    }

    [Test]
    public async Task WoundWithResidualEffect_BlockContainsResidual()
    {
        var charId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        await svc.AddAsync(charId, "right hand", "nerve damage", "serious",
            residualEffect: "grip strength halved");

        var block = await svc.BuildPromptBlockAsync(charId);

        Assert.That(block, Does.Contain("grip strength halved"));
    }

    [Test]
    public async Task WoundWithStatusScarred_ExcludedFromBlock()
    {
        var charId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
        await svc.AddAsync(charId, "chin", "old cut", "minor", status: "scarred");

        var block = await svc.BuildPromptBlockAsync(charId);

        Assert.That(block, Is.EqualTo(""));
    }

    [Test]
    public async Task WoundWithStatusHealed_ExcludedFromBlock()
    {
        var charId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");
        await svc.AddAsync(charId, "torso", "bruising", "minor", status: "healed");

        var block = await svc.BuildPromptBlockAsync(charId);

        Assert.That(block, Is.EqualTo(""));
    }

    [Test]
    public async Task GetActive_NoAtDate_ReturnsAllNonScarredHealed()
    {
        var charId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        await svc.AddAsync(charId, "neck", "razor nick", "minor", status: "fresh");
        await svc.AddAsync(charId, "back", "old scar", "scarred", status: "scarred");
        await svc.AddAsync(charId, "ankle", "sprain", "moderate", status: "healing");

        var active = await svc.GetActiveAsync(charId);

        Assert.That(active.Count, Is.EqualTo(2));
        Assert.That(active, Has.None.Matches<WoundRow>(w => w.Status is "scarred" or "healed"));
    }

    [Test]
    public async Task GetActive_AtDateExactlyEqualsInWorldDate_WoundIsActive()
    {
        var charId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var woundDate = new DateTime(2153, 4, 10, 0, 0, 0, DateTimeKind.Utc);
        await svc.AddAsync(charId, "left leg", "bone fracture", "serious", inWorldDate: woundDate, expectedHealingDays: 30);

        var active = await svc.GetActiveAsync(charId, atInWorldDate: woundDate);

        Assert.That(active, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task GetActive_AtDateEqualsInWorldDatePlusHealingDays_WoundIsActive()
    {
        var charId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var woundDate = new DateTime(2153, 4, 10, 0, 0, 0, DateTimeKind.Utc);
        await svc.AddAsync(charId, "ribcage", "cracked ribs", "serious", inWorldDate: woundDate, expectedHealingDays: 21);

        var atDate = woundDate.AddDays(21);
        var active = await svc.GetActiveAsync(charId, atInWorldDate: atDate);

        Assert.That(active, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task GetActive_AtDateOneDayAfterHealingWindow_WoundExcluded()
    {
        var charId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var woundDate = new DateTime(2153, 4, 10, 0, 0, 0, DateTimeKind.Utc);
        await svc.AddAsync(charId, "right eye", "retinal tear", "severe", inWorldDate: woundDate, expectedHealingDays: 14);

        var atDate = woundDate.AddDays(15);
        var active = await svc.GetActiveAsync(charId, atInWorldDate: atDate);

        Assert.That(active, Is.Empty);
    }

    [Test]
    public async Task GetActive_AtDateBeforeInWorldDate_WoundExcluded()
    {
        var charId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        var woundDate = new DateTime(2153, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        await svc.AddAsync(charId, "forearm", "burn", "moderate", inWorldDate: woundDate, expectedHealingDays: 7);

        var atDate = woundDate.AddDays(-1);
        var active = await svc.GetActiveAsync(charId, atInWorldDate: atDate);

        Assert.That(active, Is.Empty);
    }

    [Test]
    public async Task GetActive_InWorldDateNull_WithAtDate_WoundIncluded()
    {
        var charId = Guid.Parse("66666666-6666-6666-6666-666666666666");
        await svc.AddAsync(charId, "chest", "blunt trauma", "moderate", inWorldDate: null);

        var atDate = new DateTime(2153, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var active = await svc.GetActiveAsync(charId, atInWorldDate: atDate);

        Assert.That(active, Has.Count.EqualTo(1));
    }
}
