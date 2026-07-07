using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;

namespace StreetSamurai.Cli;

/// <summary>
/// <c>ss --clone-story (--id &lt;guid&gt; | --slug &lt;slug&gt;) [--title "New Title"] [--story-code "SM1"] [--draft] [--status &lt;status&gt;]</c>
/// — deep-clone a node: creates a new Node row plus independent copies of every
/// enabled beat (new IDs, new Numbers). Audio, scores, and review history are NOT
/// cloned — the clone starts fresh so review scores are independent.
/// </summary>
public static class CloneNodeCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? id = null, slug = null, title = null, nodeCode = null, status = "ready";
        bool isDraft = false;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--id":           if (i + 1 < args.Length) id          = args[++i]; break;
                case "--slug":         if (i + 1 < args.Length) slug        = args[++i]; break;
                case "--title":        if (i + 1 < args.Length) title       = args[++i]; break;
                case "--story-code":  if (i + 1 < args.Length) nodeCode  = args[++i]; break;
                case "--status":       if (i + 1 < args.Length) status      = args[++i]; break;
                case "--draft":        isDraft = true; break;
            }
        }

        if (string.IsNullOrWhiteSpace(id) && string.IsNullOrWhiteSpace(slug))
        {
            Console.Error.WriteLine("[clone-story] One of --id or --slug is required.");
            return 1;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        // ── Resolve source node ─────────────────────────────────────────────
        Node? source;
        if (!string.IsNullOrWhiteSpace(slug))
            source = await db.Nodes.AsNoTracking().FirstOrDefaultAsync(s => s.Slug == slug);
        else if (Guid.TryParse(id, out var g))
            source = await db.Nodes.AsNoTracking().FirstOrDefaultAsync(s => s.Id == g);
        else
            source = await db.Nodes.AsNoTracking()
                .Where(s => s.Id.ToString().StartsWith(id!.ToLower()))
                .Take(2).ToListAsync() switch
                { { Count: 1 } m => m[0], _ => null };

        if (source == null)
        {
            Console.Error.WriteLine("[clone-story] Source node not found.");
            return 1;
        }

        // ── Validate node-code uniqueness ───────────────────────────────────
        var code = string.IsNullOrWhiteSpace(nodeCode) ? null : nodeCode.Trim().ToUpperInvariant();
        if (code != null)
        {
            var clash = await db.Nodes.AsNoTracking()
                .FirstOrDefaultAsync(s => s.NodeCode == code);
            if (clash != null)
            {
                Console.Error.WriteLine(
                    $"[clone-story] NodeCode '{code}' is already in use by '{clash.Title}' ({clash.Slug}).");
                return 1;
            }
        }

        // ── Load enabled beats in SortKey order ───────────────────────────────
        var sourceBeats = await db.BeatNodes
            .AsNoTracking()
            .Where(sb => sb.NodeId == source.Id && sb.IsEnabled)
            .OrderBy(sb => sb.SortKey)
            .Join(db.Beats.AsNoTracking(), sb => sb.BeatId, b => b.Id,
                  (sb, b) => new { sb.SortKey, Beat = b })
            .ToListAsync();

        Console.WriteLine($"[clone-story] Source: '{source.Title}' ({source.Slug}) — {sourceBeats.Count} beat(s)");

        // ── Determine new title and slug ──────────────────────────────────────
        var newTitle = string.IsNullOrWhiteSpace(title)
            ? $"{source.Title} (Clone)"
            : title.Trim();
        var newId   = Guid.CreateVersion7();
        var newSlug = $"{Slugify(newTitle)}-{newId.ToString("N")[..8]}";

        // ── Sort key: append after all existing root nodes ─────────────────
        var maxSort = await db.Nodes
            .Where(s => s.ParentNodeId == null)
            .Select(s => (double?)s.SortKey)
            .MaxAsync() ?? 0;

        var now = DateTime.UtcNow;

        // ── Insert new Node (same concrete type as the source) ──────────────
        var newNode = NodeFactory.CreateLike(source);
        newNode.Id              = newId;
        newNode.Slug            = newSlug;
        newNode.Title           = newTitle;
        newNode.NodeCode        = code;
        newNode.Kind            = source.Kind;
        newNode.Status          = status;
        newNode.Description     = source.Description;
        newNode.Seed            = source.Seed;
        newNode.UniverseId      = source.UniverseId;
        newNode.VoiceId         = source.VoiceId;
        newNode.VoiceModel      = source.VoiceModel;
        newNode.VoiceStability  = source.VoiceStability;
        newNode.VoiceSimilarity = source.VoiceSimilarity;
        newNode.VoiceStyle      = source.VoiceStyle;
        newNode.VoiceSeed       = source.VoiceSeed;
        newNode.TtsEngine       = source.TtsEngine;
        newNode.SortKey         = maxSort + 100.0;
        newNode.CreatedAt       = now;
        newNode.UpdatedAt       = now;
        db.Nodes.Add(newNode);

        // ── Clone beats ───────────────────────────────────────────────────────
        var beatMax = await db.Beats.MaxAsync(b => (int?)b.Number) ?? 0;
        int nextNum = beatMax + 1;

        foreach (var entry in sourceBeats)
        {
            var src  = entry.Beat;
            var beatId = Guid.CreateVersion7();
            var cloned = new Beat
            {
                Id              = beatId,
                Number          = nextNum++,
                Text            = src.Text,
                Title           = src.Title,
                Description     = src.Description,
                StructureRole   = src.StructureRole,
                Act             = src.Act,
                SceneType       = src.SceneType,
                EmotionalTone   = src.EmotionalTone,
                PaceHint        = src.PaceHint,
                Kind            = src.Kind,
                IsChapterStart  = src.IsChapterStart,
                GapAfterMs      = src.GapAfterMs,
                GapAfterAudioPath = src.GapAfterAudioPath,
                Stale           = false,
                EntityStale     = false,
                WasCorrected    = false,
                Version         = 0,
                CreatedAt       = now,
                UpdatedAt       = now,
            };
            db.Beats.Add(cloned);

            db.BeatNodes.Add(new BeatNode
            {
                NodeId  = newId,
                BeatId    = beatId,
                SortKey   = entry.SortKey,
                IsEnabled = true,
            });
        }

        await db.SaveChangesAsync();

        Console.WriteLine($"[clone-story] Created '{newTitle}' — {sourceBeats.Count} beat(s) cloned");
        Console.WriteLine($"[clone-story] id:   {newId}");
        Console.WriteLine($"[clone-story] slug: {newSlug}");
        return 0;
    }

    private static string Slugify(string s)
    {
        var clean = new string(s.ToLowerInvariant()
            .Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray());
        var parts = clean.Split('-').Where(p => p.Length > 0).Take(8);
        return string.Join("-", parts);
    }
}
