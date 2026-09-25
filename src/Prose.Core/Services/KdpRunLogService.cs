using Prose.Core.Kdp;

namespace Prose.Core.Services;

/// <summary>
/// Durable mirror of every line KdpPublish posts during a run — written to the KDP store
/// (<see cref="KdpStore"/>: a <c>Runs</c> row per run, a <c>RunLines</c> row per line) and to a
/// plain-text <c>.log</c> file under <c>tools/kdp/logs/</c> that a terminal can tail while the run
/// is live (the same format <see cref="KdpRunLogFormat"/> imports and exports). Exists because the
/// WPF app's own <c>PostLogAsync</c> otherwise only renders lines into its WebView2 control-panel
/// UI — nothing durable to follow along with or query after the fact.
/// <para>Before the KDP store existed, the durable copy was a raw-DDL <c>dbo.KdpRunLog</c> table in
/// the Prose SQL Server database. That table (and its rows) is left exactly as it was; new runs
/// are no longer written there.</para>
/// Every write is best-effort and never throws into the caller: a logging failure must never
/// interrupt or fail an actual publish run. Writes are serialized so lines land in order.
/// </summary>
public class KdpRunLogService
{
    private readonly KdpStore store;
    private readonly SemaphoreSlim gate = new(1, 1);
    private string? logFilePath;
    private Task runStarted = Task.CompletedTask;

    public KdpRunLogService(KdpStore store)
    {
        this.store = store;
    }

    /// <summary>Call once at the start of a run. Creates a fresh log file under
    /// tools/kdp/logs/ and returns a RunId every subsequent <see cref="LogAsync"/> call for
    /// this run should pass, so a query can pull just this run's lines out of the store.</summary>
    public Guid StartRun(string repoRoot, IEnumerable<string> codes)
    {
        var runId = Guid.CreateVersion7();
        var codeList = codes.ToList();
        // Milliseconds too: the name is a unique key, and two runs in one second lost the second
        // run's store log (its row insert failed and every line insert after it).
        var fileName = $"kdp-run-{DateTime.UtcNow:yyyyMMdd-HHmmss-fff}.log";
        try
        {
            var logsDir = Path.Combine(repoRoot, "tools", "kdp", "logs");
            Directory.CreateDirectory(logsDir);
            logFilePath = Path.Combine(logsDir, fileName);
        }
        catch { logFilePath = null; }

        runStarted = StartInStoreAsync(runId, codeList, fileName);
        _ = LogAsync(runId, KdpRunLogFormat.StartedMessage(runId, codeList));
        return runId;
    }

    private async Task StartInStoreAsync(Guid runId, List<string> codes, string fileName)
    {
        try { await store.StartRunAsync(runId, codes, fileName); }
        catch { /* best-effort — the text log still has the run */ }
    }

    public async Task LogAsync(Guid runId, string message)
    {
        var at = DateTimeOffset.UtcNow;
        await gate.WaitAsync();
        try
        {
            try
            {
                if (logFilePath != null)
                    await File.AppendAllTextAsync(logFilePath, KdpRunLogFormat.Stamp(at, message) + KdpRunLogFormat.NewLine);
            }
            catch { /* best-effort — a file-write failure must never break the run */ }

            try
            {
                await runStarted;
                await store.AppendRunLineAsync(runId, at, message);
            }
            catch { /* best-effort — same guarantee for the store */ }
        }
        finally { gate.Release(); }
    }

    /// <summary>Stamps the run's finish time in the store. Best-effort.</summary>
    public async Task FinishRunAsync(Guid runId)
    {
        try
        {
            await runStarted;
            await store.FinishRunAsync(runId);
        }
        catch { /* best-effort */ }
    }
}
