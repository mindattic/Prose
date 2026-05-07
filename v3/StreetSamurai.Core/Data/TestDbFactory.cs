using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Core.Data;

/// <summary>
/// SQLite-in-memory <see cref="IDbContextFactory{StreetSamuraiDbContext}"/> for unit
/// tests that construct a repository with just <see cref="IPathProvider"/>. Each
/// distinct path provider gets its own in-memory database; shared SQLite cache
/// keeps the schema alive across short-lived contexts.
///
/// Production code never hits this — the DI-registered SQL Server factory is
/// supplied to the IDbContextFactory ctors instead. This is exclusively a test
/// affordance to keep the legacy `(IPathProvider)` repo signatures compiling.
/// </summary>
public static class TestDbFactory
{
    private static readonly ConcurrentDictionary<string, IDbContextFactory<StreetSamuraiDbContext>> Cache = new();

    public static IDbContextFactory<StreetSamuraiDbContext> For(IPathProvider paths, string _entityType)
    {
        var key = paths.EngineDataDir ?? "default";
        return Cache.GetOrAdd(key, _ => new SqliteInMemoryFactory(paths.EngineDataDir));
    }

    /// <summary>Drop the cached factory for a given path. Useful when a test fixture wants a clean slate between fixtures sharing the same root.</summary>
    public static void Reset(IPathProvider paths)
    {
        var key = paths.EngineDataDir ?? "default";
        if (Cache.TryRemove(key, out var f) && f is IDisposable d) d.Dispose();
    }

    private sealed class SqliteInMemoryFactory : IDbContextFactory<StreetSamuraiDbContext>, IDisposable
    {
        private readonly SqliteConnection keepAlive;
        private readonly DbContextOptions<StreetSamuraiDbContext> options;

        public SqliteInMemoryFactory(string engineDataDir)
        {
            // Open a connection and keep it alive for the lifetime of this factory.
            // SQLite ":memory:" databases vanish when the last connection closes; we
            // pin the connection here and pass it directly to UseSqlite so every
            // context created from this factory shares the same underlying memory DB.
            keepAlive = new SqliteConnection("DataSource=:memory:");
            keepAlive.Open();

            options = new DbContextOptionsBuilder<StreetSamuraiDbContext>()
                .UseSqlite(keepAlive)
                .Options;

            using var ctx = new StreetSamuraiDbContext(options);
            ctx.Database.EnsureCreated();

            // Seed from real engine/data JSON files when present so tests that
            // expect canon data ("Kyle exists", "all characters have descriptions")
            // continue to pass against the in-memory SQL store. No-op when the test
            // points at a temp dir.
            SeedFromJson(ctx, engineDataDir);
        }

        public StreetSamuraiDbContext CreateDbContext()
            => new StreetSamuraiDbContext(options);

        public void Dispose()
        {
            keepAlive.Close();
            keepAlive.Dispose();
        }

        private static void SeedFromJson(StreetSamuraiDbContext db, string engineDataDir)
        {
            if (string.IsNullOrEmpty(engineDataDir) || !Directory.Exists(engineDataDir)) return;

            // Each entry: directory under engineDataDir → entity type the EF rows expect.
            var seeds = new (string Dir, string Type)[]
            {
                ("people",          "character"),
                ("places",          "place"),
                ("factions",        "faction"),
                ("corponations",    "corponation"),
                ("subsidiaries",    "subsidiary"),
                ("synthetics",      "synthetic"),
                ("automata",        "automaton"),
                ("weaponry",        "weapon"),
                ("equipment",       "equipment"),
                ("cyberware",       "cyberware"),
                ("apparel",         "apparel"),
                ("ammunition",      "ammunition"),
                ("pharmaceuticals", "pharmaceutical"),
                ("genemods",        "genemod"),
                ("materials",       "material"),
                ("transportation",  "transportation"),
                ("consumer_goods",  "consumer_good"),
                ("archetypes",      "archetype"),
                ("quotes",          "quote"),
                ("news",            "news"),
                ("contracts",       "contract"),
                ("documents",       "document"),
                ("vocabulary",      "vocabulary"),
                ("lab_specimens",   "lab_specimen"),
                ("psionics",        "psionic"),
                ("technology",      "technology"),
                ("flyover_entities","flyover_entity"),
                ("entertainment",   "entertainment"),
                ("facets",          "facet"),
                ("motifs",          "motif"),
            };

            // Local slug tracker — db.Entities.Any() doesn't see pending-but-unflushed
            // adds in the same SaveChanges, so we track slugs in process memory.
            var seenSlugs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var seenIds = new HashSet<Guid>();

            foreach (var (sub, type) in seeds)
            {
                var dir = Path.Combine(engineDataDir, sub);
                if (!Directory.Exists(dir)) continue;
                foreach (var file in Directory.EnumerateFiles(dir, "*.json"))
                {
                    try
                    {
                        var raw = File.ReadAllText(file);
                        using var doc = JsonDocument.Parse(raw);
                        var root = doc.RootElement;
                        if (!root.TryGetProperty("id", out var idEl)) continue;
                        var idStr = idEl.GetString() ?? "";
                        if (!Guid.TryParseExact(idStr, "N", out var id) && !Guid.TryParse(idStr, out id)) continue;

                        var name =
                            (root.TryGetProperty("name", out var n) ? n.GetString() : null)
                            ?? (root.TryGetProperty("title", out var t) ? t.GetString() : null)
                            ?? (root.TryGetProperty("term", out var tm) ? tm.GetString() : null)
                            ?? (root.TryGetProperty("codename", out var cn) ? cn.GetString() : null)
                            ?? Path.GetFileNameWithoutExtension(file);

                        var description =
                            (root.TryGetProperty("description", out var d) ? d.GetString() : null)
                            ?? (root.TryGetProperty("body", out var bd) ? bd.GetString() : null);

                        if (!seenIds.Add(id)) continue;
                        var slug = WorldGraphService.Slugify(name ?? "");
                        var compositeKey = type + "|" + slug;
                        // Skip slug collisions within the same type — real data has dupes
                        // (two characters named "Sasha", etc.). The first one wins; the
                        // second can be handled manually via /entity/stub.
                        if (!seenSlugs.Add(compositeKey)) continue;
                        db.Entities.Add(new Entity
                        {
                            Id = id,
                            EntityType = type,
                            Name = name ?? "",
                            Slug = slug,
                            Status = "canon",
                            Description = description,
                            CreatedAt = DateTime.UtcNow,
                            ModifiedAt = DateTime.UtcNow,
                            IsActive = true,
                        });
                        db.Records.Add(new Entities.Record { EntityId = id, Json = raw, UpdatedAt = DateTime.UtcNow });

                        // Characters now live on a fully relational schema. Seed
                        // the columnar Characters row + every bridge from the
                        // same JSON blob so CharacterRepository.GetAll (which
                        // reads from columns, not Records.Json) returns them.
                        if (type == "character")
                        {
                            try
                            {
                                var charData = JsonSerializer.Deserialize<Models.Canon.CharacterData>(raw,
                                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                                if (charData != null)
                                {
                                    var ch = new Character { Id = id };
                                    CharacterMapper.FillScalars(ch, charData);
                                    db.Characters.Add(ch);
                                    CharacterMapper.FillBridges(db, id, charData);
                                }
                            }
                            catch { /* malformed character — Records.Json still holds the blob */ }
                        }
                    }
                    catch { /* malformed file — skip */ }
                }
            }

            // Singleton settings: tone bible, story bible, literary rules, character profile.
            foreach (var (file, key) in new (string, string)[]
            {
                ("neo-noir_tone_bible.json", "neo-noir_tone_bible"),
                ("story_bible.json",         "story_bible"),
                ("literary_rules.json",      "literary_rules"),
                ("character_profile.json",   "character_profile"),
            })
            {
                var path = Path.Combine(engineDataDir, file);
                if (!File.Exists(path)) continue;
                try
                {
                    var raw = File.ReadAllText(path);
                    if (!db.Settings.Any(s => s.Key == key))
                        db.Settings.Add(new Setting { Key = key, Json = raw, UpdatedAt = DateTime.UtcNow });
                }
                catch { /* malformed file — skip */ }
            }

            db.SaveChanges();
        }
    }
}
