using System.Text.RegularExpressions;
using Prose.Core.Interfaces;

namespace Prose.Core.Services;

/// <summary>
/// Scans the source tree (v3/Prose.*, root .js, tools/, engine/scripts/) to produce an
/// inventory of every service, CLI verb, MCP tool, and standalone script — the automated,
/// repeatable version of the manual 8-agent "Engine Manifest" service audit run 2026-09-01.
/// Originally powered a Blazor `/architecture` page (deleted 2026-08-13); resurrected
/// 2026-09-01 as the `prose --architecture-scan` CLI command instead (see
/// <c>Prose.Cli/Cli/ArchitectureScanCli.cs</c>), with its <c>ScanServices</c>/<c>ScanCli</c>
/// project paths corrected — they still pointed at the deleted Prose.Shared/Prose.Blazor
/// projects, which is why this service went dead in the first place. Razor-page scanning was
/// dropped entirely: the live UI, Prose.ObserverUi, is a tab-shell SPA with zero `@page`-routed
/// components, so there is nothing meaningful left for that scan to find.
/// </summary>
public class ProjectArchitectureService
{
    private readonly IPathProvider paths;
    private ArchitectureSnapshot? cached;
    private readonly object cacheLock = new();

    public ProjectArchitectureService(IPathProvider paths) => this.paths = paths;

    public ArchitectureSnapshot Scan(bool force = false)
    {
        lock (cacheLock)
        {
            if (cached != null && !force) return cached;
            cached = BuildSnapshot();
            return cached;
        }
    }

    private ArchitectureSnapshot BuildSnapshot()
    {
        var repoRoot = paths.DataRoot;
        var v3 = Path.Combine(repoRoot, "v3");
        var snapshot = new ArchitectureSnapshot { RepoRoot = repoRoot, ScannedAt = DateTime.UtcNow };

        if (Directory.Exists(v3))
        {
            snapshot.Services = ScanServices(v3);
            // RazorPages intentionally left empty — see class doc comment. Prose.ObserverUi has
            // no @page-routed components, and the Blazor host that ScanRazorPages originally
            // targeted (Prose.Shared/Prose.Blazor) was deleted 2026-08-13.
            snapshot.CliCommands = ScanCli(v3);
            snapshot.McpTools = ScanMcp(v3);
            snapshot.DiRegistrations = ScanDiRegistrations(v3);
        }

        snapshot.Scripts = ScanScripts(repoRoot);
        snapshot.DuplicateClusters = FindDuplicates(snapshot);
        return snapshot;
    }

    // ── Services ──────────────────────────────────────────────────────────

    // Prose.Shared/Prose.Blazor were deleted 2026-08-13 along with the Blazor host — Prose.Core
    // is the only project with a /Services/ directory today.
    static readonly string[] ServiceProjects = ["Prose.Core"];

    private List<ServiceEntry> ScanServices(string v3)
    {
        var list = new List<ServiceEntry>();
        foreach (var proj in ServiceProjects)
        {
            var dir = Path.Combine(v3, proj, "Services");
            if (!Directory.Exists(dir)) continue;

            foreach (var file in Directory.EnumerateFiles(dir, "*.cs", SearchOption.AllDirectories))
            {
                var text = SafeRead(file);
                if (text == null) continue;

                // Find each top-level class / interface in the file.
                var classRx = new Regex(@"(?ms)(?<doc>(?:^[\t ]*///[^\n]*\n[\t ]*)*)\s*public\s+(?:abstract\s+|sealed\s+|static\s+|partial\s+)*(?<kind>class|interface|record)\s+(?<name>[A-Z][A-Za-z0-9_]*)");
                foreach (Match m in classRx.Matches(text))
                {
                    var name = m.Groups["name"].Value;
                    if (!IsServicelike(name)) continue;
                    list.Add(new ServiceEntry
                    {
                        Name = name,
                        Kind = m.Groups["kind"].Value,
                        Project = proj,
                        File = Rel(v3, file),
                        Summary = ParseDocSummary(m.Groups["doc"].Value),
                    });
                }
            }
        }
        return list.OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    static bool IsServicelike(string name)
    {
        // Any class in /Services/ counts; we just dedupe trivial helpers.
        if (name.EndsWith("Options") || name.EndsWith("Args") || name.EndsWith("Result")) return false;
        if (name.EndsWith("Dto") || name.EndsWith("Request") || name.EndsWith("Response")) return false;
        return true;
    }

    static string ParseDocSummary(string docBlock)
    {
        if (string.IsNullOrWhiteSpace(docBlock)) return "";
        // Strip /// and pull out summary contents
        var lines = docBlock.Split('\n')
            .Select(l => Regex.Replace(l.TrimStart(), @"^///\s?", ""))
            .ToArray();
        var joined = string.Join(' ', lines);
        var sm = Regex.Match(joined, @"<summary>(?<s>.*?)</summary>", RegexOptions.Singleline);
        if (sm.Success) joined = sm.Groups["s"].Value;
        joined = Regex.Replace(joined, @"<[^>]+>", "");
        return Regex.Replace(joined, @"\s+", " ").Trim();
    }

    // ── DI registrations (canonical service list) ─────────────────────────

    private List<DiRegistration> ScanDiRegistrations(string v3)
    {
        var file = Path.Combine(v3, "Prose.Core", "Extensions", "ServiceCollectionExtensions.cs");
        var text = SafeRead(file);
        if (text == null) return [];

        // Match: services.Add(Singleton|Scoped|Transient)<Type>...   or   <IFace, Impl>...
        var rx = new Regex(@"services\.Add(?<lifetime>Singleton|Scoped|Transient)(?:<(?<types>[^>]+)>)?\s*\(");
        var list = new List<DiRegistration>();
        foreach (Match m in rx.Matches(text))
        {
            var types = m.Groups["types"].Value.Trim();
            if (string.IsNullOrEmpty(types)) continue;
            list.Add(new DiRegistration
            {
                Lifetime = m.Groups["lifetime"].Value,
                Types = types,
            });
        }
        return list;
    }

    // ── CLI commands ──────────────────────────────────────────────────────

    private List<CliCommand> ScanCli(string v3)
    {
        var dir = Path.Combine(v3, "Prose.Cli", "Cli");
        if (!Directory.Exists(dir)) return [];

        var list = new List<CliCommand>();
        foreach (var file in Directory.EnumerateFiles(dir, "*.cs"))
        {
            var text = SafeRead(file);
            if (text == null) continue;

            var className = Path.GetFileNameWithoutExtension(file);
            var verbs = Regex.Matches(text, @"args\.Contains\(""(?<v>--[a-z-]+)""\)")
                .Cast<Match>().Select(m => m.Groups["v"].Value).Distinct().ToList();

            // Pull every "  --verb subcommand …" usage line from any PrintUsage block.
            // Triple-quoted blocks survive in raw form — match indented "--…" lines.
            var usage = ExtractUsageBlock(text);
            var subcommands = Regex.Matches(usage ?? "", @"^[ \t]+(?<line>(?:ss\s+)?--[a-z][^\r\n]+)", RegexOptions.Multiline)
                .Cast<Match>().Select(m => m.Groups["line"].Value.Trim())
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToList();

            // Class-level XML doc summary.
            var summary = Regex.Match(text, @"(?ms)(?<doc>(?:///[^\n]*\n[ \t]*)+)\s*public\s+static\s+class\s+" + Regex.Escape(className));
            var summaryText = summary.Success ? ParseDocSummary(summary.Groups["doc"].Value) : "";

            list.Add(new CliCommand
            {
                Class = className,
                File = Rel(v3, file),
                Verbs = verbs,
                Subcommands = subcommands,
                Summary = summaryText,
                RawUsage = usage ?? "",
            });
        }
        return list.OrderBy(c => c.Class, StringComparer.OrdinalIgnoreCase).ToList();
    }

    static string? ExtractUsageBlock(string text)
    {
        // Find """…""" raw-string blocks that contain "Usage:".
        var rx = new Regex(@"""""""(?<body>[^""]*?Usage:[^""]*?)""""""", RegexOptions.Singleline);
        var m = rx.Match(text);
        if (m.Success) return m.Groups["body"].Value;

        // Fallback: look at everything after PrintUsage()' opening for Console.WriteLine lines
        var p = text.IndexOf("static void PrintUsage", StringComparison.Ordinal);
        if (p < 0) return null;
        var slice = text[p..Math.Min(text.Length, p + 4000)];
        var lines = Regex.Matches(slice, @"Console\.(?:Write|Error\.Write)Line\(""(?<l>[^""]*)""\)")
            .Cast<Match>().Select(m => m.Groups["l"].Value);
        return string.Join('\n', lines);
    }

    // ── MCP tools ─────────────────────────────────────────────────────────

    private List<McpTool> ScanMcp(string v3)
    {
        var dir = Path.Combine(v3, "Prose.Mcp");
        if (!Directory.Exists(dir)) return [];

        var list = new List<McpTool>();
        foreach (var file in Directory.EnumerateFiles(dir, "Tools*.cs"))
        {
            var text = SafeRead(file);
            if (text == null) continue;

            // [McpServerTool, Description("...")] above a public method.
            var rx = new Regex(@"\[McpServerTool\s*,\s*Description\(""(?<desc>(?:\\.|[^""\\])*)""\)\]\s*(?:\r?\n[ \t]*\[[^\]]+\]\s*)*\s*public\s+[A-Za-z<>?\s]+?\s+(?<method>[A-Z][A-Za-z0-9_]*)\s*\((?<params>[^)]*)\)", RegexOptions.Singleline);
            foreach (Match m in rx.Matches(text))
            {
                var paramText = Regex.Replace(m.Groups["params"].Value, @"\[Description\(""[^""]*""\)\]\s*", "");
                paramText = Regex.Replace(paramText, @"\s+", " ").Trim();

                list.Add(new McpTool
                {
                    Name = m.Groups["method"].Value,
                    Description = Regex.Unescape(m.Groups["desc"].Value),
                    Parameters = paramText,
                    File = Rel(v3, file),
                });
            }
        }
        return list.OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    // ── Standalone scripts (.js) ──────────────────────────────────────────

    private List<ScriptEntry> ScanScripts(string repoRoot)
    {
        var list = new List<ScriptEntry>();
        var dirs = new[]
        {
            (Folder: "v3",            Path: Path.Combine(repoRoot, "v3")),
            (Folder: "v3/python",     Path: Path.Combine(repoRoot, "v3", "python")),
            (Folder: "tools",         Path: Path.Combine(repoRoot, "tools")),
            (Folder: "scripts",       Path: Path.Combine(repoRoot, "scripts")),
            (Folder: "engine/scripts",Path: Path.Combine(repoRoot, "engine", "scripts")),
            (Folder: "(root)",        Path: repoRoot),
        };

        foreach (var (folder, path) in dirs)
        {
            if (!Directory.Exists(path)) continue;
            foreach (var file in Directory.EnumerateFiles(path, "*.js", SearchOption.TopDirectoryOnly))
            {
                list.Add(new ScriptEntry
                {
                    Name = Path.GetFileName(file),
                    Folder = folder,
                    Purpose = ExtractScriptPurpose(file),
                    SizeKb = Math.Round(new FileInfo(file).Length / 1024.0, 1),
                });
            }
            foreach (var file in Directory.EnumerateFiles(path, "*.py", SearchOption.TopDirectoryOnly))
            {
                list.Add(new ScriptEntry
                {
                    Name = Path.GetFileName(file),
                    Folder = folder,
                    Purpose = ExtractScriptPurpose(file),
                    SizeKb = Math.Round(new FileInfo(file).Length / 1024.0, 1),
                });
            }
        }
        return list.OrderBy(s => s.Folder).ThenBy(s => s.Name).ToList();
    }

    static string ExtractScriptPurpose(string file)
    {
        try
        {
            using var reader = new StreamReader(file);
            var lines = new List<string>();
            for (int i = 0; i < 8; i++)
            {
                var l = reader.ReadLine();
                if (l == null) break;
                lines.Add(l.Trim());
            }
            // First contiguous comment block at top of file.
            var sb = new System.Text.StringBuilder();
            foreach (var l in lines.SkipWhile(l => l.StartsWith("#!") || l.Length == 0))
            {
                if (l.StartsWith("//")) sb.Append(l[2..].Trim()).Append(' ');
                else if (l.StartsWith("#")) sb.Append(l[1..].Trim()).Append(' ');
                else if (l.StartsWith("/*") || l.StartsWith("*") || l.StartsWith("\"\"\"")) sb.Append(Regex.Replace(l, @"^[/*\""]+|[*/\""]+$", "").Trim()).Append(' ');
                else break;
            }
            var s = sb.ToString().Trim();
            return s.Length > 240 ? s[..240] + "…" : s;
        }
        catch { return ""; }
    }

    // ── Duplicate detection ───────────────────────────────────────────────

    private List<DuplicateCluster> FindDuplicates(ArchitectureSnapshot snap)
    {
        // Token-overlap clustering on service/CLI/MCP names.
        var items = new List<(string Source, string Name, string File)>();
        items.AddRange(snap.Services.Select(s => ("service", s.Name, s.File)));
        items.AddRange(snap.CliCommands.Select(c => ("cli", c.Class, c.File)));
        items.AddRange(snap.McpTools.Select(t => ("mcp", t.Name, t.File)));

        // Tokens = camel-case splits, lowercase, drop trivial words.
        static IEnumerable<string> Tokenize(string s) =>
            Regex.Split(s, @"(?<=[a-z0-9])(?=[A-Z])")
                .Select(t => t.ToLowerInvariant())
                .Where(t => t.Length >= 4 && t is not "service" and not "cli" and not "tool" and not "tools");

        var byToken = items
            .SelectMany(i => Tokenize(i.Name).Select(t => (Token: t, Item: i)))
            .GroupBy(x => x.Token)
            .Where(g => g.Select(x => x.Item.Name).Distinct().Count() >= 2)
            .OrderByDescending(g => g.Count())
            .Take(40)
            .ToList();

        return byToken.Select(g => new DuplicateCluster
        {
            Token = g.Key,
            Members = g.Select(x => new DuplicateMember
            {
                Source = x.Item.Source,
                Name = x.Item.Name,
                File = x.Item.File,
            }).DistinctBy(m => m.Name).ToList(),
        }).ToList();
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    static string Rel(string root, string path)
    {
        try { return Path.GetRelativePath(Directory.GetParent(root)?.FullName ?? root, path).Replace('\\', '/'); }
        catch { return path; }
    }

    static string? SafeRead(string file)
    {
        try { return File.ReadAllText(file); }
        catch { return null; }
    }

    static string CleanText(string s) => Regex.Replace(s, @"\s+", " ").Trim();
}

// ── DTOs ──────────────────────────────────────────────────────────────────

public class ArchitectureSnapshot
{
    public string RepoRoot { get; set; } = "";
    public DateTime ScannedAt { get; set; }
    public List<ServiceEntry> Services { get; set; } = [];
    public List<DiRegistration> DiRegistrations { get; set; } = [];
    public List<RazorPageEntry> RazorPages { get; set; } = [];
    public List<CliCommand> CliCommands { get; set; } = [];
    public List<McpTool> McpTools { get; set; } = [];
    public List<ScriptEntry> Scripts { get; set; } = [];
    public List<DuplicateCluster> DuplicateClusters { get; set; } = [];
}

public class ServiceEntry
{
    public string Name { get; set; } = "";
    public string Kind { get; set; } = "";        // class / interface / record
    public string Project { get; set; } = "";
    public string File { get; set; } = "";
    public string Summary { get; set; } = "";
}

public class DiRegistration
{
    public string Lifetime { get; set; } = "";
    public string Types { get; set; } = "";
}

public class RazorPageEntry
{
    public string Route { get; set; } = "";
    public string Title { get; set; } = "";
    public string File { get; set; } = "";
    public string Roles { get; set; } = "";
}

public class CliCommand
{
    public string Class { get; set; } = "";
    public string File { get; set; } = "";
    public List<string> Verbs { get; set; } = [];
    public List<string> Subcommands { get; set; } = [];
    public string Summary { get; set; } = "";
    public string RawUsage { get; set; } = "";
}

public class McpTool
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Parameters { get; set; } = "";
    public string File { get; set; } = "";
}

public class ScriptEntry
{
    public string Name { get; set; } = "";
    public string Folder { get; set; } = "";
    public string Purpose { get; set; } = "";
    public double SizeKb { get; set; }
}

public class DuplicateCluster
{
    public string Token { get; set; } = "";
    public List<DuplicateMember> Members { get; set; } = [];
}

public class DuplicateMember
{
    public string Source { get; set; } = "";
    public string Name { get; set; } = "";
    public string File { get; set; } = "";
}
