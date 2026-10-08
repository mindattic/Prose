using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// Corrections to world rows that no record writer reaches. Each is dry-run unless --apply, takes
/// explicit ids, and reads the row back. See <see cref="CanonMaintenanceService"/>.
///
///   prose --set-entity-slug --id &lt;entity guid&gt; --to &lt;slug&gt; [--apply]
///   prose --set-entity-name --id &lt;entity guid&gt; --to "&lt;name&gt;" [--apply]   (the entity row's name only)
///   prose --edit-edge --edge &lt;edge id&gt; [--target &lt;entity guid&gt;] [--description "&lt;text&gt;"] [--apply]
///   prose --prune-orphan-tag --name "&lt;tag&gt;" [--apply]
///   prose --delete-obligation --id &lt;obligation guid&gt; [--apply]
///
/// Exit codes: 0 ok (planned or written) · 1 bad args / refused.
/// </summary>
public static class CanonMaintenanceCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? Flag(string name) { var i = Array.IndexOf(args, name); return i >= 0 && i + 1 < args.Length ? args[i + 1] : null; }
        var apply = args.Contains("--apply");
        var svc = services.GetRequiredService<CanonMaintenanceService>();
        MaintenanceResult r;

        if (args.Contains("--set-entity-slug"))
        {
            if (!Guid.TryParse(Flag("--id"), out var id) || Flag("--to") is not { } slug)
            { Console.Error.WriteLine("Usage: prose --set-entity-slug --id <entity guid> --to <slug> [--apply]"); return 1; }
            r = await svc.SetEntitySlugAsync(id, slug, apply);
        }
        else if (args.Contains("--set-entity-name"))
        {
            if (!Guid.TryParse(Flag("--id"), out var id) || Flag("--to") is not { } name)
            { Console.Error.WriteLine("Usage: prose --set-entity-name --id <entity guid> --to \"<name>\" [--apply]"); return 1; }
            r = await svc.SetEntityNameAsync(id, name, apply);
        }
        else if (args.Contains("--edit-edge"))
        {
            if (!long.TryParse(Flag("--edge"), out var edge))
            { Console.Error.WriteLine("Usage: prose --edit-edge --edge <edge id> [--target <entity guid>] [--description \"<text>\"] [--apply]"); return 1; }
            Guid? target = null;
            if (Flag("--target") is { } rawTarget)
            {
                if (!Guid.TryParse(rawTarget, out var t)) { Console.Error.WriteLine($"[edit-edge] --target '{rawTarget}' is not an entity id."); return 1; }
                target = t;
            }
            r = await svc.EditEdgeAsync(edge, target, Flag("--description"), apply);
        }
        else if (args.Contains("--prune-orphan-tag"))
        {
            if (Flag("--name") is not { } name) { Console.Error.WriteLine("Usage: prose --prune-orphan-tag --name \"<tag>\" [--apply]"); return 1; }
            r = await svc.PruneOrphanTagAsync(name, apply);
        }
        else
        {
            if (!Guid.TryParse(Flag("--id"), out var id)) { Console.Error.WriteLine("Usage: prose --delete-obligation --id <obligation guid> [--apply]"); return 1; }
            r = await svc.DeleteObligationAsync(id, apply);
        }

        (r.Ok ? Console.Out : Console.Error).WriteLine($"[maintenance] {r.Message}");
        if (r.Ok && !r.Applied && r.Message.StartsWith("DRY RUN")) Console.WriteLine("[maintenance] Re-run with --apply to write it.");
        return r.Ok ? 0 : 1;
    }
}
