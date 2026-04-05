using System.Text.Json;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

public class SettingsService
{
    private readonly string settingsPath;
    private SettingsData data = new();

    public SettingsService()
    {
        var appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MindAttic", "StreetSamurai");
        Directory.CreateDirectory(appData);
        settingsPath = Path.Combine(appData, "Settings.json");
        Load();

        // Auto-detect canon root if not set
        if (string.IsNullOrWhiteSpace(data.CanonRootPath))
        {
            var detected = AutoDetectCanonRoot();
            if (detected != null)
            {
                data.CanonRootPath = detected;
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

    public string ApiKey { get => data.ApiKey; set { data.ApiKey = value; Save(); } }
    public string Model { get => data.Model; set { data.Model = value; Save(); } }
    public string Theme { get => data.Theme; set { data.Theme = value; Save(); } }
    public string CanonRootPath { get => data.CanonRootPath; set { data.CanonRootPath = value; Save(); } }
    public int MaxTokens { get => data.MaxTokens; set { data.MaxTokens = value; Save(); } }
    public string ElevenLabsApiKey { get => data.ElevenLabsApiKey; set { data.ElevenLabsApiKey = value; Save(); } }
    public string ElevenLabsVoiceId { get => data.ElevenLabsVoiceId; set { data.ElevenLabsVoiceId = value; Save(); } }
    public string NarratorVoiceName { get => data.NarratorVoiceName; set { data.NarratorVoiceName = value; Save(); } }
    public string TtsModel { get => data.TtsModel; set { data.TtsModel = value; Save(); } }
    public double TtsStability { get => data.TtsStability; set { data.TtsStability = value; Save(); } }
    public double TtsSimilarityBoost { get => data.TtsSimilarityBoost; set { data.TtsSimilarityBoost = value; Save(); } }
    public double TtsStyle { get => data.TtsStyle; set { data.TtsStyle = value; Save(); } }
    public string OpenAiApiKey { get => data.OpenAiApiKey; set { data.OpenAiApiKey = value; Save(); } }
    public string OpenAiModel { get => data.OpenAiModel; set { data.OpenAiModel = value; Save(); } }
    public string ActiveLlmProvider { get => data.ActiveLlmProvider; set { data.ActiveLlmProvider = value; Save(); } }
    public int EditorFontSize { get => data.EditorFontSize; set { data.EditorFontSize = value; Save(); } }
    public int AutoSaveIntervalMs { get => data.AutoSaveIntervalMs; set { data.AutoSaveIntervalMs = value; Save(); } }
    public string GeminiApiKey { get => data.GeminiApiKey; set { data.GeminiApiKey = value; Save(); } }
    public string DeepSeekApiKey { get => data.DeepSeekApiKey; set { data.DeepSeekApiKey = value; Save(); } }
    public string MistralApiKey { get => data.MistralApiKey; set { data.MistralApiKey = value; Save(); } }
    public string GrokApiKey { get => data.GrokApiKey; set { data.GrokApiKey = value; Save(); } }
    public string GroqApiKey { get => data.GroqApiKey; set { data.GroqApiKey = value; Save(); } }
    public string TogetherApiKey { get => data.TogetherApiKey; set { data.TogetherApiKey = value; Save(); } }
    public string OpenRouterApiKey { get => data.OpenRouterApiKey; set { data.OpenRouterApiKey = value; Save(); } }
    public string FireworksApiKey { get => data.FireworksApiKey; set { data.FireworksApiKey = value; Save(); } }
    public string CohereApiKey { get => data.CohereApiKey; set { data.CohereApiKey = value; Save(); } }
    public string GeminiModel { get => data.GeminiModel; set { data.GeminiModel = value; Save(); } }
    public string DeepSeekModel { get => data.DeepSeekModel; set { data.DeepSeekModel = value; Save(); } }
    public string MistralModel { get => data.MistralModel; set { data.MistralModel = value; Save(); } }
    public string GrokModel { get => data.GrokModel; set { data.GrokModel = value; Save(); } }
    public string GroqModel { get => data.GroqModel; set { data.GroqModel = value; Save(); } }
    public string TogetherModel { get => data.TogetherModel; set { data.TogetherModel = value; Save(); } }
    public string OpenRouterModel { get => data.OpenRouterModel; set { data.OpenRouterModel = value; Save(); } }
    public string FireworksModel { get => data.FireworksModel; set { data.FireworksModel = value; Save(); } }
    public string CohereModel { get => data.CohereModel; set { data.CohereModel = value; Save(); } }
    public string MapService { get => data.MapService; set { data.MapService = value; Save(); } }
    public string MapAppId { get => data.MapAppId; set { data.MapAppId = value; Save(); } }
    public string MapApiKey { get => data.MapApiKey; set { data.MapApiKey = value; Save(); } }

    /// <summary>Reset non-secret settings to defaults. Preserves API keys and canon root.</summary>
    public void ResetToDefaults()
    {
        var keys = new
        {
            data.ApiKey, data.OpenAiApiKey, data.ElevenLabsApiKey,
            data.ElevenLabsVoiceId, data.NarratorVoiceName, data.CanonRootPath,
            data.GeminiApiKey, data.DeepSeekApiKey, data.MistralApiKey,
            data.GrokApiKey, data.GroqApiKey, data.TogetherApiKey,
            data.OpenRouterApiKey, data.FireworksApiKey, data.CohereApiKey,
        };
        data = new SettingsData
        {
            ApiKey = keys.ApiKey,
            OpenAiApiKey = keys.OpenAiApiKey,
            ElevenLabsApiKey = keys.ElevenLabsApiKey,
            ElevenLabsVoiceId = keys.ElevenLabsVoiceId,
            NarratorVoiceName = keys.NarratorVoiceName,
            CanonRootPath = keys.CanonRootPath,
            GeminiApiKey = keys.GeminiApiKey,
            DeepSeekApiKey = keys.DeepSeekApiKey,
            MistralApiKey = keys.MistralApiKey,
            GrokApiKey = keys.GrokApiKey,
            GroqApiKey = keys.GroqApiKey,
            TogetherApiKey = keys.TogetherApiKey,
            OpenRouterApiKey = keys.OpenRouterApiKey,
            FireworksApiKey = keys.FireworksApiKey,
            CohereApiKey = keys.CohereApiKey,
        };
        Save();
    }

    private void Load()
    {
        if (File.Exists(settingsPath))
        {
            var json = File.ReadAllText(settingsPath);
            data = JsonSerializer.Deserialize<SettingsData>(json) ?? new();
        }
    }

    private void Save()
    {
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(settingsPath, json);
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
        public string NarratorVoiceName { get; set; } = "Oliver Silk - Deep Gravel Narrative";
        public string TtsModel { get; set; } = "eleven_v3";
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
        // Model selections per provider
        public string GeminiModel { get; set; } = "gemini-2.5-flash";
        public string DeepSeekModel { get; set; } = "deepseek-chat";
        public string MistralModel { get; set; } = "mistral-large-latest";
        public string GrokModel { get; set; } = "grok-3-mini";
        public string GroqModel { get; set; } = "llama-3.3-70b-versatile";
        public string TogetherModel { get; set; } = "meta-llama/Llama-3.3-70B-Instruct-Turbo";
        public string OpenRouterModel { get; set; } = "anthropic/claude-sonnet-4";
        public string FireworksModel { get; set; } = "accounts/fireworks/models/llama-v3p3-70b-instruct";
        public string CohereModel { get; set; } = "command-r-plus";
        public string MapService { get; set; } = "here";
        public string MapAppId { get; set; } = "rI9gpj49oW5SGZ8EsAe9";
        public string MapApiKey { get; set; } = "CIPFwnEI3bF6whfMT-1yL0kFa6wq1G9v8cBudCXdLE0";
    }
}
