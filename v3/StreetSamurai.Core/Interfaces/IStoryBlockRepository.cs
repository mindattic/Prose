using StreetSamurai.Core.Models;

namespace StreetSamurai.Core.Interfaces;

public interface IStoryBlockRepository
{
    List<StoryProject> ListProjects();
    StoryProject? LoadProject(string id);
    void SaveProject(StoryProject project);
    void DeleteProject(string id);
}
