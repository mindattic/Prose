using Prose.Core.Services.ReadAlong;

namespace Prose.UnitTests;

/// <summary>RA-T3.1.1.3: the audit replays without writing, samples by pattern, and scores verdicts.</summary>
[TestFixture]
public class ReadAlongAuditTests
{
    [Test]
    public void Scorer_computes_precision_and_flags_patterns_under_the_bar()
    {
        var csv = """
            kind,pattern,subject,attribute,value,quote,chapter,verdict
            fact,age.1,Wren,age,19,"She was nineteen.",One,y
            fact,age.1,Kael,age,30,"He was thirty, or so.",Two,y
            fact,eyes.2,Wren,eyes,grey,x,One,n
            fact,eyes.2,Kael,eyes,green,x,One,n
            fact,eyes.2,Sera,eyes,blue,x,One,y
            fact,eyes.2,Tobin,eyes,black,x,One,n
            fact,eyes.2,Maren,eyes,brown,x,One,n
            name,name.signal,Frost,character,,x,One,
            """;
        var scores = ReadAlongAudit.Score(ReadAlongAudit.FromCsv(csv));
        var eyes = scores.Single(s => s.Pattern == "eyes.2");
        Assert.That((eyes.Judged, eyes.Right, eyes.SwitchOff), Is.EqualTo((5, 1, true)));
        var age = scores.Single(s => s.Pattern == "age.1");
        Assert.That((age.Precision, age.SwitchOff), Is.EqualTo((1.0, false)), "too few verdicts never switch off");
        Assert.That(scores.Any(s => s.Pattern == "name.signal"), Is.False, "unjudged rows are not scored");
        Assert.That(ReadAlongAudit.ToMarkdown(scores, "x.csv"), Does.Contain("`eyes.2`").And.Contain("switch off"));
    }

    [Test]
    public void Replay_samples_by_pattern_and_includes_would_be_cards()
    {
        var p = (Guid ch, string t) => new StoryFactExtractor.Paragraph(Guid.NewGuid(), t);
        var chapters = new List<ReadAlongService.BookChapter>
        {
            new(Guid.NewGuid(), 1, "One", null, null, [p(default, "Kael said nothing. Kael's eyes were grey.")], "h1", 6),
            new(Guid.NewGuid(), 2, "Two", null, null, [p(default, "Kael turned. Kael's eyes were green.")], "h2", 5),
        };
        var rows = ReadAlongAudit.Replay(chapters);
        Assert.That(rows.Count(r => r.Kind == "card"), Is.EqualTo(1));
        var sample = ReadAlongAudit.Sample(rows, 10);
        Assert.That(sample.Any(r => r.Kind == "card"));
        var roundTrip = ReadAlongAudit.FromCsv(ReadAlongAudit.ToCsv(sample));
        Assert.That(roundTrip.Select(r => r.Quote), Is.EqualTo(sample.Select(r => r.Quote)));
    }
}
