namespace Prose.Core.Services;

/// <summary>
/// Tracks read-only vs edit mode per repository page.
/// State persists across entry and repo navigation within the session.
/// Default is read-only (beautifully formatted).
/// </summary>
public class ViewModeService
{
    private readonly Dictionary<string, bool> editModes = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Returns true if the repo is in edit mode.</summary>
    public bool IsEditMode(string repoName) => editModes.GetValueOrDefault(repoName, false);

    /// <summary>Toggle edit mode for a repo.</summary>
    public void Toggle(string repoName) => editModes[repoName] = !IsEditMode(repoName);

    /// <summary>Set edit mode explicitly.</summary>
    public void SetEditMode(string repoName, bool edit) => editModes[repoName] = edit;
}
