using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;

namespace Prose.Cli;

/// <summary>
/// <c>prose --set-store-url --slug &lt;slug|code&gt; --url &lt;https://www.amazon.com/dp/ASIN&gt;</c> — corrects where a published
/// book is on sale: <see cref="Prose.Core.Data.Entities.Node.PublishUrl"/> and its <c>Asin</c> (read from the
/// <c>/dp/</c> part), nothing else. Unlike <c>--kdp-mark-published</c> it records no publication: the publish date,
/// status and edition history stay as they are. A URL that is not an Amazon store page is refused (PRS-T1060: Neon &amp;
/// Rust carried its KDP console URL, which the wiki's reconciliation caught).
/// Exit codes: 0 set (or already so) · 1 bad args, no such book, or not a store URL.
/// </summary>
public static class StoreUrlCli
{
    static readonly Regex StorePage = new(@"^https://(www\.)?amazon\.[a-z.]+/(?:.*/)?dp/(?<asin>[A-Z0-9]{10})", RegexOptions.IgnoreCase);

    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? Flag(string name) { var i = Array.IndexOf(args, name); return i >= 0 && i + 1 < args.Length ? args[i + 1] : null; }
        var slug = Flag("--slug");
        var url = Flag("--url")?.Split("/ref=")[0].Trim();
        if (string.IsNullOrWhiteSpace(slug) || string.IsNullOrWhiteSpace(url))
        {
            Console.Error.WriteLine("[set-store-url] usage: --set-store-url --slug <slug|code> --url <https://www.amazon.com/dp/ASIN>");
            return 1;
        }
        var m = StorePage.Match(url);
        if (!m.Success)
        {
            Console.Error.WriteLine($"[set-store-url] '{url}' is not an Amazon store page (https://www.amazon.com/dp/<ASIN>).");
            return 1;
        }
        var asin = m.Groups["asin"].Value.ToUpperInvariant();

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();
        var key = slug.ToLower();
        var node = await db.Nodes.FirstOrDefaultAsync(n => n.Kind == "book" && (n.Slug.ToLower() == key || (n.NodeCode != null && n.NodeCode.ToLower() == key)));
        if (node == null) { Console.Error.WriteLine($"[set-store-url] no book '{slug}'."); return 1; }
        if (node.PublishUrl == url && node.Asin == asin)
        {
            Console.WriteLine($"[set-store-url] {node.Title}: already {url}");
            return 0;
        }
        var was = node.PublishUrl;
        node.PublishUrl = url;
        node.Asin = asin;
        await db.SaveChangesAsync();
        Console.WriteLine($"[set-store-url] {node.Title}: {was ?? "(none)"} -> {url} (ASIN {asin})");
        return 0;
    }
}
