using System.Text.RegularExpressions;
using StreetSamurai.Core.Models.Canon;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Resolves entity names in free text to clickable cross-reference links.
/// Indexes all named entities (characters, places, factions, corps, synthetics,
/// technology, vocabulary). Rebuilds automatically when repos change.
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
        indexById = newIndexById;

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
                ? indexById.GetValueOrDefault(secondPart)       // [[DisplayText|id]] — resolve by ID
                  ?? index.GetValueOrDefault(secondPart)        // fallback: secondPart is a name
                : index.GetValueOrDefault(displayText);         // [[Name]] — resolve by name

            pass1.Add(entry != null
                ? new XrefSegment(displayText, entry)
                : new PlainSegment(displayText));  // unresolved → plain text

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
