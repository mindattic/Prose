using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Prose.Core.Data;

/// <summary>
/// Design-time factory for `dotnet ef` tooling. The runtime DbContext is built
/// from DI in <c>ServiceCollectionExtensions</c>; this factory only fires when
/// running `dotnet ef migrations add`/`update`. Connection string honors
/// <c>ConnectionStrings__Prose</c> or falls back to the shared Azure SQL database
/// (<see cref="ProseSqlResilience.AzureDeveloperConnectionString"/>, the developer's own sign-in).
///
/// <para>NOTE (PRS-T1009): there is no local default any more, so <c>dotnet ef database update</c>
/// migrates the SHARED CLOUD database unless <c>ConnectionStrings__Prose</c> points somewhere else.
/// <c>dotnet ef migrations add</c> only reads the model and touches no database.</para>
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
