namespace StreetSamurai.Shared.Services;

/// <summary>
/// Canonical error codes for the app. Format: CATEGORY-###
/// Use these with ToastNotifier so every toast leaves a grep-able
/// [SS CATEGORY-###] line in the browser console.
/// </summary>
public static class AppError
{
    // ── Configuration ─────────────────────────────────────────────────────────
    public const string ConfMissingEnvVar    = "CONF-001";
    public const string ConfInvalidSetting   = "CONF-002";
    public const string ConfCanonRootMissing = "CONF-003";

    // ── Authentication ────────────────────────────────────────────────────────
    public const string AuthLoginFailed      = "AUTH-001";
    public const string AuthUnauthorized     = "AUTH-002";
    public const string AuthSessionExpired   = "AUTH-003";
    public const string AuthPasswordChange   = "AUTH-004";

    // ── LLM ──────────────────────────────────────────────────────────────────
    public const string LlmApiFailure        = "LLM-001";
    public const string LlmTimeout           = "LLM-002";
    public const string LlmProviderBad       = "LLM-003";
    public const string LlmQuotaExceeded     = "LLM-004";

    // ── TTS ──────────────────────────────────────────────────────────────────
    public const string TtsApiFailure        = "TTS-001";
    public const string TtsVoiceUnavailable  = "TTS-002";
    public const string TtsSynthesisFailed   = "TTS-003";

    // ── Story ─────────────────────────────────────────────────────────────────
    public const string StorySaveFailed      = "STORY-001";
    public const string StoryLoadFailed      = "STORY-002";
    public const string StoryGenerateFailed  = "STORY-003";
    public const string StoryExportFailed    = "STORY-004";

    // ── Graph ─────────────────────────────────────────────────────────────────
    public const string GraphLoadFailed      = "GRAPH-001";
    public const string GraphRebuildFailed   = "GRAPH-002";
    public const string GraphSearchFailed    = "GRAPH-003";

    // ── Repository / Data ─────────────────────────────────────────────────────
    public const string RepoReadFailed       = "REPO-001";
    public const string RepoWriteFailed      = "REPO-002";
    public const string RepoDeleteFailed     = "REPO-003";

    // ── Export ────────────────────────────────────────────────────────────────
    public const string ExportFailed         = "EXPORT-001";
    public const string ExportHtmlFailed     = "EXPORT-002";

    // ── Audio ─────────────────────────────────────────────────────────────────
    public const string AudioLoadFailed      = "AUDIO-001";
    public const string AudioSaveFailed      = "AUDIO-002";
    public const string AudioPlayFailed      = "AUDIO-003";
}
