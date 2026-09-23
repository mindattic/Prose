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
    IReadOnlyList<BundleEntity> Entities, int Laws, int TotalChars, string Hash,
    string WorldPath, IReadOnlyList<string> CanonDocuments, int WorldChars, string WorldHash);

/// <summary>
/// The writer's working memory for one unit (RFC 0015 §3.9), derived, rebuilt on every call and
/// never edited. Two files:
/// <list type="bullet">
///   <item><c>{slug}.world.md</c> — the book's law and the universe's world and craft canon. The
///         same for every unit of the book, so a writer reads it once per session.</item>
///   <item><c>{slug}.context.md</c> — the unit's own working memory, within the budget: the law
///         again (it is short and binds every line), the records of every entity the unit tags as
///         they stand, the prose before the unit — the previous unit, or every preceding unit within
///         the budget, oldest dropped first, never cut at a chapter boundary — and the unit itself
///         (its prose, or its planned beats).</item>
/// </list>
/// <para>This is the fix for the old writer's blindness: its "scene so far" reset to empty at every
/// chapter and was capped at 6KB, so each chapter was written without the one before it. The canon
/// lives in its own file because in one file it crowded the previous chapter out of the budget —
/// BCODA's first bundle for unit 2 held 190K of canon and none of unit 1.</para>
/// </summary>
public sealed class ContextBundleService(IDbContextFactory<ProseDbContext> dbFactory, BookSpineService spine, RulingService rulings)
{
    /// <summary>Canon document types that are the world and its craft, world first. The universe-scoped
    /// ones are read from the book's universe, the shared craft guides from <see cref="Universe.SharedId"/>.
    /// Engine documents (EngineGuide) and the franchise plan are not the world and are left out.</summary>
    public static readonly string[] CanonTypes =
        ["WorldMaster", "UniverseCanon", "WorldBible", "UniverseCraft", "CraftGuide", "CharacterDoctrine", "DelightGuide"];

    /// <summary>The unit file's budget in characters (the world file has none: it is what the canon is).</summary>
    public const int DefaultBudget = 400_000;

    public static string DefaultDirectory =>
        System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Prose", "factory", "context");

    private static readonly JsonSerializerOptions RecordJson = new() { WriteIndented = false };

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
        var bookName = node.NodeCode ?? node.Slug;
        var dir = outDir ?? DefaultDirectory;
        Directory.CreateDirectory(dir);

        // Law, shared by both files.
        var laws = (await rulings.ListAsync(bookId, null, ct)).Where(r => RulingKinds.BindThePage.Contains(r.Kind)).ToList();
        var law = new StringBuilder();
        law.AppendLine("## Law");
        foreach (var r in laws)
            law.AppendLine($"- {(r.Kind == RulingKinds.PageLaw ? "[page only] " : "")}{r.Text}{(r.Pattern is null ? "" : $"  (never matches /{r.Pattern}/)")}");
        law.AppendLine();

        // The world file: law, then world and craft.
        var docs = await db.Set<CanonDocument>().AsNoTracking().Include(d => d.Sections)
            .Where(d => (d.UniverseId == node.UniverseId || d.UniverseId == Universe.SharedId) && CanonTypes.Contains(d.DocumentType))
            .ToListAsync(ct);
        var world = new StringBuilder();
        world.AppendLine($"# World — {node.Title} ({bookName})");
        world.AppendLine();
        world.AppendLine("Derived, rebuilt on every call, never edited. The same for every unit of the book: read it once per session.");
        world.AppendLine();
        world.Append(law);
        world.AppendLine("## World and craft");
        foreach (var d in docs.OrderBy(d => Array.IndexOf(CanonTypes, d.DocumentType)))
        {
            world.AppendLine($"### {d.Title}");
            foreach (var s in d.Sections.OrderBy(s => s.SortKey))
            {
                if (string.IsNullOrWhiteSpace(s.Content)) continue;
                world.AppendLine($"#### {s.SectionTitle ?? s.SectionKey}");
                world.AppendLine(s.Content.Trim());
                world.AppendLine();
            }
        }
        var worldText = world.ToString();
        var worldPath = System.IO.Path.Combine(dir, $"{node.Slug}.world.md");
        await File.WriteAllTextAsync(worldPath, worldText, new UTF8Encoding(false), ct);

        // The unit file.
        var sb = new StringBuilder();
        sb.AppendLine($"# Context — {node.Title} ({bookName}), unit {unit.Ordinal}: {unit.Heading}");
        sb.AppendLine();
        sb.AppendLine($"Derived, rebuilt on every call, never edited. The prose answers to everything below, and to the world file ({System.IO.Path.GetFileName(worldPath)}).");
        sb.AppendLine();
        sb.Append(law);

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
            sb.AppendLine(record?.ToJsonString(RecordJson) ?? "{}");
        }
        sb.AppendLine();

        // The unit's own text is fixed, so the prose before it gets whatever budget is left.
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

        // Before this unit — never reset at a chapter boundary.
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
        var path = System.IO.Path.Combine(dir, $"{node.Slug}.context.md");
        await File.WriteAllTextAsync(path, content, new UTF8Encoding(false), ct);
        return new ContextManifest(path, bookId, bookName, unit.Ordinal, unit.Heading,
            kept.Count > 0 ? kept[0].Ordinal : null, kept.Count > 0 ? kept[^1].Ordinal : null, used, available,
            truncated || kept.Count < priorTexts.Count, entities, laws.Count, content.Length, Hash(content),
            worldPath, docs.Select(d => d.Title).ToList(), worldText.Length, Hash(worldText));
    }

    private static string Hash(string s) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(s))).ToLowerInvariant();
}
