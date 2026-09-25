using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Prose.Core.Services.Factory;

/// <summary>The Hub's build id (the Hub.exe module version id), set once at Hub startup. A work
/// order records it when it opens, so a `deploy` check can prove the Hub was redeployed since.</summary>
public static class HubBuildInfo
{
    public static string? Build { get; set; }
}

/// <summary>Read-only git queries the Hub runs to validate `commit` checks. The factory never
/// takes a commit's existence or contents on anyone's word.</summary>
public static class GitProbe
{
    /// <summary>The repo the factory validates against. Override with PROSE_REPO_PATH (tests do).</summary>
    public static string RepoPath =>
        Environment.GetEnvironmentVariable("PROSE_REPO_PATH") is { Length: > 0 } p ? p : @"D:\Projects\MindAttic\Prose";

    public static (int Exit, string Out, string Err) Run(string repo, params string[] args)
    {
        var psi = new ProcessStartInfo("git")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        psi.ArgumentList.Add("-C");
        psi.ArgumentList.Add(repo);
        foreach (var a in args) psi.ArgumentList.Add(a);
        using var p = Process.Start(psi) ?? throw new InvalidOperationException("git could not be started");
        var outTask = p.StandardOutput.ReadToEndAsync();
        var errTask = p.StandardError.ReadToEndAsync();
        if (!p.WaitForExit(30_000)) { try { p.Kill(entireProcessTree: true); } catch { } return (-1, "", "git timed out"); }
        return (p.ExitCode, outTask.Result, errTask.Result);
    }

    public static string? Head(string repo)
    {
        var r = Run(repo, "rev-parse", "HEAD");
        return r.Exit == 0 ? r.Out.Trim() : null;
    }

    /// <summary>True when <paramref name="hash"/> is a commit reachable from HEAD.</summary>
    public static bool IsAncestorOfHead(string repo, string hash) =>
        Run(repo, "merge-base", "--is-ancestor", hash, "HEAD").Exit == 0;

    public static IReadOnlyList<string> ChangedFiles(string repo, string hash) =>
        // quotePath=false: otherwise git wraps non-ASCII paths in "…" with octal escapes and no
        // declared glob can match them.
        Run(repo, "-c", "core.quotePath=false", "show", "--name-only", "--format=", hash).Out
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(f => f.Replace('\\', '/'))
            .ToList();

    public static DateTimeOffset? CommitTime(string repo, string hash)
    {
        var r = Run(repo, "show", "-s", "--format=%cI", hash);
        return r.Exit == 0 && DateTimeOffset.TryParse(r.Out.Trim(), out var t) ? t : null;
    }
}

/// <summary>Repo-relative glob matching for a work order's declared paths. `**` crosses folders,
/// `*` and `?` do not. Case-insensitive (the repo lives on Windows).</summary>
public static class PathGlob
{
    public static Regex ToRegex(string glob)
    {
        var g = glob.Replace('\\', '/').Trim();
        var rx = "^" + Regex.Escape(g)
            .Replace(@"\*\*/", "(.*/)?")
            .Replace(@"\*\*", ".*")
            .Replace(@"\*", "[^/]*")
            .Replace(@"\?", "[^/]") + "$";
        return new Regex(rx, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    public static bool MatchesAny(string path, IEnumerable<string> globs)
    {
        var p = path.Replace('\\', '/');
        return globs.Any(g => ToRegex(g).IsMatch(p));
    }
}

/// <summary>Reads a Visual Studio TRX (`dotnet test --logger trx`) to validate `tests` checks.</summary>
public static class TrxReader
{
    public sealed record TrxResult(string ClassName, string TestName, string Outcome);

    public sealed record TrxRun(DateTimeOffset? Start, IReadOnlyList<TrxResult> Results);

    public static TrxRun Read(string path)
    {
        var doc = XDocument.Load(path);
        XNamespace ns = doc.Root?.Name.Namespace ?? XNamespace.None;
        DateTimeOffset? start = null;
        var times = doc.Root?.Element(ns + "Times");
        if (times?.Attribute("start")?.Value is { } s && DateTimeOffset.TryParse(s, out var st)) start = st;

        var classById = doc.Descendants(ns + "UnitTest")
            .Where(u => u.Attribute("id") != null)
            .ToDictionary(
                u => u.Attribute("id")!.Value,
                u => ShortClass(u.Element(ns + "TestMethod")?.Attribute("className")?.Value ?? ""));

        var results = doc.Descendants(ns + "UnitTestResult")
            .Select(r => new TrxResult(
                classById.GetValueOrDefault(r.Attribute("testId")?.Value ?? "", ""),
                r.Attribute("testName")?.Value ?? "",
                r.Attribute("outcome")?.Value ?? ""))
            .ToList();
        return new TrxRun(start, results);
    }

    /// <summary>A required name is "ClassName" (every result in that class) or "ClassName.Method"
    /// (every result whose test name is Method, or Method(args) for parameterised cases). It passes
    /// when at least one result matches and every match Passed.</summary>
    public static (bool Ok, string Detail) Check(TrxRun run, IEnumerable<string> required)
    {
        var problems = new List<string>();
        var total = 0;
        foreach (var name in required)
        {
            var dot = name.IndexOf('.');
            var cls = dot < 0 ? name : name[..dot];
            var method = dot < 0 ? null : name[(dot + 1)..];
            var hits = run.Results.Where(r => string.Equals(r.ClassName, cls, StringComparison.Ordinal)
                && (method == null || r.TestName == method || r.TestName.StartsWith(method + "(", StringComparison.Ordinal))).ToList();
            total += hits.Count;
            if (hits.Count == 0) problems.Add($"{name}: no results in the TRX");
            else if (hits.Any(h => h.Outcome != "Passed"))
                problems.Add($"{name}: {hits.Count(h => h.Outcome != "Passed")} of {hits.Count} not Passed");
        }
        return problems.Count == 0 ? (true, $"{total} results, all Passed") : (false, string.Join("; ", problems));
    }

    private static string ShortClass(string full) => full.Contains('.') ? full[(full.LastIndexOf('.') + 1)..] : full;
}
