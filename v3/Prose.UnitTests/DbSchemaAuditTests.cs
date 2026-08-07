using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.UnitTests;

/// <summary>
/// SS-US-K5: verifies no Character entity table carries denormalized Location,
/// CurrentAmmo, or IsAlive columns. Those facts live exclusively in EntityStateEvents
/// (SCL-5). Uses EF model metadata — no live DB required.
/// </summary>
[TestFixture]
public class DbSchemaAuditTests
{
    private DbContextOptions<ProseDbContext> options = null!;

    [OneTimeSetUp]
    public void SetUp()
    {
        var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        options = new DbContextOptionsBuilder<ProseDbContext>()
            .UseSqlite(conn)
            .Options;
    }

    private IReadOnlyList<string> CharacterColumnNames()
    {
        using var ctx = new ProseDbContext(options);
        return ctx.Model.FindEntityType(typeof(Character))!
            .GetProperties()
            .Select(p => p.Name)
            .ToList();
    }

    [Test]
    public void Character_HasNoLocationColumn()
        => Assert.That(CharacterColumnNames(), Does.Not.Contain("Location"),
            "SCL-5: Location must live in EntityStateEvents, not on Character.");

    [Test]
    public void Character_HasNoCurrentAmmoColumn()
        => Assert.That(CharacterColumnNames(), Does.Not.Contain("CurrentAmmo"),
            "SCL-5: CurrentAmmo must live in EntityStateEvents, not on Character.");

    [Test]
    public void Character_HasNoIsAliveColumn()
        => Assert.That(CharacterColumnNames(), Does.Not.Contain("IsAlive"),
            "SCL-5: IsAlive must live in EntityStateEvents, not on Character.");
}
