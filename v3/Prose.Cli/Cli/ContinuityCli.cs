using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MindAttic.Legion;
using Prose.Core.Data;
using Prose.Core.Interfaces;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// CLI surface for the unified continuity store. Subcommands:
///
///   prose --continuity migrate                          One-time migration from engine/data/continuity/*.json into the SQLite store.
///   prose --continuity stats                            Total / NEW / CONFIRMED / CONTRADICTED / CANONICAL / REJECTED counts + sources breakdown.
///   prose --continuity contradictions                   List every CONTRADICTED claim awaiting resolution.
///   prose --continuity resolve --a &lt;uid&gt; --b &lt;uid&gt; --winner A|B|custom [--object "..."] [--note "..."]
///                                                    Resolve a contradiction.
///   prose --continuity entity &lt;name&gt;                    Dump every claim about one entity.
///   prose --continuity extract --chapter &lt;chapterId&gt;    Extract claims from one chapter's prose via Legion Quorum.
///   prose --continuity extract --book &lt;bookId&gt;          Extract claims from every chapter in a book.
///   prose --continuity extract --entity &lt;guid&gt;          Extract claims from one entity's Records.Json blob (by EntityId).
///   prose --continuity apply --claim &lt;uid&gt;              Apply a CANONICAL claim back to its entity record (Legion picks the field).
///
/// Backed by ContinuityService / ContinuityExtractionService / ContinuityApplyService —
/// same code path the UI and MCP tools use.
/// </summary>
public static class ContinuityCli
{
    public static int Run(string[] args, IServiceProvider services)
    {
        var idx = Array.FindIndex(args, a => a == "--continuity");
        if (idx < 0 || idx + 1 >= args.Length) { PrintUsage(); return 1; }

        var sub  = args[idx + 1].ToLowerInvariant();
        var rest = args[(idx + 2)..];
        var svc  = services.GetRequiredService<ContinuityService>();

        return sub switch
        {
            "migrate"         => CmdMigrate(svc),
            "stats"           => CmdStats(svc),
            "contradictions"  => CmdContradictions(svc),
            "resolve"         => CmdResolve(rest, svc),
            "entity"          => CmdEntity(rest, svc),
            "extract"         => CmdExtract(rest, services).GetAwaiter().GetResult(),
            "apply"           => CmdApply(rest, services).GetAwaiter().GetResult(),
            "sweep"           => CmdSweep(rest, services).GetAwaiter().GetResult(),
            _                 => Fail($"unknown subcommand: {sub}"),
        };
    }

    static int Fail(string msg) { Console.Error.WriteLine($"[continuity] {msg}"); PrintUsage(); return 1; }

    static int CmdMigrate(ContinuityService svc)
    {
        Console.WriteLine("[continuity] Legacy JSON migration is retired. Use:");
        Console.WriteLine("  prose --migrate-sql --import continuity   (SQLite continuity.db → SQL Server)");
        return 0;
    }

    static int CmdStats(ContinuityService svc)
    {
        var s = svc.GetStats();
        Console.WriteLine($"[continuity] DB:          {svc.DbPath}");
        Console.WriteLine($"[continuity] Total:       {s.Total}");
        Console.WriteLine($"[continuity]   NEW:          {s.New}");
        Console.WriteLine($"[continuity]   CONFIRMED:    {s.Confirmed}");
        Console.WriteLine($"[continuity]   CONTRADICTED: {s.Contradicted}");
        Console.WriteLine($"[continuity]   CANONICAL:    {s.Canonical}");
        Console.WriteLine($"[continuity]   REJECTED:     {s.Rejected}");
        Console.WriteLine($"[continuity]   SUPERSEDED:   {s.Superseded}");
        Console.WriteLine($"[continuity] Sources:");
        Console.WriteLine($"[continuity]   prose:         {s.FromProse}");
        Console.WriteLine($"[continuity]   entity_record: {s.FromEntityRecord}");
        return 0;
    }

    static int CmdContradictions(ContinuityService svc)
    {
        var pairs = svc.GetContradictions();
        if (pairs.Count == 0) { Console.WriteLine("[continuity] No unresolved contradictions."); return 0; }
        foreach (var p in pairs)
        {
            Console.WriteLine($"[{p.A.EntityName}] {p.A.Predicate}");
            Console.WriteLine($"  A {p.A.ClaimUid}  → \"{p.A.Object}\"  (ch.{p.A.SourceChapterNumber} {p.A.SourceChapterTitle})");
            Console.WriteLine($"  B {p.B.ClaimUid}  → \"{p.B.Object}\"  (ch.{p.B.SourceChapterNumber} {p.B.SourceChapterTitle})");
            Console.WriteLine();
        }
        return pairs.Count > 0 ? 1 : 0;
    }

    static int CmdResolve(string[] args, ContinuityService svc)
    {
        var aUid    = ArgValue(args, "--a");
        var bUid    = ArgValue(args, "--b");
        var winner  = ArgValue(args, "--winner") ?? "";
        var custom  = ArgValue(args, "--object")  ?? "";
        var note    = ArgValue(args, "--note")    ?? "";
        if (string.IsNullOrEmpty(aUid) || string.IsNullOrEmpty(bUid)) return Fail("--a and --b required");

        try
        {
            var r = svc.Resolve(aUid, bUid, winner, custom, note);
            Console.WriteLine($"[continuity] Resolved. Winner: {r.Winner.ClaimUid} \"{r.Winner.Object}\"");
            return 0;
        }
        catch (Exception ex)
        {
            return Fail(ex.Message);
        }
    }

    static int CmdEntity(string[] args, ContinuityService svc)
    {
        if (args.Length == 0) return Fail("entity name required");
        var name = string.Join(" ", args);
        // Resolve by name → take any claim's entity_id
        var hits = svc.GetByStatus("CONFIRMED").Concat(svc.GetByStatus("CANONICAL")).Concat(svc.GetByStatus("NEW"))
                      .Where(c => string.Equals(c.EntityName, name, StringComparison.OrdinalIgnoreCase))
                      .ToList();
        if (hits.Count == 0) { Console.WriteLine($"[continuity] No claims for entity \"{name}\"."); return 0; }
        var eid = hits[0].EntityId;
        var all = svc.GetByEntity(eid);
        Console.WriteLine($"[continuity] {hits[0].EntityName} ({eid}) — {all.Count} claims");
        foreach (var c in all)
            Console.WriteLine($"  [{c.Status,-12}] {c.Predicate,-32}  →  {c.Object}");
        return 0;
    }

    static async Task<int> CmdExtract(string[] args, IServiceProvider services)
    {
        var ext       = services.GetRequiredService<ContinuityExtractionService>();
        var bookRepo  = services.GetRequiredService<IBookRepository>();
        var chapterId = ArgValue(args, "--chapter");
        var bookId    = ArgValue(args, "--book");
        var entityRef = ArgValue(args, "--entity");

        if (!string.IsNullOrEmpty(chapterId))
        {
            Console.WriteLine($"[continuity] Extracting from chapter {chapterId} (Legion vote — this may take a minute)…");
            try
            {
                var r = await ext.ExtractFromChapterAsync(chapterId);
                Console.WriteLine($"[continuity] ch.{r.ChapterNumber} {r.ChapterTitle} — voters {r.VotersSuccessful}/{r.VotersTotal}, candidates {r.CandidatesProposed}, validated {r.CandidatesValidated}");
                Console.WriteLine($"[continuity] {r.NewClaims} new, {r.ConfirmedClaims} confirmed, {r.ContradictedClaims} contradicted, {r.UnknownEntities.Count} unknown entity references");
                if (r.UnknownEntities.Count > 0) Console.WriteLine("[continuity] unknown: " + string.Join(", ", r.UnknownEntities));
                return r.ContradictedClaims > 0 ? 1 : 0;
            }
            catch (Exception ex) { return Fail("extract failed: " + ex.Message); }
        }
        if (!string.IsNullOrEmpty(bookId))
        {
            var book = bookRepo.LoadBook(bookId);
            if (book == null) return Fail($"book not found: {bookId}");
            Console.WriteLine($"[continuity] Extracting from book \"{book.Title}\" — {book.ChapterIds?.Count ?? 0} chapters (Legion vote per chapter — minutes)…");
            try
            {
                var rs = await ext.ExtractFromBookAsync(book);
                int n = rs.Sum(r => r.NewClaims), cf = rs.Sum(r => r.ConfirmedClaims), ct = rs.Sum(r => r.ContradictedClaims);
                Console.WriteLine($"[continuity] Done. {n} new, {cf} confirmed, {ct} contradicted across {rs.Count} chapters.");
                return ct > 0 ? 1 : 0;
            }
            catch (Exception ex) { return Fail("extract failed: " + ex.Message); }
        }
        if (!string.IsNullOrEmpty(entityRef))
        {
            if (!Guid.TryParse(entityRef, out var entityId)
                && !(entityRef.Length == 32 && Guid.TryParseExact(entityRef, "N", out entityId)))
                return Fail($"--entity expects an EntityId guid (got '{entityRef}')");

            Console.WriteLine($"[continuity] Extracting from entity record {entityId}…");
            try
            {
                var r = await ext.ExtractFromEntityRecordAsync(entityId);
                if (r.Error != null) return Fail(r.Error);
                Console.WriteLine($"[continuity] {r.NewClaims} new, {r.ConfirmedClaims} confirmed, {r.ContradictedClaims} contradicted.");
                return r.ContradictedClaims > 0 ? 1 : 0;
            }
            catch (Exception ex) { return Fail("extract failed: " + ex.Message); }
        }
        return Fail("extract requires one of: --chapter <id> | --book <id> | --entity <guid>");
    }

    static async Task<int> CmdApply(string[] args, IServiceProvider services)
    {
        var apply = services.GetRequiredService<ContinuityApplyService>();
        var uid = ArgValue(args, "--claim");
        if (string.IsNullOrEmpty(uid)) return Fail("apply requires --claim <uid>");

        Console.WriteLine($"[continuity] Applying {uid} (Legion picks the entity field)…");
        try
        {
            var r = await apply.ApplyAsync(uid);
            if (!r.Ok) return Fail(r.Error);
            Console.WriteLine($"[continuity] Wrote to {Path.GetFileName(r.EntityFile)} → {r.FieldPath}  (confidence {r.DecisionConfidence:P0})");
            if (!string.IsNullOrEmpty(r.DecisionReason)) Console.WriteLine("[continuity] reason: " + r.DecisionReason);
            return 0;
        }
        catch (Exception ex) { return Fail("apply failed: " + ex.Message); }
    }

    /// <summary>
    /// One-shot end-to-end sweep:
    ///   1. Extract from every entity record (people, places, factions, corponations).
    ///   2. Extract from every chapter (or just one book if --book is given).
    ///   3. Auto-resolve every CONTRADICTED pair using Legion DecideAsync.
    ///   4. Auto-apply every unapplied CANONICAL claim using ContinuityApplyService
    ///      (which itself uses Legion DecideAsync to pick the entity-file field).
    ///
    /// --book <id>   restrict prose extraction to one book's chapters
    /// --skip-records  skip step 1 (entity-record extraction)
    /// --skip-prose    skip step 2 (chapter prose extraction)
    /// --skip-resolve  skip step 3 (auto-resolve)
    /// --skip-apply    skip step 4 (apply CANONICAL → entity)
    /// --dry-run       print plan + go through extraction-only; do not resolve or apply
    /// </summary>
    static async Task<int> CmdSweep(string[] args, IServiceProvider services)
    {
        var store      = services.GetRequiredService<ContinuityService>();
        var extraction = services.GetRequiredService<ContinuityExtractionService>();
        var apply      = services.GetRequiredService<ContinuityApplyService>();
        var voting     = services.GetRequiredService<LlmVotingService>();
        var paths      = services.GetRequiredService<IPathProvider>();
        var bookRepo   = services.GetRequiredService<IBookRepository>();
        var chapRepo   = services.GetRequiredService<IChapterRepository>();

        var bookId       = ArgValue(args, "--book");
        var skipRecords  = args.Contains("--skip-records");
        var skipProse    = args.Contains("--skip-prose");
        var skipResolve  = args.Contains("--skip-resolve");
        var skipApply    = args.Contains("--skip-apply");
        var dryRun       = args.Contains("--dry-run");

        // SS-A44: the auto-resolve (step 3) and apply (step 4) phases decide
        // canonical values via a Legion panel vote (DecideAsync). Those are
        // disabled by default — extraction still runs, but resolution/apply are
        // skipped unless --allow-votes is passed. The sweep never fails on this.
        var votingGate = services.GetRequiredService<VotingGate>();
        if (!votingGate.IsAllowed(args.Contains("--allow-votes")))
        {
            if (!skipResolve || !skipApply)
                Console.WriteLine("[sweep] Auto-resolve/apply skipped: voting disabled by default (SS-A44). Pass --allow-votes to let the panel resolve contradictions.");
            skipResolve = true;
            skipApply   = true;
        }

        // Resolve scope of chapters
        List<string> chapterIds;
        string scopeLabel;
        if (!string.IsNullOrEmpty(bookId))
        {
            var book = bookRepo.LoadBook(bookId);
            if (book == null) return Fail($"book not found: {bookId}");
            chapterIds = book.ChapterIds ?? new();
            scopeLabel = $"book \"{book.Title}\" ({chapterIds.Count} chapters)";
        }
        else
        {
            chapterIds = chapRepo.ListChapters().Select(c => c.Id).ToList();
            scopeLabel = $"all chapters ({chapterIds.Count})";
        }

        // Resolve scope of entity records — pulled from SQL so the sweep
        // hits whatever's in canon today, not whatever happened to be on disk.
        var entityKinds = new[] { "character", "place", "faction", "corponation" };
        var dbFactory   = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        List<(Guid Id, string Name, string Type)> recordEntities;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            recordEntities = await db.Entities.AsNoTracking()
                .Where(e => e.IsActive && entityKinds.Contains(e.EntityType))
                .Select(e => new ValueTuple<Guid, string, string>(e.Id, e.Name, e.EntityType))
                .ToListAsync();
        }

        Console.WriteLine($"[sweep] Plan:");
        Console.WriteLine($"[sweep]   1) Extract from entity records: {(skipRecords ? "skipped" : $"{recordEntities.Count} entities")}");
        Console.WriteLine($"[sweep]   2) Extract from chapter prose : {(skipProse ? "skipped" : scopeLabel)}");
        Console.WriteLine($"[sweep]   3) Auto-resolve contradictions: {(skipResolve || dryRun ? "skipped" : "Legion DecideAsync per pair")}");
        Console.WriteLine($"[sweep]   4) Apply CANONICAL → entity   : {(skipApply || dryRun ? "skipped" : "Legion DecideAsync per claim")}");
        Console.WriteLine();

        // ── Step 1: entity records ─────────────────────────────────────────
        if (!skipRecords)
        {
            int i = 0;
            foreach (var ent in recordEntities)
            {
                i++;
                Console.WriteLine($"[sweep] [{i,4}/{recordEntities.Count}] {ent.Type}: {ent.Name}");
                try
                {
                    var r = await extraction.ExtractFromEntityRecordAsync(ent.Id);
                    if (r.Error != null) Console.WriteLine($"[sweep]     ! {r.Error}");
                    else Console.WriteLine($"[sweep]     {r.NewClaims} new, {r.ConfirmedClaims} confirmed, {r.ContradictedClaims} contradicted");
                }
                catch (Exception ex) { Console.WriteLine($"[sweep]     ! {ex.Message}"); }
            }
        }

        // ── Step 2: chapters ───────────────────────────────────────────────
        if (!skipProse)
        {
            int i = 0;
            foreach (var cid in chapterIds)
            {
                i++;
                Console.WriteLine($"[sweep] [{i,4}/{chapterIds.Count}] chapter: {cid}");
                try
                {
                    var r = await extraction.ExtractFromChapterAsync(cid);
                    Console.WriteLine($"[sweep]     ch.{r.ChapterNumber} {r.ChapterTitle} — {r.NewClaims} new, {r.ConfirmedClaims} confirmed, {r.ContradictedClaims} contradicted ({r.UnknownEntities.Count} unknown entities)");
                }
                catch (Exception ex) { Console.WriteLine($"[sweep]     ! {ex.Message}"); }
            }
        }

        if (dryRun)
        {
            Console.WriteLine();
            Console.WriteLine("[sweep] Dry run — skipping resolve and apply phases.");
            PrintFinalStats(store);
            return 0;
        }

        // ── Step 3: auto-resolve contradictions ────────────────────────────
        if (!skipResolve)
        {
            var pairs = store.GetContradictions();
            Console.WriteLine();
            Console.WriteLine($"[sweep] {pairs.Count} contradictions to auto-resolve via Legion DecideAsync");
            int idx = 0;
            foreach (var p in pairs)
            {
                idx++;
                var question = $"For {p.A.EntityName}, which is the canonical value for the predicate \"{p.A.Predicate}\"?";
                var options  = new[] { p.A.Object, p.B.Object };
                var context  = BuildContradictionContext(p);
                Console.WriteLine($"[sweep] [{idx,4}/{pairs.Count}] {p.A.EntityName}.{p.A.Predicate}: \"{p.A.Object}\" vs \"{p.B.Object}\"");
                try
                {
                    var d = await voting.DecideAsync(question, options, context, Quorum.Plurality, maxTokens: 256);
                    string winner;
                    if (string.Equals(d.Choice, p.A.Object, StringComparison.OrdinalIgnoreCase)) winner = "A";
                    else if (string.Equals(d.Choice, p.B.Object, StringComparison.OrdinalIgnoreCase)) winner = "B";
                    else { Console.WriteLine($"[sweep]     panel did not pick a clean winner — leaving CONTRADICTED for human review"); continue; }
                    store.Resolve(p.A.ClaimUid, p.B.ClaimUid, winner, "", $"auto-resolved by Legion DecideAsync (confidence {d.Confidence:P0})");
                    Console.WriteLine($"[sweep]     winner: {winner} → \"{d.Choice}\" (confidence {d.Confidence:P0})");
                }
                catch (Exception ex) { Console.WriteLine($"[sweep]     ! {ex.Message}"); }
            }
        }

        // ── Step 4: apply CANONICAL claims back to entities ────────────────
        if (!skipApply)
        {
            var canonical = store.GetByStatus("CANONICAL").Where(c => string.IsNullOrEmpty(c.AppliedAt)).ToList();
            Console.WriteLine();
            Console.WriteLine($"[sweep] {canonical.Count} CANONICAL claims to apply to entity records");
            int idx = 0;
            foreach (var c in canonical)
            {
                idx++;
                Console.WriteLine($"[sweep] [{idx,4}/{canonical.Count}] {c.EntityName}.{c.Predicate} = \"{c.Object}\"");
                try
                {
                    var r = await apply.ApplyAsync(c.ClaimUid);
                    if (r.Ok) Console.WriteLine($"[sweep]     → {Path.GetFileName(r.EntityFile)}#{r.FieldPath}  (confidence {r.DecisionConfidence:P0})");
                    else     Console.WriteLine($"[sweep]     ! {r.Error}");
                }
                catch (Exception ex) { Console.WriteLine($"[sweep]     ! {ex.Message}"); }
            }
        }

        Console.WriteLine();
        PrintFinalStats(store);
        return 0;
    }

    static void PrintFinalStats(ContinuityService store)
    {
        var s = store.GetStats();
        Console.WriteLine($"[sweep] Final state: total={s.Total} new={s.New} confirmed={s.Confirmed} contradicted={s.Contradicted} canonical={s.Canonical} rejected={s.Rejected} (prose={s.FromProse} record={s.FromEntityRecord})");
    }

    static string BuildContradictionContext(ContradictionPair p)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Entity: {p.A.EntityName} ({p.A.EntityKind})");
        sb.AppendLine($"Predicate: {p.A.Predicate}");
        sb.AppendLine();
        sb.AppendLine($"Option A: \"{p.A.Object}\"");
        sb.AppendLine($"  Source: {SourceLabel(p.A)}  Voters: {string.Join(", ", p.A.ExtractedBy)}  Confidence: {p.A.Confidence}");
        if (!string.IsNullOrEmpty(p.A.Snippet)) sb.AppendLine($"  Snippet: \"{p.A.Snippet}\"");
        sb.AppendLine();
        sb.AppendLine($"Option B: \"{p.B.Object}\"");
        sb.AppendLine($"  Source: {SourceLabel(p.B)}  Voters: {string.Join(", ", p.B.ExtractedBy)}  Confidence: {p.B.Confidence}");
        if (!string.IsNullOrEmpty(p.B.Snippet)) sb.AppendLine($"  Snippet: \"{p.B.Snippet}\"");
        sb.AppendLine();
        sb.AppendLine("Pick the option that is most consistent with the snippet evidence and the prevailing voter consensus. " +
                      "If both are plausible, prefer the one drawn from a CANONICAL or higher-confidence source.");
        return sb.ToString();
    }

    static string SourceLabel(ContinuityClaim c) => c.SourceType switch
    {
        "prose"            => $"prose ch.{c.SourceChapterNumber} ({c.SourceChapterTitle})",
        "entity_record"    => $"entity record {Path.GetFileName(c.SourcePath ?? "")}",
        "writer_assertion" => "writer assertion",
        _                  => c.SourceType,
    };

    static string? ArgValue(string[] args, string name)
    {
        var i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }

    static void PrintUsage()
    {
        Console.WriteLine("""
            Usage:
              prose --continuity migrate
              prose --continuity stats
              prose --continuity contradictions
              prose --continuity resolve --a <uid> --b <uid> --winner A|B|custom [--object "..."] [--note "..."]
              prose --continuity entity <name>
              prose --continuity extract --chapter <chapterId>
              prose --continuity extract --book <bookId>
              prose --continuity extract --entity <path-to-entity-json>
              prose --continuity apply --claim <claimUid>
              prose --continuity sweep [--book <id>] [--skip-records] [--skip-prose] [--skip-resolve] [--skip-apply] [--dry-run]
                  one-shot end-to-end pipeline: extract from records + chapters → auto-resolve via Legion DecideAsync → apply CANONICAL claims
            """);
    }
}
