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
        var promoted = await ctx2.Database.ExecuteSqlRawAsync(
            "UPDATE Entities SET Status = 'canon', ModifiedAt = SYSUTCDATETIME() WHERE Status != 'canon' AND Status != 'archived' AND Id IN (SELECT DISTINCT EntityId FROM BeatEntityMentions)");
        var demoted = await ctx2.Database.ExecuteSqlRawAsync(
            "UPDATE Entities SET Status = 'stub', ModifiedAt = SYSUTCDATETIME() WHERE Status != 'stub' AND Status != 'archived' AND Id NOT IN (SELECT DISTINCT EntityId FROM BeatEntityMentions)");
        Console.WriteLine($"[backfill-stubs] promoted={promoted} canon, demoted={demoted} stub.");
        return 0;
    }
}
