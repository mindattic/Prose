using System.Globalization;
using System.Text.RegularExpressions;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Reads and searches Serilog daily log files. Supports filtering by time range,
/// severity level, and free-text search. Logs are stored one file per day in
/// a "logs" folder under the data root.
/// </summary>
public class LoggingService
{
    private readonly IPathProvider paths;

    public LoggingService(IPathProvider paths)
    {
        this.paths = paths;
    }

    public string LogDirectory => Path.Combine(paths.DataRoot, "logs");

    /// <summary>
    /// Search log entries across daily log files.
    /// </summary>
    public List<LogEntry> Search(LogSearchRequest request)
    {
        var logDir = LogDirectory;
        if (!Directory.Exists(logDir)) return [];

        var cutoff = request.Since ?? DateTime.Now.AddDays(-1);
        var results = new List<LogEntry>();

        // Get log files sorted newest first
        var logFiles = Directory.GetFiles(logDir, "log-*.txt")
            .Select(f => new { Path = f, Date = ExtractDate(f) })
            .Where(f => f.Date != null && f.Date.Value >= cutoff.Date)
            .OrderByDescending(f => f.Date)
            .ToList();

        foreach (var file in logFiles)
        {
            try
            {
                var entries = ParseLogFile(file.Path);

                foreach (var entry in entries)
                {
                    if (entry.Timestamp < cutoff) continue;

                    if (request.MinSeverity != null && GetSeverityRank(entry.Level) < GetSeverityRank(request.MinSeverity))
                        continue;

                    if (!string.IsNullOrWhiteSpace(request.SearchText) &&
                        !entry.Message.Contains(request.SearchText, StringComparison.OrdinalIgnoreCase) &&
                        !(entry.Exception?.Contains(request.SearchText, StringComparison.OrdinalIgnoreCase) ?? false))
                        continue;

                    results.Add(entry);
                }
            }
            catch { /* Skip unreadable log files */ }
        }

        return results.OrderByDescending(e => e.Timestamp).Take(request.MaxResults).ToList();
    }

    /// <summary>Get available log dates for the file picker.</summary>
    public List<DateTime> GetAvailableDates()
    {
        var logDir = LogDirectory;
        if (!Directory.Exists(logDir)) return [];

        return Directory.GetFiles(logDir, "log-*.txt")
            .Select(f => ExtractDate(f))
            .Where(d => d != null)
            .Select(d => d!.Value)
            .OrderByDescending(d => d)
            .ToList();
    }

    /// <summary>Get total log file size for a date.</summary>
    public long GetLogSizeBytes(DateTime date)
    {
        var path = GetLogPath(date);
        return File.Exists(path) ? new FileInfo(path).Length : 0;
    }

    private string GetLogPath(DateTime date) =>
        Path.Combine(LogDirectory, $"log-{date:yyyyMMdd}.txt");

    private static DateTime? ExtractDate(string filePath)
    {
        var name = Path.GetFileNameWithoutExtension(filePath);
        // Format: log-20260404
        if (name.Length >= 12 && name.StartsWith("log-"))
        {
            var datePart = name[4..];
            // Handle Serilog's underscore suffix for same-day rollover
            if (datePart.Contains('_')) datePart = datePart[..datePart.IndexOf('_')];
            if (DateTime.TryParseExact(datePart, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                return date;
        }
        return null;
    }

    private static List<LogEntry> ParseLogFile(string path)
    {
        var entries = new List<LogEntry>();
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream);

        LogEntry? current = null;
        string? line;

        while ((line = reader.ReadLine()) != null)
        {
            var parsed = TryParseLogLine(line);
            if (parsed != null)
            {
                if (current != null) entries.Add(current);
                current = parsed;
            }
            else if (current != null)
            {
                // Continuation line (stack trace, multi-line message)
                if (current.Exception == null)
                    current.Exception = line;
                else
                    current.Exception += "\n" + line;
            }
        }

        if (current != null) entries.Add(current);
        return entries;
    }

    // Matches any supported timestamp format followed by [LVL] message
    private static readonly Regex LogLinePattern = new(
        @"^(.+?) \[(\w{3})\] (.+)$",
        RegexOptions.Compiled);

    // All format strings we may encounter in log files
    private static readonly string[] ParseFormats = SettingsService.TimestampFormats
        .Select(f => f.Format).ToArray();

    private static LogEntry? TryParseLogLine(string line)
    {
        var match = LogLinePattern.Match(line);
        if (!match.Success) return null;

        if (!DateTime.TryParseExact(match.Groups[1].Value, ParseFormats,
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var timestamp))
            return null;

        return new LogEntry
        {
            Timestamp = timestamp,
            Level = NormalizeLevel(match.Groups[2].Value),
            Message = match.Groups[3].Value,
        };
    }

    private static string NormalizeLevel(string abbrev) => abbrev.ToUpperInvariant() switch
    {
        "VRB" => "Verbose",
        "DBG" => "Debug",
        "INF" => "Information",
        "WRN" => "Warning",
        "ERR" => "Error",
        "FTL" => "Fatal",
        _ => abbrev,
    };

    private static int GetSeverityRank(string? level) => level switch
    {
        "Verbose" => 0,
        "Debug" => 1,
        "Information" => 2,
        "Warning" => 3,
        "Error" => 4,
        "Fatal" => 5,
        _ => 0,
    };
}

public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public string Level { get; set; } = "";
    public string Message { get; set; } = "";
    public string? Exception { get; set; }
}

public class LogSearchRequest
{
    public DateTime? Since { get; set; }
    public string? MinSeverity { get; set; }
    public string? SearchText { get; set; }
    public int MaxResults { get; set; } = 500;
}
