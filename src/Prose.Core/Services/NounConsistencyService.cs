using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services.Audit;

namespace Prose.Core.Services;

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
//
// Each DeprecatedEntityName rule now also runs through AuditRunner (as an
// IDeterministicAuditRule) so violations persist to Findings — this used to write nothing
// anywhere; the NounConsistencyReport/NounViolation return shape is unchanged for existing
// callers (prose --validate-nouns, MCP validate_nouns).
//
// COST: zero LLM calls — a free query over DeprecatedEntityNames/Beats, the same DCM-
// adjacent ledger data TimelineConsistencyService reads. Runs in BookHealthService's FREE
// tier (validate-nouns), not part of the paid audit battery. Confirmed 2026-08-13.
// ─────────────────────────────────────────────────────────────────────────────

public class NounConsistencyService(IDbContextFactory<ProseDbContext> dbFactory, AuditRunner auditRunner)
{
    // ── Public API ────────────────────────────────────────────────────────────

    public async Task<NounConsistencyReport> ValidateAsync(Guid nodeId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var node = await db.Nodes.AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == nodeId, ct)
            ?? throw new InvalidOperationException($"Node {nodeId} not found.");
        return await ScanAsync(db, node, auditRunner, ct);
    }

    public async Task<NounConsistencyReport> ValidateSlugAsync(string slug, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var node = await db.Nodes.AsNoTracking()
            .FirstOrDefaultAsync(n => n.Slug == slug, ct)
            ?? throw new InvalidOperationException($"Node slug '{slug}' not found.");
        return await ScanAsync(db, node, auditRunner, ct);
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
        // A rule this run's node-scoped auto-heal will never see again (it's not in the
        // rules list to iterate over next time, not just producing zero violations this
        // time) — clear whatever it already wrote, wherever it wrote it.
        auditRunner.DeleteAllForRule("NOUNCONSISTENCY", RuleKeyFor(rule));
        return true;
    }

    // ── Core scan ─────────────────────────────────────────────────────────────

    static async Task<NounConsistencyReport> ScanAsync(
        ProseDbContext db, Node node, AuditRunner auditRunner, CancellationToken ct)
    {
        var rules = await db.DeprecatedEntityNames
            .AsNoTracking()
            .Where(r => r.UniverseId == node.UniverseId)
            .ToListAsync(ct);

        if (rules.Count == 0)
            return new NounConsistencyReport(node.Title, node.Slug, node.NodeCode, 0, []);

        // Recurses past any nested Collection so a BookNode slug covers all its chapters,
        // at any depth (2026-08-09 fix).
        var nodeIds = await NodeWorkbenchService.GetLeafDescendantIdsAsync(db, node.Id, ct);

        var beats = await db.BeatNodes
            .AsNoTracking()
            .Include(nb => nb.Beat)
            .Where(nb => nodeIds.Contains(nb.NodeId) && true && nb.Beat != null)
            .OrderBy(nb => nb.SortKey)
            .Select(nb => new { nb.Beat!.Id, nb.Beat.Number, nb.Beat.Text })
            .ToListAsync(ct);

        var violations = new List<NounViolation>();

        foreach (var beat in beats)
        {
            if (string.IsNullOrWhiteSpace(beat.Text)) continue;

            foreach (var rule in rules)
            {
                // An all-caps deprecated name is an acronym (e.g. "ANGEL" — Aerogel Null Globe
                // Evacuated Lifter, retired 2026-07-07) rather than a normal proper noun/name
                // rename. A case-INSENSITIVE match against an acronym also matches the ordinary
                // English word it happens to spell ("Angel"/"angel" in prose that has nothing to
                // do with the retired tech, e.g. an in-world ad slogan "Voice of an Angel") — so
                // require exact case for these specifically. A genuine name rename (e.g. a
                // character's old name) is virtually never itself all-caps, so this doesn't
                // weaken those checks.
                var requireExactCase = rule.DeprecatedName.Length > 0
                    && rule.DeprecatedName.All(c => !char.IsLetter(c) || char.IsUpper(c));

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

                    if (requireExactCase &&
                        beat.Text.Substring(idx, rule.DeprecatedName.Length) != rule.DeprecatedName)
                        continue;

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

        // Persist through the shared Findings lifecycle. The scan loop above is unchanged
        // (kept as the direct, already-correct computation rather than re-run through
        // AuditRunner's rule dispatch — there's no LLM step here for the dispatcher to add
        // value to, only the delete-then-recreate persistence pattern is worth sharing).
        var verdicts = violations.Select(v => new AuditVerdict(
            RuleKey:  RuleKeyFor(rules.First(r => r.DeprecatedName == v.DeprecatedName)),
            Title:    $"No references to deprecated name '{v.DeprecatedName}'",
            Severity: "MODERATE",
            Evidence: $"Beat {v.BeatNumber}: uses '{v.DeprecatedName}' (should be '{v.CanonicalName}') — {v.Snippet}",
            Location: v.BeatId.ToString())).ToList();
        auditRunner.WriteFindingsForRules("NOUNCONSISTENCY", $"node:{node.Slug}", FindingCategory.Other,
            rules.Select(RuleKeyFor).ToList(), verdicts);

        return new NounConsistencyReport(node.Title, node.Slug, node.NodeCode, beats.Count, violations);
    }

    static string RuleKeyFor(DeprecatedEntityName rule) => $"noun_{rule.Id}";

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
