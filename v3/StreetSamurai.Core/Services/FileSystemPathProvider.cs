using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

public class FileSystemPathProvider : IPathProvider
{
    private readonly SettingsService settings;

    public FileSystemPathProvider(SettingsService settings)
    {
        this.settings = settings;
    }

    private string Root => settings.CanonRootPath;

    public string DataRoot => Root;
    public string WorldbuildingDir => Path.Combine(Root, "worldbuilding");
    public string CharactersDir => Path.Combine(Root, "characters");
    public string EssencesDir => Path.Combine(Root, "essences");
    public string NarrativeBiblePath => Path.Combine(Root, "narrative_bible.md");
    public string WorldDir => Path.Combine(Root, "world");
    public string FacetsDir => Path.Combine(Root, "character", "facets");

    // Everything under engine/
    private string EngineRoot => Path.Combine(Root, Constants.Folders.Engine);
    public string EngineDataDir => EnsureDir(Path.Combine(EngineRoot, "data"));
    public string GraphDir => EnsureDir(Path.Combine(EngineRoot, "data", Constants.Folders.Graph));
    public string StoriesDir => EnsureDir(Path.Combine(EngineRoot, "data", Constants.Folders.Stories));
    public string LogDir => EnsureDir(Path.Combine(EngineRoot, Constants.Folders.Logs));
    public string ExportDir => EnsureDir(Path.Combine(EngineRoot, Constants.Folders.Exports));
    public string ArchiveDir => EnsureDir(Path.Combine(EngineRoot, Constants.Folders.Archives));

    private static string EnsureDir(string path)
    {
        Directory.CreateDirectory(path);
        return path;
    }
}
