using System.Net;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Uploads exported HTML files to a remote FTP server for web publishing.
/// Uses System.Net.FtpWebRequest — no external dependencies needed.
/// </summary>
public class FtpPublishService
{
    private readonly SettingsService settings;

    public FtpPublishService(SettingsService settings)
    {
        this.settings = settings;
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(settings.FtpHost) &&
        !string.IsNullOrWhiteSpace(settings.FtpUsername);

    /// <summary>
    /// Upload all files from the export directory to the FTP remote path.
    /// Calls onProgress(current, total, fileName) for each file.
    /// Returns (success, message).
    /// </summary>
    public async Task<(bool success, string message)> PublishAsync(
        string exportDir, Action<int, int, string>? onProgress = null, CancellationToken ct = default)
    {
        if (!IsConfigured)
            return (false, "FTP not configured — set host, username, and password in Settings.");

        if (!Directory.Exists(exportDir))
            return (false, $"Export directory not found: {exportDir}");

        var files = Directory.GetFiles(exportDir)
            .Where(f => f.EndsWith(".htm") || f.EndsWith(".css") || f.EndsWith(".js") || f.EndsWith(".json"))
            .ToArray();

        if (files.Length == 0)
            return (false, "No export files found. Run Export All first.");

        var baseUri = BuildBaseUri();
        var credentials = new NetworkCredential(settings.FtpUsername, settings.FtpPassword);
        int uploaded = 0;
        int failed = 0;
        var errors = new List<string>();

        // Ensure remote directory exists
        try { await EnsureDirectoryAsync(baseUri, credentials); }
        catch (Exception ex) { Serilog.Log.Warning(ex, "FTP directory check failed (may already exist)"); }

        for (int i = 0; i < files.Length; i++)
        {
            ct.ThrowIfCancellationRequested();
            var file = files[i];
            var fileName = Path.GetFileName(file);
            onProgress?.Invoke(i + 1, files.Length, fileName);

            var success = false;
            var fileBytes = await File.ReadAllBytesAsync(file, ct);
            var maxAttempts = fileBytes.Length > 1_000_000 ? 5 : 3; // more retries for large files

            for (int attempt = 0; attempt < maxAttempts && !success; attempt++)
            {
                try
                {
                    if (attempt > 0)
                    {
                        var backoff = (int)(1000 * Math.Pow(2, attempt)); // exponential: 2s, 4s, 8s, 16s
                        await Task.Delay(backoff, ct);
                        onProgress?.Invoke(i + 1, files.Length, $"{fileName} (retry {attempt}/{maxAttempts - 1})");
                    }

                    var uploadUri = $"{baseUri}/{fileName}";
                    var request = CreateRequest(uploadUri, WebRequestMethods.Ftp.UploadFile, credentials);
                    // Scale timeout with file size: 30s base + 1s per 100KB
                    var timeoutMs = 30_000 + (fileBytes.Length / 100);
                    request.Timeout = Math.Max(timeoutMs, 300_000);
                    request.ReadWriteTimeout = request.Timeout;
                    request.ContentLength = fileBytes.Length;

                    var requestStream = await request.GetRequestStreamAsync();
                    try
                    {
                        // Scale chunk size with file: 32KB for small, 128KB for large
                        int chunkSize = fileBytes.Length > 500_000 ? 128 * 1024 : 32 * 1024;
                        int offset = 0;
                        while (offset < fileBytes.Length)
                        {
                            int count = Math.Min(chunkSize, fileBytes.Length - offset);
                            await requestStream.WriteAsync(fileBytes.AsMemory(offset, count), ct);
                            await requestStream.FlushAsync(ct);
                            offset += count;
                        }
                    }
                    finally
                    {
                        try { requestStream.Close(); } catch { /* 451 on close is common with SSL — ignore if data was sent */ }
                    }

                    try
                    {
                        using var response = (FtpWebResponse)await request.GetResponseAsync();
                        response.Close();
                    }
                    catch { /* Response may fail after stream close issue — verify size below */ }

                    // Verify upload by checking remote file size
                    try
                    {
                        var sizeRequest = CreateRequest(uploadUri, WebRequestMethods.Ftp.GetFileSize, credentials);
                        using var sizeResponse = (FtpWebResponse)await sizeRequest.GetResponseAsync();
                        var remoteSize = sizeResponse.ContentLength;
                        sizeResponse.Close();
                        if (remoteSize >= 0 && remoteSize != fileBytes.Length)
                        {
                            Serilog.Log.Warning("FTP size mismatch for {File}: local={Local} remote={Remote}", fileName, fileBytes.Length, remoteSize);
                            if (attempt < maxAttempts - 1) continue; // retry on size mismatch
                        }
                    }
                    catch { /* Size check not supported by all servers — skip */ }

                    success = true;
                    uploaded++;
                }
                catch (Exception ex) when (attempt < maxAttempts - 1)
                {
                    Serilog.Log.Debug("FTP upload attempt {Attempt} failed for {File}: {Msg}", attempt + 1, fileName, ex.Message);
                }
                catch (Exception ex)
                {
                    failed++;
                    errors.Add($"{fileName}: {ex.Message}");
                    Serilog.Log.Warning(ex, "FTP upload failed for {FileName} after {Attempts} attempts", fileName, maxAttempts);
                }
            }

            // Small delay between files to avoid overwhelming the server
            if (i < files.Length - 1) await Task.Delay(500, ct);
        }

        var msg = $"Uploaded {uploaded}/{files.Length} files to {settings.FtpHost}{settings.FtpRemotePath}";
        if (failed > 0) msg += $"\n{failed} failed:\n" + string.Join("\n", errors.Take(5));

        return (failed == 0, msg);
    }

    /// <summary>Test the FTP connection with current settings.</summary>
    public async Task<(bool success, string message)> TestConnectionAsync()
    {
        if (!IsConfigured)
            return (false, "FTP not configured.");

        try
        {
            var baseUri = BuildBaseUri();
            var credentials = new NetworkCredential(settings.FtpUsername, settings.FtpPassword);
            var request = CreateRequest(baseUri, WebRequestMethods.Ftp.ListDirectory, credentials);

            using var response = (FtpWebResponse)await request.GetResponseAsync();
            var status = response.StatusDescription;
            response.Close();

            return (true, $"Connected to {settings.FtpHost} — {status?.Trim()}");
        }
        catch (Exception ex)
        {
            return (false, $"Connection failed: {ex.Message}");
        }
    }

    private string BuildBaseUri()
    {
        var scheme = settings.FtpUseSsl ? "ftps" : "ftp";
        var port = settings.FtpPort != 21 ? $":{settings.FtpPort}" : "";
        var remotePath = settings.FtpRemotePath.TrimStart('/');
        return $"ftp://{settings.FtpHost}{port}/{remotePath}";
    }

    private FtpWebRequest CreateRequest(string uri, string method, NetworkCredential credentials)
    {
        var request = (FtpWebRequest)WebRequest.Create(uri);
        request.Method = method;
        request.Credentials = credentials;
        request.UsePassive = settings.FtpPassive;
        request.UseBinary = true;
        request.EnableSsl = settings.FtpUseSsl;
        request.KeepAlive = false;
        request.Timeout = 300000; // 5 minutes for large files
        request.ReadWriteTimeout = 300000;

        // Accept any SSL cert for self-signed servers
        if (settings.FtpUseSsl)
            ServicePointManager.ServerCertificateValidationCallback = (_, _, _, _) => true;

        return request;
    }

    private async Task EnsureDirectoryAsync(string baseUri, NetworkCredential credentials)
    {
        try
        {
            var request = CreateRequest(baseUri, WebRequestMethods.Ftp.MakeDirectory, credentials);
            using var response = (FtpWebResponse)await request.GetResponseAsync();
            response.Close();
        }
        catch (WebException ex) when (ex.Response is FtpWebResponse ftpResponse &&
            ftpResponse.StatusCode == FtpStatusCode.ActionNotTakenFileUnavailable)
        {
            // Directory already exists — fine
        }
    }
}
