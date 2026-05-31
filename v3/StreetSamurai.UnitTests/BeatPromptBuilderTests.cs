using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Services;

namespace StreetSamurai.UnitTests;

/// <summary>
/// Locks in the EmotionalTone / FacetTag / PaceHint → ElevenLabs prompt
/// mapping. These tests are the contract: changing a mapping requires
/// updating the test, which keeps the audiobook narration behaviour
/// reviewable.
/// </summary>
[TestFixture]
public class BeatPromptBuilderTests
{
    private const double BaseStability = 0.5;
    private const double BaseSimilarity = 0.75;
    private const double BaseStyle = 0.0;

    private static Beat B(string? tone = null, string? facet = null, string? pace = null, string text = "Hello.") =>
        new()
        {
            Id = Guid.NewGuid(),
            Text = text,
            EmotionalTone = tone,
            FacetTag = facet,
            PaceHint = pace,
        };

    // ── ModelSupportsAudioTags ───────────────────────────────────────────

    [TestCase("eleven_v3", true)]
    [TestCase("eleven_v3_turbo", true)]
    [TestCase("ELEVEN_V3", true)]
    [TestCase("eleven_multilingual_v2", false)]
    [TestCase("eleven_turbo_v2_5", false)]
    [TestCase("", false)]
    [TestCase(null, false)]
    public void ModelSupportsAudioTags_PrefixMatchesV3(string? modelId, bool expected)
    {
        Assert.That(BeatPromptBuilder.ModelSupportsAudioTags(modelId), Is.EqualTo(expected));
    }

    // ── AudioTagFor ──────────────────────────────────────────────────────

    [TestCase("quiet", "[whispering]")]
    [TestCase("tender", "[softly]")]
    [TestCase("violent", "[shouting]")]
    [TestCase("wry", "[sarcastic]")]
    [TestCase("tense", "[tense]")]
    public void AudioTagFor_KnownTone_ReturnsCanonicalTag(string tone, string expected)
    {
        Assert.That(BeatPromptBuilder.AudioTagFor(tone, null, null), Is.EqualTo(expected));
    }

    [Test]
    public void AudioTagFor_FacetTag_UsedWhenToneEmpty()
    {
        Assert.That(BeatPromptBuilder.AudioTagFor(null, "SHADOW", null), Is.EqualTo("[menacing]"));
        Assert.That(BeatPromptBuilder.AudioTagFor(null, "WOUND", null),  Is.EqualTo("[somber]"));
    }

    [Test]
    public void AudioTagFor_PaceHint_FallbackWhenNothingElse()
    {
        Assert.That(BeatPromptBuilder.AudioTagFor(null, null, "languorous"), Is.EqualTo("[slowly]"));
        Assert.That(BeatPromptBuilder.AudioTagFor(null, null, "staccato"),   Is.EqualTo("[clipped]"));
    }

    [Test]
    public void AudioTagFor_TonePrecedesFacet()
    {
        // EmotionalTone is the strongest signal — must win over Facet.
        Assert.That(BeatPromptBuilder.AudioTagFor("tender", "SHADOW", null), Is.EqualTo("[softly]"));
    }

    [Test]
    public void AudioTagFor_NothingMaps_ReturnsNull()
    {
        Assert.That(BeatPromptBuilder.AudioTagFor(null, null, null), Is.Null);
        Assert.That(BeatPromptBuilder.AudioTagFor("plain", "neutral", "normal"), Is.Null);
    }

    // ── Build: prompt + voice settings together ──────────────────────────

    [Test]
    public void Build_V3ModelTagsOn_InjectsTagPrefix()
    {
        var p = BeatPromptBuilder.Build(B(tone: "quiet", text: "She didn't move."),
            modelId: "eleven_v3", tagsEnabled: true,
            BaseStability, BaseSimilarity, BaseStyle);
        Assert.That(p.Text, Is.EqualTo("[whispering] She didn't move."));
    }

    [Test]
    public void Build_NonV3Model_SkipsTagInjection_KeepsVoiceSettings()
    {
        var p = BeatPromptBuilder.Build(B(tone: "violent", text: "GET DOWN."),
            modelId: "eleven_multilingual_v2", tagsEnabled: true,
            BaseStability, BaseSimilarity, BaseStyle);
        Assert.That(p.Text, Is.EqualTo("GET DOWN."), "Non-v3 models read tags as literal text — must NOT inject");
        // Voice settings still bias for the tone.
        Assert.That(p.Stability, Is.LessThan(BaseStability));
        Assert.That(p.Style,     Is.GreaterThan(BaseStyle));
    }

    [Test]
    public void Build_TagsDisabledGlobally_SkipsInjection_OnV3()
    {
        var p = BeatPromptBuilder.Build(B(tone: "tender", text: "Hey."),
            modelId: "eleven_v3", tagsEnabled: false,
            BaseStability, BaseSimilarity, BaseStyle);
        Assert.That(p.Text, Is.EqualTo("Hey."));
    }

    // Per-beat stability biasing is a v2-only channel (v2 has continuous
    // stability). On v3 it's suppressed in favour of inline audio tags — see
    // Build_V3_PinsStabilityToBaseline_RegardlessOfTone below.
    [Test]
    public void Build_QuietTone_RaisesStability_OnV2()
    {
        var p = BeatPromptBuilder.Build(B(tone: "quiet"), "eleven_multilingual_v2", true,
            BaseStability, BaseSimilarity, BaseStyle);
        Assert.That(p.Stability, Is.GreaterThan(BaseStability));
    }

    [Test]
    public void Build_ViolentTone_LowersStability_RaisesStyle_OnV2()
    {
        var p = BeatPromptBuilder.Build(B(tone: "violent"), "eleven_multilingual_v2", true,
            BaseStability, BaseSimilarity, BaseStyle);
        Assert.That(p.Stability, Is.LessThan(BaseStability));
        Assert.That(p.Style,     Is.GreaterThan(BaseStyle));
    }

    // v3 holds stability flat at the strand baseline so the narrator stays on a
    // single stability preset across beats (liquid, not mode-switching). Emotion
    // is carried by the injected audio tag instead.
    [TestCase("quiet")]
    [TestCase("violent")]
    [TestCase("tense")]
    [TestCase("tender")]
    public void Build_V3_PinsStabilityToBaseline_RegardlessOfTone(string tone)
    {
        var p = BeatPromptBuilder.Build(B(tone: tone), "eleven_v3", true,
            BaseStability, BaseSimilarity, BaseStyle);
        Assert.That(p.Stability, Is.EqualTo(BaseStability));
    }

    [Test]
    public void Build_ClampsAt0And1_OnV2()
    {
        // Extreme baselines: the v2 per-beat bias should not push below 0 or above 1.
        var pHi = BeatPromptBuilder.Build(B(tone: "tense"), "eleven_multilingual_v2", true,
            baselineStability: 0.95, baselineSimilarityBoost: 0.75, baselineStyle: 0.0);
        Assert.That(pHi.Stability, Is.LessThanOrEqualTo(1.0));

        var pLo = BeatPromptBuilder.Build(B(tone: "violent"), "eleven_multilingual_v2", true,
            baselineStability: 0.05, baselineSimilarityBoost: 0.75, baselineStyle: 0.95);
        Assert.That(pLo.Stability, Is.GreaterThanOrEqualTo(0.0));
        Assert.That(pLo.Style,     Is.LessThanOrEqualTo(1.0));
    }

    [Test]
    public void Build_NoMetadata_PassesThroughBaseline()
    {
        var p = BeatPromptBuilder.Build(B(text: "Plain."), "eleven_v3", true,
            BaseStability, BaseSimilarity, BaseStyle);
        Assert.That(p.Text,            Is.EqualTo("Plain."));
        Assert.That(p.Stability,       Is.EqualTo(BaseStability));
        Assert.That(p.SimilarityBoost, Is.EqualTo(BaseSimilarity));
        Assert.That(p.Style,           Is.EqualTo(BaseStyle));
    }
}
