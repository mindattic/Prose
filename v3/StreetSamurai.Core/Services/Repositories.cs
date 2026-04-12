using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Models.Canon;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Typed repositories — one per entity type. Each stores entities as individual
/// JSON files in a typed directory (e.g. engine/characters/kyle.json).
/// On first access, auto-migrates from legacy single-array files if present.
/// </summary>

public class CharacterRepository : JsonDirectoryRepository<CharacterData>
{
    public CharacterRepository(IPathProvider paths)
        : base(Path.Combine(paths.EngineDataDir, "people"), c => c.Name) { AutoMigrate(paths); }
    private void AutoMigrate(IPathProvider p) { var old = Path.Combine(p.EngineDataDir, "people.json"); if (File.Exists(old)) MigrateFromArrayFile(old); }
}

public class CorponationRepository : JsonDirectoryRepository<CorponationData>
{
    public CorponationRepository(IPathProvider paths)
        : base(Path.Combine(paths.EngineDataDir, "corponations"), c => c.Name) { AutoMigrate(paths); }
    private void AutoMigrate(IPathProvider p) { var old = Path.Combine(p.EngineDataDir, "corponations.json"); if (File.Exists(old)) MigrateFromArrayFile(old); }
}

public class DistrictRepository : JsonDirectoryRepository<DistrictData>
{
    public DistrictRepository(IPathProvider paths)
        : base(Path.Combine(paths.EngineDataDir, "places"), d => d.Name) { AutoMigrate(paths); }
    private void AutoMigrate(IPathProvider p)
    {
        var old = Path.Combine(p.EngineDataDir, "districts.json");
        if (File.Exists(old)) MigrateFromArrayFile(old);
    }
}

public class FactionRepository : JsonDirectoryRepository<FactionData>
{
    public FactionRepository(IPathProvider paths)
        : base(Path.Combine(paths.EngineDataDir, "factions"), f => f.Name) { AutoMigrate(paths); }
    private void AutoMigrate(IPathProvider p) { var old = Path.Combine(p.EngineDataDir, "factions.json"); if (File.Exists(old)) MigrateFromArrayFile(old); }
}

public class FacetRepository : JsonDirectoryRepository<FacetData>
{
    public FacetRepository(IPathProvider paths)
        : base(Path.Combine(paths.EngineDataDir, "facets"), f => f.Name) { AutoMigrate(paths); }
    private void AutoMigrate(IPathProvider p) { var old = Path.Combine(p.EngineDataDir, "facets.json"); if (File.Exists(old)) MigrateFromArrayFile(old); }
}

public class WorldbuildingDocRepository : JsonDirectoryRepository<WorldbuildingDocument>
{
    public WorldbuildingDocRepository(IPathProvider paths)
        : base(Path.Combine(paths.EngineDataDir, "documents"), d => d.FileName) { AutoMigrate(paths); }
    private void AutoMigrate(IPathProvider p) { var old = Path.Combine(p.EngineDataDir, "worldbuilding_docs.json"); if (File.Exists(old)) MigrateFromArrayFile(old); }
}

public class MotifRepository : JsonDirectoryRepository<MotifData>
{
    public MotifRepository(IPathProvider paths)
        : base(Path.Combine(paths.EngineDataDir, "motifs"), m => m.Name) { AutoMigrate(paths); }
    private void AutoMigrate(IPathProvider p) { var old = Path.Combine(p.EngineDataDir, "motifs.json"); if (File.Exists(old)) MigrateFromArrayFile(old); }
}

public class WeaponryRepository : JsonDirectoryRepository<WeaponryData>
{
    public WeaponryRepository(IPathProvider paths)
        : base(Path.Combine(paths.EngineDataDir, "weaponry"), w => w.Name) { AutoMigrate(paths); }
    private void AutoMigrate(IPathProvider p) { var old = Path.Combine(p.EngineDataDir, "weaponry.json"); if (File.Exists(old)) MigrateFromArrayFile(old); }
}

public class AmmunitionRepository : JsonDirectoryRepository<AmmunitionData>
{
    public AmmunitionRepository(IPathProvider paths)
        : base(Path.Combine(paths.EngineDataDir, "ammunition"), a => a.Name) { AutoMigrate(paths); }
    private void AutoMigrate(IPathProvider p) { var old = Path.Combine(p.EngineDataDir, "ammunition.json"); if (File.Exists(old)) MigrateFromArrayFile(old); }
}

public class EquipmentRepository : JsonDirectoryRepository<EquipmentData>
{
    public EquipmentRepository(IPathProvider paths)
        : base(Path.Combine(paths.EngineDataDir, "equipment"), e => e.ProductName.Length > 0 ? e.ProductName : e.Name) { AutoMigrate(paths); }
    private void AutoMigrate(IPathProvider p) { var old = Path.Combine(p.EngineDataDir, "equipment.json"); if (File.Exists(old)) MigrateFromArrayFile(old); }
}

public class TechnologyRepository : JsonDirectoryRepository<TechnologyData>
{
    public TechnologyRepository(IPathProvider paths)
        : base(Path.Combine(paths.EngineDataDir, "technology"), t => t.ProductName.Length > 0 ? t.ProductName : t.Name) { AutoMigrate(paths); }
    private void AutoMigrate(IPathProvider p) { var old = Path.Combine(p.EngineDataDir, "technology.json"); if (File.Exists(old)) MigrateFromArrayFile(old); }
}

public class CyberwareRepository : JsonDirectoryRepository<CyberwareData>
{
    public CyberwareRepository(IPathProvider paths)
        : base(Path.Combine(paths.EngineDataDir, "cyberware"), c => c.ProductName.Length > 0 ? c.ProductName : c.Name) { }
}

public class VocabularyRepository : JsonDirectoryRepository<VocabularyEntry>
{
    public VocabularyRepository(IPathProvider paths)
        : base(Path.Combine(paths.EngineDataDir, "vocabulary"), v => v.Term) { }
}

public class SyntheticLifeRepository : JsonDirectoryRepository<SyntheticLifeData>
{
    public SyntheticLifeRepository(IPathProvider paths)
        : base(Path.Combine(paths.EngineDataDir, "synthetics"), s => s.Name) { }
}

public class GenemodRepository : JsonDirectoryRepository<GenemodData>
{
    public GenemodRepository(IPathProvider paths)
        : base(Path.Combine(paths.EngineDataDir, "genemods"), g => g.ProductName.Length > 0 ? g.ProductName : g.Name) { }
}

public class TransportationRepository : JsonDirectoryRepository<TransportationData>
{
    public TransportationRepository(IPathProvider paths)
        : base(Path.Combine(paths.EngineDataDir, "transportation"), t => t.Name) { }
}

public class ContractRepository : JsonDirectoryRepository<ContractData>
{
    public ContractRepository(IPathProvider paths)
        : base(Path.Combine(paths.EngineDataDir, "contracts"), c => c.Codename) { }
}

public class AutomatonRepository : JsonDirectoryRepository<AutomatonData>
{
    public AutomatonRepository(IPathProvider paths)
        : base(Path.Combine(paths.EngineDataDir, "automata"), a => a.Name) { }
}

public class SubsidiaryRepository : JsonDirectoryRepository<SubsidiaryData>
{
    public SubsidiaryRepository(IPathProvider paths)
        : base(Path.Combine(paths.EngineDataDir, "subsidiaries"), s => s.Name) { }
}

public class EntertainmentRepository : JsonDirectoryRepository<EntertainmentData>
{
    public EntertainmentRepository(IPathProvider paths)
        : base(Path.Combine(paths.EngineDataDir, "entertainment"), e => e.Name) { }
}

public class ApparelRepository : JsonDirectoryRepository<ApparelData>
{
    public ApparelRepository(IPathProvider paths)
        : base(Path.Combine(paths.EngineDataDir, "apparel"), a => a.Name) { }
}

public class NewsRepository : JsonDirectoryRepository<NewsData>
{
    public NewsRepository(IPathProvider paths)
        : base(Path.Combine(paths.EngineDataDir, "news"), n => n.Headline) { }
}

public class ArchetypeRepository : JsonDirectoryRepository<ArchetypeData>
{
    public ArchetypeRepository(IPathProvider paths)
        : base(Path.Combine(paths.EngineDataDir, "archetypes"), a => a.Name) { }
}

public class MaterialRepository : JsonDirectoryRepository<MaterialData>
{
    public MaterialRepository(IPathProvider paths)
        : base(Path.Combine(paths.EngineDataDir, "materials"), s => s.ProductName.Length > 0 ? s.ProductName : s.Name) { }
}

public class PharmaceuticalRepository : JsonDirectoryRepository<PharmaceuticalData>
{
    public PharmaceuticalRepository(IPathProvider paths)
        : base(Path.Combine(paths.EngineDataDir, "pharmaceuticals"), p => p.Name) { }
}

public class ConsumerGoodRepository : JsonDirectoryRepository<ConsumerGoodData>
{
    public ConsumerGoodRepository(IPathProvider paths)
        : base(Path.Combine(paths.EngineDataDir, "consumer_goods"), g => g.ProductName.Length > 0 ? g.ProductName : g.Name) { }
}

public class QuoteRepository : JsonDirectoryRepository<QuoteData>
{
    public QuoteRepository(IPathProvider paths)
        : base(Path.Combine(paths.EngineDataDir, "quotes"), q => q.Quote.Length > 40 ? q.Quote[..40] : q.Quote) { }
}

// Singleton repositories stay file-based (they're single objects, not collections)
public class ToneBibleRepository : JsonSingletonRepository<ToneBibleData>
{
    public ToneBibleRepository(IPathProvider paths)
        : base(Path.Combine(paths.EngineDataDir, "neo-noir_tone_bible.json")) { }
}

public class StoryBibleRepository : JsonSingletonRepository<StoryBibleData>
{
    public StoryBibleRepository(IPathProvider paths)
        : base(Path.Combine(paths.EngineDataDir, "story_bible.json")) { }
}

public class LiteraryRulesRepository : JsonSingletonRepository<LiteraryRulesData>
{
    public LiteraryRulesRepository(IPathProvider paths)
        : base(Path.Combine(paths.EngineDataDir, "literary_rules.json")) { }
}

public class CharacterProfileRepository : JsonSingletonRepository<CharacterProfileData>
{
    public CharacterProfileRepository(IPathProvider paths)
        : base(Path.Combine(paths.EngineDataDir, "character_profile.json")) { }
}

public class LabSpecimenRepository : JsonDirectoryRepository<LabSpecimenData>
{
    public LabSpecimenRepository(IPathProvider paths)
        : base(Path.Combine(paths.EngineDataDir, "lab_specimens"), s => s.Name) { }
}

public class CeramicManRepository : JsonDirectoryRepository<CeramicManData>
{
    public CeramicManRepository(IPathProvider paths)
        : base(Path.Combine(paths.EngineDataDir, "ceramic_men"), c => c.Name) { }
}

public class WastelandEntityRepository : JsonDirectoryRepository<WastelandEntityData>
{
    public WastelandEntityRepository(IPathProvider paths)
        : base(Path.Combine(paths.EngineDataDir, "wasteland_entities"), w => w.Name) { }
}

public class PsionicRepository : JsonDirectoryRepository<PsionicData>
{
    public PsionicRepository(IPathProvider paths)
        : base(Path.Combine(paths.EngineDataDir, "psionics"), p => p.Name) { }
}
