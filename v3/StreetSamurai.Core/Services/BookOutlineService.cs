using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MindAttic.Legion;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Models;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Owns the book-level plot spine — the shared outline every chapter in a book
/// draws from. The Director loads this on every chapter generation so chapter N
/// knows what chapter N-1 set up and what chapter N+1 will need. The outline
/// is auto-synced to the book's chapter list (additions/removals/reorders are
/// reflected) but human-written content (synopses, key beats, threads) is
/// preserved across syncs.
/// </summary>
public class BookOutlineService
{
    private readonly IBookRepository books;
    private readonly IChapterRepository chapters;
    private readonly SettingsKvStore kv;
    private readonly LlmVotingService llmVoting;
    private readonly DatabaseService db;
    private readonly ILogger<BookOutlineService> log;

    public BookOutlineService(
        IBookRepository books, IChapterRepository chapters,
        SettingsKvStore kv,
        LlmVotingService llmVoting, DatabaseService db,
        ILogger<BookOutlineService> log)
    {
        this.books = books;
        this.chapters = chapters;
        this.kv = kv;
        this.llmVoting = llmVoting;
        this.db = db;
        this.log = log;
    }

    /// <summary>Test-fixture ctor — wraps a SQLite-in-memory factory so unit tests don't need LocalDB.</summary>
    public BookOutlineService(
        IBookRepository books, IChapterRepository chapters,
        IPathProvider paths,
        LlmVotingService llmVoting, DatabaseService db,
        ILogger<BookOutlineService> log)
        : this(books, chapters,
               new SettingsKvStore(StreetSamurai.Core.Data.TestDbFactory.For(paths, "settings")),
               llmVoting, db, log) { }

    /// <summary>Settings key for a per-book outline document. Book ids arrive both dashed
    /// (GUID form) and dashless (32-hex book file form); stored keys use the dashless form,
    /// so normalize here — a dashed lookup must not silently miss the row and fall back to
    /// BuildFromCanon with empty threads.</summary>
    private static string Key(string bookId) =>
        $"book_outline:{bookId.Trim().Replace("-", "").ToLowerInvariant()}";

    public BookOutline Load(string bookId)
    {
        var loaded = kv.Get<BookOutline>(Key(bookId));
        if (loaded == null) return BuildFromCanon(bookId);
        loaded.BookId = bookId;
        return SyncWithBook(loaded);
    }

    public void Save(BookOutline outline)
    {
        outline.Modified = DateTime.UtcNow;
        kv.Set(Key(outline.BookId), outline);
    }

    /// <summary>
    /// True when this book's outline has been Approved and is therefore unlocked
    /// for chapter prose generation. Defense-in-depth contract for any flow that
    /// triggers book-context prose generation — UI guards may be missing on a
    /// new entry point, but a service-layer check covers the new path for free.
    /// </summary>
    public bool IsApprovedForGeneration(string bookId)
        => Load(bookId).Status == OutlineStatus.Approved;

    /// <summary>
    /// Compare a chapter's outline body against the actual prose and surface
    /// discrepancies. The outline is a CONTRACT — what the chapter should
    /// deliver — and the prose is the receipt. When they disagree, either the
    /// prose lost the thread or the outline grew stale; either way, the user
    /// should see it.
    ///
    /// Single low-tier LLM call. Returns the list of detected drifts;
    /// summaries can be passed to <c>FindingsService</c> for persistence.
    /// </summary>
    public async Task<List<OutlineDriftFinding>> DetectChapterDriftAsync(
        string bookId, string chapterId, string chapterProse, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(chapterProse)) return new List<OutlineDriftFinding>();
        var outline = Load(bookId);
        var ch = outline.Chapters.FirstOrDefault(c => c.ChapterId == chapterId);
        if (ch == null || string.IsNullOrWhiteSpace(ch.EffectiveBody)) return new List<OutlineDriftFinding>();

        var providers = llmVoting.GetActiveProviderIds();
        if (providers.Count == 0) return new List<OutlineDriftFinding>();

        var system =
            "You audit chapter prose against the chapter's outline contract. The outline says " +
            "what the chapter SHOULD deliver — beats, threads opened/closed, character POV, " +
            "end-state. The prose is what was actually written. Surface drift: things the outline " +
            "promised that the prose didn't deliver, or things the prose introduced that contradict " +
            "the outline. Return STRICT JSON: an array of objects with shape " +
            "{\"kind\": <'missing' | 'contradiction' | 'extra'>, \"summary\": <one sentence>, " +
            "\"outline_says\": <quote/paraphrase>, \"prose_says\": <quote/paraphrase>}. " +
            "Empty array if the prose delivers what the outline promised. No prose outside the JSON.";

        var user = $"""
            CHAPTER {ch.Number}: {ch.Title}
            POV: {ch.PovCharacter}

            OUTLINE BODY (what the chapter should deliver):
            {ch.EffectiveBody}

            PROSE (what was actually written):
            {(chapterProse.Length > 8000 ? chapterProse[..8000] + "\n…" : chapterProse)}
            """;

        var request = new VoteRequest
        {
            Question = system,
            Context  = user,
            MaxTokens = 1024,
            Temperature = 0.2,
            SynthesizeNarrative = false,
        };

        VotingResult result;
        try { result = await llmVoting.VoteAsync(request, Quorum.Plurality, ct); }
        catch (Exception ex) { log.LogWarning(ex, "Outline drift detection failed"); return new List<OutlineDriftFinding>(); }

        // Aggregate drift findings across voters — dedupe by summary.
        var bag = new Dictionary<string, OutlineDriftFinding>(StringComparer.OrdinalIgnoreCase);
        foreach (var v in result.IndividualVotes.Where(v => !v.IsError))
        {
            var payload = !string.IsNullOrWhiteSpace(v.Decision) ? v.Decision : v.Reasoning;
            foreach (var f in ParseDriftFindings(payload))
            {
                var key = f.Summary;
                if (string.IsNullOrWhiteSpace(key)) continue;
                bag.TryAdd(key, f);
            }
        }
        return bag.Values.ToList();
    }

    internal static List<OutlineDriftFinding> ParseDriftFindings(string? payload)
    {
        var result = new List<OutlineDriftFinding>();
        if (string.IsNullOrWhiteSpace(payload)) return result;
        var start = payload.IndexOf('[');
        var end   = payload.LastIndexOf(']');
        if (start < 0 || end <= start) return result;
        try
        {
            using var doc = JsonDocument.Parse(payload[start..(end + 1)]);
            if (doc.RootElement.ValueKind != JsonValueKind.Array) return result;
            foreach (var e in doc.RootElement.EnumerateArray())
            {
                if (e.ValueKind != JsonValueKind.Object) continue;
                result.Add(new OutlineDriftFinding(
                    Kind:        e.TryGetProperty("kind",         out var k)  ? k.GetString() ?? "" : "",
                    Summary:     e.TryGetProperty("summary",      out var s)  ? s.GetString() ?? "" : "",
                    OutlineSays: e.TryGetProperty("outline_says", out var os) ? os.GetString() ?? "" : "",
                    ProseSays:   e.TryGetProperty("prose_says",   out var ps) ? ps.GetString() ?? "" : ""));
            }
        }
        catch { }
        return result;
    }

    /// <summary>
    /// Throw <see cref="OutlineNotApprovedException"/> when the outline isn't
    /// Approved. Call this from any prose-generation entry point that targets a
    /// specific book — e.g. the autopilot loop in BookOutlineEditor.razor — so
    /// the contract documented on <see cref="OutlineStatus"/> is mechanically
    /// enforced, not just commented.
    /// </summary>
    public void EnsureApprovedForGeneration(string bookId)
    {
        var outline = Load(bookId);
        if (outline.Status != OutlineStatus.Approved)
            throw new OutlineNotApprovedException(bookId, outline.Status);
    }

    /// <summary>
    /// Build a starter outline from canon: pulls Book metadata + each chapter's
    /// existing title/synopsis. Used on first access (no file) so chapters always
    /// see *some* shared spine even before the user authors one.
    /// </summary>
    private BookOutline BuildFromCanon(string bookId)
    {
        var book = books.LoadBook(bookId);
        if (book == null) return new BookOutline { BookId = bookId };

        var outline = new BookOutline
        {
            BookId    = bookId,
            Premise   = book.Premise,
            ArcTarget = book.ArcTarget,
            Theme     = "",  // user-authored; empty by default
            Structure = "freeform",
        };
        for (int i = 0; i < book.ChapterIds.Count; i++)
        {
            var c = chapters.LoadChapter(book.ChapterIds[i]);
            if (c == null) continue;
            outline.Chapters.Add(new BookChapterOutline
            {
                ChapterId     = c.Id,
                Number        = i + 1,
                Title         = c.Title,
                ShortSynopsis = "",
                LongSynopsis  = c.Synopsis,
                PovCharacter  = c.Characters.FirstOrDefault() ?? book.Protagonists.FirstOrDefault() ?? "",
            });
        }
        return outline;
    }

    /// <summary>
    /// Reconcile the outline with current Book.ChapterIds. Adds entries for new chapters,
    /// removes orphaned entries (chapter deleted), reorders to match. Preserves user-authored
    /// fields (synopses, key beats, threads) for chapters still present.
    /// </summary>
    private BookOutline SyncWithBook(BookOutline outline)
    {
        var book = books.LoadBook(outline.BookId);
        if (book == null) return outline;

        var byId = outline.Chapters.ToDictionary(c => c.ChapterId);
        var synced = new List<BookChapterOutline>();

        for (int i = 0; i < book.ChapterIds.Count; i++)
        {
            var cid = book.ChapterIds[i];
            if (byId.TryGetValue(cid, out var existing))
            {
                existing.Number = i + 1;
                // Refresh title from chapter in case it was renamed.
                var c = chapters.LoadChapter(cid);
                if (c != null) existing.Title = c.Title;
                synced.Add(existing);
            }
            else
            {
                var c = chapters.LoadChapter(cid);
                synced.Add(new BookChapterOutline
                {
                    ChapterId    = cid,
                    Number       = i + 1,
                    Title        = c?.Title ?? "(missing)",
                    LongSynopsis = c?.Synopsis ?? "",
                    PovCharacter = c?.Characters.FirstOrDefault() ?? "",
                });
            }
        }

        outline.Chapters = synced;
        return outline;
    }

    /// <summary>
    /// Compose a "WHERE WE ARE" prompt block for the Director. This is the cross-chapter
    /// communication layer — when generating chapter N, the prompt sees the full plot,
    /// what each prior chapter established, what threads are open, and what the next
    /// chapter is supposed to deliver.
    /// </summary>
    public string BuildDirectorContext(string bookId, string currentChapterId)
    {
        var outline = Load(bookId);
        if (outline.Chapters.Count == 0) return "";

        var sb = new StringBuilder();
        sb.AppendLine("── BOOK PLOT (shared across all chapters) ──");
        if (!string.IsNullOrEmpty(outline.Premise))   sb.AppendLine($"PREMISE: {outline.Premise}");
        if (!string.IsNullOrEmpty(outline.ArcTarget)) sb.AppendLine($"ARC TARGET: {outline.ArcTarget}");
        if (!string.IsNullOrEmpty(outline.Theme))     sb.AppendLine($"THEME: {outline.Theme}");
        sb.AppendLine();

        // Locate the current chapter's position so we can split prior / current / future.
        var currentIdx = outline.Chapters.FindIndex(c => c.ChapterId == currentChapterId);
        if (currentIdx < 0) currentIdx = outline.Chapters.Count;  // treat as appending

        if (currentIdx > 0)
        {
            sb.AppendLine("── PRIOR CHAPTERS (already established — DO NOT contradict, DO call back where natural) ──");
            for (int i = 0; i < currentIdx; i++)
            {
                var ch = outline.Chapters[i];
                sb.Append($"  Ch {ch.Number} \"{ch.Title}\"");
                if (!string.IsNullOrEmpty(ch.PovCharacter)) sb.Append($" [POV: {ch.PovCharacter}]");
                sb.AppendLine();
                var blurb = ch.EffectiveBody;
                if (!string.IsNullOrWhiteSpace(blurb))
                {
                    foreach (var line in blurb.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                        sb.AppendLine($"    {line.TrimEnd()}");
                }
            }
            sb.AppendLine();
        }

        if (currentIdx < outline.Chapters.Count)
        {
            var current = outline.Chapters[currentIdx];
            sb.AppendLine($"── THIS CHAPTER ({current.Number}: \"{current.Title}\") ──");
            var currentBlurb = current.EffectiveBody;
            if (!string.IsNullOrWhiteSpace(currentBlurb))
                foreach (var line in currentBlurb.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                    sb.AppendLine($"  {line.TrimEnd()}");
            sb.AppendLine();
        }

        if (currentIdx + 1 < outline.Chapters.Count)
        {
            sb.AppendLine("── UPCOMING CHAPTERS (do not steal their material) ──");
            for (int i = currentIdx + 1; i < outline.Chapters.Count; i++)
            {
                var ch = outline.Chapters[i];
                sb.Append($"  Ch {ch.Number} \"{ch.Title}\"");
                // First non-empty line of the blurb stands in for "short synopsis" in this dense list view.
                var firstLine = (ch.EffectiveBody ?? "").Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(firstLine)) sb.Append(" — ").Append(Truncate(firstLine.Trim(), 140));
                sb.AppendLine();
            }
            sb.AppendLine();
        }

        // Open threads at the book level — useful when chapter outlines don't cover everything.
        var openThreads = outline.Threads.Where(t => t.Status == ThreadStatus.Open).ToList();
        if (openThreads.Any())
        {
            sb.AppendLine("── OPEN THREADS (planted, not yet paid off) ──");
            foreach (var t in openThreads)
                sb.AppendLine($"  {t.Name}: {t.Description}");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static string Truncate(string s, int n) =>
        s.Length <= n ? s : s[..n] + "…";

    // ── LLM-assisted: combined Surprise/Guide outline generator ─────────────────
    // The user can fill in as little or as much as they want. Anything left blank
    // gets filled by the LLM, treating filled-in fields as fixed canon. The result
    // is always a complete outline ready for human review.

    public async Task<BookOutline> GenerateOutlineAsync(string bookId, CancellationToken ct = default)
    {
        var outline = Load(bookId);
        var book = books.LoadBook(bookId);
        if (book == null) return outline;

        var providers = llmVoting.GetActiveProviderIds();
        if (providers.Count == 0)
        {
            log.LogWarning("No LLM providers configured — cannot generate outline");
            return outline;
        }

        var prompt = BuildGenerationPrompt(book, outline);

        var request = new VoteRequest
        {
            Question = "Produce a complete book outline in the JSON format specified. Honor any pre-filled fields exactly; fill blanks with material consistent with the book's premise, arc target, theme, and protagonist voice. Output strict JSON, no prose outside.",
            Context = prompt,
            MaxTokens = 4096,
            Temperature = 0.7,
            SynthesizeNarrative = false,
        };

        VotingResult voting;
        try
        {
            voting = await llmVoting.VoteAsync(request, Quorum.Plurality, ct);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Outline generation voting failed for {BookId}", bookId);
            return outline;
        }

        // Take the first non-error voter — outline generation is creative, multi-voter merge would homogenize.
        var bestVote = voting.IndividualVotes.FirstOrDefault(v => !v.IsError && !string.IsNullOrWhiteSpace(v.Decision));
        if (bestVote == null) return outline;

        var generated = ParseGenerated(bestVote.Decision, outline);
        if (generated == null) return outline;

        outline = MergeGenerated(outline, generated);
        outline.Status = OutlineStatus.Draft;  // even after generation, user reviews before approving
        Save(outline);
        return outline;
    }

    private string BuildGenerationPrompt(Book book, BookOutline filled)
    {
        var sb = new StringBuilder();
        sb.AppendLine("BOOK:");
        sb.AppendLine($"  title: {book.Title}");
        if (!string.IsNullOrEmpty(book.Tagline)) sb.AppendLine($"  tagline: {book.Tagline}");
        if (!string.IsNullOrEmpty(book.Premise)) sb.AppendLine($"  premise: {book.Premise}");
        if (!string.IsNullOrEmpty(book.ArcTarget)) sb.AppendLine($"  arc target: {book.ArcTarget}");
        if (book.Protagonists.Any()) sb.AppendLine($"  protagonists: {string.Join(", ", book.Protagonists)}");
        sb.AppendLine();

        // Inject character voice rubric for each protagonist — drives chapter-level POV consistency.
        foreach (var name in book.Protagonists)
        {
            var c = db.FindCharacter(name);
            if (c?.SpeechPatterns == null) continue;
            sb.AppendLine($"VOICE — {name}: {c.SpeechPatterns.Cadence} | {c.SpeechPatterns.Vocabulary}");
        }
        sb.AppendLine();

        sb.AppendLine("CURRENT OUTLINE (any non-empty fields are FIXED — do not change them, fill in around them):");
        if (!string.IsNullOrEmpty(filled.Theme))     sb.AppendLine($"  theme [FIXED]: {filled.Theme}");
        if (!string.IsNullOrEmpty(filled.Structure)) sb.AppendLine($"  structure: {filled.Structure}");

        for (int i = 0; i < filled.Chapters.Count; i++)
        {
            var ch = filled.Chapters[i];
            sb.AppendLine($"  Chapter {ch.Number} \"{ch.Title}\":");
            if (!string.IsNullOrEmpty(ch.PovCharacter)) sb.AppendLine($"    pov: {ch.PovCharacter}");
            if (!string.IsNullOrWhiteSpace(ch.EffectiveBody))
                sb.AppendLine($"    body [FIXED]: {ch.EffectiveBody.Replace("\n", " ").Trim()}");
        }
        sb.AppendLine();

        sb.AppendLine("OUTPUT JSON SHAPE (fill every chapter; preserve all [FIXED] fields verbatim):");
        sb.AppendLine("{");
        sb.AppendLine("  \"theme\": \"<one sentence on what this book is ABOUT>\",");
        sb.AppendLine("  \"structure\": \"<three-act | five-act | freeform>\",");
        sb.AppendLine("  \"chapters\": [");
        sb.AppendLine("    {");
        sb.AppendLine("      \"number\": <int>,");
        sb.AppendLine("      \"title\": \"<chapter title — keep existing if provided>\",");
        sb.AppendLine("      \"pov_character\": \"<name>\",");
        sb.AppendLine("      \"body\": \"<one short paragraph: what happens, what beats fire, what threads this chapter opens or closes, end-state. Freeform prose, not a structured field list.>\"");
        sb.AppendLine("    }");
        sb.AppendLine("  ],");
        sb.AppendLine("  \"threads\": [");
        sb.AppendLine("    { \"name\": \"\", \"description\": \"\", \"planted_in_chapter_number\": <int>, \"pays_off_in_chapter_number\": <int or null> }");
        sb.AppendLine("  ]");
        sb.AppendLine("}");
        return sb.ToString();
    }

    private record GenChapter(int Number, string? Title, string? Pov, string? Body);
    private record GenThread(string Name, string Description, int? PlantedNum, int? PaysOffNum);
    private record GenOutline(string? Theme, string? Structure, List<GenChapter> Chapters, List<GenThread> Threads);

    private static GenOutline? ParseGenerated(string answer, BookOutline existing)
    {
        var start = answer.IndexOf('{');
        var end = answer.LastIndexOf('}');
        if (start < 0 || end <= start) return null;
        var json = answer[start..(end + 1)];
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var chapters = new List<GenChapter>();
            if (root.TryGetProperty("chapters", out var chArr) && chArr.ValueKind == JsonValueKind.Array)
            {
                foreach (var c in chArr.EnumerateArray())
                {
                    if (c.ValueKind != JsonValueKind.Object) continue;
                    // Tolerate either the new "body" shape or the legacy structured shape —
                    // older voters may still respond with short_synopsis / long_synopsis /
                    // key_beats / opens_threads / closes_threads. Whatever we get, fold it
                    // into one body string and let the editor surface that.
                    string? body = c.TryGetProperty("body", out var b) ? b.GetString() : null;
                    if (string.IsNullOrWhiteSpace(body))
                    {
                        var sb = new StringBuilder();
                        if (c.TryGetProperty("long_synopsis",  out var l) && !string.IsNullOrWhiteSpace(l.GetString())) sb.AppendLine(l.GetString());
                        else if (c.TryGetProperty("short_synopsis", out var s) && !string.IsNullOrWhiteSpace(s.GetString())) sb.AppendLine(s.GetString());
                        var beats  = ReadStringArray(c, "key_beats");
                        var opens  = ReadStringArray(c, "opens_threads");
                        var closes = ReadStringArray(c, "closes_threads");
                        if (beats.Count  > 0) sb.AppendLine("Beats: "  + string.Join(" | ", beats));
                        if (opens.Count  > 0) sb.AppendLine("Opens: "  + string.Join("; ",  opens));
                        if (closes.Count > 0) sb.AppendLine("Closes: " + string.Join("; ",  closes));
                        body = sb.ToString().TrimEnd();
                    }
                    chapters.Add(new GenChapter(
                        Number: c.TryGetProperty("number", out var n) && n.TryGetInt32(out var ni) ? ni : 0,
                        Title:  c.TryGetProperty("title", out var t) ? t.GetString() : null,
                        Pov:    c.TryGetProperty("pov_character", out var p) ? p.GetString() : null,
                        Body:   body));
                }
            }
            var threads = new List<GenThread>();
            if (root.TryGetProperty("threads", out var thArr) && thArr.ValueKind == JsonValueKind.Array)
            {
                foreach (var t in thArr.EnumerateArray())
                {
                    if (t.ValueKind != JsonValueKind.Object) continue;
                    threads.Add(new GenThread(
                        Name:        t.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "",
                        Description: t.TryGetProperty("description", out var d) ? d.GetString() ?? "" : "",
                        PlantedNum:  t.TryGetProperty("planted_in_chapter_number", out var pl) && pl.TryGetInt32(out var pli) ? pli : null,
                        PaysOffNum:  t.TryGetProperty("pays_off_in_chapter_number", out var po) && po.TryGetInt32(out var poi) ? poi : null));
                }
            }
            return new GenOutline(
                Theme:     root.TryGetProperty("theme", out var th) ? th.GetString() : null,
                Structure: root.TryGetProperty("structure", out var st) ? st.GetString() : null,
                Chapters:  chapters,
                Threads:   threads);
        }
        catch (Exception ex) { Serilog.Log.Warning(ex, "Failed to parse generated outline"); return null; }
    }

    private static List<string> ReadStringArray(JsonElement parent, string name)
    {
        if (!parent.TryGetProperty(name, out var arr) || arr.ValueKind != JsonValueKind.Array) return [];
        return arr.EnumerateArray().Where(e => e.ValueKind == JsonValueKind.String)
            .Select(e => e.GetString() ?? "").Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
    }

    /// <summary>Merge LLM-generated content into the outline. Existing user-authored fields are preserved.</summary>
    private static BookOutline MergeGenerated(BookOutline existing, GenOutline gen)
    {
        if (string.IsNullOrEmpty(existing.Theme) && !string.IsNullOrEmpty(gen.Theme)) existing.Theme = gen.Theme;
        if (existing.Structure == "freeform" && !string.IsNullOrEmpty(gen.Structure)) existing.Structure = gen.Structure;

        // Match generated chapter entries to existing by Number; fill blanks only.
        foreach (var g in gen.Chapters)
        {
            var ch = existing.Chapters.FirstOrDefault(c => c.Number == g.Number);
            if (ch == null) continue;
            if (string.IsNullOrEmpty(ch.PovCharacter) && !string.IsNullOrEmpty(g.Pov)) ch.PovCharacter = g.Pov!;
            // Fill Body only if neither the new field nor the legacy fields have content —
            // i.e. the chapter has no human-authored outline yet at all.
            var hasAuthored = !string.IsNullOrWhiteSpace(ch.Body)
                           || !string.IsNullOrWhiteSpace(ch.LongSynopsis)
                           || !string.IsNullOrWhiteSpace(ch.ShortSynopsis)
                           || ch.KeyBeats.Count > 0 || ch.OpensThreads.Count > 0 || ch.ClosesThreads.Count > 0;
            if (!hasAuthored && !string.IsNullOrWhiteSpace(g.Body)) ch.Body = g.Body!;
        }

        // Generated threads: add only if no thread by that name already exists.
        foreach (var t in gen.Threads)
        {
            if (existing.Threads.Any(e => string.Equals(e.Name, t.Name, StringComparison.OrdinalIgnoreCase))) continue;
            var planted = t.PlantedNum.HasValue
                ? existing.Chapters.FirstOrDefault(c => c.Number == t.PlantedNum.Value)?.ChapterId ?? "" : "";
            var paysOff = t.PaysOffNum.HasValue
                ? existing.Chapters.FirstOrDefault(c => c.Number == t.PaysOffNum.Value)?.ChapterId ?? "" : "";
            existing.Threads.Add(new BookThread
            {
                Name = t.Name,
                Description = t.Description,
                PlantedInChapterId = planted,
                PaysOffInChapterId = paysOff,
            });
        }
        return existing;
    }

    // ── Reconsideration on edit ─────────────────────────────────────────────

    /// <summary>
    /// User edited one chapter's outline entry. Ask the LLM to surface adjustments
    /// that need to happen in OTHER chapters (before for setup, after for consequences)
    /// so the outline stays coherent. Suggestions land in
    /// <see cref="BookOutline.PendingAdjustments"/> for human accept/reject.
    /// </summary>
    public async Task<BookOutline> ReconsiderAfterEditAsync(string bookId, string editedChapterId, CancellationToken ct = default)
    {
        var outline = Load(bookId);
        var book = books.LoadBook(bookId);
        if (book == null) return outline;

        var providers = llmVoting.GetActiveProviderIds();
        if (providers.Count == 0) return outline;

        var idx = outline.Chapters.FindIndex(c => c.ChapterId == editedChapterId);
        if (idx < 0) return outline;

        var prompt = BuildReconsiderationPrompt(outline, idx);

        var request = new VoteRequest
        {
            Question = "An outline entry was edited. Surface adjustments needed in OTHER chapters (before for setup, after for consequence) to keep the outline coherent. Output strict JSON array, no prose.",
            Context = prompt,
            MaxTokens = 3072,
            Temperature = 0.4,
            SynthesizeNarrative = false,
        };

        VotingResult voting;
        try
        {
            voting = await llmVoting.VoteAsync(request, Quorum.Plurality, ct);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Reconsideration failed for {BookId}/{ChapterId}", bookId, editedChapterId);
            return outline;
        }

        var newAdjustments = new List<OutlineAdjustment>();
        foreach (var v in voting.IndividualVotes.Where(v => !v.IsError))
        {
            var payload = !string.IsNullOrWhiteSpace(v.Decision) ? v.Decision : v.Reasoning;
            newAdjustments.AddRange(ParseAdjustments(payload, outline));
        }

        // Replace prior pending set — user accepts/rejects the freshest reconsideration.
        outline.PendingAdjustments = newAdjustments;
        outline.Status = newAdjustments.Any() ? OutlineStatus.InReview : OutlineStatus.Draft;
        Save(outline);
        return outline;
    }

    private static string BuildReconsiderationPrompt(BookOutline outline, int editedIdx)
    {
        var edited = outline.Chapters[editedIdx];
        var sb = new StringBuilder();
        sb.AppendLine($"BOOK PREMISE: {outline.Premise}");
        sb.AppendLine($"BOOK THEME: {outline.Theme}");
        sb.AppendLine($"ARC TARGET: {outline.ArcTarget}");
        sb.AppendLine();
        sb.AppendLine($"EDITED CHAPTER (Ch {edited.Number} \"{edited.Title}\"):");
        sb.AppendLine($"  body: {edited.EffectiveBody}");
        sb.AppendLine();
        sb.AppendLine("OTHER CHAPTERS (these may need adjustments to stay coherent):");
        for (int i = 0; i < outline.Chapters.Count; i++)
        {
            if (i == editedIdx) continue;
            var c = outline.Chapters[i];
            var direction = i < editedIdx ? "before" : "after";
            sb.AppendLine($"  [{direction}] Ch {c.Number} \"{c.Title}\":");
            sb.AppendLine($"    body: {c.EffectiveBody}");
        }
        sb.AppendLine();
        sb.AppendLine("OUTPUT — strict JSON array. Each adjustment:");
        sb.AppendLine("{");
        sb.AppendLine("  \"chapter_number\": <int — the chapter being adjusted, NOT the edited one>,");
        sb.AppendLine("  \"field\": \"body\",");
        sb.AppendLine("  \"before\": \"<current value>\",");
        sb.AppendLine("  \"after\": \"<proposed replacement>\",");
        sb.AppendLine("  \"rationale\": \"<one or two sentences why this adjustment is needed given the edit>\",");
        sb.AppendLine("  \"direction\": \"before\" | \"after\"");
        sb.AppendLine("}");
        sb.AppendLine("Only suggest adjustments that genuinely improve coherence. An empty array is a valid answer if the edit doesn't ripple. Cap at 8 suggestions.");
        return sb.ToString();
    }

    private static List<OutlineAdjustment> ParseAdjustments(string answer, BookOutline outline)
    {
        var start = answer.IndexOf('[');
        var end = answer.LastIndexOf(']');
        if (start < 0 || end <= start) return [];
        var json = answer[start..(end + 1)];
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array) return [];

            var result = new List<OutlineAdjustment>();
            foreach (var e in doc.RootElement.EnumerateArray())
            {
                if (e.ValueKind != JsonValueKind.Object) continue;
                var num = e.TryGetProperty("chapter_number", out var n) && n.TryGetInt32(out var ni) ? ni : 0;
                var ch = outline.Chapters.FirstOrDefault(c => c.Number == num);
                if (ch == null) continue;

                result.Add(new OutlineAdjustment
                {
                    ChapterId = ch.ChapterId,
                    Field     = e.TryGetProperty("field", out var f) ? f.GetString() ?? "" : "",
                    Before    = e.TryGetProperty("before", out var b) ? b.GetString() ?? "" : "",
                    After     = e.TryGetProperty("after", out var a) ? a.GetString() ?? "" : "",
                    Rationale = e.TryGetProperty("rationale", out var r) ? r.GetString() ?? "" : "",
                    Direction = e.TryGetProperty("direction", out var d) ? d.GetString() ?? "" : "",
                });
            }
            return result;
        }
        catch { return []; }
    }

    /// <summary>Apply a pending adjustment to the chapter outline entry. Idempotent — already-applied is a no-op.</summary>
    public BookOutline ApplyAdjustment(string bookId, string adjustmentId)
    {
        var outline = Load(bookId);
        var adj = outline.PendingAdjustments.FirstOrDefault(a => a.Id == adjustmentId);
        if (adj == null || adj.Status != ReviewStatus.Pending) return outline;

        var ch = outline.Chapters.FirstOrDefault(c => c.ChapterId == adj.ChapterId);
        if (ch == null) return outline;

        switch (adj.Field)
        {
            case "body":           ch.Body = adj.After; break;
            // Legacy field paths preserved for adjustments emitted by older voters
            // before the body migration. Tolerated, not invited — the prompt only
            // asks for "body" now.
            case "long_synopsis":  ch.LongSynopsis = adj.After; break;
            case "short_synopsis": ch.ShortSynopsis = adj.After; break;
            case "key_beats":      ch.KeyBeats = SplitList(adj.After); break;
            case "opens_threads":  ch.OpensThreads = SplitList(adj.After); break;
            case "closes_threads": ch.ClosesThreads = SplitList(adj.After); break;
        }

        adj.Status = ReviewStatus.Applied;
        Save(outline);
        return outline;
    }

    public BookOutline DismissAdjustment(string bookId, string adjustmentId)
    {
        var outline = Load(bookId);
        var adj = outline.PendingAdjustments.FirstOrDefault(a => a.Id == adjustmentId);
        if (adj == null) return outline;
        adj.Status = ReviewStatus.Rejected;
        Save(outline);
        return outline;
    }

    public BookOutline Approve(string bookId)
    {
        var outline = Load(bookId);
        // Drop any rejected adjustments; keep applied ones for audit.
        outline.PendingAdjustments = outline.PendingAdjustments.Where(a => a.Status != ReviewStatus.Rejected).ToList();
        outline.Status = OutlineStatus.Approved;
        Save(outline);
        return outline;
    }

    private static List<string> SplitList(string s) =>
        s.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();
}
