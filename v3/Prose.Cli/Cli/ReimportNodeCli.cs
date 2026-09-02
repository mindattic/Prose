using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Cli;

/// <summary>
/// <c>prose --reimport-node (--id &lt;guid&gt; | --slug &lt;slug&gt;) --file path.node [--dry-run] [--force]</c>
///
/// Replaces an EXISTING node's live beats wholesale from a hand-authored
/// <c>.node</c> file (same format as <see cref="ImportNodeCli"/> — see that
/// class for the full grammar). This is the missing half of the export/edit/
/// reimport loop: <c>--publish-md</c> flattens a node's beats into one
/// document for external editing; <c>--import-md</c> patches individual
/// beats back in by their existing ID when the edit stayed inside the
/// existing beat boundaries. This command is for the case where the edit
/// (or a from-scratch rewrite done outside the app entirely) no longer lines
/// up with the old beat boundaries at all — the whole node's content needs
/// to be swapped for a fresh, re-segmented version.
///
/// What it does NOT do: touch the node's own metadata (Title, Slug,
/// NodeCode, cover art, KDP fields, voice settings, etc). Only the beat
/// content changes — the book keeps its identity.
///
/// Safety: Beats/Nodes/BeatNodes are no longer system-versioned (that
/// mechanism is what let disabled-but-undeleted beats accumulate across
/// every past revision with nothing forcing reconciliation — see
/// ProseDbContext.SystemVersionedTables). Instead, the OLD content is
/// captured once, in full, as an ArchivedBook markdown snapshot
/// (reason "pre-reimport") immediately before the old BeatNode links —
/// and any Beat rows left with no remaining links at all — are actually
/// deleted. There is exactly one live version of every beat afterward.
///
/// A rough word-count comparison between the old (currently enabled) content
/// and the new parsed content is printed before writing. If the new content
/// is under 50% of the old word count, the command refuses to write unless
/// --force is passed — this catches "I pointed it at the wrong file" before
/// it costs you a whole book, without blocking legitimate edits that trim or
/// expand the manuscript.
///
/// Args:
///   --id / --slug   Required (one of). Identifies the node to replace.
///   --file PATH     Required. Path to the replacement .node file (or "-" for stdin).
///   --dry-run       Parse and report only — don't write anything.
///   --force         Skip the word-retention safety check.
/// </summary>
public static class ReimportNodeCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? id = null, slug = null, file = null;
        bool dryRun = false, force = false;
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--id":      if (i + 1 < args.Length) id = args[++i]; break;
                case "--slug":    if (i + 1 < args.Length) slug = args[++i]; break;
                case "--file":    if (i + 1 < args.Length) file = args[++i]; break;
                case "--dry-run": dryRun = true; break;
                case "--force":   force = true; break;
            }
        }

        if (string.IsNullOrWhiteSpace(id) && string.IsNullOrWhiteSpace(slug))
        {
            Console.Error.WriteLine("[reimport-node] One of --id or --slug is required.");
            return 2;
        }
        if (string.IsNullOrWhiteSpace(file))
        {
            Console.Error.WriteLine("[reimport-node] --file is required (or '-' for stdin).");
            Console.Error.WriteLine("Usage: prose --reimport-node (--id ... | --slug ...) --file path.node [--dry-run] [--force]");
            return 2;
        }

        string raw;
        if (file == "-") raw = await Console.In.ReadToEndAsync();
        else
        {
            if (!File.Exists(file))
            {
                Console.Error.WriteLine($"[reimport-node] File not found: {file}");
                return 1;
            }
            raw = await File.ReadAllTextAsync(file);
        }

        ParsedNodeFile parsed;
        try { parsed = NodeFileParser.Parse(raw); }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[reimport-node] Parse failed: {ex.Message}");
            return 1;
        }

        if (parsed.Beats.Count == 0)
        {
            Console.Error.WriteLine("[reimport-node] No beats found in the file.");
            return 1;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        var nodeQuery = db.Nodes.AsQueryable();
        var node = !string.IsNullOrWhiteSpace(slug)
            ? await nodeQuery.FirstOrDefaultAsync(n => n.Slug == slug)
            : Guid.TryParse(id, out var g)
                ? await nodeQuery.FirstOrDefaultAsync(n => n.Id == g)
                : null;

        if (node == null)
        {
            Console.Error.WriteLine("[reimport-node] Target node not found.");
            return 1;
        }

        // Every link for this node, enabled or not — a reimport is meant to leave
        // the node with exactly one clean set of beats, so any already-disabled
        // stragglers get swept up and archived too, not left behind again.
        var oldLinks = await db.BeatNodes
            .Where(bn => bn.NodeId == node.Id)
            .Join(db.Beats, bn => bn.BeatId, b => b.Id, (bn, b) => new { bn, b })
            .ToListAsync();

        var oldWordCount = oldLinks.Sum(x => x.b.Text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length);
        var newWordCount = NodeBeatWriter.CountWords(parsed.Beats);
        var retention = oldWordCount == 0 ? 1.0 : (double)newWordCount / oldWordCount;

        Console.WriteLine($"[reimport-node] target=\"{node.Title}\" ({node.Slug})");
        Console.WriteLine($"[reimport-node] old: {oldLinks.Count} enabled beats, {oldWordCount:N0} words");
        Console.WriteLine($"[reimport-node] new: {parsed.Beats.Count} beats, {newWordCount:N0} words (from {file})");
        Console.WriteLine($"[reimport-node] retention: {retention:P0} of old word count");

        if (retention < 0.5 && !force)
        {
            Console.Error.WriteLine();
            Console.Error.WriteLine("[reimport-node] New content is under 50% of the old word count.");
            Console.Error.WriteLine("[reimport-node] Refusing to write. Re-check --file, or pass --force if this is intentional.");
            return 1;
        }

        if (dryRun)
        {
            Console.WriteLine("[reimport-node] dry-run — nothing written. Old beats would be archived then deleted; new beats attached.");
            return 0;
        }

        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

        // Snapshot the outgoing content before it's gone for good.
        var snapshotMd = new System.Text.StringBuilder();
        snapshotMd.AppendLine($"# {node.Title}");
        snapshotMd.AppendLine();
        foreach (var x in oldLinks.OrderBy(x => x.bn.SortKey))
        {
            if (!string.IsNullOrWhiteSpace(x.b.Text))
            {
                snapshotMd.AppendLine(x.b.Text.Trim());
                snapshotMd.AppendLine();
            }
        }
        db.ArchivedBooks.Add(new ArchivedBook
        {
            Id = Guid.NewGuid(),
            NodeId = node.Id,
            Title = node.Title,
            Version = node.Version,
            Reason = "pre-reimport",
            Markdown = snapshotMd.ToString().TrimEnd() + "\n",
            BeatCount = oldLinks.Count,
            WordCount = oldWordCount,
            Description = node.Description,
            NodeOutline = node.NodeOutline,
            Summary = node.Summary,
            Seed = node.Seed,
            Subtitle = node.Subtitle,
            CreatedAt = DateTime.UtcNow,
        });

        // Delete the old BeatNode links, then any Beat rows that no longer
        // have ANY remaining link (a Beat can in principle be shared across
        // more than one node — see BeatNode's doc comment — so only drop a
        // Beat outright once nothing references it anymore).
        var oldBeatIds = oldLinks.Select(x => x.b.Id).ToList();
        db.BeatNodes.RemoveRange(oldLinks.Select(x => x.bn));
        await db.SaveChangesAsync();

        var stillReferenced = await db.BeatNodes
            .Where(bn => oldBeatIds.Contains(bn.BeatId))
            .Select(bn => bn.BeatId)
            .Distinct()
            .ToListAsync();
        var orphanedBeatIds = oldBeatIds.Except(stillReferenced).ToList();
        if (orphanedBeatIds.Count > 0)
        {
            var orphanedBeats = await db.Beats.Where(b => orphanedBeatIds.Contains(b.Id)).ToListAsync();
            await Prose.Core.Services.NodeWorkbenchService.ClearEdgeBeatBoundsAsync(db, orphanedBeatIds, default);
            db.Beats.RemoveRange(orphanedBeats);
        }

        var written = await NodeBeatWriter.WriteBeatsAsync(db, node.Id, parsed.Beats, startSortKey: 100.0);

        await db.SaveChangesAsync();
        await tx.CommitAsync();

        Console.WriteLine();
        Console.WriteLine($"[reimport-node] OK — archived and deleted {oldLinks.Count} old beats, wrote {written} new beats.");
        Console.WriteLine($"[reimport-node] URL: https://localhost:7103/node/{node.Slug}");
        return 0;
    }
}
