using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Cli;

/// <summary>
/// <c>prose --seed-keywords --slug &lt;slug&gt; --keywords "phrase one|phrase two|..."</c>
/// — set this node's Amazon KDP backend search keywords (up to 7, pipe-separated,
/// each becomes its own row). Replaces any existing keyword rows for the node
/// (never blends stale generic filler with real ones).
/// <para>There is deliberately NO generic/default keyword list here — every book
/// needs its own researched, book-specific SEO phrases (title/subtitle words
/// wasted as keywords, since Amazon already indexes those separately). A prior
/// version of this command silently applied one identical hardcoded list to
/// every book with no keywords; that defeated the entire point of backend
/// keywords and has been removed.</para>
/// </summary>
public static class SeedKeywordsCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? slug = null, keywordsArg = null;
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--slug" && i + 1 < args.Length) slug = args[++i];
            else if (args[i] == "--keywords" && i + 1 < args.Length) keywordsArg = args[++i];
        }

        if (string.IsNullOrWhiteSpace(slug))
        {
            Console.Error.WriteLine("[seed-keywords] --slug <slug> is required.");
            return 2;
        }
        if (string.IsNullOrWhiteSpace(keywordsArg))
        {
            Console.Error.WriteLine("[seed-keywords] --keywords \"phrase one|phrase two|...\" is required — no generic default is provided. Research 7 unique, book-specific SEO phrases first.");
            return 2;
        }

        var phrases = keywordsArg.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (phrases.Length == 0 || phrases.Length > 7)
        {
            Console.Error.WriteLine($"[seed-keywords] Expected 1-7 pipe-separated phrases, got {phrases.Length}.");
            return 2;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        var node = await db.Nodes.AsNoTracking()
            .Where(s => s.Slug == slug)
            .Select(s => new { s.Id, s.Slug })
            .FirstOrDefaultAsync();
        if (node == null)
        {
            Console.Error.WriteLine($"[seed-keywords] Node not found: {slug}");
            return 1;
        }

        var existing = await db.NodeKeywords.Where(k => k.NodeId == node.Id).ToListAsync();
        if (existing.Count > 0)
        {
            db.NodeKeywords.RemoveRange(existing);
            Console.WriteLine($"[seed-keywords] {node.Slug} — replacing {existing.Count} existing keyword(s).");
        }

        for (int i = 0; i < phrases.Length; i++)
        {
            db.NodeKeywords.Add(new NodeKeyword
            {
                Id        = Guid.NewGuid(),
                NodeId  = node.Id,
                Keyword   = phrases[i],
                SortOrder = i + 1,
                CreatedAt = DateTime.UtcNow,
            });
        }

        await db.SaveChangesAsync();
        Console.WriteLine($"[seed-keywords] {node.Slug} — set {phrases.Length} keyword(s).");
        return 0;
    }
}
