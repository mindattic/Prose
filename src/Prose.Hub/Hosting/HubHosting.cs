using System.Security.Cryptography;

namespace Prose.Hub.Hosting;

/// <summary>Hosted mode was asked for and its configuration is missing or wrong. The Hub must not start.</summary>
public sealed class HubHostingException(string message) : Exception(message);

/// <summary>
/// How the Hub is exposed (PRS-T1004). Two modes, chosen by configuration:
///
/// <para><b>Local</b> (the default, and the only mode before this): bound to <c>http://127.0.0.1:5900</c>,
/// anonymous, for the Writer, Launcher, CLI and MCP on the same PC. Nothing in this class applies.</para>
///
/// <para><b>Public</b> (<c>Hosting:Mode=Public</c>, env <c>Hosting__Mode</c>): reachable from the internet, for
/// example on Azure App Service. The Hub binds the way the platform asks, serves only the configured host
/// names over HTTPS, and requires a signed-in, allow-listed MindAttic.Authentication user for every route
/// except the login pages and <c>/healthz</c>. It refuses to start when any of that is not configured.</para>
///
/// <para>Every key (a <c>:</c> becomes <c>__</c> in an environment variable or App Service setting):</para>
/// <list type="table">
/// <item><term>Hosting:Mode</term><description><c>Local</c> (default) or <c>Public</c>. Anything else stops the Hub.</description></item>
/// <item><term>Hosting:AllowedHosts</term><description>Required. Host names the Hub answers to, separated by <c>;</c> or <c>,</c>
/// (for example <c>mindattic-prose.azurewebsites.net</c>). No wildcard.</description></item>
/// <item><term>Hosting:Auth:AllowedUsers</term><description>Required. The user names allowed in, separated by <c>;</c> or <c>,</c>.
/// Any other account in the auth tables is refused even with a correct password.</description></item>
/// <item><term>Hosting:Auth:RequireMfa</term><description>Default <c>true</c>: a session without a TOTP step is sent to
/// <c>/account/mfa/setup</c> and reaches nothing else.</description></item>
/// <item><term>Hosting:Auth:LoginPermitLimit / LoginWindowSeconds</term><description>Per-address rate limit on the sign-in
/// endpoints, default 10 requests per 60 seconds (on top of the auth library's own per-account and per-address back-off).</description></item>
/// <item><term>Hosting:DataProtection:KeyRingPath</term><description>Folder for the Data Protection key ring. It must
/// survive restarts: it protects the login cookie, the antiforgery tokens and the stored TOTP secrets. Default on
/// App Service (where <c>HOME</c> is persistent storage): <c>%HOME%\data\Prose\DataProtection-Keys</c>. Required elsewhere.</description></item>
/// <item><term>Hosting:Urls</term><description>Optional listen URLs. Without it the Hub honours <c>ASPNETCORE_URLS</c> /
/// <c>ASPNETCORE_HTTP_PORTS</c>, then IIS out-of-process's <c>ASPNETCORE_PORT</c>, then the platform's <c>PORT</c> /
/// <c>WEBSITES_PORT</c>, then the host's default (IIS in-process, where /app has no loopback port to call and does not work).</description></item>
/// <item><term>Hosting:RequireHttps / HttpsPort / HstsMaxAgeDays</term><description>Defaults <c>true</c> / <c>443</c> / <c>365</c>.</description></item>
/// <item><term>Hosting:TrustForwardedHeaders / ForwardLimit</term><description>Defaults <c>true</c> / <c>1</c>: read the
/// platform proxy's <c>X-Forwarded-For</c> / <c>X-Forwarded-Proto</c>.</description></item>
/// <item><term>Hosting:SelfBaseUrl</term><description>Optional. The loopback address the Hub's own /app page calls it on;
/// default <c>http://127.0.0.1:{listen port}</c>.</description></item>
/// </list>
/// <para>The login itself is MindAttic.Authentication's: <c>MindAttic:Vault:Security:pepper.v1</c> is required (the same
/// value the accounts were hashed with) and <c>MindAttic:Vault:Security:bootstrap-token</c> only while the auth tables
/// hold no user at all. Its own options stay under <c>MindAttic:Auth:*</c>.</para>
/// </summary>
public sealed class HubHosting
{
    public const string ModeKey = "Hosting:Mode";
    public const string AllowedHostsKey = "Hosting:AllowedHosts";
    public const string AllowedUsersKey = "Hosting:Auth:AllowedUsers";
    public const string RequireMfaKey = "Hosting:Auth:RequireMfa";
    public const string LoginPermitLimitKey = "Hosting:Auth:LoginPermitLimit";
    public const string LoginWindowSecondsKey = "Hosting:Auth:LoginWindowSeconds";
    public const string KeyRingPathKey = "Hosting:DataProtection:KeyRingPath";
    public const string UrlsKey = "Hosting:Urls";
    public const string RequireHttpsKey = "Hosting:RequireHttps";
    public const string HttpsPortKey = "Hosting:HttpsPort";
    public const string HstsMaxAgeDaysKey = "Hosting:HstsMaxAgeDays";
    public const string TrustForwardedHeadersKey = "Hosting:TrustForwardedHeaders";
    public const string ForwardLimitKey = "Hosting:ForwardLimit";
    public const string SelfBaseUrlKey = "Hosting:SelfBaseUrl";

    /// <summary>The address every local client uses; local mode binds exactly this.</summary>
    public const string LocalBaseUrl = "http://127.0.0.1:5900";

    /// <summary>The local, loopback-only, anonymous Hub. The default.</summary>
    public static HubHosting Local { get; } = new();

    public bool IsPublic { get; private init; }
    public IReadOnlyList<string> AllowedHosts { get; private init; } = [];
    public IReadOnlyList<string> AllowedUsers { get; private init; } = [];
    public bool RequireMfa { get; private init; } = true;
    public int LoginPermitLimit { get; private init; } = 10;
    public int LoginWindowSeconds { get; private init; } = 60;
    public string KeyRingPath { get; private init; } = "";
    /// <summary>Non-null when the Hub itself must call <c>UseUrls</c>; null leaves the binding to the host.</summary>
    public string? ListenUrls { get; private init; }
    public bool RequireHttps { get; private init; } = true;
    public int HttpsPort { get; private init; } = 443;
    public int HstsMaxAgeDays { get; private init; } = 365;
    public bool TrustForwardedHeaders { get; private init; } = true;
    public int ForwardLimit { get; private init; } = 1;
    /// <summary>Loopback address the Hub's own /app page calls it on; null when no listen port is known (IIS in-process).</summary>
    public string? SelfBaseUrl { get; private init; }

    /// <summary>
    /// Per-process secret the Hub's own in-process UI sends on its loopback calls (see <see cref="HostedGate"/>).
    /// Never leaves the process, never written anywhere, different on every start.
    /// </summary>
    public string InternalToken { get; } = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

    public IReadOnlyDictionary<string, string> InternalHeaders
    {
        get
        {
            var headers = new Dictionary<string, string> { [HostedGate.InternalHeader] = InternalToken };
            // Behind IIS out-of-process (Azure App Service on Windows) Kestrel answers only requests that carry
            // the module's pairing token, and answers 400 to anything else, the Hub's own loopback calls
            // included. The module hands the token to this process alone, in ASPNETCORE_TOKEN (PRS-T1005).
            if (IisPairingToken is { } pairing) headers[IisPairingHeader] = pairing;
            return headers;
        }
    }

    /// <summary>The header IIS's ASP.NET Core Module sends on every request it forwards (out-of-process hosting).</summary>
    public const string IisPairingHeader = "MS-ASPNETCORE-TOKEN";

    /// <summary>The module's pairing token; null when the Hub is not behind IIS out-of-process.</summary>
    public string? IisPairingToken { get; private init; }

    /// <summary>Browser origins allowed to open a WebSocket: the allowed hosts, over the scheme the Hub accepts.</summary>
    public IReadOnlyList<string> AllowedOrigins
    {
        get
        {
            var origins = new List<string>();
            foreach (var host in AllowedHosts)
            {
                origins.Add(HttpsPort == 443 ? $"https://{host}" : $"https://{host}:{HttpsPort}");
                if (!RequireHttps) origins.Add($"http://{host}");
            }
            return origins;
        }
    }

    public bool IsUserAllowed(string? userName) =>
        !string.IsNullOrWhiteSpace(userName)
        && AllowedUsers.Any(u => string.Equals(u, userName.Trim(), StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Reads the mode. Local returns <see cref="Local"/> and looks at nothing else. Public validates every
    /// key and throws <see cref="HubHostingException"/> listing all problems at once (fail closed).
    /// </summary>
    /// <param name="env">Environment lookup; defaults to the process environment. A seam for tests.</param>
    public static HubHosting Read(IConfiguration config, Func<string, string?>? env = null)
    {
        env ??= Environment.GetEnvironmentVariable;
        var mode = (config[ModeKey] ?? "").Trim();
        if (mode.Length == 0 || mode.Equals("Local", StringComparison.OrdinalIgnoreCase)) return Local;
        if (!mode.Equals("Public", StringComparison.OrdinalIgnoreCase))
            throw new HubHostingException(
                $"[hub] {ModeKey} is '{mode}'. It must be 'Local' (the default) or 'Public'. The Hub will not guess; not starting.");

        var problems = new List<string>();

        var hosts = SplitList(config, AllowedHostsKey);
        if (hosts.Count == 0)
            problems.Add($"{AllowedHostsKey} is empty. Set the host name(s) the Hub is served on, e.g. mindattic-prose.azurewebsites.net.");
        else if (hosts.Any(h => h.Contains('*') || h.Contains('/') || h.Contains(':')))
            problems.Add($"{AllowedHostsKey} takes bare host names (no scheme, port, path or wildcard): '{string.Join(";", hosts)}'.");

        var users = SplitList(config, AllowedUsersKey);
        if (users.Count == 0)
            problems.Add($"{AllowedUsersKey} is empty. Name the account(s) allowed to sign in.");

        var loginPermit = ReadInt(config, LoginPermitLimitKey, 10, 1, 10_000, problems);
        var loginWindow = ReadInt(config, LoginWindowSecondsKey, 60, 1, 86_400, problems);
        var httpsPort = ReadInt(config, HttpsPortKey, 443, 1, 65_535, problems);
        var hstsDays = ReadInt(config, HstsMaxAgeDaysKey, 365, 0, 3_650, problems);
        var forwardLimit = ReadInt(config, ForwardLimitKey, 1, 1, 10, problems);
        var requireMfa = ReadBool(config, RequireMfaKey, true, problems);
        var requireHttps = ReadBool(config, RequireHttpsKey, true, problems);
        var trustForwarded = ReadBool(config, TrustForwardedHeadersKey, true, problems);

        var keyRing = (config[KeyRingPathKey] ?? "").Trim();
        if (keyRing.Length == 0)
        {
            // App Service: HOME is the persistent, instance-shared content share (D:\home, /home).
            var home = env("HOME");
            if (!string.IsNullOrWhiteSpace(env("WEBSITE_SITE_NAME")) && !string.IsNullOrWhiteSpace(home))
                keyRing = Path.Combine(home, "data", "Prose", "DataProtection-Keys");
            else
                problems.Add($"{KeyRingPathKey} is not set. Give a folder that survives restarts: it holds the keys behind the " +
                             "login cookie and the stored TOTP secrets.");
        }

        var (listenUrls, port) = ResolveBinding(config, env);
        var self = (config[SelfBaseUrlKey] ?? "").Trim();
        if (self.Length > 0 && !(Uri.TryCreate(self, UriKind.Absolute, out var selfUri) && selfUri.IsLoopback))
            problems.Add($"{SelfBaseUrlKey} must be an absolute loopback URL (http://127.0.0.1:<port>): '{self}'.");

        if (problems.Count > 0)
            throw new HubHostingException(
                $"[hub] {ModeKey}=Public but hosted mode is not fully configured. The Hub will not serve anonymously; not starting." +
                Environment.NewLine + string.Join(Environment.NewLine, problems.Select(p => "  - " + p)));

        return new HubHosting
        {
            IsPublic = true,
            AllowedHosts = hosts,
            AllowedUsers = users,
            RequireMfa = requireMfa,
            LoginPermitLimit = loginPermit,
            LoginWindowSeconds = loginWindow,
            KeyRingPath = keyRing,
            ListenUrls = listenUrls,
            RequireHttps = requireHttps,
            HttpsPort = httpsPort,
            HstsMaxAgeDays = hstsDays,
            TrustForwardedHeaders = trustForwarded,
            ForwardLimit = forwardLimit,
            SelfBaseUrl = self.Length > 0 ? self.TrimEnd('/') : port is { } p ? $"http://127.0.0.1:{p}" : null,
            IisPairingToken = FirstNonEmpty(env("ASPNETCORE_TOKEN")),
        };
    }

    /// <summary>
    /// Creates the key-ring folder and proves it is writable. A ring that cannot be written falls back to
    /// in-memory keys, which silently signs everyone out and orphans the TOTP secrets on every restart.
    /// </summary>
    public void EnsureKeyRingWritable()
    {
        try
        {
            Directory.CreateDirectory(KeyRingPath);
            var probe = Path.Combine(KeyRingPath, $".write-probe-{Guid.NewGuid():N}");
            File.WriteAllText(probe, "");
            File.Delete(probe);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException)
        {
            throw new HubHostingException(
                $"[hub] The Data Protection key ring folder '{KeyRingPath}' ({KeyRingPathKey}) cannot be written: {ex.Message} Not starting.");
        }
    }

    /// <summary>
    /// Where to listen. Returns the URLs the Hub must pass to <c>UseUrls</c> (null = leave it to the host,
    /// which already honours <c>ASPNETCORE_URLS</c>, <c>ASPNETCORE_HTTP_PORTS</c> or IIS) and the port, when known.
    /// </summary>
    internal static (string? ListenUrls, int? Port) ResolveBinding(IConfiguration config, Func<string, string?> env)
    {
        var explicitUrls = (config[UrlsKey] ?? "").Trim();
        if (explicitUrls.Length > 0) return (explicitUrls, FirstPort(explicitUrls));

        var urls = FirstNonEmpty(config["urls"], env("ASPNETCORE_URLS"));
        if (urls is not null) return (null, FirstPort(urls));

        var ports = FirstNonEmpty(config["http_ports"], env("ASPNETCORE_HTTP_PORTS"));
        if (ports is not null)
            return (null, int.TryParse(ports.Split(';', ',')[0].Trim(), out var hp) ? hp : null);

        // IIS out-of-process (App Service on Windows): the module picks the loopback port Kestrel must use
        // and UseIISIntegration binds it; the Hub only needs the number, for its own /app calls.
        if (FirstNonEmpty(env("ASPNETCORE_PORT")) is { } iisPort && int.TryParse(iisPort, out var ip) && ip is > 0 and <= 65_535)
            return (null, ip);

        // App Service on Linux and most container platforms name the port instead.
        var platformPort = FirstNonEmpty(env("PORT"), env("WEBSITES_PORT"));
        if (platformPort is not null && int.TryParse(platformPort, out var pp) && pp is > 0 and <= 65_535)
            return ($"http://0.0.0.0:{pp}", pp);

        return (null, null);
    }

    internal static int? FirstPort(string urls)
    {
        var first = urls.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
        if (first is null) return null;
        // "http://*:8080" and "http://+:8080" are valid to Kestrel but not to Uri.
        var parsable = first.Replace("://*", "://localhost").Replace("://+", "://localhost");
        return Uri.TryCreate(parsable, UriKind.Absolute, out var uri) ? uri.Port : null;
    }

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))?.Trim();

    /// <summary>A list given as one "a;b,c" string or as an indexed section (Key:0, Key:1).</summary>
    private static List<string> SplitList(IConfiguration config, string key)
    {
        var raw = new List<string>();
        if (config[key] is { } single) raw.Add(single);
        raw.AddRange(config.GetSection(key).GetChildren().Select(c => c.Value ?? ""));
        return raw.SelectMany(v => v.Split(';', ','))
            .Select(v => v.Trim())
            .Where(v => v.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static int ReadInt(IConfiguration config, string key, int fallback, int min, int max, List<string> problems)
    {
        var raw = (config[key] ?? "").Trim();
        if (raw.Length == 0) return fallback;
        if (int.TryParse(raw, out var value) && value >= min && value <= max) return value;
        problems.Add($"{key} must be a whole number from {min} to {max}: '{raw}'.");
        return fallback;
    }

    private static bool ReadBool(IConfiguration config, string key, bool fallback, List<string> problems)
    {
        var raw = (config[key] ?? "").Trim();
        if (raw.Length == 0) return fallback;
        if (bool.TryParse(raw, out var value)) return value;
        problems.Add($"{key} must be true or false: '{raw}'.");
        return fallback;
    }
}
