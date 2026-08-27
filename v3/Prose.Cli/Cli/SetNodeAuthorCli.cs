using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>prose --set-node-author --slug &lt;slug|code|guid&gt; --author "&lt;Name&gt;"</c>
///
/// Sets <see cref="Prose.Core.Data.Entities.Node.Author"/> — the pen name every export format
/// (docx/epub/pdf/txt) resolves to when no explicit <c>--author</c> override is passed to
/// <c>--export-node</c> (see ManuscriptExportService's "explicit param -> node.Author ->
/// 'MindAttic'" resolution order comment, repeated at each export method). No prior tool could
/// set this field — every book silently fell through to the "MindAttic" default. Needed for the
/// ANTHOLOGY universe, where each book is deliberately written and credited as a distinct
/// fictional Author persona (see docs/series/ANTHOLOGY.md), not as MindAttic.
/// Pass an empty --author to clear the override back to the "MindAttic" default.
/// </summary>
public static class SetNodeAuthorCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? slug = null, author = null;
        var authorGiven = false;
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--slug":   if (i + 1 < args.Length) slug = args[++i]; break;
                case "--author": authorGiven = true; if (i + 1 < args.Length) author = args[++i]; break;
            }
        }

        if (string.IsNullOrWhiteSpace(slug) || !authorGiven)
        {
            Console.Error.WriteLine("[set-node-author] --slug and --author are both required (pass --author \"\" to clear).");
            Console.Error.WriteLine("Usage: prose --set-node-author --slug <slug|code|guid> --author \"<Name>\"");
            return 2;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        var nodeId = await NodeRefResolver.ResolveAsync(db, slug);
        if (nodeId == null)
        {
            Console.Error.WriteLine(NodeRefResolver.NotFoundMessage(slug));
            return 1;
        }

        var node = await db.Nodes.IgnoreQueryFilters().FirstOrDefaultAsync(n => n.Id == nodeId.Value);
        if (node == null)
        {
            Console.Error.WriteLine(NodeRefResolver.NotFoundMessage(slug));
            return 1;
        }

        var prev = node.Author;
        node.Author = string.IsNullOrWhiteSpace(author) ? null : author!.Trim();
        await db.SaveChangesAsync();

        Console.WriteLine($"[set-node-author] '{node.Title}' ({node.Slug}): author '{prev ?? "(none — fell through to MindAttic)"}' -> '{node.Author ?? "(none — falls through to MindAttic)"}'");
        return 0;
    }
}
