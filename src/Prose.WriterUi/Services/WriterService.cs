using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;
using Prose.Core.Services.Discussion;

namespace Prose.WriterUi.Services;

/// <summary>
/// Everything the editor needs from the engine, in one place. Runs in-process inside Prose.Hub,
/// so it calls Core services directly rather than going back out over HTTP — "only Hub reaches
/// the DB" is satisfied by being the Hub.
///
/// <para>Every method that touches a book scopes itself to that book's universe first (see
/// <see cref="ScopeToBookAsync"/>). This is not optional: universe scoping is ambient, and a save
/// that runs with an unresolved universe strips every entity tag out of the prose and adds none
/// back.</para>
/// </summary>
public sealed class WriterService(
    IDbContextFactory<ProseDbContext> dbFactory,
    NodeWorkbenchService workbench,
    EntityLookupService entityLookup,
    BlastRadiusService blastRadius,
    Prose.Core.Services.Audit.LogicSweepService logicSweep,
    DocxExportService docx,
    ManuscriptExportService manuscript,
    TokenLedger ledger,
    EditSessionService editSessions,
    Prose.Core.Services.Discussion.RamificationService ramifications,
    Prose.Core.Services.Contradiction.NarrativeContradictionChecker contradictions,
    FindingsService findings,
    IUniverseContext universe)
{
    /// <summary>A book the author can open.</summary>
    public sealed record BookListItem(Guid Id, string Slug, string Title, string? NodeCode);

    /// <summary>
    /// One row in the left-hand list.
    /// </summary>
    /// <param name="Label">Pre-rendered as <c>#0122 - Chapter 3: Teeth</c>.</param>
    /// <param name="ChapterTitle">So the list can GROUP by chapter instead of repeating the
    /// chapter's name on all thirty of its rows.</param>
    /// <param name="Summary">The beat's own opening words. This is what makes the list navigable
    /// at all: five hundred rows reading <c>#0122 - Chapter 3: Teeth</c>,
    /// <c>#0123 - Chapter 3: Teeth</c> carry one bit of information between them, which is that
    /// the book is long. Always the prose itself — there is no stored summary of a beat (author
    /// ruling 2026-09-22), and a first line cannot be wrong about what it is.</param>
    /// <param name="Words">Length at a glance — the thing that makes an oversized beat findable.</param>
    public sealed record SpineItem(
        Guid BeatId,
        Guid ChapterNodeId,
        int Ordinal,
        string Label,
        string ChapterTitle,
        string Summary,
        int Words,
        bool Empty);

    /// <summary>An open beat. <paramref name="Text"/> is the RAW tagged prose — reads do not
    /// strip markup, which is exactly what the display/markdown toggle needs.</summary>
    /// <param name="Description">The author's stated intent: why this beat exists.</param>
    /// <param name="DescriptionState">Whether that intent was written against the prose as it
    /// stands — "current", "stale" or "unverified". Computed by the beat itself.</param>
    public sealed record OpenBeat(
        Guid Id, Guid ChapterNodeId, string Text, DateTime UpdatedAt, int Version,
        int Number = 0,
        string? Description = null, string? DescriptionState = null);

    /// <summary>What the entity scanner did to the text on the way in. The author sees this after
    /// every save, because the save rewrites their markup and hiding that would make the editor a
    /// liar.</summary>
    public sealed record TagDiff(IReadOnlyList<string> Added, IReadOnlyList<string> Removed);

    public sealed record SaveResult(bool Changed, OpenBeat Beat, TagDiff Tags,
                                    IReadOnlyList<BeatMarkup.MarkupProblem> Problems);

    public async Task<List<BookListItem>> ListBooksAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.BookNodes.AsNoTracking().IgnoreQueryFilters()
            .OrderBy(b => b.Title)
            .Select(b => new BookListItem(b.Id, b.Slug, b.Title, b.NodeCode))
            .ToListAsync(ct);
    }

    /// <summary>Pins the ambient universe to the one this book lives in, for the rest of this
    /// async flow. Returns the book node id.</summary>
    // NOT async, deliberately. The flow universe is an AsyncLocal, and a value set inside an async
    // method is discarded when that method returns to its caller — so the old async version pinned
    // nothing, and every read after "await ScopeToBookAsync(...)" ran in the Hub's default
    // universe (empty or wrong rows for any book outside it). A synchronous method shares its
    // caller's execution context, so the assignment survives. One single-row lookup.
    private Task<Guid> ScopeToBookAsync(Guid bookNodeId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var db = dbFactory.CreateDbContext();
        var universeId = db.Nodes.AsNoTracking().IgnoreQueryFilters()
            .Where(n => n.Id == bookNodeId).Select(n => n.UniverseId).FirstOrDefault();
        if (universeId != Guid.Empty) universe.SetFlowUniverse(universeId);
        return Task.FromResult(bookNodeId);
    }

    public async Task<List<SpineItem>> GetSpineAsync(Guid bookNodeId, CancellationToken ct = default)
    {
        await ScopeToBookAsync(bookNodeId, ct);

        var ordered = await workbench.GetOrderedBeatsAsync(bookNodeId, ct);
        if (ordered.Count == 0) return [];

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var chapterIds = ordered.Select(o => o.NodeId).Distinct().ToList();
        var chapterTitles = await db.Nodes.AsNoTracking().IgnoreQueryFilters()
            .Where(n => chapterIds.Contains(n.Id))
            .ToDictionaryAsync(n => n.Id, n => n.Title, ct);

        var items = new List<SpineItem>(ordered.Count);
        for (var i = 0; i < ordered.Count; i++)
        {
            var beat = ordered[i].Beat;
            var chapter = chapterTitles.GetValueOrDefault(ordered[i].NodeId, "(unfiled)") ?? "(unfiled)";
            var plain = ProseInline.StripFormatting(BeatMarkup.StripEntityTags(beat.Text ?? ""));


            items.Add(new SpineItem(
                beat.Id, ordered[i].NodeId, i + 1,
                $"#{i + 1:D4} - {chapter}",
                chapter,
                Shorten(FirstLine(plain), 110),
                Words: plain.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length,
                Empty: plain.Trim().Length == 0));
        }
        return items;
    }

    /// <summary>The beat's opening sentence-ish, for a list row. Cut at a sentence end when there
    /// is one nearby, so the row reads as a line rather than as a truncation.</summary>
    private static string FirstLine(string plain)
    {
        var text = plain.Replace('\n', ' ').Trim();
        if (text.Length == 0) return "";

        var stop = text.IndexOfAny(['.', '!', '?'], 0);
        return stop > 20 && stop < 140 ? text[..(stop + 1)] : text;
    }

    private static string Shorten(string s, int max)
        => s.Length <= max ? s : s[..(max - 1)].TrimEnd() + "…";

    /// <summary>
    /// Set the beat's stated intent, by hand.
    /// </summary>
    /// <remarks>
    /// <para><b>There is deliberately no method that writes this from the prose.</b> Intent is
    /// what the prose is checked AGAINST: back-filling it from the beat would make the check
    /// tautological — the beat would agree with its intent by construction, forever — and the one
    /// instrument that can say "this beat is not doing what it was for" would be reporting on
    /// itself.</para>
    ///
    /// <para>The hash is stamped with it, so the trust state reads "current" until the prose moves
    /// again. Writing the text without the hash is what makes a summary look verified when it was
    /// never checked.</para>
    /// </remarks>
    public async Task SetBeatIntentAsync(
        Guid bookNodeId, Guid beatId, string intent, CancellationToken ct = default)
    {
        await ScopeToBookAsync(bookNodeId, ct);

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var beat = await db.Beats.FirstOrDefaultAsync(b => b.Id == beatId, ct)
            ?? throw new InvalidOperationException($"Beat {beatId} not found.");

        beat.Description = string.IsNullOrWhiteSpace(intent) ? null : intent.Trim();
        beat.DescriptionHash = beat.Description is null ? null : Beat.ComputeHash(beat.Text ?? "");
        await db.SaveChangesAsync(ct);
    }

    // ── The save gate ──────────────────────────────────────────────────────

    /// <param name="Blocking">Everything the author has to answer before this beat is finished.
    /// Already filtered by what they have said they meant at this exact wording.</param>
    /// <param name="Cost">What the judgement tier spent. Zero when the exact checks found the
    /// answer on their own or there were no facts to check against.</param>
    /// <param name="Checked">What was actually looked at, so an empty result can say which. A
    /// zero that cannot name its own coverage is indistinguishable from not having looked.</param>
    public sealed record SaveGateResult(
        IReadOnlyList<Mismatch> Blocking, double Cost, string Checked);

    /// <summary>
    /// Everything that should stop the author on a beat they have just finished.
    /// </summary>
    /// <remarks>
    /// <para><b>Manual save only.</b> Autosave keeps <c>deferAnalysis: true</c> and spends nothing
    /// — at twenty-second intervals an LLM tier would be ~180 calls an hour of writing, and the
    /// full battery historically cost $27–135 a run for zero applied findings.</para>
    ///
    /// <para>Three tiers, in descending order of how much they can be trusted. The exact ones run
    /// first and cost nothing; the judgement tier runs once, at temperature 0, and only when there
    /// are recorded facts for it to check against.</para>
    /// </remarks>
    public async Task<SaveGateResult> RunSaveGateAsync(
        Guid bookNodeId, Guid beatId, CancellationToken ct = default)
    {
        await ScopeToBookAsync(bookNodeId, ct);
        using var scope = LlmActionContext.BeginCostScope();

        // Tier A — exact, free, and the only tier this would be worth gating on by itself.
        var blocking = new List<Mismatch>(await ramifications.GateAsync(bookNodeId, beatId, ct));
        var looked = new List<string> { "obligations, continuity, entity links, gear, summaries" };

        // Tier B — one call, calibrated 7/7, reasoning before verdict, and no call at all when the
        // record holds nothing about the people in this beat.
        var carried = await ramifications.CarriedByAsync(bookNodeId, beatId, ct);
        if (carried.EstablishedFacts.Count > 0)
        {
            var text = await GetBeatAsync(beatId, ct);
            if (text is not null)
            {
                try
                {
                    var verdict = await contradictions.CheckAsync(
                        carried.EstablishedFacts,
                        Prose.Core.Services.Discussion.BeatDiscussTarget.PlainText(text.Text), ct);

                    looked.Add($"{carried.EstablishedFacts.Count} recorded facts");
                    if (verdict.Contradicts)
                        blocking.Add(new Mismatch(
                            "contradiction",
                            "This contradicts something already on the record: "
                            + (verdict.ViolatedFact ?? "a fact given for this beat"),
                            verdict.Reasoning,
                            ["The prose is right — update the record.",
                             "The record is right — put the prose back.",
                             "Leave both for now, and remember why."]));
                }
                catch (Exception ex)
                {
                    // "Could not look" is a different answer from "nothing found" and must be
                    // shown as one — it is the failure mode that laundered unchecked into verified
                    // and let the Dae-jung Seo contradiction survive five clean sweeps.
                    blocking.Add(new Mismatch(
                        "contradiction",
                        "The contradiction check could not run, so nothing was checked against the record.",
                        ex.Message, ["Carry on anyway.", "Stop and look into it."]));
                }
            }
        }
        else
        {
            looked.Add("no recorded facts for anyone in this beat, so nothing to contradict");
        }

        // Tier C — the six-rule sweep already ran inline as part of this save. Its findings are
        // scoped to this beat, so they are read back rather than re-run; running it twice would
        // double the bill for the same answer.
        blocking.AddRange(await BlastFindingsAsync(beatId, ct));
        looked.Add("the logic sweep over this beat's blast radius");

        return new SaveGateResult(blocking, ledger.CostForScope(scope.Id), string.Join(" · ", looked));
    }

    /// <summary>
    /// What the narrow logic sweep filed against this beat.
    /// </summary>
    /// <remarks>
    /// Read by the sweep's own scope key — <c>beat:{id:N}:blast</c> — not by timestamp. The sweep
    /// deletes and recreates its findings per scope, so this is always that beat's current set and
    /// never a neighbour's.
    /// </remarks>
    private Task<IReadOnlyList<Mismatch>> BlastFindingsAsync(Guid beatId, CancellationToken ct)
    {
        var rows = findings.List(FindingStatus.New, limit: 20, filePathPrefix: $"beat:{beatId:N}:blast");
        return Task.FromResult<IReadOnlyList<Mismatch>>(rows
            .Select(f => new Mismatch(
                "logic-sweep",
                f.Summary,
                f.Snippet,
                ["It reads the book in 100,000-character windows — it may not have seen the rest.",
                 "It is right; I will fix it.",
                 "Leave it for now."]))
            .ToList());
    }

    /// <param name="Checked">How many beats were actually looked at, and out of how many. Reported
    /// because a pass that finds nothing and cannot say what it examined is indistinguishable from
    /// a pass that could not look — the failure that let a contradiction survive five clean sweeps.</param>
    public sealed record BookCheckResult(
        int Checked, int Total, IReadOnlyList<(int Number, Mismatch Finding)> Findings);

    /// <summary>
    /// Run the exact checks over a whole book, or over the part of it edited recently.
    /// </summary>
    /// <remarks>
    /// <para>Tier A only, so it is free however large the book. That is what makes it safe to
    /// offer as a button: a version of this that spent a model call per beat would cost real money
    /// on a 521-beat manuscript and would be pressed exactly once.</para>
    /// </remarks>
    /// <param name="since">Only beats written to since this instant. Null checks the whole book.</param>
    public async Task<BookCheckResult> CheckBookAsync(
        Guid bookNodeId, DateTime? since = null, CancellationToken ct = default)
    {
        await ScopeToBookAsync(bookNodeId, ct);

        var all = await workbench.GetOrderedBeatsAsync(bookNodeId, ct);
        var target = since is { } t
            ? await workbench.GetBeatsChangedSinceAsync(bookNodeId, t, ct)
            : all.Select(o => o.Beat).ToList();

        var found = new List<(int, Mismatch)>();
        foreach (var beat in target)
        {
            ct.ThrowIfCancellationRequested();
            foreach (var m in await ramifications.GateAsync(bookNodeId, beat.Id, ct))
                found.Add((beat.Number, m));
        }

        return new BookCheckResult(target.Count, all.Count, found);
    }

    /// <summary>
    /// The beat's prose one revision back, for the gate's Undo.
    /// </summary>
    /// <remarks>
    /// From the temporal history, which is where the previous wording actually lives — index 1 is
    /// the revision before the live row. Returns null on a beat with no history, and on SQLite,
    /// where <c>FOR SYSTEM_TIME</c> does not exist; the caller says so rather than silently
    /// offering an undo that would do nothing.
    /// </remarks>
    public Task<string?> PreviousBeatTextAsync(Guid beatId, CancellationToken ct = default)
        => workbench.GetBeatVersionTextAsync(beatId, 1, ct);

    /// <summary>Record that the author meant it, for this beat at this exact wording.</summary>
    public Task DismissGateFindingAsync(
        Guid beatId, Mismatch mismatch, string? note = null, CancellationToken ct = default)
        => ramifications.DismissAsync(beatId, mismatch, note, ct);

    /// <param name="Label">What the other session called itself.</param>
    public sealed record OtherSession(string Label, string Kind, DateTime StartedAt);

    /// <summary>
    /// Editing sessions already open on this book.
    /// </summary>
    /// <remarks>
    /// <para>Read, never started. EditSessionService is a record of a named editing PASS, not
    /// per-beat presence, and its StartSessionAsync deliberately closes any open auto-session
    /// first — so a Writer that opened one would evict whatever the CLI or another window had
    /// running, which is the opposite of presence.</para>
    ///
    /// <para>What it can honestly answer is the question that matters: is something else working
    /// on this tree right now. Two sessions editing the same beat is a conflict banner after the
    /// fact; knowing beforehand is worth a line in the status bar.</para>
    /// </remarks>
    public async Task<IReadOnlyList<OtherSession>> OpenSessionsAsync(
        Guid bookNodeId, CancellationToken ct = default)
    {
        await ScopeToBookAsync(bookNodeId, ct);
        var sessions = await editSessions.GetSessionsAsync(bookNodeId, limit: 10, ct);
        return sessions
            .Where(s => s.ClosedAt is null)
            .Select(s => new OtherSession(s.Label, s.SessionType, s.StartedAt))
            .ToList();
    }

    public async Task<OpenBeat?> GetBeatAsync(Guid beatId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var beat = await db.Beats.AsNoTracking().IgnoreQueryFilters()
            .FirstOrDefaultAsync(b => b.Id == beatId, ct);
        if (beat is null) return null;

        var chapterNodeId = await db.BeatNodes.AsNoTracking()
            .Where(bn => bn.BeatId == beatId).Select(bn => bn.NodeId).FirstOrDefaultAsync(ct);

        return new OpenBeat(
            beat.Id, chapterNodeId, beat.Text ?? "", beat.UpdatedAt, beat.Version,
            beat.Number,
            beat.Description, beat.DescriptionState);
    }

    /// <summary>
    /// Save one beat's prose. Refuses malformed markup outright — an unclosed
    /// <c>&lt;entity&gt;</c> would otherwise be persisted into the prose as literal angle
    /// brackets, and nothing downstream checks for it.
    ///
    /// <para><paramref name="deferAnalysis"/> is true while the author is still working in the
    /// beat, and false when they leave it or when the edit looks risky — see the Ramifications
    /// panel. It suppresses only the LLM tails, never the write itself.</para>
    /// </summary>
    public async Task<SaveResult> SaveBeatAsync(Guid bookNodeId, Guid beatId, string newText,
                                                DateTime expectedUpdatedAt, bool deferAnalysis,
                                                CancellationToken ct = default)
    {
        var problems = BeatMarkup.Validate(newText);
        if (problems.Count > 0)
        {
            var unchanged = await GetBeatAsync(beatId, ct)
                ?? throw new InvalidOperationException($"Beat {beatId} not found.");
            return new SaveResult(false, unchanged, new TagDiff([], []), problems);
        }

        await ScopeToBookAsync(bookNodeId, ct);

        var before = await GetBeatAsync(beatId, ct)
            ?? throw new InvalidOperationException($"Beat {beatId} not found.");

        await workbench.UpdateBeatTextAsync(beatId, newText, BeatWriteReason.AuthorEdit,
                                            expectedUpdatedAt: expectedUpdatedAt,
                                            deferAnalysis: deferAnalysis, ct: ct);

        // Re-read rather than assume: the save re-derives every tag from scratch, so what landed
        // is not what was sent. The save is also a no-op when the re-tagged text matches what was
        // already there, which is why "changed" is decided by Version and not by the text we hold.
        var after = await GetBeatAsync(beatId, ct)
            ?? throw new InvalidOperationException($"Beat {beatId} disappeared during save.");

        return new SaveResult(after.Version != before.Version, after,
                              DiffTags(newText, after.Text), problems);
    }

    /// <summary>What the scanner added or removed relative to the markup the author submitted.</summary>
    private static TagDiff DiffTags(string submitted, string stored)
    {
        static Dictionary<Guid, string> Mentions(string t) =>
            BeatMarkup.ExtractTaggedMentions(t)
                .GroupBy(m => m.EntityId)
                .ToDictionary(g => g.Key, g => g.First().Text);

        var sent = Mentions(submitted);
        var kept = Mentions(stored);

        return new TagDiff(
            kept.Where(k => !sent.ContainsKey(k.Key)).Select(k => k.Value).Distinct().ToList(),
            sent.Where(s => !kept.ContainsKey(s.Key)).Select(s => s.Value).Distinct().ToList());
    }

    /// <summary>
    /// The analysis the quiet saves skipped, run once for the beat the author has finished with.
    /// This is the same work <see cref="NodeWorkbenchService.UpdateBeatTextAsync"/> fires on a
    /// normal save — the blast radius of the edit, swept by the six logic rules — just moved to a
    /// moment where it runs once instead of once per keystroke batch.
    ///
    /// <para>Costs real money (six LLM rules over the radius), so it is only ever called when the
    /// author leaves a beat they actually changed, closes the book, or makes an edit the
    /// ramifications check flagged as risky.</para>
    /// </summary>
    // ── Structural editing ─────────────────────────────────────────────────
    //
    // The workbench has had all of these since long before the Writer existed, and the Writer
    // called none of them: a drafting novelist could change the words in a beat and nothing else.
    // Ordered here by what that actually costs them — split first, because the commonest real act
    // in drafting is "this is two beats"; delete last, because it is the one the author can do by
    // emptying a beat and the only one that destroys something.
    //
    // Every one of these is a WHOLE-BEAT structural act, which is why none of them goes through
    // SpanWrite: there is no span, and nothing here rewords anything.

    /// <summary>
    /// Split the beat at the caret, leaving the text before it here and the rest in a new beat
    /// immediately after.
    /// </summary>
    /// <param name="splitPosition">A character offset into the beat's STORED text. The editor
    /// reports the caret in reader-visible coordinates, so callers map it first — see
    /// <c>PlainTextMap</c>. Splitting at a raw plain offset would land inside an entity tag.</param>
    /// <returns>The new beat that holds the tail.</returns>
    public async Task<Beat> SplitBeatAsync(
        Guid bookNodeId, Guid chapterNodeId, Guid beatId, int splitPosition,
        CancellationToken ct = default)
    {
        await ScopeToBookAsync(bookNodeId, ct);
        return await workbench.SplitBeatAtAsync(chapterNodeId, beatId, splitPosition, ct);
    }

    /// <summary>Split down the middle, at the nearest paragraph break the workbench can find.
    /// What the toolbar offers when there is no caret to split at.</summary>
    public async Task<Beat> SplitBeatAsync(
        Guid bookNodeId, Guid chapterNodeId, Guid beatId, CancellationToken ct = default)
    {
        await ScopeToBookAsync(bookNodeId, ct);
        return await workbench.SplitBeatAsync(chapterNodeId, beatId, ct);
    }

    /// <summary>A new, empty beat after this one — the "and then" a drafting session runs on.</summary>
    public async Task<Beat> InsertBeatAsync(
        Guid bookNodeId, Guid chapterNodeId, Guid? afterBeatId, CancellationToken ct = default)
    {
        await ScopeToBookAsync(bookNodeId, ct);
        return await workbench.InsertBeatAsync(chapterNodeId, afterBeatId, ct: ct);
    }

    /// <summary>Move a beat to sit after another one, or to the front when
    /// <paramref name="afterBeatId"/> is null.</summary>
    public async Task MoveBeatAsync(
        Guid bookNodeId, Guid chapterNodeId, Guid beatId, Guid? afterBeatId,
        CancellationToken ct = default)
    {
        await ScopeToBookAsync(bookNodeId, ct);
        await workbench.MoveBeatAsync(chapterNodeId, beatId, afterBeatId, ct);
    }

    /// <summary>Fold this beat into the one before it. The inverse of a split, and the reason a
    /// split is safe to try: getting it wrong costs one click to undo.</summary>
    public async Task JoinWithPreviousAsync(
        Guid bookNodeId, Guid chapterNodeId, Guid beatId, CancellationToken ct = default)
    {
        await ScopeToBookAsync(bookNodeId, ct);
        await workbench.JoinBeatWithPreviousAsync(chapterNodeId, beatId, ct);
    }

    /// <summary>
    /// Delete a beat outright.
    /// </summary>
    /// <remarks>
    /// The only verb here that destroys something, and the UI confirms it by showing the prose
    /// rather than by asking "are you sure" — a confirmation that does not show what is about to
    /// be lost is a confirmation nobody reads.
    /// </remarks>
    public async Task DeleteBeatAsync(
        Guid bookNodeId, Guid chapterNodeId, Guid beatId, CancellationToken ct = default)
    {
        await ScopeToBookAsync(bookNodeId, ct);
        await workbench.DeleteBeatAsync(chapterNodeId, beatId, ct);
    }

    /// <summary>What a deferred sweep did, so the UI can say so.</summary>
    /// <param name="Cost">Dollars, from the ledger's own scope total — not an estimate.</param>
    public sealed record SweepReport(
        bool Ran, int BeatsChecked, int Findings, double Cost, TimeSpan Elapsed, string? Error);

    /// <summary>
    /// The logic sweep over everything the last edit could have broken.
    /// </summary>
    /// <remarks>
    /// <para>Now returns what it did. It used to return void: six logic rules over a blast radius
    /// take minutes and spend real money, and the author had no indication that it had started,
    /// finished, failed or cost anything. A background task that silently bills you is not a
    /// feature the author can reason about, and the first thing they do when they notice is turn
    /// the whole thing off.</para>
    /// </remarks>
    public async Task<SweepReport> RunDeferredAnalysisAsync(
        Guid bookNodeId, Guid beatId, CancellationToken ct = default)
    {
        var started = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            await ScopeToBookAsync(bookNodeId, ct);
            var radius = await blastRadius.GetBlastRadiusBeatIdsAsync(beatId, ct: ct);
            if (radius.Count == 0)
                return new SweepReport(false, 0, 0, 0, started.Elapsed, null);

            // One scope around the whole sweep, so the cost reported is this sweep's and not the
            // session's running total.
            using var scope = LlmActionContext.BeginCostScope();
            var report = await logicSweep.RunNarrowAsync(bookNodeId, radius, beatId, ct);

            return new SweepReport(
                true, radius.Count, report.Findings.Count,
                ledger.CostForScope(scope.Id), started.Elapsed, null);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            // A sweep that fell over used to be indistinguishable from one that found nothing.
            return new SweepReport(false, 0, 0, 0, started.Elapsed, ex.Message);
        }
    }

    /// <summary>Candidates for "assign this highlighted text to an entity". Ranked exact → prefix
    /// → substring by the lookup service, and scoped to the open book's universe by the caller
    /// having gone through <see cref="GetSpineAsync"/> first.</summary>
    public async Task<IReadOnlyList<EntityLookupMatch>> SearchEntitiesAsync(
        Guid bookNodeId, string query, int limit = 20, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];
        await ScopeToBookAsync(bookNodeId, ct);
        return await entityLookup.FindAsync(query.Trim(), entityType: null, limit, ct);
    }

    /// <summary>Where an export landed, and how long it took.</summary>
    public sealed record ExportResult(string Directory, IReadOnlyList<string> Files, TimeSpan Elapsed);

    /// <summary>
    /// Render the open book to the publish directory so the author can see the finished object.
    ///
    /// <para><b>Deliberately not <see cref="NodeFullExportService.ExportAllAsync"/>.</b> That is the
    /// right call for <c>prose --export-node</c>, but it is the wrong thing to put one click away
    /// from Save, because one of its steps spends real money: <c>SynopsisExportService</c> makes an
    /// LLM call per chapter with no cached synopsis (38 of them on BCODA). A button the author
    /// presses to check their formatting must never quietly bill them.</para>
    ///
    /// <para>So this renders the three formats that answer "what does it actually look like" and
    /// nothing else: docx, epub (what KDP ingests), and pdf. All deterministic, all free, no LLM
    /// call anywhere in the path. The full bundle — synopsis, keywords, cover, audio txt, the
    /// beat-marked markdown — stays on <c>prose --export-node</c>, where its cost is explicit.</para>
    ///
    /// <para>Author is left null so each exporter falls back to the node's own <c>Author</c>, which
    /// is the single source for it.</para>
    /// </summary>
    public async Task<ExportResult> ExportBookAsync(Guid bookNodeId, CancellationToken ct = default)
    {
        await ScopeToBookAsync(bookNodeId, ct);

        var started = DateTime.UtcNow;
        var files = new List<string>
        {
            await docx.ExportNodeAsync(bookNodeId, author: null, ct),
            await manuscript.ExportEpubAsync(bookNodeId, author: null, ct),
            await manuscript.ExportPdfAsync(bookNodeId, author: null, ct),
        };

        return new ExportResult(
            Path.GetDirectoryName(files[0]) ?? "",
            files.Select(Path.GetFileName).Where(f => !string.IsNullOrEmpty(f)).Select(f => f!).ToList(),
            DateTime.UtcNow - started);
    }

    /// <summary>The record behind a tag, for the right-click inspector.</summary>
    public async Task<Entity?> GetEntityAsync(Guid entityId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.Entities.AsNoTracking().IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.Id == entityId, ct);
    }
}
