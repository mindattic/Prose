using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Core.Services.Factory;

public sealed record BundleEntity(Guid Id, string Name, string Type, DateTime ModifiedAt);

/// <summary>What a bundle holds, so a writer knows exactly what it was — and was not — given.</summary>
public sealed record ContextManifest(
    string Path, Guid BookId, string Book, int Unit, string UnitHeading,
    int? PriorFromUnit, int? PriorToUnit, int PriorChars, int PriorCharsAvailable, bool PriorTruncated,
    IReadOnlyList<BundleEntity> Entities, int Laws, IReadOnlyList<string> CanonDocuments, int TotalChars, string Hash);

/// <summary>
/// The writer's working memory for one unit (RFC 0015 §3.9): one derived file, rebuilt on every
/// call and never edited, holding everything the prose must answer to —
/// <list type="number">
///   <item>the book's law (its law and page-law rulings, verbatim),</item>
///   <item>the universe's world and craft canon,</item>
///   <item>the canonical records of every entity the unit tags, as they stand,</item>
///   <item>the prose before the unit — the previous unit, or every preceding unit within the budget,
///         oldest dropped first — which is never cut at a chapter boundary,</item>
///   <item>the unit itself: its prose, or its planned beats.</item>
/// </list>
/// <para>This is the fix for the old writer's blindness: its "scene so far" reset to empty at every
/// chapter and was capped at 6KB, so each chapter was written without the one before it.</para>
/// </summary>
public sealed class ContextBundleService(IDbContextFactory<ProseDbContext> dbFactory, BookSpineService spine, RulingService rulings)
{
    /// <summary>Canon document types that are the world and its craft, world first. The universe-scoped
    /// ones are read from the book's universe, the shared craft guides from <see cref="Universe.SharedId"/>.
    /// Engine documents (EngineGuide) and the franchise plan are not the world and are left out.</summary>
    public static readonly string[] CanonTypes =
        ["WorldMaster", "UniverseCanon", "WorldBible", "UniverseCraft", "CraftGuide", "CharacterDoctrine", "DelightGuide"];

    public const int DefaultBudget = 400_000;

    public static string DefaultDirectory =>
        System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Prose", "factory", "context");

    public async Task<ContextManifest> BuildAsync(Guid bookId, int unitOrdinal, bool allPrior = false, int budgetChars = DefaultBudget,
        string? outDir = null, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var node = await db.Nodes.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(n => n.Id == bookId, ct)
                   ?? throw new ArgumentException($"Book {bookId} not found.");
        var sp = await spine.GetAsync(bookId, ct);
        var chapters = sp.Chapters.Where(c => c.Beats.Count > 0).ToList();
        var unitIndex = chapters.FindIndex(c => c.Ordinal == unitOrdinal);
        if (unitIndex < 0) throw new ArgumentException($"The book has no unit {unitOrdinal}.");
        var unit = chapters[unitIndex];

        var ids = chapters.SelectMany(c => c.Beats).Select(b => b.BeatId).ToList();
        var beats = await db.Beats.AsNoTracking().Where(b => ids.Contains(b.Id))
            .Select(b => new { b.Id, b.Text, b.Title, b.Description }).ToDictionaryAsync(b => b.Id, ct);
        string Plain(Guid id) => beats.TryGetValue(id, out var b) ? BeatMarkup.StripEntityTags(b.Text ?? "").Trim() : "";

        var sb = new StringBuilder();
        var bookName = node.NodeCode ?? node.Slug;
        sb.AppendLine($"# Context — {node.Title} ({bookName}), unit {unit.Ordinal}: {unit.Heading}");
        sb.AppendLine();
        sb.AppendLine("Derived, rebuilt on every call, never edited. The prose answers to everything below.");
        sb.AppendLine();

        // 1. Law.
        var laws = (await rulings.ListAsync(bookId, null, ct)).Where(r => RulingKinds.BindThePage.Contains(r.Kind)).ToList();
        sb.AppendLine("## Law");
        foreach (var r in laws)
            sb.AppendLine($"- {(r.Kind == RulingKinds.PageLaw ? "[page only] " : "")}{r.Text}{(r.Pattern is null ? "" : $"  (never matches /{r.Pattern}/)")}");
        sb.AppendLine();

        // 2. World and craft.
        var docs = await db.Set<CanonDocument>().AsNoTracking().Include(d => d.Sections)
            .Where(d => (d.UniverseId == node.UniverseId || d.UniverseId == Universe.SharedId) && CanonTypes.Contains(d.DocumentType))
            .ToListAsync(ct);
        sb.AppendLine("## World and craft");
        foreach (var d in docs.OrderBy(d => Array.IndexOf(CanonTypes, d.DocumentType)))
        {
            sb.AppendLine($"### {d.Title}");
            foreach (var s in d.Sections.OrderBy(s => s.SortKey))
            {
                sb.AppendLine($"#### {s.SectionTitle ?? s.SectionKey}");
                sb.AppendLine(s.Content.Trim());
                sb.AppendLine();
            }
        }

        // 3. The unit's world.
        var tagged = unit.Beats.SelectMany(b => beats.TryGetValue(b.BeatId, out var r) ? BeatMarkup.ExtractEntityGuids(r.Text) : Enumerable.Empty<Guid>())
            .Distinct().ToList();
        var rows = await db.Entities.IgnoreQueryFilters().AsNoTracking().Where(e => tagged.Contains(e.Id))
            .Select(e => new { e.Id, e.Name, e.EntityType, e.ModifiedAt }).ToListAsync(ct);
        var entities = tagged.Select(t => rows.FirstOrDefault(r => r.Id == t)).Where(r => r != null)
            .Select(r => new BundleEntity(r!.Id, r.Name, r.EntityType, r.ModifiedAt)).ToList();
        sb.AppendLine("## The unit's world (records as they stand)");
        foreach (var e in entities)
        {
            var record = CanonRecordLoader.Load(db, e.Type, e.Id);
            sb.AppendLine($"### {e.Name} ({e.Type}, record of {e.ModifiedAt:u})");
            sb.AppendLine("```json");
            sb.AppendLine(record?.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) ?? "{}");
            sb.AppendLine("```");
        }
        sb.AppendLine();

        // 5 before 4: the unit's own text is fixed, so the prior prose gets whatever budget is left.
        var unitText = new StringBuilder();
        unitText.AppendLine($"## This unit — {unit.Heading}");
        foreach (var b in unit.Beats)
        {
            var text = Plain(b.BeatId);
            if (text.Length > 0) unitText.AppendLine(text);
            else if (beats.TryGetValue(b.BeatId, out var r))
                unitText.AppendLine($"[planned] {r.Title}{(string.IsNullOrWhiteSpace(r.Description) ? "" : " — " + r.Description)}");
            unitText.AppendLine();
        }

        // 4. Before this unit — never reset at a chapter boundary.
        var priorUnits = allPrior ? chapters.Take(unitIndex).ToList() : chapters.Skip(Math.Max(0, unitIndex - 1)).Take(unitIndex == 0 ? 0 : 1).ToList();
        var priorTexts = priorUnits.Select(c => (c.Ordinal, c.Heading,
            Text: string.Join("\n\n", c.Beats.Select(b => Plain(b.BeatId)).Where(t => t.Length > 0)))).ToList();
        var available = priorTexts.Sum(p => p.Text.Length);
        var room = Math.Max(0, budgetChars - sb.Length - unitText.Length - 200);
        var kept = new List<(int Ordinal, string Heading, string Text)>();
        var used = 0;
        var truncated = false;
        foreach (var p in Enumerable.Reverse(priorTexts))   // newest first; the oldest are what fall off
        {
            if (used + p.Text.Length <= room) { kept.Insert(0, p); used += p.Text.Length; continue; }
            var left = room - used;
            if (left > 0) { kept.Insert(0, (p.Ordinal, p.Heading, "…" + p.Text[^left..])); used += left; }
            truncated = true;
            break;
        }
        sb.AppendLine("## Before this unit");
        if (kept.Count == 0) sb.AppendLine(unitIndex == 0 ? "(the book begins here)" : "(no room within the budget)");
        foreach (var p in kept)
        {
            sb.AppendLine($"### Unit {p.Ordinal} — {p.Heading}");
            sb.AppendLine(p.Text);
            sb.AppendLine();
        }
        sb.Append(unitText);

        var content = sb.ToString();
        var dir = outDir ?? DefaultDirectory;
        Directory.CreateDirectory(dir);
        var path = System.IO.Path.Combine(dir, $"{node.Slug}.context.md");
        await File.WriteAllTextAsync(path, content, new UTF8Encoding(false), ct);
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content))).ToLowerInvariant();
        return new ContextManifest(path, bookId, bookName, unit.Ordinal, unit.Heading,
            kept.Count > 0 ? kept[0].Ordinal : null, kept.Count > 0 ? kept[^1].Ordinal : null, used, available,
            truncated || kept.Count < priorTexts.Count, entities, laws.Count, docs.Select(d => d.Title).ToList(), content.Length, hash);
    }
}
