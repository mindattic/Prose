using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Interfaces;

namespace Prose.Core.Services;

/// <summary>
/// When a canon entity is updated, finds every beat that mentions it
/// (via <see cref="BeatEntityMention"/>) and marks those beats
/// <see cref="Beat.EntityStale"/>.
///
/// The index side (<see cref="IndexBeatMentionsAsync"/>) is called by
/// <see cref="NodeWorkbenchService"/> after every beat write.
///
/// <para><b>COST — the save path is FREE and must stay that way.</b>
/// <see cref="ProcessEntityUpdateAsync"/>, <see cref="IndexBeatMentionsAsync"/> and its
/// <see cref="GetNameIndexAsync"/> name-index build are zero-LLM, deterministic bookkeeping,
/// and not duplicated anywhere else (checked against <see cref="WorldStateLedger"/> and
/// <see cref="EntityDocService"/>, which serve different jobs — ledger facts and DCM context
/// docs, not mention indexing).</para>
///
/// <para><b>Why the automatic LLM scan was removed (2026-09-12).</b> This service used to
/// fire <c>ScanDownstreamAsync</c> from <see cref="ProcessEntityUpdateAsync"/> as
/// <c>_ = Task.Run(…)</c> with <see cref="CancellationToken.None"/>: one LLM call per
/// (mentioning beat × every later beat in that beat's node). A protagonist is mentioned in
/// hundreds of beats, each of which enumerated every downstream beat in its chapter, and the
/// same downstream beat was re-checked once per upstream mention — quadratic within a chapter,
/// thousands of uncapped, un-costed, uncancellable calls from a single description edit. It
/// also asked the model for its verdict *before* its reason, the shape this project has ruled
/// against. The judgment call itself is worth keeping, so it now lives in
/// <see cref="ScanForContradictionsAsync"/>: explicit, capped, deduped, cost-scoped, and never
/// invoked from a save handler. It files nothing and rewrites nothing on its own — an entity
/// edit may surface findings for review, never touch prose (RFC 0009).</para>
/// </summary>
public class EntityRamificationService(
    IDbContextFactory<ProseDbContext> dbFactory,
    ILlmService llm,
    TokenLedger tokenLedger,
    ILogger<EntityRamificationService> log)
{
    /// <summary>Reason first, verdict last: the model must commit to an explanation before it
    /// commits to an answer. A verdict-first contract lets it guess and then rationalise.</summary>
    const string RamificationSystem =
        "You are a continuity checker for a fiction project. Reply with exactly two lines:\n" +
        "REASON: <one sentence explaining what in the beat does or does not conflict>\n" +
        "VERDICT: <CONFLICT or CONSISTENT>";

    // ── Public API ──────────────────────────────────────────────────────────

    /// <summary>
    /// Called when entity <paramref name="entityId"/> is saved. Marks every beat that mentions
    /// it <see cref="Beat.EntityStale"/> so the author can review them.
    /// <para><b>Free by contract.</b> This runs on the save path from an <c>OnEntitySaved</c>
    /// handler, so it must never make an LLM call, and nothing it calls may either. The
    /// judgment-based check lives in <see cref="ScanForContradictionsAsync"/>, which the author
    /// invokes deliberately. See the class doc for what this cost the project before the split.</para>
    /// </summary>
    public async Task<int> ProcessEntityUpdateAsync(Guid entityId, string entityName, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var directBeatIds = await db.BeatEntityMentions
            .Where(m => m.EntityId == entityId)
            .Select(m => m.BeatId)
            .ToListAsync(ct);

        if (directBeatIds.Count == 0) return 0;

        await db.Beats
            .Where(b => directBeatIds.Contains(b.Id))
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.EntityStale, true), ct);

        log.LogInformation(
            "EntityRamification: {Count} direct beat(s) flagged for entity {Name} ({Id})",
            directBeatIds.Count, entityName, entityId);

        return directBeatIds.Count;
    }

    /// <summary>How many beats mention this entity — the size of the set
    /// <see cref="ScanForContradictionsAsync"/> would check. Free; call it to price the scan
    /// before offering the button.</summary>
    public async Task<int> CountMentioningBeatsAsync(Guid entityId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.BeatEntityMentions.CountAsync(m => m.EntityId == entityId, ct);
    }

    /// <summary>
    /// Extracts entity name matches from <paramref name="beatText"/> and
    /// upserts <see cref="BeatEntityMention"/> rows for <paramref name="beatId"/>.
    /// Called after every beat write.
    /// </summary>
    public async Task IndexBeatMentionsAsync(Guid beatId, string beatText, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(beatText)) return;

        var index = await GetNameIndexAsync(ct);

        var hits = new Dictionary<Guid, NameEntry>();
        foreach (var entry in index)
        {
            if (hits.ContainsKey(entry.EntityId)) continue;
            if (ContainsWholeWord(beatText, entry.MatchText, entry.CaseSensitive))
                hits[entry.EntityId] = entry;
        }

        var mentioned = hits.Values
            .Select(e => new BeatEntityMention
            {
                BeatId     = beatId,
                EntityId   = e.EntityId,
                EntityName = e.CanonicalName,
                EntityType = e.EntityType,
                CreatedAt  = DateTime.UtcNow,
            })
            .ToList();

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        // Replace all existing mentions for this beat atomically.
        await db.BeatEntityMentions.Where(m => m.BeatId == beatId).ExecuteDeleteAsync(ct);
        if (mentioned.Count > 0)
        {
            db.BeatEntityMentions.AddRange(mentioned);
            await db.SaveChangesAsync(ct);
        }
    }

    // ── Name index (names + character aliases, whole-word matching) ─────────

    private sealed record NameEntry(Guid EntityId, string MatchText, string CanonicalName, string EntityType, bool CaseSensitive);

    /// <summary>Alias values that are ordinary English words (number words etc.) — too
    /// ambiguous to index even case-sensitively ("Eight seconds later…").</summary>
    private static readonly HashSet<string> AliasStopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "one","two","three","four","five","six","seven","eight","nine","ten",
        "eleven","twelve","thirteen","fourteen","fifteen","sixteen","seventeen","eighteen","nineteen","twenty",
        "thirty","forty","fifty","sixty","seventy","eighty","ninety","hundred","thousand",
        "north","south","east","west","left","right",
    };

    private static List<NameEntry>? nameIndexCache;
    private static DateTime nameIndexBuiltAt = DateTime.MinValue;

    /// <summary>
    /// Match texts for every active entity: the entity Name plus, for characters,
    /// every CharacterAliases value (this is what lets prose that says just "Kyle"
    /// index against "Kyle Ellen Corbin"). Cached for 60s so the bulk backfill
    /// builds it once instead of once per beat.
    /// </summary>
    private async Task<List<NameEntry>> GetNameIndexAsync(CancellationToken ct)
    {
        if (nameIndexCache is { } cached && (DateTime.UtcNow - nameIndexBuiltAt) < TimeSpan.FromSeconds(60))
            return cached;

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var entities = await db.Entities
            .Where(e => e.Name != "")
            .Select(e => new { e.Id, e.Name, e.EntityType })
            .ToListAsync(ct);

        var aliasRows = await db.Characters
            .SelectMany(c => c.Aliases.Select(a => new { c.Id, a.Value }))
            .ToListAsync(ct);
        aliasRows.AddRange(await db.Weapons
            .SelectMany(w => w.Aliases.Select(a => new { w.Id, a.Value }))
            .ToListAsync(ct));
        aliasRows.AddRange(await db.Places
            .SelectMany(p => p.Aliases.Select(a => new { p.Id, a.Value }))
            .ToListAsync(ct));
        aliasRows.AddRange(await db.Factions
            .SelectMany(f => f.Aliases.Select(a => new { f.Id, a.Value }))
            .ToListAsync(ct));

        var byId = entities.ToDictionary(e => e.Id);
        var index = new List<NameEntry>(entities.Count + aliasRows.Count);

        // Longer match texts first so a hit on "Kyle Ellen Corbin" short-circuits "Kyle".
        foreach (var e in entities)
            if (e.Name.Length >= 3)
                index.Add(new NameEntry(e.Id, e.Name, e.Name, e.EntityType, CaseSensitive: false));

        // Aliases are proper-noun handles: match case-SENSITIVELY ("Bear said" but not
        // "couldn't bear it"), skip lowercase-initial epithets ("the wall" would match
        // that literal phrase in any beat) and ordinary-word aliases.
        foreach (var a in aliasRows)
            if (!string.IsNullOrWhiteSpace(a.Value) && a.Value.Length >= 3
                && char.IsUpper(a.Value[0])
                && !AliasStopWords.Contains(a.Value)
                && byId.TryGetValue(a.Id, out var owner))
                index.Add(new NameEntry(owner.Id, a.Value, owner.Name, owner.EntityType, CaseSensitive: true));

        var built = index.OrderByDescending(e => e.MatchText.Length).ToList();
        nameIndexCache = built;
        nameIndexBuiltAt = DateTime.UtcNow;
        return built;
    }

    /// <summary>Case-insensitive whole-word containment: the match may not be
    /// flanked by letters or digits ("held" no longer matches "Eld").</summary>
    private static bool ContainsWholeWord(string text, string word, bool caseSensitive = false)
    {
        var cmp = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        int i = 0;
        while ((i = text.IndexOf(word, i, cmp)) >= 0)
        {
            bool leftOk  = i == 0 || !char.IsLetterOrDigit(text[i - 1]);
            int end = i + word.Length;
            bool rightOk = end >= text.Length || !char.IsLetterOrDigit(text[end]);
            if (leftOk && rightOk) return true;
            i += 1;
        }
        return false;
    }

    /// <summary>
    /// Bulk backfill: indexes mentions for every beat in the DB.
    /// Used by the <c>--scan-entity-mentions</c> CLI.
    /// </summary>
    public async Task BackfillAllBeatsAsync(IProgress<(int done, int total)>? progress = null, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var beatIds = await db.Beats
            .Select(b => new { b.Id, b.Text })
            .ToListAsync(ct);

        int done = 0;
        foreach (var beat in beatIds)
        {
            if (ct.IsCancellationRequested) break;
            await IndexBeatMentionsAsync(beat.Id, beat.Text, ct);
            progress?.Report((++done, beatIds.Count));
        }
    }

    // ── Entity-stale review helpers ─────────────────────────────────────────

    /// <summary>Returns all beats with <see cref="Beat.EntityStale"/> = true.</summary>
    public async Task<List<EntityStaleBeatDto>> GetEntityStaleBeatsAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        return await db.BeatNodes
            .Where(sb => sb.Beat!.EntityStale)
            .Select(sb => new EntityStaleBeatDto
            {
                BeatId      = sb.BeatId,
                BeatNumber  = sb.Beat!.Number,
                NodeId    = sb.NodeId,
                NodeTitle = sb.Node!.Title,
                SortKey     = sb.SortKey,
                TextPreview = string.IsNullOrEmpty(sb.Beat.Text) ? "" : sb.Beat.Text.Length > 120 ? sb.Beat.Text.Substring(0, 120) + "…" : sb.Beat.Text,
                Entities    = db.BeatEntityMentions
                    .Where(m => m.BeatId == sb.BeatId)
                    .Select(m => m.EntityName)
                    .ToList(),
            })
            .OrderBy(x => x.NodeTitle).ThenBy(x => x.SortKey)
            .ToListAsync(ct);
    }

    /// <summary>Clears <see cref="Beat.EntityStale"/> on a beat after author review.</summary>
    public async Task ClearEntityStaleAsync(Guid beatId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        await db.Beats
            .Where(b => b.Id == beatId)
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.EntityStale, false), ct);
    }

    // ── Contradiction scan (the ONE deliberate LLM cost — never call from a save) ──

    /// <summary>
    /// Checks every beat that mentions <paramref name="entityId"/> against that entity's current
    /// record, and reports the ones that contradict it. Files nothing and edits nothing — the
    /// caller decides what to do with the hits (RFC 0009: an entity edit never rewrites prose).
    ///
    /// <para>This is the project's one genuinely judgment-based entity check, and it costs real
    /// money, so it is deliberate by construction:</para>
    /// <list type="bullet">
    /// <item>each beat is checked <b>once</b> — the mention set is distinct by beat id, so a beat
    /// naming the entity five times is still one call (the old downstream walk re-checked the same
    /// beat once per upstream mention);</item>
    /// <item>hard-capped at <paramref name="maxBeats"/>, and the result says how many were skipped
    /// rather than silently truncating;</item>
    /// <item>wrapped in a <see cref="LlmActionContext.BeginCostScope"/> so the spend is
    /// attributable in <c>prose --cost --history</c> instead of landing on whatever command
    /// happened to be running;</item>
    /// <item>cancellable — a long scan can be abandoned and reports what it managed.</item>
    /// </list>
    /// </summary>
    /// <param name="maxBeats">Upper bound on LLM calls. Callers should show the caller
    /// <see cref="CountMentioningBeatsAsync"/> first so the author knows what they are buying.</param>
    public async Task<ContradictionScanResult> ScanForContradictionsAsync(
        Guid entityId,
        int maxBeats = 50,
        IProgress<(int done, int total)>? progress = null,
        CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        // IgnoreQueryFilters: this is an explicit-id lookup. The universe filter is for "show me
        // what's in scope"; a caller holding a specific guid has already decided what it wants,
        // and silently returning null here would look like "no such entity" rather than "wrong
        // ambient universe".
        var entity = await db.Entities.AsNoTracking().IgnoreQueryFilters()
            .Where(e => e.Id == entityId)
            .Select(e => new { e.Name, e.Description })
            .FirstOrDefaultAsync(ct);

        if (entity == null)
            return new ContradictionScanResult(0, 0, [], 0, false);

        // Distinct by beat id: one call per beat. BeatEntityMentions is keyed on
        // (BeatId, EntityId) so duplicates cannot exist, but the Distinct also makes the
        // one-call-per-beat guarantee explicit at the point it matters.
        //
        // Two round trips rather than a join: the id set is small (it is bounded by how many
        // beats name one entity), and a join whose outer side is a projected-then-Distinct
        // scalar sequence is the kind of shape a provider can translate into something that
        // silently returns nothing.
        var beatIds = await db.BeatEntityMentions
            .Where(m => m.EntityId == entityId)
            .Select(m => m.BeatId)
            .Distinct()
            .ToListAsync(ct);

        var beats = await db.Beats.AsNoTracking().IgnoreQueryFilters()
            .Where(b => beatIds.Contains(b.Id) && b.Text != "")
            .OrderBy(b => b.Number)
            .Select(b => new { b.Id, b.Number, b.Text })
            .ToListAsync(ct);

        var total = beats.Count;
        var toCheck = beats.Take(Math.Max(0, maxBeats)).ToList();
        var skipped = total - toCheck.Count;

        var hits = new List<ContradictionHit>();
        var cancelled = false;
        var desc = entity.Description ?? "";

        using var scope = LlmActionContext.BeginCostScope();

        var done = 0;
        foreach (var b in toCheck)
        {
            if (ct.IsCancellationRequested) { cancelled = true; break; }

            var hit = await CheckRamificationAsync(entity.Name, desc, b.Id, b.Number, b.Text, ct);
            if (hit != null) hits.Add(hit);

            // Incremented on its own line, deliberately. `progress?.Report((++done, …))` looks
            // equivalent but is not: the null-conditional short-circuits the ENTIRE expression,
            // arguments included, so with no progress callback the counter never moves and a scan
            // that really did run reports "0 beats checked".
            done++;
            progress?.Report((done, toCheck.Count));
        }

        var cost = tokenLedger.CostForScope(scope.Id);

        log.LogInformation(
            "EntityRamification: contradiction scan for {Name} ({Id}) — {Checked} beat(s) checked, " +
            "{Skipped} skipped, {Hits} hit(s), ${Cost:F4}{Cancelled}",
            entity.Name, entityId, done, skipped, hits.Count, cost, cancelled ? " (cancelled)" : "");

        return new ContradictionScanResult(done, skipped, hits, cost, cancelled);
    }

    // ── Private ─────────────────────────────────────────────────────────────

    private async Task<ContradictionHit?> CheckRamificationAsync(
        string entityName, string entityDesc, Guid beatId, int beatNumber, string beatText, CancellationToken ct)
    {
        var user =
            $"Entity \"{entityName}\" was just updated. Its current canonical description:\n" +
            $"{(entityDesc.Length > 400 ? entityDesc[..400] + "…" : entityDesc)}\n\n" +
            $"Beat text:\n{beatText}\n\n" +
            "Does this beat's content conflict with or contradict the current entity description?";

        try
        {
            var raw = await llm.GenerateAsync(RamificationSystem, user, temperature: 0.0f, maxTokens: 120);

            // Reason-first contract: pull the reason out, then decide on the verdict line.
            // Anything that doesn't clearly say CONFLICT is treated as consistent — a malformed
            // reply must never manufacture a finding.
            string reason = "";
            bool conflict = false;
            foreach (var line in raw.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (line.StartsWith("REASON:", StringComparison.OrdinalIgnoreCase))
                    reason = line["REASON:".Length..].Trim();
                else if (line.StartsWith("VERDICT:", StringComparison.OrdinalIgnoreCase))
                    conflict = line["VERDICT:".Length..].Trim()
                        .StartsWith("CONFLICT", StringComparison.OrdinalIgnoreCase);
            }

            return conflict ? new ContradictionHit(beatId, beatNumber, reason, Truncate(beatText, 200)) : null;
        }
        catch (Exception ex)
        {
            log.LogDebug(ex, "EntityRamification: LLM check skipped for entity {Name}", entityName);
            return null;
        }
    }

    private static string Truncate(string s, int max)
        => s.Length <= max ? s : s[..max] + "…";
}

/// <summary>One beat the contradiction scan judged to conflict with the entity's current record.</summary>
public record ContradictionHit(Guid BeatId, int BeatNumber, string Reason, string Snippet);

/// <summary>Outcome of one <see cref="EntityRamificationService.ScanForContradictionsAsync"/> run.
/// <paramref name="Checked"/> is how many beats were actually sent to the model,
/// <paramref name="Skipped"/> how many were left unchecked because the cap was hit.</summary>
public record ContradictionScanResult(
    int Checked, int Skipped, IReadOnlyList<ContradictionHit> Hits, double CostUsd, bool Cancelled);

public class EntityStaleBeatDto
{
    public Guid   BeatId      { get; set; }
    public int    BeatNumber  { get; set; }
    public Guid   NodeId    { get; set; }
    public string NodeTitle { get; set; } = "";
    public double SortKey     { get; set; }
    public string TextPreview { get; set; } = "";
    public List<string> Entities { get; set; } = [];
}
