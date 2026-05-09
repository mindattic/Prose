using System.Text;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Models;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Always-on collaborative dialog. The user can ask the system to alter any part
/// of the book — outline, chapter prose, canon entities — and the LLM responds with
/// either a direct change proposal or, when the request conflicts with established
/// canon, a challenge: "this contradicts X. Want to override (and propagate the
/// override across the book) or revise the request?"
///
/// v1 surfaces proposed changes as text + structured action hints; the user
/// confirms/dismisses each. Direct tool execution will land in a later pass on
/// top of the existing <see cref="Operator.WriterOperatorService"/> infrastructure.
/// </summary>
public class CoWriterService
{
    private readonly ILlmService llm;
    private readonly IBookRepository books;
    private readonly IChapterRepository chapters;
    private readonly DatabaseService db;
    private readonly BookOutlineService outlineSvc;
    private readonly LoreService lore;
    private readonly ILogger<CoWriterService> log;

    public CoWriterService(
        ILlmService llm, IBookRepository books, IChapterRepository chapters,
        DatabaseService db, BookOutlineService outlineSvc, LoreService lore,
        ILogger<CoWriterService> log)
    {
        this.llm = llm;
        this.books = books;
        this.chapters = chapters;
        this.db = db;
        this.outlineSvc = outlineSvc;
        this.lore = lore;
        this.log = log;
    }

    public async Task<CoWriterResponse> AskAsync(string bookId, string userMessage,
        List<CoWriterMessage>? history = null, CancellationToken ct = default)
    {
        var systemPrompt = BuildSystemPrompt(bookId);
        var fullPrompt = BuildHistoryPrompt(history) + $"\nUSER: {userMessage}\nASSISTANT:";

        try
        {
            var response = await llm.GenerateAsync(systemPrompt, fullPrompt, temperature: 0.6, maxTokens: 2048, ct: ct);
            return new CoWriterResponse
            {
                AssistantMessage = response,
                ParsedActions = ParseActionHints(response),
            };
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "CoWriter call failed");
            return new CoWriterResponse { AssistantMessage = $"(Error: {ex.Message})", IsError = true };
        }
    }

    private string BuildSystemPrompt(string bookId)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are a collaborative co-writer for a book project. Your role:");
        sb.AppendLine();
        sb.AppendLine("1. Help the user shape the book's outline, chapters, and canon.");
        sb.AppendLine("2. CHALLENGE conflicts. If the user proposes something that contradicts established canon");
        sb.AppendLine("   (a character's documented psychology, a place's rules, world-building constants), point");
        sb.AppendLine("   it out — don't silently accept. Phrase it: \"That conflicts with X. You can override (and");
        sb.AppendLine("   I'll help propagate the change across the book) or we can find another way.\" Then wait.");
        sb.AppendLine("3. When the user confirms an override, treat the new fact as canon and trace what else");
        sb.AppendLine("   needs to change in the outline / chapter prose / character files to align.");
        sb.AppendLine("4. When the user accepts a non-conflict suggestion, propose a concrete change in the form:");
        sb.AppendLine("   [ACTION:KIND] description [/ACTION] where KIND is one of: outline-edit, chapter-prose-edit,");
        sb.AppendLine("   canon-edit, motif-add, thread-add. The user will confirm before anything is written.");
        sb.AppendLine("5. Be brief and direct. This is a working dialog, not an essay.");
        sb.AppendLine();

        var book = books.LoadBook(bookId);
        if (book != null)
        {
            sb.AppendLine($"BOOK: {book.Title}");
            if (!string.IsNullOrEmpty(book.Tagline)) sb.AppendLine($"TAGLINE: {book.Tagline}");
            if (!string.IsNullOrEmpty(book.Premise)) sb.AppendLine($"PREMISE: {book.Premise}");
            if (!string.IsNullOrEmpty(book.ArcTarget)) sb.AppendLine($"ARC TARGET: {book.ArcTarget}");
            if (book.Protagonists.Any()) sb.AppendLine($"PROTAGONISTS: {string.Join(", ", book.Protagonists)}");
            sb.AppendLine();

            var outline = outlineSvc.Load(bookId);
            if (outline.Chapters.Any())
            {
                sb.AppendLine("CHAPTER OUTLINES (current state — your changes propose edits to this):");
                foreach (var ch in outline.Chapters)
                {
                    sb.AppendLine($"  Ch {ch.Number} \"{ch.Title}\" [POV: {ch.PovCharacter}]: {ch.EffectiveBody}");
                }
                sb.AppendLine();
            }

            // Inject protagonist character canon — most likely conflict source.
            foreach (var name in book.Protagonists.Take(3))
            {
                var c = db.FindCharacter(name);
                if (c == null) continue;
                sb.AppendLine($"CANON — {name}:");
                if (!string.IsNullOrEmpty(c.Role)) sb.AppendLine($"  role: {c.Role}");
                if (c.Psychology?.CoreFears?.Count > 0) sb.AppendLine($"  core fears: {string.Join(" | ", c.Psychology.CoreFears.Take(2))}");
                if (c.Psychology?.CoreDesires?.Count > 0) sb.AppendLine($"  core desires: {string.Join(" | ", c.Psychology.CoreDesires.Take(2))}");
                if (!string.IsNullOrEmpty(c.SpeechPatterns?.Cadence)) sb.AppendLine($"  cadence: {c.SpeechPatterns.Cadence}");
                sb.AppendLine();
            }
        }
        return sb.ToString();
    }

    private string BuildHistoryPrompt(List<CoWriterMessage>? history)
    {
        if (history == null || history.Count == 0) return "";
        var sb = new StringBuilder();
        foreach (var m in history.TakeLast(10))  // bound history to last 10 turns
        {
            sb.AppendLine($"{(m.Role == "user" ? "USER" : "ASSISTANT")}: {m.Content}");
        }
        return sb.ToString();
    }

    private static List<CoWriterAction> ParseActionHints(string text)
    {
        // Match [ACTION:kind] body [/ACTION] blocks. Keep loose — models phrase variably.
        var actions = new List<CoWriterAction>();
        var rx = new System.Text.RegularExpressions.Regex(
            @"\[ACTION:(?<kind>[a-z\-]+)\](?<body>.*?)\[/ACTION\]",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);
        foreach (System.Text.RegularExpressions.Match m in rx.Matches(text))
        {
            actions.Add(new CoWriterAction
            {
                Kind = m.Groups["kind"].Value.ToLowerInvariant(),
                Description = m.Groups["body"].Value.Trim(),
            });
        }
        return actions;
    }
}

public class CoWriterMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = "user";  // "user" | "assistant"

    [JsonPropertyName("content")]
    public string Content { get; set; } = "";

    [JsonPropertyName("at")]
    public DateTime At { get; set; } = DateTime.UtcNow;
}

public class CoWriterResponse
{
    public string AssistantMessage { get; set; } = "";
    public List<CoWriterAction> ParsedActions { get; set; } = [];
    public bool IsError { get; set; }
}

public class CoWriterAction
{
    public string Kind { get; set; } = "";
    public string Description { get; set; } = "";
}
