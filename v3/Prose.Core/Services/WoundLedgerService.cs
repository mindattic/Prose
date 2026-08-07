using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;

namespace Prose.Core.Services;

/// <summary>One wound on a character's literal body map.</summary>
public sealed record WoundRow(
    long Id, Guid CharacterId, string BodyLocation, string Description, string Severity,
    string? SourceNodeSlug, Guid? SourceBeatId, DateTime? InWorldDate,
    int ExpectedHealingDays, string Status, string ResidualEffect);

/// <summary>
/// The literal body map (user directive 2026-06-10): wounds are story-state with healing
/// curves. Pixel patching a wound does not heal it; the prose must show Kyle favoring a
/// limb and calling back to the event that cost it. The X-Ray assembler injects each
/// character's ACTIVE wounds into every prose prompt (see SceneContextAssembler).
/// Status flow: fresh → healing → scarred (permanent marks graduate to CharacterPhysicalMarks).
/// </summary>
public class WoundLedgerService(
    IDbContextFactory<ProseDbContext> dbFactory,
    ILogger<WoundLedgerService> log)
{
    protected bool schemaEnsured;

    protected virtual async Task EnsureSchemaAsync(CancellationToken ct)
    {
        if (schemaEnsured) return;
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        await db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'[dbo].[WoundLedger]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[WoundLedger] (
                    [Id]                  BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [CharacterId]         UNIQUEIDENTIFIER NOT NULL,
                    [BodyLocation]        NVARCHAR(120)  NOT NULL,
                    [Description]         NVARCHAR(500)  NOT NULL,
                    [Severity]            NVARCHAR(20)   NOT NULL,
                    [SourceNodeSlug]    NVARCHAR(200)  NULL,
                    [SourceBeatId]        UNIQUEIDENTIFIER NULL,
                    [InWorldDate]         DATETIME2      NULL,
                    [ExpectedHealingDays] INT            NOT NULL DEFAULT 14,
                    [Status]              NVARCHAR(20)   NOT NULL DEFAULT 'fresh',
                    [ResidualEffect]      NVARCHAR(500)  NOT NULL DEFAULT '',
                    [CreatedAt]           DATETIME2      NOT NULL DEFAULT SYSUTCDATETIME()
                );
                CREATE INDEX [IX_WoundLedger_Character] ON [dbo].[WoundLedger]([CharacterId],[Status]);
            END;
            """, ct);
        schemaEnsured = true;
    }

    public virtual async Task<long> AddAsync(
        Guid characterId, string bodyLocation, string description, string severity,
        string? sourceNodeSlug = null, Guid? sourceBeatId = null, DateTime? inWorldDate = null,
        int expectedHealingDays = 14, string status = "fresh", string residualEffect = "",
        CancellationToken ct = default)
    {
        await EnsureSchemaAsync(ct);
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        await db.Database.ExecuteSqlRawAsync("""
            INSERT INTO [dbo].[WoundLedger]
                ([CharacterId],[BodyLocation],[Description],[Severity],[SourceNodeSlug],[SourceBeatId],[InWorldDate],[ExpectedHealingDays],[Status],[ResidualEffect])
            VALUES ({0},{1},{2},{3},{4},{5},{6},{7},{8},{9})
            """, [characterId, bodyLocation, description, severity, (object?)sourceNodeSlug ?? DBNull.Value, (object?)sourceBeatId ?? DBNull.Value, (object?)inWorldDate ?? DBNull.Value, expectedHealingDays, status, residualEffect], ct);
        log.LogInformation("Wound logged: {Char} {Loc} ({Sev})", characterId, bodyLocation, severity);
        await using var db2 = await dbFactory.CreateDbContextAsync(ct);
        return await db2.Database.SqlQueryRaw<long>("SELECT MAX(Id) AS [Value] FROM [dbo].[WoundLedger]").FirstAsync(ct);
    }

    /// <summary>Active (non-scarred) wounds, optionally as-of an in-world date: a wound is
    /// active at date D when it has no date (assume current) or D is within its healing window.
    /// Status overrides date math when set to 'scarred' or 'healed'.</summary>
    public virtual async Task<List<WoundRow>> GetActiveAsync(Guid characterId, DateTime? atInWorldDate = null, CancellationToken ct = default)
    {
        await EnsureSchemaAsync(ct);
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var rows = await db.Database.SqlQueryRaw<WoundRow>("""
            SELECT [Id],[CharacterId],[BodyLocation],[Description],[Severity],
                   [SourceNodeSlug],[SourceBeatId],[InWorldDate],[ExpectedHealingDays],[Status],[ResidualEffect]
            FROM [dbo].[WoundLedger] WHERE [CharacterId] = {0} AND [Status] NOT IN ('scarred','healed')
            """, characterId).ToListAsync(ct);
        if (atInWorldDate.HasValue)
            rows = rows.Where(w => w.InWorldDate == null
                || (atInWorldDate.Value >= w.InWorldDate.Value
                    && atInWorldDate.Value <= w.InWorldDate.Value.AddDays(w.ExpectedHealingDays))).ToList();
        return rows;
    }

    /// <summary>The prompt block the X-Ray assembler appends to a character's entry.</summary>
    public async Task<string> BuildPromptBlockAsync(Guid characterId, DateTime? atInWorldDate = null, CancellationToken ct = default)
    {
        var wounds = await GetActiveAsync(characterId, atInWorldDate, ct);
        if (wounds.Count == 0) return "";
        var lines = wounds.Select(w =>
            $"- {w.BodyLocation}: {w.Description} ({w.Severity}{(w.SourceNodeSlug != null ? $", from {w.SourceNodeSlug}" : "")})" +
            (w.ResidualEffect.Length > 0 ? $" — {w.ResidualEffect}" : ""));
        return "ACTIVE WOUNDS — the body remembers; exertion costs, movement compensates, and callbacks to the wounding event are earned:\n"
            + string.Join("\n", lines);
    }

    public async Task<int> SetStatusAsync(long woundId, string status, CancellationToken ct = default)
    {
        await EnsureSchemaAsync(ct);
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.Database.ExecuteSqlRawAsync(
            "UPDATE [dbo].[WoundLedger] SET [Status] = {0} WHERE [Id] = {1}", [status, woundId], ct);
    }
}
