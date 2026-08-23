namespace Prose.Core.Services.WriteGate;

/// <summary>
/// Ambient hook <see cref="Prose.Core.Data.ProseDbContext"/> reads from its
/// <c>SaveChanges</c>/<c>SaveChangesAsync</c> overrides — mirrors the existing
/// <c>UniverseScope</c> pattern (a static gateway a DI-constructed service populates once at
/// startup) rather than adding constructor dependencies to <c>ProseDbContext</c> itself, which is
/// resolved through <c>IDbContextFactory</c> in most call sites and would risk a DI-lifetime
/// mismatch if it took scoped services directly.
/// </summary>
public static class WriteGateScope
{
    /// <summary>Registered fast pre-save checks, wired once at startup. Empty until Layer B adds
    /// concrete checks — an empty list makes the gate a no-op, never a save-time regression.</summary>
    public static IReadOnlyList<IWriteGateSyncCheck> SyncChecks { get; set; } = Array.Empty<IWriteGateSyncCheck>();

    /// <summary>Registered post-save async dispatcher, wired once at startup. Null until Layer B
    /// wires a concrete implementation — a null dispatcher makes the post-save step a no-op.</summary>
    public static IWriteAuditService? AuditService { get; set; }

    /// <summary>
    /// Caller tag for the write currently in flight (CLI handler class name, MCP tool method,
    /// etc.), for the findings/ledger trail. Defaults to "unknown" — nothing sets this yet;
    /// wiring <c>CliDispatch</c>/<c>ToolDispatch</c> to push a real tag per invocation is a fast
    /// follow, not required for the gate mechanism itself to function.
    /// </summary>
    public static string CurrentSource
    {
        get => currentSource.Value ?? "unknown";
        set => currentSource.Value = value;
    }
    private static readonly AsyncLocal<string?> currentSource = new();
}
