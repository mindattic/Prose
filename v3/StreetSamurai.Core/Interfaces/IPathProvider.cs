namespace StreetSamurai.Core.Interfaces;

/// <summary>
/// Resolves paths to canon directories. Allows different implementations
/// for Blazor (file system) vs tests (temp dirs).
/// </summary>
public interface IPathProvider
{
    string DataRoot { get; }
    string WorldbuildingDir { get; }
    string CharactersDir { get; }
    string EssencesDir { get; }
    string NarrativeBiblePath { get; }
    string WorldDir { get; }

    /// <summary>Read-only world content — baked into the deployment.</summary>
    string EngineDataDir { get; }

    /// <summary>
    /// Runtime-writable data (users, stories, graph, archives, exports, logs).
    /// On Azure: set SS_MUTABLE_DATA_ROOT=D:\home\data\StreetSamurai so this
    /// survives redeployments. Falls back to EngineDataDir on dev.
    /// </summary>
    string MutableDataDir { get; }

    string StoriesDir { get; }
    string BooksDir { get; }
    string SeriesDir { get; }
    string GraphDir { get; }
    string LogDir { get; }
    string ExportDir { get; }
    string ArchiveDir { get; }
    /// <summary>Media files — named {entityId}.{index:D2}.{ext}, e.g. abc123.00.png</summary>
    string MediaDir { get; }
    string MediaArchiveDir { get; }
}
