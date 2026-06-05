using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using MindAttic.Authentication.Data;
using MindAttic.Authentication.Entities;

namespace StreetSamurai.Core.Data;

/// <summary>
/// Dedicated, EF-migration-managed context for the MindAttic.Authentication identity tables (the isolated
/// <c>auth</c> schema), on the SAME database as <see cref="StreetSamuraiDbContext"/>. Kept separate
/// because the world tables in StreetSamuraiDbContext are created by hand-written temporal SQL (EF can't
/// emit <c>PERIOD FOR SYSTEM_TIME</c>); the auth tables instead get clean, standard EF migrations.
/// </summary>
public sealed class StreetSamuraiAuthDbContext(DbContextOptions<StreetSamuraiAuthDbContext> options)
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

/// <summary>Design-time factory so <c>dotnet ef migrations add … --context StreetSamuraiAuthDbContext</c> works.</summary>
public sealed class StreetSamuraiAuthDbContextFactory : IDesignTimeDbContextFactory<StreetSamuraiAuthDbContext>
{
    public StreetSamuraiAuthDbContext CreateDbContext(string[] args)
    {
        var conn = Environment.GetEnvironmentVariable("ConnectionStrings__StreetSamurai")
                   ?? "Server=(localdb)\\MSSQLLocalDB;Database=StreetSamurai;Trusted_Connection=True;TrustServerCertificate=True;";
        var options = new DbContextOptionsBuilder<StreetSamuraiAuthDbContext>().UseSqlServer(conn).Options;
        return new StreetSamuraiAuthDbContext(options);
    }
}
