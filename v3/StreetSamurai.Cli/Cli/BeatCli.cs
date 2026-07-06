using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Cli;

/// <summary>
/// <c>ss --beat &lt;subcommand&gt;</c> — fine-grained beat manipulation without the UI.
///
/// Subcommands:
///   insert  --node &lt;slug|id&gt; [--after &lt;beatId&gt;] [--text "..."]
///           Insert a new beat into a node. With no --after, inserts at the top.
///   delete  --id &lt;beatId&gt;
///           Delete a beat (soft-delete; the node loses it immediately).
///   update  --id &lt;beatId&gt; --text "..."
///           Replace a beat's prose. Use `--text -` to read from stdin.
///   meta    --id &lt;beatId&gt; [--title "..."] [--kind "..."] [--note "..."] [--in-world-date "..."]
///           Update beat metadata without touching prose.
///   show    --id &lt;beatId&gt;
///           Print a beat's full text and metadata.
///   list    --node &lt;slug|id&gt;
///           List beats in a node (position, id, first 80 chars of text).
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
            "insert" => await InsertAsync(rest, services),
            "delete" => await DeleteAsync(rest, services),
            "update" => await UpdateAsync(rest, services),
            "meta"   => await MetaAsync(rest, services),
            "show"   => await ShowAsync(rest, services),
            "list"   => await ListAsync(rest, services),
            _        => PrintUsage(),
        };
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
        var dbFactory = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();

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
            var sb = await db.NodeBeats.AsNoTracking().FirstOrDefaultAsync(x => x.BeatId == beatId);
            if (sb == null) { Console.Error.WriteLine($"[beat delete] Beat {beatId} not found in any node."); return 1; }
            nodeId = sb.NodeId;
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
        string? beatIdStr = null, title = null, kind = null, synopsis = null, subtext = null, tone = null, pace = null, role = null, sceneType = null;
        int act = 0;
        bool chapterStart = false;
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--id":            if (i + 1 < args.Length) beatIdStr = args[++i]; break;
                case "--title":         if (i + 1 < args.Length) title = args[++i]; break;
                case "--kind":          if (i + 1 < args.Length) kind = args[++i]; break;
                case "--synopsis":      if (i + 1 < args.Length) synopsis = args[++i]; break;
                case "--subtext":       if (i + 1 < args.Length) subtext = args[++i]; break;
                case "--tone":          if (i + 1 < args.Length) tone = args[++i]; break;
                case "--pace":          if (i + 1 < args.Length) pace = args[++i]; break;
                case "--role":          if (i + 1 < args.Length) role = args[++i]; break;
                case "--scene-type":    if (i + 1 < args.Length) sceneType = args[++i]; break;
                case "--act":           if (i + 1 < args.Length && int.TryParse(args[++i], out var a)) act = a; break;
                case "--chapter-start": chapterStart = true; break;
            }
        }
        if (string.IsNullOrWhiteSpace(beatIdStr)) { Console.Error.WriteLine("[beat meta] --id <beatGuid> is required."); return 1; }
        if (!Guid.TryParse(beatIdStr, out var beatId)) { Console.Error.WriteLine("[beat meta] --id must be a GUID."); return 1; }

        var workbench = services.GetRequiredService<NodeWorkbenchService>();
        var update = new NodeWorkbenchService.BeatMetadataUpdate(title, synopsis, subtext, tone, pace, role, act, sceneType, chapterStart, kind);
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

        var dbFactory = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();
        var beat = await db.Beats.AsNoTracking().FirstOrDefaultAsync(b => b.Id == beatId);
        if (beat == null) { Console.Error.WriteLine($"[beat show] Beat {beatId} not found."); return 1; }

        Console.WriteLine($"Id:           {beat.Id}");
        Console.WriteLine($"Title:        {beat.BeatTitle ?? "(none)"}");
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

    private static async Task<Guid?> ResolveNodeIdAsync(string idOrSlug, IServiceProvider services)
    {
        var dbFactory = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();
        if (Guid.TryParse(idOrSlug, out var g))
        {
            var byId = await db.Nodes.AsNoTracking().FirstOrDefaultAsync(s => s.Id == g);
            if (byId != null) return byId.Id;
        }
        var bySlug = await db.Nodes.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Slug == idOrSlug || s.NodeCode == idOrSlug);
        return bySlug?.Id;
    }

    private static int PrintUsage()
    {
        Console.Error.WriteLine("Usage: ss --beat <subcommand> [args]");
        Console.Error.WriteLine("  insert  --node <slug|id> [--after <beatId>] [--text \"...\"]");
        Console.Error.WriteLine("  delete  --id <beatId> [--node <slug|id>]");
        Console.Error.WriteLine("  update  --id <beatId> --text \"...\"  (use '-' for stdin)");
        Console.Error.WriteLine("  meta    --id <beatId> [--title \"...\"] [--kind \"...\"] [--synopsis \"...\"] [--tone \"...\"] [--pace \"...\"] [--role \"...\"] [--scene-type \"...\"] [--act N] [--chapter-start]");
        Console.Error.WriteLine("  show    --id <beatId>");
        Console.Error.WriteLine("  list    --node <slug|id>");
        return 1;
    }
}
