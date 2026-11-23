using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Prose.Core.Services.ReadAlong;

/// <summary>
/// Reads one chapter of plain prose and finds what a story bible keeps: the names it meets, the
/// details it states about them, and the relationships between them — each with the exact sentence
/// it came from (RFC 0019 §3).
///
/// <para><b>Deterministic on purpose.</b> No LLM reads the book here: the Novel Factory's law 7
/// (deterministic checks only) applies, and a story bible the author did not write is only useful if
/// every line in it can be traced to a sentence, the same way, every time. Patterns trade recall for
/// precision: a missed detail costs nothing (the author can add it by hand); a hallucinated one would
/// cost the author's trust in every entry.</para>
///
/// <para>Pure: no database, no clock, no I/O. <see cref="ReadAlongService"/> decides what becomes a
/// draft, a fact or a continuity card.</para>
/// </summary>
public static class StoryFactExtractor
{
    // ── Inputs and outputs ──────────────────────────────────────────────────────────────────

    /// <summary>An entry the story bible already has (kept entity or pending draft).</summary>
    /// <param name="Gender">'f' or 'm' when the bible records it (a kept "gender"/"pronouns" detail), so
    /// pronouns skip the wrong character from the first sentence on.</param>
    public sealed record KnownEntry(Guid? EntityId, string Name, string Category, IReadOnlyList<string> Aliases, char? Gender = null);

    /// <summary>'f', 'm' or null for a recorded gender or pronoun value ("she/her", "female", "he", "male").</summary>
    public static char? GenderOf(string? value) => (value ?? "").Trim().ToLowerInvariant() switch
    {
        var v when v.StartsWith("she") || v is "f" or "female" or "woman" or "girl" => 'f',
        var v when v.StartsWith("he") || v is "m" or "male" or "man" or "boy" => 'm',
        _ => null,
    };

    /// <summary>One beat of plain text (entity tags and formatting already stripped).</summary>
    public sealed record Paragraph(Guid BeatId, string Text);

    /// <summary>A name met in the chapter. <see cref="IsNew"/> when no entry has it yet.</summary>
    public sealed record NameHit(string Name, string Key, string Category, Guid? EntityId, Guid BeatId, string Quote, bool IsNew, string Pattern = "");

    /// <summary>A stated detail about a subject. <see cref="ContinuityCategory"/> is which family of
    /// contradiction it can raise; <see cref="Multi"/> attributes accumulate and never conflict.</summary>
    public sealed record FactHit(
        string SubjectName, string SubjectKey, Guid? EntityId, string SubjectCategory,
        string Attribute, string Value, string Quote, Guid BeatId, int Confidence,
        string ContinuityCategory, bool Multi, bool Evidence = false, string Pattern = "");

    /// <summary>A stated relationship between two subjects.</summary>
    public sealed record Relation(string FromName, string FromKey, string Type, string ToName, string ToKey, string Quote, Guid BeatId, string Pattern = "");

    public sealed record Result(IReadOnlyList<NameHit> Names, IReadOnlyList<FactHit> Facts, IReadOnlyList<Relation> Relations);

    // ── Vocabulary ──────────────────────────────────────────────────────────────────────────

    public const string CatCharacter = "character";
    public const string CatPlace = "place";
    public const string CatItem = "item";
    public const string CatOrganization = "organization";
    public const string CatConcept = "concept";

    public static readonly IReadOnlyList<string> Categories = [CatCharacter, CatPlace, CatItem, CatOrganization, CatConcept];

    /// <summary>Attributes that hold one value at a time — two different values are a question for
    /// the author. Everything else accumulates.</summary>
    private static readonly HashSet<string> SingleValued = new(StringComparer.OrdinalIgnoreCase)
    {
        "age", "eyes", "left eye", "right eye", "hair", "scar", "born", "status", "years-ago",
        "direction", "companion", "role",
    };

    private static readonly HashSet<string> LowercaseAttributes = new(StringComparer.OrdinalIgnoreCase)
    {
        "eyes", "hair", "scar", "direction", "companion", "carries", "role",
    };

    /// <summary>One value at a time: the fixed set, every world-rule duration ("time to refill"),
    /// and the look of a possessed item ("coat": grey) — the cloak that turned black by chapter ten.</summary>
    public static bool IsSingleValued(string attribute) =>
        SingleValued.Contains(attribute)
        || attribute.StartsWith("time to ", StringComparison.Ordinal)
        || ItemAttribute.IsMatch(attribute);

    private static readonly Regex ItemAttribute = new("^" + Items + "$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private const string NumWords =
        "(?i:\\d{1,3}|(?:twenty|thirty|forty|fifty|sixty|seventy|eighty|ninety)(?:[-\\s](?:one|two|three|four|five|six|seven|eight|nine))?" +
        "|eleven|twelve|thirteen|fourteen|fifteen|sixteen|seventeen|eighteen|nineteen|ten|one|two|three|four|five|six|seven|eight|nine|a\\s+hundred|hundred)";

    private const string Colors =
        "(?:ink-black|jet-black|raven-black|steel-grey|steel-gray|ice-blue|sea-green|grey|gray|black|white|red|blue|green|brown|hazel|amber|golden|gold|silver|violet|purple|yellow|pale|auburn|copper|crimson|scarlet|blonde|blond|chestnut|ashen|ash)";

    private const string Materials = "(?:iron|steel|bronze|brass|wooden|oak|bone|leather|silk|wool|woollen|glass|stone|silver|golden)";

    private const string Items =
        "(?:staff|sword|blade|dagger|knife|bow|spear|axe|shield|helm|helmet|mask|crown|ring|amulet|pendant|locket|necklace|cloak|coat|robe|hood|scarf|gloves|boots|lamp|lantern|torch|stick|cane|book|journal|map|key|seal|stone|orb|wand|banner|horn|pipe|watch|compass|satchel|pack)";

    private const string PlaceNouns =
        "(?:gate|gates|tower|door|wall|keep|bridge|road|stair|stairs|window|hall|house|cathedral|chapel|church|temple|inn|tavern|square|market|harbor|harbour|river|city|town|village|castle|palace|camp)";

    private const string Kin =
        "(?:brother|sister|mother|father|son|daughter|wife|husband|uncle|aunt|cousin|grandmother|grandfather|twin|mentor|master|apprentice|niece|nephew|betrothed|lover)";

    private const string Feelings =
        "(?:hated|loved|feared|trusted|distrusted|served|betrayed|killed|despised|admired|married|mistrusted|resented|worshipped|followed|protected|hunted)";

    private const string NameCore = "[A-Z][\\p{L}'’-]+(?:\\s+(?:of\\s+)?[A-Z][\\p{L}'’-]+){0,3}";

    // Subject: a capitalized name or a bare pronoun (resolved to the last character on the page).
    private const string Subj = "(?<s>" + NameCore + "|[Ss]he|[Hh]e)";

    // Possessor: "Elena's", "her", "his".
    private const string Poss = "(?:(?<s>" + NameCore + ")['’]s|(?<p>[Hh]er|[Hh]is|[Tt]heir))";

    private static readonly HashSet<string> Stop = new(StringComparer.OrdinalIgnoreCase)
    {
        "I","The","A","An","He","She","It","They","We","You","His","Her","Hers","Their","Theirs","Our","Its","My","Me","Him","Them","Us",
        "But","And","Or","Nor","So","Yet","For","If","When","Then","There","Here","This","That","These","Those","What","Who","Whom","Whose",
        "Why","How","Where","Which","Yes","No","Oh","Ah","Not","Now","Once","Still","Even","Just","Only","All","Some","Any","Every","Each",
        "Nothing","Something","Everything","Anything","Someone","Somewhere","Nowhere","Everyone","Anyone","Otherwise","Perhaps","Maybe",
        "After","Before","While","Though","Although","Because","Since","Until","Unless","As","At","In","On","Of","To","From","By","With",
        "Without","Into","Onto","Over","Under","Above","Below","Behind","Beyond","Across","Through","Toward","Towards","Again","Ever",
        "Never","Always","Sometimes","Too","Very","Well","Good","Okay","OK","Sir","Madam","Lord","Lady","Chapter","Part","Volume","Book",
        "Prologue","Epilogue","Interlude","Monday","Tuesday","Wednesday","Thursday","Friday","Saturday","Sunday","January","February",
        "March","April","May","June","July","August","September","October","November","December","God","Mr","Mrs","Ms","Miss","Dr",
        "Let","Did","Do","Does","Was","Were","Is","Are","Be","Been","Had","Have","Has","Could","Would","Should","Will","Shall","Might","Must",
        "Can","Don't","Didn't","Wasn't","Isn't","Couldn't","Wouldn't","I'm","I'd","I'll","It's","That's","There's","Then","Thank","Thanks",
        "Please","Sorry","Hello","Goodbye","Come","Go","Look","Listen","Wait","Stop","Run","Enough","Later","Today","Tonight","Tomorrow",
        "Yesterday","One","Two","Three","Four","Five","Six","Seven","Eight","Nine","Ten","First","Second","Third","Last","Next","Another",
        "Other","Most","More","Less","Few","Many","Much","Both","Either","Neither","Half","Whatever","Whoever","However","Instead",
    };

    private static readonly HashSet<string> Titles = new(StringComparer.OrdinalIgnoreCase)
    {
        "Lord","Lady","Captain","King","Queen","Sir","Dame","Master","Mistress","Doctor","Dr","Father","Mother","Brother","Sister",
        "Prince","Princess","Duke","Duchess","Count","Countess","Baron","Baroness","Chancellor","General","Commander","Sergeant",
        "Lieutenant","Professor","Mr","Mrs","Ms","Miss","Emperor","Empress","Archbishop","Bishop","Saint","Uncle","Aunt",
    };

    private static char? KinGender(string rel) => rel.ToLowerInvariant() switch
    {
        "sister" or "mother" or "daughter" or "wife" or "aunt" or "grandmother" or "niece" => 'f',
        "brother" or "father" or "son" or "husband" or "uncle" or "grandfather" or "nephew" => 'm',
        _ => null,
    };

    private static readonly HashSet<string> FemaleTitles = new(StringComparer.OrdinalIgnoreCase)
        { "Lady", "Queen", "Dame", "Mistress", "Mother", "Sister", "Princess", "Duchess", "Countess", "Baroness", "Mrs", "Ms", "Miss", "Empress", "Aunt" };
    private static readonly HashSet<string> MaleTitles = new(StringComparer.OrdinalIgnoreCase)
        { "Lord", "King", "Sir", "Master", "Father", "Brother", "Prince", "Duke", "Count", "Baron", "Mr", "Emperor", "Uncle" };

    private static readonly (string Suffix, string Category)[] Suffixes =
    [
        ("Tower", CatPlace), ("Gate", CatPlace), ("Palace", CatPlace), ("Road", CatPlace), ("City", CatPlace), ("Street", CatPlace), ("Hall", CatPlace),
        ("Keep", CatPlace), ("Castle", CatPlace), ("River", CatPlace), ("Mountain", CatPlace), ("Mountains", CatPlace), ("Forest", CatPlace),
        ("Sea", CatPlace), ("Bay", CatPlace), ("Cathedral", CatPlace), ("Market", CatPlace), ("Square", CatPlace), ("Stair", CatPlace), ("Vault", CatPlace),
        ("Undervault", CatPlace), ("Citadel", CatPlace), ("Wood", CatPlace), ("Woods", CatPlace), ("Moor", CatPlace), ("Vale", CatPlace), ("Valley", CatPlace),
        ("Isle", CatPlace), ("Island", CatPlace), ("Port", CatPlace), ("Harbor", CatPlace), ("Harbour", CatPlace), ("Bridge", CatPlace), ("Inn", CatPlace),
        ("Court", CatOrganization), ("Guild", CatOrganization), ("Order", CatOrganization), ("Council", CatOrganization), ("Company", CatOrganization),
        ("Corp", CatOrganization), ("Inc", CatOrganization), ("Church", CatOrganization), ("Army", CatOrganization), ("Guard", CatOrganization),
        ("Watch", CatOrganization), ("Brotherhood", CatOrganization), ("Sisterhood", CatOrganization), ("Clan", CatOrganization), ("Legion", CatOrganization),
        ("Senate", CatOrganization), ("Crown", CatOrganization), ("Assembly", CatOrganization), ("Circle", CatOrganization),
        ("Sword", CatItem), ("Blade", CatItem), ("Staff", CatItem), ("Mask", CatItem), ("Ring", CatItem), ("Amulet", CatItem), ("Key", CatItem),
        ("Seal", CatItem), ("Seals", CatItem), ("Stone", CatItem), ("Keystone", CatItem), ("Orb", CatItem), ("Banner", CatItem), ("Lamp", CatItem),
        ("Siege", CatConcept), ("War", CatConcept), ("Sundering", CatConcept), ("Pact", CatConcept), ("Fall", CatConcept), ("Accord", CatConcept),
        ("Treaty", CatConcept), ("Rebellion", CatConcept), ("Plague", CatConcept), ("Burning", CatConcept), ("Flood", CatConcept), ("Sight", CatConcept),
        ("Exodus", CatConcept), ("Reckoning", CatConcept), ("Voice", CatConcept), ("Ward", CatConcept), ("Currents", CatConcept), ("Magic", CatConcept),
    ];

    private static readonly Regex SentenceSplit = new(@"(?<=[.!?…][""”’)]?)\s+(?=[""“‘(]?[A-Z0-9])", RegexOptions.Compiled);
    private static readonly Regex CapRun = new(@"(?<![\p{L}'’-])(?:(?<the>[Tt]he)\s+)?(?<n>[A-Z][\p{L}'’-]*(?:\s+(?:of\s+(?:the\s+)?)?[A-Z][\p{L}'’-]*)*)", RegexOptions.Compiled);

    // ── Fact patterns ───────────────────────────────────────────────────────────────────────

    private sealed record Pattern(string Attribute, Regex Rx, string Continuity, int Confidence, string? SubjectCategory = null, bool Multi = false)
    {
        /// <summary>Stable id, "<attribute>.<n>" in declaration order (RA-T3.1.1.1).</summary>
        public string Id { get; init; } = "";
    }

    private static Pattern[] Number(Pattern[] ps)
    {
        var seen = new Dictionary<string, int>();
        return ps.Select(p => p with { Id = $"{p.Attribute}.{(seen[p.Attribute] = seen.GetValueOrDefault(p.Attribute) + 1)}" }).ToArray();
    }

    /// <summary>Every pattern id the extractor can emit — fact patterns, item/rule/relation emitters and
    /// name-acceptance reasons. Disabling an id (or its family, the part before the dot) switches it off.</summary>
    public static IReadOnlyList<string> PatternIds =>
        FactPatterns.Select(p => p.Id)
            .Concat(["item.look", "item.carries", "rule.duration", "kin.1", "kin.2", "feeling.1", "member.1", "lives.1",
                     "evidence.speaks", "alias.fold", "name.known", "name.mid-sentence", "name.repeated", "name.titled",
                     "name.suffix", "name.signal"])
            .ToList();

    private static Regex R(string p) => new(p, RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Pattern[] FactPatterns = Number(
    [
        new("age", R(@"\b" + Subj + @"\s+(?:was|is|had\s+been|turned)\s+(?<v>" + NumWords + @")(?:\s+years?\s+old)?\s*(?=[.,;!?""”]|$|\s+(?:now|then|that\s+year|this\s+year|when|and\s+(?:had|was|she|he)))"), "character", 85, CatCharacter),
        new("age", R(@"\b(?<v>" + NumWords + @")-year-old\s+(?<s>" + NameCore + ")"), "character", 85, CatCharacter),
        new("age", R(@"\b(?<s>" + NameCore + @"),\s+(?:now\s+|just\s+|barely\s+)?(?<v>" + NumWords + @")(?:\s+years\s+old)?,"), "character", 70, CatCharacter),

        new("eyes", R(Poss + @"\s+eyes\s+(?:was|were|had\s+been|are|is|had\s+always\s+been)\s+(?:a\s+|the\s+)?(?:deep\s+|pale\s+|bright\s+|dark\s+)?(?<v>" + Colors + @")\b"), "character", 90, CatCharacter),
        new("eyes", R(Poss + @"\s+(?:deep\s+|pale\s+|bright\s+)?(?<v>" + Colors + @")\s+eyes\b"), "character", 80, CatCharacter),
        new("eyes", R(@"\b" + Subj + @"\s+had\s+(?:deep\s+|pale\s+|bright\s+)?(?<v>" + Colors + @")\s+eyes\b"), "character", 85, CatCharacter),
        new("hair", R(Poss + @"\s+hair\s+(?:was|had\s+been|is|had\s+gone|had\s+turned)\s+(?:a\s+)?(?<v>" + Colors + @")\b"), "character", 85, CatCharacter),
        new("hair", R(Poss + @"\s+(?:long\s+|short\s+|cropped\s+)?(?<v>" + Colors + @")\s+hair\b"), "character", 75, CatCharacter),
        new("hair", R(@"\b" + Subj + @"\s+had\s+(?:long\s+|short\s+|cropped\s+)?(?<v>" + Colors + @")\s+hair\b"), "character", 80, CatCharacter),

        new("scar", R(@"\bscar\s+(?:that\s+)?(?:ran\s+|cut\s+|split\s+)?(?:over|on|across|above|below|through|under|beneath)\s+" + Poss + @"\s+(?<v>(?:left|right)\s+(?:eye|cheek|hand|arm|brow|jaw|temple|ear|wrist))"), "character", 90, CatCharacter),
        new("scar", R(Poss + @"\s+(?<v>(?:left|right)\s+(?:eye|cheek|hand|arm|brow|jaw|temple|ear|wrist))\s+(?:was\s+|had\s+been\s+)?(?:scarred|bore\s+a\s+scar|was\s+marked\s+by\s+a\s+scar)"), "character", 85, CatCharacter),

        new("role", R(@"\b(?<s>" + NameCore + @"),\s+the\s+(?<v>[a-z][a-z-]+(?:\s+(?:of\s+(?:the\s+)?)?[a-z][a-z-]+){0,3}),"), "character", 60, CatCharacter),
        new("born", R(@"\b" + Subj + @"\s+(?:had\s+been\s+|was\s+)?born\s+(?:in|at|near|within\s+a\s+mile\s+of|outside)\s+(?<v>(?:the\s+)?[A-Z][\p{L}'’-]+(?:\s+[A-Z][\p{L}'’-]+)*)"), "character", 80, CatCharacter),
        new("status", R(@"\b" + Subj + @"\s+(?:was|lay)\s+(?<v>dead)\b"), "character", 80, CatCharacter),
        new("status", R(@"\b" + Subj + @"\s+(?<v>died|had\s+died|was\s+killed|had\s+been\s+killed|was\s+slain)\b"), "character", 80, CatCharacter),
        new("companion", R(@"\b" + Subj + @"\s+(?:had\s+)?(?:came|come|went|gone|arrived|travelled|traveled|returned)\s+(?<v>alone)\b"), "character", 70, CatCharacter),
        new("capability", R(@"\b" + Subj + @"\s+had\s+(?<v>the\s+[A-Z][\p{L}'’-]+)"), "world-rule", 60, CatCharacter, Multi: true),
        new("alias", R(@"\b(?<s>" + NameCore + @"),?\s+(?:known\s+as|called|named|whom\s+they\s+called)\s+(?<v>(?:the\s+)?[A-Z][\p{L}'’-]+(?:\s+[A-Z][\p{L}'’-]+)*)"), "character", 75, null, Multi: true),

        new("years-ago", R(@"\b(?<v>" + NumWords + @")\s+years\s+(?:since|after)\s+(?<s>the\s+[A-Z][\p{L}'’-]+(?:\s+(?:of\s+)?[A-Z][\p{L}'’-]+)*)"), "timeline", 80, CatConcept),
        new("years-ago", R(@"\b(?<s>the\s+[A-Z][\p{L}'’-]+(?:\s+(?:of\s+)?[A-Z][\p{L}'’-]+)*)\s*,?\s+(?:was\s+|had\s+been\s+|happened\s+|ended\s+|fell\s+)?(?<v>" + NumWords + @")\s+years\s+(?:ago|before|earlier)"), "timeline", 80, CatConcept),

        new("direction", R(@"\b(?<s>(?:[Tt]he\s+)" + PlaceNouns + @"|[A-Z][\p{L}'’-]+(?:\s+[A-Z][\p{L}'’-]+)*)\s+(?:faced|faces|opened|opens|looked|looks)\s+(?:to\s+the\s+|toward\s+the\s+|towards\s+the\s+)?(?<v>north|south|east|west|northeast|northwest|southeast|southwest)\b"), "place", 85, CatPlace),
    ]);

    // Possessed items: "her grey cloak", "Elena's staff". Attribute = the item; value = its look.
    private static readonly Regex ItemLook = R(Poss + @"\s+(?:(?<adj>" + Colors + "|" + Materials + @")\s+)(?<item>" + Items + @")\b");
    private static readonly Regex ItemLook2 = R(Poss + @"\s+(?<item>" + Items + @")\s+(?:was|were|had\s+been|had\s+turned)\s+(?<adj>" + Colors + "|" + Materials + @")\b");
    private static readonly Regex ItemOwned = R(Poss + @"\s+(?<item>" + Items + @")\b");

    // World rules: "the staff needs three months to refill".
    private static readonly Regex Duration = R(@"\b(?:(?<s>[Tt]he\s+" + Items + @"|[Hh]er\s+" + Items + @"|[Hh]is\s+" + Items + @"|" + NameCore + @"))\s+(?:needs|needed|takes|took|requires|required|would\s+need|would\s+take)\s+(?<v>" + NumWords + @"\s+(?:hours?|days?|weeks?|months?|years?|nights?))\s+to\s+(?<verb>[a-z]+)");

    private static readonly Regex KinA = R(Poss + @"\s+(?<rel>" + Kin + @"),?\s+(?<o>" + NameCore + ")");
    private static readonly Regex KinB = R(@"\b(?<o>" + NameCore + @"),\s+" + Poss + @"\s+(?<rel>" + Kin + @")\b");
    private static readonly Regex Feel = R(@"\b" + Subj + @"\s+(?:had\s+(?:always\s+)?)?(?<rel>" + Feelings + @")\s+(?<o>(?:the\s+)?" + NameCore + ")");
    private static readonly Regex Member = R(@"\b(?<s>" + NameCore + @"),?\s+(?:a|an|the)\s+(?:member|agent|knight|soldier|servant|captain|officer|envoy|spy|priest|priestess|scholar|archivist)\s+of\s+(?<o>(?:the\s+)?" + NameCore + ")");
    private static readonly Regex Lives = R(@"\b" + Subj + @"\s+(?:lived|lives|ruled|rules|dwelt|served)\s+(?:in|at|over)\s+(?<o>(?:the\s+)?" + NameCore + ")");
    private static readonly Regex Speaks = R(@"(?:\b(?:said|asked|replied|whispered|shouted|answered|muttered|called)\s+(?<s>" + NameCore + @")\b|\b(?<s2>" + NameCore + @")\s+(?:said|asked|replied|whispered|shouted|answered|muttered|nodded|smiled|laughed)\b)");

    private static readonly Regex CharacterSignal = R(@"^\s*(?:[a-z]{2,}ed\b|said|asked|replied|whispered|shouted|answered|muttered|nodded|smiled|laughed|frowned|sighed|turned|looked|crossed|walked|ran|stood|sat|watched|waited|stepped|moved|leaned|paused|knelt|arrived|talked|spoke|returned|knew|thought|felt|wanted|reached|lowered|raised|shook|grinned|wept|cried|swore|drew|held|took|gave|came|went|had|was)\b");
    private static readonly Regex PlacePreposition = R(@"\b(?:in|at|into|from|toward|towards|through|across|to|near|inside|outside|beyond|reached|entered|left)\s+(?:the\s+)?$");

    // ── Entry point ─────────────────────────────────────────────────────────────────────────

    /// <summary>Read one chapter. <paramref name="known"/> is everything the bible already holds
    /// (kept entries and pending drafts); <paramref name="tossedKeys"/> are subjects the author said
    /// "Not quite" to — they are never raised again.</summary>
    public static Result Extract(
        IReadOnlyList<Paragraph> paragraphs,
        IReadOnlyList<KnownEntry> known,
        IReadOnlySet<string>? tossedKeys = null,
        IReadOnlySet<string>? disabledPatterns = null)
    {
        tossedKeys ??= new HashSet<string>();
        var disabled = disabledPatterns ?? new HashSet<string>();
        bool Off(string id) => disabled.Contains(id) || (id.Contains('.') && disabled.Contains(id[..id.IndexOf('.')]));
        IEnumerable<Match> On(string id, Regex rx, string input) => Off(id) ? [] : rx.Matches(input);

        // Name index: key → entry, plus every alias and every first name of a multi-word character.
        var index = new Dictionary<string, KnownEntry>(StringComparer.Ordinal);
        foreach (var k in known)
        {
            foreach (var n in new[] { k.Name }.Concat(k.Aliases))
            {
                var key = Key(n);
                if (key.Length > 0) index.TryAdd(key, k);
            }
        }
        foreach (var k in known.Where(k => k.Category == CatCharacter))
        {
            var parts = StripArticle(k.Name).Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 1 && !Titles.Contains(parts[0]) && !Stop.Contains(parts[0]))
                index.TryAdd(Key(parts[0]), k);
        }

        // Pass 1: sentences, and every capitalized run with its context.
        var sentences = new List<(Guid BeatId, string Text, int Paragraph)>();
        for (var p = 0; p < paragraphs.Count; p++)
            foreach (var s in SplitSentences(paragraphs[p].Text))
                sentences.Add((paragraphs[p].BeatId, s, p));

        var lowerWords = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (_, text, _) in sentences)
            foreach (Match w in Regex.Matches(text, @"(?<![\p{L}'’])[a-z][\p{L}'’-]*"))
                lowerWords.Add(w.Value);

        var candidates = new Dictionary<string, Candidate>(StringComparer.Ordinal);
        var names = new List<NameHit>();

        foreach (var (beatId, text, _) in sentences)
        {
            foreach (Match m in CapRun.Matches(text))
            {
                var raw = m.Groups["n"].Value.TrimEnd('\'', '’', '-');
                if (raw.EndsWith("'s", StringComparison.Ordinal) || raw.EndsWith("’s", StringComparison.Ordinal)) raw = raw[..^2];
                if (raw.Length < 2) continue;

                var atStart = IsSentenceStart(text, m.Index);
                var hasThe = m.Groups["the"].Success;
                var words = raw.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                // "Then Elena" at sentence start: drop leading stop words.
                while (words.Length > 0 && Stop.Contains(words[0]) && !Titles.Contains(words[0])) { words = words[1..]; atStart = false; }
                if (words.Length == 0) continue;
                raw = string.Join(' ', words);

                var display = hasThe && !Titles.Contains(words[0]) ? "the " + raw : raw;
                var key = Key(display);
                if (key.Length < 2) continue;

                if (index.TryGetValue(key, out var hit) || (words.Length > 1 && index.TryGetValue(Key(words[^1]), out hit) && Titles.Contains(words[0])))
                {
                    if (!Off("name.known")) names.Add(new NameHit(hit.Name, Key(hit.Name), hit.Category, hit.EntityId, beatId, text, false, "name.known"));
                    continue;
                }

                if (words.Length == 1)
                {
                    if (Stop.Contains(words[0]) || Titles.Contains(words[0])) continue;
                    if (lowerWords.Contains(words[0].ToLowerInvariant())) continue; // "Frost lay…" / "frost" elsewhere
                }
                if (tossedKeys.Contains(key)) continue;

                if (!candidates.TryGetValue(key, out var c))
                    candidates[key] = c = new Candidate(display, key);
                c.Count++;
                if (!atStart) c.MidSentence++;
                c.Occurrences.Add((beatId, text, m.Index, m.Length));
                ScoreCategory(c, text, m.Index, m.Length, words);
            }
        }

        // Candidates the chapter actually establishes as names.
        var accepted = new Dictionary<string, Candidate>(StringComparer.Ordinal);
        foreach (var c in candidates.Values)
        {
            // The first reason that holds and is not switched off is the name's pattern.
            string? reason = new (bool Holds, string Id)[]
            {
                (c.MidSentence >= 1, "name.mid-sentence"), (c.Count >= 2, "name.repeated"), (c.Titled, "name.titled"),
                (c.SuffixCategory is not null, "name.suffix"), (c.CharacterScore > 0, "name.signal"),
            }.FirstOrDefault(x => x.Holds && !Off(x.Id)).Id;
            if (reason is null) continue;
            c.Reason = reason;
            accepted[c.Key] = c;
        }
        // "Hollis" beside "Hollis Marsh", "Aldous" beside "Captain Aldous": one person, not two. A
        // single word that is the first or last word of an established multi-word character name
        // folds into it (its occurrences count for the full name, and it is kept as an alias).
        var aliasOf = new Dictionary<string, Candidate>(StringComparer.Ordinal);
        var multiNames = accepted.Values.Where(c => c.Display.Contains(' ') && c.Category == CatCharacter).ToList();
        foreach (var single in accepted.Values.Where(c => !c.Display.Contains(' ')).ToList())
        {
            var parent = multiNames.FirstOrDefault(m =>
            {
                var w = StripArticle(m.Display).Split(' ', StringSplitOptions.RemoveEmptyEntries);
                return w[0].Equals(single.Display, StringComparison.Ordinal) && !Titles.Contains(w[0])
                       || w[^1].Equals(single.Display, StringComparison.Ordinal);
            });
            if (parent is null) continue;
            aliasOf[single.Key] = parent;
            accepted.Remove(single.Key);
            parent.Occurrences.AddRange(single.Occurrences);
            parent.Aliases.Add((single.Display, single.Occurrences[0].Sentence, single.Occurrences[0].BeatId));
        }
        // The same for entries the bible already has: "Aldous" alone means "Captain Aldous".
        foreach (var k in known.Where(k => k.Category == CatCharacter))
        {
            var parts = StripArticle(k.Name).Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 1 && Titles.Contains(parts[0])) index.TryAdd(Key(parts[^1]), k);
        }

        foreach (var c in accepted.Values)
            foreach (var o in c.Occurrences)
                names.Add(new NameHit(c.Display, c.Key, c.Category, null, o.BeatId, o.Sentence, true, c.Reason));

        // Pass 2: facts and relations, with pronouns resolved to the last character on the page.
        var facts = new List<FactHit>();
        var relations = new List<Relation>();
        (string Name, string Key, Guid? Id)? lastCharacter = null;
        (string Name, string Key, Guid? Id)? previousCharacter = null;
        var gender = new Dictionary<string, char>(StringComparer.Ordinal);
        var namedInLast = new List<string>();
        (string Name, string Key, Guid? Id, string Category)? lastGroup = null;
        foreach (var k in known)
            if (k.Gender is { } kg) gender.TryAdd(Key(k.Name), kg);
        var lastParagraph = -1;
        var paragraphNamedCharacter = false;

        Subject? Resolve(string? raw, bool pronoun)
        {
            if (pronoun)
                return lastCharacter is { } lc ? new Subject(lc.Name, lc.Key, lc.Id, CatCharacter, true) : null;
            if (string.IsNullOrWhiteSpace(raw)) return null;
            raw = raw.Trim();
            if (raw.EndsWith("'s", StringComparison.Ordinal) || raw.EndsWith("’s", StringComparison.Ordinal)) raw = raw[..^2];
            var words = raw.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            while (words.Length > 1 && Stop.Contains(words[0]) && !Titles.Contains(words[0]) && !words[0].Equals("the", StringComparison.OrdinalIgnoreCase)) words = words[1..];
            raw = string.Join(' ', words);
            var key = Key(raw);
            if (key.Length < 2 || tossedKeys.Contains(key)) return null;
            if (index.TryGetValue(key, out var k)) return new Subject(k.Name, Key(k.Name), k.EntityId, k.Category, false);
            if (accepted.TryGetValue(key, out var c)) return new Subject(c.Display, c.Key, null, c.Category, false);
            if (aliasOf.TryGetValue(key, out var ac)) return new Subject(ac.Display, ac.Key, null, ac.Category, false);
            if (words.Length == 1 && (Stop.Contains(words[0]) || lowerWords.Contains(words[0].ToLowerInvariant()))) return null;
            return null; // an unestablished capitalized word is not a subject
        }

        foreach (var (beatId, text, para) in sentences)
        {
            // A gap, or a whole paragraph that named no character, breaks the thread: "She" after it is no one's.
            if (para != lastParagraph)
            {
                if (para - lastParagraph > 1 || (lastParagraph >= 0 && !paragraphNamedCharacter)) { lastCharacter = null; previousCharacter = null; }
                paragraphNamedCharacter = false;
            }
            lastParagraph = para;

            // Pronoun antecedent BEFORE this sentence's own facts, so "She was twenty-eight." after
            // "Elena crossed…" resolves to Elena; then update from this sentence's names.
            void Add(Subject s, string attr, string value, int confidence, string continuity, bool multi, string? subjectCategory, bool evidence = false, string pid = "")
            {
                if (pid.Length > 0 && Off(pid)) return;
                value = NormalizeValue(attr, value);
                if (value.Length == 0 || value.Length > 200) return;
                var cat = s.Category;
                if (subjectCategory is not null && s.EntityId is null && !index.ContainsKey(s.Key) && cat == CatConcept)
                    cat = subjectCategory;
                facts.Add(new FactHit(s.Name, s.Key, s.EntityId, cat, attr, value, text, beatId,
                    Math.Clamp(confidence - (s.ViaPronoun ? 15 : 0), 5, 99), continuity, multi || !IsSingleValued(attr), evidence, pid));
            }

            foreach (var p in FactPatterns)
            {
                foreach (Match m in p.Rx.Matches(text))
                {
                    var subj = ResolveMatch(m);
                    if (subj is null) continue;
                    if (p.Attribute == "years-ago" || p.Attribute == "direction")
                    {
                        var raw = m.Groups["s"].Value;
                        subj = Resolve(raw, false) ?? NewThing(raw, p.SubjectCategory ?? CatConcept, tossedKeys);
                        if (subj is null) continue;
                    }
                    Add(subj, p.Attribute, m.Groups["v"].Value, p.Confidence, p.Continuity, p.Multi, p.SubjectCategory, pid: p.Id);
                }
            }

            foreach (Match m in ItemLook.Matches(text).Concat(ItemLook2.Matches(text)))
            {
                var owner = ResolveMatch(m);
                if (owner is null) continue;
                Add(owner, m.Groups["item"].Value.ToLowerInvariant(), m.Groups["adj"].Value, 75, "description", false, null, pid: "item.look");
            }
            foreach (Match m in ItemOwned.Matches(text))
            {
                var owner = ResolveMatch(m);
                if (owner is null || owner.Category != CatCharacter) continue;
                Add(owner, "carries", m.Groups["item"].Value.ToLowerInvariant(), 60, "item", true, null, pid: "item.carries");
            }
            foreach (Match m in Duration.Matches(text))
            {
                var rawS = m.Groups["s"].Value;
                var thing = Regex.Replace(rawS, @"^(?:[Tt]he|[Hh]er|[Hh]is)\s+", "");
                var subj = Resolve(rawS, false) ?? Resolve("the " + thing, false) ?? NewThing("the " + thing.ToLowerInvariant(), CatItem, tossedKeys);
                if (subj is null) continue;
                Add(subj, "time to " + m.Groups["verb"].Value.ToLowerInvariant(), m.Groups["v"].Value, 80, "world-rule", false, CatItem, pid: "rule.duration");
            }

            foreach (Match m in On("kin.1", KinA, text).Concat(On("kin.2", KinB, text)))
            {
                var a = ResolveMatch(m);
                var b = Resolve(m.Groups["o"].Value, false);
                if (a is null || b is null || a.Key == b.Key) continue;
                relations.Add(new Relation(a.Name, a.Key, m.Groups["rel"].Value.ToLowerInvariant(), b.Name, b.Key, text, beatId, "kin.1"));
                // "Wren's brother, Hollis" says Hollis is male (RA-T3.3.1.1).
                if (KinGender(m.Groups["rel"].Value) is { } kg2) gender.TryAdd(b.Key, kg2);
                Add(a, m.Groups["rel"].Value.ToLowerInvariant(), b.Name, 80, "character", true, CatCharacter, pid: "kin.1");
            }
            foreach (Match m in On("feeling.1", Feel, text))
            {
                var a = ResolveMatch(m);
                var b = Resolve(m.Groups["o"].Value, false);
                if (a is null || b is null || a.Key == b.Key) continue;
                relations.Add(new Relation(a.Name, a.Key, m.Groups["rel"].Value.ToLowerInvariant(), b.Name, b.Key, text, beatId, "feeling.1"));
            }
            foreach (Match m in On("member.1", Member, text).Concat(On("lives.1", Lives, text)))
            {
                var a = ResolveMatch(m);
                var b = Resolve(m.Groups["o"].Value, false);
                if (a is null || b is null || a.Key == b.Key) continue;
                var type = m.Value.Contains(" of ", StringComparison.Ordinal) && !Regex.IsMatch(m.Value, @"\b(?:lived|lives|ruled|rules|dwelt|served)\b") ? "member of" : "lives in";
                relations.Add(new Relation(a.Name, a.Key, type, b.Name, b.Key, text, beatId, type == "member of" ? "member.1" : "lives.1"));
            }
            foreach (Match m in Speaks.Matches(text))
            {
                var raw = m.Groups["s"].Success ? m.Groups["s"].Value : m.Groups["s2"].Value;
                var subj = Resolve(raw, false);
                if (subj is null || subj.Category != CatCharacter) continue;
                // Evidence only: never a draft, never shown — it exists to meet a "dead" fact.
                Add(subj, "status", "acts on the page", 50, "character", false, CatCharacter, evidence: true, pid: "evidence.speaks");
            }

            // Gender, learned from the prose: a sentence that opens "She…"/"He…" right after a
            // sentence naming exactly one character tells us that character's pronoun.
            var startPronoun = Regex.Match(text, @"^[""“‘]?(She|He)\b");
            if (startPronoun.Success && namedInLast.Count == 1)
                gender.TryAdd(namedInLast[0], startPronoun.Groups[1].Value == "She" ? 'f' : 'm');

            // "Wren had her own reasons": a possessive "own" after exactly one named character (RA-T3.3.1.1).
            var own = Regex.Match(text, @"\b(her|his)\s+own\b", RegexOptions.IgnoreCase);
            if (own.Success)
            {
                var before = CapRun.Matches(text[..own.Index]).Cast<Match>()
                    .Select(m => Resolve(m.Groups["n"].Value, false)).Where(x => x is { Category: CatCharacter }).Select(x => x!.Key).Distinct().ToList();
                if (before.Count == 1) gender.TryAdd(before[0], own.Groups[1].Value.ToLowerInvariant() == "her" ? 'f' : 'm');
            }

            // Who "she" means in the next sentence: the last character named in this one.
            namedInLast.Clear();
            foreach (Match m in CapRun.Matches(text))
            {
                var s = Resolve(m.Groups["n"].Value, false);
                if (s is not null && (s.Category == CatOrganization || (s.Key.EndsWith('s') && m.Groups["the"].Success && s.Category != CatCharacter)))
                    lastGroup = (s.Name, s.Key, s.EntityId, s.Category);
                if (s is null || s.Category != CatCharacter) continue;
                var words = m.Groups["n"].Value.Split(' ');
                if (FemaleTitles.Contains(words[0])) gender.TryAdd(s.Key, 'f');
                else if (MaleTitles.Contains(words[0])) gender.TryAdd(s.Key, 'm');
                if (!namedInLast.Contains(s.Key)) namedInLast.Add(s.Key);
                paragraphNamedCharacter = true;
                if (lastCharacter is { } prev && prev.Key != s.Key) previousCharacter = prev;
                lastCharacter = (s.Name, s.Key, s.EntityId);
            }

            Subject? ResolveMatch(Match m)
            {
                if (m.Groups["p"].Success) return ResolvePronoun(m.Groups["p"].Value, m.Groups["p"].Index);
                var raw = m.Groups["s"].Value;
                if (raw.Length == 0) return null;
                if (raw is "She" or "she" or "He" or "he") return ResolvePronoun(raw, m.Groups["s"].Index);
                return Resolve(raw, false);
            }

            // "Wren pulled her coat tight": the nearest character named earlier in the SAME sentence
            // wins; only a sentence that names no one reaches back to the last sentence's character.
            // A character whose gender the prose has already shown is skipped for the other pronoun.
            Subject? ResolvePronoun(string pronoun, int at)
            {
                // "their" is a group's, never one character's (RA-T3.3.1.2): the nearest organization or
                // plural group earlier in the sentence, else the last one on the page.
                if (pronoun.Equals("their", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var cand in CapRun.Matches(text[..at]).Cast<Match>().Reverse())
                    {
                        var c = Resolve(cand.Groups["n"].Value, false);
                        if (c is not null && (c.Category == CatOrganization || (c.Key.EndsWith('s') && c.Category != CatCharacter)))
                            return c with { ViaPronoun = true };
                    }
                    return lastGroup is { } lg0 ? new Subject(lg0.Name, lg0.Key, lg0.Id, lg0.Category, true) : null;
                }
                var g = pronoun.ToLowerInvariant() is "she" or "her" ? 'f' : 'm';
                // Candidates by recency: earlier in this sentence (nearest first), then the last two
                // characters on the page. A candidate KNOWN to match the pronoun wins over a nearer one of
                // unknown gender; a known mismatch is never chosen.
                var cands = new List<Subject>();
                foreach (var cand in CapRun.Matches(text[..at]).Cast<Match>().Reverse())
                    if (Resolve(cand.Groups["n"].Value, false) is { Category: CatCharacter } c && cands.All(x => x.Key != c.Key)) cands.Add(c);
                if (lastCharacter is { } lc && cands.All(x => x.Key != lc.Key)) cands.Add(new Subject(lc.Name, lc.Key, lc.Id, CatCharacter, false));
                if (previousCharacter is { } pc && cands.All(x => x.Key != pc.Key)) cands.Add(new Subject(pc.Name, pc.Key, pc.Id, CatCharacter, false));
                var pick = cands.FirstOrDefault(x => gender.TryGetValue(x.Key, out var xg) && xg == g)
                        ?? cands.FirstOrDefault(x => !gender.ContainsKey(x.Key));
                return pick is null ? null : pick with { ViaPronoun = true };
            }
        }

        foreach (var c in accepted.Values)
            foreach (var (alias, quote, beatId) in c.Aliases)
                if (!Off("alias.fold")) facts.Add(new FactHit(c.Display, c.Key, null, c.Category, "alias", alias, quote, beatId, 70, "character", true, Pattern: "alias.fold"));

        return new Result(names, Dedupe(facts), relations);
    }

    private static List<FactHit> Dedupe(List<FactHit> facts) =>
        facts.GroupBy(f => (f.SubjectKey, f.Attribute, f.Value, f.BeatId))
             .Select(g => g.OrderByDescending(f => f.Confidence).First())
             .ToList();

    private static Subject? NewThing(string raw, string category, IReadOnlySet<string> tossed)
    {
        var key = Key(raw);
        if (key.Length < 2 || tossed.Contains(key)) return null;
        var display = raw.Trim();
        if (display.StartsWith("The ", StringComparison.Ordinal)) display = "the " + display[4..];
        return new Subject(display, key, null, category, false);
    }

    private sealed record Subject(string Name, string Key, Guid? EntityId, string Category, bool ViaPronoun);

    private sealed class Candidate(string display, string key)
    {
        public string Display { get; } = display;
        public string Key { get; } = key;
        public int Count { get; set; }
        public int MidSentence { get; set; }
        public int CharacterScore { get; set; }
        public string Reason { get; set; } = "";
        public int PlaceScore { get; set; }
        public string? SuffixCategory { get; set; }
        public bool Titled { get; set; }
        public List<(Guid BeatId, string Sentence, int Index, int Length)> Occurrences { get; } = [];
        public List<(string Alias, string Quote, Guid BeatId)> Aliases { get; } = [];

        public string Category =>
            Titled ? CatCharacter
            : SuffixCategory is not null ? SuffixCategory
            : Display.StartsWith("the ", StringComparison.Ordinal) && Display.EndsWith('s') && !Display.EndsWith("ss") ? CatOrganization
            : CharacterScore > 0 && CharacterScore >= PlaceScore ? CatCharacter
            : PlaceScore > 0 ? CatPlace
            : Display.StartsWith("the ", StringComparison.Ordinal) && Display.EndsWith('s') && !Display.EndsWith("ss") ? CatOrganization
            : Display.StartsWith("the ", StringComparison.Ordinal) ? CatConcept
            : CatCharacter;
    }

    private static void ScoreCategory(Candidate c, string sentence, int index, int length, string[] words)
    {
        if (Titles.Contains(words[0]) && words.Length > 1) c.Titled = true;
        foreach (var (suffix, cat) in Suffixes)
            if (words[^1].Equals(suffix, StringComparison.Ordinal)) { c.SuffixCategory = cat; break; }

        var after = sentence[(index + length)..];
        var matched = sentence.Substring(index, length);
        if (matched.EndsWith("'s", StringComparison.Ordinal) || matched.EndsWith("’s", StringComparison.Ordinal)) after = "'s" + after;
        var before = sentence[..index];
        if (CharacterSignal.IsMatch(after) || Regex.IsMatch(after, @"^['’]s\s+(?:eyes|hair|face|hand|hands|voice|mother|father|brother|sister)\b")) c.CharacterScore++;
        if (Regex.IsMatch(before, @"\b(?:said|asked|told|replied|met|found|watched|followed)\s*$")) c.CharacterScore++;
        if (Regex.IsMatch(before, @"\b(?:spoke|said|talked|whispered|nodded|turned|wrote|called|lied)\s+to\s+$")) { c.CharacterScore += 2; }
        if (PlacePreposition.IsMatch(before)) c.PlaceScore++;
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────────────────

    /// <summary>Split prose into sentences, keeping closing quotes with their sentence.</summary>
    public static IEnumerable<string> SplitSentences(string text)
    {
        foreach (var line in (text ?? "").Split('\n'))
            foreach (var s in SentenceSplit.Split(line.Trim()))
                if (!string.IsNullOrWhiteSpace(s)) yield return s.Trim();
    }

    private static bool IsSentenceStart(string sentence, int index)
    {
        var prefix = sentence[..index].TrimEnd();
        return prefix.Length == 0 || prefix.EndsWith('"') || prefix.EndsWith('“') || prefix.EndsWith('‘') || prefix.EndsWith('(')
               || prefix.EndsWith(':') || prefix.EndsWith('—') && prefix.Length < 3;
    }

    /// <summary>The identity of a name: lowercase, no leading article, single spaces, straight
    /// apostrophes. "The Siege" and "the siege" are one subject.</summary>
    public static string Key(string name)
    {
        var s = StripArticle((name ?? "").Trim()).Replace('’', '\'').ToLowerInvariant();
        return Regex.Replace(s, @"\s+", " ").Trim();
    }

    private static string StripArticle(string s) =>
        s.StartsWith("the ", StringComparison.OrdinalIgnoreCase) ? s[4..].Trim() : s;

    private static readonly Dictionary<string, int> Units = new(StringComparer.OrdinalIgnoreCase)
    {
        ["one"] = 1, ["two"] = 2, ["three"] = 3, ["four"] = 4, ["five"] = 5, ["six"] = 6, ["seven"] = 7, ["eight"] = 8, ["nine"] = 9,
        ["ten"] = 10, ["eleven"] = 11, ["twelve"] = 12, ["thirteen"] = 13, ["fourteen"] = 14, ["fifteen"] = 15, ["sixteen"] = 16,
        ["seventeen"] = 17, ["eighteen"] = 18, ["nineteen"] = 19, ["twenty"] = 20, ["thirty"] = 30, ["forty"] = 40, ["fifty"] = 50,
        ["sixty"] = 60, ["seventy"] = 70, ["eighty"] = 80, ["ninety"] = 90, ["hundred"] = 100,
    };

    /// <summary>"twenty-eight" → 28, "a hundred" → 100, "12" → 12; null when not a number.</summary>
    public static int? ParseNumber(string s)
    {
        s = (s ?? "").Trim().ToLowerInvariant();
        if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n)) return n;
        if (s is "a hundred" or "hundred") return 100;
        var total = 0;
        foreach (var part in s.Split(['-', ' '], StringSplitOptions.RemoveEmptyEntries))
        {
            if (!Units.TryGetValue(part, out var v)) return null;
            total += v;
        }
        return total > 0 ? total : null;
    }

    /// <summary>One spelling per value, so "gray"/"grey" and "twenty-eight"/"28" never look like a
    /// contradiction.</summary>
    public static string NormalizeValue(string attribute, string value)
    {
        var v = Regex.Replace((value ?? "").Trim().Trim(',', '.', ';'), @"\s+", " ");
        var lower = v.ToLowerInvariant();
        switch (attribute)
        {
            case "age":
                return ParseNumber(lower)?.ToString(CultureInfo.InvariantCulture) ?? "";
            case "years-ago":
                return ParseNumber(lower)?.ToString(CultureInfo.InvariantCulture) ?? "";
            case "status":
                return lower.Contains("acts") ? "acts on the page" : "dead";
            case "born":
            case "capability":
            case "alias":
                return v.StartsWith("The ", StringComparison.Ordinal) ? "the " + v[4..] : v;
        }
        if (attribute.StartsWith("time to ", StringComparison.Ordinal))
        {
            var m = Regex.Match(lower, @"^(.+?)\s+(hours?|days?|weeks?|months?|years?|nights?)$");
            if (m.Success && ParseNumber(m.Groups[1].Value) is { } n)
            {
                var unit = m.Groups[2].Value.TrimEnd('s');
                return $"{n} {unit}{(n == 1 ? "" : "s")}";
            }
            return lower;
        }
        // Names keep their case ("brother: Hollis Marsh"); descriptions are normalized to lowercase.
        if (!LowercaseAttributes.Contains(attribute) && !Regex.IsMatch(attribute, "^" + Items + "$")) return v;
                lower = lower switch
        {
            "gray" => "grey", "blonde" => "blond", "golden" => "gold", "steel-gray" => "steel-grey",
            "woollen" => "wool", "wooden" => "wood",
            _ => lower,
        };
        return lower;
    }

    /// <summary>Whether two values of one attribute disagree, and how sure that is (0 = they agree).
    /// <paramref name="kind"/> is <c>updated</c> when the new value can be the story moving on (an
    /// age that went up, a role that changed), <c>contradiction</c> when both cannot hold.</summary>
    public static (int Confidence, string Kind) Compare(string attribute, string existing, string proposed, int baseConfidence, bool proposedIsLater)
    {
        if (string.Equals(existing, proposed, StringComparison.OrdinalIgnoreCase)) return (0, "");

        switch (attribute)
        {
            case "age":
            {
                if (ParseNumber(existing) is { } a && ParseNumber(proposed) is { } b)
                {
                    if (proposedIsLater && b > a && b - a <= 10) return (Math.Min(baseConfidence, 55), "updated");
                    return (baseConfidence, "contradiction");
                }
                break;
            }
            case "years-ago":
            {
                if (ParseNumber(existing) is { } a && ParseNumber(proposed) is { } b)
                {
                    if (proposedIsLater && b > a && b - a <= 2) return (Math.Min(baseConfidence, 50), "updated");
                    return (baseConfidence, "contradiction");
                }
                break;
            }
            case "status":
                // "dead" then acting on the page: a ghost, a flashback, or a mistake. Only that pair.
                if (existing == "dead" && proposed == "acts on the page" && proposedIsLater) return (55, "contradiction");
                return (0, "");
            case "role":
                return (Math.Min(baseConfidence, 50), "updated");
        }
        return (baseConfidence, "contradiction");
    }

    /// <summary>Human words for an attribute key, for cards and entries.</summary>
    public static string Label(string attribute) => attribute switch
    {
        "years-ago" => "years ago",
        "carries" => "carries",
        _ => attribute,
    };

    /// <summary>A deterministic "Explain": why the read-along thinks these two lines disagree.</summary>
    public static string Explain(string entity, string attribute, string existing, string existingChapter, string proposed, string proposedChapter, string kind)
    {
        var sb = new StringBuilder();
        sb.Append($"{existingChapter} records {entity}'s {Label(attribute)} as \"{existing}\"; {proposedChapter} says \"{proposed}\". ");
        sb.Append(kind == "updated"
            ? "The later value can be the story moving on (time passing, a change of role). If it is, Accept new; if the earlier line was right, Keep existing."
            : "Both cannot be true at once. Accept new if the later line is the story you are telling, Keep existing if the earlier one is, Retcon if the change is deliberate and the earlier line should stand as history, or Dismiss if both are right in context.");
        if (attribute == "status")
            sb.Append(" A character recorded as dead speaks or acts in the later chapter: a flashback, a ghost, a vision, or a slip.");
        return sb.ToString();
    }
}
