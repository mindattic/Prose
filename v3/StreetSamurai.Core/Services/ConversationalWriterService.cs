using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Models;

namespace StreetSamurai.Core.Services;

/// <summary>
/// The single brain behind the simplified writer UI: "You, Me, and the Page."
/// Wraps the LLM with full world-state awareness — for every conversational turn
/// it pulls dossiers for any entity the user mentions, the current chapter prose,
/// the book outline, and the running conversation history. Returns either a
/// concrete prose draft, an interesting path-forward suggestion, or a clarifying
/// question — never silently writes anything to disk.
/// </summary>
public class ConversationalWriterService
{
    private readonly ILlmService llm;
    private readonly OllamaClient ollama;
    private readonly WorldStateService worldState;
    private readonly WorldGraphService graph;
    private readonly IBookRepository books;
    private readonly IChapterRepository chapters;
    private readonly BookOutlineService outline;
    private readonly DatabaseService canon;
    private readonly ILogger<ConversationalWriterService> log;

    public ConversationalWriterService(
        ILlmService llm, OllamaClient ollama,
        WorldStateService worldState, WorldGraphService graph,
        IBookRepository books, IChapterRepository chapters,
        BookOutlineService outline, DatabaseService canon,
        ILogger<ConversationalWriterService> log)
    {
        this.llm = llm;
        this.ollama = ollama;
        this.worldState = worldState;
        this.graph = graph;
        this.books = books;
        this.chapters = chapters;
        this.outline = outline;
        this.canon = canon;
        this.log = log;
    }

    /// <summary>
    /// Streaming variant that pipes the LLM reply token-by-token. Uses Ollama directly
    /// when reachable for snappy first-token latency; otherwise falls back to a single
    /// blocking <see cref="ILlmService.GenerateAsync"/> call. Caller should append yielded
    /// strings to the in-progress assistant message.
    /// </summary>
    public async IAsyncEnumerable<string> TalkStreamAsync(TalkTurn turn,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var system = BuildSystemPrompt(turn);
        var userPrompt = BuildUserPrompt(turn);

        bool ollamaUp = false;
        try { ollamaUp = await ollama.IsReachableAsync(ct); } catch { ollamaUp = false; }

        if (ollamaUp)
        {
            var messages = new List<(string Role, string Content)>
            {
                ("system", system),
                ("user",   userPrompt),
            };
            await foreach (var piece in ollama.StreamChatAsync(messages, ct))
                yield return piece;
            yield break;
        }

        // Fallback: non-streaming through ILlmService.
        var reply = await GenerateNonStreamingSafelyAsync(system, userPrompt, ct);
        yield return reply;
    }

    private async Task<string> GenerateNonStreamingSafelyAsync(string system, string userPrompt, CancellationToken ct)
    {
        try { return await llm.GenerateAsync(system, userPrompt, 0.55, 2200, ct: ct); }
        catch (Exception ex) { log.LogWarning(ex, "ConversationalWriterService stream fallback failed"); return $"(Error: {ex.Message})"; }
    }

    /// <summary>
    /// Resolve mentions for a turn — exposed so the UI can render the "in dossier
    /// scope" badge without running a full talk turn.
    /// </summary>
    public IReadOnlyList<string> ResolveMentionsForUi(TalkTurn turn) => MentionsResolved(turn);

    /// <summary>
    /// One conversational turn. The LLM sees: literary rules, book context, chapter
    /// content, dossiers for any entity the user named, and the conversation history.
    /// Reply is plain text — the UI decides how to present prose vs. questions.
    /// </summary>
    public async Task<TalkResponse> TalkAsync(TalkTurn turn, CancellationToken ct = default)
    {
        var system = BuildSystemPrompt(turn);
        var userPrompt = BuildUserPrompt(turn);

        try
        {
            var reply = await llm.GenerateAsync(system, userPrompt,
                temperature: 0.55, maxTokens: 2200, ct: ct);
            return new TalkResponse(reply, MentionsResolved(turn));
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "ConversationalWriterService turn failed");
            return new TalkResponse($"(Error: {ex.Message})", new List<string>());
        }
    }

    private string BuildSystemPrompt(TalkTurn turn)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are the writer's collaborator on a single book. Three rules:");
        sb.AppendLine("1. PROSE: when the writer asks for prose, deliver tight, in-voice prose — no scaffolding, no labels.");
        sb.AppendLine("2. SUGGEST: when the writer is exploring, offer 2–3 specific paths forward grounded in established canon. Each path: one sentence.");
        sb.AppendLine("3. CLARIFY: when the writer's intent is ambiguous, ask one focused question. One.");
        sb.AppendLine("Never silently invent character facts that contradict the dossiers below. If a fact is missing, ask.");
        sb.AppendLine("Cite the dossier or chapter line you relied on when it sharpens your reply.");
        sb.AppendLine();

        var rules = canon.GetLiteraryRulesPrompt();
        if (!string.IsNullOrWhiteSpace(rules))
        {
            sb.AppendLine("LITERARY RULES (non-negotiable):");
            sb.AppendLine(rules);
            sb.AppendLine();
        }

        if (!string.IsNullOrEmpty(turn.BookId))
        {
            var book = books.LoadBook(turn.BookId);
            if (book != null)
            {
                sb.AppendLine($"BOOK: {book.Title}");
                if (!string.IsNullOrWhiteSpace(book.Tagline)) sb.AppendLine($"  tagline: {book.Tagline}");
                if (!string.IsNullOrWhiteSpace(book.Premise)) sb.AppendLine($"  premise: {book.Premise}");
                if (book.Protagonists.Any())                  sb.AppendLine($"  protagonists: {string.Join(", ", book.Protagonists)}");
                sb.AppendLine();
            }
            try
            {
                var ol = outline.Load(turn.BookId);
                if (ol.Chapters.Any())
                {
                    sb.AppendLine("OUTLINE:");
                    foreach (var ch in ol.Chapters.OrderBy(c => c.Number))
                        sb.AppendLine($"  Ch{ch.Number} \"{ch.Title}\" [POV {ch.PovCharacter}]: {ch.LongSynopsis}");
                    sb.AppendLine();
                }
            }
            catch { /* outline missing is fine */ }
        }

        // Inject dossiers for entities the user named (and protagonists by default).
        var asOf = AsOfChapter(turn);
        var dossierTargets = ResolveMentions(turn).ToList();
        if (dossierTargets.Count > 0)
        {
            sb.AppendLine("DOSSIERS (world-state-at-now for entities mentioned):");
            foreach (var name in dossierTargets.Take(5))
            {
                var d = worldState.GetDossier(name, asOf);
                if (d != null) sb.AppendLine(d.ToPromptString()).AppendLine();
            }
        }

        return sb.ToString();
    }

    private static string BuildUserPrompt(TalkTurn turn)
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(turn.ChapterTitle) || !string.IsNullOrWhiteSpace(turn.ChapterPlain))
        {
            sb.AppendLine("THE PAGE — current chapter draft:");
            if (!string.IsNullOrWhiteSpace(turn.ChapterTitle)) sb.AppendLine($"Title: {turn.ChapterTitle}");
            if (!string.IsNullOrWhiteSpace(turn.ChapterPlain))
                sb.AppendLine(Truncate(turn.ChapterPlain, 6000));
            sb.AppendLine();
        }

        if (turn.History.Count > 0)
        {
            sb.AppendLine("CONVERSATION SO FAR:");
            foreach (var m in turn.History.TakeLast(20))
                sb.AppendLine($"{m.Role.ToUpperInvariant()}: {m.Content}");
            sb.AppendLine();
        }

        sb.AppendLine($"USER: {turn.UserMessage}");
        sb.AppendLine("ASSISTANT:");
        return sb.ToString();
    }

    private IEnumerable<string> ResolveMentions(TalkTurn turn)
    {
        var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var hay = $"{turn.UserMessage} {turn.ChapterPlain}";

        foreach (var node in graph.AllNodes())
        {
            if (string.IsNullOrWhiteSpace(node.Name)) continue;
            if (node.Name.Length < 3) continue;
            if (hay.IndexOf(node.Name, StringComparison.OrdinalIgnoreCase) >= 0)
                found.Add(node.Id);
        }

        if (!string.IsNullOrEmpty(turn.BookId))
        {
            var book = books.LoadBook(turn.BookId);
            if (book != null)
                foreach (var p in book.Protagonists)
                    if (graph.ResolveId(p) is string id) found.Add(id);
        }

        return found;
    }

    private List<string> MentionsResolved(TalkTurn turn)
        => ResolveMentions(turn)
            .Select(id => graph.GetNode(id)?.Name ?? id)
            .ToList();

    private static AsOfCursor AsOfChapter(TalkTurn turn)
        => turn.ChapterNumber.HasValue ? new AsOfCursor(turn.ChapterId, turn.ChapterNumber, null) : AsOfCursor.Current;

    private static string Truncate(string s, int n) => s.Length <= n ? s : s[..(n - 1)] + "…";
}

public sealed record TalkTurn(
    string? BookId,
    string? ChapterId,
    int? ChapterNumber,
    string? ChapterTitle,
    string? ChapterPlain,
    string UserMessage,
    IReadOnlyList<TalkMessage> History);

public sealed record TalkMessage(string Role, string Content);

public sealed record TalkResponse(string Reply, IReadOnlyList<string> ResolvedEntities);
