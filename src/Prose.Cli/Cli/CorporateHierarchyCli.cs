using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// CorpoNation hierarchies from the command line (PRS-T819): every CorpoNation's Tier, who owns
/// what all the way down to ground level, and the forces each one can call up. The rules (Tier 1-4,
/// owner above owned, no loops, one universe) live in <see cref="CorporateHierarchyService"/>; this
/// only parses and prints.
///
/// Usage (always with --universe):
///   prose --corp tier --entity X [--set N | --clear] [--confirm-unread]
///   prose --corp own add --owner X --owned Y [--kind K] [--stake P] [--since YYYY] [--notes "..."]
///   prose --corp own remove --owner X --owned Y
///   prose --corp own list --entity X
///   prose --corp force add --corp X --name "..." --kind K [--entity Y] [--strength "..."] [--notes "..."]
///   prose --corp force remove --id G
///   prose --corp force list --corp X
///   prose --corp chain --entity X          owners up to the ultimate parent
///   prose --corp tree --entity X           everything it owns, down to the ground
///   prose --corp forces --entity X         every force it can call up (its own + its subsidiaries')
///   prose --corp audit                     tier-order breaks, loops, untiered corponations and parents
///   prose --corp backfill-ownership        a wholly-owned edge per Subsidiary.ParentCorponationId (idempotent)
///
/// An entity is its id, slug or exact name. Exit codes: 0 ok · 1 refused or not found · 2 bad args.
/// </summary>
public static class CorporateHierarchyCli
{
    private const string Usage =
        "usage: prose --universe <u> --corp tier --entity X [--set N | --clear] | own add|remove|list … | force add|remove|list … | " +
        "chain --entity X | tree --entity X | forces --entity X | audit | backfill-ownership";

    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var svc = services.GetRequiredService<CorporateHierarchyService>();
        var at = Array.IndexOf(args, "--corp");
        var words = args.Skip(at + 1).TakeWhile(a => !a.StartsWith("--", StringComparison.Ordinal)).ToList();
        var verb = words.ElementAtOrDefault(0) ?? "";
        var sub = words.ElementAtOrDefault(1) ?? "";
        // Flags are read after the verb, so `force add --corp X` finds the keeper, not the command.
        var tail = args.Skip(at + 1).ToList();
        string? Flag(string name) { var i = tail.IndexOf(name); return i >= 0 && i + 1 < tail.Count ? tail[i + 1] : null; }

        try
        {
            switch (verb)
            {
                case "tier":
                {
                    var entity = Flag("--entity");
                    if (entity == null) return Bad("tier needs --entity X");
                    var set = Flag("--set");
                    var clear = tail.Contains("--clear");
                    if (set == null && !clear)
                    {
                        var e = await svc.ResolveAsync(entity, preferCorporate: true);
                        Console.WriteLine($"{e.Name} [{e.EntityType}]: {(e.Tier is int t ? $"Tier {t}" : "no Tier yet")}");
                        return 0;
                    }
                    int? tier = null;
                    if (set != null)
                    {
                        if (!int.TryParse(set, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n)) return Bad($"--set needs a whole number 1-4, got '{set}'");
                        tier = n;
                    }
                    var (who, write) = await svc.SetTierAsync(entity, tier, tail.Contains("--confirm-unread"));
                    if (!write.Ok) { Console.Error.WriteLine($"[corp] {write.Error}"); return 1; }
                    Console.WriteLine(write.Changed.Count == 0
                        ? $"[corp] {who.Name} is already {(tier is int same ? $"Tier {same}" : "untiered")}; nothing written."
                        : $"[corp] {who.Name}: {(tier is int now ? $"Tier {now}" : "Tier cleared")}.");
                    return 0;
                }
                case "own":
                    switch (sub)
                    {
                        case "add":
                        {
                            var owner = Flag("--owner"); var owned = Flag("--owned");
                            if (owner == null || owned == null) return Bad("own add needs --owner X --owned Y");
                            decimal? stake = null; int? since = null;
                            if (Flag("--stake") is { } s)
                            {
                                if (!decimal.TryParse(s.TrimEnd('%'), NumberStyles.Number, CultureInfo.InvariantCulture, out var p)) return Bad($"--stake needs a percent, got '{s}'");
                                stake = p;
                            }
                            if (Flag("--since") is { } y)
                            {
                                if (!int.TryParse(y, NumberStyles.Integer, CultureInfo.InvariantCulture, out var yr)) return Bad($"--since needs a year, got '{y}'");
                                since = yr;
                            }
                            var (link, created) = await svc.SetOwnershipAsync(new OwnershipDraft(owner, owned, Flag("--kind"), stake, since, Flag("--notes")));
                            Console.WriteLine($"[corp] {(created ? "recorded" : "updated")}: {Describe(link)}");
                            return 0;
                        }
                        case "remove":
                        {
                            var owner = Flag("--owner"); var owned = Flag("--owned");
                            if (owner == null || owned == null) return Bad("own remove needs --owner X --owned Y");
                            var gone = await svc.RemoveOwnershipAsync(owner, owned);
                            Console.WriteLine(gone ? $"[corp] removed: {owner} no longer owns {owned}." : $"[corp] {owner} did not own {owned}; nothing removed.");
                            return gone ? 0 : 1;
                        }
                        case "list":
                        {
                            var entity = Flag("--entity");
                            if (entity == null) return Bad("own list needs --entity X");
                            var (e, ownedBy, owns) = await svc.ListOwnershipAsync(entity);
                            Console.WriteLine(e);
                            Console.WriteLine($"  owned by ({ownedBy.Count}):");
                            foreach (var l in ownedBy) Console.WriteLine($"    {l.Owner}  {Terms(l)}");
                            Console.WriteLine($"  owns ({owns.Count}):");
                            foreach (var l in owns) Console.WriteLine($"    {l.Owned}  {Terms(l)}");
                            return 0;
                        }
                        default: return Bad("own add | remove | list");
                    }
                case "force":
                    switch (sub)
                    {
                        case "add":
                        {
                            var corp = Flag("--corp"); var name = Flag("--name"); var kind = Flag("--kind");
                            if (corp == null || name == null || kind == null) return Bad("force add needs --corp X --name \"...\" --kind K");
                            var (f, created) = await svc.SetForceAsync(new ForceDraft(corp, name, kind, Flag("--entity"), Flag("--strength"), Flag("--notes")));
                            Console.WriteLine($"[corp] {(created ? "recorded" : "updated")} force {f.Id:N}: {f.Keeper.Name} keeps {f.Name} ({f.Kind})" +
                                              (f.ForceEntity != null ? $" = {f.ForceEntity.Name}" : "") + (f.Strength != null ? $", {f.Strength}" : "") + ".");
                            return 0;
                        }
                        case "remove":
                        {
                            if (!Guid.TryParse(Flag("--id"), out var id)) return Bad("force remove needs --id <force id>");
                            var gone = await svc.RemoveForceAsync(id);
                            Console.WriteLine(gone ? $"[corp] removed force {id:N}." : $"[corp] no force {id:N}; nothing removed.");
                            return gone ? 0 : 1;
                        }
                        case "list":
                        {
                            var corp = Flag("--corp");
                            if (corp == null) return Bad("force list needs --corp X");
                            var (keeper, forces) = await svc.ListForcesAsync(corp);
                            Console.WriteLine($"{keeper} keeps {forces.Count} force(s):");
                            foreach (var f in forces) Console.WriteLine($"  {f.Id:N}  {Force(f)}");
                            return 0;
                        }
                        default: return Bad("force add | remove | list");
                    }
                case "chain":
                {
                    var entity = Flag("--entity");
                    if (entity == null) return Bad("chain needs --entity X");
                    var (e, chain) = await svc.ChainAsync(entity);
                    Console.WriteLine(e);
                    if (chain.Count == 0) Console.WriteLine("  nobody owns it — it is its own ultimate parent.");
                    foreach (var s in chain) Console.WriteLine($"  {new string('↑', s.Depth)} {s.Link.Owner}  owns {s.Link.Owned.Name}  {Terms(s.Link)}");
                    return 0;
                }
                case "tree":
                {
                    var entity = Flag("--entity");
                    if (entity == null) return Bad("tree needs --entity X");
                    var (e, tree) = await svc.TreeAsync(entity);
                    Console.WriteLine(e);
                    if (tree.Count == 0) Console.WriteLine("  owns nothing on record.");
                    foreach (var s in tree) Console.WriteLine($"  {new string(' ', 2 * (s.Depth - 1))}└ {s.Link.Owned}  {Terms(s.Link)}");
                    return 0;
                }
                case "forces":
                {
                    var entity = Flag("--entity");
                    if (entity == null) return Bad("forces needs --entity X");
                    var (e, forces) = await svc.CallableForcesAsync(entity);
                    Console.WriteLine($"{e} can call up {forces.Count} force(s):");
                    foreach (var f in forces)
                        Console.WriteLine($"  {(f.Depth == 0 ? "own" : $"-{f.Depth}"),-4} {Force(f)}  — kept by {f.Keeper.Name}");
                    return 0;
                }
                case "audit":
                {
                    var findings = await svc.AuditAsync();
                    foreach (var g in findings.GroupBy(f => f.Kind))
                    {
                        Console.WriteLine($"{g.Key} ({g.Count()}):");
                        foreach (var f in g) Console.WriteLine($"  {f.Message}");
                    }
                    Console.WriteLine($"[corp] audit: {findings.Count} finding(s).");
                    return 0;
                }
                case "backfill-ownership":
                {
                    var (created, already, skipped) = await svc.BackfillFromParentsAsync();
                    Console.WriteLine($"[corp] backfill: {created} wholly-owned edge(s) created, {already} already recorded, {skipped} skipped (self or cross-universe parent).");
                    return 0;
                }
                default:
                    return Bad(verb.Length == 0 ? "no verb" : $"unknown verb '{verb}'");
            }
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            Console.Error.WriteLine($"[corp] {ex.Message}");
            return 1;
        }
    }

    private static int Bad(string why)
    {
        Console.Error.WriteLine($"[corp] {why}. {Usage}");
        return 2;
    }

    private static string Terms(OwnershipLink l) =>
        "(" + string.Join(", ", new[]
        {
            l.Kind,
            l.StakePercent is decimal p ? p.ToString("0.##", CultureInfo.InvariantCulture) + "%" : null,
            l.SinceYear is int y ? $"since {y}" : null,
            l.Notes,
        }.Where(x => !string.IsNullOrWhiteSpace(x))) + ")";

    private static string Describe(OwnershipLink l) => $"{l.Owner} owns {l.Owned} {Terms(l)}";

    private static string Force(CallableForce f) =>
        $"{f.Name} ({f.Kind})" + (f.ForceEntity != null ? $" = {f.ForceEntity.Name}" : "") +
        (f.Strength != null ? $", {f.Strength}" : "") + (f.Notes != null ? $" — {f.Notes}" : "");
}
