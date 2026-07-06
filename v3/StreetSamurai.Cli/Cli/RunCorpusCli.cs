using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Cli;

/// <summary>
/// <c>ss --run-corpus --count N</c> — autonomous end-to-end pipeline:
///
///   For each of N nodes (or resume from a prior run):
///     1. Create node + planned beats (NodeBibleService)
///     2. Expand every beat to prose (BeatGeneratorService)
///     3. Reflow prose (ProseReflowService — mechanical punctuation only)
///     4. Validate against canon (CanonContradictionService)
///     5. Review with a sampled reader panel (NodeReviewService)
///     6. Harvest voice rules if score ≥80% (VoiceHarvestService)
///
///   Progress is checkpointed to <c>ss-corpus-run.json</c> in the working
///   directory after each stage so the run can be resumed after a crash.
///
/// Args:
///   --count N        Number of nodes to generate (required unless --resume).
///   --seed "..."     Seed prompt for every node. Default: "A night-shift
///                    freelancer takes a job that escalates into something personal."
///   --kind K         Node kind tag. Default: "episode".
///   --beats N        Target beat count per node. Default: 12.
///   --ballots N      Review ballot count per node. Default: 20.
///   --resume         Resume the run described in ss-corpus-run.json.
///   --dry-run        Print what would be done without calling LLMs.
///
/// Exit codes:
///   0 — at least one node completed the full pipeline.
///   1 — bad args, no nodes completed, or fatal error.
/// </summary>
public static class RunCorpusCli
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private const string CheckpointFile = "ss-corpus-run.json";

    // ── Checkpoint model ────────────────────────────────────────────────────

    private sealed class RunState
    {
        public int Target { get; set; }
        public string StartedAt { get; set; } = "";
        public string Seed { get; set; } = "";
        public string Kind { get; set; } = "episode";
        public int Beats { get; set; } = 12;
        public List<NodeEntry> Nodes { get; set; } = new();
    }

    private sealed class NodeEntry
    {
        public Guid NodeId { get; set; }
        public string Slug { get; set; } = "";
        public string Title { get; set; } = "";
        // Stages: created | expanded | reflowed | validated | reviewed | harvested | failed
        public string Stage { get; set; } = "created";
        public double? ReviewScore { get; set; }
        public int ContradictionCount { get; set; }
        public int HarvestProposals { get; set; }
        public string? Error { get; set; }
    }

    // ── Entry point ─────────────────────────────────────────────────────────

    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        // Parse args
        int count = 0, beats = 12, ballots = 20;
        string seed = "A night-shift freelancer takes a job that escalates into something personal.";
        string kind = "episode";
        bool resume = args.Contains("--resume");
        bool dryRun = args.Contains("--dry-run");
        bool allowVotes = args.Contains("--allow-votes");
        var votingGate = services.GetRequiredService<VotingGate>();
        bool votingAllowed = votingGate.IsAllowed(allowVotes);

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--count":   if (i + 1 < args.Length && int.TryParse(args[++i], out var c)) count = c; break;
                case "--seed":    if (i + 1 < args.Length) seed = args[++i]; break;
                case "--kind":    if (i + 1 < args.Length) kind = args[++i]; break;
                case "--beats":   if (i + 1 < args.Length && int.TryParse(args[++i], out var b)) beats = b; break;
                case "--ballots": if (i + 1 < args.Length && int.TryParse(args[++i], out var bl)) ballots = bl; break;
            }
        }

        // Load or create run state
        RunState state;
        if (resume && File.Exists(CheckpointFile))
        {
            var raw = await File.ReadAllTextAsync(CheckpointFile);
            state = JsonSerializer.Deserialize<RunState>(raw, JsonOpts) ?? new RunState();
            Console.WriteLine($"[run-corpus] Resuming run from {CheckpointFile} — {state.Nodes.Count}/{state.Target} nodes previously recorded.");
        }
        else
        {
            if (count <= 0)
            {
                Console.Error.WriteLine("[run-corpus] --count N is required (or --resume to continue a prior run).");
                Console.Error.WriteLine("Usage: ss --run-corpus --count N [--seed \"...\"] [--kind episode] [--beats 12] [--ballots 20] [--resume] [--dry-run]");
                return 1;
            }
            state = new RunState
            {
                Target = count,
                StartedAt = DateTime.UtcNow.ToString("o"),
                Seed = seed,
                Kind = kind,
                Beats = beats,
            };
            SaveCheckpoint(state);
        }

        if (dryRun)
        {
            Console.WriteLine($"[run-corpus] DRY-RUN: would generate {state.Target} nodes");
            Console.WriteLine($"  seed:    {state.Seed}");
            Console.WriteLine($"  kind:    {state.Kind}");
            Console.WriteLine($"  beats:   {state.Beats}");
            Console.WriteLine($"  ballots: {ballots}");
            var remaining = state.Target - state.Nodes.Count(s => s.Stage is "reviewed" or "harvested");
            Console.WriteLine($"  remaining: {remaining}");
            return 0;
        }

        // Resolve services
        var dbFactory  = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();
        var bibleService  = services.GetRequiredService<NodeBibleService>();
        var router     = services.GetRequiredService<ProseWriterRouter>();
        var workbench  = services.GetRequiredService<NodeWorkbenchService>();
        var reflow     = services.GetRequiredService<ProseReflowService>();
        var checker    = services.GetRequiredService<CanonContradictionService>();
        var reviewer   = services.GetRequiredService<NodeReviewService>();
        var harvest    = services.GetRequiredService<VoiceHarvestService>();
        var canonDb    = services.GetRequiredService<IDatabaseService>();

        string storyBible;
        try { storyBible = canonDb.GetLiteraryRulesPrompt() ?? ""; }
        catch { storyBible = ""; }

        // Determine how many more nodes to create
        int completed = state.Nodes.Count(s => s.Stage is "reviewed" or "harvested");
        int needed = state.Target - completed;
        Console.WriteLine($"[run-corpus] Target: {state.Target}  Completed: {completed}  Remaining: {needed}");

        // Create any nodes that still need to be generated
        int toCreate = state.Target - state.Nodes.Count;
        for (int n = 0; n < toCreate; n++)
        {
            Console.WriteLine($"[run-corpus] Creating node {state.Nodes.Count + 1}/{state.Target}…");
            var entry = await CreateNodeAsync(dbFactory, bibleService, state.Seed, state.Kind, state.Beats);
            state.Nodes.Add(entry);
            SaveCheckpoint(state);
        }

        // Pipeline each node through the remaining stages
        foreach (var entry in state.Nodes)
        {
            if (entry.Stage is "reviewed" or "harvested") continue;

            Console.WriteLine();
            Console.WriteLine($"[run-corpus] ── {entry.Title} ({entry.Slug}) stage={entry.Stage} ──");

            // Stage 2: expand beats
            if (entry.Stage == "created")
            {
                Console.WriteLine($"[run-corpus]   expand beats…");
                try
                {
                    await ExpandBeatsAsync(entry.NodeId, storyBible, router, workbench);
                    entry.Stage = "expanded";
                    SaveCheckpoint(state);
                    Console.WriteLine($"[run-corpus]   expanded.");
                }
                catch (Exception ex)
                {
                    entry.Stage = "failed";
                    entry.Error = $"expand: {ex.Message}";
                    SaveCheckpoint(state);
                    Console.Error.WriteLine($"[run-corpus]   expand failed: {ex.Message}");
                    continue;
                }
            }

            // Stage 3: reflow
            if (entry.Stage == "expanded")
            {
                Console.WriteLine($"[run-corpus]   reflow…");
                try
                {
                    var rr = await reflow.ReflowNodeAsync(entry.NodeId, apply: true);
                    entry.Stage = "reflowed";
                    SaveCheckpoint(state);
                    Console.WriteLine($"[run-corpus]   reflow: {rr.Changed}/{rr.Total} beats updated ({rr.Rejected} rejected, {rr.Errors} errors).");
                }
                catch (Exception ex)
                {
                    // Reflow is non-fatal — log and continue
                    entry.Stage = "reflowed";
                    SaveCheckpoint(state);
                    Console.WriteLine($"[run-corpus]   reflow failed (continuing): {ex.Message}");
                }
            }

            // Stage 4: validate
            if (entry.Stage == "reflowed")
            {
                Console.WriteLine($"[run-corpus]   check-canon…");
                try
                {
                    var vr = await checker.CheckNodeAsync(entry.NodeId, proposeFixes: true);
                    entry.ContradictionCount = vr.Contradictions.Count;
                    entry.Stage = "validated";
                    SaveCheckpoint(state);
                    Console.WriteLine($"[run-corpus]   validated: {vr.ChunksChecked} chunk(s) → {vr.Contradictions.Count} contradiction(s) queued.");
                }
                catch (Exception ex)
                {
                    entry.Stage = "validated";
                    SaveCheckpoint(state);
                    Console.WriteLine($"[run-corpus]   check-canon failed (continuing): {ex.Message}");
                }
            }

            // Stage 5: review (SS-A44 — score panel disabled by default; skip gracefully)
            if (entry.Stage == "validated" && !votingAllowed)
            {
                Console.WriteLine("[run-corpus]   review skipped: voting disabled by default (SS-A44). Pass --allow-votes to score the corpus.");
                entry.ReviewScore = null;
                entry.Stage = "reviewed";
                SaveCheckpoint(state);
            }
            if (entry.Stage == "validated")
            {
                Console.WriteLine($"[run-corpus]   review ({ballots} ballots)…");
                try
                {
                    var bp = new Progress<int>(k => { if (k % 5 == 0 || k == ballots) Console.WriteLine($"   …{k}/{ballots}"); });
                    var rv = await reviewer.RunSampledReviewAsync(entry.NodeId, ballots, proseCount: 0, bp, allowVotes: allowVotes);
                    entry.ReviewScore = rv.MeanScore;
                    entry.Stage = "reviewed";
                    SaveCheckpoint(state);
                    Console.WriteLine($"[run-corpus]   reviewed: {rv.MeanScore:0.0}/100 (SD {rv.Sd:0.0}, {rv.BallotsSaved}/{rv.Ballots} saved).");
                }
                catch (Exception ex)
                {
                    entry.Stage = "failed";
                    entry.Error = $"review: {ex.Message}";
                    SaveCheckpoint(state);
                    Console.Error.WriteLine($"[run-corpus]   review failed: {ex.Message}");
                    continue;
                }
            }

            // Stage 6: harvest voice (only for nodes ≥80%)
            if (entry.Stage == "reviewed" && (entry.ReviewScore ?? 0) >= 80.0)
            {
                Console.WriteLine($"[run-corpus]   harvest voice (score {entry.ReviewScore:0.0} ≥ 80)…");
                try
                {
                    var hr = await harvest.HarvestNodeAsync(entry.NodeId, force: false);
                    entry.HarvestProposals = hr.Proposals.Count;
                    entry.Stage = "harvested";
                    SaveCheckpoint(state);
                    Console.WriteLine($"[run-corpus]   harvested: {hr.Proposals.Count} rule proposal(s) queued (review at /voice or --harvest-voice --pending).");
                }
                catch (Exception ex)
                {
                    // Below-80 throws from HarvestNodeAsync — mark as reviewed (no harvest), not failed
                    entry.Stage = "harvested";
                    entry.HarvestProposals = 0;
                    SaveCheckpoint(state);
                    Console.WriteLine($"[run-corpus]   harvest skipped: {ex.Message}");
                }
            }
            else if (entry.Stage == "reviewed")
            {
                entry.Stage = "harvested";
                SaveCheckpoint(state);
                Console.WriteLine($"[run-corpus]   harvest skipped (score {entry.ReviewScore:0.#}/100 < 80).");
            }
        }

        // ── Final report ────────────────────────────────────────────────────
        Console.WriteLine();
        Console.WriteLine("═══════════════════════════════════════════════════════════");
        Console.WriteLine($"[run-corpus] RUN COMPLETE");
        Console.WriteLine($"  Target:        {state.Target}");

        var done       = state.Nodes.Where(s => s.Stage is "reviewed" or "harvested").ToList();
        var failed     = state.Nodes.Where(s => s.Stage == "failed").ToList();
        var harvested  = state.Nodes.Where(s => s.Stage == "harvested" && s.HarvestProposals > 0).ToList();
        var totalContra = state.Nodes.Sum(s => s.ContradictionCount);
        var totalProp   = state.Nodes.Sum(s => s.HarvestProposals);

        Console.WriteLine($"  Completed:     {done.Count}/{state.Target}");
        Console.WriteLine($"  Failed:        {failed.Count}");

        if (done.Count > 0)
        {
            var avgScore = done.Average(s => s.ReviewScore ?? 0);
            Console.WriteLine($"  Avg score:     {avgScore:0.0}/100");
            Console.WriteLine($"  ≥80% count:    {done.Count(s => (s.ReviewScore ?? 0) >= 80)}");
        }

        Console.WriteLine($"  Contradictions:{totalContra} queued → review at /findings or --findings");
        Console.WriteLine($"  Voice props:   {totalProp} queued → review at /voice or --harvest-voice --pending");
        Console.WriteLine();

        Console.WriteLine("  Node scores:");
        foreach (var s in done.OrderByDescending(s => s.ReviewScore ?? 0))
            Console.WriteLine($"    {(s.ReviewScore ?? 0):0.0}  {s.Slug}  ({s.ContradictionCount} contradictions, {s.HarvestProposals} voice props)");

        if (failed.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("  Failed nodes:");
            foreach (var s in failed)
                Console.WriteLine($"    {s.Slug}  [{s.Stage}] {s.Error}");
            Console.WriteLine("  Rerun with --resume to retry failed nodes.");
        }

        Console.WriteLine("═══════════════════════════════════════════════════════════");
        Console.WriteLine($"  Checkpoint: {Path.GetFullPath(CheckpointFile)}");

        return done.Count > 0 ? 0 : 1;
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static async Task<NodeEntry> CreateNodeAsync(
        IDbContextFactory<StreetSamuraiDbContext> dbFactory,
        NodeBibleService bibleService,
        string seed, string kind, int beats)
    {
        var nodeId = Guid.CreateVersion7();
        var words = seed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var working = string.Join(" ", words.Take(8)) + (words.Length > 8 ? "…" : "");
        var slug = EpisodeGeneratorService.Slugify(working) + "-" + nodeId.ToString("N")[..8];

        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var node = NodeFactory.Create(kind);
            node.Id        = nodeId;
            node.Title     = working;
            node.Slug      = slug;
            node.Seed      = seed;
            node.Status    = "draft";
            node.Synopsis  = seed.Length > 200 ? seed[..200] : seed;
            node.CreatedAt = DateTime.UtcNow;
            node.UpdatedAt = DateTime.UtcNow;
            db.Nodes.Add(node);
            await db.SaveChangesAsync();
        }

        await bibleService.GenerateAndSaveAsync(nodeId, seed, working, beats);

        // Re-read to get the final title (bible generation may update it)
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var s = await db.Nodes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == nodeId);
            var title = s?.Title ?? working;
            var finalSlug = s?.Slug ?? slug;
            Console.WriteLine($"[run-corpus]   created: \"{title}\" ({finalSlug})");
            return new NodeEntry { NodeId = nodeId, Slug = finalSlug, Title = title, Stage = "created" };
        }
    }

    private static async Task ExpandBeatsAsync(
        Guid nodeId,
        string storyBible,
        ProseWriterRouter router,
        NodeWorkbenchService workbench)
    {
        var ordered = await workbench.GetOrderedBeatsAsync(nodeId);
        var sceneSoFar = "";
        int expanded = 0;
        int beatIndex = 0;

        foreach (var ob in ordered)
        {
            var beat = ob.Beat;
            // Skip beats that already have prose
            if (!string.IsNullOrWhiteSpace(beat.Text)) { sceneSoFar += "\n\n" + beat.Text; beatIndex++; continue; }
            var goal = beat.Synopsis ?? beat.BeatTitle ?? $"Beat {beat.Number}";
            if (string.IsNullOrWhiteSpace(goal)) { beatIndex++; continue; }

            var ctx = new BeatContext
            {
                NodeId          = nodeId,
                StoryBibleContext = storyBible,
                SceneSoFar        = sceneSoFar.Length > 6000 ? sceneSoFar[^6000..] : sceneSoFar,
                BeatGoal          = goal,
                Subtext           = beat.Subtext ?? "",
            };

            var prose = await router.WriteAsync(ctx, beat.Id, beatIndex, ordered.Count);
            if (string.IsNullOrWhiteSpace(prose)) { beatIndex++; continue; }

            prose = prose.Trim();
            await workbench.UpdateBeatTextAsync(beat.Id, prose, expectedUpdatedAt: null);
            sceneSoFar += "\n\n" + prose;
            expanded++;
            beatIndex++;
        }

        Console.WriteLine($"[run-corpus]     {expanded}/{ordered.Count} beats expanded.");
    }

    private static void SaveCheckpoint(RunState state)
    {
        var json = JsonSerializer.Serialize(state, JsonOpts);
        File.WriteAllText(CheckpointFile, json);
    }
}
