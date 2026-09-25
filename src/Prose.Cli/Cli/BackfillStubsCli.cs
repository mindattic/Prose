using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;

namespace Prose.Cli;

/// <summary>
/// <c>prose --backfill-stubs</c> — backfill <c>Entities.Status</c> = 'stub' / 'canon' based on
/// <c>BeatEntityMentions</c>. Entities with no BeatEntityMentions row → Status='stub' (excluded
/// from the universe graph). Entities that ARE mentioned → Status='canon'. Re-run after
/// <c>--scan-entity-mentions</c>.
/// </summary>
public static class BackfillStubsCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var db2 = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var ctx2 = await db2.CreateDbContextAsync();
        // Only the stub <-> canon flip. The raw SQL excluded just 'archived', so a merged or retired
        // entity with stale mentions came back as canon and one without became a stub; it also
        // spanned every universe. EF's universe filter scopes these to the current one.
        var now = DateTime.UtcNow;
        var promoted = await ctx2.Entities
            .Where(e => e.Status == "stub" && ctx2.BeatEntityMentions.Any(m => m.EntityId == e.Id))
            .ExecuteUpdateAsync(u => u.SetProperty(e => e.Status, "canon").SetProperty(e => e.ModifiedAt, now));
        var demoted = await ctx2.Entities
            .Where(e => e.Status == "canon" && !ctx2.BeatEntityMentions.Any(m => m.EntityId == e.Id))
            .ExecuteUpdateAsync(u => u.SetProperty(e => e.Status, "stub").SetProperty(e => e.ModifiedAt, now));
        Console.WriteLine($"[backfill-stubs] promoted={promoted} canon, demoted={demoted} stub.");
        return 0;
    }
}
