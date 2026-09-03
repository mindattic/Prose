using Prose.Core.Data.Entities;
using Prose.Core.Services;
using Prose.Core.Services.Audit;

namespace Prose.UnitTests;

/// <summary>
/// Story Ledger Phase 2 — the deterministic half of contradiction detection.
///
/// <para>This is the half that decides what gets PAID for: every candidate it emits becomes one
/// LLM adjudication. A rule that is slightly too broad turns into a bill and a false-positive
/// flood, and a rule that silently never matches is indistinguishable from having no rule at
/// all — both failure modes this project has hit before (see
/// <c>ContinuityService.VolatilePredicates</c> and <c>SemanticFidelityService.IsIntentOutlier</c>).
/// So the matching logic is pinned hard.</para>
/// </summary>
[TestFixture]
public class PredicateExclusionTests
{
    // The seeded builtin axiom that makes the Dae-jung Seo defect expressible at all.
    private static PredicateExclusion ConstructedVsFather() => new()
    {
        Id = 1,
        UniverseId = Guid.Empty,
        PredicateA = "origin|nature|true_nature",
        ObjectPatternA = "constructed|construct|no prior life|no before",
        PredicateB = "father",
        ObjectPatternB = null,
        Symmetric = true,
        Source = "builtin",
        Status = "active",
        Rationale = "A constructed being has no biological father.",
    };

    private static ContinuityClaim Claim(
        string uid, string entityId, string predicate, string obj, string status = "NEW") => new()
    {
        ClaimUid = uid, EntityId = entityId, EntityName = "Kyle Ellen Corbin",
        Predicate = predicate, Object = obj, Status = status,
    };

    // ── object pattern matching ──────────────────────────────────────────────

    [Test]
    public void ObjectPattern_NullPattern_MatchesAnything() =>
        Assert.That(PredicateExclusionService.ObjectMatchesPattern(null, "anything at all"), Is.True);

    [Test]
    public void ObjectPattern_BlankPattern_MatchesAnything() =>
        Assert.That(PredicateExclusionService.ObjectMatchesPattern("   ", "anything at all"), Is.True);

    [Test]
    public void ObjectPattern_MatchesAnyAlternativeAsSubstring() =>
        Assert.That(PredicateExclusionService.ObjectMatchesPattern(
            "constructed|no prior life", "a Marrow configuration, no prior life before the vat"), Is.True);

    [Test]
    public void ObjectPattern_IsCaseInsensitive() =>
        Assert.That(PredicateExclusionService.ObjectMatchesPattern("CONSTRUCTED", "he was constructed"), Is.True);

    [Test]
    public void ObjectPattern_NoAlternativeMatches_IsFalse() =>
        Assert.That(PredicateExclusionService.ObjectMatchesPattern(
            "constructed|fabricated", "born in Detroit to a swordsmith"), Is.False);

    [Test]
    public void ObjectPattern_NonEmptyPatternAgainstEmptyObject_IsFalse() =>
        // A pattern that demands something cannot be satisfied by an object that says nothing.
        Assert.That(PredicateExclusionService.ObjectMatchesPattern("constructed", ""), Is.False);

    // ── predicate normalization ──────────────────────────────────────────────

    [Test]
    public void Predicate_SeparatorInsensitive()
    {
        // Extraction passes have historically used life_status / life status / life-status for
        // the same idea. An axiom that misses on a separator looks identical to no axiom.
        Assert.Multiple(() =>
        {
            Assert.That(PredicateExclusionService.PredicateEquals("life_status", "life status"), Is.True);
            Assert.That(PredicateExclusionService.PredicateEquals("life-status", "life_status"), Is.True);
            Assert.That(PredicateExclusionService.PredicateEquals("TRUE_NATURE", "true nature"), Is.True);
        });
    }

    [Test]
    public void Predicate_DistinctPredicatesStillDiffer() =>
        Assert.That(PredicateExclusionService.PredicateEquals("father", "mother"), Is.False);

    [Test]
    public void PredicatePattern_MatchesAnyAlternative()
    {
        // The seeded builtins are written this way, so if this breaks every shipped axiom
        // silently matches nothing.
        Assert.Multiple(() =>
        {
            Assert.That(PredicateExclusionService.PredicateMatchesPattern("origin|nature|true_nature", "origin"), Is.True);
            Assert.That(PredicateExclusionService.PredicateMatchesPattern("origin|nature|true_nature", "true nature"), Is.True);
            Assert.That(PredicateExclusionService.PredicateMatchesPattern("origin|nature|true_nature", "father"), Is.False);
        });
    }

    [Test]
    public void PredicatePattern_MatchesByEqualityNotSubstring()
    {
        // Load-bearing asymmetry with ObjectMatchesPattern: a predicate is an identifier. A
        // substring match here would make the `father` axiom also fire on `grandfather`.
        Assert.Multiple(() =>
        {
            Assert.That(PredicateExclusionService.PredicateMatchesPattern("father", "grandfather"), Is.False);
            Assert.That(PredicateExclusionService.PredicateMatchesPattern("father", "stepfather"), Is.False);
            Assert.That(PredicateExclusionService.PredicateMatchesPattern("father", "father"), Is.True);
        });
    }

    [Test]
    public void PredicatePattern_StarDeclaresAnAnchoredFamily()
    {
        // Real extraction vocabulary is a family, not a word: BCODA's ledger records the
        // fabricated father across father_name / father_occupation / father_profession /
        // father_status. An axiom naming only "father" matched none of them (found live).
        Assert.Multiple(() =>
        {
            Assert.That(PredicateExclusionService.PredicateMatchesPattern("father*", "father"), Is.True);
            Assert.That(PredicateExclusionService.PredicateMatchesPattern("father*", "father_name"), Is.True);
            Assert.That(PredicateExclusionService.PredicateMatchesPattern("father*", "father_occupation"), Is.True);
        });
    }

    [Test]
    public void PredicatePattern_StarStaysAnchoredAtTheStart() =>
        // The anchor is what keeps a family declaration from becoming a substring match.
        Assert.That(PredicateExclusionService.PredicateMatchesPattern("father*", "grandfather_name"), Is.False);

    [Test]
    public void PredicatePattern_StarRequiresAnUnderscoreBoundary() =>
        // "father*" must not swallow an unrelated predicate that merely starts with the letters.
        Assert.That(PredicateExclusionService.PredicateMatchesPattern("father*", "fatherless_upbringing"), Is.False);

    [Test]
    public void PredicatePattern_BareStarIsRefused() =>
        // A bare "*" would pair every predicate in the ledger with every other — a guaranteed
        // candidate explosion, and never what an author means.
        Assert.That(PredicateExclusionService.PredicateMatchesPattern("*", "anything"), Is.False);

    [Test]
    public void PredicatePattern_BlankNeverMatches() =>
        Assert.That(PredicateExclusionService.PredicateMatchesPattern("", "father"), Is.False);

    [Test]
    public void PredicatePattern_OverlappingSidesAreDetected() =>
        // "origin|nature" against "nature|father" would make a single `nature` claim contradict
        // itself, because both sides match the same row.
        Assert.That(PredicateExclusionService.SplitAlternatives("origin|nature")
            .Overlaps(PredicateExclusionService.SplitAlternatives("nature|father")), Is.True);

    // ── rule matching ────────────────────────────────────────────────────────

    [Test]
    public void Matches_TheDaeJungSeoShape()
    {
        // The exact pair that was invisible to the ledger: different predicates, incompatible
        // meaning. This is the defect that motivated the whole program.
        var father = Claim("a", "kyle", "father", "Dae-jung Seo, a craftsman who made swords");
        var origin = Claim("b", "kyle", "origin", "the tenth configuration; no before");

        Assert.That(PredicateExclusionService.Matches(ConstructedVsFather(), origin, father), Is.True);
    }

    [Test]
    public void Matches_IsSymmetric_SoLedgerOrderIsIrrelevant()
    {
        var father = Claim("a", "kyle", "father", "Dae-jung Seo");
        var origin = Claim("b", "kyle", "origin", "constructed");

        Assert.Multiple(() =>
        {
            Assert.That(PredicateExclusionService.Matches(ConstructedVsFather(), origin, father), Is.True);
            Assert.That(PredicateExclusionService.Matches(ConstructedVsFather(), father, origin), Is.True,
                "claim order in the ledger is an accident of extraction order, not meaning");
        });
    }

    [Test]
    public void Matches_DirectionalRule_DoesNotMatchReversed()
    {
        var rule = ConstructedVsFather();
        rule.Symmetric = false;
        var father = Claim("a", "kyle", "father", "Dae-jung Seo");
        var origin = Claim("b", "kyle", "origin", "constructed");

        Assert.Multiple(() =>
        {
            Assert.That(PredicateExclusionService.Matches(rule, origin, father), Is.True);
            Assert.That(PredicateExclusionService.Matches(rule, father, origin), Is.False);
        });
    }

    [Test]
    public void Matches_OriginThatIsNotConstructed_DoesNotFire()
    {
        // The object pattern is the whole safety margin: "origin" alone must not exclude having
        // a father, or every character with a recorded hometown becomes a finding.
        var father = Claim("a", "kyle", "father", "Dae-jung Seo");
        var origin = Claim("b", "kyle", "origin", "born in Detroit");

        Assert.That(PredicateExclusionService.Matches(ConstructedVsFather(), origin, father), Is.False);
    }

    // ── candidate generation ─────────────────────────────────────────────────

    [Test]
    public void Candidates_PairsOnlyClaimsAboutTheSameEntity()
    {
        // An axiom says two things cannot both be true OF ONE SUBJECT. Cross-entity pairing would
        // assert that one character being constructed forbids another from having a father.
        var claims = new List<ContinuityClaim>
        {
            Claim("a", "kyle", "origin", "constructed"),
            Claim("b", "bear", "father", "Someone Else"),
        };

        Assert.That(PredicateExclusionService.GenerateCandidates(claims, [ConstructedVsFather()]), Is.Empty);
    }

    [Test]
    public void Candidates_EmitsOneQuestionPerPair_NotTwo()
    {
        // A symmetric rule matches (A,B) and (B,A). Both must collapse to one adjudication or
        // every finding is billed and reported twice.
        var claims = new List<ContinuityClaim>
        {
            Claim("a", "kyle", "origin", "constructed"),
            Claim("b", "kyle", "father", "Dae-jung Seo"),
        };

        var candidates = PredicateExclusionService.GenerateCandidates(claims, [ConstructedVsFather()]);
        Assert.That(candidates, Has.Count.EqualTo(1));
    }

    [Test]
    public void Candidates_PairIsOrderedByUid_SoTheCacheKeyIsStable()
    {
        // The verdict cache is keyed on (A,B) in order. If generation could emit them either way
        // round, the same question would cache twice and re-bill.
        var claims = new List<ContinuityClaim>
        {
            Claim("zzz", "kyle", "father", "Dae-jung Seo"),
            Claim("aaa", "kyle", "origin", "constructed"),
        };

        var c = PredicateExclusionService.GenerateCandidates(claims, [ConstructedVsFather()]).Single();
        Assert.Multiple(() =>
        {
            Assert.That(c.A.ClaimUid, Is.EqualTo("aaa"));
            Assert.That(c.B.ClaimUid, Is.EqualTo("zzz"));
        });
    }

    [Test]
    public void Candidates_IgnoresRejectedAndSupersededClaims()
    {
        // A claim a human already ruled out must not be re-raised — and must not be billed for.
        foreach (var deadStatus in new[] { "REJECTED", "SUPERSEDED" })
        {
            var claims = new List<ContinuityClaim>
            {
                Claim("a", "kyle", "origin", "constructed"),
                Claim("b", "kyle", "father", "Dae-jung Seo", deadStatus),
            };
            Assert.That(PredicateExclusionService.GenerateCandidates(claims, [ConstructedVsFather()]),
                Is.Empty, $"a {deadStatus} claim must not generate a candidate");
        }
    }

    [Test]
    public void Candidates_CanonicalClaimsAreStillPaired()
    {
        // The opposite of the rule above: a CANONICAL fact is exactly the thing a new, silently
        // drifting claim must be checked against (same reasoning as ContinuityService.Upsert's
        // 2026 fix that stopped excluding canon from conflict detection).
        var claims = new List<ContinuityClaim>
        {
            Claim("a", "kyle", "origin", "constructed", "CANONICAL"),
            Claim("b", "kyle", "father", "Dae-jung Seo"),
        };

        Assert.That(PredicateExclusionService.GenerateCandidates(claims, [ConstructedVsFather()]), Has.Count.EqualTo(1));
    }

    [Test]
    public void Candidates_NoRules_MeansNoCandidates()
    {
        var claims = new List<ContinuityClaim>
        {
            Claim("a", "kyle", "origin", "constructed"),
            Claim("b", "kyle", "father", "Dae-jung Seo"),
        };

        Assert.That(PredicateExclusionService.GenerateCandidates(claims, []), Is.Empty);
    }

    [Test]
    public void Candidates_SingleClaimEntity_IsSkipped() =>
        Assert.That(PredicateExclusionService.GenerateCandidates(
            [Claim("a", "kyle", "origin", "constructed")], [ConstructedVsFather()]), Is.Empty);

    [Test]
    public void Candidates_TheSeitoCaseDoesNotFire()
    {
        // BCODA canon: Kyle DID have a mentor called Seito, and Seito was later revealed to be a
        // personality construct. Both are true at once. No seeded axiom may pair a "construct"
        // claim against `mentor` — a rule that flags correct canon is worse than no rule, and
        // this is the concrete case that kept that axiom out of the seed.
        var claims = new List<ContinuityClaim>
        {
            Claim("a", "kyle", "mentor", "Seito, who worked him for nine years"),
            Claim("b", "kyle", "origin", "constructed; no before"),
        };

        Assert.That(PredicateExclusionService.GenerateCandidates(claims, [ConstructedVsFather()]), Is.Empty);
    }

    // ── collapsing ───────────────────────────────────────────────────────────

    [Test]
    public void Collapse_ReducesAFamilyToOneQuestion()
    {
        // Measured live: BCODA's ledger records the fabricated father under ~13 predicate names
        // and the constructed origin under ~5. The raw cross product asks ONE question 65 times.
        var claims = new List<ContinuityClaim>
        {
            Claim("o1", "kyle", "origin", "constructed"),
            Claim("o2", "kyle", "construction_type", "Marrow program composite"),
            Claim("f1", "kyle", "father_name", "Dae-jung Seo"),
            Claim("f2", "kyle", "father_occupation", "craftsman, sword maker"),
            Claim("f3", "kyle", "father_status", "dead"),
        };
        var rule = ConstructedVsFather();
        rule.PredicateA = "origin*|construction_type*";
        rule.PredicateB = "father*";
        // The widened object pattern the shipped axiom uses — without "composite"/"marrow" the
        // construction_type claim is (correctly) not matched at all, which is itself the
        // calibration lesson: the object filter is as load-bearing as the predicate one.
        rule.ObjectPatternA = "constructed|construct|composite|marrow|no before";

        var raw = PredicateExclusionService.GenerateCandidates(claims, [rule]);
        var collapsed = PredicateExclusionService.Collapse(raw);

        Assert.Multiple(() =>
        {
            Assert.That(raw, Has.Count.EqualTo(6), "2 origin-side x 3 father-side");
            Assert.That(collapsed, Has.Count.EqualTo(1), "one entity, one axiom, one question");
            Assert.That(collapsed[0].FamilySize, Is.EqualTo(6),
                "the finding must still report how many pairs it stands for");
        });
    }

    [Test]
    public void Collapse_KeepsDifferentEntitiesSeparate()
    {
        var claims = new List<ContinuityClaim>
        {
            Claim("o1", "kyle", "origin", "constructed"),
            Claim("f1", "kyle", "father", "Dae-jung Seo"),
            Claim("o2", "bear", "origin", "constructed"),
            Claim("f2", "bear", "father", "Someone Else"),
        };

        var collapsed = PredicateExclusionService.Collapse(
            PredicateExclusionService.GenerateCandidates(claims, [ConstructedVsFather()]));

        Assert.That(collapsed, Has.Count.EqualTo(2), "an axiom is per-subject, so each entity is its own question");
    }

    [Test]
    public void Collapse_PrefersTheBestAnchoredRepresentative()
    {
        // An unanchored pair cannot be adjudicated against prose at all, so it must never be
        // chosen as the representative when an anchored pair exists.
        var anchored = Claim("f1", "kyle", "father_name", "Dae-jung Seo");
        anchored.SourceBeatId = Guid.CreateVersion7();
        anchored.Snippet = "Your father's name was Dae-jung Seo";
        var unanchored = Claim("f0", "kyle", "father_status", "dead");
        var origin = Claim("o1", "kyle", "origin", "constructed");
        origin.SourceBeatId = Guid.CreateVersion7();

        var rule = ConstructedVsFather();
        rule.PredicateB = "father*";   // the claims here are father_name / father_status

        var collapsed = PredicateExclusionService.Collapse(
            PredicateExclusionService.GenerateCandidates([origin, unanchored, anchored], [rule]));

        var chosen = collapsed.Single();
        Assert.That(new[] { chosen.A.ClaimUid, chosen.B.ClaimUid }, Contains.Item("f1"),
            "the anchored, snippet-carrying claim must be the representative");
    }

    [Test]
    public void Collapse_RepresentativeIsStableAcrossRuns()
    {
        // The verdict cache key includes the representative's uids, so an unstable choice would
        // re-bill the same question every run.
        var claims = new List<ContinuityClaim>
        {
            Claim("o1", "kyle", "origin", "constructed"),
            Claim("f1", "kyle", "father_name", "A"),
            Claim("f2", "kyle", "father_status", "B"),
        };
        var rule = ConstructedVsFather();
        rule.PredicateB = "father*";

        var first = PredicateExclusionService.Collapse(PredicateExclusionService.GenerateCandidates(claims, [rule])).Single();
        var second = PredicateExclusionService.Collapse(PredicateExclusionService.GenerateCandidates(claims, [rule])).Single();

        Assert.Multiple(() =>
        {
            Assert.That(second.A.ClaimUid, Is.EqualTo(first.A.ClaimUid));
            Assert.That(second.B.ClaimUid, Is.EqualTo(first.B.ClaimUid));
        });
    }

    // ── the mechanical grounding gate ────────────────────────────────────────

    [Test]
    public void Grounding_AcceptsAQuotePresentInTheProse() =>
        Assert.That(TunedReadService.QuoteAppearsIn(
            "Your father's name was Dae-jung Seo",
            "[Beat #543]\nMrs. Chen said it plainly. Your father's name was Dae-jung Seo. He made swords."),
            Is.True);

    [Test]
    public void Grounding_NormalizesWhitespaceOnBothSides() =>
        Assert.That(TunedReadService.QuoteAppearsIn(
            "Your father's   name\nwas Dae-jung Seo",
            "Your father's name was Dae-jung Seo."), Is.True);

    [Test]
    public void Grounding_RejectsAQuoteThatIsNotThere() =>
        // This single behaviour is what makes the instrument incapable of the failure it exists
        // to catch: an unquotable assertion about the prose is discarded, not filed.
        Assert.That(TunedReadService.QuoteAppearsIn(
            "Your father was a swordsmith in Osaka",
            "Mrs. Chen said nothing about anyone's father."), Is.False);

    [Test]
    public void Grounding_RejectsAnEmptyOrTrivialQuote()
    {
        Assert.Multiple(() =>
        {
            Assert.That(TunedReadService.QuoteAppearsIn(null, "some prose"), Is.False);
            Assert.That(TunedReadService.QuoteAppearsIn("", "some prose"), Is.False);
            Assert.That(TunedReadService.QuoteAppearsIn("father", "his father"), Is.False,
                "a fragment too short to be evidence must fail closed, not pass on a substring hit");
        });
    }

    [Test]
    public void Grounding_FailsClosedWhenThereIsNoProse() =>
        Assert.That(TunedReadService.QuoteAppearsIn("a perfectly real quote here", ""), Is.False);

    // ── verdict parsing ──────────────────────────────────────────────────────

    [Test]
    public void Verdict_ParsesAContradiction()
    {
        var ok = TunedReadService.TryParseVerdict(
            """{"contradiction": true, "severity": "BLOCKER", "quote": "no before", "note": "cannot have a father"}""",
            out var isC, out var sev, out var quote, out var note);

        Assert.Multiple(() =>
        {
            Assert.That(ok, Is.True);
            Assert.That(isC, Is.True);
            Assert.That(sev, Is.EqualTo("BLOCKER"));
            Assert.That(quote, Is.EqualTo("no before"));
            Assert.That(note, Is.EqualTo("cannot have a father"));
        });
    }

    [Test]
    public void Verdict_ToleratesFencesAndSurroundingProse()
    {
        var ok = TunedReadService.TryParseVerdict(
            "Here is my answer:\n```json\n{\"contradiction\": false, \"note\": \"reconciled later\"}\n```\n",
            out var isC, out _, out _, out var note);

        Assert.Multiple(() =>
        {
            Assert.That(ok, Is.True);
            Assert.That(isC, Is.False);
            Assert.That(note, Is.EqualTo("reconciled later"));
        });
    }

    [Test]
    public void Verdict_UnparseableResponseIsNotAContradiction()
    {
        var ok = TunedReadService.TryParseVerdict("I think they conflict, honestly.", out var isC, out _, out _, out _);
        Assert.Multiple(() =>
        {
            Assert.That(ok, Is.False);
            Assert.That(isC, Is.False, "a response we could not parse must never read as a contradiction");
        });
    }

    [Test]
    public void Verdict_UnknownSeverityFallsBackToModerate()
    {
        TunedReadService.TryParseVerdict(
            """{"contradiction": true, "severity": "CATASTROPHIC", "quote": "x"}""",
            out _, out var sev, out _, out _);
        Assert.That(sev, Is.EqualTo("MODERATE"));
    }

    // ── the verdict cache key ────────────────────────────────────────────────

    [Test]
    public void CacheKey_IsStableForTheSameQuestion()
    {
        var first = TunedReadService.ComputeCacheKey("a", "b", 1, "h1", "h2");
        var second = TunedReadService.ComputeCacheKey("a", "b", 1, "h1", "h2");
        Assert.That(second, Is.EqualTo(first));
    }

    [Test]
    public void CacheKey_ChangesWhenAnchorProseChanges() =>
        // A verdict is a judgement about specific prose. If that prose changes the verdict is
        // stale even though both claim rows are byte-identical — keying on claim uids alone
        // would cache the wrong answer past the fix.
        Assert.That(TunedReadService.ComputeCacheKey("a", "b", 1, "h1", "h2"),
            Is.Not.EqualTo(TunedReadService.ComputeCacheKey("a", "b", 1, "h1-CHANGED", "h2")));

    [Test]
    public void CacheKey_ChangesWhenTheAxiomChanges() =>
        Assert.That(TunedReadService.ComputeCacheKey("a", "b", 1, "h1", "h2"),
            Is.Not.EqualTo(TunedReadService.ComputeCacheKey("a", "b", 2, "h1", "h2")));

    [Test]
    public void CacheKey_HandlesUnanchoredClaims() =>
        Assert.That(TunedReadService.ComputeCacheKey("a", "b", 1, null, null), Is.Not.Empty);

    // ── provenance grades ────────────────────────────────────────────────────

    [Test]
    public void Provenance_OnlyAuthoredAndObservedAreTrustworthy()
    {
        Assert.Multiple(() =>
        {
            Assert.That(ClaimProvenance.IsTrustworthy(ClaimProvenance.Authored), Is.True);
            Assert.That(ClaimProvenance.IsTrustworthy(ClaimProvenance.Observed), Is.True);
            Assert.That(ClaimProvenance.IsTrustworthy(ClaimProvenance.Inferred), Is.False);
            Assert.That(ClaimProvenance.IsTrustworthy(ClaimProvenance.Scaffolded), Is.False,
                "scaffolded is never canon — it is a candidate");
            Assert.That(ClaimProvenance.IsTrustworthy(ClaimProvenance.LegacyUnknown), Is.False);
            Assert.That(ClaimProvenance.IsTrustworthy(null), Is.False);
        });
    }

    [Test]
    public void Provenance_NewClaimsDefaultToInferred() =>
        // Defaulting to the weakest believable grade means a writer that forgets to grade cannot
        // accidentally launder a guess into unqualified canon.
        Assert.That(new ContinuityClaim().Provenance, Is.EqualTo(ClaimProvenance.Inferred));
}
