using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using System.Security.Cryptography;
using System.Text;

namespace Prose.Core.Services;

/// <summary>
/// Dynamic Context Memory — entity doc materialization layer.
///
/// Generates compact per-entity <c>.md</c> files directly in <c>MarkdownFiles</c>
/// (DB-only rows; no corresponding disk file) from live entity records. These docs
/// participate in the DocContextStack keyword-trigger and relational-cascade passes
/// the same way hand-authored canon docs do.
///
/// <b>Hash-gated:</b> if the entity description hasn't changed since the last
/// materialization, <c>EnsureEntityDocAsync</c> is a fast no-op.
///
/// <b>DB-only rows:</b> entity docs are written to <c>MarkdownFiles</c> with
/// <c>FilePath = ""</c> and <c>SyncedBy = "inferred"</c>. They are never written to
/// disk and are never touched by <c>MarkdownFileService.SyncAllAsync</c> (which is
/// disk-sourced). <c>EntityDocService</c> is their sole manager.
///
/// <b>Inference entry point:</b> <c>InferFromTextAsync</c> calls
/// <c>SceneContextAssembler.AssembleAsync</c> to discover entities in a beat-goal
/// string, then ensures each has a current entity doc in <c>MarkdownFiles</c>.
/// Call this BEFORE <c>DocContextService.PrepareContextAsync</c> so newly-created
/// rows are included in the candidate query.
/// </summary>
public sealed class EntityDocService(
    IDbContextFactory<ProseDbContext> dbFactory,
    SceneContextAssembler assembler,
    ILogger<EntityDocService> log)
{
    private const string EntityDocRoot = "docs/entities";

    // ── Public API ──────────────────────────────────────────────────────────

    /// <summary>
    /// Ensure a <c>MarkdownFiles</c> row exists for the given entity and is current.
    /// Returns <c>true</c> if a row was created or updated; <c>false</c> if the
    /// existing row was already up-to-date (hash match) or the entity was not found.
    /// </summary>
    public async Task<bool> EnsureEntityDocAsync(Guid entityId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        // IgnoreQueryFilters: this method is universe-agnostic by construction — it stamps the doc
        // with whatever universe the entity itself declares, so it does not need the ambient scope
        // to agree. Without this, a maintenance pass (prose --repair-entity-docs) could only ever
        // reach the entities of whichever universe happened to be active. During normal prose
        // generation the ids come from SceneContextAssembler, which is already universe-scoped, so
        // nothing widens there.
        var entity = await db.Entities.AsNoTracking().IgnoreQueryFilters()
            .Where(e => e.Id == entityId && e.IsActive)
            .FirstOrDefaultAsync(ct);
        if (entity == null) return false;

        var (content, triggers) = await BuildContentAsync(db, entity, ct);
        var hash    = ComputeHash(content);
        var relPath = $"{EntityDocRoot}/{entity.Slug}.md";

        // IgnoreQueryFilters: this is an upsert keyed on the UNIQUE (FileRoot, RelativePath)
        // index, which is not universe-scoped. Under the universe query filter a row belonging to
        // another universe would come back null here, and the insert below would then violate that
        // unique index. The row must be found regardless of which universe currently owns it.
        var existing = await db.MarkdownFiles.IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.RelativePath == relPath && m.FileRoot == "project", ct);

        // The UniverseId/EntityId comparison is part of the gate, not decoration: the gate
        // short-circuits on unchanged content, so a pure metadata back-fill (which does not alter
        // the rendered doc, and therefore not the hash) would skip every already-correct row and
        // never stamp it. This bit DocContextService.PrepareContextAsync once already for
        // UniverseId; EntityId needs the same guard or pre-existing rows never get it.
        if (existing != null && existing.ContentHash == hash
            && existing.UniverseId == entity.UniverseId && existing.EntityId == entity.Id)
            return false;

        if (existing == null)
        {
            db.MarkdownFiles.Add(new MarkdownFile
            {
                Id           = Guid.NewGuid(),
                FilePath     = "",      // DB-only — no disk file
                FileRoot     = "project",
                RelativePath = relPath,
                FileName     = $"{entity.Slug}.md",
                Category     = "entity-doc",
                Content      = content,
                ContentHash  = hash,
                LastSyncedAt = DateTime.UtcNow,
                SyncedBy     = "inferred",
                Tier         = "topic",
                Scope        = "",
                Triggers     = triggers,
                AutoTier     = false,
                // An entity doc belongs to exactly the universe its entity does — this is what
                // keeps a GLMZ character out of a SCRY beat's keyword/embedding candidate set.
                UniverseId   = entity.UniverseId,
                // Back-reference so DocContextService can join to Entity.OriginNodeId and resolve
                // same-universe, cross-book bare-name collisions (e.g. two books each having a
                // character whose bare first name is "James") — Scope can't fill this gap, entity
                // docs are always written with Scope = "".
                EntityId     = entity.Id,
            });
        }
        else
        {
            existing.Content      = content;
            existing.ContentHash  = hash;
            existing.Triggers     = triggers;
            existing.LastSyncedAt = DateTime.UtcNow;
            existing.SyncedBy     = "inferred";
            existing.UniverseId   = entity.UniverseId;
            existing.EntityId     = entity.Id;
        }

        await db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>
    /// Discover entities referenced in <paramref name="text"/> (beat goal, scene synopsis,
    /// or any prose fragment) and ensure each has a current entity doc in
    /// <c>MarkdownFiles</c>. Returns the count of docs created or updated.
    ///
    /// Call this BEFORE <c>DocContextService.PrepareContextAsync</c> so the freshly-
    /// created rows participate in the keyword-trigger and relational-cascade passes.
    /// This method is best-effort — errors per entity are logged and skipped; the overall
    /// call never throws.
    /// </summary>
    /// <param name="contextNodeId">The book/chapter Node this text belongs to, when known — passed
    /// through to <see cref="SceneContextAssembler.AssembleAsync"/> so its name-collision resolver
    /// (<see cref="EntityDisambiguationService"/>) has book context instead of resolving blind.
    /// Optional and trailing for call-site compatibility.</param>
    public async Task<int> InferFromTextAsync(string text, CancellationToken ct = default, Guid? contextNodeId = null)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0;

        SceneContext ctx;
        try { ctx = await assembler.AssembleAsync(text, tokenBudget: 1200, ct, contextNodeId); }
        catch (Exception ex)
        {
            log.LogDebug(ex, "EntityDocService: SceneContextAssembler unavailable; skipping inference");
            return 0;
        }

        int changed = 0;
        foreach (var entry in ctx.Roster)
        {
            try
            {
                if (await EnsureEntityDocAsync(entry.EntityId, ct)) changed++;
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "EntityDocService: failed to ensure doc for entity {Id} ({Name})",
                    entry.EntityId, entry.Name);
            }
        }

        if (changed > 0)
            log.LogDebug("EntityDocService: materialized {Count} entity doc(s) from text inference", changed);

        return changed;
    }

    // ── Content building ─────────────────────────────────────────────────────

    private async Task<(string content, string triggers)> BuildContentAsync(
        ProseDbContext db, Entity entity, CancellationToken ct)
    {
        var triggerList = new List<string>();
        CollectNameTokens(entity.Name, triggerList);
        if (!string.IsNullOrEmpty(entity.Slug)) triggerList.Add(entity.Slug.Replace("-", " ").Replace("_", " "));

        var sb = new StringBuilder();

        if (entity.EntityType.Equals("character", StringComparison.OrdinalIgnoreCase))
        {
            var ch = await db.Characters.AsNoTracking()
                .Include(c => c.Aliases)
                .Where(c => c.Id == entity.Id)
                .FirstOrDefaultAsync(ct);

            // Collect aliases before writing frontmatter so triggers are complete.
            if (ch != null)
                foreach (var a in ch.Aliases) CollectNameTokens(a.Value, triggerList);

            var triggers = NormalizeTriggers(triggerList);
            WriteFrontmatter(sb, triggers);

            sb.AppendLine($"# {entity.Name}");
            sb.AppendLine();

            if (ch != null && !string.IsNullOrEmpty(ch.Species) && ch.Species != "human")
                sb.AppendLine($"**Type:** {entity.EntityType} — {ch.Species}  ");
            else
                sb.AppendLine($"**Type:** {entity.EntityType}  ");

            var lifeStatus = ch?.LifeStatus ?? "alive";
            var statusLine = lifeStatus is "alive" or ""
                ? entity.Status
                : $"{entity.Status} ({lifeStatus})";
            sb.AppendLine($"**Status:** {statusLine}  ");

            if (!string.IsNullOrEmpty(entity.Description))
            {
                sb.AppendLine();
                sb.AppendLine(entity.Description.Trim());
            }
            if (!string.IsNullOrEmpty(entity.GrammarNote))
            {
                sb.AppendLine();
                sb.AppendLine($"*Grammar: {entity.GrammarNote.Trim()}*");
            }
            if (ch != null)
            {
                // SS-A46 register — all 6 fields, matching SceneContextAssembler.FormatCharacterAsync.
                // This doc is pinned dominant (score 999) as the beat's narrator voice, so dropping
                // any of these silently mutes part of the register for as long as the character POVs.
                var voiceParts = new[]
                    {
                        ch.SpeechVocabulary, ch.SpeechCadence, ch.SpeechSubtext,
                        ch.SpeechUnderPressure, ch.SpeechIntimacyRegister,
                    }
                    .Where(s => !string.IsNullOrEmpty(s)).ToList();
                if (voiceParts.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("**Voice:**");
                    foreach (var v in voiceParts) sb.AppendLine(v.Trim());
                }
                if (!string.IsNullOrEmpty(ch.PsychologySecret))
                {
                    sb.AppendLine();
                    sb.AppendLine($"**Secret:** {ch.PsychologySecret.Trim()}");
                }
            }

            return (sb.ToString().TrimEnd() + "\n", triggers);
        }
        else
        {
            var triggers = NormalizeTriggers(triggerList);
            WriteFrontmatter(sb, triggers);

            sb.AppendLine($"# {entity.Name}");
            sb.AppendLine();
            sb.AppendLine($"**Type:** {entity.EntityType}  ");
            sb.AppendLine($"**Status:** {entity.Status}  ");

            if (!string.IsNullOrEmpty(entity.Description))
            {
                sb.AppendLine();
                sb.AppendLine(entity.Description.Trim());
            }
            if (!string.IsNullOrEmpty(entity.GrammarNote))
            {
                sb.AppendLine();
                sb.AppendLine($"*Grammar: {entity.GrammarNote.Trim()}*");
            }

            return (sb.ToString().TrimEnd() + "\n", triggers);
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static void WriteFrontmatter(StringBuilder sb, string triggers)
    {
        sb.AppendLine("---");
        sb.AppendLine("tier: topic");
        sb.AppendLine($"triggers: {triggers}");
        sb.AppendLine("---");
        sb.AppendLine();
    }

    // Words that mean a multi-word name/alias is an epithet or a descriptive phrase, not a
    // plain "First Last" personal name — "Herod the Great" (connector "the"), "Pharisee
    // movement" or "Samaritan woman at the well" (a descriptive noun, not a surname). Their
    // trailing word is a common English word, not a discriminating surname, and false-positive
    // matches any unrelated prose that happens to use it ("a great army", "the movement began").
    private static readonly HashSet<string> NonSurnameWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "the", "of", "at", "in", "on", "a", "an", "and", "or",
        "son", "daughter", "brother", "sister", "father", "mother",
        "movement", "group", "people", "tribe", "house", "clan", "order", "man", "woman",
    };

    private static void CollectNameTokens(string name, List<string> list)
    {
        if (string.IsNullOrWhiteSpace(name)) return;
        list.Add(name);
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        // Only trust the last word as a surname when the whole phrase looks like a plain
        // personal name — see NonSurnameWords above.
        if (parts.Length > 1 && !parts.Any(p => NonSurnameWords.Contains(p))) list.Add(parts[^1]); // surname
    }

    private static string NormalizeTriggers(List<string> triggerList) =>
        string.Join(", ", triggerList
            .Select(t => t.Trim().ToLowerInvariant())
            .Where(t => t.Length >= 3)
            .Distinct());

    private static string ComputeHash(string content)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
