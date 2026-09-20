using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Interfaces;

namespace Prose.Core.Services;

/// <summary>
/// Reads a node's beats and produces a beat-by-beat narrative synopsis (act-grouped, one
/// sentence per beat) — a post-hoc "what actually happens" summary generated FROM the
/// written prose. (Renamed from NodeOutlineService 2026-08-29 when "Outline" became the
/// name of the per-book pre-writing plan formerly called the Node Bible — this service is
/// the opposite direction: it describes finished prose, it does not plan it.)
/// Split out from the former BookLogicAuditService, which bundled this
/// with an adversarial logic audit — that audit predated the current LOGIC.md six-dimension
/// doctrine, never matched it, and is retired; use <see cref="LogicSweepService"/> for a real
/// logic check.
///
/// CLI: prose --write-synopsis --slug &lt;slug&gt;
/// MCP: write_synopsis
/// </summary>
public class NarrativeSynopsisService(ILlmService llm, IDbContextFactory<ProseDbContext> dbFactory)
{
    public async Task<NarrativeSynopsisResult> GenerateAsync(Guid nodeId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var node = await db.Nodes.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == nodeId, ct)
            ?? throw new InvalidOperationException($"Node {nodeId} not found.");

        // Respect the same book/chapter hierarchy BookAuditService uses. Recurses past any
        // nested Collection (2026-08-09 fix) — the old Include-based direct-children query
        // missed a split chapter's grandchildren (their BeatNodes navigation is empty).
        var leafIds = await NodeWorkbenchService.GetLeafDescendantIdsAsync(db, nodeId, ct);
        var isFlatNode = leafIds.Count == 1 && leafIds[0] == nodeId;

        List<Beat> indexedBeats;
        if (isFlatNode)
        {
            var nodeWithBeats = await db.Nodes.AsNoTracking()
                .Include(s => s.BeatNodes).ThenInclude(sb => sb.Beat)
                .FirstOrDefaultAsync(s => s.Id == nodeId, ct);
            indexedBeats = nodeWithBeats?.BeatNodes
                .Where(sb => true)
                .OrderBy(sb => sb.SortKey)
                .Select(sb => sb.Beat!)
                .Where(b => !string.IsNullOrWhiteSpace(b.Text))
                .ToList() ?? [];
        }
        else
        {
            var rows = await db.BeatNodes.AsNoTracking()
                .Where(sb => leafIds.Contains(sb.NodeId) && true)
                .Include(sb => sb.Beat)
                .ToListAsync(ct);
            indexedBeats = rows
                .OrderBy(sb => leafIds.IndexOf(sb.NodeId)).ThenBy(sb => sb.SortKey)
                .Select(sb => sb.Beat!)
                .Where(b => !string.IsNullOrWhiteSpace(b.Text))
                .ToList();
        }

        if (indexedBeats.Count == 0)
            return new NarrativeSynopsisResult(nodeId, node.Title, 0, "(No enabled beats found.)");

        var corpus = BuildCorpus(indexedBeats);
        var synopsis = await GenerateSynopsisAsync(node.Title, corpus, ct);
        return new NarrativeSynopsisResult(nodeId, node.Title, indexedBeats.Count, synopsis);
    }

    static string BuildCorpus(IList<Beat> beats) =>
        string.Join("\n\n", beats.Select((b, i) =>
        {
            var header = $"[Beat {i + 1}]";
            if (!string.IsNullOrWhiteSpace(b.Description))
                header += $" {b.Description}";
            return $"{header}\n{b.Text.Trim()}";
        }));

    async Task<string> GenerateSynopsisAsync(string title, string corpus, CancellationToken ct)
    {
        const string system = """
            You are a story analyst. Read the story beats and produce a clean narrative synopsis.

            Format:
            - Group beats by act: ACT 1 — [name], ACT 2 — [name], ACT 3 — [name]
            - One sentence per beat: "Beat N: what happens / what changes"
            - After the beat list, write a 3-sentence "Story spine": want → obstacle → resolution

            Rules:
            - Be precise and concrete — name what actually happens
            - Do not editorialize or praise; just describe
            - Note the protagonist's key decisions (not just events)
            """;
        var user = $"Book: \"{title}\"\n\nBeats:\n{corpus}";
        return await llm.GenerateAsync(system, user, temperature: 0.3, maxTokens: 8192, ct: ct);
    }
}

public record NarrativeSynopsisResult(Guid NodeId, string Title, int BeatCount, string Synopsis);
