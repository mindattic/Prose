using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using MindAttic.Authentication.Data;
using MindAttic.Authentication.Entities;

namespace Prose.Core.Data;

/// <summary>
/// Dedicated, EF-migration-managed context for the MindAttic.Authentication identity tables (the isolated
/// <c>auth</c> schema), on the SAME database as <see cref="ProseDbContext"/>. Kept separate
/// because the world tables in ProseDbContext are created by hand-written temporal SQL (EF can't
/// emit <c>PERIOD FOR SYSTEM_TIME</c>); the auth tables instead get clean, standard EF migrations.
/// </summary>
public sealed class ProseAuthDbContext(DbContextOptions<ProseAuthDbContext> options)
    : DbContext(options), IAuthDataContext
{
    public DbSet<AuthUser>               AuthUsers               => Set<AuthUser>();
    public DbSet<AuthUserMfa>            AuthUserMfa             => Set<AuthUserMfa>();
    public DbSet<AuthRecoveryCode>       AuthRecoveryCodes       => Set<AuthRecoveryCode>();
    public DbSet<AuthSession>            AuthSessions            => Set<AuthSession>();
    public DbSet<AuthLoginThrottle>      AuthLoginThrottles      => Set<AuthLoginThrottle>();
    public DbSet<AuthAuditLog>           AuthAuditLog            => Set<AuthAuditLog>();
    public DbSet<AuthPasswordHistory>    AuthPasswordHistory     => Set<AuthPasswordHistory>();
    public DbSet<AuthPasswordResetToken> AuthPasswordResetTokens => Set<AuthPasswordResetToken>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        b.ApplyMindAtticAuthConfiguration();   // all 8 tables in the 'auth' schema
    }
}

/// <summary>Design-time factory so <c>dotnet ef migrations add … --context ProseAuthDbContext</c> works.</summary>
public sealed class ProseAuthDbContextFactory : IDesignTimeDbContextFactory<ProseAuthDbContext>
{
    public ProseAuthDbContext CreateDbContext(string[] args)
    {
        var conn = Environment.GetEnvironmentVariable("ConnectionStrings__Prose")
                   ?? "Server=(localdb)\\MSSQLLocalDB;Database=Prose;Trusted_Connection=True;TrustServerCertificate=True;";
        var options = new DbContextOptionsBuilder<ProseAuthDbContext>().UseSqlServer(conn).Options;
        return new ProseAuthDbContext(options);
    }
}
