using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

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
    /// <param name="Summary">The beat's recorded event line, or its opening words when there is
    /// none. This is what makes the list navigable at all: five hundred rows reading
    /// <c>#0122 - Chapter 3: Teeth</c>, <c>#0123 - Chapter 3: Teeth</c> carry one bit of
    /// information between them, which is that the book is long.</param>
    /// <param name="SummaryIsProse">True when <paramref name="Summary"/> is the beat's own opening
    /// words rather than a recorded summary. Shown differently: a first line is evidence, an event
    /// summary is a claim, and 31 of BCODA's described the wrong beat.</param>
    /// <param name="Words">Length at a glance — the thing that makes an oversized beat findable.</param>
    public sealed record SpineItem(
        Guid BeatId,
        Guid ChapterNodeId,
        int Ordinal,
        string Label,
        string ChapterTitle,
        string Summary,
        bool SummaryIsProse,
        int Words,
        bool Empty);

    /// <summary>An open beat. <paramref name="Text"/> is the RAW tagged prose — reads do not
    /// strip markup, which is exactly what the display/markdown toggle needs.</summary>
    public sealed record OpenBeat(Guid Id, Guid ChapterNodeId, string Text, DateTime UpdatedAt, int Version);

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
    private async Task<Guid> ScopeToBookAsync(Guid bookNodeId, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var universeId = await db.Nodes.AsNoTracking().IgnoreQueryFilters()
            .Where(n => n.Id == bookNodeId).Select(n => n.UniverseId).FirstOrDefaultAsync(ct);
        if (universeId != Guid.Empty) universe.SetFlowUniverse(universeId);
        return bookNodeId;
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

            // The recorded event line when there is one, the beat's own opening words otherwise.
            // Which of the two is showing is carried separately rather than blurred: an event
            // summary is a claim about the beat and 31 of BCODA's described the wrong one, while
            // a first line cannot be wrong about what it is.
            var recorded = !string.IsNullOrWhiteSpace(beat.EventSummary);
            var summary = recorded ? beat.EventSummary!.Trim() : FirstLine(plain);

            items.Add(new SpineItem(
                beat.Id, ordered[i].NodeId, i + 1,
                $"#{i + 1:D4} - {chapter}",
                chapter,
                Shorten(summary, 110),
                SummaryIsProse: !recorded,
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

    public async Task<OpenBeat?> GetBeatAsync(Guid beatId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var beat = await db.Beats.AsNoTracking().IgnoreQueryFilters()
            .FirstOrDefaultAsync(b => b.Id == beatId, ct);
        if (beat is null) return null;

        var chapterNodeId = await db.BeatNodes.AsNoTracking()
            .Where(bn => bn.BeatId == beatId).Select(bn => bn.NodeId).FirstOrDefaultAsync(ct);

        return new OpenBeat(beat.Id, chapterNodeId, beat.Text ?? "", beat.UpdatedAt, beat.Version);
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
