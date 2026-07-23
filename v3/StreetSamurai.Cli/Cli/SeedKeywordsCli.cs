using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;

namespace StreetSamurai.Cli;

/// <summary>
/// <c>ss --seed-keywords [--slug &lt;slug&gt;]</c>
/// — seed Amazon KDP keywords for exported nodes that have none.
/// Without --slug, targets every node where Version > 0.
/// Skips nodes that already have keyword rows.
/// </summary>
public static class SeedKeywordsCli
{
    static readonly string[] DefaultKeywords =
    [
        "cyberpunk heist thriller",
        "dystopian corporate crime fiction",
        "near future science fiction action",
        "body modification biopunk",
        "cyberpunk found family adventure",
        "AI noir science fiction",
        "urban dystopia thriller series",
    ];

    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? slug = null;
        for (int i = 0; i < args.Length; i++)
            if (args[i] == "--slug" && i + 1 < args.Length) slug = args[++i];

        var dbFactory = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        IQueryable<Node> q = db.Nodes.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(slug))
            q = q.Where(s => s.Slug == slug);
        else
            q = q.Where(s => s.Version > 0);

        var nodes = await q.Select(s => new { s.Id, s.Slug, s.Title }).ToListAsync();
        if (nodes.Count == 0)
        {
            Console.WriteLine("[seed-keywords] No matching exported nodes found.");
            return 0;
        }

        var existingIds = await db.NodeKeywords
            .Where(k => nodes.Select(s => s.Id).Contains(k.NodeId))
            .Select(k => k.NodeId)
            .Distinct()
            .ToListAsync();

        int seeded = 0;
        foreach (var node in nodes)
        {
            if (existingIds.Contains(node.Id))
            {
                Console.WriteLine($"[seed-keywords] {node.Slug} — already has keywords, skipping.");
                continue;
            }
            for (int i = 0; i < DefaultKeywords.Length; i++)
            {
                db.NodeKeywords.Add(new NodeKeyword
                {
                    Id        = Guid.NewGuid(),
                    NodeId  = node.Id,
                    Keyword   = DefaultKeywords[i],
                    SortOrder = i + 1,
                    CreatedAt = DateTime.UtcNow,
                });
            }
            Console.WriteLine($"[seed-keywords] {node.Slug} — seeded {DefaultKeywords.Length} keywords.");
            seeded++;
        }

        if (seeded > 0) await db.SaveChangesAsync();
        Console.WriteLine($"[seed-keywords] Done — {seeded} node(s) seeded.");
        return 0;
    }
}
