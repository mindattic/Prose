using Prose.Core.Interfaces;

namespace Prose.Core.Services;

/// <summary>
/// Stores per-user 64×64 avatar images as base64 data URLs.
/// Avatars are written to {MutableDataDir}/profiles/{username}.txt
/// </summary>
public class ProfileService
{
    private readonly string profileDir;

    public event Action? AvatarUpdated;

    public ProfileService(IPathProvider paths)
    {
        profileDir = Directory.CreateDirectory(Path.Combine(paths.MutableDataDir, "profiles")).FullName;
    }

    public string? GetAvatar(string username)
    {
        if (string.IsNullOrWhiteSpace(username)) return null;
        var path = AvatarPath(username);
        return File.Exists(path) ? File.ReadAllText(path) : null;
    }

    public void SaveAvatar(string username, string dataUrl)
    {
        if (string.IsNullOrWhiteSpace(username)) return;
        File.WriteAllText(AvatarPath(username), dataUrl);
        AvatarUpdated?.Invoke();
    }

    private string AvatarPath(string username)
    {
        var safe = string.Concat(username.Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_'));
        return Path.Combine(profileDir, $"{safe}.txt");
    }
}
