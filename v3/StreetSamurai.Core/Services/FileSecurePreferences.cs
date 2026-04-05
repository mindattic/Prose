using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

/// <summary>
/// File-based secure preferences using AES encryption with a machine-derived key.
/// Stores encrypted credentials separately from settings.
/// </summary>
public class FileSecurePreferences : ISecurePreferences
{
    private readonly string filePath;
    private readonly byte[] key;
    private Dictionary<string, string> _cache = new();

    public FileSecurePreferences()
    {
        var appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MindAttic", "StreetSamurai");
        Directory.CreateDirectory(appData);
        filePath = Path.Combine(appData, "secure.dat");

        // Derive key from machine name + user — not cryptographically perfect but
        // ensures credentials aren't trivially portable between machines
        var seed = $"{Environment.MachineName}:{Environment.UserName}:StreetSamurai";
        key = SHA256.HashData(Encoding.UTF8.GetBytes(seed));

        Load();
    }

    public Task<string> GetAsync(string key)
    {
        _cache.TryGetValue(key, out var value);
        return Task.FromResult(value ?? "");
    }

    public Task SetAsync(string key, string value)
    {
        if (string.IsNullOrEmpty(value))
            _cache.Remove(key);
        else
            _cache[key] = value;
        Save();
        return Task.CompletedTask;
    }

    private void Load()
    {
        if (!File.Exists(filePath)) return;
        try
        {
            var allBytes = File.ReadAllBytes(filePath);
            if (allBytes.Length < 17) return; // IV (16) + at least 1 byte

            var iv = allBytes[..16];
            var encrypted = allBytes[16..];

            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;

            var decrypted = aes.DecryptCbc(encrypted, iv);
            var json = Encoding.UTF8.GetString(decrypted).TrimEnd('\0');
            _cache = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "Failed to decrypt secure preferences, resetting cache");
            _cache = new();
        }
    }

    private void Save()
    {
        var json = JsonSerializer.Serialize(_cache);
        var plaintext = Encoding.UTF8.GetBytes(json);

        using var aes = Aes.Create();
        aes.Key = key;
        aes.GenerateIV();

        var encrypted = aes.EncryptCbc(plaintext, aes.IV);

        // Write IV + ciphertext
        using var fs = File.Create(filePath);
        fs.Write(aes.IV);
        fs.Write(encrypted);
    }
}
