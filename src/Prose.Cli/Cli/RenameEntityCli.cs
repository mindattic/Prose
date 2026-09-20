using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// prose --rename-entity --entity &lt;guid|slug&gt; --node &lt;book-guid|slug|code&gt;
///   --new-name "..." [--apply --yes] [--note "..."]
/// </summary>
public static class RenameEntityCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? entity = Flag(args, "--entity");
        string? node = Flag(args, "--node");
        string? newName = Flag(args, "--new-name");
        if (string.IsNullOrWhiteSpace(entity) || string.IsNullOrWhiteSpace(node) || string.IsNullOrWhiteSpace(newName))
        {
            Console.Error.WriteLine("Usage: prose --rename-entity --entity <guid|slug> --node <book-guid|slug|code> --new-name \"...\" [--apply --yes] [--note \"...\"]");
            return 2;
        }

        var rename = services.GetRequiredService<EntityRenameService>();
        if (!args.Contains("--apply"))
        {
            var preview = await rename.PreviewAsync(entity, node, newName);
            if (!preview.Ok) { Console.Error.WriteLine($"[rename-entity] {preview.Error}"); return 1; }
            Console.WriteLine($"[rename-entity] Preview: {preview.OldName} → {preview.NewName} ({preview.EntityId})");
            Console.WriteLine($"  outline sections: {preview.OutlineSections.Count}; beats: {preview.BeatIds.Count}; ledger claims: {preview.LedgerCount}");
            Console.WriteLine("Nothing changed. Re-run with --apply --yes after review.");
            return 0;
        }
        if (!args.Contains("--yes") && !args.Contains("--no-confirm"))
        {
            Console.Error.WriteLine("[rename-entity] --apply requires --yes after reviewing the preview.");
            return 2;
        }

        var result = await rename.ApplyAsync(entity, node, newName, Flag(args, "--note"));
        if (!result.Ok) { Console.Error.WriteLine($"[rename-entity] {result.Error}"); return 1; }
        Console.WriteLine($"[rename-entity] Renamed {result.OldName} → {result.NewName}; outline={result.OutlineSectionsChanged}, beats={result.BeatsChanged}, ledger={result.LedgerClaimsRelabeled}.");
        return 0;
    }

    static string? Flag(string[] args, string flag)
    {
        var i = Array.IndexOf(args, flag);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }
}
