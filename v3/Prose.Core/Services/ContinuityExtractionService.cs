using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;
using Prose.Core.Interfaces;
using Prose.Core.Models;
using ContinuityExtractionCursor = Prose.Core.Data.Entities.ContinuityExtractionCursor;

namespace Prose.Core.Services;

/// <summary>
/// Drives the LLM-side of the unified continuity store. Pulls atomic
/// (entity, predicate, object) claims out of a source — chapter prose or an
/// entity record — and hands each candidate to <see cref="ContinuityService"/>
/// for upsert. Contradictions are surfaced automatically (same predicate,
/// different object on the same entity).
///
/// 2026-08-14: de-Legion'd — was backed by Legion's <c>LlmVotingService</c>
/// Quorum vote (every active LLM provider as a voter). That vote's only
/// externally-visible product beyond the candidate list was a corroboration
/// count nothing downstream actually consumed, and Quorum/panel voting is
/// project-wide quarantined by SS-A44 ("no votes/panels unless explicitly
/// requested"). Now a single <see cref="ILlmService"/> call, same pattern as
/// NarrativeScienceService/ThemeCoherenceService. Snippet-in-prose grounding
/// (every fact must be an exact substring quote) is unchanged and is the real
/// quality gate, not the vote.
/// </summary>
public class ContinuityExtractionService
{
    private readonly ContinuityService store;
    private readonly ILlmService llm;
    private readonly IChapterRepository chapters;
    private readonly CharacterRepository peopleRepo;
    private readonly DistrictRepository placesRepo;
    private readonly FactionRepository factionsRepo;
    private readonly CorponationRepository corponationsRepo;
    private readonly IDbContextFactory<ProseDbContext> dbFactory;
    private readonly ILogger<ContinuityExtractionService> log;

    public ContinuityExtractionService(
        ContinuityService store,
        ILlmService llm,
        IChapterRepository chapters,
        CharacterRepository peopleRepo,
        DistrictRepository placesRepo,
        FactionRepository factionsRepo,
        CorponationRepository corponationsRepo,
        IDbContextFactory<ProseDbContext> dbFactory,
        ILogger<ContinuityExtractionService> log)
    {
        this.store           = store;
        this.llm             = llm;
        this.chapters        = chapters;
        this.peopleRepo      = peopleRepo;
        this.placesRepo      = placesRepo;
        this.factionsRepo    = factionsRepo;
        this.corponationsRepo = corponationsRepo;
        this.dbFactory       = dbFactory;
        this.log             = log;
    }

    private const string ExtractionQuestion =
        "Extract every atomic factual assertion the prose makes about every named entity. " +
        "Cover: physical features, gear/weapon placement, abilities, locations, possessions, relationships, " +
        "knowledge, residence, employment, ages, handedness, and any persistent attribute. " +
        "Skip transient emotion or one-time action. " +
        "For each fact, return: " +
        "{ \"entity_name\": \"<exact name as it appears>\", \"predicate\": \"<short snake_case key, e.g. weapon_carry_location, hair_color, lives_at>\", " +
        "\"object\": \"<the value, concise>\", \"snippet\": \"<≤200-char exact quote from the prose that supports the claim>\", " +
        "\"voice\": \"narrator|character|inner_monologue\", \"confidence\": \"low|medium|high\" }. " +
        "Output ONLY a single JSON array on the FINAL line of your response. If no facts can be extracted, output []. " +
        "Be strict: every fact MUST be supported by an exact substring quote from the prose. Do not invent or paraphrase. " +
        "Prefer atomic predicates over compound ones (e.g. \"weapon_carry_location\" not \"carry_setup\"). " +
        "Use the SAME predicate name when reasserting the same kind of fact about different entities.";

    /// <summary>
    /// Extract continuity claims from one chapter's prose.
    /// </summary>
    /// <param name="bookSlug">
    /// Code of the parent BookNode (e.g. "BCODA"). When provided, each extracted
    /// claim is tagged with this slug so cross-book consistency queries can identify
    /// which book the claim originates from. Pass <c>null</c> when the book context
    /// is not available (existing callers are unaffected — the field stays null).
    /// </param>
    public async Task<ContinuityExtractionResult> ExtractFromChapterAsync(
        string chapterId,
        int maxTokens = 4096,
        string? bookSlug = null,
        CancellationToken ct = default)
    {
        var chapter = chapters.LoadChapter(chapterId)
            ?? throw new InvalidOperationException($"Chapter not found: {chapterId}");
        var prose = chapter.PlainText;
        if (string.IsNullOrWhiteSpace(prose))
            throw new InvalidOperationException($"Chapter has no prose: {chapterId}");

        log.LogInformation("[continuity] Extracting from chapter {Num}: {Title} ({Chars} chars)",
            chapter.Number, chapter.Title, prose.Length);

        var contextHeader = "=== CHAPTER PROSE (extract facts from this) ===\n" +
            $"Chapter {chapter.Number}: {chapter.Title}\n";

        return await ExtractClaimsFromProseAsync(
            prose, contextHeader, chapter.Id, chapter.Number, chapter.Title, bookSlug, maxTokens, ct);
    }

    /// <summary>
    /// Extract continuity claims from every chapter in a book. Sequential to
    /// keep cost predictable; long-running.
    /// </summary>
    public async Task<List<ContinuityExtractionResult>> ExtractFromBookAsync(
        Book book,
        int maxTokens = 4096,
        CancellationToken ct = default)
    {
        var results = new List<ContinuityExtractionResult>();
        foreach (var cid in book.ChapterIds ?? new())
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var r = await ExtractFromChapterAsync(cid, maxTokens, bookSlug: null, ct);
                results.Add(r);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "[continuity] Chapter {Cid} extraction failed", cid);
                results.Add(new ContinuityExtractionResult { ChapterId = cid, Error = ex.Message });
            }
        }
        return results;
    }

    /// <summary>
    /// Extract continuity claims from every leaf chapter under a modern SS-A43 BookNode
    /// (<c>Nodes</c>/<c>BeatNodes</c>/<c>Beats</c>) — the counterpart to
    /// <see cref="ExtractFromBookAsync"/>, which only knows the legacy
    /// <see cref="IBookRepository"/>/<see cref="IChapterRepository"/> model. Every book created
    /// under the locked New Story Workflow pipeline (VIGL included) lives here, not there, so
    /// this is the method a BookHealthService-style per-node caller needs. Every claim is
    /// tagged with the book node's own Slug so <see cref="ContinuityService.GetContradictionGroups"/>
    /// can be scoped to just this book.
    /// </summary>
    public async Task<List<ContinuityExtractionResult>> ExtractFromBookNodeAsync(
        Guid nodeId, int maxTokens = 4096, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        // IgnoreQueryFilters(): explicit nodeId, not an ambient scope (same bug class found and
        // fixed in BookArchiveService.ArchiveAsync, 2026-08-17).
        var node = await db.Nodes.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(n => n.Id == nodeId, ct)
            ?? throw new InvalidOperationException($"Node {nodeId} not found.");
        var bookSlug = node.Slug;

        var leafIds = await NodeWorkbenchService.GetLeafDescendantIdsAsync(db, nodeId, ct);
        // IgnoreQueryFilters(): leafIds are explicit descendants of the already-resolved nodeId
        // above, not an ambient scope — same bug class as the node lookup two lines up. Without
        // this, a book outside whatever universe happens to be ambient-scoped (e.g. a SCRY book
        // extracted while --universe glmz is set) silently resolves to zero chapters here even
        // though GetLeafDescendantIdsAsync (which already IgnoreQueryFilters) found real leaf ids —
        // found live 2026-08-19 running Trinity Reconciliation Phase 1 across GLMZ+SCRY+FICTION at
        // once, where SCRY books executed under an ambient glmz scope.
        var chapterNodes = await db.Nodes.AsNoTracking().IgnoreQueryFilters()
            .Where(n => leafIds.Contains(n.Id))
            .OrderBy(n => n.SortKey)
            .Select(n => new { n.Id, n.Title })
            .ToListAsync(ct);

        var results = new List<ContinuityExtractionResult>();
        var chapterNumber = 0;
        foreach (var chNode in chapterNodes)
        {
            ct.ThrowIfCancellationRequested();
            chapterNumber++;

            var (prose, beatIndex) = await LoadChapterProseAsync(db, chNode.Id, ct);

            if (string.IsNullOrWhiteSpace(prose))
            {
                results.Add(new ContinuityExtractionResult
                {
                    ChapterId = chNode.Id.ToString(), ChapterNumber = chapterNumber,
                    ChapterTitle = chNode.Title ?? "", Error = "no prose",
                });
                continue;
            }

            try
            {
                log.LogInformation("[continuity] Extracting from node chapter {Num}: {Title} ({Chars} chars)",
                    chapterNumber, chNode.Title, prose.Length);
                var contextHeader = "=== CHAPTER PROSE (extract facts from this) ===\n" +
                    $"Chapter {chapterNumber}: {chNode.Title}\n";
                var r = await ExtractClaimsFromProseAsync(
                    prose, contextHeader, chNode.Id.ToString(), chapterNumber, chNode.Title, bookSlug, maxTokens, ct,
                    beatIndex: beatIndex);
                results.Add(r);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "[continuity] Node chapter {ChapterId} extraction failed", chNode.Id);
                results.Add(new ContinuityExtractionResult
                {
                    ChapterId = chNode.Id.ToString(), ChapterNumber = chapterNumber,
                    ChapterTitle = chNode.Title ?? "", Error = ex.Message,
                });
            }
        }
        return results;
    }

    /// <summary>Max chars pulled from the raw <c>Nodes.NodeOutline</c> fallback when no matching
    /// <see cref="NodeOutlineSection"/> row exists yet — mirrors <c>BookEntityReconciliationService</c>'s
    /// existing bible-excerpt guard so this doesn't blow the extraction prompt's token budget on a
    /// long hand-authored bible.</summary>
    private const int MaxBibleExcerptChars = 30000;

    /// <summary>
    /// Extract continuity claims from a book's story bible instead of its prose — the third leg
    /// of the Bible/Book/Entities validation triangle. Prefers the typed <c>NodeOutlineSections</c>
    /// row matching <paramref name="sectionType"/> when one exists (narrower, cheaper, and skews
    /// present-tense/settled fact rather than plot-forward — see the caller-facing warning below);
    /// falls back to the raw <c>Nodes.NodeOutline</c> blob (clamped to <see cref="MaxBibleExcerptChars"/>)
    /// so this works even for books that have never had a typed section authored.
    ///
    /// Deliberately defaults <paramref name="sectionType"/> to "Characters", not "ArcSummary" or
    /// "BeatSpine": those two are plot-forward by design ("by the end of the book, X will have
    /// moved to Y") and the (entity, predicate, object) claim model has no temporal qualifier to
    /// distinguish "true now" from "true eventually" — pointing extraction at them would produce
    /// real, not hypothetical, false-positive CONTRADICTED claims against present-day prose.
    /// Character-sheet content ("her hair is dark red") skews settled fact instead.
    ///
    /// Claims land with <c>SourceType = "outline"</c> in the SAME <see cref="ContinuityClaims"/>
    /// ledger prose/entity-record extraction already populates — <see cref="ContinuityService.Upsert"/>
    /// is source-agnostic, so a bible claim and a prose claim on the same (EntityId, Predicate)
    /// compete/reconcile automatically; no new comparison logic needed.
    ///
    /// Default <paramref name="maxTokens"/> is double <see cref="ExtractFromChapterAsync"/>'s
    /// (8192 vs 4096): confirmed live against a real book (Iron &amp; Silk) that a fact-dense
    /// Characters section — a dozen named characters each carrying several atomic facts — produced
    /// a response that got cut off mid-JSON-array at 4096 tokens, which <see cref="ExtractJsonArrayFromText"/>
    /// then silently parsed as "0 candidates" rather than a visible failure (same truncation failure
    /// mode <c>AltitudeAuditService</c> already hit once). A single beat/chapter rarely has enough
    /// named entities to need this; a book's whole character roster commonly does.
    /// </summary>
    public async Task<ContinuityExtractionResult> ExtractFromOutlineAsync(
        Guid nodeId, string sectionType = "Characters", int maxTokens = 8192, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        // IgnoreQueryFilters(): explicit nodeId, not an ambient scope (same pattern as
        // ExtractFromBookNodeAsync above).
        var node = await db.Nodes.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(n => n.Id == nodeId, ct)
            ?? throw new InvalidOperationException($"Node {nodeId} not found.");

        var section = await db.NodeOutlineSections.AsNoTracking()
            .Where(s => s.NodeId == nodeId && s.SectionType == sectionType)
            .Select(s => s.Content)
            .FirstOrDefaultAsync(ct);

        string bibleText;
        string sourcePath;
        if (!string.IsNullOrWhiteSpace(section))
        {
            bibleText = section;
            sourcePath = $"bible-section:{sectionType}";
        }
        else if (!string.IsNullOrWhiteSpace(node.NodeOutline))
        {
            bibleText = node.NodeOutline.Length > MaxBibleExcerptChars
                ? node.NodeOutline[..MaxBibleExcerptChars] : node.NodeOutline;
            sourcePath = "bible-full:fallback";
        }
        else
        {
            return new ContinuityExtractionResult
            {
                ChapterId = "", ChapterTitle = $"bible:{node.Slug}", Error = "no bible content (no section, no NodeOutline)",
            };
        }

        // Bible content is hand-authored markdown (##, **, backticks) — beat prose never has this
        // (BeatMarkup already strips it before extraction), so the shared snippet-grounding check
        // (an exact substring match against THIS text) was never exercised against markdown syntax
        // before. Confirmed live: real bible character sheets (e.g. "**Heritage:** Korean") are
        // fact-dense but produced ZERO candidates on a first production run, because the LLM
        // naturally quotes a snippet without the ** it doesn't consider part of the sentence —
        // stripping formatting here (not loosening the containment check itself) is what actually
        // fixes it, same principle as StripEntityTags cleaning beat text before its own extraction.
        bibleText = StripMarkdownFormatting(bibleText);

        log.LogInformation("[continuity] Extracting from bible for {Slug} (section={Section}, {Chars} chars)",
            node.Slug, sectionType, bibleText.Length);

        var contextHeader = "=== STORY BIBLE (extract facts from this) ===\n" +
            $"{node.Title} — {sectionType} section\n";

        var result = await ExtractClaimsFromProseAsync(
            bibleText, contextHeader, sourceChapterId: "", sourceChapterNumber: null,
            sourceChapterTitle: sectionType, bookSlug: node.Slug, maxTokens, ct,
            sourceType: "outline", sourcePath: sourcePath);
        result.ChapterTitle = $"bible:{node.Slug}:{sectionType}";
        return result;
    }

    // ── Continuous re-extraction (hash-gated) ───────────────────────────────
    //
    // ExtractBookIfNeededAsync (TrinityReconciliationService) only ever extracts a book ONCE —
    // HasAnyClaimsForBook is a pure existence check, never compared against current content.
    // Found live 2026-08-19/20: a duplicated sentence in a published, complete book's prose sat
    // undetected until an unrelated investigation happened to snag on it — the ledger had no way
    // to know the text had drifted from what it extracted. These two methods are the fix: called
    // from NodeWorkbenchService.UpdateBeatTextAsync / CanonDocumentService.SetNodeOutlineSectionAsync
    // on every save, they re-extract ONLY the one chapter/section that changed, and ONLY for a
    // book that already opted in via ExtractBookIfNeededAsync — this never silently extracts a
    // book for the first time; that stays ExtractBookIfNeededAsync's explicit, supervised job.

    /// <summary>Re-extracts one chapter's claims if its content has changed since extraction last
    /// ran against it. No-op (returns false) if the book has never been extracted at all, or if
    /// the chapter's stripped prose is byte-identical to what's cached in
    /// <see cref="ContinuityExtractionCursor"/>.</summary>
    public async Task<bool> ReExtractChapterIfChangedAsync(Guid chapterNodeId, int maxTokens = 4096, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var chapter = await db.Nodes.AsNoTracking().IgnoreQueryFilters()
            .FirstOrDefaultAsync(n => n.Id == chapterNodeId, ct);
        if (chapter?.ParentNodeId == null) return false;

        var book = await db.Nodes.AsNoTracking().IgnoreQueryFilters()
            .FirstOrDefaultAsync(n => n.Id == chapter.ParentNodeId.Value, ct);
        if (book == null || string.IsNullOrEmpty(book.Slug)) return false;
        var bookSlug = book.Slug;

        if (!store.HasAnyClaimsForBook(bookSlug)) return false;

        var (prose, beatIndex) = await LoadChapterProseAsync(db, chapterNodeId, ct);
        if (string.IsNullOrWhiteSpace(prose)) return false;

        var hash = ComputeContentHash(prose);
        var sourceKey = chapterNodeId.ToString("D");
        var cursor = await db.ContinuityExtractionCursors
            .FirstOrDefaultAsync(c => c.BookSlug == bookSlug && c.SourceKind == "chapter" && c.SourceKey == sourceKey, ct);
        if (cursor != null && cursor.ContentHash == hash) return false; // unchanged — no re-bill

        var siblingChapterIds = await db.Nodes.AsNoTracking().IgnoreQueryFilters()
            .Where(n => n.ParentNodeId == book.Id)
            .OrderBy(n => n.SortKey)
            .Select(n => n.Id)
            .ToListAsync(ct);
        var chapterNumber = siblingChapterIds.IndexOf(chapterNodeId) + 1; // 1-indexed; 0 if not found (moved/detached)

        log.LogInformation("[continuity] Re-extracting chapter {Title} ({Chapter}) for {BookSlug} — content changed since last extraction.",
            chapter.Title, chapterNumber, bookSlug);
        var contextHeader = "=== CHAPTER PROSE (extract facts from this) ===\n" +
            $"Chapter {chapterNumber}: {chapter.Title}\n";
        await ExtractClaimsFromProseAsync(prose, contextHeader, chapterNodeId.ToString(), chapterNumber, chapter.Title, bookSlug, maxTokens, ct,
            beatIndex: beatIndex);

        await UpsertCursorAsync(db, bookSlug, "chapter", sourceKey, hash, ct);
        return true;
    }

    /// <summary>Re-extracts one bible section's claims if its content has changed since
    /// extraction last ran against it. Same no-op/opt-in rules as
    /// <see cref="ReExtractChapterIfChangedAsync"/>.</summary>
    public async Task<bool> ReExtractOutlineSectionIfChangedAsync(Guid nodeId, string sectionType, int maxTokens = 8192, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var node = await db.Nodes.AsNoTracking().IgnoreQueryFilters().FirstOrDefaultAsync(n => n.Id == nodeId, ct);
        if (node == null || string.IsNullOrEmpty(node.Slug)) return false;
        var bookSlug = node.Slug;

        if (!store.HasAnyClaimsForBook(bookSlug)) return false;

        var content = await db.NodeOutlineSections.AsNoTracking()
            .Where(s => s.NodeId == nodeId && s.SectionType == sectionType)
            .Select(s => s.Content)
            .FirstOrDefaultAsync(ct);
        if (string.IsNullOrWhiteSpace(content))
            content = node.NodeOutline; // same fallback ExtractFromOutlineAsync itself uses
        if (string.IsNullOrWhiteSpace(content)) return false;

        var hash = ComputeContentHash(content);
        var cursor = await db.ContinuityExtractionCursors
            .FirstOrDefaultAsync(c => c.BookSlug == bookSlug && c.SourceKind == "outline_section" && c.SourceKey == sectionType, ct);
        if (cursor != null && cursor.ContentHash == hash) return false;

        log.LogInformation("[continuity] Re-extracting bible section '{Section}' for {BookSlug} — content changed since last extraction.",
            sectionType, bookSlug);
        await ExtractFromOutlineAsync(nodeId, sectionType, maxTokens, ct);

        await UpsertCursorAsync(db, bookSlug, "outline_section", sectionType, hash, ct);
        return true;
    }

    /// <summary>Seeds a book's extraction cursors right after its first
    /// <c>ExtractBookIfNeededAsync</c> pass succeeds, so the hash-gate has a real baseline from
    /// day one instead of treating every chapter as "changed" on the very first post-rollout
    /// save. Best-effort — a missing cursor just means the next save re-extracts once more than
    /// strictly necessary, never a correctness problem.</summary>
    public async Task SeedExtractionCursorsAsync(Guid bookNodeId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var book = await db.Nodes.AsNoTracking().IgnoreQueryFilters().FirstOrDefaultAsync(n => n.Id == bookNodeId, ct);
        if (book == null || string.IsNullOrEmpty(book.Slug)) return;

        var chapterIds = await db.Nodes.AsNoTracking().IgnoreQueryFilters()
            .Where(n => n.ParentNodeId == bookNodeId)
            .OrderBy(n => n.SortKey)
            .Select(n => n.Id)
            .ToListAsync(ct);

        foreach (var chapterId in chapterIds)
        {
            var prose = string.Join("\n\n", (await db.BeatNodes.AsNoTracking()
                    .Where(bn => bn.NodeId == chapterId)
                    .Include(bn => bn.Beat)
                    .ToListAsync(ct))
                .OrderBy(bn => bn.SortKey)
                .Select(bn => BeatMarkup.StripEntityTags(bn.Beat!.Text))
                .Where(t => !string.IsNullOrWhiteSpace(t)));
            if (string.IsNullOrWhiteSpace(prose)) continue;
            await UpsertCursorAsync(db, book.Slug, "chapter", chapterId.ToString("D"), ComputeContentHash(prose), ct);
        }

        var sections = await db.NodeOutlineSections.AsNoTracking()
            .Where(s => s.NodeId == bookNodeId)
            .Select(s => new { s.SectionType, s.Content })
            .ToListAsync(ct);
        foreach (var s in sections)
        {
            if (string.IsNullOrWhiteSpace(s.Content)) continue;
            await UpsertCursorAsync(db, book.Slug, "outline_section", s.SectionType, ComputeContentHash(s.Content), ct);
        }
    }

    private static async Task UpsertCursorAsync(ProseDbContext db, string bookSlug, string sourceKind, string sourceKey, string hash, CancellationToken ct)
    {
        var existing = await db.ContinuityExtractionCursors
            .FirstOrDefaultAsync(c => c.BookSlug == bookSlug && c.SourceKind == sourceKind && c.SourceKey == sourceKey, ct);
        if (existing != null)
        {
            existing.ContentHash = hash;
            existing.LastExtractedAt = DateTime.UtcNow;
        }
        else
        {
            db.ContinuityExtractionCursors.Add(new ContinuityExtractionCursor
            {
                Id = Guid.NewGuid(), BookSlug = bookSlug, SourceKind = sourceKind, SourceKey = sourceKey,
                ContentHash = hash, LastExtractedAt = DateTime.UtcNow,
            });
        }
        await db.SaveChangesAsync(ct);
    }

    private static string ComputeContentHash(string content)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>One chapter's prose in reading order, returned BOTH as the joined block the
    /// extraction prompt needs and as the per-beat index that lets a validated snippet be traced
    /// back to the exact beat it came from (<c>ContinuityClaim.SourceBeatId</c>, Story Ledger
    /// Phase 2). The two must be built from the same stripped text in the same order or the
    /// snippet-to-beat match silently misses — which is why they are produced together here
    /// rather than assembled twice at each call site, as they were before.</summary>
    private static async Task<(string Prose, List<(Guid BeatId, string Text)> Beats)> LoadChapterProseAsync(
        ProseDbContext db, Guid chapterNodeId, CancellationToken ct)
    {
        var beats = (await db.BeatNodes.AsNoTracking()
                .Where(bn => bn.NodeId == chapterNodeId)
                .Include(bn => bn.Beat)
                .ToListAsync(ct))
            .OrderBy(bn => bn.SortKey)
            // Stripped, not raw — the LLM prompt and the exact-substring snippet grounding both
            // need the plain text a reader would see; a stray <entity guid="..."> tag straddling
            // a quoted span would otherwise break Contains() and discard a true claim.
            .Select(bn => (BeatId: bn.BeatId, Text: BeatMarkup.StripEntityTags(bn.Beat!.Text)))
            .Where(x => !string.IsNullOrWhiteSpace(x.Text))
            .ToList();

        return (string.Join("\n\n", beats.Select(b => b.Text)), beats);
    }

    /// <summary>Shared body for "extract atomic claims from one block of prose, upsert each" —
    /// used by the legacy IChapterRepository path (<see cref="ExtractFromChapterAsync"/>), the
    /// SS-A43 Nodes path (<see cref="ExtractFromBookNodeAsync"/>), and the bible path
    /// (<see cref="ExtractFromOutlineAsync"/>) so the extraction prompt, JSON parsing,
    /// snippet-grounding, and upsert logic exist exactly once.</summary>
    private async Task<ContinuityExtractionResult> ExtractClaimsFromProseAsync(
        string prose, string contextHeader, string sourceChapterId, int? sourceChapterNumber,
        string? sourceChapterTitle, string? bookSlug, int maxTokens, CancellationToken ct,
        string sourceType = "prose", string? sourcePath = null,
        IReadOnlyList<(Guid BeatId, string Text)>? beatIndex = null)
    {
        // Snippet -> beat anchor (Story Ledger Phase 2). Every candidate that survives the
        // `validated` filter below is guaranteed to appear verbatim SOMEWHERE in `prose`; this
        // says where. Null for the outline path, which has no beats at all.
        Guid? ResolveSnippetBeatId(string? snippet)
        {
            if (beatIndex == null || string.IsNullOrWhiteSpace(snippet)) return null;
            foreach (var b in beatIndex)
                if (b.Text.Contains(snippet, StringComparison.Ordinal)) return b.BeatId;
            foreach (var b in beatIndex)
                if (b.Text.Contains(snippet, StringComparison.OrdinalIgnoreCase)) return b.BeatId;
            // A snippet that spans a beat boundary matches the joined prose but no single beat.
            // Leave it unanchored rather than guessing a beat it isn't wholly inside.
            return null;
        }

        var context = contextHeader + "\n" + prose;
        var raw = await llm.GenerateAsync(ExtractionQuestion, context, temperature: 0.1, maxTokens: maxTokens, ct: ct);

        var allCandidates = new List<RawCandidate>();
        var arr = ExtractJsonArrayFromText(raw);
        if (arr == null || arr.Value.GetArrayLength() == 0)
            // Warning, not Information: a non-trivial response that yields no claims means the
            // fact ledger silently got nothing from this source, and the ledger is point 2 of the
            // publish gate. This sat at Information for a long time and was easy to scroll past.
            log.LogWarning(
                "[continuity] extraction produced 0 candidates from a {Len}-char response (maxTokens={MaxTokens}). " +
                "If the response looks like valid claims, it was likely truncated mid-array. " +
                "Raw response (first 1000 chars): {Raw}",
                raw.Length, maxTokens, raw.Length > 1000 ? raw[..1000] : raw);
        if (arr != null)
        {
            foreach (var el in arr.Value.EnumerateArray())
            {
                if (el.ValueKind != JsonValueKind.Object) continue;
                var c = ParseCandidate(el, "single");
                if (c != null) allCandidates.Add(c);
            }
        }

        var validated = allCandidates
            .Where(c => prose.Contains(c.Snippet, StringComparison.Ordinal)
                     || prose.Contains(c.Snippet, StringComparison.OrdinalIgnoreCase))
            .ToList();

        // Dedup by (entity_name, predicate, object) — a single response can still repeat
        // itself; store.Upsert is idempotent either way, this just avoids double-counting.
        var grouped = validated
            .GroupBy(c => $"{Normalize(c.EntityName)}|{Normalize(c.Predicate)}|{Normalize(c.Object)}")
            .Select(g => g.First())
            .ToList();

        var diff = new ContinuityExtractionResult
        {
            ChapterId           = sourceChapterId,
            ChapterNumber       = sourceChapterNumber ?? 0,
            ChapterTitle        = sourceChapterTitle ?? "",
            VotersSuccessful    = 1,
            VotersTotal         = 1,
            CandidatesProposed  = allCandidates.Count,
            CandidatesValidated = grouped.Count,
        };

        foreach (var cand in grouped)
        {
            var resolved = ResolveEntity(cand.EntityName);
            if (resolved == null)
            {
                diff.UnknownEntities.Add(cand.EntityName);
                continue;
            }

            var claim = new ContinuityClaim
            {
                EntityId            = resolved.Value.Id,
                EntityName          = resolved.Value.Name,
                EntityKind          = resolved.Value.Kind,
                Predicate           = cand.Predicate,
                Object              = cand.Object,
                SourceType          = sourceType,
                SourceChapterId     = sourceChapterId,
                SourceChapterNumber = sourceChapterNumber,
                SourceChapterTitle  = sourceChapterTitle,
                SourcePath          = sourcePath,
                Snippet             = cand.Snippet,
                Voice               = cand.Voice,
                Confidence          = cand.Confidence,
                ExtractedBy         = new List<string> { cand.Voter },
                BookSlug            = bookSlug,
                // "observed", not "inferred": every candidate reaching this loop already passed
                // a MECHANICAL grounding gate — the `validated` filter above drops any whose
                // Snippet is not present verbatim in the source text. That is exactly what
                // ClaimProvenance.Observed means, so the grade is earned rather than asserted.
                //
                // This method serves the prose paths AND the outline path, and the grade is
                // right for both: the source text is Beat prose in one case and the
                // hand-authored Nodes.NodeOutline in the other, and in both the snippet is
                // verified verbatim against it. Neither earns "authored" — a human wrote the
                // sentence, but a model decided the (entity, predicate, object) triple, and
                // "authored" is reserved for a fact a human actually approved AS a claim.
                Provenance          = ClaimProvenance.Observed,
                SourceBeatId        = ResolveSnippetBeatId(cand.Snippet),
            };
            var r = store.Upsert(claim);
            switch (r.Outcome)
            {
                case "NEW":          diff.NewClaims++;          break;
                case "CONFIRMED":    diff.ConfirmedClaims++;    break;
                case "CONTRADICTED": diff.ContradictedClaims++; break;
            }
        }

        return diff;
    }

    /// <summary>
    /// Flatten a structured entity record into atomic claims. Loads the
    /// canonical <c>Records.Json</c> blob for the given EntityId from SQL.
    /// Trivial scalar fields (e.g. "role": "fixer") are emitted directly;
    /// prose fields (description, personality) are run through the same
    /// single-call extraction as chapter prose so we extract atomic claims from them too.
    /// </summary>
    public async Task<ContinuityExtractionResult> ExtractFromEntityRecordAsync(
        Guid entityId,
        int maxTokens = 2048,
        CancellationToken ct = default)
    {
        var result = new ContinuityExtractionResult
        {
            ChapterId    = "",
            ChapterTitle = $"entity:{entityId}",
        };

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var blob = await db.Records.AsNoTracking()
            .Where(r => r.EntityId == entityId)
            .Select(r => new { r.Json, EntityType = r.Entity!.EntityType, EntityName = r.Entity.Name })
            .FirstOrDefaultAsync(ct);
        if (blob == null)
        {
            result.Error = $"no Records.Json for entity {entityId}";
            return result;
        }
        result.ChapterTitle = $"entity:{blob.EntityName}";

        using var doc = JsonDocument.Parse(blob.Json);
        var root = doc.RootElement;

        var entityIdStr = root.TryGetProperty("id",   out var i) ? i.GetString() ?? entityId.ToString("N") : entityId.ToString("N");
        var entityName  = root.TryGetProperty("name", out var n) ? n.GetString() ?? blob.EntityName : blob.EntityName;
        var entityKind  = InferKindFromEntityType(blob.EntityType);

        if (string.IsNullOrEmpty(entityIdStr) || string.IsNullOrEmpty(entityName))
        {
            result.Error = "entity_record missing id or name";
            return result;
        }

        // 1) Direct scalar claims for top-level string fields.
        foreach (var prop in root.EnumerateObject())
        {
            if (prop.Name is "id" or "name" or "type" or "tags" or "aliases") continue;
            if (prop.Value.ValueKind != JsonValueKind.String) continue;
            var val = prop.Value.GetString() ?? "";
            if (string.IsNullOrWhiteSpace(val)) continue;

            // Skip obvious prose-style fields — those go through the LLM pass.
            if (IsProseField(prop.Name)) continue;
            if (val.Length > 200) continue; // treat long strings as prose

            var claim = new ContinuityClaim
            {
                EntityId    = entityIdStr,
                EntityName  = entityName,
                EntityKind  = entityKind,
                Predicate   = prop.Name,
                Object      = val,
                SourceType  = "entity_record",
                SourcePath  = $"db:Records[{entityId}]",
                Snippet     = val.Length > 200 ? val[..200] : val,
                Voice       = "writer",
                Confidence  = "high",
                ExtractedBy = new List<string> { "entity_record_walker" },
                // "inferred", NOT "authored": an entity-record field may have been typed by the
                // author or auto-scaffolded by CanonGroundingService, and nothing in Entities
                // records which. Grading it "authored" here would launder scaffolded guesses
                // into unqualified canon — the exact laundering that let a fabricated character
                // spread into a weapon record. Story Ledger Phase 3 adds provenance to Entities
                // themselves; that is what will make this answerable.
                Provenance  = ClaimProvenance.Inferred,
            };
            var r = store.Upsert(claim);
            switch (r.Outcome)
            {
                case "NEW":          result.NewClaims++;          break;
                case "CONFIRMED":    result.ConfirmedClaims++;    break;
                case "CONTRADICTED": result.ContradictedClaims++; break;
            }
        }

        // 2) Prose fields (description, personality, ideology, narrative_function …) get the LLM pass.
        var proseSections = new List<(string field, string text)>();
        foreach (var prop in root.EnumerateObject())
        {
            if (prop.Value.ValueKind != JsonValueKind.String) continue;
            var v = prop.Value.GetString() ?? "";
            if (IsProseField(prop.Name) && v.Length >= 80)
                proseSections.Add((prop.Name, v));
        }
        if (proseSections.Count == 0) return result;

        var ctxBuilder = new System.Text.StringBuilder();
        ctxBuilder.AppendLine($"=== ENTITY RECORD: {entityName} ({entityKind}) ===");
        foreach (var (field, text) in proseSections)
        {
            ctxBuilder.AppendLine($"--- {field} ---");
            ctxBuilder.AppendLine(text);
            ctxBuilder.AppendLine();
        }
        var ctxText = ctxBuilder.ToString();

        var raw = await llm.GenerateAsync(ExtractionQuestion, ctxText, temperature: 0.1, maxTokens: maxTokens, ct: ct);

        // The "snippet must exist in prose" check uses the combined prose
        // section text as the substrate.
        var prose = string.Join("\n", proseSections.Select(s => s.text));

        var arr = ExtractJsonArrayFromText(raw);
        if (arr != null)
        {
            foreach (var el in arr.Value.EnumerateArray())
            {
                if (el.ValueKind != JsonValueKind.Object) continue;
                var c = ParseCandidate(el, "single");
                if (c == null) continue;
                if (!prose.Contains(c.Snippet, StringComparison.OrdinalIgnoreCase)) continue;

                var claim = new ContinuityClaim
                {
                    EntityId    = entityIdStr,
                    EntityName  = entityName,
                    EntityKind  = entityKind,
                    Predicate   = c.Predicate,
                    Object      = c.Object,
                    SourceType  = "entity_record",
                    SourcePath  = $"db:Records[{entityId}]",
                    Snippet     = c.Snippet,
                    Voice       = c.Voice,
                    Confidence  = c.Confidence,
                    ExtractedBy = new List<string> { c.Voter },
                    // Same reasoning as the property-walker site above: the source record's own
                    // trustworthiness is not yet knowable, so this cannot be graded "authored".
                    Provenance  = ClaimProvenance.Inferred,
                };
                var r = store.Upsert(claim);
                switch (r.Outcome)
                {
                    case "NEW":          result.NewClaims++;          break;
                    case "CONFIRMED":    result.ConfirmedClaims++;    break;
                    case "CONTRADICTED": result.ContradictedClaims++; break;
                }
            }
        }
        result.VotersSuccessful = 1;
        result.VotersTotal      = 1;
        return result;
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static bool IsProseField(string name)
    {
        return name is "description" or "personality" or "ideology" or "narrative_function"
            or "premise" or "synopsis" or "biography" or "background" or "motto"
            or "story_hooks" or "context" or "summary";
    }

    /// <summary>
    /// Map a canonical <c>Entities.EntityType</c> value to the kind label
    /// that <see cref="ContinuityClaim.EntityKind"/> uses (mostly identical;
    /// `character` becomes `person` to match the legacy claim taxonomy).
    /// </summary>
    private static string InferKindFromEntityType(string entityType) => entityType switch
    {
        "character" => "person",
        _           => entityType,
    };

    private static string Normalize(string s)
        => string.IsNullOrEmpty(s) ? "" : Regex.Replace(s.ToLowerInvariant(), @"\s+", " ").Trim();

    /// <summary>Strips the hand-authored-markdown syntax bible content carries (heading `#`/`##`
    /// markers, `**bold**`/`*italic*`, and `` `backticks` ``) down to plain readable text — so the
    /// LLM's quoted snippets land as exact substrings of what extraction actually sees, the same
    /// grounding guarantee beat prose already gets from <see cref="BeatMarkup.StripEntityTags"/>.
    /// Only strips the markers themselves, keeps the enclosed words (`**Heritage:**` → `Heritage:`).</summary>
    internal static string StripMarkdownFormatting(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        var noHeadings = Regex.Replace(s, @"(?m)^#{1,6}\s*", "");
        var noBold     = Regex.Replace(noHeadings, @"\*\*(.+?)\*\*", "$1");
        var noItalic   = Regex.Replace(noBold, @"(?<!\*)\*(?!\*)(.+?)(?<!\*)\*(?!\*)", "$1");
        var noTicks    = noItalic.Replace("`", "");
        return noTicks;
    }

    private (string Id, string Name, string Kind)? ResolveEntity(string rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName)) return null;
        var clean = Regex.Replace(rawName, @"\s*\([^)]*\)\s*$", "").Trim();

        var p = peopleRepo.GetByName(clean) ?? peopleRepo.GetByName(rawName);
        if (p != null) return (p.Id, p.Name, "person");

        var d = placesRepo.GetByName(clean) ?? placesRepo.GetByName(rawName);
        if (d != null) return (d.Id, d.Name, "place");

        var f = factionsRepo.GetByName(clean) ?? factionsRepo.GetByName(rawName);
        if (f != null) return (f.Id, f.Name, "faction");

        var c = corponationsRepo.GetByName(clean) ?? corponationsRepo.GetByName(rawName);
        if (c != null) return (c.Id, c.Name, "corponation");

        // Universal fallback: resolve against the Entities table so a fact about
        // ANY entity type (gear, drugs, materials, orgs, synthetics, documents, …)
        // becomes a continuity claim — not just the four typed repos above. This is
        // what makes contradiction-checking corpus-wide instead of character-deep.
        using var ctx = dbFactory.CreateDbContext();
        var lower = clean.ToLowerInvariant();
        var rawLower = rawName.Trim().ToLowerInvariant();
        var hit = ctx.Entities.AsNoTracking()
            .Where(e => (e.Name.ToLower() == lower || e.Name.ToLower() == rawLower))
            .Select(e => new { e.Id, e.Name, e.EntityType })
            .FirstOrDefault();
        if (hit != null) return (hit.Id.ToString("N"), hit.Name, InferKindFromEntityType(hit.EntityType));

        return null;
    }

    private static JsonElement? ExtractJsonArrayFromText(string text)
    {
        if (string.IsNullOrEmpty(text)) return null;

        // Greedy: from first '[' to last ']'.
        var first = text.IndexOf('[');
        var last  = text.LastIndexOf(']');
        if (first >= 0 && last > first)
        {
            var slice = text[first..(last + 1)];
            try
            {
                using var d = JsonDocument.Parse(slice);
                if (d.RootElement.ValueKind == JsonValueKind.Array)
                    return d.RootElement.Clone();
            }
            catch { }
        }

        // Fallback: scan for non-empty arrays of objects.
        var rx = new Regex(@"\[\s*\{[\s\S]*?\}\s*\]", RegexOptions.Compiled);
        foreach (Match m in rx.Matches(text))
        {
            try
            {
                using var d = JsonDocument.Parse(m.Value);
                if (d.RootElement.ValueKind == JsonValueKind.Array && d.RootElement.GetArrayLength() > 0)
                    return d.RootElement.Clone();
            }
            catch { }
        }

        // Last resort: salvage whole objects out of a TRUNCATED array.
        //
        // Both passes above need the array to be closed — the greedy one needs a final ']' and the
        // regex needs a literal "}]". When the model hits its maxTokens mid-object neither matches,
        // so a response carrying a dozen perfectly good claims was discarded in full and the beat
        // silently contributed nothing to the fact ledger. Observed live 2026-08-24: a beat-save
        // extraction logged "produced 0 candidates" while its own raw response plainly contained
        // four complete, well-formed claims. Since the fact ledger is point 2 of the docs/LOGIC.md
        // §9 publish gate, a silent zero there is worse than a loud partial. Salvaging the complete
        // objects and dropping only the half-written tail is strictly better than losing the batch.
        var salvaged = SalvageCompleteObjects(text);
        if (salvaged != null)
        {
            try
            {
                using var d = JsonDocument.Parse(salvaged);
                if (d.RootElement.ValueKind == JsonValueKind.Array && d.RootElement.GetArrayLength() > 0)
                    return d.RootElement.Clone();
            }
            catch { }
        }
        return null;
    }

    /// <summary>Pull every COMPLETE top-level <c>{...}</c> object out of a possibly-truncated JSON
    /// array and re-emit them as a well-formed array. String-aware (braces and brackets inside
    /// string literals don't move the depth counter) and escape-aware, so a snippet containing a
    /// quote or a brace can't desynchronise the scan. Returns null when nothing complete is found.
    /// </summary>
    internal static string? SalvageCompleteObjects(string text)
    {
        var start = text.IndexOf('[');
        if (start < 0) return null;

        var objects = new List<string>();
        var depth = 0;
        var objStart = -1;
        var inString = false;
        var escaped = false;

        for (var i = start + 1; i < text.Length; i++)
        {
            var ch = text[i];

            if (escaped) { escaped = false; continue; }
            if (ch == '\\' && inString) { escaped = true; continue; }
            if (ch == '"') { inString = !inString; continue; }
            if (inString) continue;

            if (ch == '{')
            {
                if (depth == 0) objStart = i;
                depth++;
            }
            else if (ch == '}')
            {
                depth--;
                if (depth == 0 && objStart >= 0)
                {
                    objects.Add(text[objStart..(i + 1)]);
                    objStart = -1;
                }
                else if (depth < 0)
                {
                    depth = 0; // stray brace — resynchronise rather than abort.
                }
            }
            else if (ch == ']' && depth == 0)
            {
                break; // array closed cleanly; anything after it isn't ours.
            }
        }

        if (objects.Count == 0) return null;

        // Keep only objects that individually parse — the salvage is worthless if it re-emits
        // something malformed, and one bad object must not cost the others.
        var good = new List<string>();
        foreach (var o in objects)
        {
            try
            {
                using var d = JsonDocument.Parse(o);
                if (d.RootElement.ValueKind == JsonValueKind.Object) good.Add(o);
            }
            catch { }
        }

        return good.Count == 0 ? null : "[" + string.Join(",", good) + "]";
    }

    private static RawCandidate? ParseCandidate(JsonElement el, string voterProviderId)
    {
        var entityName = el.TryGetProperty("entity_name", out var n) ? n.GetString() ?? "" : "";
        var predicate  = el.TryGetProperty("predicate",   out var p) ? p.GetString() ?? "" : "";
        var obj        = el.TryGetProperty("object",      out var o) ? o.GetString() ?? "" : "";
        var snippet    = el.TryGetProperty("snippet",     out var s) ? s.GetString() ?? "" : "";
        if (string.IsNullOrEmpty(entityName) || string.IsNullOrEmpty(predicate)
         || string.IsNullOrEmpty(obj) || string.IsNullOrEmpty(snippet)) return null;

        return new RawCandidate
        {
            EntityName = Truncate(entityName, 200),
            Predicate  = Truncate(predicate, 80),
            Object     = Truncate(obj, 300),
            Snippet    = Truncate(snippet, 300),
            Voice      = el.TryGetProperty("voice",      out var v)  ? Truncate(v.GetString() ?? "narrator", 32) : "narrator",
            Confidence = el.TryGetProperty("confidence", out var cf) ? Truncate(cf.GetString() ?? "medium", 16) : "medium",
            Voter      = voterProviderId ?? "unknown",
        };
    }

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max];

    // ── inner types ──────────────────────────────────────────────────────────

    private class RawCandidate
    {
        public string EntityName { get; set; } = "";
        public string Predicate  { get; set; } = "";
        public string Object     { get; set; } = "";
        public string Snippet    { get; set; } = "";
        public string Voice      { get; set; } = "";
        public string Confidence { get; set; } = "";
        public string Voter      { get; set; } = "";
    }
}

public class ContinuityExtractionResult
{
    public string ChapterId           { get; set; } = "";
    public int    ChapterNumber       { get; set; }
    public string ChapterTitle        { get; set; } = "";
    public int    VotersSuccessful    { get; set; }
    public int    VotersTotal         { get; set; }
    public int    CandidatesProposed  { get; set; }
    public int    CandidatesValidated { get; set; }
    public int    NewClaims           { get; set; }
    public int    ConfirmedClaims     { get; set; }
    public int    ContradictedClaims  { get; set; }
    public List<string> UnknownEntities { get; set; } = new();
    public string? Error              { get; set; }
}
