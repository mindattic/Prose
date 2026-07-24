using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Services;

namespace StreetSamurai.UnitTests;

/// <summary>
/// Live validation suite against the real engine/data directory.
/// Catches world rule violations, Xref conflicts, duplicate IDs, self-aliases,
/// malformed JSON, and stale forbidden terms before they become canon.
///
/// Run after any entry creation or edit:
///   dotnet test --filter Category=WorldValidation
/// </summary>
[TestFixture]
[Category("WorldValidation")]
[Ignore("Retired: file-based repositories migrated to SQL (2026-05-08). Rewrite to query the DB directly.")]
public class WorldValidationTests
{
    private static readonly string EngineDataDir = FindEngineDataDir();

    private ServiceProvider provider = null!;
    private XrefService xref = null!;

    private static readonly string[] ExcludedDirs =
        ["archives", "logs", "graph", "chapters", "exports", "media", "chromadb", "profiles"];

    [OneTimeSetUp]
    public void Setup()
    {
        Assert.That(Directory.Exists(EngineDataDir), Is.True,
            $"engine/data not found — searched from {AppDomain.CurrentDomain.BaseDirectory}");

        var paths = new ValidationPathProvider(EngineDataDir);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IPathProvider>(paths);
        services.AddSingleton<SettingsService>();

        // All repositories consumed by XrefService
        services.AddSingleton<CharacterRepository>();
        services.AddSingleton<DistrictRepository>();
        services.AddSingleton<FactionRepository>();
        services.AddSingleton<CorponationRepository>();
        services.AddSingleton<TechnologyRepository>();
        services.AddSingleton<VocabularyRepository>();
        services.AddSingleton<WeaponryRepository>();
        services.AddSingleton<AmmunitionRepository>();
        services.AddSingleton<EquipmentRepository>();
        services.AddSingleton<CyberwareRepository>();
        services.AddSingleton<GenemodRepository>();
        services.AddSingleton<TransportationRepository>();
        services.AddSingleton<AutomatonRepository>();
        services.AddSingleton<SubsidiaryRepository>();
        services.AddSingleton<EntertainmentRepository>();
        services.AddSingleton<ApparelRepository>();
        services.AddSingleton<MaterialRepository>();
        services.AddSingleton<PharmaceuticalRepository>();
        services.AddSingleton<ConsumerGoodRepository>();
        services.AddSingleton<ContractRepository>();
        services.AddSingleton<LabSpecimenRepository>();
        services.AddSingleton<PsionicRepository>();
        services.AddSingleton<XrefService>();

        provider = services.BuildServiceProvider();
        xref = provider.GetRequiredService<XrefService>();
        xref.EnsureBuilt();
    }

    [OneTimeTearDown]
    public void Teardown() => provider?.Dispose();

    // ── 1. Xref — no two entities of the same type share a name ─────────

    [Test]
    public void NoSameTypeXrefConflicts()
    {
        var conflicts = xref.GetConflicts()
            .Where(c => c.Winner.Type == c.Challenger.Type)
            .OrderBy(c => c.Name)
            .ToList();

        Assert.That(conflicts, Is.Empty, () =>
            "Same-type Xref conflicts (two entities of the same type share a name):\n" +
            string.Join("\n", conflicts.Select(c =>
                $"  \"{c.Name}\" ({c.Winner.Type}): {c.Winner.Id} vs {c.Challenger.Id}")));
    }

    // ── 2. No entity lists its own name as an alias ──────────────────────

    [Test]
    public void NoSelfAliases()
    {
        var violations = new List<string>();
        foreach (var file in AllEntityFiles())
        {
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(file));
                var root = doc.RootElement;
                if (root.ValueKind != JsonValueKind.Object) continue;
                if (!root.TryGetProperty("name", out var nameProp)) continue;
                var name = nameProp.GetString();
                if (string.IsNullOrEmpty(name)) continue;

                if (root.TryGetProperty("aliases", out var aliases) &&
                    aliases.ValueKind == JsonValueKind.Array)
                {
                    foreach (var alias in aliases.EnumerateArray())
                    {
                        if ((alias.GetString() ?? "").Equals(name, StringComparison.OrdinalIgnoreCase))
                            violations.Add($"  {Rel(file)}: '{name}' listed in its own aliases");
                    }
                }
            }
            catch (JsonException) { }
        }

        Assert.That(violations, Is.Empty, () =>
            "Self-alias violations:\n" + string.Join("\n", violations));
    }

    // ── 3. All entity IDs are unique across every repository ─────────────

    [Test]
    public void AllEntityIdsAreUnique()
    {
        var seen = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var dupes = new List<string>();

        foreach (var file in AllEntityFiles())
        {
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(file));
                if (doc.RootElement.ValueKind != JsonValueKind.Object) continue;
                if (!doc.RootElement.TryGetProperty("id", out var idProp)) continue;
                var id = idProp.GetString();
                if (string.IsNullOrEmpty(id)) continue;

                if (seen.TryGetValue(id, out var existing))
                    dupes.Add($"  {id}: {Rel(existing)} AND {Rel(file)}");
                else
                    seen[id] = file;
            }
            catch (JsonException) { }
        }

        Assert.That(dupes, Is.Empty, () =>
            "Duplicate entity IDs:\n" + string.Join("\n", dupes));
    }

    // ── 4. World rule violations (rule-scan only, no LLM) ────────────────

    [Test]
    public async Task NoWorldRuleViolations()
    {
        var svc = new WorldConsistencyService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            StreetSamurai.Core.Data.TestDbFactory.For(
                provider.GetRequiredService<IPathProvider>(),
                "validation"),
            NullLogger<WorldConsistencyService>.Instance);

        svc.RunRuleScan = true;
        svc.RunConflictCheck = false;   // requires LLM — skip
        svc.RunDedup = false;           // slow — skip

        await svc.RunAsync();

        Assert.That(svc.RuleViolations, Is.Empty, () =>
            "World rule violations:\n" +
            string.Join("\n", svc.RuleViolations.Select(v =>
                $"  {Rel(v.FilePath)} [{v.EntityName}]: {v.Rule} — matched \"{v.MatchedText}\"")));
    }

    // ── 5. No stale forbidden terms ──────────────────────────────────────

    [Test]
    public void NoStaleForbiddenTerms()
    {
        // Terms that have been corrected and must not reappear
        var forbidden = new (string term, string fix)[]
        {
            ("Emergent Life Form", "ELF = Electronic Life Form"),
            ("emergent life form", "ELF = Electronic Life Form"),
        };

        var violations = new List<string>();
        foreach (var file in AllEntityFiles())
        {
            string content;
            try { content = File.ReadAllText(file); }
            catch { continue; }

            foreach (var (term, fix) in forbidden)
            {
                if (content.Contains(term, StringComparison.Ordinal))
                    violations.Add($"  {Rel(file)}: \"{term}\" → {fix}");
            }
        }

        Assert.That(violations, Is.Empty, () =>
            "Stale forbidden terms:\n" + string.Join("\n", violations));
    }

    // ── 6. All entity files are valid JSON ───────────────────────────────

    [Test]
    public void AllEntityFilesAreValidJson()
    {
        var violations = new List<string>();
        foreach (var file in AllEntityFiles())
        {
            try { using var _ = JsonDocument.Parse(File.ReadAllText(file)); }
            catch (JsonException ex)
            {
                violations.Add($"  {Rel(file)}: {ex.Message}");
            }
        }

        Assert.That(violations, Is.Empty, () =>
            "Malformed JSON:\n" + string.Join("\n", violations));
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private IEnumerable<string> AllEntityFiles()
    {
        foreach (var dir in Directory.GetDirectories(EngineDataDir))
        {
            if (ExcludedDirs.Contains(Path.GetFileName(dir), StringComparer.OrdinalIgnoreCase))
                continue;
            foreach (var file in Directory.GetFiles(dir, "*.json"))
                yield return file;
        }
        foreach (var file in Directory.GetFiles(EngineDataDir, "*.json"))
            yield return file;
    }

    private string Rel(string fullPath) =>
        Path.GetRelativePath(EngineDataDir, fullPath);

    private static string FindEngineDataDir()
    {
        var dir = AppDomain.CurrentDomain.BaseDirectory;
        while (dir != null)
        {
            var candidate = Path.Combine(dir, "engine", "data");
            if (Directory.Exists(candidate)) return candidate;
            dir = Directory.GetParent(dir)?.FullName;
        }
        // Fallback: 5 levels up from test bin
        return Path.GetFullPath(Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "engine", "data"));
    }

    // Minimal IPathProvider pointing at the real engine/data directory
    private sealed class ValidationPathProvider : IPathProvider
    {
        private readonly string dataDir;
        public ValidationPathProvider(string dataDir) => this.dataDir = dataDir;
        public string EngineDataDir     => dataDir;
        public string MutableDataDir    => dataDir;
        public string DataRoot          => Path.GetDirectoryName(dataDir) ?? dataDir;
        public string ChaptersDir       => Path.Combine(dataDir, "chapters");
        public string BooksDir           => Path.Combine(dataDir, "books");
        public string SeriesDir          => Path.Combine(dataDir, "series");
        public string GraphDir          => Path.Combine(dataDir, "graph");
        public string LogDir            => Path.Combine(dataDir, "logs");
        public string ExportDir         => Path.GetFullPath(Path.Combine(dataDir, "..", "exports"));
        public string ArchiveDir        => Path.Combine(dataDir, "archives");
        public string WorldbuildingDir  => dataDir;
        public string CharactersDir     => Path.Combine(dataDir, "people");
        public string EssencesDir       => Path.Combine(dataDir, "facets");
        public string NarrativeBiblePath => Path.Combine(dataDir, "story_bible.json");
        public string WorldDir          => dataDir;
        public string MediaDir          => Path.Combine(dataDir, "media");
        public string MediaArchiveDir   => Path.Combine(dataDir, "archives", "media");
    }
}
