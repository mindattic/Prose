using Microsoft.Extensions.DependencyInjection;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Blazor.Cli;

/// <summary>
/// CLI surface for <see cref="FamilyGeneratorService"/>. Two-step UX so the
/// cast doesn't grow uncontrollably:
///
///   ss --family-gen propose --of &lt;id|slug&gt; [--seed N] [--with-cousins]
///       Print the proposed family — no DB writes. Add --with-cousins to
///       also propose subject's aunts/uncles and their children.
///
///   ss --family-gen propose --of &lt;id|slug&gt; --commit [--seed N] [--with-cousins]
///       Same proposal, then write characters + edges + propagate genetics.
/// </summary>
public static class FamilyGenCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider sp)
    {
        var idx = Array.IndexOf(args, "--family-gen");
        var sub = idx >= 0 && idx + 1 < args.Length ? args[idx + 1] : null;
        if (sub != "propose")
        {
            Console.Error.WriteLine("Usage: ss --family-gen propose --of <id|slug> [--seed N] [--commit]");
            return 2;
        }

        var ofArg = GetArg(args, "--of");
        if (ofArg == null) { Console.Error.WriteLine("--of <id|slug> required"); return 2; }

        var gen     = sp.GetRequiredService<FamilyGeneratorService>();
        var export  = sp.GetRequiredService<CanonExportService>();

        var id = await export.ResolveEntityIdAsync(ofArg);
        if (id == null) { Console.Error.WriteLine($"could not resolve '{ofArg}'"); return 1; }

        var seedArg = GetArg(args, "--seed");
        var rng     = seedArg != null && int.TryParse(seedArg, out var s) ? new Random(s) : null;
        var withCousins = args.Contains("--with-cousins");

        var proposal = await gen.ProposeAsync(id.Value, rng, withCousins);

        Console.WriteLine($"=== Proposal for {proposal.SubjectName} ({id}) ===");
        PrintGroup("parents",  proposal.Parents);
        PrintGroup("siblings", proposal.Siblings);
        PrintGroup("spouses",  proposal.Spouses);
        PrintGroup("children", proposal.Children);
        if (withCousins)
        {
            PrintGroup("aunts/uncles",         proposal.AuntsUncles     .Select(au  => au.Person).ToList());
            PrintGroup("aunt/uncle spouses",   proposal.AuntUncleSpouses.Select(aus => aus.Person).ToList());
            PrintGroup("cousins",              proposal.Cousins         .Select(c   => c.Person ).ToList());
        }
        Console.WriteLine($"  total new characters: {proposal.Total}");

        if (!args.Contains("--commit"))
        {
            Console.WriteLine();
            Console.WriteLine("(dry run — re-run with --commit to persist)");
            return 0;
        }

        Console.WriteLine();
        Console.WriteLine("[apply] writing characters + edges + propagating genetics...");
        var newIds = await gen.ApplyProposalAsync(proposal, rng);
        Console.WriteLine($"[apply] done. {newIds.Count} new characters created.");
        return 0;
    }

    private static string? GetArg(string[] args, string flag)
    {
        var i = Array.IndexOf(args, flag);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }

    private static void PrintGroup(string label, List<FamilyGeneratorService.ProposedCharacter> rows)
    {
        Console.WriteLine($"  {label,-9} ({rows.Count}):");
        foreach (var r in rows)
        {
            var middle = string.IsNullOrWhiteSpace(r.MiddleName) ? "" : $" {r.MiddleName}";
            Console.WriteLine($"    - {r.FirstName}{middle} {r.LastName,-20}  age {r.Age,3}  {r.Gender,-6}  ({r.Role})");
        }
    }
}
