namespace Prose.Core.Services;

/// <summary>
/// Renders a character's recorded stress ladder and per-person interpersonal modes for the writer.
/// Shared by the scene X-Ray block and the dialogue context so both say the same thing.
/// Pure formatting over the character record — no inference, no scoring.
/// </summary>
public static class CharacterBehaviorFormatter
{
    public static readonly string[] StressLevels = ["low", "medium", "high", "critical"];

    public static string FormatUnderPressure(
        IReadOnlyDictionary<string, string> stressResponses,
        IReadOnlyList<string> copingMechanisms,
        IReadOnlyList<string> blindSpots,
        string? speechUnderPressure,
        int maxChars)
    {
        var parts = new List<string>();
        foreach (var level in StressLevels)
        {
            var value = Lookup(stressResponses, level);
            if (!string.IsNullOrWhiteSpace(value)) parts.Add($"{level}: {value.Trim()}");
        }
        if (copingMechanisms.Count > 0) parts.Add("copes by: " + string.Join("; ", copingMechanisms));
        if (blindSpots.Count > 0) parts.Add("blind to: " + string.Join("; ", blindSpots));
        if (!string.IsNullOrWhiteSpace(speechUnderPressure)) parts.Add("speech: " + speechUnderPressure.Trim());
        if (parts.Count == 0) return "";

        // Every part gets an even share so a long "low" entry cannot crowd out "critical".
        var share = Math.Max(40, maxChars / parts.Count);
        return "UNDER PRESSURE (pick the level the beat's stressor puts them at): "
            + string.Join(" | ", parts.Select(p => Clip(p, share)));
    }

    public static string? ModeToward(IReadOnlyDictionary<string, string> interpersonalModes, string otherName)
    {
        foreach (var (key, value) in interpersonalModes)
            if (!string.IsNullOrWhiteSpace(value) && NamesOverlap(key, otherName)) return value.Trim();
        return null;
    }

    /// <summary>Either name contains the other, case-insensitive; names under 3 chars never match.</summary>
    public static bool NamesOverlap(string? x, string? y) =>
        !string.IsNullOrWhiteSpace(x) && !string.IsNullOrWhiteSpace(y)
        && x.Trim().Length >= 3 && y.Trim().Length >= 3
        && (x.Contains(y.Trim(), StringComparison.OrdinalIgnoreCase) || y.Contains(x.Trim(), StringComparison.OrdinalIgnoreCase));

    private static string? Lookup(IReadOnlyDictionary<string, string> map, string key)
    {
        if (map.TryGetValue(key, out var v)) return v;
        foreach (var (k, value) in map)
            if (string.Equals(k.Trim(), key, StringComparison.OrdinalIgnoreCase)) return value;
        return null;
    }

    public static string Clip(string s, int max) =>
        s.Length <= max ? s : s[..max].TrimEnd() + "…";
}
