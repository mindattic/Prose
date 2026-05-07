using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace StreetSamurai.Core.Data;

/// <summary>
/// Design-time factory for `dotnet ef` tooling. The runtime DbContext is built
/// from DI in <c>ServiceCollectionExtensions</c>; this factory only fires when
/// running `dotnet ef migrations add`/`update`. Connection string honors
/// <c>ConnectionStrings__StreetSamurai</c> or falls back to LocalDB.
/// </summary>
public class StreetSamuraiDbContextFactory : IDesignTimeDbContextFactory<StreetSamuraiDbContext>
{
    public StreetSamuraiDbContext CreateDbContext(string[] args)
    {
        var connStr =
            Environment.GetEnvironmentVariable("ConnectionStrings__StreetSamurai")
            ?? @"Server=(localdb)\MSSQLLocalDB;Database=StreetSamurai;Trusted_Connection=True;";

        var opts = new DbContextOptionsBuilder<StreetSamuraiDbContext>()
            .UseSqlServer(connStr)
            .Options;
        return new StreetSamuraiDbContext(opts);
    }
}
