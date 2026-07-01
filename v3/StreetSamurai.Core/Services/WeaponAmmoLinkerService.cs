using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MindAttic.Legion;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Ensures every ranged weapon has at least one Edges row pointing to an ammunition entity.
/// Uses the local LLM to infer the correct ammo from the weapon's description + the full
/// list of available ammunition entities. Melee weapons are skipped (no ammo needed).
///
/// RelationType: "uses_ammunition"
/// Source: "linker:weapon-ammo"
/// </summary>
public class WeaponAmmoLinkerService
{
    private readonly LegionClient legion;
    private readonly IDbContextFactory<StreetSamuraiDbContext> dbFactory;
    private readonly ILogger<WeaponAmmoLinkerService> log;

    private const int Concurrency = 12;

    public WeaponAmmoLinkerService(
        LegionClient legion,
        IDbContextFactory<StreetSamuraiDbContext> dbFactory,
        ILogger<WeaponAmmoLinkerService> log)
    {
        this.legion    = legion;
        this.dbFactory = dbFactory;
        this.log       = log;
    }

    public async Task LinkAllAsync(
        string? localUrl, string? localKey, string? localModel,
        bool dryRun = false, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        // Load all ammunition entities: id + name + short description.
        var ammoList = await db.Entities
            .Where(e => e.EntityType == "ammunition" && e.IsActive)
            .Select(e => new { e.Id, e.Name, e.Description, e.UniverseId })
            .ToListAsync(ct);

        if (ammoList.Count == 0)
        {
            log.LogWarning("No ammunition entities found — nothing to link.");
            return;
        }

        // Build ammo menu: "Name — first 60 chars of description"
        var ammoMenu = string.Join("\n", ammoList.Select(a =>
            $"- {a.Name}" + (!string.IsNullOrWhiteSpace(a.Description)
                ? $" — {a.Description[..Math.Min(60, a.Description.Length)].TrimEnd()}"
                : "")));

        // All weapons that have no outgoing edge to any ammunition entity.
        var linkedWeaponIds = await db.Edges
            .Where(e => db.Entities.Any(a => a.Id == e.TargetId && a.EntityType == "ammunition"))
            .Select(e => e.SourceId)
            .Distinct()
            .ToListAsync(ct);

        var weapons = await db.Entities
            .Where(w => w.EntityType == "weapon" && w.IsActive && !linkedWeaponIds.Contains(w.Id))
            .Select(w => new { w.Id, w.Name, w.Description, w.UniverseId })
            .ToListAsync(ct);

        log.LogInformation("WeaponAmmoLinker: {Unlinked} weapons need ammo edges (of {Total} total)",
            weapons.Count, weapons.Count + linkedWeaponIds.Count);

        // Build a quick name→id lookup for ammo matching.
        var ammoById   = ammoList.ToDictionary(a => a.Id);
        var ammoByName = ammoList
            .GroupBy(a => a.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var useLocal = !string.IsNullOrWhiteSpace(localUrl);
        var key      = string.IsNullOrWhiteSpace(localKey) ? "local" : localKey;
        var model    = string.IsNullOrWhiteSpace(localModel) ? "qwen2.5-72b-32k" : localModel;

        var sem     = new SemaphoreSlim(Concurrency);
        var linked  = 0;
        var skipped = 0; // melee / no match
        var failed  = 0;

        await Task.WhenAll(weapons.Select(w => Task.Run(async () =>
        {
            await sem.WaitAsync(ct);
            try
            {
                var system = BuildPrompt(ammoMenu);
                var weaponDesc = (w.Description ?? "").Length > 1200 ? w.Description![..1200] : (w.Description ?? "(none)");
                var userMsg = $"Weapon name: {w.Name}\n\nDescription:\n{weaponDesc}";

                string raw;
                if (useLocal)
                    raw = await legion.CallAsync("local", key, model, system, userMsg, localUrl!, maxTokens: 150, temperature: 0.3, ct);
                else
                    raw = await legion.CallAsync("claude-api", key, model, system, userMsg, maxTokens: 150, temperature: 0.3, ct);

                if (!TryParseMatch(raw, out var ammoName, out var confidence))
                {
                    Interlocked.Increment(ref failed);
                    log.LogWarning("WeaponAmmoLinker: parse failed for '{Weapon}': {Raw}", w.Name, raw?.Length > 80 ? raw[..80] : raw);
                    return;
                }

                if (string.IsNullOrWhiteSpace(ammoName))
                {
                    Interlocked.Increment(ref skipped);
                    log.LogDebug("WeaponAmmoLinker: '{Weapon}' → melee/no-ammo (skipped)", w.Name);
                    return;
                }

                if (!ammoByName.TryGetValue(ammoName, out var ammo))
                {
                    // Try fuzzy: first ammo whose name contains the LLM's answer (or vice versa).
                    ammo = ammoList.FirstOrDefault(a =>
                        a.Name.Contains(ammoName, StringComparison.OrdinalIgnoreCase) ||
                        ammoName.Contains(a.Name, StringComparison.OrdinalIgnoreCase));
                }

                if (ammo == null)
                {
                    Interlocked.Increment(ref failed);
                    log.LogWarning("WeaponAmmoLinker: no ammo entity matches '{AmmoName}' for weapon '{Weapon}'", ammoName, w.Name);
                    return;
                }

                if (!dryRun)
                {
                    await using var db2 = await dbFactory.CreateDbContextAsync(ct);
                    // Check not already linked (race guard).
                    var exists = await db2.Edges.AnyAsync(e =>
                        e.SourceId == w.Id && e.TargetId == ammo.Id && e.RelationType == "uses_ammunition", ct);
                    if (!exists)
                    {
                        db2.Edges.Add(new Edge
                        {
                            SourceId     = w.Id,
                            TargetId     = ammo.Id,
                            RelationType = "uses_ammunition",
                            Description  = $"{w.Name} uses {ammo.Name}.",
                            Sentiment    = "neutral",
                            Weight       = confidence,
                            Source       = "linker:weapon-ammo",
                            UniverseId   = w.UniverseId,
                        });
                        await db2.SaveChangesAsync(ct);
                    }
                }

                Interlocked.Increment(ref linked);
                log.LogInformation("WeaponAmmoLinker: '{Weapon}' → {Ammo} (conf {C:F2}{DryRun})",
                    w.Name, ammo.Name, confidence, dryRun ? " [DRY]" : "");
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref failed);
                log.LogWarning(ex, "WeaponAmmoLinker: exception for '{Weapon}'", w.Name);
            }
            finally
            {
                sem.Release();
            }
        }, ct)));

        log.LogInformation("WeaponAmmoLinker complete — linked={L} skipped(melee)={S} failed={F}",
            linked, skipped, failed);
        Console.WriteLine($"  linked  : {linked}");
        Console.WriteLine($"  skipped : {skipped}  (melee / no ammo)");
        Console.WriteLine($"  failed  : {failed}");
    }

    private static string BuildPrompt(string ammoMenu)
    {
        return
$@"You are a GLMZ (cyberpunk 2225) weapons database editor. Given a weapon's name and description, identify which ammunition from the list below it uses.

Rules:
- Conventional firearms → match by caliber (e.g. 9mm → '9x19mm Standard')
- Energy weapons (laser, plasma, electrostatic) → match to a power cell
- Railguns / magnetic accelerators → match to slugs or flechettes
- Pneumatic / gas weapons → 'Standard CO2 Propulsion Cartridge SPC-12'
- Chemical / aerosol weapons → aerosol or canister ammo
- Bio-hybrid / organic weapons → 'Nutrient Cartridge Biological Sustenance Pack BSP-72'
- Melee weapons (blades, clubs, whips) → return null
- If genuinely uncertain → return the closest plausible match with lower confidence

Available ammunition:
{ammoMenu}

Return ONLY JSON:
{{""ammoName"": ""<exact name from list above, or null if melee>"", ""confidence"": 0.95}}";
    }

    private static bool TryParseMatch(string? raw, out string? ammoName, out double confidence)
    {
        ammoName = null; confidence = 0.5;
        if (string.IsNullOrWhiteSpace(raw)) return false;
        var open = raw.IndexOf('{');
        var close = raw.LastIndexOf('}');
        if (open < 0 || close <= open) return false;
        try
        {
            using var doc = JsonDocument.Parse(raw[open..(close + 1)]);
            var root = doc.RootElement;
            if (root.TryGetProperty("ammoName", out var n))
                ammoName = n.ValueKind == JsonValueKind.Null ? null : n.GetString();
            if (root.TryGetProperty("confidence", out var c))
                confidence = c.GetDouble();
            return true;
        }
        catch { return false; }
    }
}
