using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Prose.Core.Data;

/// <summary>
/// Design-time factory for `dotnet ef` tooling. The runtime DbContext is built
/// from DI in <c>ServiceCollectionExtensions</c>; this factory only fires when
/// running `dotnet ef migrations add`/`update`. Connection string honors
/// <c>ConnectionStrings__Prose</c> or falls back to LocalDB.
/// </summary>
public class ProseDbContextFactory : IDesignTimeDbContextFactory<ProseDbContext>
{
    public ProseDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<ProseDbContext>();
        builder.UseProseSqlServer(ProseSqlResilience.ResolveConnectionString());
        return new ProseDbContext(builder.Options);
    }
}
