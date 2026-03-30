namespace StreetSamurai.Core.Interfaces;

/// <summary>
/// Saves audio files to disk and reveals them in the file explorer.
/// </summary>
public interface IAudioFileService
{
    /// <summary>
    /// Saves audio bytes to disk and returns the full file path.
    /// </summary>
    Task<string> SaveAudioAsync(byte[] audioData, string? fileName = null);

    /// <summary>
    /// Opens the file explorer with the given file selected.
    /// </summary>
    void RevealInExplorer(string filePath);
}
