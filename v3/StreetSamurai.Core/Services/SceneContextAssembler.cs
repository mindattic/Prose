using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using System.Text;
using System.Text.RegularExpressions;

namespace StreetSamurai.Core.Services;

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
    IDbContextFactory<StreetSamuraiDbContext> dbFactory,
    EmbeddingService embedding,
    Interfaces.ILlmService llm,
    FindingsService findings,
    WoundLedgerService wounds,
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
        foreach (var r in ctx.Roster)
            await db.Database.ExecuteSqlRawAsync(
                "INSERT INTO [dbo].[BeatEntities] ([BeatId],[EntityId],[Name],[EntityType],[MatchSource],[Score]) VALUES ({0},{1},{2},{3},{4},{5})",
                [beatId, r.EntityId, r.Name, r.EntityType, r.MatchSource, r.Score], ct);
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

        var ctx = await AssembleAsync(beat.Text, tokenBudget: 600, ct);
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
                    category: FindingCategory.Other,
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
        new(StringComparer.OrdinalIgnoreCase) { "chapter", "book", "strand", "series", "beat" };

    // name → (entityId, canonicalName, entityType, singleToken). Built once, refreshed lazily.
    private Dictionary<string, (Guid Id, string Name, string Type, bool SingleToken)>? nameIndex;
    private DateTime nameIndexBuiltAt;
    private static readonly TimeSpan NameIndexTtl = TimeSpan.FromMinutes(10);
    private readonly SemaphoreSlim indexLock = new(1, 1);

    /// <summary>Assemble the scene context for an existing beat.</summary>
    public async Task<SceneContext?> AssembleForBeatAsync(Guid beatId, int tokenBudget = 2000, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var beat = await db.Beats.AsNoTracking().FirstOrDefaultAsync(b => b.Id == beatId, ct);
        if (beat == null) return null;
        return await AssembleAsync(beat.Text, tokenBudget, ct);
    }

    /// <summary>Assemble the scene context for arbitrary prose text.</summary>
    public async Task<SceneContext> AssembleAsync(string proseText, int tokenBudget = 2000, CancellationToken ct = default)
    {
        var index = await GetNameIndexAsync(ct);

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

    private async Task<Dictionary<string, (Guid, string, string, bool)>> GetNameIndexAsync(CancellationToken ct)
    {
        if (nameIndex != null && DateTime.UtcNow - nameIndexBuiltAt < NameIndexTtl) return nameIndex;
        await indexLock.WaitAsync(ct);
        try
        {
            if (nameIndex != null && DateTime.UtcNow - nameIndexBuiltAt < NameIndexTtl) return nameIndex;

            var idx = new Dictionary<string, (Guid, string, string, bool)>(StringComparer.Ordinal);
            await using var db = await dbFactory.CreateDbContextAsync(ct);

            var entities = await db.Set<Entity>().AsNoTracking()
                .Where(e => e.IsActive && e.Status != "archived" && e.Name.Length >= 3)
                .Select(e => new { e.Id, e.Name, e.EntityType })
                .ToListAsync(ct);
            foreach (var e in entities)
                if (!ExcludedTypes.Contains(e.EntityType) && !e.Name.StartsWith('('))
                    idx.TryAdd(e.Name, (e.Id, e.Name, e.EntityType, !e.Name.Contains(' ')));

            // character aliases ("Pixel" for a character whose canonical name differs, etc.)
            var aliases = await db.Set<CharacterAlias>().AsNoTracking()
                .Where(a => a.Value.Length >= 3)
                .Select(a => new { a.CharacterId, a.Value })
                .ToListAsync(ct);
            var characterNames = await db.Set<Character>().AsNoTracking()
                .Select(c => new { c.Id, c.Name })
                .ToDictionaryAsync(c => c.Id, c => c.Name, ct);
            foreach (var a in aliases)
                if (characterNames.TryGetValue(a.CharacterId, out var canonical))
                    idx.TryAdd(a.Value, (a.CharacterId, canonical, "character", !a.Value.Contains(' ')));

            nameIndex = idx;
            nameIndexBuiltAt = DateTime.UtcNow;
            log.LogInformation("Scene name index built: {Count} triggers", idx.Count);
            return idx;
        }
        finally { indexLock.Release(); }
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

    private async Task<string> FormatCharacterAsync(StreetSamuraiDbContext db, SceneEntityRef r, CancellationToken ct)
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

            var lines = await db.Set<CharacterSpeechPhrase>().AsNoTracking()
                .Where(p => p.CharacterId == r.EntityId && p.Bucket == "example_lines")
                .OrderBy(p => p.Position).Take(3).Select(p => p.Phrase).ToListAsync(ct);
            if (lines.Count > 0)
                sb.AppendLine("VOICE — example lines: " + string.Join(" | ", lines.Select(l => $"\"{Clip(l, 140)}\"")));

            var woundBlock = await wounds.BuildPromptBlockAsync(r.EntityId, atInWorldDate: null, ct);
            if (woundBlock.Length > 0) sb.AppendLine(woundBlock);
        }
        sb.AppendLine();
        return sb.ToString();
    }

    private static async Task<string> FormatGenericAsync(StreetSamuraiDbContext db, SceneEntityRef r, CancellationToken ct)
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
