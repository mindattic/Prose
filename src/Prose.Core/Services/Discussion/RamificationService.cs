using System.Text;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services.Contradiction;
using Prose.Core.Services.Obligations;

namespace Prose.Core.Services.Discussion;

/// <summary>
/// One thing a passage is carrying that an edit disagrees with.
/// </summary>
/// <param name="Kind">A short slug — <c>obligation</c>, <c>continuity</c>, <c>entity</c>,
/// <c>gear</c>, <c>summary</c>, <c>discussion</c>. Grouped on, not parsed.</param>
/// <param name="Headline">What happened, in one line the author can act on.</param>
/// <param name="Detail">The evidence — the quote that vanished, the fact that disagrees.</param>
/// <param name="Fork">The three ways out, in the author's words. Offered as a choice rather than
/// resolved, because which one is right is a judgement about the story.</param>
public sealed record Mismatch(
    string Kind,
    string Headline,
    string? Detail,
    IReadOnlyList<string> Fork);

/// <summary>What a passage is carrying, and what an edit to it would cost.</summary>
/// <param name="ObligationsOpenedHere">Promises this beat makes. Cutting the line one of these was
/// opened on leaves the payoff dangling with nothing to point back at.</param>
/// <param name="ObligationsPaidHere">Promises this beat DELIVERS. The most dangerous thing on the
/// page: cut this and a promise the book made earlier stops being kept, and no other instrument
/// in the system notices.</param>
/// <param name="EstablishedFacts">What the record already says about the people in this beat.
/// Fed to the contradiction checker as ground truth, and shown to the assistant so it can point
/// at a specific fact instead of saying "this seems load-bearing".</param>
/// <param name="Locks">The author's own immovable statements for this book, verbatim.</param>
/// <param name="DependentBeatCount">How many beats share an entity with this one — the reach of
/// any change made here. NULL when the reach could not be determined, which is a different answer
/// from zero and is reported as one.</param>
public sealed record CarriedWeight(
    IReadOnlyList<NarrativeObligation> ObligationsOpenedHere,
    IReadOnlyList<NarrativeObligation> ObligationsPaidHere,
    IReadOnlyList<OnScreenFact> EstablishedFacts,
    IReadOnlyList<string> Locks,
    int? DependentBeatCount,
    IReadOnlyList<int> NearestDependents);

/// <summary>
/// Everything a passage is carrying, and what an edit to it broke.
///
/// <para><b>One service, two moments.</b> Before a change it is advice — the evidence the Discuss
/// panel needs to say "cutting this breaks the payoff in #412" instead of guessing. After a change
/// it is a gate. The question is identical either way, so there is one implementation of it; two
/// would drift, and the author would get different answers depending on which door they came in.</para>
///
/// <para><b>Everything here is free and exact.</b> Not one check asks a model whether the prose
/// still means what it meant. That is deliberate and it is the whole reason this is trustworthy
/// enough to block on: the obligation EXTRACTOR measured a 96% false-positive rate on prose
/// engineered to owe nothing, while the obligation LEDGER records the literal quote a promise was
/// opened on — so "is that quote still in the beat" is a substring test that cannot be wrong.
/// Corpus-wide, instruments that check against facts the author FIXED work; instruments that check
/// against an inferred plan produce noise (the altitude audit: nine findings, nine false).</para>
///
/// <para><b>It resolves nothing.</b> Every mismatch comes back with the same three-way fork,
/// because which way it resolves is a judgement about the story and RFC 0009 puts that with the
/// author. A finding describes; it never instructs.</para>
/// </summary>
public sealed class RamificationService(
    IDbContextFactory<ProseDbContext> dbFactory,
    BlastRadiusService blastRadius,
    ContinuityService continuity,
    GearCarryEnforcer gearCarry)
{
    private static readonly string[] ProseFork =
    [
        "The prose is right — update the record.",
        "The record is right — put the prose back.",
        "Leave both for now, and remember why.",
    ];

    // ── Before a change: what is this passage carrying? ────────────────────

    /// <summary>
    /// The evidence an assessment needs, for one beat.
    /// </summary>
    /// <remarks>
    /// Every read here is free. That matters more than it sounds: this block is assembled on every
    /// question the author asks about a passage, and a version of it that cost a model call would
    /// be switched off within a week.
    /// </remarks>
    public async Task<CarriedWeight> CarriedByAsync(
        Guid bookNodeId, Guid beatId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var citing = await db.NarrativeObligations.AsNoTracking()
            .Where(o => o.NodeId == bookNodeId
                        && (o.OriginBeatId == beatId || o.ClosingBeatId == beatId))
            .ToListAsync(ct);

        var locks = await db.NodeOutlineSections.AsNoTracking()
            .Where(n => n.NodeId == bookNodeId
                        && (n.SectionType == "NarrativeLocks" || n.SectionType == "AuthorNotes"))
            .Select(n => n.Content)
            .ToListAsync(ct);

        var facts = await EstablishedFactsAsync(db, beatId, ct);

        // The blast radius is three cheap queries and is the honest answer to "how far does this
        // reach" — same-chapter neighbours plus every beat anywhere that shares an entity.
        //
        // Isolated, because one of those queries is raw SQL against BeatEntityPresence, a table
        // with no EF mapping: it does not exist on SQLite at all, and on SQL Server it can lag a
        // freshly imported book. The reach is context; the obligations and recorded facts below
        // are the substance, and losing the first must not cost the author the second.
        int? reach = null;
        var nearest = new List<int>();
        try
        {
            var dependents = await blastRadius.GetBlastRadiusBeatIdsAsync(beatId, ct: ct);
            reach = Math.Max(0, dependents.Count - 1);
            nearest = await db.Beats.AsNoTracking()
                .Where(b => dependents.Contains(b.Id) && b.Id != beatId)
                .OrderBy(b => b.Number)
                .Select(b => b.Number)
                .Take(6)
                .ToListAsync(ct);
        }
        catch (Exception) { /* reported as unknown below, never as zero */ }

        return new CarriedWeight(
            citing.Where(o => o.OriginBeatId == beatId).ToList(),
            citing.Where(o => o.ClosingBeatId == beatId).ToList(),
            facts,
            locks.Where(l => !string.IsNullOrWhiteSpace(l)).ToList(),
            reach,
            nearest);
    }

    /// <summary>
    /// What the record already says about the entities present in this beat.
    /// </summary>
    /// <remarks>
    /// <para>CANONICAL and CONFIRMED only, and volatile predicates excluded. A location or a
    /// posture is a character's LAST KNOWN state, not a permanent claim — ~14 of 24 BCODA findings
    /// were exactly that mistake, which is why <see cref="ContinuityService.IsVolatilePredicate"/>
    /// exists and why it is applied here rather than left for the reader of the list to remember.</para>
    /// </remarks>
    private async Task<IReadOnlyList<OnScreenFact>> EstablishedFactsAsync(
        ProseDbContext db, Guid beatId, CancellationToken ct)
    {
        var entityIds = await db.BeatEntityMentions.AsNoTracking()
            .Where(m => m.BeatId == beatId)
            .Select(m => m.EntityId)
            .Distinct()
            .ToListAsync(ct);
        if (entityIds.Count == 0) return [];

        var facts = new List<OnScreenFact>();
        foreach (var id in entityIds)
        {
            List<ContinuityClaim> claims;
            // The ledger is a separate store; an unreachable one must degrade to "no facts", not
            // take the whole assessment down.
            try { claims = continuity.GetByEntity(id.ToString()); }
            catch { continue; }

            foreach (var claim in claims)
            {
                if (claim.Status is not ("CANONICAL" or "CONFIRMED")) continue;
                if (ContinuityService.IsVolatilePredicate(claim.Predicate)) continue;
                facts.Add(new OnScreenFact(
                    claim.EntityName, claim.Predicate, claim.Object,
                    $"claim:{claim.Status}", claim.Snippet));
            }
        }

        // Bounded. A POV character can carry hundreds of claims, and a fact list longer than the
        // prose it is checking drowns the thing it is meant to inform.
        return facts.Take(40).ToList();
    }

    // ── After a change: what did it break? ─────────────────────────────────

    /// <summary>
    /// Check a beat against everything that cites it.
    /// </summary>
    /// <param name="removedText">Reader-visible text the edit took out, so a lost entity link can
    /// be named. By the time this runs the beat's own mentions index has been re-derived against
    /// the NEW text, so what went can only come from here.</param>
    public async Task<IReadOnlyList<Mismatch>> ReviewAsync(
        Guid bookNodeId, Guid beatId, string? removedText = null, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var beat = await db.Beats.AsNoTracking().FirstOrDefaultAsync(b => b.Id == beatId, ct);
        if (beat is null) return [];

        var plain = BeatDiscussTarget.PlainText(beat.Text);
        var found = new List<Mismatch>();

        await CheckObligationsAsync(db, bookNodeId, beatId, plain, found, ct);
        await CheckEntityTagsAsync(db, beatId, beat.Text, removedText, found, ct);
        CheckSummaries(beat, found);
        await CheckOtherDiscussionsAsync(db, beatId, plain, found, ct);
        await CheckContinuityAsync(db, bookNodeId, beatId, found, ct);
        await CheckGearAsync(db, beatId, plain, found, ct);

        return found;
    }

    /// <summary>
    /// An obligation whose evidence the edit deleted.
    ///
    /// <para>The highest-value check in the set and the cheapest: an obligation records the exact
    /// quote that opened or closed it, so "is that quote still there" is a substring test. A
    /// closing quote that has gone means a promise the book no longer pays off, and nothing else
    /// in the system would notice.</para>
    /// </summary>
    private static async Task CheckObligationsAsync(
        ProseDbContext db, Guid bookNodeId, Guid beatId, string plain,
        List<Mismatch> found, CancellationToken ct)
    {
        var citing = await db.NarrativeObligations.AsNoTracking()
            .Where(o => o.NodeId == bookNodeId
                        && (o.OriginBeatId == beatId || o.ClosingBeatId == beatId))
            .ToListAsync(ct);

        foreach (var o in citing)
        {
            if (o.OriginBeatId == beatId && Missing(plain, o.OriginQuote))
                found.Add(new Mismatch(
                    "obligation",
                    $"The line that opened an obligation is gone: \"{Shorten(o.Description, 90)}\"",
                    $"It was opened on this quote, which is no longer in the beat: \"{Shorten(o.OriginQuote, 120)}\"",
                    ProseFork));

            if (o.ClosingBeatId == beatId && Missing(plain, o.ClosingQuote))
                found.Add(new Mismatch(
                    "obligation",
                    $"The line that PAID OFF an obligation is gone: \"{Shorten(o.Description, 90)}\"",
                    $"It was closed on this quote, which is no longer in the beat: "
                    + $"\"{Shorten(o.ClosingQuote, 120)}\". The promise is now unpaid.",
                    ProseFork));
        }
    }

    /// <summary>
    /// A recorded fact this beat's entities now disagree with.
    /// </summary>
    /// <remarks>
    /// Read from <see cref="ContinuityService"/>, which is deterministic and free: same entity,
    /// same predicate, different object, with numeric normalisation ("fifty" == "50"). Volatile
    /// predicates are excluded by the service itself.
    /// </remarks>
    private async Task CheckContinuityAsync(
        ProseDbContext db, Guid bookNodeId, Guid beatId, List<Mismatch> found, CancellationToken ct)
    {
        var slug = await db.Nodes.AsNoTracking().IgnoreQueryFilters()
            .Where(n => n.Id == bookNodeId).Select(n => n.Slug).FirstOrDefaultAsync(ct);
        if (string.IsNullOrEmpty(slug)) return;

        var present = await db.BeatEntityMentions.AsNoTracking()
            .Where(m => m.BeatId == beatId)
            .Select(m => m.EntityId.ToString())
            .ToListAsync(ct);
        if (present.Count == 0) return;

        var here = present.ToHashSet(StringComparer.OrdinalIgnoreCase);

        List<ContradictionGroup> groups;
        try
        {
            groups = continuity.GetContradictionGroups(slug, excludeVolatile: true);
        }
        catch (Exception ex)
        {
            // The ledger being unreachable is "could not look", which is a different answer from
            // "nothing found" and must never be reported as the second.
            found.Add(new Mismatch("continuity",
                "The continuity ledger could not be read, so nothing was checked against it.",
                ex.Message, ProseFork));
            return;
        }

        // Only the groups involving someone actually in this beat. A book-wide contradiction the
        // author did not just touch is the ledger's business, not this save's.
        foreach (var g in groups.Where(g => here.Contains(g.EntityId)).Take(10))
        {
            var objects = g.Claims.Select(c => c.Object).Distinct().Take(3);
            found.Add(new Mismatch(
                "continuity",
                $"{g.EntityName} — the record disagrees with itself about {g.Predicate}.",
                "Recorded: " + string.Join(" · ", objects.Select(o => $"\"{Shorten(o, 60)}\"")),
                ProseFork));
        }
    }

    /// <summary>
    /// Gear a character is using that they were never given.
    /// </summary>
    /// <remarks>
    /// <see cref="GearCarryEnforcer"/> is a deterministic graph check that has been written and
    /// registered for months while being called by nothing but the CLI — <c>PostBeatValidationService</c>
    /// holds a reference to it that <c>UpdateBeatTextAsync</c> uses only to look up a slug.
    /// </remarks>
    private async Task CheckGearAsync(
        ProseDbContext db, Guid beatId, string plain, List<Mismatch> found, CancellationToken ct)
    {
        var characters = await db.BeatEntityMentions.AsNoTracking()
            .Where(m => m.BeatId == beatId && m.EntityType == "character")
            .Select(m => new { m.EntityId, m.EntityName })
            .Distinct()
            .ToListAsync(ct);

        foreach (var c in characters)
        {
            List<GearUsageViolation> violations;
            try
            {
                violations = await gearCarry.EnforceAsync(plain, c.EntityId, null, beatId, ct);
            }
            catch
            {
                // Best-effort: a gear check that cannot run must not stop the save from reporting
                // the checks that did.
                continue;
            }

            foreach (var v in violations.Take(3))
                found.Add(new Mismatch(
                    "gear",
                    $"{v.CharacterName} {v.VerbUsed} \"{v.GearName}\" — nothing gave it to them.",
                    "There is no carry or wield edge for it. Either add one, or the prose is "
                    + "reaching for something the character does not have.",
                    ProseFork));
        }
    }

    private static async Task CheckEntityTagsAsync(
        ProseDbContext db, Guid beatId, string? storedText, string? removedText,
        List<Mismatch> found, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(removedText)) return;

        var goneGuids = BeatMarkup.ExtractEntityGuids(removedText).ToList();
        if (goneGuids.Count == 0) return;

        var stillThere = BeatMarkup.ExtractEntityGuids(storedText).ToHashSet();
        var lost = goneGuids.Where(g => !stillThere.Contains(g)).ToList();
        if (lost.Count == 0) return;

        var names = await db.Entities.AsNoTracking()
            .Where(e => lost.Contains(e.Id))
            .Select(e => e.Name)
            .ToListAsync(ct);
        if (names.Count == 0) return;

        found.Add(new Mismatch(
            "entity",
            names.Count == 1
                ? $"{names[0]} is no longer mentioned in this beat."
                : $"{names.Count} entities are no longer mentioned in this beat.",
            string.Join(", ", names)
            + ". If they are still present in the scene, the beat no longer says so.",
            ProseFork));
    }

    private static void CheckSummaries(Beat beat, List<Mismatch> found)
    {
        if (!string.IsNullOrWhiteSpace(beat.Description) && beat.DescriptionState is "stale")
            found.Add(new Mismatch(
                "summary",
                "The beat's stated intent was written against different prose.",
                $"Intent: \"{Shorten(beat.Description, 140)}\"",
                ProseFork));

        if (!string.IsNullOrWhiteSpace(beat.EventSummary) && beat.EventSummaryState is "stale")
            found.Add(new Mismatch(
                "summary",
                "The beat's recorded event summary was written against different prose.",
                $"Recorded: \"{Shorten(beat.EventSummary, 140)}\"",
                ProseFork));
    }

    private static async Task CheckOtherDiscussionsAsync(
        ProseDbContext db, Guid beatId, string plain, List<Mismatch> found, CancellationToken ct)
    {
        var threads = await db.DiscussionThreads.AsNoTracking()
            .Where(t => t.BeatId == beatId && t.State != DiscussionThreadState.Resolved)
            .Select(t => new { t.Id, t.AnchorQuote, t.Title })
            .ToListAsync(ct);

        var detached = threads.Where(t => Missing(plain, t.AnchorQuote)).ToList();
        if (detached.Count == 0) return;

        found.Add(new Mismatch(
            "discussion",
            detached.Count == 1
                ? "Another discussion on this beat no longer points at anything."
                : $"{detached.Count} other discussions on this beat no longer point at anything.",
            string.Join(" · ", detached.Select(t => $"\"{Shorten(t.Title ?? t.AnchorQuote, 70)}\"")),
            ProseFork));
    }

    // ── The gate ───────────────────────────────────────────────────────────

    /// <summary>
    /// What should stop the author, on a beat they just finished.
    /// </summary>
    /// <remarks>
    /// <para><see cref="ReviewAsync"/> minus everything they have already said they meant, at this
    /// exact wording. That filter is not a convenience — it is what keeps a blocking gate usable.
    /// Some of what this finds will be wrong, and without a memory one false positive blocks every
    /// save of that beat forever.</para>
    /// </remarks>
    public async Task<IReadOnlyList<Mismatch>> GateAsync(
        Guid bookNodeId, Guid beatId, CancellationToken ct = default)
    {
        var found = await ReviewAsync(bookNodeId, beatId, ct: ct);
        if (found.Count == 0) return found;

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var hash = await db.Beats.AsNoTracking()
            .Where(b => b.Id == beatId).Select(b => b.TextHash).FirstOrDefaultAsync(ct);
        if (string.IsNullOrEmpty(hash)) return found;

        var dismissed = await db.BeatCheckDismissals.AsNoTracking()
            .Where(d => d.BeatId == beatId && d.TextHash == hash)
            .Select(d => d.CheckKey)
            .ToListAsync(ct);
        if (dismissed.Count == 0) return found;

        var silenced = dismissed.ToHashSet(StringComparer.Ordinal);
        return found.Where(m => !silenced.Contains(KeyFor(m))).ToList();
    }

    /// <summary>Record that the author meant it, for this beat at this exact wording.</summary>
    public async Task DismissAsync(
        Guid beatId, Mismatch mismatch, string? note = null, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var hash = await db.Beats.AsNoTracking()
            .Where(b => b.Id == beatId).Select(b => b.TextHash).FirstOrDefaultAsync(ct);
        // No hash means nothing to pin the dismissal to, and a dismissal that cannot expire is
        // worse than none — it would silence this check on that beat for the life of the book.
        if (string.IsNullOrEmpty(hash)) return;

        var key = KeyFor(mismatch);
        var already = await db.BeatCheckDismissals
            .AnyAsync(d => d.BeatId == beatId && d.TextHash == hash && d.CheckKey == key, ct);
        if (already) return;

        db.BeatCheckDismissals.Add(new BeatCheckDismissal
        {
            BeatId = beatId,
            TextHash = hash,
            CheckKey = key,
            Headline = mismatch.Headline.Length <= 400 ? mismatch.Headline : mismatch.Headline[..400],
            Note = note,
        });
        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// A stable identity for one finding.
    /// </summary>
    /// <remarks>
    /// Kind plus a hash of the headline, not the kind alone: a beat can carry two obligations and
    /// the author may mean one of them and not the other. Keying on the kind would silence both
    /// from one click, which is how a gate quietly stops gating.
    /// </remarks>
    public static string KeyFor(Mismatch m)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(m.Headline.Trim()));
        return m.Kind + ":" + Convert.ToHexString(bytes)[..32].ToLowerInvariant();
    }

    // ── Rendering ──────────────────────────────────────────────────────────

    /// <summary>
    /// The evidence block the assistant is given before it judges a change.
    ///
    /// <para>Written as facts with their sources, never as instructions. The prompt that consumes
    /// it asks the model to NAME something from this block or say plainly that nothing here is at
    /// risk — which is only answerable because the block distinguishes "nothing is carried" from
    /// "nothing was looked at".</para>
    /// </summary>
    public static string ToPrompt(CarriedWeight w)
    {
        var sb = new StringBuilder();
        sb.AppendLine("CARRIED BY THIS PASSAGE (measured from the record, not recalled):");

        if (w.ObligationsOpenedHere.Count == 0 && w.ObligationsPaidHere.Count == 0)
            sb.AppendLine("  OBLIGATIONS: this beat neither opens nor pays off any recorded promise.");

        foreach (var o in w.ObligationsOpenedHere)
            sb.AppendLine($"  OPENS A PROMISE: \"{Shorten(o.Description, 120)}\" — due {o.DueByKind}"
                          + (o.ClosingBeatId is null ? ", NOT YET PAID OFF" : ", paid off later"));

        foreach (var o in w.ObligationsPaidHere)
            sb.AppendLine($"  PAYS OFF A PROMISE: \"{Shorten(o.Description, 120)}\" — cutting the "
                          + "line it closes on leaves that promise unpaid, and nothing else in the "
                          + "system will notice."
                          + (o.ClosingQuote is { Length: > 0 }
                              ? $" It closes on: \"{Shorten(o.ClosingQuote, 100)}\""
                              : ""));

        if (w.EstablishedFacts.Count > 0)
        {
            sb.AppendLine("  ALREADY ESTABLISHED ABOUT WHO IS HERE:");
            foreach (var f in w.EstablishedFacts)
                sb.AppendLine($"    {f.EntityName} — {f.Predicate}: {f.Value} ({f.Source})");
        }
        else
        {
            sb.AppendLine("  ALREADY ESTABLISHED ABOUT WHO IS HERE: nothing is on record. That means "
                          + "the ledger is empty for these entities, NOT that the passage is free of "
                          + "commitments.");
        }

        if (w.Locks.Count > 0)
        {
            sb.AppendLine("  THE AUTHOR'S OWN IMMOVABLE STATEMENTS FOR THIS BOOK:");
            foreach (var l in w.Locks) sb.AppendLine("    " + l.Replace("\n", "\n    "));
        }

        sb.AppendLine(w.DependentBeatCount is { } reach
            ? $"  REACH: {reach} other beat(s) share an entity with this one"
              + (w.NearestDependents.Count > 0
                  ? $" (e.g. {string.Join(", ", w.NearestDependents.Select(n => "#" + n))})."
                  : ".")
            // Unknown, not zero. Reporting a failed lookup as "nothing depends on this" would be
            // the single most dangerous sentence in this block.
            : "  REACH: could not be determined. Do not read that as nothing depending on this beat.");

        return sb.ToString();
    }

    private static bool Missing(string plain, string? quote)
        => !string.IsNullOrWhiteSpace(quote)
           && !plain.Contains(quote.Trim(), StringComparison.Ordinal);

    private static string Shorten(string? s, int max)
    {
        s = (s ?? "").Replace('\n', ' ').Replace('\r', ' ').Trim();
        return s.Length <= max ? s : s[..(max - 1)] + "…";
    }
}
