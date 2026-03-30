using StreetSamurai.Core.Models;

namespace StreetSamurai.Core.Interfaces;

/// <summary>
/// Repository for story projects and their blocks.
/// Designed as the seam for migrating from JSON files to a database.
/// </summary>
public interface IStoryBlockRepository
{
    /// <summary>List all story projects (metadata only — blocks loaded on demand).</summary>
    List<StoryProject> ListProjects();

    /// <summary>Load a full project including all blocks.</summary>
    StoryProject? LoadProject(string id);

    /// <summary>Save a project and all its blocks.</summary>
    void SaveProject(StoryProject project);

    /// <summary>Delete a project and its data.</summary>
    void DeleteProject(string id);

    /// <summary>
    /// Rename a project's prefix. Handles renaming the backing store
    /// (e.g., JSON file rename) and updating all block IDs.
    /// Returns the updated project.
    /// </summary>
    StoryProject RenamePrefix(string projectId, string newPrefix);

    /// <summary>Check if a prefix is already in use by another project.</summary>
    bool PrefixExists(string prefix, string? excludeProjectId = null);
}
