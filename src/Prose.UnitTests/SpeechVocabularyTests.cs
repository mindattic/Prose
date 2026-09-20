using NUnit.Framework;
using Prose.Core.Services.Discussion;

namespace Prose.UnitTests;

/// <summary>
/// The dictionary a spoken turn is transcribed against.
///
/// <para><b>What fails without this.</b> Whisper's <c>prompt</c> is capped at 224 tokens and
/// enforces the cap by truncating from the END. An overlong vocabulary therefore does not error —
/// it silently stops priming the names at the tail of the list, and the only symptom is that
/// transcription is slightly worse than it should be at exactly the invented nouns this feature
/// exists to get right. That is a defect no one would ever notice by using the app, so the budget
/// is asserted here instead.</para>
///
/// <para>The DB query that ranks the names is not covered here — these tests cover the pure step
/// that decides what survives the cut, which is where the arithmetic lives.</para>
/// </summary>
[TestFixture]
public class SpeechVocabularyTests
{
    [Test]
    public void Composes_a_sentence_not_a_bare_list()
    {
        var prompt = SpeechVocabularyService.Compose(["Togishi", "Seito", "Cacophony"]);

        Assert.That(prompt, Is.EqualTo(
            "Names and terms used in this story: Togishi, Seito, Cacophony."));
    }

    [Test]
    public void Empty_input_primes_nothing_rather_than_an_empty_sentence()
    {
        Assert.That(SpeechVocabularyService.Compose([]), Is.Empty);
        Assert.That(SpeechVocabularyService.Compose([null, "", "  "]), Is.Empty);
    }

    [Test]
    public void Never_exceeds_the_budget_even_with_far_too_many_names()
    {
        var many = Enumerable.Range(0, 500).Select(i => $"Entity{i:0000}");

        var prompt = SpeechVocabularyService.Compose(many);

        Assert.That(prompt, Is.Not.Empty);
        Assert.That(prompt.Length, Is.LessThanOrEqualTo(SpeechService.MaxVocabularyChars),
            "an overlong prompt is truncated by Whisper itself, silently, from the end");
        Assert.That(prompt, Does.EndWith("."));
    }

    [Test]
    public void Keeps_priority_order_and_stops_rather_than_skipping_ahead()
    {
        // A long name that does not fit must END the list, not be stepped over in favour of a
        // short one behind it — the ranking is by how often the book says the word, and slipping
        // past an entry would silently re-rank the vocabulary by length instead.
        var longName = new string('A', 200);
        var prompt = SpeechVocabularyService.Compose(
            ["First", longName, "Later"], maxChars: SpeechVocabularyService.Lead.Length + 40);

        Assert.That(prompt, Does.Contain("First"));
        Assert.That(prompt, Does.Not.Contain("Later"));
    }

    [Test]
    public void Drops_tag_debris_and_names_that_would_read_as_two()
    {
        var prompt = SpeechVocabularyService.Compose(["Kyle", "a", "Mr", "Park, Nadia", "Pixel"]);

        Assert.That(prompt, Does.Contain("Kyle"));
        Assert.That(prompt, Does.Contain("Pixel"));
        Assert.That(prompt, Does.Not.Contain("Park, Nadia"),
            "a comma inside a term splits it into two names in the prompt");
        Assert.That(prompt, Does.Not.Contain(" a,"));
    }

    [Test]
    public void Deduplicates_case_insensitively_so_an_alias_does_not_repeat_its_name()
    {
        var prompt = SpeechVocabularyService.Compose(["Togishi", "togishi", "TOGISHI"]);

        Assert.That(prompt, Is.EqualTo("Names and terms used in this story: Togishi."));
    }
}

/// <summary>
/// The container a recording is sent in.
///
/// <para><c>MediaRecorder</c> reports <c>audio/webm;codecs=opus</c>, and the codec parameter is
/// not cosmetic: <c>MediaTypeHeaderValue</c> throws on it, which would turn every spoken turn
/// into an exception on the first line of the request.</para>
/// </summary>
[TestFixture]
public class SpeechMimeTests
{
    [TestCase("audio/webm;codecs=opus", "audio/webm", "webm")]
    [TestCase("audio/webm", "audio/webm", "webm")]
    [TestCase("audio/ogg; codecs=opus", "audio/ogg", "ogg")]
    [TestCase("AUDIO/MP4", "audio/mp4", "mp4")]
    [TestCase("audio/x-wav", "audio/wav", "wav")]
    public void Strips_codec_parameters_and_names_the_container(string given, string mime, string ext)
    {
        var (actualMime, actualExt) = SpeechService.NormalizeMime(given);

        Assert.That(actualMime, Is.EqualTo(mime));
        Assert.That(actualExt, Is.EqualTo(ext));
    }

    [Test]
    public void An_unknown_container_is_tried_as_webm_rather_than_refused()
    {
        // Every browser WebView2 can host records webm/opus. Refusing an unrecognised type would
        // throw away a recording the provider would most likely have accepted.
        var (mime, ext) = SpeechService.NormalizeMime("audio/flac");

        Assert.That(mime, Is.EqualTo("audio/webm"));
        Assert.That(ext, Is.EqualTo("webm"));
    }

    [Test]
    public void A_missing_type_does_not_throw()
    {
        Assert.That(SpeechService.NormalizeMime(null).Mime, Is.EqualTo("audio/webm"));
        Assert.That(SpeechService.NormalizeMime("").Mime, Is.EqualTo("audio/webm"));
    }
}
