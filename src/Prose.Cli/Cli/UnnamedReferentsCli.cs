using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;
using Prose.Core.Services;
using Prose.Core.Services.Obligations;

namespace Prose.Cli;

/// <summary>
/// prose --scan-unnamed-referents --slug &lt;slug&gt; [--json] [--min-mentions N]
///
/// Deterministic, free, report-only (RFC 0013 D6d): definite noun phrases that behave like a
/// person or thing of note but carry no entity tag — "the girl behind the curtain", "the woman in
/// the tan coat" — with where they first appear, how often, and whether they are ever seen again.
/// A referent seen in one beat and never again is exactly the shape of the BCODA curtain-girl.
/// Hint-only: it creates nothing; the obligation extractor decides what is owed.
/// </summary>
public static class UnnamedReferentsCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? Flag(string name) { for (int i = 0; i < args.Length - 1; i++) if (args[i] == name) return args[i + 1]; return null; }
        var slug = Flag("--slug");
        var json = args.Contains("--json");
        var minMentions = int.TryParse(Flag("--min-mentions"), out var m) ? m : 1;
        if (string.IsNullOrWhiteSpace(slug)) { Console.Error.WriteLine("Usage: prose --scan-unnamed-referents --slug <slug> [--json] [--min-mentions N]"); return 2; }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();
        var resolved = await NodeRefResolver.ResolveAsync(db, slug);
        if (resolved == null) { Console.Error.WriteLine($"[unnamed-referents] {NodeRefResolver.NotFoundMessage(slug)}"); return 1; }

        var clock = await NarrativeObligationService.LoadClockAsync(db, resolved.Value, CancellationToken.None);
        var ids = clock.Beats.Keys.ToList();
        var beats = await db.Beats.AsNoTracking().Where(b => ids.Contains(b.Id)).Select(b => new { b.Id, b.Text }).ToListAsync();
        var universeId = await db.Nodes.IgnoreQueryFilters().AsNoTracking().Where(n => n.Id == resolved.Value).Select(n => n.UniverseId).FirstOrDefaultAsync();
        var aliases = (await db.Entities.AsNoTracking().IgnoreQueryFilters().Where(e => e.UniverseId == universeId && e.Status != "archived").Select(e => e.Name).ToListAsync())
            .Select(n => n.ToLowerInvariant().Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var byLabel = new Dictionary<string, (int FirstChapter, Guid FirstBeat, int Mentions, int Beats, int LastChapter, bool Subject)>(StringComparer.OrdinalIgnoreCase);
        foreach (var b in beats.OrderBy(b => clock.PositionOf(b.Id)))
        {
            var ch = clock.ChapterOf(b.Id);
            foreach (var r in UnnamedReferentScanner.Scan(BeatMarkup.StripEntityTags(b.Text), aliases, max: 20))
            {
                if (byLabel.TryGetValue(r.Label, out var cur))
                    byLabel[r.Label] = (cur.FirstChapter, cur.FirstBeat, cur.Mentions + r.Mentions, cur.Beats + 1, ch, cur.Subject || r.SubjectPosition);
                else
                    byLabel[r.Label] = (ch, b.Id, r.Mentions, 1, ch, r.SubjectPosition);
            }
        }

        var rows = byLabel.Where(kv => kv.Value.Mentions >= minMentions)
            .Select(kv => new
            {
                referent = kv.Key, first_chapter = kv.Value.FirstChapter, first_beat_id = kv.Value.FirstBeat,
                mentions = kv.Value.Mentions, beats_spanned = kv.Value.Beats, last_chapter = kv.Value.LastChapter,
                acts = kv.Value.Subject,
                flagged = kv.Value.Mentions >= 2 && kv.Value.Beats == 1 && kv.Value.Subject,
            })
            .OrderByDescending(r => r.flagged).ThenByDescending(r => r.mentions).ToList();

        if (json) { Console.WriteLine(JsonSerializer.Serialize(new { node_id = resolved, examined_beats = beats.Count, referents = rows }, new JsonSerializerOptions { WriteIndented = true })); return beats.Count == 0 ? 1 : 0; }

        Console.WriteLine($"[unnamed-referents] examined {beats.Count} of {clock.BeatCount} beat(s) — {rows.Count} referent(s), {rows.Count(r => r.flagged)} flagged (≥2 mentions in one beat, acts, never seen again)");
        if (beats.Count == 0) { Console.WriteLine("  COULD NOT LOOK — no beats."); return 1; }
        foreach (var r in rows.Take(80))
            Console.WriteLine($"  {(r.flagged ? "!" : " ")} Ch{r.first_chapter,-3} x{r.mentions,-3} beats {r.beats_spanned,-3} last Ch{r.last_chapter,-3} {(r.acts ? "acts" : "    ")}  {r.referent}");
        return 0;
    }
}
