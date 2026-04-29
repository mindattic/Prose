using StreetSamurai.Core.Models;

namespace StreetSamurai.Core.Interfaces;

public interface IChapterRepository
{
    List<Chapter> ListChapters();
    Chapter? LoadChapter(string id);
    void SaveChapter(Chapter chapter);
    void DeleteChapter(string id);
}
