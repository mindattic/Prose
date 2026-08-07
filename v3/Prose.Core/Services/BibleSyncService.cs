using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;
using Prose.Core.Interfaces;

namespace Prose.Core.Services;

public record BibleSyncFact(string Fact, int BeatNumber, string BeatTitle, string Category);

public record BibleSyncReport(
    Guid SessionId,
    string SessionLabel,
    string NodeCode,
    List<BibleSyncFact> Facts,
    bool WroteToFile,
    string? FilePath);

public class BibleSyncService
{
    private readonly IDbContextFactory<ProseDbContext> dbFactory;
    private readonly ILlmService llm;
    private readonly IPathProvider paths;
    private readonly MarkdownFileService markdown;
    private readonly ILogger<BibleSyncService> log;

    public BibleSyncService(
        IDbContextFactory<ProseDbContext> dbFactory,
        ILlmService llm,
        IPathProvider paths,
        MarkdownFileService markdown,
        ILogger<BibleSyncService> log)
    {
        this.dbFactory = dbFactory;
        this.llm = llm;
        this.paths = paths;
        this.markdown = markdown;
        this.log = log;
    }

    public async Task<BibleSyncReport> ExtractFromSessionAsync(
        Guid sessionId, bool dryRun = false, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var session = await db.EditSessions
            .FirstOrDefaultAsync(s => s.EditSessionId == sessionId, ct)
            ?? throw new InvalidOperationException($"Session {sessionId} not found.");

        var node = await db.Nodes
            .FirstOrDefaultAsync(n => n.Id == session.NodeId, ct)
            ?? throw new InvalidOperationException($"Node {session.NodeId} not found.");

        var sessionBeats = await db.EditSessionBeats
            .Include(esb => esb.Beat)
            .Where(esb => esb.EditSessionId == sessionId)
            .OrderBy(esb => esb.EditedAt)
            .ToListAsync(ct);

        if (sessionBeats.Count == 0)
            return new BibleSyncReport(sessionId, session.Label, node.NodeCode ?? node.Slug,
                new(), false, null);

        // Short-circuit BEFORE the LLM call when there is no bible file to append to
        // (book-scale nodes attach beats to chapter children that have no docs/nodes/<CODE>.md;
        // running fact extraction only to discard it wastes tokens on every commit).
        var earlyNodeCode = node.NodeCode?.ToUpperInvariant() ?? node.Slug.ToUpperInvariant();
        var earlyBibleFile = Path.Combine(paths.DataRoot, "docs", "nodes", $"{earlyNodeCode}.md");
        if (!dryRun && !File.Exists(earlyBibleFile))
        {
            log.LogInformation("BibleSync: no bible file at {Path} — skipping extraction for session {SessionId}",
                earlyBibleFile, sessionId);
            return new BibleSyncReport(sessionId, session.Label, earlyNodeCode, new(), false, earlyBibleFile);
        }

        // Build the prose corpus for LLM extraction
        var corpus = new StringBuilder();
        foreach (var esb in sessionBeats)
        {
            if (esb.Beat == null || string.IsNullOrWhiteSpace(esb.Beat.Text)) continue;
            corpus.AppendLine($"[Beat {esb.Beat.Number} — \"{esb.Beat.Title ?? "(untitled)"}\"]");
            corpus.AppendLine(esb.Beat.Text);
            corpus.AppendLine();
        }

        var system = """
You are a narrative analyst extracting canon facts from prose beats.
For each beat, identify concrete facts that should be permanently recorded in the book bible:
- Character details established (appearance, voice quirks, specific behavior, relationship dynamics)
- World or setting details described in specific terms
- Plot events that are now canon
- Rules or constraints demonstrated by the prose
- Character decisions that reveal who they are

Do NOT include obvious genre conventions or vague observations.
Only record specific, quotable facts that a future writer would need to know.

Output STRICT JSON — no markdown fences, no commentary:
{
  "facts": [
    { "fact": "concise statement of what was established", "beatNumber": N, "beatTitle": "...", "category": "character|world|plot|rule" }
  ]
}
""";

        var user = $"Book: {node.Title ?? node.Slug}\nSession: {session.Label}\n\nBeats:\n\n{corpus}";

        List<BibleSyncFact> facts = new();
        try
        {
            var raw = await llm.GenerateAsync(system, user, temperature: 0.3,
                maxTokens: 3000, ct: ct);
            using var doc = JsonDocument.Parse(raw.Trim());
            if (doc.RootElement.TryGetProperty("facts", out var arr))
            {
                foreach (var el in arr.EnumerateArray())
                {
                    var fact     = el.GetProperty("fact").GetString() ?? "";
                    var beatNum  = el.TryGetProperty("beatNumber", out var bn) ? bn.GetInt32() : 0;
                    var beatTitle = el.TryGetProperty("beatTitle", out var bt) ? bt.GetString() ?? "" : "";
                    var category = el.TryGetProperty("category", out var cat) ? cat.GetString() ?? "other" : "other";
                    if (!string.IsNullOrWhiteSpace(fact))
                        facts.Add(new BibleSyncFact(fact, beatNum, beatTitle, category));
                }
            }
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "BibleSyncService LLM parse failed for session {SessionId}", sessionId);
        }

        var nodeCode = node.NodeCode?.ToUpperInvariant() ?? node.Slug.ToUpperInvariant();

        if (dryRun || facts.Count == 0)
            return new BibleSyncReport(sessionId, session.Label, nodeCode, facts, false, null);

        // Append section to docs/nodes/<CODE>.md
        var bibleFile = Path.Combine(paths.DataRoot, "docs", "nodes", $"{nodeCode}.md");
        if (!File.Exists(bibleFile))
        {
            log.LogWarning("Bible file not found at {Path} — cannot append session extracts", bibleFile);
            return new BibleSyncReport(sessionId, session.Label, nodeCode, facts, false, bibleFile);
        }

        var span = session.ClosedAt.HasValue
            ? $"{session.StartedAt:yyyy-MM-dd HH:mm}–{session.ClosedAt.Value:HH:mm} UTC"
            : $"{session.StartedAt:yyyy-MM-dd HH:mm} UTC (open)";

        var section = new StringBuilder();
        section.AppendLine();
        section.AppendLine($"## Session Extracts — {session.Label}");
        section.AppendLine($"*{span} · {sessionBeats.Count} beats edited*");
        section.AppendLine();

        var byCategory = facts.GroupBy(f => f.Category);
        foreach (var grp in byCategory)
        {
            section.AppendLine($"### {char.ToUpperInvariant(grp.Key[0]) + grp.Key[1..]}");
            foreach (var f in grp)
            {
                var beatRef = f.BeatNumber > 0 ? $" *(Beat {f.BeatNumber})*" : "";
                section.AppendLine($"- {f.Fact}{beatRef}");
            }
            section.AppendLine();
        }

        await File.AppendAllTextAsync(bibleFile, section.ToString(), ct);

        // Re-sync to MarkdownFiles DB so DocContextService sees it immediately
        await markdown.SyncAllAsync(ct: ct);

        return new BibleSyncReport(sessionId, session.Label, nodeCode, facts, true, bibleFile);
    }
}
