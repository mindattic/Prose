using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// prose --get-book-outline (--slug &lt;slug|code|guid&gt;) [--out &lt;path&gt;]
///
/// Dump a node bible VERBATIM — the exact <c>Nodes.NodeOutline</c> text, byte for byte, with no
/// generation, formatting, or truncation. Written to <c>--out</c> when given, otherwise stdout.
///
/// Added 2026-08-23 to close a real tooling gap. The only ways to read a bible were
/// <c>--generate-book-outline</c> (renamed from --book-outline 2026-08-30 for exactly this
/// reason — it GENERATES a fresh one via an LLM, destructive, not a dump) and the
/// MCP <c>get_book_outline</c> tool (which returns 100K+ chars into the caller's context). That left
/// no safe way to do the read-edit-write round trip <c>--set-book-outline --file</c> exists for, so a
/// targeted bible correction on VIGL (a 118K-char bible needing a two-clause fix) had no sanctioned
/// path at all and was deferred twice as "risking a hand round-trip of the full document".
/// This is the read half of that round trip: dump here, edit the file, push with
/// <c>--set-book-outline --file</c>.
///
/// Uses File.WriteAllTextAsync (UTF-8, no BOM) rather than shell redirection — this corpus is full
/// of em-dashes and PowerShell's Get-Content/Set-Content mangle them.
/// </summary>
public static class GetBookOutlineCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? slug = null, outPath = null;
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--slug":
                case "--id": if (i + 1 < args.Length) slug = args[++i]; break;
                case "--out": if (i + 1 < args.Length) outPath = args[++i]; break;
            }
        }

        if (string.IsNullOrWhiteSpace(slug))
        {
            Console.Error.WriteLine("[get-book-bible] --slug (or --id) is required.");
            Console.Error.WriteLine("Usage: prose --get-book-outline --slug <slug|code|guid> [--out <path>]");
            return 2;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        // NodeRefResolver accepts slug, NodeCode, GUID, or unique GUID prefix — same resolution
        // SetBookOutlineCli uses, so a dump and its matching push always target the same row.
        var nodeId = await NodeRefResolver.ResolveAsync(db, slug);
        // IgnoreQueryFilters(): explicit ref, not an ambient universe scope.
        var node = nodeId == null ? null
            : await db.Nodes.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(n => n.Id == nodeId.Value);
        if (node == null)
        {
            Console.Error.WriteLine($"[get-book-bible] {NodeRefResolver.NotFoundMessage(slug)}");
            return 1;
        }

        var bible = node.NodeOutline ?? "";
        if (bible.Length == 0)
            Console.Error.WriteLine($"[get-book-bible] WARNING: '{node.Title}' ({node.Slug}) has an empty NodeOutline.");

        if (string.IsNullOrWhiteSpace(outPath))
        {
            Console.Out.Write(bible);
            return 0;
        }

        await File.WriteAllTextAsync(outPath, bible, new System.Text.UTF8Encoding(false));
        Console.WriteLine($"[get-book-bible] Wrote {bible.Length} chars for '{node.Title}' ({node.Slug}) to {outPath}");
        return 0;
    }
}
