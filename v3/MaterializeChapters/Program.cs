using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Extensions;
using StreetSamurai.Core.Services;

// ── MaterializeChapters ────────────────────────────────────────────────────
// One-shot. Finds every chapter strand whose StrandBeat row count is zero and
// whose source Chapter (Records.Json) has body prose, then calls
// StrandWorkbenchService.MaterializeChapterFromHtmlAsync to burst the prose
// into one Beat per paragraph. Idempotent — safe to re-run; chapters with
// any existing beats are skipped.
//
// Run:  dotnet run --project v3/MaterializeChapters

var services = new ServiceCollection();
services.AddLogging(b =>
{
    b.SetMinimumLevel(LogLevel.Information);
    b.AddSimpleConsole(o => { o.SingleLine = true; o.TimestampFormat = "HH:mm:ss "; });
});
services.AddStreetSamuraiServices();
using var sp = services.BuildServiceProvider();

var dbFactory = sp.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();
var workbench = sp.GetRequiredService<StrandWorkbenchService>();

await using var db = await dbFactory.CreateDbContextAsync();

// Find chapter strands with zero StrandBeat rows.
var emptyChapters = await db.Strands
    .AsNoTracking()
    .Where(s => s.Kind == "chapter")
    .Where(s => !db.StrandBeats.Any(sb => sb.StrandId == s.Id))
    .OrderBy(s => s.Title)
    .Select(s => new { s.Id, s.Title, s.Slug, s.ParentStrandId })
    .ToListAsync();

if (emptyChapters.Count == 0)
{
    Console.WriteLine("No chapter strands with zero beats. Nothing to do.");
    return 0;
}

Console.WriteLine($"Found {emptyChapters.Count} empty chapter strand(s):");
foreach (var c in emptyChapters)
    Console.WriteLine($"  • {c.Title}  ({c.Slug})");
Console.WriteLine();

int total = 0;
int converted = 0;
int empty = 0;
int failed = 0;
foreach (var c in emptyChapters)
{
    try
    {
        var beats = await workbench.MaterializeChapterFromHtmlAsync(c.Id);
        if (beats > 0)
        {
            Console.WriteLine($"  ✓ {c.Title}: {beats} beats");
            converted++;
            total += beats;
        }
        else
        {
            Console.WriteLine($"  ○ {c.Title}: no prose body — skipped");
            empty++;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  ✗ {c.Title}: {ex.GetType().Name}: {ex.Message}");
        failed++;
    }
}

Console.WriteLine();
Console.WriteLine($"Done. {converted} converted, {empty} empty/skipped, {failed} failed. {total} total beats created.");
return failed > 0 ? 1 : 0;
