using System.Globalization;

namespace Prose.Cli;

/// <summary>
/// Date flags for CLI handlers. <c>DateTime.TryParse(s, out d)</c> read the Hub's culture
/// (13/09 vs 09/13) and a value that failed to parse silently dropped the filter, so the
/// command answered for "now" or "all time" as if it had been asked. These parse invariantly
/// and throw on bad input (CliDispatch turns that into exit 1 with the message).
/// </summary>
public static class CliDates
{
    /// <summary>A real-world instant ("--since", "--as-of" on logs): read as local time when no
    /// offset is given, returned as UTC.</summary>
    public static DateTime ParseInstant(string value, string flag) =>
        DateTime.TryParse(value, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeLocal | DateTimeStyles.AdjustToUniversal, out var d)
            ? d
            : throw new ArgumentException($"{flag} is not a date: '{value}' (use ISO, e.g. 2026-09-20 or 2026-09-20T14:00Z).");

    /// <summary>No time-zone shift: in-world story dates, or filters the service compares as given.</summary>
    public static DateTime ParseAsGiven(string value, string flag) =>
        DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
            ? d
            : throw new ArgumentException($"{flag} is not a date: '{value}' (use ISO, e.g. 2225-03-12).");
}
