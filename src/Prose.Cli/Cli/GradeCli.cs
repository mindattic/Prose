using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;
using Prose.Core.Services.Audit;
using Prose.Core.Services.Factory;

namespace Prose.Cli;

/// <summary>
/// prose --grade --slug &lt;book&gt; | --series &lt;series&gt; | --universe &lt;slug&gt; --a &lt;V|date|working&gt; --b &lt;V|date|working&gt;
///
/// Version grader (PRS-T866), deterministic battery only — no LLM, no jury. Each version is a press
/// (ExportRecord → its ArchivedBook snapshot; a date names the newest press on or before it), an
/// archive, or <c>working</c> (the live beats). Measures both with <see cref="VersionGrader"/> and
/// prints a per-metric A-vs-B table with a weighted win count (config <c>Grade:Weights</c>, key →
/// weight, default 1; <c>metric</c> weighs every metric-ruling row). No 0-100 score. A series or
/// universe grades each book and rolls the measurements up by summing; a book missing either
/// version is listed and left out of the rollup. Writes <c>&lt;CODE&gt;_Grade_&lt;A&gt;_vs_&lt;B&gt;.md</c>
/// to the book's export folder (the universe export root for a series or universe) and one
/// InstrumentRun row per graded book.
/// </summary>
public static class GradeCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? Flag(string name) { var i = Array.IndexOf(args, name); return i >= 0 && i + 1 < args.Length ? args[i + 1] : null; }
        var slug = Flag("--slug"); var series = Flag("--series"); var universe = Flag("--universe");
        var aRaw = Flag("--a"); var bRaw = Flag("--b");
        if ((slug ?? series ?? universe) is null || aRaw is null || bRaw is null)
        {
            Console.Error.WriteLine("Usage: prose --grade --slug <book> | --series <series> | --universe <slug> --a <V|date|working> --b <V|date|working>");
            return 2;
        }
        GradeVersionSpec a, b;
        try { a = GradeVersionSpec.Parse(aRaw); b = GradeVersionSpec.Parse(bRaw); }
        catch (ArgumentException ex) { Console.Error.WriteLine($"[grade] {ex.Message}"); return 2; }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        var grader = services.GetRequiredService<VersionGrader>();
        var settings = services.GetRequiredService<SettingsService>();
        var ledger = services.GetRequiredService<InstrumentRunLedger>();
        var weights = services.GetService<IConfiguration>()?.GetSection("Grade:Weights").GetChildren()
            .Where(c => double.TryParse(c.Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out _))
            .ToDictionary(c => c.Key, c => double.Parse(c.Value!, System.Globalization.CultureInfo.InvariantCulture), StringComparer.OrdinalIgnoreCase);

        await using var db = await dbFactory.CreateDbContextAsync();
        Node? scope = null; Guid universeId; string code;
        List<Node> books;
        if (slug is not null || series is not null)
        {
            var id = await NodeRefResolver.ResolveAsync(db, slug ?? series);
            if (id is null) { Console.Error.WriteLine($"[grade] node not found: {slug ?? series}"); return 1; }
            scope = await db.Nodes.IgnoreQueryFilters().AsNoTracking().FirstAsync(n => n.Id == id);
            universeId = scope.UniverseId;
            code = scope.NodeCode ?? scope.Slug;
            books = slug is not null ? [scope]
                : await db.Nodes.IgnoreQueryFilters().AsNoTracking().OfType<BookNode>()
                    .Where(n => n.ParentNodeId == scope.Id && !n.IsArchived).OrderBy(n => n.SortKey).Cast<Node>().ToListAsync();
        }
        else
        {
            var u = await db.Universes.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(x => x.Slug == universe);
            if (u is null) { Console.Error.WriteLine($"[grade] universe not found: {universe}"); return 1; }
            universeId = u.Id; code = u.Slug.ToUpperInvariant();
            books = await db.Nodes.IgnoreQueryFilters().AsNoTracking().OfType<BookNode>()
                .Where(n => n.UniverseId == u.Id && n.IsCanon && !n.IsArchived).OrderBy(n => n.Timeline).ThenBy(n => n.Title).Cast<Node>().ToListAsync();
        }
        if (books.Count == 0) { Console.Error.WriteLine("[grade] no books in scope."); return 1; }

        var graded = new List<(Node Book, GradeSnapshot A, GradeSnapshot B, List<GradeMeasure> MA, List<GradeMeasure> MB)>();
        var skipped = new List<string>();
        foreach (var book in books)
        {
            var sa = await grader.ResolveAsync(book.Id, a);
            var sb = await grader.ResolveAsync(book.Id, b);
            if (sa is null || sb is null)
            {
                skipped.Add($"{book.Title} ({book.NodeCode ?? book.Slug}): no {(sa is null ? a.Label : b.Label)}");
                continue;
            }
            graded.Add((book, sa, sb, await grader.MeasureAsync(book.Id, sa), await grader.MeasureAsync(book.Id, sb)));
        }
        if (graded.Count == 0) { foreach (var s in skipped) Console.Error.WriteLine($"[grade] skipped {s}"); return 1; }

        var title = slug is not null ? graded[0].Book.Title : scope?.Title ?? code;
        var md = new StringBuilder();
        md.AppendLine($"# {title} — grade {a.Label} vs {b.Label}");
        md.AppendLine();
        md.AppendLine($"_Deterministic battery, {DateTime.UtcNow:yyyy-MM-dd HH:mm}Z. A = {a.Label}, B = {b.Label}. Today's rulings and plant registry applied to both texts. ↑/↓ = which way wins; unarrowed rows are shown, never won._");
        md.AppendLine();
        if (graded.Count > 1)
        {
            md.AppendLine($"## Rollup ({graded.Count} books)");
            md.AppendLine();
            md.AppendLine(GradeMath.Table(GradeMath.Compare(GradeMath.Rollup(graded.Select(g => (IReadOnlyList<GradeMeasure>)g.MA)),
                GradeMath.Rollup(graded.Select(g => (IReadOnlyList<GradeMeasure>)g.MB)), weights), "A", "B"));
        }
        foreach (var g in graded)
        {
            var c = GradeMath.Compare(g.MA, g.MB, weights);
            md.AppendLine($"## {g.Book.Title} ({g.Book.NodeCode ?? g.Book.Slug})");
            md.AppendLine();
            md.AppendLine($"A: {g.A.Source} · B: {g.B.Source}");
            md.AppendLine();
            md.AppendLine(GradeMath.Table(c, "A", "B"));
            Console.WriteLine($"[grade] {g.Book.Title}: A {c.AWins:0.##} · B {c.BWins:0.##} → {c.Verdict}");
            await ledger.RecordAsync(g.Book.Id, InstrumentRunLedger.VersionGrade, g.A.Beats.Count + g.B.Beats.Count,
                g.A.Beats.Count + g.B.Beats.Count, detail: $"{a.Label} vs {b.Label}: A {c.AWins:0.##} B {c.BWins:0.##} ties {c.Ties:0.##} -> {c.Verdict}");
        }
        if (skipped.Count > 0)
        {
            md.AppendLine("## Not graded");
            md.AppendLine();
            foreach (var s in skipped) md.AppendLine($"- {s}");
        }

        var universeSlug = await db.Universes.IgnoreQueryFilters().AsNoTracking().Where(u => u.Id == universeId).Select(u => u.Slug).FirstOrDefaultAsync();
        var dir = settings.GetExportDirectory(universeSlug);
        if (slug is not null) (dir, _) = await ExportPathResolver.ResolveAsync(db, graded[0].Book, dir, CancellationToken.None);
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, $"{Safe(code.ToUpperInvariant())}_Grade_{Safe(a.Label)}_vs_{Safe(b.Label)}.md");
        await File.WriteAllTextAsync(path, md.ToString(), new UTF8Encoding(false));
        Console.WriteLine(md.ToString());
        Console.WriteLine($"[grade] report -> {path}");
        return 0;
    }

    private static string Safe(string s) => string.Concat(s.Select(ch => Path.GetInvalidFileNameChars().Contains(ch) ? '-' : ch));
}
