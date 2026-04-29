using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Mcp;

// ── Encyclopedia tools — list/get for every remaining canon repo ───────────
// Same pattern as CanonTools (characters, places, factions, corponations) but
// for the production canon: weapons, equipment, automata, synthetics, etc.
// All read-only. The list_X variants return a slim projection (name + a couple
// of identity fields) so a tools/list call doesn't dump 50KB; get_X returns
// the full record. Equipment / Cyberware / Technology key on ProductName when
// non-empty (falling back to Name) — the matching is case-insensitive on
// whichever the repo's name selector returned.

[McpServerToolType]
public class EncyclopediaTools
{
    private readonly WeaponryRepository weapons;
    private readonly AmmunitionRepository ammunition;
    private readonly EquipmentRepository equipment;
    private readonly TechnologyRepository technology;
    private readonly CyberwareRepository cyberware;
    private readonly ApparelRepository apparel;
    private readonly PharmaceuticalRepository pharmaceuticals;
    private readonly AutomatonRepository automata;
    private readonly SyntheticLifeRepository synthetics;
    private readonly ArchetypeRepository archetypes;
    private readonly MaterialRepository materials;
    private readonly TransportationRepository transportation;
    private readonly ConsumerGoodRepository consumerGoods;
    private readonly QuoteRepository quotes;
    private readonly WorldbuildingDocRepository documents;
    private readonly GenemodRepository genemods;
    private readonly LabSpecimenRepository labSpecimens;
    private readonly PsionicRepository psionics;
    private readonly SubsidiaryRepository subsidiaries;

    public EncyclopediaTools(
        WeaponryRepository weapons, AmmunitionRepository ammunition,
        EquipmentRepository equipment, TechnologyRepository technology,
        CyberwareRepository cyberware, ApparelRepository apparel,
        PharmaceuticalRepository pharmaceuticals,
        AutomatonRepository automata, SyntheticLifeRepository synthetics,
        ArchetypeRepository archetypes, MaterialRepository materials,
        TransportationRepository transportation,
        ConsumerGoodRepository consumerGoods,
        QuoteRepository quotes, WorldbuildingDocRepository documents,
        GenemodRepository genemods, LabSpecimenRepository labSpecimens,
        PsionicRepository psionics, SubsidiaryRepository subsidiaries)
    {
        this.weapons = weapons;
        this.ammunition = ammunition;
        this.equipment = equipment;
        this.technology = technology;
        this.cyberware = cyberware;
        this.apparel = apparel;
        this.pharmaceuticals = pharmaceuticals;
        this.automata = automata;
        this.synthetics = synthetics;
        this.archetypes = archetypes;
        this.materials = materials;
        this.transportation = transportation;
        this.consumerGoods = consumerGoods;
        this.quotes = quotes;
        this.documents = documents;
        this.genemods = genemods;
        this.labSpecimens = labSpecimens;
        this.psionics = psionics;
        this.subsidiaries = subsidiaries;
    }

    [McpServerTool, Description("List every weapon in canon. Returns name + category + manufacturer. Use this to find a weapon for an action scene.")]
    public string ListWeapons()
    {
        weapons.Reload();
        var list = weapons.GetAll()
            .Select(w => new { name = w.Name, category = w.Category, manufacturer = w.Manufacturer })
            .OrderBy(x => x.name).ToList();
        return JsonSerializer.Serialize(list, CanonTools.JsonOpts);
    }

    [McpServerTool, Description("Load a weapon's full record by name: category, manufacturer, ammunition_type, lethality, mechanics, sensory detail, story_hooks, image prompts.")]
    public string GetWeapon([Description("Weapon name.")] string name)
    {
        var w = weapons.GetByName(name);
        if (w == null) return JsonSerializer.Serialize(new { error = "not_found", name }, CanonTools.JsonOpts);
        return JsonSerializer.Serialize(w, CanonTools.JsonOpts);
    }

    [McpServerTool, Description("List every ammunition variant in canon (calibers, specialty rounds, energy cells).")]
    public string ListAmmunition()
    {
        ammunition.Reload();
        var list = ammunition.GetAll()
            .Select(a => new { name = a.Name, category = a.Category, manufacturer = a.Manufacturer })
            .OrderBy(x => x.name).ToList();
        return JsonSerializer.Serialize(list, CanonTools.JsonOpts);
    }

    [McpServerTool, Description("Load an ammunition record.")]
    public string GetAmmunition([Description("Ammunition name.")] string name)
    {
        var a = ammunition.GetByName(name);
        if (a == null) return JsonSerializer.Serialize(new { error = "not_found", name }, CanonTools.JsonOpts);
        return JsonSerializer.Serialize(a, CanonTools.JsonOpts);
    }

    [McpServerTool, Description("List every equipment item in canon: gear, tools, devices, augmentation accessories.")]
    public string ListEquipment()
    {
        equipment.Reload();
        var list = equipment.GetAll()
            .Select(e => new { product_name = e.ProductName, name = e.Name, brand = e.BrandName, category = e.Category, manufacturer = e.Manufacturer })
            .OrderBy(x => x.product_name.Length > 0 ? x.product_name : x.name).ToList();
        return JsonSerializer.Serialize(list, CanonTools.JsonOpts);
    }

    [McpServerTool, Description("Load an equipment record. Match is by ProductName when set, else Name (case-insensitive).")]
    public string GetEquipment([Description("Equipment ProductName or Name.")] string name)
    {
        var e = equipment.GetByName(name);
        if (e == null) return JsonSerializer.Serialize(new { error = "not_found", name }, CanonTools.JsonOpts);
        return JsonSerializer.Serialize(e, CanonTools.JsonOpts);
    }

    [McpServerTool, Description("List every technology entry: software, protocols, networks, systems.")]
    public string ListTechnology()
    {
        technology.Reload();
        var list = technology.GetAll()
            .Select(t => new { product_name = t.ProductName, name = t.Name, brand = t.BrandName, subcategory = t.Subcategory, tier_availability = t.TierAvailability })
            .OrderBy(x => x.product_name.Length > 0 ? x.product_name : x.name).ToList();
        return JsonSerializer.Serialize(list, CanonTools.JsonOpts);
    }

    [McpServerTool, Description("Load a technology record. Match is by ProductName when set, else Name.")]
    public string GetTechnology([Description("Technology ProductName or Name.")] string name)
    {
        var t = technology.GetByName(name);
        if (t == null) return JsonSerializer.Serialize(new { error = "not_found", name }, CanonTools.JsonOpts);
        return JsonSerializer.Serialize(t, CanonTools.JsonOpts);
    }

    [McpServerTool, Description("List every cyberware product: implants, neural augmentations, prosthetic limbs.")]
    public string ListCyberware()
    {
        cyberware.Reload();
        var list = cyberware.GetAll()
            .Select(c => new { product_name = c.ProductName, name = c.Name, brand = c.BrandName, category = c.Category, manufacturer = c.Manufacturer })
            .OrderBy(x => x.product_name.Length > 0 ? x.product_name : x.name).ToList();
        return JsonSerializer.Serialize(list, CanonTools.JsonOpts);
    }

    [McpServerTool, Description("Load a cyberware record: install procedure, side effects, sensory experience, dependency profile.")]
    public string GetCyberware([Description("Cyberware ProductName or Name.")] string name)
    {
        var c = cyberware.GetByName(name);
        if (c == null) return JsonSerializer.Serialize(new { error = "not_found", name }, CanonTools.JsonOpts);
        return JsonSerializer.Serialize(c, CanonTools.JsonOpts);
    }

    [McpServerTool, Description("List every apparel item in canon: clothing, armor, accessories.")]
    public string ListApparel()
    {
        apparel.Reload();
        var list = apparel.GetAll()
            .Select(a => new { name = a.Name, category = a.Category, manufacturer = a.Manufacturer })
            .OrderBy(x => x.name).ToList();
        return JsonSerializer.Serialize(list, CanonTools.JsonOpts);
    }

    [McpServerTool, Description("Load an apparel record.")]
    public string GetApparel([Description("Apparel name.")] string name)
    {
        var a = apparel.GetByName(name);
        if (a == null) return JsonSerializer.Serialize(new { error = "not_found", name }, CanonTools.JsonOpts);
        return JsonSerializer.Serialize(a, CanonTools.JsonOpts);
    }

    [McpServerTool, Description("List every pharmaceutical: drugs, stims, pain modulators, neuro-pharma.")]
    public string ListPharmaceuticals()
    {
        pharmaceuticals.Reload();
        var list = pharmaceuticals.GetAll()
            .Select(p => new { name = p.Name, category = p.Category, manufacturer = p.Manufacturer })
            .OrderBy(x => x.name).ToList();
        return JsonSerializer.Serialize(list, CanonTools.JsonOpts);
    }

    [McpServerTool, Description("Load a pharmaceutical record: effects, dosage, side effects, dependency profile.")]
    public string GetPharmaceutical([Description("Pharmaceutical name.")] string name)
    {
        var p = pharmaceuticals.GetByName(name);
        if (p == null) return JsonSerializer.Serialize(new { error = "not_found", name }, CanonTools.JsonOpts);
        return JsonSerializer.Serialize(p, CanonTools.JsonOpts);
    }

    [McpServerTool, Description("List every automaton in canon: drones, security bots, Iowan Behemoths, agricultural machines.")]
    public string ListAutomata()
    {
        automata.Reload();
        var list = automata.GetAll()
            .Select(a => new { name = a.Name, manufacturer = a.Manufacturer })
            .OrderBy(x => x.name).ToList();
        return JsonSerializer.Serialize(list, CanonTools.JsonOpts);
    }

    [McpServerTool, Description("Load an automaton record. Behemoths and other industrial automata: not synthetic life — these are machines.")]
    public string GetAutomaton([Description("Automaton name.")] string name)
    {
        var a = automata.GetByName(name);
        if (a == null) return JsonSerializer.Serialize(new { error = "not_found", name }, CanonTools.JsonOpts);
        return JsonSerializer.Serialize(a, CanonTools.JsonOpts);
    }

    [McpServerTool, Description("List every synthetic life entry: Emergent Life Forms (ELFs), constructs, distributed AIs treated as persons.")]
    public string ListSynthetics()
    {
        synthetics.Reload();
        var list = synthetics.GetAll()
            .Select(s => new { name = s.Name, type = s.Type })
            .OrderBy(x => x.name).ToList();
        return JsonSerializer.Serialize(list, CanonTools.JsonOpts);
    }

    [McpServerTool, Description("Load a synthetic life record.")]
    public string GetSynthetic([Description("Synthetic name.")] string name)
    {
        var s = synthetics.GetByName(name);
        if (s == null) return JsonSerializer.Serialize(new { error = "not_found", name }, CanonTools.JsonOpts);
        return JsonSerializer.Serialize(s, CanonTools.JsonOpts);
    }

    [McpServerTool, Description("List every archetype: occupational/social roles in the world (street samurai, fixer, runner, gleaner, etc).")]
    public string ListArchetypes()
    {
        archetypes.Reload();
        var list = archetypes.GetAll()
            .Select(a => new { name = a.Name, category = a.Category })
            .OrderBy(x => x.name).ToList();
        return JsonSerializer.Serialize(list, CanonTools.JsonOpts);
    }

    [McpServerTool, Description("Load an archetype record: typical behavior, knowledge, equipment, social position.")]
    public string GetArchetype([Description("Archetype name.")] string name)
    {
        var a = archetypes.GetByName(name);
        if (a == null) return JsonSerializer.Serialize(new { error = "not_found", name }, CanonTools.JsonOpts);
        return JsonSerializer.Serialize(a, CanonTools.JsonOpts);
    }

    [McpServerTool, Description("List every material: alloys, composites, fabrics, biomaterials. Use this when describing physical objects with specificity.")]
    public string ListMaterials()
    {
        materials.Reload();
        var list = materials.GetAll()
            .Select(m => new { name = m.Name, category = m.Category })
            .OrderBy(x => x.name).ToList();
        return JsonSerializer.Serialize(list, CanonTools.JsonOpts);
    }

    [McpServerTool, Description("Load a material record: properties, applications, sensory qualities.")]
    public string GetMaterial([Description("Material name.")] string name)
    {
        var m = materials.GetByName(name);
        if (m == null) return JsonSerializer.Serialize(new { error = "not_found", name }, CanonTools.JsonOpts);
        return JsonSerializer.Serialize(m, CanonTools.JsonOpts);
    }

    [McpServerTool, Description("List every transportation entry: vehicles, transit systems, The Pulse stations, individual transports.")]
    public string ListTransportation()
    {
        transportation.Reload();
        var list = transportation.GetAll()
            .Select(t => new { name = t.Name, category = t.Category, manufacturer = t.Manufacturer })
            .OrderBy(x => x.name).ToList();
        return JsonSerializer.Serialize(list, CanonTools.JsonOpts);
    }

    [McpServerTool, Description("Load a transportation record.")]
    public string GetTransportation([Description("Transportation name.")] string name)
    {
        var t = transportation.GetByName(name);
        if (t == null) return JsonSerializer.Serialize(new { error = "not_found", name }, CanonTools.JsonOpts);
        return JsonSerializer.Serialize(t, CanonTools.JsonOpts);
    }

    [McpServerTool, Description("List every consumer good: food, drinks, household items, branded products.")]
    public string ListConsumerGoods()
    {
        consumerGoods.Reload();
        var list = consumerGoods.GetAll()
            .Select(c => new { name = c.Name, category = c.Category, manufacturer = c.Manufacturer })
            .OrderBy(x => x.name).ToList();
        return JsonSerializer.Serialize(list, CanonTools.JsonOpts);
    }

    [McpServerTool, Description("Load a consumer good record.")]
    public string GetConsumerGood([Description("Consumer good name.")] string name)
    {
        var c = consumerGoods.GetByName(name);
        if (c == null) return JsonSerializer.Serialize(new { error = "not_found", name }, CanonTools.JsonOpts);
        return JsonSerializer.Serialize(c, CanonTools.JsonOpts);
    }

    [McpServerTool, Description("List every quote: in-world sayings, graffiti, advertising copy, attributed quotes. Useful for chapter epigraphs and ambient flavor.")]
    public string ListQuotes(
        [Description("Optional filter: only quotes with a tag matching this value. Empty for all.")] string tag = "")
    {
        quotes.Reload();
        var all = quotes.GetAll();
        if (!string.IsNullOrWhiteSpace(tag))
            all = all.Where(q => q.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase)).ToList();
        var list = all
            .Select(q => new { id = q.Id, quote = q.Quote, attribution = q.Attribution, source = q.Source, category = q.Category, in_world = q.InWorld, tags = q.Tags })
            .ToList();
        return JsonSerializer.Serialize(list, CanonTools.JsonOpts);
    }

    [McpServerTool, Description("List every worldbuilding document by file name + title + category. Use get_document to load the body.")]
    public string ListDocuments()
    {
        documents.Reload();
        var list = documents.GetAll()
            .Select(d => new { file_name = d.FileName, title = d.Title, category = d.Category, line_count = d.LineCount, headings = d.Headings })
            .OrderBy(x => x.file_name).ToList();
        return JsonSerializer.Serialize(list, CanonTools.JsonOpts);
    }

    [McpServerTool, Description("Load a worldbuilding document by its file_name (the filename-derived identifier). Returns the full prose body.")]
    public string GetDocument([Description("Document file_name (e.g. 'corponations_overview' or as listed by list_documents).")] string fileName)
    {
        var d = documents.GetByName(fileName);
        if (d == null) return JsonSerializer.Serialize(new { error = "not_found", file_name = fileName }, CanonTools.JsonOpts);
        return JsonSerializer.Serialize(d, CanonTools.JsonOpts);
    }

    [McpServerTool, Description("List every gene modification: somatic edits, lineage modifications, body-spec edits.")]
    public string ListGenemods()
    {
        genemods.Reload();
        var list = genemods.GetAll().Select(g => new { name = g.Name }).OrderBy(x => x.name).ToList();
        return JsonSerializer.Serialize(list, CanonTools.JsonOpts);
    }

    [McpServerTool, Description("Load a gene modification record.")]
    public string GetGenemod([Description("Genemod name.")] string name)
    {
        var g = genemods.GetByName(name);
        if (g == null) return JsonSerializer.Serialize(new { error = "not_found", name }, CanonTools.JsonOpts);
        return JsonSerializer.Serialize(g, CanonTools.JsonOpts);
    }

    [McpServerTool, Description("List every lab specimen — anomalous biological / synthetic / hybrid samples held in research facilities.")]
    public string ListLabSpecimens()
    {
        labSpecimens.Reload();
        var list = labSpecimens.GetAll().Select(s => new { name = s.Name }).OrderBy(x => x.name).ToList();
        return JsonSerializer.Serialize(list, CanonTools.JsonOpts);
    }

    [McpServerTool, Description("Load a lab specimen record.")]
    public string GetLabSpecimen([Description("Specimen name.")] string name)
    {
        var s = labSpecimens.GetByName(name);
        if (s == null) return JsonSerializer.Serialize(new { error = "not_found", name }, CanonTools.JsonOpts);
        return JsonSerializer.Serialize(s, CanonTools.JsonOpts);
    }

    [McpServerTool, Description("List every psionic phenomenon recorded in canon.")]
    public string ListPsionics()
    {
        psionics.Reload();
        var list = psionics.GetAll().Select(p => new { name = p.Name }).OrderBy(x => x.name).ToList();
        return JsonSerializer.Serialize(list, CanonTools.JsonOpts);
    }

    [McpServerTool, Description("Load a psionic record.")]
    public string GetPsionic([Description("Psionic name.")] string name)
    {
        var p = psionics.GetByName(name);
        if (p == null) return JsonSerializer.Serialize(new { error = "not_found", name }, CanonTools.JsonOpts);
        return JsonSerializer.Serialize(p, CanonTools.JsonOpts);
    }

    [McpServerTool, Description("List every subsidiary — child/holding companies of larger corponations.")]
    public string ListSubsidiaries()
    {
        subsidiaries.Reload();
        var list = subsidiaries.GetAll().Select(s => new { name = s.Name }).OrderBy(x => x.name).ToList();
        return JsonSerializer.Serialize(list, CanonTools.JsonOpts);
    }

    [McpServerTool, Description("Load a subsidiary record.")]
    public string GetSubsidiary([Description("Subsidiary name.")] string name)
    {
        var s = subsidiaries.GetByName(name);
        if (s == null) return JsonSerializer.Serialize(new { error = "not_found", name }, CanonTools.JsonOpts);
        return JsonSerializer.Serialize(s, CanonTools.JsonOpts);
    }
}

[McpServerToolType]
public class BibleTools
{
    private readonly ToneBibleRepository tone;
    private readonly StoryBibleRepository storyBible;
    private readonly CharacterProfileRepository profile;

    public BibleTools(ToneBibleRepository tone, StoryBibleRepository storyBible, CharacterProfileRepository profile)
    {
        this.tone = tone;
        this.storyBible = storyBible;
        this.profile = profile;
    }

    [McpServerTool, Description("Load the Tone Bible — voice, register, sensory palette, what to do and what not to do for prose. Inject this into the system prompt when drafting prose.")]
    public string GetToneBible()
        => JsonSerializer.Serialize(tone.Get(), CanonTools.JsonOpts);

    [McpServerTool, Description("Load the Story Bible — structural rules for narrative shape: act structure, beat anatomy, motif planting, dialogue cadence.")]
    public string GetStoryBible()
        => JsonSerializer.Serialize(storyBible.Get(), CanonTools.JsonOpts);

    [McpServerTool, Description("Load the Character Profile — the protagonist's core contradiction, signature behavior, voice anchors. Often Kyle's profile in this project.")]
    public string GetCharacterProfile()
        => JsonSerializer.Serialize(profile.Get(), CanonTools.JsonOpts);
}
