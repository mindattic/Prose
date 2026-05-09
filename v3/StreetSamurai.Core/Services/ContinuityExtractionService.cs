using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MindAttic.Legion;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Models;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Drives the LLM-side of the unified continuity store. Pulls atomic
/// (entity, predicate, object) claims out of a source — chapter prose or an
/// entity record — and hands each candidate to <see cref="ContinuityService"/>
/// for upsert. Contradictions are surfaced automatically (same predicate,
/// different object on the same entity).
///
/// Backed by Legion's <see cref="LLMVotingService"/> Quorum vote: every active
/// LLM provider becomes a voter, and only candidates whose snippet exists in
/// the source prose survive. This gives multi-model agreement on what's
/// actually being asserted.
/// </summary>
public class ContinuityExtractionService
{
    private readonly ContinuityService store;
    private readonly LLMVotingService voting;
    private readonly IChapterRepository chapters;
    private readonly CharacterRepository peopleRepo;
    private readonly DistrictRepository placesRepo;
    private readonly FactionRepository factionsRepo;
    private readonly CorponationRepository corponationsRepo;
    private readonly IDbContextFactory<StreetSamuraiDbContext> dbFactory;
    private readonly ILogger<ContinuityExtractionService> log;

    public ContinuityExtractionService(
        ContinuityService store,
        LLMVotingService voting,
        IChapterRepository chapters,
        CharacterRepository peopleRepo,
        DistrictRepository placesRepo,
        FactionRepository factionsRepo,
        CorponationRepository corponationsRepo,
        IDbContextFactory<StreetSamuraiDbContext> dbFactory,
        ILogger<ContinuityExtractionService> log)
    {
        this.store           = store;
        this.voting          = voting;
        this.chapters        = chapters;
        this.peopleRepo      = peopleRepo;
        this.placesRepo      = placesRepo;
        this.factionsRepo    = factionsRepo;
        this.corponationsRepo = corponationsRepo;
        this.dbFactory       = dbFactory;
        this.log             = log;
    }

    private const string ExtractionQuestion =
        "Extract every atomic factual assertion the prose makes about every named entity. " +
        "Cover: physical features, gear/weapon placement, abilities, locations, possessions, relationships, " +
        "knowledge, residence, employment, ages, handedness, and any persistent attribute. " +
        "Skip transient emotion or one-time action. " +
        "For each fact, return: " +
        "{ \"entity_name\": \"<exact name as it appears>\", \"predicate\": \"<short snake_case key, e.g. weapon_carry_location, hair_color, lives_at>\", " +
        "\"object\": \"<the value, concise>\", \"snippet\": \"<≤200-char exact quote from the prose that supports the claim>\", " +
        "\"voice\": \"narrator|character|inner_monologue\", \"confidence\": \"low|medium|high\" }. " +
        "Output ONLY a single JSON array on the FINAL line of your response. If no facts can be extracted, output []. " +
        "Be strict: every fact MUST be supported by an exact substring quote from the prose. Do not invent or paraphrase. " +
        "Prefer atomic predicates over compound ones (e.g. \"weapon_carry_location\" not \"carry_setup\"). " +
        "Use the SAME predicate name when reasserting the same kind of fact about different entities.";

    /// <summary>
    /// Extract continuity claims from one chapter's prose.
    /// </summary>
    public async Task<ContinuityExtractionResult> ExtractFromChapterAsync(
        string chapterId,
        Quorum quorum = Quorum.Plurality,
        int maxTokens = 4096,
        int minVoters = 1,
        CancellationToken ct = default)
    {
        var chapter = chapters.LoadChapter(chapterId)
            ?? throw new InvalidOperationException($"Chapter not found: {chapterId}");
        var prose = chapter.PlainText;
        if (string.IsNullOrWhiteSpace(prose))
            throw new InvalidOperationException($"Chapter has no prose: {chapterId}");

        log.LogInformation("[continuity] Extracting from chapter {Num}: {Title} ({Chars} chars)",
            chapter.Number, chapter.Title, prose.Length);

        var context =
            "=== CHAPTER PROSE (extract facts from this) ===\n" +
            $"Chapter {chapter.Number}: {chapter.Title}\n\n" +
            prose;

        var voteRequest = new VoteRequest
        {
            Question  = ExtractionQuestion,
            Context   = context,
            MaxTokens = maxTokens,
            SynthesizeNarrative = false,
        };
        var vote = await voting.VoteAsync(voteRequest, quorum, ct);

        log.LogInformation("[continuity] Vote complete — {Successful}/{Total} voters returned",
            vote.SuccessfulVoters, vote.IndividualVotes.Count);

        // Collect candidates
        var allCandidates = new List<RawCandidate>();
        foreach (var v in vote.IndividualVotes)
        {
            if (v.IsError) continue;
            var combined = (v.Decision ?? "") + "\n" + (v.Reasoning ?? "");
            var arr = ExtractJsonArrayFromText(combined);
            if (arr == null) continue;
            foreach (var el in arr.Value.EnumerateArray())
            {
                if (el.ValueKind != JsonValueKind.Object) continue;
                var c = ParseCandidate(el, v.ProviderId);
                if (c != null) allCandidates.Add(c);
            }
        }

        // Validate snippets exist in prose
        var validated = allCandidates
            .Where(c => prose.Contains(c.Snippet, StringComparison.Ordinal)
                     || prose.Contains(c.Snippet, StringComparison.OrdinalIgnoreCase))
            .ToList();

        // Group by (entity_name, predicate, object) → union of voters
        var grouped = validated
            .GroupBy(c => $"{Normalize(c.EntityName)}|{Normalize(c.Predicate)}|{Normalize(c.Object)}")
            .Select(g => new GroupedCandidate
            {
                EntityName = g.First().EntityName,
                Predicate  = g.First().Predicate,
                Object     = g.First().Object,
                Snippet    = g.First().Snippet,
                Voice      = g.First().Voice,
                Confidence = g.First().Confidence,
                Voters     = g.Select(x => x.Voter).Distinct().ToList(),
            })
            .Where(g => g.Voters.Count >= minVoters)
            .ToList();

        // Resolve entity → upsert
        var diff = new ContinuityExtractionResult
        {
            ChapterId           = chapter.Id,
            ChapterNumber       = chapter.Number ?? 0,
            ChapterTitle        = chapter.Title,
            VotersSuccessful    = vote.SuccessfulVoters,
            VotersTotal         = vote.IndividualVotes.Count,
            CandidatesProposed  = allCandidates.Count,
            CandidatesValidated = grouped.Count,
        };

        foreach (var cand in grouped)
        {
            var resolved = ResolveEntity(cand.EntityName);
            if (resolved == null)
            {
                diff.UnknownEntities.Add(cand.EntityName);
                continue;
            }

            var claim = new ContinuityClaim
            {
                EntityId            = resolved.Value.Id,
                EntityName          = resolved.Value.Name,
                EntityKind          = resolved.Value.Kind,
                Predicate           = cand.Predicate,
                Object              = cand.Object,
                SourceType          = "prose",
                SourceChapterId     = chapter.Id,
                SourceChapterNumber = chapter.Number,
                SourceChapterTitle  = chapter.Title,
                Snippet             = cand.Snippet,
                Voice               = cand.Voice,
                Confidence          = cand.Confidence,
                ExtractedBy         = cand.Voters,
            };
            var r = store.Upsert(claim);
            switch (r.Outcome)
            {
                case "NEW":          diff.NewClaims++;          break;
                case "CONFIRMED":    diff.ConfirmedClaims++;    break;
                case "CONTRADICTED": diff.ContradictedClaims++; break;
            }
        }

        return diff;
    }

    /// <summary>
    /// Extract continuity claims from every chapter in a book. Sequential to
    /// keep cost predictable; long-running.
    /// </summary>
    public async Task<List<ContinuityExtractionResult>> ExtractFromBookAsync(
        Book book,
        Quorum quorum = Quorum.Plurality,
        int maxTokens = 4096,
        int minVoters = 1,
        CancellationToken ct = default)
    {
        var results = new List<ContinuityExtractionResult>();
        foreach (var cid in book.ChapterIds ?? new())
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var r = await ExtractFromChapterAsync(cid, quorum, maxTokens, minVoters, ct);
                results.Add(r);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "[continuity] Chapter {Cid} extraction failed", cid);
                results.Add(new ContinuityExtractionResult { ChapterId = cid, Error = ex.Message });
            }
        }
        return results;
    }

    /// <summary>
    /// Flatten a structured entity record into atomic claims. Loads the
    /// canonical <c>Records.Json</c> blob for the given EntityId from SQL.
    /// Trivial scalar fields (e.g. "role": "fixer") are emitted directly;
    /// prose fields (description, personality) are run through the same
    /// Legion vote as chapter prose so we extract atomic claims from them too.
    /// </summary>
    public async Task<ContinuityExtractionResult> ExtractFromEntityRecordAsync(
        Guid entityId,
        Quorum quorum = Quorum.Plurality,
        int maxTokens = 2048,
        CancellationToken ct = default)
    {
        var result = new ContinuityExtractionResult
        {
            ChapterId    = "",
            ChapterTitle = $"entity:{entityId}",
        };

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var blob = await db.Records.AsNoTracking()
            .Where(r => r.EntityId == entityId)
            .Select(r => new { r.Json, EntityType = r.Entity!.EntityType, EntityName = r.Entity.Name })
            .FirstOrDefaultAsync(ct);
        if (blob == null)
        {
            result.Error = $"no Records.Json for entity {entityId}";
            return result;
        }
        result.ChapterTitle = $"entity:{blob.EntityName}";

        using var doc = JsonDocument.Parse(blob.Json);
        var root = doc.RootElement;

        var entityIdStr = root.TryGetProperty("id",   out var i) ? i.GetString() ?? entityId.ToString("N") : entityId.ToString("N");
        var entityName  = root.TryGetProperty("name", out var n) ? n.GetString() ?? blob.EntityName : blob.EntityName;
        var entityKind  = InferKindFromEntityType(blob.EntityType);

        if (string.IsNullOrEmpty(entityIdStr) || string.IsNullOrEmpty(entityName))
        {
            result.Error = "entity_record missing id or name";
            return result;
        }

        // 1) Direct scalar claims for top-level string fields.
        foreach (var prop in root.EnumerateObject())
        {
            if (prop.Name is "id" or "name" or "type" or "tags" or "aliases") continue;
            if (prop.Value.ValueKind != JsonValueKind.String) continue;
            var val = prop.Value.GetString() ?? "";
            if (string.IsNullOrWhiteSpace(val)) continue;

            // Skip obvious prose-style fields — those go through the LLM pass.
            if (IsProseField(prop.Name)) continue;
            if (val.Length > 200) continue; // treat long strings as prose

            var claim = new ContinuityClaim
            {
                EntityId    = entityIdStr,
                EntityName  = entityName,
                EntityKind  = entityKind,
                Predicate   = prop.Name,
                Object      = val,
                SourceType  = "entity_record",
                SourcePath  = $"db:Records[{entityId}]",
                Snippet     = val.Length > 200 ? val[..200] : val,
                Voice       = "writer",
                Confidence  = "high",
                ExtractedBy = new List<string> { "entity_record_walker" },
            };
            var r = store.Upsert(claim);
            switch (r.Outcome)
            {
                case "NEW":          result.NewClaims++;          break;
                case "CONFIRMED":    result.ConfirmedClaims++;    break;
                case "CONTRADICTED": result.ContradictedClaims++; break;
            }
        }

        // 2) Prose fields (description, personality, ideology, narrative_function …) get the Legion pass.
        var proseSections = new List<(string field, string text)>();
        foreach (var prop in root.EnumerateObject())
        {
            if (prop.Value.ValueKind != JsonValueKind.String) continue;
            var v = prop.Value.GetString() ?? "";
            if (IsProseField(prop.Name) && v.Length >= 80)
                proseSections.Add((prop.Name, v));
        }
        if (proseSections.Count == 0) return result;

        var ctxBuilder = new System.Text.StringBuilder();
        ctxBuilder.AppendLine($"=== ENTITY RECORD: {entityName} ({entityKind}) ===");
        foreach (var (field, text) in proseSections)
        {
            ctxBuilder.AppendLine($"--- {field} ---");
            ctxBuilder.AppendLine(text);
            ctxBuilder.AppendLine();
        }
        var ctxText = ctxBuilder.ToString();

        var voteRequest = new VoteRequest
        {
            Question  = ExtractionQuestion,
            Context   = ctxText,
            MaxTokens = maxTokens,
            SynthesizeNarrative = false,
        };
        var vote = await voting.VoteAsync(voteRequest, quorum, ct);

        // The "snippet must exist in prose" check uses the combined prose
        // section text as the substrate.
        var prose = string.Join("\n", proseSections.Select(s => s.text));

        foreach (var v in vote.IndividualVotes)
        {
            if (v.IsError) continue;
            var combined = (v.Decision ?? "") + "\n" + (v.Reasoning ?? "");
            var arr = ExtractJsonArrayFromText(combined);
            if (arr == null) continue;
            foreach (var el in arr.Value.EnumerateArray())
            {
                if (el.ValueKind != JsonValueKind.Object) continue;
                var c = ParseCandidate(el, v.ProviderId);
                if (c == null) continue;
                if (!prose.Contains(c.Snippet, StringComparison.OrdinalIgnoreCase)) continue;

                var claim = new ContinuityClaim
                {
                    EntityId    = entityIdStr,
                    EntityName  = entityName,
                    EntityKind  = entityKind,
                    Predicate   = c.Predicate,
                    Object      = c.Object,
                    SourceType  = "entity_record",
                    SourcePath  = $"db:Records[{entityId}]",
                    Snippet     = c.Snippet,
                    Voice       = c.Voice,
                    Confidence  = c.Confidence,
                    ExtractedBy = new List<string> { c.Voter },
                };
                var r = store.Upsert(claim);
                switch (r.Outcome)
                {
                    case "NEW":          result.NewClaims++;          break;
                    case "CONFIRMED":    result.ConfirmedClaims++;    break;
                    case "CONTRADICTED": result.ContradictedClaims++; break;
                }
            }
        }
        result.VotersSuccessful = vote.SuccessfulVoters;
        result.VotersTotal      = vote.IndividualVotes.Count;
        return result;
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static bool IsProseField(string name)
    {
        return name is "description" or "personality" or "ideology" or "narrative_function"
            or "premise" or "synopsis" or "biography" or "background" or "motto"
            or "story_hooks" or "context" or "summary";
    }

    /// <summary>
    /// Map a canonical <c>Entities.EntityType</c> value to the kind label
    /// that <see cref="ContinuityClaim.EntityKind"/> uses (mostly identical;
    /// `character` becomes `person` to match the legacy claim taxonomy).
    /// </summary>
    private static string InferKindFromEntityType(string entityType) => entityType switch
    {
        "character" => "person",
        _           => entityType,
    };

    private static string Normalize(string s)
        => string.IsNullOrEmpty(s) ? "" : Regex.Replace(s.ToLowerInvariant(), @"\s+", " ").Trim();

    private (string Id, string Name, string Kind)? ResolveEntity(string rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName)) return null;
        var clean = Regex.Replace(rawName, @"\s*\([^)]*\)\s*$", "").Trim();

        var p = peopleRepo.GetByName(clean) ?? peopleRepo.GetByName(rawName);
        if (p != null) return (p.Id, p.Name, "person");

        var d = placesRepo.GetByName(clean) ?? placesRepo.GetByName(rawName);
        if (d != null) return (d.Id, d.Name, "place");

        var f = factionsRepo.GetByName(clean) ?? factionsRepo.GetByName(rawName);
        if (f != null) return (f.Id, f.Name, "faction");

        var c = corponationsRepo.GetByName(clean) ?? corponationsRepo.GetByName(rawName);
        if (c != null) return (c.Id, c.Name, "corponation");

        return null;
    }

    private static JsonElement? ExtractJsonArrayFromText(string text)
    {
        if (string.IsNullOrEmpty(text)) return null;

        // Greedy: from first '[' to last ']'.
        var first = text.IndexOf('[');
        var last  = text.LastIndexOf(']');
        if (first >= 0 && last > first)
        {
            var slice = text[first..(last + 1)];
            try
            {
                using var d = JsonDocument.Parse(slice);
                if (d.RootElement.ValueKind == JsonValueKind.Array)
                    return d.RootElement.Clone();
            }
            catch { }
        }

        // Fallback: scan for non-empty arrays of objects.
        var rx = new Regex(@"\[\s*\{[\s\S]*?\}\s*\]", RegexOptions.Compiled);
        foreach (Match m in rx.Matches(text))
        {
            try
            {
                using var d = JsonDocument.Parse(m.Value);
                if (d.RootElement.ValueKind == JsonValueKind.Array && d.RootElement.GetArrayLength() > 0)
                    return d.RootElement.Clone();
            }
            catch { }
        }
        return null;
    }

    private static RawCandidate? ParseCandidate(JsonElement el, string voterProviderId)
    {
        var entityName = el.TryGetProperty("entity_name", out var n) ? n.GetString() ?? "" : "";
        var predicate  = el.TryGetProperty("predicate",   out var p) ? p.GetString() ?? "" : "";
        var obj        = el.TryGetProperty("object",      out var o) ? o.GetString() ?? "" : "";
        var snippet    = el.TryGetProperty("snippet",     out var s) ? s.GetString() ?? "" : "";
        if (string.IsNullOrEmpty(entityName) || string.IsNullOrEmpty(predicate)
         || string.IsNullOrEmpty(obj) || string.IsNullOrEmpty(snippet)) return null;

        return new RawCandidate
        {
            EntityName = Truncate(entityName, 200),
            Predicate  = Truncate(predicate, 80),
            Object     = Truncate(obj, 300),
            Snippet    = Truncate(snippet, 300),
            Voice      = el.TryGetProperty("voice",      out var v)  ? Truncate(v.GetString() ?? "narrator", 32) : "narrator",
            Confidence = el.TryGetProperty("confidence", out var cf) ? Truncate(cf.GetString() ?? "medium", 16) : "medium",
            Voter      = voterProviderId ?? "unknown",
        };
    }

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max];

    // ── inner types ──────────────────────────────────────────────────────────

    private class RawCandidate
    {
        public string EntityName { get; set; } = "";
        public string Predicate  { get; set; } = "";
        public string Object     { get; set; } = "";
        public string Snippet    { get; set; } = "";
        public string Voice      { get; set; } = "";
        public string Confidence { get; set; } = "";
        public string Voter      { get; set; } = "";
    }

    private class GroupedCandidate
    {
        public string EntityName { get; set; } = "";
        public string Predicate  { get; set; } = "";
        public string Object     { get; set; } = "";
        public string Snippet    { get; set; } = "";
        public string Voice      { get; set; } = "";
        public string Confidence { get; set; } = "";
        public List<string> Voters { get; set; } = new();
    }
}

public class ContinuityExtractionResult
{
    public string ChapterId           { get; set; } = "";
    public int    ChapterNumber       { get; set; }
    public string ChapterTitle        { get; set; } = "";
    public int    VotersSuccessful    { get; set; }
    public int    VotersTotal         { get; set; }
    public int    CandidatesProposed  { get; set; }
    public int    CandidatesValidated { get; set; }
    public int    NewClaims           { get; set; }
    public int    ConfirmedClaims     { get; set; }
    public int    ContradictedClaims  { get; set; }
    public List<string> UnknownEntities { get; set; } = new();
    public string? Error              { get; set; }
}
