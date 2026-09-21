using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Prose.Hub.Contracts;

namespace Prose.UnitTests;

/// <summary>
/// Proves that every CLI command the product advertises can actually be dispatched.
///
/// <para><b>Why this exists.</b> <c>Prose.Cli</c> is almost entirely a forwarder: its dispatch
/// chain names each handler class as a <em>string literal</em> — <c>ForwardAsync("SeedCli", args)</c>
/// — and the Hub resolves that string by reflection at runtime. Nothing checks it at compile time.
/// Rename or delete a handler class and the build stays green, the tests stay green, and exactly
/// one command starts answering <c>unknown_handler_class</c> — discovered only when someone
/// happens to run it. With 303 such literals, that is not a hypothetical.</para>
///
/// <para>These tests call the Hub's own resolution rules
/// (<see cref="DispatchResolution"/>) rather than re-implementing them. A guard carrying its own
/// copy of the rules passes while the Hub rejects the call, which would make it worse than
/// nothing.</para>
/// </summary>
[TestFixture]
[Category("Static")]
public class CliWiringTests
{
    /// <summary>
    /// Handlers invoked in-process by Program.cs instead of being forwarded to the Hub, so they
    /// never appear as a dispatch string. Both are deliberate: <c>--worker-mode</c> is explicitly
    /// exempt from the Hub-reachable gate (it IS the worker the Hub talks to), and
    /// <c>--estimate-cost</c> runs without a service provider at all.
    /// </summary>
    private static readonly HashSet<string> InProcessHandlers =
    [
        "WorkerModeCli",
        "EstimateCostCli",
    ];

    /// <summary>Any string literal in Program.cs that names a CLI handler class.</summary>
    private static readonly Regex HandlerLiteral = new(@"""([A-Za-z0-9_]+Cli)""", RegexOptions.Compiled);

    /// <summary>
    /// A forward call with a literal handler name, captured together with the remainder of its
    /// statement so the optional <c>method:</c> and <c>extraParamValue:</c> arguments can be read.
    /// Spans newlines because a few calls wrap.
    /// </summary>
    private static readonly Regex ForwardCall = new(
        @"Forward(?:WithCostGate)?Async\s*\(\s*""(?<handler>[A-Za-z0-9_]+Cli)""(?<rest>[^;]*)",
        RegexOptions.Compiled | RegexOptions.Singleline);

    private static readonly Regex MethodArg = new(@"method\s*:\s*""(?<m>[A-Za-z0-9_]+)""", RegexOptions.Compiled);

    [Test]
    public void EveryHandlerNameInProgramResolvesToARealClass()
    {
        var (text, path) = ReadDispatchChain();
        var matches = HandlerLiteral.Matches(text);

        // The scanner must prove it can see something before its silence means anything. If a
        // refactor changes how handlers are named, this test must fail loudly rather than pass by
        // matching nothing at all.
        Assert.That(matches.Count, Is.GreaterThan(250),
            $"Only {matches.Count} handler-name literals found in {path} — the scanner has stopped " +
            "seeing the dispatch chain. Fix the pattern; do not lower this bound.");

        var assemblies = CliAssemblies();
        var unresolved = new List<string>();
        foreach (var name in matches.Select(m => m.Groups[1].Value).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal))
            if (DispatchResolution.ResolveCliHandlerType(assemblies, name) == null)
                unresolved.Add(name);

        Assert.That(unresolved, Is.Empty,
            "These handler names appear in Program.cs but resolve to no class in namespace " +
            "Prose.Cli. Each is a command that answers unknown_handler_class at runtime:\n  " +
            string.Join("\n  ", unresolved));
    }

    [Test]
    public void EveryForwardedCallBindsWithoutANullParameter()
    {
        var (text, path) = ReadDispatchChain();
        var calls = ForwardCall.Matches(text);

        Assert.That(calls.Count, Is.GreaterThan(250),
            $"Only {calls.Count} forward calls found in {path} — the scanner has stopped seeing " +
            "them. Fix the pattern; do not lower this bound.");

        var assemblies = CliAssemblies();
        var failures = new List<string>();

        foreach (Match call in calls)
        {
            var handler = call.Groups["handler"].Value;
            var rest = call.Groups["rest"].Value;
            var method = MethodArg.Match(rest) is { Success: true } mm ? mm.Groups["m"].Value : null;
            var hasExtra = rest.Contains("extraParamValue", StringComparison.Ordinal);
            var line = LineOf(text, call.Index);

            var type = DispatchResolution.ResolveCliHandlerType(assemblies, handler);
            if (type == null)
            {
                failures.Add($"{path}:{line} → \"{handler}\" — unknown_handler_class");
                continue;
            }

            if (!DispatchResolution.TryBindCli(type, method, hasExtra, out var binding, out var error))
            {
                failures.Add($"{path}:{line} → \"{handler}\"{(method is null ? "" : $".{method}")} — {error}");
                continue;
            }

            // An unbound parameter does not fail at invoke time. It is passed as null and surfaces
            // later as an ArgumentNullException from inside the handler, once its Task is awaited —
            // which reads as a bug in the command rather than a wiring mistake.
            if (binding!.HasUnboundParameter)
            {
                var shape = string.Join(", ", binding.Method.GetParameters()
                    .Select((p, i) => $"{p.ParameterType.Name} {p.Name} ⇒ {binding.Parameters[i]}"));
                failures.Add($"{path}:{line} → \"{handler}\"{(method is null ? "" : $".{method}")} — " +
                             $"parameter would bind to null: {shape}");
            }
        }

        Assert.That(failures, Is.Empty,
            "Forwarded CLI calls that the Hub could not dispatch as written:\n  " + string.Join("\n  ", failures));
    }

    [Test]
    public void EveryHandlerClassIsReachableFromTheDispatchChain()
    {
        var (text, _) = ReadDispatchChain();
        var named = HandlerLiteral.Matches(text)
            .Select(m => m.Groups[1].Value)
            .ToHashSet(StringComparer.Ordinal);

        var declared = CliAssemblies()
            .SelectMany(a => { try { return a.GetTypes(); } catch { return []; } })
            .Where(t => t.Namespace == "Prose.Cli" && t.Name.EndsWith("Cli", StringComparison.Ordinal))
            .Where(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static)
                         .Any(m => m.Name is "RunAsync" or "Run"))
            .Select(t => t.Name)
            .ToHashSet(StringComparer.Ordinal);

        var orphans = declared.Except(named).Except(InProcessHandlers).Order(StringComparer.Ordinal).ToList();

        Assert.That(orphans, Is.Empty,
            "These handler classes have a Run/RunAsync entry point but nothing in Program.cs names " +
            "them, so no command can reach them. Either wire them up, or add them to " +
            $"{nameof(InProcessHandlers)} with the reason:\n  " + string.Join("\n  ", orphans));

        // The allow-list must not outlive what it excuses, or it quietly starts hiding real orphans.
        var stale = InProcessHandlers.Except(declared).Order(StringComparer.Ordinal).ToList();
        Assert.That(stale, Is.Empty,
            $"Stale {nameof(InProcessHandlers)} entries (class no longer exists — remove the line):\n  " +
            string.Join("\n  ", stale));
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    /// <summary>The assemblies the Hub would search, narrowed to the one that actually holds
    /// handlers. Resolution matches on namespace as well as name, so this is equivalent.</summary>
    private static Assembly[] CliAssemblies() => [typeof(Prose.Cli.HubCliClient).Assembly];

    /// <summary>
    /// The dispatch chain with comments removed. Program.cs discusses its own handler classes at
    /// length; a class name quoted inside a comment is documentation, not a dispatch string, and
    /// scanning it would make this guard fail for a rename that was already done correctly.
    /// Replacing rather than deleting keeps byte offsets intact so reported line numbers are real.
    /// </summary>
    private static (string Text, string Path) ReadDispatchChain()
    {
        var path = Path.Combine(FindRepoRoot(), "src", "Prose.Cli", "Program.cs");
        Assert.That(File.Exists(path), $"Dispatch chain not found at {path}");
        var text = File.ReadAllText(path);
        text = Regex.Replace(text, @"/\*.*?\*/", m => Blank(m.Value), RegexOptions.Singleline);
        text = Regex.Replace(text, @"//[^\n]*", m => Blank(m.Value));
        return (text, "Prose.Cli/Program.cs");
    }

    /// <summary>Same length, same newlines, no content — preserves line numbering.</summary>
    private static string Blank(string s) => new(s.Select(c => c == '\n' ? '\n' : ' ').ToArray());

    private static int LineOf(string text, int index) =>
        text.AsSpan(0, index).Count('\n') + 1;

    private static string? repoRoot;

    private static string FindRepoRoot()
    {
        if (repoRoot != null) return repoRoot;
        var dir = AppDomain.CurrentDomain.BaseDirectory;
        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir, "src", "Prose.Core"))) return repoRoot = dir;
            dir = Directory.GetParent(dir)?.FullName;
        }
        // Build artefacts may live outside the repo; fall back to this file's compile-time path.
        var fromSource = Directory.GetParent(SourceFilePath())?.Parent?.Parent?.FullName;
        if (fromSource != null && Directory.Exists(Path.Combine(fromSource, "src", "Prose.Core")))
            return repoRoot = fromSource;
        Assert.Fail("Could not locate the repo root (no src/Prose.Core above the test binary or this source file).");
        return "";
    }

    private static string SourceFilePath([CallerFilePath] string path = "") => path;
}
