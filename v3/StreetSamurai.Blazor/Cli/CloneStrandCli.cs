using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;

namespace StreetSamurai.Blazor.Cli;

/// <summary>
/// <c>ss --clone-strand (--id &lt;guid&gt; | --slug &lt;slug&gt;) [--title "New Title"] [--strand-code "SM1"] [--draft] [--status &lt;status&gt;]</c>
/// — deep-clone a strand: creates a new Strand row plus independent copies of every
/// enabled beat (new IDs, new Numbers). Audio, scores, and review history are NOT
/// cloned — the clone starts fresh so review scores are independent.
/// IsWIP is set by default so the clone is excluded from global review/score/publish
/// flows until the author promotes it.
/// </summary>
public static class CloneStrandCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? id = null, slug = null, title = null, strandCode = null, status = "ready";
        bool isDraft = false;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--id":           if (i + 1 < args.Length) id          = args[++i]; break;
                case "--slug":         if (i + 1 < args.Length) slug        = args[++i]; break;
                case "--title":        if (i + 1 < args.Length) title       = args[++i]; break;
                case "--strand-code":  if (i + 1 < args.Length) strandCode  = args[++i]; break;
                case "--status":       if (i + 1 < args.Length) status      = args[++i]; break;
                case "--draft":        isDraft = true; break;
            }
        }

        if (string.IsNullOrWhiteSpace(id) && string.IsNullOrWhiteSpace(slug))
        {
            Console.Error.WriteLine("[clone-strand] One of --id or --slug is required.");
            return 1;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        // ── Resolve source strand ─────────────────────────────────────────────
        Strand? source;
        if (!string.IsNullOrWhiteSpace(slug))
            source = await db.Strands.AsNoTracking().FirstOrDefaultAsync(s => s.Slug == slug);
        else if (Guid.TryParse(id, out var g))
            source = await db.Strands.AsNoTracking().FirstOrDefaultAsync(s => s.Id == g);
        else
            source = await db.Strands.AsNoTracking()
                .Where(s => s.Id.ToString().StartsWith(id!.ToLower()))
                .Take(2).ToListAsync() switch
                { { Count: 1 } m => m[0], _ => null };

        if (source == null)
        {
            Console.Error.WriteLine("[clone-strand] Source strand not found.");
            return 1;
        }

        // ── Validate strand-code uniqueness ───────────────────────────────────
        var code = string.IsNullOrWhiteSpace(strandCode) ? null : strandCode.Trim().ToUpperInvariant();
        if (code != null)
        {
            var clash = await db.Strands.AsNoTracking()
                .FirstOrDefaultAsync(s => s.StrandCode == code);
            if (clash != null)
            {
                Console.Error.WriteLine(
                    $"[clone-strand] StrandCode '{code}' is already in use by '{clash.Title}' ({clash.Slug}).");
                return 1;
            }
        }

        // ── Load enabled beats in SortKey order ───────────────────────────────
        var sourceBeats = await db.StrandBeats
            .AsNoTracking()
            .Where(sb => sb.StrandId == source.Id && sb.IsEnabled)
            .OrderBy(sb => sb.SortKey)
            .Join(db.Beats.AsNoTracking(), sb => sb.BeatId, b => b.Id,
                  (sb, b) => new { sb.SortKey, Beat = b })
            .ToListAsync();

        Console.WriteLine($"[clone-strand] Source: '{source.Title}' ({source.Slug}) — {sourceBeats.Count} beat(s)");

        // ── Determine new title and slug ──────────────────────────────────────
        var newTitle = string.IsNullOrWhiteSpace(title)
            ? $"{source.Title} (Clone)"
            : title.Trim();
        var newId   = Guid.CreateVersion7();
        var newSlug = $"{Slugify(newTitle)}-{newId.ToString("N")[..8]}";

        // ── Sort key: append after all existing root strands ─────────────────
        var maxSort = await db.Strands
            .Where(s => s.ParentStrandId == null)
            .Select(s => (double?)s.SortKey)
            .MaxAsync() ?? 0;

        var now = DateTime.UtcNow;

        // ── Insert new Strand ─────────────────────────────────────────────────
        var newStrand = new Strand
        {
            Id               = newId,
            Slug             = newSlug,
            Title            = newTitle,
            StrandCode       = code,
            Kind             = source.Kind,
            Status           = status,
            Synopsis         = source.Synopsis,
            Seed             = source.Seed,
            UniverseId       = source.UniverseId,
            VoiceId          = source.VoiceId,
            VoiceModel       = source.VoiceModel,
            VoiceStability   = source.VoiceStability,
            VoiceSimilarity  = source.VoiceSimilarity,
            VoiceStyle       = source.VoiceStyle,
            VoiceSeed        = source.VoiceSeed,
            TtsEngine        = source.TtsEngine,
            IsWIP            = isDraft,
            SortKey          = maxSort + 100.0,
            CreatedAt        = now,
            UpdatedAt        = now,
        };
        db.Strands.Add(newStrand);

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
                BeatTitle       = src.BeatTitle,
                Synopsis        = src.Synopsis,
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

            db.StrandBeats.Add(new StrandBeat
            {
                StrandId  = newId,
                BeatId    = beatId,
                SortKey   = entry.SortKey,
                IsEnabled = true,
            });
        }

        await db.SaveChangesAsync();

        Console.WriteLine($"[clone-strand] Created '{newTitle}' — {sourceBeats.Count} beat(s) cloned");
        Console.WriteLine($"[clone-strand] id:   {newId}");
        Console.WriteLine($"[clone-strand] slug: {newSlug}");
        if (isDraft) Console.WriteLine("[clone-strand] IsWIP=true — excluded from review/score/publish flows");
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
