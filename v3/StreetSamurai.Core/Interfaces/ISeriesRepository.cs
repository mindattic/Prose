using StreetSamurai.Core.Models;

namespace StreetSamurai.Core.Interfaces;

public interface ISeriesRepository
{
    List<Series> ListSeries();
    Series? LoadSeries(string id);
    void SaveSeries(Series series);
    void DeleteSeries(string id);
}
