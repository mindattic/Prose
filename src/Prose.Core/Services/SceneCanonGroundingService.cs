using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Core.Services;

/// <summary>
/// PRS-T926 — canon grounding for a scene written with no beat (<see cref="OneShotGenerationService"/>,
/// <c>generate_scene</c>).
///
/// <para><b>Why it exists.</b> A book beat is grounded by stages of <see cref="ProseWriterRouter"/> that key off the
/// beat and its node: the X-Ray (<see cref="SceneContextAssembler.AssembleForBeatAsync"/>, beat id), the entity
/// working memory and the doc stack (node id). A one-shot scene has no beat id, and a game universe has no book
/// node at all, so every one of those stages was skipped and the model wrote from the goal alone: SpaceGod scenes
/// invented "Kepler Station" and Kraken Port "on Enceladus Crater", GridGame scenes invented "Kess" and
/// "Whitehall". This service gives such a scene the same canon a beat gets, from the universe itself.</para>
///
/// <para><b>Before the write</b> (<see cref="BuildAsync"/>): the scene's characters, location and <c>about</c>
/// entities are resolved to entity rows (id, name, alias, a name without its article, then the shortest name
/// containing the word); the X-Ray block is assembled by <see cref="SceneContextAssembler.AssembleAsync"/> with
/// those entities pinned (book writing's own scene-assembly path, RFC 0002); and a canon block lists, for each
/// resolved entity, its record (description, properties and stats), its relations (gate links, roads, ownership,
/// faction ties) with the entities at the other end, what else is at or near the place, and the approved
/// <c>description</c> texts about it, under a rule to use no proper noun the canon does not have.</para>
///
/// <para><b>After the write</b> (<see cref="FindUnknownNames"/>): the capitalised names in the draft that resolve to
/// no entity, alias or caller-supplied word of the universe. Deterministic (protocol Law 7): a name scan, no model
/// call. <see cref="OneShotGenerationService"/> reports them and retries once with them as a correction.</para>
/// </summary>
public class SceneCanonGroundingService(
    IDbContextFactory<ProseDbContext> dbFactory,
    SceneContextAssembler sceneAssembler,
    ProseTextService texts,
    ILogger<SceneCanonGroundingService> log)
{
    public const int XRayTokenBudget = 2500;
    private const int MaxRelationsPerEntity = 24;
    private const int MaxNearbyPerPlace = 24;
    private const int MaxPropertiesPerEntity = 14;
    private const int MaxDescriptionTextsPerEntity = 3;
    private const int MaxPinned = 12;

    /// <summary>Entity types never on screen: a game text is writing about the world, a book node is structure.</summary>
    private static readonly HashSet<string> NonSceneTypes = new(StringComparer.OrdinalIgnoreCase)
        { ProseTextService.EntityType, "chapter", "book", "node", "series", "beat", "quote", "archetype" };

    /// <summary>Relation types that say where a thing is or how to get there: followed one more hop from a place.</summary>
    private static readonly HashSet<string> PlaceRelations = new(StringComparer.OrdinalIgnoreCase)
        { "located-in", "located_at", "located-at", "orbits", "in", "part-of", "within", "inside" };

    public sealed record ResolvedEntity(Guid Id, string Name, string Slug, string EntityType, string Requested);

    public sealed class Grounding
    {
        public required string UniverseSlug { get; init; }
        public required IReadOnlyList<ResolvedEntity> Pinned { get; init; }
        /// <summary>The requested characters/location/about words that resolved to no entity.</summary>
        public required IReadOnlyList<string> Unresolved { get; init; }
        /// <summary>The X-Ray roster block (SceneContextAssembler), for BeatContext.XRayContext.</summary>
        public required string XRayBlock { get; init; }
        /// <summary>The names on the X-Ray roster.</summary>
        public required IReadOnlyList<string> RosterNames { get; init; }
        /// <summary>The canon block (records, relations, nearby, approved descriptions, the naming rule), for
        /// BeatContext.RelationshipContext.</summary>
        public required string CanonBlock { get; init; }
        /// <summary>Every name and alias of the universe, plus the caller's own words, lower-cased: what
        /// <see cref="FindUnknownNames"/> accepts.</summary>
        public required IReadOnlySet<string> KnownNames { get; init; }
        /// <summary>Every word of a known multi-word name ("Federation" of "Sol Federation"), lower-cased.</summary>
        public required IReadOnlySet<string> KnownNameWords { get; init; }
    }

    /// <summary>Resolve the scene's entities and build its canon context. Runs in the caller's flow universe
    /// (<paramref name="universeId"/> is also applied explicitly to every query).</summary>
    public async Task<Grounding> BuildAsync(Guid universeId, string universeSlug, string beatGoal, string? subtext,
        IReadOnlyList<string> characters, string? location, IReadOnlyList<string> about, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var index = await LoadNameIndexAsync(db, universeId, ct);

        // 1 — resolve what the caller named.
        var pinned = new List<ResolvedEntity>();
        var unresolved = new List<string>();
        void Pin(IEnumerable<string> tokens, Func<IndexedEntity, bool>? prefer)
        {
            foreach (var token in tokens.Select(t => (t ?? "").Trim()).Where(t => t.Length > 0))
            {
                var hits = Resolve(index, token, prefer);
                if (hits.Count == 0) { unresolved.Add(token); continue; }
                foreach (var h in hits)
                    if (pinned.All(p => p.Id != h.Id))
                        pinned.Add(new ResolvedEntity(h.Id, h.Name, h.Slug, h.Type, token));
            }
        }
        Pin(characters, e => e.Type.Equals("character", StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(location)) Pin([location!], e => !e.Type.Equals("character", StringComparison.OrdinalIgnoreCase));
        Pin(about, null);
        if (pinned.Count > MaxPinned) pinned = pinned.Take(MaxPinned).ToList();

        // 2 — the X-Ray, through book writing's scene assembler, with the resolved entities pinned.
        var scanText = string.Join("\n", new[] { beatGoal, subtext ?? "", location ?? "" }
            .Concat(characters).Concat(pinned.Select(p => p.Name)).Where(s => !string.IsNullOrWhiteSpace(s)));
        string xray = "";
        IReadOnlyList<string> roster = [];
        try
        {
            var sc = await sceneAssembler.AssembleAsync(scanText, XRayTokenBudget, ct, contextNodeId: null,
                pinnedEntityIds: pinned.Select(p => p.Id).ToList());
            xray = sc.ContextBlock;
            roster = sc.Roster.Select(r => r.Name).Distinct().ToList();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // The canon block below still grounds the scene; a missing X-Ray is reported, not fatal.
            log.LogWarning(ex, "[SceneCanonGrounding] scene assembly failed for {Universe}", universeSlug);
        }

        // 3 — the canon block.
        var canon = await BuildCanonBlockAsync(db, universeId, universeSlug, pinned, index, ct);

        var known = new HashSet<string>(index.Names.Keys, StringComparer.OrdinalIgnoreCase);
        foreach (var w in CallerWords(beatGoal, subtext, characters, location, about)) known.Add(w);
        var words = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in index.Names.Keys)
            foreach (var w in NameSplit.Split(name))
                if (w.Length >= 3) words.Add(w);

        return new Grounding
        {
            UniverseSlug = universeSlug,
            Pinned = pinned,
            Unresolved = unresolved,
            XRayBlock = xray,
            RosterNames = roster,
            CanonBlock = canon,
            KnownNames = known,
            KnownNameWords = words,
        };
    }

    // ── Resolution ─────────────────────────────────────────────────────────

    internal sealed record IndexedEntity(Guid Id, string Name, string Slug, string Type);

    internal sealed class NameIndex
    {
        public Dictionary<Guid, IndexedEntity> ById = new();
        public Dictionary<string, IndexedEntity> BySlug = new(StringComparer.OrdinalIgnoreCase);
        /// <summary>Name, alias and article-less name → entities.</summary>
        public Dictionary<string, List<IndexedEntity>> Names = new(StringComparer.OrdinalIgnoreCase);
        /// <summary>Name slug ("sol-federation", the graph's node id for "Sol Federation") → entities.</summary>
        public Dictionary<string, List<IndexedEntity>> NameSlugs = new(StringComparer.OrdinalIgnoreCase);

        public void AddName(string? name, IndexedEntity e)
        {
            var n = (name ?? "").Trim();
            if (n.Length < 2) return;
            Add(n, e);
            var slug = NameSlug(n);
            if (slug.Length >= 2)
            {
                if (!NameSlugs.TryGetValue(slug, out var sl)) NameSlugs[slug] = sl = new();
                if (sl.All(x => x.Id != e.Id)) sl.Add(e);
            }
            var bare = StripArticle(n);
            if (!ReferenceEquals(bare, n) && bare.Length >= 3) Add(bare, e);
        }

        private void Add(string key, IndexedEntity e)
        {
            if (!Names.TryGetValue(key, out var list)) Names[key] = list = new();
            if (list.All(x => x.Id != e.Id)) list.Add(e);
        }
    }

    private async Task<NameIndex> LoadNameIndexAsync(ProseDbContext db, Guid universeId, CancellationToken ct)
    {
        var rows = await db.Set<Entity>().AsNoTracking().IgnoreQueryFilters()
            .Where(e => e.UniverseId == universeId && e.Status != "archived")
            .Select(e => new { e.Id, e.Name, e.Slug, e.EntityType })
            .ToListAsync(ct);
        var index = new NameIndex();
        foreach (var r in rows)
        {
            if (NonSceneTypes.Contains(r.EntityType ?? "")) continue;
            var e = new IndexedEntity(r.Id, r.Name ?? "", r.Slug ?? "", r.EntityType ?? "");
            index.ById[e.Id] = e;
            if (!string.IsNullOrEmpty(e.Slug)) index.BySlug.TryAdd(e.Slug, e);
            index.AddName(e.Name, e);
        }

        var ids = index.ById.Keys.ToHashSet();
        var aliases = await db.Set<CharacterAlias>().AsNoTracking()
            .Select(a => new { a.CharacterId, a.Value }).ToListAsync(ct);
        foreach (var a in aliases)
            if (ids.Contains(a.CharacterId)) index.AddName(a.Value, index.ById[a.CharacterId]);

        // Interchange imports keep a game's aliases as a property ("aliases": "the Leech"; comma or semicolon lists).
        var aliasProps = await db.Set<EntityProperty>().AsNoTracking()
            .Where(p => p.PropertyKey == "aliases" || p.PropertyKey == "alias")
            .Select(p => new { p.EntityId, p.Value }).ToListAsync(ct);
        foreach (var p in aliasProps)
            if (ids.Contains(p.EntityId) && !string.IsNullOrWhiteSpace(p.Value))
                foreach (var alias in SplitList(p.Value!))
                    index.AddName(alias, index.ById[p.EntityId]);
        return index;
    }

    /// <summary>A requested word → its entities: exact id/slug; exact name or alias (article optional); else the
    /// shortest names that contain it as a whole word ("the cathedral" → "The Choir's Cathedral"). Ties prefer
    /// <paramref name="prefer"/>; at most two (a character and its class may share a name).</summary>
    internal static List<IndexedEntity> Resolve(NameIndex index, string token, Func<IndexedEntity, bool>? prefer)
    {
        var t = token.Trim();
        if (Guid.TryParse(t, out var gid) && index.ById.TryGetValue(gid, out var byId)) return [byId];
        if (index.BySlug.TryGetValue(t, out var bySlug)) return [bySlug];

        List<IndexedEntity>? hits = null;
        if (index.Names.TryGetValue(t, out var exact)) hits = exact;
        else if (index.Names.TryGetValue(StripArticle(t), out var bare)) hits = bare;
        else if (index.NameSlugs.TryGetValue(NameSlug(t), out var bySlugName)) hits = bySlugName;
        if (hits is { Count: > 0 }) return Rank(hits, prefer).Take(2).ToList();

        var word = StripArticle(t);
        if (word.Length < 4) return [];
        var rx = new Regex($@"(?<!\w){Regex.Escape(word)}(?!\w)", RegexOptions.IgnoreCase);
        var contains = index.ById.Values.Where(e => rx.IsMatch(e.Name)).ToList();
        if (contains.Count == 0) return [];
        var ranked = Rank(contains, prefer).ToList();
        var bestLen = ranked.First().Name.Length;
        var first = ranked.First();
        return ranked.Where(e => e.Name.Length == bestLen && (prefer == null || prefer(e) == prefer(first))).Take(2).ToList();
    }

    private static IEnumerable<IndexedEntity> Rank(IEnumerable<IndexedEntity> list, Func<IndexedEntity, bool>? prefer) =>
        list.OrderByDescending(e => prefer?.Invoke(e) ?? false)
            .ThenBy(e => e.Name.Length)
            .ThenBy(e => e.Name, StringComparer.Ordinal);

    // ── The canon block ────────────────────────────────────────────────────

    private async Task<string> BuildCanonBlockAsync(ProseDbContext db, Guid universeId, string universeSlug,
        IReadOnlyList<ResolvedEntity> pinned, NameIndex index, CancellationToken ct)
    {
        var sb = new StringBuilder();
        var universe = await db.Universes.AsNoTracking().FirstOrDefaultAsync(u => u.Id == universeId, ct);
        sb.AppendLine($"CANON GROUNDING — the {universe?.Name ?? universeSlug} universe. This scene is written FROM its canon:");
        sb.AppendLine("- Every proper noun you use (person, ship, station, port, planet, place, faction, creature, god, item) must be one named in this context or the scene x-ray. Do not invent names.");
        sb.AppendLine("- An unnamed figure or place is described by role or look (\"the patrol's captain\", \"a dockhand\"), never given a new name.");
        sb.AppendLine("- The facts below (where things are, who holds them, how they connect, what they carry) are canon: do not contradict them or add geography, weather, history or technology they rule out.");
        if (!string.IsNullOrWhiteSpace(universe?.Description))
            sb.AppendLine($"SETTING: {Clip(universe!.Description!, 600)}");
        if (string.IsNullOrWhiteSpace(universe?.UniversePrimer) && !string.IsNullOrWhiteSpace(universe?.WorldFacts))
            sb.AppendLine($"WORLD RULES:\n{Clip(universe!.WorldFacts!, 1500)}");
        sb.AppendLine();

        if (pinned.Count == 0)
        {
            sb.AppendLine("(No characters, location or about entities were given or resolved; use only names in the scene x-ray.)");
            return sb.ToString().TrimEnd();
        }

        var ids = pinned.Select(p => p.Id).ToList();
        var entities = await db.Set<Entity>().AsNoTracking().IgnoreQueryFilters()
            .Where(e => ids.Contains(e.Id)).ToDictionaryAsync(e => e.Id, ct);
        var props = await db.Set<EntityProperty>().AsNoTracking()
            .Where(p => ids.Contains(p.EntityId) && p.StoryValidUntil == null)
            .Select(p => new { p.EntityId, p.PropertyKey, p.Value }).ToListAsync(ct);
        var edges = await db.Set<Edge>().AsNoTracking().IgnoreQueryFilters()
            .Where(e => e.UniverseId == universeId && e.InvalidatedAt == null && (ids.Contains(e.SourceId) || ids.Contains(e.TargetId)))
            .Select(e => new { e.SourceId, e.TargetId, e.RelationType, e.Description })
            .Take(2000).ToListAsync(ct);

        // One more hop from a place: what else is at its container (the patrol ships near Earth, the stalls of the
        // cathedral's city) — the names a scene set there may use.
        var containers = edges
            .Where(e => ids.Contains(e.SourceId) && PlaceRelations.Contains(e.RelationType))
            .Select(e => e.TargetId).Distinct().ToList();
        var nearbyEdges = containers.Count == 0 ? [] : await db.Set<Edge>().AsNoTracking().IgnoreQueryFilters()
            .Where(e => e.UniverseId == universeId && e.InvalidatedAt == null && containers.Contains(e.TargetId)
                && (e.RelationType == "near" || e.RelationType == "located-in" || e.RelationType == "orbits"
                    || e.RelationType == "located_at" || e.RelationType == "located-at"))
            .Select(e => new { e.SourceId, e.TargetId, e.RelationType })
            .Take(400).ToListAsync(ct);

        var describedTexts = await LoadDescriptionTextsAsync(universeSlug, pinned, ct);

        foreach (var p in pinned)
        {
            entities.TryGetValue(p.Id, out var row);
            sb.AppendLine($"## {p.Name} ({p.EntityType}; id {p.Slug})");
            if (!string.IsNullOrWhiteSpace(row?.Description)) sb.AppendLine(Clip(row!.Description!, 700));

            var fields = props.Where(x => x.EntityId == p.Id && !string.IsNullOrWhiteSpace(x.Value)
                    && !x.PropertyKey.Equals("description", StringComparison.OrdinalIgnoreCase))
                .GroupBy(x => x.PropertyKey, StringComparer.OrdinalIgnoreCase)
                .Take(MaxPropertiesPerEntity)
                .Select(g => $"{g.Key}: {Clip(string.Join("; ", g.Select(x => x.Value)), 180)}")
                .ToList();
            if (fields.Count > 0) sb.AppendLine("RECORD: " + string.Join(" | ", fields));

            var rels = new List<string>();
            var stocked = new List<string>();
            foreach (var e in edges.Where(e => e.SourceId == p.Id || e.TargetId == p.Id))
            {
                var outgoing = e.SourceId == p.Id;
                var otherId = outgoing ? e.TargetId : e.SourceId;
                if (!index.ById.TryGetValue(otherId, out var other)) continue; // a text, archived or another universe
                if (outgoing && e.RelationType.Equals("stocks", StringComparison.OrdinalIgnoreCase)) { stocked.Add(other.Name); continue; }
                var line = outgoing
                    ? $"{e.RelationType} {other.Name} ({other.Type})"
                    : $"{other.Name} ({other.Type}) {e.RelationType} it";
                if (!string.IsNullOrWhiteSpace(e.Description)) line += $" — {Clip(e.Description!, 100)}";
                if (!rels.Contains(line)) rels.Add(line);
            }
            if (rels.Count > 0)
                sb.AppendLine("RELATIONS: " + string.Join("; ", rels.Take(MaxRelationsPerEntity))
                    + (rels.Count > MaxRelationsPerEntity ? $"; and {rels.Count - MaxRelationsPerEntity} more" : ""));
            if (stocked.Count > 0) sb.AppendLine("STOCKS: " + string.Join(", ", stocked.Distinct().Take(30)));

            var myContainers = edges.Where(e => e.SourceId == p.Id && PlaceRelations.Contains(e.RelationType))
                .Select(e => e.TargetId).ToHashSet();
            foreach (var c in myContainers)
            {
                if (!index.ById.TryGetValue(c, out var container)) continue;
                var nearby = nearbyEdges.Where(e => e.TargetId == c && e.SourceId != p.Id)
                    .Select(e => index.ById.TryGetValue(e.SourceId, out var n) ? n : null)
                    .Where(n => n != null).Select(n => n!)
                    .DistinctBy(n => n.Id)
                    .OrderBy(n => n.Type, StringComparer.Ordinal).ThenBy(n => n.Name, StringComparer.Ordinal)
                    .ToList();
                if (nearby.Count == 0) continue;
                sb.AppendLine($"ALSO AT/NEAR {container.Name}: " + string.Join(", ",
                    nearby.Take(MaxNearbyPerPlace).Select(n => $"{n.Name} ({n.Type})"))
                    + (nearby.Count > MaxNearbyPerPlace ? $", and {nearby.Count - MaxNearbyPerPlace} more" : ""));
            }

            if (describedTexts.TryGetValue(p.Slug, out var descs))
                foreach (var d in descs.Take(MaxDescriptionTextsPerEntity))
                    sb.AppendLine($"APPROVED TEXT ({d.Key}): {Clip(d.Body, 700)}");
            sb.AppendLine();
        }
        return sb.ToString().TrimEnd();
    }

    private async Task<Dictionary<string, List<(string Key, string Body)>>> LoadDescriptionTextsAsync(
        string universeSlug, IReadOnlyList<ResolvedEntity> pinned, CancellationToken ct)
    {
        var result = new Dictionary<string, List<(string, string)>>(StringComparer.OrdinalIgnoreCase);
        var slugs = pinned.Select(p => p.Slug).Where(s => !string.IsNullOrEmpty(s)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (slugs.Count == 0) return result;
        try
        {
            // Live (approved or published) description texts only: a draft is not canon until the author approves it.
            foreach (var doc in await texts.ListAsync(universeSlug, kind: "description", status: "pullable", ct: ct))
            {
                var body = string.Join(" ", new[] { Str(doc.Payload, "short"), Str(doc.Payload, "long") }
                    .Where(s => !string.IsNullOrWhiteSpace(s)));
                if (body.Length == 0) continue;
                foreach (var a in doc.About.Where(slugs.Contains))
                {
                    if (!result.TryGetValue(a, out var list)) result[a] = list = new();
                    list.Add((doc.Key, body));
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            log.LogWarning(ex, "[SceneCanonGrounding] description texts unavailable for {Universe}", universeSlug);
        }
        return result;
    }

    // ── After the write: names the canon does not have ─────────────────────

    /// <summary>Titles and generic capitalised words that are not names on their own.</summary>
    private static readonly HashSet<string> NotNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Captain", "Commander", "Admiral", "Lieutenant", "Sergeant", "Officer", "Pilot", "Sir", "Madam", "Lady", "Lord",
        "Master", "Mistress", "Father", "Mother", "Brother", "Sister", "Doctor", "Chief", "Boss", "Elder", "Saint",
        "Your", "Majesty", "Highness", "Grace", "Holiness", "Excellency", "Honor", "Honour",
        "God", "Gods", "Heaven", "Hell", "Lord", "Amen", "Hail", "Mayday", "Over", "Out", "Copy", "Roger", "Negative",
        "Affirmative", "Stand", "Halt", "Stop", "Hold", "Listen", "Look", "Please", "Thanks", "Sorry", "Hello",
        "North", "South", "East", "West",
    };

    private static readonly Regex NameSplit = new(@"[\s\-–—/]+", RegexOptions.Compiled);

    /// <summary>The capitalised names in <paramref name="text"/> that the canon does not have: not an entity name or
    /// alias of the universe (article optional), not a word of one ("Federation" of "Sol Federation"), not a word
    /// the caller wrote in the request, and not a title. A multi-word run is judged whole, so "Luna Station" is
    /// flagged although "Luna" is a known word.</summary>
    public static List<string> FindUnknownNames(string text, Grounding g) =>
        FindUnknownNames(text, g.KnownNames, g.KnownNameWords);

    internal static List<string> FindUnknownNames(string text, IReadOnlySet<string> known, IReadOnlySet<string> knownWords)
    {
        if (string.IsNullOrWhiteSpace(text)) return [];
        // Placeholders the game fills ({ship}, {pilot}) are not prose; markdown heading/label lines the model adds
        // ("# BEAT: Inspection", "sceneEnd=false") are not names; and a capitalised run never spans a line break
        // ("Inspection\n\nThe" is two words, not a name), so every line ends as a sentence does.
        var scrubbed = Regex.Replace(text, @"\{[^}]*\}", " ");
        scrubbed = Regex.Replace(scrubbed, @"(?m)^\s*(#.*|\*\*[A-Z][A-Za-z ]*:.*|sceneEnd\s*=.*)$", "");
        scrubbed = Regex.Replace(scrubbed, @"[ \t]*\r?\n\s*", ".\n");
        var candidates = EntityMentionScanner.FindUnresolvedProperNouns(scrubbed, []);
        var unknown = new List<string>();
        foreach (var c in candidates)
        {
            var name = StripArticle(c.Trim());
            if (name.Length <= 3) continue;
            if (known.Contains(name) || known.Contains("The " + name)) continue;
            var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 1 && (IsKnownWord(name) || NotNames.Contains(name))) continue;
            // A run that starts with a title ("Captain Voss") is judged by the rest.
            var rest = words.SkipWhile(NotNames.Contains).ToArray();
            if (rest.Length == 0) continue;
            if (rest.Length < words.Length)
            {
                var tail = string.Join(' ', rest);
                if (known.Contains(tail) || (rest.Length == 1 && IsKnownWord(tail))) continue;
                name = tail;
            }
            // A possessive of a known name ("Hierarch's") reads as the name.
            if (name.EndsWith("s", StringComparison.Ordinal) && known.Contains(name[..^1])) continue;
            // A known name without its prefix ("Lucky Strike" for the ship "MV Lucky Strike"): only a whole trailing
            // part of one known name counts, so "Luna Station" stays unknown.
            if (words.Length > 1 && known.Any(k => k.EndsWith(" " + name, StringComparison.OrdinalIgnoreCase))) continue;
            if (!unknown.Contains(name, StringComparer.OrdinalIgnoreCase)) unknown.Add(name);
        }
        return unknown;

        // The scan splits at an apostrophe ("Choir's" reads as "Choir"), so a word is known with or without one.
        bool IsKnownWord(string w) => knownWords.Contains(w) || knownWords.Contains(w + "'s") || knownWords.Contains(w + "’s");
    }

    // ── helpers ────────────────────────────────────────────────────────────

    private static IEnumerable<string> CallerWords(string goal, string? subtext, IReadOnlyList<string> characters, string? location,
        IReadOnlyList<string> about)
    {
        foreach (var s in characters.Concat(about).Append(location ?? ""))
            if (!string.IsNullOrWhiteSpace(s)) yield return s.Trim();
        foreach (var s in new[] { goal, subtext ?? "" })
            foreach (Match m in Regex.Matches(s, @"\p{Lu}[\p{L}'’]+(?:\s+\p{Lu}[\p{L}'’]+)*"))
            {
                yield return m.Value;
                foreach (var w in m.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries)) yield return w;
            }
    }

    private static IEnumerable<string> SplitList(string value) =>
        value.Split([',', ';', '|'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(v => v.Trim('"', '\'', ' '))
            .Where(v => v.Length >= 2);

    /// <summary>The graph's name slug: lower-case, every run of non-alphanumerics one hyphen ("Barnard's Star" →
    /// "barnard-s-star").</summary>
    internal static string NameSlug(string name) =>
        Regex.Replace(name.ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');

    internal static string StripArticle(string name)
    {
        foreach (var a in new[] { "the ", "a ", "an " })
            if (name.Length > a.Length && name.StartsWith(a, StringComparison.OrdinalIgnoreCase))
                return name[a.Length..].Trim();
        return name;
    }

    private static string? Str(JsonElement e, string prop) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString() : null;

    private static string Clip(string s, int max) => s.Length <= max ? s : s[..max].TrimEnd() + "…";
}
