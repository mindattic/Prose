using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

public class FileSystemPathProvider : IPathProvider
{
    private readonly SettingsService _settings;

    public FileSystemPathProvider(SettingsService settings)
    {
        _settings = settings;
    }

    private string Root => _settings.CanonRootPath;

    public string DataRoot => Root;
    public string WorldbuildingDir => Path.Combine(Root, "worldbuilding");
    public string CharactersDir => Path.Combine(Root, "characters");
    public string EssencesDir => Path.Combine(Root, "essences");
    public string StoriesDir => EnsureDir(Path.Combine(Root, "stories"));
    public string EngineDataDir => EnsureDir(Path.Combine(Root, "engine_data"));
    public string NarrativeBiblePath => Path.Combine(Root, "narrative_bible.md");
    public string WorldDir => Path.Combine(Root, "world");
    public string FacetsDir => Path.Combine(Root, "character", "facets");
    public string GraphDir => EnsureDir(Path.Combine(Root, "engine_data", "graph"));

    private static string EnsureDir(string path)
    {
        Directory.CreateDirectory(path);
        return path;
    }
}
