using MindAttic.Export;
using MindAttic.Export.Model;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// A book's layout decides how its beat text prints (WO:01a12fb3-4602-74f9-b4dd-5793103e74f4):
/// prose lines become paragraphs, lyrics become stanzas without their singing cues, and a
/// screenplay becomes Fountain elements on the screenplay page.
/// </summary>
[TestFixture]
public class BeatLayoutTests
{
    [TestCase(null, "prose")]
    [TestCase("", "prose")]
    [TestCase("novel", "prose")]
    [TestCase(" Lyrics ", "lyrics")]
    [TestCase("SCREENPLAY", "screenplay")]
    public void Unknown_or_blank_layouts_read_as_prose(string? layout, string expected) =>
        Assert.That(BeatLayout.Normalize(layout), Is.EqualTo(expected));

    [Test]
    public void Prose_prints_every_line_as_a_paragraph()
    {
        var blocks = BeatLayout.Blocks("prose", "One.\n\nTwo.\r\nThree.").ToList();
        Assert.That(blocks, Is.EqualTo(new Block[] { new ParagraphBlock("One."), new ParagraphBlock("Two."), new ParagraphBlock("Three.") }));
    }

    [Test]
    public void Lyrics_keep_stanzas_and_drop_section_labels()
    {
        const string text = "[Intro: solo bass]\n\n[Verse 1]\nOut past the Struts\nThere's a hole in the sky\n\n[Pre-Chorus]\nSix. Oh. Six.\n\n[Final Chorus - crowd]\nI've got you";
        var stanzas = BeatLayout.Blocks("lyrics", text).Cast<StanzaBlock>().Select(s => s.Lines.ToArray()).ToList();
        Assert.That(stanzas, Is.EqualTo(new[]
        {
            new[] { "Out past the Struts", "There's a hole in the sky" },
            new[] { "Six. Oh. Six." },
            new[] { "I've got you" }
        }));
    }

    [Test]
    public void Lyrics_keep_a_bracket_inside_a_sung_line()
    {
        var stanza = (StanzaBlock)BeatLayout.Blocks("lyrics", "Count it [down] into the bone").Single();
        Assert.That(stanza.Lines, Is.EqualTo(new[] { "Count it [down] into the bone" }));
    }

    [Test]
    public void Screenplay_parses_fountain_elements()
    {
        const string text =
            "INT. BAR - NIGHT\n\n" +
            "Rain on the glass. Kyle waits.\n\n" +
            "KYLE (V.O.)\n(quietly)\nNot here.\nNot tonight.\n\n" +
            "@McAllister\nYou sure?\n\n" +
            ".the roof\n\n" +
            "CUT TO:\n\n" +
            "> FADE OUT.";
        var blocks = BeatLayout.Blocks("screenplay", text).Cast<ScriptBlock>().ToList();
        Assert.That(blocks, Is.EqualTo(new[]
        {
            new ScriptBlock(ScriptElement.SceneHeading, "INT. BAR - NIGHT"),
            new ScriptBlock(ScriptElement.Action, "Rain on the glass. Kyle waits."),
            new ScriptBlock(ScriptElement.Character, "KYLE (V.O.)"),
            new ScriptBlock(ScriptElement.Parenthetical, "(quietly)"),
            new ScriptBlock(ScriptElement.Dialogue, "Not here."),
            new ScriptBlock(ScriptElement.Dialogue, "Not tonight."),
            new ScriptBlock(ScriptElement.Character, "McAllister"),
            new ScriptBlock(ScriptElement.Dialogue, "You sure?"),
            new ScriptBlock(ScriptElement.SceneHeading, "the roof"),
            new ScriptBlock(ScriptElement.Transition, "CUT TO:"),
            new ScriptBlock(ScriptElement.Transition, "FADE OUT.")
        }));
    }

    [Test]
    public void Screenplay_upper_case_line_alone_is_action_not_a_cue()
    {
        var block = (ScriptBlock)BeatLayout.Blocks("screenplay", "BANG.").Single();
        Assert.That(block.Element, Is.EqualTo(ScriptElement.Action));
    }

    [Test]
    public void Screenplay_prints_on_the_screenplay_page_and_the_rest_on_the_book_trim()
    {
        Assert.That(BeatLayout.Options("screenplay").Profile, Is.EqualTo(PageProfile.Screenplay));
        Assert.That(BeatLayout.Options("screenplay").FontFamily, Is.EqualTo("Courier New"));
        Assert.That(BeatLayout.Options("lyrics"), Is.EqualTo(ExportOptions.ProseBook));
        Assert.That(BeatLayout.Options(null), Is.EqualTo(ExportOptions.ProseBook));
    }
}
