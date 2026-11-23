using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using static Prose.Core.Services.ReadAlong.StoryFactExtractor;

namespace Prose.Core.Services.ReadAlong;

/// <summary>
/// The automatic story bible (RFC 0019 §4): the codex a book grows from its own prose. Drafts wait
/// for <c>Keep it</c> or <c>Not quite</c>; nothing a draft says reaches an entry, the atlas or an
/// answer until the author keeps it. Every kept detail carries its receipt — the sentence and
/// chapter that put it there — and a confirmed detail is never quietly overwritten by a later
/// chapter: a disagreeing line goes to the Continuity Inbox instead.
/// </summary>
public sealed class StoryBibleService(IDbContextFactory<ProseDbContext> dbFactory, ILogger<StoryBibleService> log)
{
    // ── Shapes ──────────────────────────────────────────────────────────────────────────────

    public sealed record CodexItem(
        Guid Id, bool IsDraft, string Name, string Category, int Facts, int PendingDetails, int FirstChapter);

    public sealed record CodexCategory(string Category, IReadOnlyList<CodexItem> Items);

    public sealed record Receipt(Guid FactId, string Attribute, string Value, string Quote, Guid? BeatId, Guid? ChapterNodeId,
        string ChapterTitle, int ChapterIndex, string Source, bool Confirmed, DateTime? SupersededAt);

    public sealed record DraftAttribute(string Key, string Value, string Quote, Guid? BeatId);

    public sealed record Draft(Guid Id, string Kind, string Name, string Category, Guid? EntityId, string Quote,
        Guid? BeatId, Guid? ChapterNodeId, string ChapterTitle, IReadOnlyList<DraftAttribute> Attributes, DateTime CreatedAt);

    public sealed record Link(Guid? EntityId, string Name, string Type, string Quote, Guid? BeatId, string ChapterTitle);

    public sealed record Entry(
        Guid EntityId, string Name, string Category, string EntityType, string? Description, string? Group,
        IReadOnlyList<string> Aliases, IReadOnlyList<Receipt> Details, IReadOnlyList<Receipt> Timeline,
        IReadOnlyList<Link> Links, IReadOnlyList<Draft> PendingDetails, int Mentions);

    // ── The codex ───────────────────────────────────────────────────────────────────────────

    /// <summary>Every entry the book's bible holds, by category, drafts included (marked).</summary>
    public async Task<List<CodexCategory>> GetCodexAsync(Guid bookId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var ids = await BookEntityIdsAsync(db, bookId, ct);
        var entities = await db.Entities.AsNoTracking().IgnoreQueryFilters()
            .Where(e => ids.Contains(e.Id) && e.Status != "archived")
            .Select(e => new { e.Id, e.Name, e.EntityType }).ToListAsync(ct);
        var factCounts = await db.StoryFacts.AsNoTracking().IgnoreQueryFilters()
            .Where(f => f.BookNodeId == bookId && f.EntityId != null && f.Confirmed && f.SupersededAt == null)
            .GroupBy(f => f.EntityId!.Value).Select(g => new { Id = g.Key, N = g.Count(), First = g.Min(x => x.ChapterIndex) })
            .ToDictionaryAsync(x => x.Id, ct);
        var pendingDetails = await db.StoryBibleSuggestions.AsNoTracking().IgnoreQueryFilters()
            .Where(s => s.BookNodeId == bookId && s.Kind == "detail" && s.Status == ReadAlongStatus.Pending && s.EntityId != null)
            .GroupBy(s => s.EntityId!.Value).Select(g => new { Id = g.Key, N = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.N, ct);
        var firstSeen = await FirstSeenAsync(db, bookId, ct);

        var items = entities.Select(e => new CodexItem(e.Id, false, e.Name, ReadAlongService.CategoryOf(e.EntityType),
                factCounts.TryGetValue(e.Id, out var fc) ? fc.N : 0, pendingDetails.GetValueOrDefault(e.Id),
                firstSeen.TryGetValue(Key(e.Name), out var fs) ? fs : fc?.First ?? 0))
            .ToList();

        var drafts = await db.StoryBibleSuggestions.AsNoTracking().IgnoreQueryFilters()
            .Where(s => s.BookNodeId == bookId && s.Kind == "entry" && s.Status == ReadAlongStatus.Pending)
            .ToListAsync(ct);
        items.AddRange(drafts.Select(d => new CodexItem(d.Id, true, d.Name, d.Category, Attributes(d.AttributesJson).Count, 0,
            firstSeen.GetValueOrDefault(Key(d.Name)))));

        return Categories
            .Select(c => new CodexCategory(c, items.Where(i => i.Category == c)
                .OrderBy(i => i.IsDraft).ThenBy(i => i.Name, StringComparer.OrdinalIgnoreCase).ToList()))
            .Where(c => c.Items.Count > 0)
            .ToList();
    }

    /// <summary>Every name the manuscript view highlights: kept entries and their confirmed aliases
    /// (linked to the entry), and drafts (unlinked, marked).</summary>
    public async Task<List<(string Name, Guid? EntityId, bool Draft)>> GetHighlightNamesAsync(Guid bookId, CancellationToken ct = default)
    {
        var codex = await GetCodexAsync(bookId, ct);
        var list = codex.SelectMany(c => c.Items).Select(i => (i.Name, i.IsDraft ? (Guid?)null : i.Id, i.IsDraft)).ToList();
        var kept = codex.SelectMany(c => c.Items).Where(i => !i.IsDraft).Select(i => i.Id).ToList();
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var aliases = await db.StoryFacts.AsNoTracking().IgnoreQueryFilters()
            .Where(f => f.EntityId != null && kept.Contains(f.EntityId.Value) && f.Attribute == "alias" && f.Confirmed && f.SupersededAt == null)
            .Select(f => new { f.Value, f.EntityId }).ToListAsync(ct);
        list.AddRange(aliases.Select(a => (a.Value, a.EntityId, false)));
        return list;
    }

    /// <summary>The entities that belong to this book's bible: kept from its prose, made from it,
    /// tagged in its beats, or met by the read-along in its chapters.</summary>
    internal static async Task<HashSet<Guid>> BookEntityIdsAsync(ProseDbContext db, Guid bookId, CancellationToken ct)
    {
        var ids = (await db.StoryFacts.AsNoTracking().IgnoreQueryFilters()
            .Where(f => f.BookNodeId == bookId && f.EntityId != null).Select(f => f.EntityId!.Value).Distinct().ToListAsync(ct)).ToHashSet();
        ids.UnionWith(await db.Entities.AsNoTracking().IgnoreQueryFilters().Where(e => e.OriginNodeId == bookId).Select(e => e.Id).ToListAsync(ct));
        foreach (var json in await db.ChapterReadings.AsNoTracking().IgnoreQueryFilters()
                     .Where(r => r.BookNodeId == bookId).Select(r => r.MetJson).ToListAsync(ct))
            foreach (var m in ReadAlongService.Detail(json).Mentions)
                if (m.EntityId is { } id) ids.Add(id);
        return ids;
    }

    /// <summary>Reading-order index of the chapter where each subject key first appears.</summary>
    internal static async Task<Dictionary<string, int>> FirstSeenAsync(ProseDbContext db, Guid bookId, CancellationToken ct)
    {
        var first = new Dictionary<string, int>(StringComparer.Ordinal);
        var readings = await db.ChapterReadings.AsNoTracking().IgnoreQueryFilters()
            .Where(r => r.BookNodeId == bookId).OrderBy(r => r.ChapterIndex).Select(r => new { r.ChapterIndex, r.MetJson }).ToListAsync(ct);
        foreach (var r in readings)
            foreach (var m in ReadAlongService.Detail(r.MetJson).Mentions)
                first.TryAdd(m.Key, r.ChapterIndex);
        return first;
    }

    // ── Drafts ──────────────────────────────────────────────────────────────────────────────

    public async Task<List<Draft>> GetDraftsAsync(Guid bookId, string? kind = null, int take = 200, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var rows = await db.StoryBibleSuggestions.AsNoTracking().IgnoreQueryFilters()
            .Where(s => s.BookNodeId == bookId && s.Status == ReadAlongStatus.Pending && (kind == null || s.Kind == kind))
            .OrderBy(s => s.CreatedAt).Take(take).ToListAsync(ct);
        var titles = await TitlesAsync(db, rows.Where(r => r.ChapterNodeId != null).Select(r => r.ChapterNodeId!.Value), ct);
        return rows.Select(r => ToDraft(r, titles)).ToList();
    }

    private static Draft ToDraft(StoryBibleSuggestion r, Dictionary<Guid, string> titles) =>
        new(r.Id, r.Kind, r.Name, r.Category, r.EntityId, r.Quote, r.BeatId, r.ChapterNodeId,
            r.ChapterNodeId is { } c ? titles.GetValueOrDefault(c, "") : "",
            Attributes(r.AttributesJson).Select(kv => new DraftAttribute(kv.Key, kv.Value.Value, kv.Value.Quote, kv.Value.BeatId)).ToList(),
            r.CreatedAt);

    internal static Dictionary<string, ReadAlongService.AttributeValue> Attributes(string json)
    {
        try { return JsonSerializer.Deserialize<Dictionary<string, ReadAlongService.AttributeValue>>(json, ReadAlongService.Json) ?? []; }
        catch (JsonException) { return []; }
    }

    public sealed record KeepResult(bool Ok, string? Error, Guid? EntityId, int FactsConfirmed);

    /// <summary>Keep it: a draft entry becomes a story-bible entry (an entity of this universe, made
    /// from this book) with every detail the drafts gathered, confirmed; a draft detail is confirmed
    /// on its entry. <paramref name="name"/> and <paramref name="category"/> let the author correct
    /// the draft as they keep it.</summary>
    public async Task<KeepResult> KeepAsync(Guid suggestionId, string? name = null, string? category = null, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var s = await db.StoryBibleSuggestions.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == suggestionId, ct);
        if (s is null) return new(false, "draft not found", null, 0);
        if (s.Status != ReadAlongStatus.Pending) return new(false, $"already {s.Status}", s.EntityId, 0);

        if (s.Kind == "detail")
        {
            var facts = await db.StoryFacts.IgnoreQueryFilters().Where(f => f.SuggestionId == s.Id).ToListAsync(ct);
            foreach (var f in facts) await ConfirmAsync(db, f, ct);
            s.Status = ReadAlongStatus.Kept;
            s.DecidedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            return new(true, null, s.EntityId, facts.Count);
        }

        var finalName = string.IsNullOrWhiteSpace(name) ? s.Name : name.Trim();
        var finalCategory = Categories.Contains(category ?? "") ? category! : s.Category;
        var entityType = ReadAlongService.EntityTypeOf(finalCategory);
        var key = Key(s.Name);

        // Link to an entity of the same name already in the universe rather than minting a twin.
        var existing = await db.Entities.IgnoreQueryFilters()
            .Where(e => e.UniverseId == s.UniverseId && e.Name == finalName && e.Status != "archived")
            .OrderByDescending(e => e.EntityType == entityType).FirstOrDefaultAsync(ct);
        Guid entityId;
        if (existing is not null) entityId = existing.Id;
        else
        {
            entityId = await EntityStubFactory.CreateStubAsync(db, s.UniverseId, finalName, entityType,
                description: null, originNodeId: s.BookNodeId, provenance: ClaimProvenance.Authored, ct: ct);
            var created = await db.Entities.IgnoreQueryFilters().FirstAsync(e => e.Id == entityId, ct);
            created.Status = "canon"; // the author kept it: this is canon, not a placeholder
        }

        var subjectFacts = await db.StoryFacts.IgnoreQueryFilters()
            .Where(f => f.BookNodeId == s.BookNodeId && (f.SuggestionId == s.Id || f.SubjectKey == key))
            .ToListAsync(ct);
        var confirmed = 0;
        foreach (var f in subjectFacts)
        {
            f.EntityId = entityId;
            if (f.SuggestionId == s.Id) { await ConfirmAsync(db, f, ct); confirmed++; }
        }
        foreach (var c in await db.ContinuityConflicts.IgnoreQueryFilters()
                     .Where(c => c.BookNodeId == s.BookNodeId && c.EntityId == null && c.EntityName == s.Name).ToListAsync(ct))
            c.EntityId = entityId;
        if (!string.Equals(finalName, s.Name, StringComparison.Ordinal))
            db.StoryFacts.Add(new StoryFact
            {
                UniverseId = s.UniverseId, BookNodeId = s.BookNodeId, EntityId = entityId, SubjectKey = Key(finalName), SubjectName = finalName,
                Attribute = "alias", Value = s.Name, Quote = s.Quote, BeatId = s.BeatId, ChapterNodeId = s.ChapterNodeId,
                Source = "author", Confirmed = true, Category = "character",
            });

        s.Status = ReadAlongStatus.Kept;
        s.EntityId = entityId;
        s.DecidedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        log.LogInformation("Story bible: kept {Name} ({Category}) as entity {Id}, {Facts} details", finalName, finalCategory, entityId, confirmed);
        return new(true, null, entityId, confirmed);
    }

    /// <summary>A confirmed single-valued fact supersedes any older confirmed value of the same
    /// attribute for the same entry (the author has just said which is true).</summary>
    private static async Task ConfirmAsync(ProseDbContext db, StoryFact f, CancellationToken ct)
    {
        f.Confirmed = true;
        if (f.EntityId is not { } id || !IsSingleValued(f.Attribute)) return;
        var older = await db.StoryFacts.IgnoreQueryFilters()
            .Where(o => o.Id != f.Id && o.EntityId == id && o.Attribute == f.Attribute && o.Confirmed && o.SupersededAt == null)
            .ToListAsync(ct);
        foreach (var o in older) o.SupersededAt = DateTime.UtcNow;
    }

    internal static bool IsSingleValued(string attribute) =>
        attribute == "group" || StoryFactExtractor.IsSingleValued(attribute);

    /// <summary>Not quite: the draft is gone for good. Its observations are deleted and the same
    /// draft is never raised again (the read-along skips tossed names and tossed details).</summary>
    public async Task<bool> TossAsync(Guid suggestionId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var s = await db.StoryBibleSuggestions.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == suggestionId, ct);
        if (s is null || s.Status != ReadAlongStatus.Pending) return false;

        var key = Key(s.Name);
        var facts = await db.StoryFacts.IgnoreQueryFilters()
            .Where(f => f.BookNodeId == s.BookNodeId && !f.Confirmed &&
                        (f.SuggestionId == s.Id || (s.Kind == "entry" && f.EntityId == null && f.SubjectKey == key)))
            .ToListAsync(ct);
        db.StoryFacts.RemoveRange(facts);
        if (s.Kind == "entry")
            foreach (var c in await db.ContinuityConflicts.IgnoreQueryFilters()
                         .Where(c => c.BookNodeId == s.BookNodeId && c.EntityId == null && c.EntityName == s.Name && c.Status == ReadAlongStatus.Open)
                         .ToListAsync(ct))
            {
                c.Status = ReadAlongStatus.Dismissed;
                c.ResolvedAt = DateTime.UtcNow;
                c.Note = "the entry was tossed (Not quite)";
            }
        s.Status = ReadAlongStatus.Tossed;
        s.DecidedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>Keep every pending draft of a book (or of one kind). For an author who has read the
    /// drafts and agrees with all of them; still one decision per draft, recorded.</summary>
    public async Task<int> KeepAllAsync(Guid bookId, string? kind = null, CancellationToken ct = default)
    {
        List<Guid> ids;
        await using (var db = await dbFactory.CreateDbContextAsync(ct))
            ids = await db.StoryBibleSuggestions.IgnoreQueryFilters()
                .Where(s => s.BookNodeId == bookId && s.Status == ReadAlongStatus.Pending && (kind == null || s.Kind == kind))
                .OrderBy(s => s.Kind == "detail").ThenBy(s => s.CreatedAt).Select(s => s.Id).ToListAsync(ct);
        var n = 0;
        foreach (var id in ids)
            if ((await KeepAsync(id, ct: ct)).Ok) n++;
        return n;
    }

    // ── Entries ─────────────────────────────────────────────────────────────────────────────

    public async Task<Entry?> GetEntryAsync(Guid bookId, Guid entityId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var e = await db.Entities.AsNoTracking().IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == entityId, ct);
        if (e is null) return null;

        var facts = await db.StoryFacts.AsNoTracking().IgnoreQueryFilters()
            .Where(f => f.EntityId == entityId && (f.Confirmed || f.SupersededAt != null))
            .OrderBy(f => f.BookNodeId != bookId).ThenBy(f => f.ChapterIndex).ThenBy(f => f.CreatedAt)
            .ToListAsync(ct);
        var titles = await TitlesAsync(db, facts.Where(f => f.ChapterNodeId != null).Select(f => f.ChapterNodeId!.Value), ct);
        Receipt R(StoryFact f) => new(f.Id, f.Attribute, f.Value, f.Quote, f.BeatId, f.ChapterNodeId,
            f.ChapterNodeId is { } c ? titles.GetValueOrDefault(c, "") : "", f.ChapterIndex, f.Source, f.Confirmed, f.SupersededAt);

        var current = facts.Where(f => f.Confirmed && f.SupersededAt == null).ToList();
        var aliases = current.Where(f => f.Attribute == "alias").Select(f => f.Value).Distinct().ToList();
        var group = current.LastOrDefault(f => f.Attribute == "group")?.Value;
        var relationTypes = new HashSet<string>(["brother", "sister", "mother", "father", "son", "daughter", "wife", "husband", "uncle", "aunt",
            "cousin", "grandmother", "grandfather", "twin", "mentor", "master", "apprentice", "niece", "nephew", "betrothed", "lover",
            "hated", "loved", "feared", "trusted", "distrusted", "served", "betrayed", "killed", "despised", "admired", "married",
            "mistrusted", "resented", "worshipped", "followed", "protected", "hunted", "member of", "lives in"]);

        var details = current.Where(f => f.Attribute is not "alias" and not "group" && !relationTypes.Contains(f.Attribute)).Select(R).ToList();

        // Links: typed relations from this entry's facts, relations others state about it, and edges.
        var links = new List<Link>();
        var keyIndex = await db.Entities.AsNoTracking().IgnoreQueryFilters()
            .Where(x => x.UniverseId == e.UniverseId && x.Status != "archived")
            .Select(x => new { x.Id, x.Name }).Take(20000).ToListAsync(ct);
        var byKey = keyIndex.GroupBy(x => Key(x.Name)).ToDictionary(g => g.Key, g => g.First().Id);
        foreach (var f in current.Where(f => relationTypes.Contains(f.Attribute)))
            links.Add(new Link(byKey.TryGetValue(Key(f.Value), out var tid) ? tid : null, f.Value, f.Attribute, f.Quote, f.BeatId,
                f.ChapterNodeId is { } c ? titles.GetValueOrDefault(c, "") : ""));
        var myKey = Key(e.Name);
        var relArr = relationTypes.ToArray();
        var inbound = await db.StoryFacts.AsNoTracking().IgnoreQueryFilters()
            .Where(f => f.Confirmed && f.SupersededAt == null && f.EntityId != entityId && f.UniverseId == e.UniverseId && relArr.Contains(f.Attribute))
            .ToListAsync(ct);
        foreach (var f in inbound.Where(f => Key(f.Value) == myKey))
            links.Add(new Link(f.EntityId, f.SubjectName, f.Attribute + " (of)", f.Quote, f.BeatId, ""));
        var edges = await db.Edges.AsNoTracking().IgnoreQueryFilters()
            .Where(x => (x.SourceId == entityId || x.TargetId == entityId) && x.InvalidatedAt == null).Take(100).ToListAsync(ct);
        var other = edges.Select(x => x.SourceId == entityId ? x.TargetId : x.SourceId).Distinct().ToList();
        var otherNames = await db.Entities.AsNoTracking().IgnoreQueryFilters().Where(x => other.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.Name, ct);
        foreach (var x in edges)
        {
            var oid = x.SourceId == entityId ? x.TargetId : x.SourceId;
            links.Add(new Link(oid, otherNames.GetValueOrDefault(oid, "?"), x.RelationType + (x.SourceId == entityId ? "" : " (of)"), x.Description ?? "", null, ""));
        }

        var pending = await db.StoryBibleSuggestions.AsNoTracking().IgnoreQueryFilters()
            .Where(s => s.EntityId == entityId && s.Kind == "detail" && s.Status == ReadAlongStatus.Pending).ToListAsync(ct);
        var ptitles = await TitlesAsync(db, pending.Where(p => p.ChapterNodeId != null).Select(p => p.ChapterNodeId!.Value), ct);

        var mentions = 0;
        foreach (var json in await db.ChapterReadings.AsNoTracking().IgnoreQueryFilters().Where(r => r.BookNodeId == bookId).Select(r => r.MetJson).ToListAsync(ct))
            mentions += ReadAlongService.Detail(json).Mentions.Where(m => m.EntityId == entityId || m.Key == myKey).Sum(m => m.Count);

        return new Entry(e.Id, e.Name, ReadAlongService.CategoryOf(e.EntityType), e.EntityType, e.Description, group, aliases,
            details, facts.Select(R).ToList(), links, pending.Select(p => ToDraft(p, ptitles)).ToList(), mentions);
    }

    /// <summary>Add an entry by hand. It is the author's bible: the read-along keeps up.</summary>
    public async Task<Guid> AddEntryAsync(Guid bookId, string name, string category, string? description = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("name is required", nameof(name));
        if (!Categories.Contains(category)) category = CatCharacter;
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var universeId = await db.Nodes.IgnoreQueryFilters().Where(n => n.Id == bookId).Select(n => n.UniverseId).FirstAsync(ct);
        var id = await EntityStubFactory.CreateStubAsync(db, universeId, name.Trim(), ReadAlongService.EntityTypeOf(category),
            string.IsNullOrWhiteSpace(description) ? null : description.Trim(), bookId, ClaimProvenance.Authored, ct);
        var e = await db.Entities.IgnoreQueryFilters().FirstAsync(x => x.Id == id, ct);
        e.Status = "canon";

        // A pending draft of the same name is this entry now.
        var key = Key(name);
        foreach (var d in await db.StoryBibleSuggestions.IgnoreQueryFilters()
                     .Where(s => s.BookNodeId == bookId && s.Kind == "entry" && s.Status == ReadAlongStatus.Pending).ToListAsync(ct))
            if (Key(d.Name) == key) { d.Status = ReadAlongStatus.Kept; d.EntityId = id; d.DecidedAt = DateTime.UtcNow; }
        foreach (var f in await db.StoryFacts.IgnoreQueryFilters().Where(f => f.BookNodeId == bookId && f.SubjectKey == key && f.EntityId == null).ToListAsync(ct))
            f.EntityId = id;
        await db.SaveChangesAsync(ct);
        return id;
    }

    /// <summary>Write a detail by hand (or correct one). Confirmed, sourced to the author.</summary>
    public async Task<Guid> SetDetailAsync(Guid bookId, Guid entityId, string attribute, string value, CancellationToken ct = default)
    {
        attribute = (attribute ?? "").Trim().ToLowerInvariant();
        if (attribute.Length == 0 || string.IsNullOrWhiteSpace(value)) throw new ArgumentException("attribute and value are required");
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var e = await db.Entities.IgnoreQueryFilters().FirstAsync(x => x.Id == entityId, ct);
        var fact = new StoryFact
        {
            UniverseId = e.UniverseId, BookNodeId = bookId, EntityId = entityId, SubjectKey = Key(e.Name), SubjectName = e.Name,
            Attribute = ReadAlongService.Trim(attribute, 80), Value = ReadAlongService.Trim(NormalizeValue(attribute, value), 400),
            Quote = "", Source = "author", Confirmed = true, Category = "character",
        };
        if (fact.Value.Length == 0) fact.Value = ReadAlongService.Trim(value.Trim(), 400);
        db.StoryFacts.Add(fact);
        await ConfirmAsync(db, fact, ct);
        await db.SaveChangesAsync(ct);
        return fact.Id;
    }

    public async Task<bool> DeleteDetailAsync(Guid factId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var f = await db.StoryFacts.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == factId, ct);
        if (f is null) return false;
        db.StoryFacts.Remove(f);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> SetDescriptionAsync(Guid entityId, string? description, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var e = await db.Entities.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == entityId, ct);
        if (e is null) return false;
        e.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        e.ModifiedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>Delete an entry from this book's bible. Its details from this book go; an entity
    /// that was made from this book's prose is archived (never hard-deleted — canon is versioned);
    /// an entity the universe already had stays. Its name will not be drafted again.</summary>
    public async Task<bool> RemoveEntryAsync(Guid bookId, Guid entityId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var e = await db.Entities.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == entityId, ct);
        if (e is null) return false;
        db.StoryFacts.RemoveRange(await db.StoryFacts.IgnoreQueryFilters().Where(f => f.BookNodeId == bookId && f.EntityId == entityId).ToListAsync(ct));
        db.StoryBibleSuggestions.RemoveRange(await db.StoryBibleSuggestions.IgnoreQueryFilters()
            .Where(s => s.BookNodeId == bookId && s.EntityId == entityId && s.Status == ReadAlongStatus.Pending).ToListAsync(ct));
        if (e.OriginNodeId == bookId)
        {
            e.Status = "archived";
            e.ModifiedAt = DateTime.UtcNow;
            db.StoryBibleSuggestions.Add(new StoryBibleSuggestion
            {
                UniverseId = e.UniverseId, BookNodeId = bookId, Kind = "entry", Category = ReadAlongService.CategoryOf(e.EntityType),
                Name = e.Name, Quote = "", Fingerprint = $"entry|{Key(e.Name)}", Status = ReadAlongStatus.Tossed, DecidedAt = DateTime.UtcNow,
            });
        }
        await db.SaveChangesAsync(ct);
        return true;
    }

    private static async Task<Dictionary<Guid, string>> TitlesAsync(ProseDbContext db, IEnumerable<Guid> ids, CancellationToken ct)
    {
        var list = ids.Distinct().ToList();
        if (list.Count == 0) return [];
        return await db.Nodes.AsNoTracking().IgnoreQueryFilters().Where(n => list.Contains(n.Id)).ToDictionaryAsync(n => n.Id, n => n.Title, ct);
    }
}
