using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>prose --beat &lt;subcommand&gt;</c> — fine-grained beat manipulation without the UI.
///
/// Subcommands:
///   insert  --node &lt;slug|id&gt; [--after &lt;beatId&gt;] [--text "..."]
///           Insert a new beat into a node. With no --after, inserts at the top.
///   delete  --id &lt;beatId&gt;
///           Delete a beat (soft-delete; the node loses it immediately).
///   update  --id &lt;beatId&gt; --text "..."
///           Replace a beat's prose. Use `--text -` to read from stdin.
///   meta    --id &lt;beatId&gt; [--title "..."] [--kind "..."] [--note "..."] [--in-world-date "..."]
///           Update beat metadata without touching prose. Only the fields you pass change
///           (2026-08-24 — it used to reset every field you omitted); pass an empty string
///           to clear one, and --no-chapter-start to unset the chapter-start flag.
///   show    --id &lt;beatId&gt;
///           Print a beat's full text and metadata.
///   list    --node &lt;slug|id&gt;
///           List beats in a node (position, id, first 80 chars of text).
///   clear   --node &lt;slug|id&gt;
///           Soft-delete every currently-enabled beat in a node, in one process —
///           for a full replot where the whole existing beat set is being discarded.
///           Same soft-delete as `delete` (true = false); reversible via
///           the writer UI's restore, never a raw SQL delete.
///   seed-spine --node &lt;slug|id&gt;
///           Create planned (empty-text) beats from the book node's *already-saved*
///           NodeOutline "## BEAT SPINE" section — no LLM call, no bible regeneration.
///           For when a bible was hand-written (e.g. via set_book_outline) and the node
///           currently has zero enabled beats. No-op if the node already has beats.
/// </summary>
public static class BeatCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        if (args.Length == 0 || args[0].StartsWith("--"))
        {
            PrintUsage();
            return 1;
        }

        var sub = args[0];
        var rest = args[1..];

        return sub switch
        {
            "insert"     => await InsertAsync(rest, services),
            "delete"     => await DeleteAsync(rest, services),
            "clear"      => await ClearAsync(rest, services),
            "seed-spine" => await SeedSpineAsync(rest, services),
            "update"     => await UpdateAsync(rest, services),
            "meta"       => await MetaAsync(rest, services),
            "show"       => await ShowAsync(rest, services),
            "list"       => await ListAsync(rest, services),
            _            => PrintUsage(),
        };
    }

    // ── seed-spine ────────────────────────────────────────────────────────────

    private static async Task<int> SeedSpineAsync(string[] args, IServiceProvider services)
    {
        string? nodeIdOrSlug = null;
        for (int i = 0; i < args.Length; i++)
            if (args[i] == "--node" && i + 1 < args.Length) nodeIdOrSlug = args[++i];

        if (string.IsNullOrWhiteSpace(nodeIdOrSlug)) { Console.Error.WriteLine("[beat seed-spine] --node <slug|id> is required."); return 1; }

        var nodeId = await ResolveNodeIdAsync(nodeIdOrSlug, services);
        if (nodeId == null) { Console.Error.WriteLine($"[beat seed-spine] Node '{nodeIdOrSlug}' not found."); return 1; }

        var bibleSvc = services.GetRequiredService<NodeOutlineService>();
        var bibleText = await bibleSvc.GetBibleTextAsync(nodeId.Value);
        if (string.IsNullOrWhiteSpace(bibleText)) { Console.Error.WriteLine("[beat seed-spine] Node has no NodeOutline — set one first."); return 1; }

        var plans = NodeOutlineService.ParseBeatSpine(bibleText);
        if (plans.Count == 0) { Console.Error.WriteLine("[beat seed-spine] No \"## BEAT SPINE\" entries parsed from the bible."); return 1; }

        Console.WriteLine($"[beat seed-spine] Parsed {plans.Count} spine entries. Creating planned beats…");
        await bibleSvc.SaveBibleAndCreateBeatsAsync(nodeId.Value, bibleText);
        Console.WriteLine("[beat seed-spine] Done (no-op logged above if the node already had beats).");
        return 0;
    }

    // ── clear ─────────────────────────────────────────────────────────────────

    private static async Task<int> ClearAsync(string[] args, IServiceProvider services)
    {
        string? nodeIdOrSlug = null;
        bool directOnly = args.Contains("--direct-only");
        for (int i = 0; i < args.Length; i++)
            if (args[i] == "--node" && i + 1 < args.Length) nodeIdOrSlug = args[++i];

        if (string.IsNullOrWhiteSpace(nodeIdOrSlug)) { Console.Error.WriteLine("[beat clear] --node <slug|id> is required."); return 1; }

        var nodeId = await ResolveNodeIdAsync(nodeIdOrSlug, services);
        if (nodeId == null) { Console.Error.WriteLine($"[beat clear] Node '{nodeIdOrSlug}' not found."); return 1; }

        var workbench = services.GetRequiredService<NodeWorkbenchService>();
        // (NodeId, BeatId) pairs — beats live on chapter children, not the book node
        // (SS-A43), so DeleteBeatAsync must be called with each beat's ACTUAL owning
        // node, not the id/slug the caller passed in. Using the passed-in id blindly
        // (as this command originally did) silently no-ops when it's a book: the
        // junction lookup finds nothing under the book id and returns early.
        List<(Guid NodeId, Guid BeatId)> pairs;
        if (directOnly)
        {
            var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
            await using var db = await dbFactory.CreateDbContextAsync();
            pairs = await db.BeatNodes.Where(bn => bn.NodeId == nodeId.Value && true)
                .Select(bn => new ValueTuple<Guid, Guid>(bn.NodeId, bn.BeatId)).ToListAsync();
        }
        else
        {
            pairs = (await workbench.GetOrderedBeatsAsync(nodeId.Value))
                .Select(ob => (ob.NodeId, ob.Beat.Id)).ToList();
        }

        Console.WriteLine($"[beat clear] Soft-deleting {pairs.Count} beat(s) from node {nodeId.Value}{(directOnly ? " (direct only)" : "")}…");
        int n = 0;
        foreach (var (ownerNodeId, beatId) in pairs)
        {
            await workbench.DeleteBeatAsync(ownerNodeId, beatId);
            n++;
        }
        Console.WriteLine($"[beat clear] Done. {n} beat(s) disabled.");
        return 0;
    }

    // ── insert ────────────────────────────────────────────────────────────────

    private static async Task<int> InsertAsync(string[] args, IServiceProvider services)
    {
        string? nodeIdOrSlug = null, afterBeatId = null, text = null;
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--node": if (i + 1 < args.Length) nodeIdOrSlug = args[++i]; break;
                case "--after":  if (i + 1 < args.Length) afterBeatId = args[++i]; break;
                case "--text":   if (i + 1 < args.Length) text = args[++i]; break;
            }
        }
        if (string.IsNullOrWhiteSpace(nodeIdOrSlug)) { Console.Error.WriteLine("[beat insert] --node is required."); return 1; }

        var workbench = services.GetRequiredService<NodeWorkbenchService>();
        var nodeId = await ResolveNodeIdAsync(nodeIdOrSlug, services);
        if (nodeId == null) { Console.Error.WriteLine($"[beat insert] Node '{nodeIdOrSlug}' not found."); return 1; }

        Guid? afterId = null;
        if (!string.IsNullOrWhiteSpace(afterBeatId))
        {
            if (!Guid.TryParse(afterBeatId, out var ag)) { Console.Error.WriteLine("[beat insert] --after must be a GUID."); return 1; }
            afterId = ag;
        }

        if (text == "-") text = await Console.In.ReadToEndAsync();

        var beat = await workbench.InsertBeatAsync(nodeId.Value, afterId, text ?? "");
        Console.WriteLine($"[beat insert] Created beat {beat.Id} at position after={afterId?.ToString() ?? "top"}.");
        return 0;
    }

    // ── delete ────────────────────────────────────────────────────────────────

    private static async Task<int> DeleteAsync(string[] args, IServiceProvider services)
    {
        string? beatIdStr = null, nodeIdOrSlug = null;
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--id":     if (i + 1 < args.Length) beatIdStr = args[++i]; break;
                case "--node": if (i + 1 < args.Length) nodeIdOrSlug = args[++i]; break;
            }
        }
        if (string.IsNullOrWhiteSpace(beatIdStr)) { Console.Error.WriteLine("[beat delete] --id <beatGuid> is required."); return 1; }
        if (!Guid.TryParse(beatIdStr, out var beatId)) { Console.Error.WriteLine("[beat delete] --id must be a GUID."); return 1; }

        var workbench = services.GetRequiredService<NodeWorkbenchService>();
        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();

        Guid nodeId;
        if (!string.IsNullOrWhiteSpace(nodeIdOrSlug))
        {
            var sid = await ResolveNodeIdAsync(nodeIdOrSlug, services);
            if (sid == null) { Console.Error.WriteLine($"[beat delete] Node '{nodeIdOrSlug}' not found."); return 1; }
            nodeId = sid.Value;
        }
        else
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            var sb = await db.BeatNodes.AsNoTracking().FirstOrDefaultAsync(x => x.BeatId == beatId);
            if (sb == null)
            {
                // No BeatNodes row anywhere for this beat — it may still be a real, fully
                // orphaned row (e.g. a superseded draft never linked to any chapter). Confirm
                // the beat itself exists, then pass Guid.Empty through so DeleteBeatAsync's
                // zero-anywhere branch can remove the dangling row directly.
                var exists = await db.Beats.AsNoTracking().AnyAsync(b => b.Id == beatId);
                if (!exists) { Console.Error.WriteLine($"[beat delete] Beat {beatId} not found."); return 1; }
                nodeId = Guid.Empty;
            }
            else
            {
                nodeId = sb.NodeId;
            }
        }

        await workbench.DeleteBeatAsync(nodeId, beatId);
        Console.WriteLine($"[beat delete] Deleted beat {beatId}.");
        return 0;
    }

    // ── update ────────────────────────────────────────────────────────────────

    private static async Task<int> UpdateAsync(string[] args, IServiceProvider services)
    {
        string? beatIdStr = null, text = null;
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--id":   if (i + 1 < args.Length) beatIdStr = args[++i]; break;
                case "--text": if (i + 1 < args.Length) text = args[++i]; break;
            }
        }
        if (string.IsNullOrWhiteSpace(beatIdStr)) { Console.Error.WriteLine("[beat update] --id <beatGuid> is required."); return 1; }
        if (!Guid.TryParse(beatIdStr, out var beatId)) { Console.Error.WriteLine("[beat update] --id must be a GUID."); return 1; }
        if (string.IsNullOrWhiteSpace(text)) { Console.Error.WriteLine("[beat update] --text is required (or '-' for stdin)."); return 1; }

        if (text == "-") text = await Console.In.ReadToEndAsync();

        var workbench = services.GetRequiredService<NodeWorkbenchService>();
        await workbench.UpdateBeatTextAsync(beatId, text);
        Console.WriteLine($"[beat update] Beat {beatId} updated ({text.Length} chars).");
        return 0;
    }

    // ── meta ──────────────────────────────────────────────────────────────────

    private static async Task<int> MetaAsync(string[] args, IServiceProvider services)
    {
        string? beatIdStr = null, title = null, kind = null, description = null, subtext = null, tone = null, pace = null, role = null, sceneType = null;
        // Nullable so an unsupplied flag stays unsupplied: BeatMetadataUpdate treats null as
        // "leave this column alone". `int act = 0` / `bool chapterStart = false` previously meant
        // every `--beat meta` call reset Act to 0 and cleared IsChapterStart, so a title edit on a
        // chapter-opening beat silently demoted it.
        int? act = null;
        bool? chapterStart = null;
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--id":            if (i + 1 < args.Length) beatIdStr = args[++i]; break;
                case "--title":         if (i + 1 < args.Length) title = args[++i]; break;
                case "--kind":          if (i + 1 < args.Length) kind = args[++i]; break;
                case "--description":      if (i + 1 < args.Length) description = args[++i]; break;
                case "--subtext":       if (i + 1 < args.Length) subtext = args[++i]; break;
                case "--tone":          if (i + 1 < args.Length) tone = args[++i]; break;
                case "--pace":          if (i + 1 < args.Length) pace = args[++i]; break;
                case "--role":          if (i + 1 < args.Length) role = args[++i]; break;
                case "--scene-type":    if (i + 1 < args.Length) sceneType = args[++i]; break;
                case "--act":           if (i + 1 < args.Length && int.TryParse(args[++i], out var a)) act = a; break;
                case "--chapter-start": chapterStart = true; break;
                // Without this there is no way to UNSET the flag — the old code cleared it as a
                // side effect of every call, which is not a feature you can rely on.
                case "--no-chapter-start": chapterStart = false; break;
            }
        }
        if (string.IsNullOrWhiteSpace(beatIdStr)) { Console.Error.WriteLine("[beat meta] --id <beatGuid> is required."); return 1; }
        if (!Guid.TryParse(beatIdStr, out var beatId)) { Console.Error.WriteLine("[beat meta] --id must be a GUID."); return 1; }

        var workbench = services.GetRequiredService<NodeWorkbenchService>();
        var update = new NodeWorkbenchService.BeatMetadataUpdate(title, description, subtext, tone, pace, role, act, sceneType, chapterStart, kind);
        await workbench.UpdateBeatMetadataAsync(beatId, update);
        Console.WriteLine($"[beat meta] Beat {beatId} metadata updated.");
        return 0;
    }

    // ── show ──────────────────────────────────────────────────────────────────

    private static async Task<int> ShowAsync(string[] args, IServiceProvider services)
    {
        string? beatIdStr = null;
        for (int i = 0; i < args.Length; i++)
            if (args[i] == "--id" && i + 1 < args.Length) beatIdStr = args[++i];

        if (string.IsNullOrWhiteSpace(beatIdStr)) { Console.Error.WriteLine("[beat show] --id <beatGuid> is required."); return 1; }
        if (!Guid.TryParse(beatIdStr, out var beatId)) { Console.Error.WriteLine("[beat show] --id must be a GUID."); return 1; }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();
        var beat = await db.Beats.AsNoTracking().FirstOrDefaultAsync(b => b.Id == beatId);
        if (beat == null) { Console.Error.WriteLine($"[beat show] Beat {beatId} not found."); return 1; }

        Console.WriteLine($"Id:           {beat.Id}");
        Console.WriteLine($"Title:        {beat.Title ?? "(none)"}");
        Console.WriteLine($"Kind:         {beat.Kind ?? "(none)"}");
        Console.WriteLine($"UpdatedAt:    {beat.UpdatedAt:u}");
        Console.WriteLine();
        Console.WriteLine(beat.Text ?? "(empty)");
        return 0;
    }

    // ── list ──────────────────────────────────────────────────────────────────

    private static async Task<int> ListAsync(string[] args, IServiceProvider services)
    {
        string? nodeIdOrSlug = null;
        for (int i = 0; i < args.Length; i++)
            if (args[i] == "--node" && i + 1 < args.Length) nodeIdOrSlug = args[++i];

        if (string.IsNullOrWhiteSpace(nodeIdOrSlug)) { Console.Error.WriteLine("[beat list] --node <slug|id> is required."); return 1; }

        var nodeId = await ResolveNodeIdAsync(nodeIdOrSlug, services);
        if (nodeId == null) { Console.Error.WriteLine($"[beat list] Node '{nodeIdOrSlug}' not found."); return 1; }

        var workbench = services.GetRequiredService<NodeWorkbenchService>();
        var beats = await workbench.GetOrderedBeatsAsync(nodeId.Value);

        Console.WriteLine($"{"Pos",-5} {"Id",-36} {"Text preview"}");
        Console.WriteLine(new string('-', 100));
        int pos = 1;
        foreach (var ob in beats)
        {
            var preview = (ob.Beat.Text ?? "").Replace('\n', ' ');
            if (preview.Length > 80) preview = preview[..80] + "…";
            Console.WriteLine($"{pos,-5} {ob.Beat.Id,-36} {preview}");
            pos++;
        }
        Console.WriteLine($"\n{beats.Count} beats.");
        return 0;
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// 2026-08-23: its slug/code branch was missing the <c>IgnoreQueryFilters()</c> its GUID
    /// branch had, so an explicit slug resolved to null cross-universe. Delegates to
    /// <see cref="NodeRefResolver"/>.
    /// </summary>
    private static Task<Guid?> ResolveNodeIdAsync(string idOrSlug, IServiceProvider services) =>
        NodeRefResolver.ResolveAsync(
            services.GetRequiredService<IDbContextFactory<ProseDbContext>>(), idOrSlug);

    private static int PrintUsage()
    {
        Console.Error.WriteLine("Usage: prose --beat <subcommand> [args]");
        Console.Error.WriteLine("  insert  --node <slug|id> [--after <beatId>] [--text \"...\"]");
        Console.Error.WriteLine("  delete  --id <beatId> [--node <slug|id>]");
        Console.Error.WriteLine("  clear   --node <slug|id>  (soft-delete every enabled beat in the node)");
        Console.Error.WriteLine("  seed-spine --node <slug|id>  (create planned beats from the node's saved bible spine)");
        Console.Error.WriteLine("  update  --id <beatId> --text \"...\"  (use '-' for stdin)");
        Console.Error.WriteLine("  meta    --id <beatId> [--title \"...\"] [--kind \"...\"] [--description \"...\"] [--tone \"...\"] [--pace \"...\"] [--role \"...\"] [--scene-type \"...\"] [--act N] [--chapter-start | --no-chapter-start]");
        Console.Error.WriteLine("          (only the fields you pass change; pass \"\" to clear one)");
        Console.Error.WriteLine("  show    --id <beatId>");
        Console.Error.WriteLine("  list    --node <slug|id>");
        return 1;
    }
}
