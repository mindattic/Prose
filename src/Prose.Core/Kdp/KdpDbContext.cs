using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Prose.Core.Kdp;

/// <summary>
/// The KDP store: one machine-local SQLite file (<see cref="KdpPaths.ResolveDbPath"/>) holding
/// the KDP-only state KdpPublish, the <c>prose --kdp-*</c> commands and the operator tools share.
/// Deliberately separate from <see cref="Data.ProseDbContext"/> (SQL Server, shared by every
/// Prose front end): books are still read from there, and nothing here duplicates a book field.
/// Schema changes ship as EF migrations under <c>Kdp/Migrations</c>
/// (<c>dotnet ef migrations add &lt;Name&gt; --context KdpDbContext --output-dir Kdp/Migrations</c>).
/// JSON files are import/export only — see <see cref="KdpJsonTransfer"/>.
/// </summary>
public sealed class KdpDbContext(DbContextOptions<KdpDbContext> options) : DbContext(options)
{
    public DbSet<KdpTitle> Titles => Set<KdpTitle>();
    public DbSet<KdpTitleNote> TitleNotes => Set<KdpTitleNote>();
    public DbSet<KdpBook> Books => Set<KdpBook>();
    public DbSet<KdpPublishRecord> PublishRecords => Set<KdpPublishRecord>();
    public DbSet<KdpCategoryTree> CategoryTrees => Set<KdpCategoryTree>();
    public DbSet<KdpRun> Runs => Set<KdpRun>();
    public DbSet<KdpRunLine> RunLines => Set<KdpRunLine>();
    public DbSet<KdpImport> Imports => Set<KdpImport>();

    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        // SQLite has no native DateTimeOffset. Stored as a sortable long so ORDER BY and range
        // queries run in SQL. UTC ticks rather than EF's DateTimeOffsetToBinaryConverter because
        // that one drops the last digits of precision, and a round trip must reproduce the
        // 7-digit "O" timestamps the .publish markers carry. Every KDP timestamp is UTC.
        builder.Properties<DateTimeOffset>().HaveConversion<UtcTicksConverter>();
        builder.Properties<DateTimeOffset?>().HaveConversion<UtcTicksConverter>();
        // decimal as TEXT keeps exact values (SQLite REAL would round).
        builder.Properties<decimal>().HaveConversion<string>();
        builder.Properties<decimal?>().HaveConversion<string>();
        // Enums as their names: readable in any SQLite browser, and reordering an enum can't
        // silently remap stored rows.
        builder.Properties<KdpPublishSource>().HaveConversion<string>();
        builder.Properties<KdpImportKind>().HaveConversion<string>();
    }

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<KdpTitle>(t =>
        {
            t.HasKey(x => x.Code);
            t.HasIndex(x => x.TitleId);
        });

        model.Entity<KdpTitleNote>().HasKey(x => x.Key);

        model.Entity<KdpBook>(b =>
        {
            b.HasKey(x => x.Code);
            // Fixed value groups with no identity of their own: complex types, stored as columns
            // on the Books row (SignOff_Ready, LastPublish_File, ...).
            b.ComplexProperty(x => x.SignOff);
            b.ComplexProperty(x => x.LastPublish);
            b.HasMany(x => x.History).WithOne().HasForeignKey(x => x.Code).OnDelete(DeleteBehavior.Cascade);
        });

        model.Entity<KdpPublishRecord>(r =>
        {
            r.ComplexProperty(x => x.Publish);
            r.HasIndex(x => new { x.Code, x.RecordedAt });
        });

        model.Entity<KdpCategoryTree>(c =>
        {
            c.HasKey(x => x.Slug);
            c.ComplexProperty(x => x.Crawl);
            c.Property(x => x.Tree).HasJson();
        });

        model.Entity<KdpRun>(r =>
        {
            r.HasIndex(x => x.StartedAt);
            r.HasIndex(x => x.LogFileName).IsUnique();
            r.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.RunId).OnDelete(DeleteBehavior.Cascade);
        });
        model.Entity<KdpRunLine>().HasIndex(x => new { x.RunId, x.Id });

        model.Entity<KdpImport>(i =>
        {
            i.ComplexProperty(x => x.Counts);
            i.HasIndex(x => x.Kind);
        });
    }
}

/// <summary>DateTimeOffset ↔ UTC ticks. Lossless for UTC values (the only kind this store holds).</summary>
internal sealed class UtcTicksConverter() : ValueConverter<DateTimeOffset, long>(
    v => v.UtcTicks,
    v => new DateTimeOffset(v, TimeSpan.Zero));

internal static class KdpJsonColumns
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>
    /// Stores a property as one JSON text column. For shapes always read and written whole (the
    /// recursive category tree) that would otherwise cost a table per level. The comparer makes
    /// EF detect in-place edits.
    /// </summary>
    public static PropertyBuilder<T> HasJson<T>(this PropertyBuilder<T> property) where T : class, new()
    {
        property.HasConversion(
            new ValueConverter<T, string>(
                v => JsonSerializer.Serialize(v, Options),
                s => string.IsNullOrWhiteSpace(s) ? new T() : JsonSerializer.Deserialize<T>(s, Options) ?? new T()),
            new ValueComparer<T>(
                (a, b) => JsonSerializer.Serialize(a, Options) == JsonSerializer.Serialize(b, Options),
                v => JsonSerializer.Serialize(v, Options).GetHashCode(),
                v => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(v, Options), Options)!));
        return property;
    }
}
