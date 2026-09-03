using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;
using Prose.Core.Interfaces;

namespace Prose.Core.Services;

public record OutlineSyncFact(string Fact, int BeatNumber, string BeatTitle, string Category);

public record OutlineSyncReport(
    Guid SessionId,
    string SessionLabel,
    string NodeCode,
    List<OutlineSyncFact> Facts,
    bool WroteToFile,
    string? FilePath);

public class OutlineSyncService
{
    private readonly IDbContextFactory<ProseDbContext> dbFactory;
    private readonly ILlmService llm;
    private readonly IPathProvider paths;
    private readonly MarkdownFileService markdown;
    private readonly ILogger<OutlineSyncService> log;

    public OutlineSyncService(
        IDbContextFactory<ProseDbContext> dbFactory,
        ILlmService llm,
        IPathProvider paths,
        MarkdownFileService markdown,
        ILogger<OutlineSyncService> log)
    {
        this.dbFactory = dbFactory;
        this.llm = llm;
        this.paths = paths;
        this.markdown = markdown;
        this.log = log;
    }

    public async Task<OutlineSyncReport> ExtractFromSessionAsync(
        Guid sessionId, bool dryRun = false, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var session = await db.EditSessions
            .FirstOrDefaultAsync(s => s.EditSessionId == sessionId, ct)
            ?? throw new InvalidOperationException($"Session {sessionId} not found.");

        // IgnoreQueryFilters(): session.NodeId is an explicit id the caller already holds, not
        // an ambient-scope lookup — same rationale as CloseAllSessionsCli's own resolution of
        // this exact id. Without it, every session on a node outside the Hub's default
        // current_universe threw "Node not found" here, and since --close-all-sessions is
        // universe-agnostic by design (Program.cs), that meant /commit's Beat<->Bible<->Blueprint
        // sync step failed silently for those books on every single commit (found live
        // 2026-09-03: 31 of 31 open sessions failing this way).
        var node = await db.Nodes.IgnoreQueryFilters()
            .FirstOrDefaultAsync(n => n.Id == session.NodeId, ct)
            ?? throw new InvalidOperationException($"Node {session.NodeId} not found.");

        var sessionBeats = await db.EditSessionBeats
            .Include(esb => esb.Beat)
            .Where(esb => esb.EditSessionId == sessionId)
            .OrderBy(esb => esb.EditedAt)
            .ToListAsync(ct);

        if (sessionBeats.Count == 0)
            return new OutlineSyncReport(sessionId, session.Label, node.NodeCode ?? node.Slug,
                new(), false, null);

        // Short-circuit BEFORE the LLM call when there is nothing this method can legally write
        // to — running fact extraction only to discard the result wastes tokens on every commit.
        //
        // Two distinct reasons the destination can be unusable:
        //   1. The file is absent. Was the only case that mattered while sessions were keyed to
        //      CHAPTER nodes (no NodeCode ⇒ no docs/nodes/<CODE>.md). Sessions key to the owning
        //      BOOK node as of 2026-09-03 (EditSessionService.ResolveBookNodeIdAsync), so real
        //      book codes now resolve and this case is the uncommon one.
        //   2. The file exists but is READ-ONLY — i.e. it is a generated artifact. Every
        //      docs/nodes/<CODE>.md is written by GeneratedFileWriter.WriteReadOnlyAsync and
        //      regenerated from Nodes.NodeOutline (SS-A45); appending here would throw
        //      UnauthorizedAccessException, and clearing the attribute to force it would only
        //      buy content that the next generate_node_doc silently destroys. Detected here
        //      rather than at the append below so the LLM call is never paid for.
        //      NOTE: this is why the fact-extraction half of the Beat<->Bible<->Blueprint sync is
        //      still dormant. Its write target is architecturally stale — the source of truth is
        //      Nodes.NodeOutline, not the mirror — and choosing where machine-extracted facts
        //      belong in a hand-authored outline is an authorial decision, not a bug fix.
        var earlyNodeCode = node.NodeCode?.ToUpperInvariant() ?? node.Slug.ToUpperInvariant();
        var earlyBibleFile = Path.Combine(paths.DataRoot, "docs", "nodes", $"{earlyNodeCode}.md");
        if (!dryRun && !IsAppendable(earlyBibleFile, out var whyNot))
        {
            log.LogInformation("OutlineSync: {Path} {Reason} — skipping extraction for session {SessionId}",
                earlyBibleFile, whyNot, sessionId);
            return new OutlineSyncReport(sessionId, session.Label, earlyNodeCode, new(), false, earlyBibleFile);
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

        List<OutlineSyncFact> facts = new();
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
                        facts.Add(new OutlineSyncFact(fact, beatNum, beatTitle, category));
                }
            }
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "OutlineSyncService LLM parse failed for session {SessionId}", sessionId);
        }

        var nodeCode = node.NodeCode?.ToUpperInvariant() ?? node.Slug.ToUpperInvariant();

        if (dryRun || facts.Count == 0)
            return new OutlineSyncReport(sessionId, session.Label, nodeCode, facts, false, null);

        // Append section to docs/nodes/<CODE>.md. Unreachable for a non-dryRun call (the
        // pre-LLM short-circuit above already rejected an unusable destination), but kept as
        // the same guard so the dryRun path and any future caller can't fall through into an
        // UnauthorizedAccessException.
        var bibleFile = Path.Combine(paths.DataRoot, "docs", "nodes", $"{nodeCode}.md");
        if (!IsAppendable(bibleFile, out var appendWhyNot))
        {
            log.LogWarning("Bible file at {Path} {Reason} — cannot append session extracts", bibleFile, appendWhyNot);
            return new OutlineSyncReport(sessionId, session.Label, nodeCode, facts, false, bibleFile);
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

        return new OutlineSyncReport(sessionId, session.Label, nodeCode, facts, true, bibleFile);
    }

    /// <summary>
    /// True when <paramref name="path"/> is a file this service may append to. False (with a
    /// human-readable <paramref name="reason"/> for the log line) when it is missing, or when it
    /// carries <see cref="FileAttributes.ReadOnly"/> — the marker every generated artifact in
    /// this project is written with.
    /// </summary>
    private static bool IsAppendable(string path, out string reason)
    {
        if (!File.Exists(path)) { reason = "does not exist"; return false; }
        try
        {
            if (File.GetAttributes(path).HasFlag(FileAttributes.ReadOnly))
            {
                reason = "is a read-only generated artifact (source of truth is Nodes.NodeOutline)";
                return false;
            }
        }
        catch (Exception)
        {
            // Unreadable attributes ⇒ treat as not appendable rather than discovering it
            // after paying for the extraction call.
            reason = "attributes could not be read";
            return false;
        }
        reason = "";
        return true;
    }
}
