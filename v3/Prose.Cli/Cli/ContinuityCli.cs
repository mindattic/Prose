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
///   prose --continuity search --text "&lt;substring&gt;"       Free-text search over EntityName,
///                                                       Predicate AND Object, printing ClaimUids.
///                                                       Finds facts hidden in object strings that
///                                                       no predicate-name sweep can match.
///   prose --continuity reject --claim &lt;uid&gt;             Reject one fabricated claim (or a whole
///                                                       predicate family via --entity +
///                                                       --predicate-prefix). Reversible.
///   prose --continuity extract --chapter &lt;chapterId&gt;    Extract claims from one chapter's prose (legacy Book/Chapter model).
///   prose --continuity extract --book &lt;bookId&gt;          Extract claims from every chapter in a book (legacy Book/Chapter model).
///   prose --continuity extract --node &lt;nodeIdOrSlug&gt;    Extract claims from every leaf chapter under a modern SS-A43 BookNode.
///   prose --continuity extract --entity &lt;guid&gt;          Extract claims from one entity's Records.Json blob (by EntityId).
///   prose --continuity extract --outline &lt;nodeIdOrSlug&gt;   Extract claims from the story bible (SourceType="outline").
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
            "groups"          => CmdGroups(rest, svc),
            "reset-book"      => CmdResetBook(rest, svc),
            "resolve"         => CmdResolve(rest, svc),
            "entity"          => CmdEntity(rest, svc),
            "search"          => CmdSearch(rest, svc),
            "reject"          => CmdReject(rest, svc),
            "extract"         => CmdExtract(rest, services).GetAwaiter().GetResult(),
            "apply"           => CmdApply(rest, services).GetAwaiter().GetResult(),
            "sweep"           => CmdSweep(rest, services).GetAwaiter().GetResult(),
            _                 => Fail($"unknown subcommand: {sub}"),
        };
    }

    static int Fail(string msg) { Console.Error.WriteLine($"[continuity] {msg}"); PrintUsage(); return 1; }

    /// <summary>
    /// prose --continuity reject --claim &lt;uid&gt; [--claim &lt;uid&gt; ...] [--note "..."]
    /// prose --continuity reject --entity &lt;name-or-id&gt; --predicate-prefix &lt;p&gt; [--note "..."]
    ///
    /// Rejects one claim, several named claims, or a whole predicate family on one entity.
    ///
    /// <para><b>Why this exists (2026-09-03).</b> There was no sanctioned way to reject a single
    /// ledger claim. <c>resolve</c> needs a CONTRADICTED pair and picks a winner;
    /// <c>reset-book</c> supersedes every live claim for a whole book. Neither can express "this
    /// one fact is fabricated and has no counterpart to lose to" — which is the shape of the
    /// defect the Story Ledger exists for. Found the moment the Tuned Read shipped: BCODA's
    /// ledger still held four CONFIRMED claims asserting a fabricated father (father_name,
    /// father_occupation, father_profession, father_status) whose prose Phase 0 had already
    /// removed. The ledger feeds ContinuityService's ESTABLISHED CANON prompt block, so a
    /// fabrication surviving there is not cosmetic: the next beat generated with that character
    /// on the page would be told it as fact.</para>
    ///
    /// <para><c>--predicate-prefix</c> takes the family form because extraction records one idea
    /// under many names (father_name / father_occupation / father_status / father_took_swords);
    /// rejecting them one uid at a time is how the last four get missed. Prints exactly what it
    /// will reject and requires <c>--yes</c> for the family form, since that is the destructive
    /// shape. Claims are never deleted — <c>RejectClaim</c> sets status REJECTED and
    /// ContinuityClaims is system-versioned, so this is reversible.</para>
    /// </summary>
    static int CmdReject(string[] args, ContinuityService svc)
    {
        var note = Flag(args, "--note") ?? "rejected via prose --continuity reject";
        var uids = AllFlags(args, "--claim");
        var entity = Flag(args, "--entity");
        var prefix = Flag(args, "--predicate-prefix");

        if (uids.Count == 0 && (string.IsNullOrWhiteSpace(entity) || string.IsNullOrWhiteSpace(prefix)))
        {
            Console.Error.WriteLine(
                "Usage: prose --continuity reject --claim <uid> [--claim <uid> ...] [--note \"...\"]\n" +
                "       prose --continuity reject --entity <name-or-id> --predicate-prefix <p> --yes [--note \"...\"]");
            return 2;
        }

        // ── family form ─────────────────────────────────────────────────────
        if (uids.Count == 0)
        {
            var all = svc.GetByEntity(entity!);
            if (all.Count == 0)
            {
                // GetByEntity takes an id; fall back to a name match over the live ledger so the
                // command works from the name the --continuity entity listing prints.
                Console.Error.WriteLine(
                    $"[continuity] No claims found for entity id '{entity}'. " +
                    "This form takes the ENTITY ID (the hex string --continuity entity prints in " +
                    "its header), not the display name.");
                return 2;
            }

            var live = all
                .Where(c => c.Status is not ("REJECTED" or "SUPERSEDED"))
                .Where(c => c.Predicate.StartsWith(prefix!, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (live.Count == 0)
            {
                Console.WriteLine($"[continuity] No live claims on '{all[0].EntityName}' with predicate prefix '{prefix}'.");
                return 0;
            }

            Console.WriteLine($"{all[0].EntityName} — {live.Count} live claim(s) matching predicate prefix '{prefix}':");
            foreach (var c in live)
                Console.WriteLine($"  [{c.Status,-12}] {c.Predicate,-34} →  {c.Object}");

            if (!args.Contains("--yes") && !args.Contains("--no-confirm"))
            {
                Console.WriteLine();
                Console.WriteLine("Nothing rejected. Re-run with --yes to reject all of the above.");
                return 0;
            }

            foreach (var c in live) svc.RejectClaim(c.ClaimUid, note);
            Console.WriteLine();
            Console.WriteLine($"[continuity] Rejected {live.Count} claim(s). ContinuityClaims is system-versioned — " +
                              "these are recoverable from ContinuityClaims_History.");
            return 0;
        }

        // ── explicit-uid form ───────────────────────────────────────────────
        var rejected = 0;
        foreach (var uid in uids)
        {
            try { svc.RejectClaim(uid, note); rejected++; Console.WriteLine($"  rejected {uid}"); }
            catch (Exception ex) { Console.Error.WriteLine($"  ! {uid}: {ex.Message}"); }
        }
        Console.WriteLine($"[continuity] Rejected {rejected} of {uids.Count} claim(s).");
        return rejected == uids.Count ? 0 : 1;
    }

    static string? Flag(string[] args, string name)
    {
        var i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }

    /// <summary>Every value of a repeatable flag, so several --claim uids can be rejected in one
    /// call (one transaction per claim, but one command per decision).</summary>
    static List<string> AllFlags(string[] args, string name)
    {
        var values = new List<string>();
        for (var i = 0; i < args.Length - 1; i++)
            if (args[i] == name && !string.IsNullOrWhiteSpace(args[i + 1])) values.Add(args[i + 1]);
        return values;
    }

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
        Console.WriteLine($"[continuity]   bible:         {s.FromBible}");
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

    /// <summary>
    /// prose --continuity groups --slug &lt;slug&gt; [--predicate &lt;name&gt;]
    ///
    /// Prints ContinuityService.GetContradictionGroups(slug) (the book-scoped, N-way grouping
    /// FactLedgerAsync's "FACT-LEDGER" findings are built from) WITH each claim's ClaimUid —
    /// the one thing `--findings show` can't provide, since a Finding is just the rendered
    /// summary text. Added 2026-09-01: hand-resolving a fact-ledger contradiction requires
    /// `--continuity resolve --a &lt;uid&gt; --b &lt;uid&gt;`, and there was no surface printing the UIDs
    /// this grouping (as opposed to the older pairwise GetContradictions()) actually produces.
    /// </summary>
    static int CmdGroups(string[] args, ContinuityService svc)
    {
        var slug = ArgValue(args, "--slug");
        if (string.IsNullOrEmpty(slug)) return Fail("groups requires --slug <slug>");
        var predicateFilter = ArgValue(args, "--predicate");

        var groups = svc.GetContradictionGroups(slug);
        if (!string.IsNullOrEmpty(predicateFilter))
            groups = groups.Where(g => string.Equals(g.Predicate, predicateFilter, StringComparison.OrdinalIgnoreCase)).ToList();

        if (groups.Count == 0) { Console.WriteLine($"[continuity] no contradiction groups for {slug}."); return 0; }

        foreach (var g in groups)
        {
            Console.WriteLine($"[{g.EntityName}] {g.Predicate}  ({g.Claims.Count} variants)");
            foreach (var c in g.Claims)
            {
                var src = c.SourceChapterNumber.HasValue ? $"ch.{c.SourceChapterNumber}" : c.SourceType;
                Console.WriteLine($"  {c.ClaimUid}  [{c.Status,-10}]  \"{c.Object}\"  ({src})");
            }
            Console.WriteLine();
        }
        return 0;
    }

    /// <summary>
    /// prose --continuity reset-book --slug &lt;slug&gt; [--note "..."]
    ///
    /// Supersedes every live claim tagged with this book's slug — see
    /// ContinuityService.SupersedeAllLiveClaimsForBook's doc comment for why. Run
    /// `--continuity extract --node &lt;slug&gt;` right after to repopulate the ledger from only the
    /// book's current live text, then `--fact-ledger-refresh --slug &lt;slug&gt;` to see the real
    /// remaining count.
    /// </summary>
    static int CmdResetBook(string[] args, ContinuityService svc)
    {
        var slug = ArgValue(args, "--slug");
        if (string.IsNullOrEmpty(slug)) return Fail("reset-book requires --slug <slug>");
        var note = ArgValue(args, "--note") ?? "reset before fresh extraction (prose --continuity reset-book)";

        var n = svc.SupersedeAllLiveClaimsForBook(slug, note);
        Console.WriteLine($"[continuity] {n} claim(s) for \"{slug}\" superseded. Now run:");
        Console.WriteLine($"  prose --continuity extract --node {slug}");
        Console.WriteLine($"  prose --fact-ledger-refresh --slug {slug}");
        return 0;
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

    /// <summary>
    /// prose --continuity search --text "&lt;substring&gt;" [--entity &lt;id&gt;] [--predicate-prefix &lt;p&gt;] [--live] [--limit N]
    ///
    /// Free-text search across the whole ledger — entity name, predicate AND object — printing
    /// each hit's ClaimUid so it can be fed straight to <c>--continuity reject</c>.
    ///
    /// <para><b>Why this exists (2026-09-03).</b> There was no way to search claims by text, and
    /// that gap hid fabricated canon twice. Phase 0 called the "Dae-jung Seo" fabrication purged
    /// because <c>search_universe</c> found nothing — it searches <c>Entities</c>, not this table
    /// — and Phase 2 then found twelve live claims still asserting it. The author's family purge
    /// found four more that no <c>father_*</c> predicate sweep could ever match, because the fact
    /// was in the OBJECT: <c>second_sword_possession → "old sword wrapped in oilcloth, made by
    /// father"</c>. A predicate-name search cannot find a fact hidden in an object string.</para>
    ///
    /// <para>Rejected/superseded rows are shown by default (pass <c>--live</c> to hide them):
    /// confirming a purge landed means being able to see the rejected rows.</para>
    /// </summary>
    static int CmdSearch(string[] args, ContinuityService svc)
    {
        var text = Flag(args, "--text") ?? Flag(args, "--pattern");
        if (string.IsNullOrWhiteSpace(text))
        {
            Console.Error.WriteLine(
                "Usage: prose --continuity search --text \"<substring>\" [--entity <entityId>] " +
                "[--predicate-prefix <p>] [--live] [--limit N]\n" +
                "  Searches EntityName, Predicate AND Object. Prints ClaimUid for --continuity reject.");
            return 2;
        }

        var limit = int.TryParse(Flag(args, "--limit"), out var n) && n > 0 ? n : 200;
        var hits = svc.Search(
            text,
            entityId: Flag(args, "--entity"),
            predicatePrefix: Flag(args, "--predicate-prefix"),
            liveOnly: args.Contains("--live"));

        if (hits.Count == 0)
        {
            Console.WriteLine($"[continuity] No claims matching \"{text}\".");
            return 0;
        }

        var live = hits.Count(c => c.Status is not ("REJECTED" or "SUPERSEDED"));
        Console.WriteLine($"[continuity] {hits.Count} claim(s) matching \"{text}\" — {live} live, " +
                          $"{hits.Count - live} rejected/superseded.");
        foreach (var c in hits.Take(limit))
            Console.WriteLine($"  {c.ClaimUid}  [{c.Status,-12}] {c.EntityName} :: {c.Predicate}  →  {c.Object}");

        if (hits.Count > limit)
            Console.WriteLine($"  … {hits.Count - limit} more (raise --limit).");
        return 0;
    }

    static async Task<int> CmdExtract(string[] args, IServiceProvider services)
    {
        var ext       = services.GetRequiredService<ContinuityExtractionService>();
        var bookRepo  = services.GetRequiredService<IBookRepository>();
        var chapterId = ArgValue(args, "--chapter");
        var bookId    = ArgValue(args, "--book");
        var nodeRef   = ArgValue(args, "--node");
        var entityRef = ArgValue(args, "--entity");
        var bibleRef  = ArgValue(args, "--outline");

        if (!string.IsNullOrEmpty(chapterId))
        {
            Console.WriteLine($"[continuity] Extracting from chapter {chapterId} (this may take a minute)…");
            try
            {
                var r = await ext.ExtractFromChapterAsync(chapterId);
                Console.WriteLine($"[continuity] ch.{r.ChapterNumber} {r.ChapterTitle} — candidates {r.CandidatesProposed}, validated {r.CandidatesValidated}");
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
            Console.WriteLine($"[continuity] Extracting from book \"{book.Title}\" — {book.ChapterIds?.Count ?? 0} chapters (minutes)…");
            try
            {
                var rs = await ext.ExtractFromBookAsync(book);
                int n = rs.Sum(r => r.NewClaims), cf = rs.Sum(r => r.ConfirmedClaims), ct = rs.Sum(r => r.ContradictedClaims);
                Console.WriteLine($"[continuity] Done. {n} new, {cf} confirmed, {ct} contradicted across {rs.Count} chapters.");
                return ct > 0 ? 1 : 0;
            }
            catch (Exception ex) { return Fail("extract failed: " + ex.Message); }
        }
        if (!string.IsNullOrEmpty(nodeRef))
        {
            var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
            await using var db = await dbFactory.CreateDbContextAsync();
            Guid nodeId;
            if (!Guid.TryParse(nodeRef, out nodeId))
            {
                // IgnoreQueryFilters(): explicit id/slug, not ambient scope (2026-08-17).
                var found = await db.Nodes.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(n => n.Slug == nodeRef);
                if (found == null) return Fail($"node not found: {nodeRef}");
                nodeId = found.Id;
            }
            Console.WriteLine($"[continuity] Extracting from BookNode {nodeRef} — every leaf chapter (minutes)…");
            try
            {
                var rs = await ext.ExtractFromBookNodeAsync(nodeId);
                int n = rs.Sum(r => r.NewClaims), cf = rs.Sum(r => r.ConfirmedClaims), ct = rs.Sum(r => r.ContradictedClaims);
                var failed = rs.Count(r => r.Error != null);
                Console.WriteLine($"[continuity] Done. {n} new, {cf} confirmed, {ct} contradicted across {rs.Count} chapters ({failed} failed).");
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
        if (!string.IsNullOrEmpty(bibleRef))
        {
            var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
            await using var db = await dbFactory.CreateDbContextAsync();
            Guid nodeId;
            if (!Guid.TryParse(bibleRef, out nodeId))
            {
                // IgnoreQueryFilters(): explicit id/slug, not ambient scope (2026-08-17 pattern).
                var found = await db.Nodes.IgnoreQueryFilters().AsNoTracking()
                    .FirstOrDefaultAsync(n => n.Slug == bibleRef || n.NodeCode == bibleRef);
                if (found == null) return Fail($"node not found: {bibleRef}");
                nodeId = found.Id;
            }
            var sectionType = ArgValue(args, "--section") ?? "Characters";
            Console.WriteLine($"[continuity] Extracting from bible for {bibleRef} (section={sectionType})…");
            try
            {
                var r = await ext.ExtractFromOutlineAsync(nodeId, sectionType);
                if (r.Error != null) return Fail(r.Error);
                Console.WriteLine($"[continuity] {r.ChapterTitle} — candidates {r.CandidatesProposed}, validated {r.CandidatesValidated}");
                Console.WriteLine($"[continuity] {r.NewClaims} new, {r.ConfirmedClaims} confirmed, {r.ContradictedClaims} contradicted, {r.UnknownEntities.Count} unknown entity references");
                if (r.UnknownEntities.Count > 0) Console.WriteLine("[continuity] unknown: " + string.Join(", ", r.UnknownEntities));
                return r.ContradictedClaims > 0 ? 1 : 0;
            }
            catch (Exception ex) { return Fail("extract failed: " + ex.Message); }
        }
        return Fail("extract requires one of: --chapter <id> | --book <id> | --node <nodeIdOrSlug> | --entity <guid> | --outline <slug>");
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
                .Where(e => entityKinds.Contains(e.EntityType))
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
        Console.WriteLine($"[sweep] Final state: total={s.Total} new={s.New} confirmed={s.Confirmed} contradicted={s.Contradicted} canonical={s.Canonical} rejected={s.Rejected} (prose={s.FromProse} record={s.FromEntityRecord} bible={s.FromBible})");
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
        "outline"            => $"story bible ({c.SourcePath ?? c.SourceChapterTitle})",
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
              prose --continuity groups --slug <slug> [--predicate <name>]
                  N-way contradiction groups (ClaimUid included) for --continuity resolve
              prose --continuity reset-book --slug <slug> [--note "..."]
                  Supersede every live claim for a book (before a fresh extraction pass)
              prose --continuity resolve --a <uid> --b <uid> --winner A|B|custom [--object "..."] [--note "..."]
              prose --continuity entity <name>
              prose --continuity search --text "<substring>" [--entity <id>] [--predicate-prefix <p>] [--live] [--limit N]
                  Free-text search over EntityName, Predicate AND Object, printing each ClaimUid.
                  The only way to find a fact hidden in an object string — a father claim recorded
                  as second_sword_possession -> "...made by father" matches no father_* prefix.
              prose --continuity reject --claim <uid> [--claim <uid> ...] [--note "..."]
              prose --continuity reject --entity <entityId> --predicate-prefix <p> --yes [--note "..."]
                  Reject one claim, several, or a whole predicate family on one entity. The only
                  path for "this fact is fabricated and has no counterpart to lose to" — resolve
                  needs a pair, reset-book takes the whole book. Reversible (system-versioned).
              prose --continuity extract --chapter <chapterId>
              prose --continuity extract --book <bookId>
              prose --continuity extract --node <nodeIdOrSlug>
              prose --continuity extract --entity <path-to-entity-json>
              prose --continuity extract --outline <nodeIdOrSlug> [--section Characters|ArcSummary|VoiceRegister|NarrativeLocks|BeatSpine]
                  extract claims from the story bible (NodeOutlineSections, default section Characters,
                  falls back to the raw NodeOutline blob) — lands as SourceType="outline" in the same
                  ledger prose/entity-record claims use, so bible facts compete/reconcile automatically
              prose --continuity apply --claim <claimUid>
              prose --continuity sweep [--book <id>] [--skip-records] [--skip-prose] [--skip-resolve] [--skip-apply] [--dry-run]
                  one-shot end-to-end pipeline: extract from records + chapters → auto-resolve via Legion DecideAsync → apply CANONICAL claims
            """);
    }
}
