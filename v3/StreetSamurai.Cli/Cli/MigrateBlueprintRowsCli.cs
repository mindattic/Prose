using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;

namespace StreetSamurai.Cli;

/// <summary>
/// ss --migrate-blueprint-rows [--slug &lt;slug&gt;] [--dry-run]
///
/// Step B2: decomposes per-story JSON blob arrays (EscalationCurveJson,
/// EventTypePaletteJson) and NodeStructuralBlueprintBeatTags rows into
/// per-beat BeatBlueprintDecision rows.
///
/// For each story node that has a blueprint:
///   1. Load beats in SortKey order from the primary chapter.
///   2. Parse EscalationCurveJson (int[]) → EscalationFloor per beat by position.
///   3. Parse EventTypePaletteJson ({beatIndex, eventType, revelationMode}[]) →
///      EventType per beat by index.
///   4. Map existing BeatTags (subplot | anachrony-cut | intertextual-touchpoint) →
///      SubplotCarrier / AnachronyType fields.
///   5. Seed DeclaredPurpose from the beat's Description field (approximation).
///
/// Idempotent: skips any beat that already has a BeatBlueprintDecision row.
/// </summary>
public static class MigrateBlueprintRowsCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        bool dryRun = args.Contains("--dry-run");
        string? filterSlug = null;
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == "--slug") { filterSlug = args[i + 1]; i++; }

        var dbFactory = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        // Load all blueprints (with beat tags)
        var blueprintQuery = db.NodeStructuralBlueprints
            .Include(bp => bp.BeatTags)
            .AsNoTracking();
        if (filterSlug != null)
        {
            var nodeId = await db.Nodes
                .Where(n => n.Slug == filterSlug || n.NodeCode == filterSlug)
                .Select(n => n.Id)
                .FirstOrDefaultAsync();
            if (nodeId == Guid.Empty)
            {
                Console.Error.WriteLine($"Node '{filterSlug}' not found.");
                return 2;
            }
            blueprintQuery = blueprintQuery.Where(bp => bp.NodeId == nodeId);
        }

        var blueprints = await blueprintQuery.ToListAsync();

        // Beats that already have a decision row (to skip)
        var alreadyDecided = await db.BeatBlueprintDecisions
            .Select(d => d.BeatId)
            .ToHashSetAsync();

        int created = 0, skipped = 0, noBeats = 0;

        foreach (var bp in blueprints)
        {
            // Load beats for this story in spine order via its primary chapter
            var beats = await db.BeatNodes
                .Where(bn => bn.NodeId == bp.NodeId)
                .OrderBy(bn => bn.SortKey)
                .Join(db.Beats, bn => bn.BeatId, b => b.Id, (bn, b) => new
                {
                    b.Id,
                    b.Description,
                    SortIndex = bn.SortKey,
                })
                .ToListAsync();

            if (beats.Count == 0)
            {
                Console.WriteLine($"  SKIP  blueprint {bp.Id} — no beats on node");
                noBeats++;
                continue;
            }

            // Parse escalation curve: int[] indexed by beat position
            var escalation = ParseIntArray(bp.EscalationCurveJson);

            // Parse event type palette: {beatIndex, eventType}[]
            var eventTypes = ParseEventPalette(bp.EventTypePaletteJson);

            // Build tag lookups by BeatId
            var subplotCarrierBeats = bp.BeatTags
                .Where(t => t.TagType == "subplot")
                .Select(t => t.BeatId)
                .ToHashSet();
            var anachronyBeats = bp.BeatTags
                .Where(t => t.TagType == "anachrony-cut")
                .ToDictionary(t => t.BeatId, t => t.Note ?? "Flashback");

            int beatsAdded = 0;
            for (int i = 0; i < beats.Count; i++)
            {
                var beat = beats[i];
                if (alreadyDecided.Contains(beat.Id))
                {
                    skipped++;
                    continue;
                }

                var floor    = i < escalation.Count ? (decimal?)escalation[i] : null;
                var evtType  = eventTypes.TryGetValue(i, out var et) ? et : null;
                var isSubplot = subplotCarrierBeats.Contains(beat.Id);
                var anachrony = anachronyBeats.TryGetValue(beat.Id, out var an) ? an : null;

                if (!dryRun)
                {
                    db.BeatBlueprintDecisions.Add(new BeatBlueprintDecision
                    {
                        BeatId          = beat.Id,
                        BlueprintId     = bp.Id,
                        EventType       = evtType,
                        EscalationFloor = floor,
                        SubplotCarrier  = isSubplot,
                        AnachronyType   = anachrony,
                        DeclaredPurpose = beat.Description, // seed from existing Description
                        CreatedAt       = DateTime.UtcNow,
                        UpdatedAt       = DateTime.UtcNow,
                    });
                    alreadyDecided.Add(beat.Id);
                    beatsAdded++;
                    created++;
                }
                else
                {
                    created++;
                }
            }

            Console.WriteLine($"  {(dryRun ? "DRY " : "")}ADDED  {beatsAdded} decisions for blueprint {bp.Id} ({beats.Count} beats, {skipped} skipped)");
        }

        if (!dryRun && created > 0)
            await db.SaveChangesAsync();

        Console.WriteLine();
        Console.WriteLine($"Done. created={created} skipped={skipped} noBeats={noBeats}{(dryRun ? " (dry run)" : "")}");
        return 0;
    }

    private static List<int> ParseIntArray(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<int>>(json) ?? new();
        }
        catch { return new(); }
    }

    private static Dictionary<int, string> ParseEventPalette(string json)
    {
        var result = new Dictionary<int, string>();
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array) return result;
            foreach (var item in doc.RootElement.EnumerateArray())
            {
                int idx = -1;
                string? evtType = null;
                if (item.TryGetProperty("beatIndex", out var idxProp)) idx = idxProp.GetInt32();
                if (item.TryGetProperty("eventType", out var etProp)) evtType = etProp.GetString();
                if (idx >= 0 && evtType != null) result[idx] = evtType;
            }
        }
        catch { }
        return result;
    }
}
