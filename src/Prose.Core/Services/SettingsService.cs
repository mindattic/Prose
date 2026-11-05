using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using MindAttic.Legion;
using MindAttic.Vault.Credentials;
using MindAttic.Vault.Paths;
using MindAttic.Vault.Settings;
using Prose.Core.Interfaces;

namespace Prose.Core.Services;

/// <summary>
/// Prose's settings, shared by every Prose process on the machine. Since LAMP (PRS-T829) the
/// app-level settings (models, review defaults, local LLM, TTS, editor, export, maps, SMTP host,
/// feature flags) live in MindAttic Vault as <see cref="ProseSettings"/>
/// (<c>%APPDATA%\MindAttic\Prose\settings.json</c>, edited in MindAttic.Vault.Desktop); the first load
/// imports them from the old <c>%LOCALAPPDATA%\MindAttic\Prose\Settings.json</c>. That file keeps
/// what isn't an app setting: per-universe export folders, voice profiles, and the legacy secret
/// fields (SMTP password, Hub key, local-endpoint bearer tokens, pre-Vault API keys). Its copies of
/// the Vault-owned fields are no longer written (they stay as the rollback).
/// Setters debounce a save; <see cref="Flush"/> merges only the keys this process changed onto
/// both files. A save made in Vault.Desktop reaches a running process through <see cref="Watch"/>.
/// API-key properties resolve through the credential chain in <see cref="ResolveApiKey"/>.
/// </summary>
public class SettingsService : IDisposable
{
    /// <summary>The <c>{prefix}-{provider}</c> prefix of Prose's pre-LAMP own key copies in the LLM bucket.</summary>
    private const string LegacyKeyPrefix = "prose";

    /// <summary>
    /// Pre-LAMP <see cref="AppScopedCredentialStore"/> over the LLM keyring (<c>prose-{provider}</c>).
    /// Only used when <c>MINDATTIC_LLM_CREDENTIALS</c> points at a bare key folder (tests and
    /// explicit redirects); everywhere else Prose's own keys are its Vault key assignment, and any
    /// leftover <c>prose-*</c> copy is moved into the shared named list on first start
    /// (<see cref="KeyImporter.ImportAppScopedFor"/>).
    /// </summary>
    private static AppScopedCredentialStore OwnProviderStore() => new(
        LegacyKeyPrefix,
        new LlmCredentialStore(
            Environment.GetEnvironmentVariable(LlmCredentialStore.DirectoryEnvVar)
            ?? VaultPaths.RoamingBucket(LlmCredentialStore.Bucket)));

    /// <summary>True when <c>MINDATTIC_LLM_CREDENTIALS</c> redirects keys to a bare folder (the pre-LAMP path).</summary>
    private static bool UsesKeyOverrideFolder =>
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(LlmCredentialStore.DirectoryEnvVar));

    private static int importedOwnKeys;

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

    // ── MindAttic Vault (LAMP, PRS-T829) ─────────────────────────────────────
    private readonly JsonSettingsStore<ProseSettings> vaultStore;
    // The Vault values this process uses; every app-level property reads and writes it.
    private ProseSettings vault = new();
    // The Vault values as last read from / written to settings.json; FlushVault writes only the
    // properties this process changed since, so two processes can't clobber each other there either.
    private JsonObject vaultBaseline = new();
    private FileSystemWatcher? vaultWatcher;
    private Timer? vaultDebounce;

    // Same names as the old Settings.json fields (PascalCase) — used to import and to diff.
    private static readonly JsonSerializerOptions VaultDiffOptions = new();

    /// <summary>The Settings.json fields MindAttic Vault owns since LAMP (every <see cref="ProseSettings"/> property).</summary>
    internal static readonly HashSet<string> VaultOwnedFields =
        typeof(ProseSettings).GetProperties().Select(p => p.Name).ToHashSet(StringComparer.Ordinal);

    /// <summary>
    /// Default location of the pre-LAMP settings (and the remaining non-Vault fields):
    /// <c>%LOCALAPPDATA%\MindAttic\Prose</c>, resolved through <see cref="VaultPaths.LocalApp"/> on every read so
    /// <c>MINDATTIC_VAULT_LOCAL_ROOT</c> moves it too — a test run that isolates the Vault roots can then never
    /// rewrite the user's real Settings.json through the DI singleton (PRS-T832, M.VALT-LAW-6).
    /// </summary>
    public static string DefaultStorageDirectory => VaultPaths.LocalApp("Prose");

    /// <summary>The real install: Settings.json in %LOCALAPPDATA%, Vault in %APPDATA%\MindAttic\Prose; watches Vault.</summary>
    public SettingsService() : this(DefaultStorageDirectory, ProseSettings.ForCurrentUser())
    {
        Watch();
    }

    /// <summary>
    /// Explicit storage directory (tests, scratch hosts). With no <paramref name="vaultStore"/>, the
    /// default directory gets the real Vault file and any other directory gets a private one in
    /// <c>&lt;storageDir&gt;\vault</c>, so a scratch instance can never read or change the user's settings.
    /// </summary>
    public SettingsService(string storageDir, JsonSettingsStore<ProseSettings>? vaultStore = null)
    {
        Directory.CreateDirectory(storageDir);
        settingsPath = Path.Combine(storageDir, "Settings.json");
        defaultsPath = Path.Combine(storageDir, "Defaults.json");
        this.vaultStore = vaultStore ?? VaultStoreFor(storageDir);
        Load();
        LoadVault();
        ImportOwnKeyCopiesOnce();
        MigrateLegacyCredentialsToSharedStore();
        NormalizeLegacyClaudeProviderIds();

        // Auto-detect canon root if not set or current path has insufficient data.
        // Post-archival, "valid canon" means the engine dir exists and has the
        // standard subfolders — entity content lives in SQL now, not on disk,
        // so the old "≥ 10 .json files" heuristic no longer applies.
        var engineDir = string.IsNullOrWhiteSpace(vault.CanonRootPath)
            ? ""
            : Path.Combine(vault.CanonRootPath, Constants.Folders.Engine);
        var hasData = !string.IsNullOrWhiteSpace(engineDir)
            && Directory.Exists(engineDir)
            && Directory.Exists(Path.Combine(engineDir, "data"));

        if (!hasData)
        {
            var detected = AutoDetectCanonRoot();
            if (detected != null)
            {
                vault.CanonRootPath = detected;
                Flush();
            }
        }
    }

    /// <summary>The Vault store this instance reads and writes.</summary>
    public JsonSettingsStore<ProseSettings> VaultStore => vaultStore;

    /// <summary>
    /// The Vault store for a storage directory: the real one for <see cref="DefaultStorageDirectory"/>,
    /// a private <c>&lt;storageDir&gt;\vault</c> for anything else.
    /// </summary>
    public static JsonSettingsStore<ProseSettings> VaultStoreFor(string storageDir) =>
        string.Equals(Path.GetFullPath(storageDir).TrimEnd('\\', '/'),
                      Path.GetFullPath(DefaultStorageDirectory).TrimEnd('\\', '/'), StringComparison.OrdinalIgnoreCase)
            ? ProseSettings.ForCurrentUser()
            : new JsonSettingsStore<ProseSettings>(Path.Combine(storageDir, "vault"));

    /// <summary>
    /// <c>--write-settings-manifest</c> (run.ps1's install step): imports today's Settings.json into
    /// MindAttic Vault if it isn't there yet, writes <c>settings.manifest.json</c> for Vault.Desktop,
    /// moves any own key copies into the shared named lists, prints the Vault folder and returns true.
    /// Returns false (doing nothing) when the switch is absent.
    /// </summary>
    public static bool TryRunManifestCommand(string[] args, TextWriter? output = null,
        string? storageDir = null, JsonSettingsStore<ProseSettings>? vaultStore = null)
    {
        if (args is null || !args.Any(a => string.Equals(a, MindAttic.Vault.Settings.Manifest.SettingsManifestCommand.Switch, StringComparison.OrdinalIgnoreCase)))
            return false;
        using var settings = new SettingsService(storageDir ?? DefaultStorageDirectory, vaultStore ?? ProseSettings.ForCurrentUser());
        settings.Flush();
        (output ?? Console.Out).WriteLine(settings.vaultStore.Directory);
        return true;
    }

    /// <summary>
    /// Loads the Vault values. The first time (no Vault file yet) imports today's values from
    /// Settings.json by name and saves them; <see cref="JsonSettingsStore{T}.LoadOrSeed"/> then writes
    /// the manifest. A read-only profile still starts, on the imported values.
    /// </summary>
    private void LoadVault()
    {
        var imported = ImportFromLegacy(data);
        try
        {
            if (!vaultStore.Exists()) vaultStore.Save(imported);
            vault = vaultStore.LoadOrSeed();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Serilog.Log.Warning(ex, "SettingsService: MindAttic Vault settings could not be read or written; using Settings.json values");
            vault = imported;
        }
        vaultBaseline = ToNode(vault);
    }

    /// <summary>The Vault values held by an old Settings.json snapshot (same property names).</summary>
    private static ProseSettings ImportFromLegacy(SettingsData legacy) =>
        JsonSerializer.Deserialize<ProseSettings>(JsonSerializer.Serialize(legacy, VaultDiffOptions), VaultDiffOptions) ?? new ProseSettings();

    private static JsonObject ToNode(ProseSettings s) =>
        JsonSerializer.SerializeToNode(s, VaultDiffOptions)?.AsObject() ?? new JsonObject();

    /// <summary>
    /// Prose's pre-LAMP own key copies (<c>prose-{provider}</c>) move into the shared named lists once per
    /// process and become Prose's assigned keys, so the same key stays in use (never throws).
    /// </summary>
    private static void ImportOwnKeyCopiesOnce()
    {
        if (UsesKeyOverrideFolder) return;
        if (Interlocked.Exchange(ref importedOwnKeys, 1) == 0) KeyImporter.ImportAppScopedFor(ProseSettings.AppId);
    }

    /// <summary>Raised after a Vault save from another process (e.g. Vault.Desktop) was applied here.</summary>
    public event Action? Changed;

    /// <summary>
    /// Watches the Vault settings file and applies changes made elsewhere (Vault.Desktop, another Prose
    /// process) to this instance, raising <see cref="ThemeChanged"/> and <see cref="Changed"/>.
    /// Idempotent; the default constructor calls it.
    /// </summary>
    public void Watch()
    {
        if (vaultWatcher is not null) return;
        try
        {
            Directory.CreateDirectory(vaultStore.Directory);
            var watcher = new FileSystemWatcher(vaultStore.Directory, vaultStore.FileName)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime,
            };
            FileSystemEventHandler changed = (_, _) =>
                (vaultDebounce ??= new Timer(_ =>
                {
                    try { ReloadVault(); }
                    catch (Exception ex) { Serilog.Log.Warning(ex, "SettingsService: applying a MindAttic Vault change failed"); }
                })).Change(300, Timeout.Infinite);
            watcher.Changed += changed;
            watcher.Created += changed;
            watcher.Renamed += (s, e) => changed(s, e);
            watcher.EnableRaisingEvents = true;
            vaultWatcher = watcher;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            Serilog.Log.Warning(ex, "SettingsService: cannot watch MindAttic Vault settings; changes apply on restart");
        }
    }

    /// <summary>
    /// Applies the Vault file's current values to this instance: first writes this process's own
    /// pending changes (so they aren't lost), then adopts the file. Raises <see cref="ThemeChanged"/>
    /// when the theme changed and <see cref="Changed"/> when anything did. Public for tests and for
    /// hosts that want to re-read on demand.
    /// </summary>
    public void ReloadVault()
    {
        string oldTheme, newTheme;
        bool any;
        var versionAtStart = Volatile.Read(ref changeVersion);
        lock (saveLock)
        {
            FlushVault();
            if (!vaultStore.Exists()) return;
            var fromDisk = vaultStore.Load();
            var diskNode = ToNode(fromDisk);
            // Setters change `vault` without this lock. One that ran since the flush above began
            // (it can wait up to 5s on the cross-process lock) would be discarded by adopting the
            // file here, and its queued save would then find nothing to write. Keep ours instead:
            // that save merges it onto the file and adopts the merge, other writers' values included.
            // (That save raises ThemeChanged/Changed for the external values it adopts: see FlushVault.)
            if (Volatile.Read(ref changeVersion) != versionAtStart)
            {
                any = false;
                oldTheme = newTheme = vault.Theme;
            }
            else
            {
                any = !JsonNode.DeepEquals(diskNode, ToNode(vault));
                oldTheme = vault.Theme;
                if (any) vault = fromDisk;
                vaultBaseline = diskNode;
                newTheme = vault.Theme;
            }
        }
        RaiseExternalChanges();
        if (!any) return;
        if (!string.Equals(oldTheme, newTheme, StringComparison.Ordinal)) RaiseThemeChanged(newTheme);
        RaiseChanged();
    }

    private void RaiseThemeChanged(string theme)
    {
        foreach (var h in ThemeChanged?.GetInvocationList() ?? []) try { ((Action<string>)h)(theme); } catch { }
    }

    private void RaiseChanged()
    {
        foreach (var h in Changed?.GetInvocationList() ?? []) try { ((Action)h)(); } catch { }
    }

    // Set by FlushVault (under saveLock) when it adopts values another writer put in the Vault file;
    // raised by the caller once the lock is released, so handlers never run inside it.
    private bool pendingExternalChange;
    private string? pendingExternalTheme;

    private void RaiseExternalChanges()
    {
        bool change;
        string? theme;
        lock (saveLock)
        {
            change = pendingExternalChange;
            theme = pendingExternalTheme;
            pendingExternalChange = false;
            pendingExternalTheme = null;
        }
        if (!change) return;
        if (theme is not null) RaiseThemeChanged(theme);
        RaiseChanged();
    }

    /// <summary>
    /// Writes the Vault properties this process changed since it last read or wrote the Vault file,
    /// merged onto the file's current content (same merge rule as Settings.json). Caller holds <see cref="saveLock"/>.
    /// </summary>
    private void FlushVault()
    {
        var versionAtSnapshot = Volatile.Read(ref changeVersion);
        var current = ToNode(vault);
        var changed = current.Where(kv => !vaultBaseline.TryGetPropertyValue(kv.Key, out var b) || !JsonNode.DeepEquals(kv.Value, b))
                             .Select(kv => kv.Key).ToList();
        if (changed.Count == 0) return;

        using var guard = new CrossProcessLock(vaultStore.FilePath);
        guard.Acquire(TimeSpan.FromSeconds(5));
        try
        {
            var disk = vaultStore.Exists() ? ToNode(vaultStore.Load()) : ToNode(new ProseSettings());
            foreach (var key in changed) disk[key] = current[key]?.DeepClone();
            var merged = disk.Deserialize<ProseSettings>(VaultDiffOptions) ?? vault;
            vaultStore.Save(merged);
            // Adopt the merge (picks up values other processes wrote) unless a setter ran meanwhile:
            // then keep ours, and baseline on our snapshot so its own queued flush writes it.
            if (Volatile.Read(ref changeVersion) == versionAtSnapshot)
            {
                // The merge overlays every key we changed, so anything else that differs from ours came from
                // another writer (Vault.Desktop, another process) - typically a change ReloadVault deferred to
                // this save because a setter raced it. Note it so the caller raises the events after the lock.
                var mergedNode = ToNode(merged);
                if (!JsonNode.DeepEquals(mergedNode, current))
                {
                    pendingExternalChange = true;
                    if (!string.Equals(vault.Theme, merged.Theme, StringComparison.Ordinal)) pendingExternalTheme = merged.Theme;
                }
                vault = merged;
                vaultBaseline = mergedNode;
            }
            else
                vaultBaseline = current;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Serilog.Log.Warning(ex, "SettingsService: could not write MindAttic Vault settings; the next save retries");
        }
    }

    private static string? AutoDetectCanonRoot()
    {
        // Azure App Service: set PROSE_DATA_ROOT in Application Settings
        var envRoot = Environment.GetEnvironmentVariable("PROSE_DATA_ROOT");
        if (!string.IsNullOrWhiteSpace(envRoot))
            return envRoot;

        // Walk up from the executing assembly to find the repo root
        var candidates = new[]
        {
            AppContext.BaseDirectory,   // Published app root — engine/data is co-deployed here on Azure
            @"D:\Projects\MindAttic\Prose",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Projects", "MindAttic", "Prose"),
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

    // Credential resolution, same order as before LAMP, with Prose's own tier now being its key
    // assignment in MindAttic Vault rather than a private "prose-*" copy:
    //   VaultConfiguration (cloud) → Prose's assigned keys (Vault.Desktop, app "Prose") →
    //   PROSE_* env var → the shared named list in its default order → legacy Settings.json field.
    // VaultConfiguration always wins (an admin-pushed override is never shadowed by a per-app key),
    // the env var still beats the shared list, and every Vault tier goes through KeySelector, so a
    // key another app (or Legion) found broken (401/403/quota) is skipped for the next healthy one.
    // MINDATTIC_LLM_CREDENTIALS pointing at a bare folder keeps the pre-LAMP chain (tests).
    private static string ResolveApiKey(string envVar, string providerId, string legacyValue)
    {
        var fromConfig = VaultConfiguration?[$"MindAttic:Vault:LLM:{providerId}:apiKey"];
        if (!string.IsNullOrWhiteSpace(fromConfig)) return fromConfig.Trim();

        if (UsesKeyOverrideFolder)
        {
            var ownKey = OwnProviderStore().GetKey(providerId);
            if (!string.IsNullOrWhiteSpace(ownKey)) return ownKey;
            if (Environment.GetEnvironmentVariable(envVar) is { Length: > 0 } env) return env;
            var fromStore = MindAtticCredentialStore.GetKey(providerId);
            return !string.IsNullOrEmpty(fromStore) ? fromStore : legacyValue;
        }

        var assigned = AssignedVaultKey(providerId);
        if (!string.IsNullOrWhiteSpace(assigned)) return assigned;
        if (Environment.GetEnvironmentVariable(envVar) is { Length: > 0 } v) return v;
        var shared = SharedVaultKey(providerId);
        return !string.IsNullOrEmpty(shared) ? shared : legacyValue;
    }

    /// <summary>The Vault key selector over the current user's named keys and shared key health.</summary>
    private static KeySelector VaultKeys(out NamedKeyStore keys)
    {
        keys = NamedKeyStore.ForRoamingRoot();
        return new KeySelector(new FileKeySource(keys), KeyHealthStore.ForLocalRoot());
    }

    /// <summary>
    /// Prose's usable assigned keys for the provider, in its Vault order (disabled, failed and cooling-down
    /// keys skipped); empty when Prose has no assignment or keys are redirected to a bare folder. For call
    /// paths that fail over through a key list themselves (the operator tool-calling loop).
    /// </summary>
    public static IReadOnlyList<string> AssignedVaultKeys(string providerId)
    {
        if (UsesKeyOverrideFolder) return Array.Empty<string>();
        try
        {
            var selector = VaultKeys(out var keys);
            if (keys.Assignments.Get(ProseSettings.AppId, providerId).Count == 0) return Array.Empty<string>();
            return selector.Plan(providerId, ProseSettings.AppId).Usable.Select(k => k.Value).ToList();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Text.Json.JsonException) { return Array.Empty<string>(); }
    }

    /// <summary>Prose's first usable assigned key for the provider, or null when Prose has no assignment.</summary>
    private static string? AssignedVaultKey(string providerId)
    {
        try
        {
            var selector = VaultKeys(out var keys);
            if (keys.Assignments.Get(ProseSettings.AppId, providerId).Count == 0) return null;
            return selector.GetActiveKey(providerId, ProseSettings.AppId)?.Value;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Text.Json.JsonException) { return null; }
    }

    /// <summary>
    /// The first healthy key in the provider's shared named list (aliases merged, e.g. xai/grok), or —
    /// when every key is flagged failed or cooling down — the first enabled one, as before LAMP.
    /// </summary>
    private static string? SharedVaultKey(string providerId)
    {
        try
        {
            var selector = VaultKeys(out var keys);
            return selector.GetActiveKey(providerId, appId: null)?.Value
                ?? keys.GetKeys(providerId).FirstOrDefault(k => !k.Disabled)?.Value;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Text.Json.JsonException) { return null; }
    }

    /// <summary>
    /// A key set through a SettingsService property (scripts, tests). Under Vault it goes into the shared
    /// named list as <c>Prose</c> and becomes Prose's active key (an identical value already there is
    /// reused, never duplicated), the LAMP equivalent of the old private copy. Keys are normally
    /// managed in MindAttic.Vault.Desktop. An empty value only clears the legacy field.
    /// </summary>
    private static void SetOwnKey(string providerId, string? value)
    {
        if (UsesKeyOverrideFolder) { OwnProviderStore().SetKey(providerId, value ?? ""); return; }
        if (string.IsNullOrWhiteSpace(value)) return;
        try { new KeyImporter(NamedKeyStore.ForRoamingRoot()).ImportValue(providerId, ProseSettings.AppId, value, ProseSettings.AppId); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException)
        {
            Serilog.Log.Warning("SettingsService: could not store a {Provider} key in MindAttic Vault: {Error}", providerId, ex.Message);
        }
    }

    // API keys resolve through MindAttic Vault's named key lists (see ResolveApiKey); the
    // non-credential app settings below read and write the Vault ProseSettings values.
    public string ApiKey
    {
        get => ResolveApiKey("PROSE_CLAUDE_API_KEY", "claude", data.ApiKey);
        set { SetOwnKey("claude", value); data.ApiKey = value; ScheduleSave(); }
    }
    public string Model { get => vault.Model; set { vault.Model = value; ScheduleSave(); } }
    /// <summary>Raised when the theme changes so layout components can update without a full reload.</summary>
    public event Action<string>? ThemeChanged;
    public string Theme { get => vault.Theme; set { vault.Theme = value; ScheduleSave(); RaiseThemeChanged(value); } }
    public string CanonRootPath { get => vault.CanonRootPath; set { vault.CanonRootPath = value; ScheduleSave(); } }
    public int MaxTokens { get => vault.MaxTokens; set { vault.MaxTokens = value; ScheduleSave(); } }
    public string ElevenLabsApiKey
    {
        get => ResolveApiKey("PROSE_ELEVENLABS_API_KEY", "elevenlabs", data.ElevenLabsApiKey);
        set { SetOwnKey("elevenlabs", value); data.ElevenLabsApiKey = value; ScheduleSave(); }
    }
    public string ElevenLabsVoiceId { get => vault.ElevenLabsVoiceId; set { vault.ElevenLabsVoiceId = value; ScheduleSave(); } }
    public string NarratorVoiceName { get => vault.NarratorVoiceName; set { vault.NarratorVoiceName = value; ScheduleSave(); } }
    public string TtsModel { get => vault.TtsModel; set { vault.TtsModel = value; ScheduleSave(); } }
    public double TtsStability { get => vault.TtsStability; set { vault.TtsStability = value; ScheduleSave(); } }
    public double TtsSimilarityBoost { get => vault.TtsSimilarityBoost; set { vault.TtsSimilarityBoost = value; ScheduleSave(); } }
    public double TtsStyle { get => vault.TtsStyle; set { vault.TtsStyle = value; ScheduleSave(); } }

    /// <summary>Final delivery format/quality for a published audiobook. The
    /// source is always fetched at the highest fidelity the ElevenLabs tier
    /// allows (lossless <c>pcm_44100</c> when available); this controls only how
    /// the combined track is encoded for the file the user receives. One of the
    /// keys in <see cref="AudiobookFormats"/>. Default: 320 kbps MP3.</summary>
    public string AudiobookFormat
    {
        get => string.IsNullOrWhiteSpace(vault.AudiobookFormat) ? "mp3_320" : vault.AudiobookFormat;
        set { vault.AudiobookFormat = value; ScheduleSave(); }
    }

    /// <summary>Where Publish drops the combined audio file so it's easy to
    /// find. Empty = Desktop. The in-app player still serves an internal copy;
    /// this is the user-facing export.</summary>
    public string PublishOutputDirectory
    {
        get => vault.PublishOutputDirectory;
        set { vault.PublishOutputDirectory = value ?? ""; ScheduleSave(); }
    }

    /// <summary>Resolve the effective publish output directory: the configured
    /// path, or the Desktop when unset. Always returns an absolute, existing
    /// directory (creates it if needed).</summary>
    public string ResolvePublishOutputDirectory()
    {
        var dir = vault.PublishOutputDirectory;
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
        get => vault.PublishExportDirectory;
        set { vault.PublishExportDirectory = value ?? ""; ScheduleSave(); }
    }

    /// <summary>Returns the export base directory for the given universe slug.
    /// <c>PublishExportDirectory</c> is the shared root (falls back to Desktop).
    /// The <c>UniverseExportDirectories[slug]</c> entry fills in the rest of the
    /// path: a <em>relative</em> entry (e.g. "GLMZ") is combined onto the root,
    /// while an <em>absolute</em> entry (e.g. "R:\…\GLMZ") is used verbatim for
    /// back-compat. With no entry, the bare root is returned.</summary>
    public string GetExportDirectory(string? universeSlug)
    {
        var global = (vault.PublishExportDirectory ?? string.Empty).Trim().Trim('"', '\'').Trim();
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
    public int TtsPauseSectionMs { get => vault.TtsPauseSectionMs; set { vault.TtsPauseSectionMs = value; ScheduleSave(); } }
    /// <summary>Silence (ms) inserted after a beat whose SceneType is
    /// <c>scene-end</c> — the last paragraph before a <c>---</c> divider.
    /// Medium pause; "scene changes." Default 1000.</summary>
    public int TtsPauseSceneMs { get => vault.TtsPauseSceneMs; set { vault.TtsPauseSceneMs = value; ScheduleSave(); } }
    /// <summary>Silence (ms) inserted between paragraph beats that end in a
    /// hard sentence terminator (<c>.</c>, <c>!</c>, <c>?</c>). The "breath
    /// between paragraphs" budget. Default 400.</summary>
    public int TtsPauseParagraphMs { get => vault.TtsPauseParagraphMs; set { vault.TtsPauseParagraphMs = value; ScheduleSave(); } }
    /// <summary>Silence (ms) inserted between paragraph beats whose prose
    /// ends mid-sentence (comma, em-dash, colon, no terminator). Short pause
    /// that keeps reading momentum across a paragraph break. Default 200.</summary>
    public int TtsPauseContinuationMs { get => vault.TtsPauseContinuationMs; set { vault.TtsPauseContinuationMs = value; ScheduleSave(); } }

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
            Label           = string.IsNullOrEmpty(vault.NarratorVoiceName) ? "Narrator" : vault.NarratorVoiceName,
            VoiceId         = vault.ElevenLabsVoiceId,
            Model           = vault.TtsModel,
            Stability       = vault.TtsStability,
            SimilarityBoost = vault.TtsSimilarityBoost,
            Style           = vault.TtsStyle,
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
    public bool TtsUseAudioTags { get => vault.TtsUseAudioTags; set { vault.TtsUseAudioTags = value; ScheduleSave(); } }
    public string OpenAiApiKey
    {
        get => ResolveApiKey("PROSE_OPENAI_API_KEY", "openai", data.OpenAiApiKey);
        set { SetOwnKey("openai", value); data.OpenAiApiKey = value; ScheduleSave(); }
    }
    public string OpenAiModel { get => vault.OpenAiModel; set { vault.OpenAiModel = value; ScheduleSave(); } }
    public string ActiveLlmProvider { get => vault.ActiveLlmProvider; set { vault.ActiveLlmProvider = value; ScheduleSave(); } }
    /// <summary>
    /// CSV, priority order. When the active provider (<see cref="ActiveLlmProvider"/> or a
    /// per-call override) fails, <see cref="LlmRouter"/> walks this list and tries the next
    /// entry instead of throwing — so a Claude outage degrades to another backend
    /// automatically. Ids not present in LlmRouter's provider map are skipped.
    /// </summary>
    public string ActiveLlmProviderChain { get => vault.ActiveLlmProviderChain; set { vault.ActiveLlmProviderChain = value; ScheduleSave(); } }
    public int EditorFontSize { get => vault.EditorFontSize; set { vault.EditorFontSize = value; ScheduleSave(); } }
    public int AutoSaveIntervalMs { get => vault.AutoSaveIntervalMs; set { vault.AutoSaveIntervalMs = value; ScheduleSave(); } }
    public string GeminiApiKey
    {
        get => ResolveApiKey("PROSE_GEMINI_API_KEY", "gemini", data.GeminiApiKey);
        set { SetOwnKey("gemini", value); data.GeminiApiKey = value; ScheduleSave(); }
    }
    public string DeepSeekApiKey
    {
        get => ResolveApiKey("PROSE_DEEPSEEK_API_KEY", "deepseek", data.DeepSeekApiKey);
        set { SetOwnKey("deepseek", value); data.DeepSeekApiKey = value; ScheduleSave(); }
    }
    public string MistralApiKey
    {
        get => ResolveApiKey("PROSE_MISTRAL_API_KEY", "mistral", data.MistralApiKey);
        set { SetOwnKey("mistral", value); data.MistralApiKey = value; ScheduleSave(); }
    }
    public string KimiApiKey
    {
        get => ResolveApiKey("PROSE_KIMI_API_KEY", "kimi", data.KimiApiKey);
        set { SetOwnKey("kimi", value); data.KimiApiKey = value; ScheduleSave(); }
    }
    public string KimiModel { get => vault.KimiModel; set { vault.KimiModel = value; ScheduleSave(); } }
    public string PerplexityApiKey
    {
        get => ResolveApiKey("PROSE_PERPLEXITY_API_KEY", "perplexity", data.PerplexityApiKey);
        set { SetOwnKey("perplexity", value); data.PerplexityApiKey = value; ScheduleSave(); }
    }
    public string PerplexityModel { get => vault.PerplexityModel; set { vault.PerplexityModel = value; ScheduleSave(); } }
    public string GrokApiKey
    {
        get => ResolveApiKey("PROSE_GROK_API_KEY", "xai", data.GrokApiKey);
        set { SetOwnKey("xai", value); data.GrokApiKey = value; ScheduleSave(); }
    }
    public string GroqApiKey
    {
        get => ResolveApiKey("PROSE_GROQ_API_KEY", "groq", data.GroqApiKey);
        set { SetOwnKey("groq", value); data.GroqApiKey = value; ScheduleSave(); }
    }
    public string TogetherApiKey
    {
        get => ResolveApiKey("PROSE_TOGETHER_API_KEY", "together", data.TogetherApiKey);
        set { SetOwnKey("together", value); data.TogetherApiKey = value; ScheduleSave(); }
    }
    public string OpenRouterApiKey
    {
        get => ResolveApiKey("PROSE_OPENROUTER_API_KEY", "openrouter", data.OpenRouterApiKey);
        set { SetOwnKey("openrouter", value); data.OpenRouterApiKey = value; ScheduleSave(); }
    }
    public string FireworksApiKey
    {
        get => ResolveApiKey("PROSE_FIREWORKS_API_KEY", "fireworks", data.FireworksApiKey);
        set { SetOwnKey("fireworks", value); data.FireworksApiKey = value; ScheduleSave(); }
    }
    public string CohereApiKey
    {
        get => ResolveApiKey("PROSE_COHERE_API_KEY", "cohere", data.CohereApiKey);
        set { SetOwnKey("cohere", value); data.CohereApiKey = value; ScheduleSave(); }
    }
    public string IdeogramApiKey
    {
        get => ResolveApiKey("PROSE_IDEOGRAM_API_KEY", "ideogram", data.IdeogramApiKey);
        set { SetOwnKey("ideogram", value); data.IdeogramApiKey = value; ScheduleSave(); }
    }
    public string FalApiKey
    {
        get => ResolveApiKey("PROSE_FAL_API_KEY", "fal", data.FalApiKey);
        set { SetOwnKey("fal", value); data.FalApiKey = value; ScheduleSave(); }
    }
    /// <summary>Stability AI API key — https://api.stability.ai.
    ///
    /// <para>Its only consumer, the "stability" cover-image provider, was deleted 2026-09-13 with
    /// the rest of the cover pipeline. The key itself is KEPT: it is ordinary provider credential
    /// plumbing shared with the credential store and its migration path, and Stability also offers
    /// the image2video endpoints the BookTok feature uses. Removing a key from the store's
    /// migration is a different and riskier change than deleting a feature.</para></summary>
    public string StabilityApiKey
    {
        get => ResolveApiKey("PROSE_STABILITY_API_KEY", "stability", data.StabilityApiKey);
        set { SetOwnKey("stability", value); data.StabilityApiKey = value; ScheduleSave(); }
    }
    /// <summary>Kling AI (image2video) API key, stored as "{accessKey}:{secretKey}" — Kling
    /// authenticates with a short-lived JWT minted from that pair, not a plain bearer token.
    /// Used by BookTokVideoService's "kling" provider.</summary>
    public string KlingApiKey
    {
        get => ResolveApiKey("PROSE_KLING_API_KEY", "kling", data.KlingApiKey);
        set { SetOwnKey("kling", value); data.KlingApiKey = value; ScheduleSave(); }
    }
    /// <summary>Runway (Gen-4 image_to_video) API key. Used by BookTokVideoService's
    /// "runway" provider.</summary>
    public string RunwayApiKey
    {
        get => ResolveApiKey("PROSE_RUNWAY_API_KEY", "runway", data.RunwayApiKey);
        set { SetOwnKey("runway", value); data.RunwayApiKey = value; ScheduleSave(); }
    }
    public string GeminiModel { get => vault.GeminiModel; set { vault.GeminiModel = value; ScheduleSave(); } }
    public string DeepSeekModel { get => vault.DeepSeekModel; set { vault.DeepSeekModel = value; ScheduleSave(); } }
    public string MistralModel { get => vault.MistralModel; set { vault.MistralModel = value; ScheduleSave(); } }
    public string GrokModel { get => vault.GrokModel; set { vault.GrokModel = value; ScheduleSave(); } }
    public string GroqModel { get => vault.GroqModel; set { vault.GroqModel = value; ScheduleSave(); } }
    public string TogetherModel { get => vault.TogetherModel; set { vault.TogetherModel = value; ScheduleSave(); } }
    public string OpenRouterModel { get => vault.OpenRouterModel; set { vault.OpenRouterModel = value; ScheduleSave(); } }
    public string FireworksModel { get => vault.FireworksModel; set { vault.FireworksModel = value; ScheduleSave(); } }
    public string CohereModel { get => vault.CohereModel; set { vault.CohereModel = value; ScheduleSave(); } }
    public string MapService { get => vault.MapService; set { vault.MapService = value; ScheduleSave(); } }
    public string MapAppId { get => Env("PROSE_MAP_APP_ID", vault.MapAppId); set { vault.MapAppId = value; ScheduleSave(); } }
    public string MapApiKey
    {
        get => ResolveApiKey("PROSE_MAP_API_KEY", "here-maps", data.MapApiKey);
        set { SetOwnKey("here-maps", value); data.MapApiKey = value; ScheduleSave(); }
    }
    public string GoogleMapsApiKey
    {
        get => ResolveApiKey("PROSE_GOOGLE_MAPS_API_KEY", "google-maps", data.GoogleMapsApiKey);
        set { SetOwnKey("google-maps", value); data.GoogleMapsApiKey = value; ScheduleSave(); }
    }
    public string MapMode { get => vault.MapMode; set { vault.MapMode = value; ScheduleSave(); } }
    public string TimestampFormat { get => vault.TimestampFormat; set { vault.TimestampFormat = value; ScheduleSave(); } }
    public string TimezoneId { get => vault.TimezoneId; set { vault.TimezoneId = value; ScheduleSave(); } }
    public string FontFamily { get => vault.FontFamily; set { vault.FontFamily = value; ScheduleSave(); } }
    public bool RepoListOnRight { get => vault.RepoListOnRight; set { vault.RepoListOnRight = value; ScheduleSave(); } }
    public bool EnablePlainTextNer { get => vault.EnablePlainTextNer; set { vault.EnablePlainTextNer = value; ScheduleSave(); } }
    public bool SaveStoriesAsMarkdown { get => vault.SaveStoriesAsMarkdown; set { vault.SaveStoriesAsMarkdown = value; ScheduleSave(); } }
    public bool DocxIncludeToc { get => vault.DocxIncludeToc; set { vault.DocxIncludeToc = value; ScheduleSave(); } }

    /// <summary>Master switch for the Doc Context Stack injection into prose-generation prompts.
    /// Default ON — injects the node bible, voice register, and topic docs into every beat prompt.
    /// Dry-run (`--doc-context`) + MCP tools work regardless of this flag.</summary>
    public bool DocContextEnabled { get => vault.DocContextEnabled; set { vault.DocContextEnabled = value; ScheduleSave(); } }

    /// <summary>When ON, ProseWriterRouter captures the FULL DCM working-set (not budget-clipped)
    /// per beat into ContextTelemetryService for Gantt visualization export. Default OFF (zero
    /// overhead until you opt in). Use <c>prose --dcm-viz</c> to generate the .htm without a full prose run.</summary>
    public bool DcmLoggingEnabled { get => vault.DcmLoggingEnabled; set { vault.DcmLoggingEnabled = value; ScheduleSave(); } }

    /// <summary>When ON, CommandNarrator (Prose.Hub) follows every command's existing log line with
    /// a Haiku-generated plain-English gloss of what it did. Default OFF. Toggle with
    /// <c>prose --hub-narration on|off</c> — takes effect on the running Hub immediately, no restart.</summary>
    public bool HubNarrationEnabled { get => vault.HubNarrationEnabled; set { vault.HubNarrationEnabled = value; ScheduleSave(); } }

    /// <summary>COROBS master switch (RFC 0016, <c>prose --corobs enable|disable</c>, <c>/corobs on|off</c>).
    /// Default OFF. When off every COROBS entry point answers <c>disabled</c> and writes nothing, the
    /// wiki's Corobs panel is hidden, and nothing is deleted — turning it back on needs no rebuild.</summary>
    public bool CorobsEnabled { get => vault.CorobsEnabled; set { vault.CorobsEnabled = value; ScheduleSave(); } }

    // ── Review voting ──────────────────────────────────────────────────────────
    /// <summary>Default number of cheap score-only ballots per sampled node review (--ballots).</summary>
    public int ReviewBallots { get => Math.Max(1, vault.ReviewBallots); set { vault.ReviewBallots = Math.Max(1, value); ScheduleSave(); } }
    /// <summary>Default number of full prose upgrades per sampled run (--prose).</summary>
    public int ReviewProse { get => Math.Max(0, vault.ReviewProse); set { vault.ReviewProse = Math.Max(0, value); ScheduleSave(); } }
    /// <summary>Default segment-study panel size (--panel / --study).</summary>
    public int ReviewPanel { get => Math.Max(1, vault.ReviewPanel); set { vault.ReviewPanel = Math.Max(1, value); ScheduleSave(); } }
    /// <summary>Default reader count for full / census runs (--readers).</summary>
    public int ReviewReaders { get => Math.Max(1, vault.ReviewReaders); set { vault.ReviewReaders = Math.Max(1, value); ScheduleSave(); } }
    /// <summary>Provider that synthesises the reader-synopsis after a review run.</summary>
    public string ReviewJudgeProvider { get => vault.ReviewJudgeProvider; set { vault.ReviewJudgeProvider = value; ScheduleSave(); } }
    /// <summary>Comma-separated provider IDs allowed to cast ballots (e.g. "claude,openai,gemini,deepseek").</summary>
    public string ReviewAllowedProviders { get => vault.ReviewAllowedProviders; set { vault.ReviewAllowedProviders = value; ScheduleSave(); } }
    /// <summary>Maximum simultaneous LLM calls during a review run.</summary>
    public int ReviewMaxConcurrency { get => Math.Max(1, Math.Min(50, vault.ReviewMaxConcurrency)); set { vault.ReviewMaxConcurrency = Math.Max(1, Math.Min(50, value)); ScheduleSave(); } }

    // ── Reader-Proxy QA (docs/READER-QA.md) ──────────────────────────────────────
    /// <summary>Comma-separated provider IDs Reader-Proxy QA juries draw from — cross-FAMILY
    /// diversity is the point (correlated-error research: same-family voters ≈ one vote).
    /// Providers without keys or failing their liveness ping are skipped automatically;
    /// juries degrade gracefully to whatever is alive.</summary>
    public string ReaderQaJuryProviders { get => vault.ReaderQaJuryProviders; set { vault.ReaderQaJuryProviders = value; ScheduleSave(); } }
    /// <summary>JSON array declaring OpenAI-compatible jury providers OUTSIDE the Legion
    /// catalog (Kimi, Grok, Mistral, …): [{"id","baseUrl","cheapModel","model?","label?",
    /// "inPerMtok?","cacheWritePerMtok?","cacheReadPerMtok?","outPerMtok?"}]. Keys resolve
    /// from MindAtticCredentialStore by id. Adding a family is a settings edit, not code.</summary>
    public string ExtraJuryProvidersJson { get => vault.ExtraJuryProvidersJson; set { vault.ExtraJuryProvidersJson = value; ScheduleSave(); } }
    /// <summary>The cheap "median reader" instrument for comprehension probes. Its GENUINE
    /// misreadings are the signal — do not upgrade this to a strong model, that defeats
    /// the instrument (a strong model reads too well to proxy a casual reader).</summary>
    public string ComprehensionProbeModel { get => vault.ComprehensionProbeModel; set { vault.ComprehensionProbeModel = value; ScheduleSave(); } }
    /// <summary>Arbiter that checks each candidate misreading against the chapter text
    /// (separates reader-plausible confusion from probe hallucination).</summary>
    public string ComprehensionArbiterModel { get => vault.ComprehensionArbiterModel; set { vault.ComprehensionArbiterModel = value; ScheduleSave(); } }

    // ── Local-LLM review (--local) ───────────────────────────────────────────────
    /// <summary>OpenAI-compatible chat-completions endpoint of the local inference server
    /// (Ollama default). Only used by <c>--local</c> node reviews; cloud reviews never touch it.</summary>
    public string LocalReviewBaseUrl { get => vault.LocalReviewBaseUrl; set { vault.LocalReviewBaseUrl = value; ScheduleSave(); } }
    /// <summary>Local model tag used by <c>--local</c> reviews (e.g. an Ollama tag with a baked-in num_ctx).</summary>
    public string LocalReviewModel { get => vault.LocalReviewModel; set { vault.LocalReviewModel = value; ScheduleSave(); } }
    /// <summary>Human label for WHICH local backend a <c>--local</c> run used (e.g. "vast", "runpod").
    /// Stamped as the report "brain" so vast.ai / RunPod / Ollama runs write SEPARATE report files
    /// (<c>… reviews (vast).htm</c> vs <c>(runpod).htm</c>) instead of all colliding under "(local)".
    /// Empty = auto-derive from <see cref="LocalReviewBaseUrl"/> host, falling back to "local".</summary>
    public string LocalReviewLabel { get => vault.LocalReviewLabel; set { vault.LocalReviewLabel = value; ScheduleSave(); } }
    /// <summary>Bearer token for the local/self-hosted review endpoint. Empty for a bare
    /// localhost Ollama (which ignores auth); set it to the API key when
    /// <see cref="LocalReviewBaseUrl"/> points at a SECURED remote GPU (RunPod/vLLM/etc.),
    /// so "local" reviews can run on a rented machine instead of your own VRAM.</summary>
    public string LocalReviewApiKey { get => data.LocalReviewApiKey; set { data.LocalReviewApiKey = value; ScheduleSave(); } }
    /// <summary>Max simultaneous local generations — kept low because a single GPU can only
    /// run a few large-model generations at once before spilling / OOM.</summary>
    public int LocalReviewMaxConcurrency { get => Math.Max(1, Math.Min(16, vault.LocalReviewMaxConcurrency)); set { vault.LocalReviewMaxConcurrency = Math.Max(1, Math.Min(16, value)); ScheduleSave(); } }
    /// <summary>The local model's context window in TOKENS (the Ollama tag's num_ctx). The review
    /// engine uses this to size segments so an oversized node is chunked to FIT the local window
    /// instead of being silently truncated (which drops the system prompt and fails every ballot).
    /// Cloud reviews ignore this. Default 16384 — raise it to match a model rebuilt with a larger
    /// num_ctx, and big nodes will segment into fewer, larger chunks.</summary>
    public int LocalReviewContextTokens { get => Math.Max(4096, vault.LocalReviewContextTokens); set { vault.LocalReviewContextTokens = Math.Max(4096, value); ScheduleSave(); } }

    // ── Hub HTTP auth (portable-writing-service plan, Phase 1) ───────────────────
    /// <summary>Shared secret checked on the Hub's sensitive HTTP endpoints (generic CLI/MCP
    /// dispatch, universe import, outbox) via the <c>X-Prose-Key</c> header. Empty until the Hub
    /// generates one at first startup; Cli/Mcp read the same value from this shared file, so
    /// nothing needs manual configuration for the common single-machine case.</summary>
    public string HubApiKey { get => data.HubApiKey; set { data.HubApiKey = value; ScheduleSave(); } }

    // ── Local-LLM generation (--local prose) ─────────────────────────────────────
    /// <summary>OpenAI-compatible chat-completions endpoint for local prose generation
    /// (Ollama, vLLM, RunPod, etc.). Used by <c>--local</c> beat/node generation;
    /// cloud generation never reads it. Empty = local prose generation disabled.</summary>
    public string LocalLlmBaseUrl { get => vault.LocalLlmBaseUrl; set { vault.LocalLlmBaseUrl = value; ScheduleSave(); } }
    /// <summary>Bearer token for the local generation endpoint. Empty for bare localhost Ollama
    /// (which ignores auth); set to the pod token when pointing at a secured remote GPU (RunPod/vLLM).</summary>
    public string LocalLlmApiKey { get => data.LocalLlmApiKey; set { data.LocalLlmApiKey = value; ScheduleSave(); } }
    /// <summary>Model tag used for local prose generation (e.g. "qwen2.5-32b").</summary>
    public string LocalLlmModel { get => vault.LocalLlmModel; set { vault.LocalLlmModel = value; ScheduleSave(); } }

    // ── Local-LLM embeddings ─────────────────────────────────────────────────────
    /// <summary>Full URL to an OpenAI-compatible /v1/embeddings endpoint on the local/remote GPU
    /// (e.g. <c>https://&lt;runpod&gt;/v1/embeddings</c>). When set, EmbeddingService routes ALL
    /// embed calls here instead of OpenAI — no OpenAI key needed for local review runs.</summary>
    public string LocalEmbeddingBaseUrl { get => vault.LocalEmbeddingBaseUrl; set { vault.LocalEmbeddingBaseUrl = value; ScheduleSave(); } }
    /// <summary>Bearer token for the local embedding endpoint. Empty = no auth header sent (bare Ollama).</summary>
    public string LocalEmbeddingApiKey  { get => data.LocalEmbeddingApiKey;  set { data.LocalEmbeddingApiKey  = value; ScheduleSave(); } }
    /// <summary>Model tag for the local embedding endpoint (e.g. "Qwen/Qwen3-Embedding-0.6B").</summary>
    public string LocalEmbeddingModel   { get => vault.LocalEmbeddingModel;   set { vault.LocalEmbeddingModel   = value; ScheduleSave(); } }

    /// <summary>vast.ai REST API key for <c>prose --gpu</c> (start/stop/destroy the rented review box).
    /// Read straight from the shared MindAttic credential vault — <c>VAST_API_KEY</c> env, then
    /// %APPDATA%/MindAttic/LLM/<c>vast.json</c>, then the <c>vast</c> entry in <c>providers.json</c>,
    /// then cloud config. The standard LLM resolver only knows registered providers, so a non-LLM
    /// credential like this reads the vault files directly. Not an LLM key.</summary>
    public string VastApiKey => ResolveVaultKey("VAST_API_KEY", "vast");

    /// <summary>RunPod REST API key for <c>prose --runpod</c> (status/stop/start/terminate the rented
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
    public bool ReviewAutoRunEnabled { get => vault.ReviewAutoRunEnabled; set { vault.ReviewAutoRunEnabled = value; ScheduleSave(); } }
    /// <summary>When true, CanonGroundingService fires post-write on every beat to flag PROVISIONAL-ENTITY findings. Off by default — opt in to avoid extra LLM cost.</summary>
    public bool AutoCanonGrounding { get => vault.AutoCanonGrounding; set { vault.AutoCanonGrounding = value; ScheduleSave(); } }
    /// <summary>When true, SceneContextAssembler.HarvestRevealedDetailsAsync fires post-write to propose XRAY-REVEAL findings. Off by default.</summary>
    public bool AutoHarvestRevealedDetails { get => vault.AutoHarvestRevealedDetails; set { vault.AutoHarvestRevealedDetails = value; ScheduleSave(); } }
    // AutoGenerateCoverOnExport removed 2026-09-13 with the cover-generation pipeline it gated.
    // Cover art is a manual step; there is no longer anything for a flag to switch on.

    // SMTP — outbound email for password reset codes
    public string SmtpHost { get => Env("PROSE_SMTP_HOST", vault.SmtpHost); set { vault.SmtpHost = value; ScheduleSave(); } }
    public int SmtpPort { get => int.TryParse(Env("PROSE_SMTP_PORT", ""), out var p) ? p : vault.SmtpPort; set { vault.SmtpPort = value; ScheduleSave(); } }
    public string SmtpUsername { get => Env("PROSE_SMTP_USERNAME", vault.SmtpUsername); set { vault.SmtpUsername = value; ScheduleSave(); } }
    public string SmtpPassword { get => Env("PROSE_SMTP_PASSWORD", data.SmtpPassword); set { data.SmtpPassword = value; ScheduleSave(); } }
    public string SmtpFrom { get => Env("PROSE_SMTP_FROM", vault.SmtpFrom); set { vault.SmtpFrom = value; ScheduleSave(); } }
    public bool SmtpEnableSsl { get => vault.SmtpEnableSsl; set { vault.SmtpEnableSsl = value; ScheduleSave(); } }

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
        // A blank or unknown TimezoneId (hand-edited Settings.json, another OS's id) must not
        // make every timestamp display throw; fall back to the machine's local zone.
        TimeZoneInfo tz;
        try { tz = TimeZoneInfo.FindSystemTimeZoneById(TimezoneId); }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException or ArgumentException)
        { tz = TimeZoneInfo.Local; }
        var converted = TimeZoneInfo.ConvertTime(timestamp, tz);
        return converted.ToString(TimestampFormat);
    }

    /// <summary>Snapshot current settings as the default baseline for future resets.</summary>
    public void SaveAsDefaults()
    {
        // The snapshot carries the Vault values too (same field names), so a reset restores them.
        var node = JsonSerializer.SerializeToNode(data, JsonDefaults.Indented)?.AsObject() ?? new JsonObject();
        foreach (var (key, value) in ToNode(vault)) node[key] = value?.DeepClone();
        File.WriteAllText(defaultsPath, node.ToJsonString(JsonDefaults.Indented));
    }

    /// <summary>Reset all settings to the saved defaults snapshot (includes secrets).
    /// Also overwrites the shared MindAttic credential store <em>and</em> this app's own
    /// scoped tier (Prose's Vault key assignment) with the reset values, so the next read
    /// doesn't pick up a stale "first-stop" key from either tier — <see cref="ResolveApiKey"/>
    /// checks the own-scoped key before the shared store, so leaving it behind would make a
    /// reset silently no-op for any provider with a Prose-scoped override on file. The shared
    /// store write intentionally affects every MindAttic app — resetting one app's credentials
    /// is a fresh slate.</summary>
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
        vault = ImportFromLegacy(data);
        Interlocked.Increment(ref changeVersion);
        Flush();
        SyncCredentialStoreFromData();
    }

    /// <summary>Reset's version of <see cref="SetOwnKey"/>: an empty value also drops Prose's Vault assignment.</summary>
    private static void ResetOwnKey(string providerId, string value)
    {
        if (UsesKeyOverrideFolder || !string.IsNullOrWhiteSpace(value)) { SetOwnKey(providerId, value); return; }
        try { NamedKeyStore.ForRoamingRoot().Assignments.Set(ProseSettings.AppId, providerId, Array.Empty<string>()); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException) { }
    }

    private void SyncCredentialStoreFromData()
    {
        MindAtticCredentialStore.SetKey("claude",       data.ApiKey);
        MindAtticCredentialStore.SetKey("openai",      data.OpenAiApiKey);
        MindAtticCredentialStore.SetKey("gemini",      data.GeminiApiKey);
        MindAtticCredentialStore.SetKey("ideogram",    data.IdeogramApiKey);
        MindAtticCredentialStore.SetKey("fal",         data.FalApiKey);
        MindAtticCredentialStore.SetKey("stability",   data.StabilityApiKey);
        MindAtticCredentialStore.SetKey("kling",       data.KlingApiKey);
        MindAtticCredentialStore.SetKey("runway",      data.RunwayApiKey);
        MindAtticCredentialStore.SetKey("deepseek",    data.DeepSeekApiKey);
        MindAtticCredentialStore.SetKey("mistral",     data.MistralApiKey);
        MindAtticCredentialStore.SetKey("kimi",        data.KimiApiKey);
        MindAtticCredentialStore.SetKey("perplexity",  data.PerplexityApiKey);
        MindAtticCredentialStore.SetKey("xai",         data.GrokApiKey);
        MindAtticCredentialStore.SetKey("groq",        data.GroqApiKey);
        MindAtticCredentialStore.SetKey("together",    data.TogetherApiKey);
        MindAtticCredentialStore.SetKey("openrouter",  data.OpenRouterApiKey);
        MindAtticCredentialStore.SetKey("fireworks",   data.FireworksApiKey);
        MindAtticCredentialStore.SetKey("cohere",      data.CohereApiKey);
        MindAtticCredentialStore.SetKey("elevenlabs",  data.ElevenLabsApiKey);
        MindAtticCredentialStore.SetKey("here-maps",   data.MapApiKey);
        MindAtticCredentialStore.SetKey("google-maps", data.GoogleMapsApiKey);

        ResetOwnKey("claude",       data.ApiKey);
        ResetOwnKey("openai",       data.OpenAiApiKey);
        ResetOwnKey("gemini",       data.GeminiApiKey);
        ResetOwnKey("ideogram",     data.IdeogramApiKey);
        ResetOwnKey("fal",          data.FalApiKey);
        ResetOwnKey("stability",    data.StabilityApiKey);
        ResetOwnKey("kling",        data.KlingApiKey);
        ResetOwnKey("runway",       data.RunwayApiKey);
        ResetOwnKey("deepseek",     data.DeepSeekApiKey);
        ResetOwnKey("mistral",      data.MistralApiKey);
        ResetOwnKey("kimi",         data.KimiApiKey);
        ResetOwnKey("perplexity",   data.PerplexityApiKey);
        ResetOwnKey("xai",          data.GrokApiKey);
        ResetOwnKey("groq",         data.GroqApiKey);
        ResetOwnKey("together",     data.TogetherApiKey);
        ResetOwnKey("openrouter",   data.OpenRouterApiKey);
        ResetOwnKey("fireworks",    data.FireworksApiKey);
        ResetOwnKey("cohere",       data.CohereApiKey);
        ResetOwnKey("elevenlabs",   data.ElevenLabsApiKey);
        ResetOwnKey("here-maps",    data.MapApiKey);
        ResetOwnKey("google-maps",  data.GoogleMapsApiKey);
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

        MigrateIfMissing("claude",       data.ApiKey);
        MigrateIfMissing("openai",      data.OpenAiApiKey);
        MigrateIfMissing("gemini",      data.GeminiApiKey);
        MigrateIfMissing("ideogram",    data.IdeogramApiKey);
        MigrateIfMissing("fal",         data.FalApiKey);
        MigrateIfMissing("stability",   data.StabilityApiKey);
        MigrateIfMissing("kling",       data.KlingApiKey);
        MigrateIfMissing("runway",      data.RunwayApiKey);
        MigrateIfMissing("deepseek",    data.DeepSeekApiKey);
        MigrateIfMissing("mistral",     data.MistralApiKey);
        MigrateIfMissing("kimi",        data.KimiApiKey);
        MigrateIfMissing("perplexity",  data.PerplexityApiKey);
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

    /// <summary>
    /// One-time upgrade for a real, already-persisted <c>Settings.json</c> written before Legion's
    /// provider ids were unified: <c>"claude-team"</c> (a Claude Code Team subscription OAuth
    /// token used as a substitute API credential) never actually worked for calling the public
    /// Anthropic API and has been removed outright; <c>"claude-api"</c> is now just
    /// <c>"claude"</c>. Rewrites <see cref="SettingsData.ActiveLlmProvider"/>,
    /// <see cref="SettingsData.ActiveLlmProviderChain"/>, <see cref="SettingsData.ReviewAllowedProviders"/>,
    /// and <see cref="SettingsData.ReaderQaJuryProviders"/> in place (deduplicating the chain/list
    /// fields, since both old ids may already appear side by side) and saves once if anything
    /// changed. Idempotent — a no-op on every subsequent load.
    /// </summary>
    private void NormalizeLegacyClaudeProviderIds()
    {
        static string NormalizeSingle(string providerId) =>
            providerId is "claude-api" or "claude-team" ? "claude" : providerId;

        static string NormalizeList(string csv)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var result = new List<string>();
            foreach (var raw in csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var id = NormalizeSingle(raw);
                if (seen.Add(id)) result.Add(id);
            }
            return string.Join(",", result);
        }

        var changed = false;

        var normalizedActive = NormalizeSingle(vault.ActiveLlmProvider);
        if (normalizedActive != vault.ActiveLlmProvider) { vault.ActiveLlmProvider = normalizedActive; changed = true; }

        var normalizedChain = NormalizeList(vault.ActiveLlmProviderChain);
        if (normalizedChain != vault.ActiveLlmProviderChain) { vault.ActiveLlmProviderChain = normalizedChain; changed = true; }

        var normalizedAllowed = NormalizeList(vault.ReviewAllowedProviders);
        if (normalizedAllowed != vault.ReviewAllowedProviders) { vault.ReviewAllowedProviders = normalizedAllowed; changed = true; }

        var normalizedJury = NormalizeList(vault.ReaderQaJuryProviders);
        if (normalizedJury != vault.ReaderQaJuryProviders) { vault.ReaderQaJuryProviders = normalizedJury; changed = true; }

        if (changed) Flush();
    }

    // Bumped by every setter (through ScheduleSave), read by Flush: see the adopt step there.
    private int changeVersion;

    private void ScheduleSave()
    {
        Interlocked.Increment(ref changeVersion);
        lock (saveLock)
        {
            saveTimer?.Dispose();
            saveTimer = new Timer(_ =>
            {
                // Anything, not just I/O: this runs on a timer thread, where an uncaught exception (a
                // collection modified mid-serialize by a concurrent setter) ends the whole process.
                try { Flush(); }
                catch (Exception ex) { Serilog.Log.Warning(ex, "SettingsService: deferred save failed; the next save retries"); }
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
        // finally: the early returns below still raise what FlushVault adopted from other writers.
        try { FlushCore(); }
        finally { RaiseExternalChanges(); }
    }

    private void FlushCore()
    {
        lock (saveLock)
        {
            saveTimer?.Dispose();
            saveTimer = null;
            FlushVault();
            var dir = Path.GetDirectoryName(settingsPath);
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return;

            using var guard = new CrossProcessLock(settingsPath);
            guard.Acquire(TimeSpan.FromSeconds(5));

            var versionAtSnapshot = Volatile.Read(ref changeVersion);
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
            // A failed read must not be treated as an empty file: overlaying only our changed keys on
            // {} and writing it back would erase every key this process never touched. Keep the
            // file as it is; the in-memory changes stay queued and the next flush retries.
            catch (JsonException ex) { Serilog.Log.Warning(ex, "SettingsService: could not read Settings.json; save skipped"); return; }
            catch (IOException ex)   { Serilog.Log.Warning(ex, "SettingsService: could not read Settings.json; save skipped"); return; }

            // Overlay only the keys THIS process changed since it loaded / last flushed.
            foreach (var (key, node) in current)
            {
                // Vault owns these now; Settings.json keeps its pre-LAMP copies untouched (the rollback).
                if (VaultOwnedFields.Contains(key)) continue;
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
            // Setters change `data` without this lock. If one ran after the snapshot, swapping in the
            // merged object would discard its change; keep ours instead — its own ScheduleSave has
            // already queued the next flush, which diffs it against this baseline and writes it.
            // In that case the baseline must be our own snapshot, not the disk: `data` still holds the
            // old values of keys other processes wrote, and against a disk baseline every one of them
            // read as a local change on the next flush and was written back over the newer value.
            if (Volatile.Read(ref changeVersion) == versionAtSnapshot)
            {
                data = disk.Deserialize<SettingsData>() ?? data;
                baseline = disk;
            }
            else
                baseline = current;
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
        vaultWatcher?.Dispose();
        vaultWatcher = null;
        vaultDebounce?.Dispose();
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
        public string ActiveLlmProvider { get; set; } = "claude";
        public string ActiveLlmProviderChain { get; set; } =
            "claude,codex-cli,openai,gemini-cli,gemini,kimi,deepseek,mistral,perplexity";
        public int EditorFontSize { get; set; } = 14;
        public int AutoSaveIntervalMs { get; set; } = 2000;
        public string GeminiApiKey { get; set; } = "";
        public string DeepSeekApiKey { get; set; } = "";
        public string MistralApiKey { get; set; } = "";
        public string KimiApiKey { get; set; } = "";
        public string KimiModel { get; set; } = "kimi-k2.6";
        public string PerplexityApiKey { get; set; } = "";
        public string PerplexityModel { get; set; } = "sonar";
        public string GrokApiKey { get; set; } = "";
        public string GroqApiKey { get; set; } = "";
        public string TogetherApiKey { get; set; } = "";
        public string OpenRouterApiKey { get; set; } = "";
        public string FireworksApiKey { get; set; } = "";
        public string CohereApiKey { get; set; } = "";
        public string IdeogramApiKey { get; set; } = "";
        public string FalApiKey { get; set; } = "";
        public string StabilityApiKey { get; set; } = "";
        public string KlingApiKey { get; set; } = "";
        public string RunwayApiKey { get; set; } = "";
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
        public bool HubNarrationEnabled { get; set; } = false;
        public bool CorobsEnabled { get; set; } = false;
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
        public string ReviewAllowedProviders { get; set; } = "claude";
        public int ReviewMaxConcurrency { get; set; } = 10;
        // Reader-Proxy QA jury roster (cross-family; dead/keyless providers auto-skip)
        public string ReaderQaJuryProviders { get; set; } = "claude,openai,gemini,deepseek,kimi";
        // Registry of OpenAI-compatible jury families outside the Legion catalog.
        // Kimi K2.6 pricing per platform.moonshot.ai 2026-08: $0.60/M in, $0.16/M cache read, $2.50/M out.
        public string ExtraJuryProvidersJson { get; set; } =
            "[{\"id\":\"kimi\",\"baseUrl\":\"https://api.moonshot.ai/v1\",\"cheapModel\":\"kimi-k2.6\",\"label\":\"Kimi K2.6\",\"inPerMtok\":0.60,\"cacheReadPerMtok\":0.16,\"outPerMtok\":2.50}]";
        public string ComprehensionProbeModel { get; set; } = LlmModels.Haiku;
        public string ComprehensionArbiterModel { get; set; } = LlmModels.Sonnet;
        // Local-LLM review (--local) defaults
        public string LocalReviewBaseUrl { get; set; } = "http://localhost:11434/v1/chat/completions";
        public string LocalReviewModel { get; set; } = "qwen2.5-32b-rev-128k";
        public string LocalReviewLabel { get; set; } = "";
        /// <summary>Bearer token for a SECURED remote review endpoint (RunPod/vLLM/etc.); empty = bare localhost Ollama.</summary>
        public string LocalReviewApiKey { get; set; } = "";
        /// <summary>Local model context window in tokens (num_ctx) — used to size review segments to fit.</summary>
        public int LocalReviewContextTokens { get; set; } = 131072;
        public int LocalReviewMaxConcurrency { get; set; } = 2;
        // Portable-writing-service plan, Phase 1: shared secret for the Hub's sensitive HTTP
        // endpoints. Generated once by the Hub at startup if empty; every trusted local process
        // (Cli, Mcp) reads it from this same shared Settings.json file.
        public string HubApiKey { get; set; } = "";
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
        /// <summary>When true, CanonGroundingService fires after each beat write to flag PROVISIONAL-ENTITY findings. Default OFF — opt in to avoid extra LLM cost per beat.</summary>
        public bool AutoCanonGrounding { get; set; } = false;
        /// <summary>When true, SceneContextAssembler.HarvestRevealedDetailsAsync fires after each beat write to propose XRAY-REVEAL findings. Default OFF.</summary>
        public bool AutoHarvestRevealedDetails { get; set; } = false;
        // AutoGenerateCoverOnExport removed 2026-09-13 with the cover-generation pipeline. An
        // existing settings file that still carries the key simply ignores it on load.
    }
}
