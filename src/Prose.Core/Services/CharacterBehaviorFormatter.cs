namespace Prose.Core.Services;

/// <summary>
/// Renders a character's recorded stress ladder and per-person interpersonal modes for the writer.
/// Shared by the scene X-Ray block and the dialogue context so both say the same thing.
/// Pure formatting over the character record — no inference, no scoring.
/// </summary>
public static class CharacterBehaviorFormatter
{
    public static readonly string[] StressLevels = ["low", "medium", "high", "critical"];

    /// <summary>Per-part caps: each stress level is the payload, so it gets more room than the rest.
    /// Fixed per part so a long "low" entry cannot crowd out "critical".</summary>
    public const int LevelChars = 140, OtherChars = 90;

    public static string FormatUnderPressure(
        IReadOnlyDictionary<string, string> stressResponses,
        IReadOnlyList<string> copingMechanisms,
        IReadOnlyList<string> blindSpots,
        string? speechUnderPressure)
    {
        var parts = new List<string>();
        foreach (var level in StressLevels)
        {
            var value = Lookup(stressResponses, level);
            if (!string.IsNullOrWhiteSpace(value)) parts.Add(Clip($"{level}: {value.Trim()}", LevelChars));
        }
        if (copingMechanisms.Count > 0) parts.Add(Clip("copes by: " + string.Join("; ", copingMechanisms), OtherChars));
        if (blindSpots.Count > 0) parts.Add(Clip("blind to: " + string.Join("; ", blindSpots), OtherChars));
        if (!string.IsNullOrWhiteSpace(speechUnderPressure)) parts.Add(Clip("speech: " + speechUnderPressure.Trim(), OtherChars));
        if (parts.Count == 0) return "";

        return "UNDER PRESSURE (pick the level the beat's stressor puts them at): " + string.Join(" | ", parts);
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
