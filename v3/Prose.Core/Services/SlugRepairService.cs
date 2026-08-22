using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Interfaces;

namespace Prose.Core.Services;

/// <summary>
/// Regenerates every slug from its owning row's metadata (Name/Title) and
/// updates the places that reference the old slug, so slugs behave as LOOSE
/// keys: human-readable, always derivable, never load-bearing. The UUIDv7 id
/// is the real key everywhere; anything holding a stale slug either gets
/// rewritten here or falls back to the guid at resolution time.
///
/// All families use ONE canonical style — hyphen-separated with diacritics
/// folded to ASCII (see <see cref="SlugifyTitle"/>), which is what the existing
/// corpus already uses:
///   entities  — from Name, unique per (UniverseId, EntityType). Old slug is
///               preserved as an <c>alt_slug</c> EntityProperty so prose→canon
///               resolution by the previous slug keeps working.
///   nodes     — hyphen style from Title, unique per UniverseId. Beat audio
///               paths, combined-audio paths, publication paths, and the
///               on-disk audio directories all carry the slug and are renamed.
///   books     — hyphen style from Title, unique per UniverseId.
///   series    — hyphen style from Title (Name fallback), globally unique.
///   episodes  — hyphen style from Title, globally unique; the on-disk
///               engine/episodes/{slug} directory is renamed.
///
/// Dry-run by default: pass apply=true to write. Runs across ALL universes
/// (IgnoreQueryFilters); uniqueness is checked within each family's real scope.
/// CLI: <c>prose --repair-slugs [--apply] [--family &lt;name&gt;] [--json]</c>.
/// </summary>
public class SlugRepairService(
    IDbContextFactory<ProseDbContext> dbFactory,
    IPathProvider paths,
    ILogger<SlugRepairService> log)
{
    public record SlugChange(string Family, Guid Id, string Label, string OldSlug, string NewSlug, List<string> SideEffects);

    public record SlugRepairReport(bool Applied, List<SlugChange> Changes, List<string> Warnings)
    {
        public int Count => Changes.Count;
    }

    public static readonly string[] Families = ["entities", "nodes", "books", "series", "episodes"];

    /// <summary>Canonical slugifier for ALL families: fold diacritics to ASCII
    /// (Möller → moller, Cissé → cisse — matching how the existing corpus was
    /// slugged), lowercase, collapse non-alphanumerics to "-", 80-char cap.
    /// NOT UniverseGraphService.Slugify — that one drops non-ASCII letters
    /// outright (Cissé → ciss_), which would degrade every diaspora name.</summary>
    public static string SlugifyTitle(string title)
    {
        var folded = FoldToAscii(title ?? "");
        var slug = Regex.Replace(folded.ToLowerInvariant().Trim(), @"[^a-z0-9]+", "-").Trim('-');
        if (slug.Length > 80) slug = slug[..80].Trim('-');
        return slug.Length == 0 ? "untitled" : slug;
    }

    /// <summary>Strip combining marks via FormD decomposition (é→e, ö→o, ā→a)
    /// plus the handful of Latin letters that don't decompose.</summary>
    private static string FoldToAscii(string text)
    {
        var sb = new System.Text.StringBuilder(text.Length);
        foreach (var ch in text.Normalize(System.Text.NormalizationForm.FormD))
        {
            var cat = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch);
            if (cat == System.Globalization.UnicodeCategory.NonSpacingMark) continue;
            sb.Append(ch switch
            {
                'ß' => "ss", 'Æ' or 'æ' => "ae", 'Ø' or 'ø' => "o", 'Œ' or 'œ' => "oe",
                'Đ' or 'đ' or 'Ð' or 'ð' => "d", 'Ł' or 'ł' => "l", 'Þ' or 'þ' => "th",
                'İ' => "i", 'ı' => "i",
                _ => ch.ToString(),
            });
        }
        return sb.ToString();
    }

    public async Task<SlugRepairReport> RepairAsync(bool apply, string family = "all", CancellationToken ct = default)
    {
        var changes = new List<SlugChange>();
        var warnings = new List<string>();
        var all = family is "all" or "";

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        if (all || family == "entities") await RepairEntitiesAsync(db, apply, changes, warnings, ct);
        if (all || family == "nodes")    await RepairNodesAsync(db, apply, changes, warnings, ct);
        if (all || family == "books")    await RepairBooksAsync(db, apply, changes, ct);
        if (all || family == "series")   await RepairSeriesAsync(db, apply, changes, ct);
        if (all || family == "episodes") await RepairEpisodesAsync(db, apply, changes, warnings, ct);

        if (apply && changes.Count > 0)
            await db.SaveChangesAsync(ct);

        log.LogInformation("Slug repair ({Mode}): {Count} change(s), {Warn} warning(s)",
            apply ? "APPLY" : "dry-run", changes.Count, warnings.Count);
        return new SlugRepairReport(apply, changes, warnings);
    }

    // ── entities ─────────────────────────────────────────────────────────

    private static async Task RepairEntitiesAsync(
        ProseDbContext db, bool apply, List<SlugChange> changes, List<string> warnings, CancellationToken ct)
    {
        var rows = await db.Entities.IgnoreQueryFilters()
            .OrderBy(e => e.CreatedAt)
            .ToListAsync(ct);

        // Uniqueness scope: (UniverseId, EntityType, Slug).
        var taken = new HashSet<(Guid, string, string)>(
            rows.Select(e => (e.UniverseId, e.EntityType, e.Slug)));

        foreach (var e in rows)
        {
            ct.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(e.Name))
            {
                warnings.Add($"entities: {e.EntityType} {e.Id} has an empty Name — slug left as '{e.Slug}'.");
                continue;
            }
            var desired = SlugifyTitle(e.Name);
            if (desired == e.Slug) continue;

            taken.Remove((e.UniverseId, e.EntityType, e.Slug));
            var unique = Disambiguate(desired, e.Id, s => taken.Contains((e.UniverseId, e.EntityType, s)));
            taken.Add((e.UniverseId, e.EntityType, unique));
            if (unique == e.Slug) continue;   // disambiguation resolved back to the current slug

            var effects = new List<string>();
            var old = e.Slug;
            if (apply)
            {
                e.Slug = unique;
                e.ModifiedAt = DateTime.UtcNow;

                // Preserve the old slug as alt_slug so prose→canon resolution by
                // the previous key keeps working (DataConsistencyService convention).
                if (!string.IsNullOrEmpty(old))
                {
                    var haveAlt = await db.EntityProperties
                        .AnyAsync(p => p.EntityId == e.Id && p.PropertyKey == "alt_slug" && p.Value == old, ct);
                    if (!haveAlt)
                    {
                        db.EntityProperties.Add(new EntityProperty
                        {
                            EntityId = e.Id,
                            PropertyKey = "alt_slug",
                            Value = old,
                            ValueKind = "text",
                            Source = "repair:slug-repair",
                        });
                        effects.Add($"alt_slug='{old}' preserved");
                    }
                }

                // Keep the canonical JSON blob's slug field in agreement, when present.
                var record = await db.Records.FirstOrDefaultAsync(r => r.EntityId == e.Id, ct);
                if (record != null && TryRewriteJsonSlug(record.Json, unique, out var rewritten))
                {
                    record.Json = rewritten;
                    record.UpdatedAt = DateTime.UtcNow;
                    effects.Add("Records.Json slug updated");
                }
            }
            else if (!string.IsNullOrEmpty(old))
            {
                effects.Add($"would preserve alt_slug='{old}'");
            }

            changes.Add(new SlugChange("entities", e.Id, $"{e.EntityType}:{e.Name}", old, unique, effects));
        }
    }

    private static bool TryRewriteJsonSlug(string json, string newSlug, out string rewritten)
    {
        rewritten = json;
        try
        {
            var node = JsonNode.Parse(json);
            if (node is not JsonObject obj) return false;
            // Match the blob's own casing; only touch an existing slug field.
            var key = obj.ContainsKey("slug") ? "slug" : obj.ContainsKey("Slug") ? "Slug" : null;
            if (key == null || (string?)obj[key] == newSlug) return false;
            obj[key] = newSlug;
            rewritten = obj.ToJsonString();
            return true;
        }
        catch { return false; }
    }

    // ── nodes ────────────────────────────────────────────────────────────

    private async Task RepairNodesAsync(
        ProseDbContext db, bool apply, List<SlugChange> changes, List<string> warnings, CancellationToken ct)
    {
        var rows = await db.Nodes.IgnoreQueryFilters()
            .OrderBy(n => n.CreatedAt)
            .ToListAsync(ct);

        var taken = new HashSet<(Guid, string)>(rows.Select(n => (n.UniverseId, n.Slug)));

        foreach (var n in rows)
        {
            ct.ThrowIfCancellationRequested();
            var desired = SlugifyTitle(n.Title);
            if (desired == n.Slug) continue;

            taken.Remove((n.UniverseId, n.Slug));
            var unique = Disambiguate(desired, n.Id, s => taken.Contains((n.UniverseId, s)));
            taken.Add((n.UniverseId, unique));
            if (unique == n.Slug) continue;   // disambiguation resolved back to the current slug

            var old = n.Slug;
            var effects = new List<string>();

            if (apply)
            {
                n.Slug = unique;
                n.UpdatedAt = DateTime.UtcNow;
            }

            if (!string.IsNullOrEmpty(old))
            {
                // Slug-prefixed relative paths in the DB.
                var oldPrefix = old + "/";
                var newPrefix = unique + "/";

                var beatIds = await db.BeatNodes.Where(bn => bn.NodeId == n.Id).Select(bn => bn.BeatId).ToListAsync(ct);
                var beats = await db.Beats
                    .Where(b => beatIds.Contains(b.Id) &&
                                ((b.AudioPath != null && b.AudioPath.StartsWith(oldPrefix)) ||
                                 (b.GapAfterAudioPath != null && b.GapAfterAudioPath.StartsWith(oldPrefix))))
                    .ToListAsync(ct);
                foreach (var b in beats)
                {
                    if (apply)
                    {
                        if (b.AudioPath?.StartsWith(oldPrefix) == true) b.AudioPath = newPrefix + b.AudioPath[oldPrefix.Length..];
                        if (b.GapAfterAudioPath?.StartsWith(oldPrefix) == true) b.GapAfterAudioPath = newPrefix + b.GapAfterAudioPath[oldPrefix.Length..];
                    }
                }
                if (beats.Count > 0) effects.Add($"{beats.Count} beat audio path(s)");

                if (n.CombinedAudioPath?.StartsWith(oldPrefix) == true)
                {
                    if (apply) n.CombinedAudioPath = newPrefix + n.CombinedAudioPath[oldPrefix.Length..];
                    effects.Add("CombinedAudioPath");
                }

                var pubs = await db.NodePublications
                    .Where(p => p.NodeId == n.Id && p.Path != null && p.Path.StartsWith(oldPrefix))
                    .ToListAsync(ct);
                foreach (var p in pubs)
                    if (apply) p.Path = newPrefix + p.Path![oldPrefix.Length..];
                if (pubs.Count > 0) effects.Add($"{pubs.Count} publication path(s)");

                // On-disk audio directories (current + legacy layout).
                foreach (var root in new[]
                {
                    Path.Combine(paths.MutableDataDir, "nodes"),
                    Path.Combine(paths.DataRoot, "engine", "strands"),
                })
                {
                    RenameDirIfExists(root, old, unique, apply, effects, warnings);
                }
            }

            changes.Add(new SlugChange("nodes", n.Id, n.Title, old, unique, effects));
        }
    }

    // ── books / series ───────────────────────────────────────────────────

    private static async Task RepairBooksAsync(ProseDbContext db, bool apply, List<SlugChange> changes, CancellationToken ct)
    {
        var rows = await db.Books.IgnoreQueryFilters().OrderBy(b => b.CreatedAt).ToListAsync(ct);
        var taken = new HashSet<(Guid, string)>(rows.Select(b => (b.UniverseId, b.Slug)));

        foreach (var b in rows)
        {
            ct.ThrowIfCancellationRequested();
            var desired = SlugifyTitle(b.Title);
            if (desired == b.Slug) continue;

            taken.Remove((b.UniverseId, b.Slug));
            var unique = Disambiguate(desired, b.Id, s => taken.Contains((b.UniverseId, s)));
            taken.Add((b.UniverseId, unique));
            if (unique == b.Slug) continue;   // disambiguation resolved back to the current slug

            var old = b.Slug;
            if (apply) b.Slug = unique;
            changes.Add(new SlugChange("books", b.Id, b.Title, old, unique, new List<string>()));
        }
    }

    private static async Task RepairSeriesAsync(ProseDbContext db, bool apply, List<SlugChange> changes, CancellationToken ct)
    {
        var rows = await db.SeriesItems.ToListAsync(ct);
        var taken = new HashSet<string>(rows.Select(s => s.Slug));

        foreach (var s in rows)
        {
            ct.ThrowIfCancellationRequested();
            var source = string.IsNullOrWhiteSpace(s.Title) ? s.Name : s.Title;
            var desired = SlugifyTitle(source);
            if (desired == s.Slug) continue;

            taken.Remove(s.Slug);
            var unique = Disambiguate(desired, s.Id, t => taken.Contains(t));
            taken.Add(unique);
            if (unique == s.Slug) continue;   // disambiguation resolved back to the current slug

            var old = s.Slug;
            if (apply) s.Slug = unique;
            changes.Add(new SlugChange("series", s.Id, source, old, unique, new List<string>()));
        }
    }

    // ── episodes ─────────────────────────────────────────────────────────

    private async Task RepairEpisodesAsync(
        ProseDbContext db, bool apply, List<SlugChange> changes, List<string> warnings, CancellationToken ct)
    {
        var rows = await db.Episodes.OrderBy(e => e.StartedAt).ToListAsync(ct);
        var taken = new HashSet<string>(rows.Select(e => e.Slug));

        foreach (var e in rows)
        {
            ct.ThrowIfCancellationRequested();
            var desired = SlugifyTitle(e.Title);
            if (desired == e.Slug) continue;

            taken.Remove(e.Slug);
            var unique = Disambiguate(desired, e.Id, t => taken.Contains(t));
            taken.Add(unique);
            if (unique == e.Slug) continue;   // disambiguation resolved back to the current slug

            var old = e.Slug;
            var effects = new List<string>();
            if (apply) e.Slug = unique;

            if (!string.IsNullOrEmpty(old))
                RenameDirIfExists(Path.Combine(paths.DataRoot, "engine", "episodes"), old, unique, apply, effects, warnings);

            changes.Add(new SlugChange("episodes", e.Id, e.Title, old, unique, effects));
        }
    }

    // ── helpers ──────────────────────────────────────────────────────────

    /// <summary>Resolve a collision by appending the LAST 8 hex chars of the id
    /// (the random bits of a UUIDv7 — the first 8 are timestamp bits shared by
    /// rows created near each other), falling back to the full guid.</summary>
    private static string Disambiguate(string desired, Guid id, Func<string, bool> isTaken)
    {
        if (!isTaken(desired)) return desired;
        var short8 = $"{desired}-{id.ToString("N")[^8..]}";
        return isTaken(short8) ? $"{desired}-{id:N}" : short8;
    }

    private void RenameDirIfExists(string root, string oldSlug, string newSlug, bool apply,
        List<string> effects, List<string> warnings)
    {
        var oldDir = Path.Combine(root, oldSlug);
        if (!Directory.Exists(oldDir)) return;

        var newDir = Path.Combine(root, newSlug);
        if (Directory.Exists(newDir))
        {
            warnings.Add($"dir rename skipped — target already exists: {newDir}");
            return;
        }
        if (apply)
        {
            try
            {
                Directory.Move(oldDir, newDir);
                effects.Add($"dir {oldDir} → {newSlug}/");
            }
            catch (Exception ex)
            {
                warnings.Add($"dir rename failed for {oldDir}: {ex.Message}");
                log.LogWarning(ex, "Slug repair: directory rename failed for {Dir}", oldDir);
            }
        }
        else
        {
            effects.Add($"would rename dir {oldDir} → {newSlug}/");
        }
    }
}
