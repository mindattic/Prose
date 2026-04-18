using System.Text.RegularExpressions;
using StreetSamurai.Core.Models.Canon;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Resolves entity names in free text to clickable cross-reference links.
/// Indexes all named entities across every canon repo. Rebuilds automatically when repos change.
///
/// Wiki-link syntax (as written by wiki_scan.py):
///   [[DisplayText|entityId]]  — primary format, resolved by ID (stable across renames)
///   [[Name]]                  — fallback, resolved by name (manual links, auto-linking)
/// Explicit wiki links are resolved in Pass 1; remaining plain text is auto-linked in Pass 2.
/// </summary>
public class XrefService
{
    private static readonly Regex WikiLinkRe =
        new(@"\[\[([^\]|]+?)(?:\|([^\]]+?))?\]\]", RegexOptions.Compiled);

    private readonly CharacterRepository characters;
    private readonly SyntheticLifeRepository synthetics;
    private readonly DistrictRepository districts;
    private readonly FactionRepository factions;
    private readonly CorponationRepository corponations;
    private readonly TechnologyRepository technology;
    private readonly VocabularyRepository vocabulary;
    private readonly WeaponryRepository weaponry;
    private readonly AmmunitionRepository ammunition;
    private readonly EquipmentRepository equipment;
    private readonly CyberwareRepository cyberware;
    private readonly GenemodRepository genemods;
    private readonly TransportationRepository transportation;
    private readonly AutomatonRepository automata;
    private readonly SubsidiaryRepository subsidiaries;
    private readonly EntertainmentRepository entertainment;
    private readonly ApparelRepository apparel;
    private readonly MaterialRepository materials;
    private readonly PharmaceuticalRepository pharmaceuticals;
    private readonly ConsumerGoodRepository consumerGoods;
    private readonly ContractRepository contracts;
    private readonly LabSpecimenRepository labSpecimens;
    private readonly PsionicRepository psionics;

    private Dictionary<string, XrefEntry> index = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, XrefEntry> indexById = new(StringComparer.OrdinalIgnoreCase);
    private Regex? matchRegex;
    private readonly object syncLock = new();

    public XrefService(
        CharacterRepository characters,
        SyntheticLifeRepository synthetics,
        DistrictRepository districts,
        FactionRepository factions,
        CorponationRepository corponations,
        TechnologyRepository technology,
        VocabularyRepository vocabulary,
        WeaponryRepository weaponry,
        AmmunitionRepository ammunition,
        EquipmentRepository equipment,
        CyberwareRepository cyberware,
        GenemodRepository genemods,
        TransportationRepository transportation,
        AutomatonRepository automata,
        SubsidiaryRepository subsidiaries,
        EntertainmentRepository entertainment,
        ApparelRepository apparel,
        MaterialRepository materials,
        PharmaceuticalRepository pharmaceuticals,
        ConsumerGoodRepository consumerGoods,
        ContractRepository contracts,
        LabSpecimenRepository labSpecimens,
        PsionicRepository psionics
        )
    {
        this.characters = characters;
        this.synthetics = synthetics;
        this.districts = districts;
        this.factions = factions;
        this.corponations = corponations;
        this.technology = technology;
        this.vocabulary = vocabulary;
        this.weaponry = weaponry;
        this.ammunition = ammunition;
        this.equipment = equipment;
        this.cyberware = cyberware;
        this.genemods = genemods;
        this.transportation = transportation;
        this.automata = automata;
        this.subsidiaries = subsidiaries;
        this.entertainment = entertainment;
        this.apparel = apparel;
        this.materials = materials;
        this.pharmaceuticals = pharmaceuticals;
        this.consumerGoods = consumerGoods;
        this.contracts = contracts;
        this.labSpecimens = labSpecimens;
        this.psionics = psionics;
        characters.OnItemSaved     += _ => Invalidate();
        synthetics.OnItemSaved     += _ => Invalidate();
        districts.OnItemSaved      += _ => Invalidate();
        factions.OnItemSaved       += _ => Invalidate();
        corponations.OnItemSaved   += _ => Invalidate();
        technology.OnItemSaved     += _ => Invalidate();
        vocabulary.OnItemSaved     += _ => Invalidate();
        weaponry.OnItemSaved       += _ => Invalidate();
        ammunition.OnItemSaved     += _ => Invalidate();
        equipment.OnItemSaved      += _ => Invalidate();
        cyberware.OnItemSaved      += _ => Invalidate();
        genemods.OnItemSaved       += _ => Invalidate();
        transportation.OnItemSaved += _ => Invalidate();
        automata.OnItemSaved       += _ => Invalidate();
        subsidiaries.OnItemSaved   += _ => Invalidate();
        entertainment.OnItemSaved  += _ => Invalidate();
        apparel.OnItemSaved        += _ => Invalidate();
        materials.OnItemSaved      += _ => Invalidate();
        pharmaceuticals.OnItemSaved += _ => Invalidate();
        consumerGoods.OnItemSaved  += _ => Invalidate();
        contracts.OnItemSaved      += _ => Invalidate();
        labSpecimens.OnItemSaved   += _ => Invalidate();
        psionics.OnItemSaved       += _ => Invalidate();
    }

    private void Invalidate()
    {
        lock (syncLock)
        {
            index = new(StringComparer.OrdinalIgnoreCase);
            indexById = new(StringComparer.OrdinalIgnoreCase);
            matchRegex = null;
        }
    }

    public void EnsureBuilt()
    {
        lock (syncLock)
        {
            if (matchRegex != null) return;
            RebuildIndex();
        }
    }

    private void RebuildIndex()
    {
        var newIndex = new Dictionary<string, XrefEntry>(StringComparer.OrdinalIgnoreCase);
        var newIndexById = new Dictionary<string, XrefEntry>(StringComparer.OrdinalIgnoreCase);

        void Add(string name, string id, string type, string route, string subtitle = "")
        {
            if (string.IsNullOrWhiteSpace(name) || name.Length < 3) return;
            var entry = new XrefEntry(id, name, type, route, subtitle);
            newIndex.TryAdd(name, entry);
            newIndexById.TryAdd(id, entry);
        }

        string BestName(string productName, string name) =>
            !string.IsNullOrWhiteSpace(productName) ? productName : name;

        foreach (var c in characters.GetAll())
        {
            Add(c.Name, c.Id, "character", "/characters", c.Role);
            foreach (var a in c.Aliases) Add(a, c.Id, "character", "/characters", c.Role);
        }
        foreach (var s in synthetics.GetAll())
        {
            Add(s.Name, s.Id, "synthetic", "/synthetics", s.Classification);
            foreach (var a in s.Aliases) Add(a, s.Id, "synthetic", "/synthetics", s.Classification);
        }
        foreach (var d in districts.GetAll())
        {
            Add(d.Name, d.Id, "place", "/places", "");
            foreach (var a in d.Aliases) Add(a, d.Id, "place", "/places", "");
        }
        foreach (var f in factions.GetAll())
        {
            Add(f.Name, f.Id, "faction", "/factions", f.Motto);
            foreach (var a in f.Aliases) Add(a, f.Id, "faction", "/factions", f.Motto);
        }
        foreach (var c in corponations.GetAll())
        {
            Add(c.Name, c.Id, "corponation", "/corps", c.Sector);
            foreach (var cn in c.CommonNames) Add(cn, c.Id, "corponation", "/corps", c.Sector);
        }
        foreach (var t in technology.GetAll())
        {
            var n = BestName(t.ProductName, t.Name);
            Add(n, t.Id, "technology", "/technology", t.Subcategory);
            if (!string.IsNullOrWhiteSpace(t.ProductName) && t.Name != t.ProductName)
                Add(t.Name, t.Id, "technology", "/technology", t.Subcategory);
            foreach (var a in t.Aliases) Add(a, t.Id, "technology", "/technology", t.Subcategory);
        }
        foreach (var v in vocabulary.GetAll())
        {
            Add(v.Term, v.Id, "vocabulary", "/vocabulary", v.Definition.Length > 60 ? v.Definition[..57] + "…" : v.Definition);
        }
        foreach (var w in weaponry.GetAll())
        {
            Add(w.Name, w.Id, "weapon", "/weaponry", w.Category);
            foreach (var a in w.Aliases) Add(a, w.Id, "weapon", "/weaponry", w.Category);
        }
        foreach (var a in ammunition.GetAll())
        {
            Add(a.Name, a.Id, "ammunition", "/ammunition", a.Category);
            foreach (var al in a.Aliases) Add(al, a.Id, "ammunition", "/ammunition", a.Category);
        }
        foreach (var e in equipment.GetAll())
        {
            var n = BestName(e.ProductName, e.Name);
            Add(n, e.Id, "equipment", "/equipment", e.Category);
            if (!string.IsNullOrWhiteSpace(e.ProductName) && e.Name != e.ProductName)
                Add(e.Name, e.Id, "equipment", "/equipment", e.Category);
            foreach (var al in e.Aliases) Add(al, e.Id, "equipment", "/equipment", e.Category);
        }
        foreach (var cw in cyberware.GetAll())
        {
            var n = BestName(cw.ProductName, cw.Name);
            Add(n, cw.Id, "cyberware", "/cyberware", cw.Category);
            if (!string.IsNullOrWhiteSpace(cw.ProductName) && cw.Name != cw.ProductName)
                Add(cw.Name, cw.Id, "cyberware", "/cyberware", cw.Category);
            foreach (var al in cw.Aliases) Add(al, cw.Id, "cyberware", "/cyberware", cw.Category);
        }
        foreach (var g in genemods.GetAll())
        {
            var n = BestName(g.ProductName, g.Name);
            Add(n, g.Id, "genemod", "/genemods", g.Category);
            if (!string.IsNullOrWhiteSpace(g.ProductName) && g.Name != g.ProductName)
                Add(g.Name, g.Id, "genemod", "/genemods", g.Category);
            foreach (var al in g.Aliases) Add(al, g.Id, "genemod", "/genemods", g.Category);
        }
        foreach (var t in transportation.GetAll())
        {
            Add(t.Name, t.Id, "transportation", "/transportation", t.Category);
            foreach (var a in t.Aliases) Add(a, t.Id, "transportation", "/transportation", t.Category);
        }
        foreach (var a in automata.GetAll())
        {
            Add(a.Name, a.Id, "automaton", "/automata", a.Classification);
            foreach (var al in a.Aliases) Add(al, a.Id, "automaton", "/automata", a.Classification);
        }
        foreach (var s in subsidiaries.GetAll())
        {
            Add(s.Name, s.Id, "subsidiary", "/subsidiaries", s.LineOfBusiness);
        }
        foreach (var e in entertainment.GetAll())
        {
            Add(e.Name, e.Id, "entertainment", "/entertainment", e.Category);
            foreach (var a in e.Aliases) Add(a, e.Id, "entertainment", "/entertainment", e.Category);
        }
        foreach (var a in apparel.GetAll())
        {
            Add(a.Name, a.Id, "apparel", "/apparel", a.Category);
        }
        foreach (var m in materials.GetAll())
        {
            var n = BestName(m.ProductName, m.Name);
            Add(n, m.Id, "material", "/materials", m.Category);
            if (!string.IsNullOrWhiteSpace(m.ProductName) && m.Name != m.ProductName)
                Add(m.Name, m.Id, "material", "/materials", m.Category);
            foreach (var al in m.Aliases) Add(al, m.Id, "material", "/materials", m.Category);
        }
        foreach (var p in pharmaceuticals.GetAll())
        {
            Add(p.Name, p.Id, "pharmaceutical", "/pharmaceuticals", p.Category);
            foreach (var a in p.Aliases) Add(a, p.Id, "pharmaceutical", "/pharmaceuticals", p.Category);
        }
        foreach (var cg in consumerGoods.GetAll())
        {
            var n = BestName(cg.ProductName, cg.Name);
            Add(n, cg.Id, "consumer-good", "/goods", cg.Category);
            if (!string.IsNullOrWhiteSpace(cg.ProductName) && cg.Name != cg.ProductName)
                Add(cg.Name, cg.Id, "consumer-good", "/goods", cg.Category);
        }
        foreach (var c in contracts.GetAll())
        {
            Add(c.Codename, c.Id, "contract", "/contracts", c.Category);
        }
        foreach (var ls in labSpecimens.GetAll())
        {
            Add(ls.Name, ls.Id, "lab-specimen", "/lab-specimens", ls.Classification);
            foreach (var a in ls.Aliases) Add(a, ls.Id, "lab-specimen", "/lab-specimens", ls.Classification);
        }
        foreach (var p in psionics.GetAll())
        {
            Add(p.Name, p.Id, "psionic", "/psionics", p.Classification);
            foreach (var a in p.Aliases) Add(a, p.Id, "psionic", "/psionics", p.Classification);
        }


        index = newIndex;
        indexById = newIndexById;

        // Build regex — longest names first so e.g. "The Circuit" beats "Circuit"
        var patterns = newIndex.Keys
            .OrderByDescending(n => n.Length)
            .Select(Regex.Escape)
            .ToArray();

        if (patterns.Length == 0) { matchRegex = null; return; }

        matchRegex = new Regex(
            $@"(?<![a-zA-Z0-9\-])({string.Join("|", patterns)})(?![a-zA-Z0-9\-])",
            RegexOptions.IgnoreCase,
            TimeSpan.FromSeconds(2));
    }

    public XrefEntry? Resolve(string name)
    {
        EnsureBuilt();
        return index.GetValueOrDefault(name);
    }

    /// <summary>Splits text into alternating plain and xref segments for inline rendering.
    /// Explicit [[DisplayText|entityId]] wiki links are resolved first by ID; plain [[Name]]
    /// links fall back to name lookup. Remaining plain text is auto-linked in Pass 2.</summary>
    public List<TextSegment> ParseSegments(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return [new PlainSegment(text ?? "")];
        EnsureBuilt();

        // Pass 1 — split on explicit [[...]] wiki links
        // Format written by wiki_scan.py: [[DisplayText|entityId]]
        // Fallback (manual / legacy):      [[Name]]
        var pass1 = new List<TextSegment>();
        int cursor = 0;
        foreach (Match wm in WikiLinkRe.Matches(text))
        {
            if (wm.Index > cursor)
                pass1.Add(new PlainSegment(text[cursor..wm.Index]));

            var displayText = wm.Groups[1].Value.Trim();
            var secondPart  = wm.Groups[2].Success ? wm.Groups[2].Value.Trim() : null;

            XrefEntry? entry = secondPart != null
                ? indexById.GetValueOrDefault(secondPart)
                  ?? index.GetValueOrDefault(secondPart)
                : index.GetValueOrDefault(displayText);

            pass1.Add(entry != null
                ? new XrefSegment(displayText, entry)
                : new PlainSegment(displayText));

            cursor = wm.Index + wm.Length;
        }
        if (cursor < text.Length)
            pass1.Add(new PlainSegment(text[cursor..]));

        if (matchRegex == null) return pass1;

        // Pass 2 — auto-link plain segments by entity name
        var result = new List<TextSegment>();
        foreach (var seg in pass1)
        {
            if (seg is not PlainSegment plain || string.IsNullOrWhiteSpace(plain.Text))
            {
                result.Add(seg);
                continue;
            }

            int last = 0;
            try
            {
                foreach (Match m in matchRegex.Matches(plain.Text))
                {
                    if (m.Index > last)
                        result.Add(new PlainSegment(plain.Text[last..m.Index]));

                    var entry = index.GetValueOrDefault(m.Value);
                    result.Add(entry != null
                        ? new XrefSegment(m.Value, entry)
                        : new PlainSegment(m.Value));

                    last = m.Index + m.Length;
                }
            }
            catch (RegexMatchTimeoutException)
            {
                result.Add(plain);
                continue;
            }

            if (last < plain.Text.Length)
                result.Add(new PlainSegment(plain.Text[last..]));
        }

        return result;
    }

    /// <summary>Resolves only explicit [[DisplayText|id]] wiki links — no auto-linking.
    /// Safe for structured data fields where the auto-link regex would overflow the stack.</summary>
    public List<TextSegment> ParseWikiLinksOnly(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return [new PlainSegment(text ?? "")];
        EnsureBuilt();

        var result = new List<TextSegment>();
        int cursor = 0;
        foreach (Match wm in WikiLinkRe.Matches(text))
        {
            if (wm.Index > cursor)
                result.Add(new PlainSegment(text[cursor..wm.Index]));

            var displayText = wm.Groups[1].Value.Trim();
            var secondPart  = wm.Groups[2].Success ? wm.Groups[2].Value.Trim() : null;

            XrefEntry? entry = secondPart != null
                ? indexById.GetValueOrDefault(secondPart)
                  ?? index.GetValueOrDefault(secondPart)
                : index.GetValueOrDefault(displayText);

            result.Add(entry != null
                ? new XrefSegment(displayText, entry)
                : new PlainSegment(displayText));

            cursor = wm.Index + wm.Length;
        }
        if (cursor < text.Length)
            result.Add(new PlainSegment(text[cursor..]));

        return result;
    }

    /// <summary>All indexed entries, for typeahead / full search.</summary>
    public IEnumerable<XrefEntry> AllEntries()
    {
        EnsureBuilt();
        return index.Values.DistinctBy(e => e.Id);
    }
}

public record XrefEntry(string Id, string DisplayName, string Type, string Route, string Subtitle = "");

public abstract record TextSegment(string Text);
public record PlainSegment(string Text) : TextSegment(Text);
public record XrefSegment(string Text, XrefEntry Entry) : TextSegment(Text);
