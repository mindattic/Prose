using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>prose --list-canon-sections --type &lt;DocumentType&gt; [--universe &lt;slug&gt;]</c> —
/// CLI equivalent of the MCP tool <c>list_canon_sections</c>. Read-only.
///
/// Built 2026-08-23: canon-doc editing (<c>set_canon_section</c>/<c>list_canon_sections</c>)
/// previously had no CLI equivalent at all — only reachable from an MCP client connected to
/// <c>Prose.Mcp</c>. A Claude Code CLI-only session had no way to read or fix canon content,
/// only to regenerate the existing (possibly stale) `.md` mirror. This closes that gap using the
/// exact same sanctioned <see cref="CanonDocumentService"/> methods the MCP tool calls — no raw
/// SQL, same Hub-routed write path.
/// </summary>
public static class ListCanonSectionsCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var type = args.SkipWhile(a => a != "--type").Skip(1).FirstOrDefault();
        var universeSlug = args.SkipWhile(a => a != "--universe").Skip(1).FirstOrDefault() ?? "glmz";
        if (string.IsNullOrWhiteSpace(type))
        {
            Console.Error.WriteLine("Usage: prose --list-canon-sections --type <DocumentType> [--universe <slug>]");
            return 1;
        }

        var canonDocs = services.GetRequiredService<CanonDocumentService>();

        var universeId = await canonDocs.ResolveUniverseIdAsync(universeSlug);
        if (universeId == null) { Console.Error.WriteLine($"[list-canon-sections] Unknown universe '{universeSlug}'."); return 1; }

        var doc = await canonDocs.FindDocumentAsync(type, universeId.Value);

        if (doc == null) { Console.Error.WriteLine($"[list-canon-sections] No {type} document for universe {universeSlug}."); return 1; }

        Console.WriteLine($"[list-canon-sections] {type} ({universeSlug}) — {doc.Sections.Count} section(s):");
        foreach (var s in doc.Sections)
            Console.WriteLine($"  {s.SectionKey,-24} \"{s.SectionTitle}\"  ({s.Content.Length} chars, updated {s.UpdatedAt:yyyy-MM-dd})");

        return 0;
    }
}

/// <summary>
/// <c>prose --get-canon-section --type &lt;DocumentType&gt; --key &lt;sectionKey&gt; [--out &lt;path.md&gt;]
/// [--universe &lt;slug&gt;]</c> — dump ONE canon section's raw stored content, byte-for-byte as the
/// DB holds it (no assembled heading, no frontmatter, no generated banner).
///
/// Exists so a section can be round-tripped losslessly: <c>--get-canon-section</c> to a file,
/// edit precisely, <c>--set-canon-section</c> back. Reconstructing a section body by slicing the
/// generated <c>.md</c> mirror between headings is NOT equivalent — the mirror has assembly
/// artifacts layered on, and a lossy round-trip through a 31KB section (e.g. WorldBible's
/// <c>SS-§5</c>, which holds every numbered Law) risks silently corrupting canon to fix a typo.
/// The MCP side has <c>get_canon_document</c>, but that returns the whole assembled document
/// rather than one section's raw body, so it can't serve this purpose either.
/// </summary>
public static class GetCanonSectionCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var type = args.SkipWhile(a => a != "--type").Skip(1).FirstOrDefault();
        var key = args.SkipWhile(a => a != "--key").Skip(1).FirstOrDefault();
        var outPath = args.SkipWhile(a => a != "--out").Skip(1).FirstOrDefault();
        var universeSlug = args.SkipWhile(a => a != "--universe").Skip(1).FirstOrDefault() ?? "glmz";

        if (string.IsNullOrWhiteSpace(type) || string.IsNullOrWhiteSpace(key))
        {
            Console.Error.WriteLine("Usage: prose --get-canon-section --type <DocumentType> --key <sectionKey> [--out <path.md>] [--universe <slug>]");
            return 1;
        }

        var canonDocs = services.GetRequiredService<CanonDocumentService>();
        var universeId = await canonDocs.ResolveUniverseIdAsync(universeSlug);
        if (universeId == null) { Console.Error.WriteLine($"[get-canon-section] Unknown universe '{universeSlug}'."); return 1; }

        var doc = await canonDocs.FindDocumentAsync(type, universeId.Value);
        if (doc == null) { Console.Error.WriteLine($"[get-canon-section] No {type} document for universe {universeSlug}."); return 1; }

        var section = doc.Sections.FirstOrDefault(s => s.SectionKey.Equals(key, StringComparison.OrdinalIgnoreCase));
        if (section == null) { Console.Error.WriteLine($"[get-canon-section] No section '{key}' in {type}. Use --list-canon-sections to see keys."); return 1; }

        if (string.IsNullOrWhiteSpace(outPath))
        {
            Console.WriteLine(section.Content);
        }
        else
        {
            // UTF-8 without BOM — matches how GeneratedFileWriter/the rest of the pipeline writes
            // markdown, so a get→edit→set round-trip can't introduce an encoding change.
            await File.WriteAllTextAsync(outPath, section.Content, new System.Text.UTF8Encoding(false));
            Console.WriteLine($"[get-canon-section] {type}/{section.SectionKey} → {outPath} ({section.Content.Length} chars)");
        }
        return 0;
    }
}

/// <summary>
/// <c>prose --set-canon-section --type &lt;DocumentType&gt; --key &lt;sectionKey&gt; --file &lt;path.md&gt;
/// [--title &lt;title&gt;] [--universe &lt;slug&gt;]</c> — CLI equivalent of the MCP tool
/// <c>set_canon_section</c>. Content is read from a file (not an inline arg) since canon section
/// bodies are typically multi-paragraph markdown. See <see cref="ListCanonSectionsCli"/>'s doc
/// comment for why this exists. Calls the exact same <see cref="CanonDocumentService"/> path the
/// MCP tool uses, then regenerates the `.md` mirror and syncs `MarkdownFiles` in the same
/// operation — the edit isn't "done" until both reflect it, same contract as the MCP tool.
/// </summary>
public static class SetCanonSectionCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var type = args.SkipWhile(a => a != "--type").Skip(1).FirstOrDefault();
        var key = args.SkipWhile(a => a != "--key").Skip(1).FirstOrDefault();
        var file = args.SkipWhile(a => a != "--file").Skip(1).FirstOrDefault();
        var title = args.SkipWhile(a => a != "--title").Skip(1).FirstOrDefault();
        var universeSlug = args.SkipWhile(a => a != "--universe").Skip(1).FirstOrDefault() ?? "glmz";

        if (string.IsNullOrWhiteSpace(type) || string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(file))
        {
            Console.Error.WriteLine("Usage: prose --set-canon-section --type <DocumentType> --key <sectionKey> --file <path.md> [--title <title>] [--universe <slug>]");
            return 1;
        }
        if (!File.Exists(file)) { Console.Error.WriteLine($"[set-canon-section] File not found: {file}"); return 1; }

        var content = await File.ReadAllTextAsync(file);
        var canonDocs = services.GetRequiredService<CanonDocumentService>();
        var markdownFiles = services.GetRequiredService<MarkdownFileService>();

        var universeId = await canonDocs.ResolveUniverseIdAsync(universeSlug);
        if (universeId == null) { Console.Error.WriteLine($"[set-canon-section] Unknown universe '{universeSlug}'."); return 1; }

        var result = await canonDocs.UpsertSectionAsync(type, universeId.Value, key, content, title);
        if (!result.Ok) { Console.Error.WriteLine($"[set-canon-section] {result.Error}: {result.ErrorMessage}"); return 1; }

        var genResult = await canonDocs.GenerateMdAsync(type, universeId.Value);
        var syncResult = await markdownFiles.SyncAllAsync();

        Console.WriteLine($"[set-canon-section] {result.Action} section '{result.SectionKey}' in {type} ({universeSlug}).");
        Console.WriteLine($"  Regenerated: {genResult.FilePath} ({genResult.SectionCount} sections, ok={genResult.Ok})");
        Console.WriteLine($"  Synced: inserted={syncResult.Inserted} updated={syncResult.Updated} unchanged={syncResult.Unchanged} errors={syncResult.Errors.Count}");
        return 0;
    }
}
