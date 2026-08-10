using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using System.Text;
using System.Text.RegularExpressions;

namespace Prose.Core.Services;

/// <summary>One entity on a scene's X-Ray roster and how it got there.</summary>
public sealed record SceneEntityRef(Guid EntityId, string Name, string EntityType, string MatchSource, double Score);

/// <summary>The assembled live memory block for a scene/beat (RFC 0002).</summary>
public sealed class SceneContext
{
    public required IReadOnlyList<SceneEntityRef> Roster { get; init; }
    public required string ContextBlock { get; init; }
    public int EstimatedTokens => ContextBlock.Length / 4;
}

/// <summary>
/// X-Ray scene assembly (RFC 0002 — docs/rfc/0002-entity-xray-scene-assembly.md).
/// Given the prose of a beat (or any scene text), assembles the entities that are
/// on screen — characters, places, objects — into one budgeted context block that
/// carries each entity's voice/psychology fields into the writing prompt.
///
/// Four passes, per the RFC's open-source survey conclusions:
///   1. NAME SCAN — lorebook-style trigger: entity names + character aliases matched
///      against the text (single-token names match case-sensitively so "Echo" the
///      operator does not fire on "echo" the noun; multi-token names are forgiving).
///   2. EMBEDDING PASS — EmbeddingService.FindSimilarAsync catches entities that are
///      thematically present but not named.
///   3. GRAPH EXPANSION — one hop along DB Edges from every matched entity
///      (carries/wields/located_at/partner_of…), confidence-floored by edge weight.
///   4. BUDGET GATE — rank (name > embedding > graph), format through per-type
///      formatters, stop at the token cap, then one recursive re-scan of the
///      included blocks (an included Kyle block naming Silence pulls Silence in).
/// </summary>
public class SceneContextAssembler(
    IDbContextFactory<ProseDbContext> dbFactory,
    EmbeddingService embedding,
    Interfaces.ILlmService llm,
    FindingsService findings,
    WoundLedgerService wounds,
    EntityDisambiguationService disambiguation,
    ILogger<SceneContextAssembler> log)
{
    private bool schemaEnsured;

    /// <summary>BeatEntities — the persisted X-Ray index (RFC 0002). Raw idempotent DDL,
    /// same pattern as FindingsService.EnsureSchema (EnsureCreated cannot add tables
    /// to an existing DB).</summary>
    private async Task EnsureSchemaAsync(CancellationToken ct)
    {
        if (schemaEnsured) return;
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        await db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'[dbo].[BeatEntities]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[BeatEntities] (
                    [BeatId]      UNIQUEIDENTIFIER NOT NULL,
                    [EntityId]    UNIQUEIDENTIFIER NOT NULL,
                    [Name]        NVARCHAR(450)    NOT NULL,
                    [EntityType]  NVARCHAR(80)     NOT NULL,
                    [MatchSource] NVARCHAR(20)     NOT NULL,
                    [Score]       FLOAT            NOT NULL,
                    [AssembledAt] DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
                    CONSTRAINT [PK_BeatEntities] PRIMARY KEY ([BeatId], [EntityId])
                );
                CREATE INDEX [IX_BeatEntities_EntityId] ON [dbo].[BeatEntities]([EntityId]);
            END;
            """, ct);
        schemaEnsured = true;
    }

    /// <summary>Persist a beat's roster to BeatEntities (replace semantics).</summary>
    public async Task PersistRosterAsync(Guid beatId, SceneContext ctx, CancellationToken ct = default)
    {
        await EnsureSchemaAsync(ct);
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        await db.Database.ExecuteSqlRawAsync("DELETE FROM [dbo].[BeatEntities] WHERE [BeatId] = {0}", [beatId], ct);
        if (ctx.Roster.Count == 0) return;
        var sql = new System.Text.StringBuilder(
            "INSERT INTO [dbo].[BeatEntities] ([BeatId],[EntityId],[Name],[EntityType],[MatchSource],[Score]) VALUES ");
        var parameters = new List<object?> { beatId };
        for (int i = 0; i < ctx.Roster.Count; i++)
        {
            var r = ctx.Roster[i];
            int b = 1 + i * 5;
            if (i > 0) sql.Append(',');
            sql.Append($"({{0}},{{{b}}},{{{b+1}}},{{{b+2}}},{{{b+3}}},{{{b+4}}})");
            parameters.Add(r.EntityId);
            parameters.Add(r.Name);
            parameters.Add(r.EntityType);
            parameters.Add(r.MatchSource);
            parameters.Add((object?)r.Score);
        }
        await db.Database.ExecuteSqlRawAsync(sql.ToString(), parameters.Cast<object>().ToArray(), ct);
    }

    /// <summary>
    /// The reverse direction (RFC 0002): the story reveals details about entities; this
    /// PROPOSES them as findings (prefix "XRAY-REVEAL") for explicit human approval —
    /// never auto-routed onto canon, per the standing write-back rule.
    /// Returns the number of proposals filed.
    /// </summary>
    public async Task<int> HarvestRevealedDetailsAsync(Guid beatId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var beat = await db.Beats.AsNoTracking().FirstOrDefaultAsync(b => b.Id == beatId, ct);
        if (beat == null || string.IsNullOrWhiteSpace(beat.Text) || beat.Text.Length < 120) return 0;

        var ownerNodeId = await db.BeatNodes.AsNoTracking()
            .Where(bn => bn.BeatId == beatId).Select(bn => (Guid?)bn.NodeId).FirstOrDefaultAsync(ct);
        var ctx = await AssembleAsync(beat.Text, tokenBudget: 600, ct, ownerNodeId);
        var named = ctx.Roster.Where(r => r.MatchSource == "name").Take(6).ToList();
        if (named.Count == 0) return 0;

        var system =
            "You audit fiction against a canon database. Given a passage and the entities known to be in it, " +
            "list concrete, durable details the passage REVEALS about each entity that a canon record should carry — " +
            "physical traits, history, capabilities, possessions, relationships, speech habits. " +
            "Durable facts only: no momentary actions, no emotions of the moment, no plot events. " +
            "Return ONLY a JSON array (possibly empty): [{\"entity\":\"<name>\",\"detail\":\"<one concrete fact, one sentence>\",\"quote\":\"<short supporting quote>\"}]";
        var user = $"ENTITIES IN SCENE: {string.Join(", ", named.Select(n => n.Name))}\n\nPASSAGE:\n{beat.Text}";

        string raw;
        try { raw = await llm.GenerateAsync(system, user, temperature: 0.2, maxTokens: 800, ct: ct); }
        catch (Exception ex) { log.LogWarning(ex, "XRAY harvest LLM call failed for beat {Beat}", beatId); return 0; }

        var start = raw.IndexOf('[');
        var end = raw.LastIndexOf(']');
        if (start < 0 || end <= start) return 0;

        int filed = 0;
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(raw[start..(end + 1)]);
            foreach (var item in doc.RootElement.EnumerateArray())
            {
                var entity = item.TryGetProperty("entity", out var e) ? e.GetString() : null;
                var detail = item.TryGetProperty("detail", out var d) ? d.GetString() : null;
                var quote = item.TryGetProperty("quote", out var q) ? q.GetString() : null;
                if (string.IsNullOrWhiteSpace(entity) || string.IsNullOrWhiteSpace(detail)) continue;
                if (named.All(n => !n.Name.Contains(entity, StringComparison.OrdinalIgnoreCase)
                                && !entity.Contains(n.Name, StringComparison.OrdinalIgnoreCase))) continue;
                findings.Upsert(
                    filePath: $"beat:{beatId:N}",
                    chapterId: null,
                    category: FindingCategory.Xray,
                    severity: FindingSeverity.Low,
                    summary: $"XRAY-REVEAL [{entity}]: {detail}",
                    snippet: quote,
                    suggestedFix: $"If true and missing from canon, apply to the {entity} record (explicit field pick — never auto-route).");
                filed++;
            }
        }
        catch (System.Text.Json.JsonException ex)
        {
            log.LogWarning(ex, "XRAY harvest returned unparseable JSON for beat {Beat}", beatId);
        }
        return filed;
    }

    private const double EmbeddingFloor = 0.50;
    private const int GraphNeighborCap = 8;
    private const int CharsPerToken = 4;

    // Structural/registry entity types that are never "on screen" in a scene.
    private static readonly HashSet<string> ExcludedTypes =
        new(StringComparer.OrdinalIgnoreCase) { "chapter", "book", "node", "series", "beat" };

    // name → all entities sharing that Name (usually exactly one). Built once, refreshed lazily,
    // universe-wide (not per-book) — same-name collisions across different books' entities (see
    // EntityDisambiguationService) are resolved per-call in ResolveIndexForContext, not here.
    private Dictionary<string, List<(Guid Id, string Name, string Type, bool SingleToken, Guid? OriginNodeId)>>? nameIndex;
    private DateTime nameIndexBuiltAt;
    private int nameIndexBuiltEpoch = -1;
    private static readonly TimeSpan NameIndexTtl = TimeSpan.FromMinutes(10);
    private readonly SemaphoreSlim indexLock = new(1, 1);

    // Budget reserved for the storytelling-science block so it is accounted for
    // before the entity roster consumes the remaining tokens.
    private const int ScienceBlockCharBudget = 350;

    /// <summary>Assemble the scene context for an existing beat.</summary>
    /// <remarks>
    /// When persisted narrative-science findings exist for this beat
    /// (written by <c>prose --narrative-science … --slug …</c>) they are injected as a compact
    /// deterministic guidance block — zero extra LLM cost. The block is capped at
    /// ~350 chars (~88 tokens) and deducted from the token budget before the
    /// entity roster is assembled so the total never overruns the caller's cap.
    /// </remarks>
    public async Task<SceneContext?> AssembleForBeatAsync(Guid beatId, int tokenBudget = 2000, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var beat = await db.Beats.AsNoTracking().FirstOrDefaultAsync(b => b.Id == beatId, ct);
        if (beat == null) return null;

        // P4: inject stored science guidance (deterministic DB read, no LLM call).
        var scienceBlock = BuildScienceBlock(beatId);
        var effectiveBudget = scienceBlock.Length > 0
            ? Math.Max(tokenBudget - scienceBlock.Length / CharsPerToken, 200)
            : tokenBudget;

        var ownerNodeId = await db.BeatNodes.AsNoTracking()
            .Where(bn => bn.BeatId == beatId).Select(bn => (Guid?)bn.NodeId).FirstOrDefaultAsync(ct);
        var ctx = await AssembleAsync(beat.Text ?? "", effectiveBudget, ct, ownerNodeId);
        if (scienceBlock.Length == 0) return ctx;

        return new SceneContext
        {
            Roster       = ctx.Roster,
            ContextBlock = scienceBlock + ctx.ContextBlock,
        };
    }

    /// <summary>
    /// Read any persisted NARRATIVE-SCIENCE findings for this beat and format
    /// them as a compact guidance block for the prose prompt.
    /// Returns an empty string when no findings exist.
    /// </summary>
    private string BuildScienceBlock(Guid beatId)
    {
        const string dqPrefix = "NARRATIVE-SCIENCE [dramatic-question]:";
        const string sePrefix = "NARRATIVE-SCIENCE [scene-engagement]:";

        var allFindings = findings.ListByFilePathPrefix($"beat:{beatId:N}");
        var dq = allFindings.FirstOrDefault(f => f.Summary?.StartsWith(dqPrefix, StringComparison.OrdinalIgnoreCase) == true);
        var se = allFindings.FirstOrDefault(f => f.Summary?.StartsWith(sePrefix, StringComparison.OrdinalIgnoreCase) == true);

        if (dq == null && se == null) return "";

        var sb = new StringBuilder();
        sb.AppendLine("STORYTELLING SCIENCE FOR THIS BEAT (make the prose satisfy these):");
        if (dq != null)
        {
            // Strip the "NARRATIVE-SCIENCE [dramatic-question]: Beat #N — " prefix for compactness.
            var text = StripNsPrefix(dq.Summary ?? "", dqPrefix);
            sb.AppendLine($"- dramatic question: {Clip(text, 150)}");
        }
        if (se != null)
        {
            var text = StripNsPrefix(se.Summary ?? "", sePrefix);
            sb.AppendLine($"- scene engagement: {Clip(text, 150)}");
            if (!string.IsNullOrWhiteSpace(se.SuggestedFix))
                sb.AppendLine($"  fix: {Clip(se.SuggestedFix, 80)}");
        }
        sb.AppendLine();
        return sb.ToString();
    }

    private static string StripNsPrefix(string summary, string prefix)
    {
        if (summary.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return summary[prefix.Length..].TrimStart();
        return summary;
    }

    /// <summary>Assemble the scene context for arbitrary prose text.</summary>
    /// <param name="contextNodeId">
    /// The current beat/scene's owning Node (chapter, typically), when known. Used only to
    /// disambiguate a Name that collides across more than one entity (see
    /// <see cref="EntityDisambiguationService"/>) — omit for callers with no book context
    /// (e.g. an ad-hoc text snippet not tied to any node), which falls back to universe-wide
    /// (OriginNodeId == null) entities for any colliding name. Kept as the LAST parameter
    /// (after <paramref name="ct"/>) specifically so every pre-existing positional call site
    /// (<c>AssembleAsync(text, tokenBudget: N, ct)</c>) keeps binding <c>ct</c> to the
    /// CancellationToken slot unchanged — only callers that want disambiguation need to pass
    /// this one by name.
    /// </param>
    public async Task<SceneContext> AssembleAsync(string proseText, int tokenBudget = 2000, CancellationToken ct = default, Guid? contextNodeId = null)
    {
        var rawIndex = await GetNameIndexAsync(ct);
        var index = await ResolveIndexForContextAsync(rawIndex, contextNodeId, ct);

        // 1 — name/alias scan (the lorebook trigger).
        var matched = new Dictionary<Guid, SceneEntityRef>();
        foreach (var hit in ScanNames(proseText, index))
            matched.TryAdd(hit.EntityId, hit);

        // 2 — embedding pass for unnamed-but-relevant entities.
        try
        {
            var similar = await embedding.FindSimilarAsync(proseText, k: 6, entityTypes: null, ct);
            foreach (var s in similar)
                if (s.Similarity >= EmbeddingFloor && !ExcludedTypes.Contains(s.EntityType))
                    matched.TryAdd(s.EntityId, new SceneEntityRef(s.EntityId, s.EntityName, s.EntityType, "embedding", 2.0 * s.Similarity));
        }
        catch (Exception ex)
        {
            // The assembler must work offline — name scan + graph still deliver.
            log.LogWarning(ex, "Embedding pass unavailable; assembling from name scan + graph only");
        }

        // 3 — one-hop graph expansion from everything matched so far.
        await using (var db = await dbFactory.CreateDbContextAsync(ct))
        {
            var ids = matched.Keys.ToList();
            if (ids.Count > 0)
            {
                var edges = await db.Set<Edge>().AsNoTracking()
                    .Where(e => ids.Contains(e.SourceId) || ids.Contains(e.TargetId))
                    .OrderByDescending(e => e.Weight)
                    .Take(64)
                    .ToListAsync(ct);

                var neighborIds = edges
                    .SelectMany(e => new[] { e.SourceId, e.TargetId })
                    .Where(id => !matched.ContainsKey(id))
                    .Distinct()
                    .Take(GraphNeighborCap)
                    .ToList();

                if (neighborIds.Count > 0)
                {
                    var neighbors = await db.Set<Entity>().AsNoTracking()
                        .Where(en => neighborIds.Contains(en.Id) && en.IsActive && en.Status != "archived")
                        .ToListAsync(ct);
                    neighbors.RemoveAll(n => ExcludedTypes.Contains(n.EntityType));
                    foreach (var n in neighbors)
                    {
                        var w = edges.Where(e => e.SourceId == n.Id || e.TargetId == n.Id).Max(e => e.Weight);
                        matched.TryAdd(n.Id, new SceneEntityRef(n.Id, n.Name, n.EntityType, "graph", Math.Min(w, 1.0)));
                    }
                }
            }
        }

        // 4 — budget gate: rank, format, cap; then one recursive re-scan.
        var ranked = matched.Values.OrderByDescending(r => r.Score).ToList();
        var (roster, block) = await FormatWithinBudgetAsync(ranked, tokenBudget, ct);

        var extra = ScanNames(block, index)
            .Where(h => roster.All(r => r.EntityId != h.EntityId))
            .ToList();
        if (extra.Count > 0)
        {
            var remaining = tokenBudget - block.Length / CharsPerToken;
            if (remaining > 100)
            {
                var (roster2, block2) = await FormatWithinBudgetAsync(extra, remaining, ct);
                roster = roster.Concat(roster2).ToList();
                block += block2;
            }
        }

        return new SceneContext { Roster = roster, ContextBlock = block };
    }

    // ── pass 1: the name/alias trigger ─────────────────────────────────────────

    private static IEnumerable<SceneEntityRef> ScanNames(
        string text, Dictionary<string, (Guid Id, string Name, string Type, bool SingleToken)> index)
    {
        foreach (var (key, e) in index)
        {
            // cheap containment first, exact rules second
            var cmp = e.SingleToken ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            var at = text.IndexOf(key, cmp);
            if (at < 0) continue;
            if (!Regex.IsMatch(text, $@"\b{Regex.Escape(key)}\b",
                    e.SingleToken ? RegexOptions.None : RegexOptions.IgnoreCase))
                continue;
            yield return new SceneEntityRef(e.Id, e.Name, e.Type, "name", 3.0);
        }
    }

    // Bare single tokens AND article+noun names ("The Ledger", "The Spine") hold to
    // case-sensitive matching: their nouns are ordinary prose words ("the ledger is
    // open") and ignore-case containment attaches the wrong entity to the scene —
    // the BLST contamination vector. Multi-word proper names keep ignore-case.
    private static bool RequiresStrictCase(string name)
    {
        var tokens = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return tokens.Length == 1
            || (tokens.Length == 2 && tokens[0].ToLowerInvariant() is "the" or "a" or "an");
    }

    private async Task<Dictionary<string, List<(Guid Id, string Name, string Type, bool SingleToken, Guid? OriginNodeId)>>> GetNameIndexAsync(CancellationToken ct)
    {
        // Universe-switch invalidation (mirrors WorldGraphService's builtEpoch pattern): a
        // process that switches universe mid-run (CLI --universe, MCP switch_universe, the
        // web dropdown) must not keep serving the previous universe's roster for up to
        // NameIndexTtl minutes.
        var currentEpoch = UniverseScope.Epoch;
        if (nameIndex != null && nameIndexBuiltEpoch == currentEpoch && DateTime.UtcNow - nameIndexBuiltAt < NameIndexTtl)
            return nameIndex;
        await indexLock.WaitAsync(ct);
        try
        {
            if (nameIndex != null && nameIndexBuiltEpoch == currentEpoch && DateTime.UtcNow - nameIndexBuiltAt < NameIndexTtl)
                return nameIndex;

            // List-valued (not TryAdd-first-wins): two entities CAN legitimately share a Name
            // within one Universe — e.g. a historical/citation-grounded research entry and a
            // literary-fictional character of the same name, scoped to different books via
            // OriginNodeId. Collecting every candidate here and resolving per-call (see
            // ResolveIndexForContext) is what makes that distinction real instead of a coin flip.
            var idx = new Dictionary<string, List<(Guid, string, string, bool, Guid?)>>(StringComparer.Ordinal);
            await using var db = await dbFactory.CreateDbContextAsync(ct);

            var entities = await db.Set<Entity>().AsNoTracking()
                .Where(e => e.IsActive && e.Status != "archived" && e.Name.Length >= 3)
                .Select(e => new { e.Id, e.Name, e.EntityType, e.OriginNodeId })
                .ToListAsync(ct);
            foreach (var e in entities)
                if (!ExcludedTypes.Contains(e.EntityType) && !e.Name.StartsWith('('))
                {
                    if (!idx.TryGetValue(e.Name, out var list))
                        idx[e.Name] = list = new();
                    list.Add((e.Id, e.Name, e.EntityType, RequiresStrictCase(e.Name), e.OriginNodeId));
                }

            // character aliases ("Pixel" for a character whose canonical name differs, etc.).
            // Character/CharacterAlias carry no UniverseId of their own (Character.Id IS the
            // parent Entity.Id — see FormatCharacterAsync, which looks both up by the same id).
            // Scope aliases to the entityIds set above (already correctly filtered by the
            // ambient universe via Entity's HasQueryFilter) instead of trusting Character/
            // CharacterAlias directly — otherwise a GLMZ/SCRY character's alias leaks into a
            // GSPL (or any other universe's) name-scan regardless of which universe is active.
            var entityById = entities.ToDictionary(e => e.Id);
            var entityIds = entityById.Keys.ToHashSet();
            var aliases = await db.Set<CharacterAlias>().AsNoTracking()
                .Where(a => a.Value.Length >= 3)
                .Select(a => new { a.CharacterId, a.Value })
                .ToListAsync(ct);
            var characterNames = await db.Set<Character>().AsNoTracking()
                .Select(c => new { c.Id, c.Name })
                .ToDictionaryAsync(c => c.Id, c => c.Name, ct);
            foreach (var a in aliases)
                if (entityIds.Contains(a.CharacterId) && characterNames.TryGetValue(a.CharacterId, out var canonical))
                {
                    if (!idx.TryGetValue(a.Value, out var list))
                        idx[a.Value] = list = new();
                    list.Add((a.CharacterId, canonical, "character", RequiresStrictCase(a.Value), entityById[a.CharacterId].OriginNodeId));
                }

            nameIndex = idx;
            nameIndexBuiltAt = DateTime.UtcNow;
            nameIndexBuiltEpoch = currentEpoch;
            log.LogInformation("Scene name index built: {Count} triggers", idx.Count);
            return idx;
        }
        finally { indexLock.Release(); }
    }

    /// <summary>
    /// Collapses the raw (possibly multi-candidate) name index into the single-candidate shape
    /// <see cref="ScanNames"/> expects, resolving any same-name collisions via
    /// <see cref="EntityDisambiguationService"/> against the current scene's book/series context.
    /// Cheap: only names with more than one candidate ever call the disambiguation service.
    /// </summary>
    private async Task<Dictionary<string, (Guid Id, string Name, string Type, bool SingleToken)>> ResolveIndexForContextAsync(
        Dictionary<string, List<(Guid Id, string Name, string Type, bool SingleToken, Guid? OriginNodeId)>> rawIndex,
        Guid? contextNodeId,
        CancellationToken ct)
    {
        Guid? contextBookId = null;
        if (contextNodeId is { } cid)
            contextBookId = await disambiguation.ResolveNearestBookOrSeriesNodeIdAsync(cid, ct);

        var resolved = new Dictionary<string, (Guid, string, string, bool)>(rawIndex.Count, StringComparer.Ordinal);
        foreach (var (name, candidates) in rawIndex)
        {
            var best = candidates.Count == 1
                ? candidates[0]
                : disambiguation.ResolveBestMatch(candidates, c => c.OriginNodeId, contextBookId, name);
            resolved[name] = (best.Id, best.Name, best.Type, best.SingleToken);
        }
        return resolved;
    }

    // ── pass 4: per-type formatters + budget ───────────────────────────────────

    private async Task<(List<SceneEntityRef> roster, string block)> FormatWithinBudgetAsync(
        IReadOnlyList<SceneEntityRef> ranked, int tokenBudget, CancellationToken ct)
    {
        var sb = new StringBuilder();
        var roster = new List<SceneEntityRef>();
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        foreach (var r in ranked)
        {
            // Defensive: a malformed candidate (null EntityType) must not abort the whole
            // batch — found 2026-08-10 via --backfill-entity-presence hitting a corpus book
            // whose ranked list produced one such entry; root candidate not fully isolated
            // (every direct Entities/CharacterAlias read confirmed non-null EntityType), so
            // this guards the actual crash site rather than a still-unconfirmed upstream one.
            if (r is null || string.IsNullOrWhiteSpace(r.EntityType))
            {
                log.LogWarning("[SceneContextAssembler] Skipping ranked candidate with null/empty EntityType (EntityId={Id})", r?.EntityId);
                continue;
            }
            var entry = r.EntityType.Equals("character", StringComparison.OrdinalIgnoreCase)
                ? await FormatCharacterAsync(db, r, ct)
                : await FormatGenericAsync(db, r, ct);
            if (string.IsNullOrWhiteSpace(entry)) continue;
            if ((sb.Length + entry.Length) / CharsPerToken > tokenBudget && roster.Count > 0) break;
            sb.Append(entry);
            roster.Add(r);
        }
        return (roster, sb.ToString());
    }

    private async Task<string> FormatCharacterAsync(ProseDbContext db, SceneEntityRef r, CancellationToken ct)
    {
        var c = await db.Set<Character>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == r.EntityId, ct);
        var entity = await db.Set<Entity>().AsNoTracking()
            .Where(e => e.Id == r.EntityId).FirstOrDefaultAsync(ct);

        var sb = new StringBuilder();
        sb.AppendLine($"## {r.Name}  (character, in scene via {r.MatchSource})");
        if (!string.IsNullOrWhiteSpace(entity?.Description)) sb.AppendLine(Clip(entity.Description, 400));
        if (!string.IsNullOrWhiteSpace(entity?.GrammarNote))  sb.AppendLine($"GRAMMAR: {entity.GrammarNote}");
        if (c != null)
        {
            AppendField(sb, "VOICE — vocabulary", c.SpeechVocabulary);
            AppendField(sb, "VOICE — cadence", c.SpeechCadence);
            AppendField(sb, "VOICE — subtext", c.SpeechSubtext);
            AppendField(sb, "VOICE — under pressure", c.SpeechUnderPressure);
            AppendField(sb, "VOICE — intimacy register", c.SpeechIntimacyRegister);
            if (!string.IsNullOrWhiteSpace(c.NarrationVoice))
                AppendField(sb, "NARRATION VOICE", c.NarrationVoice);
            // SS-A46 register field 6/6 — informs subtext/evasion, never stated outright on the page.
            AppendField(sb, "PSYCHOLOGY — secret", c.PsychologySecret);

            var lines = await db.Set<CharacterSpeechPhrase>().AsNoTracking()
                .Where(p => p.CharacterId == r.EntityId && p.Bucket == "example_lines")
                .OrderBy(p => p.Position).Take(3).Select(p => p.Phrase).ToListAsync(ct);
            if (lines.Count > 0)
                sb.AppendLine("VOICE — example lines: " + string.Join(" | ", lines.Select(l => $"\"{Clip(l, 140)}\"")));

            var woundBlock = await wounds.BuildPromptBlockAsync(r.EntityId, atInWorldDate: null, ct);
            if (woundBlock.Length > 0) sb.AppendLine(woundBlock);

            // Behavioral rules — injected at write-time so characters act in-character
            // without needing a post-generation LLM check (RFC 0009 §5 Part B).
            // Cap at ~400 chars total to respect the scene-context token budget.
            await AppendBehavioralRulesAsync(db, sb, r.EntityId, ct);
        }
        sb.AppendLine();
        return sb.ToString();
    }

    /// <summary>
    /// Appends a compact "HOW THEY DECIDE" block from the character's
    /// <c>CharacterBehavioralRules</c> rows. Bounded to ~400 characters total.
    /// No-op if the character has no rules.
    /// </summary>
    private static async Task AppendBehavioralRulesAsync(
        ProseDbContext db, StringBuilder sb, Guid characterId, CancellationToken ct)
    {
        const int MaxBehavioralChars = 400;

        var rules = await db.Set<CharacterBehavioralRule>().AsNoTracking()
            .Where(r => r.CharacterId == characterId)
            .OrderBy(r => r.Bucket).ThenBy(r => r.Position)
            .ToListAsync(ct);

        if (rules.Count == 0) return;

        // Map canonical bucket names to the display label we show in the block.
        // Unknown buckets are included under a generic label.
        var bucketLabels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["decision_rules"]    = "decisions",
            ["escalation_ladder"] = "escalates",
            ["breaking_points"]   = "breaks at",
        };

        // Priority order for the block (most useful to a writer at the point of generation).
        var priorityOrder = new[] { "decision_rules", "escalation_ladder", "breaking_points", "habits", "contradictions" };

        var grouped = rules.GroupBy(r => r.Bucket, StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => Array.IndexOf(priorityOrder, g.Key.ToLowerInvariant()) is int idx && idx >= 0 ? idx : 99);

        var block = new StringBuilder();
        block.AppendLine("HOW THEY DECIDE (honor these — they are not a plot puppet):");
        foreach (var grp in grouped)
        {
            if (block.Length >= MaxBehavioralChars) break;
            var label = bucketLabels.GetValueOrDefault(grp.Key, grp.Key);
            var ruleTexts = grp.Select(r => r.Rule).Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
            if (ruleTexts.Count == 0) continue;
            // Clip the concatenated rules for this bucket.
            var combined = string.Join("; ", ruleTexts);
            var remaining = MaxBehavioralChars - block.Length;
            if (remaining <= 0) break;
            block.AppendLine($"- {label}: {Clip(combined, Math.Max(40, remaining - label.Length - 4))}");
        }

        if (block.Length > 2) // more than just the header line
            sb.Append(block);
    }

    private static async Task<string> FormatGenericAsync(ProseDbContext db, SceneEntityRef r, CancellationToken ct)
    {
        var entity = await db.Set<Entity>().AsNoTracking()
            .Where(e => e.Id == r.EntityId).FirstOrDefaultAsync(ct);
        var sb = new StringBuilder();
        sb.AppendLine($"## {r.Name}  ({r.EntityType}, in scene via {r.MatchSource})");
        if (!string.IsNullOrWhiteSpace(entity?.Description)) sb.AppendLine(Clip(entity.Description, 500));
        if (!string.IsNullOrWhiteSpace(entity?.GrammarNote))  sb.AppendLine($"GRAMMAR: {entity.GrammarNote}");
        sb.AppendLine();
        return sb.ToString();
    }

    private static void AppendField(StringBuilder sb, string label, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value)) sb.AppendLine($"{label}: {Clip(value, 350)}");
    }

    private static string Clip(string s, int max) =>
        s.Length <= max ? s : s[..max].TrimEnd() + "…";
}
