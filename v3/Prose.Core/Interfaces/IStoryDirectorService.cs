using Prose.Core.Services;

namespace Prose.Core.Interfaces;

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
    Task<AutonomousStory> GuidedStoryAsync(List<string> protagonists, string synopsis, string? location = null, int targetBeats = 8, CancellationToken ct = default);
    Task<AutonomousStory> ResumeStoryAsync(AutonomousStory story, string? nextBeatGoalOverride = null, CancellationToken ct = default);
    Task<AutonomousStory> RepairStoryAsync(AutonomousStory story, CancellationToken ct = default);
    AutonomousStory? LoadCheckpoint(string projectId);
    List<AutonomousStory> ListCheckpoints();
}
