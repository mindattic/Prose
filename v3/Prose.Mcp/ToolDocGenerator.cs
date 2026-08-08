using System.ComponentModel;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Prose.Mcp;

/// <summary>
/// Generates <c>docs/MCP_TOOLS.md</c> by REFLECTING over this assembly's
/// <c>[McpServerToolType]</c> classes and their <c>[McpServerTool]</c> methods —
/// the exact same source the MCP host registers via <c>WithToolsFromAssembly()</c>.
/// Because the doc is generated from the attributes, it can never drift from the
/// running server. Re-run it whenever a tool is added/changed:
/// <code>dotnet run --project v3/Prose.Mcp -- --export-tools docs/MCP_TOOLS.md</code>
/// (the SessionStart/pre-commit wiring keeps it current automatically — see README).
/// </summary>
public static class ToolDocGenerator
{
    public static int Generate(string outputPath)
    {
        var asm = typeof(ToolDocGenerator).Assembly;

        var toolTypes = asm.GetTypes()
            .Where(t => HasAttr(t.GetCustomAttributes(), "McpServerToolTypeAttribute"))
            .OrderBy(t => FamilyLabel(t.Name), StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Collect first so we can print an accurate total in the header.
        var families = new List<(string label, string typeName, List<ToolDoc> tools)>();
        var total = 0;
        foreach (var t in toolTypes)
        {
            var tools = t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(m => HasAttr(m.GetCustomAttributes(), "McpServerToolAttribute"))
                .Select(Describe)
                .OrderBy(d => d.Name, StringComparer.Ordinal)
                .ToList();
            if (tools.Count == 0) continue;
            families.Add((FamilyLabel(t.Name), t.Name, tools));
            total += tools.Count;
        }

        var sb = new StringBuilder();
        sb.AppendLine("# Prose MCP Tools");
        sb.AppendLine();
        sb.AppendLine("> **GENERATED — do not hand-edit.** Produced by `ToolDocGenerator` from the");
        sb.AppendLine("> `[McpServerTool]` + `[Description]` attributes in `v3/Prose.Mcp/Tools*.cs`,");
        sb.AppendLine("> the same source the MCP host registers via `WithToolsFromAssembly()`. To refresh:");
        sb.AppendLine("> ");
        sb.AppendLine("> ```powershell");
        sb.AppendLine("> dotnet run --project v3/Prose.Mcp -- --export-tools docs/MCP_TOOLS.md");
        sb.AppendLine("> ```");
        sb.AppendLine(">");
        sb.AppendLine("> All tools are MCP-prefixed `mcp__prose__<name>` by the client. Most return a");
        sb.AppendLine("> JSON string; the canon is the SQL database, scoped to the active Universe.");
        sb.AppendLine();
        sb.AppendLine($"**{total} tools** across **{families.Count} tool families.**");
        sb.AppendLine();

        // Table of contents.
        sb.AppendLine("## Families");
        sb.AppendLine();
        sb.AppendLine("| Family | Tools |");
        sb.AppendLine("| --- | --- |");
        foreach (var f in families)
            sb.AppendLine($"| [{f.label}](#{Anchor(f.label)}) | {f.tools.Count} |");
        sb.AppendLine();

        foreach (var f in families)
        {
            sb.AppendLine($"## {f.label}");
            sb.AppendLine();
            sb.AppendLine($"<sub>`{f.typeName}`</sub>");
            sb.AppendLine();
            foreach (var d in f.tools)
            {
                sb.AppendLine($"### `{d.Name}`");
                sb.AppendLine();
                sb.AppendLine(d.Description.Length == 0 ? "_(no description)_" : d.Description);
                sb.AppendLine();
                if (d.Parameters.Count > 0)
                {
                    foreach (var p in d.Parameters)
                    {
                        var req = p.Optional ? "optional" : "required";
                        var desc = string.IsNullOrWhiteSpace(p.Desc) ? "" : $" — {p.Desc}";
                        sb.AppendLine($"- `{p.Name}` ({p.Type}, {req}){desc}");
                    }
                    sb.AppendLine();
                }
                else
                {
                    sb.AppendLine("- _(no parameters)_");
                    sb.AppendLine();
                }
            }
        }

        var dir = Path.GetDirectoryName(Path.GetFullPath(outputPath));
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        File.WriteAllText(outputPath, sb.ToString());
        return total;
    }

    private sealed record ToolDoc(string Name, string Description, List<ParamDoc> Parameters);
    private sealed record ParamDoc(string Name, string Type, string Desc, bool Optional);

    private static ToolDoc Describe(MethodInfo m)
    {
        var toolAttr = m.GetCustomAttributes().First(a => a.GetType().Name == "McpServerToolAttribute");
        var explicitName = toolAttr.GetType().GetProperty("Name")?.GetValue(toolAttr) as string;
        var name = string.IsNullOrWhiteSpace(explicitName) ? Snake(m.Name) : explicitName!;

        var desc = (m.GetCustomAttribute<DescriptionAttribute>()?.Description ?? "")
            .Replace("\r", " ").Replace("\n", " ").Trim();
        desc = Regex.Replace(desc, " {2,}", " ");

        var ps = m.GetParameters()
            // Drop framework-injected params (CancellationToken, IServiceProvider, etc.).
            .Where(p => p.ParameterType != typeof(CancellationToken))
            .Select(p => new ParamDoc(
                p.Name ?? "?",
                FriendlyType(p.ParameterType),
                (p.GetCustomAttribute<DescriptionAttribute>()?.Description ?? "").Replace("\r", " ").Replace("\n", " ").Trim(),
                p.HasDefaultValue || IsNullable(p)))
            .ToList();

        return new ToolDoc(name, desc, ps);
    }

    private static bool HasAttr(IEnumerable<Attribute> attrs, string name) =>
        attrs.Any(a => a.GetType().Name == name);

    /// <summary>PascalCase method name → snake_case tool name (the MCP SDK default).</summary>
    private static string Snake(string pascal) =>
        Regex.Replace(pascal, "(?<!^)([A-Z])", "_$1").ToLowerInvariant();

    /// <summary>"WorldModellingTools" → "World Modelling".</summary>
    private static string FamilyLabel(string typeName)
    {
        var n = typeName.EndsWith("Tools") ? typeName[..^"Tools".Length] : typeName;
        if (n.Length == 0) n = typeName;
        return Regex.Replace(n, "(?<!^)([A-Z])", " $1");
    }

    private static string Anchor(string label) =>
        Regex.Replace(label.ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');

    private static bool IsNullable(ParameterInfo p)
    {
        if (Nullable.GetUnderlyingType(p.ParameterType) != null) return true; // Nullable<T>
        if (p.ParameterType.IsValueType) return false;
        // Reference type: check the C# 8 nullable annotation.
        return new NullabilityInfoContext().Create(p).WriteState == NullabilityState.Nullable;
    }

    private static string FriendlyType(Type t)
    {
        var u = Nullable.GetUnderlyingType(t) ?? t;
        var name = u == typeof(int) ? "int"
            : u == typeof(long) ? "long"
            : u == typeof(bool) ? "bool"
            : u == typeof(double) ? "double"
            : u == typeof(string) ? "string"
            : u == typeof(Guid) ? "guid"
            : u.Name;
        return name;
    }
}
