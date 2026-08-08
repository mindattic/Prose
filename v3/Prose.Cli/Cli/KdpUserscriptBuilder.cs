using System.Text.Json;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// Bakes a <see cref="KdpManifestEntry"/> list into <c>tools/kdp/kdp-panel.template.js</c>,
/// replacing the <c>/*__BOOKS_JSON__*/</c> placeholder with a literal JS array, and writes the
/// result to <c>tools/kdp/kdp-panel.user.js</c> — the file actually installed in Tampermonkey.
/// The template holds all UI/behavior; only the data changes between regenerations, so hand
/// edits to the sidebar's look or step logic always go in the template, never the generated file.
/// </summary>
public static class KdpUserscriptBuilder
{
    private const string Placeholder = "/*__BOOKS_JSON__*/";

    public static bool Build(string kdpDir, List<KdpManifestEntry> entries, JsonSerializerOptions jsonOpts)
    {
        var templatePath = Path.Combine(kdpDir, "kdp-panel.template.js");
        if (!File.Exists(templatePath)) return false;

        var template = File.ReadAllText(templatePath);
        var booksJson = JsonSerializer.Serialize(entries, jsonOpts);
        var output = template.Replace(Placeholder, booksJson);

        var outPath = Path.Combine(kdpDir, "kdp-panel.user.js");
        var header = $"// Generated {DateTime.Now:yyyy-MM-dd HH:mm} by `prose --kdp-manifest --userscript`. Do not hand-edit — edit kdp-panel.template.js instead.\n";
        File.WriteAllText(outPath, header + output);
        return true;
    }
}
