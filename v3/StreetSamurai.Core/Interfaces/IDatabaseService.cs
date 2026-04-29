using StreetSamurai.Core.Models;
using StreetSamurai.Core.Models.Canon;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Core.Interfaces;

public interface IDatabaseService
{
    List<CharacterData> Characters { get; }
    List<FacetData> Facets { get; }
    List<DistrictData> Districts { get; }
    List<FactionData> Factions { get; }
    List<CorponationData> Corponations { get; }
    List<WeaponryData> Weaponry { get; }
    List<EquipmentData> Equipment { get; }
    List<TechnologyData> Technology { get; }
    List<WorldbuildingDocument> WorldbuildingDocs { get; }
    StoryBibleData StoryBible { get; }
    LiteraryRulesData LiteraryRules { get; }
    List<MotifData> Motifs { get; }
    CharacterProfileData CharacterProfile { get; }
    ToneBibleData ToneBible { get; }

    void Reload();
    CharacterData? FindCharacter(string nameOrAlias);
    string GetCharacterContext(string nameOrAlias);
    string GetDistrictContext(string nameOrAlias);
    string GetToneBiblePrompt();
    string GetSensoryPalettePrompt(string? location = null);
    string GetLiteraryRulesPrompt();
    List<SearchResult> Search(string query, int maxResults = 20);
}
