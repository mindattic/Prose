namespace StreetSamurai.Core;

/// <summary>
/// Centralized string constants. Structured as nested static classes
/// to keep autocomplete navigable: Constants.Status.Draft, Constants.Paths.Stories, etc.
/// </summary>
public static class Constants
{
    /// <summary>Story and entity status values.</summary>
    public static class Status
    {
        public const string Draft = "draft";
        public const string Canon = "canon";
        public const string Pending = "pending";
        public const string Rejected = "rejected";
        public const string Promoted = "promoted";
        public const string NonCanon = "non-canon";
        public const string Published = "published";
        public const string Archived = "archived";
    }

    /// <summary>Entity type strings for canon data (supplements Graph.EntityTypes for repo-level types).</summary>
    public static class EntityType
    {
        public const string Character = "character";
        public const string Place = "place";
        public const string Organization = "organization";
        public const string Faction = "faction";
        public const string Weapon = "weapon";
        public const string Equipment = "equipment";
        public const string Technology = "technology";
        public const string Cyberware = "cyberware";
        public const string Genemods = "genemods";
        public const string Ammunition = "ammunition";
        public const string Apparel = "apparel";
        public const string Pharmaceutical = "pharmaceutical";
        public const string Substrate = "substrate";
        public const string ConsumerGood = "consumer_good";
        public const string Transportation = "transportation";
        public const string Synthetic = "elf";
        public const string Archetype = "archetype";
        public const string Contract = "contract";
        public const string News = "news";
        public const string Vocabulary = "vocabulary";
        public const string Quote = "quote";
        public const string Motif = "motif";
        public const string Document = "document";
        public const string Facet = "facet";
        public const string Corponation = "corponation";
    }

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

    /// <summary>File suffixes for story-associated files. All share the same GUID.</summary>
    public static class StorySuffix
    {
        public const string Story = ".story.json";
        public const string Checkpoint = ".checkpoint.json";
        public const string Outline = ".outline.json";
        public const string Events = ".events.json";
        public const string Knowledge = ".knowledge.json";
    }

    /// <summary>Default display values.</summary>
    public static class Defaults
    {
        public const string UntitledStory = "Untitled";
        public const string Loading = "Loading...";
        public const string DefaultModel = "claude-sonnet-4-6";
    }

    /// <summary>Bootstrap CSS badge classes for status values.</summary>
    public static class StatusBadge
    {
        public const string Draft = "bg-warning text-dark";
        public const string Canon = "bg-success";
        public const string Pending = "bg-info text-dark";
        public const string Rejected = "bg-danger";
        public const string Promoted = "bg-primary";
        public const string NonCanon = "bg-secondary";

        public static string For(string status) => status switch
        {
            Status.Draft => Draft,
            Status.Canon => Canon,
            Status.Pending => Pending,
            Status.Rejected => Rejected,
            Status.Promoted => Promoted,
            Status.NonCanon => NonCanon,
            _ => "bg-secondary",
        };
    }

    /// <summary>Bootstrap CSS badge classes for entity types.</summary>
    public static class TypeBadge
    {
        public static string For(string type) => type switch
        {
            EntityType.Character => "bg-info",
            EntityType.Place => "bg-success",
            EntityType.Faction => "bg-purple",
            EntityType.Corponation => "bg-warning text-dark",
            EntityType.Weapon => "bg-danger",
            EntityType.Equipment => "bg-primary",
            EntityType.Technology => "bg-cyan",
            EntityType.Document => "bg-secondary",
            _ => "bg-secondary",
        };
    }

    /// <summary>JSON property names used across multiple files.</summary>
    public static class JsonProps
    {
        public const string Id = "id";
    }

    /// <summary>
    /// Logical grouping of data repos under parent categories.
    /// Repos stay flat on disk — this is for UI navigation and organization only.
    /// </summary>
    public static class RepoGroups
    {
        public static readonly (string Group, string[] Repos)[] All =
        [
            ("Characters", ["people", "synthetics", "archetypes", "facets"]),
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
