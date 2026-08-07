using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.Logging;
using Prose.Core.Interfaces;
using Prose.Core.Models;

namespace Prose.Core.Services;

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
    private readonly WorldStateService worldState;
    private readonly WorldGraphService graph;
    private readonly IBookRepository books;
    private readonly IChapterRepository chapters;
    private readonly BookOutlineService outline;
    private readonly DatabaseService canon;
    private readonly EmbeddingService? embeddings;
    private readonly ILogger<ConversationalWriterService> log;

    public ConversationalWriterService(
        ILlmService llm,
        WorldStateService worldState, WorldGraphService graph,
        IBookRepository books, IChapterRepository chapters,
        BookOutlineService outline, DatabaseService canon,
        ILogger<ConversationalWriterService> log,
        EmbeddingService? embeddings = null)
    {
        this.llm = llm;
        this.worldState = worldState;
        this.graph = graph;
        this.books = books;
        this.chapters = chapters;
        this.outline = outline;
        this.canon = canon;
        this.embeddings = embeddings;
        this.log = log;
    }

    /// <summary>
    /// Streaming-shaped variant kept for callers that iterate an
    /// <see cref="IAsyncEnumerable{T}"/>; emits exactly one chunk because the
    /// underlying cloud provider call is non-streaming. Caller should append
    /// the yielded string to the in-progress assistant message.
    /// </summary>
    public async IAsyncEnumerable<string> TalkStreamAsync(TalkTurn turn,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var system = BuildSystemPrompt(turn);
        var userPrompt = BuildUserPrompt(turn);
        yield return await GenerateSafelyAsync(system, userPrompt, ct);
    }

    private async Task<string> GenerateSafelyAsync(string system, string userPrompt, CancellationToken ct)
    {
        try { return await llm.GenerateAsync(system, userPrompt, 0.55, 2200, ct: ct); }
        catch (Exception ex) { log.LogWarning(ex, "ConversationalWriterService generation failed"); return $"(Error: {ex.Message})"; }
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
                        sb.AppendLine($"  Ch{ch.Number} \"{ch.Title}\" [POV {ch.PovCharacter}]: {ch.EffectiveBody}");
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

        // Stage 1: substring match on every graph node — catches every
        // explicitly-named entity. Substring-grounding is the floor.
        foreach (var node in graph.AllNodes())
        {
            if (string.IsNullOrWhiteSpace(node.Name)) continue;
            if (node.Name.Length < 3) continue;
            if (hay.IndexOf(node.Name, StringComparison.OrdinalIgnoreCase) >= 0)
                found.Add(node.Id);
        }

        // Stage 2: protagonist-by-default — even if the user's message doesn't
        // name them, the book's protagonists are always relevant.
        if (!string.IsNullOrEmpty(turn.BookId))
        {
            var book = books.LoadBook(turn.BookId);
            if (book != null)
                foreach (var p in book.Protagonists)
                    if (graph.ResolveId(p) is string id) found.Add(id);
        }

        // Stage 3 (audit Priority-1): embedding-augmented thematic discovery.
        // If the user is asking "how would Kyle handle a betrayal?" without
        // naming Hua, embedding similarity surfaces Hua because the prose
        // around Kyle's betrayals semantically resembles her dossier. Top-3
        // is a deliberate floor — we don't want to flood dossier context
        // with weak matches; substring + protagonist already cover the
        // strong signals.
        if (embeddings != null && !string.IsNullOrWhiteSpace(hay))
        {
            try
            {
                var hits = embeddings.FindSimilarAsync(hay, k: 3).GetAwaiter().GetResult();
                foreach (var h in hits)
                {
                    var id = WorldGraphService.Slugify(h.EntityName);
                    if (!string.IsNullOrEmpty(id)) found.Add(id);
                }
            }
            catch
            {
                // Embedding cache cold or API down — substring + protagonist
                // path already gave us a usable mention list. Don't fail the
                // whole turn for a discovery miss.
            }
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
