using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Prose.Hub.Contracts;

namespace Prose.Hub.Logging;

/// <summary>
/// Makes the Hub's own console output (currently invisible — it runs with a hidden window)
/// observable: a bounded in-memory ring buffer of every logged line, plus an <see cref="OnLine"/>
/// callback a subscriber can wire up to push each line live (see Phase 4 — the
/// SignalR/ObservabilityHub wiring — which sets this in Program.cs after building the app).
/// Registered as both a singleton (so it's resolvable for wiring/querying) and an
/// <see cref="ILoggerProvider"/> (so ASP.NET Core's logging pipeline actually feeds it) via the
/// two-step DI registration <see cref="ILoggerProvider"/> instances normally require.
/// </summary>
public sealed class RingBufferLoggerProvider : ILoggerProvider
{
    private const int Capacity = 5000;
    private readonly ConcurrentQueue<LogLineDto> buffer = new();

    /// <summary>Set once, after the app is built, by whatever wants to push new lines live
    /// (the SignalR hub forward, Phase 4). Left unset here — Phase 3 only captures.</summary>
    public Action<LogLineDto>? OnLine { get; set; }

    /// <summary>The most recent <paramref name="take"/> lines — used for initial page
    /// load/reconnect catch-up (<c>GET /api/logs/recent</c>).</summary>
    public IReadOnlyList<LogLineDto> Recent(int take = 200)
    {
        var all = buffer.ToArray();
        return all.Length <= take ? all : all[^take..];
    }

    private void Enqueue(LogLineDto line)
    {
        buffer.Enqueue(line);
        while (buffer.Count > Capacity) buffer.TryDequeue(out _);
        OnLine?.Invoke(line);
    }

    public ILogger CreateLogger(string categoryName) => new RingBufferLogger(this, categoryName);

    public void Dispose() { }

    private sealed class RingBufferLogger(RingBufferLoggerProvider owner, string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;
            owner.Enqueue(new LogLineDto(DateTime.UtcNow, logLevel.ToString(), category, formatter(state, exception), exception?.ToString()));
        }
    }
}
