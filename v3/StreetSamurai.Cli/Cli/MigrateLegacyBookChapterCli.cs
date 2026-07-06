using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;

namespace StreetSamurai.Cli;

/// <summary>
/// <c>ss --migrate-legacy-book-chapter</c> — one-shot cleanup of the 44 legacy
/// <c>book</c> and <c>chapter</c> entity rows whose content already lives in the
/// relational <c>Nodes</c> + <c>Beats</c> + <c>BeatNodes</c> model.
///
/// Three dispositions:
///   JUNK     — inactive "Untitled Book" blobs → DELETE Entity + Records row.
///   REDUNDANT— content is covered by an existing Node (matched by same GUID,
///              title, slug-prefix, or membership in a parent book Node) →
///              DELETE Entity + Records row.
///   ORPHAN   — no matching Node exists → CREATE a new Node + Beats from
///              the blob's beats/html content, then DELETE Entity + Records row.
///
/// A full DB backup was taken before running this tool
/// (preBookChapterMigration_20260616.bak). Safe to run once; idempotent re-runs
/// are a no-op because the entity rows are deleted on success.
/// </summary>
public static class MigrateLegacyBookChapterCli
{
    // ── GLMZ universe ID (SS-LAW-15) ─────────────────────────────────────────
    private static readonly Guid GlmzUniverseId = Universe.GlmzId;

    // ── Entity IDs: books that already ARE the Node (same GUID) ────────────
    private static readonly HashSet<Guid> RedundantSameId = new()
    {
        Guid.Parse("EB91080D-9C9C-4F2B-9B40-5FA5996BDEA1"), // Bushido Coda
        Guid.Parse("DE7B5D9C-9EB4-796B-84C1-A1FB29C1BD27"), // Eleven Minutes
        Guid.Parse("C0792E2F-8E99-43C3-B03D-AD5BC9526340"), // GLMZ Stories: Vol. 1
        Guid.Parse("15892163-6CE8-4EF7-9126-AB5BF6E298A7"), // The Door Is Unlocked
    };

    // ── Book entity that matches node by slug prefix ────────────────────────
    private static readonly Guid VulturesEntityId    = Guid.Parse("019EC467-77B7-76E4-98B1-A134F13B89C1");
    private static readonly Guid VulturesNodeId    = Guid.Parse("019EC467-878A-7B25-8AF3-F72EBF6E57B6");

    // ── The Voice You Trust book entity → matches node by title ────────────
    private static readonly Guid VoiceYouTrustBookEntityId  = Guid.Parse("5AB1E000-0000-0000-0000-000000000001");
    private static readonly Guid VoiceYouTrustNodeId      = Guid.Parse("019EA026-DD37-72A1-A42A-524E3115CF91");

    // ── Chapters that title-match an existing Node ─────────────────────────
    // Map: chapter-entity-id → matched-node-id
    private static readonly Dictionary<Guid, Guid> RedundantTitleMatchChapters = new()
    {
        [Guid.Parse("019DD24F-EB04-7E9F-B9C9-01450389A8B9")] = Guid.Parse("019E9FB2-60D4-7726-9E7A-2C0C5DC450AE"), // A Borrowed Hand → node
        [Guid.Parse("6AF2D5EA-6EE3-198C-285F-C152631D82B0")] = Guid.Parse("DE7B5D9C-9EB4-796B-84C1-A1FB29C1BD27"), // Eleven Minutes chapter → node
        [Guid.Parse("2A4007BD-AAED-4B1A-996A-155E0130BD76")] = Guid.Parse("15892163-6CE8-4EF7-9126-AB5BF6E298A7"), // The Door Is Unlocked chapter → node
        [Guid.Parse("5AB1EC01-0000-0000-0000-000000000001")] = Guid.Parse("019EA026-DD37-72A1-A42A-524E3115CF91"), // The Voice You Trust chapter → node
    };

    // ── Chapters whose prose lives inside a parent book Node ───────────────
    // These are chapters belonging to one of the 4 fully-migrated book nodes.
    // Their individual beats/html (if any) are already present as beats in the
    // parent node. We delete the entity blobs only.
    private static readonly HashSet<Guid> RedundantInParentNode = new()
    {
        // Bushido Coda chapters (EB91080D)
        Guid.Parse("5A0959EB-5619-BF91-F59F-FB8632C80259"), // A Restless Mind
        Guid.Parse("019D6143-AB61-752D-A68E-0BC71595CD6C"), // Bearing Teeth
        Guid.Parse("367FDF7F-9760-4712-9F30-402A647D05D7"), // Day in the Life
        Guid.Parse("019EA837-A0D6-7596-B586-222B5F7F3D36"), // Ghost Period
        Guid.Parse("CF64FEFC-01E9-4BA9-8EC1-B760C8B9398D"), // Inside the Cage
        Guid.Parse("019EA814-5ACC-7580-AEAB-5E136A7B75AF"), // Interlude I: Something Fixed
        Guid.Parse("019EA814-A950-75B2-A962-5BDA00F64718"), // Interlude II: Half a Step
        Guid.Parse("019EA814-FBEE-7352-AB94-52311A822781"), // Interlude III: Before Something Changes
        Guid.Parse("019EA815-6D90-75E6-8F3F-4C62C1C55142"), // Interlude IV: The Morning (in Bushido node, no book_id in blob)
        Guid.Parse("019EA819-4B3D-7417-897A-3C308EDFF59A"), // Sexy Time
        Guid.Parse("019DB31F-E888-7C97-A049-65978B5CCDB3"), // Street Meat
        Guid.Parse("019EA818-579D-7C80-8D7F-D2FE7DA9A330"), // Sunset Clause
        Guid.Parse("019DAD5F-DB77-766B-9D54-8FB43A11BE18"), // The Interview
        Guid.Parse("019EA826-61E6-7763-8D6D-589332DD1F1A"), // The One Who Doesn't Stop
        Guid.Parse("019EAEEB-8B29-77DA-BA69-0516A640D194"), // The Quiet Hour
        Guid.Parse("5AB1EC04-0000-0000-0000-000000000004"), // The Restructuring
        Guid.Parse("019EA82F-AA94-7547-9B17-520B713BED7D"), // The Ride Back
        Guid.Parse("6D75764C-C8DA-8E32-FD73-3DD5C43E92E2"), // The Rogue AI
        // The Door Is Unlocked chapters (15892163)
        Guid.Parse("2BC9EE96-155D-4939-803D-3E9D61D660F1"), // 2F
        Guid.Parse("30EFC81D-C864-4A91-A1C5-53A46D65FF21"), // Bourbon and Bullet Holes
        Guid.Parse("39304A0E-E849-4A8E-A98B-A3BFD6A2E5C1"), // The Bus
        Guid.Parse("40BC736A-8EA7-46A6-BB4F-705988CD5B8D"), // Two Addresses
        // GLMZ Stories: Vol. 1 chapters (C0792E2F)
        Guid.Parse("019DB2D5-F5E3-7F3D-A915-275C01BB15EC"), // Every Road Leads Back
        Guid.Parse("019D6143-AB62-7C8C-8673-CEB358E4F3BB"), // The Cartography of Uncharted Interfaces
        Guid.Parse("019DA83A-E361-72A5-A1CB-49CA1C62384A"), // The Last Chair
        // The Voice You Trust chapters (5AB1E000)
        Guid.Parse("5AB1EC03-0000-0000-0000-000000000003"), // Discrepancies in the Margin
        Guid.Parse("5AB1EC02-0000-0000-0000-000000000002"), // Floor 47
        Guid.Parse("5AB1EC05-0000-0000-0000-000000000005"), // Sable
    };

    // ── True orphans: no matching Node — must convert ─────────────────────
    private static readonly Guid ColdChainEntityId = Guid.Parse("18A6455A-D4F3-54FE-CF95-C59D09AD1A7E");
    private static readonly Guid SashaVoEntityId   = Guid.Parse("0260C8B9-D1A7-F4E2-B8C9-D0A1E2F3B4C5");

    // ─────────────────────────────────────────────────────────────────────────

    public static async Task<int> RunAsync(string[] args, IServiceProvider sp)
    {
        var dbFactory = sp.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        // ── Pre-flight: confirm we still have book/chapter Records rows ───────
        var legacyCount = await db.Database
            .SqlQueryRaw<int>(
                "SELECT COUNT(*) AS Value FROM Records r JOIN Entities e ON e.Id=r.EntityId WHERE e.EntityType IN ('book','chapter')")
            .FirstOrDefaultAsync();

        Console.WriteLine($"[migrate-legacy-book-chapter] Found {legacyCount} legacy book/chapter Records rows.");
        if (legacyCount == 0)
        {
            Console.WriteLine("[migrate-legacy-book-chapter] Nothing to do — all legacy rows already gone.");
            return 0;
        }

        int junkDeleted = 0, redundantDeleted = 0, orphansConverted = 0;
        var newNodeSlugs = new List<(string Slug, int Beats)>();

        // ── 1. JUNK: inactive "Untitled Book" blobs ───────────────────────────
        var junkIds = new[]
        {
            Guid.Parse("019DFF7A-D07C-776B-902D-32101659527D"),
            Guid.Parse("019DFF7A-D079-7709-8233-6911847D4036"),
            Guid.Parse("019DFF7A-D07A-762D-B964-9A2D19153159"),
            Guid.Parse("019DFF7A-D078-74DC-8A8A-D5C9F050D456"),
        };
        foreach (var id in junkIds)
        {
            var deleted = await DeleteEntityAndRecord(db, id);
            if (deleted) { junkDeleted++; Console.WriteLine($"  JUNK  deleted  {id}  (inactive Untitled Book)"); }
        }

        // ── 2. REDUNDANT: same GUID already is the Node ────────────────────
        foreach (var id in RedundantSameId)
        {
            var deleted = await DeleteEntityAndRecord(db, id);
            if (deleted) { redundantDeleted++; Console.WriteLine($"  REDUNDANT(same-id)  deleted  {id}  → node {id}"); }
        }

        // ── 3. REDUNDANT: Vultures book → vultures-at-the-door node ────────
        {
            var deleted = await DeleteEntityAndRecord(db, VulturesEntityId);
            if (deleted) { redundantDeleted++; Console.WriteLine($"  REDUNDANT(slug-prefix)  deleted  {VulturesEntityId}  'Vultures on the Doorstep' → node {VulturesNodeId}"); }
        }

        // ── 4. REDUNDANT: The Voice You Trust book → title-match node ───────
        {
            var deleted = await DeleteEntityAndRecord(db, VoiceYouTrustBookEntityId);
            if (deleted) { redundantDeleted++; Console.WriteLine($"  REDUNDANT(title-match)  deleted  {VoiceYouTrustBookEntityId}  'The Voice You Trust' → node {VoiceYouTrustNodeId}"); }
        }

        // ── 5. REDUNDANT: chapters with title-matching Node ─────────────────
        foreach (var (entityId, nodeId) in RedundantTitleMatchChapters)
        {
            var deleted = await DeleteEntityAndRecord(db, entityId);
            if (deleted) { redundantDeleted++; Console.WriteLine($"  REDUNDANT(title-match)  deleted  {entityId} → node {nodeId}"); }
        }

        // ── 6. REDUNDANT: chapters in parent book nodes ────────────────────
        foreach (var id in RedundantInParentNode)
        {
            var deleted = await DeleteEntityAndRecord(db, id);
            if (deleted) { redundantDeleted++; Console.WriteLine($"  REDUNDANT(in-parent)  deleted  {id}"); }
        }

        // ── 7. ORPHAN: Cold Chain → convert beats → new Node ───────────────
        {
            var (slug, beatCount) = await ConvertChapterToNode(db, ColdChainEntityId, "chapter");
            if (slug != null)
            {
                await DeleteEntityAndRecord(db, ColdChainEntityId);
                orphansConverted++;
                newNodeSlugs.Add((slug, beatCount));
                Console.WriteLine($"  ORPHAN  converted  {ColdChainEntityId}  'Cold Chain' → new node slug={slug}  beats={beatCount}");
            }
        }

        // ── 8. ORPHAN: Sasha Võ → convert HTML → single beat Node ─────────
        {
            var (slug, beatCount) = await ConvertChapterToNode(db, SashaVoEntityId, "chapter");
            if (slug != null)
            {
                await DeleteEntityAndRecord(db, SashaVoEntityId);
                orphansConverted++;
                newNodeSlugs.Add((slug, beatCount));
                Console.WriteLine($"  ORPHAN  converted  {SashaVoEntityId}  'Sasha Võ' → new node slug={slug}  beats={beatCount}");
            }
        }

        // ── Final verification ────────────────────────────────────────────────
        var remaining = await db.Database
            .SqlQueryRaw<int>(
                "SELECT COUNT(*) AS Value FROM Records r JOIN Entities e ON e.Id=r.EntityId WHERE e.EntityType IN ('book','chapter')")
            .FirstOrDefaultAsync();

        Console.WriteLine();
        Console.WriteLine($"[migrate-legacy-book-chapter] COMPLETE");
        Console.WriteLine($"  junk-deleted:      {junkDeleted}");
        Console.WriteLine($"  redundant-deleted: {redundantDeleted}");
        Console.WriteLine($"  orphans-converted: {orphansConverted}");
        foreach (var (slug, beats) in newNodeSlugs)
            Console.WriteLine($"    new node: {slug}  ({beats} beats)");
        Console.WriteLine($"  remaining book/chapter Records rows: {remaining}");

        if (remaining != 0)
        {
            Console.Error.WriteLine($"[migrate-legacy-book-chapter] WARNING: {remaining} rows still present — unexpected!");
            return 1;
        }

        return 0;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Converts a chapter entity's blob (beats array or HTML) into a new
    /// Node + Beat + BeatNode set. Returns (slug, beatCount) on success,
    /// (null, 0) if the entity is missing or has no content to convert.
    /// </summary>
    private static async Task<(string? Slug, int BeatCount)> ConvertChapterToNode(
        StreetSamuraiDbContext db, Guid entityId, string kind)
    {
        // Load raw JSON blob without EF's universe filter interfering
        var record = await db.Database
            .SqlQuery<RecordRow>(
                $"SELECT r.Json FROM Records r WHERE r.EntityId = {entityId}")
            .FirstOrDefaultAsync();

        if (record == null)
        {
            Console.Error.WriteLine($"  [convert] No Record found for {entityId}");
            return (null, 0);
        }

        using var doc = JsonDocument.Parse(record.Json);
        var root = doc.RootElement;

        var title    = root.TryGetProperty("title",    out var tProp) ? tProp.GetString() ?? "" : "";
        var synopsis = root.TryGetProperty("synopsis", out var sProp) ? sProp.GetString() : null;

        // Build slug: Slugify(title) + "-" + entityId[..8]
        var baseSlug = Slugify(title);
        var shortId  = entityId.ToString("N")[..8];
        var slug     = $"{baseSlug}-{shortId}";

        // Ensure slug is unique (shouldn't collide but guard anyway)
        if (await db.Nodes.AnyAsync(s => s.Slug == slug))
            slug = $"{baseSlug}-{shortId}-{Guid.NewGuid():N8}";

        var newNodeId = Guid.CreateVersion7();
        int nextNumber  = (await db.Beats.MaxAsync(b => (int?)b.Number) ?? 0) + 1;
        int beatCount   = 0;

        // ── Collect beats: prefer the beats array; fall back to HTML ─────────
        var beatsToInsert = new List<(string Title, string Text, string? Synopsis, int ActNum, string? StructureRole, string SceneType, int Idx)>();

        // Try beats array first
        if (root.TryGetProperty("beats", out var beatsProp) && beatsProp.ValueKind == JsonValueKind.Array)
        {
            foreach (var b in beatsProp.EnumerateArray())
            {
                var text = b.TryGetProperty("text", out var tp) ? tp.GetString() ?? "" : "";
                if (string.IsNullOrWhiteSpace(text)) continue;
                var bTitle = b.TryGetProperty("title",          out var btProp)  ? btProp.GetString()  ?? "" : "";
                var bSyn   = b.TryGetProperty("synopsis",       out var bsProp)  ? bsProp.GetString()        : null;
                var bAct   = b.TryGetProperty("act",            out var baProp) && baProp.TryGetInt32(out var ai) ? ai : 0;
                var bRole  = b.TryGetProperty("structure_role", out var brProp)  ? brProp.GetString()        : null;
                var bScene = b.TryGetProperty("scene_type",     out var bcProp)  ? bcProp.GetString() ?? "scene" : "scene";
                var bIdx   = b.TryGetProperty("index",          out var biProp) && biProp.TryGetInt32(out var ii) ? ii : beatsToInsert.Count;
                beatsToInsert.Add((bTitle, text, bSyn, bAct, string.IsNullOrEmpty(bRole) ? null : bRole, string.IsNullOrEmpty(bScene) ? "scene" : bScene, bIdx));
            }
        }

        // If no beats from array, try HTML — split into sections by ## heading
        if (beatsToInsert.Count == 0 && root.TryGetProperty("html", out var htmlProp))
        {
            var html = htmlProp.GetString() ?? "";
            if (!string.IsNullOrWhiteSpace(html))
            {
                // Strip the leading # Title line, then split on ## section headings
                var sections = Regex.Split(html, @"(?=^##\s)", RegexOptions.Multiline);
                int idx = 0;
                foreach (var section in sections)
                {
                    var trimmed = section.Trim();
                    if (string.IsNullOrWhiteSpace(trimmed)) continue;
                    // Strip leading # or ## heading line
                    var lines = trimmed.Split('\n');
                    var headingLine = lines[0].Trim();
                    if (headingLine.StartsWith("#"))
                    {
                        var sectionTitle = headingLine.TrimStart('#').Trim();
                        var body = string.Join('\n', lines.Skip(1)).Trim();
                        if (!string.IsNullOrWhiteSpace(body))
                            beatsToInsert.Add((sectionTitle, body, null, 0, null, "scene", idx++));
                    }
                    else if (!string.IsNullOrWhiteSpace(trimmed))
                    {
                        beatsToInsert.Add(("", trimmed, null, 0, null, "scene", idx++));
                    }
                }
                // If splitting produced nothing useful, wrap the whole HTML as one beat
                if (beatsToInsert.Count == 0 && !string.IsNullOrWhiteSpace(html))
                    beatsToInsert.Add(("", html, synopsis, 0, null, "scene", 0));
            }
        }

        if (beatsToInsert.Count == 0)
        {
            Console.Error.WriteLine($"  [convert] {entityId} '{title}' has no usable content — creating synopsis-only node with 0 beats.");
        }

        // ── Create the Node ─────────────────────────────────────────────────
        var node = NodeFactory.Create(kind);
        node.Id         = newNodeId;
        node.UniverseId = GlmzUniverseId;
        node.Slug       = slug;
        node.Title      = title;
        node.Description = synopsis;
        node.Status     = "draft";
        node.SortKey    = 9999.0;
        db.Nodes.Add(node);

        // ── Create Beats + BeatNodes ────────────────────────────────────────
        double sortKey = 100.0;
        foreach (var (bTitle, text, bSyn, bAct, bRole, bScene, _) in beatsToInsert.OrderBy(x => x.Idx))
        {
            var beatId = Guid.CreateVersion7();
            db.Beats.Add(new Beat
            {
                Id            = beatId,
                Number        = nextNumber++,
                Text          = text,
                TextHash      = ComputeTextHash(text),
                Title         = string.IsNullOrEmpty(bTitle) ? null : bTitle,
                Description   = bSyn,
                Act           = bAct,
                StructureRole = bRole,
                SceneType     = bScene,
            });
            db.BeatNodes.Add(new BeatNode
            {
                NodeId = newNodeId,
                BeatId   = beatId,
                SortKey  = sortKey,
            });
            sortKey += 100.0;
            beatCount++;
        }

        await db.SaveChangesAsync();
        return (slug, beatCount);
    }

    /// <summary>
    /// Deletes all rows referencing this entity (in dependency order), then the
    /// Entity row itself. Uses raw SQL so EF's universe-scope query filter doesn't
    /// hide anything. Returns true if any rows were deleted.
    /// </summary>
    private static async Task<bool> DeleteEntityAndRecord(StreetSamuraiDbContext db, Guid entityId)
    {
        int total = 0;

        // 1. Dependent tables with FKs → Entities (delete children before parent)
        // Edges: TargetId and SourceId both reference Entities
        total += await db.Database.ExecuteSqlAsync($"DELETE FROM Edges WHERE TargetId = {entityId} OR SourceId = {entityId}");
        // EntityEmbeddings
        total += await db.Database.ExecuteSqlAsync($"DELETE FROM EntityEmbeddings WHERE EntityId = {entityId}");
        // EntityTags, EntityProperties, EntityStateEvents, EntityTaxonomies (usually empty for book/chapter)
        total += await db.Database.ExecuteSqlAsync($"DELETE FROM EntityTags WHERE EntityId = {entityId}");
        total += await db.Database.ExecuteSqlAsync($"DELETE FROM EntityProperties WHERE EntityId = {entityId}");
        total += await db.Database.ExecuteSqlAsync($"DELETE FROM EntityStateEvents WHERE EntityId = {entityId}");
        total += await db.Database.ExecuteSqlAsync($"DELETE FROM EntityTaxonomies WHERE EntityId = {entityId}");
        // ContinuityClaims
        total += await db.Database.ExecuteSqlAsync($"DELETE FROM ContinuityClaims WHERE EntityId = {entityId}");

        // 2. Records (FK to Entity)
        total += await db.Database.ExecuteSqlAsync($"DELETE FROM Records WHERE EntityId = {entityId}");

        // 3. The entity itself
        total += await db.Database.ExecuteSqlAsync($"DELETE FROM Entities WHERE Id = {entityId}");

        return total > 0;
    }

    // ── Static helpers ────────────────────────────────────────────────────────

    /// <summary>
    /// Mirrors <c>NodeMigrationService.Slugify</c>: lowercase, replace
    /// non-alphanumeric runs with "-", trim leading/trailing dashes.
    /// </summary>
    private static string Slugify(string s) =>
        Regex.Replace(s.ToLowerInvariant().Trim(), @"[^a-z0-9]+", "-").Trim('-');

    /// <summary>Mirrors <c>NodeMigrationService.ComputeTextHash</c>.</summary>
    private static string ComputeTextHash(string text)
    {
        var normalized = (text ?? "").Trim();
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    // ── Raw query projection types ────────────────────────────────────────────

    private record RecordRow(string Json);
}
