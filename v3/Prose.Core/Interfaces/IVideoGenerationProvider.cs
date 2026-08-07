namespace Prose.Core.Interfaces;

/// <summary>Seed frame + motion prompt submitted to an image-to-video backend.</summary>
public record VideoGenerationRequest(byte[] SeedImage, string SeedImageExtension, string Prompt, int DurationSeconds);

/// <summary>A rendered video clip plus the file extension it should be saved with.</summary>
public record VideoGenerationResult(byte[] Bytes, string Extension);

public enum VideoJobState { Pending, Running, Done, Failed }

/// <summary>Current state of a submitted job. <see cref="Error"/> is set only when <see cref="State"/> is Failed.</summary>
public record VideoJobStatus(VideoJobState State, string? Error = null);

/// <summary>
/// One image-to-video generation backend. Implementations: <c>kling</c>, <c>runway</c>, <c>sora</c>.
/// All three vendors run generation as an async job — submit, poll until done, then download —
/// so the interface mirrors that shape directly rather than pretending it's a single call like
/// <see cref="ICoverImageProvider"/>. <see cref="Services.BookTokVideoService"/> owns the shared
/// poll loop; each provider only implements its own three REST calls.
/// </summary>
public interface IVideoGenerationProvider
{
    /// <summary>Stable provider key used in CLI calls: "kling" | "runway" | "sora".</summary>
    string Id { get; }

    /// <summary>True once the provider's API key is configured in Settings.</summary>
    bool IsConfigured { get; }

    /// <summary>Longest clip this provider will accept in one job.</summary>
    int MaxDurationSeconds { get; }

    /// <summary>Submits a new image-to-video job and returns the vendor's job id.</summary>
    Task<string> SubmitJobAsync(VideoGenerationRequest request, CancellationToken ct = default);

    /// <summary>Checks the status of a previously submitted job.</summary>
    Task<VideoJobStatus> PollAsync(string jobId, CancellationToken ct = default);

    /// <summary>Downloads the finished clip for a job whose <see cref="PollAsync"/> reported Done.</summary>
    Task<VideoGenerationResult> DownloadAsync(string jobId, CancellationToken ct = default);
}
