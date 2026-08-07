using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// CLI surface for <see cref="FamilyTieService"/>. Hand-seeding family ties
/// before the genetics walker can do anything useful.
///
///   ss --family parent  --parent &lt;id|slug&gt; --child &lt;id|slug&gt;
///   ss --family sibling --a &lt;id|slug&gt; --b &lt;id|slug&gt;
///   ss --family spouse  --a &lt;id|slug&gt; --b &lt;id|slug&gt;
///   ss --family show    --of &lt;id|slug&gt;
/// </summary>
public static class FamilyCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider sp)
    {
        var idx = Array.IndexOf(args, "--family");
        var sub = idx >= 0 && idx + 1 < args.Length ? args[idx + 1] : null;
        if (string.IsNullOrWhiteSpace(sub))
        {
            Console.Error.WriteLine("Usage: ss --family <parent|sibling|spouse|show> ...");
            return 2;
        }

        var fam    = sp.GetRequiredService<FamilyTieService>();
        var export = sp.GetRequiredService<CanonExportService>();

        async Task<Guid?> Resolve(string token) => await export.ResolveEntityIdAsync(token);

        switch (sub)
        {
            case "parent":
            {
                var parent = GetArg(args, "--parent");
                var child  = GetArg(args, "--child");
                if (parent == null || child == null) { Console.Error.WriteLine("--parent and --child required"); return 2; }
                var pid = await Resolve(parent); var cid = await Resolve(child);
                if (pid == null || cid == null) { Console.Error.WriteLine("could not resolve one or both ids"); return 1; }
                await fam.AddParentAsync(pid.Value, cid.Value);
                Console.WriteLine($"linked: parent {pid} -> child {cid}");
                return 0;
            }
            case "sibling":
            {
                var a = GetArg(args, "--a"); var b = GetArg(args, "--b");
                if (a == null || b == null) { Console.Error.WriteLine("--a and --b required"); return 2; }
                var ida = await Resolve(a); var idb = await Resolve(b);
                if (ida == null || idb == null) { Console.Error.WriteLine("could not resolve one or both ids"); return 1; }
                await fam.AddSiblingAsync(ida.Value, idb.Value);
                Console.WriteLine($"linked: siblings {ida} <-> {idb}");
                return 0;
            }
            case "spouse":
            {
                var a = GetArg(args, "--a"); var b = GetArg(args, "--b");
                if (a == null || b == null) { Console.Error.WriteLine("--a and --b required"); return 2; }
                var ida = await Resolve(a); var idb = await Resolve(b);
                if (ida == null || idb == null) { Console.Error.WriteLine("could not resolve one or both ids"); return 1; }
                await fam.AddSpouseAsync(ida.Value, idb.Value);
                Console.WriteLine($"linked: spouses {ida} <-> {idb}");
                return 0;
            }
            case "show":
            {
                var of = GetArg(args, "--of");
                if (of == null) { Console.Error.WriteLine("--of required"); return 2; }
                var id = await Resolve(of);
                if (id == null) { Console.Error.WriteLine("could not resolve id"); return 1; }
                var snap = await fam.GetSnapshotAsync(id.Value);
                Console.WriteLine($"=== Family snapshot for {id} ===");
                PrintGroup("parents",      snap.Parents);
                PrintGroup("children",     snap.Children);
                PrintGroup("siblings",     snap.Siblings);
                PrintGroup("spouses",      snap.Spouses);
                PrintGroup("grandparents", snap.Grandparents);
                PrintGroup("cousins",      snap.Cousins);
                return 0;
            }
            default:
                Console.Error.WriteLine($"unknown subcommand: {sub}");
                return 2;
        }
    }

    private static string? GetArg(string[] args, string flag)
    {
        var i = Array.IndexOf(args, flag);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }

    private static void PrintGroup(string label, List<Prose.Core.Data.Entities.Entity> entities)
    {
        Console.WriteLine($"  {label,-13} ({entities.Count}):");
        foreach (var e in entities)
            Console.WriteLine($"    - {e.Name}  ({e.Id})");
    }
}
