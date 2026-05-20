namespace StreetSamurai.Core.Services;

/// <summary>
/// Thrown by <see cref="StrandWorkbenchService.UpdateBeatTextAsync"/> when
/// the caller supplied an <c>expectedUpdatedAt</c> that doesn't match the
/// row's current <c>UpdatedAt</c> — meaning the beat was modified between
/// when the caller loaded it and when they tried to save.
///
/// The exception carries the freshly-loaded <see cref="CurrentText"/> and
/// <see cref="CurrentUpdatedAt"/> so the UI can surface a clear "another
/// tab edited this — keep yours or reload?" choice instead of silently
/// clobbering.
/// </summary>
public class BeatConflictException : Exception
{
    public Guid BeatId { get; }
    public DateTime ExpectedUpdatedAt { get; }
    public DateTime CurrentUpdatedAt { get; }
    public string CurrentText { get; }

    public BeatConflictException(Guid beatId, DateTime expected, DateTime current, string currentText)
        : base($"Beat {beatId} was modified elsewhere. Expected UpdatedAt={expected:o}, current={current:o}.")
    {
        BeatId = beatId;
        ExpectedUpdatedAt = expected;
        CurrentUpdatedAt = current;
        CurrentText = currentText ?? "";
    }
}
