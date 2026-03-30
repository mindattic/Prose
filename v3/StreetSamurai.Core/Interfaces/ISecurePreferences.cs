namespace StreetSamurai.Core.Interfaces;

public interface ISecurePreferences
{
    Task<string> GetAsync(string key);
    Task SetAsync(string key, string value);
}
