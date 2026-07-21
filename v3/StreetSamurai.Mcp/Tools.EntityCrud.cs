using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using StreetSamurai.Core.Models.Canon;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Mcp;

// ── Entity CRUD tools — create / upsert for every canon entity type ────────
// Mirrors the read surface in Tools.cs + Tools.Encyclopedia.cs but adds write
// capability. Each tool is idempotent on id: empty id → new v7 GUID; known id
// → upsert on the existing record. List parameters (tags, story_hooks, etc.)
// are comma-delimited strings. Complex nested objects (psychology,
// speech_patterns, physical_description for characters) accept optional JSON
// strings — omit or pass empty to keep defaults.

/// <summary>
/// Tool group for creating and updating the core canon entities: characters,
/// places, factions, and CorpoNations. These are the primary entities most
/// stories reference.
/// </summary>
[McpServerToolType]
public class CoreEntityCrudTools
{
    private readonly CharacterRepository characters;
    private readonly DistrictRepository places;
    private readonly FactionRepository factions;
    private readonly CorponationRepository corponations;

    public CoreEntityCrudTools(
        CharacterRepository characters,
        DistrictRepository places,
        FactionRepository factions,
        CorponationRepository corponations)
    {
        this.characters = characters;
        this.places = places;
        this.factions = factions;
        this.corponations = corponations;
    }

    /// <summary>Create or update a character record. Pass empty id to create new; pass an existing id to update (upsert).</summary>
    [McpServerTool, Description("Create or update a character in canon. Pass empty id to create new; pass an existing id to update. List fields (tags, story_hooks, aliases) are comma-delimited strings. Complex fields (psychology_json, speech_patterns_json, physical_description_json) accept optional JSON — omit to keep defaults.")]
    public string CreateCharacter(
        [Description("Character's full name. Required.")] string name,
        [Description("Role or function in the world (e.g. 'street samurai', 'fixer', 'cleanup contractor').")] string role = "",
        [Description("Prose description of who this character is.")] string description = "",
        [Description("Species: human, ai, android, robot, cyborg, synthetic, hybrid, unknown.")] string species = "human",
        [Description("Gender identity.")] string gender = "",
        [Description("Pronouns (e.g. 'he/him', 'she/her', 'they/them').")] string pronouns = "",
        [Description("Age in years.")] int age = 0,
        [Description("Status: alive, deceased, unknown, missing.")] string status = "alive",
        [Description("Current location or home territory.")] string location = "",
        [Description("Faction, corp, or freelancer network affiliation.")] string affiliation = "",
        [Description("Augmentation summary — cyberware, genemods, neural enhancements.")] string augmentations = "",
        [Description("Narrative function: what role this character plays in stories.")] string narrativeFunction = "",
        [Description("Comma-separated tags (e.g. 'freelancer,enforcer,Tier 3').")] string tags = "",
        [Description("Comma-separated story hooks — unresolved threads this character carries.")] string storyHooks = "",
        [Description("Optional JSON for the psychology block: {core_fears, core_desires, coping_mechanisms, blind_spots, secret}.")] string psychologyJson = "",
        [Description("Optional JSON for speech_patterns: {vocabulary, cadence, verbal_tics, example_lines, subtext}.")] string speechPatternsJson = "",
        [Description("Optional JSON for physical_description: {heritage, height_cm, weight_kg, build, hair_color, eye_color, distinguishing_marks}.")] string physicalDescriptionJson = "",
        [Description("Optional existing character id (32-char hex or full UUID) to update.")] string id = "")
    {
        var c = string.IsNullOrEmpty(id)
            ? new CharacterData()
            : (characters.GetById(id) ?? new CharacterData { Id = id });

        c.Name = name;
        if (!string.IsNullOrEmpty(role)) c.Role = role;
        if (!string.IsNullOrEmpty(description)) c.Description = description;
        if (!string.IsNullOrEmpty(species)) c.Species = species;
        if (!string.IsNullOrEmpty(gender)) c.Gender = gender;
        if (!string.IsNullOrEmpty(pronouns)) c.Pronouns = pronouns;
        if (age > 0) c.Age = age;
        if (!string.IsNullOrEmpty(status)) c.Status = status;
        if (!string.IsNullOrEmpty(location)) c.Location = location;
        if (!string.IsNullOrEmpty(affiliation)) c.Affiliation = affiliation;
        if (!string.IsNullOrEmpty(augmentations)) c.Augmentations = augmentations;
        if (!string.IsNullOrEmpty(narrativeFunction)) c.NarrativeFunction = narrativeFunction;
        if (!string.IsNullOrEmpty(tags))
            c.Tags = [.. tags.Split(',').Select(t => t.Trim()).Where(t => t.Length > 0)];
        if (!string.IsNullOrEmpty(storyHooks))
            c.StoryHooks = [.. storyHooks.Split(',').Select(h => h.Trim()).Where(h => h.Length > 0)];

        // Parse failures are surfaced as warnings, not silently swallowed — a swallowed error here
        // returned ok:true while the register/psychology never persisted (the SS-A46 voice no-op bug).
        var warnings = new List<string>();
        if (!string.IsNullOrWhiteSpace(psychologyJson))
        {
            try { c.Psychology = JsonSerializer.Deserialize<CharacterPsychology>(psychologyJson, CanonTools.JsonOpts) ?? c.Psychology; }
            catch (Exception ex) { warnings.Add($"psychologyJson ignored — parse error: {ex.Message}"); }
        }
        if (!string.IsNullOrWhiteSpace(speechPatternsJson))
        {
            try
            {
                var sp = JsonSerializer.Deserialize<SpeechPatterns>(speechPatternsJson, CanonTools.JsonOpts);
                if (sp is null ||
                    (string.IsNullOrWhiteSpace(sp.Vocabulary) && string.IsNullOrWhiteSpace(sp.Cadence) &&
                     string.IsNullOrWhiteSpace(sp.Subtext) && string.IsNullOrWhiteSpace(sp.UnderPressure) &&
                     sp.VerbalTics.Count == 0 && sp.ExampleLines.Count == 0))
                    warnings.Add("speechPatternsJson parsed but produced an EMPTY register — Speech* columns not populated. "
                               + "Expected keys: vocabulary, cadence, verbal_tics (array), example_lines (array), subtext, under_pressure, intimacy_register.");
                else
                    c.SpeechPatterns = sp;   // CharacterMapper.ToEntity flattens this into Speech* columns on Save
            }
            catch (Exception ex) { warnings.Add($"speechPatternsJson ignored — parse error: {ex.Message}"); }
        }
        if (!string.IsNullOrWhiteSpace(physicalDescriptionJson))
        {
            try { c.PhysicalDescription = JsonSerializer.Deserialize<PhysicalDescription>(physicalDescriptionJson, CanonTools.JsonOpts) ?? c.PhysicalDescription; }
            catch { /* keep existing */ }
        }

        characters.Save(c);
        return JsonSerializer.Serialize(
            new { ok = true, id = c.Id, name = c.Name, warnings = warnings.Count > 0 ? warnings : null },
            CanonTools.JsonOpts);
    }

    /// <summary>Create or update a place / district record. Pass empty id to create new; pass an existing id to update.</summary>
    [McpServerTool, Description("Create or update a place / district in canon. Pass empty id to create new; pass an existing id to update. List fields are comma-delimited strings.")]
    public string CreatePlace(
        [Description("Place name. Required.")] string name,
        [Description("Type of place (e.g. 'district', 'building', 'landmark', 'corridor', 'station').")] string type = "place",
        [Description("Prose description of the place.")] string description = "",
        [Description("Demographic makeup.")] string demographics = "",
        [Description("Economic profile.")] string economy = "",
        [Description("Who holds power here and how.")] string powerStructure = "",
        [Description("Comma-separated dangers present in this place.")] string dangers = "",
        [Description("Comma-separated story hooks.")] string storyHooks = "",
        [Description("Comma-separated tags.")] string tags = "",
        [Description("Optional existing place id to update.")] string id = "")
    {
        var p = string.IsNullOrEmpty(id)
            ? new DistrictData()
            : (places.GetById(id) ?? new DistrictData { Id = id });

        p.Name = name;
        if (!string.IsNullOrEmpty(type)) p.Type = type;
        if (!string.IsNullOrEmpty(description)) p.Description = description;
        if (!string.IsNullOrEmpty(demographics)) p.Demographics = demographics;
        if (!string.IsNullOrEmpty(economy)) p.Economy = economy;
        if (!string.IsNullOrEmpty(powerStructure)) p.PowerStructure = powerStructure;
        if (!string.IsNullOrEmpty(dangers))
            p.Dangers = [.. dangers.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0)];
        if (!string.IsNullOrEmpty(storyHooks))
            p.StoryHooks = [.. storyHooks.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0)];
        if (!string.IsNullOrEmpty(tags))
            p.Tags = [.. tags.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0)];

        places.Save(p);
        return JsonSerializer.Serialize(new { ok = true, id = p.Id, name = p.Name }, CanonTools.JsonOpts);
    }

    /// <summary>Create or update a faction record. Pass empty id to create new; pass an existing id to update.</summary>
    [McpServerTool, Description("Create or update a faction (street gang, syndicate, cell, advocacy group, etc.) in canon. Pass empty id to create new; pass an existing id to update. List fields are comma-delimited strings.")]
    public string CreateFaction(
        [Description("Faction name. Required.")] string name,
        [Description("Faction motto or slogan.")] string motto = "",
        [Description("Prose description.")] string description = "",
        [Description("Core ideology.")] string ideology = "",
        [Description("Territory the faction controls.")] string territory = "",
        [Description("Leadership structure and named leaders.")] string leadership = "",
        [Description("Narrative function — what role this faction plays in stories.")] string narrativeFunction = "",
        [Description("Comma-separated operational methods.")] string methods = "",
        [Description("Comma-separated goals.")] string goals = "",
        [Description("Comma-separated story hooks.")] string storyHooks = "",
        [Description("Comma-separated tags.")] string tags = "",
        [Description("Optional existing faction id to update.")] string id = "")
    {
        var f = string.IsNullOrEmpty(id)
            ? new FactionData()
            : (factions.GetById(id) ?? new FactionData { Id = id });

        f.Name = name;
        if (!string.IsNullOrEmpty(motto)) f.Motto = motto;
        if (!string.IsNullOrEmpty(description)) f.Description = description;
        if (!string.IsNullOrEmpty(ideology)) f.Ideology = ideology;
        if (!string.IsNullOrEmpty(territory)) f.Territory = territory;
        if (!string.IsNullOrEmpty(leadership)) f.Leadership = leadership;
        if (!string.IsNullOrEmpty(narrativeFunction)) f.NarrativeFunction = narrativeFunction;
        if (!string.IsNullOrEmpty(methods))
            f.Methods = [.. methods.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0)];
        if (!string.IsNullOrEmpty(goals))
            f.Goals = [.. goals.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0)];
        if (!string.IsNullOrEmpty(storyHooks))
            f.StoryHooks = [.. storyHooks.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0)];
        if (!string.IsNullOrEmpty(tags))
            f.Tags = [.. tags.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0)];

        factions.Save(f);
        return JsonSerializer.Serialize(new { ok = true, id = f.Id, name = f.Name }, CanonTools.JsonOpts);
    }

    /// <summary>Create or update a CorpoNation record. Pass empty id to create new; pass an existing id to update.</summary>
    [McpServerTool, Description("Create or update a CorpoNation (corporate sovereign entity) in canon. Pass empty id to create new; pass an existing id to update.")]
    public string CreateCorponation(
        [Description("CorpoNation name. Required.")] string name,
        [Description("Full legal corporate name.")] string fullLegalName = "",
        [Description("Industry sector.")] string sector = "",
        [Description("Territory the corp controls or dominates.")] string sovereignTerritory = "",
        [Description("Stock ticker or designation.")] string stockDesignation = "",
        [Description("Founding story or origin.")] string foundingStory = "",
        [Description("Security force name and description.")] string securityForce = "",
        [Description("Key distinguishing detail about this corp.")] string keyDetail = "",
        [Description("Full prose text describing the CorpoNation.")] string fullText = "",
        [Description("Comma-separated tags.")] string tags = "",
        [Description("Optional existing CorpoNation id to update.")] string id = "")
    {
        var corp = string.IsNullOrEmpty(id)
            ? new CorponationData()
            : (corponations.GetById(id) ?? new CorponationData { Id = id });

        corp.Name = name;
        if (!string.IsNullOrEmpty(fullLegalName)) corp.FullLegalName = fullLegalName;
        if (!string.IsNullOrEmpty(sector)) corp.Sector = sector;
        if (!string.IsNullOrEmpty(sovereignTerritory)) corp.SovereignTerritory = sovereignTerritory;
        if (!string.IsNullOrEmpty(stockDesignation)) corp.StockDesignation = stockDesignation;
        if (!string.IsNullOrEmpty(foundingStory)) corp.FoundingStory = foundingStory;
        if (!string.IsNullOrEmpty(securityForce)) corp.SecurityForce = securityForce;
        if (!string.IsNullOrEmpty(keyDetail)) corp.KeyDetail = keyDetail;
        if (!string.IsNullOrEmpty(fullText)) corp.FullText = fullText;
        if (!string.IsNullOrEmpty(tags))
            corp.Tags = [.. tags.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0)];

        corponations.Save(corp);
        return JsonSerializer.Serialize(new { ok = true, id = corp.Id, name = corp.Name }, CanonTools.JsonOpts);
    }
}

/// <summary>
/// Tool group for creating and updating gear-class canon entities: weapons,
/// cyberware, equipment, technology, apparel, pharmaceuticals, and ammunition.
/// </summary>
[McpServerToolType]
public class GearEntityCrudTools
{
    private readonly WeaponryRepository weapons;
    private readonly CyberwareRepository cyberware;
    private readonly EquipmentRepository equipment;
    private readonly TechnologyRepository technology;
    private readonly ApparelRepository apparel;
    private readonly PharmaceuticalRepository pharmaceuticals;
    private readonly AmmunitionRepository ammunition;

    public GearEntityCrudTools(
        WeaponryRepository weapons,
        CyberwareRepository cyberware,
        EquipmentRepository equipment,
        TechnologyRepository technology,
        ApparelRepository apparel,
        PharmaceuticalRepository pharmaceuticals,
        AmmunitionRepository ammunition)
    {
        this.weapons = weapons;
        this.cyberware = cyberware;
        this.equipment = equipment;
        this.technology = technology;
        this.apparel = apparel;
        this.pharmaceuticals = pharmaceuticals;
        this.ammunition = ammunition;
    }

    /// <summary>Create or update a weapon record. Pass empty id to create new; pass an existing id to update.</summary>
    [McpServerTool, Description("Create or update a weapon in canon. Pass empty id to create new; pass an existing id to update. List fields are comma-delimited strings.")]
    public string CreateWeapon(
        [Description("Weapon name. Required.")] string name,
        [Description("Category (e.g. 'melee', 'pistol', 'shotgun', 'rifle', 'explosive', 'launcher').")] string category = "",
        [Description("Prose description of the weapon.")] string description = "",
        [Description("Manufacturer name.")] string manufacturer = "",
        [Description("Tier availability (e.g. 'Tier 2+', 'black market', 'military only').")] string tierAvailability = "",
        [Description("Legal status.")] string legality = "",
        [Description("Technical specifications.")] string specifications = "",
        [Description("Tactical use and combat role.")] string tacticalUse = "",
        [Description("Cultural context in the GLMZ world.")] string culturalContext = "",
        [Description("Comma-separated ammunition types this weapon accepts.")] string ammunitionTypes = "",
        [Description("Comma-separated story hooks.")] string storyHooks = "",
        [Description("Comma-separated tags.")] string tags = "",
        [Description("Optional existing weapon id to update.")] string id = "")
    {
        var w = string.IsNullOrEmpty(id)
            ? new WeaponryData()
            : (weapons.GetById(id) ?? new WeaponryData { Id = id });

        w.Name = name;
        if (!string.IsNullOrEmpty(category)) w.Category = category;
        if (!string.IsNullOrEmpty(description)) w.Description = description;
        if (!string.IsNullOrEmpty(manufacturer)) w.Manufacturer = manufacturer;
        if (!string.IsNullOrEmpty(tierAvailability)) w.TierAvailability = tierAvailability;
        if (!string.IsNullOrEmpty(legality)) w.Legality = legality;
        if (!string.IsNullOrEmpty(specifications)) w.Specifications = specifications;
        if (!string.IsNullOrEmpty(tacticalUse)) w.TacticalUse = tacticalUse;
        if (!string.IsNullOrEmpty(culturalContext)) w.CulturalContext = culturalContext;
        if (!string.IsNullOrEmpty(ammunitionTypes))
            w.AmmunitionType = [.. ammunitionTypes.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0)];
        if (!string.IsNullOrEmpty(storyHooks))
            w.StoryHooks = [.. storyHooks.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0)];
        if (!string.IsNullOrEmpty(tags))
            w.Tags = [.. tags.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0)];

        weapons.Save(w);
        return JsonSerializer.Serialize(new { ok = true, id = w.Id, name = w.Name }, CanonTools.JsonOpts);
    }

    /// <summary>Create or update a cyberware record. Pass empty id to create new; pass an existing id to update.</summary>
    [McpServerTool, Description("Create or update a cyberware implant in canon. Pass empty id to create new; pass an existing id to update. List fields are comma-delimited strings.")]
    public string CreateCyberware(
        [Description("Cyberware name. Required.")] string name,
        [Description("Brand name.")] string brandName = "",
        [Description("Product model name.")] string productName = "",
        [Description("Category (e.g. 'neural', 'limb', 'sensory', 'combat', 'subdermal').")] string category = "",
        [Description("Body location of installation.")] string bodyLocation = "",
        [Description("Prose description.")] string description = "",
        [Description("Manufacturer name.")] string manufacturer = "",
        [Description("Tier availability.")] string tierAvailability = "",
        [Description("Legal status.")] string legality = "",
        [Description("Installation requirements.")] string installationRequirements = "",
        [Description("Technical specifications.")] string specifications = "",
        [Description("Street price (unregulated market).")] string streetPrice = "",
        [Description("Cultural context in the GLMZ world.")] string culturalContext = "",
        [Description("Comma-separated side effects.")] string sideEffects = "",
        [Description("Comma-separated story hooks.")] string storyHooks = "",
        [Description("Comma-separated tags.")] string tags = "",
        [Description("Optional existing cyberware id to update.")] string id = "")
    {
        var cw = string.IsNullOrEmpty(id)
            ? new CyberwareData()
            : (cyberware.GetById(id) ?? new CyberwareData { Id = id });

        cw.Name = name;
        if (!string.IsNullOrEmpty(brandName)) cw.BrandName = brandName;
        if (!string.IsNullOrEmpty(productName)) cw.ProductName = productName;
        if (!string.IsNullOrEmpty(category)) cw.Category = category;
        if (!string.IsNullOrEmpty(bodyLocation)) cw.BodyLocation = bodyLocation;
        if (!string.IsNullOrEmpty(description)) cw.Description = description;
        if (!string.IsNullOrEmpty(manufacturer)) cw.Manufacturer = manufacturer;
        if (!string.IsNullOrEmpty(tierAvailability)) cw.TierAvailability = tierAvailability;
        if (!string.IsNullOrEmpty(legality)) cw.Legality = legality;
        if (!string.IsNullOrEmpty(installationRequirements)) cw.InstallationRequirements = installationRequirements;
        if (!string.IsNullOrEmpty(specifications)) cw.Specifications = specifications;
        if (!string.IsNullOrEmpty(streetPrice)) cw.StreetPrice = streetPrice;
        if (!string.IsNullOrEmpty(culturalContext)) cw.CulturalContext = culturalContext;
        if (!string.IsNullOrEmpty(sideEffects))
            cw.SideEffects = [.. sideEffects.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0)];
        if (!string.IsNullOrEmpty(storyHooks))
            cw.StoryHooks = [.. storyHooks.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0)];
        if (!string.IsNullOrEmpty(tags))
            cw.Tags = [.. tags.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0)];

        cyberware.Save(cw);
        return JsonSerializer.Serialize(new { ok = true, id = cw.Id, name = cw.Name }, CanonTools.JsonOpts);
    }

    /// <summary>Create or update an equipment record. Pass empty id to create new; pass an existing id to update.</summary>
    [McpServerTool, Description("Create or update a piece of equipment (gear, tools, devices, accessories) in canon. Pass empty id to create new; pass an existing id to update.")]
    public string CreateEquipment(
        [Description("Equipment name. Required.")] string name,
        [Description("Brand name.")] string brandName = "",
        [Description("Product model name.")] string productName = "",
        [Description("Category (e.g. 'surveillance', 'medical', 'demolitions', 'comms').")] string category = "",
        [Description("Prose description.")] string description = "",
        [Description("Manufacturer name.")] string manufacturer = "",
        [Description("Tier availability.")] string tierAvailability = "",
        [Description("Comma-separated tags.")] string tags = "",
        [Description("Optional existing equipment id to update.")] string id = "")
    {
        var eq = string.IsNullOrEmpty(id)
            ? new EquipmentData()
            : (equipment.GetById(id) ?? new EquipmentData { Id = id });

        eq.Name = name;
        if (!string.IsNullOrEmpty(brandName)) eq.BrandName = brandName;
        if (!string.IsNullOrEmpty(productName)) eq.ProductName = productName;
        if (!string.IsNullOrEmpty(category)) eq.Category = category;
        if (!string.IsNullOrEmpty(description)) eq.Description = description;
        if (!string.IsNullOrEmpty(manufacturer)) eq.Manufacturer = manufacturer;
        if (!string.IsNullOrEmpty(tierAvailability)) eq.TierAvailability = tierAvailability;
        if (!string.IsNullOrEmpty(tags))
            eq.Tags = [.. tags.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0)];

        equipment.Save(eq);
        return JsonSerializer.Serialize(new { ok = true, id = eq.Id, name = eq.Name }, CanonTools.JsonOpts);
    }

    /// <summary>Create or update a technology record. Pass empty id to create new; pass an existing id to update.</summary>
    [McpServerTool, Description("Create or update a technology entry (software, protocols, networks, systems) in canon. Pass empty id to create new; pass an existing id to update.")]
    public string CreateTechnology(
        [Description("Technology name. Required.")] string name,
        [Description("Brand name.")] string brandName = "",
        [Description("Product model name.")] string productName = "",
        [Description("Subcategory (e.g. 'neural interface', 'network protocol', 'AI system').")] string subcategory = "",
        [Description("Prose description.")] string description = "",
        [Description("Comma-separated developer names (corporations, labs, individuals).")] string developers = "",
        [Description("Tier availability.")] string tierAvailability = "",
        [Description("Comma-separated story hooks.")] string storyHooks = "",
        [Description("Comma-separated tags.")] string tags = "",
        [Description("Optional existing technology id to update.")] string id = "")
    {
        var tech = string.IsNullOrEmpty(id)
            ? new TechnologyData()
            : (technology.GetById(id) ?? new TechnologyData { Id = id });

        tech.Name = name;
        if (!string.IsNullOrEmpty(brandName)) tech.BrandName = brandName;
        if (!string.IsNullOrEmpty(productName)) tech.ProductName = productName;
        if (!string.IsNullOrEmpty(subcategory)) tech.Subcategory = subcategory;
        if (!string.IsNullOrEmpty(description)) tech.Description = description;
        if (!string.IsNullOrEmpty(developers))
            tech.Developers = [.. developers.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0)];
        if (!string.IsNullOrEmpty(tierAvailability)) tech.TierAvailability = tierAvailability;
        if (!string.IsNullOrEmpty(storyHooks))
            tech.StoryHooks = [.. storyHooks.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0)];
        if (!string.IsNullOrEmpty(tags))
            tech.Tags = [.. tags.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0)];

        technology.Save(tech);
        return JsonSerializer.Serialize(new { ok = true, id = tech.Id, name = tech.Name }, CanonTools.JsonOpts);
    }

    /// <summary>Create or update an apparel record. Pass empty id to create new; pass an existing id to update.</summary>
    [McpServerTool, Description("Create or update an apparel item (clothing, armor, accessories) in canon. Pass empty id to create new; pass an existing id to update.")]
    public string CreateApparel(
        [Description("Apparel name. Required.")] string name,
        [Description("Category (e.g. 'outerwear', 'armor', 'footwear', 'accessories').")] string category = "",
        [Description("Prose description.")] string description = "",
        [Description("Manufacturer name.")] string manufacturer = "",
        [Description("Comma-separated tags.")] string tags = "",
        [Description("Optional existing apparel id to update.")] string id = "")
    {
        var ap = string.IsNullOrEmpty(id)
            ? new ApparelData()
            : (apparel.GetById(id) ?? new ApparelData { Id = id });

        ap.Name = name;
        if (!string.IsNullOrEmpty(category)) ap.Category = category;
        if (!string.IsNullOrEmpty(description)) ap.Description = description;
        if (!string.IsNullOrEmpty(manufacturer)) ap.Manufacturer = manufacturer;
        if (!string.IsNullOrEmpty(tags))
            ap.Tags = [.. tags.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0)];

        apparel.Save(ap);
        return JsonSerializer.Serialize(new { ok = true, id = ap.Id, name = ap.Name }, CanonTools.JsonOpts);
    }

    /// <summary>Create or update a pharmaceutical record. Pass empty id to create new; pass an existing id to update.</summary>
    [McpServerTool, Description("Create or update a pharmaceutical (drug, stim, pain modulator, neuro-pharma) in canon. Pass empty id to create new; pass an existing id to update.")]
    public string CreatePharmaceutical(
        [Description("Pharmaceutical name. Required.")] string name,
        [Description("Category (e.g. 'stimulant', 'analgesic', 'neuro-modulator', 'combat stim').")] string category = "",
        [Description("Prose description and effects.")] string description = "",
        [Description("Manufacturer name.")] string manufacturer = "",
        [Description("Comma-separated tags.")] string tags = "",
        [Description("Optional existing pharmaceutical id to update.")] string id = "")
    {
        var ph = string.IsNullOrEmpty(id)
            ? new PharmaceuticalData()
            : (pharmaceuticals.GetById(id) ?? new PharmaceuticalData { Id = id });

        ph.Name = name;
        if (!string.IsNullOrEmpty(category)) ph.Category = category;
        if (!string.IsNullOrEmpty(description)) ph.Description = description;
        if (!string.IsNullOrEmpty(manufacturer)) ph.Manufacturer = manufacturer;
        if (!string.IsNullOrEmpty(tags))
            ph.Tags = [.. tags.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0)];

        pharmaceuticals.Save(ph);
        return JsonSerializer.Serialize(new { ok = true, id = ph.Id, name = ph.Name }, CanonTools.JsonOpts);
    }

    /// <summary>Create or update an ammunition record. Pass empty id to create new; pass an existing id to update.</summary>
    [McpServerTool, Description("Create or update an ammunition type (calibers, specialty rounds, energy cells) in canon. Pass empty id to create new; pass an existing id to update.")]
    public string CreateAmmunition(
        [Description("Ammunition name. Required.")] string name,
        [Description("Category (e.g. 'pistol', 'rifle', 'shotgun', 'energy', 'specialty').")] string category = "",
        [Description("Prose description.")] string description = "",
        [Description("Manufacturer name.")] string manufacturer = "",
        [Description("Comma-separated tags.")] string tags = "",
        [Description("Optional existing ammunition id to update.")] string id = "")
    {
        var am = string.IsNullOrEmpty(id)
            ? new AmmunitionData()
            : (ammunition.GetById(id) ?? new AmmunitionData { Id = id });

        am.Name = name;
        if (!string.IsNullOrEmpty(category)) am.Category = category;
        if (!string.IsNullOrEmpty(description)) am.Description = description;
        if (!string.IsNullOrEmpty(manufacturer)) am.Manufacturer = manufacturer;
        if (!string.IsNullOrEmpty(tags))
            am.Tags = [.. tags.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0)];

        ammunition.Save(am);
        return JsonSerializer.Serialize(new { ok = true, id = am.Id, name = am.Name }, CanonTools.JsonOpts);
    }
}

/// <summary>
/// Tool group for creating and updating world-category canon entities: automata,
/// transportation, consumer goods, and worldbuilding documents.
/// </summary>
[McpServerToolType]
public class WorldEntityCrudTools
{
    private readonly AutomatonRepository automata;
    private readonly TransportationRepository transportation;
    private readonly ConsumerGoodRepository consumerGoods;
    private readonly WorldbuildingDocRepository documents;
    private readonly SubsidiaryRepository subsidiaries;

    public WorldEntityCrudTools(
        AutomatonRepository automata,
        TransportationRepository transportation,
        ConsumerGoodRepository consumerGoods,
        WorldbuildingDocRepository documents,
        SubsidiaryRepository subsidiaries)
    {
        this.automata = automata;
        this.transportation = transportation;
        this.consumerGoods = consumerGoods;
        this.documents = documents;
        this.subsidiaries = subsidiaries;
    }

    /// <summary>Create or update an automaton record. Pass empty id to create new; pass an existing id to update.</summary>
    [McpServerTool, Description("Create or update an automaton (drone, security bot, Iowan Behemoth, agricultural machine) in canon. Automata are machines, NOT synthetic life. Pass empty id to create new; pass an existing id to update.")]
    public string CreateAutomaton(
        [Description("Automaton name. Required.")] string name,
        [Description("Prose description.")] string description = "",
        [Description("Manufacturer name.")] string manufacturer = "",
        [Description("Comma-separated tags.")] string tags = "",
        [Description("Optional existing automaton id to update.")] string id = "")
    {
        var a = string.IsNullOrEmpty(id)
            ? new AutomatonData()
            : (automata.GetById(id) ?? new AutomatonData { Id = id });

        a.Name = name;
        if (!string.IsNullOrEmpty(description)) a.Description = description;
        if (!string.IsNullOrEmpty(manufacturer)) a.Manufacturer = manufacturer;
        if (!string.IsNullOrEmpty(tags))
            a.Tags = [.. tags.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0)];

        automata.Save(a);
        return JsonSerializer.Serialize(new { ok = true, id = a.Id, name = a.Name }, CanonTools.JsonOpts);
    }

    /// <summary>Create or update a transportation record. Pass empty id to create new; pass an existing id to update.</summary>
    [McpServerTool, Description("Create or update a transportation entry (vehicle, transit line, Pulse station, individual transport) in canon. Pass empty id to create new; pass an existing id to update.")]
    public string CreateTransportation(
        [Description("Transportation name. Required.")] string name,
        [Description("Category (e.g. 'motorcycle', 'rail', 'air', 'Pulse', 'water').")] string category = "",
        [Description("Prose description.")] string description = "",
        [Description("Manufacturer name.")] string manufacturer = "",
        [Description("Comma-separated tags.")] string tags = "",
        [Description("Optional existing transportation id to update.")] string id = "")
    {
        var t = string.IsNullOrEmpty(id)
            ? new TransportationData()
            : (transportation.GetById(id) ?? new TransportationData { Id = id });

        t.Name = name;
        if (!string.IsNullOrEmpty(category)) t.Category = category;
        if (!string.IsNullOrEmpty(description)) t.Description = description;
        if (!string.IsNullOrEmpty(manufacturer)) t.Manufacturer = manufacturer;
        if (!string.IsNullOrEmpty(tags))
            t.Tags = [.. tags.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0)];

        transportation.Save(t);
        return JsonSerializer.Serialize(new { ok = true, id = t.Id, name = t.Name }, CanonTools.JsonOpts);
    }

    /// <summary>Create or update a consumer good record. Pass empty id to create new; pass an existing id to update.</summary>
    [McpServerTool, Description("Create or update a consumer good (food, drinks, household items, branded products) in canon. Pass empty id to create new; pass an existing id to update.")]
    public string CreateConsumerGood(
        [Description("Consumer good name. Required.")] string name,
        [Description("Product name if different from name.")] string productName = "",
        [Description("Category (e.g. 'food', 'beverage', 'household', 'luxury').")] string category = "",
        [Description("Prose description.")] string description = "",
        [Description("Manufacturer or brand.")] string manufacturer = "",
        [Description("Comma-separated tags.")] string tags = "",
        [Description("Optional existing consumer good id to update.")] string id = "")
    {
        var g = string.IsNullOrEmpty(id)
            ? new ConsumerGoodData()
            : (consumerGoods.GetById(id) ?? new ConsumerGoodData { Id = id });

        g.Name = name;
        if (!string.IsNullOrEmpty(productName)) g.ProductName = productName;
        if (!string.IsNullOrEmpty(category)) g.Category = category;
        if (!string.IsNullOrEmpty(description)) g.Description = description;
        if (!string.IsNullOrEmpty(manufacturer)) g.Manufacturer = manufacturer;
        if (!string.IsNullOrEmpty(tags))
            g.Tags = [.. tags.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0)];

        consumerGoods.Save(g);
        return JsonSerializer.Serialize(new { ok = true, id = g.Id, name = g.Name }, CanonTools.JsonOpts);
    }

    /// <summary>Create or update a worldbuilding document. Pass empty id to create new; pass an existing id to update.</summary>
    [McpServerTool, Description("Create or update a worldbuilding document in canon. Documents hold long-form canon text (lore articles, guides, in-world publications). Pass empty id to create new; pass an existing id to update.")]
    public string CreateDocument(
        [Description("Document file name (slug, e.g. 'network_operators_guide'). Required.")] string fileName,
        [Description("Document title. Required.")] string title,
        [Description("Category (e.g. 'lore', 'technical', 'in-world-publication', 'history').")] string category = "",
        [Description("Full prose body of the document.")] string body = "",
        [Description("Comma-separated tags.")] string tags = "",
        [Description("Optional existing document id to update.")] string id = "")
    {
        var d = string.IsNullOrEmpty(id)
            ? new WorldbuildingDocument()
            : (documents.GetById(id) ?? new WorldbuildingDocument { Id = id });

        d.FileName = fileName;
        d.Title = title;
        if (!string.IsNullOrEmpty(category)) d.Category = category;
        if (!string.IsNullOrEmpty(body)) d.Body = body;
        if (!string.IsNullOrEmpty(tags))
            d.Tags = [.. tags.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0)];

        documents.Save(d);
        return JsonSerializer.Serialize(new { ok = true, id = d.Id, file_name = d.FileName, title = d.Title }, CanonTools.JsonOpts);
    }

    /// <summary>Create or update a subsidiary record. Pass empty id to create new; pass an existing id to update.</summary>
    [McpServerTool, Description("Create or update a subsidiary (child/holding company of a larger CorpoNation) in canon. Pass empty id to create new; pass an existing id to update.")]
    public string CreateSubsidiary(
        [Description("Subsidiary name. Required.")] string name,
        [Description("Parent CorpoNation name.")] string parentCorponation = "",
        [Description("Prose description.")] string description = "",
        [Description("Comma-separated tags.")] string tags = "",
        [Description("Optional existing subsidiary id to update.")] string id = "")
    {
        var s = string.IsNullOrEmpty(id)
            ? new SubsidiaryData()
            : (subsidiaries.GetById(id) ?? new SubsidiaryData { Id = id });

        s.Name = name;
        if (!string.IsNullOrEmpty(parentCorponation)) s.ParentCorponation = parentCorponation;
        if (!string.IsNullOrEmpty(description)) s.Description = description;
        if (!string.IsNullOrEmpty(tags))
            s.Tags = [.. tags.Split(',').Select(s2 => s2.Trim()).Where(s2 => s2.Length > 0)];

        subsidiaries.Save(s);
        return JsonSerializer.Serialize(new { ok = true, id = s.Id, name = s.Name }, CanonTools.JsonOpts);
    }
}
