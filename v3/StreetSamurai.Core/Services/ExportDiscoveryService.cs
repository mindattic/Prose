using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Auto-discovers all IExportableRepository instances from DI.
/// No manual registration needed — adding a new repo that inherits
/// JsonDirectoryRepository automatically includes it in exports.
/// </summary>
public class ExportDiscoveryService
{
    private readonly IEnumerable<IExportableRepository> repos;

    public ExportDiscoveryService(IEnumerable<IExportableRepository> repos)
    {
        this.repos = repos;
    }

    /// <summary>Get all repos as a dictionary for the Export All function.</summary>
    public Dictionary<string, List<(string name, string json)>> GetAllRepos()
    {
        var result = new Dictionary<string, List<(string name, string json)>>();
        foreach (var repo in repos)
        {
            var name = repo.RepoName;
            // Capitalize first letter of each word
            name = string.Join(" ", name.Split(' ').Select(w =>
                w.Length > 0 ? char.ToUpper(w[0]) + w[1..] : w));

            try
            {
                var entries = repo.GetExportEntries();
                if (entries.Count > 0)
                    result[name] = entries;
            }
            catch (Exception ex) { Serilog.Log.Warning(ex, "Failed to load export entries from repo {RepoName}", name); }
        }
        return result;
    }

    /// <summary>Get repo names and counts for display.</summary>
    public List<(string name, int count)> GetRepoCounts()
    {
        return repos.Select(r =>
        {
            try { return (r.RepoName, r.GetExportEntries().Count); }
            catch (Exception ex) { Serilog.Log.Warning(ex, "Failed to get export entry count for repo {RepoName}", r.RepoName); return (r.RepoName, 0); }
        }).Where(x => x.Item2 > 0).OrderBy(x => x.RepoName).ToList();
    }
}
