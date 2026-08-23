namespace Prose.Core.Services.WriteGate;

/// <summary>
/// Thrown by an <see cref="IWriteGateSyncCheck"/> to abort a save before it commits — the same
/// tier as an <see cref="ArgumentException"/>, surfaced to the caller (CLI/MCP handler) as a
/// clean rejection rather than a corrupted row landing in the database. Never caught and
/// swallowed silently: a rejected write must reach the human as an explicit error.
/// </summary>
public sealed class WriteGateRejectedException : Exception
{
    public WriteGateRejectedException(string message) : base(message)
    {
    }

    public WriteGateRejectedException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
