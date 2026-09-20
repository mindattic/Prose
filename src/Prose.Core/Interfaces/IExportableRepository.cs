namespace Prose.Core.Interfaces;

/// <summary>
/// Implemented by any repository that can export its data.
/// Discovered automatically via DI — no manual registration needed.
/// Adding a new repo that implements this interface automatically includes it in exports.
/// </summary>
public interface IExportableRepository
{
    string RepoName { get; }
    List<(string name, string json)> GetExportEntries();
}
