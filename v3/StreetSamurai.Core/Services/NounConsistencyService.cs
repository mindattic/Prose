using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;

namespace StreetSamurai.Core.Services;

// ─────────────────────────────────────────────────────────────────────────────
// NounConsistencyService
//
// Deterministic (no-LLM) scan of a node's prose beats for deprecated or renamed
// noun references. Any named thing that was renamed gets its old name registered
// in DeprecatedEntityNames; this service flags every beat that still uses it.
//
// Rules are universe-scoped: a GLMZ rename never flags Fantasy beats.
//
// Scan logic:
//   • Whole-word, case-insensitive match.
//   • Covers the target node's direct beats AND one level of chapter children
//     (so calling with a BookNode slug covers its ChapterNode children).
// ─────────────────────────────────────────────────────────────────────────────

public class NounConsistencyService(IDbContextFactory<StreetSamuraiDbContext> dbFactory)
{
    // ── Public API ────────────────────────────────────────────────────────────

    public async Task<NounConsistencyReport> ValidateAsync(Guid nodeId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var node = await db.Nodes.AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == nodeId, ct)
            ?? throw new InvalidOperationException($"Node {nodeId} not found.");
        return await ScanAsync(db, node, ct);
    }

    public async Task<NounConsistencyReport> ValidateSlugAsync(string slug, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var node = await db.Nodes.AsNoTracking()
            .FirstOrDefaultAsync(n => n.Slug == slug, ct)
            ?? throw new InvalidOperationException($"Node slug '{slug}' not found.");
        return await ScanAsync(db, node, ct);
    }

    public async Task<DeprecatedEntityName> AddRuleAsync(
        Guid universeId, string deprecatedName, string canonicalName,
        string? notes = null, Guid? entityId = null, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var rule = new DeprecatedEntityName
        {
            UniverseId     = universeId,
            DeprecatedName = deprecatedName.Trim(),
            CanonicalName  = canonicalName.Trim(),
            Notes          = notes?.Trim(),
            EntityId       = entityId,
            AddedAt        = DateTime.UtcNow,
        };
        db.DeprecatedEntityNames.Add(rule);
        await db.SaveChangesAsync(ct);
        return rule;
    }

    public async Task<IReadOnlyList<DeprecatedEntityName>> ListRulesAsync(
        Guid? universeId = null, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var q = db.DeprecatedEntityNames
            .AsNoTracking()
            .Include(r => r.Entity)
            .OrderBy(r => r.DeprecatedName)
            .AsQueryable();
        if (universeId.HasValue)
            q = q.Where(r => r.UniverseId == universeId.Value);
        return await q.ToListAsync(ct);
    }

    public async Task<bool> DeleteRuleAsync(long id, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var rule = await db.DeprecatedEntityNames.FindAsync([id], ct);
        if (rule == null) return false;
        db.DeprecatedEntityNames.Remove(rule);
        await db.SaveChangesAsync(ct);
        return true;
    }

    // ── Core scan ─────────────────────────────────────────────────────────────

    static async Task<NounConsistencyReport> ScanAsync(
        StreetSamuraiDbContext db, Node node, CancellationToken ct)
    {
        var rules = await db.DeprecatedEntityNames
            .AsNoTracking()
            .Where(r => r.UniverseId == node.UniverseId)
            .ToListAsync(ct);

        if (rules.Count == 0)
            return new NounConsistencyReport(node.Title, node.Slug, node.NodeCode, 0, []);

        // Include one level of chapter children so a BookNode slug covers its chapters.
        var nodeIds = new List<Guid> { node.Id };
        var childIds = await db.Nodes.AsNoTracking()
            .Where(n => n.ParentNodeId == node.Id)
            .Select(n => n.Id)
            .ToListAsync(ct);
        nodeIds.AddRange(childIds);

        var beats = await db.BeatNodes
            .AsNoTracking()
            .Include(nb => nb.Beat)
            .Where(nb => nodeIds.Contains(nb.NodeId) && nb.IsEnabled && nb.Beat != null)
            .OrderBy(nb => nb.SortKey)
            .Select(nb => new { nb.Beat!.Id, nb.Beat.Number, nb.Beat.Text })
            .ToListAsync(ct);

        var violations = new List<NounViolation>();

        foreach (var beat in beats)
        {
            if (string.IsNullOrWhiteSpace(beat.Text)) continue;

            foreach (var rule in rules)
            {
                int offset = 0;
                while (true)
                {
                    int idx = beat.Text.IndexOf(rule.DeprecatedName, offset, StringComparison.OrdinalIgnoreCase);
                    if (idx < 0) break;
                    offset = idx + 1;

                    // Whole-word check
                    bool leftOk  = idx == 0                                   || !char.IsLetterOrDigit(beat.Text[idx - 1]);
                    bool rightOk = idx + rule.DeprecatedName.Length >= beat.Text.Length
                                   || !char.IsLetterOrDigit(beat.Text[idx + rule.DeprecatedName.Length]);
                    if (!leftOk || !rightOk) continue;

                    violations.Add(new NounViolation(
                        BeatId:         beat.Id,
                        BeatNumber:     beat.Number,
                        DeprecatedName: rule.DeprecatedName,
                        CanonicalName:  rule.CanonicalName,
                        Snippet:        Snippet(beat.Text, idx, 80)));
                    break; // one violation per rule per beat is sufficient
                }
            }
        }

        return new NounConsistencyReport(node.Title, node.Slug, node.NodeCode, beats.Count, violations);
    }

    static string Snippet(string text, int matchIndex, int radius)
    {
        int start = Math.Max(0, matchIndex - radius / 2);
        int end   = Math.Min(text.Length, matchIndex + radius / 2);
        var raw   = text[start..end].Replace('\n', ' ').Replace('\r', ' ');
        return (start > 0 ? "…" : "") + raw + (end < text.Length ? "…" : "");
    }
}

// ── Result models ─────────────────────────────────────────────────────────────

public sealed record NounViolation(
    Guid   BeatId,
    int    BeatNumber,
    string DeprecatedName,
    string CanonicalName,
    string Snippet);

public sealed record NounConsistencyReport(
    string                       NodeTitle,
    string                       NodeSlug,
    string?                      NodeCode,
    int                          BeatCount,
    IReadOnlyList<NounViolation> Violations)
{
    public bool IsClean => Violations.Count == 0;
}
