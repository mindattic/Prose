using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Prose.Core.Interfaces;

namespace Prose.Core.Services;

public record ScriptArgDef(string Flag, string Desc, string? Default);

public record ScriptInfo(
    string Name,
    string RelativePath,
    string FullPath,
    string Language,   // "python" | "node"
    string Description,
    IReadOnlyList<ScriptArgDef> Args
);

public record ScriptOutputLine(string Text, bool IsError, DateTime Timestamp);

public class ScriptRunnerService
{
    private readonly string scriptsRoot;

    public ScriptRunnerService(IPathProvider paths)
    {
        // DataRoot is the project root (e.g. D:\Projects\MindAttic\Prose\)
        // scripts/ lives at the same level as engine/
        scriptsRoot = Path.GetFullPath(Path.Combine(paths.DataRoot, "scripts"));
        if (!Directory.Exists(scriptsRoot))
            scriptsRoot = Path.GetFullPath(Path.Combine(paths.DataRoot, "..", "scripts"));
    }

    public IReadOnlyList<ScriptInfo> GetAllScripts()
    {
        var manifestArgs = LoadManifestArgs();

        var result = new List<ScriptInfo>();
        foreach (var (folder, lang, ext) in new[] { ("py", "python", ".py"), ("js", "node", ".js") })
        {
            var dir = Path.Combine(scriptsRoot, folder);
            if (!Directory.Exists(dir)) continue;

            foreach (var file in Directory.GetFiles(dir, $"*{ext}").OrderBy(f => f))
            {
                var fileName = Path.GetFileName(file);
                var name = Path.GetFileNameWithoutExtension(file);
                if (name == "__pycache__" || name.StartsWith("__")) continue;

                manifestArgs.TryGetValue(fileName, out ManifestEntry? entry);
                result.Add(new ScriptInfo(
                    Name: name,
                    RelativePath: Path.Combine(folder, fileName),
                    FullPath: file,
                    Language: lang,
                    Description: entry?.Description ?? ReadFirstDocLine(file, ext),
                    Args: entry?.Args ?? []
                ));
            }
        }
        return result;
    }

    private sealed record ManifestEntry(string Description, IReadOnlyList<ScriptArgDef> Args);

    private Dictionary<string, ManifestEntry> LoadManifestArgs()
    {
        var result = new Dictionary<string, ManifestEntry>(StringComparer.OrdinalIgnoreCase);
        var manifestPath = Path.Combine(scriptsRoot, "manifest.json");
        if (!File.Exists(manifestPath)) return result;

        try
        {
            var root = JsonNode.Parse(File.ReadAllText(manifestPath));
            if (root?["categories"] is not JsonArray cats) return result;
            foreach (var cat in cats.OfType<JsonObject>())
            {
                if (cat["scripts"] is not JsonArray scripts) continue;
                foreach (var s in scripts.OfType<JsonObject>())
                {
                    var file = s["file"]?.GetValue<string>();
                    var desc = s["description"]?.GetValue<string>() ?? "";
                    if (string.IsNullOrEmpty(file)) continue;

                    var args = new List<ScriptArgDef>();
                    if (s["args"] is JsonArray argArr)
                    {
                        foreach (var a in argArr.OfType<JsonObject>())
                        {
                            var flag = a["flag"]?.GetValue<string>() ?? "";
                            var argDesc = a["desc"]?.GetValue<string>() ?? "";
                            var def = a["default"]?.ToJsonString();
                            if (!string.IsNullOrEmpty(flag))
                                args.Add(new ScriptArgDef(flag, argDesc, def));
                        }
                    }
                    result[file] = new ManifestEntry(desc, args);
                }
            }
        }
        catch { }
        return result;
    }

    public async Task<int> RunAsync(
        ScriptInfo script,
        string extraArgs,
        IProgress<ScriptOutputLine> output,
        CancellationToken ct)
    {
        var executable = script.Language == "python" ? FindPython() : FindNode();
        if (executable == null)
        {
            output.Report(new ScriptOutputLine(
                $"ERROR: Could not find {script.Language} executable. Make sure it is on PATH.", true, DateTime.Now));
            return -1;
        }

        var args = $"\"{script.FullPath}\"" + (string.IsNullOrWhiteSpace(extraArgs) ? "" : $" {extraArgs}");
        var psi = new ProcessStartInfo(executable, args)
        {
            WorkingDirectory = Path.GetDirectoryName(script.FullPath)!,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null)
                output.Report(new ScriptOutputLine(e.Data.Replace("\r", ""), false, DateTime.Now));
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null)
                output.Report(new ScriptOutputLine(e.Data.Replace("\r", ""), true, DateTime.Now));
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        using var kill = ct.Register(() =>
        {
            try { process.Kill(entireProcessTree: true); } catch { }
        });

        await process.WaitForExitAsync(CancellationToken.None);
        return process.ExitCode;
    }

    private static string? FindPython()
    {
        foreach (var candidate in new[] { "python", "python3", "py" })
            if (TryResolve(candidate) is { } p) return p;
        return null;
    }

    private static string? FindNode()
    {
        foreach (var candidate in new[] { "node", "node.exe" })
            if (TryResolve(candidate) is { } p) return p;
        return null;
    }

    private static string? TryResolve(string exe)
    {
        try
        {
            using var probe = Process.Start(new ProcessStartInfo(exe, "--version")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            });
            probe?.WaitForExit(2000);
            return probe?.ExitCode == 0 ? exe : null;
        }
        catch { return null; }
    }

    private static string ReadFirstDocLine(string path, string ext)
    {
        try
        {
            using var reader = new StreamReader(path);
            string? line;
            var linesRead = 0;
            while ((line = reader.ReadLine()) != null && linesRead < 20)
            {
                linesRead++;
                var trimmed = line.TrimStart();
                if (ext == ".py")
                {
                    if (trimmed.StartsWith("\"\"\"") || trimmed.StartsWith("'''"))
                    {
                        var content = trimmed.TrimStart('"', '\'').Trim();
                        if (!string.IsNullOrEmpty(content)) return Truncate(content, 120);
                        // Multi-line: read next non-empty line
                        while ((line = reader.ReadLine()) != null)
                        {
                            var inner = line.Trim().TrimStart('"', '\'').Trim();
                            if (!string.IsNullOrEmpty(inner) && !inner.StartsWith("\"\"\"") && !inner.StartsWith("'''"))
                                return Truncate(inner, 120);
                        }
                    }
                }
                else
                {
                    if (trimmed.StartsWith("//"))
                        return Truncate(trimmed[2..].Trim(), 120);
                    if (trimmed.StartsWith("/*"))
                    {
                        var content = trimmed[2..].TrimStart('*').Trim();
                        if (!string.IsNullOrEmpty(content) && !content.StartsWith("*"))
                            return Truncate(content, 120);
                    }
                }
            }
        }
        catch { }
        return "";
    }

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..max] + "…";
}
