using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using MindAttic.Legion;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

public class SettingsService : IDisposable
{
    /// <summary>
    /// Optional cloud-native configuration source. When set (typically once at host
    /// startup via <c>SettingsService.VaultConfiguration = builder.Configuration</c>),
    /// every <see cref="ResolveApiKey"/> call checks
    /// <c>MindAttic:Vault:LLM:&lt;providerId&gt;:apiKey</c> in <see cref="IConfiguration"/>
    /// before consulting env vars or the file store. That layer covers User Secrets in
    /// dev, App Service Application Settings in prod, and Azure Key Vault references —
    /// all without changing this class's public surface or requiring DI plumbing.
    /// </summary>
    public static IConfiguration? VaultConfiguration { get; set; }

    private readonly string settingsPath;
    private readonly string defaultsPath;
    private SettingsData data = new();
    private Timer? saveTimer;
    private readonly object saveLock = new();
    // Snapshot of the settings as this process last saw them persisted (set at Load and after each
    // Flush). Flush() diffs the current in-memory state against this to write ONLY the fields this
    // process changed — so a stale copy can't clobber fields other processes wrote. See Flush().
    private JsonObject? baseline;

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
        MigrateLegacyCredentialsToSharedStore();

        // Auto-detect canon root if not set or current path has insufficient data.
        // Post-archival, "valid canon" means the engine dir exists and has the
        // standard subfolders — entity content lives in SQL now, not on disk,
        // so the old "≥ 10 .json files" heuristic no longer applies.
        var engineDir = string.IsNullOrWhiteSpace(data.CanonRootPath)
            ? ""
            : Path.Combine(data.CanonRootPath, Constants.Folders.Engine);
        var hasData = !string.IsNullOrWhiteSpace(engineDir)
            && Directory.Exists(engineDir)
            && Directory.Exists(Path.Combine(engineDir, "data"));

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
                Directory.Exists(Path.Combine(candidateDir, "data")))
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

    // Credential resolution: VaultConfiguration → env var → shared %APPDATA%/MindAttic/LLM/
    // store → legacy Settings.json. VaultConfiguration is the cloud-native primary; when
    // unset (e.g. in unit tests that construct SettingsService directly), the chain
    // falls back to the prior env-var-first behaviour with no observable difference.
    // Override the store location with the MINDATTIC_LLM_CREDENTIALS env var.
    private static string ResolveApiKey(string envVar, string providerId, string legacyValue)
    {
        var fromConfig = VaultConfiguration?[$"MindAttic:Vault:LLM:{providerId}:apiKey"];
        if (!string.IsNullOrWhiteSpace(fromConfig)) return fromConfig.Trim();

        if (Environment.GetEnvironmentVariable(envVar) is { Length: > 0 } v) return v;
        var fromStore = MindAtticCredentialStore.GetKey(providerId);
        return !string.IsNullOrEmpty(fromStore) ? fromStore : legacyValue;
    }

    // API keys route through MindAtticCredentialStore (LLMVoting library) at %APPDATA%/MindAttic/LLM/.
    // Resolution: env var → shared store → legacy Settings.json field.
    // Models, voice prefs, and other non-credential settings stay in Settings.json (per-app).
    public string ApiKey
    {
        get => ResolveApiKey("SS_CLAUDE_API_KEY", "claude-api", data.ApiKey);
        set { MindAtticCredentialStore.SetKey("claude-api", value); data.ApiKey = value; ScheduleSave(); }
    }
    public string Model { get => data.Model; set { data.Model = value; ScheduleSave(); } }
    /// <summary>Raised when the theme changes so layout components can update without a full reload.</summary>
    public event Action<string>? ThemeChanged;
    public string Theme { get => data.Theme; set { data.Theme = value; ScheduleSave(); foreach (var h in ThemeChanged?.GetInvocationList() ?? []) try { ((Action<string>)h)(value); } catch { } } }
    public string CanonRootPath { get => data.CanonRootPath; set { data.CanonRootPath = value; ScheduleSave(); } }
    public int MaxTokens { get => data.MaxTokens; set { data.MaxTokens = value; ScheduleSave(); } }
    public string ElevenLabsApiKey
    {
        get => ResolveApiKey("SS_ELEVENLABS_API_KEY", "elevenlabs", data.ElevenLabsApiKey);
        set { MindAtticCredentialStore.SetKey("elevenlabs", value); data.ElevenLabsApiKey = value; ScheduleSave(); }
    }
    public string ElevenLabsVoiceId { get => data.ElevenLabsVoiceId; set { data.ElevenLabsVoiceId = value; ScheduleSave(); } }
    public string NarratorVoiceName { get => data.NarratorVoiceName; set { data.NarratorVoiceName = value; ScheduleSave(); } }
    public string TtsModel { get => data.TtsModel; set { data.TtsModel = value; ScheduleSave(); } }
    public double TtsStability { get => data.TtsStability; set { data.TtsStability = value; ScheduleSave(); } }
    public double TtsSimilarityBoost { get => data.TtsSimilarityBoost; set { data.TtsSimilarityBoost = value; ScheduleSave(); } }
    public double TtsStyle { get => data.TtsStyle; set { data.TtsStyle = value; ScheduleSave(); } }

    /// <summary>Final delivery format/quality for a published audiobook. The
    /// source is always fetched at the highest fidelity the ElevenLabs tier
    /// allows (lossless <c>pcm_44100</c> when available); this controls only how
    /// the combined track is encoded for the file the user receives. One of the
    /// keys in <see cref="AudiobookFormats"/>. Default: 320 kbps MP3.</summary>
    public string AudiobookFormat
    {
        get => string.IsNullOrWhiteSpace(data.AudiobookFormat) ? "mp3_320" : data.AudiobookFormat;
        set { data.AudiobookFormat = value; ScheduleSave(); }
    }

    /// <summary>Where Publish drops the combined audio file so it's easy to
    /// find. Empty = Desktop. The in-app player still serves an internal copy;
    /// this is the user-facing export.</summary>
    public string PublishOutputDirectory
    {
        get => data.PublishOutputDirectory;
        set { data.PublishOutputDirectory = value ?? ""; ScheduleSave(); }
    }

    /// <summary>Resolve the effective publish output directory: the configured
    /// path, or the Desktop when unset. Always returns an absolute, existing
    /// directory (creates it if needed).</summary>
    public string ResolvePublishOutputDirectory()
    {
        var dir = data.PublishOutputDirectory;
        if (string.IsNullOrWhiteSpace(dir))
            dir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        try { Directory.CreateDirectory(dir); } catch { /* fall through; caller handles write failure */ }
        return dir;
    }

    /// <summary>The literal output folder for the exported manuscript <c>.docx</c> export
    /// (typically the book's own folder). <c>--export-node</c> writes
    /// <c>&lt;PublishExportDirectory&gt;\&lt;Hyphenated-Title&gt;\&lt;Title&gt; V&lt;N&gt;.docx</c> and clears
    /// any existing <c>.docx</c> in that subfolder first. Empty = Desktop.
    /// Stray wrapping quotes/whitespace are tolerated by the exporter.</summary>
    public string PublishExportDirectory
    {
        get => data.PublishExportDirectory;
        set { data.PublishExportDirectory = value ?? ""; ScheduleSave(); }
    }

    /// <summary>Returns the export base directory for the given universe slug.
    /// <c>PublishExportDirectory</c> is the shared root (falls back to Desktop).
    /// The <c>UniverseExportDirectories[slug]</c> entry fills in the rest of the
    /// path: a <em>relative</em> entry (e.g. "GLMZ") is combined onto the root,
    /// while an <em>absolute</em> entry (e.g. "R:\…\GLMZ") is used verbatim for
    /// back-compat. With no entry, the bare root is returned.</summary>
    public string GetExportDirectory(string? universeSlug)
    {
        var global = (data.PublishExportDirectory ?? string.Empty).Trim().Trim('"', '\'').Trim();
        var root = string.IsNullOrWhiteSpace(global)
            ? Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            : global;

        if (!string.IsNullOrWhiteSpace(universeSlug)
            && data.UniverseExportDirectories.TryGetValue(universeSlug, out var universeDir)
            && !string.IsNullOrWhiteSpace(universeDir))
        {
            var segment = universeDir.Trim().Trim('"', '\'').Trim();
            return Path.IsPathRooted(segment) ? segment : Path.Combine(root, segment);
        }

        return root;
    }

    /// <summary>Persists a universe-specific export directory override.</summary>
    public void SetUniverseExportDirectory(string slug, string dir)
    {
        data.UniverseExportDirectories[slug] = dir ?? "";
        ScheduleSave();
    }

    // ── Inter-beat silence pacing (combined-audio export) ──────────────────
    // When the per-beat audio files are concatenated into the combined node
    // audio, the NodeWorkbenchService.ExportCombinedAsync injects a brief
    // window of digital silence between each pair of beats. The amount is
    // chosen per-beat from these four budgets, picked by SceneType and the
    // trailing punctuation of the prose — see ComputeTrailingSilenceMs in
    // NodeWorkbenchService. Setting any value to 0 disables that tier.
    /// <summary>Silence (ms) inserted after a beat whose SceneType is
    /// <c>section-end</c> — the last paragraph before a <c>##</c> section
    /// header. Long pause; "new chapter section starts here." Default 1800.</summary>
    public int TtsPauseSectionMs { get => data.TtsPauseSectionMs; set { data.TtsPauseSectionMs = value; ScheduleSave(); } }
    /// <summary>Silence (ms) inserted after a beat whose SceneType is
    /// <c>scene-end</c> — the last paragraph before a <c>---</c> divider.
    /// Medium pause; "scene changes." Default 1000.</summary>
    public int TtsPauseSceneMs { get => data.TtsPauseSceneMs; set { data.TtsPauseSceneMs = value; ScheduleSave(); } }
    /// <summary>Silence (ms) inserted between paragraph beats that end in a
    /// hard sentence terminator (<c>.</c>, <c>!</c>, <c>?</c>). The "breath
    /// between paragraphs" budget. Default 400.</summary>
    public int TtsPauseParagraphMs { get => data.TtsPauseParagraphMs; set { data.TtsPauseParagraphMs = value; ScheduleSave(); } }
    /// <summary>Silence (ms) inserted between paragraph beats whose prose
    /// ends mid-sentence (comma, em-dash, colon, no terminator). Short pause
    /// that keeps reading momentum across a paragraph break. Default 200.</summary>
    public int TtsPauseContinuationMs { get => data.TtsPauseContinuationMs; set { data.TtsPauseContinuationMs = value; ScheduleSave(); } }

    // ── Voice profile registry ─────────────────────────────────────────────
    // Named bundles of (voice_id + model + stability + similarity_boost +
    // style + use_speaker_boost). One profile is marked default; narration
    // uses the default's full bundle whenever a beat doesn't carry its own
    // override. The point: pull voice config out of free-floating settings
    // and into a named record so the same profile always renders the same
    // tone — no risk of the user remembering to set sliders identically
    // across sessions.
    public List<Models.VoiceProfile> VoiceProfiles
    {
        get => data.VoiceProfiles;
        set { data.VoiceProfiles = value ?? new(); ScheduleSave(); }
    }
    public string DefaultVoiceProfileId
    {
        get => data.DefaultVoiceProfileId;
        set { data.DefaultVoiceProfileId = value ?? ""; ScheduleSave(); }
    }

    /// <summary>Resolve the active default voice profile. If the user has
    /// added profiles and pinned a default, returns that. Otherwise
    /// synthesises one on the fly from the legacy scalar fields
    /// (<see cref="ElevenLabsVoiceId"/>, <see cref="TtsModel"/>,
    /// <see cref="TtsStability"/>, <see cref="TtsSimilarityBoost"/>,
    /// <see cref="TtsStyle"/>) so first-run callers still get a coherent
    /// profile until they create one in the settings UI.</summary>
    public Models.VoiceProfile GetDefaultVoiceProfile()
    {
        if (!string.IsNullOrEmpty(data.DefaultVoiceProfileId))
        {
            var match = data.VoiceProfiles.FirstOrDefault(p => p.Id == data.DefaultVoiceProfileId);
            if (match != null) return match;
        }
        if (data.VoiceProfiles.Count > 0) return data.VoiceProfiles[0];
        // Synthesise from legacy scalars.
        return new Models.VoiceProfile
        {
            Id              = "narrator-default",
            Label           = string.IsNullOrEmpty(data.NarratorVoiceName) ? "Narrator" : data.NarratorVoiceName,
            VoiceId         = data.ElevenLabsVoiceId,
            Model           = data.TtsModel,
            Stability       = data.TtsStability,
            SimilarityBoost = data.TtsSimilarityBoost,
            Style           = data.TtsStyle,
            UseSpeakerBoost = true,
        };
    }

    /// <summary>Insert or update a profile (matched by <c>Id</c>). Returns
    /// the persisted profile.</summary>
    public Models.VoiceProfile UpsertVoiceProfile(Models.VoiceProfile profile)
    {
        if (profile == null) throw new ArgumentNullException(nameof(profile));
        if (string.IsNullOrWhiteSpace(profile.Id))
            profile.Id = Slugify(profile.Label) + "-" + Guid.CreateVersion7().ToString("N")[..6];
        var existing = data.VoiceProfiles.FindIndex(p => p.Id == profile.Id);
        if (existing >= 0) data.VoiceProfiles[existing] = profile;
        else data.VoiceProfiles.Add(profile);
        if (string.IsNullOrEmpty(data.DefaultVoiceProfileId))
            data.DefaultVoiceProfileId = profile.Id;
        ScheduleSave();
        return profile;
    }

    /// <summary>
    /// Bulk-import every ElevenLabs voice as a v2 baseline profile. Deterministic
    /// id (<c>vox-{slug}-v2</c>) makes re-import idempotent: baselines refresh in
    /// place rather than duplicating, and user-saved tuned copies (which carry
    /// Guid-suffixed ids) are never touched. Returns the number of voices imported.
    /// </summary>
    public int ImportAllVoicesAsProfiles(IEnumerable<TtsVoice> voices)
    {
        if (voices is null) return 0;
        var count = 0;
        foreach (var v in voices)
        {
            if (string.IsNullOrWhiteSpace(v.VoiceId)) continue;
            var name = string.IsNullOrWhiteSpace(v.Name) ? v.VoiceId : v.Name;
            var slug = Slugify(name);
            UpsertVoiceProfile(new Models.VoiceProfile
            {
                Id = $"vox-{slug}-v2", Label = name,
                VoiceId = v.VoiceId, Model = "eleven_multilingual_v2",
                Stability = 0.5, SimilarityBoost = 0.75, Style = 0.0, UseSpeakerBoost = true,
                Description = v.Description,
            });
            UpsertVoiceProfile(new Models.VoiceProfile
            {
                Id = $"vox-{slug}-v3", Label = $"{name} · v3",
                VoiceId = v.VoiceId, Model = "eleven_v3",
                Stability = 1.0, SimilarityBoost = 0.75, Style = 0.0, UseSpeakerBoost = true,
                Description = v.Description,
            });
            count++;
        }
        return count;
    }

    /// <summary>Remove a profile by id. If the deleted profile was the
    /// default, the remaining first profile (if any) becomes the new default.</summary>
    public void DeleteVoiceProfile(string profileId)
    {
        if (string.IsNullOrEmpty(profileId)) return;
        var removed = data.VoiceProfiles.RemoveAll(p => p.Id == profileId);
        if (removed > 0)
        {
            if (data.DefaultVoiceProfileId == profileId)
                data.DefaultVoiceProfileId = data.VoiceProfiles.Count > 0 ? data.VoiceProfiles[0].Id : "";
            ScheduleSave();
        }
    }

    private static string Slugify(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "profile";
        var lower = s.ToLowerInvariant();
        var slug = System.Text.RegularExpressions.Regex.Replace(lower, @"[^a-z0-9]+", "-").Trim('-');
        return string.IsNullOrWhiteSpace(slug) ? "profile" : slug;
    }
    /// <summary>When the configured model is v3-class (audio-tag capable),
    /// inject inline tags like <c>[whispering]</c> / <c>[softly]</c> based
    /// on each beat's EmotionalTone / FacetTag. Off to fall back to plain
    /// voice_settings tuning only.</summary>
    public bool TtsUseAudioTags { get => data.TtsUseAudioTags; set { data.TtsUseAudioTags = value; ScheduleSave(); } }
    public string OpenAiApiKey
    {
        get => ResolveApiKey("SS_OPENAI_API_KEY", "openai", data.OpenAiApiKey);
        set { MindAtticCredentialStore.SetKey("openai", value); data.OpenAiApiKey = value; ScheduleSave(); }
    }
    public string OpenAiModel { get => data.OpenAiModel; set { data.OpenAiModel = value; ScheduleSave(); } }
    public string ActiveLlmProvider { get => data.ActiveLlmProvider; set { data.ActiveLlmProvider = value; ScheduleSave(); } }
    public int EditorFontSize { get => data.EditorFontSize; set { data.EditorFontSize = value; ScheduleSave(); } }
    public int AutoSaveIntervalMs { get => data.AutoSaveIntervalMs; set { data.AutoSaveIntervalMs = value; ScheduleSave(); } }
    public string GeminiApiKey
    {
        get => ResolveApiKey("SS_GEMINI_API_KEY", "gemini", data.GeminiApiKey);
        set { MindAtticCredentialStore.SetKey("gemini", value); data.GeminiApiKey = value; ScheduleSave(); }
    }
    public string DeepSeekApiKey
    {
        get => ResolveApiKey("SS_DEEPSEEK_API_KEY", "deepseek", data.DeepSeekApiKey);
        set { MindAtticCredentialStore.SetKey("deepseek", value); data.DeepSeekApiKey = value; ScheduleSave(); }
    }
    public string MistralApiKey
    {
        get => ResolveApiKey("SS_MISTRAL_API_KEY", "mistral", data.MistralApiKey);
        set { MindAtticCredentialStore.SetKey("mistral", value); data.MistralApiKey = value; ScheduleSave(); }
    }
    public string GrokApiKey
    {
        get => ResolveApiKey("SS_GROK_API_KEY", "xai", data.GrokApiKey);
        set { MindAtticCredentialStore.SetKey("xai", value); data.GrokApiKey = value; ScheduleSave(); }
    }
    public string GroqApiKey
    {
        get => ResolveApiKey("SS_GROQ_API_KEY", "groq", data.GroqApiKey);
        set { MindAtticCredentialStore.SetKey("groq", value); data.GroqApiKey = value; ScheduleSave(); }
    }
    public string TogetherApiKey
    {
        get => ResolveApiKey("SS_TOGETHER_API_KEY", "together", data.TogetherApiKey);
        set { MindAtticCredentialStore.SetKey("together", value); data.TogetherApiKey = value; ScheduleSave(); }
    }
    public string OpenRouterApiKey
    {
        get => ResolveApiKey("SS_OPENROUTER_API_KEY", "openrouter", data.OpenRouterApiKey);
        set { MindAtticCredentialStore.SetKey("openrouter", value); data.OpenRouterApiKey = value; ScheduleSave(); }
    }
    public string FireworksApiKey
    {
        get => ResolveApiKey("SS_FIREWORKS_API_KEY", "fireworks", data.FireworksApiKey);
        set { MindAtticCredentialStore.SetKey("fireworks", value); data.FireworksApiKey = value; ScheduleSave(); }
    }
    public string CohereApiKey
    {
        get => ResolveApiKey("SS_COHERE_API_KEY", "cohere", data.CohereApiKey);
        set { MindAtticCredentialStore.SetKey("cohere", value); data.CohereApiKey = value; ScheduleSave(); }
    }
    public string IdeogramApiKey
    {
        get => ResolveApiKey("SS_IDEOGRAM_API_KEY", "ideogram", data.IdeogramApiKey);
        set { MindAtticCredentialStore.SetKey("ideogram", value); data.IdeogramApiKey = value; ScheduleSave(); }
    }
    public string FalApiKey
    {
        get => ResolveApiKey("SS_FAL_API_KEY", "fal", data.FalApiKey);
        set { MindAtticCredentialStore.SetKey("fal", value); data.FalApiKey = value; ScheduleSave(); }
    }
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
    public string MapApiKey
    {
        get => ResolveApiKey("SS_MAP_API_KEY", "here-maps", data.MapApiKey);
        set { MindAtticCredentialStore.SetKey("here-maps", value); data.MapApiKey = value; ScheduleSave(); }
    }
    public string GoogleMapsApiKey
    {
        get => ResolveApiKey("SS_GOOGLE_MAPS_API_KEY", "google-maps", data.GoogleMapsApiKey);
        set { MindAtticCredentialStore.SetKey("google-maps", value); data.GoogleMapsApiKey = value; ScheduleSave(); }
    }
    public string MapMode { get => data.MapMode; set { data.MapMode = value; ScheduleSave(); } }
    public string TimestampFormat { get => data.TimestampFormat; set { data.TimestampFormat = value; ScheduleSave(); } }
    public string TimezoneId { get => data.TimezoneId; set { data.TimezoneId = value; ScheduleSave(); } }
    public string FontFamily { get => data.FontFamily; set { data.FontFamily = value; ScheduleSave(); } }
    public bool RepoListOnRight { get => data.RepoListOnRight; set { data.RepoListOnRight = value; ScheduleSave(); } }
    public bool EnablePlainTextNer { get => data.EnablePlainTextNer; set { data.EnablePlainTextNer = value; ScheduleSave(); } }
    public bool SaveStoriesAsMarkdown { get => data.SaveStoriesAsMarkdown; set { data.SaveStoriesAsMarkdown = value; ScheduleSave(); } }
    public bool DocxIncludeToc { get => data.DocxIncludeToc; set { data.DocxIncludeToc = value; ScheduleSave(); } }

    /// <summary>Master switch for the Doc Context Stack injection into prose-generation prompts.
    /// Default ON — injects the node bible, voice register, and topic docs into every beat prompt.
    /// Dry-run (`--doc-context`) + MCP tools work regardless of this flag.</summary>
    public bool DocContextEnabled { get => data.DocContextEnabled; set { data.DocContextEnabled = value; ScheduleSave(); } }

    /// <summary>When ON, ProseWriterRouter captures the FULL DCM working-set (not budget-clipped)
    /// per beat into ContextTelemetryService for Gantt visualization export. Default OFF (zero
    /// overhead until you opt in). Use <c>ss --dcm-viz</c> to generate the .htm without a full prose run.</summary>
    public bool DcmLoggingEnabled { get => data.DcmLoggingEnabled; set { data.DcmLoggingEnabled = value; ScheduleSave(); } }

    // ── Review voting ──────────────────────────────────────────────────────────
    /// <summary>Default number of cheap score-only ballots per sampled node review (--ballots).</summary>
    public int ReviewBallots { get => data.ReviewBallots; set { data.ReviewBallots = Math.Max(1, value); ScheduleSave(); } }
    /// <summary>Default number of full prose upgrades per sampled run (--prose).</summary>
    public int ReviewProse { get => data.ReviewProse; set { data.ReviewProse = Math.Max(0, value); ScheduleSave(); } }
    /// <summary>Default segment-study panel size (--panel / --study).</summary>
    public int ReviewPanel { get => data.ReviewPanel; set { data.ReviewPanel = Math.Max(1, value); ScheduleSave(); } }
    /// <summary>Default reader count for full / census runs (--readers).</summary>
    public int ReviewReaders { get => data.ReviewReaders; set { data.ReviewReaders = Math.Max(1, value); ScheduleSave(); } }
    /// <summary>Provider that synthesises the reader-synopsis after a review run.</summary>
    public string ReviewJudgeProvider { get => data.ReviewJudgeProvider; set { data.ReviewJudgeProvider = value; ScheduleSave(); } }
    /// <summary>Comma-separated provider IDs allowed to cast ballots (e.g. "claude,openai,gemini,deepseek").</summary>
    public string ReviewAllowedProviders { get => data.ReviewAllowedProviders; set { data.ReviewAllowedProviders = value; ScheduleSave(); } }
    /// <summary>Maximum simultaneous LLM calls during a review run.</summary>
    public int ReviewMaxConcurrency { get => data.ReviewMaxConcurrency; set { data.ReviewMaxConcurrency = Math.Max(1, Math.Min(50, value)); ScheduleSave(); } }

    // ── Local-LLM review (--local) ───────────────────────────────────────────────
    /// <summary>OpenAI-compatible chat-completions endpoint of the local inference server
    /// (Ollama default). Only used by <c>--local</c> node reviews; cloud reviews never touch it.</summary>
    public string LocalReviewBaseUrl { get => data.LocalReviewBaseUrl; set { data.LocalReviewBaseUrl = value; ScheduleSave(); } }
    /// <summary>Local model tag used by <c>--local</c> reviews (e.g. an Ollama tag with a baked-in num_ctx).</summary>
    public string LocalReviewModel { get => data.LocalReviewModel; set { data.LocalReviewModel = value; ScheduleSave(); } }
    /// <summary>Human label for WHICH local backend a <c>--local</c> run used (e.g. "vast", "runpod").
    /// Stamped as the report "brain" so vast.ai / RunPod / Ollama runs write SEPARATE report files
    /// (<c>… reviews (vast).htm</c> vs <c>(runpod).htm</c>) instead of all colliding under "(local)".
    /// Empty = auto-derive from <see cref="LocalReviewBaseUrl"/> host, falling back to "local".</summary>
    public string LocalReviewLabel { get => data.LocalReviewLabel; set { data.LocalReviewLabel = value; ScheduleSave(); } }
    /// <summary>Bearer token for the local/self-hosted review endpoint. Empty for a bare
    /// localhost Ollama (which ignores auth); set it to the API key when
    /// <see cref="LocalReviewBaseUrl"/> points at a SECURED remote GPU (RunPod/vLLM/etc.),
    /// so "local" reviews can run on a rented machine instead of your own VRAM.</summary>
    public string LocalReviewApiKey { get => data.LocalReviewApiKey; set { data.LocalReviewApiKey = value; ScheduleSave(); } }
    /// <summary>Max simultaneous local generations — kept low because a single GPU can only
    /// run a few large-model generations at once before spilling / OOM.</summary>
    public int LocalReviewMaxConcurrency { get => data.LocalReviewMaxConcurrency; set { data.LocalReviewMaxConcurrency = Math.Max(1, Math.Min(16, value)); ScheduleSave(); } }
    /// <summary>The local model's context window in TOKENS (the Ollama tag's num_ctx). The review
    /// engine uses this to size segments so an oversized node is chunked to FIT the local window
    /// instead of being silently truncated (which drops the system prompt and fails every ballot).
    /// Cloud reviews ignore this. Default 16384 — raise it to match a model rebuilt with a larger
    /// num_ctx, and big nodes will segment into fewer, larger chunks.</summary>
    public int LocalReviewContextTokens { get => data.LocalReviewContextTokens; set { data.LocalReviewContextTokens = Math.Max(4096, value); ScheduleSave(); } }

    // ── Local-LLM generation (--local prose) ─────────────────────────────────────
    /// <summary>OpenAI-compatible chat-completions endpoint for local prose generation
    /// (Ollama, vLLM, RunPod, etc.). Used by <c>--local</c> beat/node generation;
    /// cloud generation never reads it. Empty = local prose generation disabled.</summary>
    public string LocalLlmBaseUrl { get => data.LocalLlmBaseUrl; set { data.LocalLlmBaseUrl = value; ScheduleSave(); } }
    /// <summary>Bearer token for the local generation endpoint. Empty for bare localhost Ollama
    /// (which ignores auth); set to the pod token when pointing at a secured remote GPU (RunPod/vLLM).</summary>
    public string LocalLlmApiKey { get => data.LocalLlmApiKey; set { data.LocalLlmApiKey = value; ScheduleSave(); } }
    /// <summary>Model tag used for local prose generation (e.g. "qwen2.5-32b").</summary>
    public string LocalLlmModel { get => data.LocalLlmModel; set { data.LocalLlmModel = value; ScheduleSave(); } }

    // ── Local-LLM embeddings ─────────────────────────────────────────────────────
    /// <summary>Full URL to an OpenAI-compatible /v1/embeddings endpoint on the local/remote GPU
    /// (e.g. <c>https://&lt;runpod&gt;/v1/embeddings</c>). When set, EmbeddingService routes ALL
    /// embed calls here instead of OpenAI — no OpenAI key needed for local review runs.</summary>
    public string LocalEmbeddingBaseUrl { get => data.LocalEmbeddingBaseUrl; set { data.LocalEmbeddingBaseUrl = value; ScheduleSave(); } }
    /// <summary>Bearer token for the local embedding endpoint. Empty = no auth header sent (bare Ollama).</summary>
    public string LocalEmbeddingApiKey  { get => data.LocalEmbeddingApiKey;  set { data.LocalEmbeddingApiKey  = value; ScheduleSave(); } }
    /// <summary>Model tag for the local embedding endpoint (e.g. "Qwen/Qwen3-Embedding-0.6B").</summary>
    public string LocalEmbeddingModel   { get => data.LocalEmbeddingModel;   set { data.LocalEmbeddingModel   = value; ScheduleSave(); } }

    /// <summary>vast.ai REST API key for <c>ss --gpu</c> (start/stop/destroy the rented review box).
    /// Read straight from the shared MindAttic credential vault — <c>VAST_API_KEY</c> env, then
    /// %APPDATA%/MindAttic/LLM/<c>vast.json</c>, then the <c>vast</c> entry in <c>providers.json</c>,
    /// then cloud config. The standard LLM resolver only knows registered providers, so a non-LLM
    /// credential like this reads the vault files directly. Not an LLM key.</summary>
    public string VastApiKey => ResolveVaultKey("VAST_API_KEY", "vast");

    /// <summary>RunPod REST API key for <c>ss --runpod</c> (status/stop/start/terminate the rented
    /// review pod). Resolved from the shared MindAttic credential vault — <c>RUNPOD_API_KEY</c> env,
    /// then %APPDATA%/MindAttic/LLM/<c>runpod.json</c>, then the <c>runpod</c> entry in
    /// <c>providers.json</c>, then cloud config. Not an LLM key; never on the command line.</summary>
    public string RunPodApiKey => ResolveVaultKey("RUNPOD_API_KEY", "runpod");

    private static string ResolveVaultKey(string envVar, string providerId)
    {
        var fromEnv = Environment.GetEnvironmentVariable(envVar);
        if (!string.IsNullOrWhiteSpace(fromEnv)) return fromEnv.Trim();
        try
        {
            var dir = Environment.GetEnvironmentVariable("MINDATTIC_LLM_CREDENTIALS");
            if (string.IsNullOrWhiteSpace(dir))
                dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MindAttic", "LLM");

            var perFile = Path.Combine(dir, providerId + ".json");
            if (File.Exists(perFile))
            {
                using var d = System.Text.Json.JsonDocument.Parse(File.ReadAllText(perFile));
                if (d.RootElement.TryGetProperty("apiKey", out var k) && k.ValueKind == System.Text.Json.JsonValueKind.String
                    && !string.IsNullOrWhiteSpace(k.GetString())) return k.GetString()!.Trim();
            }
            var index = Path.Combine(dir, "providers.json");
            if (File.Exists(index))
            {
                using var d = System.Text.Json.JsonDocument.Parse(File.ReadAllText(index));
                if (d.RootElement.TryGetProperty(providerId, out var prov)
                    && prov.TryGetProperty("apiKey", out var k2) && k2.ValueKind == System.Text.Json.JsonValueKind.String
                    && !string.IsNullOrWhiteSpace(k2.GetString())) return k2.GetString()!.Trim();
            }
        }
        catch { /* fall through to cloud config */ }
        return VaultConfiguration?[$"MindAttic:Vault:{providerId}:apiKey"]?.Trim() ?? "";
    }
    /// <summary>When false, ContinuousQualityService does not fire automatically on beat save. Reviews must be called manually.</summary>
    public bool ReviewAutoRunEnabled { get => data.ReviewAutoRunEnabled; set { data.ReviewAutoRunEnabled = value; ScheduleSave(); } }
    /// <summary>When true, CanonGroundingService fires post-write on every beat to flag PROVISIONAL-ENTITY findings. Off by default — opt in to avoid extra LLM cost.</summary>
    public bool AutoCanonGrounding { get => data.AutoCanonGrounding; set { data.AutoCanonGrounding = value; ScheduleSave(); } }
    /// <summary>When true, SceneContextAssembler.HarvestRevealedDetailsAsync fires post-write to propose XRAY-REVEAL findings. Off by default.</summary>
    public bool AutoHarvestRevealedDetails { get => data.AutoHarvestRevealedDetails; set { data.AutoHarvestRevealedDetails = value; ScheduleSave(); } }
    /// <summary>When true, WorldTickService advances the story clock and writes EntityStateEvents per active character on each tick.
    /// Off by default — enable deliberately once the rule layer is ready.</summary>
    public bool WorldTickEnabled { get => data.WorldTickEnabled; set { data.WorldTickEnabled = value; ScheduleSave(); } }

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

    /// <summary>Selectable audiobook delivery formats: a stable key, a UI label,
    /// and the container extension. The encode args live in
    /// <see cref="ResolveAudiobookEncode"/>. MP3 first (universal), then lossless.</summary>
    public static readonly (string Key, string Label, string Extension)[] AudiobookFormats =
    [
        ("mp3_320", "MP3 — 320 kbps (recommended)", "mp3"),
        ("mp3_256", "MP3 — 256 kbps",               "mp3"),
        ("mp3_192", "MP3 — 192 kbps",               "mp3"),
        ("mp3_128", "MP3 — 128 kbps (smallest)",    "mp3"),
        ("wav",     "WAV — lossless (largest)",      "wav"),
        ("flac",    "FLAC — lossless (compressed)",  "flac"),
    ];

    /// <summary>Resolve the configured <see cref="AudiobookFormat"/> to the file
    /// extension and the ffmpeg audio-codec argument list used to encode the
    /// combined WAV. <c>Args == null</c> means "deliver the assembled WAV as-is"
    /// (no re-encode). Unknown keys fall back to 320 kbps MP3.</summary>
    public (string Extension, string[]? Args) ResolveAudiobookEncode()
    {
        return AudiobookFormat switch
        {
            "mp3_320" => ("mp3",  ["-codec:a", "libmp3lame", "-b:a", "320k"]),
            "mp3_256" => ("mp3",  ["-codec:a", "libmp3lame", "-b:a", "256k"]),
            "mp3_192" => ("mp3",  ["-codec:a", "libmp3lame", "-b:a", "192k"]),
            "mp3_128" => ("mp3",  ["-codec:a", "libmp3lame", "-b:a", "128k"]),
            "wav"     => ("wav",  null),
            "flac"    => ("flac", ["-codec:a", "flac"]),
            _         => ("mp3",  ["-codec:a", "libmp3lame", "-b:a", "320k"]),
        };
    }

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

    /// <summary>Reset all settings to the saved defaults snapshot (includes secrets).
    /// Also overwrites the shared MindAttic credential store with the reset values so
    /// the next read doesn't pick up stale "first-stop" keys. This intentionally
    /// affects every MindAttic app — resetting one app's credentials is a fresh slate.</summary>
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
        SyncCredentialStoreFromData();
    }

    private void SyncCredentialStoreFromData()
    {
        MindAtticCredentialStore.SetKey("claude-api",   data.ApiKey);
        MindAtticCredentialStore.SetKey("openai",      data.OpenAiApiKey);
        MindAtticCredentialStore.SetKey("gemini",      data.GeminiApiKey);
        MindAtticCredentialStore.SetKey("ideogram",    data.IdeogramApiKey);
        MindAtticCredentialStore.SetKey("fal",         data.FalApiKey);
        MindAtticCredentialStore.SetKey("deepseek",    data.DeepSeekApiKey);
        MindAtticCredentialStore.SetKey("mistral",     data.MistralApiKey);
        MindAtticCredentialStore.SetKey("xai",         data.GrokApiKey);
        MindAtticCredentialStore.SetKey("groq",        data.GroqApiKey);
        MindAtticCredentialStore.SetKey("together",    data.TogetherApiKey);
        MindAtticCredentialStore.SetKey("openrouter",  data.OpenRouterApiKey);
        MindAtticCredentialStore.SetKey("fireworks",   data.FireworksApiKey);
        MindAtticCredentialStore.SetKey("cohere",      data.CohereApiKey);
        MindAtticCredentialStore.SetKey("elevenlabs",  data.ElevenLabsApiKey);
        MindAtticCredentialStore.SetKey("here-maps",   data.MapApiKey);
        MindAtticCredentialStore.SetKey("google-maps", data.GoogleMapsApiKey);
    }

    private void Load()
    {
        if (File.Exists(settingsPath))
        {
            var json = File.ReadAllText(settingsPath);
            data = JsonSerializer.Deserialize<SettingsData>(json) ?? new();
        }
        // Baseline = what we just loaded (serialized through the same options Flush uses, so the
        // diff compares like-for-like). Empty object when the file is absent.
        baseline = JsonSerializer.SerializeToNode(data, JsonDefaults.Indented)?.AsObject() ?? new JsonObject();
    }

    /// <summary>
    /// One-shot copy of any legacy credentials from Settings.json into the shared
    /// MindAttic credential store at %APPDATA%/MindAttic/LLM/. Idempotent: only writes
    /// when the shared store has no key for that provider yet, so it never overwrites
    /// a credential another MindAttic app may have rotated.
    /// </summary>
    private void MigrateLegacyCredentialsToSharedStore()
    {
        void MigrateIfMissing(string providerId, string legacyKey)
        {
            if (string.IsNullOrEmpty(legacyKey)) return;
            if (!string.IsNullOrEmpty(MindAtticCredentialStore.GetKey(providerId))) return;
            MindAtticCredentialStore.SetKey(providerId, legacyKey);
        }

        MigrateIfMissing("claude-api",   data.ApiKey);
        MigrateIfMissing("openai",      data.OpenAiApiKey);
        MigrateIfMissing("gemini",      data.GeminiApiKey);
        MigrateIfMissing("ideogram",    data.IdeogramApiKey);
        MigrateIfMissing("fal",         data.FalApiKey);
        MigrateIfMissing("deepseek",    data.DeepSeekApiKey);
        MigrateIfMissing("mistral",     data.MistralApiKey);
        MigrateIfMissing("xai",         data.GrokApiKey);
        MigrateIfMissing("groq",        data.GroqApiKey);
        MigrateIfMissing("together",    data.TogetherApiKey);
        MigrateIfMissing("openrouter",  data.OpenRouterApiKey);
        MigrateIfMissing("fireworks",   data.FireworksApiKey);
        MigrateIfMissing("cohere",      data.CohereApiKey);
        MigrateIfMissing("elevenlabs",  data.ElevenLabsApiKey);
        MigrateIfMissing("here-maps",   data.MapApiKey);
        MigrateIfMissing("google-maps", data.GoogleMapsApiKey);
    }

    private void ScheduleSave()
    {
        lock (saveLock)
        {
            saveTimer?.Dispose();
            saveTimer = new Timer(_ =>
            {
                try { Flush(); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }, null, 500, Timeout.Infinite);
        }
    }

    /// <summary>
    /// Persist settings to disk as a MERGE, not a wholesale overwrite. Settings.json is shared by
    /// several live processes (the CLI, the MCP server, and the Blazor/Writer/Codex web hosts), each
    /// holding its own in-memory copy loaded at startup. A plain "serialize the whole object → overwrite
    /// the file" lets a process with a STALE snapshot silently clobber fields other processes changed
    /// after it loaded (e.g. an admin toggling a theme on /settings wiping a CLI-written export dir).
    /// Instead we overlay ONLY the top-level keys this process actually changed — diffed against the
    /// load-time <see cref="baseline"/> — onto a fresh read of the current file, under a cross-process
    /// mutex with an atomic temp+rename. Keys we didn't touch keep their on-disk value, so concurrent
    /// writers can no longer stomp each other.
    /// </summary>
    public void Flush()
    {
        lock (saveLock)
        {
            saveTimer?.Dispose();
            saveTimer = null;
            var dir = Path.GetDirectoryName(settingsPath);
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return;

            using var guard = new CrossProcessLock(settingsPath);
            guard.Acquire(TimeSpan.FromSeconds(5));

            // This process's full current state, as JSON nodes (same options as the file, so nulls are
            // omitted identically and the diff compares like-for-like).
            var current = JsonSerializer.SerializeToNode(data, JsonDefaults.Indented)?.AsObject()
                          ?? new JsonObject();

            // The current on-disk truth (may include fields other processes wrote after we loaded).
            JsonObject disk;
            try
            {
                disk = File.Exists(settingsPath)
                    ? (JsonNode.Parse(File.ReadAllText(settingsPath)) as JsonObject) ?? new JsonObject()
                    : new JsonObject();
            }
            catch (JsonException) { disk = new JsonObject(); }   // corrupt/partial read — rebuild from ours
            catch (IOException)   { disk = new JsonObject(); }

            // Overlay only the keys THIS process changed since it loaded / last flushed.
            foreach (var (key, node) in current)
            {
                var changedHere = baseline is null
                    || !baseline.TryGetPropertyValue(key, out var baseNode)
                    || !JsonNode.DeepEquals(node, baseNode);
                if (changedHere)
                    disk[key] = node?.DeepClone();
            }

            var merged = disk.ToJsonString(JsonDefaults.Indented);
            var tmp = settingsPath + ".tmp";
            File.WriteAllText(tmp, merged);
            File.Move(tmp, settingsPath, overwrite: true);

            // Adopt the merged result: pick up fields other writers contributed, and re-baseline so the
            // next diff is taken against what is actually persisted now.
            data = disk.Deserialize<SettingsData>() ?? data;
            baseline = disk;
        }
    }

    /// <summary>Best-effort cross-process lock (named mutex) around the settings read-modify-write.
    /// Degrades to no-lock if named mutexes are unavailable; the atomic temp+rename still applies.</summary>
    private sealed class CrossProcessLock : IDisposable
    {
        private readonly Mutex? mutex;
        private bool held;

        public CrossProcessLock(string path)
        {
            try
            {
                var name = "Global\\MindAttic_SS_Settings_" + Convert.ToHexString(
                    System.Security.Cryptography.MD5.HashData(
                        System.Text.Encoding.UTF8.GetBytes(path.ToLowerInvariant())));
                mutex = new Mutex(false, name);
            }
            catch { mutex = null; }
        }

        public void Acquire(TimeSpan timeout)
        {
            if (mutex is null) return;
            try { held = mutex.WaitOne(timeout); }
            catch (AbandonedMutexException) { held = true; }   // prior holder crashed — we own it now
            catch { held = false; }
        }

        public void Dispose()
        {
            try { if (held) mutex?.ReleaseMutex(); } catch { /* not owner / already released */ }
            mutex?.Dispose();
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
        public string Theme { get; set; } = "auto";
        public string CanonRootPath { get; set; } = "";
        public int MaxTokens { get; set; } = 2048;
        public string ElevenLabsApiKey { get; set; } = "";
        public string ElevenLabsVoiceId { get; set; } = "jfIS2w2yJi0grJZPyEsk";
        public string NarratorVoiceName { get; set; } = "Oliver Silk - Deep Gravel Narrative";
        public string TtsModel { get; set; } = "eleven_multilingual_v2";
        public double TtsStability { get; set; } = 0.5;
        public double TtsSimilarityBoost { get; set; } = 0.75;
        public double TtsStyle { get; set; } = 0.0;
        /// <summary>Final audiobook delivery format key (see <see cref="AudiobookFormats"/>).
        /// Default 320 kbps MP3 — the source is fetched losslessly when the tier allows.</summary>
        public string AudiobookFormat { get; set; } = "mp3_320";
        public bool TtsUseAudioTags { get; set; } = true;
        /// <summary>Empty = Desktop.</summary>
        public string PublishOutputDirectory { get; set; } = "";
        /// <summary>Base dir for the manuscript export. Empty = Desktop.</summary>
        public string PublishExportDirectory { get; set; } = "";
        /// <summary>Per-universe overrides for the manuscript export base dir.
        /// Key = universe slug (e.g. "glmz", "scry"). Wins over <see cref="PublishExportDirectory"/>.</summary>
        public Dictionary<string, string> UniverseExportDirectories { get; set; } = new();
        public int TtsPauseSectionMs { get; set; } = 1800;
        public int TtsPauseSceneMs { get; set; } = 1000;
        public int TtsPauseParagraphMs { get; set; } = 400;
        public int TtsPauseContinuationMs { get; set; } = 200;
        public List<Models.VoiceProfile> VoiceProfiles { get; set; } = new();
        public string DefaultVoiceProfileId { get; set; } = "";
        public string OpenAiApiKey { get; set; } = "";
        public string OpenAiModel { get; set; } = "gpt-4.1-mini";
        public string ActiveLlmProvider { get; set; } = "claude-api";
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
        public string IdeogramApiKey { get; set; } = "";
        public string FalApiKey { get; set; } = "";
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
        public bool RepoListOnRight { get; set; } = true;
        public bool EnablePlainTextNer { get; set; } = false;
        public bool SaveStoriesAsMarkdown { get; set; } = true;
        public bool DocxIncludeToc { get; set; } = false;
        public bool DocContextEnabled { get; set; } = true;
        public bool DcmLoggingEnabled { get; set; } = false;
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
        // Review voting defaults
        public int ReviewBallots { get; set; } = 20;
        public int ReviewProse { get; set; } = 4;
        public int ReviewPanel { get; set; } = 128;
        public int ReviewReaders { get; set; } = 50;
        public string ReviewJudgeProvider { get; set; } = "gemini";
        public string ReviewAllowedProviders { get; set; } = "claude-api";
        public int ReviewMaxConcurrency { get; set; } = 10;
        // Local-LLM review (--local) defaults
        public string LocalReviewBaseUrl { get; set; } = "http://localhost:11434/v1/chat/completions";
        public string LocalReviewModel { get; set; } = "qwen2.5-32b-rev-128k";
        public string LocalReviewLabel { get; set; } = "";
        /// <summary>Bearer token for a SECURED remote review endpoint (RunPod/vLLM/etc.); empty = bare localhost Ollama.</summary>
        public string LocalReviewApiKey { get; set; } = "";
        /// <summary>Local model context window in tokens (num_ctx) — used to size review segments to fit.</summary>
        public int LocalReviewContextTokens { get; set; } = 131072;
        public int LocalReviewMaxConcurrency { get; set; } = 2;
        // Local-LLM generation (--local prose) defaults
        public string LocalLlmBaseUrl { get; set; } = "";
        public string LocalLlmApiKey { get; set; } = "";
        public string LocalLlmModel { get; set; } = "qwen2.5-32b";
        // Local-LLM embeddings
        public string LocalEmbeddingBaseUrl { get; set; } = "";
        public string LocalEmbeddingApiKey  { get; set; } = "";
        public string LocalEmbeddingModel   { get; set; } = "";
        /// <summary>When false, ContinuousQualityService does not fire on beat save. Reviews must be called manually.</summary>
        public bool ReviewAutoRunEnabled { get; set; } = true;
        /// <summary>When true, WorldTickService is active — advances story clock + writes EntityStateEvents per tick.</summary>
        public bool WorldTickEnabled { get; set; } = false;
        /// <summary>When true, CanonGroundingService fires after each beat write to flag PROVISIONAL-ENTITY findings. Default OFF — opt in to avoid extra LLM cost per beat.</summary>
        public bool AutoCanonGrounding { get; set; } = false;
        /// <summary>When true, SceneContextAssembler.HarvestRevealedDetailsAsync fires after each beat write to propose XRAY-REVEAL findings. Default OFF.</summary>
        public bool AutoHarvestRevealedDetails { get; set; } = false;
    }
}
