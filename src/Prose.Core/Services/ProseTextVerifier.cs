using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using static Prose.Core.Services.ProseTextService;

namespace Prose.Core.Services;

/// <summary>
/// RFC 0018 (PRS-T922) — the narrative instruments, on game texts (<c>prose-text/1</c>).
///
/// <para>Books are checked beat by beat (<see cref="CanonContradictionService"/>, <see cref="ContinuityEnforcer"/>,
/// the obligation ledger, <see cref="VoiceFingerprintAnalyzer"/>). A game text is not a beat: a conversation is a
/// graph, a bark pool a random draw, a description a codex entry shown any time, and a game has no single reading
/// order. This service runs the game-mode version of each instrument over a universe's texts and entities and files
/// what it finds in the shared <see cref="FindingsService"/> sink, at <c>text:{universe}/{id}#v{version}</c>.</para>
///
/// <para><b>Deterministic only</b> (protocol Law 7): no model call, so it runs on every draft and approval without a
/// cost gate. <b>Describes, never writes</b> (RFC 0009): the only rows it writes are findings, every finding's
/// SuggestedFix is null, and a finding never refuses a draft or an approval. The author decides.</para>
///
/// <para>Families and codes (each registered in <see cref="FindingCodeRegistry"/>):
/// <list type="bullet">
/// <item><c>TEXT-CANON</c> [retired-entity] [missing-entity] [glossary-conflict] [unknown-giver]</item>
/// <item><c>TEXT-CONTINUITY</c> [before-introduction] [unknown-after] [after-order] [place-type]</item>
/// <item><c>TEXT-OBLIGATION</c> [quest-unresolved] [undescribed]</item>
/// <item><c>TEXT-VOICE</c> [drift]</item>
/// <item><c>TEXT-BRANCH</c> [unreachable] [no-ending] [duplicate-choice]</item>
/// <item><c>TEXT-BARK</c> [duplicate] [repeated] [long]</item>
/// </list></para>
/// </summary>
public class ProseTextVerifier
{
    /// <summary>Stamped on every finding (RFC 0011 brick 2), so a rule change can list what is stale.</summary>
    public const string RuleVersion = "prose-text-verify/1";

    /// <summary>Every finding summary this service files starts with this; a re-run clears its own open rows by it.</summary>
    public const string SummaryPrefix = "TEXT-";

    public const int MaxBarkChars = 140;

    private static readonly string[] PlaceTypeWords =
        ["place", "location", "region", "area", "planet", "system", "station", "stage", "city", "zone", "sector", "world", "district"];

    private static readonly Regex EntityToken = new(@"\{entity:[^}]*\}", RegexOptions.Compiled);
    private static readonly Regex PlaceholderToken = new(@"\$[A-Za-z0-9_]+", RegexOptions.Compiled);
    private static readonly Regex CapitalWord = new(@"\p{Lu}[\p{L}\p{N}'’\-]*", RegexOptions.Compiled);
    private static readonly Regex NonWord = new(@"[^\p{L}\p{N}]+", RegexOptions.Compiled);

    private readonly IDbContextFactory<ProseDbContext> dbFactory;
    private readonly FindingsService findings;

    public ProseTextVerifier(IDbContextFactory<ProseDbContext> dbFactory, FindingsService findings)
    {
        this.dbFactory = dbFactory;
        this.findings = findings;
    }

    // ── Shapes ─────────────────────────────────────────────────────────────

    /// <summary>One thing a verifier observed on one version of one text.</summary>
    public sealed record TextFinding(
        string TextId, int Version, string Family, string Code,
        [property: JsonConverter(typeof(JsonStringEnumConverter))] FindingCategory Category,
        [property: JsonConverter(typeof(JsonStringEnumConverter))] FindingSeverity Severity,
        string Summary, string? Snippet);

    public sealed class VerifyResult
    {
        public string Universe { get; set; } = "";
        public int TextsChecked { get; set; }
        public int VersionsChecked { get; set; }
        public List<TextFinding> Findings { get; set; } = new();
        public List<string> Errors { get; set; } = new();
        public bool Success => Errors.Count == 0;
    }

    /// <summary>The finding path of one version of a text.</summary>
    public static string PathFor(string universe, string id, int version) => $"text:{universe}/{id}#v{version}";

    /// <summary>The finding path of a text (every version's findings are under it).</summary>
    public static string PathFor(string universe, string id) => $"text:{universe}/{id}";

    /// <summary>The finding path prefix of a whole universe's texts.</summary>
    public static string PathForUniverse(string universe) => $"text:{universe}/";

    /// <summary>Reads (universe, id, version) back out of a text finding's path; null for any other path.</summary>
    public static (string Universe, string Id, int? Version)? ParsePath(string filePath)
    {
        if (!filePath.StartsWith("text:", StringComparison.Ordinal)) return null;
        var rest = filePath[5..];
        var slash = rest.IndexOf('/');
        if (slash <= 0) return null;
        var universe = rest[..slash];
        var tail = rest[(slash + 1)..];
        var hash = tail.IndexOf("#v", StringComparison.Ordinal);
        if (hash < 0) return (universe, tail, null);
        return (universe, tail[..hash], int.TryParse(tail[(hash + 2)..], out var v) ? v : null);
    }

    // ── Entry points ───────────────────────────────────────────────────────

    /// <summary>
    /// Verify texts of a universe and file the findings: the given ids, else every text (optionally of one kind).
    /// Each text's pending draft and its live version are checked (retired texts are skipped: they are out of every
    /// pull). For each version checked, its open <c>TEXT-</c> findings are cleared and the current set filed; rows
    /// the author already triaged or dismissed are kept and stay ruled on when found again.
    /// <paramref name="file"/> false computes without writing (used by previews).
    /// </summary>
    public async Task<VerifyResult> VerifyAsync(string universeSlug, IReadOnlyCollection<string>? ids = null,
        string? kind = null, bool file = true, CancellationToken ct = default)
    {
        var result = new VerifyResult { Universe = (universeSlug ?? "").Trim().ToLowerInvariant() };
        var corpus = await LoadCorpusAsync(result.Universe, result.Errors, ct);
        if (corpus == null) return result;

        var wanted = ids is { Count: > 0 } ? ids.ToHashSet(StringComparer.Ordinal) : null;
        if (wanted != null)
            foreach (var missing in wanted.Where(w => !corpus.TextById.ContainsKey(w)))
                result.Errors.Add($"no text '{missing}'");

        var targets = corpus.TextById.Values
            .Where(r => !r.Retired)
            .Where(r => wanted == null || wanted.Contains(r.Id))
            .Where(r => string.IsNullOrWhiteSpace(kind) || string.Equals(r.Kind, kind.Trim(), StringComparison.OrdinalIgnoreCase))
            .OrderBy(r => r.Id, StringComparer.Ordinal)
            .ToList();

        foreach (var record in targets)
        {
            ct.ThrowIfCancellationRequested();
            result.TextsChecked++;
            foreach (var version in new[] { record.Draft, record.Live }.Where(v => v != null).Distinct())
            {
                List<TextFinding> found;
                try { found = Analyze(record, version!, corpus); }
                catch (Exception ex)
                {
                    // Never purge on a failed evaluation: the prior findings stay, and the failure is reported.
                    result.Errors.Add($"{record.Id} v{version!.Version}: verification failed ({ex.Message})");
                    continue;
                }
                result.VersionsChecked++;
                result.Findings.AddRange(found);
                if (file) File(result.Universe, record.Id, version!.Version, found);
            }
        }
        return result;
    }

    /// <summary>The findings filed on a universe's texts, or on one text (every version), newest first within severity.</summary>
    public IReadOnlyList<Finding> ListFindings(string universeSlug, string? id = null, FindingStatus? status = null, int limit = 500)
    {
        var slug = (universeSlug ?? "").Trim().ToLowerInvariant();
        var prefix = string.IsNullOrWhiteSpace(id) ? PathForUniverse(slug) : PathFor(slug, id.Trim());
        return findings.List(status, limit, prefix);
    }

    /// <summary>A filed text finding as the HTTP, CLI, MCP and Writer surfaces show it (enums as names, the text id and
    /// version read back out of the path).</summary>
    public sealed record FindingRow(long Id, string TextId, int? Version, string Category, string Severity, string Status,
        string Summary, string? Snippet, DateTime DetectedAt, DateTime? ResolvedAt);

    public IReadOnlyList<FindingRow> ListFindingRows(string universeSlug, string? id = null, FindingStatus? status = null, int limit = 500) =>
        ListFindings(universeSlug, id, status, limit).Select(ToRow).ToList();

    /// <summary>The findings in any of <paramref name="statuses"/> (null: every status), most severe and newest first.</summary>
    public IReadOnlyList<FindingRow> ListFindingRows(string universeSlug, string? id, IReadOnlyCollection<FindingStatus>? statuses, int limit = 500)
    {
        if (statuses == null) return ListFindingRows(universeSlug, id, (FindingStatus?)null, limit);
        if (statuses.Count == 1) return ListFindingRows(universeSlug, id, statuses.First(), limit);
        return statuses.Distinct()
            .SelectMany(s => ListFindings(universeSlug, id, s, limit))
            .OrderByDescending(f => f.Severity).ThenByDescending(f => f.DetectedAt)
            .Take(limit)
            .Select(ToRow).ToList();
    }

    /// <summary>The statuses a findings filter names, for the HTTP, MCP and CLI surfaces (PRS-T926: <c>open</c> was
    /// refused with a 400). <c>new</c>, <c>triaged</c>, <c>applied</c>, <c>dismissed</c> are one status each;
    /// <c>open</c> is the ones still to rule on (new and triaged); <c>all</c> or none is every status (null).
    /// False, with the error, for any other word.</summary>
    public static bool TryParseStatusFilter(string? status, out IReadOnlyCollection<FindingStatus>? statuses, out string? error)
    {
        statuses = null;
        error = null;
        var s = (status ?? "").Trim();
        if (s.Length == 0 || s.Equals("all", StringComparison.OrdinalIgnoreCase)) return true;
        if (s.Equals("open", StringComparison.OrdinalIgnoreCase))
        {
            statuses = [FindingStatus.New, FindingStatus.Triaged];
            return true;
        }
        if (Enum.TryParse<FindingStatus>(s, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed))
        {
            statuses = [parsed];
            return true;
        }
        error = $"unknown status '{status}' (open, new, triaged, applied, dismissed, all)";
        return false;
    }

    public static FindingRow ToRow(Finding f)
    {
        var at = ParsePath(f.FilePath);
        return new FindingRow(f.Id, at?.Id ?? f.FilePath, at?.Version, f.Category.ToString(), f.Severity.ToString(),
            f.Status.ToString(), f.Summary, f.Snippet, f.DetectedAt, f.ResolvedAt);
    }

    private void File(string universe, string id, int version, List<TextFinding> found)
    {
        var path = PathFor(universe, id, version);
        findings.DeleteBySummaryPrefix(path, SummaryPrefix);
        foreach (var f in found)
            findings.Upsert(path, chapterId: null, f.Category, f.Severity, f.Summary, f.Snippet, suggestedFix: null,
                sourceRuleVersion: RuleVersion);
    }

    // ── The corpus: one universe's entities and texts, loaded once ─────────

    internal sealed record EntityInfo(Guid Id, string Slug, string Name, string Type, string Status)
    {
        public bool Live => Status is not ("archived" or "stub");
        public bool Archived => Status == "archived";
    }

    internal sealed class Corpus
    {
        public string Universe = "";
        public Dictionary<string, List<EntityInfo>> BySlug = new(StringComparer.Ordinal);
        public Dictionary<string, List<EntityInfo>> ByName = new(StringComparer.Ordinal);
        public Dictionary<string, List<string>> NamesByFirstWord = new(StringComparer.Ordinal);
        /// <summary>Every text, retired ones included (an <c>after</c> may name a retired text).</summary>
        public Dictionary<string, TextRecord> TextById = new(StringComparer.Ordinal);
        /// <summary>Each live (not retired) text's current version: its pending draft, else its live version.</summary>
        public Dictionary<string, TextVersion> Current = new(StringComparer.Ordinal);
        /// <summary>The universe's declared <c>$outcome</c> node names (PRS-T926); empty when none are declared.</summary>
        public HashSet<string> OutcomeNodes = new(StringComparer.Ordinal);

        public static Corpus Build(string universe, IEnumerable<EntityInfo> entities, IEnumerable<TextRecord> texts)
        {
            var c = new Corpus { Universe = universe };
            foreach (var e in entities)
            {
                Add(c.BySlug, e.Slug, e);
                if (string.IsNullOrWhiteSpace(e.Name)) continue;
                var name = e.Name.Trim();
                // Names worth scanning lines for: capitalised and long enough not to be noise.
                if (name.Length < 4 || !char.IsUpper(name[0])) continue;
                Add(c.ByName, name, e);
                var first = name.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
                if (!c.NamesByFirstWord.TryGetValue(first, out var list)) c.NamesByFirstWord[first] = list = new();
                if (!list.Contains(name)) list.Add(name);
            }
            foreach (var list in c.NamesByFirstWord.Values) list.Sort((a, b) => b.Length.CompareTo(a.Length)); // longest first
            foreach (var t in texts)
            {
                c.TextById[t.Id] = t;
                if (t.Retired) continue;
                var current = t.Draft ?? t.Live;
                if (current != null) c.Current[t.Id] = current;
            }
            return c;
        }

        private static void Add<T>(Dictionary<string, List<T>> d, string key, T value)
        {
            if (!d.TryGetValue(key, out var list)) d[key] = list = new();
            list.Add(value);
        }

        public bool IsLiveSlug(string slug) => BySlug.TryGetValue(slug, out var l) && l.Any(e => e.Live);
        public bool IsLiveName(string name) => ByName.TryGetValue(name, out var l) && l.Any(e => e.Live);
        public EntityInfo? LiveEntity(string slug) => BySlug.TryGetValue(slug, out var l) ? l.FirstOrDefault(e => e.Live) : null;

        public IEnumerable<(string Id, TextRecord Record, TextVersion Version)> Others(string selfId) =>
            Current.Where(kv => kv.Key != selfId).Select(kv => (kv.Key, TextById[kv.Key], kv.Value));
    }

    private async Task<Corpus?> LoadCorpusAsync(string slug, List<string> errors, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var universe = await db.Universes.FirstOrDefaultAsync(u => u.Slug == slug, ct);
        if (universe == null) { errors.Add($"unknown universe '{slug}'"); return null; }

        var entities = await db.Entities.IgnoreQueryFilters().AsNoTracking()
            .Where(e => e.UniverseId == universe.Id && e.EntityType != ProseTextService.EntityType)
            .Select(e => new { e.Id, e.Slug, e.Name, e.EntityType, e.Status })
            .ToListAsync(ct);
        var texts = await LoadAllAsync(db, universe.Id, ct);
        var corpus = Corpus.Build(universe.Slug,
            entities.Where(e => !string.IsNullOrEmpty(e.Slug))
                .Select(e => new EntityInfo(e.Id, e.Slug!, e.Name ?? "", e.EntityType ?? "", e.Status ?? "")),
            texts.Select(t => t.Record));
        corpus.OutcomeNodes = await ReadOutcomeNodesAsync(db, universe.Id, ct);
        return corpus;
    }

    // ── Declared $outcome node names (PRS-T926) ────────────────────────────

    /// <summary>The per-universe Settings row holding the names of the conversation nodes a game re-enters after a
    /// <c>$outcome</c> exit, as a JSON array of strings. Written by <see cref="SetOutcomeNodesAsync"/> (the HTTP
    /// <c>PUT /api/universes/{slug}/texts/outcome-nodes</c>, and the consumer kit's <c>push</c> from the config's
    /// <c>outcomeNodes</c>).</summary>
    public const string OutcomeNodesSettingKey = "texts.outcome-nodes";

    private static async Task<HashSet<string>> ReadOutcomeNodesAsync(ProseDbContext db, Guid universeId, CancellationToken ct)
    {
        var json = await db.Settings.IgnoreQueryFilters().AsNoTracking()
            .Where(s => s.Key == OutcomeNodesSettingKey && s.UniverseId == universeId)
            .Select(s => s.Json).FirstOrDefaultAsync(ct);
        return ParseOutcomeNodes(json);
    }

    internal static HashSet<string> ParseOutcomeNodes(string? json)
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        if (string.IsNullOrWhiteSpace(json)) return set;
        try
        {
            foreach (var n in JsonSerializer.Deserialize<List<string>>(json) ?? [])
                if (!string.IsNullOrWhiteSpace(n)) set.Add(n.Trim());
        }
        catch (JsonException) { /* an unreadable row declares nothing */ }
        return set;
    }

    /// <summary>The universe's declared outcome node names, sorted.</summary>
    public async Task<List<string>> GetOutcomeNodesAsync(string universeSlug, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var slug = (universeSlug ?? "").Trim().ToLowerInvariant();
        var universe = await db.Universes.FirstOrDefaultAsync(u => u.Slug == slug, ct)
            ?? throw new InvalidOperationException($"unknown universe '{slug}'");
        return (await ReadOutcomeNodesAsync(db, universe.Id, ct)).OrderBy(n => n, StringComparer.Ordinal).ToList();
    }

    /// <summary>Replace the universe's declared outcome node names (an empty list clears them). Names are node ids:
    /// no <c>$</c>, no whitespace. Returns the stored, sorted list. Re-verify the texts to refresh their findings.</summary>
    public async Task<List<string>> SetOutcomeNodesAsync(string universeSlug, IEnumerable<string>? names, CancellationToken ct = default)
    {
        var clean = (names ?? []).Select(n => (n ?? "").Trim()).Where(n => n.Length > 0)
            .Distinct(StringComparer.Ordinal).OrderBy(n => n, StringComparer.Ordinal).ToList();
        var bad = clean.Where(n => n.StartsWith('$') || n.Any(char.IsWhiteSpace)).ToList();
        if (bad.Count > 0)
            throw new InvalidOperationException($"not a node name: {string.Join(", ", bad.Select(b => $"'{b}'"))}");

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var slug = (universeSlug ?? "").Trim().ToLowerInvariant();
        var universe = await db.Universes.FirstOrDefaultAsync(u => u.Slug == slug, ct)
            ?? throw new InvalidOperationException($"unknown universe '{slug}'");
        var row = await db.Settings.IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.Key == OutcomeNodesSettingKey && s.UniverseId == universe.Id, ct);
        if (clean.Count == 0)
        {
            if (row != null) db.Settings.Remove(row);
        }
        else
        {
            var json = JsonSerializer.Serialize(clean);
            if (row == null)
                db.Settings.Add(new Data.Entities.Setting { Key = OutcomeNodesSettingKey, Json = json, UniverseId = universe.Id, UpdatedAt = DateTime.UtcNow });
            else { row.Json = json; row.UpdatedAt = DateTime.UtcNow; }
        }
        await db.SaveChangesAsync(ct);
        return clean;
    }

    // ── The analysis (pure: corpus in, findings out) ───────────────────────

    internal static List<TextFinding> Analyze(TextRecord record, TextVersion v, Corpus corpus)
    {
        var sink = new Sink(record.Id, v.Version);
        var strings = ProseStrings(record.Kind, v.Payload).ToList();

        CheckCanon(record, v, corpus, strings, sink);
        CheckContinuity(record, v, corpus, strings, sink);
        CheckObligations(record, v, corpus, sink);
        CheckVoice(record, v, corpus, sink);
        if (record.Kind == "conversation") CheckBranches(v, sink, corpus.OutcomeNodes);
        if (record.Kind == "bark") CheckBarks(record, v, corpus, sink);
        return sink.Found;
    }

    internal sealed class Sink(string id, int version)
    {
        public readonly List<TextFinding> Found = new();
        private readonly HashSet<string> seen = new(StringComparer.Ordinal);

        public void Add(string family, string code, FindingCategory category, FindingSeverity severity, string what, string? snippet = null)
        {
            var summary = $"TEXT-{family} [{code}]: {what}";
            if (seen.Add(summary))
                Found.Add(new TextFinding(id, version, family, code, category, severity, summary, Clip(snippet, 300)));
        }
    }

    // ── Canon ──────────────────────────────────────────────────────────────

    private static void CheckCanon(TextRecord record, TextVersion v, Corpus corpus,
        List<(string Where, string Text, string? Speaker)> strings, Sink sink)
    {
        // Referenced by id: about, entity speakers, {entity:id}, context.place.
        foreach (var slug in ReferencesOf(v))
        {
            if (corpus.IsLiveSlug(slug)) continue;
            if (corpus.BySlug.TryGetValue(slug, out var rows) && rows.Any(e => e.Archived))
                sink.Add("CANON", "retired-entity", FindingCategory.Contradiction, FindingSeverity.High,
                    $"refers to {slug} ({rows.First().Name}), which is retired in canon");
            else
                sink.Add("CANON", "missing-entity", FindingCategory.Contradiction, FindingSeverity.High,
                    $"refers to {slug}, which is not a live entity of '{corpus.Universe}'");
        }

        // Named in a line: a name only retired entities carry.
        foreach (var (where, text, _) in strings)
            foreach (var name in NamesIn(text, corpus))
                if (!corpus.IsLiveName(name) && corpus.ByName[name].Any(e => e.Archived))
                    sink.Add("CANON", "retired-entity", FindingCategory.Contradiction, FindingSeverity.Medium,
                        $"names \"{name}\" in {where}; every entity of that name is retired in canon", text);

        if (record.Kind == "glossary")
        {
            var terms = GlossaryTerms(v.Payload);
            foreach (var (otherId, other, ov) in corpus.Others(record.Id).Where(o => o.Record.Kind == "glossary"))
            {
                var shared = terms.Intersect(GlossaryTerms(ov.Payload)).OrderBy(t => t, StringComparer.Ordinal).ToList();
                if (shared.Count > 0)
                    sink.Add("CANON", "glossary-conflict", FindingCategory.Contradiction, FindingSeverity.Medium,
                        $"defines \"{string.Join("\", \"", shared)}\", also defined by {otherId}",
                        Str(ov.Payload, "definition"));
            }
        }

        if (record.Kind == "quest" && Str(v.Payload, "giver") is { } giver && giver.Contains('-')
            && IsStableId(giver) && !corpus.BySlug.ContainsKey(giver))
            sink.Add("CANON", "unknown-giver", FindingCategory.Contradiction, FindingSeverity.Medium,
                $"quest giver '{giver}' is written as an entity id but names no entity");
    }

    // ── Continuity: what the player can know at this text ──────────────────

    private static void CheckContinuity(TextRecord record, TextVersion v, Corpus corpus,
        List<(string Where, string Text, string? Speaker)> strings, Sink sink)
    {
        var ctx = v.Context;
        if (ctx == null) return;

        if (ctx.Place is { } place && corpus.LiveEntity(place) is { } placeEntity
            && !PlaceTypeWords.Any(w => placeEntity.Type.Contains(w, StringComparison.OrdinalIgnoreCase)))
            sink.Add("CONTINUITY", "place-type", FindingCategory.Contradiction, FindingSeverity.Low,
                $"context.place {place} is a {placeEntity.Type}, not a place");

        foreach (var a in ctx.After ?? [])
        {
            if (!corpus.TextById.ContainsKey(a))
                sink.Add("CONTINUITY", "unknown-after", FindingCategory.Contradiction, FindingSeverity.Medium,
                    $"context.after names {a}, which is not a text of '{corpus.Universe}'");
            else if (ctx.Order is { } order && corpus.Current.TryGetValue(a, out var av) && av.Context?.Order is { } ao && ao > order)
                sink.Add("CONTINUITY", "after-order", FindingCategory.Contradiction, FindingSeverity.Medium,
                    $"context.after names {a} (order {Num(ao)}), which is later than this text's order {Num(order)}");
        }

        var (known, effective) = KnownBefore(record.Id, ctx, corpus);
        if (effective == null && known.Count == 0) return; // unplaced: nothing to judge against

        // What this text relies on the player knowing: its entity speakers, {entity:id} references and the live
        // entities it names. A text that is ABOUT an entity introduces it; the place is where the player stands.
        var mentions = new List<(string Label, HashSet<string> Slugs, string? Snippet)>();
        foreach (var s in v.Speakers.Where(s => !IsPlaceholder(s) && corpus.IsLiveSlug(s)))
            mentions.Add((s, new HashSet<string> { s }, null));
        foreach (var r in CollectEntityRefs(v.Payload).Distinct().Where(corpus.IsLiveSlug))
            mentions.Add((r, new HashSet<string> { r }, null));
        foreach (var (_, text, _) in strings)
            foreach (var name in NamesIn(text, corpus).Where(corpus.IsLiveName))
                mentions.Add(($"\"{name}\"", corpus.ByName[name].Where(e => e.Live).Select(e => e.Slug).ToHashSet(), text));

        foreach (var (label, slugs, snippet) in mentions)
        {
            if (slugs.Overlaps(v.About) || (ctx.Place != null && slugs.Contains(ctx.Place))) continue;
            var intros = corpus.Others(record.Id)
                .Where(o => o.Version.About.Any(slugs.Contains))
                .Where(o => o.Version.Context?.Order != null || known.Contains(o.Id))
                .ToList();
            if (intros.Count == 0) continue; // never introduced at a placed point: not judged
            if (intros.Any(o => known.Contains(o.Id))) continue;
            var ordered = intros.Where(o => o.Version.Context?.Order != null).ToList();
            if (ordered.Count == 0) continue;
            var first = ordered.MinBy(o => o.Version.Context!.Order!.Value);
            var target = string.Join(", ", slugs.OrderBy(s => s, StringComparer.Ordinal));
            sink.Add("CONTINUITY", "before-introduction", FindingCategory.Contradiction, FindingSeverity.Medium,
                $"names {label} ({target}) at order {Num(effective)}; the first text about it is {first.Id} at order {Num(first.Version.Context!.Order!.Value)}",
                snippet);
        }
    }

    /// <summary>
    /// RFC 0018 §4.2: the texts the player is guaranteed to have been able to see when this text plays, and the
    /// text's effective order. The <c>after</c> closure is always known; a text cannot play before the texts it
    /// comes after, so its effective order is the larger of its own order and theirs; and every other text placed
    /// at or before that order is known too.
    /// </summary>
    internal static (HashSet<string> Known, double? Effective) KnownBefore(string selfId, TextContext ctx, Corpus corpus)
    {
        var known = new HashSet<string>(StringComparer.Ordinal);
        var stack = new Stack<string>(ctx.After ?? []);
        double? effective = ctx.Order;
        while (stack.Count > 0)
        {
            var a = stack.Pop();
            if (a == selfId || !known.Add(a)) continue;
            if (!corpus.Current.TryGetValue(a, out var av) || av.Context == null) continue;
            if (av.Context.Order is { } ao) effective = effective == null ? ao : Math.Max(effective.Value, ao);
            foreach (var next in av.Context.After ?? []) stack.Push(next);
        }
        if (effective is { } eff)
            foreach (var (id, _, ov) in corpus.Others(selfId))
                if (ov.Context?.Order is { } oo && oo <= eff) known.Add(id);
        return (known, effective);
    }

    // ── Obligations (RFC 0013, the text holds both ends) ───────────────────

    private static readonly HashSet<string> StoryKinds = ["conversation", "crawl", "quest", "book", "bark"];

    private static void CheckObligations(TextRecord record, TextVersion v, Corpus corpus, Sink sink)
    {
        if (record.Kind == "quest" && Str(v.Payload, "complete") == null)
            sink.Add("OBLIGATION", "quest-unresolved", FindingCategory.NarrativeObligation, FindingSeverity.Low,
                "the quest's brief makes a promise and the quest has no complete text to pay it off",
                Str(v.Payload, "brief"));

        if (!StoryKinds.Contains(record.Kind)) return;
        var referents = v.About.Concat(CollectEntityRefs(v.Payload)).Distinct().Where(corpus.IsLiveSlug).ToList();
        if (referents.Count == 0) return;
        var described = corpus.Current
            .Where(kv => corpus.TextById[kv.Key].Kind == "description")
            .SelectMany(kv => kv.Value.About).ToHashSet(StringComparer.Ordinal);
        var missing = referents.Where(r => !described.Contains(r)).OrderBy(r => r, StringComparer.Ordinal).ToList();
        if (missing.Count > 0)
            sink.Add("OBLIGATION", "undescribed", FindingCategory.NarrativeObligation, FindingSeverity.Low,
                $"names {string.Join(", ", missing)} with no description text the player can look up");
    }

    // ── Voice, per speaker ─────────────────────────────────────────────────

    private static void CheckVoice(TextRecord record, TextVersion v, Corpus corpus, Sink sink)
    {
        var mine = SpeakerLines(record.Kind, v.Payload)
            .Where(x => !IsPlaceholder(x.Speaker) && corpus.LiveEntity(x.Speaker) != null)
            .GroupBy(x => x.Speaker, x => x.Line).ToList();
        if (mine.Count == 0) return;

        // Each entity speaker's established vocabulary, from every other text of the universe.
        var lines = new Dictionary<string, StringBuilder>(StringComparer.Ordinal);
        foreach (var (_, other, ov) in corpus.Others(record.Id))
            foreach (var (speaker, line) in SpeakerLines(other.Kind, ov.Payload))
            {
                if (IsPlaceholder(speaker) || corpus.LiveEntity(speaker) == null) continue;
                if (!lines.TryGetValue(speaker, out var sb)) lines[speaker] = sb = new StringBuilder();
                sb.Append(' ').Append(line);
            }
        var fingerprints = lines.ToDictionary(
            kv => corpus.LiveEntity(kv.Key)!.Id,
            kv => (corpus.LiveEntity(kv.Key)!.Name, VoiceFingerprintAnalyzer.DistinctiveTokens(kv.Value.ToString())));

        foreach (var group in mine)
        {
            var entity = corpus.LiveEntity(group.Key)!;
            var test = VoiceFingerprintAnalyzer.DistinctiveTokens(string.Join(" ", group));
            var check = VoiceFingerprintAnalyzer.CheckDrift(test, entity.Id, fingerprints);
            if (check is { Drifted: true } c)
                sink.Add("VOICE", "drift", FindingCategory.Voice, FindingSeverity.Medium,
                    $"{entity.Name}'s lines here read closer to {c.TopMatchName}'s established voice ({c.TopMatchScore:0.000}) than to {entity.Name}'s own ({c.OwnScore:0.000})",
                    group.First());
        }
    }

    // ── Branching conversations ────────────────────────────────────────────

    /// <summary>The token a choice or node <c>next</c> uses to hand the talk to the game, which comes back into the
    /// conversation at the node named after what the action came to (SpaceGod: <c>talked</c>, <c>fought</c>, …;
    /// GridGame: <c>join</c>, <c>part</c>). See <see cref="OutcomeNodesSettingKey"/>.</summary>
    public const string OutcomeToken = "$outcome";

    /// <param name="outcomeNodes">The universe's declared outcome node names (PRS-T926). When a reachable node exits
    /// to <see cref="OutcomeToken"/>, every node of the text with one of these names is reachable too: the game
    /// re-enters the talk there. Null or empty: <c>$outcome</c> only ends the talk, as before.</param>
    internal static void CheckBranches(TextVersion v, Sink sink, IReadOnlySet<string>? outcomeNodes = null)
    {
        var p = v.Payload;
        if (p.ValueKind != JsonValueKind.Object || !p.TryGetProperty("nodes", out var nodes) || nodes.ValueKind != JsonValueKind.Array) return;
        var start = Str(p, "start");
        var graph = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        var order = new List<string>();
        foreach (var n in nodes.EnumerateArray())
        {
            if (Str(n, "id") is not { } id || graph.ContainsKey(id)) continue;
            order.Add(id);
            var targets = new List<string>();
            if (Str(n, "next") is { } next) targets.Add(next);
            if (n.TryGetProperty("choices", out var choices) && choices.ValueKind == JsonValueKind.Array)
            {
                var texts = new HashSet<string>(StringComparer.Ordinal);
                foreach (var ch in choices.EnumerateArray())
                {
                    if (Str(ch, "next") is { } cn) targets.Add(cn);
                    if (Str(ch, "text") is { } ct && !texts.Add(Normalize(ct)))
                        sink.Add("BRANCH", "duplicate-choice", FindingCategory.StructuralFailure, FindingSeverity.Low,
                            $"node '{id}' offers the choice \"{Clip(ct, 80)}\" twice", ct);
                }
            }
            graph[id] = targets;
        }
        if (start == null || !graph.ContainsKey(start)) return; // drafting already refuses a bad start

        var reached = new HashSet<string>(StringComparer.Ordinal);
        var queue = new Queue<string>([start]);
        var exits = false;
        while (queue.Count > 0)
        {
            var id = queue.Dequeue();
            if (!reached.Add(id)) continue;
            var targets = graph[id];
            if (targets.Count == 0) exits = true; // an ending
            foreach (var t in targets)
            {
                if (t == OutcomeToken && outcomeNodes is { Count: > 0 })
                    foreach (var o in outcomeNodes)
                        if (graph.ContainsKey(o)) queue.Enqueue(o); // the game comes back at the outcome's node
                if (t.StartsWith('$')) { exits = true; continue; } // the game takes over
                if (graph.ContainsKey(t)) queue.Enqueue(t);
            }
        }

        var unreachable = order.Where(id => !reached.Contains(id)).ToList();
        if (unreachable.Count > 0)
            sink.Add("BRANCH", "unreachable", FindingCategory.StructuralFailure, FindingSeverity.Medium,
                $"no path from '{start}' reaches {(unreachable.Count == 1 ? "node" : "nodes")} {string.Join(", ", unreachable.Select(u => $"'{u}'"))}");
        if (!exits)
            sink.Add("BRANCH", "no-ending", FindingCategory.StructuralFailure, FindingSeverity.High,
                $"no path from '{start}' ever ends or hands back to the game: the player is stuck in a loop");
    }

    // ── Bark pools ─────────────────────────────────────────────────────────

    private static void CheckBarks(TextRecord record, TextVersion v, Corpus corpus, Sink sink)
    {
        var speaker = Str(v.Payload, "speaker");
        var lines = Strings(v.Payload, "lines").ToList();

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var line in lines)
            if (!seen.Add(Normalize(line)))
                sink.Add("BARK", "duplicate", FindingCategory.NearDuplicate, FindingSeverity.Low,
                    $"the pool has \"{Clip(line, 80)}\" more than once", line);

        foreach (var line in lines.Where(l => l.Length > MaxBarkChars))
            sink.Add("BARK", "long", FindingCategory.NearDuplicate, FindingSeverity.Low,
                $"a {line.Length}-character line is long for a bark (over {MaxBarkChars}): \"{Clip(line, 60)}\"", line);

        if (speaker == null) return;
        foreach (var (otherId, other, ov) in corpus.Others(record.Id).Where(o => o.Record.Kind == "bark"))
        {
            if (Str(ov.Payload, "speaker") != speaker) continue;
            var theirs = Strings(ov.Payload, "lines").Select(Normalize).ToHashSet(StringComparer.Ordinal);
            var shared = lines.Where(l => theirs.Contains(Normalize(l))).Distinct().ToList();
            if (shared.Count > 0)
                sink.Add("BARK", "repeated", FindingCategory.NearDuplicate, FindingSeverity.Low,
                    $"{shared.Count} line(s) of {speaker}'s pool are also in {otherId} ({Str(ov.Payload, "context") ?? "no context"})",
                    shared[0]);
        }
    }

    // ── Payload reading ────────────────────────────────────────────────────

    /// <summary>Every player-facing string of a payload, with where it is and who says it (conversation and bark).</summary>
    internal static IEnumerable<(string Where, string Text, string? Speaker)> ProseStrings(string kind, JsonElement p)
    {
        if (p.ValueKind != JsonValueKind.Object) yield break;
        switch (kind)
        {
            case "conversation":
                if (Str(p, "title") is { } title) yield return ("the title", title, null);
                if (!p.TryGetProperty("nodes", out var nodes) || nodes.ValueKind != JsonValueKind.Array) yield break;
                foreach (var n in nodes.EnumerateArray())
                {
                    var id = Str(n, "id") ?? "?";
                    if (Str(n, "line") is { } line) yield return ($"node '{id}'", line, Str(n, "speaker"));
                    if (!n.TryGetProperty("choices", out var choices) || choices.ValueKind != JsonValueKind.Array) continue;
                    foreach (var ch in choices.EnumerateArray())
                        if (Str(ch, "text") is { } t) yield return ($"a choice of node '{id}'", t, null);
                }
                break;
            case "bark":
                var speaker = Str(p, "speaker");
                foreach (var l in Strings(p, "lines")) yield return ("a bark line", l, speaker);
                break;
            default:
                foreach (var field in new[] { "title", "author", "short", "long", "brief", "complete", "fail", "term", "definition" })
                    if (Str(p, field) is { } s) yield return ($"payload.{field}", s, null);
                foreach (var field in new[] { "paragraphs", "pages", "aliases" })
                {
                    var i = 0;
                    foreach (var s in Strings(p, field)) yield return ($"payload.{field}[{i++}]", s, null);
                }
                break;
        }
    }

    private static IEnumerable<(string Speaker, string Line)> SpeakerLines(string kind, JsonElement p) =>
        ProseStrings(kind, p).Where(x => x.Speaker != null).Select(x => (x.Speaker!, x.Text));

    /// <summary>Entity names (as the corpus knows them) that occur in a line, longest match first, on word boundaries.
    /// <c>{entity:id}</c> tokens and <c>$placeholders</c> are blanked first: they are references, not names.</summary>
    internal static HashSet<string> NamesIn(string text, Corpus corpus)
    {
        var found = new HashSet<string>(StringComparer.Ordinal);
        if (corpus.NamesByFirstWord.Count == 0 || string.IsNullOrEmpty(text)) return found;
        var clean = PlaceholderToken.Replace(EntityToken.Replace(text, " "), " ");
        foreach (Match m in CapitalWord.Matches(clean))
        {
            if (!corpus.NamesByFirstWord.TryGetValue(m.Value, out var names)) continue;
            foreach (var name in names)
            {
                if (string.CompareOrdinal(clean, m.Index, name, 0, name.Length) != 0) continue;
                var end = m.Index + name.Length;
                if (end < clean.Length && char.IsLetterOrDigit(clean[end])) continue;
                found.Add(name);
                break;
            }
        }
        return found;
    }

    private static HashSet<string> GlossaryTerms(JsonElement p)
    {
        var terms = new HashSet<string>(StringComparer.Ordinal);
        if (Str(p, "term") is { } term) terms.Add(Normalize(term));
        foreach (var a in Strings(p, "aliases")) terms.Add(Normalize(a));
        terms.Remove("");
        return terms;
    }

    private static string? Str(JsonElement o, string name) =>
        o.ValueKind == JsonValueKind.Object && o.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String
        && !string.IsNullOrWhiteSpace(v.GetString()) ? v.GetString() : null;

    private static IEnumerable<string> Strings(JsonElement o, string name)
    {
        if (o.ValueKind != JsonValueKind.Object || !o.TryGetProperty(name, out var arr) || arr.ValueKind != JsonValueKind.Array) yield break;
        foreach (var item in arr.EnumerateArray())
            if (item.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(item.GetString()))
                yield return item.GetString()!;
    }

    private static string Normalize(string s) => NonWord.Replace(s.ToLowerInvariant(), " ").Trim();

    private static string Num(double? d) => d == null ? "?" : d.Value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);

    private static string? Clip(string? s, int max) => s == null ? null : s.Length <= max ? s : s[..(max - 1)] + "…";
}
