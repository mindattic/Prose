using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;

namespace Prose.Cli;

/// <summary>
/// prose --set-node-code (--slug &lt;slug|code|id&gt; | --id &lt;id&gt;) --to &lt;CODE&gt; [--apply]
/// — changes a book's NodeCode (the short code its exports, reports and the author use: SRZR, CYB1...).
///
/// No path set it after creation: --create-book takes --code, --rename-node changes Title/Slug only.
/// The author renamed Ren's book code to REN on 2026-11-18 (PRS-T1016), which is what this exists for.
///
/// Dry run unless --apply. Refuses a code that is not 2-12 letters/digits, or one another node in the
/// same universe already carries (codes resolve nodes, so a duplicate would make the code ambiguous).
/// The export folder follows the code on the next --export-node; old folders are left where they are.
/// </summary>
public static class SetNodeCodeCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? Flag(string name) { for (var i = 0; i < args.Length - 1; i++) if (args[i] == name) return args[i + 1]; return null; }

        var nodeRef = Flag("--slug") ?? Flag("--id") ?? Flag("--node");
        var target = Flag("--to")?.Trim().ToUpperInvariant();
        var apply = args.Contains("--apply");

        if (string.IsNullOrWhiteSpace(nodeRef) || string.IsNullOrWhiteSpace(target))
        {
            Console.Error.WriteLine("Usage: prose --set-node-code --slug <slug|code|id> --to <CODE> [--apply]");
            return 1;
        }
        if (!System.Text.RegularExpressions.Regex.IsMatch(target, "^[A-Z0-9]{2,12}$"))
        {
            Console.Error.WriteLine($"[set-node-code] REFUSED: '{target}' is not 2-12 letters or digits.");
            return 1;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        var nodeId = await Prose.Core.Services.NodeRefResolver.ResolveAsync(db, nodeRef);
        var node = nodeId == null ? null : await db.Nodes.IgnoreQueryFilters().FirstOrDefaultAsync(n => n.Id == nodeId.Value);
        if (node == null) { Console.Error.WriteLine($"[set-node-code] Node '{nodeRef}' not found."); return 1; }

        var clash = await db.Nodes.IgnoreQueryFilters()
            .Where(n => n.Id != node.Id && n.UniverseId == node.UniverseId && n.NodeCode != null && n.NodeCode.ToUpper() == target)
            .Select(n => new { n.Title, n.Slug })
            .FirstOrDefaultAsync();
        if (clash != null)
        {
            Console.Error.WriteLine($"[set-node-code] REFUSED: code {target} already belongs to \"{clash.Title}\" ({clash.Slug}).");
            return 1;
        }

        var previous = node.NodeCode ?? "(none)";
        if (!apply)
        {
            Console.WriteLine($"[set-node-code] DRY RUN: \"{node.Title}\" {previous} -> {target}. Re-run with --apply to write.");
            return 0;
        }

        node.NodeCode = target;
        await db.SaveChangesAsync();

        var readBack = await db.Nodes.IgnoreQueryFilters().AsNoTracking()
            .Where(n => n.Id == node.Id).Select(n => n.NodeCode).FirstOrDefaultAsync();
        if (readBack != target)
        {
            Console.Error.WriteLine($"[set-node-code] VERIFY FAILED: read back '{readBack}'.");
            return 3;
        }
        Console.WriteLine($"[set-node-code] \"{node.Title}\" {previous} -> {target} (read back).");
        return 0;
    }
}
