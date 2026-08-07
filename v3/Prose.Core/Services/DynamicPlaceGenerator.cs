using System.Text.Json;
using Prose.Core.Interfaces;
using Prose.Core.Models.Canon;

namespace Prose.Core.Services;

/// <summary>
/// Generates places dynamically as characters move through the world.
/// When a character goes DOWN into sewers, the sewer place is created.
/// When they take an elevator UP through 28 floors, each floor gets a
/// minimal entry and the destination floor gets a full description.
///
/// Every generated place is saved to the Places repository — the world
/// grows as stories are told. Places that were just "passed through"
/// get minimal entries. Places where action happens get full descriptions.
///
/// ── WHY (THE WORLD GROWS THROUGH STORYTELLING) ──
/// The world starts with a finite set of canon places (districts, landmarks). But
/// stories need interiors, sublevels, rooftops, and transition spaces that don't
/// exist yet. Rather than pre-generating thousands of places, this service creates
/// them on-demand as the narrative needs them. Crucially, every generated place is
/// PERSISTED to the DistrictRepository — so the next story that visits the same
/// building finds the lobby already there, with its atmosphere and exits intact.
/// The world literally accumulates detail through the act of telling stories about it.
///
/// ── GENERATION TIERS ──
/// 1. Transit/pass-through: Minimal entries (one-liner descriptions, elevator floors,
///    tunnel sections). Created in bulk by GenerateTransitSequenceAsync().
/// 2. Destination/action: Full LLM-generated descriptions with atmosphere (sights,
///    sounds, smells, feel), dangers, and opportunities. Created by GeneratePlaceAsync().
/// 3. Building entry: Special case — EnterBuildingAsync() creates a lobby and wires
///    bidirectional exits (street <-> lobby), establishing the building as explorable.
///
/// ── HOW IT CONNECTS ──
/// CALLS: ILlmService (atmospheric description generation), DistrictRepository (save
///        new places), NavigationService (exit wiring, direction labels).
/// CALLED BY: StoryDirectorService (when narrative moves to a new location),
///            narrative continuation logic (when characters enter buildings or go vertical).
/// PERSISTS TO: Places repository (JSON files) — new places survive across stories.
///
/// ── WHEN IT RUNS ──
/// On-demand during story generation, whenever a character moves to a place that
/// doesn't exist yet. Also callable from UI for manual world-building.
/// Idempotent: if a place already exists, returns its name without regenerating.
/// </summary>
public class DynamicPlaceGenerator
{
    private readonly ILlmService llm;
    private readonly DatabaseService db;
    private readonly DistrictRepository places;
    private readonly NavigationService nav;

    public DynamicPlaceGenerator(ILlmService llm, DatabaseService db, DistrictRepository places, NavigationService nav)
    {
        this.llm = llm;
        this.db = db;
        this.places = places;
        this.nav = nav;
    }

    /// <summary>
    /// Generate a sequence of transitional places (e.g., elevator floors, tunnel sections).
    /// Creates minimal entries for pass-through locations and a full entry for the destination.
    /// Returns the names of all created places in order.
    ///
    /// Each pass-through gets bidirectional exits (up/down or forward/back) so the
    /// character can retrace their path. The final destination gets full LLM-generated
    /// atmospheric description. Pass-throughs are cheap (no LLM call); only the
    /// destination triggers generation.
    /// </summary>
    public async Task<List<string>> GenerateTransitSequenceAsync(
        string fromPlace, string direction, int count, string destinationContext,
        string? parentBuilding = null, CancellationToken ct = default)
    {
        var from = places.GetByName(fromPlace);
        if (from == null) return [];

        var created = new List<string>();
        var coords = from.Coordinates;

        // Generate pass-through entries (minimal — just markers)
        for (int i = 1; i < count; i++)
        {
            var floorName = parentBuilding != null
                ? $"{parentBuilding} Floor {i}"
                : $"{fromPlace} — {NavigationService.DirectionLabel(direction)} Level {i}";

            // Skip if already exists
            if (places.GetByName(floorName) != null) { created.Add(floorName); continue; }

            var passThrough = new DistrictData
            {
                Name = floorName,
                Description = parentBuilding != null
                    ? $"Floor {i} of {parentBuilding}. The elevator passes through without stopping."
                    : $"Level {i} {NavigationService.DirectionLabel(direction)} of {fromPlace}. Transitional space.",
                Coordinates = coords,
                Connections = new DistrictConnections
                {
                    Exits =
                    [
                        new PlaceExit
                        {
                            Direction = direction,
                            Destination = parentBuilding != null ? $"{parentBuilding} Floor {i + 1}" : "",
                            Type = direction is "up" or "down" ? "elevator" : "corridor",
                            Description = $"Continue {NavigationService.DirectionLabel(direction)}",
                        },
                        new PlaceExit
                        {
                            Direction = direction == "up" ? "down" : direction == "down" ? "up" : OppositeDirection(direction),
                            Destination = i == 1 ? fromPlace : (parentBuilding != null ? $"{parentBuilding} Floor {i - 1}" : ""),
                            Type = direction is "up" or "down" ? "elevator" : "corridor",
                            Description = $"Go back {NavigationService.DirectionLabel(direction == "up" ? "down" : "up")}",
                        },
                    ],
                },
            };

            places.Save(passThrough);
            created.Add(floorName);
        }

        // Generate the destination (full entry)
        var destName = await GenerateDestinationAsync(fromPlace, direction, count, destinationContext, parentBuilding, coords, ct);
        created.Add(destName);

        // Wire the last pass-through to the destination
        if (created.Count >= 2)
        {
            var lastPassThrough = places.GetByName(created[^2]);
            if (lastPassThrough != null)
            {
                var exit = lastPassThrough.Connections.Exits.FirstOrDefault(e => e.Direction == direction);
                if (exit != null) exit.Destination = destName;
                places.Save(lastPassThrough);
            }
        }

        return created;
    }

    /// <summary>
    /// Generate a single dynamic place with full description.
    /// Used for locations that characters enter and experience.
    /// </summary>
    public async Task<string> GeneratePlaceAsync(
        string name, string context, string? parentPlace = null,
        GeoCoordinates? coords = null, CancellationToken ct = default)
    {
        if (places.GetByName(name) != null) return name;

        var systemIdentity = UniverseScope.Current?.UniverseGroundingOr("You are generating a place for near-future fiction set in GLMZ (2100).")
            ?? "You are generating a place for near-future fiction set in GLMZ (2100).";
        var system = systemIdentity + """

            This place is being discovered during a story — describe what the characters see,
            hear, smell, and feel when they arrive. Be specific and atmospheric.

            Return a JSON object:
            {
              "description": "2-3 paragraphs of atmospheric description",
              "atmosphere": {
                "sights": ["4-5 visual details"],
                "sounds": ["3-4 audio details"],
                "smells": ["2-3 scent details"],
                "feel": "1-2 sentences of emotional texture"
              },
              "dangers": ["2-3 dangers"],
              "opportunities": ["1-2 opportunities"]
            }
            Return ONLY the JSON.
            """;

        var user = $"Place name: {name}\nContext: {context}" +
            (parentPlace != null ? $"\nInside: {parentPlace}" : "");

        try
        {
            var response = await llm.GenerateAsync(system, user, 0.8, 1024, ct: ct);
            var json = response.Trim();
            json = JsonDefaults.StripCodeFences(json);

            var generated = JsonSerializer.Deserialize<GeneratedPlaceData>(json.Trim(),
                JsonDefaults.LlmParsing);

            var place = new DistrictData
            {
                Name = name,
                Description = generated?.Description ?? context,
                Atmosphere = generated?.Atmosphere ?? new AtmosphereData(),
                Dangers = generated?.Dangers ?? [],
                Opportunities = generated?.Opportunities ?? [],
                Coordinates = coords ?? new GeoCoordinates(),
            };

            if (parentPlace != null)
            {
                place.Connections.AdjacentTo.Add(parentPlace);
            }

            places.Save(place);
            return name;
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "Dynamic place generation failed for {PlaceName}, saving minimal entry", name);
            // Fallback — save minimal entry
            places.Save(new DistrictData { Name = name, Description = context, Coordinates = coords ?? new() });
            return name;
        }
    }

    /// <summary>
    /// Generate a building interior when characters enter a specific building.
    /// Creates the lobby and connects it to the street-level place with bidirectional
    /// exits (street -> "in" -> lobby, lobby -> "out" -> street). This is the entry
    /// point for making any building explorable — once the lobby exists, transit
    /// sequences can extend upward/downward from it.
    /// </summary>
    public async Task<string> EnterBuildingAsync(
        string streetPlace, string buildingName, string buildingContext,
        CancellationToken ct = default)
    {
        var street = places.GetByName(streetPlace);
        var coords = street?.Coordinates ?? new GeoCoordinates();

        var lobbyName = $"{buildingName} Lobby";
        await GeneratePlaceAsync(lobbyName, $"Ground floor lobby of {buildingName}. {buildingContext}", buildingName, coords, ct);

        // Connect street to lobby
        if (street != null)
        {
            street.Connections.Exits.Add(new PlaceExit
            {
                Direction = "in",
                Destination = lobbyName,
                Type = "entrance",
                Description = $"Enter {buildingName} through the main entrance.",
            });
            places.Save(street);
        }

        // Connect lobby back to street
        var lobby = places.GetByName(lobbyName);
        if (lobby != null)
        {
            lobby.Connections.Exits.Add(new PlaceExit
            {
                Direction = "out",
                Destination = streetPlace,
                Type = "entrance",
                Description = $"Exit {buildingName} to the street.",
            });
            places.Save(lobby);
        }

        return lobbyName;
    }

    private async Task<string> GenerateDestinationAsync(
        string fromPlace, string direction, int level, string context,
        string? parentBuilding, GeoCoordinates coords, CancellationToken ct)
    {
        var destName = parentBuilding != null
            ? $"{parentBuilding} Floor {level}"
            : $"{fromPlace} — {NavigationService.DirectionLabel(direction)} Level {level}";

        await GeneratePlaceAsync(destName, context, parentBuilding, coords, ct);
        return destName;
    }

    private static string OppositeDirection(string dir) => dir switch
    {
        "n" => "s", "s" => "n", "e" => "w", "w" => "e",
        "ne" => "sw", "sw" => "ne", "nw" => "se", "se" => "nw",
        "up" => "down", "down" => "up", _ => dir,
    };
}

internal record GeneratedPlaceData
{
    public string? Description { get; init; }
    public AtmosphereData? Atmosphere { get; init; }
    public List<string>? Dangers { get; init; }
    public List<string>? Opportunities { get; init; }
}
