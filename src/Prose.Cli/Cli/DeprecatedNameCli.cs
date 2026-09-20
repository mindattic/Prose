using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// prose --deprecated-names --list [--universe &lt;slug&gt;]
/// prose --deprecated-names --add --universe &lt;slug&gt; --name &lt;deprecatedName&gt; --canonical &lt;canonicalName&gt; [--notes &lt;notes&gt;]
/// prose --deprecated-names --remove --id &lt;id&gt;
///
/// CRUD surface for DeprecatedEntityNames (the rule table NounConsistencyService/--validate-nouns
/// scans prose against). Before this file, --list and --add existed only as MCP tools
/// (list_deprecated_names / add_deprecated_name) and --remove had NO interface at all —
/// NounConsistencyService.DeleteRuleAsync was a complete, correct method (it also cleans up the
/// rule's Findings) that nothing could actually call. Added 2026-08-09 after a same-session
/// incident where 4 misapplied rules (entity-merge-dedup records misread as "ban this word in
/// prose") produced 91 false findings, including one that would have told an editor to
/// "correct" a NONFICTION book's reference to a real, different Bethany — fixing it required a
/// raw SQL DELETE only because no removal tool existed.
/// </summary>
public static class DeprecatedNameCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var svc = services.GetRequiredService<NounConsistencyService>();
        var canonDocs = services.GetRequiredService<CanonDocumentService>();

        if (args.Contains("--remove"))
        {
            var idStr = Flag(args, "--id");
            if (!long.TryParse(idStr, out var id))
            {
                Console.Error.WriteLine("[deprecated-names] --remove requires --id <numeric id>.");
                return 2;
            }
            var removed = await svc.DeleteRuleAsync(id);
            if (!removed)
            {
                Console.Error.WriteLine($"[deprecated-names] No rule with id {id}.");
                return 2;
            }
            Console.WriteLine($"[deprecated-names] Removed rule {id} and cleared any Findings it wrote.");
            return 0;
        }

        if (args.Contains("--add"))
        {
            var universeSlug = Flag(args, "--universe");
            var name         = Flag(args, "--name");
            var canonical    = Flag(args, "--canonical");
            var notes        = Flag(args, "--notes");
            if (universeSlug == null || name == null || canonical == null)
            {
                Console.Error.WriteLine("[deprecated-names] --add requires --universe, --name, --canonical.");
                return 2;
            }
            var universeId = await canonDocs.ResolveUniverseIdAsync(universeSlug);
            if (universeId == null)
            {
                Console.Error.WriteLine($"[deprecated-names] Unknown universe '{universeSlug}'.");
                return 2;
            }
            var rule = await svc.AddRuleAsync(universeId.Value, name, canonical, notes);
            Console.WriteLine($"[deprecated-names] Added rule {rule.Id}: '{rule.DeprecatedName}' -> '{rule.CanonicalName}'.");
            return 0;
        }

        // Default / --list
        Guid? filterUniverseId = null;
        var listUniverseSlug = Flag(args, "--universe");
        if (listUniverseSlug != null)
        {
            filterUniverseId = await canonDocs.ResolveUniverseIdAsync(listUniverseSlug);
            if (filterUniverseId == null)
            {
                Console.Error.WriteLine($"[deprecated-names] Unknown universe '{listUniverseSlug}'.");
                return 2;
            }
        }

        var rules = await svc.ListRulesAsync(filterUniverseId);
        if (rules.Count == 0)
        {
            Console.WriteLine("[deprecated-names] No rules registered.");
            return 0;
        }
        foreach (var r in rules)
            Console.WriteLine($"  [{r.Id}] '{r.DeprecatedName}' -> '{r.CanonicalName}'" +
                (string.IsNullOrWhiteSpace(r.Notes) ? "" : $"  — {r.Notes}"));
        return 0;
    }

    static string? Flag(string[] args, string name)
    {
        var idx = Array.IndexOf(args, name);
        return idx >= 0 && idx + 1 < args.Length ? args[idx + 1] : null;
    }
}
