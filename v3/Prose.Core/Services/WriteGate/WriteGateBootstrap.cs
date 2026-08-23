namespace Prose.Core.Services.WriteGate;

/// <summary>
/// Wires the concrete write-gate checks/audit service into the ambient <see cref="WriteGateScope"/>
/// exactly once — mirrors <c>UniverseContext</c>'s own "constructor sets a static gateway" pattern
/// (see <see cref="WriteGateScope"/>'s doc comment for why it's a static gateway at all).
///
/// <b>Must be eagerly resolved once at process startup</b> (see <c>Prose.Hub/Program.cs</c>). A
/// singleton that is never resolved never runs its constructor — the gate would silently stay a
/// no-op forever, exactly the same failure class as the <c>NodeWorkbenchService</c> DI gap this
/// entire project plan ("Make Prose.Hub the real gatekeeper") exists to prevent. Only needs
/// resolving in <c>Prose.Hub</c>: that is the one process where CLI/MCP-dispatched writes actually
/// execute (<c>CliDispatch</c>/<c>ToolDispatch</c> reflection-invoke handler classes inside it) —
/// other processes (tests, a standalone CLI run) never resolving this just leaves the gate at its
/// safe default (empty checks, null audit service), same as <c>UniverseScope</c> when
/// <c>UniverseContext</c> is never constructed.
/// </summary>
public sealed class WriteGateBootstrap
{
    public WriteGateBootstrap(SelfAliasSyncCheck selfAlias, IWriteAuditService audit)
    {
        WriteGateScope.SyncChecks = new IWriteGateSyncCheck[] { selfAlias };
        WriteGateScope.AuditService = audit;
    }
}
