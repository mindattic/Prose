using System.Text.RegularExpressions;
using StreetSamurai.Core.Models.Canon;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Resolves entity names in free text to clickable cross-reference links.
/// Indexes all named entities (characters, places, factions, corps, synthetics,
/// technology, vocabulary). Rebuilds automatically when repos change.
/// </summary>
public class XrefService
{
    private readonly CharacterRepository characters;
    private readonly SyntheticLifeRepository synthetics;
    private readonly DistrictRepository districts;
    private readonly FactionRepository factions;
    private readonly CorponationRepository corponations;
    private readonly TechnologyRepository technology;
    private readonly VocabularyRepository vocabulary;

    private Dictionary<string, XrefEntry> index = new(StringComparer.OrdinalIgnoreCase);
    private Regex? matchRegex;
    private readonly object syncLock = new();

    public XrefService(
        CharacterRepository characters,
        SyntheticLifeRepository synthetics,
        DistrictRepository districts,
        FactionRepository factions,
        CorponationRepository corponations,
        TechnologyRepository technology,
        VocabularyRepository vocabulary)
    {
        this.characters = characters;
        this.synthetics = synthetics;
        this.districts = districts;
        this.factions = factions;
        this.corponations = corponations;
        this.technology = technology;
        this.vocabulary = vocabulary;

        characters.OnItemSaved += _ => Invalidate();
        synthetics.OnItemSaved += _ => Invalidate();
        districts.OnItemSaved += _ => Invalidate();
        factions.OnItemSaved += _ => Invalidate();
        corponations.OnItemSaved += _ => Invalidate();
        technology.OnItemSaved += _ => Invalidate();
        vocabulary.OnItemSaved += _ => Invalidate();
    }

    private void Invalidate()
    {
        lock (syncLock) { index = new(StringComparer.OrdinalIgnoreCase); matchRegex = null; }
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

        void Add(string name, string id, string type, string route, string subtitle = "")
        {
            if (string.IsNullOrWhiteSpace(name) || name.Length < 3) return;
            newIndex.TryAdd(name, new XrefEntry(id, name, type, route, subtitle));
        }

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
            var name = !string.IsNullOrWhiteSpace(t.ProductName) ? t.ProductName : t.Name;
            Add(name, t.Id, "technology", "/technology", t.Subcategory);
            foreach (var a in t.Aliases) Add(a, t.Id, "technology", "/technology", t.Subcategory);
        }
        foreach (var v in vocabulary.GetAll())
        {
            Add(v.Term, v.Id, "vocabulary", "/vocabulary", v.Definition.Length > 60 ? v.Definition[..57] + "…" : v.Definition);
        }

        index = newIndex;

        // Build regex — longest names first so e.g. "The Circuit" beats "Circuit"
        var patterns = newIndex.Keys
            .OrderByDescending(n => n.Length)
            .Select(Regex.Escape)
            .ToArray();

        if (patterns.Length == 0) { matchRegex = null; return; }

        matchRegex = new Regex(
            $@"(?<![a-zA-Z0-9\-])({string.Join("|", patterns)})(?![a-zA-Z0-9\-])",
            RegexOptions.IgnoreCase | RegexOptions.Compiled,
            TimeSpan.FromSeconds(2));
    }

    public XrefEntry? Resolve(string name)
    {
        EnsureBuilt();
        return index.GetValueOrDefault(name);
    }

    /// <summary>Splits text into alternating plain and xref segments for inline rendering.</summary>
    public List<TextSegment> ParseSegments(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return [new PlainSegment(text ?? "")];
        EnsureBuilt();
        if (matchRegex == null) return [new PlainSegment(text)];

        var result = new List<TextSegment>();
        int last = 0;
        try
        {
            foreach (Match m in matchRegex.Matches(text))
            {
                if (m.Index > last)
                    result.Add(new PlainSegment(text[last..m.Index]));

                var entry = index.GetValueOrDefault(m.Value);
                result.Add(entry != null
                    ? new XrefSegment(m.Value, entry)
                    : new PlainSegment(m.Value));

                last = m.Index + m.Length;
            }
        }
        catch (RegexMatchTimeoutException)
        {
            return [new PlainSegment(text)];
        }

        if (last < text.Length)
            result.Add(new PlainSegment(text[last..]));

        return result;
    }

    /// <summary>All indexed entries, for typeahead / full search.</summary>
    public IEnumerable<XrefEntry> AllEntries()
    {
        EnsureBuilt();
        // Return only canonical name entries (avoid duplicate aliases)
        return index.Values.DistinctBy(e => e.Id);
    }
}

public record XrefEntry(string Id, string DisplayName, string Type, string Route, string Subtitle = "");

public abstract record TextSegment(string Text);
public record PlainSegment(string Text) : TextSegment(Text);
public record XrefSegment(string Text, XrefEntry Entry) : TextSegment(Text);
