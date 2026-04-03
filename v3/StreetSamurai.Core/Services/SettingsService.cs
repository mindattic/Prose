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

        // Auto-detect canon root if not set
        if (string.IsNullOrWhiteSpace(_data.CanonRootPath))
        {
            var detected = AutoDetectCanonRoot();
            if (detected != null)
            {
                _data.CanonRootPath = detected;
                Save();
            }
        }
    }

    private static string? AutoDetectCanonRoot()
    {
        // Walk up from the executing assembly to find the repo root
        var candidates = new[]
        {
            @"D:\Projects\MindAttic\StreetSamurai",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Projects", "MindAttic", "StreetSamurai"),
        };

        foreach (var path in candidates)
        {
            if (Directory.Exists(Path.Combine(path, "engine_data")) &&
                File.Exists(Path.Combine(path, "engine_data", "canon.json")))
                return path;
        }

        // Try walking up from current directory
        var dir = AppContext.BaseDirectory;
        for (int i = 0; i < 8; i++)
        {
            if (Directory.Exists(Path.Combine(dir, "worldbuilding")) &&
                Directory.Exists(Path.Combine(dir, "essences")))
                return dir;
            var parent = Directory.GetParent(dir);
            if (parent == null) break;
            dir = parent.FullName;
        }

        return null;
    }

    public string ApiKey { get => _data.ApiKey; set { _data.ApiKey = value; Save(); } }
    public string Model { get => _data.Model; set { _data.Model = value; Save(); } }
    public string Theme { get => _data.Theme; set { _data.Theme = value; Save(); } }
    public string CanonRootPath { get => _data.CanonRootPath; set { _data.CanonRootPath = value; Save(); } }
    public int MaxTokens { get => _data.MaxTokens; set { _data.MaxTokens = value; Save(); } }
    public string ElevenLabsApiKey { get => _data.ElevenLabsApiKey; set { _data.ElevenLabsApiKey = value; Save(); } }
    public string ElevenLabsVoiceId { get => _data.ElevenLabsVoiceId; set { _data.ElevenLabsVoiceId = value; Save(); } }
    public string NarratorVoiceName { get => _data.NarratorVoiceName; set { _data.NarratorVoiceName = value; Save(); } }
    public string TtsModel { get => _data.TtsModel; set { _data.TtsModel = value; Save(); } }
    public double TtsStability { get => _data.TtsStability; set { _data.TtsStability = value; Save(); } }
    public double TtsSimilarityBoost { get => _data.TtsSimilarityBoost; set { _data.TtsSimilarityBoost = value; Save(); } }
    public double TtsStyle { get => _data.TtsStyle; set { _data.TtsStyle = value; Save(); } }
    public string OpenAiApiKey { get => _data.OpenAiApiKey; set { _data.OpenAiApiKey = value; Save(); } }
    public string OpenAiModel { get => _data.OpenAiModel; set { _data.OpenAiModel = value; Save(); } }
    public string ActiveLlmProvider { get => _data.ActiveLlmProvider; set { _data.ActiveLlmProvider = value; Save(); } }
    public int EditorFontSize { get => _data.EditorFontSize; set { _data.EditorFontSize = value; Save(); } }
    public int AutoSaveIntervalMs { get => _data.AutoSaveIntervalMs; set { _data.AutoSaveIntervalMs = value; Save(); } }
    public string GeminiApiKey { get => _data.GeminiApiKey; set { _data.GeminiApiKey = value; Save(); } }
    public string DeepSeekApiKey { get => _data.DeepSeekApiKey; set { _data.DeepSeekApiKey = value; Save(); } }
    public string MistralApiKey { get => _data.MistralApiKey; set { _data.MistralApiKey = value; Save(); } }
    public string GrokApiKey { get => _data.GrokApiKey; set { _data.GrokApiKey = value; Save(); } }
    public string GroqApiKey { get => _data.GroqApiKey; set { _data.GroqApiKey = value; Save(); } }
    public string TogetherApiKey { get => _data.TogetherApiKey; set { _data.TogetherApiKey = value; Save(); } }
    public string OpenRouterApiKey { get => _data.OpenRouterApiKey; set { _data.OpenRouterApiKey = value; Save(); } }
    public string FireworksApiKey { get => _data.FireworksApiKey; set { _data.FireworksApiKey = value; Save(); } }
    public string CohereApiKey { get => _data.CohereApiKey; set { _data.CohereApiKey = value; Save(); } }
    public bool GhostWriterEnabled { get => _data.GhostWriterEnabled; set { _data.GhostWriterEnabled = value; Save(); } }
    public int GhostWriterScanIntervalSeconds { get => _data.GhostWriterScanIntervalSeconds; set { _data.GhostWriterScanIntervalSeconds = value; Save(); } }
    public int GhostWriterRateLimitDelayMs { get => _data.GhostWriterRateLimitDelayMs; set { _data.GhostWriterRateLimitDelayMs = value; Save(); } }
    public int GhostWriterMaxVoters { get => _data.GhostWriterMaxVoters; set { _data.GhostWriterMaxVoters = value; Save(); } }
    public int GhostWriterRescanMinutes { get => _data.GhostWriterRescanMinutes; set { _data.GhostWriterRescanMinutes = value; Save(); } }
    public double GhostWriterMajorityThreshold { get => _data.GhostWriterMajorityThreshold; set { _data.GhostWriterMajorityThreshold = value; Save(); } }

    /// <summary>Reset non-secret settings to defaults. Preserves API keys and canon root.</summary>
    public void ResetToDefaults()
    {
        var keys = new
        {
            _data.ApiKey, _data.OpenAiApiKey, _data.ElevenLabsApiKey,
            _data.ElevenLabsVoiceId, _data.NarratorVoiceName, _data.CanonRootPath,
        };
        _data = new SettingsData
        {
            ApiKey = keys.ApiKey,
            OpenAiApiKey = keys.OpenAiApiKey,
            ElevenLabsApiKey = keys.ElevenLabsApiKey,
            ElevenLabsVoiceId = keys.ElevenLabsVoiceId,
            NarratorVoiceName = keys.NarratorVoiceName,
            CanonRootPath = keys.CanonRootPath,
        };
        Save();
    }

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
        public string ElevenLabsVoiceId { get; set; } = "jfIS2w2yJi0grJZPyEsk";
        public string NarratorVoiceName { get; set; } = "Oliver Silk";
        public string TtsModel { get; set; } = "eleven_multilingual_v2";
        public double TtsStability { get; set; } = 0.5;
        public double TtsSimilarityBoost { get; set; } = 0.75;
        public double TtsStyle { get; set; } = 0.0;
        public string OpenAiApiKey { get; set; } = "";
        public string OpenAiModel { get; set; } = "gpt-4.1-mini";
        public string ActiveLlmProvider { get; set; } = "claude";
        public int EditorFontSize { get; set; } = 14;
        public int AutoSaveIntervalMs { get; set; } = 2000;
        public string GeminiApiKey { get; set; } = "";
        public string DeepSeekApiKey { get; set; } = "";
        public string MistralApiKey { get; set; } = "";
        public string GrokApiKey { get; set; } = "";
        public string GroqApiKey { get; set; } = "";
        public string TogetherApiKey { get; set; } = "";
        public string OpenRouterApiKey { get; set; } = "";
        public string FireworksApiKey { get; set; } = "";
        public string CohereApiKey { get; set; } = "";
        // GhostWriter settings
        public bool GhostWriterEnabled { get; set; } = true;
        public int GhostWriterScanIntervalSeconds { get; set; } = 120;
        public int GhostWriterRateLimitDelayMs { get; set; } = 65000;
        public int GhostWriterMaxVoters { get; set; } = 5;
        public int GhostWriterRescanMinutes { get; set; } = 30;
        public double GhostWriterMajorityThreshold { get; set; } = 0.67;
    }
}
