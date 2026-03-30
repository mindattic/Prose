using System.Text.Json;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

public class SettingsService
{
    private readonly string _settingsPath;
    private SettingsData _data = new();

    public SettingsService()
    {
        var appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MindAttic", "StreetSamurai");
        Directory.CreateDirectory(appData);
        _settingsPath = Path.Combine(appData, "Settings.json");
        Load();
    }

    public string ApiKey { get => _data.ApiKey; set { _data.ApiKey = value; Save(); } }
    public string Model { get => _data.Model; set { _data.Model = value; Save(); } }
    public string Theme { get => _data.Theme; set { _data.Theme = value; Save(); } }
    public string CanonRootPath { get => _data.CanonRootPath; set { _data.CanonRootPath = value; Save(); } }
    public int MaxTokens { get => _data.MaxTokens; set { _data.MaxTokens = value; Save(); } }
    public string ElevenLabsApiKey { get => _data.ElevenLabsApiKey; set { _data.ElevenLabsApiKey = value; Save(); } }
    public string ElevenLabsVoiceId { get => _data.ElevenLabsVoiceId; set { _data.ElevenLabsVoiceId = value; Save(); } }
    public int EditorFontSize { get => _data.EditorFontSize; set { _data.EditorFontSize = value; Save(); } }
    public int AutoSaveIntervalMs { get => _data.AutoSaveIntervalMs; set { _data.AutoSaveIntervalMs = value; Save(); } }

    private void Load()
    {
        if (File.Exists(_settingsPath))
        {
            var json = File.ReadAllText(_settingsPath);
            _data = JsonSerializer.Deserialize<SettingsData>(json) ?? new();
        }
    }

    private void Save()
    {
        var json = JsonSerializer.Serialize(_data, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_settingsPath, json);
    }

    private class SettingsData
    {
        public string ApiKey { get; set; } = "";
        public string Model { get; set; } = "claude-sonnet-4-6";
        public string Theme { get; set; } = "dark";
        public string CanonRootPath { get; set; } = "";
        public int MaxTokens { get; set; } = 4096;
        public string ElevenLabsApiKey { get; set; } = "";
        public string ElevenLabsVoiceId { get; set; } = "";
        public int EditorFontSize { get; set; } = 14;
        public int AutoSaveIntervalMs { get; set; } = 2000;
    }
}
