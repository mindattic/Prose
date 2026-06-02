using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MindAttic.Legion;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Persona reader-review system. Many distinct Legion personas (from the
/// 1000-persona library) each read a strand and, IN CHARACTER, write an honest
/// review with a 1-100 score and concrete improvement notes — round-robined
/// across the trusted-4 providers for genuine model + viewpoint diversity. The
/// reviews are saved to <see cref="StrandReview"/>; an Amazon-style aggregate is
/// synthesized into <see cref="StrandReviewSummary"/>.
/// </summary>
public class StrandReviewService
{
    private readonly LegionClient legion;
    private readonly VotingConfiguration cfg;
    private readonly StrandMarkdownExporter exporter;
    private readonly IDbContextFactory<StreetSamuraiDbContext> dbFactory;
    private readonly ILogger<StrandReviewService> log;

    private const int MaxConcurrency = 10;

    public StrandReviewService(
        LegionClient legion,
        VotingConfiguration cfg,
        StrandMarkdownExporter exporter,
        IDbContextFactory<StreetSamuraiDbContext> dbFactory,
        ILogger<StrandReviewService> log)
    {
        this.legion = legion;
        this.cfg = cfg;
        this.exporter = exporter;
        this.dbFactory = dbFactory;
        this.log = log;
    }

    public record ReviewRunResult(int Requested, int Saved, int Failed, double AvgScore, string ContentHash, string ExportPath);

    /// <summary>Distinct PersonaIds from the strand's most-recent review batch —
    /// used to re-run the SAME readers against a revised version (focus group).</summary>
    public async Task<List<string>> GetLatestPersonaIdsAsync(Guid strandId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var latestHash = await db.StrandReviews
            .Where(r => r.StrandId == strandId)
            .OrderByDescending(r => r.ReviewedAt)
            .Select(r => r.ContentHash)
            .FirstOrDefaultAsync(ct);
        if (string.IsNullOrEmpty(latestHash)) return new List<string>();
        return await db.StrandReviews
            .Where(r => r.StrandId == strandId && r.ContentHash == latestHash)
            .Select(r => r.PersonaId)
            .Distinct()
            .ToListAsync(ct);
    }

    /// <summary>Run persona reviews of the strand. When <paramref name="personaIds"/>
    /// is supplied, re-runs those EXACT personas (a before/after focus group);
    /// otherwise samples <paramref name="readers"/> fresh enriched personas.
    /// Reports completed-reviewer count via <paramref name="progress"/>.</summary>
    public async Task<ReviewRunResult> ReviewStrandAsync(
        Guid strandId, int readers, IReadOnlyList<string>? personaIds = null,
        string? groupName = null, IProgress<int>? progress = null, CancellationToken ct = default)
    {
        if (readers <= 0) readers = 1;

        var providers = cfg.ActiveProviderIds;
        if (providers.Count == 0)
            throw new InvalidOperationException("No trusted LLM providers are configured with API keys — cannot run reviews.");

        var export = await exporter.ExportAsync(strandId, ct: ct);

        // Resolve the focus group (named panel). An EXISTING group's roster is the
        // reusable panel — reuse it verbatim (a focus-group rerun). A NEW group is
        // seeded from the sampled/supplied personas and its roster is persisted.
        Guid? groupId = null;
        List<Persona> personas;
        if (!string.IsNullOrWhiteSpace(groupName))
        {
            var (gid, memberIds) = await GetGroupAsync(groupName!, ct);
            if (gid != null && memberIds.Count > 0)
            {
                groupId = gid;
                personas = PersonasByIds(memberIds);
            }
            else
            {
                personas = (personaIds is { Count: > 0 }) ? PersonasByIds(personaIds) : SampleEnrichedPersonas(readers);
                groupId = await CreateGroupAsync(groupName!, personas, ct);
            }
        }
        else
        {
            personas = (personaIds is { Count: > 0 }) ? PersonasByIds(personaIds) : SampleEnrichedPersonas(readers);
        }

        var sem = new SemaphoreSlim(MaxConcurrency);
        var done = 0;
        var reviews = new System.Collections.Concurrent.ConcurrentBag<StrandReview>();
        var failed = 0;

        var tasks = new List<Task>(personas.Count);
        for (int i = 0; i < personas.Count; i++)
        {
            var persona = personas[i];
            var provider = providers[i % providers.Count];
            tasks.Add(Task.Run(async () =>
            {
                await sem.WaitAsync(ct);
                try
                {
                    var review = await ReviewOnceAsync(strandId, export, persona, provider, studyMode: false, ct);
                    if (review != null) reviews.Add(review);
                    else Interlocked.Increment(ref failed);
                }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref failed);
                    log.LogWarning(ex, "Review failed: persona {Persona} via {Provider}", persona.Id, provider);
                }
                finally
                {
                    sem.Release();
                    progress?.Report(Interlocked.Increment(ref done));
                }
            }, ct));
        }
        await Task.WhenAll(tasks);

        var saved = reviews.ToList();
        foreach (var r in saved) { r.FocusGroupId = groupId; r.FocusGroupName = groupName; }
        if (saved.Count > 0)
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            db.StrandReviews.AddRange(saved);
            await db.SaveChangesAsync(ct);
            await RecomputeScoresAsync(strandId, ct);
        }

        var avg = saved.Count > 0 ? saved.Average(r => r.Score) : 0.0;
        return new ReviewRunResult(personas.Count, saved.Count, failed, avg, export.ContentHash, export.Path);
    }

    public sealed record StudyRunResult(int Requested, int Saved, int Failed, int Clusters,
        double MeanScore, double MeanFlow, string ContentHash, string ReportMarkdown);

    /// <summary>Segment study: one large INDEPENDENT panel (disjoint from Group A)
    /// reads the strand and micro-scores every beat; reviewers are then clustered
    /// into emergent audiences and the per-beat scores aggregated into a
    /// Pareto/contested decision report. Freeze-then-study: nothing is edited
    /// during the run, so groups can't conflict.</summary>
    public async Task<StudyRunResult> RunSegmentStudyAsync(
        Guid strandId, int panelSize, IProgress<int>? progress = null, CancellationToken ct = default)
    {
        if (panelSize <= 0) panelSize = 128;
        var providers = cfg.ActiveProviderIds;
        if (providers.Count == 0)
            throw new InvalidOperationException("No trusted LLM providers are configured with API keys — cannot run a study.");

        var export = await exporter.ExportAsync(strandId, numberBeats: true, ct);
        var beatCount = export.BeatCount;

        // Fresh panel, disjoint from Group A (fresh eyes, no anchoring).
        var (_, groupAIds) = await GetGroupAsync("Group A", ct);
        var personas = SampleEnrichedPersonasExcluding(panelSize, groupAIds.ToHashSet());

        var sem = new SemaphoreSlim(MaxConcurrency);
        var reviews = new System.Collections.Concurrent.ConcurrentBag<StrandReview>();
        var done = 0; var failed = 0;
        var tasks = new List<Task>(personas.Count);
        for (int i = 0; i < personas.Count; i++)
        {
            var persona = personas[i];
            var provider = providers[i % providers.Count];
            tasks.Add(Task.Run(async () =>
            {
                await sem.WaitAsync(ct);
                try
                {
                    var r = await ReviewOnceAsync(strandId, export, persona, provider, studyMode: true, ct);
                    if (r != null && r.BeatScores.Count > 0) reviews.Add(r);
                    else Interlocked.Increment(ref failed);
                }
                catch (Exception ex) { Interlocked.Increment(ref failed); log.LogWarning(ex, "Study review failed: {P}", persona.Id); }
                finally { sem.Release(); progress?.Report(Interlocked.Increment(ref done)); }
            }, ct));
        }
        await Task.WhenAll(tasks);

        var saved = reviews.ToList();
        if (saved.Count == 0)
            return new StudyRunResult(personas.Count, 0, failed, 0, 0, 0, export.ContentHash, "_No reviews saved._");

        // Cluster in memory on the reviewer x beat matrix.
        var matrix = BuildMatrix(saved, beatCount);
        var clustering = ReviewClusterer.Cluster(matrix);

        // Aggregate → report + cluster labels.
        var reviewerRows = new List<SegmentAggregator.Reviewer>(saved.Count);
        for (int i = 0; i < saved.Count; i++)
        {
            var bs = saved[i].BeatScores.ToDictionary(x => x.BeatNumber, x => x.Score);
            reviewerRows.Add(new SegmentAggregator.Reviewer(clustering.Assignments[i], saved[i].Score, saved[i].FlowScore, bs));
        }
        var report = SegmentAggregator.Build(reviewerRows, beatCount, clustering.K);
        var labelById = report.Clusters.ToDictionary(c => c.Id, c => c.Label);

        // Stamp cluster id/label + a study group name on each review, then persist.
        var groupName = $"Study {export.ContentHash[..6]}";
        for (int i = 0; i < saved.Count; i++)
        {
            saved[i].ClusterId = clustering.Assignments[i];
            saved[i].ClusterLabel = labelById.TryGetValue(clustering.Assignments[i], out var lbl) ? Trunc(lbl, 60) : null;
            saved[i].FocusGroupName = groupName;
        }
        await using (var db = await dbFactory.CreateDbContextAsync(ct))
        {
            db.StrandReviews.AddRange(saved);
            await db.SaveChangesAsync(ct);
        }

        await RecomputeScoresAsync(strandId, ct);

        var meanScore = saved.Average(r => r.Score);
        var meanFlow = saved.Where(r => r.FlowScore.HasValue).Select(r => (double)r.FlowScore!.Value).DefaultIfEmpty(0).Average();
        return new StudyRunResult(personas.Count, saved.Count, failed, clustering.K,
            Math.Round(meanScore, 1), Math.Round(meanFlow, 1), export.ContentHash, report.Markdown);
    }

    private static string Trunc(string s, int n) => s.Length <= n ? s : s[..n];

    /// <summary>reviewer x beat matrix, mean-imputed per beat for any missing score.</summary>
    private static double[][] BuildMatrix(List<StrandReview> reviews, int beatCount)
    {
        int n = reviews.Count;
        var present = new int?[n][];
        for (int i = 0; i < n; i++)
        {
            present[i] = new int?[beatCount];
            foreach (var b in reviews[i].BeatScores)
                if (b.BeatNumber >= 1 && b.BeatNumber <= beatCount) present[i][b.BeatNumber - 1] = b.Score;
        }
        var colMean = new double[beatCount];
        for (int j = 0; j < beatCount; j++)
        {
            var vals = new List<int>();
            for (int i = 0; i < n; i++) if (present[i][j].HasValue) vals.Add(present[i][j]!.Value);
            colMean[j] = vals.Count > 0 ? vals.Average() : 3.0;
        }
        var m = new double[n][];
        for (int i = 0; i < n; i++)
        {
            m[i] = new double[beatCount];
            for (int j = 0; j < beatCount; j++) m[i][j] = present[i][j] ?? colMean[j];
        }
        return m;
    }

    private static List<Persona> SampleEnrichedPersonasExcluding(int count, HashSet<string> exclude)
    {
        var pool = PersonaLibrary.Enriched.Where(p => !exclude.Contains(p.Id)).ToList();
        var rng = Random.Shared;
        for (int i = 0; i < Math.Min(count, pool.Count); i++)
        {
            int j = rng.Next(i, pool.Count);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }
        return pool.Take(Math.Min(count, pool.Count)).ToList();
    }

    private async Task<StrandReview?> ReviewOnceAsync(
        Guid strandId, StrandMarkdownExporter.StrandExport export, Persona persona, string provider,
        bool studyMode, CancellationToken ct)
    {
        var key = ResolveKey(provider);
        if (string.IsNullOrWhiteSpace(key)) { log.LogWarning("No API key for provider {Provider}", provider); return null; }
        var model = cfg.ModelOverrides.TryGetValue(provider, out var m) && !string.IsNullOrWhiteSpace(m)
            ? m
            : LegionClient.DefaultModels.GetValueOrDefault(provider, "");

        var system = studyMode
            ? BuildStudyReviewerSystemPrompt(persona, export.Title, export.BeatCount)
            : BuildReviewerSystemPrompt(persona, export.Title);
        var maxTok = studyMode ? 2400 : 1400; // study mode also returns a per-beat score object
        var raw = await legion.CallAsync(provider, key!, model, system, export.Markdown, maxTokens: maxTok, temperature: 0.85, ct);

        int score; string reviewText; List<string> improvements;
        int? flow = null; Dictionary<int, int>? beatScores = null;
        if (studyMode)
        {
            if (!TryParseStudyReview(raw, export.BeatCount, out score, out flow, out reviewText, out improvements, out beatScores))
            {
                log.LogWarning("Unparseable study review from {Persona} via {Provider}", persona.Id, provider);
                return null;
            }
        }
        else if (!TryParseReview(raw, out score, out reviewText, out improvements))
        {
            log.LogWarning("Unparseable review from {Persona} via {Provider}: {Head}", persona.Id, provider,
                (raw ?? "").Length > 120 ? raw![..120] : raw);
            return null;
        }

        var review = new StrandReview
        {
            Id           = Guid.CreateVersion7(),
            StrandId     = strandId,
            PersonaId    = persona.Id,
            PersonaName  = persona.Name,
            PersonaBlurb = FirstLine(persona.PersonalityMarkdown),
            ProviderId   = provider,
            Model        = string.IsNullOrWhiteSpace(model) ? null : model,
            Score        = Math.Clamp(score, 1, 100),
            FlowScore    = flow.HasValue ? Math.Clamp(flow.Value, 1, 100) : null,
            ReviewText   = reviewText.Trim(),
            Improvements = improvements.Count > 0 ? string.Join("\n", improvements) : null,
            ContentHash  = export.ContentHash,
            BeatCount    = export.BeatCount,
            ReviewedAt   = DateTime.UtcNow,
            CreatedAt    = DateTime.UtcNow,
            UpdatedAt    = DateTime.UtcNow,
        };
        if (beatScores != null)
            foreach (var kv in beatScores)
                review.BeatScores.Add(new StrandReviewBeatScore { ReviewId = review.Id, BeatNumber = kv.Key, Score = kv.Value });
        return review;
    }

    /// <summary>The persona's voice + their measured psychometric profile (from the
    /// Legion package's embedded profiles), so each reviewer judges THROUGH their
    /// real personality — Openness governs tolerance for the strange/lyrical,
    /// Conscientiousness governs patience for looseness, etc. No DB: the profile is
    /// delivered by <see cref="PersonaLibrary.GetProfile"/>.</summary>
    private static string BuildWhoBlock(Persona persona)
    {
        var who = string.IsNullOrWhiteSpace(persona.PersonalityMarkdown)
            ? "You are an ordinary, opinionated reader."
            : persona.PersonalityMarkdown;

        var profile = PersonaLibrary.GetProfile(persona.Id);
        if (profile != null)
            who +=
$@"

YOUR MEASURED PSYCHOMETRIC PROFILE — let it genuinely shape what you notice, what bothers you, and how you score: {profile.Summary()}.
Read through this psychology, not a generic critic's: high Openness welcomes the strange, lyrical, and rule-breaking; low Openness wants clarity and convention. High Conscientiousness is impatient with looseness, purple prose, and unearned flourish; lower Conscientiousness forgives it for energy and feel. High Neuroticism feels stakes and dread sharply; low Neuroticism stays cool. Let your Agreeableness set how gentle or blunt your review reads. React as THIS person actually would.";
        return who;
    }

    private static string BuildStudyReviewerSystemPrompt(Persona persona, string title, int beatCount)
    {
        var who = BuildWhoBlock(persona);
        return
$@"{who}

You are reading a complete short audio-fiction story titled ""{title}"" (below), then giving structured, HONEST feedback exactly as the person described above would react. The story is split into {beatCount} numbered beats, each marked [Beat 1] through [Beat {beatCount}].

Read the WHOLE story first. Beats do NOT stand alone — judge each one for how it LANDS IN CONTEXT: its job in the sequence (a setup, a payoff, a turn, a breather, a momentum push), not its standalone flash. A quiet beat that earns a later payoff should score HIGH; a showy beat that stalls the run should score LOW.

Return ONLY a JSON object, nothing else, with exactly these fields:
- ""score"": integer 1-100 — your overall reaction as this reader. Use the whole scale; do not default to the 70s.
- ""flow"": integer 1-100 — how well the story hangs together as a SEQUENCE: momentum, setups paying off, clean transitions, no dead stretches or tonal whiplash. This is SEPARATE from how good the individual beats are — a story can have great beats and broken flow.
- ""review"": a few honest sentences in your own voice. Not flattering.
- ""improvements"": array of concrete fixes, each naming the beat number it applies to (e.g. ""Beat 19: the lore-dump kills momentum"").
- ""beat_scores"": an object rating EVERY beat 1-5 in context (1 = this beat hurt the story for me, 3 = fine, 5 = a highlight), keyed by beat number as a string, covering beats 1 through {beatCount}: {{""1"": 4, ""2"": 3, ""3"": 5}}.

Score honestly and specifically. The author wants the truth, not to be glazed.";
    }

    private static string BuildReviewerSystemPrompt(Persona persona, string title)
    {
        var who = BuildWhoBlock(persona);
        return
$@"{who}

You are reading a complete short audio-fiction story titled ""{title}"" (provided below) and writing an HONEST reader review of it, exactly as the person described above would react.

Ignore any earlier instruction to keep your answer to a sentence or two — a review needs room. Write a genuine review of a few short paragraphs, in your own voice and taste.

Be honest, NOT flattering. If it bored you, confused you, or lost you, say so and say where. Praise only what genuinely earned it. The author wants the truth, not to be glazed.

Give an overall score from 1 to 100 that reflects YOUR real reaction as this person — your taste differs from other readers, and that is the point. Use the whole scale; do not default to the 70s-80s.

Then list CONCRETE, specific ways the story could be better — point at actual moments. Cover whatever applies: grammar/typos, prose quality, dialogue, pacing, clarity of physical action, characters, the world, the ending. ""Make it better"" is useless — name the line, beat, or moment.

Return ONLY a JSON object and nothing else:
{{""score"": <integer 1-100>, ""review"": ""<your honest review>"", ""improvements"": [""<concrete fix>"", ""<concrete fix>""]}}";
    }

    /// <summary>Generate (and upsert) the Amazon-style aggregate summary for the
    /// strand's most-recent review batch.</summary>
    public async Task<StrandReviewSummary> GenerateSummaryAsync(Guid strandId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        // Use the newest content fingerprint's reviews (the latest run).
        var latestHash = await db.StrandReviews
            .Where(r => r.StrandId == strandId)
            .OrderByDescending(r => r.ReviewedAt)
            .Select(r => r.ContentHash)
            .FirstOrDefaultAsync(ct);
        var reviews = await db.StrandReviews
            .Where(r => r.StrandId == strandId && r.ContentHash == latestHash)
            .AsNoTracking()
            .ToListAsync(ct);
        if (reviews.Count == 0)
            throw new InvalidOperationException("No reviews to summarize.");

        var avg = reviews.Average(r => r.Score);
        var dist = ScoreBuckets(reviews);
        var distJson = JsonSerializer.Serialize(dist);

        var summaryMd = await SynthesizeSummaryAsync(reviews, avg, dist, ct);

        var existing = await db.StrandReviewSummaries.FirstOrDefaultAsync(s => s.StrandId == strandId, ct);
        if (existing == null)
        {
            existing = new StrandReviewSummary { Id = Guid.CreateVersion7(), StrandId = strandId };
            db.StrandReviewSummaries.Add(existing);
        }
        existing.GeneratedAt           = DateTime.UtcNow;
        existing.ReviewCount           = reviews.Count;
        existing.AvgScore              = Math.Round(avg, 1);
        existing.ScoreDistributionJson = distJson;
        existing.SummaryMarkdown       = summaryMd;
        existing.ContentHash           = latestHash;
        await db.SaveChangesAsync(ct);
        return existing;
    }

    private async Task<string> SynthesizeSummaryAsync(
        List<StrandReview> reviews, double avg, Dictionary<string, int> dist, CancellationToken ct)
    {
        // Judge provider (claude) synthesizes; fall back to any active provider.
        var judge = cfg.ActiveProviderIds.Contains(cfg.JudgeProviderId)
            ? cfg.JudgeProviderId
            : cfg.ActiveProviderIds.FirstOrDefault() ?? "claude";
        var key = ResolveKey(judge);
        if (string.IsNullOrWhiteSpace(key)) return FallbackSummary(reviews, avg, dist);
        var model = LegionClient.DefaultModels.GetValueOrDefault(judge, "");

        // Compact corpus: score + improvements + a trimmed review excerpt per row.
        var sb = new StringBuilder();
        sb.AppendLine($"{reviews.Count} reader reviews. Average score {avg:0.0}/100.");
        sb.AppendLine($"Score distribution: {string.Join(", ", dist.Select(kv => $"{kv.Key}:{kv.Value}"))}");
        sb.AppendLine();
        foreach (var r in reviews.OrderByDescending(r => r.Score))
        {
            var excerpt = r.ReviewText.Length > 360 ? r.ReviewText[..360] + "…" : r.ReviewText;
            sb.AppendLine($"- [{r.Score}] {r.PersonaName} ({r.ProviderId}): {excerpt}");
            if (!string.IsNullOrWhiteSpace(r.Improvements))
                sb.AppendLine($"    fixes: {r.Improvements.Replace("\n", " | ")}");
        }

        var system =
@"You are aggregating many reader reviews of a single story into an honest editorial summary for the author — like the summary box at the top of a book's reviews. Be candid and concrete, never flattering. The author wants to know how readers actually reacted and exactly what to fix.";
        var user =
$@"Here are the reviews (score in brackets), with each reviewer's concrete fix notes:

{sb}

Write a Markdown summary with these sections:
**What readers think** — 2-4 sentences on the overall reception and the score spread (note if opinion is divided).
**What landed** — bullet list of the strengths readers repeatedly praised.
**Top fixes (most-requested first)** — bullet list of the concrete improvements raised most often, each phrased as an actionable change with the kind of issue (grammar / prose / dialogue / pacing / clarity / characters / ending). Rank by how many reviewers raised it.
**The split** — one line on who tended to score it high vs low and why.
Be specific. Do not invent praise that the reviews do not support.";

        try
        {
            var md = await legion.CallAsync(judge, key!, model, system, user, maxTokens: 2200, temperature: 0.4, ct);
            return string.IsNullOrWhiteSpace(md) ? FallbackSummary(reviews, avg, dist) : md.Trim();
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Summary synthesis failed; using computed fallback.");
            return FallbackSummary(reviews, avg, dist);
        }
    }

    private static string FallbackSummary(List<StrandReview> reviews, double avg, Dictionary<string, int> dist)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"**What readers think** — {reviews.Count} readers, average **{avg:0.0}/100**.");
        sb.AppendLine();
        sb.AppendLine("**Score distribution**");
        foreach (var kv in dist) sb.AppendLine($"- {kv.Key}: {kv.Value}");
        return sb.ToString();
    }

    private static Dictionary<string, int> ScoreBuckets(List<StrandReview> reviews)
    {
        var buckets = new Dictionary<string, int>
        {
            ["1-20"] = 0, ["21-40"] = 0, ["41-60"] = 0, ["61-80"] = 0, ["81-100"] = 0,
        };
        foreach (var r in reviews)
        {
            var s = r.Score;
            var key = s <= 20 ? "1-20" : s <= 40 ? "21-40" : s <= 60 ? "41-60" : s <= 80 ? "61-80" : "81-100";
            buckets[key]++;
        }
        return buckets;
    }

    /// <summary>
    /// Recompute and persist latest-run scores: <see cref="Strand.Score"/> = mean of the
    /// most-recent review per persona within the newest reviewed version; each
    /// <see cref="Beat.Score"/> = the newest study run's per-beat micro-scores (mean 1-5 →
    /// percentage, latest study review per persona). "Current state," never an average of
    /// stale opinions. Called automatically after every review/study run; safe to call
    /// directly to refresh.
    /// </summary>
    public async Task RecomputeScoresAsync(Guid strandId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var strand = await db.Strands.FirstOrDefaultAsync(s => s.Id == strandId, ct);
        if (strand == null) return;

        var latestHash = await db.StrandReviews
            .Where(r => r.StrandId == strandId)
            .OrderByDescending(r => r.ReviewedAt)
            .Select(r => r.ContentHash)
            .FirstOrDefaultAsync(ct);

        if (string.IsNullOrEmpty(latestHash))
        {
            strand.Score = null; strand.ScoredAt = null;
            await db.SaveChangesAsync(ct);
            return;
        }

        var reviews = await db.StrandReviews
            .Where(r => r.StrandId == strandId && r.ContentHash == latestHash)
            .Include(r => r.BeatScores)
            .ToListAsync(ct);

        // Strand score: the FOCUS-GROUP result only (the A/B/C/D panels), latest review
        // per persona → mean overall (0-100). Study reviews use a beat-focused prompt and
        // are excluded from the headline strand score.
        var latestPerPersona = reviews
            .Where(r => r.FocusGroupName != null && r.FocusGroupName.StartsWith("Group"))
            .GroupBy(r => r.PersonaId)
            .Select(g => g.OrderByDescending(r => r.ReviewedAt).First())
            .ToList();
        strand.Score = latestPerPersona.Count > 0 ? latestPerPersona.Average(r => (double)r.Score) : (double?)null;
        strand.ScoredAt = DateTime.UtcNow;

        // Beat scores: from the study reviews (those carrying per-beat micro-scores),
        // latest study review per persona, then per beat number mean(1-5) → percentage.
        var perBeat = reviews
            .Where(r => r.BeatScores.Count > 0)
            .GroupBy(r => r.PersonaId)
            .Select(g => g.OrderByDescending(r => r.ReviewedAt).First())
            .SelectMany(r => r.BeatScores)
            .GroupBy(bs => bs.BeatNumber)
            .ToDictionary(g => g.Key, g => g.Average(x => (double)x.Score) / 5.0 * 100.0);

        if (perBeat.Count > 0)
        {
            // perBeat is keyed by POSITIONAL beat index (1..N, the order the study saw the
            // beats), NOT the global Beat.Number. Map positional → the strand's beats in
            // reading (SortKey) order.
            var ordered = await db.StrandBeats
                .Where(sb => sb.StrandId == strandId)
                .OrderBy(sb => sb.SortKey)
                .Include(sb => sb.Beat)
                .Select(sb => sb.Beat!)
                .ToListAsync(ct);
            var now = DateTime.UtcNow;
            for (int pos = 1; pos <= ordered.Count; pos++)
                if (perBeat.TryGetValue(pos, out var pct)) { ordered[pos - 1].Score = pct; ordered[pos - 1].ScoredAt = now; }
        }

        await db.SaveChangesAsync(ct);
    }

    // ── helpers ──────────────────────────────────────────────────────────

    private string? ResolveKey(string provider)
    {
        if (cfg.ApiKeys.TryGetValue(provider, out var k) && !string.IsNullOrWhiteSpace(k)) return k;
        return MindAtticCredentialStore.GetKey(provider);
    }

    /// <summary>Distinct enriched personas (real personalities, not the empty
    /// per-provider defaults), drawn without replacement.</summary>
    /// <summary>Look up a focus group by name; returns its id + member persona
    /// ids, or (null, empty) if no such group exists.</summary>
    private async Task<(Guid? id, List<string> memberIds)> GetGroupAsync(string name, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var g = await db.FocusGroups.FirstOrDefaultAsync(x => x.Name == name, ct);
        if (g == null) return (null, new List<string>());
        var ids = await db.FocusGroupMembers.Where(m => m.FocusGroupId == g.Id)
            .Select(m => m.PersonaId).ToListAsync(ct);
        return (g.Id, ids);
    }

    /// <summary>Create a named tracking panel of <paramref name="size"/> enriched
    /// personas drawn at random but DISJOINT from every existing focus group, so
    /// A/B/C/... never share a member. Fixed roster → reuse with <c>--group</c> to
    /// track the same audience over versions; multiple disjoint panels give
    /// replication (data mass → lower-variance, less-biased aggregates).</summary>
    public async Task<(Guid id, int count)> CreateDisjointGroupAsync(string name, int size, CancellationToken ct = default)
    {
        if (size <= 0) size = 128;
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        if (await db.FocusGroups.AnyAsync(g => g.Name == name, ct))
            throw new InvalidOperationException($"Focus group '{name}' already exists.");
        var used = (await db.FocusGroupMembers.Select(m => m.PersonaId).Distinct().ToListAsync(ct)).ToHashSet();
        var personas = SampleEnrichedPersonasExcluding(size, used);
        if (personas.Count == 0)
            throw new InvalidOperationException("No un-used enriched personas left to staff a new disjoint panel.");
        var gid = await CreateGroupAsync(name, personas, ct);
        return (gid, personas.Count);
    }

    /// <summary>Create a named focus group and persist its roster.</summary>
    public async Task<Guid> CreateGroupAsync(string name, List<Persona> personas, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var g = new FocusGroup { Id = Guid.CreateVersion7(), Name = name, CreatedAt = DateTime.UtcNow };
        db.FocusGroups.Add(g);
        foreach (var p in personas)
            db.FocusGroupMembers.Add(new FocusGroupMember
            {
                FocusGroupId = g.Id,
                PersonaId = p.Id,
                PersonaName = p.Name,
                PersonaBlurb = FirstLine(p.PersonalityMarkdown),
            });
        await db.SaveChangesAsync(ct);
        return g.Id;
    }

    /// <summary>Resolve enriched personas by id (used to materialize a group's
    /// roster into Persona objects for a rerun).</summary>
    public List<Persona> PersonasForIds(IReadOnlyList<string> ids) => PersonasByIds(ids);

    /// <summary>Resolve a fixed set of personas by id (focus-group rerun),
    /// preserving order and skipping any id no longer in the library.</summary>
    private static List<Persona> PersonasByIds(IReadOnlyList<string> ids)
    {
        var byId = PersonaLibrary.All.ToDictionary(p => p.Id, p => p);
        var list = new List<Persona>(ids.Count);
        foreach (var id in ids)
            if (byId.TryGetValue(id, out var p)) list.Add(p);
        return list;
    }

    private static List<Persona> SampleEnrichedPersonas(int count)
    {
        var pool = PersonaLibrary.Enriched.ToList();
        var rng = Random.Shared;
        // Fisher-Yates partial shuffle.
        for (int i = 0; i < Math.Min(count, pool.Count); i++)
        {
            int j = rng.Next(i, pool.Count);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }
        return pool.Take(Math.Min(count, pool.Count)).ToList();
    }

    private static string? FirstLine(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        var line = s.Split('\n').FirstOrDefault()?.Trim();
        return string.IsNullOrEmpty(line) ? null : (line.Length > 400 ? line[..400] : line);
    }

    /// <summary>Tolerant JSON extraction: strips code fences, isolates the first
    /// {...} object, reads score/review/improvements. Falls back to a bare
    /// "score": N scan with the whole text as the review.</summary>
    private static bool TryParseReview(string? raw, out int score, out string review, out List<string> improvements)
    {
        score = 0; review = ""; improvements = new List<string>();
        if (string.IsNullOrWhiteSpace(raw)) return false;
        var text = raw.Trim();
        // Strip ``` / ```json fences.
        if (text.StartsWith("```"))
        {
            var nl = text.IndexOf('\n');
            if (nl >= 0) text = text[(nl + 1)..];
            if (text.EndsWith("```")) text = text[..^3];
            text = text.Trim();
        }
        var open = text.IndexOf('{');
        var close = text.LastIndexOf('}');
        if (open >= 0 && close > open)
        {
            var json = text[open..(close + 1)];
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.TryGetProperty("score", out var sEl))
                {
                    if (sEl.ValueKind == JsonValueKind.Number && sEl.TryGetInt32(out var si)) score = si;
                    else if (sEl.ValueKind == JsonValueKind.String && int.TryParse(sEl.GetString(), out var ss)) score = ss;
                }
                if (root.TryGetProperty("review", out var rEl) && rEl.ValueKind == JsonValueKind.String)
                    review = rEl.GetString() ?? "";
                if (root.TryGetProperty("improvements", out var iEl))
                {
                    if (iEl.ValueKind == JsonValueKind.Array)
                        improvements = iEl.EnumerateArray()
                            .Where(x => x.ValueKind == JsonValueKind.String)
                            .Select(x => x.GetString()!.Trim())
                            .Where(x => x.Length > 0).ToList();
                    else if (iEl.ValueKind == JsonValueKind.String)
                        improvements = new List<string> { iEl.GetString()!.Trim() };
                }
                if (score > 0 && !string.IsNullOrWhiteSpace(review)) return true;
            }
            catch { /* fall through to scan */ }
        }
        // Fallback: scan for a score number, keep the raw text as the review.
        var m = System.Text.RegularExpressions.Regex.Match(text, @"score""?\s*[:=]\s*(\d{1,3})", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (m.Success && int.TryParse(m.Groups[1].Value, out var fs) && fs is >= 1 and <= 100)
        {
            score = fs;
            review = text;
            return true;
        }
        return false;
    }

    /// <summary>Study-mode parse: overall score + flow + review + improvements +
    /// the per-beat micro-score object. Tolerant of fences/preamble. Beat keys
    /// out of [1, beatCount] are dropped; scores clamped to 1-5.</summary>
    private static bool TryParseStudyReview(
        string? raw, int beatCount, out int score, out int? flow, out string review,
        out List<string> improvements, out Dictionary<int, int>? beatScores)
    {
        score = 0; flow = null; review = ""; improvements = new List<string>(); beatScores = null;
        if (string.IsNullOrWhiteSpace(raw)) return false;
        var text = raw.Trim();
        if (text.StartsWith("```"))
        {
            var nl = text.IndexOf('\n');
            if (nl >= 0) text = text[(nl + 1)..];
            if (text.EndsWith("```")) text = text[..^3];
            text = text.Trim();
        }
        var open = text.IndexOf('{');
        var close = text.LastIndexOf('}');
        if (open < 0 || close <= open) return false;
        try
        {
            using var doc = JsonDocument.Parse(text[open..(close + 1)]);
            var root = doc.RootElement;
            if (root.TryGetProperty("score", out var sEl))
            {
                if (sEl.ValueKind == JsonValueKind.Number && sEl.TryGetInt32(out var si)) score = si;
                else if (sEl.ValueKind == JsonValueKind.String && int.TryParse(sEl.GetString(), out var ss)) score = ss;
            }
            if (root.TryGetProperty("flow", out var fEl))
            {
                if (fEl.ValueKind == JsonValueKind.Number && fEl.TryGetInt32(out var fi)) flow = fi;
                else if (fEl.ValueKind == JsonValueKind.String && int.TryParse(fEl.GetString(), out var fs2)) flow = fs2;
            }
            if (root.TryGetProperty("review", out var rEl) && rEl.ValueKind == JsonValueKind.String)
                review = rEl.GetString() ?? "";
            if (root.TryGetProperty("improvements", out var iEl))
            {
                if (iEl.ValueKind == JsonValueKind.Array)
                    improvements = iEl.EnumerateArray()
                        .Where(x => x.ValueKind == JsonValueKind.String)
                        .Select(x => x.GetString()!.Trim()).Where(x => x.Length > 0).ToList();
                else if (iEl.ValueKind == JsonValueKind.String)
                    improvements = new List<string> { iEl.GetString()!.Trim() };
            }
            if (root.TryGetProperty("beat_scores", out var bEl) && bEl.ValueKind == JsonValueKind.Object)
            {
                var d = new Dictionary<int, int>();
                foreach (var p in bEl.EnumerateObject())
                {
                    if (!int.TryParse(p.Name, out var bn) || bn < 1 || bn > beatCount) continue;
                    int v;
                    if (p.Value.ValueKind == JsonValueKind.Number && p.Value.TryGetInt32(out var iv)) v = iv;
                    else if (p.Value.ValueKind == JsonValueKind.String && int.TryParse(p.Value.GetString(), out var sv)) v = sv;
                    else continue;
                    d[bn] = Math.Clamp(v, 1, 5);
                }
                if (d.Count > 0) beatScores = d;
            }
            return score > 0 && !string.IsNullOrWhiteSpace(review);
        }
        catch { return false; }
    }
}
