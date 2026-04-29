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
        fileName ??= $"narration_{DateTime.Now:yyyyMMdd_HHmmss}.mp3";
        var filePath = Path.Combine(AudioDir, fileName);
        await File.WriteAllBytesAsync(filePath, audioData);
        return filePath;
    }

    public void RevealInExplorer(string filePath)
    {
        if (!File.Exists(filePath)) return;

        if (OperatingSystem.IsWindows())
        {
            Process.Start("explorer.exe", $"/select,\"{filePath}\"");
        }
        else if (OperatingSystem.IsMacOS())
        {
            Process.Start("open", $"-R \"{filePath}\"");
        }
        else if (OperatingSystem.IsLinux())
        {
            Process.Start("xdg-open", $"\"{Path.GetDirectoryName(filePath)}\"");
        }
    }
}
