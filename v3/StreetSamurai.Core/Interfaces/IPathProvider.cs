namespace StreetSamurai.Core.Interfaces;

/// <summary>
/// Resolves paths to canon directories. Allows different implementations
/// for MAUI (bundled resources) vs Blazor (file system) vs tests (temp dirs).
/// </summary>
public interface IPathProvider
{
    string DataRoot { get; }
    string WorldbuildingDir { get; }
    string CharactersDir { get; }
    string EssencesDir { get; }
    string StoriesDir { get; }
    string EngineDataDir { get; }
    string NarrativeBiblePath { get; }
    string WorldDir { get; }
    string FacetsDir { get; }
    string GraphDir { get; }
    string LogDir { get; }
    string ExportDir { get; }
    string ArchiveDir { get; }
}
