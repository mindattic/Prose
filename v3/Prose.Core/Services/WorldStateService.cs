using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Interfaces;
using Prose.Core.Models;
using Prose.Core.Models.Canon;
using Prose.Core.Models.Graph;

namespace Prose.Core.Services;

/// <summary>
/// The spine that every prose-writing operation hangs off of. Given an entity and
/// a story-time cursor, assembles a typed Dossier — properties at AsOf, the entity's
/// 1-hop linked entities (gear, partners, employer, location), 2-hop adjacents,
/// continuity facts (CANONICAL/CONFIRMED only), and a timeline of chapter beats
/// the entity actually appeared in. Replaces "scan files, paste briefs into the
/// prompt" with a single object the LLM sees, the precondition-checker validates
/// against, and post-write extraction updates.
/// </summary>
public class WorldStateService
{
    private readonly UniverseGraphService graph;
    private readonly ContinuityService continuity;
    private readonly IChapterRepository chapters;
    private readonly CharacterRepository characterRepo;
    private readonly ILogger<WorldStateService> log;

    /// <summary>
    /// Per-entity dossier cache keyed by (entityId, asOf-storypoint). Invalidated
    /// when chapters or character records change — see <see cref="Invalidate"/> and
    /// <see cref="InvalidateAll"/>. Keeps the LLM-side cost stable when the same
    /// dossier is requested repeatedly across a writing session.
    /// </summary>
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, Dossier> cache = new();

    /// <summary>Edge relation types treated as "core" links worth promoting to full cards.</summary>
    private static readonly HashSet<string> LinkedRelations = new(StringComparer.OrdinalIgnoreCase)
    {
        "carries", "wields", "wears", "owns", "uses",
        "partner_of", "married_to", "sibling_of", "parent_of", "child_of", "family_of",
        "employer_of", "employee_of", "works_for", "member_of", "affiliated_with",
        "located_at", "lives_at", "operates_at",
    };

    /// <summary>Holding-type relations (gear in hand). Drives DerivedState.Holding.</summary>
    private static readonly HashSet<string> HoldingRelations = new(StringComparer.OrdinalIgnoreCase)
    {
        "carries", "wields", "uses",
    };

    /// <summary>Wearing-type relations (apparel/equipment). Drives DerivedState.Wearing.</summary>
    private static readonly HashSet<string> WearingRelations = new(StringComparer.OrdinalIgnoreCase)
    {
        "wears", "equipped_with",
    };

    private const int MaxLinked        = 10;
    private const int MaxAdjacent      = 16;
    private const int MaxTimelineBeats = 40;
    private const int SnippetMaxChars  = 240;

    public WorldStateService(
        UniverseGraphService graph,
        ContinuityService continuity,
        IChapterRepository chapters,
        CharacterRepository characterRepo,
        ILogger<WorldStateService> log)
    {
        this.graph = graph;
        this.continuity = continuity;
        this.chapters = chapters;
        this.characterRepo = characterRepo;
        this.log = log;
    }

    /// <summary>Invalidate the cached dossier for one entity at all asOf cursors.</summary>
    public void Invalidate(string entityId)
    {
        if (string.IsNullOrEmpty(entityId)) return;
        foreach (var key in cache.Keys.Where(k => k.StartsWith(entityId + "|", StringComparison.OrdinalIgnoreCase)).ToList())
            cache.TryRemove(key, out _);
    }

    /// <summary>Drop every cached dossier. Call on chapter save / canon edit.</summary>
    public void InvalidateAll() => cache.Clear();

    /// <summary>
    /// Temporal rewind — fetch the canonical record for an entity AS IT EXISTED
    /// in the database at <paramref name="sysTime"/>. Uses SQL Server's
    /// <c>FOR SYSTEM_TIME AS OF</c> clause to query the system-versioned history
    /// table. Returns null when the entity didn't exist yet, the database
    /// provider isn't SQL Server, or the row simply has no history at that point.
    /// </summary>
    public string? GetRecordJsonAsOf(string entityIdOrName, DateTime sysTime)
    {
        graph.EnsureLoaded();
        var idStr = graph.ResolveId(entityIdOrName) ?? entityIdOrName;
        if (!Guid.TryParse(idStr, out var id) && !Guid.TryParseExact(idStr, "N", out id))
            return null;

        // Reach the Prose context. Cache holds dossiers, not raw history;
        // we resolve a fresh context here for the temporal query.
        try
        {
            using var db = DbCtxFactory?.CreateDbContext();
            if (db == null) return null;
            if (!db.Database.IsSqlServer()) return null;

            var formatted = sysTime.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.fffffff");
            // FOR SYSTEM_TIME AS OF requires a literal datetime — EF cannot parameterize it.
            // EntityId is parameterized to follow the safe-query contract.
            var sql = $"SELECT [Json] FROM [dbo].[Records] FOR SYSTEM_TIME AS OF '{formatted}' WHERE [EntityId] = @p0";
            var json = db.Database.SqlQueryRaw<string>(sql, id.ToString()).AsEnumerable().FirstOrDefault();
            return json;
        }
        catch (Exception ex)
        {
            log.LogDebug(ex, "Temporal recall failed for {Id} as of {When}", id, sysTime);
            return null;
        }
    }

    /// <summary>Optional DbContextFactory accessor used by temporal recall. Wired by DI.</summary>
    public Microsoft.EntityFrameworkCore.IDbContextFactory<Data.ProseDbContext>? DbCtxFactory { get; set; }

    /// <summary>
    /// Build a dossier for one entity at a given story-time cursor. Pass either
    /// a graph node id or a name/alias — both resolve via UniverseGraphService.ResolveId.
    /// Returns null only when the entity cannot be resolved.
    /// </summary>
    public Dossier? GetDossier(string entityIdOrName, AsOfCursor? asOf = null, CancellationToken ct = default)
    {
        asOf ??= AsOfCursor.Current;
        graph.EnsureLoaded();

        var id = graph.ResolveId(entityIdOrName) ?? (graph.GetNode(entityIdOrName) != null ? entityIdOrName : null);
        if (id == null) return null;
        var node = graph.GetNode(id);
        if (node == null) return null;

        var cacheKey = $"{id}|{asOf.ToStoryPoint()}|{asOf.ChapterId}|{asOf.BeatIndex}";
        if (cache.TryGetValue(cacheKey, out var cached)) return cached;

        var storyPoint = asOf.ToStoryPoint();
        var subject = BuildEntityCard(node, storyPoint, includeEdges: true, includeCharacterDetails: true);

        // 1-hop linked entities — promote to full cards for the relations that matter most.
        var linkedNodes = subject.Edges
            .Where(e => LinkedRelations.Contains(e.Relation))
            .Select(e => graph.GetNode(e.OtherId))
            .Where(n => n != null)
            .DistinctBy(n => n!.Id)
            .Take(MaxLinked)
            .ToList();

        var linked = linkedNodes
            .Select(n => BuildEntityCard(n!, storyPoint, includeEdges: false, includeCharacterDetails: false))
            .ToList();

        // 2-hop adjacents — light cards, ranked by relevance. Score = accumulated edge
        // weight along paths from subject's 1-hop linked neighbors, plus a bonus for
        // entities that appear in the subject's recent chapter beats.
        var skipIds = new HashSet<string>(linked.Select(l => l.Id), StringComparer.OrdinalIgnoreCase) { id };
        var scored = ScoreAdjacents(node, linked, storyPoint, skipIds);
        var adjacent = scored
            .OrderByDescending(s => s.Score)
            .ThenBy(s => s.Node.Name, StringComparer.OrdinalIgnoreCase)
            .Take(MaxAdjacent)
            .Select(s => BuildLightCard(s.Node, storyPoint))
            .ToList();

        var facts = LoadFacts(id, asOf, subject.Properties);
        var timeline = LoadTimeline(node.Name, ct, asOf);
        var derived = DeriveState(subject, timeline);

        var dossier = new Dossier(subject, linked, adjacent, facts, timeline, derived, asOf);
        cache[cacheKey] = dossier;
        return dossier;
    }

    /// <summary>
    /// Build dossiers for every named entity in a scene. Used by prose generation
    /// to fill the prompt with structured world-state for every speaker/POV.
    /// </summary>
    public IReadOnlyList<Dossier> GetSceneDossiers(IEnumerable<string> entityNamesOrIds, AsOfCursor? asOf = null, CancellationToken ct = default)
    {
        asOf ??= AsOfCursor.Current;
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var bag = new List<Dossier>();
        foreach (var name in entityNamesOrIds)
        {
            if (string.IsNullOrWhiteSpace(name)) continue;
            var d = GetDossier(name, asOf, ct);
            if (d == null) { log.LogDebug("WorldStateService: could not resolve '{Name}' to a graph node", name); continue; }
            if (!seen.Add(d.Subject.Id)) continue;
            bag.Add(d);
        }
        return bag;
    }

    // ── Card construction ─────────────────────────────────────────────────────

    private EntityCard BuildEntityCard(UniverseNode node, string storyPoint, bool includeEdges, bool includeCharacterDetails)
    {
        var props = MaterializeProperties(node, storyPoint);

        var edges = includeEdges
            ? GetEdgesForCard(node.Id, storyPoint)
            : (IReadOnlyList<EdgeRef>)Array.Empty<EdgeRef>();

        var oneLine = BuildOneLine(props);
        var sections = includeCharacterDetails && IsCharacterKind(node.NodeType)
            ? BuildCharacterSections(node, storyPoint)
            : (IReadOnlyList<DossierSection>)Array.Empty<DossierSection>();

        return new EntityCard(
            Id: node.Id,
            Name: node.Name,
            Kind: node.NodeType,
            Properties: props,
            Edges: edges,
            OneLine: oneLine,
            Sections: sections);
    }

    private EntityCard BuildLightCard(UniverseNode node, string storyPoint)
    {
        var props = MaterializeProperties(node, storyPoint);
        return new EntityCard(
            Id: node.Id,
            Name: node.Name,
            Kind: node.NodeType,
            Properties: props,
            Edges: Array.Empty<EdgeRef>(),
            OneLine: BuildOneLine(props),
            Sections: Array.Empty<DossierSection>());
    }

    private static bool IsCharacterKind(string kind)
        => string.Equals(kind, "character", StringComparison.OrdinalIgnoreCase)
        || string.Equals(kind, "person",    StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Pull the canonical character record and surface the high-leverage sub-blocks
    /// the LLM needs but graph properties don't carry: belongings, cyberware, knowledge,
    /// conditions, psychology, behavioral rules, and speech tics. Filtered to AsOf where
    /// the data carries chapter cursors.
    /// </summary>
    private IReadOnlyList<DossierSection> BuildCharacterSections(UniverseNode node, string storyPoint)
    {
        CharacterData? record;
        try { record = characterRepo.GetById(node.Id) ?? characterRepo.GetByName(node.Name); }
        catch (Exception ex) { log.LogDebug(ex, "Character record lookup failed for {Id}", node.Id); return Array.Empty<DossierSection>(); }
        if (record == null) return Array.Empty<DossierSection>();

        var asOfChapter = ParseChapterFromStoryPoint(storyPoint);
        var sections = new List<DossierSection>();

        var belongings = BuildBelongingsLines(record.Belongings);
        if (belongings.Count > 0) sections.Add(new DossierSection("belongings", belongings));

        if (record.CyberwareInventory.Count > 0)
        {
            var cyber = record.CyberwareInventory
                .Where(c => string.Equals(c.Condition, "functional", StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(c.Condition))
                .Select(c => $"{c.Name}{(string.IsNullOrEmpty(c.BodyLocation) ? "" : $" @{c.BodyLocation}")}{(string.IsNullOrEmpty(c.Manufacturer) ? "" : $" — {c.Manufacturer}")}")
                .ToList();
            if (cyber.Count > 0) sections.Add(new DossierSection("cyberware", cyber));
        }

        var knownAt = record.Knowledge
            .Where(k => !asOfChapter.HasValue || !k.LearnedChapter.HasValue || k.LearnedChapter.Value <= asOfChapter.Value)
            .Select(k => $"{k.Topic}{(string.IsNullOrEmpty(k.Summary) ? "" : $" — {k.Summary}")}{(k.LearnedChapter.HasValue ? $" [Ch{k.LearnedChapter}]" : "")}")
            .ToList();
        if (knownAt.Count > 0) sections.Add(new DossierSection("knowledge", knownAt));

        var conditionsActive = record.Conditions
            .Where(c => (!asOfChapter.HasValue || !c.SinceChapter.HasValue || c.SinceChapter.Value <= asOfChapter.Value)
                     && (!asOfChapter.HasValue || !c.UntilChapter.HasValue || c.UntilChapter.Value > asOfChapter.Value))
            .Select(c => $"{c.Kind}: {c.Name}{(string.IsNullOrEmpty(c.Severity) ? "" : $" ({c.Severity})")}{(string.IsNullOrEmpty(c.Notes) ? "" : $" — {c.Notes}")}")
            .ToList();
        if (conditionsActive.Count > 0) sections.Add(new DossierSection("conditions", conditionsActive));

        var psych = new List<string>();
        if (record.Psychology.CoreFears.Count > 0)   psych.Add("fears: "   + string.Join("; ", record.Psychology.CoreFears.Take(3)));
        if (record.Psychology.CoreDesires.Count > 0) psych.Add("desires: " + string.Join("; ", record.Psychology.CoreDesires.Take(3)));
        if (!string.IsNullOrWhiteSpace(record.Psychology.Secret)) psych.Add("secret: " + record.Psychology.Secret);
        if (psych.Count > 0) sections.Add(new DossierSection("psychology", psych));

        var behavior = new List<string>();
        if (record.Behavioral.DecisionRules.Count > 0)   behavior.AddRange(record.Behavioral.DecisionRules.Take(4).Select(r => "rule: "  + r));
        if (record.Behavioral.BreakingPoints.Count > 0)  behavior.AddRange(record.Behavioral.BreakingPoints.Take(3).Select(r => "break: " + r));
        if (record.Behavioral.Habits.Count > 0)          behavior.AddRange(record.Behavioral.Habits.Take(3).Select(r => "habit: " + r));
        if (behavior.Count > 0) sections.Add(new DossierSection("behavior", behavior));

        var speech = new List<string>();
        if (!string.IsNullOrWhiteSpace(record.SpeechPatterns.Cadence))    speech.Add("cadence: "    + record.SpeechPatterns.Cadence);
        if (record.SpeechPatterns.VerbalTics.Count > 0)                   speech.Add("tics: "       + string.Join(" | ", record.SpeechPatterns.VerbalTics.Take(3)));
        if (record.SpeechPatterns.ExampleLines.Count > 0)                 speech.AddRange(record.SpeechPatterns.ExampleLines.Take(2).Select(l => "ex: \"" + l + "\""));
        if (speech.Count > 0) sections.Add(new DossierSection("speech", speech));

        var relationships = record.Relationships
            .Where(r => !string.IsNullOrWhiteSpace(r.Name))
            .Where(r => !asOfChapter.HasValue || !r.SinceChapter.HasValue || r.SinceChapter.Value <= asOfChapter.Value)
            .Where(r => string.IsNullOrEmpty(r.Status) || !string.Equals(r.Status, "severed", StringComparison.OrdinalIgnoreCase) || (!r.UntilChapter.HasValue || (asOfChapter ?? int.MaxValue) < r.UntilChapter.Value))
            .Select(r => $"{r.Type}: {r.Name}{(string.IsNullOrEmpty(r.Status) || string.Equals(r.Status, "active", StringComparison.OrdinalIgnoreCase) ? "" : $" [{r.Status}]")}{(string.IsNullOrEmpty(r.EmotionalCore) ? "" : $" — {r.EmotionalCore}")}")
            .ToList();
        if (relationships.Count > 0) sections.Add(new DossierSection("relationships (rich)", relationships));

        return sections;
    }

    private static List<string> BuildBelongingsLines(CharacterBelongings b)
    {
        var lines = new List<string>();
        if (!string.IsNullOrWhiteSpace(b.PrimaryWeapon))   lines.Add("primary weapon: "   + b.PrimaryWeapon);
        if (!string.IsNullOrWhiteSpace(b.SecondaryWeapon)) lines.Add("secondary weapon: " + b.SecondaryWeapon);
        if (!string.IsNullOrWhiteSpace(b.Armor))           lines.Add("armor: "            + b.Armor);
        if (!string.IsNullOrWhiteSpace(b.Vehicle))         lines.Add("vehicle: "          + b.Vehicle);
        if (!string.IsNullOrWhiteSpace(b.Residence))       lines.Add("residence: "        + b.Residence);
        if (!string.IsNullOrWhiteSpace(b.ClothingStyle))   lines.Add("clothing: "         + b.ClothingStyle);
        if (!string.IsNullOrWhiteSpace(b.FavoriteDrink))   lines.Add("drinks: "           + b.FavoriteDrink);
        if (!string.IsNullOrWhiteSpace(b.FavoriteFood))    lines.Add("eats: "             + b.FavoriteFood);
        if (!string.IsNullOrWhiteSpace(b.Stimulant))       lines.Add("stimulant: "        + b.Stimulant);
        if (!string.IsNullOrWhiteSpace(b.CommDevice))      lines.Add("comm: "             + b.CommDevice);
        if (b.SignatureGear.Count > 0)                      lines.Add("signature gear: "   + string.Join(", ", b.SignatureGear));
        if (b.Pharmaceuticals.Count > 0)                    lines.Add("pharmaceuticals: "  + string.Join(", ", b.Pharmaceuticals));
        foreach (var kv in b.Other) lines.Add($"{kv.Key}: {kv.Value}");
        return lines;
    }

    private static int? ParseChapterFromStoryPoint(string sp)
    {
        if (string.IsNullOrEmpty(sp)) return null;
        if (sp.StartsWith("chapter:", StringComparison.OrdinalIgnoreCase)
            && int.TryParse(sp[8..], out var n)) return n;
        return null;
    }

    private static IReadOnlyDictionary<string, string> MaterializeProperties(UniverseNode node, string storyPoint)
    {
        var keys = new[]
        {
            "gender", "pronouns", "role", "status", "age", "affiliation", "location",
            "category", "manufacturer", "sector", "tier_availability", "legality",
            "description", "aliases"
        };
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var k in keys)
        {
            var v = string.IsNullOrEmpty(storyPoint)
                ? node.Properties.GetValueOrDefault(k, "")
                : node.GetPropertyAt(k, storyPoint);
            if (!string.IsNullOrWhiteSpace(v)) dict[k] = v;
        }
        return dict;
    }

    private IReadOnlyList<EdgeRef> GetEdgesForCard(string nodeId, string storyPoint)
    {
        var rawEdges = string.IsNullOrEmpty(storyPoint)
            ? graph.GetAllEdges(nodeId)
            : graph.GetEdgesAt(nodeId, storyPoint);

        var refs = new List<EdgeRef>(rawEdges.Count);
        foreach (var e in rawEdges)
        {
            var otherId = e.Source == nodeId ? e.Target : e.Source;
            var other = graph.GetNode(otherId);
            if (other == null) continue;
            refs.Add(new EdgeRef(
                OtherId:     otherId,
                OtherName:   other.Name,
                OtherKind:   other.NodeType,
                Direction:   e.Source == nodeId ? "→" : "←",
                Relation:    e.RelationType,
                Description: string.IsNullOrWhiteSpace(e.Description) ? null : e.Description,
                ValidFrom:   string.IsNullOrWhiteSpace(e.ValidFrom)   ? null : e.ValidFrom));
        }
        return refs;
    }

    private static string BuildOneLine(IReadOnlyDictionary<string, string> props)
    {
        if (props.TryGetValue("role", out var role) && !string.IsNullOrWhiteSpace(role))
            return Truncate(role, 120);
        if (props.TryGetValue("description", out var desc) && !string.IsNullOrWhiteSpace(desc))
            return Truncate(desc, 120);
        if (props.TryGetValue("category", out var cat) && !string.IsNullOrWhiteSpace(cat))
            return Truncate(cat, 120);
        return "";
    }

    /// <summary>
    /// Score 2-hop candidate nodes by weighted-path count through the subject's
    /// immediate neighborhood. Higher score = the candidate sits at the intersection
    /// of multiple of subject's connections, which is exactly what "adjacent" should
    /// mean in a prompt: don't drop random tourist nodes, surface the people/things
    /// that show up across the subject's life.
    /// </summary>
    private List<ScoredNode> ScoreAdjacents(
        UniverseNode subject,
        IReadOnlyList<EntityCard> linked,
        string storyPoint,
        HashSet<string> skipIds)
    {
        var scores = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        foreach (var l in linked)
        {
            var edges = string.IsNullOrEmpty(storyPoint)
                ? graph.GetAllEdges(l.Id)
                : graph.GetEdgesAt(l.Id, storyPoint);

            foreach (var e in edges)
            {
                var otherId = e.Source == l.Id ? e.Target : e.Source;
                if (skipIds.Contains(otherId)) continue;
                var weight = e.Weight > 0 ? e.Weight : 1.0;
                if (!scores.ContainsKey(otherId)) scores[otherId] = 0;
                scores[otherId] += weight;
            }
        }

        // Recency bonus: bump nodes that appear by name in the subject's recent beats.
        // Cheap pass — scan up to 8 most recent chapters/beats for token matches.
        var recencyBonus = ComputeRecencyBonus(subject.Name, storyPoint, scores.Keys);
        foreach (var (k, v) in recencyBonus)
            scores[k] = scores.GetValueOrDefault(k) + v;

        var ordered = new List<ScoredNode>(scores.Count);
        foreach (var (otherId, score) in scores)
        {
            var n = graph.GetNode(otherId);
            if (n == null) continue;
            ordered.Add(new ScoredNode(n, score));
        }
        return ordered;
    }

    private Dictionary<string, double> ComputeRecencyBonus(string subjectName, string storyPoint, IEnumerable<string> candidateIds)
    {
        var bonuses = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        try
        {
            var asOfChapter = ParseChapterFromStoryPoint(storyPoint);
            var recent = chapters.ListChapters()
                .OrderByDescending(c => c.Number ?? int.MinValue)
                .ThenByDescending(c => c.Created)
                .Take(8)
                .ToList();

            // Build a name→id lookup for the candidates.
            var idByName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var cid in candidateIds)
            {
                var n = graph.GetNode(cid);
                if (n == null || string.IsNullOrWhiteSpace(n.Name)) continue;
                idByName[n.Name] = cid;
            }

            foreach (var ch in recent)
            {
                if (asOfChapter.HasValue && ch.Number.HasValue && ch.Number.Value > asOfChapter.Value) continue;
                var hay = $"{ch.Title} {ch.Synopsis} {string.Concat(ch.Beats.Select(b => " " + b.Synopsis + " " + b.Text))}";
                if (hay.IndexOf(subjectName, StringComparison.OrdinalIgnoreCase) < 0) continue;

                foreach (var (name, cid) in idByName)
                {
                    if (name.Length < 3) continue;
                    if (hay.IndexOf(name, StringComparison.OrdinalIgnoreCase) < 0) continue;
                    bonuses[cid] = bonuses.GetValueOrDefault(cid) + 0.5;
                }
            }
        }
        catch (Exception ex) { log.LogDebug(ex, "Recency bonus pass failed"); }
        return bonuses;
    }

    private sealed record ScoredNode(UniverseNode Node, double Score);

    // ── Continuity facts ──────────────────────────────────────────────────────

    private List<DossierFact> LoadFacts(string entityId, AsOfCursor asOf, IReadOnlyDictionary<string, string> graphProps)
    {
        if (!continuity.IsAvailable) return new List<DossierFact>();

        List<ContinuityClaim> all;
        try { all = continuity.GetByEntity(entityId); }
        catch (Exception ex) { log.LogWarning(ex, "ContinuityService.GetByEntity failed for {Id}", entityId); return new(); }

        var asOfChapter = asOf.ChapterNumber;
        var facts = new List<DossierFact>();
        foreach (var c in all)
        {
            if (c.Status != "CANONICAL" && c.Status != "CONFIRMED") continue;
            if (asOfChapter.HasValue && c.SourceChapterNumber.HasValue && c.SourceChapterNumber.Value > asOfChapter.Value)
                continue; // claim came from a future chapter — must not leak into this dossier

            // Skip claims that are just restating a graph property the prompt already shows.
            if (graphProps.TryGetValue(c.Predicate, out var existing)
                && string.Equals(existing.Trim(), c.Object.Trim(), StringComparison.OrdinalIgnoreCase))
                continue;

            facts.Add(new DossierFact(
                Predicate:   c.Predicate,
                Object:      c.Object,
                Status:      c.Status,
                SourceLabel: BuildFactSource(c)));
        }
        return facts;
    }

    private static string BuildFactSource(ContinuityClaim c)
    {
        if (c.SourceChapterNumber is int n)
            return string.IsNullOrEmpty(c.SourceChapterTitle) ? $"Ch{n}" : $"Ch{n} \"{c.SourceChapterTitle}\"";
        if (!string.IsNullOrEmpty(c.SourceChapterId)) return c.SourceChapterId;
        if (!string.IsNullOrEmpty(c.SourcePath))      return Path.GetFileName(c.SourcePath);
        return c.SourceType ?? "";
    }

    // ── Timeline (chapter beats featuring the entity) ─────────────────────────

    private List<ChapterEvent> LoadTimeline(string entityName, CancellationToken ct, AsOfCursor asOf)
    {
        List<Chapter> all;
        try { all = chapters.ListChapters(); }
        catch (Exception ex) { log.LogWarning(ex, "ListChapters failed during timeline load"); return new(); }

        var events = new List<ChapterEvent>();
        var asOfNum = asOf.ChapterNumber;
        var asOfBeat = asOf.BeatIndex;
        var asOfChapterId = asOf.ChapterId;

        foreach (var chapter in all.OrderBy(c => c.Number ?? int.MaxValue).ThenBy(c => c.Created))
        {
            if (ct.IsCancellationRequested) break;
            if (asOfNum.HasValue && chapter.Number.HasValue && chapter.Number.Value > asOfNum.Value)
                break;

            foreach (var beat in chapter.Beats.OrderBy(b => b.Index))
            {
                if (asOfNum.HasValue && chapter.Number == asOfNum && asOfBeat.HasValue && beat.Index > asOfBeat.Value
                    && string.Equals(chapter.Id, asOfChapterId, StringComparison.OrdinalIgnoreCase))
                    break;

                var hay = string.Concat(beat.Text, " ", beat.Synopsis, " ", beat.Title);
                if (string.IsNullOrWhiteSpace(hay)) continue;
                if (hay.IndexOf(entityName, StringComparison.OrdinalIgnoreCase) < 0) continue;

                events.Add(new ChapterEvent(
                    ChapterId:     chapter.Id,
                    ChapterNumber: chapter.Number,
                    ChapterTitle:  chapter.Title,
                    BeatIndex:     beat.Index,
                    Snippet:       Truncate(string.IsNullOrWhiteSpace(beat.Synopsis) ? beat.Text : beat.Synopsis, SnippetMaxChars)));

                if (events.Count >= MaxTimelineBeats) return events;
            }
        }

        return events;
    }

    // ── Derived state ─────────────────────────────────────────────────────────

    private static DerivedState DeriveState(EntityCard subject, IReadOnlyList<ChapterEvent> timeline)
    {
        var location = subject.Properties.GetValueOrDefault("location", "");
        var status   = subject.Properties.GetValueOrDefault("status", "");

        var holding = subject.Edges
            .Where(e => HoldingRelations.Contains(e.Relation) && e.Direction == "→")
            .Select(e => e.OtherName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var wearing = subject.Edges
            .Where(e => WearingRelations.Contains(e.Relation) && e.Direction == "→")
            .Select(e => e.OtherName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        string? lastSeen = null;
        if (timeline.Count > 0)
        {
            var t = timeline[^1];
            lastSeen = t.ChapterNumber.HasValue
                ? $"Ch{t.ChapterNumber} §{t.BeatIndex} \"{t.ChapterTitle}\""
                : $"§{t.BeatIndex} \"{t.ChapterTitle}\"";
        }

        return new DerivedState(
            Location:         string.IsNullOrWhiteSpace(location) ? null : location,
            Status:           string.IsNullOrWhiteSpace(status)   ? null : status,
            Holding:          holding,
            Wearing:          wearing,
            LastSeenChapter:  lastSeen);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static string Truncate(string s, int n) => s.Length <= n ? s : s[..(n - 1)] + "…";
}
