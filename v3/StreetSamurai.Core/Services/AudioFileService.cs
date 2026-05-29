using System.Diagnostics;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

public class AudioFileService : IAudioFileService
{
    private readonly IPathProvider paths;

    public AudioFileService(IPathProvider paths)
    {
        this.paths = paths;
    }

    private string AudioDir
    {
        get
        {
            var dir = Path.Combine(Path.GetDirectoryName(paths.ChaptersDir)!, Constants.Folders.Audio);
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    public async Task<string> SaveAudioAsync(byte[] audioData, string? fileName = null)
    {
        fileName ??= $"narration_{DateTime.UtcNow:yyyyMMdd_HHmmss}.mp3";
        var filePath = Path.Combine(AudioDir, fileName);
        await File.WriteAllBytesAsync(filePath, audioData);
        return filePath;
    }

    public void RevealInExplorer(string filePath)
    {
        if (!File.Exists(filePath)) return;

        // Launch the OS file browser fire-and-forget, but Dispose the returned
        // Process so we release our handle — without this the handle lingers
        // until finalization, leaking one per reveal.
        if (OperatingSystem.IsWindows())
        {
            Process.Start("explorer.exe", $"/select,\"{filePath}\"")?.Dispose();
        }
        else if (OperatingSystem.IsMacOS())
        {
            Process.Start("open", $"-R \"{filePath}\"")?.Dispose();
        }
        else if (OperatingSystem.IsLinux())
        {
            Process.Start("xdg-open", $"\"{Path.GetDirectoryName(filePath)}\"")?.Dispose();
        }
    }
}
