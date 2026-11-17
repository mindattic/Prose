using System.Text.Json;
using System.Text.Json.Serialization;
using FluentFTP;
using Microsoft.Extensions.Logging;

namespace Prose.Core.Services;

/// <summary>
/// Mirrors a book's newest export bundle to the public mindattic.com site over FTPS (author
/// decision 2026-11-16, PRS-T942: every export publishes immediately, across every universe, no
/// opt-out — the author weighed and accepted that this includes draft-status universes).
///
/// <para>Reads the SAME credentials file <c>MindAttic.Deploy</c>'s own FTP pipeline prefers —
/// <c>%APPDATA%\MindAttic\Ftp\ftp.json</c> (host, port, user, password, secure,
/// rejectUnauthorized) — rather than inventing a second credential store. Until that file exists,
/// every call logs one warning and returns: a missing or unreachable public mirror must never
/// break a local export.</para>
/// </summary>
public class WebPublishService
{
    /// <summary>mindattic.com's FTPS remote root, same host every MindAttic.Deploy site upload uses.</summary>
    private const string RemoteSiteRoot = "/mindattic.com";

    private static readonly string CredentialsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MindAttic", "Ftp", "ftp.json");

    private readonly ILogger<WebPublishService> log;
    private bool warnedMissingCredentialsOnce;

    public WebPublishService(ILogger<WebPublishService> log) => this.log = log;

    private sealed class FtpCredentials
    {
        [JsonPropertyName("host")] public string Host { get; set; } = "";
        [JsonPropertyName("port")] public int Port { get; set; } = 21;
        [JsonPropertyName("user")] public string User { get; set; } = "";
        [JsonPropertyName("password")] public string Password { get; set; } = "";
        [JsonPropertyName("secure")] public bool Secure { get; set; } = true;
        [JsonPropertyName("rejectUnauthorized")] public bool RejectUnauthorized { get; set; } = true;
    }

    /// <summary>
    /// Uploads every top-level file currently in <paramref name="nodeDir"/> (the just-archived,
    /// newest-only bundle — no Archives subfolder to skip, since that now lives in a sibling tree)
    /// to <c>mindattic.com/&lt;universe&gt;/&lt;book-relative-path&gt;</c>, mirroring
    /// <paramref name="nodeDir"/>'s position under <paramref name="baseDir"/>. Never deletes a
    /// remote file absent locally — same "never delete" convention as the local archive and as
    /// MindAttic.Deploy's own site uploads. Never throws: a network or credential problem is
    /// logged and swallowed so a public-site hiccup can never fail a local export.
    /// </summary>
    public async Task PublishAsync(string baseDir, string nodeDir, CancellationToken ct = default)
    {
        FtpCredentials creds;
        try
        {
            if (!File.Exists(CredentialsPath))
            {
                if (!warnedMissingCredentialsOnce)
                {
                    warnedMissingCredentialsOnce = true;
                    log.LogWarning("WebPublishService: no FTP credentials at {Path} — exports are not being mirrored to mindattic.com until that file is created (same shape as MindAttic.Deploy/secrets/ftp.json.template).", CredentialsPath);
                }
                return;
            }
            var json = await File.ReadAllTextAsync(CredentialsPath, ct);
            creds = JsonSerializer.Deserialize<FtpCredentials>(json) ?? new FtpCredentials();
            if (string.IsNullOrWhiteSpace(creds.Host))
            {
                log.LogWarning("WebPublishService: {Path} has no host set — skipping public publish.", CredentialsPath);
                return;
            }
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "WebPublishService: could not read FTP credentials from {Path} — skipping public publish.", CredentialsPath);
            return;
        }

        var remoteDir = RemotePathFor(baseDir, nodeDir);
        try
        {
            using var client = new AsyncFtpClient(creds.Host, creds.User, creds.Password, creds.Port, new FtpConfig
            {
                EncryptionMode = creds.Secure ? FtpEncryptionMode.Explicit : FtpEncryptionMode.None,
                ValidateAnyCertificate = !creds.RejectUnauthorized,
            });
            await client.Connect(ct);
            await client.CreateDirectory(remoteDir, ct);
            foreach (var file in Directory.EnumerateFiles(nodeDir, "*", SearchOption.TopDirectoryOnly))
            {
                var remotePath = $"{remoteDir}/{Path.GetFileName(file)}";
                await client.UploadFile(file, remotePath, FtpRemoteExists.Overwrite, createRemoteDir: false, token: ct);
            }
            await client.Disconnect(ct);
            log.LogInformation("WebPublishService: mirrored {NodeDir} -> ftp://{Host}{RemoteDir}", nodeDir, creds.Host, remoteDir);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "WebPublishService: publishing {NodeDir} to mindattic.com failed — the local export is unaffected.", nodeDir);
        }
    }

    /// <summary>The remote FTPS path for a book's export folder: <c>/mindattic.com/&lt;universe&gt;/&lt;book-relative-path&gt;</c>,
    /// mirroring <paramref name="nodeDir"/>'s position under <paramref name="baseDir"/> exactly
    /// the way <see cref="ExportCleanupService.ArchiveDirFor"/> mirrors it into the sibling
    /// Archives tree.</summary>
    internal static string RemotePathFor(string baseDir, string nodeDir)
    {
        var trimmed = baseDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var universeFolder = Path.GetFileName(trimmed);
        var relative = Path.GetRelativePath(baseDir, nodeDir).Replace(Path.DirectorySeparatorChar, '/').Replace(Path.AltDirectorySeparatorChar, '/');
        return relative == "."
            ? $"{RemoteSiteRoot}/{universeFolder}"
            : $"{RemoteSiteRoot}/{universeFolder}/{relative}";
    }
}
