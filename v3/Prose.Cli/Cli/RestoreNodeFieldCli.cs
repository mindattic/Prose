using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>prose --restore-node-field (--id &lt;guid&gt; | --slug &lt;slug&gt;) --archive-id &lt;guid&gt;
///     --field description|nodeoutline|summary|seed|subtitle|all</c>
///
/// Copies one (or all) of a Node's content fields FROM a chosen <c>ArchivedBook</c> snapshot
/// BACK onto the live Node row. Explicit and human-driven by design: the archive-id names
/// exactly which point in time to restore from (see <c>prose --list-archives</c> to find it) —
/// there is no "latest"/"current" inference here, so this can never restore the wrong version
/// by ambiguity. Refuses if the named archive doesn't belong to the target node.
/// </summary>
public static class RestoreNodeFieldCli
{
    private static readonly string[] ValidFields = ["description", "nodeoutline", "summary", "seed", "subtitle", "all"];

    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? id = null, slug = null, archiveId = null, field = null;
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--id":         if (i + 1 < args.Length) id = args[++i]; break;
                case "--slug":       if (i + 1 < args.Length) slug = args[++i]; break;
                case "--archive-id": if (i + 1 < args.Length) archiveId = args[++i]; break;
                case "--field":      if (i + 1 < args.Length) field = args[++i]?.ToLowerInvariant(); break;
            }
        }

        if (string.IsNullOrWhiteSpace(id) && string.IsNullOrWhiteSpace(slug))
        {
            Console.Error.WriteLine("[restore-node-field] One of --id or --slug is required.");
            Console.Error.WriteLine("Usage: prose --restore-node-field (--id <guid> | --slug <slug>) --archive-id <guid> --field description|nodeoutline|summary|seed|subtitle|all --universe <u>");
            return 2;
        }
        if (!Guid.TryParse(archiveId, out var archiveGuid))
        {
            Console.Error.WriteLine("[restore-node-field] --archive-id <guid> is required and must be a valid GUID.");
            return 2;
        }
        if (field == null || !ValidFields.Contains(field))
        {
            Console.Error.WriteLine($"[restore-node-field] --field must be one of: {string.Join(", ", ValidFields)}.");
            return 2;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        // NodeRefResolver accepts slug, NodeCode, GUID, or unique GUID prefix (2026-09-04 — this
        // was slug-or-GUID only and rejected a NodeCode like "BCODA" that every other command takes).
        var nodeRef = slug ?? id;
        var resolvedId = await NodeRefResolver.ResolveAsync(db, nodeRef);
        // IgnoreQueryFilters(): explicit id/slug, not ambient scope (2026-08-17).
        var node = resolvedId == null ? null
            : await db.Nodes.IgnoreQueryFilters().AsTracking().FirstOrDefaultAsync(n => n.Id == resolvedId.Value);

        if (node == null)
        {
            Console.Error.WriteLine($"[restore-node-field] {NodeRefResolver.NotFoundMessage(nodeRef)}");
            return 1;
        }

        var archive = await db.ArchivedBooks.AsNoTracking().FirstOrDefaultAsync(a => a.Id == archiveGuid);
        if (archive == null)
        {
            Console.Error.WriteLine($"[restore-node-field] No ArchivedBook found with id {archiveGuid}.");
            return 1;
        }
        if (archive.NodeId != node.Id)
        {
            Console.Error.WriteLine($"[restore-node-field] Archive {archiveGuid} belongs to a different node (NodeId={archive.NodeId}), not '{node.Title}' ({node.Slug}). Refusing.");
            return 1;
        }

        var restored = new List<string>();
        void Restore(string name, string? archivedValue, Action<string?> setter, Func<string?> getter)
        {
            var before = getter()?.Length ?? 0;
            setter(archivedValue);
            var after = archivedValue?.Length ?? 0;
            restored.Add($"{name}: {before} chars -> {after} chars");
        }

        if (field is "description" or "all") Restore("Description", archive.Description, v => node.Description = v, () => node.Description);
        if (field is "nodeoutline" or "all") Restore("NodeOutline", archive.NodeOutline, v => node.NodeOutline = v, () => node.NodeOutline);
        if (field is "summary" or "all") Restore("Summary", archive.Summary, v => node.Summary = v, () => node.Summary);
        if (field is "seed" or "all") Restore("Seed", archive.Seed, v => node.Seed = v, () => node.Seed);
        if (field is "subtitle" or "all") Restore("Subtitle", archive.Subtitle, v => node.Subtitle = v, () => node.Subtitle);

        node.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        Console.WriteLine($"[restore-node-field] Restored onto '{node.Title}' ({node.Slug}) from archive {archiveGuid} ({archive.CreatedAt:yyyy-MM-dd HH:mm:ss}Z, reason={archive.Reason}):");
        foreach (var line in restored)
            Console.WriteLine($"  {line}");

        return 0;
    }
}
