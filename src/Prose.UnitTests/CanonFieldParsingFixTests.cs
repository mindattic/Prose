using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Models.Canon;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Regression tests for field-parsing defects fixed 2026-09-29: a place exit with a string/decimal
/// danger level failed the whole place, News.PublishedDate was parsed with the machine culture and
/// kept a stale value, a null quote threw in QuoteMapper, TolerantStringConverter wrote
/// culture-formatted decimals, and AssignTiers treated a synthetic's empty "tier" as assigned.
/// </summary>
[TestFixture]
public class CanonFieldParsingFixTests
{
    [Test]
    public void PlaceExit_StringAndDecimalScalars_DoNotFailDeserialization()
    {
        var exit = JsonSerializer.Deserialize<PlaceExit>(
            """{"direction":"north","restricted":"true","danger_level":"3"}""")!;
        Assert.That(exit.Restricted, Is.True);
        Assert.That(exit.DangerLevel, Is.EqualTo(3));

        var rounded = JsonSerializer.Deserialize<PlaceExit>("""{"danger_level":2.6,"restricted":false}""")!;
        Assert.That(rounded.DangerLevel, Is.EqualTo(3));
        Assert.That(rounded.Restricted, Is.False);

        var junk = JsonSerializer.Deserialize<PlaceExit>("""{"danger_level":"high","restricted":"maybe"}""")!;
        Assert.That(junk.DangerLevel, Is.Zero);
        Assert.That(junk.Restricted, Is.False);
    }

    [Test]
    public void NewsMapper_PublishedDate_IsInvariant_AndClearedWhenUnparseable()
    {
        var saved = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("en-GB");   // dd/MM/yyyy
            var row = new Prose.Core.Data.Entities.News();
            NewsMapper.FillScalars(row, new NewsData { Headline = "h", Date = "03/04/2089" });
            Assert.That(row.PublishedDate, Is.EqualTo(new DateTime(2089, 3, 4)), "invariant culture reads MM/dd");

            NewsMapper.FillScalars(row, new NewsData { Headline = "h", Date = "the week of the flood" });
            Assert.That(row.PublishedDate, Is.Null, "a date that no longer parses must not keep the old value");
        }
        finally { CultureInfo.CurrentCulture = saved; }
    }

    [Test]
    public void QuoteMapper_NullQuote_DoesNotThrow()
    {
        var src = JsonSerializer.Deserialize<QuoteData>("""{"quote":null,"category":"street"}""")!;
        var row = new Quote();
        Assert.DoesNotThrow(() => QuoteMapper.FillScalars(row, src));
        Assert.That(row.QuoteText, Is.EqualTo(""));
        Assert.That(row.Name, Is.EqualTo(""));
    }

    [Test]
    public void TolerantStringConverter_WritesInvariantDecimals()
    {
        var saved = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            var m = JsonSerializer.Deserialize<MaterialData>("""{"cost":12.5}""")!;
            Assert.That(m.Cost, Is.EqualTo("12.5"));
        }
        finally { CultureInfo.CurrentCulture = saved; }
    }

    [Test]
    public async Task AssignTiers_EmptyStringTier_IsTreatedAsUnassigned()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"ss_tiers_empty_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var paths = new TestPathProviderWithRoot(tempDir);
        try
        {
            TestDbFactory.Reset(paths);
            var factory = TestDbFactory.For(paths, "tiers");
            var id = Guid.NewGuid();
            using (var db = factory.CreateDbContext())
            {
                db.Entities.Add(new Entity
                {
                    Id = id, EntityType = "synthetic", Name = "Unit-7", Slug = $"unit-7-{id:N}",
                    Status = "canon", CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow,
                });
                // SyntheticLifeData serializes an unset tier as "".
                db.Records.Add(new Record { EntityId = id, Json = """{"name":"Unit-7","role":"CEO","tier":""}""" });
                db.SaveChanges();
            }

            await new AssignTiersService(factory).RunAsync(parallelism: 1);

            using var read = factory.CreateDbContext();
            var json = JsonNode.Parse(read.Records.First(r => r.EntityId == id).Json)!.AsObject();
            Assert.That(json["tier"]?.GetValue<string>(), Is.EqualTo("5"));
        }
        finally
        {
            TestDbFactory.Reset(paths);
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
        }
    }
}
