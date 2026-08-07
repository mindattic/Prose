namespace Prose.Core.Models;

/// <summary>
/// A typed world-state-at-T bundle for one entity. The spine that prose generation,
/// pre-write validation, and continuity extraction all hang off of. Replaces the
/// loose "scan files, paste strings into the prompt" pattern that lets contradictions
/// slip in. Built by WorldStateService.GetDossierAsync.
/// </summary>
public sealed record Dossier(
    EntityCard Subject,
    IReadOnlyList<EntityCard> Linked,
    IReadOnlyList<EntityCard> Adjacent,
    IReadOnlyList<DossierFact> Facts,
    IReadOnlyList<ChapterEvent> Timeline,
    DerivedState Now,
    AsOfCursor AsOf
)
{
    /// <summary>Render the dossier as a structured block for LLM prompt injection.</summary>
    public string ToPromptString()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"=== DOSSIER: {Subject.Name} ({Subject.Kind}) — as of {AsOf.Describe()} ===");
        sb.AppendLine(Subject.ToBlock(indent: ""));

        if (Now.HasAny)
        {
            sb.AppendLine();
            sb.AppendLine("CURRENT STATE:");
            sb.AppendLine(Now.ToBlock(indent: "  "));
        }

        if (Linked.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("LINKED ENTITIES (1 hop — gear, partners, employers, locations):");
            foreach (var l in Linked)
                sb.AppendLine(l.ToBlock(indent: "  "));
        }

        if (Facts.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("CANONICAL FACTS:");
            foreach (var f in Facts)
                sb.AppendLine($"  • {f.Predicate}: {f.Object}{(string.IsNullOrEmpty(f.SourceLabel) ? "" : $"  [{f.SourceLabel}]")}");
        }

        if (Timeline.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("EVENT TIMELINE (chapter beats featuring this entity, in order):");
            foreach (var e in Timeline)
                sb.AppendLine($"  · Ch{e.ChapterNumber?.ToString() ?? "?"} §{e.BeatIndex} \"{e.ChapterTitle}\": {e.Snippet}");
        }

        if (Adjacent.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("ADJACENT ENTITIES (2 hop — context only, do not contradict):");
            foreach (var a in Adjacent)
                sb.AppendLine($"  - [{a.Kind}] {a.Name}{(string.IsNullOrEmpty(a.OneLine) ? "" : $" — {a.OneLine}")}");
        }

        return sb.ToString().TrimEnd();
    }
}

/// <summary>Per-entity bundle: properties at AsOf + valid-at-AsOf edges + optional rich details.</summary>
public sealed record EntityCard(
    string Id,
    string Name,
    string Kind,
    IReadOnlyDictionary<string, string> Properties,
    IReadOnlyList<EdgeRef> Edges,
    string OneLine,
    IReadOnlyList<DossierSection> Sections
)
{
    public EntityCard(string id, string name, string kind,
        IReadOnlyDictionary<string, string> properties,
        IReadOnlyList<EdgeRef> edges,
        string oneLine)
        : this(id, name, kind, properties, edges, oneLine, Array.Empty<DossierSection>()) { }

    public string ToBlock(string indent)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"{indent}[{Kind.ToUpperInvariant()}] {Name}");
        foreach (var k in PromptKeys)
            if (Properties.TryGetValue(k, out var v) && !string.IsNullOrWhiteSpace(v))
                sb.AppendLine($"{indent}  {k}: {Truncate(v, 240)}");
        if (Properties.TryGetValue("description", out var desc) && !string.IsNullOrWhiteSpace(desc))
            sb.AppendLine($"{indent}  description: {Truncate(desc, 320)}");
        if (Edges.Count > 0)
        {
            sb.AppendLine($"{indent}  relationships:");
            foreach (var e in Edges.Take(15))
                sb.AppendLine($"{indent}    {e.Direction} [{e.Relation}] {e.OtherName}{(string.IsNullOrEmpty(e.Description) ? "" : $" — {e.Description}")}");
            if (Edges.Count > 15) sb.AppendLine($"{indent}    … and {Edges.Count - 15} more");
        }
        foreach (var section in Sections)
        {
            if (section.Lines.Count == 0) continue;
            sb.AppendLine($"{indent}  {section.Title}:");
            foreach (var line in section.Lines.Take(12))
                sb.AppendLine($"{indent}    • {Truncate(line, 220)}");
            if (section.Lines.Count > 12)
                sb.AppendLine($"{indent}    … and {section.Lines.Count - 12} more");
        }
        return sb.ToString().TrimEnd();
    }

    private static readonly string[] PromptKeys =
    {
        "gender", "pronouns", "role", "status", "age", "affiliation", "location",
        "category", "manufacturer", "sector", "tier_availability", "legality"
    };

    private static string Truncate(string s, int n) => s.Length <= n ? s : s[..(n - 1)] + "…";
}

/// <summary>
/// A named bullet list rendered under an entity card. Used to surface character-only
/// fields (belongings, knowledge, conditions, psychology, behavioral) that don't fit
/// into the Properties dictionary but matter to the LLM.
/// </summary>
public sealed record DossierSection(string Title, IReadOnlyList<string> Lines);

public sealed record EdgeRef(
    string OtherId,
    string OtherName,
    string OtherKind,
    string Direction,    // "→" outgoing, "←" incoming
    string Relation,
    string? Description,
    string? ValidFrom
);

/// <summary>A factual claim from the continuity store, scoped to AsOf.</summary>
public sealed record DossierFact(
    string Predicate,
    string Object,
    string Status,        // CANONICAL or CONFIRMED
    string? SourceLabel   // "Ch3 §4 Hua's Tab" if from prose
);

/// <summary>A chapter beat in which the subject appears, replayed in order.</summary>
public sealed record ChapterEvent(
    string ChapterId,
    int? ChapterNumber,
    string ChapterTitle,
    int BeatIndex,
    string Snippet
);

/// <summary>Computed view of "what is true about this entity right now."</summary>
public sealed record DerivedState(
    string? Location,
    string? Status,
    IReadOnlyList<string> Holding,        // gear/items currently in hand
    IReadOnlyList<string> Wearing,        // apparel/equipment currently worn
    string? LastSeenChapter
)
{
    public bool HasAny =>
        !string.IsNullOrEmpty(Location)
        || !string.IsNullOrEmpty(Status)
        || Holding.Count > 0
        || Wearing.Count > 0
        || !string.IsNullOrEmpty(LastSeenChapter);

    public string ToBlock(string indent)
    {
        var sb = new System.Text.StringBuilder();
        if (!string.IsNullOrEmpty(Location)) sb.AppendLine($"{indent}location: {Location}");
        if (!string.IsNullOrEmpty(Status))   sb.AppendLine($"{indent}status: {Status}");
        if (Holding.Count > 0)               sb.AppendLine($"{indent}holding: {string.Join(", ", Holding)}");
        if (Wearing.Count > 0)               sb.AppendLine($"{indent}wearing: {string.Join(", ", Wearing)}");
        if (!string.IsNullOrEmpty(LastSeenChapter)) sb.AppendLine($"{indent}last seen: {LastSeenChapter}");
        return sb.ToString().TrimEnd();
    }

    public static DerivedState Empty { get; } = new(null, null, Array.Empty<string>(), Array.Empty<string>(), null);
}

/// <summary>
/// Story-time cursor. ChapterNumber is the canonical sort key; BeatIndex narrows
/// within a chapter. Story-point format matches WorldGraphService comparisons.
/// </summary>
public sealed record AsOfCursor(string? ChapterId, int? ChapterNumber, int? BeatIndex)
{
    public string ToStoryPoint() => ChapterNumber.HasValue ? $"chapter:{ChapterNumber}" : "";

    public string Describe() => (ChapterNumber, BeatIndex) switch
    {
        (null, null)         => "current",
        ({ } n, null)        => $"chapter {n}",
        ({ } n, { } b)       => $"chapter {n} beat {b}",
        (null, { } b)        => $"beat {b}"
    };

    public static AsOfCursor Current { get; } = new(null, null, null);
    public static AsOfCursor BeforeChapter(int n) => new(null, Math.Max(0, n - 1), null);
    public static AsOfCursor AtBeat(string chapterId, int chapterNumber, int beatIndex) => new(chapterId, chapterNumber, beatIndex);
}
