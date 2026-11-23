using Prose.Core.Services.ReadAlong;
using static Prose.Core.Services.ReadAlong.StoryFactExtractor;

namespace Prose.UnitTests;

/// <summary>
/// RA-S3.3.1: thirty pronoun cases. Each names the paragraphs, the detail to look for, and who it must land
/// on (null = the read-along must NOT attach it to anyone, because the prose does not say who).
/// </summary>
[TestFixture]
public class PronounFixture
{
    public sealed record Case(string Name, string[] Paragraphs, string Attribute, string? Expected, KnownEntry[]? Known = null)
    {
        public override string ToString() => Name;
    }

    private static readonly KnownEntry[] SeraFemale = [new(Guid.NewGuid(), "Sera Vane", CatCharacter, [], 'f')];

    public static IEnumerable<Case> Cases =>
    [
        new("01 she after one name", ["Wren crossed the flats. She was nineteen."], "age", "Wren"),
        new("02 he after one name", ["Kael waited at the gate. He was thirty."], "age", "Kael"),
        new("03 her possessive, same sentence", ["Wren pulled her grey cloak tight."], "cloak", "Wren"),
        new("04 his possessive, same sentence", ["Kael drew his iron sword."], "sword", "Kael"),
        new("05 nearest name in the sentence wins", ["Kael watched while Wren pulled her grey cloak tight."], "cloak", "Wren"),
        new("06 female title skips a male", ["Lady Mara spoke to Kael. Her eyes were grey."], "eyes", "Lady Mara"),
        new("07 male title skips a female", ["Wren met Lord Tobin. His eyes were green."], "eyes", "Lord Tobin"),
        new("08 learned she, then skip him", ["Wren crossed the flats. She said nothing.", "Wren met Kael by the gate. Her eyes were grey."], "eyes", "Wren"),
        new("09 learned he, then skip her", ["Kael waited. He said nothing.", "Kael met Wren at noon. His eyes were green."], "eyes", "Kael"),
        new("10 her own teaches gender", ["Wren had her own reasons.", "Wren met Kael at the gate. Her eyes were grey."], "eyes", "Wren"),
        new("11 his own teaches gender", ["Kael kept his own counsel.", "Kael met Wren at the gate. His eyes were green."], "eyes", "Kael"),
        new("12 kin brother is male", ["Wren's brother, Hollis Marsh, came home.", "Wren met Hollis Marsh at dusk. His eyes were blue."], "eyes", "Hollis Marsh"),
        new("13 kin sister is female", ["Kael's sister, Maren Doyle, came home.", "Kael met Maren Doyle at dusk. Her eyes were brown."], "eyes", "Maren Doyle"),
        new("14 recorded gender from the bible", ["Sera Vane met Kael at the gate. Her eyes were violet."], "eyes", "Sera Vane", SeraFemale),
        new("15 their is a group's", ["The Harbor Council met at dawn. Their banner was grey."], "banner", "the Harbor Council"),
        new("16 their never lands on one person", ["Wren walked the flats alone. Their banner was grey."], "banner", null),
        new("17 their finds the group named earlier in the sentence", ["Wren watched the Tidewardens raise their grey banner."], "banner", "the Tidewardens"),
        new("18 a paragraph gap forgets the character", ["Wren crossed the flats.", "The tide came in.", "She was nineteen."], "age", null),
        new("19 adjacent paragraph keeps the character", ["Wren crossed the flats.", "She was nineteen."], "age", "Wren"),
        new("20 named possessive beats pronoun", ["Kael watched Wren. Wren's eyes were grey."], "eyes", "Wren"),
        new("21 she with no one named", ["She was nineteen."], "age", null),
        new("22 second sentence pronoun after two names, gender known", ["Wren crossed the flats. She said nothing.", "Kael and Wren walked on. She was nineteen."], "age", "Wren"),
        new("23 hair via possessive pronoun", ["Maren Doyle sat down. Her hair was black."], "hair", "Maren Doyle"),
        new("24 scar on his left cheek", ["Kael turned, and a scar ran across his left cheek."], "scar", "Kael"),
        new("25 scar on her right hand", ["Wren raised her hand; a scar ran across her right hand."], "scar", "Wren"),
        new("26 she after a titled name", ["Captain Aldous was dead. Lady Mara wept. She was forty."], "age", "Lady Mara"),
        new("27 he skips the learned she", ["Wren crossed the flats. She said nothing.", "Wren found Kael. He was thirty."], "age", "Kael"),
        new("28 her coat goes to the woman not the captain", ["Captain Aldous was dead by evening.", "Wren pulled her black coat tight."], "coat", "Wren"),
        new("29 the group's look", ["The Guard marched in. Their cloaks were red."], "cloaks", null),
        new("30 recorded gender overrides nearest", ["Kael spoke to Sera Vane. Her eyes were violet."], "eyes", "Sera Vane", SeraFemale),
    ];

    [TestCaseSource(nameof(Cases))]
    public void Pronoun_lands_on_the_right_character(Case c)
    {
        var r = Extract(c.Paragraphs.Select(t => new Paragraph(Guid.NewGuid(), t)).ToList(), c.Known ?? []);
        var hit = r.Facts.FirstOrDefault(f => f.Attribute == c.Attribute);
        if (c.Expected is null)
            Assert.That(hit, Is.Null, $"expected no {c.Attribute}, got it on {hit?.SubjectName}");
        else
            Assert.That(hit?.SubjectName, Is.EqualTo(c.Expected));
    }
}
