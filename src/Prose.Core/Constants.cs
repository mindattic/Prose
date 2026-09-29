namespace Prose.Core;

/// <summary>
/// Centralized string constants. Structured as nested static classes
/// to keep autocomplete navigable: Constants.Folders.Audio, Constants.Defaults.DefaultModel, etc.
/// </summary>
public static class Constants
{
    /// <summary>Folder names relative to DataRoot.</summary>
    public static class Folders
    {
        public const string Engine = "engine";
        public const string Chapters = "chapters";
        public const string Books = "books";
        public const string Series = "series";
        public const string Archives = "archives";
        public const string Graph = "graph";
        public const string Audio = "audio";
        public const string Exports = "exports";
        public const string Logs = "logs";
        public const string Media = "media";
    }

    /// <summary>Default values.</summary>
    public static class Defaults
    {
        public const string DefaultModel = "claude-sonnet-4-6";
    }

    /// <summary>
    /// Logical grouping of data repos under parent categories.
    /// Repos stay flat on disk — this is for UI navigation and organization only.
    /// </summary>
    public static class RepoGroups
    {
        public static readonly (string Group, string[] Repos)[] All =
        [
            ("Characters", ["people", "archetypes"]),
            ("Organizations", ["corponations", "subsidiaries", "factions", "contracts"]),
            ("Gear", ["weaponry", "ammunition", "cyberware", "equipment", "apparel", "genemods", "pharmaceuticals"]),
            ("World", ["places", "transportation", "materials", "technology", "automata"]),
            ("Culture", ["documents", "quotes", "vocabulary", "news", "entertainment", "consumer_goods", "motifs"]),
            ("Chapter", ["chapters"]),
        ];

        /// <summary>Look up which group a repo belongs to.</summary>
        public static string? GroupFor(string repoName)
        {
            foreach (var (group, repos) in All)
                if (repos.Contains(repoName, StringComparer.OrdinalIgnoreCase))
                    return group;
            return null;
        }
    }
}
