namespace Prose.Core.Services;

/// <summary>
/// Process-wide "a client did something" clock (PRS-T1004). The Hub stamps it on every request that is
/// not a health probe; periodic background work reads it to stand down while nobody is using Prose, so
/// an idle process stops querying the database and a serverless (auto-pausing) database can pause.
///
/// <para>Starts at process start, so a fresh process counts as active for one full idle window.</para>
/// </summary>
public static class ProseActivity
{
    private static long lastActivityTicks = DateTime.UtcNow.Ticks;

    /// <summary>Record client activity now.</summary>
    public static void Touch() => Interlocked.Exchange(ref lastActivityTicks, DateTime.UtcNow.Ticks);

    public static DateTime LastActivityUtc => new(Interlocked.Read(ref lastActivityTicks), DateTimeKind.Utc);

    /// <summary>True when nothing has happened for <paramref name="window"/>. A zero or negative window is never idle.</summary>
    public static bool IsIdle(TimeSpan window, DateTime nowUtc) => IsIdle(window, LastActivityUtc, nowUtc);

    /// <summary>The pure decision behind <see cref="IsIdle(TimeSpan, DateTime)"/>.</summary>
    public static bool IsIdle(TimeSpan window, DateTime lastActivityUtc, DateTime nowUtc) =>
        window > TimeSpan.Zero && nowUtc - lastActivityUtc >= window;
}
