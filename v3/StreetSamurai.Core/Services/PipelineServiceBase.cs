namespace StreetSamurai.Core.Services;

/// <summary>
/// Shared pause/resume/progress infrastructure for all long-running admin pipeline services.
/// Subclasses implement RunCoreAsync; the base class handles state transitions and the pause gate.
/// Auto-pauses when the component navigates away (component calls Pause in Dispose).
/// </summary>
public abstract class PipelineServiceBase
{
    public record PipelineProgress(int Processed, int Total, string Phase, string Current = "");

    private CancellationTokenSource? cts;
    private volatile TaskCompletionSource<bool>? pauseTcs;

    public bool IsRunning { get; private set; }
    public bool IsPaused => pauseTcs != null;
    public PipelineProgress Progress { get; protected set; } = new(0, 0, "Idle");
    public event Action? StateChanged;

    // ── Lifecycle ─────────────────────────────────────────────

    public async Task RunAsync()
    {
        if (IsRunning) return;
        IsRunning = true;
        cts = new CancellationTokenSource();
        try
        {
            await RunCoreAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            Notify("Cancelled");
        }
        catch (Exception ex)
        {
            Notify("Error: " + ex.Message);
        }
        finally
        {
            IsRunning = false;
            cts.Dispose();
            cts = null;
            StateChanged?.Invoke();
        }
    }

    public void Pause()
    {
        if (!IsRunning || IsPaused) return;
        pauseTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        StateChanged?.Invoke();
    }

    public void Resume()
    {
        if (!IsPaused) return;
        var tcs = pauseTcs;
        pauseTcs = null;
        if (!IsRunning) _ = RunAsync();
        else tcs?.TrySetResult(true);
        StateChanged?.Invoke();
    }

    public void Cancel()
    {
        pauseTcs = null;
        OnCancel();
        cts?.Cancel();
    }

    // ── Subclass hooks ────────────────────────────────────────

    protected abstract Task RunCoreAsync(CancellationToken ct);

    protected virtual void OnCancel() { }

    // ── Helpers ───────────────────────────────────────────────

    protected async Task CheckPauseAsync(CancellationToken ct)
    {
        if (pauseTcs != null)
            await pauseTcs.Task.WaitAsync(ct);
    }

    protected void Notify(string phase, int processed = 0, int total = 0, string current = "")
    {
        Progress = new(processed, total, phase, current);
        StateChanged?.Invoke();
    }
}
