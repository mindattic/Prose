using System.ComponentModel;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;
using Prose.Core.Data;
using Prose.Core.Services;
using Prose.Core.Services.ReadAlong;

namespace Prose.Mcp;

// ── Read-along tools (RFC 0019) ────────────────────────────────────────────────────────────────
// The engine reads what the author wrote and keeps the books: manuscript import, reading a book
// along, the automatic story bible (drafts that wait for the author), the Continuity Inbox, the
// Story Atlas, ask-your-book and the brainstorm canvas. Deterministic and local. No tool here
// writes prose, and an agent cannot keep or toss a draft or rule a continuity card on the author's
// behalf unless the author asked it to in this session — those are the author's decisions.

[McpServerToolType]
public class ReadAlongTools(HubInvoker hub, IServiceProvider sp)
{
    private static string J(object o) => JsonSerializer.Serialize(o, CanonTools.JsonOpts);

    private async Task<Guid?> BookAsync(string book)
    {
        await using var db = await sp.GetRequiredService<IDbContextFactory<ProseDbContext>>().CreateDbContextAsync();
        return await NodeRefResolver.ResolveAsync(db, book);
    }

    [McpServerTool, Description(
        "Read a book along (RFC 0019): read each chapter in order against everything the book has said so far, drafting " +
        "story-bible entries for new names and details (each with the sentence it came from) and filing a Continuity Inbox " +
        "card when a new line disagrees with an earlier one. Chapters already read as they stand are skipped unless force. " +
        "Deterministic, no LLM, never writes prose. Returns the reading log.")]
    public Task<string> ReadAlong(
        [Description("Book id, slug or code.")] string book,
        [Description("Optional chapter id or slug: read only this chapter.")] string chapter = "",
        [Description("Re-read chapters even when unchanged.")] bool force = false) =>
        hub.InvokeAsync(nameof(ReadAlongTools), nameof(ReadAlongImpl), new { book, chapter, force });

    public async Task<string> ReadAlongImpl(string book, string chapter = "", bool force = false)
    {
        if (await BookAsync(book) is not { } id) return J(new { error = "book_not_found", book });
        var ra = sp.GetRequiredService<ReadAlongService>();
        if (!string.IsNullOrWhiteSpace(chapter))
        {
            if (await BookAsync(chapter) is not { } ch) return J(new { error = "chapter_not_found", chapter });
            return J(new { results = new[] { await ra.ReadChapterAsync(id, ch, force) } });
        }
        return J(new { results = await ra.ReadBookAsync(id, force) });
    }

    [McpServerTool, Description(
        "The read-along's status for a book: chapters read as they stand vs changed since, words read, the reading log " +
        "(who each chapter introduced, how many story details, possible contradictions), pending story-bible drafts and " +
        "open Continuity Inbox cards.")]
    public Task<string> ReadAlongStatus([Description("Book id, slug or code.")] string book) =>
        hub.InvokeAsync(nameof(ReadAlongTools), nameof(ReadAlongStatusImpl), new { book });

    public async Task<string> ReadAlongStatusImpl(string book)
    {
        if (await BookAsync(book) is not { } id) return J(new { error = "book_not_found", book });
        return J(await sp.GetRequiredService<ReadAlongService>().GetStatusAsync(id));
    }

    [McpServerTool, Description(
        "The read-along background job at a glance: when it last swept and what it read (from the command ledger), " +
        "how many books it follows, and the open continuity cards and pending story-bible drafts across them.")]
    public Task<string> ReadAlongJobStatus() =>
        hub.InvokeAsync(nameof(ReadAlongTools), nameof(ReadAlongJobStatusImpl), new { });

    public async Task<string> ReadAlongJobStatusImpl()
    {
        await using var db = await sp.GetRequiredService<IDbContextFactory<ProseDbContext>>().CreateDbContextAsync();
        var last = await db.CommandLedgerEntries.AsNoTracking()
            .Where(r => r.Method == ReadAlongBackgroundService.LedgerLabel).OrderByDescending(r => r.At).FirstOrDefaultAsync();
        var follow = sp.GetRequiredService<ReadAlongFollow>();
        var readOnce = (await db.ChapterReadings.IgnoreQueryFilters().Select(r => r.BookNodeId).Distinct().ToListAsync()).ToHashSet();
        var followed = readOnce.Union(follow.All().Where(c => c.Value).Select(c => c.Key))
            .Where(id => follow.IsFollowed(id, readOnce.Contains(id))).ToList();
        var open = await db.ContinuityConflicts.IgnoreQueryFilters().CountAsync(c => followed.Contains(c.BookNodeId) && c.Status == "open");
        var drafts = await db.StoryBibleSuggestions.IgnoreQueryFilters().CountAsync(s => followed.Contains(s.BookNodeId) && s.Status == "pending");
        // Shape matches Prose.Hub.Contracts.ReadAlongJobStatusDto (the dashboard tile reads it).
        return J(new { lastSweepAt = last?.At, lastSweepSummary = last?.OutputSummary, lastSweepMs = last?.DurationMs, followedBooks = followed.Count, openCards = open, pendingDrafts = drafts });
    }

    [McpServerTool, Description(
        "Follow (or stop following) a book with the read-along's background job, which reads each chapter once it " +
        "stops changing. Only when the author asked. Nothing already read is deleted when following stops.")]
    public Task<string> SetReadAlongFollow(
        [Description("Book id, slug or code.")] string book,
        [Description("true to follow, false to stop.")] bool follow) =>
        hub.InvokeAsync(nameof(ReadAlongTools), nameof(SetReadAlongFollowImpl), new { book, follow });

    public async Task<string> SetReadAlongFollowImpl(string book, bool follow)
    {
        if (await BookAsync(book) is not { } id) return J(new { error = "book_not_found", book });
        sp.GetRequiredService<ReadAlongFollow>().Set(id, follow);
        return J(new { ok = true, book = id, follow });
    }

    [McpServerTool, Description(
        "Import a manuscript (RFC 0019): a .docx/.txt/.md file or a folder of them on the Hub's machine becomes a new book " +
        "(or chapters appended to an existing one) — volumes, chapters found from their headings, one beat per paragraph, " +
        "the words exactly as written. dryRun shows the chapter breaks without writing. read=true reads it along afterwards.")]
    public Task<string> ImportManuscript(
        [Description("Absolute path to a file or folder.")] string path,
        [Description("Book title (default: the file name).")] string title = "",
        [Description("Optional existing book (id/slug) to append chapters to.")] string intoBook = "",
        [Description("Read the book along after importing.")] bool read = false,
        [Description("Preview only: nothing is written.")] bool dryRun = true) =>
        hub.InvokeAsync(nameof(ReadAlongTools), nameof(ImportManuscriptImpl), new { path, title, intoBook, read, dryRun });

    public async Task<string> ImportManuscriptImpl(string path, string title = "", string intoBook = "", bool read = false, bool dryRun = true)
    {
        List<ManuscriptParser.InputFile> files;
        try { files = ManuscriptImportService.FilesFrom(path); }
        catch (FileNotFoundException ex) { return J(new { error = "not_found", message = ex.Message }); }
        if (files.Count == 0) return J(new { error = "no_supported_files", path });
        var t = string.IsNullOrWhiteSpace(title) ? null : title;
        if (dryRun)
        {
            var p = ManuscriptImportService.Preview(files, t);
            return J(new { dryRun = true, p.Title, p.Words, volumes = p.Volumes.Select(v => new { v.Title, chapters = v.Chapters.Select(c => new { c.Title, paragraphs = c.Paragraphs.Count, c.Words }) }) });
        }
        Guid? into = null;
        if (!string.IsNullOrWhiteSpace(intoBook))
        {
            into = await BookAsync(intoBook);
            if (into is null) return J(new { error = "book_not_found", intoBook });
        }
        var result = await sp.GetRequiredService<ManuscriptImportService>().ImportAsync(files, t, into);
        var reads = read ? await sp.GetRequiredService<ReadAlongService>().ReadBookAsync(result.BookId) : null;
        return J(new { result, reads });
    }

    [McpServerTool, Description(
        "A book's story bible (codex): entries by category (character, place, item, organization, concept), drafts marked. " +
        "With entry=<name or id>, one entry: aliases, description, every detail with its receipt (quote + chapter), its " +
        "timeline (superseded and retconned values included), links, and detail drafts waiting. With drafts=true, every " +
        "pending draft with the sentences behind it.")]
    public Task<string> StoryBible(
        [Description("Book id, slug or code.")] string book,
        [Description("Optional entry name or entity id.")] string entry = "",
        [Description("List pending drafts instead of the codex.")] bool drafts = false) =>
        hub.InvokeAsync(nameof(ReadAlongTools), nameof(StoryBibleImpl), new { book, entry, drafts });

    public async Task<string> StoryBibleImpl(string book, string entry = "", bool drafts = false)
    {
        if (await BookAsync(book) is not { } id) return J(new { error = "book_not_found", book });
        var bible = sp.GetRequiredService<StoryBibleService>();
        if (drafts) return J(new { drafts = await bible.GetDraftsAsync(id) });
        if (!string.IsNullOrWhiteSpace(entry))
        {
            Guid? eid = Guid.TryParse(entry, out var g) ? g
                : (await bible.GetCodexAsync(id)).SelectMany(c => c.Items).FirstOrDefault(i => !i.IsDraft && string.Equals(i.Name, entry, StringComparison.OrdinalIgnoreCase))?.Id;
            var e = eid is null ? null : await bible.GetEntryAsync(id, eid.Value);
            return e is null ? J(new { error = "entry_not_found", entry }) : J(e);
        }
        return J(new { codex = await bible.GetCodexAsync(id) });
    }

    [McpServerTool, Description(
        "Rule on a story-bible draft — ONLY when the author has said which (this is their decision, not an agent's): " +
        "decision 'keep' (Keep it: the draft becomes an entry / a confirmed detail; name and category may correct it) or " +
        "'toss' (Not quite: gone for good, never suggested again).")]
    public Task<string> DecideStoryBibleDraft(
        [Description("Draft id.")] string draftId,
        [Description("keep | toss")] string decision,
        [Description("Optional corrected name (keep only).")] string name = "",
        [Description("Optional corrected category (keep only).")] string category = "") =>
        hub.InvokeAsync(nameof(ReadAlongTools), nameof(DecideStoryBibleDraftImpl), new { draftId, decision, name, category });

    public async Task<string> DecideStoryBibleDraftImpl(string draftId, string decision, string name = "", string category = "")
    {
        if (!Guid.TryParse(draftId, out var id)) return J(new { error = "bad_id" });
        var bible = sp.GetRequiredService<StoryBibleService>();
        return decision?.ToLowerInvariant() switch
        {
            "keep" => J(await bible.KeepAsync(id, string.IsNullOrWhiteSpace(name) ? null : name, string.IsNullOrWhiteSpace(category) ? null : category)),
            "toss" => J(new { ok = await bible.TossAsync(id) }),
            _ => J(new { error = "decision must be keep or toss" }),
        };
    }

    [McpServerTool, Description(
        "Write a story-bible detail by hand for an entry (author-sourced, confirmed, protected from later chapters), or add " +
        "a new entry when entry does not exist yet and category is given.")]
    public Task<string> SetStoryBibleDetail(
        [Description("Book id, slug or code.")] string book,
        [Description("Entry name or entity id.")] string entry,
        [Description("Attribute, e.g. age, eyes, role, alias, group.")] string attribute,
        [Description("Value.")] string value,
        [Description("Category for a new entry: character | place | item | organization | concept.")] string category = "") =>
        hub.InvokeAsync(nameof(ReadAlongTools), nameof(SetStoryBibleDetailImpl), new { book, entry, attribute, value, category });

    public async Task<string> SetStoryBibleDetailImpl(string book, string entry, string attribute, string value, string category = "")
    {
        if (await BookAsync(book) is not { } id) return J(new { error = "book_not_found", book });
        var bible = sp.GetRequiredService<StoryBibleService>();
        Guid? eid = Guid.TryParse(entry, out var g) ? g
            : (await bible.GetCodexAsync(id)).SelectMany(c => c.Items).FirstOrDefault(i => !i.IsDraft && string.Equals(i.Name, entry, StringComparison.OrdinalIgnoreCase))?.Id;
        if (eid is null)
        {
            if (string.IsNullOrWhiteSpace(category)) return J(new { error = "entry_not_found", hint = "pass category to create it" });
            eid = await bible.AddEntryAsync(id, entry, category);
        }
        return J(new { ok = true, entityId = eid, factId = await bible.SetDetailAsync(id, eid.Value, attribute, value) });
    }

    [McpServerTool, Description(
        "A book's Continuity Inbox: every place the story disagrees with itself, each card quoting both lines with their " +
        "chapters, the kind (contradiction | updated) and a confidence. status: open (default) | all. explain=<card id> " +
        "returns why the two lines disagree.")]
    public Task<string> ContinuityInbox(
        [Description("Book id, slug or code.")] string book,
        [Description("open | all")] string status = "open",
        [Description("Optional card id to explain.")] string explain = "") =>
        hub.InvokeAsync(nameof(ReadAlongTools), nameof(ContinuityInboxImpl), new { book, status, explain });

    public async Task<string> ContinuityInboxImpl(string book, string status = "open", string explain = "")
    {
        var inbox = sp.GetRequiredService<ContinuityInboxService>();
        if (Guid.TryParse(explain, out var ex)) return J(new { explanation = await inbox.ExplainAsync(ex) });
        if (await BookAsync(book) is not { } id) return J(new { error = "book_not_found", book });
        return J(new { cards = await inbox.ListAsync(id, status) });
    }

    [McpServerTool, Description(
        "Rule on a Continuity Inbox card — ONLY when the author has decided: accept-new (the later line is the story), " +
        "keep-existing (the earlier line is; the disagreeing prose is the author's to fix), retcon (deliberate change: new " +
        "value is canon, old stays as history) or dismiss (both right in context).")]
    public Task<string> RuleContinuityCard(
        [Description("Card id.")] string cardId,
        [Description("accept-new | keep-existing | retcon | dismiss")] string action,
        [Description("Optional note recorded on the card.")] string note = "") =>
        hub.InvokeAsync(nameof(ReadAlongTools), nameof(RuleContinuityCardImpl), new { cardId, action, note });

    public async Task<string> RuleContinuityCardImpl(string cardId, string action, string note = "")
    {
        if (!Guid.TryParse(cardId, out var id)) return J(new { error = "bad_id" });
        var (ok, error) = await sp.GetRequiredService<ContinuityInboxService>().ResolveAsync(id, action, string.IsNullOrWhiteSpace(note) ? null : note);
        return J(new { ok, error });
    }

    [McpServerTool, Description(
        "The Story Atlas: a book's world as a graph drawn from what the read-along read — stars (entries with category, " +
        "first chapter, mentions) and threads (stated relationships and pairs that share paragraphs), with chapter ticks. " +
        "asOfChapter scrubs back to what the reader knew at that chapter.")]
    public Task<string> StoryAtlas(
        [Description("Book id, slug or code.")] string book,
        [Description("Optional 1-based chapter index; 0 = the whole story.")] int asOfChapter = 0,
        [Description("Include names still waiting for Keep it.")] bool includeDrafts = true) =>
        hub.InvokeAsync(nameof(ReadAlongTools), nameof(StoryAtlasImpl), new { book, asOfChapter, includeDrafts });

    public async Task<string> StoryAtlasImpl(string book, int asOfChapter = 0, bool includeDrafts = true)
    {
        if (await BookAsync(book) is not { } id) return J(new { error = "book_not_found", book });
        return J(await sp.GetRequiredService<StoryAtlasService>().GetAsync(id, asOfChapter > 0 ? asOfChapter : null, includeDrafts));
    }

    [McpServerTool, Description(
        "Ask a book a question about itself (what a character knows, when a thread last came up, whether the timeline " +
        "holds). Answered from the manuscript, with doors: the chapters and exact passages it leans on. Never writes prose. " +
        "Counts against the optional question allowance.")]
    public Task<string> AskBook(
        [Description("Book id, slug or code.")] string book,
        [Description("The question.")] string question) =>
        hub.InvokeAsync(nameof(ReadAlongTools), nameof(AskBookImpl), new { book, question });

    public async Task<string> AskBookImpl(string book, string question)
    {
        if (await BookAsync(book) is not { } id) return J(new { error = "book_not_found", book });
        return J(await sp.GetRequiredService<BookChatService>().AskAsync(id, question));
    }

    [McpServerTool, Description(
        "A book's brainstorm canvas: list cards, or add one (title, body, color: plain|idea|question|plot|character|world). " +
        "Cards are the author's free notes — never canon, never fed to the story bible.")]
    public Task<string> Brainstorm(
        [Description("Book id, slug or code.")] string book,
        [Description("Optional: add a card with this title.")] string addTitle = "",
        [Description("Body for the new card.")] string body = "",
        [Description("Color for the new card.")] string color = "idea") =>
        hub.InvokeAsync(nameof(ReadAlongTools), nameof(BrainstormImpl), new { book, addTitle, body, color });

    public async Task<string> BrainstormImpl(string book, string addTitle = "", string body = "", string color = "idea")
    {
        if (await BookAsync(book) is not { } id) return J(new { error = "book_not_found", book });
        var bs = sp.GetRequiredService<BrainstormService>();
        if (!string.IsNullOrWhiteSpace(addTitle)) return J(await bs.AddAsync(id, addTitle, body, color: color));
        return J(new { cards = await bs.ListAsync(id) });
    }

    [McpServerTool, Description(
        "The read-along's optional allowance: words read and questions asked so far, and the limits (0 = unlimited, the " +
        "default). Pass wordLimit/questionLimit (>= 0) to change them; -1 leaves a limit as it is.")]
    public Task<string> ReadAllowance(int wordLimit = -1, int questionLimit = -1) =>
        hub.InvokeAsync(nameof(ReadAlongTools), nameof(ReadAllowanceImpl), new { wordLimit, questionLimit });

    public async Task<string> ReadAllowanceImpl(int wordLimit = -1, int questionLimit = -1)
    {
        var a = sp.GetRequiredService<ReadAlongAllowance>();
        if (wordLimit >= 0 || questionLimit >= 0)
        {
            var cur = a.Get();
            a.Set(new ReadAlongAllowance.Limits(wordLimit >= 0 ? wordLimit : cur.WordLimit, questionLimit >= 0 ? questionLimit : cur.QuestionLimit));
        }
        return J(await a.GetUsageAsync());
    }
}
