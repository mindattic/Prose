using StreetSamurai.Core.Services;

namespace StreetSamurai.Core.Interfaces;

public interface IStoryDirectorService
{
    event Action<DirectorProgress>? OnProgress;

    Task<AutonomousStory> SurpriseMeAsync(CancellationToken ct = default);
    Task<AutonomousStory> ResumeStoryAsync(AutonomousStory story, CancellationToken ct = default);
    AutonomousStory? LoadCheckpoint(string projectId);
    List<AutonomousStory> ListCheckpoints();
}
