using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using static Prose.Core.Services.ReadAlong.StoryFactExtractor;

namespace Prose.Core.Services.ReadAlong;

/// <summary>
/// The Story Atlas (RFC 0019 §6): the book's world drawn as one map from what the read-along has
/// read — every character, place, item, organization and concept as a star, every stated
/// relationship and every pair that shares a paragraph as a thread. Each star and thread knows the
/// chapter it first appeared in, so the map can be scrubbed back to what the reader knew at any
/// chapter. Nobody diagrams anything.
/// </summary>
public sealed class StoryAtlasService(IDbContextFactory<ProseDbContext> dbFactory)
{
    public sealed record Star(string Key, string Name, string Category, Guid? EntityId, bool Draft, int FirstChapter, int Mentions, int Degree);

    public sealed record Thread(string From, string To, string Type, int FirstChapter, int Weight, string? Quote);

    public sealed record ChapterTick(int Index, string Title, Guid ChapterNodeId, int NewStars);

    public sealed record Atlas(IReadOnlyList<Star> Stars, IReadOnlyList<Thread> Threads, IReadOnlyList<ChapterTick> Chapters);

    /// <param name="asOfChapter">Only what the reader knew by this chapter (1-based); null = the whole story.</param>
    /// <param name="includeDrafts">Show names still waiting for Keep it (dimmed in the UI).</param>
    public async Task<Atlas> GetAsync(Guid bookId, int? asOfChapter = null, bool includeDrafts = true, int minPairWeight = 2, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var readings = await db.ChapterReadings.AsNoTracking().IgnoreQueryFilters()
            .Where(r => r.BookNodeId == bookId && r.Status == ReadAlongStatus.Done)
            .OrderBy(r => r.ChapterIndex).ToListAsync(ct);

        var stars = new Dictionary<string, (string Name, string Category, Guid? EntityId, int First, int Mentions)>(StringComparer.Ordinal);
        var pairs = new Dictionary<(string, string), (int First, int Weight)>();
        var ticks = new List<ChapterTick>();
        foreach (var r in readings)
        {
            var d = ReadAlongService.Detail(r.MetJson);
            var before = stars.Count;
            if (asOfChapter is null || r.ChapterIndex <= asOfChapter)
            {
                foreach (var m in d.Mentions)
                    stars[m.Key] = stars.TryGetValue(m.Key, out var s)
                        ? (s.Name, s.Category, s.EntityId ?? m.EntityId, s.First, s.Mentions + m.Count)
                        : (m.Name, m.Category, m.EntityId, r.ChapterIndex, m.Count);
                foreach (var p in d.Pairs)
                {
                    if (p.Length < 3) continue;
                    var k = (p[0], p[1]);
                    var w = int.TryParse(p[2], out var n) ? n : 1;
                    pairs[k] = pairs.TryGetValue(k, out var e) ? (e.First, e.Weight + w) : (r.ChapterIndex, w);
                }
            }
            ticks.Add(new ChapterTick(r.ChapterIndex, r.ChapterTitle, r.ChapterNodeId, stars.Count - before));
        }

        // Which keys are still drafts, and which have been tossed (never drawn).
        var suggestions = await db.StoryBibleSuggestions.AsNoTracking().IgnoreQueryFilters()
            .Where(s => s.BookNodeId == bookId && s.Kind == "entry")
            .Select(s => new { s.Name, s.Status, s.EntityId, s.Category }).ToListAsync(ct);
        var draftKeys = suggestions.Where(s => s.Status == ReadAlongStatus.Pending).Select(s => Key(s.Name)).ToHashSet();
        var tossedKeys = suggestions.Where(s => s.Status == ReadAlongStatus.Tossed).Select(s => Key(s.Name)).ToHashSet();
        var keptIds = suggestions.Where(s => s.Status == ReadAlongStatus.Kept && s.EntityId != null)
            .ToDictionary(s => Key(s.Name), s => s.EntityId);

        // Typed threads: relations the prose states (confirmed, or still observations behind drafts).
        var relationFacts = await db.StoryFacts.AsNoTracking().IgnoreQueryFilters()
            .Where(f => f.BookNodeId == bookId && f.SupersededAt == null && f.Category == "character" &&
                        (asOfChapter == null || f.ChapterIndex <= asOfChapter))
            .Select(f => new { f.SubjectKey, f.Attribute, f.Value, f.ChapterIndex, f.Quote, f.Confirmed })
            .ToListAsync(ct);

        var threads = new List<Thread>();
        foreach (var f in relationFacts)
        {
            var to = Key(f.Value);
            if (!stars.ContainsKey(f.SubjectKey) || !stars.ContainsKey(to) || to == f.SubjectKey) continue;
            if (!includeDrafts && !f.Confirmed) continue;
            threads.Add(new Thread(f.SubjectKey, to, f.Attribute, f.ChapterIndex, 3, f.Quote));
        }
        var typed = threads.Select(t => (string.CompareOrdinal(t.From, t.To) < 0 ? (t.From, t.To) : (t.To, t.From))).ToHashSet();
        foreach (var (k, v) in pairs)
        {
            if (v.Weight < minPairWeight || typed.Contains(k)) continue;
            threads.Add(new Thread(k.Item1, k.Item2, "appears with", v.First, v.Weight, null));
        }

        var visible = stars
            .Where(s => !tossedKeys.Contains(s.Key) && (includeDrafts || !draftKeys.Contains(s.Key) || s.Value.EntityId is not null))
            .ToDictionary(s => s.Key, s => s.Value);
        threads = threads.Where(t => visible.ContainsKey(t.From) && visible.ContainsKey(t.To)).ToList();
        var degree = threads.SelectMany(t => new[] { t.From, t.To }).GroupBy(k => k).ToDictionary(g => g.Key, g => g.Count());

        var list = visible.Select(s => new Star(s.Key, s.Value.Name, s.Value.Category,
                s.Value.EntityId ?? keptIds.GetValueOrDefault(s.Key), draftKeys.Contains(s.Key) && s.Value.EntityId is null,
                s.Value.First, s.Value.Mentions, degree.GetValueOrDefault(s.Key)))
            .OrderByDescending(s => s.Mentions).ToList();
        return new Atlas(list, threads, ticks);
    }
}
