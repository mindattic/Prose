using StreetSamurai.Core.Models.Canon;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Geographic navigation between places. Every place is a Zork-style room with
/// directional exits (N/NE/E/SE/S/SW/W/NW/UP/DOWN). Exits are computed from
/// real-world coordinates — the nearest place in each compass direction becomes
/// the exit for that direction.
///
/// Also provides pathfinding: "How do I get from The Shelf to Green Bay?"
/// returns the sequence of places you'd pass through.
///
/// ── WHY ──
/// The world model stores places with real geographic coordinates (lat/lng based on
/// Chicago metro area). This service bridges geography and narrative by converting
/// continuous coordinates into discrete Zork-style room exits. This means:
/// 1. Characters can "go north" and arrive at a real, geographically correct place.
/// 2. Routes between distant locations pass through intermediate places, creating
///    opportunities for encounters and scene-setting along the way.
/// 3. Exit descriptions are auto-generated with flavor (checkpoints, troll bridges,
///    maglev routes) based on the political/danger characteristics of the places.
///
/// ── HOW IT CONNECTS ──
/// READS FROM: DistrictRepository (place data with coordinates and descriptions).
/// CALLED BY: DynamicPlaceGenerator (to wire new places into the exit graph),
///            StoryDirectorService (indirectly, for route/travel narrative context),
///            UI layer (for map/navigation display).
/// Uses Haversine formula for distance and bearing calculations.
///
/// ── WHEN IT RUNS ──
/// ComputeAllExits() runs once at world setup (or when places change) to precompute
/// the full exit graph. GetExits() is lazy — computes on demand if not precomputed.
/// FindRoute() runs per-query when the narrative needs a path between two places.
///
/// ── EXIT TYPE INFERENCE ──
/// Exit flavor is determined by distance and place characteristics:
///   >20km = highway, >10km = maglev, harbor = waterway, different corps = checkpoint,
///   ungoverned zone = possible troll bridge, <2km = walkway, else = street.
/// Danger levels are inferred from place descriptions (kill zones, ungoverned, etc.)
/// </summary>
public class NavigationService
{
    private readonly DistrictRepository places;

    // Max distance in km to consider places "adjacent" for exits
    private const double MaxExitDistanceKm = 50.0;
    // For within-city (Chicago core), tighter radius
    private const double CityExitDistanceKm = 5.0;

    // Chicago metro bounding box for tighter adjacency
    private const double ChicagoLatMin = 41.6;
    private const double ChicagoLatMax = 42.1;
    private const double ChicagoLngMin = -87.95;
    private const double ChicagoLngMax = -87.5;

    public NavigationService(DistrictRepository places)
    {
        this.places = places;
    }

    /// <summary>
    /// Compute and assign directional exits for ALL places based on coordinates.
    /// Each place gets the nearest neighbor in each compass direction as its exit.
    /// </summary>
    public int ComputeAllExits()
    {
        var all = places.GetAll();
        var withCoords = all.Where(p => p.Coordinates.Lat != 0 && p.Coordinates.Lng != 0).ToList();
        int updated = 0;

        foreach (var place in withCoords)
        {
            var isChicago = IsInChicagoCore(place.Coordinates);
            var maxDist = isChicago ? CityExitDistanceKm : MaxExitDistanceKm;

            var exits = new List<PlaceExit>();

            foreach (var dir in new[] { "n", "ne", "e", "se", "s", "sw", "w", "nw" })
            {
                var nearest = FindNearestInDirection(place, dir, withCoords, maxDist);
                if (nearest != null)
                {
                    var dist = DistanceKm(place.Coordinates, nearest.Coordinates);
                    exits.Add(new PlaceExit
                    {
                        Direction = dir,
                        Destination = nearest.Name,
                        Type = InferExitType(place, nearest, dist),
                        Description = InferExitDescription(place, nearest, dir, dist),
                        Restricted = InferRestricted(place, nearest),
                        DangerLevel = InferDanger(place, nearest),
                    });
                }
            }

            // UP/DOWN for vertical places
            var verticalDown = FindVerticalConnection(place, withCoords, "down");
            if (verticalDown != null)
                exits.Add(new PlaceExit { Direction = "down", Destination = verticalDown.Name,
                    Type = "vertical", Description = $"Descend from {place.Name} into {verticalDown.Name}", DangerLevel = 4 });
            var verticalUp = FindVerticalConnection(place, withCoords, "up");
            if (verticalUp != null)
                exits.Add(new PlaceExit { Direction = "up", Destination = verticalUp.Name,
                    Type = "vertical", Description = $"Climb from {place.Name} up to {verticalUp.Name}", DangerLevel = 3 });

            if (exits.Count > 0)
            {
                place.Connections.Exits = exits;
                places.Save(place);
                updated++;
            }
        }

        return updated;
    }

    /// <summary>
    /// Get the exits for a specific place as PlaceExit objects.
    /// </summary>
    public List<PlaceExit> GetExits(string placeName)
    {
        var place = places.GetByName(placeName);
        if (place == null) return [];

        if (place.Connections.Exits.Count > 0)
            return place.Connections.Exits;

        // Compute lazily
        var all = places.GetAll().Where(p => p.Coordinates.Lat != 0).ToList();
        var isChicago = IsInChicagoCore(place.Coordinates);
        var maxDist = isChicago ? CityExitDistanceKm : MaxExitDistanceKm;
        var exits = new List<PlaceExit>();

        foreach (var dir in new[] { "n", "ne", "e", "se", "s", "sw", "w", "nw" })
        {
            var nearest = FindNearestInDirection(place, dir, all, maxDist);
            if (nearest != null)
            {
                var dist = DistanceKm(place.Coordinates, nearest.Coordinates);
                exits.Add(new PlaceExit
                {
                    Direction = dir,
                    Destination = nearest.Name,
                    Type = InferExitType(place, nearest, dist),
                    Description = InferExitDescription(place, nearest, dir, dist),
                    Restricted = InferRestricted(place, nearest),
                    DangerLevel = InferDanger(place, nearest),
                });
            }
        }

        return exits;
    }

    /// <summary>Get exits as a simple direction→name dictionary (for pathfinding).</summary>
    public Dictionary<string, string> GetExitMap(string placeName) =>
        GetExits(placeName).ToDictionary(e => e.Direction, e => e.Destination);

    /// <summary>
    /// Find a route between two places. Returns the sequence of places to pass through.
    /// Uses A* pathfinding with geographic (Haversine) distance as the heuristic.
    /// The graph edges are the Zork-style exits — A* explores only via valid exits,
    /// not arbitrary geographic neighbors, so routes respect access restrictions.
    /// Returns empty list if no route exists (disconnected graph regions).
    /// </summary>
    public List<string> FindRoute(string fromName, string toName)
    {
        var all = places.GetAll().Where(p => p.Coordinates.Lat != 0)
            .GroupBy(p => p.Name).ToDictionary(g => g.Key, g => g.First());
        if (!all.ContainsKey(fromName) || !all.ContainsKey(toName)) return [];

        var target = all[toName];

        // A* pathfinding — g(n) = actual distance traveled, h(n) = straight-line to target
        var openSet = new PriorityQueue<string, double>();
        var cameFrom = new Dictionary<string, string>();
        var gScore = new Dictionary<string, double> { [fromName] = 0 };
        var fScore = new Dictionary<string, double>();

        fScore[fromName] = DistanceKm(all[fromName].Coordinates, target.Coordinates);
        openSet.Enqueue(fromName, fScore[fromName]);

        var visited = new HashSet<string>();

        while (openSet.Count > 0)
        {
            var current = openSet.Dequeue();
            if (current == toName)
                return ReconstructPath(cameFrom, current);

            if (!visited.Add(current)) continue;

            // Get neighbors via exits
            var exitMap = GetExitMap(current);
            foreach (var (_, neighborName) in exitMap)
            {
                if (!all.ContainsKey(neighborName)) continue;

                var tentativeG = gScore.GetValueOrDefault(current, double.MaxValue) +
                    DistanceKm(all[current].Coordinates, all[neighborName].Coordinates);

                if (tentativeG < gScore.GetValueOrDefault(neighborName, double.MaxValue))
                {
                    cameFrom[neighborName] = current;
                    gScore[neighborName] = tentativeG;
                    fScore[neighborName] = tentativeG + DistanceKm(all[neighborName].Coordinates, target.Coordinates);
                    openSet.Enqueue(neighborName, fScore[neighborName]);
                }
            }
        }

        return []; // No route found
    }

    /// <summary>
    /// Build a narrative description of a route for LLM injection.
    /// "To get from The Shelf to Green Bay, you'd pass through..."
    /// </summary>
    public string DescribeRoute(string fromName, string toName)
    {
        var route = FindRoute(fromName, toName);
        if (route.Count == 0) return $"No known route from {fromName} to {toName}.";
        if (route.Count == 1) return $"You're already at {fromName}.";

        var totalDist = 0.0;
        var all = places.GetAll().GroupBy(p => p.Name).ToDictionary(g => g.Key, g => g.First());

        var steps = new List<string>();
        for (int i = 0; i < route.Count - 1; i++)
        {
            var from = all.GetValueOrDefault(route[i]);
            var to = all.GetValueOrDefault(route[i + 1]);
            if (from == null || to == null) continue;

            var dist = DistanceKm(from.Coordinates, to.Coordinates);
            totalDist += dist;
            var bearing = GetBearing(from.Coordinates, to.Coordinates);
            steps.Add($"  {i + 1}. {route[i]} → {DirectionName(bearing)} to {route[i + 1]} ({dist:F0} km)");
        }

        return $"ROUTE: {fromName} to {toName} ({totalDist:F0} km, {route.Count - 1} stops)\n" +
               string.Join("\n", steps);
    }

    /// <summary>Get the compass direction label for display.</summary>
    public static string DirectionLabel(string dir) => dir switch
    {
        "n" => "North", "ne" => "Northeast", "e" => "East", "se" => "Southeast",
        "s" => "South", "sw" => "Southwest", "w" => "West", "nw" => "Northwest",
        "up" => "Up", "down" => "Down", _ => dir,
    };

    // ── Internal ──

    // ── Exit type inference ──

    private static string InferExitType(DistrictData from, DistrictData to, double distKm)
    {
        // Corridors/arteries for long distances
        if (distKm > 20) return "highway";
        if (distKm > 10) return "maglev_route";

        // Check for water crossings
        if (from.Name.Contains("Harbor", StringComparison.OrdinalIgnoreCase) ||
            to.Name.Contains("Harbor", StringComparison.OrdinalIgnoreCase))
            return "waterway";

        // Check for corporate territory borders
        if (!string.IsNullOrEmpty(from.PowerStructure) && !string.IsNullOrEmpty(to.PowerStructure) &&
            from.PowerStructure != to.PowerStructure)
            return "checkpoint";

        // Troll bridges in ungoverned zones
        if (string.IsNullOrEmpty(from.PowerStructure) || string.IsNullOrEmpty(to.PowerStructure))
            return Random.Shared.NextDouble() < 0.2 ? "troll_bridge" : "street";

        // Default urban
        return distKm < 2 ? "walkway" : "street";
    }

    private static string InferExitDescription(DistrictData from, DistrictData to, string dir, double distKm)
    {
        var dirName = DirectionLabel(dir);
        var type = InferExitType(from, to, distKm);

        return type switch
        {
            "highway" => $"The {dirName} highway corridor stretches {distKm:F0}km to {to.Name}. Automated traffic, corporate patrol drones overhead.",
            "maglev_route" => $"Maglev line {dirName} to {to.Name}. {distKm:F0}km, tier-segregated cars. The fast way if you have clearance.",
            "waterway" => $"Water route {dirName.ToLower()} toward {to.Name}. Small boats, smuggler channels, no surveillance.",
            "checkpoint" => $"Corporate checkpoint {dirName.ToLower()} at the border with {to.Name}. ID scan, augment registry, tier verification.",
            "troll_bridge" => $"Gang-controlled crossing {dirName.ToLower()} into {to.Name}. Unofficial toll — pay or find another way.",
            "walkway" => $"Walk {dirName.ToLower()} into {to.Name}. Street-level, crowded, the usual noise.",
            "vertical" => $"Vertical access between {from.Name} and {to.Name}.",
            _ => $"Road {dirName.ToLower()} to {to.Name}. {distKm:F0}km through the sprawl.",
        };
    }

    private static bool InferRestricted(DistrictData from, DistrictData to)
    {
        // Corporate zones are restricted
        var toDesc = to.Description.ToLowerInvariant();
        return toDesc.Contains("tier 4") || toDesc.Contains("tier 5") ||
               toDesc.Contains("sovereign") || toDesc.Contains("restricted") ||
               toDesc.Contains("checkpoint") || toDesc.Contains("gated");
    }

    private static int InferDanger(DistrictData from, DistrictData to)
    {
        var toDesc = to.Description.ToLowerInvariant();
        if (toDesc.Contains("kill on sight") || toDesc.Contains("exclusion zone")) return 10;
        if (toDesc.Contains("ungoverned") || toDesc.Contains("lawless") || toDesc.Contains("contested")) return 8;
        if (toDesc.Contains("dangerous") || toDesc.Contains("gang") || toDesc.Contains("hostile")) return 7;
        if (toDesc.Contains("working class") || toDesc.Contains("informal")) return 5;
        if (toDesc.Contains("residential") || toDesc.Contains("quiet")) return 3;
        if (toDesc.Contains("corporate") || toDesc.Contains("tier 4") || toDesc.Contains("tier 5")) return 2;
        return 4;
    }

    private DistrictData? FindNearestInDirection(DistrictData from, string direction, List<DistrictData> all, double maxDistKm)
    {
        var (minAngle, maxAngle) = DirectionAngles(direction);

        return all
            .Where(p => p.Name != from.Name)
            .Select(p => new
            {
                Place = p,
                Distance = DistanceKm(from.Coordinates, p.Coordinates),
                Bearing = GetBearing(from.Coordinates, p.Coordinates),
            })
            .Where(x => x.Distance > 0.1 && x.Distance <= maxDistKm && IsInArc(x.Bearing, minAngle, maxAngle))
            .OrderBy(x => x.Distance)
            .FirstOrDefault()?.Place;
    }

    private DistrictData? FindVerticalConnection(DistrictData place, List<DistrictData> all, string direction)
    {
        // Vertical connections (UP/DOWN) are special — they connect places that share
        // nearly identical coordinates but differ in elevation/depth. Detected via
        // naming conventions (Spire=up, Harbor/Subterranean=down) and proximity <1km.
        var veryClose = all
            .Where(p => p.Name != place.Name && DistanceKm(place.Coordinates, p.Coordinates) < 1.0)
            .ToList();

        if (direction == "down")
        {
            // Places with "below", "under", "subterranean", "flooded", "harbor" in description
            return veryClose.FirstOrDefault(p =>
                p.Description.Contains("below", StringComparison.OrdinalIgnoreCase) ||
                p.Description.Contains("flooded", StringComparison.OrdinalIgnoreCase) ||
                p.Description.Contains("subterranean", StringComparison.OrdinalIgnoreCase) ||
                p.Name.Contains("Harbor", StringComparison.OrdinalIgnoreCase));
        }
        else // up
        {
            return veryClose.FirstOrDefault(p =>
                p.Description.Contains("above", StringComparison.OrdinalIgnoreCase) ||
                p.Description.Contains("tower", StringComparison.OrdinalIgnoreCase) ||
                p.Description.Contains("spire", StringComparison.OrdinalIgnoreCase) ||
                p.Name.Contains("Spire", StringComparison.OrdinalIgnoreCase));
        }
    }

    private static (double min, double max) DirectionAngles(string dir) => dir switch
    {
        "n" => (337.5, 22.5),
        "ne" => (22.5, 67.5),
        "e" => (67.5, 112.5),
        "se" => (112.5, 157.5),
        "s" => (157.5, 202.5),
        "sw" => (202.5, 247.5),
        "w" => (247.5, 292.5),
        "nw" => (292.5, 337.5),
        _ => (0, 360),
    };

    private static bool IsInArc(double bearing, double min, double max)
    {
        if (min > max) // Wraps around 0 (e.g., North: 337.5 to 22.5)
            return bearing >= min || bearing < max;
        return bearing >= min && bearing < max;
    }

    private static double GetBearing(GeoCoordinates from, GeoCoordinates to)
    {
        var dLng = ToRad(to.Lng - from.Lng);
        var y = Math.Sin(dLng) * Math.Cos(ToRad(to.Lat));
        var x = Math.Cos(ToRad(from.Lat)) * Math.Sin(ToRad(to.Lat)) -
                Math.Sin(ToRad(from.Lat)) * Math.Cos(ToRad(to.Lat)) * Math.Cos(dLng);
        var bearing = ToDeg(Math.Atan2(y, x));
        return (bearing + 360) % 360;
    }

    private static string DirectionName(double bearing) => bearing switch
    {
        < 22.5 => "north",
        < 67.5 => "northeast",
        < 112.5 => "east",
        < 157.5 => "southeast",
        < 202.5 => "south",
        < 247.5 => "southwest",
        < 292.5 => "west",
        < 337.5 => "northwest",
        _ => "north",
    };

    private static double DistanceKm(GeoCoordinates a, GeoCoordinates b)
    {
        const double R = 6371;
        var dLat = ToRad(b.Lat - a.Lat);
        var dLng = ToRad(b.Lng - a.Lng);
        var sinLat = Math.Sin(dLat / 2);
        var sinLng = Math.Sin(dLng / 2);
        var h = sinLat * sinLat + Math.Cos(ToRad(a.Lat)) * Math.Cos(ToRad(b.Lat)) * sinLng * sinLng;
        return R * 2 * Math.Atan2(Math.Sqrt(h), Math.Sqrt(1 - h));
    }

    private static bool IsInChicagoCore(GeoCoordinates c) =>
        c.Lat >= ChicagoLatMin && c.Lat <= ChicagoLatMax &&
        c.Lng >= ChicagoLngMin && c.Lng <= ChicagoLngMax;

    private static List<string> ReconstructPath(Dictionary<string, string> cameFrom, string current)
    {
        var path = new List<string> { current };
        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            path.Insert(0, current);
        }
        return path;
    }

    private static double ToRad(double deg) => deg * Math.PI / 180;
    private static double ToDeg(double rad) => rad * 180 / Math.PI;
}
