using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Interfaces;
using static Prose.Core.Services.ReadAlong.StoryFactExtractor;

namespace Prose.Core.Services.ReadAlong;

/// <summary>
/// "Ask your book" (RFC 0019 §7): a conversation about the manuscript, answered from the
/// manuscript. Every answer carries doors — the chapter and the exact passage it leans on — so the
/// author can step straight onto the page. It talks plot; it never writes the book: the assistant is
/// told so, and it is never given a way to write (it returns text to a chat pane, nothing else).
///
/// <para>Retrieval is deterministic (names and words scored against every paragraph, plus the
/// confirmed story-bible details of the names asked about). The answer uses the configured LLM when
/// there is one; with none, or when it fails, the read-along answers from its own records — the
/// details it has kept and the passages that mention them.</para>
/// </summary>
public sealed class BookChatService(
    IDbContextFactory<ProseDbContext> dbFactory,
    ReadAlongService readAlong,
    ReadAlongAllowance allowance,
    IServiceProvider services,
    ILogger<BookChatService> log)
{
    public sealed record Door(Guid BeatId, Guid ChapterNodeId, string ChapterTitle, int ChapterIndex, string Quote, Guid? EntityId = null);

    public sealed record Message(Guid Id, string Role, string Text, IReadOnlyList<Door> Doors, DateTime CreatedAt);

    public sealed record Answer(Message Question, Message Reply, bool UsedLlm, string? Refused);

    public async Task<List<Message>> HistoryAsync(Guid bookId, int take = 100, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var rows = await db.BookChatMessages.AsNoTracking().IgnoreQueryFilters()
            .Where(m => m.BookNodeId == bookId).OrderByDescending(m => m.CreatedAt).Take(take).ToListAsync(ct);
        rows.Reverse();
        return rows.Select(ToMessage).ToList();
    }

    public async Task<int> ClearAsync(Guid bookId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var rows = await db.BookChatMessages.IgnoreQueryFilters().Where(m => m.BookNodeId == bookId).ToListAsync(ct);
        db.BookChatMessages.RemoveRange(rows);
        await db.SaveChangesAsync(ct);
        return rows.Count;
    }

    private static Message ToMessage(BookChatMessage m)
    {
        List<Door> doors;
        try { doors = JsonSerializer.Deserialize<List<Door>>(m.DoorsJson, ReadAlongService.Json) ?? []; }
        catch (JsonException) { doors = []; }
        return new Message(m.Id, m.Role, m.Text, doors, m.CreatedAt);
    }

    /// <param name="historyTurns">How much of the conversation the answer sees. "Room for much
    /// longer conversations" is this number.</param>
    public async Task<Answer> AskAsync(Guid bookId, string question, int historyTurns = 8, int maxDoors = 6, CancellationToken ct = default)
    {
        question = (question ?? "").Trim();
        if (question.Length == 0) throw new ArgumentException("question is required", nameof(question));

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var universeId = await db.Nodes.IgnoreQueryFilters().Where(n => n.Id == bookId).Select(n => n.UniverseId).FirstAsync(ct);

        if (!await allowance.CanAskAsync(ct))
        {
            var usage = await allowance.GetUsageAsync(ct);
            var refusedQ = new Message(Guid.Empty, "user", question, [], DateTime.UtcNow);
            var refusedA = new Message(Guid.Empty, "assistant",
                $"You've asked {usage.QuestionsAsked} of {usage.QuestionLimit} questions this install allows. Everything already answered stays; raise the question limit in Settings to keep talking.",
                [], DateTime.UtcNow);
            return new Answer(refusedQ, refusedA, false, "allowance");
        }

        var history = await HistoryAsync(bookId, historyTurns * 2, ct);
        var userRow = new BookChatMessage { UniverseId = universeId, BookNodeId = bookId, Role = "user", Text = question };
        db.BookChatMessages.Add(userRow);
        await db.SaveChangesAsync(ct);

        // ── Retrieval ────────────────────────────────────────────────────────────────
        var chapters = await readAlong.GetChaptersAsync(bookId, ct);
        var codexIds = await StoryBibleService.BookEntityIdsAsync(db, bookId, ct);
        var codex = await db.Entities.AsNoTracking().IgnoreQueryFilters().Where(e => codexIds.Contains(e.Id))
            .Select(e => new { e.Id, e.Name }).ToListAsync(ct);
        var drafts = await db.StoryBibleSuggestions.AsNoTracking().IgnoreQueryFilters()
            .Where(s => s.BookNodeId == bookId && s.Kind == "entry" && s.Status == ReadAlongStatus.Pending).Select(s => s.Name).ToListAsync(ct);

        var askedAbout = codex.Where(e => Mentions(question, e.Name)).Select(e => (Id: (Guid?)e.Id, Name: e.Name))
            .Concat(drafts.Where(d => Mentions(question, d)).Select(d => (Id: (Guid?)null, Name: d)))
            .ToList();
        var terms = Terms(question);
        var wantsLatest = Regex.IsMatch(question, @"\b(last|latest|most recent|recently|lately)\b", RegexOptions.IgnoreCase);
        var wantsFirst = Regex.IsMatch(question, @"\b(first|introduc|origin|begin)\w*\b", RegexOptions.IgnoreCase);

        var scored = new List<(double Score, Door Door, string Text)>();
        foreach (var c in chapters)
            foreach (var p in c.Paragraphs)
            {
                double score = 0;
                Guid? entity = null;
                foreach (var (id, name) in askedAbout)
                    if (Mentions(p.Text, name)) { score += 4; entity ??= id; }
                foreach (var t in terms)
                    if (p.Text.Contains(t, StringComparison.OrdinalIgnoreCase)) score += 1;
                if (score <= 0) continue;
                if (wantsLatest) score += c.Index / (double)Math.Max(1, chapters.Count);
                if (wantsFirst) score += 1 - c.Index / (double)Math.Max(1, chapters.Count);
                scored.Add((score, new Door(p.BeatId, c.ChapterNodeId, c.Title, c.Index, Snip(BestSentence(p.Text, askedAbout.Select(a => a.Name), terms), 240), entity), p.Text));
            }
        var top = scored.OrderByDescending(s => s.Score).ThenBy(s => s.Door.ChapterIndex).Take(24).ToList();

        var factLines = new List<string>();
        foreach (var (id, name) in askedAbout.Where(a => a.Id is not null).Take(6))
        {
            var facts = await db.StoryFacts.AsNoTracking().IgnoreQueryFilters()
                .Where(f => f.EntityId == id && f.Confirmed && f.SupersededAt == null).OrderBy(f => f.ChapterIndex).Take(30).ToListAsync(ct);
            foreach (var f in facts)
                factLines.Add($"{name} — {Label(f.Attribute)}: {f.Value}" + (f.ChapterIndex > 0 ? $" (chapter {f.ChapterIndex})" : ""));
        }

        // ── Answer ───────────────────────────────────────────────────────────────────
        string text;
        var usedLlm = false;
        List<Door> doors;
        var llm = services.GetService(typeof(ILlmService)) as ILlmService;
        if (llm is not null && top.Count > 0)
        {
            try
            {
                var prompt = new StringBuilder();
                if (factLines.Count > 0)
                {
                    prompt.AppendLine("STORY BIBLE (details the author has confirmed):");
                    foreach (var l in factLines) prompt.AppendLine("- " + l);
                    prompt.AppendLine();
                }
                prompt.AppendLine("PASSAGES FROM THE MANUSCRIPT:");
                for (var i = 0; i < top.Count; i++)
                    prompt.AppendLine($"[P{i + 1}] {top[i].Door.ChapterTitle} (chapter {top[i].Door.ChapterIndex}): {Snip(top[i].Text, 900)}");
                prompt.AppendLine();
                if (history.Count > 0)
                {
                    prompt.AppendLine("CONVERSATION SO FAR:");
                    foreach (var h in history) prompt.AppendLine($"{h.Role.ToUpperInvariant()}: {Snip(h.Text, 600)}");
                    prompt.AppendLine();
                }
                prompt.Append("AUTHOR'S QUESTION: ").Append(question);

                text = (await llm.GenerateAsync(SystemPrompt, prompt.ToString(), temperature: 0.3, maxTokens: 1200, ct: ct)).Trim();
                usedLlm = true;
                var cited = Regex.Matches(text, @"\[P(\d+)\]").Select(m => int.Parse(m.Groups[1].Value) - 1)
                    .Where(i => i >= 0 && i < top.Count).Distinct().ToList();
                doors = (cited.Count > 0 ? cited.Select(i => top[i].Door) : top.Select(t => t.Door)).Take(maxDoors).ToList();
                text = Regex.Replace(text, @"\s*\[P\d+\]", "").Trim();
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                log.LogWarning(ex, "Ask your book: LLM unavailable, answering from the read-along's records");
                (text, doors) = Fallback(question, askedAbout.Select(a => a.Name).ToList(), factLines, top.Select(t => t.Door).Take(maxDoors).ToList());
            }
        }
        else
        {
            (text, doors) = Fallback(question, askedAbout.Select(a => a.Name).ToList(), factLines, top.Select(t => t.Door).Take(maxDoors).ToList());
        }

        var replyRow = new BookChatMessage
        {
            UniverseId = universeId, BookNodeId = bookId, Role = "assistant", Text = text,
            DoorsJson = JsonSerializer.Serialize(doors, ReadAlongService.Json),
        };
        db.BookChatMessages.Add(replyRow);
        await db.SaveChangesAsync(ct);
        return new Answer(ToMessage(userRow), ToMessage(replyRow), usedLlm, null);
    }

    private const string SystemPrompt =
        "You are a well-read friend talking with an author about THEIR OWN manuscript. Answer only from the " +
        "PASSAGES and STORY BIBLE given; if they do not settle the question, say what they do show and what is " +
        "missing. Cite the passages you rely on inline as [P1], [P2]. You may discuss plot, character, timeline " +
        "and continuity, and ask the author questions back. You NEVER write the book: do not draft, rewrite, " +
        "continue or suggest wording for any sentence, scene or chapter, even if asked — offer to talk it through " +
        "instead. Be concise.";

    private static (string, List<Door>) Fallback(string question, List<string> names, List<string> facts, List<Door> doors)
    {
        var sb = new StringBuilder();
        if (facts.Count > 0)
        {
            sb.AppendLine("From your story bible:");
            foreach (var f in facts.Take(12)) sb.AppendLine("• " + f);
        }
        if (doors.Count > 0)
        {
            if (sb.Length > 0) sb.AppendLine();
            var first = doors.MinBy(d => d.ChapterIndex)!;
            var last = doors.MaxBy(d => d.ChapterIndex)!;
            sb.Append(names.Count > 0 ? $"{string.Join(" and ", names)} come{(names.Count == 1 ? "s" : "")} up" : "That comes up");
            sb.Append(first.ChapterIndex == last.ChapterIndex
                ? $" in {first.ChapterTitle}."
                : $" from {first.ChapterTitle} through {last.ChapterTitle}; the passages that speak to it most are below.");
        }
        if (sb.Length == 0)
            sb.Append("I couldn't find that in what I've read so far. Try naming a character, place or thing as it appears on the page.");
        return (sb.ToString().Trim(), doors);
    }

    private static bool Mentions(string text, string name)
    {
        var n = name.StartsWith("the ", StringComparison.OrdinalIgnoreCase) ? name[4..] : name;
        if (n.Length < 2) return false;
        return Regex.IsMatch(text, $@"(?<![\p{{L}}]){Regex.Escape(n)}(?![\p{{L}}])", RegexOptions.IgnoreCase);
    }

    private static readonly HashSet<string> QuestionStop = new(StringComparer.OrdinalIgnoreCase)
    {
        "what","when","where","which","who","whom","whose","why","how","does","did","do","is","are","was","were","the","and","that","this",
        "have","has","had","know","knows","about","with","from","into","there","their","they","them","then","than","come","came","last",
        "first","time","chapter","story","book","still","ever","does","doesn","really","actually","whether","hold","holds","timeline",
    };

    private static List<string> Terms(string q) =>
        Regex.Matches(q, @"[\p{L}'’-]{4,}").Select(m => m.Value.Trim('\'', '’')).Where(w => !QuestionStop.Contains(w))
            .Distinct(StringComparer.OrdinalIgnoreCase).ToList();

    private static string BestSentence(string paragraph, IEnumerable<string> names, List<string> terms)
    {
        var sentences = SplitSentences(paragraph).ToList();
        if (sentences.Count == 0) return paragraph;
        var nameList = names.ToList();
        return sentences.OrderByDescending(s => nameList.Count(n => Mentions(s, n)) * 4 + terms.Count(t => s.Contains(t, StringComparison.OrdinalIgnoreCase)))
            .First();
    }

    private static string Snip(string s, int max) => s.Length <= max ? s : s[..max].TrimEnd() + "…";
}
