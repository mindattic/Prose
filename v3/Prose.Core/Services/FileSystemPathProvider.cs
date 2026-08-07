using Prose.Core.Interfaces;

namespace Prose.Core.Services;

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
    public string CharactersDir => Path.Combine(Root, "people");
    public string EssencesDir => Path.Combine(Root, "essences");
    public string NarrativeBiblePath => Path.Combine(Root, "narrative_bible.md");
    public string WorldDir => Path.Combine(Root, "world");

    // Read-only world content baked into the deployment
    private string EngineRoot => Path.Combine(Root, Constants.Folders.Engine);
    public string EngineDataDir => EnsureDir(Path.Combine(EngineRoot, "data"));

    // Runtime-writable data — separate root on Azure so redeployments don't wipe it.
    // PROSE_MUTABLE_DATA_ROOT=D:\home\data\Prose in Azure App Configuration.
    // Falls back to EngineDataDir on dev (no env var set).
    public string MutableDataDir => EnsureDir(
        Environment.GetEnvironmentVariable("PROSE_MUTABLE_DATA_ROOT") is { Length: > 0 } v
            ? v
            : Path.Combine(EngineRoot, "data"));

    public string ChaptersDir => EnsureDir(Path.Combine(MutableDataDir, Constants.Folders.Chapters));
    public string BooksDir => EnsureDir(Path.Combine(MutableDataDir, Constants.Folders.Books));
    public string SeriesDir => EnsureDir(Path.Combine(MutableDataDir, Constants.Folders.Series));
    public string GraphDir => EnsureDir(Path.Combine(MutableDataDir, Constants.Folders.Graph));
    public string LogDir => EnsureDir(Path.Combine(MutableDataDir, Constants.Folders.Logs));
    public string ExportDir => EnsureDir(Path.Combine(MutableDataDir, Constants.Folders.Exports));
    public string ArchiveDir => EnsureDir(Path.Combine(MutableDataDir, Constants.Folders.Archives));
    public string MediaDir => EnsureDir(Path.Combine(EngineDataDir, Constants.Folders.Media));
    public string MediaArchiveDir => EnsureDir(Path.Combine(ArchiveDir, Constants.Folders.Media));

    private static string EnsureDir(string path)
    {
        Directory.CreateDirectory(path);
        return path;
    }
}
