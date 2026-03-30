using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

public class FileSystemCanonPathProvider : ICanonPathProvider
{
    private readonly SettingsService _settings;

    public FileSystemCanonPathProvider(SettingsService settings)
    {
        _settings = settings;
    }

    private string Root => _settings.CanonRootPath;

    public string CanonRoot => Root;
    public string WorldbuildingDir => Path.Combine(Root, "worldbuilding");
    public string CharactersDir => Path.Combine(Root, "characters");
    public string EssencesDir => Path.Combine(Root, "essences");
    public string StoriesDir => EnsureDir(Path.Combine(Root, "stories"));
    public string CanonQueueDir => EnsureDir(Path.Combine(Root, "canon_queue"));
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
