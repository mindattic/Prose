using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Core.Services.Factory;

/// <summary>A capitalized name the book uses in two or more beats that no entity, alias, tag or
/// incidental ruling accounts for. <c>BookSays</c> is set when the book already tags this exact
/// surface text as one entity elsewhere (the scan dropped it as ambiguous across the universe):
/// the book itself has said who it is, so <c>--pin</c> can extend that tag without a guess.</summary>
public sealed record UnresolvedName(string Name, int Beats, IReadOnlyList<int> Numbers, IReadOnlyList<Guid> BeatIds,
    Guid? BookSays = null, string? BookSaysName = null, string? BookSaysType = null);

/// <summary>Occurrences of a known entity in one beat that the save path would tag and the stored
/// text does not.</summary>
public sealed record UntaggedMention(Guid BeatId, int Number, Guid EntityId, string EntityName, int Missing);

public sealed record CaptureReport(
    int BeatsScanned, int Candidates, IReadOnlyList<UnresolvedName> Unresolved, IReadOnlyList<UntaggedMention> Untagged,
    IReadOnlyDictionary<Guid, (int Unresolved, int Untagged)> ByBeat, long Millis);

/// <summary>
/// Station F4, Captured (RFC 0015 §3.8): every name the book uses is in the world.
///
/// <para><b>(a) Unresolved names.</b> The capitalized phrases a beat's prose uses that the save
/// path's own entity scan cannot claim — <see cref="EntityMentionScanner.FindUnresolvedProperNouns"/>,
/// the residue detector <c>--unresolved-nouns</c> used to report and nothing ever acted on — kept
/// when the name is in at least <see cref="MinBeats"/> beats and no incidental ruling names it.
/// Each is resolved by making it an entity, or by <c>record_ruling(kind: incidental)</c> when it
/// intentionally has none.</para>
///
/// <para><b>(b) Untagged known names.</b> An entity tagged somewhere in this book whose name the
/// save path's scan finds in a beat more often than the beat's stored tags point at it. The scan
/// is exactly the one the save runs (same candidate index, same pinned tags), so re-saving the
/// beat through the one door — tags only, no word changes — always clears what this reports.</para>
///
/// <para>Computed, never stored: the report is memoised in this process against a fingerprint of
/// its inputs (the book's beats and hashes, the universe's entity stamp, the incidental rulings),
/// so it is recomputed exactly when one of them changes.</para>
/// </summary>
public sealed class CaptureScanner(IDbContextFactory<ProseDbContext> dbFactory, BookSpineService spine, RulingService rulings)
{
    /// <summary>A name must be used in this many beats before it counts as unresolved.</summary>
    public const int MinBeats = 2;

    static readonly ConcurrentDictionary<Guid, (string Key, CaptureReport Report)> Memo = new();

    public async Task<CaptureReport> ScanAsync(Guid bookId, CancellationToken ct = default)
    {
        var watch = Stopwatch.StartNew();
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var universe = await db.Nodes.IgnoreQueryFilters().AsNoTracking().Where(n => n.Id == bookId)
            .Select(n => n.UniverseId).FirstOrDefaultAsync(ct);

        var sp = await spine.GetAsync(bookId, ct);
        var order = sp.Chapters.SelectMany(c => c.Beats).ToList();
        var ids = order.Select(b => b.BeatId).ToList();
        var rows = await db.Beats.AsNoTracking().Where(b => ids.Contains(b.Id))
            .Select(b => new { b.Id, b.Number, b.Text, b.TextHash }).ToDictionaryAsync(b => b.Id, ct);
        var beats = order.Where(b => rows.ContainsKey(b.BeatId)).Select(b => rows[b.BeatId]).ToList();

        var incidental = (await rulings.ListAsync(bookId, RulingKinds.Incidental, ct))
            .Where(r => !string.IsNullOrWhiteSpace(r.Pattern)).ToList();
        var universeEntities = db.Entities.IgnoreQueryFilters().AsNoTracking().Where(e => e.UniverseId == universe);
        var latest = await universeEntities.MaxAsync(e => (DateTime?)e.ModifiedAt, ct);
        var count = await universeEntities.CountAsync(ct);
        var key = Fingerprint(beats.Select(b => $"{b.Id:N}:{b.TextHash}")
            .Append($"entities:{latest?.Ticks}:{count}")
            .Concat(incidental.Select(r => $"incidental:{r.Id:N}")));
        if (Memo.TryGetValue(bookId, out var memo) && memo.Key == key) return memo.Report;

        var candidates = universe == Guid.Empty ? [] : await EntityMentionScanner.BuildCandidateIndexAsync(db, universe, bookId, ct);
        var inBook = beats.SelectMany(b => BeatMarkup.ExtractEntityGuids(b.Text)).ToHashSet();
        var names = await db.Entities.IgnoreQueryFilters().AsNoTracking().Where(e => inBook.Contains(e.Id))
            .Select(e => new { e.Id, e.Name }).ToDictionaryAsync(e => e.Id, e => e.Name, ct);
        var ignore = incidental.Select(r => r.Pattern!.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var beatsByName = new Dictionary<string, List<(Guid Id, int Number)>>(StringComparer.OrdinalIgnoreCase);
        var untagged = new List<UntaggedMention>();
        // Which entities the book's own tags give each exact surface text.
        var surfaceOwners = new Dictionary<string, HashSet<Guid>>(StringComparer.Ordinal);
        foreach (var b in beats)
            foreach (var t in BeatMarkup.ExtractTaggedMentions(b.Text))
            {
                if (!surfaceOwners.TryGetValue(t.Text, out var owners)) surfaceOwners[t.Text] = owners = [];
                owners.Add(t.EntityId);
            }
        foreach (var b in beats)
        {
            if (string.IsNullOrWhiteSpace(b.Text)) continue;
            // Honour tags already in the prose, exactly as the save path does: an ambiguous name
            // the scanner will not guess at is still a mention when a tag says whose it is.
            var pinned = BeatMarkup.ExtractTaggedMentions(b.Text);
            var beatCandidates = candidates;
            if (pinned.Count > 0 && universe != Guid.Empty)
            {
                var live = await NodeWorkbenchService.LoadLiveEntitiesAsync(db, universe, pinned, ct);
                if (live.Count > 0) beatCandidates = EntityMentionScanner.WithPinnedMentions([.. candidates], pinned, live);
            }
            var plain = BeatMarkup.StripEntityTags(b.Text);
            var matches = EntityMentionScanner.Scan(plain, beatCandidates);

            var tagged = BeatMarkup.CountTagsByEntity(b.Text);
            foreach (var g in matches.GroupBy(m => m.EntityId).Where(g => inBook.Contains(g.Key)))
            {
                var missing = g.Count() - tagged.GetValueOrDefault(g.Key);
                if (missing > 0) untagged.Add(new UntaggedMention(b.Id, b.Number, g.Key, names.GetValueOrDefault(g.Key, g.First().Name), missing));
            }

            foreach (var name in EntityMentionScanner.FindUnresolvedProperNouns(plain, matches))
            {
                if (ignore.Contains(name) || CaptureStopWords.Contains(name)) continue;
                if (!beatsByName.TryGetValue(name, out var list)) beatsByName[name] = list = [];
                list.Add((b.Id, b.Number));
            }
        }

        var says = surfaceOwners.Where(kv => kv.Value.Count == 1).ToDictionary(kv => kv.Key, kv => kv.Value.First(), StringComparer.Ordinal);
        var sayIds = says.Values.Distinct().ToList();
        var sayEntities = await db.Entities.IgnoreQueryFilters().AsNoTracking().Where(e => sayIds.Contains(e.Id))
            .Select(e => new { e.Id, e.Name, e.EntityType }).ToDictionaryAsync(e => e.Id, ct);
        var unresolved = beatsByName.Where(kv => kv.Value.Count >= MinBeats)
            .OrderByDescending(kv => kv.Value.Count).ThenBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
            .Select(kv =>
            {
                var owner = says.TryGetValue(kv.Key, out var o) && sayEntities.ContainsKey(o) ? o : (Guid?)null;
                return new UnresolvedName(kv.Key, kv.Value.Count, kv.Value.Select(v => v.Number).ToList(), kv.Value.Select(v => v.Id).ToList(),
                    owner, owner is { } oid ? sayEntities[oid].Name : null, owner is { } tid ? sayEntities[tid].EntityType : null);
            }).ToList();
        var byBeat = new Dictionary<Guid, (int Unresolved, int Untagged)>();
        foreach (var kv in beatsByName.Where(kv => kv.Value.Count >= MinBeats))
            foreach (var (beatId, _) in kv.Value)
                byBeat[beatId] = (byBeat.GetValueOrDefault(beatId).Unresolved + 1, byBeat.GetValueOrDefault(beatId).Untagged);
        foreach (var u in untagged)
            byBeat[u.BeatId] = (byBeat.GetValueOrDefault(u.BeatId).Unresolved, byBeat.GetValueOrDefault(u.BeatId).Untagged + u.Missing);

        var report = new CaptureReport(beats.Count, candidates.Count, unresolved, untagged, byBeat, watch.ElapsedMilliseconds);
        Memo[bookId] = (key, report);
        return report;
    }

    /// <summary>The stored text with every untagged, whole-word, exact-case occurrence of
    /// <paramref name="name"/> wrapped in a tag for <paramref name="entityId"/>. Text inside existing
    /// tags is left alone, so no tag is ever nested or re-pointed. The words do not change.</summary>
    public static string PinName(string stored, string name, Guid entityId, string entityType)
    {
        var spans = BeatMarkup.TagSpans(stored);
        var hits = System.Text.RegularExpressions.Regex.Matches(stored, $@"\b{System.Text.RegularExpressions.Regex.Escape(name)}\b")
            .Where(m => !spans.Any(s => m.Index >= s.Start && m.Index < s.Start + s.Length))
            .OrderByDescending(m => m.Index).ToList();
        var sb = new StringBuilder(stored);
        foreach (var m in hits)
        {
            sb.Remove(m.Index, m.Length);
            sb.Insert(m.Index, $"<entity repo=\"{entityType}\" guid=\"{entityId}\">{m.Value}</entity>");
        }
        return sb.ToString();
    }

    /// <summary>Capitalized words that are never a missing entity in GLMZ prose: languages and
    /// nationalities, titles and forms of address, and in-world common nouns the prose capitalizes
    /// by convention. Positional capitals are already suppressed upstream; this is what is left.</summary>
    public static readonly HashSet<string> CaptureStopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        // Calibrated on BCODA 2026-09-23 (108 unresolved before, under the 150 ceiling): the words
        // left once the real names had entities, aliases, pins or incidental rulings — units,
        // grades and formats ("Mach six", "Class-2", "Tier 3", "Gate 3", "Unit 2E", "Addendum A"),
        // the generic tail of a proper name whose head is already an entity ("Markel Street",
        // "Branch Manager", "Halvorsen Civic", "Kessler and Sons"), bare compass words left behind
        // by "West Town West" or "South Loop", and weekday plurals.
        "Mach", "Class", "Tier", "Unit", "Gate", "Addendum",
        "Street", "Avenue", "Road", "Boulevard", "Manager", "Civic", "Sons",
        "North", "South", "East", "West",
        "Mondays", "Tuesdays", "Wednesdays", "Thursdays", "Fridays", "Saturdays", "Sundays",
    };

    private static string Fingerprint(IEnumerable<string> parts) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("\n", parts)))).ToLowerInvariant();
}
