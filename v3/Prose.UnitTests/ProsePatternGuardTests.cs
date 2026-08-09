using Prose.Core.Services;

namespace Prose.UnitTests;

[TestFixture]
public class ProsePatternGuardTests
{
    readonly ProsePatternGuard guard = new();

    [Test]
    public void Check_EmptyString_ReturnsEmptyList()
    {
        var result = guard.Check("");
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void Check_NullString_ReturnsEmptyOrDoesNotThrow()
    {
        List<ProseViolation> result = null!;
        Assert.DoesNotThrow(() => result = guard.Check(null!));
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void Check_CleanText_ReturnsEmptyList()
    {
        var clean = "Kyle crossed the bridge. The rain hit the steel rail. He kept moving.";
        var result = guard.Check(clean);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void Check_ChromeGleam_ReturnsCliqueViolation()
    {
        var result = guard.Check("The chrome gleam of the tower reflected the sky.");
        Assert.That(result, Has.Count.GreaterThan(0));
        Assert.That(result[0].Category, Is.EqualTo(ProseViolationCategory.Cliche));
    }

    [Test]
    public void Check_NeonWashed_DetectedAsCliched()
    {
        var result = guard.Check("The neon-washed alley smelled of rot.");
        Assert.That(result.Any(v => v.Category == ProseViolationCategory.Cliche), Is.True);
    }

    [Test]
    public void Check_HeartHammered_DetectedAsCliched()
    {
        var result = guard.Check("His heart hammered as he ran.");
        Assert.That(result.Any(v => v.Category == ProseViolationCategory.Cliche), Is.True);
    }

    [Test]
    public void Check_InThisCity_DetectedAsCliched()
    {
        var result = guard.Check("In this city, nobody trusts anyone.");
        Assert.That(result.Any(v => v.Category == ProseViolationCategory.Cliche), Is.True);
    }

    [Test]
    public void Check_InThatMoment_DetectedAsPseudoProfound()
    {
        var result = guard.Check("In that moment, everything crystallised.");
        Assert.That(result.Any(v => v.Category == ProseViolationCategory.PseudoProfound), Is.True);
    }

    [Test]
    public void Check_TheTruthWas_DetectedAsPseudoProfound()
    {
        var result = guard.Check("The truth was that he had never belonged here.");
        Assert.That(result.Any(v => v.Category == ProseViolationCategory.PseudoProfound), Is.True);
    }

    [Test]
    public void Check_SuddenlyUnderstood_DetectedAsPseudoProfound()
    {
        var result = guard.Check("She suddenly understood what the contract meant.");
        Assert.That(result.Any(v => v.Category == ProseViolationCategory.PseudoProfound), Is.True);
    }

    [Test]
    public void Check_KyleThoughtAboutHow_DetectedAsOnTheNose()
    {
        var result = guard.Check("Kyle thought about how the job would change everything.");
        Assert.That(result.Any(v => v.Category == ProseViolationCategory.OnTheNose), Is.True);
    }

    [Test]
    public void Check_ThisWasThePartWhere_DetectedAsOnTheNose()
    {
        var result = guard.Check("This was the part where things got complicated.");
        Assert.That(result.Any(v => v.Category == ProseViolationCategory.OnTheNose), Is.True);
    }

    [Test]
    public void Check_ItalicisedDialogue_DetectedCorrectly()
    {
        var result = guard.Check("She said *\"I'll be back\"* and walked away.");
        Assert.That(result.Any(v => v.Category == ProseViolationCategory.ItalicisedDialogue), Is.True);
    }


    [Test]
    public void Check_CorrectPhiForm_NotFlagged()
    {
        var result = guard.Check("The fee was Φ40, released on completion. The mesh cost Φ11,200 with the array.");
        Assert.That(result.Any(v => v.Category == ProseViolationCategory.CurrencyFormat), Is.False);
    }

    [Test]
    public void Check_DigitsBeforePhi_Flagged()
    {
        var result = guard.Check("The fee was 40 Φ, released on completion.");
        Assert.That(result.Any(v => v.Category == ProseViolationCategory.CurrencyFormat), Is.True);
    }

    [Test]
    public void Check_DigitsBeforePhiNoSpace_Flagged()
    {
        var result = guard.Check("He counted out 100Φ at the counter.");
        Assert.That(result.Any(v => v.Category == ProseViolationCategory.CurrencyFormat), Is.True);
    }

    [Test]
    public void Check_SpelledNumberBeforePhi_Flagged()
    {
        var result = guard.Check("\"Forty Φ — and one true answer,\" Vey said.");
        Assert.That(result.Any(v => v.Category == ProseViolationCategory.CurrencyFormat), Is.True);
    }

    [Test]
    public void Check_HyphenatedSpelledNumberBeforePhi_Flagged()
    {
        var result = guard.Check("Thirty-five Φ a week is not a retirement plan.");
        Assert.That(result.Any(v => v.Category == ProseViolationCategory.CurrencyFormat), Is.True);
    }

    [Test]
    public void Check_BareQsAndHalfAPhi_NotFlagged()
    {
        var result = guard.Check("Carver paid him in Qs. He sent half a Φ back up the contract.");
        Assert.That(result.Any(v => v.Category == ProseViolationCategory.CurrencyFormat), Is.False);
    }

    [Test]
    public void Check_MultipleViolations_AllReturned()
    {
        var text = "The chrome gleam blinded him. In that moment, he thought about how the neon-washed street looked.";
        var result = guard.Check(text);
        Assert.That(result.Count, Is.GreaterThanOrEqualTo(3));
    }

    [Test]
    public void Check_CharOffsetAlwaysNonNegative()
    {
        var text = "The chrome gleam lit the neon-washed alley. In that moment, everything changed.";
        var result = guard.Check(text);
        Assert.That(result.All(v => v.CharOffset >= 0), Is.True);
    }

    [Test]
    public void Check_ViolationsOrderedByCharOffset()
    {
        var text = "The chrome gleam lit the neon-washed alley.";
        var result = guard.Check(text);
        if (result.Count > 1)
        {
            for (int i = 1; i < result.Count; i++)
                Assert.That(result[i].CharOffset, Is.GreaterThanOrEqualTo(result[i - 1].CharOffset));
        }
    }

    [Test]
    public void Check_AdditionalProhibition_Detected()
    {
        var result = guard.Check("The signal was lost in the static.", ["static"]);
        Assert.That(result.Any(v => v.Match.Equals("static", StringComparison.OrdinalIgnoreCase)), Is.True);
    }

    [Test]
    public void Check_AdditionalProhibition_CaseInsensitive()
    {
        var result = guard.Check("He felt the Static crackle.", ["static"]);
        Assert.That(result.Any(v => v.Match.Equals("static", StringComparison.OrdinalIgnoreCase)), Is.True);
    }

    [Test]
    public void Check_AdditionalProhibition_MultipleOccurrences_AllFound()
    {
        var result = guard.Check("Static here and static there.", ["static"]);
        Assert.That(result.Count(v => v.Match.Equals("static", StringComparison.OrdinalIgnoreCase)), Is.EqualTo(2));
    }

    [Test]
    public void Check_AdditionalProhibition_EmptyPhrase_Ignored()
    {
        Assert.DoesNotThrow(() => guard.Check("Clean text.", [""]));
    }

    [Test]
    public void Check_AdditionalProhibition_Category_IsCliched()
    {
        var result = guard.Check("The ghost walked past.", ["ghost"]);
        var v = result.FirstOrDefault(v => v.Match.Equals("ghost", StringComparison.OrdinalIgnoreCase));
        Assert.That(v, Is.Not.Null);
        Assert.That(v!.Category, Is.EqualTo(ProseViolationCategory.Cliche));
    }

    [Test]
    public void Check_CategoryEnumValuesAreSet_NotDefault()
    {
        var text = "The chrome gleam lit the neon-washed alley. In that moment, the truth was clear.";
        var result = guard.Check(text);
        var categories = result.Select(v => v.Category).Distinct().ToList();
        Assert.That(categories, Has.Count.GreaterThan(1));
    }

    [Test]
    public void Check_RuleFieldIsNonEmpty_OnAllViolations()
    {
        var text = "The chrome gleam lit the neon-washed alley. In that moment, the truth was clear.";
        var result = guard.Check(text);
        Assert.That(result.All(v => !string.IsNullOrEmpty(v.Rule)), Is.True);
    }

    [Test]
    public void Check_MatchFieldIsNonEmpty_OnAllViolations()
    {
        var text = "The chrome gleam was visible from across the neon-washed plaza.";
        var result = guard.Check(text);
        Assert.That(result.All(v => !string.IsNullOrEmpty(v.Match)), Is.True);
    }

    // ── AI-tell countermeasures (2026-08-09) ────────────────────────────────

    [Test]
    public void Check_Delve_DetectedAsAiVocabulary()
    {
        var result = guard.Check("She decided to delve into the archive.");
        Assert.That(result.Any(v => v.Category == ProseViolationCategory.AiVocabulary), Is.True);
    }

    [Test]
    public void Check_StandsAsATestament_DetectedAsAiVocabulary()
    {
        var result = guard.Check("The scar stands as a testament to what he survived.");
        Assert.That(result.Any(v => v.Category == ProseViolationCategory.AiVocabulary), Is.True);
    }

    [Test]
    public void Check_ElaraVoss_DetectedAsAiDefaultName()
    {
        var result = guard.Check("Elara Voss looked up from the console.");
        Assert.That(result.Any(v => v.Category == ProseViolationCategory.AiDefaultName), Is.True);
    }

    [Test]
    public void Check_ProjectErebus_DetectedAsAiDefaultName()
    {
        var result = guard.Check("The files were all that remained of Project Erebus.");
        Assert.That(result.Any(v => v.Category == ProseViolationCategory.AiDefaultName), Is.True);
    }

    [Test]
    public void Check_NotJustButAlso_DetectedAsAiStructuralTic()
    {
        var result = guard.Check("It was not just a warning, but also a promise.");
        Assert.That(result.Any(v => v.Category == ProseViolationCategory.AiStructuralTic), Is.True);
    }

    [Test]
    public void Check_OrdinaryContrast_NotFlaggedAsStructuralTic()
    {
        // Ordinary contrastive grammar ("X rather than Y", bare "not X, but Y") must NOT be
        // flagged — only the "not just/only ... but (also)" compound template is AI-specific.
        var result = guard.Check("He wanted to run rather than fight. It was warm, not cold.");
        Assert.That(result.Any(v => v.Category == ProseViolationCategory.AiStructuralTic), Is.False);
    }

    [Test]
    public void Check_HighEmDashDensity_DetectedAsStructuralTic()
    {
        var text = string.Join(" ", Enumerable.Repeat("word", 50)) +
                    " — one — two — three — four — five — six —";
        var result = guard.Check(text);
        Assert.That(result.Any(v => v.Category == ProseViolationCategory.AiStructuralTic
                                  && v.Rule.Contains("em-dash density")), Is.True);
    }

    [Test]
    public void Check_OccasionalEmDash_NotFlagged()
    {
        var text = "Kyle crossed the bridge — slower this time — and kept walking. " +
                    string.Join(" ", Enumerable.Repeat("word", 60));
        var result = guard.Check(text);
        Assert.That(result.Any(v => v.Rule.Contains("em-dash density")), Is.False);
    }

    [Test]
    public void Check_EnDashNumericRange_NotCountedTowardEmDashDensity()
    {
        // Regression (2026-08-09): found via real-corpus validation — a citation annotation
        // whose only elevated "dash density" came from a correctly-hyphenated year range
        // ("1315–1317", en dash) plus one genuine parenthetical em-dash aside. En-dash in a
        // numeric range is universal correct typography, unrelated to the AI em-dash tell.
        var text = "5 - The Great Famine, 1315–1317\n\n" +
            "William Chester Jordan, The Great Famine: Northern Europe in the Early Fourteenth " +
            "Century (Princeton: Princeton University Press, 1996). Jordan's study — awarded the " +
            "Medieval Academy of America's Haskins Medal — documents the catastrophic run of wet " +
            "growing seasons across northern Europe from 1315 to roughly 1317, and the resulting " +
            "mass mortality and nutritional crisis that struck a population already under strain " +
            "from decades of growth against a fixed land base.\n\nCited in: Chapter 1.";

        var result = guard.Check(text);

        Assert.That(result.Any(v => v.Rule.Contains("em-dash density")), Is.False,
            "one genuine em-dash aside in a full paragraph must not trip the density threshold " +
            "once the en-dash year-range is correctly excluded from the count");
    }

    [Test]
    public void Check_GazetteerStyleList_NotFlaggedAsEmDashOveruse()
    {
        // Regression (2026-08-09): found via real-corpus validation — a book's "Gazetteer of
        // the Rising" appendix used one em-dash per entry as a field separator (the same role
        // a colon plays in a dictionary), which is standard reference-book convention, not
        // narrative-prose em-dash habit. A list of independent one-line entries must never be
        // scored against the continuous-prose density threshold.
        var text = string.Join("\n\n", new[]
        {
            "FOBBING, ESSEX 51.5217°N, 0.4508°E — The Essex village where Thomas Baker refused the poll tax commissioner, 30 May 1381 (Chapter 6).",
            "BRENTWOOD, ESSEX 51.6212°N, 0.3040°E — The market town where the commissioner recalled village representatives, 1 June 1381 (Chapter 6).",
            "MAIDSTONE, KENT 51.2704°N, 0.5227°E — County town of Kent; site of the prison-breaking and Wat Tyler's election as leader, 7 June 1381 (Chapter 7).",
            "ROCHESTER, KENT 51.3881°N, 0.5013°E — Site of the royal castle that surrendered without resistance, early June 1381 (Chapter 7).",
            "CANTERBURY, KENT 51.2802°N, 1.0789°E — Seat of the Archbishopric; entered by the Kentish rebels 10 June 1381 (Chapter 7).",
        });

        var result = guard.Check(text);

        Assert.That(result.Any(v => v.Rule.Contains("em-dash density")), Is.False,
            "a gazetteer/index-shaped list (one em-dash per short entry) is not narrative prose " +
            "and must not be scored against the density threshold");
    }

    [Test]
    public void Check_SingleParentheticalAside_NotFlagged_EvenInShortBeat()
    {
        // Regression (2026-08-09): a single legitimate parenthetical aside is exactly 2
        // em-dashes. CRAFT.md's own "gloss_in_voice" doctrine prescribes exactly this
        // dash-bracketed in-voice jargon touch. Verified against the live GLMZ+SCRY catalog:
        // 62% of all beats crossing the raw percentage threshold had exactly 2 em-dashes — one
        // single aside in a short beat, not a chronic habit. Real example (paraphrased):
        var text = "The Low - the ungoverned band - starts thirty meters up, and the Unit sits " +
            "at twenty-eight. Corpo authority reaches to thirty meters; the NCID — Halcyon's " +
            "Neuretic Crime Investigation Division — doesn't take interest below thirty-five.";

        var result = guard.Check(text);

        Assert.That(result.Any(v => v.Rule.Contains("em-dash density")), Is.False,
            "two em-dashes (one parenthetical gloss) in a short beat must not fire — " +
            "that's the craft-correct technique, not the tic");
    }

    [Test]
    public void Check_TwoSeparateGlossesInOneBeat_FourEmDashes_NotFlagged()
    {
        // Real corpus example (2026-08-09): two separate, legitimate gloss_in_voice touches
        // (CRAFT.md §4) in one short beat — glossing "the Low" and "NCID" — landed at exactly
        // 4 em-dashes. Spot-checking the live catalog's 4-dash bucket found this same pattern
        // repeatedly: two single glosses, not decorative habit. The floor sits at 5 specifically
        // so this shape passes.
        var text = "Her name, she says, is Renata. She repeats a number - 412.7 - twice, in the " +
            "same flat tone as her name. Her grip loosens. Her eyes close. She isn't dead — the " +
            "labored breathing goes on — but she's out.".Replace(" - 412.7 - ", " — 412.7 — ");

        var result = guard.Check(text);

        Assert.That(result.Any(v => v.Rule.Contains("em-dash density")), Is.False,
            "4 em-dashes (two separate single glosses) must not fire — the floor is 5");
    }

    [Test]
    public void Check_TwoEmDashes_BelowAbsoluteFloor_NotFlaggedEvenAtHighPercentage()
    {
        // Short beat, only 2 em-dashes (one aside) — percentage alone would fire this
        // (2/44 words * 100 = 4.5%, above the 3.0 threshold), but the absolute floor must win.
        var text = "Jordan's study — awarded the Haskins Medal — documents the run " +
            string.Join(" ", Enumerable.Repeat("word", 40));

        var result = guard.Check(text);

        Assert.That(result.Any(v => v.Rule.Contains("em-dash density")), Is.False,
            "2 em-dashes must never fire regardless of how the percentage math falls out");
    }

    [Test]
    public void Check_NarrativeProseWithHeavyDashUse_StillFlaggedDespiteListGuard()
    {
        // The structured-list exclusion must not become a blanket loophole — real multi-
        // paragraph narrative prose with genuine em-dash overuse must still fire.
        var text = string.Join("\n\n", Enumerable.Range(0, 4).Select(i =>
            $"Paragraph {i} — this is a long developed sentence with real narrative weight — " +
            "and it keeps going — clause after clause — well past what a single field separator " +
            "would ever need, which is exactly the pattern that should still be flagged."));

        var result = guard.Check(text);

        Assert.That(result.Any(v => v.Rule.Contains("em-dash density")), Is.True,
            "genuine multi-dash narrative paragraphs must still be caught — the list guard " +
            "requires SHORT single-line entries with exactly one dash each, not this");
    }

    [Test]
    public void Check_BoastAsNegatedNoun_NotFlagged()
    {
        // Regression (2026-08-09): all 4 "boast" matches across the entire GLMZ+SCRY catalog
        // were this exact pattern — "boast" used as a NOUN in a negation ("not a boast," "no
        // boast in it"), a genuine characterization technique, not the AI copulative-avoidance
        // VERB tell ("the city boasts a district").
        var result = guard.Check("He said it as a plain fact, not a boast. He rarely boasted.");
        Assert.That(result.Any(v => v.Category == ProseViolationCategory.AiVocabulary), Is.False);
    }

    [Test]
    public void Check_BoastAsCopulativeAvoidanceVerb_StillFlagged()
    {
        // The negation-noun fix must not become a blanket loophole — the actual AI-tell shape
        // (verb + article + noun) must still fire.
        var result = guard.Check("The archology boasts a rooftop garden overlooking the Veil.");
        Assert.That(result.Any(v => v.Category == ProseViolationCategory.AiVocabulary), Is.True);
    }

    [Test]
    public void Check_Realm_NotFlagged()
    {
        // "realm" is deliberately excluded — legitimate SCRY/fantasy-universe vocabulary.
        var result = guard.Check("She had never left the realm before.");
        Assert.That(result.Any(v => v.Category == ProseViolationCategory.AiVocabulary), Is.False);
    }

    [Test]
    public void Check_AiVocabularyInsideQuote_NotFlagged()
    {
        // Regression (2026-08-09): found via real-corpus validation — a nonfiction chapter's
        // ONLY four "delve" instances were all the same verbatim 14th-century couplet, "When
        // Adam delved and Eve span, who was then the gentleman?" — "delved" there means "dug,"
        // completely unrelated to the modern AI-vocabulary tic. A quoted historical source's
        // word choice is never the author's own prose style.
        var text = "Later tradition credits him with a rhyming couplet: " +
            "\"When Adam delved and Eve span, who was then the gentleman?\" " +
            "though the fullest surviving text comes down through a chronicler's reconstruction.";

        var result = guard.Check(text);

        Assert.That(result.Any(v => v.Category == ProseViolationCategory.AiVocabulary), Is.False,
            "a word inside a direct quotation must never be judged as the author's own vocabulary tic");
    }

    [Test]
    public void Check_AiVocabularyOutsideQuote_StillFlagged()
    {
        // The quote-span guard must not become a blanket loophole — "delve" in the author's
        // OWN narration, outside any quotation, must still fire.
        var text = "The historian chose to delve into the parish records for this chapter.";

        var result = guard.Check(text);

        Assert.That(result.Any(v => v.Category == ProseViolationCategory.AiVocabulary), Is.True,
            "delve in the author's own narration (no quote marks involved) must still be flagged");
    }

    [Test]
    public void Check_AiVocabularyAfterAnEarlierClosedQuote_StillFlagged()
    {
        // Confirms the quote-toggle correctly closes: a tell-word appearing AFTER a fully
        // closed, unrelated quotation earlier in the same beat must still fire — the guard
        // must track paired open/close, not just "any quote mark seen anywhere in the text."
        var text = "The chronicler wrote \"a great and marvellous stir among the common people\" " +
            "of that summer, and later historians delve into exactly why it spread so fast.";

        var result = guard.Check(text);

        Assert.That(result.Any(v => v.Category == ProseViolationCategory.AiVocabulary), Is.True);
    }
}
