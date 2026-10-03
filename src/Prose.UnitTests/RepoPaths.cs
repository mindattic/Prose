using System.Runtime.CompilerServices;

namespace Prose.UnitTests;

/// <summary>
/// The one place a test finds the Prose repository root, so tests that read committed files
/// (legion.json, engine/data, tools/kdp, src/**, the WriterUi stylesheet) pass no matter where the
/// test assembly was built: in the repo's own bin/, an isolated <c>bin_&lt;tag&gt;</c>, or an output
/// folder outside the repo (<c>dotnet build -o …</c>, a CI staging dir).
///
/// Resolution order, first hit wins:
/// <list type="number">
///   <item><c>PROSE_REPO_PATH</c> (the same variable <c>KdpManifestService.FindRepoRoot</c> honours).
///         When set it must name a Prose repo root; a wrong value fails loudly rather than falling
///         through to a different checkout.</item>
///   <item>Walk up from the test assembly's directory.</item>
///   <item>Walk up from this source file's compile-time path (<c>[CallerFilePath]</c>), which is in
///         the repo that produced the build even when the binaries are not.</item>
/// </list>
/// A directory is the root when it holds <c>legion.json</c> and <c>src/Prose.Core</c>.
/// </summary>
public static class RepoPaths
{
    public const string EnvVar = "PROSE_REPO_PATH";

    private static readonly Lazy<string> root = new(() =>
        Resolve(Environment.GetEnvironmentVariable(EnvVar), AppContext.BaseDirectory, ThisFile()));

    /// <summary>The repo root (the folder holding legion.json and src/).</summary>
    public static string Root => root.Value;

    /// <summary>A path under the repo root, e.g. <c>RepoPaths.Combine("engine", "data")</c>.</summary>
    public static string Combine(params string[] parts) => Path.Combine([Root, .. parts]);

    public static bool IsRepoRoot(string dir) =>
        File.Exists(Path.Combine(dir, "legion.json")) && Directory.Exists(Path.Combine(dir, "src", "Prose.Core"));

    /// <summary>The resolution itself, with every input explicit so it can be tested.</summary>
    public static string Resolve(string? envValue, string assemblyDir, string sourceFile)
    {
        if (!string.IsNullOrWhiteSpace(envValue))
        {
            var full = Path.GetFullPath(envValue);
            if (IsRepoRoot(full)) return full;
            throw new InvalidOperationException(
                $"{EnvVar}='{envValue}' is not a Prose repo root (no legion.json + src/Prose.Core there).");
        }

        if (WalkUp(assemblyDir) is { } fromAssembly) return fromAssembly;
        if (!string.IsNullOrEmpty(sourceFile) && WalkUp(Path.GetDirectoryName(sourceFile)) is { } fromSource) return fromSource;

        throw new InvalidOperationException(
            $"Could not locate the Prose repo root: none above the test assembly ({assemblyDir}) or this " +
            $"source file ({sourceFile}). Set {EnvVar} to the repo root.");
    }

    private static string? WalkUp(string? start)
    {
        for (var dir = string.IsNullOrEmpty(start) ? null : new DirectoryInfo(start); dir != null; dir = dir.Parent)
            if (IsRepoRoot(dir.FullName)) return dir.FullName;
        return null;
    }

    private static string ThisFile([CallerFilePath] string path = "") => path;
}
