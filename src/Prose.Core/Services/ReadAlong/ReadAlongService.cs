using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using static Prose.Core.Services.ReadAlong.StoryFactExtractor;

namespace Prose.Core.Services.ReadAlong;

/// <summary>
/// The read-along (RFC 0019): reads a book chapter by chapter, in order, the way a reader does, and
/// keeps the bookkeeping — story-bible drafts for the names and details it meets, facts with the
/// sentence that put them there, and a Continuity Inbox card whenever a new line disagrees with
/// one already on the page. It never writes prose.
///
/// <para>A chapter is read once it has been written: <see cref="ReadAlongBackgroundService"/> reads
/// chapters whose text changed and then sat still, so nothing nags the author mid-sentence.
/// <see cref="ReadBookAsync"/> reads a whole imported manuscript first page to last.</para>
/// </summary>
public sealed class ReadAlongService(
    IDbContextFactory<ProseDbContext> dbFactory,
    ReadAlongAllowance allowance,
    ILogger<ReadAlongService> log,
    SettingsKvStore? settings = null)
{
    /// <summary>Settings key for extractor patterns switched off after the precision audit
    /// (RA-T3.1.1.2): a list of pattern ids or families ("age.2", "kin").</summary>
    public const string DisabledPatternsKey = "readalong.patterns.disabled";

    private IReadOnlySet<string> DisabledPatterns() =>
        (settings?.Get<List<string>>(DisabledPatternsKey) ?? []).ToHashSet(StringComparer.Ordinal);

    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    // ── Book structure ──────────────────────────────────────────────────────────────────────

    /// <summary>One chapter in reading order: its beats as plain text, and the hash that says
    /// whether the read-along has already read it as it stands.</summary>
    public sealed record BookChapter(
        Guid ChapterNodeId, int Index, string Title, Guid? ParentNodeId, string? VolumeTitle,
        IReadOnlyList<Paragraph> Paragraphs, string ContentHash, int Words, DateTime LastEdited = default);

    /// <summary>The book's chapters in reading order. A chapter is the deepest chapter-type node
    /// over a beat (scenes and sequences fold into their chapter); beats sitting directly on the book
    /// form one unit named after the book.</summary>
    public async Task<List<BookChapter>> GetChaptersAsync(Guid bookId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var acc = new List<NodeWorkbenchService.OrderedBeat>();
        await NodeWorkbenchService.WalkAsync(db, bookId, acc, [], false, ct);

        var nodeIds = acc.Select(a => a.NodeId).Distinct().ToList();
        var nodes = new Dictionary<Guid, Node>();
        var frontier = nodeIds.ToList();
        while (frontier.Count > 0)
        {
            var batch = await db.Nodes.AsNoTracking().IgnoreQueryFilters().Where(n => frontier.Contains(n.Id)).ToListAsync(ct);
            foreach (var n in batch) nodes[n.Id] = n;
            frontier = batch.Where(n => n.ParentNodeId is { } p && !nodes.ContainsKey(p) && n.Id != bookId)
                            .Select(n => n.ParentNodeId!.Value).Distinct().ToList();
        }
        if (!nodes.ContainsKey(bookId))
            if (await db.Nodes.AsNoTracking().IgnoreQueryFilters().FirstOrDefaultAsync(n => n.Id == bookId, ct) is { } b) nodes[bookId] = b;

        Guid UnitOf(Guid leaf)
        {
            var cur = leaf;
            Guid? deepestChapter = null;
            var guard = 0;
            while (nodes.TryGetValue(cur, out var n) && guard++ < 32)
            {
                if (n is ChapterNode && deepestChapter is null) deepestChapter = n.Id;
                if (n.Id == bookId || n.ParentNodeId is null) break;
                cur = n.ParentNodeId.Value;
            }
            return deepestChapter ?? bookId;
        }

        var chapters = new List<BookChapter>();
        var order = new List<Guid>();
        var groups = new Dictionary<Guid, List<NodeWorkbenchService.OrderedBeat>>();
        foreach (var a in acc)
        {
            var unit = UnitOf(a.NodeId);
            if (!groups.TryGetValue(unit, out var list)) { groups[unit] = list = []; order.Add(unit); }
            list.Add(a);
        }

        var i = 0;
        foreach (var unit in order)
        {
            var beats = groups[unit];
            var paragraphs = beats
                .Where(b => b.Beat.Kind != ManuscriptParser.SceneBreakKind)
                .Select(b => new Paragraph(b.Beat.Id, PlainText(b.Beat.Text)))
                .Where(p => p.Text.Length > 0)
                .ToList();
            var node = nodes.GetValueOrDefault(unit);
            var parent = node?.ParentNodeId is { } pid && pid != bookId ? nodes.GetValueOrDefault(pid) : null;
            chapters.Add(new BookChapter(
                unit, ++i, node?.Title ?? "(untitled)", node?.ParentNodeId,
                parent is ChapterNode ? parent.Title : null,
                paragraphs,
                HashOf(beats.Select(b => b.Beat.TextHash ?? Beat.ComputeHash(b.Beat.Text))),
                paragraphs.Sum(p => ManuscriptParser.CountWords(p.Text)),
                beats.Count == 0 ? default : beats.Max(b => b.Beat.UpdatedAt)));
        }
        return chapters;
    }

    public static string PlainText(string? raw) =>
        ProseInline.StripFormatting(BeatMarkup.StripEntityTags(raw ?? "")).Trim();

    private static string HashOf(IEnumerable<string> parts)
    {
        var joined = string.Join("|", parts);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(joined))).ToLowerInvariant();
    }

    // ── Reading ─────────────────────────────────────────────────────────────────────────────

    /// <summary>What reading one chapter found: the reading-log line.</summary>
    public sealed record ChapterReadResult(
        Guid ChapterNodeId, int Index, string Title, bool Skipped, string? Reason,
        IReadOnlyList<string> Met, int Details, int Contradictions, int Words);

    /// <summary>Progress while a whole book is read: "Reading your story — 13 of 20 chapters".</summary>
    public sealed record ReadProgress(int Done, int Total, string? Current, ChapterReadResult? Last, IReadOnlyList<string> RecentlyMet);

    /// <summary>Read every chapter of a book in order. Chapters already read as they stand are
    /// skipped unless <paramref name="force"/>. Stops (without failing) when the reading allowance
    /// is used up; everything already read stays.</summary>
    public async Task<List<ChapterReadResult>> ReadBookAsync(
        Guid bookId, bool force = false, IProgress<ReadProgress>? progress = null, CancellationToken ct = default)
    {
        await ScopeAsync(bookId, ct);
        var chapters = await GetChaptersAsync(bookId, ct);
        var results = new List<ChapterReadResult>();
        var recentlyMet = new List<string>();
        // RA-T4.3.1.2: the known-entity index (every canon entity of the universe, plus drafts) is loaded
        // once for the whole book and grown with each chapter's new drafts, not reloaded per chapter.
        Known known;
        await using (var db0 = await dbFactory.CreateDbContextAsync(ct))
        {
            var universeId = await db0.Nodes.IgnoreQueryFilters().Where(n => n.Id == bookId).Select(n => n.UniverseId).FirstAsync(ct);
            known = await LoadKnownAsync(db0, bookId, universeId, ct);
        }
        for (var i = 0; i < chapters.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            progress?.Report(new ReadProgress(i, chapters.Count, chapters[i].Title, results.LastOrDefault(), recentlyMet.TakeLast(6).ToList()));
            var r = await ReadChapterCoreAsync(bookId, chapters[i], force, ct, known);
            results.Add(r);
            recentlyMet.AddRange(r.Met);
            if (r.Reason == "allowance") break;
        }
        progress?.Report(new ReadProgress(results.Count, chapters.Count, null, results.LastOrDefault(), recentlyMet.TakeLast(6).ToList()));
        return results;
    }

    /// <summary>Read one chapter (by its node id) against everything the book has said so far.</summary>
    public async Task<ChapterReadResult> ReadChapterAsync(Guid bookId, Guid chapterNodeId, bool force = false, CancellationToken ct = default)
    {
        await ScopeAsync(bookId, ct);
        var chapters = await GetChaptersAsync(bookId, ct);
        var chapter = chapters.FirstOrDefault(c => c.ChapterNodeId == chapterNodeId)
            ?? throw new InvalidOperationException($"Chapter {chapterNodeId} is not part of book {bookId}.");
        return await ReadChapterCoreAsync(bookId, chapter, force, ct);
    }

    /// <summary>Chapters whose text changed since they were last read (or were never read).</summary>
    public async Task<List<BookChapter>> UnreadChaptersAsync(Guid bookId, CancellationToken ct = default)
    {
        var chapters = await GetChaptersAsync(bookId, ct);
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var read = await db.ChapterReadings.AsNoTracking().IgnoreQueryFilters()
            .Where(r => r.BookNodeId == bookId && r.Status == ReadAlongStatus.Done)
            .Select(r => new { r.ChapterNodeId, r.ContentHash }).ToListAsync(ct);
        var hashes = read.GroupBy(r => r.ChapterNodeId).ToDictionary(g => g.Key, g => g.Select(x => x.ContentHash).ToHashSet());
        return chapters.Where(c => c.Paragraphs.Count > 0 && !(hashes.TryGetValue(c.ChapterNodeId, out var h) && h.Contains(c.ContentHash))).ToList();
    }

    private async Task<ChapterReadResult> ReadChapterCoreAsync(Guid bookId, BookChapter chapter, bool force, CancellationToken ct, Known? preloaded = null)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var universeId = await db.Nodes.IgnoreQueryFilters().Where(n => n.Id == bookId).Select(n => n.UniverseId).FirstAsync(ct);

        if (!force && await db.ChapterReadings.IgnoreQueryFilters().AnyAsync(r =>
                r.BookNodeId == bookId && r.ChapterNodeId == chapter.ChapterNodeId && r.ContentHash == chapter.ContentHash && r.Status == ReadAlongStatus.Done, ct))
            return new ChapterReadResult(chapter.ChapterNodeId, chapter.Index, chapter.Title, true, "already read as it stands", [], 0, 0, chapter.Words);

        if (chapter.Paragraphs.Count == 0)
            return new ChapterReadResult(chapter.ChapterNodeId, chapter.Index, chapter.Title, true, "empty", [], 0, 0, 0);

        if (!await allowance.CanReadAsync(chapter.Words, ct))
            return new ChapterReadResult(chapter.ChapterNodeId, chapter.Index, chapter.Title, true, "allowance", [], 0, 0, chapter.Words);

        // ── What the bible already holds ─────────────────────────────────────────────────
        var known = preloaded ?? await LoadKnownAsync(db, bookId, universeId, ct);
        var tossed = (await db.StoryBibleSuggestions.IgnoreQueryFilters()
                .Where(s => s.BookNodeId == bookId && s.Status == ReadAlongStatus.Tossed && s.Kind == "entry")
                .Select(s => s.Name).ToListAsync(ct))
            .Select(Key).ToHashSet();
        var tossedDetails = (await db.StoryBibleSuggestions.IgnoreQueryFilters()
                .Where(s => s.BookNodeId == bookId && s.Status == ReadAlongStatus.Tossed && s.Kind == "detail")
                .Select(s => s.Fingerprint).ToListAsync(ct)).ToHashSet();

        var result = Extract(chapter.Paragraphs, known.Entries, tossed, DisabledPatterns());

        // Earlier facts about these subjects: this book's, and the same entities' in any book.
        var subjectKeys = result.Facts.Select(f => f.SubjectKey).Distinct().ToList();
        var entityIds = result.Facts.Where(f => f.EntityId is not null).Select(f => f.EntityId!.Value).Distinct().ToList();
        var prior = await db.StoryFacts.IgnoreQueryFilters()
            .Where(f => f.SupersededAt == null && (
                (f.BookNodeId == bookId && subjectKeys.Contains(f.SubjectKey)) ||
                (f.EntityId != null && entityIds.Contains(f.EntityId.Value))))
            .ToListAsync(ct);

        // A re-read replaces what this chapter said before: unconfirmed observations from this
        // chapter that are no longer on the page go; the author's confirmed facts always stay.
        var stale = prior.Where(f => f.ChapterNodeId == chapter.ChapterNodeId && !f.Confirmed).ToList();
        db.StoryFacts.RemoveRange(stale);
        prior = prior.Except(stale).ToList();

        var pendingEntries = await db.StoryBibleSuggestions.IgnoreQueryFilters()
            .Where(s => s.BookNodeId == bookId && s.Kind == "entry" && s.Status == ReadAlongStatus.Pending)
            .ToListAsync(ct);
        var entryByKey = pendingEntries.GroupBy(s => Key(s.Name)).ToDictionary(g => g.Key, g => g.First());
        // A card whose quoted line is no longer in this chapter was answered by an edit: the author
        // fixed the prose. It closes itself (status "fixed") rather than asking about words that are gone.
        var chapterText = string.Join("\n", chapter.Paragraphs.Select(p => p.Text));
        foreach (var card in await db.ContinuityConflicts.IgnoreQueryFilters()
                     .Where(c => c.BookNodeId == bookId && c.Status == ReadAlongStatus.Open &&
                                 (c.ProposedChapterNodeId == chapter.ChapterNodeId || c.ExistingChapterNodeId == chapter.ChapterNodeId))
                     .ToListAsync(ct))
        {
            var gone = (card.ProposedChapterNodeId == chapter.ChapterNodeId && !chapterText.Contains(card.ProposedQuote, StringComparison.Ordinal))
                    || (card.ExistingChapterNodeId == chapter.ChapterNodeId && !chapterText.Contains(card.ExistingQuote, StringComparison.Ordinal));
            if (!gone) continue;
            card.Status = ReadAlongStatus.Fixed;
            card.ResolvedAt = DateTime.UtcNow;
            card.Note = "One of the two lines was rewritten in the prose.";
        }

        var existingConflicts = (await db.ContinuityConflicts.IgnoreQueryFilters()
            .Where(c => c.BookNodeId == bookId).Select(c => c.Fingerprint).ToListAsync(ct)).ToHashSet();
        var titles = await ChapterTitlesAsync(db, prior.Where(p => p.ChapterNodeId != null).Select(p => p.ChapterNodeId!.Value), ct);

        var met = new List<string>();
        var details = 0;
        var contradictions = 0;

        StoryBibleSuggestion EnsureEntry(string name, string key, string category, string quote, Guid beatId)
        {
            if (entryByKey.TryGetValue(key, out var s)) return s;
            s = new StoryBibleSuggestion
            {
                UniverseId = universeId, BookNodeId = bookId, Kind = "entry", Category = category, Name = name,
                Quote = Trim(quote, 2000), BeatId = beatId, ChapterNodeId = chapter.ChapterNodeId,
                Fingerprint = $"entry|{key}", Status = ReadAlongStatus.Pending,
            };
            db.StoryBibleSuggestions.Add(s);
            entryByKey[key] = s;
            met.Add(name);
            known.Entries.Add(new KnownEntry(null, name, category, []));
            return s;
        }

        // New names → draft entries.
        foreach (var g in result.Names.Where(n => n.IsNew).GroupBy(n => n.Key))
        {
            var first = g.First();
            EnsureEntry(first.Name, first.Key, first.Category, first.Quote, first.BeatId);
        }

        // Details → observations, drafts and continuity cards.
        foreach (var f in result.Facts.OrderBy(f => f.Evidence))
        {
            var sameValue = prior.Any(p => SameSubject(p, f) && p.Attribute == f.Attribute
                                         && string.Equals(p.Value, f.Value, StringComparison.OrdinalIgnoreCase));
            if (sameValue) continue;

            if (!f.Multi)
            {
                var current = prior.Where(p => SameSubject(p, f) && p.Attribute == f.Attribute)
                    .OrderByDescending(p => p.Confirmed).ThenByDescending(p => p.ChapterIndex).ThenByDescending(p => p.CreatedAt)
                    .FirstOrDefault();
                if (current is not null)
                {
                    var later = current.ChapterIndex <= chapter.Index;
                    var (confidence, kind) = Compare(f.Attribute, current.Value, f.Value, f.Confidence, later);
                    if (confidence > 0)
                    {
                        if (current.Confirmed) confidence = Math.Min(99, confidence + 10);
                        var fp = $"{f.SubjectKey}|{f.Attribute}|{current.Value}|{f.Value}".ToLowerInvariant();
                        if (fp.Length > 400) fp = fp[..400];
                        if (existingConflicts.Add(fp))
                        {
                            db.ContinuityConflicts.Add(new ContinuityConflict
                            {
                                UniverseId = universeId, BookNodeId = bookId,
                                EntityId = current.EntityId ?? f.EntityId,
                                EntityName = Trim(f.SubjectName, 200), Kind = kind,
                                Category = f.ContinuityCategory, Attribute = Trim(f.Attribute, 80), Confidence = confidence,
                                ExistingFactId = current.Id, ExistingValue = Trim(current.Value, 400), ExistingQuote = current.Quote,
                                ExistingBeatId = current.BeatId, ExistingChapterNodeId = current.ChapterNodeId,
                                ExistingChapterTitle = Trim(current.ChapterNodeId is { } ec ? titles.GetValueOrDefault(ec, chapter.Title) : chapter.Title, 400),
                                ProposedValue = Trim(f.Value, 400), ProposedQuote = f.Quote, ProposedBeatId = f.BeatId,
                                ProposedChapterNodeId = chapter.ChapterNodeId, ProposedChapterTitle = Trim(chapter.Title, 400),
                                Fingerprint = fp,
                            });
                            contradictions++;
                        }
                        if (f.Evidence) continue;
                        // The disagreeing line is kept as an observation (the story said it), so the
                        // card can be ruled on; it is not drafted into the bible — the card is the question.
                        var obs = NewFact(f, universeId, bookId, chapter, null);
                        db.StoryFacts.Add(obs);
                        prior.Add(obs);
                        details++;
                        continue;
                    }
                }
            }
            if (f.Evidence) continue;

            // A detail the bible does not have yet.
            Guid? suggestionId;
            if (f.EntityId is { } eid)
            {
                var fp = $"detail|{f.SubjectKey}|{f.Attribute}|{f.Value}".ToLowerInvariant();
                if (fp.Length > 400) fp = fp[..400];
                if (tossedDetails.Contains(fp)) continue;
                var s = new StoryBibleSuggestion
                {
                    UniverseId = universeId, BookNodeId = bookId, Kind = "detail", Category = f.SubjectCategory,
                    Name = Trim(f.SubjectName, 200), EntityId = eid, Quote = f.Quote, BeatId = f.BeatId, ChapterNodeId = chapter.ChapterNodeId,
                    AttributesJson = JsonSerializer.Serialize(new Dictionary<string, AttributeValue> { [f.Attribute] = new(f.Value, f.Quote, f.BeatId) }, Json),
                    Fingerprint = fp,
                };
                db.StoryBibleSuggestions.Add(s);
                suggestionId = s.Id;
            }
            else
            {
                var entry = EnsureEntry(f.SubjectName, f.SubjectKey, f.SubjectCategory, f.Quote, f.BeatId);
                var attrs = JsonSerializer.Deserialize<Dictionary<string, AttributeValue>>(entry.AttributesJson, Json) ?? [];
                attrs.TryAdd(f.Multi && attrs.ContainsKey(f.Attribute) ? $"{f.Attribute} ({attrs.Count + 1})" : f.Attribute, new(f.Value, f.Quote, f.BeatId));
                entry.AttributesJson = JsonSerializer.Serialize(attrs, Json);
                suggestionId = entry.Id;
            }

            var fact = NewFact(f, universeId, bookId, chapter, suggestionId);
            db.StoryFacts.Add(fact);
            prior.Add(fact);
            details++;
        }

        // Relations → the atlas's typed threads (as facts on the "from" subject).
        foreach (var r in result.Relations)
        {
            if (prior.Any(p => p.SubjectKey == r.FromKey && p.Attribute == r.Type && Key(p.Value) == r.ToKey)) continue;
            var subject = known.Entries.FirstOrDefault(k => Key(k.Name) == r.FromKey);
            var f = new FactHit(r.FromName, r.FromKey, subject?.EntityId, subject?.Category ?? CatCharacter, r.Type, r.ToName, r.Quote, r.BeatId, 70, "character", true);
            var suggestionId = subject?.EntityId is null ? EnsureEntry(r.FromName, r.FromKey, CatCharacter, r.Quote, r.BeatId).Id : (Guid?)null;
            var fact = NewFact(f, universeId, bookId, chapter, suggestionId);
            fact.Confirmed = false;
            db.StoryFacts.Add(fact);
            prior.Add(fact);
        }

        // The reading log line, and the chapter's mentions for the atlas.
        var mentions = result.Names.GroupBy(n => n.Key)
            .Select(g => new Mention(g.Key, g.First().Name, g.First().Category, g.First().EntityId, g.Count()))
            .ToList();
        var pairs = result.Names.GroupBy(n => n.BeatId)
            .SelectMany(g =>
            {
                var keys = g.Select(n => n.Key).Distinct().OrderBy(k => k, StringComparer.Ordinal).ToList();
                return from a in Enumerable.Range(0, keys.Count) from b in Enumerable.Range(a + 1, keys.Count - a - 1) select (keys[a], keys[b]);
            })
            .GroupBy(p => p).Select(g => new[] { g.Key.Item1, g.Key.Item2, g.Count().ToString() }).ToList();

        db.ChapterReadings.RemoveRange(await db.ChapterReadings.IgnoreQueryFilters()
            .Where(r => r.BookNodeId == bookId && r.ChapterNodeId == chapter.ChapterNodeId).ToListAsync(ct));
        db.ChapterReadings.Add(new ChapterReading
        {
            UniverseId = universeId, BookNodeId = bookId, ChapterNodeId = chapter.ChapterNodeId, ChapterIndex = chapter.Index,
            ChapterTitle = Trim(chapter.Title, 400), ContentHash = chapter.ContentHash, Words = chapter.Words,
            MetJson = JsonSerializer.Serialize(new ReadingDetail(met.Distinct().ToList(), mentions, pairs), Json),
            DetailCount = details, ContradictionCount = contradictions, Status = ReadAlongStatus.Done,
        });

        await db.SaveChangesAsync(ct);
        log.LogInformation("Read-along: {Book} ch {Index} '{Title}' — met {Met}, {Details} details, {Contradictions} possible contradictions",
            bookId, chapter.Index, chapter.Title, met.Count, details, contradictions);
        return new ChapterReadResult(chapter.ChapterNodeId, chapter.Index, chapter.Title, false, null, met.Distinct().ToList(), details, contradictions, chapter.Words);
    }

    private static bool SameSubject(StoryFact p, FactHit f) =>
        (f.EntityId is { } id && p.EntityId == id) || p.SubjectKey == f.SubjectKey;

    private static StoryFact NewFact(FactHit f, Guid universeId, Guid bookId, BookChapter chapter, Guid? suggestionId) => new()
    {
        UniverseId = universeId, BookNodeId = bookId, EntityId = f.EntityId, SubjectKey = Trim(f.SubjectKey, 200),
        SubjectName = Trim(f.SubjectName, 200), SuggestionId = suggestionId, Category = f.ContinuityCategory,
        Attribute = Trim(f.Attribute, 80), Value = Trim(f.Value, 400), Quote = f.Quote, BeatId = f.BeatId,
        ChapterNodeId = chapter.ChapterNodeId, ChapterIndex = chapter.Index, Source = "read-along", Confirmed = false,
    };

    /// <summary>One attribute of a draft: the value and the sentence it came from.</summary>
    public sealed record AttributeValue(string Value, string Quote, Guid? BeatId);

    /// <summary>One name in a chapter's reading, for the atlas.</summary>
    public sealed record Mention(string Key, string Name, string Category, Guid? EntityId, int Count);

    /// <summary>What a reading stores beyond its counts.</summary>
    public sealed record ReadingDetail(List<string> Met, List<Mention> Mentions, List<string[]> Pairs);

    private sealed record Known(List<KnownEntry> Entries);

    /// <summary>The entries the bible already holds for this book: kept entities (this book's facts,
    /// entities made from this book, entities its beats are tagged with) plus pending drafts, and
    /// every non-archived entity of the universe so imported prose meets canon it already has.</summary>
    private static async Task<Known> LoadKnownAsync(ProseDbContext db, Guid bookId, Guid universeId, CancellationToken ct)
    {
        var entities = await db.Entities.AsNoTracking().IgnoreQueryFilters()
            .Where(e => e.UniverseId == universeId && e.Status != "archived" && !e.Name.StartsWith("("))
            .OrderByDescending(e => e.OriginNodeId == bookId)
            .Select(e => new { e.Id, e.Name, e.EntityType })
            .Take(20000)
            .ToListAsync(ct);
        var aliases = (await db.StoryFacts.AsNoTracking().IgnoreQueryFilters()
                .Where(f => f.UniverseId == universeId && f.EntityId != null && f.Attribute == "alias" && f.Confirmed && f.SupersededAt == null)
                .Select(f => new { f.EntityId, f.Value }).ToListAsync(ct))
            .GroupBy(a => a.EntityId!.Value).ToDictionary(g => g.Key, g => g.Select(x => x.Value).ToList());

        var genders = (await db.StoryFacts.AsNoTracking().IgnoreQueryFilters()
                .Where(f => f.UniverseId == universeId && f.EntityId != null && (f.Attribute == "gender" || f.Attribute == "pronouns") && f.Confirmed && f.SupersededAt == null)
                .Select(f => new { f.EntityId, f.Value }).ToListAsync(ct))
            .GroupBy(f => f.EntityId!.Value).ToDictionary(g => g.Key, g => GenderOf(g.Last().Value));
        var list = entities
            .Select(e => new KnownEntry(e.Id, e.Name, CategoryOf(e.EntityType), aliases.GetValueOrDefault(e.Id) ?? [], genders.GetValueOrDefault(e.Id)))
            .ToList();
        var pending = await db.StoryBibleSuggestions.AsNoTracking().IgnoreQueryFilters()
            .Where(s => s.BookNodeId == bookId && s.Kind == "entry" && s.Status == ReadAlongStatus.Pending)
            .Select(s => new { s.Name, s.Category }).ToListAsync(ct);
        list.AddRange(pending.Select(p => new KnownEntry(null, p.Name, p.Category, [])));
        return new Known(list);
    }

    /// <summary>The story-bible category of an engine entity type.</summary>
    public static string CategoryOf(string entityType) => (entityType ?? "").ToLowerInvariant() switch
    {
        "character" or "person" or "species" => CatCharacter,
        "place" or "district" or "location" or "region" or "city" or "building" or "landmark" => CatPlace,
        "faction" or "corponation" or "subsidiary" or "organization" or "gang" or "institution" => CatOrganization,
        "weapon" or "equipment" or "cyberware" or "apparel" or "ammunition" or "gear" or "item" or "vehicle"
            or "transportation" or "consumer_good" or "pharmaceutical" or "material" or "automaton" or "document" => CatItem,
        _ => CatConcept,
    };

    /// <summary>The engine entity type a kept story-bible entry of this category becomes.</summary>
    public static string EntityTypeOf(string category) => category switch
    {
        CatCharacter => "character",
        CatPlace => "place",
        CatOrganization => "faction",
        CatItem => "equipment",
        _ => "concept",
    };

    private static async Task<Dictionary<Guid, string>> ChapterTitlesAsync(ProseDbContext db, IEnumerable<Guid> ids, CancellationToken ct)
    {
        var list = ids.Distinct().ToList();
        if (list.Count == 0) return [];
        return await db.Nodes.AsNoTracking().IgnoreQueryFilters().Where(n => list.Contains(n.Id))
            .ToDictionaryAsync(n => n.Id, n => n.Title, ct);
    }

    private async Task ScopeAsync(Guid bookId, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        if (!await db.Nodes.IgnoreQueryFilters().AnyAsync(n => n.Id == bookId, ct))
            throw new InvalidOperationException($"Book {bookId} not found.");
    }

    internal static string Trim(string s, int max) => s.Length <= max ? s : s[..max];

    // ── Status: the reading bar, the reading log, "remembered this chapter" ──────────────────

    public sealed record ReadingLogLine(Guid ChapterNodeId, int Index, string Title, IReadOnlyList<string> Met, int Details, int Contradictions, int Words, DateTime ReadAt, bool Current);

    public sealed record BookReadingStatus(
        Guid BookId, int ChaptersTotal, int ChaptersRead, int ChaptersStale, int WordsRead,
        int PendingSuggestions, int OpenConflicts, int Entries, IReadOnlyList<ReadingLogLine> Log, IReadOnlyList<string> RecentlyMet);

    public async Task<BookReadingStatus> GetStatusAsync(Guid bookId, CancellationToken ct = default)
    {
        var chapters = await GetChaptersAsync(bookId, ct);
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var readings = await db.ChapterReadings.AsNoTracking().IgnoreQueryFilters()
            .Where(r => r.BookNodeId == bookId && r.Status == ReadAlongStatus.Done).ToListAsync(ct);
        var current = chapters.ToDictionary(c => c.ChapterNodeId, c => c.ContentHash);
        var log = readings
            .Select(r =>
            {
                var d = Detail(r.MetJson);
                var index = chapters.FirstOrDefault(c => c.ChapterNodeId == r.ChapterNodeId)?.Index ?? r.ChapterIndex;
                return new ReadingLogLine(r.ChapterNodeId, index, r.ChapterTitle, d.Met, r.DetailCount, r.ContradictionCount, r.Words, r.ReadAt,
                    current.TryGetValue(r.ChapterNodeId, out var h) && h == r.ContentHash);
            })
            .OrderBy(l => l.Index).ToList();
        var pending = await db.StoryBibleSuggestions.IgnoreQueryFilters().CountAsync(s => s.BookNodeId == bookId && s.Status == ReadAlongStatus.Pending, ct);
        var open = await db.ContinuityConflicts.IgnoreQueryFilters().CountAsync(c => c.BookNodeId == bookId && c.Status == ReadAlongStatus.Open, ct);
        var entries = await db.StoryFacts.IgnoreQueryFilters().Where(f => f.BookNodeId == bookId && f.EntityId != null && f.Confirmed)
            .Select(f => f.EntityId).Distinct().CountAsync(ct);
        var withText = chapters.Count(c => c.Paragraphs.Count > 0);
        var recent = log.OrderByDescending(l => l.ReadAt).SelectMany(l => l.Met).Take(6).ToList();
        return new BookReadingStatus(bookId, withText, log.Count(l => l.Current), log.Count(l => !l.Current), log.Where(l => l.Current).Sum(l => l.Words),
            pending, open, entries, log, recent);
    }

    /// <summary>Whether the read-along has read this chapter as it stands right now — the editor's
    /// "remembered this chapter" tick.</summary>
    /// <para>RA-T4.3.1.3: hashes only this chapter (its own subtree, minus any nested chapters, which are
    /// their own units), so the Writer can ask on every beat it opens without walking the whole book.</para>
    public async Task<bool> IsRememberedAsync(Guid bookId, Guid chapterNodeId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var reading = await db.ChapterReadings.AsNoTracking().IgnoreQueryFilters()
            .Where(r => r.ChapterNodeId == chapterNodeId && r.BookNodeId == bookId && r.Status == ReadAlongStatus.Done)
            .Select(r => r.ContentHash).ToListAsync(ct);
        if (reading.Count == 0) return false;
        var hash = chapterNodeId == bookId
            ? (await GetChaptersAsync(bookId, ct)).FirstOrDefault(c => c.ChapterNodeId == chapterNodeId)?.ContentHash
            : await ChapterHashAsync(db, chapterNodeId, ct);
        return hash is not null && reading.Contains(hash);
    }

    /// <summary>The content hash of one chapter unit, computed from its own subtree: the same value
    /// <see cref="GetChaptersAsync"/> gives it.</summary>
    private static async Task<string?> ChapterHashAsync(ProseDbContext db, Guid chapterNodeId, CancellationToken ct)
    {
        var acc = new List<NodeWorkbenchService.OrderedBeat>();
        await NodeWorkbenchService.WalkAsync(db, chapterNodeId, acc, [], false, ct);
        var leafIds = acc.Select(a => a.NodeId).Where(id => id != chapterNodeId).Distinct().ToList();
        // A beat under a deeper chapter belongs to that chapter's unit, not this one.
        var nodes = new Dictionary<Guid, Node>();
        var frontier = leafIds;
        while (frontier.Count > 0)
        {
            var batch = await db.Nodes.AsNoTracking().IgnoreQueryFilters().Where(n => frontier.Contains(n.Id)).ToListAsync(ct);
            foreach (var n in batch) nodes[n.Id] = n;
            frontier = batch.Where(n => n.ParentNodeId is { } p && p != chapterNodeId && !nodes.ContainsKey(p))
                            .Select(n => n.ParentNodeId!.Value).Distinct().ToList();
        }
        bool Mine(Guid leaf)
        {
            for (var cur = leaf; cur != chapterNodeId && nodes.TryGetValue(cur, out var n); cur = n.ParentNodeId ?? chapterNodeId)
                if (n is ChapterNode) return false;
            return true;
        }
        var beats = acc.Where(a => a.NodeId == chapterNodeId || Mine(a.NodeId)).ToList();
        return beats.Count == 0 ? null : HashOf(beats.Select(b => b.Beat.TextHash ?? Beat.ComputeHash(b.Beat.Text)));
    }

    public static ReadingDetail Detail(string json)
    {
        try { return JsonSerializer.Deserialize<ReadingDetail>(json, Json) ?? new([], [], []); }
        catch (JsonException) { return new([], [], []); }
    }
}
