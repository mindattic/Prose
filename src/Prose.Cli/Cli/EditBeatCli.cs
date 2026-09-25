using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>prose --edit-beat</c> — overwrite one beat's prose, or insert a new beat after a given position.
///
/// Edit mode (default):
///   --slug &lt;slug&gt;           Node slug.
///   --beat-number &lt;N&gt;       1-indexed beat position in reading order.
///   --file &lt;path&gt;           Path to a text file whose contents replace the beat prose.
///
/// Insert mode (--insert-after):
///   --slug &lt;slug&gt;           Node slug.
///   --insert-after &lt;N&gt;      Insert a new beat after position N (0 = insert at top).
///   --file &lt;path&gt;           Path to a text file whose contents become the new beat prose.
///
/// Either mode:
///   --defer-analysis        Skip the four post-save analysis tails (intent drift, blast-radius
///                           logic sweep, continuity re-extract, obligation scan). Each is an LLM
///                           call, so a hand-edit costs roughly $0.02–0.03 without this flag.
///                           For a multi-beat docket that is real money spent re-analysing a book
///                           mid-splice, when what the author wants is one analysis pass at the
///                           end. Use it for batches; leave it off for a one-off edit where the
///                           immediate feedback is the point. <c>deferAnalysis</c> has always been
///                           a parameter on <see cref="NodeWorkbenchService.UpdateBeatTextAsync"/>;
///                           it simply had no CLI surface until 2026-09-22.
///
/// Exit codes: 0 = success, 1 = bad args / node not found / beat not found.
/// </summary>
public static class EditBeatCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? slug = null, filePath = null, idStr = null;
        int beatNumber = 0, insertAfter = -1;
        bool insertMode = false;
        var deferAnalysis = args.Contains("--defer-analysis");

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--slug":         if (i + 1 < args.Length) slug        = args[++i]; break;
                case "--id":           if (i + 1 < args.Length) idStr       = args[++i]; break;
                // Strict: a value that does not parse used to become 0, and --insert-after 0 means
                // "top of the book" — "12a" or "-3" silently inserted prose at the very start.
                case "--beat-number":
                    if (i + 1 >= args.Length || !int.TryParse(args[++i], out beatNumber)) { Console.Error.WriteLine("[edit-beat] --beat-number needs a number."); return 1; }
                    break;
                case "--insert-after":
                    insertMode = true;
                    if (i + 1 >= args.Length || !int.TryParse(args[++i], out insertAfter) || insertAfter < 0)
                    { Console.Error.WriteLine("[edit-beat] --insert-after needs a position ≥ 0."); return 1; }
                    break;
                case "--file":         if (i + 1 < args.Length) filePath    = args[++i]; break;
            }
        }

        // ── Edit-by-id mode: splice one beat by its exact GUID (position/node agnostic).
        // Routes through the same workbench path so the edit is logged to the open EditSession.
        if (!string.IsNullOrWhiteSpace(idStr))
        {
            if (!Guid.TryParse(idStr, out var beatId))
            {
                Console.Error.WriteLine($"[edit-beat] --id is not a valid GUID: {idStr}");
                return 1;
            }
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                Console.Error.WriteLine("[edit-beat] --file is required and must exist.");
                return 1;
            }
            // Strict UTF-8 (a BOM is still honoured): the default decoder turned an ANSI file's dashes and curly quotes into U+FFFD, silently.
            string proseById;
            try { proseById = (await File.ReadAllTextAsync(filePath, new System.Text.UTF8Encoding(false, throwOnInvalidBytes: true))).Trim(); }
            catch (System.Text.DecoderFallbackException) { Console.Error.WriteLine($"[edit-beat] {filePath} is not valid UTF-8 — save it as UTF-8."); return 1; }
            if (string.IsNullOrWhiteSpace(proseById))
            {
                Console.Error.WriteLine("[edit-beat] Prose file is empty.");
                return 1;
            }
            var wb  = services.GetRequiredService<NodeWorkbenchService>();
            var dbf = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
            var sessionSvc = services.GetRequiredService<EditSessionService>();

            // Capture prior version/hash so we can log the edit to the session synchronously —
            // the workbench's own logging is fire-and-forget and gets dropped when the CLI exits.
            int priorVersion; string? priorHash;
            await using (var db = await dbf.CreateDbContextAsync())
            {
                var b = await db.Beats.AsNoTracking().FirstOrDefaultAsync(x => x.Id == beatId);
                if (b == null) { Console.Error.WriteLine($"[edit-beat] Beat {beatId} not found."); return 1; }
                priorVersion = b.Version; priorHash = b.TextHash;
            }

            Console.Write($"[edit-beat] Updating beat {beatId}… ");
            await wb.UpdateBeatTextAsync(beatId, proseById, BeatWriteReason.AuthorEdit,
                                         expectedUpdatedAt: null, deferAnalysis: deferAnalysis);
            await sessionSvc.TryLogBeatAsync(beatId, priorVersion, priorHash);   // synchronous — reliably logged
            Console.WriteLine($"ok ({proseById.Length} chars){(deferAnalysis ? ", analysis deferred" : "")}.");
            return 0;
        }

        if (string.IsNullOrWhiteSpace(slug))
        {
            Console.Error.WriteLine("[edit-beat] --slug is required.");
            return 1;
        }
        if (string.IsNullOrWhiteSpace(filePath))
        {
            Console.Error.WriteLine("[edit-beat] --file is required.");
            return 1;
        }
        if (!File.Exists(filePath))
        {
            Console.Error.WriteLine($"[edit-beat] File not found: {filePath}");
            return 1;
        }
        if (!insertMode && beatNumber < 1)
        {
            Console.Error.WriteLine("[edit-beat] --beat-number must be ≥1, or use --insert-after.");
            return 1;
        }

        // Strict UTF-8 (a BOM is still honoured): the default decoder turned an ANSI file's dashes and curly quotes into U+FFFD, silently.
        string prose;
        try { prose = (await File.ReadAllTextAsync(filePath, new System.Text.UTF8Encoding(false, throwOnInvalidBytes: true))).Trim(); }
        catch (System.Text.DecoderFallbackException) { Console.Error.WriteLine($"[edit-beat] {filePath} is not valid UTF-8 — save it as UTF-8."); return 1; }
        if (string.IsNullOrWhiteSpace(prose))
        {
            Console.Error.WriteLine("[edit-beat] Prose file is empty.");
            return 1;
        }

        var dbFactory  = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        var workbench  = services.GetRequiredService<NodeWorkbenchService>();

        // Resolve node
        Guid nodeId;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            // IgnoreQueryFilters(): explicit id/slug, not ambient scope (2026-08-17).
            // NodeRefResolver: a book code (BCODA) works as well as its slug.
            var node = await NodeRefResolver.ResolveNodeAsync(db, slug);
            if (node == null) { Console.Error.WriteLine($"[edit-beat] Node '{slug}' not found."); return 1; }
            nodeId = node.Id;
        }

        var ordered = await workbench.GetOrderedBeatsAsync(nodeId);

        if (insertMode)
        {
            // Resolve the anchor's REAL owning chapter node, not the raw --slug node — for a
            // book whose beats live on a child chapter (the normal Book->Chapter->Beat shape),
            // InsertBeatAsync needs that chapter's id or it can't find the anchor at all (same
            // bug class fixed in SetBeatEnabledCli 2026-08-31: VIGL's beats live on its chapter
            // node, not the book node --slug resolves to).
            Guid? afterId = null;
            // Position 0 goes at the top of the FIRST chapter, not onto the book node itself, where a
            // beat sits outside every chapter (and outside the chapter-leaf walks).
            Guid insertNodeId = ordered.Count > 0 ? ordered[0].NodeId : nodeId;
            if (insertAfter > 0)
            {
                if (insertAfter > ordered.Count)
                {
                    Console.Error.WriteLine($"[edit-beat] --insert-after {insertAfter} exceeds beat count ({ordered.Count}).");
                    return 1;
                }
                var anchor = ordered[insertAfter - 1];
                afterId = anchor.Beat.Id;
                insertNodeId = anchor.NodeId;
            }
            var newBeat = await workbench.InsertBeatAsync(insertNodeId, afterId, prose);
            Console.WriteLine($"[edit-beat] Inserted new beat after position {insertAfter} (chapter {insertNodeId}) → id {newBeat.Id} ({prose.Length} chars).");
            return 0;
        }

        if (beatNumber > ordered.Count)
        {
            Console.Error.WriteLine($"[edit-beat] --beat-number {beatNumber} exceeds beat count ({ordered.Count}).");
            return 1;
        }

        var target = ordered[beatNumber - 1].Beat;
        Console.Write($"[edit-beat] Updating beat #{beatNumber} (id {target.Id})… ");
        await workbench.UpdateBeatTextAsync(target.Id, prose, BeatWriteReason.AuthorEdit,
                                            expectedUpdatedAt: null, deferAnalysis: deferAnalysis);
        Console.WriteLine($"ok ({prose.Length} chars){(deferAnalysis ? ", analysis deferred" : "")}.");
        return 0;
    }
}
