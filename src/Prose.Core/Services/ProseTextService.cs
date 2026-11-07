using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Core.Services;

/// <summary>
/// RFC 0017 / docs/CONDUIT.md — game texts (<c>prose-text/1</c>): the conversations, crawls, descriptions,
/// quest text, books, barks and glossary terms a game's players read, authored in Prose against the
/// universe's entities and pulled by the game through the conduit.
///
/// <para><b>Storage (no migration).</b> A text is one <see cref="Entity"/> of the reserved type
/// <see cref="EntityType"/> (Slug = the text id) whose <see cref="Record"/> JSON holds the text and every
/// version it has had. Each version carries its own key, about, speakers, payload and hash. It has an
/// <c>about</c> edge to each entity it is about and a <c>spoken-by</c> edge to each entity speaker
/// (Edge.Source = <c>prose-text</c>), so the graph and the read gate see what a text depends on.</para>
///
/// <para><b>Lifecycle (RFC 0009).</b> draft → approved → published → retired. Drafting new content adds a
/// version as a draft (identical content changes nothing). The live version, the newest approved or
/// published one, stays pullable until a newer draft is approved. Approve, publish and retire are the
/// author's calls (CLI or HTTP with a named approver); there is no MCP door to them.</para>
///
/// <para>Like <see cref="UniverseInterchangeService"/>, every query is explicit about the universe
/// (IgnoreQueryFilters + UniverseId), so the service is correct under an MCP call with no ambient scope.</para>
/// </summary>
public class ProseTextService
{
    /// <summary>The reserved EntityType of a text row. An interchange file may not use it.</summary>
    public const string EntityType = "prose-text";

    public const string Schema = "prose-text/1";
    public const string EdgeSource = "prose-text";

    public static readonly string[] Kinds = ["conversation", "crawl", "description", "quest", "book", "bark", "glossary"];

    public static class Status
    {
        public const string Draft = "draft";
        public const string Approved = "approved";
        public const string Published = "published";
        public const string Superseded = "superseded";
        public const string Retired = "retired";
    }

    private static readonly Regex StableId = new("^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.Compiled);
    private static readonly Regex KeyPattern = new("^[A-Za-z0-9][A-Za-z0-9_.:/-]*$", RegexOptions.Compiled);
    private static readonly Regex Placeholder = new(@"^\$[A-Za-z0-9_]+$", RegexOptions.Compiled);
    private static readonly Regex EntityRef = new(@"\{entity:([^}]*)\}", RegexOptions.Compiled);

    internal static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>The web-cased options every prose-text/1 document is written with (CLI and MCP reuse them).</summary>
    public static JsonSerializerOptions JsonOptsPublic => JsonOpts;

    private readonly IDbContextFactory<ProseDbContext> dbFactory;
    private readonly OutboxService? outbox;

    public ProseTextService(IDbContextFactory<ProseDbContext> dbFactory, OutboxService? outbox = null)
    {
        this.dbFactory = dbFactory;
        this.outbox = outbox;
    }

    // ── Stored shape ───────────────────────────────────────────────────────

    public class TextVersion
    {
        public int Version { get; set; }
        public string Status { get; set; } = ProseTextService.Status.Draft;
        public string Key { get; set; } = "";
        public List<string> About { get; set; } = new();
        public List<string> Speakers { get; set; } = new();
        public JsonElement Payload { get; set; }
        public string Hash { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? PublishedBy { get; set; }
        public DateTime? PublishedAt { get; set; }
    }

    public class TextRecord
    {
        public string Schema { get; set; } = ProseTextService.Schema;
        public string Id { get; set; } = "";
        public string Universe { get; set; } = "";
        public string Kind { get; set; } = "";
        public bool Retired { get; set; }
        public string? RetiredBy { get; set; }
        public DateTime? RetiredAt { get; set; }
        public List<TextVersion> Versions { get; set; } = new();

        public TextVersion? Live => Versions
            .Where(v => v.Status is ProseTextService.Status.Approved or ProseTextService.Status.Published)
            .MaxBy(v => v.Version);

        public TextVersion? Draft => Versions.Where(v => v.Status == ProseTextService.Status.Draft).MaxBy(v => v.Version);

        public TextVersion? Head => Versions.MaxBy(v => v.Version);
    }

    /// <summary>The <c>prose-text/1</c> document: what the API returns and the kit writes to pending/.</summary>
    public class TextDoc
    {
        public string Schema { get; set; } = ProseTextService.Schema;
        public string Id { get; set; } = "";
        public string Universe { get; set; } = "";
        public string Kind { get; set; } = "";
        public string Key { get; set; } = "";
        public List<string> About { get; set; } = new();
        public List<string> Speakers { get; set; } = new();
        public int Version { get; set; }
        public string Status { get; set; } = "";
        public string Hash { get; set; } = "";
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public DateTime? PublishedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        /// <summary>The live (pullable) version number when this document shows a different one (a draft), else null.</summary>
        public int? LiveVersion { get; set; }
        /// <summary>The pending draft's version number when this document shows the live one, else null.</summary>
        public int? DraftVersion { get; set; }
        public JsonElement Payload { get; set; }
    }

    /// <summary>What a caller sends to draft: the prose-text/1 fields that are content. version/status/hash are ignored.</summary>
    public class TextInput
    {
        public string? Schema { get; set; }
        public string Id { get; set; } = "";
        public string? Universe { get; set; }
        public string Kind { get; set; } = "";
        public string Key { get; set; } = "";
        public List<string>? About { get; set; }
        public List<string>? Speakers { get; set; }
        public JsonElement Payload { get; set; }
    }

    public class DraftResult
    {
        public string Id { get; set; } = "";
        public int Version { get; set; }
        public bool Created { get; set; }
        public bool Unchanged { get; set; }
        public List<string> Errors { get; set; } = new();
        public bool Success => Errors.Count == 0;
    }

    public class ActionResult
    {
        public List<string> Ids { get; set; } = new();
        public List<string> Errors { get; set; } = new();
        public bool Success => Errors.Count == 0;
    }

    // ── Draft ──────────────────────────────────────────────────────────────

    /// <summary>Draft a text from its prose-text/1 JSON. Validates deterministically (payload shape, entity
    /// references, conversation graph, key uniqueness); stores a new draft version only when the content
    /// differs from the newest version.</summary>
    public async Task<DraftResult> DraftAsync(string universeSlug, string json, string? by = null, CancellationToken ct = default)
    {
        TextInput? input;
        try { input = JsonSerializer.Deserialize<TextInput>(json, JsonOpts); }
        catch (Exception ex) { return new DraftResult { Errors = { $"parse failed: {ex.Message}" } }; }
        if (input == null) return new DraftResult { Errors = { "empty document" } };
        return await DraftAsync(universeSlug, input, by, ct);
    }

    public async Task<DraftResult> DraftAsync(string universeSlug, TextInput input, string? by = null, CancellationToken ct = default)
    {
        var result = new DraftResult { Id = input.Id ?? "" };
        var slug = NormSlug(universeSlug);

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var universe = await db.Universes.FirstOrDefaultAsync(u => u.Slug == slug, ct);
        if (universe == null) { result.Errors.Add($"unknown universe '{universeSlug}'"); return result; }

        var about = (input.About ?? new()).Select(s => s.Trim()).Where(s => s.Length > 0).Distinct().ToList();
        var speakers = (input.Speakers ?? new()).Select(s => s.Trim()).Where(s => s.Length > 0).Distinct().ToList();
        var kind = (input.Kind ?? "").Trim().ToLowerInvariant();
        var key = (input.Key ?? "").Trim();

        result.Errors.AddRange(ValidateShape(input, slug, kind, key, about, speakers));
        if (result.Errors.Count > 0) return result;

        // Entity references: about ids, entity speakers, {entity:id} in any line.
        var refs = about.Concat(speakers.Where(s => !Placeholder.IsMatch(s)))
            .Concat(CollectEntityRefs(input.Payload)).Distinct().ToList();
        var known = refs.Count == 0 ? new Dictionary<string, Guid>() : (await db.Entities.IgnoreQueryFilters()
                .Where(e => e.UniverseId == universe.Id && refs.Contains(e.Slug) && e.EntityType != EntityType
                            && e.Status != "stub" && e.Status != "archived")
                .Select(e => new { e.Slug, e.Id }).ToListAsync(ct))
            .GroupBy(e => e.Slug).ToDictionary(g => g.Key, g => g.First().Id);
        foreach (var r in refs.Where(r => !known.ContainsKey(r)))
            result.Errors.Add($"entity '{r}' does not exist in universe '{slug}'");

        var rows = await LoadAllAsync(db, universe.Id, ct);
        var clash = rows.FirstOrDefault(x => x.Record.Id != input.Id && !x.Record.Retired
                                             && x.Record.Head?.Key == key);
        if (clash.Record != null) result.Errors.Add($"key '{key}' is already used by text '{clash.Record.Id}'");

        var existing = rows.FirstOrDefault(x => x.Record.Id == input.Id);
        if (existing.Record != null && existing.Record.Kind != kind)
            result.Errors.Add($"text '{input.Id}' is a {existing.Record.Kind}; its kind cannot change to {kind}");
        if (result.Errors.Count > 0) return result;

        var payload = input.Payload.Clone();
        var hash = Hash(payload);
        var now = DateTime.UtcNow;

        Entity entity;
        TextRecord record;
        if (existing.Record == null)
        {
            record = new TextRecord { Id = input.Id, Universe = slug, Kind = kind };
            entity = new Entity
            {
                UniverseId = universe.Id, EntityType = EntityType, Slug = input.Id, Status = "canon",
                CreatedAt = now, ModifiedAt = now, Provenance = ClaimProvenance.Inferred,
            };
            db.Entities.Add(entity);
            result.Created = true;
        }
        else
        {
            record = existing.Record;
            entity = await db.Entities.IgnoreQueryFilters().FirstAsync(e => e.Id == existing.EntityId, ct);
            var head = record.Head;
            if (head != null && head.Hash == hash && head.Key == key
                && head.About.SequenceEqual(about) && head.Speakers.SequenceEqual(speakers))
            {
                result.Version = head.Version;
                result.Unchanged = true;
                return result;
            }
        }

        foreach (var old in record.Versions.Where(v => v.Status == Status.Draft)) old.Status = Status.Superseded;
        var version = new TextVersion
        {
            Version = (record.Head?.Version ?? 0) + 1,
            Status = Status.Draft,
            Key = key, About = about, Speakers = speakers,
            Payload = payload, Hash = hash, CreatedAt = now, CreatedBy = by,
        };
        record.Versions.Add(version);

        entity.Name = DisplayName(kind, key, payload);
        entity.Description = key;
        entity.ModifiedAt = now;
        await db.SaveChangesAsync(ct);

        await SaveRecordAsync(db, entity.Id, record, ct);
        await SyncEdgesAsync(db, universe.Id, entity.Id, about, speakers, known, ct);
        await db.SaveChangesAsync(ct);

        result.Version = version.Version;
        return result;
    }

    // ── Approve / publish / retire (the author's calls) ─────────────────────

    /// <summary>Approve one version of each text (default: its newest draft). The previously live version
    /// becomes superseded. <paramref name="by"/> names the approver and is required.</summary>
    public async Task<ActionResult> ApproveAsync(string universeSlug, IReadOnlyList<string> ids, string by,
        int? version = null, CancellationToken ct = default)
    {
        var result = new ActionResult();
        if (string.IsNullOrWhiteSpace(by)) { result.Errors.Add("approval needs the approver's name (by)"); return result; }
        if (ids.Count == 0) { result.Errors.Add("no text ids given"); return result; }
        if (version != null && ids.Count > 1) { result.Errors.Add("version applies to one text at a time"); return result; }

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var universe = await FindUniverseAsync(db, universeSlug, result.Errors, ct);
        if (universe == null) return result;
        var rows = (await LoadAllAsync(db, universe.Id, ct)).ToDictionary(x => x.Record.Id);
        var now = DateTime.UtcNow;

        foreach (var id in ids.Distinct())
        {
            if (!rows.TryGetValue(id, out var row)) { result.Errors.Add($"no text '{id}'"); continue; }
            var target = version == null ? row.Record.Draft : row.Record.Versions.FirstOrDefault(v => v.Version == version);
            if (target == null) { result.Errors.Add($"text '{id}' has no {(version == null ? "draft" : $"version {version}")} to approve"); continue; }
            if (target.Status != Status.Draft) { result.Errors.Add($"text '{id}' v{target.Version} is {target.Status}, not a draft"); continue; }

            await ApproveVersionAsync(db, row.Record, row.EntityId, target, by, now, ct);
            result.Ids.Add(id);
        }
        await db.SaveChangesAsync(ct);
        return result;
    }

    /// <summary>Marks <paramref name="target"/> approved (the previously live version becomes superseded,
    /// a retired text comes back) and saves the record. Shared by single and bulk approval.</summary>
    private static async Task ApproveVersionAsync(ProseDbContext db, TextRecord record, Guid entityId, TextVersion target,
        string by, DateTime now, CancellationToken ct)
    {
        foreach (var v in record.Versions.Where(v => v.Status is Status.Approved or Status.Published))
            v.Status = Status.Superseded;
        target.Status = Status.Approved;
        target.ApprovedBy = by.Trim();
        target.ApprovedAt = now;
        record.Retired = false;
        record.RetiredAt = null;
        record.RetiredBy = null;

        var entity = await db.Entities.IgnoreQueryFilters().FirstAsync(e => e.Id == entityId, ct);
        entity.Status = "canon";
        entity.Provenance = ClaimProvenance.Authored;
        entity.ModifiedAt = now;
        await SaveRecordAsync(db, entityId, record, ct);
    }

    // ── Bulk approve / publish (PRS-T918: "approve everything of this type") ──

    /// <summary>One text a bulk call acted on (or, in a dry run, would act on).</summary>
    public class BulkItem
    {
        public string Id { get; set; } = "";
        public string Kind { get; set; } = "";
        public string Key { get; set; } = "";
        public int Version { get; set; }
        public string Hash { get; set; } = "";
        /// <summary>The version's status before the call: draft (bulk approve) or approved (bulk publish).</summary>
        public string From { get; set; } = "";
        /// <summary>The status the call leaves it in (or would leave it in): approved or published.</summary>
        public string To { get; set; } = "";
    }

    /// <summary>A matched text the bulk call refused, with the reasons. The rest of the batch still goes through.</summary>
    public class BulkRefusal
    {
        public string Id { get; set; } = "";
        public string Key { get; set; } = "";
        public int Version { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    /// <summary>The result of a bulk approve or publish.
    /// <see cref="Errors"/> are request-level (no approver, unknown kind or universe): nothing was done.
    /// <see cref="Refused"/> are per-text: those texts were left as they were and the rest went through.</summary>
    public class BulkResult
    {
        public bool DryRun { get; set; }
        public string Kind { get; set; } = "";
        public string? KeyPrefix { get; set; }
        public string? About { get; set; }
        /// <summary>Texts approved by the call (or that would be).</summary>
        public List<BulkItem> Approved { get; set; } = new();
        /// <summary>Texts published by the call (or that would be).</summary>
        public List<BulkItem> Published { get; set; } = new();
        public List<BulkRefusal> Refused { get; set; } = new();
        public List<string> Errors { get; set; } = new();
        public bool Success => Errors.Count == 0 && Refused.Count == 0;
    }

    /// <summary>
    /// Approve every pending draft of one kind in a universe, optionally narrowed to keys starting with
    /// <paramref name="keyPrefix"/> and/or texts about the entity <paramref name="about"/> (filters match the
    /// draft version). Retired texts are skipped: bringing one back stays a single, explicit approval.
    ///
    /// <para>Each matched draft gets single approval's checks (it is the newest draft and still a draft) and,
    /// because the author has not looked at each one, the draft-time checks again: payload shape for its
    /// kind, every entity it references still exists (not archived), and its key is not used by another
    /// live text. <b>A text that fails is reported in <see cref="BulkResult.Refused"/> and left a draft;
    /// the rest are approved</b> (one bad draft does not hold up a batch; re-run after fixing it).</para>
    ///
    /// <para><paramref name="publish"/> also publishes everything this call approved, as one batch with one
    /// <c>texts.published</c> Outbox notice. <paramref name="dryRun"/> computes the same lists and refusals
    /// and changes nothing (no save, no notice).</para>
    /// </summary>
    public async Task<BulkResult> BulkApproveAsync(string universeSlug, string kind, string by,
        string? keyPrefix = null, string? about = null, bool dryRun = false, bool publish = false, CancellationToken ct = default)
    {
        var result = NewBulk(kind, keyPrefix, about, dryRun);
        if (string.IsNullOrWhiteSpace(by)) result.Errors.Add("approval needs the approver's name (by)");
        ValidateBulkKind(result);
        if (result.Errors.Count > 0) return result;

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var universe = await FindUniverseAsync(db, universeSlug, result.Errors, ct);
        if (universe == null) return result;
        var rows = await LoadAllAsync(db, universe.Id, ct);

        var candidates = rows
            .Where(r => !r.Record.Retired && r.Record.Kind == result.Kind)
            .Select(r => (Row: r, Draft: r.Record.Draft))
            .Where(x => x.Draft != null && MatchesBulk(x.Draft, result))
            .OrderBy(x => x.Draft!.Key, StringComparer.Ordinal).ThenBy(x => x.Row.Record.Id, StringComparer.Ordinal)
            .ToList();
        if (candidates.Count == 0) return result;

        // Entity references, looked up once for the whole batch.
        var refsOf = candidates.ToDictionary(x => x.Row.Record.Id, x => ReferencesOf(x.Draft!));
        var allRefs = refsOf.Values.SelectMany(r => r).Distinct().ToList();
        var known = allRefs.Count == 0 ? new HashSet<string>() : (await db.Entities.IgnoreQueryFilters()
                .Where(e => e.UniverseId == universe.Id && allRefs.Contains(e.Slug) && e.EntityType != EntityType
                            && e.Status != "stub" && e.Status != "archived")
                .Select(e => e.Slug).ToListAsync(ct)).ToHashSet();

        var now = DateTime.UtcNow;
        var approvedRows = new List<(TextRecord Record, Guid EntityId)>();
        foreach (var (row, draft) in candidates)
        {
            var errors = new List<string>();
            if (draft!.Status != Status.Draft) errors.Add($"v{draft.Version} is {draft.Status}, not a draft");
            errors.AddRange(ValidatePayload(row.Record.Kind, draft.Payload, draft.Speakers));
            errors.AddRange(refsOf[row.Record.Id].Where(r => !known.Contains(r))
                .Select(r => $"entity '{r}' does not exist in universe '{universe.Slug}' (or is archived)"));
            var clash = rows.FirstOrDefault(x => x.Record.Id != row.Record.Id && !x.Record.Retired
                && (x.Record.Head?.Key == draft.Key || x.Record.Live?.Key == draft.Key));
            if (clash.Record != null) errors.Add($"key '{draft.Key}' is already used by text '{clash.Record.Id}'");
            if (errors.Count > 0)
            {
                result.Refused.Add(new BulkRefusal { Id = row.Record.Id, Key = draft.Key, Version = draft.Version, Errors = errors });
                continue;
            }

            result.Approved.Add(new BulkItem
            {
                Id = row.Record.Id, Kind = row.Record.Kind, Key = draft.Key, Version = draft.Version, Hash = draft.Hash,
                From = Status.Draft, To = Status.Approved,
            });
            if (publish)
                result.Published.Add(new BulkItem
                {
                    Id = row.Record.Id, Kind = row.Record.Kind, Key = draft.Key, Version = draft.Version, Hash = draft.Hash,
                    From = Status.Approved, To = Status.Published,
                });
            if (dryRun) continue;
            await ApproveVersionAsync(db, row.Record, row.EntityId, draft, by, now, ct);
            if (publish) approvedRows.Add((row.Record, row.EntityId));
        }
        if (dryRun) return result;

        foreach (var (record, entityId) in approvedRows)
        {
            var live = record.Live!;
            live.Status = Status.Published;
            live.PublishedBy = by.Trim();
            live.PublishedAt = now;
            await SaveRecordAsync(db, entityId, record, ct);
        }
        await db.SaveChangesAsync(ct);
        if (result.Published.Count > 0) await NotifyPublishedAsync(universe.Slug, result.Published, ct);
        return result;
    }

    /// <summary>
    /// Publish every approved (not yet published) live version of one kind in a universe, optionally narrowed
    /// to keys starting with <paramref name="keyPrefix"/> and/or texts about <paramref name="about"/> (filters
    /// match the live version), as one batch with one <c>texts.published</c> Outbox notice. Retired texts are
    /// skipped. Approval already ran each text's checks, so nothing is refused here.
    /// <paramref name="dryRun"/> lists what would be published and changes nothing.
    /// </summary>
    public async Task<BulkResult> BulkPublishAsync(string universeSlug, string kind, string by,
        string? keyPrefix = null, string? about = null, bool dryRun = false, CancellationToken ct = default)
    {
        var result = NewBulk(kind, keyPrefix, about, dryRun);
        if (string.IsNullOrWhiteSpace(by)) result.Errors.Add("publishing needs the publisher's name (by)");
        ValidateBulkKind(result);
        if (result.Errors.Count > 0) return result;

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var universe = await FindUniverseAsync(db, universeSlug, result.Errors, ct);
        if (universe == null) return result;
        var rows = await LoadAllAsync(db, universe.Id, ct);
        var now = DateTime.UtcNow;

        var matched = rows
            .Where(r => !r.Record.Retired && r.Record.Kind == result.Kind)
            .Select(r => (Row: r, Live: r.Record.Live))
            .Where(x => x.Live is { Status: Status.Approved } && MatchesBulk(x.Live, result))
            .OrderBy(x => x.Live!.Key, StringComparer.Ordinal).ThenBy(x => x.Row.Record.Id, StringComparer.Ordinal)
            .ToList();
        foreach (var (row, live) in matched)
        {
            result.Published.Add(new BulkItem
            {
                Id = row.Record.Id, Kind = row.Record.Kind, Key = live!.Key, Version = live.Version, Hash = live.Hash,
                From = Status.Approved, To = Status.Published,
            });
            if (dryRun) continue;
            live.Status = Status.Published;
            live.PublishedBy = by.Trim();
            live.PublishedAt = now;
            await SaveRecordAsync(db, row.EntityId, row.Record, ct);
        }
        if (dryRun) return result;
        await db.SaveChangesAsync(ct);
        if (result.Published.Count > 0) await NotifyPublishedAsync(universe.Slug, result.Published, ct);
        return result;
    }

    private static BulkResult NewBulk(string kind, string? keyPrefix, string? about, bool dryRun) => new()
    {
        DryRun = dryRun,
        Kind = (kind ?? "").Trim().ToLowerInvariant(),
        KeyPrefix = string.IsNullOrWhiteSpace(keyPrefix) ? null : keyPrefix.Trim(),
        About = string.IsNullOrWhiteSpace(about) ? null : about.Trim(),
    };

    private static void ValidateBulkKind(BulkResult result)
    {
        if (result.Kind.Length == 0) result.Errors.Add($"a bulk call needs a kind ({string.Join(", ", Kinds)})");
        else if (!Kinds.Contains(result.Kind)) result.Errors.Add($"kind '{result.Kind}' is not one of {string.Join(", ", Kinds)}");
    }

    private static bool MatchesBulk(TextVersion v, BulkResult filter) =>
        (filter.KeyPrefix == null || v.Key.StartsWith(filter.KeyPrefix, StringComparison.Ordinal))
        && (filter.About == null || v.About.Contains(filter.About));

    private static List<string> ReferencesOf(TextVersion v) =>
        v.About.Concat(v.Speakers.Where(s => !Placeholder.IsMatch(s)))
            .Concat(v.Payload.ValueKind == JsonValueKind.Undefined ? [] : CollectEntityRefs(v.Payload))
            .Distinct().ToList();

    private Task NotifyPublishedAsync(string universeSlug, IReadOnlyCollection<BulkItem> items, CancellationToken ct) =>
        NotifyAsync(universeSlug, "texts.published",
            $"{items.Count} text(s) published in '{universeSlug}'. Run the consumer kit's pull.",
            new { universe = universeSlug, texts = items.Select(i => new { id = i.Id, kind = i.Kind, key = i.Key, version = i.Version, hash = i.Hash }).ToList() }, ct);

    /// <summary>Mark approved live versions published (all of the universe's, or the given ids) and send one
    /// <c>texts.published</c> Outbox notice to the universe's consumer for the whole batch.</summary>
    public async Task<ActionResult> PublishAsync(string universeSlug, string by, IReadOnlyList<string>? ids = null, CancellationToken ct = default)
    {
        var result = new ActionResult();
        if (string.IsNullOrWhiteSpace(by)) { result.Errors.Add("publishing needs the publisher's name (by)"); return result; }

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var universe = await FindUniverseAsync(db, universeSlug, result.Errors, ct);
        if (universe == null) return result;
        var rows = await LoadAllAsync(db, universe.Id, ct);
        var wanted = ids is { Count: > 0 } ? ids.ToHashSet() : null;
        if (wanted != null)
            foreach (var missing in wanted.Where(w => rows.All(r => r.Record.Id != w)))
                result.Errors.Add($"no text '{missing}'");

        var now = DateTime.UtcNow;
        var published = new List<object>();
        foreach (var row in rows)
        {
            if (wanted != null && !wanted.Contains(row.Record.Id)) continue;
            if (row.Record.Retired) continue;
            var live = row.Record.Live;
            if (live is not { Status: Status.Approved }) continue;
            live.Status = Status.Published;
            live.PublishedBy = by.Trim();
            live.PublishedAt = now;
            await SaveRecordAsync(db, row.EntityId, row.Record, ct);
            result.Ids.Add(row.Record.Id);
            published.Add(new { id = row.Record.Id, kind = row.Record.Kind, key = live.Key, version = live.Version, hash = live.Hash });
        }
        await db.SaveChangesAsync(ct);

        if (published.Count > 0) await NotifyAsync(universe.Slug, "texts.published",
            $"{published.Count} text(s) published in '{universe.Slug}'. Run the consumer kit's pull.",
            new { universe = universe.Slug, texts = published }, ct);
        return result;
    }

    /// <summary>Retire a text: it leaves every pull (the game deletes it on seeing status retired). Kept,
    /// never deleted; approving a new draft brings it back.</summary>
    public async Task<ActionResult> RetireAsync(string universeSlug, IReadOnlyList<string> ids, string by, CancellationToken ct = default)
    {
        var result = new ActionResult();
        if (string.IsNullOrWhiteSpace(by)) { result.Errors.Add("retiring needs the author's name (by)"); return result; }

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var universe = await FindUniverseAsync(db, universeSlug, result.Errors, ct);
        if (universe == null) return result;
        var rows = (await LoadAllAsync(db, universe.Id, ct)).ToDictionary(x => x.Record.Id);
        var now = DateTime.UtcNow;
        var retired = new List<object>();
        foreach (var id in ids.Distinct())
        {
            if (!rows.TryGetValue(id, out var row)) { result.Errors.Add($"no text '{id}'"); continue; }
            if (row.Record.Retired) { result.Ids.Add(id); continue; }
            row.Record.Retired = true;
            row.Record.RetiredBy = by.Trim();
            row.Record.RetiredAt = now;
            var entity = await db.Entities.IgnoreQueryFilters().FirstAsync(e => e.Id == row.EntityId, ct);
            entity.Status = "archived";
            entity.ModifiedAt = now;
            await SaveRecordAsync(db, row.EntityId, row.Record, ct);
            result.Ids.Add(id);
            retired.Add(new { id, kind = row.Record.Kind, key = row.Record.Head?.Key });
        }
        await db.SaveChangesAsync(ct);
        if (retired.Count > 0) await NotifyAsync(universe.Slug, "texts.retired",
            $"{retired.Count} text(s) retired in '{universe.Slug}'.", new { universe = universe.Slug, texts = retired }, ct);
        return result;
    }

    // ── Reads ──────────────────────────────────────────────────────────────

    /// <summary>List texts as prose-text/1 documents.
    /// <list type="bullet">
    /// <item><c>status</c> null or <c>pullable</c>: each text's live (approved or published) version; drafts-only and retired texts are left out.</item>
    /// <item><c>approved</c> / <c>published</c>: live versions in exactly that status.</item>
    /// <item><c>draft</c>: each text's pending draft.</item>
    /// <item><c>retired</c>: retired texts, at their last version, with status retired.</item>
    /// <item><c>all</c>: every text at its newest version, whatever its status.</item>
    /// </list>
    /// <c>since</c>: an integer keeps versions newer than it; an ISO date keeps those approved or published after it.</summary>
    public async Task<List<TextDoc>> ListAsync(string universeSlug, string? kind = null, string? status = null,
        string? since = null, string? key = null, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var errors = new List<string>();
        var universe = await FindUniverseAsync(db, universeSlug, errors, ct)
            ?? throw new InvalidOperationException(errors[0]);
        var rows = await LoadAllAsync(db, universe.Id, ct);

        var mode = string.IsNullOrWhiteSpace(status) ? "pullable" : status.Trim().ToLowerInvariant();
        if (mode is not ("pullable" or "approved" or "published" or "draft" or "retired" or "all"))
            throw new InvalidOperationException($"unknown status '{status}' (pullable, approved, published, draft, retired, all)");
        int? sinceVersion = int.TryParse(since, out var sv) ? sv : null;
        DateTime? sinceDate = sinceVersion == null && DateTime.TryParse(since, null,
            System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal, out var sd) ? sd : null;
        if (!string.IsNullOrWhiteSpace(since) && sinceVersion == null && sinceDate == null)
            throw new InvalidOperationException($"since '{since}' is neither a version number nor a date");

        var docs = new List<TextDoc>();
        foreach (var (record, updatedAt, _) in rows.OrderBy(r => r.Record.Kind).ThenBy(r => r.Record.Id))
        {
            if (!string.IsNullOrWhiteSpace(kind) && !string.Equals(record.Kind, kind.Trim(), StringComparison.OrdinalIgnoreCase)) continue;
            TextVersion? v = mode switch
            {
                "draft" => record.Retired ? null : record.Draft,
                "retired" => record.Retired ? record.Live ?? record.Head : null,
                "all" => record.Head,
                _ => record.Retired ? null : record.Live,
            };
            if (v == null) continue;
            if (mode is "approved" or "published" && v.Status != mode) continue;
            if (!string.IsNullOrWhiteSpace(key) && v.Key != key.Trim()) continue;
            if (sinceVersion != null && v.Version <= sinceVersion) continue;
            if (sinceDate != null && (v.PublishedAt ?? v.ApprovedAt ?? DateTime.MinValue) <= sinceDate) continue;
            docs.Add(ToDoc(record, v, updatedAt, mode == "retired"));
        }
        return docs;
    }

    /// <summary>One text as a prose-text/1 document: the given version, else the live one, else the newest.</summary>
    public async Task<TextDoc?> GetAsync(string universeSlug, string id, int? version = null, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var errors = new List<string>();
        var universe = await FindUniverseAsync(db, universeSlug, errors, ct)
            ?? throw new InvalidOperationException(errors[0]);
        var row = (await LoadAllAsync(db, universe.Id, ct, id)).FirstOrDefault();
        if (row.Record == null) return null;
        var v = version != null ? row.Record.Versions.FirstOrDefault(x => x.Version == version)
            : row.Record.Live ?? row.Record.Head;
        return v == null ? null : ToDoc(row.Record, v, row.UpdatedAt, row.Record.Retired && version == null);
    }

    /// <summary>Every version of one text, oldest first (the history view).</summary>
    public async Task<TextRecord?> GetRecordAsync(string universeSlug, string id, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var errors = new List<string>();
        var universe = await FindUniverseAsync(db, universeSlug, errors, ct)
            ?? throw new InvalidOperationException(errors[0]);
        return (await LoadAllAsync(db, universe.Id, ct, id)).FirstOrDefault().Record;
    }

    // ── Validation ─────────────────────────────────────────────────────────

    private static IEnumerable<string> ValidateShape(TextInput input, string slug, string kind, string key,
        List<string> about, List<string> speakers)
    {
        if (input.Schema != null && input.Schema != Schema) yield return $"schema must be '{Schema}'";
        if (string.IsNullOrWhiteSpace(input.Id) || !StableId.IsMatch(input.Id))
            yield return $"id '{input.Id}' is not a stable slug (^[a-z0-9]+(-[a-z0-9]+)*$)";
        if (!string.IsNullOrWhiteSpace(input.Universe) && NormSlug(input.Universe) != slug)
            yield return $"document is for universe '{input.Universe}', not '{slug}'";
        if (!Kinds.Contains(kind)) { yield return $"kind '{input.Kind}' is not one of {string.Join(", ", Kinds)}"; yield break; }
        if (!KeyPattern.IsMatch(key)) yield return $"key '{key}' is missing or not a valid key (^[A-Za-z0-9][A-Za-z0-9_.:/-]*$)";
        foreach (var a in about.Where(a => !StableId.IsMatch(a))) yield return $"about '{a}' is not an entity id";
        foreach (var s in speakers.Where(s => !StableId.IsMatch(s) && !Placeholder.IsMatch(s)))
            yield return $"speaker '{s}' is neither an entity id nor a $placeholder";
        if (input.Payload.ValueKind != JsonValueKind.Object) { yield return "payload must be an object"; yield break; }
        foreach (var e in ValidatePayload(kind, input.Payload, speakers)) yield return e;
    }

    internal static IEnumerable<string> ValidatePayload(string kind, JsonElement p, IReadOnlyCollection<string> speakers)
    {
        switch (kind)
        {
            case "conversation":
            {
                if (!Str(p, "start", out var start)) yield return "conversation needs payload.start";
                if (!p.TryGetProperty("nodes", out var nodes) || nodes.ValueKind != JsonValueKind.Array || nodes.GetArrayLength() == 0)
                { yield return "conversation needs payload.nodes (a non-empty array)"; yield break; }
                var ids = new HashSet<string>();
                var nexts = new List<(string from, string next)>();
                var i = 0;
                foreach (var n in nodes.EnumerateArray())
                {
                    var at = $"nodes[{i++}]";
                    if (!Str(n, "id", out var nid)) { yield return $"{at} needs an id"; continue; }
                    if (!ids.Add(nid)) yield return $"node id '{nid}' is used twice";
                    if (!Str(n, "speaker", out var sp)) yield return $"node '{nid}' needs a speaker";
                    else if (!speakers.Contains(sp)) yield return $"node '{nid}' speaker '{sp}' is not one of the text's speakers";
                    if (!Str(n, "line", out _)) yield return $"node '{nid}' needs a non-empty line";
                    if (n.TryGetProperty("next", out var nx) && nx.ValueKind != JsonValueKind.Null)
                    {
                        if (nx.ValueKind == JsonValueKind.String && nx.GetString()!.Length > 0) nexts.Add((nid, nx.GetString()!));
                        else yield return $"node '{nid}' next must be a non-empty string";
                    }
                    if (n.TryGetProperty("choices", out var ch) && ch.ValueKind != JsonValueKind.Null)
                    {
                        if (ch.ValueKind != JsonValueKind.Array) { yield return $"node '{nid}' choices must be an array"; continue; }
                        var c = 0;
                        foreach (var choice in ch.EnumerateArray())
                        {
                            if (!Str(choice, "text", out _)) yield return $"node '{nid}' choice {c} needs text";
                            if (!Str(choice, "next", out var cn)) yield return $"node '{nid}' choice {c} needs next";
                            else nexts.Add((nid, cn));
                            if (choice.TryGetProperty("tags", out var tg) && tg.ValueKind is not (JsonValueKind.Array or JsonValueKind.Null))
                                yield return $"node '{nid}' choice {c} tags must be an array";
                            c++;
                        }
                    }
                }
                if (start != null && !ids.Contains(start)) yield return $"start '{start}' is not a node";
                foreach (var (from, next) in nexts.Where(x => !x.next.StartsWith('$') && !ids.Contains(x.next)))
                    yield return $"node '{from}' goes to '{next}', which is not a node (use a $token for a game-resolved target)";
                break;
            }
            case "crawl":
                if (!Str(p, "title", out _)) yield return "crawl needs payload.title";
                foreach (var e in Lines(p, "paragraphs")) yield return e;
                break;
            case "description":
                if (!Str(p, "short", out _) && !Str(p, "long", out _)) yield return "description needs payload.short or payload.long";
                break;
            case "quest":
                if (!Str(p, "title", out _)) yield return "quest needs payload.title";
                if (!Str(p, "brief", out _)) yield return "quest needs payload.brief";
                break;
            case "book":
                if (!Str(p, "title", out _)) yield return "book needs payload.title";
                foreach (var e in Lines(p, "pages")) yield return e;
                break;
            case "bark":
                if (!Str(p, "speaker", out var bs)) yield return "bark needs payload.speaker";
                else if (speakers.Count > 0 && !speakers.Contains(bs)) yield return $"bark speaker '{bs}' is not one of the text's speakers";
                foreach (var e in Lines(p, "lines")) yield return e;
                break;
            case "glossary":
                if (!Str(p, "term", out _)) yield return "glossary needs payload.term";
                if (!Str(p, "definition", out _)) yield return "glossary needs payload.definition";
                break;
        }
    }

    private static bool Str(JsonElement o, string name, out string? value)
    {
        value = null;
        if (o.ValueKind != JsonValueKind.Object || !o.TryGetProperty(name, out var v) || v.ValueKind != JsonValueKind.String) return false;
        value = v.GetString();
        return !string.IsNullOrWhiteSpace(value);
    }

    private static IEnumerable<string> Lines(JsonElement p, string name)
    {
        if (!p.TryGetProperty(name, out var arr) || arr.ValueKind != JsonValueKind.Array || arr.GetArrayLength() == 0)
        { yield return $"payload.{name} must be a non-empty array of strings"; yield break; }
        var i = 0;
        foreach (var l in arr.EnumerateArray())
        {
            if (l.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(l.GetString()))
                yield return $"payload.{name}[{i}] is empty or not a string";
            i++;
        }
    }

    /// <summary>Every <c>{entity:id}</c> in any string of the payload.</summary>
    internal static IEnumerable<string> CollectEntityRefs(JsonElement e)
    {
        switch (e.ValueKind)
        {
            case JsonValueKind.String:
                foreach (Match m in EntityRef.Matches(e.GetString()!)) yield return m.Groups[1].Value.Trim();
                break;
            case JsonValueKind.Object:
                foreach (var prop in e.EnumerateObject())
                    foreach (var r in CollectEntityRefs(prop.Value)) yield return r;
                break;
            case JsonValueKind.Array:
                foreach (var item in e.EnumerateArray())
                    foreach (var r in CollectEntityRefs(item)) yield return r;
                break;
        }
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    /// <summary>SHA-256 of the payload in canonical form (object keys sorted, no whitespace).</summary>
    public static string Hash(JsonElement payload)
    {
        var canonical = Canonical(JsonNode.Parse(payload.GetRawText()))?.ToJsonString() ?? "null";
        return "sha256:" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    private static JsonNode? Canonical(JsonNode? node) => node switch
    {
        JsonObject o => new JsonObject(o.OrderBy(kv => kv.Key, StringComparer.Ordinal)
            .Select(kv => KeyValuePair.Create(kv.Key, Canonical(kv.Value)))),
        JsonArray a => new JsonArray(a.Select(Canonical).ToArray()),
        null => null,
        _ => JsonNode.Parse(node.ToJsonString()),
    };

    private static string DisplayName(string kind, string key, JsonElement payload)
    {
        foreach (var field in new[] { "title", "term" })
            if (Str(payload, field, out var t)) return Truncate(t!, 180);
        return Truncate($"{kind} {key}", 180);
    }

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max];

    private static string NormSlug(string s) => (s ?? "").Trim().ToLowerInvariant();

    private static TextDoc ToDoc(TextRecord r, TextVersion v, DateTime updatedAt, bool asRetired)
    {
        var live = r.Live;
        var draft = r.Draft;
        return new TextDoc
        {
            Id = r.Id, Universe = r.Universe, Kind = r.Kind,
            Key = v.Key, About = v.About, Speakers = v.Speakers,
            Version = v.Version,
            Status = asRetired ? Status.Retired : v.Status,
            Hash = v.Hash, ApprovedBy = v.ApprovedBy, ApprovedAt = v.ApprovedAt, PublishedAt = v.PublishedAt,
            UpdatedAt = updatedAt,
            LiveVersion = live != null && live.Version != v.Version ? live.Version : null,
            DraftVersion = draft != null && draft.Version != v.Version ? draft.Version : null,
            Payload = v.Payload,
        };
    }

    private static async Task<Universe?> FindUniverseAsync(ProseDbContext db, string slug, List<string> errors, CancellationToken ct)
    {
        var norm = NormSlug(slug);
        var u = await db.Universes.FirstOrDefaultAsync(x => x.Slug == norm, ct);
        if (u == null) errors.Add($"unknown universe '{slug}'");
        return u;
    }

    private static async Task<List<(TextRecord Record, DateTime UpdatedAt, Guid EntityId)>> LoadAllAsync(
        ProseDbContext db, Guid universeId, CancellationToken ct, string? onlyId = null)
    {
        var q = db.Entities.IgnoreQueryFilters().Where(e => e.UniverseId == universeId && e.EntityType == EntityType);
        if (onlyId != null) q = q.Where(e => e.Slug == onlyId);
        var rows = await q.Join(db.Records.IgnoreQueryFilters(), e => e.Id, r => r.EntityId,
                (e, r) => new { e.Id, r.Json, r.UpdatedAt })
            .ToListAsync(ct);
        var list = new List<(TextRecord, DateTime, Guid)>();
        foreach (var row in rows)
        {
            TextRecord? rec;
            try { rec = JsonSerializer.Deserialize<TextRecord>(row.Json, JsonOpts); }
            catch { continue; }
            if (rec == null || string.IsNullOrEmpty(rec.Id)) continue;
            list.Add((rec, row.UpdatedAt, row.Id));
        }
        return list;
    }

    private static async Task SaveRecordAsync(ProseDbContext db, Guid entityId, TextRecord record, CancellationToken ct)
    {
        var row = await db.Records.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.EntityId == entityId, ct);
        if (row == null) { row = new Record { EntityId = entityId }; db.Records.Add(row); }
        row.Json = JsonSerializer.Serialize(record, JsonOpts);
        row.UpdatedAt = DateTime.UtcNow;
    }

    private static async Task SyncEdgesAsync(ProseDbContext db, Guid universeId, Guid textId,
        List<string> about, List<string> speakers, Dictionary<string, Guid> known, CancellationToken ct)
    {
        var wanted = about.Where(known.ContainsKey).Select(a => (Target: known[a], Kind: "about"))
            .Concat(speakers.Where(known.ContainsKey).Select(s => (Target: known[s], Kind: "spoken-by")))
            .Distinct().ToHashSet();
        var existing = await db.Edges.IgnoreQueryFilters()
            .Where(e => e.UniverseId == universeId && e.SourceId == textId && e.Source == EdgeSource && e.InvalidatedAt == null)
            .ToListAsync(ct);
        var now = DateTime.UtcNow;
        foreach (var e in existing.Where(e => !wanted.Contains((e.TargetId, e.RelationType)))) e.InvalidatedAt = now;
        foreach (var w in wanted.Where(w => !existing.Any(e => e.TargetId == w.Target && e.RelationType == w.Kind)))
            db.Edges.Add(new Edge
            {
                UniverseId = universeId, SourceId = textId, TargetId = w.Target, RelationType = w.Kind,
                Source = EdgeSource, Sentiment = "neutral", Weight = 1.0,
            });
    }

    private async Task NotifyAsync(string consumer, string kind, string summary, object data, CancellationToken ct)
    {
        if (outbox == null) return;
        try { await outbox.EnqueueAsync(consumer, kind, summary, data, ct); }
        catch { /* best-effort notification only, like the interchange notices */ }
    }
}
