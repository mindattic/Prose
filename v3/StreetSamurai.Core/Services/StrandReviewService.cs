using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
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
    private readonly SettingsService settings;
    private readonly StrandMarkdownExporter exporter;
    private readonly IDbContextFactory<StreetSamuraiDbContext> dbFactory;
    private readonly FindingsService findings;
    private readonly SemanticFidelityService fidelity;
    private readonly ILogger<StrandReviewService> log;

    private int MaxConcurrency => settings.ReviewMaxConcurrency;

    /// <summary>When set, the reviewer persona is framed as a fan of this genre
    /// instead of the default cyberpunk fandom. E.g. "cosmic horror".</summary>
    public string? GenreOverride { get; set; }

    public StrandReviewService(
        LegionClient legion,
        VotingConfiguration cfg,
        SettingsService settings,
        StrandMarkdownExporter exporter,
        IDbContextFactory<StreetSamuraiDbContext> dbFactory,
        FindingsService findings,
        SemanticFidelityService fidelity,
        ILogger<StrandReviewService> log)
    {
        this.legion = legion;
        this.cfg = cfg;
        this.settings = settings;
        this.exporter = exporter;
        this.dbFactory = dbFactory;
        this.findings = findings;
        this.fidelity = fidelity;
        this.log = log;
    }

    public record ReviewRunResult(int Requested, int Saved, int Failed, double AvgScore, string ContentHash, string ExportPath);
    public record ScoreHistoryPoint(DateTime RecordedAt, double Score, double? Sd, int ReviewCount);

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
        if (readers <= 0) readers = settings.ReviewReaders;

        var providers = ReviewProviderIds();
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

    public sealed record SampledRunResult(
        int Ballots, int BallotsSaved, int ProseAdded, int Failed,
        double MeanScore, double Sd, double Ci95, int Clusters,
        string ContentHash, string ReportMarkdown, string ExportPath);

    /// <summary>Economical default: a stratified SAMPLE of personas casts cheap
    /// score-only BALLOTS (overall + flow + per-beat 1-5 + one weakness tag), then
    /// only the most informative ballots (harshest / median / most generous) are
    /// upgraded with a full prose review. The ballots double as the segment study —
    /// clustered into emergent audiences with a Pareto/contested per-beat report —
    /// so one pass yields a tight-CI strand score, per-beat %, a complaint
    /// histogram, the decision report, AND a handful of readable reviews, at a
    /// fraction of a census run's calls.</summary>
    public async Task<SampledRunResult> RunSampledReviewAsync(
        Guid strandId, int ballotCount, int proseCount,
        IProgress<int>? progress = null, CancellationToken ct = default)
    {
        if (ballotCount <= 0) ballotCount = settings.ReviewBallots;
        if (proseCount < 0) proseCount = 0;
        var providers = ReviewProviderIds();
        if (providers.Count == 0)
            throw new InvalidOperationException("No trusted LLM providers are configured with API keys — cannot run reviews.");

        var export = await exporter.ExportAsync(strandId, numberBeats: true, ct);
        var beatCount = export.BeatCount;
        // "Group"-prefixed so the headline strand Score (RecomputeScores) counts these ballots.
        var groupName = $"Group Sample {export.ContentHash[..6]}";
        var personas = SampleEnrichedPersonas(ballotCount);

        // ── Tier 1: cheap score-only ballots (providers round-robined → even split). ──
        var sem = new SemaphoreSlim(MaxConcurrency);
        var bag = new System.Collections.Concurrent.ConcurrentBag<StrandReview>();
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
                    var r = await BallotOnceAsync(strandId, export, persona, provider, ct);
                    if (r != null) { r.FocusGroupName = groupName; bag.Add(r); }
                    else Interlocked.Increment(ref failed);
                }
                catch (Exception ex) { Interlocked.Increment(ref failed); log.LogWarning(ex, "Ballot failed: {P}", persona.Id); }
                finally { sem.Release(); progress?.Report(Interlocked.Increment(ref done)); }
            }, ct));
        }
        await Task.WhenAll(tasks);

        // ── Retry failed ballots using only the providers that proved reachable. ──
        // This replaces slots from any provider that couldn't connect (auth error,
        // network, quota) without shrinking the panel below the requested count.
        if (failed > 0 && !bag.IsEmpty)
        {
            var workingProviders = bag.Select(r => r.ProviderId).Distinct().ToList();
            var retryPersonas = SampleEnrichedPersonas(failed);
            var retryTasks = new List<Task>(failed);
            var retriesDone = 0;
            for (int i = 0; i < retryPersonas.Count; i++)
            {
                var persona = retryPersonas[i];
                var provider = workingProviders[i % workingProviders.Count];
                retryTasks.Add(Task.Run(async () =>
                {
                    await sem.WaitAsync(ct);
                    try
                    {
                        var r = await BallotOnceAsync(strandId, export, persona, provider, ct);
                        if (r != null) { r.FocusGroupName = groupName; bag.Add(r); }
                    }
                    catch (Exception ex) { log.LogWarning(ex, "Retry ballot failed: {P}", persona.Id); }
                    finally { sem.Release(); progress?.Report(Interlocked.Increment(ref retriesDone) + done); }
                }, ct));
            }
            await Task.WhenAll(retryTasks);
            failed = 0;
        }

        var saved = bag.ToList();
        if (saved.Count == 0)
            return new SampledRunResult(personas.Count, 0, 0, failed, 0, 0, 0, 0, export.ContentHash,
                "_No ballots saved — check provider API keys / connectivity._", export.Path);

        // ── Tier 2: upgrade the most informative ballots with full prose. ──
        int proseAdded = 0;
        if (proseCount > 0)
        {
            var picks = SelectInformative(saved, Math.Min(proseCount, saved.Count));
            var psem = new SemaphoreSlim(MaxConcurrency);
            var ptasks = picks.Select(rv => Task.Run(async () =>
            {
                await psem.WaitAsync(ct);
                try
                {
                    var persona = PersonasByIds(new[] { rv.PersonaId }).FirstOrDefault();
                    if (persona == null) return;
                    var prose = await ProseOnceAsync(export, persona, rv.ProviderId, ct);
                    if (prose != null)
                    {
                        rv.ReviewText = prose.Value.review.Trim();
                        if (prose.Value.improvements.Count > 0) rv.Improvements = string.Join("\n", prose.Value.improvements);
                        Interlocked.Increment(ref proseAdded);
                    }
                }
                catch (Exception ex) { log.LogWarning(ex, "Prose upgrade failed: {P}", rv.PersonaId); }
                finally { psem.Release(); }
            })).ToList();
            await Task.WhenAll(ptasks);
        }

        // ── Diagnostic: cluster the ballots' per-beat matrix → Pareto/contested report. ──
        string report = "_(per-beat report unavailable — too few ballots carried beat scores.)_";
        int clusters = 0;
        var withBeats = saved.Where(r => r.BeatScores.Count > 0).ToList();
        if (withBeats.Count >= 8)
        {
            try
            {
                var matrix = BuildMatrix(withBeats, beatCount);
                var clustering = ReviewClusterer.Cluster(matrix);
                var rows = new List<SegmentAggregator.Reviewer>(withBeats.Count);
                for (int i = 0; i < withBeats.Count; i++)
                {
                    var bs = withBeats[i].BeatScores.ToDictionary(x => x.BeatNumber, x => x.Score);
                    rows.Add(new SegmentAggregator.Reviewer(clustering.Assignments[i], withBeats[i].Score, withBeats[i].FlowScore, bs));
                }
                var agg = SegmentAggregator.Build(rows, beatCount, clustering.K);
                var labelById = agg.Clusters.ToDictionary(c => c.Id, c => c.Label);
                for (int i = 0; i < withBeats.Count; i++)
                {
                    withBeats[i].ClusterId = clustering.Assignments[i];
                    withBeats[i].ClusterLabel = labelById.TryGetValue(clustering.Assignments[i], out var lbl) ? Trunc(lbl, 60) : null;
                }
                report = agg.Markdown; clusters = clustering.K;
            }
            catch (Exception ex) { log.LogWarning(ex, "Sampled clustering failed"); }
        }

        // Persist (ballots + prose upgrades + cluster stamps), then recompute scores.
        await using (var db = await dbFactory.CreateDbContextAsync(ct))
        {
            db.StrandReviews.AddRange(saved);
            await db.SaveChangesAsync(ct);
        }
        await RecomputeScoresAsync(strandId, ct);

        var scores = saved.Select(r => (double)r.Score).ToList();
        var mean = scores.Average();
        var sd = scores.Count > 1 ? Math.Sqrt(scores.Sum(x => (x - mean) * (x - mean)) / (scores.Count - 1)) : 0.0;
        var ci = scores.Count > 1 ? 1.96 * sd / Math.Sqrt(scores.Count) : 0.0;

        return new SampledRunResult(personas.Count, saved.Count, proseAdded, failed,
            Math.Round(mean, 1), Math.Round(sd, 1), Math.Round(ci, 2), clusters,
            export.ContentHash, report, export.Path);
    }

    /// <summary>Pick the most informative ballots for prose upgrade: the harshest,
    /// the most generous, and a band around the median — a spectrum worth reading.</summary>
    private static List<StrandReview> SelectInformative(List<StrandReview> all, int k)
    {
        if (k >= all.Count) return all.ToList();
        var ordered = all.OrderBy(r => r.Score).ToList();
        int low = Math.Max(1, k * 3 / 10);
        int high = Math.Max(1, k * 3 / 10);
        int mid = Math.Max(0, k - low - high);
        var picked = new List<StrandReview>();
        picked.AddRange(ordered.Take(low));                                            // harshest
        picked.AddRange(ordered.Skip(Math.Max(low, ordered.Count - high)).Take(high)); // most generous
        if (mid > 0)
        {
            int start = Math.Clamp(ordered.Count / 2 - mid / 2, 0, Math.Max(0, ordered.Count - mid));
            picked.AddRange(ordered.Skip(start).Take(mid));                            // median band
        }
        return picked.DistinctBy(r => r.Id).Take(k).ToList();
    }

    // ── Review-driven auto-editor: weight the latest reviews, target the lowest /
    //    most-flagged beats (raise the floor), and propose a conservative rewrite of
    //    each with a before/after for an approval survey. ──────────────────────────

    public sealed record EditProposal(
        int BeatNumber, int Position, double Mean, int Flags, bool Contested, double Priority,
        IReadOnlyList<string> Addresses, string Rationale, string Before, string After);

    /// <summary>From the strand's latest review batch, score each beat's FIX-PRIORITY
    /// = floor (5 − mean) × prevalence (1 + ½·times flagged) × a modifier that favors
    /// fix-for-everyone beats (low across all clusters) and discounts contested ones,
    /// then conservatively rewrite the top <paramref name="topN"/> floor-draggers.
    /// Returns before/after proposals for an approval survey — nothing is written.</summary>
    public async Task<List<EditProposal>> ProposeEditsAsync(Guid strandId, int topN, CancellationToken ct = default)
    {
        if (topN <= 0) topN = 5;
        var providers = ReviewProviderIds();
        if (providers.Count == 0)
            throw new InvalidOperationException("No trusted LLM providers are configured with API keys — cannot edit.");
        var editProvider = providers.Contains("claude") ? "claude" : providers[0];

        var export = await exporter.ExportAsync(strandId, numberBeats: true, ct);

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var latestHash = await db.StrandReviews.Where(r => r.StrandId == strandId)
            .OrderByDescending(r => r.ReviewedAt).Select(r => r.ContentHash).FirstOrDefaultAsync(ct);
        if (string.IsNullOrEmpty(latestHash)) return new List<EditProposal>();

        var all = await db.StrandReviews
            .Where(r => r.StrandId == strandId && r.ContentHash == latestHash)
            .Include(r => r.BeatScores).ToListAsync(ct);
        var reviews = all.GroupBy(r => r.PersonaId)
            .Select(g => g.OrderByDescending(r => r.ReviewedAt).First()).ToList();
        if (reviews.Count == 0) return new List<EditProposal>();

        var ordered = await db.StrandBeats.Where(sb => sb.StrandId == strandId)
            .OrderBy(sb => sb.SortKey).Include(sb => sb.Beat).Select(sb => sb.Beat!).ToListAsync(ct);
        int n = ordered.Count;
        if (n == 0) return new List<EditProposal>();

        // Per-position aggregates (positional 1..N matches the numbered export the readers saw).
        var byPos = new Dictionary<int, List<(int cluster, int score)>>();
        foreach (var r in reviews)
            foreach (var bs in r.BeatScores)
                if (bs.BeatNumber >= 1 && bs.BeatNumber <= n)
                {
                    if (!byPos.TryGetValue(bs.BeatNumber, out var l)) { l = new(); byPos[bs.BeatNumber] = l; }
                    l.Add((r.ClusterId ?? -1, bs.Score));
                }
        var mean = new double[n + 1];
        var contested = new bool[n + 1];
        for (int p = 1; p <= n; p++)
        {
            if (byPos.TryGetValue(p, out var l) && l.Count > 0)
            {
                mean[p] = l.Average(x => x.score);
                var cm = l.Where(x => x.cluster >= 0).GroupBy(x => x.cluster).Select(g => g.Average(x => x.score)).ToList();
                if (cm.Count >= 2) contested[p] = (cm.Max() - cm.Min()) >= 1.2;
            }
            else mean[p] = 3.0;
        }

        var improvLines = reviews.Where(r => !string.IsNullOrWhiteSpace(r.Improvements))
            .SelectMany(r => r.Improvements!.Split('\n')).Select(s => s.Trim())
            .Where(s => s.Length > 0).ToList();
        var flags = new int[n + 1];
        for (int p = 1; p <= n; p++)
            flags[p] = improvLines.Count(s => Regex.IsMatch(s, $@"\bbeat\s*0*{p}\b", RegexOptions.IgnoreCase));

        double Priority(int p)
        {
            double floor = Math.Max(0, 5.0 - mean[p]);
            double prevalence = 1 + 0.5 * flags[p];
            double mod = contested[p] ? 0.8 : (mean[p] < 3.8 ? 1.4 : 1.0);
            return floor * prevalence * mod;
        }

        var candidates = Enumerable.Range(1, n)
            .Where(p => mean[p] < 4.2)               // only floor problems — leave strong beats alone
            .OrderByDescending(Priority).Take(topN).ToList();

        var globalThemes = improvLines
            .Where(s => !Regex.IsMatch(s, @"\bbeat\b", RegexOptions.IgnoreCase))
            .GroupBy(s => s.ToLowerInvariant()).OrderByDescending(g => g.Count())
            .Take(6).Select(g => g.First()).ToList();

        string Neighbors(int p)
        {
            var sb = new StringBuilder();
            if (p > 1) sb.Append($"[Beat {p - 1} — voice reference only]\n{ordered[p - 2].Text}\n\n");
            if (p < n) sb.Append($"[Beat {p + 1} — voice reference only]\n{ordered[p].Text}\n");
            return sb.ToString();
        }

        var proposals = new List<EditProposal>();
        foreach (var p in candidates)
        {
            var beat = ordered[p - 1];
            var complaints = improvLines
                .Where(s => Regex.IsMatch(s, $@"\bbeat\s*0*{p}\b", RegexOptions.IgnoreCase))
                .Distinct().Take(8).ToList();
            var edit = await EditOnceAsync(export.Title, beat.Text, p, mean[p], contested[p],
                complaints, globalThemes, Neighbors(p), editProvider, ct);
            if (edit == null) continue;
            if (string.Equals(edit.Value.after.Trim(), beat.Text.Trim(), StringComparison.Ordinal)) continue; // no-op
            proposals.Add(new EditProposal(beat.Number, p, Math.Round(mean[p], 2), flags[p], contested[p],
                Math.Round(Priority(p), 2), edit.Value.addresses, edit.Value.rationale, beat.Text, edit.Value.after));
        }
        return proposals.OrderByDescending(x => x.Priority).ToList();
    }

    private async Task<(string after, string rationale, IReadOnlyList<string> addresses)?> EditOnceAsync(
        string title, string beatText, int pos, double mean, bool contested,
        List<string> complaints, List<string> globalThemes, string neighbors, string provider, CancellationToken ct)
    {
        var key = ResolveKey(provider);
        if (string.IsNullOrWhiteSpace(key)) return null;
        var model = cfg.ModelOverrides.TryGetValue(provider, out var m) && !string.IsNullOrWhiteSpace(m)
            ? m : LegionClient.DefaultModels.GetValueOrDefault(provider, "");

        var sb = new StringBuilder();
        sb.AppendLine($"BEAT {pos} — reader score {mean:0.0}/5{(contested ? " (CONTESTED: audiences disagree — do NOT lose what one camp loves)" : "")}.");
        sb.AppendLine();
        sb.AppendLine("CURRENT TEXT:");
        sb.AppendLine(beatText);
        sb.AppendLine();
        if (complaints.Count > 0)
        {
            sb.AppendLine("WHAT READERS SAID ABOUT THIS BEAT:");
            foreach (var c in complaints) sb.AppendLine("- " + c);
            sb.AppendLine();
        }
        else if (globalThemes.Count > 0)
        {
            sb.AppendLine("OVERALL READER GRIPES (no beat-specific note — apply ONLY if they genuinely fit this beat):");
            foreach (var t in globalThemes) sb.AppendLine("- " + t);
            sb.AppendLine();
        }
        if (!string.IsNullOrWhiteSpace(neighbors))
        {
            sb.AppendLine("NEIGHBORING BEATS (match this exact voice; do NOT edit or repeat them):");
            sb.AppendLine(neighbors);
        }
        sb.AppendLine($"Revise BEAT {pos} per your rules — the smallest change that fixes the complaints. Return ONLY the JSON.");

        var raw = await legion.CallAsync(provider, key!, model, BuildEditorSystemPrompt(title), sb.ToString(),
            maxTokens: 1600, temperature: 0.6, ct);
        return TryParseEdit(raw);
    }

    private static string BuildEditorSystemPrompt(string title) =>
$@"You are the developmental line-editor for a hard-edged near-future cyberpunk audio-fiction series. You revise ONE beat at a time to widen its appeal WITHOUT betraying the author's voice. The story is ""{title}"".

VOICE: dry, controlled, witty-under-pressure; the protagonist Kyle is unflappable and audacity is the punchline. Match the neighboring beats exactly.

HARD RULES — a violation makes your edit unusable:
1. Do NOT invent plot, characters, capabilities, or world facts. Re-render only what is already there.
2. PRESERVE signature lines. Vivid, voice-defining phrasings and earned character beats stay VERBATIM — change the connective tissue around them, never the keepers. When in doubt, keep the line.
3. NO filler-wit: never a wry universal-truth aside (e.g. ""X does not, in fact, enjoy Y""). Every sentence must reveal character, raise stakes, or land a real joke. Kill on-the-nose theme-explaining and title-drops.
4. Canon terms are exact: the in-head computer is the ""Neuretics"" (NEVER ""lattice""); the reality-warp phenomenon is ""The Weather""; the currency symbol is Φ.
5. Prefer SHORTER. Cut drag, repetition, and over-narration. Add a clause of grounding ONLY where readers were genuinely confused about the physical action.
6. CONSERVATIVE: make the smallest change that addresses the complaints. If the beat is already fine, return it nearly unchanged. Keep roughly the same length unless cutting drag.

Return ONLY a JSON object, nothing else:
{{""after"": ""<the revised beat, full text>"", ""rationale"": ""<one sentence: what you changed and why>"", ""addresses"": [""<the complaint this fixes>"", ...]}}";

    private static (string after, string rationale, IReadOnlyList<string> addresses)? TryParseEdit(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
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
        if (open < 0 || close <= open) return null;
        try
        {
            using var doc = JsonDocument.Parse(text[open..(close + 1)]);
            var root = doc.RootElement;
            if (!root.TryGetProperty("after", out var aEl) || aEl.ValueKind != JsonValueKind.String) return null;
            var after = aEl.GetString() ?? "";
            if (string.IsNullOrWhiteSpace(after)) return null;
            var rationale = root.TryGetProperty("rationale", out var rEl) && rEl.ValueKind == JsonValueKind.String
                ? rEl.GetString()!.Trim() : "";
            var addresses = new List<string>();
            if (root.TryGetProperty("addresses", out var adEl) && adEl.ValueKind == JsonValueKind.Array)
                addresses = adEl.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.String)
                    .Select(x => x.GetString()!.Trim()).Where(x => x.Length > 0).ToList();
            return (after.Trim(), rationale, addresses);
        }
        catch { return null; }
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
        if (panelSize <= 0) panelSize = settings.ReviewPanel;
        var providers = ReviewProviderIds();
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
        // study mode also returns a per-beat score object — budget grows with beat count
        var maxTok = studyMode ? Math.Min(8000, Math.Max(2400, 900 + export.BeatCount * 6)) : 1400;
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

    /// <summary>One cheap SCORE-ONLY ballot: overall + flow + per-beat 1-5 + a single
    /// weakness tag, no prose paragraph. The wide-net scoring/per-beat tier.</summary>
    private async Task<StrandReview?> BallotOnceAsync(
        Guid strandId, StrandMarkdownExporter.StrandExport export, Persona persona, string provider, CancellationToken ct)
    {
        var key = ResolveKey(provider);
        if (string.IsNullOrWhiteSpace(key)) { log.LogWarning("No API key for provider {Provider}", provider); return null; }
        var model = cfg.ModelOverrides.TryGetValue(provider, out var m) && !string.IsNullOrWhiteSpace(m)
            ? m : LegionClient.DefaultModels.GetValueOrDefault(provider, "");

        var system = BuildBallotSystemPrompt(persona, export.Title, export.BeatCount);
        // beat_scores must cover every beat — the JSON grows with beat count, so the
        // output budget must too (a 535-beat book strand needs ~4k tokens of ballot).
        var maxTok = Math.Min(8000, 900 + export.BeatCount * 6);
        var raw = await legion.CallAsync(provider, key!, model, system, export.Markdown, maxTokens: maxTok, temperature: 0.85, ct);
        if (!TryParseBallot(raw, export.BeatCount, out var score, out var flow, out var weakness, out var beatScores))
        {
            log.LogWarning("Unparseable ballot from {Persona} via {Provider}", persona.Id, provider);
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
            ReviewText   = "",
            Improvements = string.IsNullOrWhiteSpace(weakness) ? null : weakness.Trim(),
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

    /// <summary>Full prose review for an already-balloted persona — used to upgrade
    /// the most informative ballots with readable text (returns text only).</summary>
    private async Task<(string review, List<string> improvements)?> ProseOnceAsync(
        StrandMarkdownExporter.StrandExport export, Persona persona, string provider, CancellationToken ct)
    {
        var key = ResolveKey(provider);
        if (string.IsNullOrWhiteSpace(key)) return null;
        var model = cfg.ModelOverrides.TryGetValue(provider, out var m) && !string.IsNullOrWhiteSpace(m)
            ? m : LegionClient.DefaultModels.GetValueOrDefault(provider, "");
        var system = BuildReviewerSystemPrompt(persona, export.Title);
        var raw = await legion.CallAsync(provider, key!, model, system, export.Markdown, maxTokens: 1400, temperature: 0.85, ct);
        return TryParseReview(raw, out _, out var review, out var improvements) ? (review, improvements) : null;
    }

    private string BuildBallotSystemPrompt(Persona persona, string title, int beatCount)
    {
        var who = BuildWhoBlock(persona);
        return
$@"{who}

You are reading a complete short audio-fiction story titled ""{title}"" (below), split into {beatCount} numbered beats, [Beat 1] through [Beat {beatCount}]. Read the WHOLE thing as the person above, then cast a quick SCORING BALLOT — no prose review, just the numbers and one gripe.

Judge each beat for how it LANDS IN CONTEXT (its job in the sequence), not its standalone flash.

Return ONLY a JSON object, nothing else:
- ""score"": integer 1-100 — your overall reaction as this reader. Use the WHOLE scale; do not default to the 70s.
- ""flow"": integer 1-100 — how well it hangs together as a sequence (momentum, payoffs, transitions), separate from beat quality.
- ""weakness"": your single biggest gripe in EIGHT WORDS OR FEWER (e.g. ""ending drags"", ""kid's dialogue too writerly"", ""jargon overload""), or ""none"".
- ""beat_scores"": rate EVERY beat 1-5 in context (1 = hurt the story, 3 = fine, 5 = highlight), keyed by beat number 1..{beatCount}: {{""1"":4,""2"":3}}.

Be honest and use the whole scale.";
    }

    /// <summary>The persona's voice + their measured psychometric profile (from the
    /// Legion package's embedded profiles), so each reviewer judges THROUGH their
    /// real personality — Openness governs tolerance for the strange/lyrical,
    /// Conscientiousness governs patience for looseness, etc. No DB: the profile is
    /// delivered by <see cref="PersonaLibrary.GetProfile"/>.</summary>
    private string BuildWhoBlock(Persona persona)
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

        var genre = GenreOverride?.Trim();
        if (string.IsNullOrWhiteSpace(genre))
        {
            // Default: die-hard cyberpunk fan (user ruling 2026-06-10).
            who +=
$@"

ONE MORE THING ABOUT YOU, layered on top of everything above: you are a DIE-HARD cyberpunk fan. You have read Neuromancer, Count Zero, Snow Crash, The Diamond Age, and Hardwired more times than you can count, and you can quote The Matrix and Johnny Mnemonic from memory. You picked this story up BECAUSE it is cyberpunk, you hold it to the standard of those classics, and you know the difference between earned tech-noir — concrete, propulsive, witty — and imitation mood-soup that performs profundity without containing any. Your psychometric profile shapes HOW you read; this fandom shapes WHAT you measure the story against.";
        }
        else
        {
            who += BuildGenreFanBlock(genre);
        }
        return who;
    }

    private static string BuildGenreFanBlock(string genre) => genre.ToLowerInvariant() switch
    {
        "cosmic horror" or "lovecraftian" =>
$@"

ONE MORE THING ABOUT YOU, layered on top of everything above: you are a devotee of COSMIC HORROR. You have read Lovecraft, Thomas Ligotti, Laird Barron, John Langan, and Jeff VanderMeer. You understand the genre's central premise — that the universe is vast, indifferent, and contains presences for which human minds were not designed — and you hold fiction to that standard. You are not frightened by monsters; you are frightened by the realisation that something has been looking at you from outside a window and the only question is how long. You reward stories that make the dread structural (woven into the mechanism, not decorating it), that treat the incomprehensible as incomprehensible (no explanations that collapse the horror), and that give the reader the feeling of being studied rather than threatened. Your psychometric profile shapes HOW you read; this fandom shapes WHAT you measure the story against.",

        _ =>
$@"

ONE MORE THING ABOUT YOU, layered on top of everything above: you are a passionate {genre} fan with deep genre literacy. You picked this story up as a {genre} reader, you hold it to the standards of the best the genre has produced, and you know the difference between the real thing and an imitation. Your psychometric profile shapes HOW you read; this fandom shapes WHAT you measure the story against."
    };

    private string BuildStudyReviewerSystemPrompt(Persona persona, string title, int beatCount)
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

    private string BuildReviewerSystemPrompt(Persona persona, string title)
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
        // Judge provider synthesizes; fall back to any active provider.
        var judgeId = settings.ReviewJudgeProvider;
        var judge = cfg.ActiveProviderIds.Contains(judgeId)
            ? judgeId
            : cfg.ActiveProviderIds.FirstOrDefault() ?? "claude";
        var key = ResolveKey(judge);
        if (string.IsNullOrWhiteSpace(key)) return FallbackSummary(reviews, avg, dist);
        var model = LegionClient.DefaultModels.GetValueOrDefault(judge, "");

        // Corpus: score distribution + a gripe TALLY (so the synopsis can calibrate
        // many/some/a few honestly) + the full-prose reviews (for specific, quotable
        // observations a single reader made).
        var tagCounts = reviews
            .Where(r => !string.IsNullOrWhiteSpace(r.Improvements))
            .SelectMany(r => r.Improvements!.Split('\n'))
            .Select(s => s.Trim()).Where(s => s.Length > 0)
            .GroupBy(s => s.ToLowerInvariant())
            .Select(g => (tag: g.First(), n: g.Count()))
            .OrderByDescending(x => x.n).Take(14).ToList();
        var prose = reviews.Where(r => !string.IsNullOrWhiteSpace(r.ReviewText))
            .OrderByDescending(r => r.Score).ToList();

        var sb = new StringBuilder();
        sb.AppendLine($"{reviews.Count} reader reviews. Average score {avg:0.0}/100.");
        sb.AppendLine($"Score distribution: {string.Join(", ", dist.Select(kv => $"{kv.Key}:{kv.Value}"))}");
        sb.AppendLine();
        if (tagCounts.Count > 0)
        {
            sb.AppendLine("MOST-MENTIONED POINTS (point : how many readers raised it) — calibrate many/some/a few from these:");
            foreach (var (tag, nn) in tagCounts) sb.AppendLine($"- ({nn}×) {tag}");
            sb.AppendLine();
        }
        sb.AppendLine($"FULL REVIEWS ({prose.Count} read in depth — mine these for specific, quotable observations):");
        foreach (var r in prose)
        {
            var excerpt = r.ReviewText.Length > 500 ? r.ReviewText[..500] + "…" : r.ReviewText;
            sb.AppendLine($"- [{r.Score}] {r.PersonaName} ({r.ProviderId}): {excerpt}");
            if (!string.IsNullOrWhiteSpace(r.Improvements))
                sb.AppendLine($"    notes: {r.Improvements.Replace("\n", " | ")}");
        }

        var system =
@"You generate the AI review-synopsis that sits atop a work's reviews — the ""Customers say"" box, but for fiction and addressed to the author. You read ALL the reader reviews and distill them into a short, natural, conversational synopsis. Attribute strictly by prevalence: ""Readers find…"", ""Many…"", ""Several mention…"", ""A few…"", ""At least one reader noted…"". Weave in one or two SPECIFIC concrete observations an individual reviewer made (credited generically, e.g. ""at least one reader noted that…""), not only generalities. Be candid and never flattering — invent no praise the reviews do not support.";
        var user =
$@"Reviews (score in brackets) with each reviewer's notes, plus the prevalence tally:

{sb}

Write a Markdown summary, leading with the synopsis:
**Readers say** — a flowing 4–7 sentence synopsis in the ""customers say"" register (prose, NOT bullets): open with the overall reaction calibrated to the score spread; then the recurring themes hedged by how many raised them (most-mentioned first, using many/some/several/a few to match the tally); and fold in at least one SPECIFIC concrete observation a reader made (e.g. ""at least one reader noted that steel doesn't…""). Honest, conversational, concrete.
**What landed** — bullets: strengths readers repeatedly praised.
**Top fixes (most-requested first)** — bullets ranked by prevalence, each an actionable change tagged by issue type (grammar / prose / dialogue / pacing / clarity / characters / ending).
**The split** — one line on who scored it high vs low and why.
Be specific; do not invent praise the reviews don't support.";

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

        // Remember the score before this recompute so we can detect a strand
        // crossing the 80% "winner" threshold and auto-flag it for a voice harvest.
        var previousScore = strand.Score;

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

        // Append a score-history row so we can chart score evolution over time.
        if (strand.Score.HasValue)
        {
            var mean = strand.Score.Value;
            double? sd = latestPerPersona.Count > 1
                ? Math.Sqrt(latestPerPersona.Sum(r => Math.Pow((double)r.Score - mean, 2)) / latestPerPersona.Count)
                : null;
            var beatCount = await db.StrandBeats.CountAsync(sb => sb.StrandId == strandId && sb.IsEnabled, ct);
            db.StrandScoreHistories.Add(new Data.Entities.StrandScoreHistory
            {
                StrandId    = strandId,
                RecordedAt  = strand.ScoredAt ?? DateTime.UtcNow,
                ContentHash = latestHash,
                MeanScore   = mean,
                Sd          = sd,
                ReviewCount = latestPerPersona.Count,
                BeatCount   = beatCount,
            });
        }

        await db.SaveChangesAsync(ct);

        // Auto-flag a freshly-crowned winner (crossed <80 → ≥80) for a voice
        // harvest. Lightweight on purpose — it raises a VOICE-HARVEST finding for
        // visibility; the actual (LLM-heavy) harvest runs on demand via
        // `ss --harvest-voice`, keeping the review loop cheap.
        if ((previousScore ?? 0) < 80 && (strand.Score ?? 0) >= 80)
        {
            try
            {
                findings.Upsert(
                    filePath:     $"strand:{strand.Slug}",
                    chapterId:    null,
                    category:     FindingCategory.Voice,
                    severity:     FindingSeverity.Medium,
                    summary:      $"VOICE-HARVEST: \"{strand.Title}\" reached {strand.Score:0.#}% — harvest its voice into the rules ( ss --harvest-voice --slug {strand.Slug} ).",
                    snippet:      null,
                    suggestedFix: "Run the voice harvest, then approve the proposed rules.");
                log.LogInformation("Strand {Slug} crossed 80% ({Score:0.#}) — raised VOICE-HARVEST finding.", strand.Slug, strand.Score);
            }
            catch (Exception ex) { log.LogWarning(ex, "Failed to raise VOICE-HARVEST finding for {Slug}", strand.Slug); }
        }

        // Auto-trigger semantic fidelity audit whenever the strand scores above the
        // gaming threshold. Fire-and-forget — doesn't block the review response.
        // Drift-skipped embeddings keep the cost near zero on unchanged beats.
        if ((strand.Score ?? 0) >= SemanticFidelityService.ScoreGamingThreshold)
        {
            var capturedId = strandId;
            _ = Task.Run(async () =>
            {
                try { await fidelity.AuditStrandAsync(capturedId, CancellationToken.None); }
                catch (Exception ex) { log.LogWarning(ex, "Background fidelity audit failed for strand {Id}", capturedId); }
            });
        }
    }

    // ── Score history (for charting) ─────────────────────────────────────

    /// <summary>
    /// Returns the score timeline for a strand.
    /// For parent strands (books), aggregates child histories by day.
    /// </summary>
    public async Task<List<ScoreHistoryPoint>> GetScoreHistoryAsync(Guid strandId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var childIds = await db.Strands
            .Where(s => s.ParentStrandId == strandId)
            .Select(s => s.Id)
            .ToListAsync(ct);

        if (childIds.Count == 0)
        {
            return await db.StrandScoreHistories
                .Where(h => h.StrandId == strandId)
                .OrderBy(h => h.RecordedAt)
                .Select(h => new ScoreHistoryPoint(h.RecordedAt, h.MeanScore, h.Sd, h.ReviewCount))
                .ToListAsync(ct);
        }

        // Parent strand: per-day weighted average across all children.
        var rows = await db.StrandScoreHistories
            .Where(h => childIds.Contains(h.StrandId))
            .OrderBy(h => h.RecordedAt)
            .ToListAsync(ct);

        return rows
            .GroupBy(h => h.RecordedAt.Date)
            .Select(g =>
            {
                var perChild = g.GroupBy(h => h.StrandId)
                                .Select(sg => sg.OrderByDescending(h => h.RecordedAt).First())
                                .ToList();
                return new ScoreHistoryPoint(
                    RecordedAt:  g.Key,
                    Score:       perChild.Average(h => h.MeanScore),
                    Sd:          null,
                    ReviewCount: perChild.Sum(h => h.ReviewCount));
            })
            .OrderBy(p => p.RecordedAt)
            .ToList();
    }

    // ── helpers ──────────────────────────────────────────────────────────

    private string? ResolveKey(string provider)
    {
        if (cfg.ApiKeys.TryGetValue(provider, out var k) && !string.IsNullOrWhiteSpace(k)) return k;
        return MindAtticCredentialStore.GetKey(provider);
    }

    /// <summary>Providers used for reviews — all active trusted providers (Claude,
    /// OpenAI, DeepSeek, Gemini), round-robined for model + temperament diversity.
    /// (Single chokepoint: narrow this here if a provider ever needs excluding.)</summary>
    private List<string> ReviewProviderIds()
    {
        var active = cfg.ActiveProviderIds;
        var allowed = new HashSet<string>(
            settings.ReviewAllowedProviders
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            StringComparer.OrdinalIgnoreCase);
        return allowed.Count > 0 ? active.Where(p => allowed.Contains(p)).ToList() : active;
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

    /// <summary>Ballot parse: overall score + flow + one weakness tag + the per-beat
    /// micro-score object. No prose review expected. Tolerant of fences/preamble.</summary>
    private static bool TryParseBallot(
        string? raw, int beatCount, out int score, out int? flow, out string weakness, out Dictionary<int, int>? beatScores)
    {
        score = 0; flow = null; weakness = ""; beatScores = null;
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
                else if (fEl.ValueKind == JsonValueKind.String && int.TryParse(fEl.GetString(), out var fs)) flow = fs;
            }
            if (root.TryGetProperty("weakness", out var wEl) && wEl.ValueKind == JsonValueKind.String)
                weakness = wEl.GetString() ?? "";
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
            return score > 0;
        }
        catch { return false; }
    }
}
