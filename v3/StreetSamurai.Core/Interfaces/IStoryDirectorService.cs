using StreetSamurai.Core.Services;

namespace StreetSamurai.Core.Interfaces;

public interface IStoryDirectorService
{
    event Action<DirectorProgress>? OnProgress;

    bool IsGenerating { get; }
    AutonomousStory? CurrentStory { get; }
    string ProgressMessage { get; }
    int ProgressCurrent { get; }
    int ProgressTotal { get; }

    Task<AutonomousStory> SurpriseMeAsync(CancellationToken ct = default);
    Task<AutonomousStory> SurpriseMeForAsync(string characterName, CancellationToken ct = default);
    Task<AutonomousStory> ResumeStoryAsync(AutonomousStory story, string? nextBeatGoalOverride = null, CancellationToken ct = default);
    AutonomousStory? LoadCheckpoint(string projectId);
    List<AutonomousStory> ListCheckpoints();
}
