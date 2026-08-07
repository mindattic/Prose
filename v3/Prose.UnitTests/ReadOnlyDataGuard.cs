namespace Prose.UnitTests;

/// <summary>
/// Protects engine_data JSON files from modification during tests.
/// Sets all .json files to ReadOnly before tests run, restores them after.
/// Uses IDisposable so it works in a using block or try/finally.
/// </summary>
public sealed class ReadOnlyDataGuard : IDisposable
{
    private readonly List<string> protectedFiles = [];
    private bool disposed;

    public ReadOnlyDataGuard(string engineDataDir)
    {
        if (!Directory.Exists(engineDataDir)) return;

        foreach (var file in Directory.EnumerateFiles(engineDataDir, "*.json", SearchOption.AllDirectories))
        {
            var info = new FileInfo(file);
            if (!info.IsReadOnly)
            {
                info.IsReadOnly = true;
                protectedFiles.Add(file);
            }
        }
    }

    public int ProtectedCount => protectedFiles.Count;

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;

        foreach (var file in protectedFiles)
        {
            try
            {
                var info = new FileInfo(file);
                if (info.Exists) info.IsReadOnly = false;
            }
            catch { /* Best effort restore — don't let cleanup failures mask test failures */ }
        }
    }
}
