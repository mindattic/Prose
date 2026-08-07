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
        var connStr =
            Environment.GetEnvironmentVariable("ConnectionStrings__Prose")
            ?? @"Server=(localdb)\MSSQLLocalDB;Database=Prose;Trusted_Connection=True;";

        var opts = new DbContextOptionsBuilder<ProseDbContext>()
            .UseSqlServer(connStr)
            .Options;
        return new ProseDbContext(opts);
    }
}
