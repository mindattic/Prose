using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StreetSamurai.Core.Migrations
{
    /// <summary>
    /// Last line of defence for Beats.TextHash — the value NodeReviewService uses to decide
    /// which beats changed since they were last scored.
    ///
    /// StreetSamuraiDbContext.StampBeatTextHash() keeps the hash correct for every write that
    /// goes through EF. It cannot help writes that DON'T: sqlcmd, a PowerShell script, Dapper,
    /// an ad-hoc session, ExecuteUpdate. Those bypass the change tracker entirely, and they are
    /// what produced the 185 drifted beats found on 2026-07-29 (the gripe-cut batches and
    /// direct prose injection).
    ///
    /// This trigger closes that hole at the only layer every writer must pass through.
    ///
    /// It deliberately does NOT recompute the hash. Matching .NET's string.Trim() in T-SQL is
    /// not reliable — Trim() strips the whole Unicode whitespace set, LTRIM/RTRIM historically
    /// only spaces — so a T-SQL-computed hash could disagree with the application's and mark
    /// every affected beat as permanently changed. Instead, when Text moves and TextHash does
    /// not, the hash is set to NULL.
    ///
    /// NULL is the fail-safe direction: it can never compare equal to the hash recorded at
    /// review time, so the beat reads as CHANGED and gets re-reviewed. The failure mode being
    /// eliminated is the opposite one — a stale hash comparing equal and silently keeping a
    /// score that was awarded to different words.
    ///
    /// Safe on this table: Beats is system-versioned (AFTER triggers are permitted), the
    /// database has RECURSIVE_TRIGGERS OFF so the trigger's own UPDATE cannot re-fire it, and
    /// Beats carries no rowversion, so there is no optimistic-concurrency token for the extra
    /// write to invalidate. For EF writes the WHERE clause never matches, because the context
    /// has already stamped the correct hash — so this is a no-op on the happy path.
    /// </summary>
    public partial class AddBeatTextHashDriftGuardTrigger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID('dbo.TR_Beats_TextHashDriftGuard', 'TR') IS NOT NULL
    DROP TRIGGER dbo.TR_Beats_TextHashDriftGuard;
");
            migrationBuilder.Sql(@"
CREATE TRIGGER dbo.TR_Beats_TextHashDriftGuard
ON dbo.Beats
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- Only interested in prose edits.
    IF NOT UPDATE(Text) RETURN;

    -- Rows whose Text moved while TextHash stayed exactly as it was: the writer forgot.
    -- NULL the hash so the drift is loud (forces re-review) instead of silent.
    UPDATE b
       SET b.TextHash = NULL
      FROM dbo.Beats AS b
      JOIN inserted AS i ON i.Id = b.Id
      JOIN deleted  AS d ON d.Id = b.Id
     WHERE ISNULL(i.Text, N'') <> ISNULL(d.Text, N'')
       AND ISNULL(i.TextHash, N'~') = ISNULL(d.TextHash, N'~')
       AND b.TextHash IS NOT NULL;
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID('dbo.TR_Beats_TextHashDriftGuard', 'TR') IS NOT NULL
    DROP TRIGGER dbo.TR_Beats_TextHashDriftGuard;
");
        }
    }
}
