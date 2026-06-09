using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
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

    // Plain-text auto-linking heuristics:
    //   • Min length 4 — shorter strings produce too many false positives (e.g. "AI", "Pulse").
    //   • Must start with an uppercase letter (proper-noun rule) OR a recognized symbol (Φ, $, #).
    //   • First-character case-sensitive match required against source (skips "the war" → "War").
    //   • Stop list filters very common English words that share spelling with entity names.
    // Explicit [[wiki|id]] markup bypasses every rule above and links anything in the index.
    private const int MinAutoLinkLength = 4;

    private static readonly HashSet<string> AutoLinkStopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "The", "This", "That", "These", "Those", "There", "Their", "Them", "They",
        "What", "When", "Where", "Which", "While", "Whose",
        "Have", "Will", "Would", "Could", "Should", "Were", "Been", "Being",
        "From", "With", "Into", "Onto", "Over", "Under", "After", "Before",
        "About", "Above", "Across", "Against", "Among", "Around", "Behind",
        "Some", "Many", "Much", "More", "Most", "Less", "Least", "Each", "Every",
        "Year", "Years", "Time", "Times", "Days", "Hour", "Week", "Month",
        "Long", "Last", "Next", "Same", "Such", "Just", "Only", "Then", "Than",
        "Once", "Here", "Even", "Also", "Both", "Other", "Another", "Until",
        "Like", "Want", "Need", "Make", "Take", "Find", "Know", "Tell", "Say",
        "Come", "Came", "Going", "Goes", "Went", "Done", "Made", "Said",
        "Good", "Best", "Better", "Best", "Bad", "Right", "Left", "Back",
        "True", "False", "Real", "Sure", "Maybe", "Yeah", "Okay",
        "Mind", "Hand", "Foot", "Head", "Face", "Eyes", "Body",
        "City", "Town", "Home", "Door", "Wall", "Road", "Path", "Place",
        "Light", "Dark", "Cold", "Warm", "Hard", "Soft", "Slow", "Fast",
        "Open", "Close", "Stop", "Start", "Wait", "Hold", "Turn", "Move",
        "Look", "Watch", "Hear", "Feel", "Touch", "Walk", "Talk",
        "First", "Second", "Third", "Final",
    };

    private readonly CharacterRepository characters;
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
    private bool indexBuilt;
    private readonly object syncLock = new();
    private readonly ILogger<XrefService> logger;
    private readonly SettingsService settings;
    private List<XrefConflict> conflicts = [];

    public XrefService(
        CharacterRepository characters,
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
        PsionicRepository psionics,
        ILogger<XrefService> logger,
        SettingsService settings
        )
    {
        this.logger = logger;
        this.settings = settings;
        this.characters = characters;
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
            indexBuilt = false;
        }
    }

    public void EnsureBuilt()
    {
        lock (syncLock)
        {
            if (indexBuilt) return;
            RebuildIndex();
        }
    }

    private void RebuildIndex()
    {
        var newIndex = new Dictionary<string, XrefEntry>(StringComparer.OrdinalIgnoreCase);
        var newIndexById = new Dictionary<string, XrefEntry>(StringComparer.OrdinalIgnoreCase);
        var newConflicts = new List<XrefConflict>();

        void Add(string name, string id, string type, string route, string subtitle = "")
        {
            if (string.IsNullOrWhiteSpace(name) || name.Length < 3) return;
            var entry = new XrefEntry(id, name, type, route, subtitle);
            if (!newIndex.TryAdd(name, entry))
            {
                var existing = newIndex[name];
                // Self-overlap: the same record reaches Add twice because its
                // Name, ProductName, and an entry in Aliases are all the same
                // string. Not a real disambiguation conflict — just a noisy
                // index pass — so swallow it instead of warning.
                if (existing.Type == type && string.Equals(existing.Id, id, StringComparison.OrdinalIgnoreCase))
                {
                    newIndexById.TryAdd(id, entry);
                    return;
                }
                newConflicts.Add(new XrefConflict(name, existing, entry));
                if (existing.Type == type)
                    logger.LogWarning("Xref disambiguation conflict: \"{Name}\" claimed by {TypeA}/{IdA} and {TypeB}/{IdB}",
                        name, existing.Type, existing.Id, type, id);
                else
                    logger.LogDebug("Xref cross-type overlap: \"{Name}\" claimed by {TypeA}/{IdA} and {TypeB}/{IdB}",
                        name, existing.Type, existing.Id, type, id);
            }
            newIndexById.TryAdd(id, entry);
        }

        string BestName(string productName, string name) =>
            !string.IsNullOrWhiteSpace(productName) ? productName : name;

        foreach (var c in characters.GetAll())
        {
            Add(c.Name, c.Id, "character", "/characters", c.Role);
            foreach (var a in c.Aliases) Add(a, c.Id, "character", "/characters", c.Role);
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
            if (!string.IsNullOrWhiteSpace(t.ProductName) && !t.Name.Equals(t.ProductName, StringComparison.OrdinalIgnoreCase))
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
            if (!string.IsNullOrWhiteSpace(e.ProductName) && !e.Name.Equals(e.ProductName, StringComparison.OrdinalIgnoreCase))
                Add(e.Name, e.Id, "equipment", "/equipment", e.Category);
            foreach (var al in e.Aliases) Add(al, e.Id, "equipment", "/equipment", e.Category);
        }
        foreach (var cw in cyberware.GetAll())
        {
            var n = BestName(cw.ProductName, cw.Name);
            Add(n, cw.Id, "cyberware", "/cyberware", cw.Category);
            if (!string.IsNullOrWhiteSpace(cw.ProductName) && !cw.Name.Equals(cw.ProductName, StringComparison.OrdinalIgnoreCase))
                Add(cw.Name, cw.Id, "cyberware", "/cyberware", cw.Category);
            foreach (var al in cw.Aliases) Add(al, cw.Id, "cyberware", "/cyberware", cw.Category);
        }
        foreach (var g in genemods.GetAll())
        {
            var n = BestName(g.ProductName, g.Name);
            Add(n, g.Id, "genemod", "/genemods", g.Category);
            if (!string.IsNullOrWhiteSpace(g.ProductName) && !g.Name.Equals(g.ProductName, StringComparison.OrdinalIgnoreCase))
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
            if (!string.IsNullOrWhiteSpace(m.ProductName) && !m.Name.Equals(m.ProductName, StringComparison.OrdinalIgnoreCase))
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
            if (!string.IsNullOrWhiteSpace(cg.ProductName) && !cg.Name.Equals(cg.ProductName, StringComparison.OrdinalIgnoreCase))
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
        conflicts = newConflicts;
        indexBuilt = true;
    }

    public XrefEntry? Resolve(string name)
    {
        EnsureBuilt();
        return index.GetValueOrDefault(name);
    }

    /// <summary>Splits text into alternating plain and xref segments for inline rendering.
    /// Resolves explicit [[DisplayText|entityId]] and [[Name]] wiki links only.</summary>
    public List<TextSegment> ParseSegments(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return [new PlainSegment(text ?? "")];
        EnsureBuilt();

        // Pass 1: resolve explicit [[WikiLink]] and [[display|id]] markup.
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
                : new BrokenLinkSegment(displayText));

            cursor = wm.Index + wm.Length;
        }
        if (cursor < text.Length)
            pass1.Add(new PlainSegment(text[cursor..]));

        // Pass 2: scan PlainSegments for entity name mentions (longest-match-first NER).
        if (!settings.EnablePlainTextNer)
            return pass1;

        // Auto-link candidates: filter out names too short, lowercase-only, or stop-listed.
        // Sort longest-first so "Smith & Wesson Vector .45" beats "Smith & Wesson" at the same position.
        var autoLinkNames = index.Keys
            .Where(IsAutoLinkable)
            .OrderByDescending(k => k.Length)
            .ToList();

        var result = new List<TextSegment>();
        foreach (var seg in pass1)
        {
            if (seg is not PlainSegment plain) { result.Add(seg); continue; }
            result.AddRange(ScanPlainText(plain.Text, autoLinkNames));
        }
        return result;
    }

    private static bool IsAutoLinkable(string name)
    {
        if (name.Length < MinAutoLinkLength) return false;
        if (AutoLinkStopWords.Contains(name)) return false;
        // Require the name to begin with an uppercase letter or recognized symbol.
        // Lowercase-only slang ("ghosting") is too easy to confuse with verbs in narration —
        // explicit [[ghosting]] markup still works for those.
        var first = name[0];
        if (char.IsLetter(first) && !char.IsUpper(first)) return false;
        return true;
    }

    private static bool IsWordChar(char c) => char.IsLetterOrDigit(c);

    private IEnumerable<TextSegment> ScanPlainText(string text, List<string> sortedNames)
    {
        int pos = 0;
        int plainStart = 0;
        while (pos < text.Length)
        {
            // Only try matching at word boundaries (start of string or after non-word char)
            bool atBoundary = pos == 0 || !IsWordChar(text[pos - 1]);
            if (atBoundary)
            {
                bool matched = false;
                foreach (var name in sortedNames)
                {
                    if (pos + name.Length > text.Length) continue;

                    var span = text.AsSpan(pos, name.Length);

                    // Case-sensitive first character: an entity named "War" only links when
                    // the source also has a capital W. Mid-sentence "the war drums" stays plain.
                    if (char.IsLetter(name[0]) && span[0] != name[0]) continue;

                    if (!span.Equals(name, StringComparison.OrdinalIgnoreCase)) continue;

                    // Verify word boundary on the right side.
                    int end = pos + name.Length;
                    if (end < text.Length && IsWordChar(text[end])) continue;

                    if (pos > plainStart)
                        yield return new PlainSegment(text[plainStart..pos]);

                    yield return new XrefSegment(text.Substring(pos, name.Length), index[name]);
                    pos = end;
                    plainStart = pos;
                    matched = true;
                    break;
                }
                if (matched) continue;
            }
            pos++;
        }
        if (plainStart < text.Length)
            yield return new PlainSegment(text[plainStart..]);
    }

    /// <summary>All indexed entries, for typeahead / full search.</summary>
    public IReadOnlyList<XrefConflict> GetConflicts() { EnsureBuilt(); return conflicts; }

    public IEnumerable<XrefEntry> AllEntries()
    {
        EnsureBuilt();
        return index.Values.DistinctBy(e => e.Id);
    }

    /// <summary>Full name→entry index including aliases. Used by CrossReferenceService.</summary>
    public IReadOnlyDictionary<string, XrefEntry> GetNameIndex()
    {
        EnsureBuilt();
        return index;
    }

    public void InvalidateIndex() => Invalidate();
}

public record XrefEntry(string Id, string DisplayName, string Type, string Route, string Subtitle = "");

public record XrefConflict(string Name, XrefEntry Winner, XrefEntry Challenger);

public abstract record TextSegment(string Text);
public record PlainSegment(string Text) : TextSegment(Text);
public record XrefSegment(string Text, XrefEntry Entry) : TextSegment(Text);
public record BrokenLinkSegment(string Text) : TextSegment(Text);
