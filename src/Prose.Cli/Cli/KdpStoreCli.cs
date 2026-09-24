using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Kdp;

namespace Prose.Cli;

// The KDP store's front door on the CLI (shared with KdpPublish, which calls the same
// KdpStore/KdpJsonTransfer in-process). Every handler first brings the store up —
// KdpStore.EnsureReadyAsync migrates the SQLite file and runs the one-time import of the legacy
// JSON files if it has never happened.

/// <summary>
/// <c>prose --kdp-import [--from &lt;dir&gt;] [--legacy-markers]</c> — loads the KDP store from JSON:
/// title-ids.json, category-tree-*.json, logs/*.log and (from an export) publish-markers/ in
/// <c>--from</c> (default: the repo's tools/kdp). <c>--legacy-markers</c> also reads every book
/// folder's <c>.publish</c> marker in place, as the first-run import did — note that a marker file
/// signs its book off, so this re-signs-off any book held back since. Merges; deletes nothing.
/// </summary>
public static class KdpImportCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? from = null;
        var legacyMarkers = false;
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--from": if (i + 1 < args.Length) from = args[++i]; break;
                case "--legacy-markers": legacyMarkers = true; break;
            }
        }
        from = Path.GetFullPath(from ?? KdpPaths.ResolveToolsDir());
        if (!Directory.Exists(from))
        {
            Console.Error.WriteLine($"[kdp-import] Folder not found: {from}");
            return 1;
        }

        await services.GetRequiredService<KdpStore>().EnsureReadyAsync();
        var folders = legacyMarkers ? await services.GetRequiredService<IKdpBookFolderSource>().ListAsync() : null;
        var counts = await services.GetRequiredService<KdpJsonTransfer>().ImportAsync(new KdpImportRequest
        {
            FromDir = from,
            LegacyMarkerFolders = folders,
            Kind = KdpImportKind.Manual,
        });

        Console.WriteLine($"[kdp-import] Imported from {from}{(folders == null ? "" : $" + {folders.Count} book folder(s)")}: {counts}.");
        Console.WriteLine($"[kdp-import] Store: {KdpPaths.ResolveDbPath()}");
        return 0;
    }
}

/// <summary>
/// <c>prose --kdp-export [--to &lt;dir&gt;] [--markers-in-place]</c> — writes the KDP store back out
/// as JSON in the original shapes: title-ids.json, category-tree-&lt;slug&gt;.json, logs/*.log, and
/// publish-markers/&lt;CODE&gt;.publish (held books under publish-markers/held/). Default
/// <c>--to</c>: a new timestamped folder under %LocalAppData%\MindAttic\Prose\kdp-export, so
/// nothing in the repo changes unless asked (<c>--to tools/kdp</c> refreshes the committed
/// title-ids.json and category trees). <c>--markers-in-place</c> also writes each signed-off
/// book's <c>.publish</c> back into its export folder.
/// </summary>
public static class KdpExportCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? to = null;
        var inPlace = false;
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--to": if (i + 1 < args.Length) to = args[++i]; break;
                case "--markers-in-place": inPlace = true; break;
            }
        }
        to = Path.GetFullPath(to ?? DefaultExportDir());

        await services.GetRequiredService<KdpStore>().EnsureReadyAsync();
        var folders = inPlace ? await services.GetRequiredService<IKdpBookFolderSource>().ListAsync() : null;
        var counts = await services.GetRequiredService<KdpJsonTransfer>().ExportAsync(new KdpExportRequest
        {
            ToDir = to,
            MarkersInPlace = folders,
        });

        Console.WriteLine($"[kdp-export] Exported to {to}: {counts}.");
        if (folders != null) Console.WriteLine($"[kdp-export] Also wrote .publish markers back into the signed-off books' export folders.");
        return 0;
    }

    public static string DefaultExportDir() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MindAttic", "Prose", "kdp-export", DateTime.Now.ToString("yyyyMMdd-HHmmss"));
}

/// <summary>
/// <c>prose --kdp-signoff --code &lt;CODE&gt;[,&lt;CODE&gt;...] [--off]</c> — the human publish gate
/// (was: create or delete the book's <c>.publish</c> marker file). Signed off, a book is eligible
/// for a KdpPublish run once its cover, description and a newer .epub are on disk; <c>--off</c>
/// holds it back. Codes are checked against the Prose database (NodeCode or slug).
/// </summary>
public static class KdpSignOffCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var codes = new List<string>();
        var off = false;
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--code":
                    if (i + 1 < args.Length)
                        codes.AddRange(args[++i].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                    break;
                case "--off": off = true; break;
            }
        }
        if (codes.Count == 0)
        {
            Console.Error.WriteLine("[kdp-signoff] --code <CODE>[,<CODE>...] is required.");
            return 1;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var known = await db.Nodes.AsNoTracking().IgnoreQueryFilters()
                .Where(n => (n.NodeCode != null && codes.Contains(n.NodeCode)) || codes.Contains(n.Slug))
                .Select(n => n.NodeCode ?? n.Slug)
                .ToListAsync();
            var unknown = codes.Where(c => !known.Contains(c, StringComparer.Ordinal)).ToList();
            if (unknown.Count > 0)
            {
                Console.Error.WriteLine($"[kdp-signoff] No book with NodeCode (or slug) {string.Join(", ", unknown)} — nothing changed. Codes are case-sensitive.");
                return 1;
            }
        }

        var store = services.GetRequiredService<KdpStore>();
        await store.EnsureReadyAsync();
        var changed = await store.SetSignOffAsync(codes, ready: !off, changedBy: "cli");
        Console.WriteLine($"[kdp-signoff] {(off ? "Held back" : "Signed off")}: {string.Join(", ", codes)} ({changed} changed).");
        return 0;
    }
}

/// <summary>
/// <c>prose --kdp-store [--code &lt;CODE&gt;]</c> — where the KDP store is, what it holds and when it
/// was imported; with <c>--code</c>, one book's sign-off, title id, last confirmed publish and full
/// publish history (what reading its <c>.publish</c> marker used to tell you).
/// </summary>
public static class KdpStoreCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? code = null;
        for (int i = 0; i < args.Length; i++)
            if (args[i] == "--code" && i + 1 < args.Length) code = args[++i];

        var store = services.GetRequiredService<KdpStore>();
        await store.EnsureReadyAsync();

        if (code == null)
        {
            var status = await store.GetStatusAsync();
            Console.WriteLine($"[kdp-store] {KdpPaths.ResolveDbPath()}");
            Console.WriteLine($"[kdp-store] Holds {status.Counts}.");
            if (status.Imports.Count == 0)
                Console.WriteLine("[kdp-store] No JSON import recorded yet (the first-run import is deferred until tools/kdp/title-ids.json is reachable).");
            foreach (var imp in status.Imports)
                Console.WriteLine($"[kdp-store] {imp.Kind} import {imp.At.UtcDateTime:yyyy-MM-dd HH:mm}Z from {imp.Source}: {imp.Counts}.");
            return 0;
        }

        var book = await store.GetBookAsync(code, withHistory: true);
        var title = await store.GetTitleAsync(code);
        Console.WriteLine($"[kdp-store] {code}");
        Console.WriteLine($"  titleId:      {title?.TitleId ?? "—"}{(title?.Asin != null ? $"  (asin {title.Asin})" : "")}");
        if (book == null)
        {
            Console.WriteLine("  sign-off:     not signed off (the store has no record of this book)");
            return 0;
        }
        Console.WriteLine($"  sign-off:     {(book.SignOff.Ready ? "signed off" : "held back")}{(book.SignOff.ChangedAt is { } at ? $" ({book.SignOff.ChangedBy}, {at.UtcDateTime:yyyy-MM-dd HH:mm}Z)" : "")}");
        Console.WriteLine($"  last publish: {Describe(book.LastPublish)}");
        if (book.PublishingDetectedAt is { } detected)
            Console.WriteLine($"  KDP 'Updates publishing' seen {detected.UtcDateTime:yyyy-MM-dd HH:mm}Z");
        Console.WriteLine($"  history ({book.History.Count}):");
        foreach (var h in book.History)
            Console.WriteLine($"    {h.RecordedAt.UtcDateTime:yyyy-MM-dd HH:mm}Z  {h.Source,-13}  {Describe(h.Publish)}");
        return 0;
    }

    private static string Describe(KdpPublishSnapshot p) =>
        p.IsEmpty ? "—" :
        $"{p.File ?? "(no file)"}{(p.Version is int v ? $" v{v}" : "")}{(p.Asin != null ? $"  asin {p.Asin}" : "")}" +
        $"{(p.PublishedAt is { } at ? $"  confirmed {at.UtcDateTime:yyyy-MM-dd HH:mm}Z" : "  (never confirmed)")}";
}
