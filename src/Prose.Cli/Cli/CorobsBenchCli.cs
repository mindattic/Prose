using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;
using Prose.Core.Services.Audit;
using Prose.Core.Services.Corobs;

namespace Prose.Cli;

/// <summary>
/// prose --universe &lt;u&gt; --corobs bench [--holdout 0.2] [--seed N] [--k 10] [--n 64] [--min-co-beats 2]
///   [--max-share 0.02] [--min-bits 13] [--no-graph-ply] [--node &lt;book&gt;] [--dry-run]
///   [--baseline-surfaced N --baseline-confirmed N --baseline-fixes N --baseline-cascade N --baseline-source &lt;text&gt;]
///
/// <para>RFC 0016 §6.7, PRS-T865: the old way (the Edge graph) against the new way (Corobs
/// proximity) on interconnectivity — hold-out recall against <c>BeatEntityMention</c>
/// co-occurrence, reach, contradictions surfaced/confirmed, cascade yield and story pull per
/// <c>event</c> — same entities, same seed, same scoring (<see cref="CorobsBenchMath"/>). Read-only
/// on the world; appends two <c>InstrumentRun</c> rows (<c>Instrument = corobs-bench</c>), one per
/// way, on the anchor node (<c>--node</c>, else the universe's first canon book by timeline).
/// Run 1 passes the hand-pass numbers as the old way's contradiction baseline.</para>
///
/// <para>Only <c>--corobs bench</c> dispatches here; the other <c>--corobs</c> verbs (enable, build,
/// patterns, rules, cue, confirm, resolve, explain) belong to the COROBS store (PRS-T863).</para>
/// </summary>
public static class CorobsBenchCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? Flag(string name) { var i = Array.IndexOf(args, name); return i >= 0 && i + 1 < args.Length ? args[i + 1] : null; }
        var verb = Flag("--corobs");
        if (verb != "bench")
        {
            Console.Error.WriteLine(verb is null
                ? "Usage: prose --universe <u> --corobs bench [--holdout f] [--seed n] [--k n] [--n dims] [--min-co-beats n] [--max-share f] [--min-bits b] [--no-graph-ply] [--node <book>] [--dry-run] [--baseline-surfaced n --baseline-confirmed n --baseline-fixes n --baseline-cascade n --baseline-source <text>]"
                : $"[corobs] '{verb}' is not a bench verb.");
            return 2;
        }
        var universe = Flag("--universe") ?? Environment.GetEnvironmentVariable("PROSE_UNIVERSE");
        if (string.IsNullOrWhiteSpace(universe)) { Console.Error.WriteLine("[corobs] --universe <slug> is required."); return 2; }

        CorobsBenchOptions o;
        CorobsHandBaseline? baseline = null;
        try
        {
            var config = services.GetService<IConfiguration>();
            var configSeed = long.TryParse(config?["Corobs:Seed"], NumberStyles.Integer, CultureInfo.InvariantCulture, out var cs) ? cs : 1L;
            var d = new CorobsBenchOptions(Seed: configSeed);
            o = d with
            {
                Holdout = Dbl("--holdout") ?? d.Holdout,
                Seed = Lng("--seed") ?? d.Seed,
                K = Int("--k") ?? d.K,
                Dimensions = Int("--n") ?? d.Dimensions,
                MinCoBeats = Int("--min-co-beats") ?? d.MinCoBeats,
                MaxShare = Dbl("--max-share") ?? d.MaxShare,
                MinBits = Dbl("--min-bits") ?? d.MinBits,
                GraphPly = !args.Contains("--no-graph-ply"),
            };
            if (o.Holdout is <= 0 or >= 1) throw new ArgumentException("--holdout must lie strictly between 0 and 1.");
            if (o.K < 1 || o.Dimensions < 20 || o.MinCoBeats < 1) throw new ArgumentException("--k ≥ 1, --n ≥ 20 and --min-co-beats ≥ 1.");
            var bs = new[] { Int("--baseline-surfaced"), Int("--baseline-confirmed"), Int("--baseline-fixes"), Int("--baseline-cascade") };
            if (bs.Any(v => v is not null))
            {
                if (bs.Any(v => v is null)) throw new ArgumentException("a hand baseline needs all four of --baseline-surfaced, --baseline-confirmed, --baseline-fixes, --baseline-cascade.");
                baseline = new CorobsHandBaseline(bs[0]!.Value, bs[1]!.Value, bs[2]!.Value, bs[3]!.Value, Flag("--baseline-source"));
            }
        }
        catch (Exception ex) when (ex is ArgumentException or FormatException or OverflowException)
        {
            Console.Error.WriteLine($"[corobs] {ex.Message}");
            return 2;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();
        var u = await db.Universes.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(x => x.Slug == universe);
        if (u is null) { Console.Error.WriteLine($"[corobs] universe not found: {universe}"); return 1; }

        Node? anchor;
        var nodeRef = Flag("--node");
        if (nodeRef is not null)
        {
            anchor = await NodeRefResolver.ResolveNodeAsync(db, nodeRef);
            if (anchor is null) { Console.Error.WriteLine(NodeRefResolver.NotFoundMessage(nodeRef)); return 1; }
            if (anchor.UniverseId != u.Id) { Console.Error.WriteLine($"[corobs] {nodeRef} is not in universe {universe}."); return 1; }
        }
        else
        {
            anchor = await db.Nodes.IgnoreQueryFilters().AsNoTracking().OfType<BookNode>()
                .Where(n => n.UniverseId == u.Id && n.IsCanon && !n.IsArchived)
                .OrderBy(n => n.Timeline).ThenBy(n => n.Title).Cast<Node>().FirstOrDefaultAsync();
        }

        var input = await new CorobsBenchService(dbFactory).LoadAsync(u.Id);
        Console.WriteLine($"[corobs] bench {u.Slug}: {input.Entities.Count} entities, {input.Edges.Count} live edges, {input.Mentions.Count} beat mentions; seed {o.Seed}.");
        if (input.Entities.Count == 0) { Console.Error.WriteLine("[corobs] COULD NOT LOOK — the universe has no entities."); return 1; }

        var report = CorobsBenchMath.Run(input, o, baseline);
        Console.WriteLine();
        Console.WriteLine($"# COROBS bench — {u.Name} ({u.Slug.ToUpperInvariant()}), {DateTime.UtcNow:yyyy-MM-dd HH:mm}Z");
        Console.WriteLine();
        Console.WriteLine($"_{report.Entities} entities · {report.TruthPairs} true pairs (tagged together in ≥{o.MinCoBeats} beats; {report.BeatsUsed} beats used, {report.CrowdBeatsSkipped} crowd beats >{o.MaxBeatCast} skipped) · " +
                          $"{report.Plies} plies, {report.TokensKept} shared tokens kept · k={o.K}, N={o.Dimensions}, hold-out {o.Holdout:0.##}, min {o.MinBits:0.#} bits, max share {o.MaxShare:0.###}, graph ply {(o.GraphPly ? "on" : "off")}._");
        Console.WriteLine();
        Console.WriteLine(CorobsBenchMath.Table(report));
        if (report.TruthPairs == 0) Console.WriteLine("COULD NOT LOOK — no true pairs: the universe has no beat co-occurrence at this threshold.");

        if (args.Contains("--dry-run")) { Console.WriteLine("[corobs] --dry-run: no InstrumentRun rows written."); return 0; }
        if (anchor is null) { Console.Error.WriteLine("[corobs] no canon book to anchor the InstrumentRun rows on; pass --node <book>."); return 1; }

        var ledger = services.GetRequiredService<InstrumentRunLedger>();
        foreach (var way in new[] { report.Old, report.New })
            await ledger.RecordAsync(anchor.Id, InstrumentRunLedger.CorobsBench,
                itemsExamined: report.TruthPairs == 0 ? 0 : report.Entities, itemsTotal: report.Entities,
                detail: CorobsBenchMath.Detail(u.Slug, report, way));
        Console.WriteLine($"[corobs] 2 InstrumentRun rows (corobs-bench) on {anchor.Title} ({anchor.NodeCode ?? anchor.Slug}).");
        return 0;

        double? Dbl(string f) => Flag(f) is { } s ? double.Parse(s, NumberStyles.Float, CultureInfo.InvariantCulture) : null;
        int? Int(string f) => Flag(f) is { } s ? int.Parse(s, NumberStyles.Integer, CultureInfo.InvariantCulture) : null;
        long? Lng(string f) => Flag(f) is { } s ? long.Parse(s, NumberStyles.Integer, CultureInfo.InvariantCulture) : null;
    }
}
