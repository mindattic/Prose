using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Interfaces;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// One-shot migration of the legacy SQLite continuity store
/// (<c>engine/data/continuity.db</c>) into the Prose SQL Server database
/// (<c>ContinuityClaims</c>, <c>ClaimConfirmations</c>, <c>ClaimContradictions</c>,
/// <c>ExtractionRuns</c>).
///
///   prose --migrate-sql --import-continuity-sqlite [--sqlite PATH]           report only
///   prose --migrate-sql --import-continuity-sqlite [--sqlite PATH] --apply   insert missing rows
///
/// Why SQL Server: continuity claims are canon facts about entities and chapters (owner ruling
/// 2026-10-22: SQLite only for simple needs; corporate-grade data lives on SQL Server), and the
/// runtime ContinuityService has read and written only SQL Server since the store was folded in.
///
/// Rules:
/// <list type="bullet">
/// <item>The SQLite file is opened read-only and never renamed, rewritten or deleted, so it stays
/// readable as an audit artefact.</item>
/// <item>Insert-only and idempotent: a row whose key already exists in SQL Server is left alone,
/// because SQL Server is authoritative (it holds every later status change, resolution and
/// provenance grade). Re-running after a full import reports zero missing rows.</item>
/// <item>Imported claims carry <see cref="ClaimProvenance.LegacyUnknown"/>, the grade every
/// earlier folded-in legacy claim carries.</item>
/// </list>
/// </summary>
public static class MigrateContinuitySqliteCli
{
    public const string Flag = "--import-continuity-sqlite";

    public static async Task<int> RunAsync(string[] args, IServiceProvider sp)
    {
        var apply = args.Contains("--apply");
        var path = ArgValue(args, "--sqlite") ?? DefaultPath(sp);

        Console.WriteLine();
        Console.WriteLine($"[import-continuity-sqlite] {(apply ? "APPLY" : "report only (pass --apply to insert)")}");
        Console.WriteLine($"  source: {path}");
        if (!File.Exists(path))
        {
            Console.WriteLine("  ✘ SQLite file not found. Nothing to migrate.");
            return 1;
        }

        using var conn = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = SqliteOpenMode.ReadOnly,
        }.ToString());
        conn.Open();

        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProseDbContext>();

        // ── claims ─────────────────────────────────────────────────────────
        var existingClaims = (await db.ContinuityClaims.IgnoreQueryFilters().AsNoTracking()
                .Select(c => c.ClaimUid).ToListAsync())
            .ToHashSet(StringComparer.Ordinal);
        var newClaims = new List<ContinuityClaim>();
        var claimRows = 0;
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = """
                SELECT claim_uid, entity_id, entity_name, entity_kind, predicate, object,
                       source_type, source_path, source_chapter_id, source_chapter_number,
                       source_chapter_title, snippet, voice, confidence, extracted_by, status,
                       first_asserted_at, last_confirmed_at, resolved_at, applied_at,
                       applied_to_field, superseded_by, resolution_note
                FROM claims;
                """;
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                claimRows++;
                var uid = r.GetString(0);
                if (existingClaims.Contains(uid)) continue;
                newClaims.Add(new ContinuityClaim
                {
                    ClaimUid            = uid,
                    EntityId            = Str(r, 1) ?? "",
                    EntityName          = Str(r, 2) ?? "",
                    EntityKind          = Str(r, 3) ?? "",
                    Predicate           = Str(r, 4) ?? "",
                    Object              = Str(r, 5) ?? "",
                    SourceType          = Str(r, 6) ?? "",
                    SourcePath          = Str(r, 7),
                    SourceChapterId     = Str(r, 8),
                    SourceChapterNumber = r.IsDBNull(9) ? null : Convert.ToInt32(r.GetValue(9)),
                    SourceChapterTitle  = Str(r, 10),
                    Snippet             = Str(r, 11),
                    Voice               = Str(r, 12),
                    Confidence          = Str(r, 13),
                    ExtractedBy         = ParseExtractedBy(Str(r, 14)),
                    Status              = Str(r, 15) ?? "NEW",
                    FirstAssertedAt     = Str(r, 16) ?? "",
                    LastConfirmedAt     = Str(r, 17) ?? "",
                    ResolvedAt          = Str(r, 18),
                    AppliedAt           = Str(r, 19),
                    AppliedToField      = Str(r, 20),
                    SupersededBy        = Str(r, 21),
                    ResolutionNote      = Str(r, 22),
                    Provenance          = ClaimProvenance.LegacyUnknown,
                });
                existingClaims.Add(uid);
            }
        }
        Report("claims", claimRows, newClaims.Count);

        // ── confirmations ──────────────────────────────────────────────────
        var existingConf = (await db.ClaimConfirmations.AsNoTracking()
                .Select(c => new { c.ClaimUid, c.SourceChapterId, c.SourcePath }).ToListAsync())
            .Select(c => Key(c.ClaimUid, c.SourceChapterId, c.SourcePath))
            .ToHashSet(StringComparer.Ordinal);
        var newConf = new List<ClaimConfirmationRow>();
        var confRows = 0;
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT claim_uid, source_chapter_id, source_path, confirmed_at FROM claim_confirmations;";
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                confRows++;
                var row = new ClaimConfirmationRow
                {
                    ClaimUid        = r.GetString(0),
                    SourceChapterId = Str(r, 1) ?? "",
                    SourcePath      = Str(r, 2) ?? "",
                    ConfirmedAt     = Str(r, 3) ?? "",
                };
                // A confirmation of a claim SQL Server does not hold would be an orphan edge.
                if (!existingClaims.Contains(row.ClaimUid)) continue;
                if (existingConf.Add(Key(row.ClaimUid, row.SourceChapterId, row.SourcePath)))
                    newConf.Add(row);
            }
        }
        Report("claim_confirmations", confRows, newConf.Count);

        // ── contradictions ─────────────────────────────────────────────────
        var existingContra = (await db.ClaimContradictions.AsNoTracking()
                .Select(c => new { c.AUid, c.BUid }).ToListAsync())
            .Select(c => Key(c.AUid, c.BUid))
            .ToHashSet(StringComparer.Ordinal);
        var newContra = new List<ClaimContradictionRow>();
        var contraRows = 0;
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT a_uid, b_uid, detected_at FROM claim_contradictions;";
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                contraRows++;
                var row = new ClaimContradictionRow
                {
                    AUid       = r.GetString(0),
                    BUid       = r.GetString(1),
                    DetectedAt = Str(r, 2) ?? "",
                };
                if (!existingClaims.Contains(row.AUid) || !existingClaims.Contains(row.BUid)) continue;
                if (existingContra.Add(Key(row.AUid, row.BUid)))
                    newContra.Add(row);
            }
        }
        Report("claim_contradictions", contraRows, newContra.Count);

        // ── extraction runs (identity key; matched on StartedAt + scope) ───
        var existingRuns = (await db.ExtractionRuns.AsNoTracking()
                .Select(x => new { x.StartedAt, x.ScopeType, x.ScopeId }).ToListAsync())
            .Select(x => Key(x.StartedAt, x.ScopeType, x.ScopeId))
            .ToHashSet(StringComparer.Ordinal);
        var newRuns = new List<ExtractionRunRow>();
        var runRows = 0;
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = """
                SELECT started_at, completed_at, scope_type, scope_id, new_claims,
                       confirmed_claims, contradicted_claims, error
                FROM extraction_runs;
                """;
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                runRows++;
                var row = new ExtractionRunRow
                {
                    StartedAt          = Str(r, 0) ?? "",
                    CompletedAt        = Str(r, 1),
                    ScopeType          = Str(r, 2) ?? "",
                    ScopeId            = Str(r, 3),
                    NewClaims          = r.IsDBNull(4) ? 0 : Convert.ToInt32(r.GetValue(4)),
                    ConfirmedClaims    = r.IsDBNull(5) ? 0 : Convert.ToInt32(r.GetValue(5)),
                    ContradictedClaims = r.IsDBNull(6) ? 0 : Convert.ToInt32(r.GetValue(6)),
                    Error              = Str(r, 7),
                };
                if (existingRuns.Add(Key(row.StartedAt, row.ScopeType, row.ScopeId)))
                    newRuns.Add(row);
            }
        }
        Report("extraction_runs", runRows, newRuns.Count);

        var missing = newClaims.Count + newConf.Count + newContra.Count + newRuns.Count;
        if (missing == 0)
        {
            Console.WriteLine("  ✔ SQL Server already holds every continuity.db row. Nothing to insert.");
            return 0;
        }
        if (!apply)
        {
            Console.WriteLine($"  · {missing} row(s) missing from SQL Server. Re-run with --apply to insert them.");
            return 0;
        }

        try
        {
            await using var tx = await db.Database.BeginTransactionAsync();
            db.ContinuityClaims.AddRange(newClaims);
            await db.SaveChangesAsync();
            db.ClaimConfirmations.AddRange(newConf);
            db.ClaimContradictions.AddRange(newContra);
            db.ExtractionRuns.AddRange(newRuns);
            await db.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ✘ import failed, nothing committed: {ex.GetBaseException().Message}");
            return 1;
        }

        // Read back: every SQLite claim uid must now be in SQL Server.
        var after = (await db.ContinuityClaims.IgnoreQueryFilters().AsNoTracking()
                .Select(c => c.ClaimUid).ToListAsync())
            .ToHashSet(StringComparer.Ordinal);
        var stillMissing = newClaims.Count(c => !after.Contains(c.ClaimUid));
        if (stillMissing > 0)
        {
            Console.WriteLine($"  ✘ read-back: {stillMissing} imported claim(s) not found in SQL Server.");
            return 1;
        }
        Console.WriteLine($"  ✔ inserted {missing} row(s); read-back confirmed. SQLite file left untouched.");
        return 0;
    }

    private static void Report(string table, int sqliteRows, int missing)
        => Console.WriteLine($"  {table,-22} sqlite={sqliteRows,7}  missing-in-sql={missing,7}");

    private static string DefaultPath(IServiceProvider sp)
        => Path.Combine(sp.GetRequiredService<IPathProvider>().MutableDataDir, "continuity.db");

    private static string Key(params string?[] parts) => string.Join("\u001f", parts.Select(p => p ?? ""));

    private static string? Str(SqliteDataReader r, int i) => r.IsDBNull(i) ? null : r.GetValue(i)?.ToString();

    /// <summary>The legacy column holds a JSON array or, in old rows, a comma list.</summary>
    private static List<string> ParseExtractedBy(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return new List<string>();
        var t = raw.Trim();
        if (t.StartsWith('['))
        {
            try { return System.Text.Json.JsonSerializer.Deserialize<List<string>>(t) ?? new List<string>(); }
            catch (System.Text.Json.JsonException) { /* fall through to the comma form */ }
        }
        return t.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
    }

    private static string? ArgValue(string[] args, string name)
    {
        var i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }
}
