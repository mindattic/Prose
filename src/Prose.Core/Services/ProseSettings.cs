using MindAttic.Legion;
using MindAttic.Vault.Settings;
using MindAttic.Vault.Settings.Manifest;

namespace Prose.Core.Services;

/// <summary>
/// Prose's app-level settings in MindAttic Vault (<c>%APPDATA%\MindAttic\Prose\settings.json</c>), edited only in
/// MindAttic.Vault.Desktop (LAMP, PRS-T829). <see cref="SettingsService"/> reads and writes these through its
/// existing properties, so every consumer is unchanged; the first load imports today's values from the old
/// <c>%LOCALAPPDATA%\MindAttic\Prose\Settings.json</c>, which is left as it was (the rollback).
///
/// <para>Not here, by design: every API key, password and bearer token (they stay in Vault's named key lists or
/// in Settings.json as before), and the per-universe / per-library data that is content rather than app
/// configuration (per-universe export folders, voice profiles and the default voice profile, per-universe
/// <c>Setting</c> rows in the database).</para>
///
/// <para>Property names match the old Settings.json field names one for one, and the initializers are exactly
/// the old defaults: the import is a by-name copy and a fresh install seeds what Prose always used.</para>
/// </summary>
[SettingsApp(AppId, "Prose", Icon = "book", Description = "Worldbuilding and long-form writing: Hub, Writer, CLI and MCP.")]
[UsesCredential("claude", Required = true, Purpose = "Prose generation, reviews and discussion")]
[UsesCredential("openai", Purpose = "Fallback LLM, embeddings, dictation fallback, images")]
[UsesCredential("gemini", Purpose = "Fallback LLM and review judge")]
[UsesCredential("deepseek", Purpose = "Fallback LLM and review juries")]
[UsesCredential("mistral", Purpose = "Fallback LLM")]
[UsesCredential("kimi", Purpose = "Fallback LLM and review juries")]
[UsesCredential("perplexity", Purpose = "Fallback LLM")]
[UsesCredential("grok", Purpose = "Review juries")]
[UsesCredential("groq", Purpose = "Dictation and review juries")]
[UsesCredential("together", Purpose = "Review juries")]
[UsesCredential("openrouter", Purpose = "Review juries")]
[UsesCredential("fireworks", Purpose = "Review juries")]
[UsesCredential("cohere", Purpose = "Review juries")]
[UsesCredential("elevenlabs", Purpose = "Narration and read-aloud")]
[UsesCredential("ideogram", Purpose = "Image generation")]
[UsesCredential("fal", Purpose = "Image generation")]
[UsesCredential("stability", Purpose = "Image-to-video (BookTok)")]
[UsesCredential("kling", Purpose = "Image-to-video (BookTok)")]
[UsesCredential("runway", Purpose = "Image-to-video (BookTok)")]
[UsesCredential("here-maps", Purpose = "Maps in HTML export")]
[UsesCredential("google-maps", Purpose = "Maps in HTML export")]
public sealed class ProseSettings
{
    /// <summary>The Vault app id: <c>[SettingsApp]</c> id, roaming folder name and key-assignment id.</summary>
    public const string AppId = "Prose";

    // ── LLM models ────────────────────────────────────────────────────────────
    [Setting("Active provider", Group = "LLM models", Order = 1, Help = "Provider Prose calls first, e.g. claude, openai, gemini.")]
    public string ActiveLlmProvider { get; set; } = "claude";

    [Setting("Provider fallback chain", Group = "LLM models", Order = 2, Help = "Comma-separated, in priority order. When the active provider fails the next one is tried.")]
    public string ActiveLlmProviderChain { get; set; } = "claude,codex-cli,openai,gemini-cli,gemini,kimi,deepseek,mistral,perplexity";

    [Setting("Claude model", Group = "LLM models", Order = 3)]
    public string Model { get; set; } = Constants.Defaults.DefaultModel;

    [Setting("Max output tokens", Group = "LLM models", Order = 4)]
    [SettingRange(1, 1_000_000)]
    public int MaxTokens { get; set; } = 2048;

    [Setting("OpenAI model", Group = "LLM models", Order = 5)]
    public string OpenAiModel { get; set; } = "gpt-4.1-mini";

    [Setting("Gemini model", Group = "LLM models", Order = 6)]
    public string GeminiModel { get; set; } = "gemini-2.5-flash";

    [Setting("DeepSeek model", Group = "LLM models", Order = 7)]
    public string DeepSeekModel { get; set; } = "deepseek-chat";

    [Setting("Mistral model", Group = "LLM models", Order = 8)]
    public string MistralModel { get; set; } = "mistral-large-latest";

    [Setting("Kimi model", Group = "LLM models", Order = 9)]
    public string KimiModel { get; set; } = "kimi-k2.6";

    [Setting("Perplexity model", Group = "LLM models", Order = 10)]
    public string PerplexityModel { get; set; } = "sonar";

    [Setting("Grok model", Group = "LLM models", Order = 11)]
    public string GrokModel { get; set; } = "grok-3-mini";

    [Setting("Groq model", Group = "LLM models", Order = 12)]
    public string GroqModel { get; set; } = "llama-3.3-70b-versatile";

    [Setting("Together model", Group = "LLM models", Order = 13)]
    public string TogetherModel { get; set; } = "meta-llama/Llama-3.3-70B-Instruct-Turbo";

    [Setting("OpenRouter model", Group = "LLM models", Order = 14)]
    public string OpenRouterModel { get; set; } = "meta-llama/llama-3.3-70b-instruct";

    [Setting("Fireworks model", Group = "LLM models", Order = 15)]
    public string FireworksModel { get; set; } = "accounts/fireworks/models/llama-v3p3-70b-instruct";

    [Setting("Cohere model", Group = "LLM models", Order = 16)]
    public string CohereModel { get; set; } = "command-a-03-2025";

    // ── Review / jury ─────────────────────────────────────────────────────────
    [Setting("Ballots per sampled review", Group = "Review and jury", Order = 1, Help = "Cheap score-only ballots per sampled node review (--ballots).")]
    [SettingRange(1, 100_000)]
    public int ReviewBallots { get; set; } = 20;

    [Setting("Prose upgrades per run", Group = "Review and jury", Order = 2, Help = "Full prose upgrades per sampled run (--prose).")]
    [SettingRange(0, 100_000)]
    public int ReviewProse { get; set; } = 4;

    [Setting("Segment-study panel size", Group = "Review and jury", Order = 3, Help = "--panel / --study.")]
    [SettingRange(1, 100_000)]
    public int ReviewPanel { get; set; } = 128;

    [Setting("Readers per full run", Group = "Review and jury", Order = 4, Help = "Reader count for full / census runs (--readers).")]
    [SettingRange(1, 100_000)]
    public int ReviewReaders { get; set; } = 50;

    [Setting("Synopsis judge provider", Group = "Review and jury", Order = 5, Help = "Provider that writes the reader synopsis after a review run.")]
    public string ReviewJudgeProvider { get; set; } = "gemini";

    [Setting("Ballot providers", Group = "Review and jury", Order = 6, Help = "Comma-separated provider ids allowed to cast ballots.")]
    public string ReviewAllowedProviders { get; set; } = "claude";

    [Setting("Max concurrent review calls", Group = "Review and jury", Order = 7)]
    [SettingRange(1, 50)]
    public int ReviewMaxConcurrency { get; set; } = 10;

    [Setting("Review beats automatically on save", Group = "Review and jury", Order = 8)]
    public bool ReviewAutoRunEnabled { get; set; } = true;

    [Setting("Reader-QA jury providers", Group = "Review and jury", Order = 9, Help = "Comma-separated; cross-family diversity is the point. Keyless or dead providers are skipped.")]
    public string ReaderQaJuryProviders { get; set; } = "claude,openai,gemini,deepseek,kimi";

    [Setting("Extra jury providers (JSON)", Group = "Review and jury", Order = 10, Help = "OpenAI-compatible jury families outside the Legion catalog: [{\"id\",\"baseUrl\",\"cheapModel\",...}]. Keys come from Vault by id.")]
    [Multiline]
    public string ExtraJuryProvidersJson { get; set; } =
        "[{\"id\":\"kimi\",\"baseUrl\":\"https://api.moonshot.ai/v1\",\"cheapModel\":\"kimi-k2.6\",\"label\":\"Kimi K2.6\",\"inPerMtok\":0.60,\"cacheReadPerMtok\":0.16,\"outPerMtok\":2.50}]";

    [Setting("Comprehension probe model", Group = "Review and jury", Order = 11, Help = "Deliberately cheap: its genuine misreadings are the signal.")]
    public string ComprehensionProbeModel { get; set; } = LlmModels.Haiku;

    [Setting("Comprehension arbiter model", Group = "Review and jury", Order = 12)]
    public string ComprehensionArbiterModel { get; set; } = LlmModels.Sonnet;

    // ── Local LLM ─────────────────────────────────────────────────────────────
    [Setting("Local review endpoint", Group = "Local LLM", Order = 1, Help = "OpenAI-compatible chat-completions URL used by --local reviews.")]
    [UrlSetting]
    public string LocalReviewBaseUrl { get; set; } = "http://localhost:11434/v1/chat/completions";

    [Setting("Local review model", Group = "Local LLM", Order = 2)]
    public string LocalReviewModel { get; set; } = "qwen2.5-32b-rev-128k";

    [Setting("Local review label", Group = "Local LLM", Order = 3, Help = "Names the backend in report file names (vast, runpod). Empty = from the endpoint host.")]
    public string LocalReviewLabel { get; set; } = "";

    [Setting("Local review concurrency", Group = "Local LLM", Order = 4)]
    [SettingRange(1, 16)]
    public int LocalReviewMaxConcurrency { get; set; } = 2;

    [Setting("Local model context (tokens)", Group = "Local LLM", Order = 5, Help = "The local model's num_ctx; review segments are sized to fit.")]
    [SettingRange(4096, 10_000_000)]
    public int LocalReviewContextTokens { get; set; } = 131072;

    [Setting("Local generation endpoint", Group = "Local LLM", Order = 6, Help = "OpenAI-compatible endpoint for --local prose generation. Empty = disabled.")]
    [UrlSetting]
    public string LocalLlmBaseUrl { get; set; } = "";

    [Setting("Local generation model", Group = "Local LLM", Order = 7)]
    public string LocalLlmModel { get; set; } = "qwen2.5-32b";

    [Setting("Local embeddings endpoint", Group = "Local LLM", Order = 8, Help = "Full /v1/embeddings URL. When set, embeddings never go to OpenAI.")]
    [UrlSetting]
    public string LocalEmbeddingBaseUrl { get; set; } = "";

    [Setting("Local embeddings model", Group = "Local LLM", Order = 9)]
    public string LocalEmbeddingModel { get; set; } = "";

    // ── TTS / narration ───────────────────────────────────────────────────────
    [Setting("ElevenLabs voice id", Group = "TTS and narration", Order = 1, Help = "Used when no voice profile is the default.")]
    public string ElevenLabsVoiceId { get; set; } = "jfIS2w2yJi0grJZPyEsk";

    [Setting("Narrator voice name", Group = "TTS and narration", Order = 2)]
    public string NarratorVoiceName { get; set; } = "Oliver Silk - Deep Gravel Narrative";

    [Setting("TTS model", Group = "TTS and narration", Order = 3)]
    public string TtsModel { get; set; } = "eleven_multilingual_v2";

    [Setting("Stability", Group = "TTS and narration", Order = 4)]
    [SettingRange(0, 1)]
    public double TtsStability { get; set; } = 0.5;

    [Setting("Similarity boost", Group = "TTS and narration", Order = 5)]
    [SettingRange(0, 1)]
    public double TtsSimilarityBoost { get; set; } = 0.75;

    [Setting("Style", Group = "TTS and narration", Order = 6)]
    [SettingRange(0, 1)]
    public double TtsStyle { get; set; } = 0.0;

    [Setting("Audio tags (v3 models)", Group = "TTS and narration", Order = 7, Help = "Inject [whispering]-style tags from each beat's tone.")]
    public bool TtsUseAudioTags { get; set; } = true;

    [Setting("Audiobook format", Group = "TTS and narration", Order = 8)]
    [SettingChoices("mp3_320", "mp3_256", "mp3_192", "mp3_128", "wav", "flac")]
    public string AudiobookFormat { get; set; } = "mp3_320";

    [Setting("Pause after a section (ms)", Group = "TTS and narration", Order = 9)]
    [SettingRange(0, 60_000)]
    public int TtsPauseSectionMs { get; set; } = 1800;

    [Setting("Pause after a scene (ms)", Group = "TTS and narration", Order = 10)]
    [SettingRange(0, 60_000)]
    public int TtsPauseSceneMs { get; set; } = 1000;

    [Setting("Pause between paragraphs (ms)", Group = "TTS and narration", Order = 11)]
    [SettingRange(0, 60_000)]
    public int TtsPauseParagraphMs { get; set; } = 400;

    [Setting("Pause mid-sentence (ms)", Group = "TTS and narration", Order = 12)]
    [SettingRange(0, 60_000)]
    public int TtsPauseContinuationMs { get; set; } = 200;

    // ── Editor / appearance ───────────────────────────────────────────────────
    [Setting("Theme", Group = "Editor and appearance", Order = 1)]
    [SettingChoices("auto", "light", "dark")]
    public string Theme { get; set; } = "auto";

    [Setting("Font family", Group = "Editor and appearance", Order = 2)]
    public string FontFamily { get; set; } = "Outfit";

    [Setting("Editor font size", Group = "Editor and appearance", Order = 3)]
    [SettingRange(6, 72)]
    public int EditorFontSize { get; set; } = 14;

    [Setting("Auto-save interval (ms)", Group = "Editor and appearance", Order = 4)]
    [SettingRange(100, 3_600_000)]
    public int AutoSaveIntervalMs { get; set; } = 2000;

    [Setting("Timestamp format", Group = "Editor and appearance", Order = 5)]
    [SettingChoices("yyyy-MM-dd hh:mm:sstt", "yyyy-MM-dd hh:mmtt", "yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd HH:mm",
        "MM/dd/yyyy hh:mm:sstt", "MM/dd/yyyy HH:mm:ss", "dd MMM yyyy hh:mm:sstt", "dd MMM yyyy HH:mm:ss")]
    public string TimestampFormat { get; set; } = "yyyy-MM-dd hh:mm:sstt";

    [Setting("Time zone", Group = "Editor and appearance", Order = 6, Help = "Windows time zone id, e.g. Central Standard Time.")]
    public string TimezoneId { get; set; } = "Central Standard Time";

    [Setting("Repository list on the right", Group = "Editor and appearance", Order = 7)]
    public bool RepoListOnRight { get; set; } = true;

    // ── Writing pipeline ──────────────────────────────────────────────────────
    [Setting("Canon root folder", Group = "Writing pipeline", Order = 1, Help = "The Prose repo root holding engine\\data. Empty = auto-detect.")]
    [PathPicker(PathKind.Folder)]
    public string CanonRootPath { get; set; } = "";

    [Setting("Doc context in prompts", Group = "Writing pipeline", Order = 2, Help = "Inject the node bible, voice register and topic docs into every beat prompt.")]
    public bool DocContextEnabled { get; set; } = true;

    [Setting("Log the full DCM working set", Group = "Writing pipeline", Order = 3, Help = "For prose --dcm-viz.")]
    public bool DcmLoggingEnabled { get; set; } = false;

    [Setting("Plain-text named-entity recognition", Group = "Writing pipeline", Order = 4)]
    public bool EnablePlainTextNer { get; set; } = false;

    [Setting("Save stories as Markdown", Group = "Writing pipeline", Order = 5)]
    public bool SaveStoriesAsMarkdown { get; set; } = true;

    [Setting("Canon grounding after each beat", Group = "Writing pipeline", Order = 6, Help = "Flags provisional entities. Costs an LLM call per beat.")]
    public bool AutoCanonGrounding { get; set; } = false;

    [Setting("Harvest revealed details after each beat", Group = "Writing pipeline", Order = 7, Help = "Proposes XRAY-REVEAL findings. Costs an LLM call per beat.")]
    public bool AutoHarvestRevealedDetails { get; set; } = false;

    // ── Export / publish ──────────────────────────────────────────────────────
    [Setting("Audiobook output folder", Group = "Export and publish", Order = 1, Help = "Where Publish drops the combined audio. Empty = Desktop.")]
    [PathPicker(PathKind.Folder)]
    public string PublishOutputDirectory { get; set; } = "";

    [Setting("Manuscript export folder", Group = "Export and publish", Order = 2, Help = "Root for --export-node; per-universe subfolders are set with --export-dir. Empty = Desktop.")]
    [PathPicker(PathKind.Folder)]
    public string PublishExportDirectory { get; set; } = "";

    [Setting("Table of contents in .docx", Group = "Export and publish", Order = 3)]
    public bool DocxIncludeToc { get; set; } = false;

    // ── Maps ──────────────────────────────────────────────────────────────────
    [Setting("Map service", Group = "Maps", Order = 1)]
    [SettingChoices("google", "here")]
    public string MapService { get; set; } = "google";

    [Setting("HERE app id", Group = "Maps", Order = 2, Help = "An identifier, not a secret; the HERE key lives in Vault's keys. PROSE_MAP_APP_ID overrides it.")]
    public string MapAppId { get; set; } = "";

    [Setting("Map style", Group = "Maps", Order = 3)]
    public string MapMode { get; set; } = "dark";

    // ── SMTP (non-secret parts; the password stays out of Vault settings) ─────
    [Setting("SMTP host", Group = "Email (SMTP)", Order = 1, Help = "Password-reset mail. PROSE_SMTP_HOST overrides it.")]
    public string SmtpHost { get; set; } = "";

    [Setting("SMTP port", Group = "Email (SMTP)", Order = 2, Help = "PROSE_SMTP_PORT overrides it.")]
    [SettingRange(1, 65535)]
    public int SmtpPort { get; set; } = 587;

    [Setting("SMTP user name", Group = "Email (SMTP)", Order = 3, Help = "PROSE_SMTP_USERNAME overrides it.")]
    public string SmtpUsername { get; set; } = "";

    [Setting("From address", Group = "Email (SMTP)", Order = 4, Help = "PROSE_SMTP_FROM overrides it.")]
    public string SmtpFrom { get; set; } = "";

    [Setting("Use SSL", Group = "Email (SMTP)", Order = 5)]
    public bool SmtpEnableSsl { get; set; } = true;

    // ── Hub ───────────────────────────────────────────────────────────────────
    [Setting("Narrate commands in the Hub", Group = "Hub", Order = 1, Help = "Follow each command's log line with a plain-English gloss (prose --hub-narration on|off).")]
    public bool HubNarrationEnabled { get; set; } = false;

    [Setting("COROBS entity geometry", Group = "Hub", Order = 2, Help = "RFC 0016's whole new way (prose --corobs enable|disable, /corobs on|off). Off hides it everywhere and deletes nothing.")]
    public bool CorobsEnabled { get; set; } = false;

    /// <summary>The real Vault store. Tests and scratch storage pass their own store to <see cref="SettingsService"/>.</summary>
    public static JsonSettingsStore<ProseSettings> ForCurrentUser() => JsonSettingsStore<ProseSettings>.ForSettingsApp();
}
