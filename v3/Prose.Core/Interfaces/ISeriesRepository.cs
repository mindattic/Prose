using Prose.Core.Models;

namespace Prose.Core.Interfaces;

public interface ISeriesRepository
{
    List<Series> ListSeries();
    Series? LoadSeries(string id);
    void SaveSeries(Series series);
    void DeleteSeries(string id);
}
