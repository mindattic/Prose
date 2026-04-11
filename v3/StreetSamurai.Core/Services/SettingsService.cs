using System.Text.Json;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

public class SettingsService : IDisposable
{
    private readonly string settingsPath;
    private readonly string defaultsPath;
    private SettingsData data = new();
    private Timer? saveTimer;
    private readonly object saveLock = new();

    public SettingsService() : this(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MindAttic", "StreetSamurai")) { }

    /// <summary>Constructor with explicit storage directory (for tests).</summary>
    public SettingsService(string storageDir)
    {
        Directory.CreateDirectory(storageDir);
        settingsPath = Path.Combine(storageDir, "Settings.json");
        defaultsPath = Path.Combine(storageDir, "Defaults.json");
        Load();

        // Auto-detect canon root if not set or current path has insufficient data
        var engineDir = string.IsNullOrWhiteSpace(data.CanonRootPath)
            ? ""
            : Path.Combine(data.CanonRootPath, Constants.Folders.Engine);
        var hasData = !string.IsNullOrWhiteSpace(engineDir)
            && Directory.Exists(engineDir)
            && Directory.EnumerateFiles(engineDir, "*.json", SearchOption.AllDirectories).Take(10).Count() >= 10;

        if (!hasData)
        {
            var detected = AutoDetectCanonRoot();
            if (detected != null)
            {
                data.CanonRootPath = detected;
                Flush();
            }
        }
    }

    private static string? AutoDetectCanonRoot()
    {
        // Azure App Service: set STREETSAMURAI_DATA_ROOT in Application Settings
        var envRoot = Environment.GetEnvironmentVariable("STREETSAMURAI_DATA_ROOT");
        if (!string.IsNullOrWhiteSpace(envRoot))
            return envRoot;

        // Walk up from the executing assembly to find the repo root
        var candidates = new[]
        {
            AppContext.BaseDirectory,   // Published app root — engine/data is co-deployed here on Azure
            @"D:\Projects\MindAttic\StreetSamurai",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Projects", "MindAttic", "StreetSamurai"),
        };

        foreach (var path in candidates)
        {
            var candidateDir = Path.Combine(path, Constants.Folders.Engine);
            if (Directory.Exists(candidateDir) &&
                Directory.EnumerateFiles(candidateDir, "*.json", SearchOption.AllDirectories).Take(10).Count() >= 10)
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

    // Reads env var first (Azure App Service Application Settings), falls back to Settings.json.
    // Set these in Azure portal: App Service → Configuration → Application Settings.
    private static string Env(string key, string fallback) =>
        Environment.GetEnvironmentVariable(key) is { Length: > 0 } v ? v : fallback;

    public string ApiKey { get => Env("SS_CLAUDE_API_KEY", data.ApiKey); set { data.ApiKey = value; ScheduleSave(); } }
    public string Model { get => data.Model; set { data.Model = value; ScheduleSave(); } }
    public string Theme { get => data.Theme; set { data.Theme = value; ScheduleSave(); } }
    public string CanonRootPath { get => data.CanonRootPath; set { data.CanonRootPath = value; ScheduleSave(); } }
    public int MaxTokens { get => data.MaxTokens; set { data.MaxTokens = value; ScheduleSave(); } }
    public string ElevenLabsApiKey { get => Env("SS_ELEVENLABS_API_KEY", data.ElevenLabsApiKey); set { data.ElevenLabsApiKey = value; ScheduleSave(); } }
    public string ElevenLabsVoiceId { get => data.ElevenLabsVoiceId; set { data.ElevenLabsVoiceId = value; ScheduleSave(); } }
    public string NarratorVoiceName { get => data.NarratorVoiceName; set { data.NarratorVoiceName = value; ScheduleSave(); } }
    public string TtsModel { get => data.TtsModel; set { data.TtsModel = value; ScheduleSave(); } }
    public double TtsStability { get => data.TtsStability; set { data.TtsStability = value; ScheduleSave(); } }
    public double TtsSimilarityBoost { get => data.TtsSimilarityBoost; set { data.TtsSimilarityBoost = value; ScheduleSave(); } }
    public double TtsStyle { get => data.TtsStyle; set { data.TtsStyle = value; ScheduleSave(); } }
    public string OpenAiApiKey { get => Env("SS_OPENAI_API_KEY", data.OpenAiApiKey); set { data.OpenAiApiKey = value; ScheduleSave(); } }
    public string OpenAiModel { get => data.OpenAiModel; set { data.OpenAiModel = value; ScheduleSave(); } }
    public string ActiveLlmProvider { get => data.ActiveLlmProvider; set { data.ActiveLlmProvider = value; ScheduleSave(); } }
    public int EditorFontSize { get => data.EditorFontSize; set { data.EditorFontSize = value; ScheduleSave(); } }
    public int AutoSaveIntervalMs { get => data.AutoSaveIntervalMs; set { data.AutoSaveIntervalMs = value; ScheduleSave(); } }
    public string GeminiApiKey { get => Env("SS_GEMINI_API_KEY", data.GeminiApiKey); set { data.GeminiApiKey = value; ScheduleSave(); } }
    public string DeepSeekApiKey { get => Env("SS_DEEPSEEK_API_KEY", data.DeepSeekApiKey); set { data.DeepSeekApiKey = value; ScheduleSave(); } }
    public string MistralApiKey { get => Env("SS_MISTRAL_API_KEY", data.MistralApiKey); set { data.MistralApiKey = value; ScheduleSave(); } }
    public string GrokApiKey { get => Env("SS_GROK_API_KEY", data.GrokApiKey); set { data.GrokApiKey = value; ScheduleSave(); } }
    public string GroqApiKey { get => Env("SS_GROQ_API_KEY", data.GroqApiKey); set { data.GroqApiKey = value; ScheduleSave(); } }
    public string TogetherApiKey { get => Env("SS_TOGETHER_API_KEY", data.TogetherApiKey); set { data.TogetherApiKey = value; ScheduleSave(); } }
    public string OpenRouterApiKey { get => Env("SS_OPENROUTER_API_KEY", data.OpenRouterApiKey); set { data.OpenRouterApiKey = value; ScheduleSave(); } }
    public string FireworksApiKey { get => Env("SS_FIREWORKS_API_KEY", data.FireworksApiKey); set { data.FireworksApiKey = value; ScheduleSave(); } }
    public string CohereApiKey { get => Env("SS_COHERE_API_KEY", data.CohereApiKey); set { data.CohereApiKey = value; ScheduleSave(); } }
    public string GeminiModel { get => data.GeminiModel; set { data.GeminiModel = value; ScheduleSave(); } }
    public string DeepSeekModel { get => data.DeepSeekModel; set { data.DeepSeekModel = value; ScheduleSave(); } }
    public string MistralModel { get => data.MistralModel; set { data.MistralModel = value; ScheduleSave(); } }
    public string GrokModel { get => data.GrokModel; set { data.GrokModel = value; ScheduleSave(); } }
    public string GroqModel { get => data.GroqModel; set { data.GroqModel = value; ScheduleSave(); } }
    public string TogetherModel { get => data.TogetherModel; set { data.TogetherModel = value; ScheduleSave(); } }
    public string OpenRouterModel { get => data.OpenRouterModel; set { data.OpenRouterModel = value; ScheduleSave(); } }
    public string FireworksModel { get => data.FireworksModel; set { data.FireworksModel = value; ScheduleSave(); } }
    public string CohereModel { get => data.CohereModel; set { data.CohereModel = value; ScheduleSave(); } }
    public string MapService { get => data.MapService; set { data.MapService = value; ScheduleSave(); } }
    public string MapAppId { get => Env("SS_MAP_APP_ID", data.MapAppId); set { data.MapAppId = value; ScheduleSave(); } }
    public string MapApiKey { get => Env("SS_MAP_API_KEY", data.MapApiKey); set { data.MapApiKey = value; ScheduleSave(); } }
    public string GoogleMapsApiKey { get => Env("SS_GOOGLE_MAPS_API_KEY", data.GoogleMapsApiKey); set { data.GoogleMapsApiKey = value; ScheduleSave(); } }
    public string MapMode { get => data.MapMode; set { data.MapMode = value; ScheduleSave(); } }
    public string TimestampFormat { get => data.TimestampFormat; set { data.TimestampFormat = value; ScheduleSave(); } }
    public string TimezoneId { get => data.TimezoneId; set { data.TimezoneId = value; ScheduleSave(); } }
    public string FontFamily { get => data.FontFamily; set { data.FontFamily = value; ScheduleSave(); } }

    // SMTP — outbound email for password reset codes
    public string SmtpHost { get => Env("SS_SMTP_HOST", data.SmtpHost); set { data.SmtpHost = value; ScheduleSave(); } }
    public int SmtpPort { get => int.TryParse(Env("SS_SMTP_PORT", ""), out var p) ? p : data.SmtpPort; set { data.SmtpPort = value; ScheduleSave(); } }
    public string SmtpUsername { get => Env("SS_SMTP_USERNAME", data.SmtpUsername); set { data.SmtpUsername = value; ScheduleSave(); } }
    public string SmtpPassword { get => Env("SS_SMTP_PASSWORD", data.SmtpPassword); set { data.SmtpPassword = value; ScheduleSave(); } }
    public string SmtpFrom { get => Env("SS_SMTP_FROM", data.SmtpFrom); set { data.SmtpFrom = value; ScheduleSave(); } }
    public bool SmtpEnableSsl { get => data.SmtpEnableSsl; set { data.SmtpEnableSsl = value; ScheduleSave(); } }

    // FTP Publishing — disabled, deploying via Azure CI/CD
    // public string FtpHost { get => data.FtpHost; set { data.FtpHost = value; ScheduleSave(); } }
    // public int FtpPort { get => data.FtpPort; set { data.FtpPort = value; ScheduleSave(); } }
    // public string FtpUsername { get => data.FtpUsername; set { data.FtpUsername = value; ScheduleSave(); } }
    // public string FtpPassword { get => data.FtpPassword; set { data.FtpPassword = value; ScheduleSave(); } }
    // public string FtpRemotePath { get => data.FtpRemotePath; set { data.FtpRemotePath = value; ScheduleSave(); } }
    // public bool FtpUseSsl { get => data.FtpUseSsl; set { data.FtpUseSsl = value; ScheduleSave(); } }
    // public bool FtpPassive { get => data.FtpPassive; set { data.FtpPassive = value; ScheduleSave(); } }

    /// <summary>All supported timestamp formats, keyed by .NET format string with example display values.</summary>
    public static readonly (string Format, string Example)[] TimestampFormats =
    [
        ("yyyy-MM-dd hh:mm:sstt",   "2026-04-05 02:01:23PM"),
        ("yyyy-MM-dd hh:mmtt",      "2026-04-05 02:01PM"),
        ("yyyy-MM-dd HH:mm:ss",     "2026-04-05 14:01:23"),
        ("yyyy-MM-dd HH:mm",        "2026-04-05 14:01"),
        ("MM/dd/yyyy hh:mm:sstt",   "04/05/2026 02:01:23PM"),
        ("MM/dd/yyyy HH:mm:ss",     "04/05/2026 14:01:23"),
        ("dd MMM yyyy hh:mm:sstt",  "05 Apr 2026 02:01:23PM"),
        ("dd MMM yyyy HH:mm:ss",    "05 Apr 2026 14:01:23"),
    ];

    /// <summary>Formats a UTC or local DateTime according to the user's configured timestamp format and timezone.</summary>
    public string FormatTimestamp(DateTime timestamp)
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById(TimezoneId);
        var converted = TimeZoneInfo.ConvertTime(timestamp, tz);
        return converted.ToString(TimestampFormat);
    }

    /// <summary>Snapshot current settings as the default baseline for future resets.</summary>
    public void SaveAsDefaults()
    {
        var json = JsonSerializer.Serialize(data, JsonDefaults.Indented);
        File.WriteAllText(defaultsPath, json);
    }

    /// <summary>Reset all settings to the saved defaults snapshot (includes secrets).</summary>
    public void ResetToDefaults()
    {
        if (File.Exists(defaultsPath))
        {
            var json = File.ReadAllText(defaultsPath);
            data = JsonSerializer.Deserialize<SettingsData>(json) ?? new();
        }
        else
        {
            data = new SettingsData();
        }
        Flush();
    }

    private void Load()
    {
        if (File.Exists(settingsPath))
        {
            var json = File.ReadAllText(settingsPath);
            data = JsonSerializer.Deserialize<SettingsData>(json) ?? new();
        }
    }

    private void ScheduleSave()
    {
        lock (saveLock)
        {
            saveTimer?.Dispose();
            saveTimer = new Timer(_ => Flush(), null, 500, Timeout.Infinite);
        }
    }

    /// <summary>Immediately write pending settings to disk.</summary>
    public void Flush()
    {
        lock (saveLock)
        {
            saveTimer?.Dispose();
            saveTimer = null;
            var json = JsonSerializer.Serialize(data, JsonDefaults.Indented);
            File.WriteAllText(settingsPath, json);
        }
    }

    public void Dispose()
    {
        Flush();
        GC.SuppressFinalize(this);
    }

    private class SettingsData
    {
        public string ApiKey { get; set; } = "";
        public string Model { get; set; } = Constants.Defaults.DefaultModel;
        public string Theme { get; set; } = "dark";
        public string CanonRootPath { get; set; } = "";
        public int MaxTokens { get; set; } = 2048;
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
        public string GeminiModel { get; set; } = "gemini-2.5-flash";
        public string DeepSeekModel { get; set; } = "deepseek-chat";
        public string MistralModel { get; set; } = "mistral-large-latest";
        public string GrokModel { get; set; } = "grok-3-mini";
        public string GroqModel { get; set; } = "llama-3.3-70b-versatile";
        public string TogetherModel { get; set; } = "meta-llama/Llama-3.3-70B-Instruct-Turbo";
        public string OpenRouterModel { get; set; } = "meta-llama/llama-3.3-70b-instruct";
        public string FireworksModel { get; set; } = "accounts/fireworks/models/llama-v3p3-70b-instruct";
        public string CohereModel { get; set; } = "command-a-03-2025";
        public string MapService { get; set; } = "google";
        public string MapAppId { get; set; } = "";
        public string MapApiKey { get; set; } = "";
        public string GoogleMapsApiKey { get; set; } = "";
        public string MapMode { get; set; } = "dark";
        public string TimestampFormat { get; set; } = "yyyy-MM-dd hh:mm:sstt";
        public string TimezoneId { get; set; } = "Central Standard Time";
        public string FontFamily { get; set; } = "Outfit";
        public string SmtpHost { get; set; } = "";
        public int SmtpPort { get; set; } = 587;
        public string SmtpUsername { get; set; } = "";
        public string SmtpPassword { get; set; } = "";
        public string SmtpFrom { get; set; } = "";
        public bool SmtpEnableSsl { get; set; } = true;
        public string FtpHost { get; set; } = "";
        public int FtpPort { get; set; } = 21;
        public string FtpUsername { get; set; } = "";
        public string FtpPassword { get; set; } = "";
        public string FtpRemotePath { get; set; } = "";
        public bool FtpUseSsl { get; set; } = true;
        public bool FtpPassive { get; set; } = true;
    }
}
