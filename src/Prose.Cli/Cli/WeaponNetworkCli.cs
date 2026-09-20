using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// prose --weapon-network --id &lt;weaponId&gt;
/// prose --weapon-network --character &lt;characterId&gt; [--as-of "date"]
/// Prints ammo types + sibling weapons for a weapon, or full loadout for a character.
/// </summary>
public static class WeaponNetworkCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        Guid? weaponId = null;
        Guid? characterId = null;
        DateTime? asOf = null;

        for (int i = 0; i < args.Length - 1; i++)
        {
            switch (args[i])
            {
                case "--id":
                    if (Guid.TryParse(args[i + 1], out var g)) { weaponId = g; i++; }
                    break;
                case "--character":
                    if (Guid.TryParse(args[i + 1], out var c)) characterId = c;
                    i++;
                    break;
                case "--as-of":
                    if (DateTime.TryParse(args[i + 1], out var dt)) asOf = dt;
                    i++;
                    break;
            }
        }

        var svc = services.GetRequiredService<WeaponAmmoCompatibilityService>();

        if (characterId.HasValue)
        {
            var loadout = await svc.GetCharacterLoadoutAsync(characterId.Value, asOf);
            Console.WriteLine($"Loadout: {loadout.CharacterName}");
            if (loadout.Weapons.Count == 0) { Console.WriteLine("  (no signature gear found)"); return 0; }

            foreach (var w in loadout.Weapons)
            {
                Console.WriteLine($"  {w.GearName}");
                if (w.Ammunition.Count > 0)
                    foreach (var a in w.Ammunition)
                        Console.WriteLine($"    • {a.AmmunitionName} ({a.Alias})");
                else
                    Console.WriteLine("    (no ammo linked)");
            }
            return 0;
        }

        if (weaponId.HasValue)
        {
            var network = await svc.GetSharedAmmoNetworkAsync(weaponId.Value);
            Console.WriteLine($"Weapon: {network.WeaponName}");

            Console.WriteLine($"  Ammo types ({network.Ammunition.Count}):");
            foreach (var a in network.Ammunition)
                Console.WriteLine($"    • {a.AmmunitionName} [{a.Alias}]");

            if (network.SiblingWeapons.Count > 0)
            {
                Console.WriteLine($"  Sibling weapons ({network.SiblingWeapons.Count}) — share at least one chambering:");
                foreach (var s in network.SiblingWeapons)
                    Console.WriteLine($"    • {s.WeaponName} (shared: {s.SharedAmmoAlias})");
            }
            return 0;
        }

        Console.Error.WriteLine("Usage: prose --weapon-network (--id <weaponId> | --character <characterId> [--as-of date])");
        return 1;
    }
}
