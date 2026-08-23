namespace Prose.Core.Services.WriteGate;

/// <summary>
/// One entity's change, captured from <c>ChangeTracker</c> before <c>base.SaveChangesAsync</c>
/// runs (so <c>EntityState</c>/modified-property info is still intact) and dispatched to
/// <see cref="IWriteAuditService"/> after the save commits successfully. Source is a free-form
/// caller tag (e.g. the CLI handler class name or MCP tool method) for the findings/ledger trail —
/// "did the check run, and for what write" must be answerable later without guessing.
/// </summary>
public sealed record WriteEvent(
    WriteSubject Subject,
    Guid PrimaryId,
    Guid? BookNodeId,
    Guid? UniverseId,
    string Source,
    IReadOnlyDictionary<string, object?>? Extra = null);
