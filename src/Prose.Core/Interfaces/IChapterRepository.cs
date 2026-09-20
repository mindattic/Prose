using Prose.Core.Models;

namespace Prose.Core.Interfaces;

public interface IChapterRepository
{
    List<Chapter> ListChapters();
    Chapter? LoadChapter(string id);
    void SaveChapter(Chapter chapter);
    void DeleteChapter(string id);

    /// <summary>
    /// Fires after a chapter is committed to the store. Subscribers (e.g.
    /// <see cref="Prose.Core.Services.ContinuousQualityService"/>) use
    /// this to trigger post-save scans without depending on the legacy
    /// FileSystemWatcher path.
    /// </summary>
    event Action<Chapter>? OnChapterSaved;
}
