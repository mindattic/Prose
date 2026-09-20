using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// prose --banned-names --list
/// prose --banned-names --add --name &lt;name&gt; [--notes &lt;notes&gt;]
/// prose --banned-names --remove --id &lt;id&gt;
///
/// CRUD surface for BannedNames (2026-08-26) — the Prose-wide, no-universe-scope, no-canonical-
/// replacement hard ban enforced at write time by Services.WriteGate.BannedNameSyncCheck. Unlike
/// --deprecated-names (a per-universe rename map only ever scanned after the fact), adding a
/// name here rejects any new/modified Entity.Name or alias containing it, everywhere, starting
/// immediately — but never touches rows that already exist (forward-only, author ruling
/// 2026-08-26).
/// </summary>
public static class BannedNameCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var svc = services.GetRequiredService<BannedNameService>();

        if (args.Contains("--remove"))
        {
            var idStr = Flag(args, "--id");
            if (!long.TryParse(idStr, out var id))
            {
                Console.Error.WriteLine("[banned-names] --remove requires --id <numeric id>.");
                return 2;
            }
            var removed = await svc.RemoveAsync(id);
            if (!removed)
            {
                Console.Error.WriteLine($"[banned-names] No banned name with id {id}.");
                return 2;
            }
            Console.WriteLine($"[banned-names] Removed banned name {id}.");
            return 0;
        }

        if (args.Contains("--add"))
        {
            var name = Flag(args, "--name");
            var notes = Flag(args, "--notes");
            if (name == null)
            {
                Console.Error.WriteLine("[banned-names] --add requires --name.");
                return 2;
            }
            var row = await svc.AddAsync(name, notes);
            Console.WriteLine($"[banned-names] Banned '{row.Name}' (id {row.Id}) — Prose-wide, forward-only.");
            return 0;
        }

        // Default / --list
        var rows = await svc.ListAsync();
        if (rows.Count == 0)
        {
            Console.WriteLine("[banned-names] No names banned.");
            return 0;
        }
        foreach (var r in rows)
            Console.WriteLine($"  [{r.Id}] '{r.Name}'" + (string.IsNullOrWhiteSpace(r.Notes) ? "" : $"  — {r.Notes}"));
        return 0;
    }

    static string? Flag(string[] args, string name)
    {
        var idx = Array.IndexOf(args, name);
        return idx >= 0 && idx + 1 < args.Length ? args[idx + 1] : null;
    }
}
