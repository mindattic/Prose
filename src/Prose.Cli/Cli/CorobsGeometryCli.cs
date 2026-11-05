using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Services.Corobs;

namespace Prose.Cli;

/// <summary>
/// prose --universe &lt;u&gt; --corobs enable | disable | status
///   | build [--full]
///   | patterns [--ply k] [--at &lt;story-time&gt;] [--status s] [--top N]
///   | rules [--status s] [--top N]
///   | cue [--entity &lt;id|slug|name&gt;] [--top N]
///   | confirm --pattern &lt;id&gt; --ruling &lt;id&gt; --label "&lt;≤120 chars&gt;" | confirm --rule &lt;id&gt; --ruling &lt;id&gt;
///   | reject --pattern|--rule &lt;id&gt;
///   | resolve --hit &lt;id&gt; --order|--ruling &lt;id&gt;   | dismiss --hit &lt;id&gt; --order|--ruling &lt;id&gt;
///   | explain --entity &lt;id|slug|name&gt; [--at &lt;story-time&gt;]
///   [--json]
///
/// <para>RFC 0016 (PRS-T863): COROBS, the persistent parallel Correlithm-Object geometry of a
/// universe's entities. <c>enable|disable</c> is the master switch (<c>/corobs on|off</c>); while it
/// is off every other verb answers <c>disabled</c> and writes nothing. Ids may be given whole or as
/// an unambiguous prefix (rulings are cited as <c>01a19ebf-9f09</c>). Deterministic, no LLM; writes
/// only <c>Corobs*</c> rows. The bench verb (PRS-T865) has its own handler.</para>
/// </summary>
public static class CorobsGeometryCli
{
    /// <summary>The verbs this handler owns; Program.cs forwards <c>--corobs &lt;verb&gt;</c> here for these.</summary>
    public static readonly string[] Verbs =
        ["enable", "disable", "on", "off", "status", "build", "patterns", "rules", "cue", "confirm", "reject", "resolve", "dismiss", "explain"];

    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? Flag(string name) { var i = Array.IndexOf(args, name); return i >= 0 && i + 1 < args.Length ? args[i + 1] : null; }
        int Top(int d) => int.TryParse(Flag("--top"), out var n) && n > 0 ? n : d;
        DateTime? At() => Flag("--at") is { } s && DateTime.TryParse(s, CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var d) ? d : null;
        var json = args.Contains("--json");
        var verb = Flag("--corobs");
        var sw = services.GetRequiredService<CorobsSwitch>();
        var corobs = services.GetRequiredService<CorobsService>();

        try
        {
            switch (verb)
            {
                case "enable" or "on":
                    sw.Set(true);
                    Console.WriteLine("[corobs] ON — the new way is visible again; nothing needed rebuilding.");
                    return 0;
                case "disable" or "off":
                    sw.Set(false);
                    Console.WriteLine("[corobs] OFF — every COROBS entry point now answers 'disabled'; nothing was deleted.");
                    return 0;
                case "status":
                {
                    var s = await corobs.StatusAsync();
                    if (json) { Console.WriteLine(JsonSerializer.Serialize(s, Json)); return 0; }
                    if (!s.Enabled) { Console.WriteLine("[corobs] disabled (prose --corobs enable, or /corobs on)."); return 0; }
                    if (s.SpaceId is null) { Console.WriteLine("[corobs] ON — no geometry for this universe yet: prose --corobs build."); return 0; }
                    Console.WriteLine($"[corobs] ON — space {s.SpaceId} seed {s.Seed} N={s.N} {s.SpaceStatus}, built {s.BuiltAt:u}");
                    foreach (var p in s.Plies) Console.WriteLine($"  ply {p.Key,-8} {p.Kind,-10} {p.Points,7:N0} points  {p.Entities,7:N0} entities");
                    Console.WriteLine($"  patterns {Counts(s.Patterns)}");
                    Console.WriteLine($"  rules    {Counts(s.Rules)}");
                    Console.WriteLine($"  hits     {Counts(s.Hits)}");
                    return 0;
                }
                case "build":
                {
                    var r = await corobs.BuildAsync(full: args.Contains("--full"));
                    if (json) { Console.WriteLine(JsonSerializer.Serialize(r, Json)); return 0; }
                    Console.WriteLine($"[corobs] built space {r.SpaceId} (seed {r.Seed}, N={r.N}) over {r.Entities:N0} entities in {r.Elapsed.TotalSeconds:F1}s");
                    Console.WriteLine($"  points   {r.PointsWritten:N0} written · {r.PointsUnchanged:N0} unchanged · {r.PointsDeleted:N0} dropped · {r.StalePoints:N0} were stale");
                    Console.WriteLine($"  patterns {r.PatternsNew} new · {r.PatternsKept} kept · {r.PatternsSuperseded} superseded");
                    Console.WriteLine($"  rules    {r.RulesNew} new · {r.RulesKept} kept · {r.RulesSuperseded} superseded");
                    Console.WriteLine($"  hits     {r.HitsOpened} opened · {r.HitsStillOpen} still open · {r.HitsClosedByCascade} closed by cascade");
                    return 0;
                }
                case "patterns":
                {
                    var rows = await corobs.PatternsAsync(Flag("--ply"), At(), Flag("--status"), Top(50));
                    if (json) { Console.WriteLine(JsonSerializer.Serialize(rows, Json)); return 0; }
                    if (rows.Count == 0) Console.WriteLine("[corobs] no patterns (build first, or loosen --ply/--status).");
                    foreach (var p in rows)
                        Console.WriteLine($"{p.Id}  {p.Ply,-8} {p.Status,-10} {p.Members,5} members {p.Bits,6:F1} bits  {p.Label ?? "(unlabelled)"}  e.g. {string.Join(", ", p.Sample)}");
                    return 0;
                }
                case "rules":
                {
                    var rows = await corobs.RulesAsync(Flag("--status"), Top(50));
                    if (json) { Console.WriteLine(JsonSerializer.Serialize(rows, Json)); return 0; }
                    if (rows.Count == 0) Console.WriteLine("[corobs] no rules.");
                    foreach (var r in rows)
                        Console.WriteLine($"{r.Id}  {r.Status,-10} {r.SourcePly}.[{r.SourceLabel ?? r.SourcePatternId.ToString()[..13]}] >: {r.TargetPly}.[{r.TargetLabel ?? r.TargetPatternId.ToString()[..13]}]  support {r.Support:P0}, {r.Bits:F1} bits");
                    return 0;
                }
                case "cue":
                {
                    var rows = await corobs.CueAsync(Flag("--entity"), Top(25));
                    if (json) { Console.WriteLine(JsonSerializer.Serialize(rows, Json)); return 0; }
                    if (rows.Count == 0) Console.WriteLine("[corobs] no open hits (hits come only from confirmed rules).");
                    foreach (var h in rows)
                        Console.WriteLine($"#{h.Rank,-4} {h.Id}  {h.Entity} ({h.EntityType})  {h.Bits:F1} bits  — {h.Rule}");
                    return 0;
                }
                case "confirm":
                {
                    var ruling = Flag("--ruling") ?? throw new ArgumentException("confirm needs --ruling <id> (record_ruling first).");
                    var msg = Flag("--pattern") is { } pid ? await corobs.ConfirmPatternAsync(pid, ruling, Flag("--label") ?? "")
                        : Flag("--rule") is { } rid ? await corobs.ConfirmRuleAsync(rid, ruling)
                        : throw new ArgumentException("confirm needs --pattern <id> or --rule <id>.");
                    Console.WriteLine("[corobs] " + msg);
                    return 0;
                }
                case "reject":
                    Console.WriteLine("[corobs] " + await corobs.RejectAsync(Flag("--pattern"), Flag("--rule")));
                    return 0;
                case "resolve" or "dismiss":
                {
                    var hit = Flag("--hit") ?? throw new ArgumentException($"{verb} needs --hit <id>.");
                    Console.WriteLine("[corobs] " + await corobs.ResolveHitAsync(hit, Flag("--order"), Flag("--ruling"), dismiss: verb == "dismiss"));
                    return 0;
                }
                case "explain":
                {
                    var entity = Flag("--entity") ?? throw new ArgumentException("explain needs --entity <id|slug|name>.");
                    var x = await corobs.ExplainAsync(entity, At());
                    if (json) { Console.WriteLine(JsonSerializer.Serialize(x, Json)); return 0; }
                    Console.WriteLine($"[corobs] {x.Name} ({x.Type}) {x.EntityId}");
                    foreach (var p in x.Plies)
                        Console.WriteLine(!p.HasPoint ? $"  {p.Ply,-8} — no point (field missing: zero bits)"
                            : p.NearestPatternId is null ? $"  {p.Ply,-8} point, no pattern on this ply"
                            : $"  {p.Ply,-8} {(p.Inside ? "inside " : "near   ")} {p.PatternLabel ?? p.NearestPatternId.ToString()} [{p.PatternStatus}, {p.PatternMembers} members] d={p.Distance:F2} ({p.Bits:F1} bits)");
                    Console.WriteLine(x.Trajectory.Count == 0 ? "  trajectory: no dated state changes"
                        : $"  trajectory: {x.Trajectory.Count} dated states, {x.Trajectory[0]:yyyy-MM-dd} … {x.Trajectory[^1]:yyyy-MM-dd}"
                          + (x.At is { } at ? $"; at {at:yyyy-MM-dd} the last state is {(x.TrajectoryPointAt is { } t ? t.ToString("yyyy-MM-dd") : "none yet")}" : ""));
                    foreach (var h in x.OpenHits) Console.WriteLine($"  open hit #{h.Rank} {h.Id} {h.Bits:F1} bits — {h.Rule}");
                    return 0;
                }
                default:
                    Console.Error.WriteLine("Usage: prose --universe <u> --corobs enable|disable|status|build [--full]|patterns [--ply k] [--at t] [--status s]|rules|cue [--entity e]|confirm --pattern <id> --ruling <id> --label \"…\"|confirm --rule <id> --ruling <id>|reject --pattern|--rule <id>|resolve|dismiss --hit <id> --order|--ruling <id>|explain --entity e [--at t] [--json]");
                    return 2;
            }
        }
        catch (CorobsDisabledException ex)
        {
            Console.WriteLine(json ? JsonSerializer.Serialize(new { status = "disabled" }) : "[corobs] " + ex.Message);
            return 2;
        }
        catch (ArgumentException ex)
        {
            Console.Error.WriteLine("[corobs] " + ex.Message);
            return 2;
        }
        catch (InvalidOperationException ex)
        {
            Console.Error.WriteLine("[corobs] " + ex.Message);
            return 1;
        }
    }

    private static string Counts(IReadOnlyDictionary<string, int> d) =>
        d.Count == 0 ? "none" : string.Join(" · ", d.OrderBy(kv => kv.Key, StringComparer.Ordinal).Select(kv => $"{kv.Value:N0} {kv.Key}"));
}
