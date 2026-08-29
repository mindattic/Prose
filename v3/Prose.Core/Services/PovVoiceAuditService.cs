using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;
using Prose.Core.Interfaces;

namespace Prose.Core.Services;

public record PovVoiceAuditReport(
    string NodeCode, int BeatsAudited, int HeadHopFindings, int VoiceSamenessFindings);

/// <summary>
/// POV discipline + cross-character voice distinctiveness audit (2026-08-28). Before this,
/// neither existed anywhere in the engine: POV only ever biased which DCM register doc
/// dominated at generation time (never verified afterwards), and the only voice-fingerprint
/// tool (VoiceFingerprintAnalyzer) compares a character against their OWN history, never
/// character A against character B in the same scene.
///
/// Batched Haiku pass per chapter over a book's beats. For each beat it knows the recorded POV
/// narrator (BeatEntityPresence PresenceType='pov') and asks two questions:
///   1. Head-hopping — does the narration directly report a NON-POV character's inner
///      thoughts/feelings (not observable behavior)? Cite the phrase.
///   2. Voice sameness — do two named characters' dialogue lines in this beat read
///      interchangeably in register (same cadence, vocabulary, subtext posture)?
///
/// Findings: "POV " (Medium) and "VOICE " (Low) prefixes, FindingCategory.CraftChecklist,
/// FilePath "node:{slug}" — the shape ProseWriterRouter's findings loop-back consumes.
/// Explicit CLI only (`prose --pov-audit`) — an LLM-cost decision, not a per-beat default.
/// </summary>
public class PovVoiceAuditService
{
    private readonly IDbContextFactory<ProseDbContext> dbFactory;
    private readonly ILlmService llm;
    private readonly FindingsService findings;
    private readonly ILogger<PovVoiceAuditService> log;

    private const int PerBeatClipChars = 9_000;
    private const int BatchCharBudget = 55_000;
    private const int MaxBatchBeats = 8;
    private const string PovPrefix = "POV ";
    private const string VoicePrefix = "VOICE ";

    public PovVoiceAuditService(
        IDbContextFactory<ProseDbContext> dbFactory,
        ILlmService llm,
        FindingsService findings,
        ILogger<PovVoiceAuditService> log)
    {
        this.dbFactory = dbFactory;
        this.llm = llm;
        this.findings = findings;
        this.log = log;
    }

    private class PresenceRow
    {
        public Guid BeatId { get; set; }
        public string? EntityName { get; set; }
        public string? PresenceType { get; set; }
    }

    private const string System = """
You audit point-of-view discipline and voice distinctiveness in fiction. Each beat below names
its POV narrator. For each beat answer two questions:

1. HEAD-HOPPING: does the NARRATION directly report a non-POV character's inner thoughts,
   feelings, or private knowledge as fact (not as the POV character's observation/inference of
   behavior)? "Marcus felt a stab of guilt" in Kyle's POV is a head-hop; "Marcus looked away —
   guilt, maybe" is not. Cite the exact offending phrase (max 15 words).

2. VOICE SAMENESS: when two or more named characters speak in this beat, do any two read
   interchangeably — same cadence, vocabulary level, and subtext posture, such that swapping
   the names would change nothing? Name the pair and say why in one short clause.

Be conservative: report only clear cases. Output STRICT JSON, no fences:
{"items":[{"ref":N,"headHops":[{"who":"Name","phrase":"..."}],"sameVoice":[{"a":"Name","b":"Name","why":"..."}]}]}
Omit empty arrays or use []. An item with both arrays empty may be omitted entirely.
""";

    public async Task<PovVoiceAuditReport> AuditAsync(
        string slugOrCode, bool dryRun = false, Action<string>? progress = null, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var node = await db.Nodes.AsNoTracking().FirstOrDefaultAsync(
            n => n.Slug == slugOrCode || (n.NodeCode != null && n.NodeCode.ToUpper() == slugOrCode.ToUpper()), ct)
            ?? throw new InvalidOperationException($"Node not found: {slugOrCode}");
        var nodeCode = node.NodeCode?.ToUpperInvariant() ?? node.Slug.ToUpperInvariant();
        var fp = $"node:{node.Slug}";

        var searchIds = await NodeWorkbenchService.GetLeafDescendantIdsAsync(db, node.Id, ct);
        var beats = await (
            from bn in db.BeatNodes.AsNoTracking()
            join b in db.Beats.AsNoTracking() on bn.BeatId equals b.Id
            join c in db.Nodes.AsNoTracking() on bn.NodeId equals c.Id
            where searchIds.Contains(bn.NodeId) && b.Text != null && b.Text != ""
            orderby c.SortKey, bn.SortKey
            select new { b.Id, b.Number, Text = b.Text!, Chapter = c.Title }
        ).ToListAsync(ct);
        if (beats.Count == 0) return new PovVoiceAuditReport(nodeCode, 0, 0, 0);

        // POV per beat — BeatEntityPresence has no EF mapping; raw SQL is the established idiom.
        var beatIdList = string.Join(",", beats.Select(b => $"'{b.Id}'"));
        var povRows = await db.Database.SqlQueryRaw<PresenceRow>(
            $"SELECT BeatId, EntityName, PresenceType FROM BeatEntityPresence WHERE PresenceType = 'pov' AND BeatId IN ({beatIdList})")
            .ToListAsync(ct);
        var povByBeat = povRows
            .GroupBy(p => p.BeatId)
            .ToDictionary(g => g.Key, g => g.First().EntityName ?? "");

        if (!dryRun)
        {
            findings.DeleteBySummaryPrefix(fp, PovPrefix);
            findings.DeleteBySummaryPrefix(fp, VoicePrefix);
        }

        int headHops = 0, sameVoice = 0, done = 0, audited = 0;
        var numberByBeatId = beats.ToDictionary(b => b.Id, b => b.Number);

        foreach (var batch in BuildBatches(beats
            .Where(b => povByBeat.ContainsKey(b.Id)) // no recorded POV → nothing to enforce
            .Select(b => (b.Id, Text: BeatMarkup.StripEntityTags(b.Text), Label: $"{b.Chapter} · POV: {povByBeat[b.Id]}"))
            .ToList()))
        {
            ct.ThrowIfCancellationRequested();
            var refMap = new Dictionary<int, Guid>();
            var sb = new StringBuilder();
            for (int i = 0; i < batch.Count; i++)
            {
                refMap[i] = batch[i].Id;
                sb.AppendLine($"[ref {i} · {batch[i].Label}]");
                sb.AppendLine(Clip(batch[i].Text));
                sb.AppendLine();
            }

            try
            {
                var raw = await llm.GenerateAsync(System, sb.ToString(), temperature: 0.1,
                    maxTokens: 1600, model: LlmModels.Haiku, ct: ct);
                foreach (var item in Parse(raw, refMap))
                {
                    var number = numberByBeatId.GetValueOrDefault(item.BeatId);
                    var pov = povByBeat.GetValueOrDefault(item.BeatId, "?");
                    foreach (var hh in item.HeadHops)
                    {
                        headHops++;
                        if (!dryRun) findings.Upsert(fp, chapterId: null,
                            FindingCategory.CraftChecklist, FindingSeverity.Medium,
                            $"{PovPrefix}beat #{number}: narration head-hops out of {pov}'s POV into {hh.Who} — \"{hh.Phrase}\"",
                            snippet: null,
                            suggestedFix: $"Stay inside {pov}'s perception: render {hh.Who}'s state as observable behavior or {pov}'s inference.");
                    }
                    foreach (var sv in item.SameVoice)
                    {
                        sameVoice++;
                        if (!dryRun) findings.Upsert(fp, chapterId: null,
                            FindingCategory.CraftChecklist, FindingSeverity.Low,
                            $"{VoicePrefix}beat #{number}: {sv.A} and {sv.B} speak in the same register — {sv.Why}",
                            snippet: null,
                            suggestedFix: "Differentiate via each character's on-file cadence/vocabulary/tics (their Speech* record).");
                    }
                }
                audited += batch.Count;
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "[PovVoiceAudit] batch failed ({Count} beats)", batch.Count);
            }

            done += batch.Count;
            progress?.Invoke($"  {done} beats processed ({headHops} head-hops, {sameVoice} voice-sameness)");
        }

        log.LogInformation("[PovVoiceAudit] {Code}: {Audited} beats audited, {HeadHops} head-hops, {Same} voice-sameness",
            nodeCode, audited, headHops, sameVoice);
        return new PovVoiceAuditReport(nodeCode, audited, headHops, sameVoice);
    }

    private sealed record AuditItem(Guid BeatId, List<(string Who, string Phrase)> HeadHops, List<(string A, string B, string Why)> SameVoice);

    internal static List<AuditItemDto> ParseRaw(string raw)
    {
        var list = new List<AuditItemDto>();
        try
        {
            using var doc = JsonDocument.Parse(JsonDefaults.StripCodeFences(raw));
            if (!doc.RootElement.TryGetProperty("items", out var arr)) return list;
            foreach (var el in arr.EnumerateArray())
            {
                try
                {
                    if (!el.TryGetProperty("ref", out var refEl) || refEl.ValueKind != JsonValueKind.Number) continue;
                    var dto = new AuditItemDto { Ref = refEl.GetInt32() };
                    if (el.TryGetProperty("headHops", out var hhArr) && hhArr.ValueKind == JsonValueKind.Array)
                        foreach (var hh in hhArr.EnumerateArray())
                        {
                            var who = hh.TryGetProperty("who", out var w) ? w.GetString() : null;
                            var phrase = hh.TryGetProperty("phrase", out var p) ? p.GetString() : null;
                            if (!string.IsNullOrWhiteSpace(who) && !string.IsNullOrWhiteSpace(phrase))
                                dto.HeadHops.Add((who!, phrase!));
                        }
                    if (el.TryGetProperty("sameVoice", out var svArr) && svArr.ValueKind == JsonValueKind.Array)
                        foreach (var sv in svArr.EnumerateArray())
                        {
                            var a = sv.TryGetProperty("a", out var ae) ? ae.GetString() : null;
                            var b = sv.TryGetProperty("b", out var be) ? be.GetString() : null;
                            var why = sv.TryGetProperty("why", out var we) ? we.GetString() : "";
                            if (!string.IsNullOrWhiteSpace(a) && !string.IsNullOrWhiteSpace(b))
                                dto.SameVoice.Add((a!, b!, why ?? ""));
                        }
                    list.Add(dto);
                }
                catch { /* skip malformed entry */ }
            }
        }
        catch { /* malformed JSON — return what parsed */ }
        return list;
    }

    internal sealed class AuditItemDto
    {
        public int Ref { get; set; }
        public List<(string Who, string Phrase)> HeadHops { get; } = new();
        public List<(string A, string B, string Why)> SameVoice { get; } = new();
    }

    private static IEnumerable<AuditItem> Parse(string raw, IReadOnlyDictionary<int, Guid> refMap)
    {
        foreach (var dto in ParseRaw(raw))
        {
            if (!refMap.TryGetValue(dto.Ref, out var beatId)) continue;
            if (dto.HeadHops.Count == 0 && dto.SameVoice.Count == 0) continue;
            yield return new AuditItem(beatId, dto.HeadHops, dto.SameVoice);
        }
    }

    private static List<List<(Guid Id, string Text, string Label)>> BuildBatches(
        List<(Guid Id, string Text, string Label)> items)
    {
        var batches = new List<List<(Guid, string, string)>>();
        var current = new List<(Guid, string, string)>();
        int currentChars = 0;
        foreach (var item in items)
        {
            var clippedLen = Math.Min(item.Text.Length, PerBeatClipChars);
            if (current.Count > 0 &&
                (current.Count >= MaxBatchBeats || currentChars + clippedLen > BatchCharBudget))
            {
                batches.Add(current);
                current = new List<(Guid, string, string)>();
                currentChars = 0;
            }
            current.Add(item);
            currentChars += clippedLen;
        }
        if (current.Count > 0) batches.Add(current);
        return batches;
    }

    private static string Clip(string text, int cap = PerBeatClipChars)
    {
        if (text.Length <= cap) return text;
        int head = cap / 2;
        int tail = cap - head;
        return text[..head] + "\n\n[...clipped for length...]\n\n" + text[^tail..];
    }
}
