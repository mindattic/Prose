using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Core.Services;

/// <summary>One relation a contribution establishes: this character <paramref name="Relation"/>
/// <paramref name="Target"/> (e.g. was_killed_by Ekow).</summary>
public sealed record ContributionEdge(string Relation, Guid Target);

/// <summary>One character's line for one beat, as written.</summary>
public sealed record ContributionInput(string Text, IReadOnlyList<ContributionEdge> Edges);

/// <summary>Where a beat's lines stand against its prose: characters tagged with no line, lines
/// for characters the beat does not tag, lines written against other text, and pronouns with no
/// tag at all. <see cref="Ok"/> only when all four are empty.</summary>
/// <summary>A name the ambiguity guard in <see cref="EntityMentionScanner.BuildCandidateIndexWithDroppedAsync"/>
/// refuses to anchor a tag to, found sitting bare (no tag at all, by any entity) in this beat's
/// prose anyway. Author 2026-10-13: the "Sable" class of silent loss — a bare mention of an
/// ambiguous name that nobody pinned, so the save path correctly declined to guess and the mention
/// went untracked with no other signal to catch it on a beat's first-ever tagging pass (a beat that
/// already has a contribution line for the entity would instead show up in <see cref="ContributionCheck.Extra"/>
/// once its tag vanishes — this field exists for the beat that never had one yet).</summary>
public sealed record UntaggedName(string Text, IReadOnlyList<Guid> Candidates);

public sealed record ContributionCheck(
    IReadOnlyList<Guid> Missing, IReadOnlyList<Guid> Extra, IReadOnlyList<Guid> Stale,
    IReadOnlyList<(int Offset, string Word)> UntaggedPronouns, IReadOnlyList<UntaggedName> UntaggedNames)
{
    public bool Ok => Missing.Count == 0 && Extra.Count == 0 && Stale.Count == 0
        && UntaggedPronouns.Count == 0 && UntaggedNames.Count == 0;
}

/// <summary>A line of a character's history: one beat, cited.</summary>
public sealed record HistoryEntry(string BookCode, int Chapter, int BeatNumber, Guid BeatId, string? Line, string State,
    IReadOnlyList<ContributionEdge> Edges);

/// <summary>A relation the story establishes, with the beat that establishes it.</summary>
public sealed record StoryEdge(Guid SourceId, string SourceName, string Relation, Guid TargetId, string TargetName,
    string BookCode, int Chapter, int BeatNumber, string Line, string State);

/// <summary>
/// Beat contributions (author 2026-10-12, WO:01a13278): what each character does in each beat,
/// written with the beat and bound to its text. A character's history and the relations the story
/// establishes are read back from these lines, never stored separately, so they cannot disagree
/// with the prose. See <see cref="BeatContribution"/>.
/// </summary>
public class BeatContributionService(IDbContextFactory<ProseDbContext> dbFactory)
{
    public const string EdgeSource = "contribution";
    private static readonly Regex RelationShape = new(@"^[a-z][a-z0-9_]{1,78}$", RegexOptions.Compiled);

    /// <summary>
    /// Replaces a beat's lines in one write. Refused unless the lines name EXACTLY the characters the
    /// beat tags (by name or pronoun), every line has text, and every edge names a lowercase relation
    /// and a live entity in the beat's universe. Each line is stamped with the beat's TextHash; its
    /// edges replace the beat's previous contribution edges in the entity graph.
    /// </summary>
    public async Task SetAsync(Guid beatId, IReadOnlyDictionary<Guid, ContributionInput> lines, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var beat = await db.Beats.AsNoTracking().FirstOrDefaultAsync(b => b.Id == beatId, ct)
                   ?? throw new InvalidOperationException($"Beat {beatId} not found.");
        var text = beat.Text ?? "";
        var universeId = await (from bn in db.BeatNodes where bn.BeatId == beatId
                                join n in db.Nodes.IgnoreQueryFilters() on bn.NodeId equals n.Id
                                select n.UniverseId).FirstOrDefaultAsync(ct);

        var tagged = await TaggedCharactersAsync(db, text, ct);
        var problems = new List<string>();
        var missing = tagged.Where(id => !lines.ContainsKey(id)).ToList();
        var extra = lines.Keys.Where(id => !tagged.Contains(id)).ToList();
        if (missing.Count > 0) problems.Add($"no line for tagged character(s): {await NamesAsync(db, missing, ct)}");
        if (extra.Count > 0) problems.Add($"line(s) for character(s) the beat does not tag: {await NamesAsync(db, extra, ct)}");
        foreach (var (id, line) in lines)
        {
            if (string.IsNullOrWhiteSpace(line.Text)) problems.Add($"empty line for {id}");
            foreach (var e in line.Edges)
                if (!RelationShape.IsMatch(e.Relation)) problems.Add($"relation \"{e.Relation}\" is not lowercase_with_underscores");
        }
        var targets = lines.Values.SelectMany(l => l.Edges).Select(e => e.Target).Distinct().ToList();
        var liveTargets = targets.Count == 0 ? [] : await db.Set<Entity>().AsNoTracking().IgnoreQueryFilters()
            .Where(e => targets.Contains(e.Id) && e.Status != "archived" && (universeId == Guid.Empty || e.UniverseId == universeId))
            .Select(e => e.Id).ToListAsync(ct);
        foreach (var t in targets.Except(liveTargets)) problems.Add($"edge target {t} is not a live entity in this universe");
        if (problems.Count > 0) throw new InvalidOperationException($"Contributions for beat #{beat.Number} refused: " + string.Join("; ", problems) + ".");

        var hash = beat.TextHash ?? Beat.ComputeHash(text);
        var now = DateTime.UtcNow;
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.BeatContributions.Where(c => c.BeatId == beatId).ExecuteDeleteAsync(ct);
        await db.Edges.IgnoreQueryFilters().Where(e => e.Source == EdgeSource && e.ValidFromBeatId == beatId).ExecuteDeleteAsync(ct);
        foreach (var (id, line) in lines)
        {
            db.BeatContributions.Add(new BeatContribution
            {
                BeatId = beatId, EntityId = id, Text = line.Text.Trim(), TextHash = hash, UpdatedAt = now,
                EdgesJson = JsonSerializer.Serialize(line.Edges.Select(e => new { relation = e.Relation, target = e.Target })),
            });
            foreach (var e in line.Edges)
                db.Edges.Add(new Edge
                {
                    UniverseId = universeId, SourceId = id, TargetId = e.Target, RelationType = e.Relation,
                    Description = line.Text.Trim(), Source = EdgeSource, ValidFromBeatId = beatId,
                });
        }
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    /// <summary>The beat's lines, as stored.</summary>
    public async Task<List<BeatContribution>> GetAsync(Guid beatId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.BeatContributions.AsNoTracking().Where(c => c.BeatId == beatId).ToListAsync(ct);
    }

    /// <summary>Where one beat's lines stand against its prose.</summary>
    public async Task<ContributionCheck> CheckAsync(Guid beatId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var beat = await db.Beats.AsNoTracking().FirstAsync(b => b.Id == beatId, ct);
        var tagged = await TaggedCharactersAsync(db, beat.Text ?? "", ct);
        var rows = await db.BeatContributions.AsNoTracking().Where(c => c.BeatId == beatId)
            .Select(c => new { c.EntityId, c.TextHash }).ToListAsync(ct);
        var check = Check(beat.Text ?? "", beat.TextHash, tagged, rows.Select(r => (r.EntityId, r.TextHash)).ToList());
        var untaggedNames = await UntaggedNamesAsync(db, beatId, beat.Text ?? "", ct);
        return untaggedNames.Count == 0 ? check : check with { UntaggedNames = untaggedNames };
    }

    /// <summary>The pure check the gate and <see cref="CheckAsync"/> share.</summary>
    public static ContributionCheck Check(string text, string? textHash, IReadOnlySet<Guid> taggedCharacters,
        IReadOnlyList<(Guid EntityId, string TextHash)> rows)
    {
        var hash = textHash ?? Beat.ComputeHash(text);
        var have = rows.Select(r => r.EntityId).ToHashSet();
        return new ContributionCheck(
            taggedCharacters.Where(id => !have.Contains(id)).ToList(),
            have.Where(id => !taggedCharacters.Contains(id)).ToList(),
            rows.Where(r => taggedCharacters.Contains(r.EntityId) && r.TextHash != hash).Select(r => r.EntityId).ToList(),
            BeatMarkup.UntaggedPronouns(text), []);
    }

    /// <summary>Bare mentions of an ambiguous name sitting untagged in <paramref name="text"/> — the
    /// "Sable" class of silent loss (see <see cref="UntaggedName"/>). Needs the book's universe to
    /// know which names are ambiguous there, so this is async/DB-backed and kept OUT of the pure
    /// <see cref="Check"/> the F3 factory gate runs per-beat across whole books: it is a diagnostic
    /// for the interactive per-beat tools (<c>get_beat_contributions</c>, CLI <c>--beat
    /// contributions</c>), not a new corpus-wide gate condition.</summary>
    private static async Task<List<UntaggedName>> UntaggedNamesAsync(
        ProseDbContext db, Guid beatId, string text, CancellationToken ct)
    {
        var bookNodeId = await NodeWorkbenchService.ResolveBookAncestorForBeatAsync(db, beatId, ct);
        if (bookNodeId is not { } book) return [];
        var universeId = await db.Nodes.IgnoreQueryFilters().AsNoTracking()
            .Where(n => n.Id == book).Select(n => n.UniverseId).FirstOrDefaultAsync(ct);
        if (universeId == Guid.Empty) return [];
        var (_, dropped) = await EntityMentionScanner.BuildCandidateIndexWithDroppedAsync(db, universeId, book, ct);
        if (dropped.Count == 0) return [];

        // A pinned mention (WithPinnedMentions) resolves EVERY bare occurrence of the same surface
        // text to the same guid for the whole beat, so if any of this name's claimants is already
        // tagged anywhere in the beat, no bare occurrence survives the next save — nothing to flag.
        var taggedGuids = BeatMarkup.ExtractEntityGuids(text).ToHashSet();
        var plain = BeatMarkup.StripEntityTags(text);
        var found = new List<UntaggedName>();
        foreach (var d in dropped)
        {
            if (d.Owners.Any(taggedGuids.Contains)) continue;
            var pattern = $@"\b{Regex.Escape(d.Text)}\b";
            var opts = RegexOptions.CultureInvariant | (d.RequiresStrictCase ? RegexOptions.None : RegexOptions.IgnoreCase);
            if (Regex.IsMatch(plain, pattern, opts))
                found.Add(new UntaggedName(d.Text, d.Owners));
        }
        return found;
    }

    /// <summary>The characters a beat tags, by name or pronoun: tagged guids that are character records.</summary>
    public static async Task<HashSet<Guid>> TaggedCharactersAsync(ProseDbContext db, string text, CancellationToken ct)
    {
        var ids = BeatMarkup.ExtractEntityGuids(text).ToList();
        if (ids.Count == 0) return [];
        return (await db.Set<Character>().AsNoTracking().IgnoreQueryFilters()
            .Where(c => ids.Contains(c.Id)).Select(c => c.Id).ToListAsync(ct)).ToHashSet();
    }

    /// <summary>
    /// A character's history: every beat that tags them, in reading order, with its line. A beat
    /// that tags them with no line reads <c>missing</c>; a line written against other text reads
    /// <c>stale</c>. Nothing is hidden: the history is exactly as complete as the beats are.
    /// </summary>
    public async Task<List<HistoryEntry>> HistoryAsync(Guid entityId, string? bookCode = null, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var needle = entityId.ToString();
        var beats = await db.Beats.AsNoTracking().Where(b => b.Text != null && b.Text.Contains(needle))
            .Select(b => new { b.Id, b.Number, b.Text, b.TextHash }).ToListAsync(ct);
        // Contains is a cheap pre-filter; the tag parse is the truth.
        beats = beats.Where(b => BeatMarkup.ExtractEntityGuids(b.Text).Contains(entityId)).ToList();
        var ids = beats.Select(b => b.Id).ToList();
        var lines = await db.BeatContributions.AsNoTracking().Where(c => c.EntityId == entityId && ids.Contains(c.BeatId))
            .ToDictionaryAsync(c => c.BeatId, ct);
        var places = await PlacesAsync(db, ids, ct);

        var entries = new List<(HistoryEntry E, (double, double, double) Order)>();
        foreach (var b in beats)
        {
            if (!places.TryGetValue(b.Id, out var p)) continue;
            if (bookCode != null && !string.Equals(p.BookCode, bookCode, StringComparison.OrdinalIgnoreCase)) continue;
            lines.TryGetValue(b.Id, out var line);
            var state = line == null ? "missing" : line.TextHash == (b.TextHash ?? Beat.ComputeHash(b.Text ?? "")) ? "current" : "stale";
            entries.Add((new HistoryEntry(p.BookCode, p.Chapter, b.Number, b.Id, line?.Text, state, ParseEdges(line?.EdgesJson)),
                (p.BookSort, p.ChapterSort, p.BeatSort)));
        }
        return entries.OrderBy(x => x.Order.Item1).ThenBy(x => x.Order.Item2).ThenBy(x => x.Order.Item3).Select(x => x.E).ToList();
    }

    /// <summary>The relations the story establishes from or to an entity, each with its beat.</summary>
    public async Task<List<StoryEdge>> EdgesAsync(Guid entityId, string? relation = null, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var q = db.Edges.AsNoTracking().IgnoreQueryFilters()
            .Where(e => e.Source == EdgeSource && e.ValidFromBeatId != null && (e.SourceId == entityId || e.TargetId == entityId));
        if (!string.IsNullOrWhiteSpace(relation)) q = q.Where(e => e.RelationType == relation);
        var edges = await q.ToListAsync(ct);
        var beatIds = edges.Select(e => e.ValidFromBeatId!.Value).Distinct().ToList();
        var beats = await db.Beats.AsNoTracking().Where(b => beatIds.Contains(b.Id))
            .Select(b => new { b.Id, b.Number, b.Text, b.TextHash }).ToDictionaryAsync(b => b.Id, ct);
        var lineHashes = await db.BeatContributions.AsNoTracking().Where(c => beatIds.Contains(c.BeatId))
            .Select(c => new { c.BeatId, c.EntityId, c.TextHash }).ToListAsync(ct);
        var entityIds = edges.SelectMany(e => new[] { e.SourceId, e.TargetId }).Distinct().ToList();
        var names = await db.Set<Entity>().AsNoTracking().IgnoreQueryFilters().Where(e => entityIds.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, e => e.Name, ct);
        var places = await PlacesAsync(db, beatIds, ct);

        return edges.Select(e =>
        {
            var b = beats.GetValueOrDefault(e.ValidFromBeatId!.Value);
            var p = places.GetValueOrDefault(e.ValidFromBeatId!.Value) ?? new Place("?", 0, 0, 0, 0);
            var h = lineHashes.FirstOrDefault(l => l.BeatId == e.ValidFromBeatId && l.EntityId == e.SourceId)?.TextHash;
            var state = b == null ? "missing" : h == (b.TextHash ?? Beat.ComputeHash(b.Text ?? "")) ? "current" : "stale";
            return (Edge: new StoryEdge(e.SourceId, names.GetValueOrDefault(e.SourceId, "?"), e.RelationType, e.TargetId,
                names.GetValueOrDefault(e.TargetId, "?"), p.BookCode ?? "?", p.Chapter, b?.Number ?? 0, e.Description ?? "", state),
                Order: (p.BookSort, p.ChapterSort, p.BeatSort));
        }).OrderBy(x => x.Order.Item1).ThenBy(x => x.Order.Item2).ThenBy(x => x.Order.Item3).Select(x => x.Edge).ToList();
    }

    private sealed record Place(string BookCode, int Chapter, double BookSort, double ChapterSort, double BeatSort);

    /// <summary>For each beat: its book's code, its chapter's ordinal in the book, and the sort keys
    /// that give reading order.</summary>
    private static async Task<Dictionary<Guid, Place>> PlacesAsync(ProseDbContext db, IReadOnlyCollection<Guid> beatIds, CancellationToken ct)
    {
        var result = new Dictionary<Guid, Place>();
        if (beatIds.Count == 0) return result;
        var links = await db.BeatNodes.AsNoTracking().Where(bn => beatIds.Contains(bn.BeatId))
            .Select(bn => new { bn.BeatId, bn.NodeId, bn.SortKey }).ToListAsync(ct);
        var nodes = new Dictionary<Guid, Node>();
        var frontier = links.Select(l => l.NodeId).Distinct().ToList();
        while (frontier.Count > 0)
        {
            var batch = await db.Nodes.IgnoreQueryFilters().AsNoTracking().Where(n => frontier.Contains(n.Id)).ToListAsync(ct);
            foreach (var n in batch) nodes[n.Id] = n;
            frontier = batch.Where(n => n.ParentNodeId != null && !nodes.ContainsKey(n.ParentNodeId.Value))
                .Select(n => n.ParentNodeId!.Value).Distinct().ToList();
        }
        // Chapter ordinals: position among the chapter's siblings.
        var parents = nodes.Values.Where(n => n.ParentNodeId != null).Select(n => n.ParentNodeId!.Value).Distinct().ToList();
        var siblings = await db.Nodes.IgnoreQueryFilters().AsNoTracking().Where(n => n.ParentNodeId != null && parents.Contains(n.ParentNodeId.Value))
            .Select(n => new { n.Id, n.ParentNodeId, n.SortKey }).ToListAsync(ct);
        var ordinal = siblings.GroupBy(s => s.ParentNodeId).SelectMany(g => g.OrderBy(s => s.SortKey).Select((s, i) => (s.Id, I: i + 1)))
            .ToDictionary(x => x.Id, x => x.I);

        foreach (var l in links)
        {
            if (result.ContainsKey(l.BeatId) || !nodes.TryGetValue(l.NodeId, out var chapter)) continue;
            var book = chapter;
            while (book.Kind != "book" && book.ParentNodeId != null && nodes.TryGetValue(book.ParentNodeId.Value, out var up)) book = up;
            result[l.BeatId] = new Place(book.NodeCode ?? book.Slug, ordinal.GetValueOrDefault(chapter.Id), book.SortKey, chapter.SortKey, l.SortKey);
        }
        return result;
    }

    private static async Task<string> NamesAsync(ProseDbContext db, IReadOnlyCollection<Guid> ids, CancellationToken ct)
    {
        var names = await db.Set<Entity>().AsNoTracking().IgnoreQueryFilters().Where(e => ids.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, e => e.Name, ct);
        return string.Join(", ", ids.Select(id => $"{names.GetValueOrDefault(id, "?")} ({id})"));
    }

    public static IReadOnlyList<ContributionEdge> ParseEdges(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.EnumerateArray()
                .Select(e => new ContributionEdge(e.GetProperty("relation").GetString() ?? "",
                    Guid.TryParse(e.GetProperty("target").GetString(), out var g) ? g : Guid.Empty))
                .ToList();
        }
        catch (JsonException) { return []; }
    }
}
