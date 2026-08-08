using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Prose.Cli;

/// <summary>
/// prose --ml-audit [--slug &lt;nodeSlug&gt;] [--all] [--skip-gripes] [--json]
///
/// Runs the Python ML beat auditor against the trained model artifacts.
/// Writes ML-PROSE-SCORE findings to the Findings table for beats
/// predicted below the quality threshold.
///
/// Exit 0 = clean, 1 = advisory (≥1 Low finding), 2 = blocking (≥1 High finding).
///
/// Prerequisites:
///   1. Python venv set up: v3/ml/.venv/
///   2. Nightly model trained: python orchestrate/nightly_run.py --phases extract train_quality
/// </summary>
public static class MlAuditCli
{
    private static readonly string MlRoot = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "ml"));

    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        // Build the Python args to pass through
        var passthrough = new List<string>();
        if (args.Contains("--all"))         passthrough.Add("--all");
        if (args.Contains("--skip-gripes")) passthrough.Add("--skip-gripes");
        if (args.Contains("--json"))        passthrough.Add("--json");

        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == "--slug") { passthrough.AddRange(["--slug", args[i + 1]]); i++; }

        if (!passthrough.Contains("--all") && !passthrough.Any(a => a == "--slug"))
            passthrough.Add("--all");

        var pythonExe = GetPythonExe();
        var auditScript = Path.Combine(MlRoot, "audit", "beat_auditor.py");

        if (!File.Exists(pythonExe))
        {
            Console.Error.WriteLine($"[ml-audit] Python venv not found at: {pythonExe}");
            Console.Error.WriteLine("  Set up with: cd v3/ml && python -m venv .venv && .venv/Scripts/pip install -r requirements.txt");
            return 2;
        }
        if (!File.Exists(auditScript))
        {
            Console.Error.WriteLine($"[ml-audit] Audit script not found: {auditScript}");
            return 2;
        }

        var psi = new ProcessStartInfo
        {
            FileName               = pythonExe,
            Arguments              = $"\"{auditScript}\" {string.Join(" ", passthrough)}",
            WorkingDirectory       = MlRoot,
            RedirectStandardOutput = false,
            RedirectStandardError  = false,
            UseShellExecute        = false,
        };

        using var proc = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start Python process.");

        await proc.WaitForExitAsync();
        return proc.ExitCode;
    }

    private static string GetPythonExe()
    {
        var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        var venvPython = isWindows
            ? Path.Combine(MlRoot, ".venv", "Scripts", "python.exe")
            : Path.Combine(MlRoot, ".venv", "bin", "python");
        return venvPython;
    }
}
