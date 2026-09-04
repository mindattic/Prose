using System.Text;
using System.Text.RegularExpressions;

namespace Prose.Cli;

/// <summary>
/// Generates <c>docs/CLI_COMMANDS.md</c> from the dispatch chain in <c>Program.cs</c>.
///
/// <para><b>The gap it closes.</b> <c>docs/ARCHITECTURE.md</c> §8 recorded "CLI command reference:
/// does not exist yet — 257 command files, no generated reference" as an open documentation gap,
/// while the MCP side has had an auto-generated reference the whole time
/// (<c>Prose.Mcp/ToolDocGenerator.cs</c>). The asymmetry had a real cost: this session alone spent
/// several rounds discovering by trial that <c>--continuity search</c> requires <c>--text</c>,
/// that <c>--list-books</c> demands <c>--universe</c>, and that <c>--exclusion-rules --help</c>
/// just prints the list.</para>
///
/// <para><b>Why it parses source instead of reflecting.</b> The MCP generator can reflect because
/// every tool carries <c>[McpServerTool]</c> + <c>[Description]</c> attributes. CLI commands carry
/// nothing — they are <c>args.Contains("--flag")</c> blocks dispatching a handler class by name
/// through <c>HubCliClient</c>, so the flag literal, the handler and the prose describing them
/// exist only in <c>Program.cs</c>'s text. Attributing 257 handler classes to make reflection
/// possible would be a far larger and more invasive change than reading the dispatch chain that
/// is already the single source of truth for what the CLI actually does.</para>
///
/// <para><b>It reports its own blind spots.</b> A block whose comment gives no <c>prose …</c>
/// usage line is emitted with its flag and handler and marked undocumented rather than dropped —
/// a reference that silently omits what it could not parse is worse than one that admits the hole,
/// which is the same discipline the audit instruments in this engine follow.</para>
/// </summary>
public static class CommandDocGenerator
{
    /// <summary>The dispatch idiom: <c>if (args.Contains("--flag"))</c>, optionally OR-ing several
    /// flags, optionally with an <c>args[0] ==</c> form for the positional commands.</summary>
    private static readonly Regex GuardPattern = new(
        @"^\s*(?:else\s+)?if\s*\((?<cond>.+)\)\s*$", RegexOptions.Compiled);

    private static readonly Regex FlagPattern = new(
        @"""(?<flag>--[a-z0-9][a-z0-9-]*)""", RegexOptions.Compiled);

    private static readonly Regex ForwardPattern = new(
        @"Forward(?<gate>WithCostGate)?Async\(\s*""(?<handler>[A-Za-z0-9_]+)""", RegexOptions.Compiled);

    private static readonly Regex UsagePattern = new(
        @"^\s*//\s{2,}(?<usage>prose\s+.+)$", RegexOptions.Compiled);

    private static readonly Regex CommentPattern = new(@"^\s*//\s?(?<text>.*)$", RegexOptions.Compiled);

    private sealed record CommandDoc(
        List<string> Flags, string? Handler, bool CostGated, List<string> Usage, string Description);

    public static int Generate(string programPath, string outputPath)
    {
        var lines = File.ReadAllLines(programPath);
        var commands = new List<CommandDoc>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        for (var i = 0; i < lines.Length; i++)
        {
            var guard = GuardPattern.Match(lines[i]);
            if (!guard.Success) continue;

            var flags = FlagPattern.Matches(guard.Groups["cond"].Value)
                .Select(m => m.Groups["flag"].Value)
                .Distinct(StringComparer.Ordinal)
                .ToList();
            if (flags.Count == 0) continue;

            // The handler is named inside the block. Scan a bounded window rather than brace-
            // matching: the blocks are short, and a bounded scan cannot run away into the next
            // command if one of them is shaped unusually.
            string? handler = null;
            var costGated = false;
            for (var j = i + 1; j < Math.Min(lines.Length, i + 14); j++)
            {
                if (GuardPattern.IsMatch(lines[j])) break;
                var fwd = ForwardPattern.Match(lines[j]);
                if (!fwd.Success) continue;
                handler = fwd.Groups["handler"].Value;
                costGated = fwd.Groups["gate"].Success;
                break;
            }

            // Walk BACKWARDS over the contiguous comment block immediately above the guard.
            var usage = new List<string>();
            var prose = new List<string>();
            for (var k = i - 1; k >= 0; k--)
            {
                var line = lines[k];
                if (line.Trim().Length == 0) break;
                var c = CommentPattern.Match(line);
                if (!c.Success) break;

                var u = UsagePattern.Match(line);
                if (u.Success) usage.Insert(0, u.Groups["usage"].Value.Trim());
                else prose.Insert(0, c.Groups["text"].Value.Trim());
            }

            var description = string.Join(" ", prose).Trim();
            // The dispatch blocks all open with this label; it carries no information in a doc
            // whose every entry is a CLI command.
            if (description.StartsWith("CLI mode:", StringComparison.OrdinalIgnoreCase))
                description = description["CLI mode:".Length..].Trim();

            var key = string.Join("|", flags);
            if (!seen.Add(key)) continue;
            commands.Add(new CommandDoc(flags, handler, costGated, usage, description));
        }

        commands = commands.OrderBy(c => c.Flags[0], StringComparer.Ordinal).ToList();
        // Two different holes, counted separately so the header cannot overstate the coverage:
        // a command may carry a usage line but no prose, and the "add one above the guard" marker
        // below fires on the prose being missing, not on both being missing.
        var noDescription = commands.Count(c => c.Description.Length == 0);
        var noneAtAll = commands.Count(c => c.Usage.Count == 0 && c.Description.Length == 0);

        var sb = new StringBuilder();
        sb.AppendLine("# Prose CLI Commands");
        sb.AppendLine();
        sb.AppendLine("> **GENERATED — do not hand-edit.** Produced by `CommandDocGenerator` from the");
        sb.AppendLine("> dispatch chain in `v3/Prose.Cli/Program.cs`, which is the single source of truth");
        sb.AppendLine("> for what the CLI actually does. To refresh:");
        sb.AppendLine("> ");
        sb.AppendLine("> ```powershell");
        sb.AppendLine("> dotnet run --project v3/Prose.Cli -- --export-commands docs/CLI_COMMANDS.md");
        sb.AppendLine("> ```");
        sb.AppendLine(">");
        sb.AppendLine("> Every command executes **inside Prose.Hub** — `Prose.Cli` forwards to it and never");
        sb.AppendLine("> touches the database itself. A command marked **cost-gated** spends LLM money and");
        sb.AppendLine("> routes through the cost gate; everything else is deterministic or read-only.");
        sb.AppendLine("> Most commands require a `--universe <slug>` scope.");
        sb.AppendLine();
        sb.AppendLine($"**{commands.Count} commands.** {commands.Count(c => c.CostGated)} cost-gated. " +
                      $"{noDescription} have no description in their dispatch comment ({noneAtAll} have " +
                      "neither a description nor a usage line); they are listed anyway with whatever could " +
                      "be recovered, because a reference that silently omits what it could not parse is " +
                      "worse than one that admits the hole.");
        sb.AppendLine();

        foreach (var c in commands)
        {
            sb.AppendLine($"### {string.Join(" / ", c.Flags.Select(f => $"`{f}`"))}");
            sb.AppendLine();
            if (c.Usage.Count > 0)
            {
                sb.AppendLine("```");
                foreach (var u in c.Usage) sb.AppendLine(u);
                sb.AppendLine("```");
                sb.AppendLine();
            }
            sb.AppendLine(c.Description.Length > 0
                ? c.Description
                : "_(no description in the dispatch comment — add one above the guard in `Program.cs`)_");
            sb.AppendLine();
            var bits = new List<string>();
            if (c.Handler != null) bits.Add($"handler `{c.Handler}`");
            if (c.CostGated) bits.Add("**cost-gated (spends LLM money)**");
            if (bits.Count > 0) sb.AppendLine($"<sub>{string.Join(" · ", bits)}</sub>");
            sb.AppendLine();
        }

        var dir = Path.GetDirectoryName(Path.GetFullPath(outputPath));
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        File.WriteAllText(outputPath, sb.ToString());
        return commands.Count;
    }
}
