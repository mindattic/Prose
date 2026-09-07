namespace Prose.Core.Services;

/// <summary>
/// Thrown by the writer when a draft fails the gate twice (RFC 0012 §3.4 step 3). Carries the
/// last draft and the reasons so the caller can show them to the author. A failing draft is
/// never saved — the CLIs already treat any exception from <c>WriteAsync</c> as "failed, not
/// saved", which is exactly the semantics wanted.
/// </summary>
public sealed class BeatGateRefusedException : Exception
{
    public string Draft { get; }
    public IReadOnlyList<string> Reasons { get; }
    public int Attempts { get; }

    public BeatGateRefusedException(string draft, IReadOnlyList<string> reasons, int attempts)
        : base($"Gate refused the draft after {attempts} attempt(s): " + string.Join(" | ", reasons))
    {
        Draft = draft;
        Reasons = reasons;
        Attempts = attempts;
    }
}
