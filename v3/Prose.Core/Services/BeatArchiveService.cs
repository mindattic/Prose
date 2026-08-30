using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Prose.Core.Services;

/// <summary>
/// The Beat Context Archive (observability plan Part F5) — the single, shared assembly of
/// "everything that fed one beat," resolved as-of that beat's own <c>BeatContextTrace</c>
/// timestamp. Both <c>Prose.Cli</c>'s <c>BeatArchiveCli</c> and <c>Prose.Mcp</c>'s
/// <c>BeatArchiveTools</c> (and therefore the Beat Archive UI tab, via the generic MCP
/// dispatch every tab already uses) call this one method — no duplicated assembly logic.
/// </summary>
public class BeatArchiveService(
    IDbContextFactory<Data.ProseDbContext> dbFactory,
    WorldStateService worldState,
    MarkdownFileService markdownFiles,
    NodeOutlineService nodeBible)
{
    private sealed class BeatEntityRosterRow
    {
        public Guid EntityId { get; set; }
        public string Name { get; set; } = "";
        public string EntityType { get; set; } = "";
        public string MatchSource { get; set; } = "";
        public double Score { get; set; }
    }

    private sealed class PovRow
    {
        public Guid EntityId { get; set; }
    }

    private sealed class EdgeQueryRow
    {
        public Guid SourceId { get; set; }
        public Guid TargetId { get; set; }
        public string RelationType { get; set; } = "";
        public string Sentiment { get; set; } = "";
        public double Weight { get; set; }
    }

    public sealed record ServiceCoverageRow(string Service, bool WasApplicable, bool WasActive, int BlockSizeChars);
    public sealed record PromptRow(DateTime At, string ProviderId, string Model, string System, string User, string? Response, int? ElapsedMs);
    public sealed record EntityRosterRow(Guid EntityId, string Name, string EntityType, string MatchSource, double Score, bool IsPov, string? ResolvedJson);
    public sealed record DocRow(string? Path, string? Tier, string? Content);
    public sealed record ModeRow(string Mode, float Confidence, string DetectionMethod);
    public sealed record BeatRow(Guid Id, string? Title, string Kind, string Text, DateTime UpdatedAt);
    public sealed record EdgeRow(Guid SourceId, string? SourceName, Guid TargetId, string? TargetName, string RelationType, string Sentiment, double Weight);

    public sealed record Archive(
        BeatRow Beat,
        Guid NodeId,
        DateTime AsOf,
        ModeRow? Mode,
        IReadOnlyList<ServiceCoverageRow> ServiceCoverage,
        string? ContextTrace,
        IReadOnlyList<PromptRow> Prompts,
        IReadOnlyList<EntityRosterRow> EntityRoster,
        IReadOnlyList<EdgeRow> Edges,
        IReadOnlyList<DocRow> Docs,
        string? Bible);

    public async Task<Archive?> BuildArchiveAsync(Guid beatId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var beat = await db.Beats.AsNoTracking().FirstOrDefaultAsync(b => b.Id == beatId, ct);
        if (beat == null) return null;

        var beatNode = await db.BeatNodes.AsNoTracking().FirstOrDefaultAsync(bn => bn.BeatId == beatId, ct);
        var nodeId = beatNode?.NodeId ?? Guid.Empty;

        var modeLog = await db.BeatModeLogs.AsNoTracking().FirstOrDefaultAsync(m => m.BeatId == beatId, ct);
        var serviceCoverage = await db.BeatServiceLogs.AsNoTracking()
            .Where(s => s.BeatId == beatId).OrderBy(s => s.Service).ToListAsync(ct);
        var trace = await db.BeatContextTraces.AsNoTracking()
            .Where(t => t.BeatId == beatId).OrderByDescending(t => t.CreatedAt).FirstOrDefaultAsync(ct);
        var prompts = await db.LlmPromptCaptures.AsNoTracking()
            .Where(p => p.BeatId == beatId).OrderBy(p => p.At).ToListAsync(ct);
        var dcmDocs = await db.DcmBeatSnapshots.AsNoTracking()
            .Where(d => d.BeatId == beatId.ToString("N")).OrderByDescending(d => d.Id).FirstOrDefaultAsync(ct);

        // The one timestamp guaranteed to exist for every beat, trace or no trace — the
        // moment BeatContextTrace was (or would have been) written, right before the LLM call.
        var asOf = trace?.CreatedAt ?? beat.UpdatedAt;

        var roster = await db.Database.SqlQueryRaw<BeatEntityRosterRow>(
            "SELECT [EntityId], [Name], [EntityType], [MatchSource], [Score] FROM [dbo].[BeatEntities] WHERE [BeatId] = {0}",
            beatId).ToListAsync(ct);
        var povRow = await db.Database.SqlQueryRaw<PovRow>(
            "SELECT [EntityId] FROM [dbo].[BeatEntityPresence] WHERE [BeatId] = {0} AND [PresenceType] = 'pov'",
            beatId).FirstOrDefaultAsync(ct);

        var resolvedRoster = roster.Select(r => new EntityRosterRow(
            r.EntityId, r.Name, r.EntityType, r.MatchSource, r.Score,
            IsPov: povRow != null && povRow.EntityId == r.EntityId,
            ResolvedJson: worldState.GetRecordJsonAsOf(r.EntityId.ToString(), asOf))).ToList();

        // Which edges were active for THIS beat's roster, as of this beat's own moment — the
        // "quikgraph state" half of the archive. Edges is system-versioned (SystemVersionedTables),
        // so FOR SYSTEM_TIME AS OF recovers the DB-time snapshot exactly like MarkdownFiles/Nodes.
        var edges = new List<EdgeRow>();
        var rosterIds = roster.Select(r => r.EntityId).Distinct().ToList();
        if (rosterIds.Count > 0 && db.Database.IsSqlServer())
        {
            var ts = asOf.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffffff");
            var placeholders = string.Join(",", Enumerable.Range(0, rosterIds.Count).Select(i => "{" + i + "}"));
            var args = rosterIds.Cast<object>().ToArray();
            var rawEdges = await db.Database.SqlQueryRaw<EdgeQueryRow>(
                $"SELECT [SourceId], [TargetId], [RelationType], [Sentiment], [Weight] " +
                $"FROM [dbo].[Edges] FOR SYSTEM_TIME AS OF '{ts}' " +
                $"WHERE [SourceId] IN ({placeholders}) OR [TargetId] IN ({placeholders})",
                args).ToListAsync(ct);
            var nameById = roster.ToDictionary(r => r.EntityId, r => r.Name);
            edges = rawEdges.Select(e => new EdgeRow(
                e.SourceId, nameById.GetValueOrDefault(e.SourceId),
                e.TargetId, nameById.GetValueOrDefault(e.TargetId),
                e.RelationType, e.Sentiment, e.Weight)).ToList();
        }

        var docs = new List<DocRow>();
        if (dcmDocs != null)
        {
            var entries = JsonSerializer.Deserialize<List<JsonElement>>(dcmDocs.FullActiveSetJson ?? dcmDocs.DocsJson) ?? [];
            foreach (var e in entries)
            {
                var path = e.TryGetProperty("Path", out var p) ? p.GetString() : null;
                var tier = e.TryGetProperty("Tier", out var t) ? t.GetString() : null;
                var content = !string.IsNullOrWhiteSpace(path)
                    ? (await markdownFiles.GetAsync(path, asOf, ct))?.Content
                    : null;
                docs.Add(new DocRow(path, tier, content));
            }
        }

        // NodeOutline lives on the BOOK node only, never the chapter (Book -> Chapter -> Beat is a
        // hard, no-exceptions hierarchy — see CLAUDE.md). `nodeId` above is the beat's chapter, so
        // looking it up directly returned null for virtually every real beat in the corpus (only
        // 43 legacy chapter rows anywhere carry a stray NodeOutline value; every real book bible
        // lives one level up). Walk to the parent book before resolving.
        Guid bibleNodeId = Guid.Empty;
        if (nodeId != Guid.Empty)
        {
            var nodeInfo = await db.Nodes.AsNoTracking().IgnoreQueryFilters()
                .Where(n => n.Id == nodeId)
                .Select(n => new { n.Kind, n.ParentNodeId })
                .FirstOrDefaultAsync(ct);
            bibleNodeId = nodeInfo == null ? nodeId
                : nodeInfo.Kind == "book" ? nodeId
                : nodeInfo.ParentNodeId ?? nodeId;
        }
        var bible = bibleNodeId != Guid.Empty ? await nodeBible.GetBibleAsync(bibleNodeId, asOf, ct) : null;

        return new Archive(
            Beat: new BeatRow(beat.Id, beat.Title, beat.Kind, beat.Text, beat.UpdatedAt),
            NodeId: nodeId,
            AsOf: asOf,
            Mode: modeLog == null ? null : new ModeRow(modeLog.Mode, modeLog.Confidence, modeLog.DetectionMethod),
            ServiceCoverage: serviceCoverage.Select(s => new ServiceCoverageRow(s.Service, s.WasApplicable, s.WasActive, s.BlockSizeChars)).ToList(),
            ContextTrace: trace?.ContextJson,
            Prompts: prompts.Select(p => new PromptRow(p.At, p.ProviderId, p.Model, p.System, p.User, p.Response, p.ElapsedMs)).ToList(),
            EntityRoster: resolvedRoster,
            Edges: edges,
            Docs: docs,
            Bible: bible);
    }
}
