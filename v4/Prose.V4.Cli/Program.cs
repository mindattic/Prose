using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;
using Prose.Core.Extensions;
using Prose.Core.Services;
using Prose.V4.Core.Ledger;
using Prose.V4.Core.Orchestration;
using Prose.V4.Core.Window;

Console.InputEncoding = System.Text.Encoding.UTF8;
Console.OutputEncoding = System.Text.Encoding.UTF8;

// Phase 0 acceptance-test CLI (v4 plan). Standalone, local DB access — see the .csproj comment
// for why this deliberately does NOT go through HubCliClient's Stage C forwarding.
string Flag(string[] a, string name) { var i = Array.IndexOf(a, name); return i >= 0 && i + 1 < a.Length ? a[i + 1] : ""; }

if (args.Length == 0 || args[0] is "-h" or "--help")
{
    Console.WriteLine("""
        prose-v4 <verb> [options]

        Verbs:
          ledger-query --node <slug|guid> --beat <guid>
              Print the OnScreenSnapshot (StoryStateQuery) for one beat.

          window-query --node <slug|guid> --beat <guid> [--size N (default 15)]
              Print the SceneWindowService window (verbatim prior beats) before one beat.

          sample --node <slug|guid> [--count N (default 10)] [--size N (default 15)]
              Phase 0 acceptance test: runs ledger-query + window-query on N beats spread evenly
              across the book, for manual review (the plan's own acceptance test: confirm zero
              fabricated facts by inspection).

          preview-generate --node <slug|guid> --after-beat <guid> --goal "<text>"
              [--characters <guid>[,<guid>...]] [--pov "<name>"] [--location "<name>"] [--size N]
              Phase 1: assembles the real window (chapter-blind) + on-screen facts for the given
              characters, calls the LLM ONCE, and PRINTS the result. Never writes to the database
              or any book — this is a preview only, by design (see BeatWriteOrchestrator's doc
              comment). Prints the exact prompt block lengths and the chapter span the window
              covers, so a chapter-boundary reset (v3's bug) would be visibly absent here.
        """);
    return;
}

var host = Host.CreateDefaultBuilder(args)
    .ConfigureLogging(lb => lb.ClearProviders()) // quiet — this is a read-only diagnostic CLI
    .ConfigureServices((_, svc) =>
    {
        svc.AddProseServices();
        svc.AddSingleton<StoryStateQuery>();
        svc.AddSingleton<SceneWindowService>();
        svc.AddSingleton<BeatWriteOrchestrator>();
    })
    .Build();

var services = host.Services;
var verb = args[0];

switch (verb)
{
    case "ledger-query":
    {
        var nodeRef = Flag(args, "--node");
        var beatArg = Flag(args, "--beat");
        if (!Guid.TryParse(beatArg, out var beatId)) { Console.Error.WriteLine("--beat <guid> is required."); return; }

        await using var db0 = await services.GetRequiredService<IDbContextFactory<ProseDbContext>>().CreateDbContextAsync();
        var nodeId = string.IsNullOrWhiteSpace(nodeRef) ? await ResolveBookRootForBeatAsync(db0, beatId) : await NodeRefResolver.ResolveAsync(db0, nodeRef);
        if (nodeId is null) { Console.Error.WriteLine($"Could not resolve --node '{nodeRef}' (or the book root for this beat)."); return; }

        var snapshot = await services.GetRequiredService<StoryStateQuery>().GetOnScreenFactsAsync(nodeId.Value, beatId);
        PrintSnapshot(beatId, snapshot);
        break;
    }
    case "window-query":
    {
        var nodeRef = Flag(args, "--node");
        var beatArg = Flag(args, "--beat");
        var sizeArg = Flag(args, "--size");
        var size = int.TryParse(sizeArg, out var s) ? s : 15;
        if (!Guid.TryParse(beatArg, out var beatId)) { Console.Error.WriteLine("--beat <guid> is required."); return; }

        await using var db0 = await services.GetRequiredService<IDbContextFactory<ProseDbContext>>().CreateDbContextAsync();
        var nodeId = await NodeRefResolver.ResolveAsync(db0, nodeRef);
        if (nodeId is null) { Console.Error.WriteLine($"Could not resolve --node '{nodeRef}'."); return; }

        var window = await services.GetRequiredService<SceneWindowService>().GetWindowAsync(nodeId.Value, beatId, size);
        PrintWindow(beatId, size, window);
        break;
    }
    case "sample":
    {
        var nodeRef = Flag(args, "--node");
        var countArg = Flag(args, "--count");
        var sizeArg = Flag(args, "--size");
        var count = int.TryParse(countArg, out var c) ? c : 10;
        var size = int.TryParse(sizeArg, out var s) ? s : 15;

        await using var db0 = await services.GetRequiredService<IDbContextFactory<ProseDbContext>>().CreateDbContextAsync();
        var nodeId = await NodeRefResolver.ResolveAsync(db0, nodeRef);
        if (nodeId is null) { Console.Error.WriteLine($"Could not resolve --node '{nodeRef}'."); return; }

        var workbench = services.GetRequiredService<NodeWorkbenchService>();
        var ordered = await workbench.GetOrderedBeatsAsync(nodeId.Value);
        if (ordered.Count == 0) { Console.Error.WriteLine("No beats found under this node."); return; }

        var storyState = services.GetRequiredService<StoryStateQuery>();
        var windowSvc = services.GetRequiredService<SceneWindowService>();

        var step = Math.Max(1, ordered.Count / count);
        var sampled = ordered.Where((_, i) => i % step == 0).Take(count).ToList();

        Console.WriteLine($"[sample] {sampled.Count} beats sampled from {ordered.Count} total (step {step}).");
        foreach (var o in sampled)
        {
            Console.WriteLine();
            Console.WriteLine($"══════════ beat {o.Beat.Id} (pos {o.Beat.StoryPosition?.ToString() ?? "?"}) ══════════");
            Console.WriteLine($"TEXT: {Truncate(o.Beat.Text, 200)}");

            var snapshot = await storyState.GetOnScreenFactsAsync(nodeId.Value, o.Beat.Id);
            PrintSnapshot(o.Beat.Id, snapshot, indent: "  ");

            var window = await windowSvc.GetWindowAsync(nodeId.Value, o.Beat.Id, size);
            Console.WriteLine($"  WINDOW: {window.Count} beat(s), spanning {(window.Count > 0 ? window[0].ChapterTitle : "-")}"
                + (window.Count > 0 && window[0].ChapterTitle != window[^1].ChapterTitle ? $" .. {window[^1].ChapterTitle}" : ""));
        }
        break;
    }
    case "preview-generate":
    {
        var nodeRef = Flag(args, "--node");
        var afterArg = Flag(args, "--after-beat");
        var goal = Flag(args, "--goal");
        var charsArg = Flag(args, "--characters");
        var pov = Flag(args, "--pov");
        var location = Flag(args, "--location");
        var sizeArg = Flag(args, "--size");
        var size = int.TryParse(sizeArg, out var s) ? s : 15;

        if (!Guid.TryParse(afterArg, out var afterBeatId)) { Console.Error.WriteLine("--after-beat <guid> is required."); return; }
        if (string.IsNullOrWhiteSpace(goal)) { Console.Error.WriteLine("--goal \"<text>\" is required."); return; }

        await using var db0 = await services.GetRequiredService<IDbContextFactory<ProseDbContext>>().CreateDbContextAsync();
        var nodeId = await NodeRefResolver.ResolveAsync(db0, nodeRef);
        if (nodeId is null) { Console.Error.WriteLine($"Could not resolve --node '{nodeRef}'."); return; }

        var afterBeat = await db0.Beats.AsNoTracking().Where(b => b.Id == afterBeatId).Select(b => b.StoryPosition).FirstOrDefaultAsync();
        var asOf = afterBeat ?? int.MaxValue;

        var characterIds = string.IsNullOrWhiteSpace(charsArg)
            ? []
            : charsArg.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(Guid.Parse).ToList();

        // Same GLMZ default line v3's BeatGeneratorService.UniverseLine() uses when no other
        // universe is wired — kept byte-identical deliberately, this isn't the thing under test.
        const string universeLine = "You are writing a beat in a literary cyberpunk scene set in GLMZ (Great Lakes Metropolitan Zone, 2226).";

        var orchestrator = services.GetRequiredService<BeatWriteOrchestrator>();
        Console.WriteLine("[preview-generate] Calling the LLM once — this is a real, billed call. Nothing will be saved.");
        var result = await orchestrator.PreviewGenerateAsync(
            nodeId.Value, afterBeatId, characterIds, asOf, pov, location, goal, universeLine, size);

        Console.WriteLine();
        Console.WriteLine("── PROMPT BLOCK LENGTHS (chars) ──");
        foreach (var (k, v) in result.Prompt.BlockLengths) Console.WriteLine($"  {k,-12} {v}");
        Console.WriteLine();
        Console.WriteLine("── FULL USER PROMPT SENT TO THE MODEL ──");
        Console.WriteLine(result.Prompt.User);
        Console.WriteLine();
        Console.WriteLine("── GENERATED TEXT (NOT SAVED) ──");
        Console.WriteLine(result.GeneratedText);
        break;
    }
    default:
        Console.Error.WriteLine($"Unknown verb '{verb}'. Run with --help.");
        break;
}

static void PrintSnapshot(Guid beatId, OnScreenSnapshot snapshot, string indent = "")
{
    Console.WriteLine($"{indent}[ledger-query] beat {beatId}: {snapshot.EntityIds.Count} on-screen entit{(snapshot.EntityIds.Count == 1 ? "y" : "ies")}");
    if (snapshot.IsEmpty) { Console.WriteLine($"{indent}  (no BeatEntityMentions/place for this beat)"); return; }

    Console.WriteLine($"{indent}  STATE LEDGER ({snapshot.EntityStateFacts.Count}):");
    foreach (var f in snapshot.EntityStateFacts)
        Console.WriteLine($"{indent}    {f.EntityName,-20} {f.Predicate,-24} = {Truncate(f.Value, 60)}  [{f.Source}]");

    Console.WriteLine($"{indent}  CONTINUITY CLAIMS ({snapshot.ContinuityFacts.Count}):");
    foreach (var f in snapshot.ContinuityFacts)
        Console.WriteLine($"{indent}    {f.EntityName,-20} {f.Predicate,-24} = {Truncate(f.Value, 60)}  [{f.Source}]");

    Console.WriteLine($"{indent}  OPEN OBLIGATIONS ({snapshot.OpenObligations.Count}):");
    foreach (var ob in snapshot.OpenObligations)
        Console.WriteLine($"{indent}    [{ob.Kind}] {ob.State}: {Truncate(ob.Description, 80)}");
}

static void PrintWindow(Guid beatId, int size, IReadOnlyList<WindowedBeat> window)
{
    Console.WriteLine($"[window-query] {window.Count} beat(s) before {beatId} (requested size {size}):");
    foreach (var w in window)
        Console.WriteLine($"  [{w.ChapterTitle}] pos {w.StoryPosition?.ToString() ?? "?"}  {Truncate(w.Text, 100)}");
}

static string Truncate(string s, int n) => string.IsNullOrEmpty(s) ? "" : (s.Length <= n ? s : s[..n] + "…");

static async Task<Guid?> ResolveBookRootForBeatAsync(ProseDbContext db, Guid beatId)
{
    var nodeId = await db.BeatNodes.AsNoTracking().Where(bn => bn.BeatId == beatId).Select(bn => (Guid?)bn.NodeId).FirstOrDefaultAsync();
    if (nodeId is null) return null;
    var walk = nodeId.Value;
    for (var depth = 0; depth < 10; depth++)
    {
        var node = await db.Nodes.IgnoreQueryFilters().AsNoTracking().Where(n => n.Id == walk).Select(n => new { n.Kind, n.ParentNodeId }).FirstOrDefaultAsync();
        if (node is null) return walk;
        if (node.Kind == "book") return walk;
        if (node.ParentNodeId is null) return walk;
        walk = node.ParentNodeId.Value;
    }
    return walk;
}
