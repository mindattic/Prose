using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Blazor.Cli;

/// <summary>
/// Fills missing beat metadata WITHOUT touching prose.
///
///   ss --backfill-synopses        --slug &lt;s&gt; [--model &lt;id&gt;] [--force]
///       Generate a 1–2 sentence Synopsis from each beat's prose (LLM). The synopsis is
///       the BeatGoal proxy the engine reads for mode detection + pacing, so filling it
///       sharpens coverage/mode signal. Use --model claude-haiku-4-5-20251001 for a cheap
///       pass. Skips beats that already have one unless --force.
///
///   ss --backfill-structure-roles --slug &lt;s&gt; [--force]
///       Assign Save-the-Cat StructureRole deterministically. For a BOOK the arc spans the
///       whole novel (global reading-order position: Ch1≈Opening/Catalyst, mid≈Midpoint,
///       end≈Finale/Final Image) — NOT per-chapter, which would yield 16 "Opening Image"
///       beats. A standalone strand is positioned within itself. No LLM.
///
/// Both flags may be combined. Book-aware (fans out into draft chapters in reading order).
/// </summary>
public static class BackfillBeatMetaCli
{
    public static async Task<int> RunAsync(IServiceProvider sp, string[] args)
    {
        var slug      = GetArg(args, "--slug");
        var model     = GetArg(args, "--model");
        var force     = args.Contains("--force");
        var doSynopses = args.Contains("--backfill-synopses");
        var doRoles    = args.Contains("--backfill-structure-roles");
        if (slug == null) { Console.Error.WriteLine("Usage: ss --backfill-synopses|--backfill-structure-roles --slug <slug> [--model <id>] [--force]"); return 2; }
        if (!doSynopses && !doRoles) { Console.Error.WriteLine("Specify --backfill-synopses and/or --backfill-structure-roles."); return 2; }

        var dbFactory  = sp.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        var root = await db.Strands.AsNoTracking()
            .Where(s => s.Slug == slug || s.StrandCode == slug)
            .Select(s => new { s.Id, s.SortKey })
            .FirstOrDefaultAsync();
        if (root == null) { Console.Error.WriteLine($"Strand not found: {slug}"); return 2; }

        var childIds = await db.Strands.AsNoTracking()
            .Where(s => s.ParentStrandId == root.Id && s.Status == "draft")
            .OrderBy(s => s.SortKey).Select(s => s.Id).ToListAsync();
        var strandIds = childIds.Count > 0 ? childIds : [root.Id];

        // All beats in global reading order: chapter order, then beat SortKey within chapter.
        var ordered = new List<Guid>();
        var beatInfo = new Dictionary<Guid, (string? Text, string? Synopsis, string? Role)>();
        foreach (var sid in strandIds)
        {
            var beats = await (from sb in db.StrandBeats.AsNoTracking()
                               join b in db.Beats.AsNoTracking() on sb.BeatId equals b.Id
                               where sb.StrandId == sid && sb.IsEnabled
                               orderby sb.SortKey
                               select new { b.Id, b.Text, b.Synopsis, b.StructureRole }).ToListAsync();
            foreach (var b in beats) { ordered.Add(b.Id); beatInfo[b.Id] = (b.Text, b.Synopsis, b.StructureRole); }
        }
        Console.WriteLine($"Backfilling {ordered.Count} beats across {strandIds.Count} strand(s).");

        // ── Structure roles (deterministic, book-global arc) ─────────────────────
        if (doRoles)
        {
            var methodology = sp.GetRequiredService<StoryMethodologyService>();
            var total = ordered.Count;
            var updates = new Dictionary<Guid, string>();
            for (var i = 0; i < total; i++)
            {
                var id = ordered[i];
                if (!force && beatInfo[id].Role != null) continue;
                updates[id] = methodology.GetBeatRole(i, total).Name;
            }
            await ApplyAsync(dbFactory, updates, (b, v) => b.StructureRole = v);
            Console.WriteLine($"  StructureRole: {updates.Count} beat(s) set (book-global arc).");
        }

        // ── Synopses (LLM) ───────────────────────────────────────────────────────
        if (doSynopses)
        {
            var llm = sp.GetRequiredService<ILlmService>();
            var targets = ordered.Where(id => force || string.IsNullOrWhiteSpace(beatInfo[id].Synopsis))
                                  .Where(id => !string.IsNullOrWhiteSpace(beatInfo[id].Text)).ToList();
            Console.WriteLine($"  Synopses: {targets.Count} beat(s) to generate (model={model ?? "default"})…");

            const string system = "You write terse editorial beat synopses. Given a story beat's prose, "
                + "return ONE sentence (max two) stating what happens and its narrative purpose. "
                + "No preamble, no quotes, no markdown — just the sentence.";

            var sem = new SemaphoreSlim(5);
            var results = new System.Collections.Concurrent.ConcurrentDictionary<Guid, string>();
            var done = 0;
            await Task.WhenAll(targets.Select(async id =>
            {
                await sem.WaitAsync();
                try
                {
                    var text = beatInfo[id].Text!;
                    var prompt = text.Length > 6000 ? text[..6000] : text;
                    var raw = await llm.GenerateAsync(system, prompt, temperature: 0.2, maxTokens: 120, model: model);
                    var syn = raw.Trim().Trim('"');
                    if (syn.Length > 0) results[id] = syn.Length > 1000 ? syn[..1000] : syn;
                    var n = Interlocked.Increment(ref done);
                    if (n % 25 == 0 || n == targets.Count) Console.WriteLine($"    {n}/{targets.Count}");
                }
                catch (Exception ex) { Console.Error.WriteLine($"    beat {id}: {ex.Message}"); }
                finally { sem.Release(); }
            }));

            await ApplyAsync(dbFactory, results.ToDictionary(k => k.Key, v => v.Value), (b, v) => b.Synopsis = v);
            Console.WriteLine($"  Synopses: {results.Count} beat(s) written.");
        }

        return 0;
    }

    private static async Task ApplyAsync(
        IDbContextFactory<StreetSamuraiDbContext> dbFactory,
        Dictionary<Guid, string> updates,
        Action<Beat, string> set)
    {
        if (updates.Count == 0) return;
        await using var db = await dbFactory.CreateDbContextAsync();
        var ids = updates.Keys.ToList();
        var beats = await db.Beats.Where(b => ids.Contains(b.Id)).ToListAsync();
        foreach (var b in beats) set(b, updates[b.Id]);
        await db.SaveChangesAsync();
    }

    private static string? GetArg(string[] args, string flag)
    {
        var i = Array.IndexOf(args, flag);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }
}
