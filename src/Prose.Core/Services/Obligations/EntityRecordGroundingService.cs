using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Interfaces;
using Prose.Core.Services.Audit;

namespace Prose.Core.Services.Obligations;

/// <summary>
/// Metadata-versus-prose grounding audit for entity records (RFC 0013 D6e). Mrs. Chen's record
/// said Kyle once "delivered a mercy job's payload to her sickbed"; no beat in the book says so.
/// The record is an LLM artefact that drifted from the text and nothing ever checked it.
///
/// <para>Method (FActScore-shaped): decompose each record into atomic claims with one Haiku call;
/// retrieve candidate beats per claim (beats tagged with the entity, plus embedding neighbours
/// when available); ask one entailment call per batch of claims which beat, if any, entails or
/// contradicts each, with a verbatim quote; a verdict whose quote the beat does not contain is
/// downgraded to <c>not_found</c> in code. Unentailed claims are filed as findings under
/// <see cref="FindingCategory.EntityDrift"/> (scope <c>node:{slug}#recordground</c>) and any
/// matching non-authored <c>ContinuityClaims</c> rows are downgraded to <c>inferred</c> with a
/// quarantine note. <b>The record text itself is never edited or deleted here</b> — the author
/// decides.</para>
/// </summary>
public class EntityRecordGroundingService(
    IDbContextFactory<ProseDbContext> dbFactory,
    ILlmService llm,
    AuditRunner auditRunner,
    ILogger<EntityRecordGroundingService> log,
    EmbeddingService? embeddings = null)
{
    public const string AuditName = "RECORDGROUND";
    public const int MaxClaimsPerEntity = 40;
    public const int ClaimsPerJudgeCall = 10;
    public const int CandidateBeatsPerClaim = 6;
    public const int MaxCandidateWords = 500;

    public sealed record ClaimVerdict(string Field, string Claim, string Verdict, Guid? BeatId, string? Quote);
    public sealed record EntityReport(Guid EntityId, string Name, string EntityType, int Claims, int Entailed, int Contradicted, int NotFound, int Downgraded, IReadOnlyList<ClaimVerdict> Verdicts);
    public sealed record Report(Guid NodeId, string Slug, int EntitiesExamined, int Claims, int Entailed, int Contradicted, int NotFound, int ClaimsDowngraded, IReadOnlyList<EntityReport> Entities, bool CouldNotLook, int LlmCalls)
    {
        public double UnentailedPct => Claims == 0 ? 0 : (double)(NotFound + Contradicted) / Claims;
    }

    private const string DecomposeSystem = """
        You decompose a fictional character/place/faction record into ATOMIC CLAIMS: short,
        self-contained factual statements about the story world that could each be checked
        against the novel's text — a relationship, a past event, a physical trait, a possession,
        a place lived, a job held, a thing done. Skip style notes, writing guidance, stats and
        numbers-as-design (e.g. "willpower 9"), and anything that is instruction to an author
        rather than a fact about the character.

        Return ONE JSON object and nothing else:
          "reasoning": one or two sentences on what kind of record this is.
          "claims": array of up to 40 objects, keys in order:
              "field": the record field the claim came from (e.g. "description", "relationships.Kyle", "timeline.Ch1").
              "text": the atomic claim, one sentence, naming the entity explicitly.
        """;

    private const string EntailSystem = """
        You verify claims about a fictional character against passages from the novel. For each
        numbered CLAIM decide: "entailed" if some passage states or clearly implies it; "contradicted"
        if some passage states the opposite; "not_found" otherwise. Reason first, then answer.

        Return ONE JSON object and nothing else:
          "reasoning": 2-5 sentences.
          "verdicts": array with one object per claim, keys in order:
              "claim_id": the claim's number.
              "verdict": "entailed" | "contradicted" | "not_found"
              "beat_number": the supporting passage's number for entailed/contradicted, else null.
              "quote": a VERBATIM sentence (≥12 characters) from that passage, else null.
        A claim the passages simply do not mention is "not_found" — never guess from general
        knowledge of the genre.
        """;

    public async Task<Report> RunAsync(Guid bookNodeId, string? entityName = null, bool writeFindings = true, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var node = await db.Nodes.AsNoTracking().IgnoreQueryFilters().Where(n => n.Id == bookNodeId).Select(n => new { n.Slug, n.UniverseId }).FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException($"Node {bookNodeId} not found.");
        var slug = node.Slug ?? bookNodeId.ToString("N");
        var clock = await NarrativeObligationService.LoadClockAsync(db, bookNodeId, ct);
        var beatIds = clock.Beats.Keys.ToList();

        // Entities that appear in this book's prose, by tag.
        var mentioned = await db.BeatEntityMentions.AsNoTracking()
            .Where(m => beatIds.Contains(m.BeatId))
            .GroupBy(m => m.EntityId)
            .Select(g => new { EntityId = g.Key, Beats = g.Select(m => m.BeatId).Distinct().ToList() })
            .ToListAsync(ct);
        var entityIds = mentioned.Select(m => m.EntityId).ToList();
        var entities = await db.Entities.AsNoTracking().IgnoreQueryFilters()
            .Where(e => entityIds.Contains(e.Id) && e.Status != "archived" && (e.EntityType == "character" || e.EntityType == "place" || e.EntityType == "faction"))
            .Select(e => new { e.Id, e.Name, e.EntityType })
            .ToListAsync(ct);
        if (entityName != null)
            entities = entities.Where(e => e.Name.Contains(entityName, StringComparison.OrdinalIgnoreCase)).ToList();

        if (entities.Count == 0 || beatIds.Count == 0)
            return new Report(bookNodeId, slug, 0, 0, 0, 0, 0, 0, [], CouldNotLook: true, 0);

        var reports = new List<EntityReport>();
        var verdictsForFindings = new List<AuditVerdict>();
        var calls = 0; var downgradedTotal = 0;
        // Any entity or batch that could not be judged makes this a PARTIAL run.
        var incomplete = false;

        foreach (var e in entities)
        {
            ct.ThrowIfCancellationRequested();
            var recordJson = await db.Records.AsNoTracking().Where(r => r.EntityId == e.Id).Select(r => r.Json).FirstOrDefaultAsync(ct);
            if (string.IsNullOrWhiteSpace(recordJson)) continue;

            // 1. Decompose.
            string raw;
            try { raw = await llm.GenerateAsync(DecomposeSystem, $"ENTITY: {e.Name} ({e.EntityType})\n\nRECORD JSON:\n{Clamp(recordJson, 12000)}", temperature: 0.1, maxTokens: 2500, model: LlmModels.Haiku, ct: ct); calls++; }
            catch (Exception ex) when (ex is not OperationCanceledException) { log.LogWarning(ex, "Record decomposition failed for {Entity}", e.Name); incomplete = true; continue; }
            var claims = ParseClaims(raw).Take(MaxClaimsPerEntity).ToList();
            if (claims.Count == 0) { reports.Add(new EntityReport(e.Id, e.Name, e.EntityType, 0, 0, 0, 0, 0, [])); continue; }

            // 2. Retrieve candidates: tagged beats (most recent first) ∪ embedding neighbours.
            var tagged = mentioned.First(m => m.EntityId == e.Id).Beats.OrderByDescending(id => clock.PositionOf(id)).ToList(); // most recent first, as documented
            var candidateIds = new List<Guid>(tagged.Take(12));
            if (embeddings != null)
            {
                foreach (var c in claims.Take(8))
                {
                    try
                    {
                        var hits = await embeddings.FindSimilarBeatNodesAsync($"{e.Name}: {c.Text}", CandidateBeatsPerClaim, null, ct);
                        foreach (var h in hits) if (clock.Beats.ContainsKey(h.ScopeId) && !candidateIds.Contains(h.ScopeId)) candidateIds.Add(h.ScopeId);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException) { log.LogDebug(ex, "embedding retrieval failed; continuing with tagged beats"); break; }
                }
            }
            var candidates = await db.Beats.AsNoTracking().Where(b => candidateIds.Contains(b.Id)).Select(b => new { b.Id, b.Text }).ToListAsync(ct);
            var listed = candidates.OrderBy(c => clock.PositionOf(c.Id)).Take(16)
                .Select((c, i) => (Number: i + 1, c.Id, Text: Clamp(BeatMarkup.StripEntityTags(c.Text), MaxCandidateWords * 6))).ToList();
            var passageBlock = string.Join("\n\n", listed.Select(l => $"[{l.Number}] (Ch{clock.ChapterOf(l.Id)})\n{l.Text}"));

            // 3. Entail, in batches.
            var verdicts = new List<ClaimVerdict>();
            for (var start = 0; start < claims.Count; start += ClaimsPerJudgeCall)
            {
                var batch = claims.Skip(start).Take(ClaimsPerJudgeCall).ToList();
                var claimBlock = string.Join("\n", batch.Select((c, i) => $"{i + 1}. {c.Text}"));
                string jraw;
                try { jraw = await llm.GenerateAsync(EntailSystem, $"CLAIMS about {e.Name}:\n{claimBlock}\n\nPASSAGES:\n{passageBlock}", temperature: 0.1, maxTokens: 2000, model: LlmModels.Haiku, ct: ct); calls++; }
                catch (Exception ex) when (ex is not OperationCanceledException) { log.LogWarning(ex, "Entailment call failed for {Entity}", e.Name); incomplete = true; break; }

                var parsed = ParseVerdicts(jraw);
                // An unreadable (e.g. truncated) reply is NOT "none entails any claim": defaulting
                // every claim to not_found filed findings and quarantined ledger rows on no evidence.
                if (parsed.Count == 0)
                {
                    log.LogWarning("Entailment reply for {Entity} could not be parsed — batch skipped", e.Name);
                    incomplete = true;
                    break;
                }
                for (var i = 0; i < batch.Count; i++)
                {
                    var p = parsed.FirstOrDefault(v => v.ClaimId == i + 1);
                    var verdict = p?.Verdict ?? "not_found";
                    Guid? beatId = null; string? quote = null;
                    if (verdict is "entailed" or "contradicted" && p!.BeatNumber is int bn && bn >= 1 && bn <= listed.Count)
                    {
                        var cand = listed[bn - 1];
                        if (QuoteGrounding.Contains(cand.Text, p.Quote, QuoteGrounding.MinObligationQuoteLength)) { beatId = cand.Id; quote = QuoteGrounding.Normalize(p.Quote); }
                        else verdict = "not_found";
                    }
                    else if (verdict is "entailed" or "contradicted") verdict = "not_found";
                    verdicts.Add(new ClaimVerdict(batch[i].Field, batch[i].Text, verdict, beatId, quote));
                }
            }

            // 4. File + quarantine.
            var downgraded = 0;
            foreach (var v in verdicts.Where(v => v.Verdict != "entailed"))
            {
                var sev = v.Verdict == "contradicted" ? "MODERATE" : "MINOR";
                verdictsForFindings.Add(new AuditVerdict(v.Verdict == "contradicted" ? "contradicted" : "unentailed", "Entity record claim not grounded in prose", sev,
                    $"{e.Name}.{v.Field}: \"{Trunc(v.Claim, 160)}\" — {listed.Count} candidate beat(s) examined, " +
                    (v.Verdict == "contradicted" ? $"contradicted in Ch{(v.BeatId is Guid b ? clock.ChapterOf(b) : 0)}: \"{Trunc(v.Quote ?? "", 100)}\"" : "none entails it"),
                    e.Id.ToString("D")));
                downgraded += await QuarantineClaimsAsync(db, e.Id, v.Claim, ct);
            }
            downgradedTotal += downgraded;
            reports.Add(new EntityReport(e.Id, e.Name, e.EntityType, verdicts.Count,
                verdicts.Count(v => v.Verdict == "entailed"), verdicts.Count(v => v.Verdict == "contradicted"), verdicts.Count(v => v.Verdict == "not_found"), downgraded, verdicts));
        }

        // WriteFindingsForRules replaces EVERY finding under the book scope: a single-entity or
        // partial run used to delete all other entities' RECORDGROUND findings.
        var complete = entityName == null && !incomplete;
        if (writeFindings && !complete)
            log.LogWarning("Record grounding for {Slug} was partial (entity filter or failed calls) — findings and snapshot not written", slug);
        if (writeFindings && complete)
            auditRunner.WriteFindingsForRules(AuditName, $"node:{slug}#recordground", FindingCategory.EntityDrift, ["unentailed", "contradicted"], verdictsForFindings);

        var report = new Report(bookNodeId, slug, reports.Count,
            reports.Sum(r => r.Claims), reports.Sum(r => r.Entailed), reports.Sum(r => r.Contradicted), reports.Sum(r => r.NotFound), downgradedTotal,
            reports, CouldNotLook: reports.Count == 0, calls);

        if (writeFindings && complete && report.Claims > 0)
        {
            // Record the ratio where the health snapshot picks it up.
            db.NarrativeHealthSnapshots.Add(new NarrativeHealthSnapshot
            {
                NodeId = bookNodeId, BookTextHash = "", WordCount = 0, TotalBeats = clock.BeatCount, ExaminedBeats = 0,
                UnentailedRecordClaimPct = report.UnentailedPct, InstrumentVersion = "recordground-v1",
            });
            await db.SaveChangesAsync(ct);
        }
        return report;
    }

    /// <summary>Downgrade non-authored ledger claims about this entity whose object text the
    /// unentailed record claim contains. Conservative: substring on the stored object, never a
    /// model judgement; authored claims are untouched; nothing is deleted.</summary>
    private static async Task<int> QuarantineClaimsAsync(ProseDbContext db, Guid entityId, string claimText, CancellationToken ct)
    {
        var idD = entityId.ToString("D"); var idN = entityId.ToString("N");
        var rows = await db.ContinuityClaims
            .Where(c => (c.EntityId == idD || c.EntityId == idN) && c.Provenance != ClaimProvenance.Authored && c.Provenance != ClaimProvenance.Inferred && c.SourceBeatId == null)
            .ToListAsync(ct);
        var n = 0;
        foreach (var c in rows)
        {
            if (string.IsNullOrWhiteSpace(c.Object) || c.Object.Length < 4) continue;
            if (!claimText.Contains(c.Object, StringComparison.OrdinalIgnoreCase)) continue;
            c.Provenance = ClaimProvenance.Inferred;
            c.ResolutionNote = $"[recordground {DateTime.UtcNow:yyyy-MM-dd}] no beat entails \"{Trunc(claimText, 120)}\" — quarantined, author to accept or strike";
            n++;
        }
        if (n > 0) await db.SaveChangesAsync(ct);
        return n;
    }

    internal sealed record Claim(string Field, string Text);
    internal sealed record Parsed(int ClaimId, string Verdict, int? BeatNumber, string? Quote);

    internal static List<Claim> ParseClaims(string? raw)
    {
        var list = new List<Claim>();
        if (string.IsNullOrWhiteSpace(raw)) return list;
        var s = raw.IndexOf('{'); var e = raw.LastIndexOf('}');
        if (s < 0 || e <= s) return list;
        try
        {
            using var doc = JsonDocument.Parse(raw[s..(e + 1)]);
            if (!doc.RootElement.TryGetProperty("claims", out var arr) || arr.ValueKind != JsonValueKind.Array) return list;
            foreach (var c in arr.EnumerateArray())
            {
                var field = c.TryGetProperty("field", out var f) && f.ValueKind == JsonValueKind.String ? f.GetString() ?? "record" : "record";
                var text = c.TryGetProperty("text", out var t) && t.ValueKind == JsonValueKind.String ? t.GetString() : null;
                if (!string.IsNullOrWhiteSpace(text) && text.Length >= 12) list.Add(new Claim(field, text.Trim()));
            }
        }
        catch (JsonException) { }
        return list;
    }

    internal static List<Parsed> ParseVerdicts(string? raw)
    {
        var list = new List<Parsed>();
        if (string.IsNullOrWhiteSpace(raw)) return list;
        var s = raw.IndexOf('{'); var e = raw.LastIndexOf('}');
        if (s < 0 || e <= s) return list;
        try
        {
            using var doc = JsonDocument.Parse(raw[s..(e + 1)]);
            if (!doc.RootElement.TryGetProperty("verdicts", out var arr) || arr.ValueKind != JsonValueKind.Array) return list;
            foreach (var v in arr.EnumerateArray())
            {
                var id = v.TryGetProperty("claim_id", out var ci) && ci.ValueKind == JsonValueKind.Number && ci.TryGetInt32(out var n) ? n : 0;
                var verdict = v.TryGetProperty("verdict", out var vv) && vv.ValueKind == JsonValueKind.String ? vv.GetString()?.Trim().ToLowerInvariant() : null;
                int? bn = v.TryGetProperty("beat_number", out var b) && b.ValueKind == JsonValueKind.Number && b.TryGetInt32(out var bi) ? bi : null;
                var quote = v.TryGetProperty("quote", out var q) && q.ValueKind == JsonValueKind.String ? q.GetString() : null;
                if (id < 1 || verdict is not ("entailed" or "contradicted" or "not_found")) continue;
                list.Add(new Parsed(id, verdict!, bn, quote));
            }
        }
        catch (JsonException) { }
        return list;
    }

    private static string Clamp(string s, int max) => s.Length <= max ? s : s[..max] + " …";
    private static string Trunc(string s, int max) => s.Length <= max ? s : s[..max] + "…";
}
