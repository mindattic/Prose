using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>prose --log-search [--since &lt;dt&gt;] [--severity &lt;lvl&gt;] [--text &lt;q&gt;] [--take N] [--json]</c>
/// — a thin wrapper around the already-existing <see cref="LoggingService.Search"/> (durable
/// Serilog daily log files, not the live in-memory ring buffer). Mirrored MCP tool:
/// <c>search_logs</c>.
/// </summary>
public static class LogSearchCli
{
    public static int Run(string[] args, IServiceProvider services)
    {
        DateTime? since = null;
        string? severity = null, text = null;
        var take = 200;
        var json = args.Contains("--json");
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--since":    if (i + 1 < args.Length && DateTime.TryParse(args[++i], out var s)) since = s; break;
                case "--severity": if (i + 1 < args.Length) severity = args[++i]; break;
                case "--text":     if (i + 1 < args.Length) text = args[++i]; break;
                case "--take":     if (i + 1 < args.Length && int.TryParse(args[++i], out var t)) take = t; break;
            }
        }

        var logging = services.GetRequiredService<LoggingService>();
        var results = logging.Search(new LogSearchRequest
        {
            Since = since,
            MinSeverity = severity,
            SearchText = text,
            MaxResults = take,
        });

        if (json)
        {
            Console.WriteLine(JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true }));
            return 0;
        }

        foreach (var e in results)
        {
            Console.WriteLine($"{e.Timestamp:yyyy-MM-dd HH:mm:ss}  [{e.Level,-11}]  {e.Message}");
            if (!string.IsNullOrWhiteSpace(e.Exception))
                Console.WriteLine($"    {e.Exception}");
        }
        Console.WriteLine($"[log-search] {results.Count} entr(y/ies).");
        return 0;
    }
}
