using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;
using Prose.Core.Interfaces;

namespace Prose.Core.Services;

// ── RegisterExemplarService ───────────────────────────────────────────────
//
// Closes the register feedback loop: after a review pass, surfaces the
// top-N beats by EmotionalScore, identifies which register law each beat
// best exemplifies, and formats them as exemplar-section markdown for
// docs/registers/<NAME>.md.
//
// Usage (via CLI):
//   prose --update-register-exemplars --slug <node-slug> [--top N] [--dry-run]
// ─────────────────────────────────────────────────────────────────────────

public class RegisterExemplarService
{
    public record ExemplarCandidate(
        int     BeatNumber,
        double  EmotionalScore,
        string  LawName,
        string  KeyQuote,
        string  Reason,
        string  BeatPreview);

    private readonly ILlmService llm;
    private readonly IDbContextFactory<ProseDbContext> dbFactory;
    private readonly IPathProvider paths;
    private readonly ILogger<RegisterExemplarService> log;

    public RegisterExemplarService(
        ILlmService llm,
        IDbContextFactory<ProseDbContext> dbFactory,
        IPathProvider paths,
        ILogger<RegisterExemplarService> log)
    {
        this.llm       = llm;
        this.dbFactory = dbFactory;
        this.paths     = paths;
        this.log       = log;
    }

    // ── Public entry point ────────────────────────────────────────────────

    public async Task<(string RegisterName, string Slug, List<ExemplarCandidate> Candidates)>
        FindExemplarsAsync(Guid nodeId, int topN = 5, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var node = await db.Nodes.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == nodeId, ct)
            ?? throw new InvalidOperationException($"Node {nodeId} not found.");

        var registerName = DetectRegister(node.NodeBible);
        if (string.IsNullOrEmpty(registerName))
        {
            log.LogWarning("Node {Slug} has no detected register — cannot surface exemplars.", node.Slug);
            return (registerName, node.Slug ?? "", []);
        }

        var registerPrompt = LoadRegisterPrompt(registerName);
        if (string.IsNullOrWhiteSpace(registerPrompt))
        {
            log.LogWarning("Register file for '{Name}' not found or empty.", registerName);
            return (registerName, node.Slug ?? "", []);
        }

        // SS-A43: for book-mode nodes, beats live on chapter children.
        var childIds = await db.Nodes.AsNoTracking()
            .Where(n => n.ParentNodeId == nodeId)
            .Select(n => n.Id).ToListAsync(ct);
        var searchIds = childIds.Count > 0 ? childIds : new List<Guid> { nodeId };

        // Top-N beats by EmotionalScore (beats with NULL score are excluded)
        var topBeats = await (
            from sb in db.BeatNodes.AsNoTracking()
            join b  in db.Beats.AsNoTracking() on sb.BeatId equals b.Id
            where searchIds.Contains(sb.NodeId)
               && sb.IsEnabled
               && b.EmotionalScore != null
               && b.Text != null && b.Text.Length > 0
            orderby b.EmotionalScore descending
            select new { b.Number, b.EmotionalScore, b.Text }
        ).Take(topN).ToListAsync(ct);

        if (topBeats.Count == 0)
        {
            Console.WriteLine("  No beats with EmotionalScore found. Run --examine-emotion first.");
            return (registerName, node.Slug ?? "", []);
        }

        var candidates = new List<ExemplarCandidate>();
        var tasks = topBeats.Select(b =>
            ClassifyBeatAsync(b.Number, b.EmotionalScore!.Value, b.Text!, registerName, registerPrompt, ct));

        var results = await Task.WhenAll(tasks);
        candidates.AddRange(results.Where(r => r is not null)!);

        return (registerName, node.Slug ?? "", candidates);
    }

    // ── Classify a single beat against the register laws ─────────────────

    private async Task<ExemplarCandidate?> ClassifyBeatAsync(
        int beatNumber, double score, string beatText,
        string registerName, string registerPrompt, CancellationToken ct)
    {
        const string system =
            "You are an expert story editor who understands register-specific craft laws. " +
            "Return ONLY the JSON object requested. No prose, no markdown fences.";

        var preview = beatText.Length > 120 ? beatText[..120].TrimEnd() + "…" : beatText;

        var prompt = $$"""
REGISTER: {{registerName}}

REGISTER PROMPT (source of the numbered laws):
{{registerPrompt}}

BEAT {{beatNumber}} TEXT:
{{Truncate(beatText, 2000)}}

Identify the ONE numbered law in the register prompt that this beat best exemplifies.
Return a JSON object with these exact keys:
{
  "law_name": "<verbatim bold title from the register prompt, e.g. 'OBJECTS CARRY WHAT CANNOT BE FILED'>",
  "key_quote": "<direct quote from the beat — 40–120 chars — that best demonstrates the law>",
  "reason": "<one sentence: what the beat does that makes this law score 4/4>"
}
""";

        try
        {
            var raw  = await llm.GenerateAsync(system, prompt, 0.1, 300, null, ct);
            var json = ExtractJson(raw);
            using var doc  = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var lawName  = root.TryGetProperty("law_name",  out var lp) ? lp.GetString() ?? "" : "";
            var keyQuote = root.TryGetProperty("key_quote", out var kp) ? kp.GetString() ?? "" : "";
            var reason   = root.TryGetProperty("reason",    out var rp) ? rp.GetString() ?? "" : "";

            return new ExemplarCandidate(beatNumber, score, lawName, keyQuote, reason, preview);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Failed to classify beat {Beat}", beatNumber);
            return null;
        }
    }

    // ── Format as markdown exemplar entries ──────────────────────────────

    public string FormatAsMarkdown(
        IEnumerable<ExemplarCandidate> candidates, string nodeSlug, string registerName)
    {
        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine($"<!-- CANDIDATES — added by --update-register-exemplars from {nodeSlug} -->");
        sb.AppendLine($"<!-- Promote to confirmed by removing this comment block. -->");
        sb.AppendLine();

        int i = 1;
        foreach (var c in candidates)
        {
            sb.AppendLine($"{i}. **Beat {c.BeatNumber}** " +
                          $"(EmotionalScore {c.EmotionalScore:F1}) — " +
                          $"*{c.LawName}*");
            sb.AppendLine($"   *\"{c.KeyQuote}\"* — {c.Reason}");
            sb.AppendLine();
            i++;
        }
        return sb.ToString();
    }

    // ── Append candidates to the register file ────────────────────────────

    public string GetRegisterFilePath(string registerName)
    {
        var registersDir = Path.Combine(paths.DataRoot, "docs", "registers");
        return Path.Combine(registersDir, $"{registerName}.md");
    }

    public void AppendToRegisterFile(string registerName, string markdown)
    {
        var path = GetRegisterFilePath(registerName);
        if (!File.Exists(path))
        {
            Console.WriteLine($"  Register file not found: {path}");
            return;
        }

        var existing = File.ReadAllText(path, Encoding.UTF8);

        // Insert before the closing of the Exemplar canon section, or append at end
        const string anchorPhrase = "**Confirmed exemplars:**";
        var insertIdx = existing.LastIndexOf(anchorPhrase, StringComparison.Ordinal);
        if (insertIdx >= 0)
        {
            // Find end of confirmed list — look for next H2 or end of file
            var nextH2 = existing.IndexOf("\n## ", insertIdx + anchorPhrase.Length, StringComparison.Ordinal);
            var insertAt = nextH2 >= 0 ? nextH2 : existing.Length;
            existing = existing[..insertAt] + markdown + existing[insertAt..];
        }
        else
        {
            existing = existing.TrimEnd() + "\n\n" + markdown;
        }

        File.WriteAllText(path, existing, Encoding.UTF8);
    }

    // ── Register detection (mirrors EmotionalDepthService) ───────────────

    private static string DetectRegister(string? bible)
    {
        if (bible is null or { Length: 0 }) return "";
        if (bible.Contains("GREY register",         StringComparison.OrdinalIgnoreCase)) return "GREY";
        if (bible.Contains("administrative horror", StringComparison.OrdinalIgnoreCase)) return "GREY";
        if (bible.Contains("VULTURES register",     StringComparison.OrdinalIgnoreCase)) return "VULTURES";
        if (bible.Contains("CODA",    StringComparison.OrdinalIgnoreCase)) return "CODA";
        if (bible.Contains("JOY",     StringComparison.OrdinalIgnoreCase)) return "JOY";
        if (bible.Contains("SORROW",  StringComparison.OrdinalIgnoreCase)) return "SORROW";
        if (bible.Contains("Fantasy", StringComparison.OrdinalIgnoreCase)) return "Fantasy";
        return "";
    }

    // ── Load register prompt section from file ───────────────────────────

    private string LoadRegisterPrompt(string registerName)
    {
        var path = GetRegisterFilePath(registerName);
        if (!File.Exists(path)) return "";

        var content = File.ReadAllText(path, Encoding.UTF8);

        // Extract the ## THE PROMPT section
        const string header = "## THE PROMPT";
        var start = content.IndexOf(header, StringComparison.Ordinal);
        if (start < 0) return content; // fallback: whole file

        // End at the next ## heading
        var nextH2 = content.IndexOf("\n## ", start + header.Length, StringComparison.Ordinal);
        return nextH2 >= 0
            ? content[(start + header.Length)..nextH2].Trim()
            : content[(start + header.Length)..].Trim();
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static string Truncate(string text, int max) =>
        text.Length <= max ? text : text[..max] + "\n[truncated]";

    private static string ExtractJson(string raw)
    {
        var start = raw.IndexOf('{');
        var end   = raw.LastIndexOf('}');
        return start >= 0 && end > start ? raw[start..(end + 1)] : raw;
    }
}
