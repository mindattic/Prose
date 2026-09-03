using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;
using Prose.Core.Services.Audit;

namespace Prose.Cli;

/// <summary>
/// prose --exclusion-rules [--all] [--json]
/// prose --exclusion-rules --propose --predicate-a &lt;p&gt; --predicate-b &lt;p&gt; --why "..." [--object-a "x|y"] [--object-b "x|y"] [--universal]
/// prose --exclusion-rules --approve --id &lt;n&gt;
/// prose --exclusion-rules --reject  --id &lt;n&gt;
/// prose --exclusion-rules --test --predicate-a &lt;p&gt; --object-a "..." --predicate-b &lt;p&gt; --object-b "..."
///
/// Management surface for the <see cref="PredicateExclusion"/> ontology the Tuned Read runs on.
///
/// <para><b>This is the "learned" half of the design, and it is deliberately human-driven.</b>
/// The plan called for the adjudicator to propose new axioms automatically after confirming a
/// contradiction. Built as specified, that proposal has no signal: every contradiction the Tuned
/// Read confirms was found BY an existing axiom, so the generalization it would "learn" is one
/// already declared. The genuine learning moment is a human reading a finding — or a missed
/// defect — and naming the rule that was absent. So <c>--propose</c> is the entry point, and
/// approval is always an explicit act: a self-approving rule generator would let one confident
/// wrong verdict widen into a corpus-wide false-positive source, which is the exact failure mode
/// this project has hit repeatedly.</para>
///
/// <para><c>--test</c> exists so a rule can be checked against a hypothetical claim pair before
/// it is approved — the cheapest possible way to find out an axiom is too broad.</para>
/// </summary>
public static class ExclusionRulesCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var dbFactory  = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        var universes  = services.GetRequiredService<IUniverseContext>();
        var exclusions = services.GetRequiredService<PredicateExclusionService>();

        if (args.Contains("--test")) return Test(args);
        if (args.Contains("--propose")) return await ProposeAsync(args, universes, exclusions);
        if (args.Contains("--approve")) return await SetStatusAsync(args, dbFactory, "active");
        if (args.Contains("--reject")) return await SetStatusAsync(args, dbFactory, "rejected");

        return await ListAsync(args, dbFactory, universes);
    }

    // ── list ─────────────────────────────────────────────────────────────────

    private static async Task<int> ListAsync(
        string[] args, IDbContextFactory<ProseDbContext> dbFactory, IUniverseContext universes)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var current = universes.CurrentId;

        // Not universe-query-filtered (see ProseDbContext's note on this table): UniverseId
        // Guid.Empty legitimately means "every universe", and the standard filter would hide
        // exactly those rows whenever a scope is set.
        var q = db.PredicateExclusions.AsNoTracking();
        if (!args.Contains("--all"))
            q = q.Where(r => r.UniverseId == current || r.UniverseId == Guid.Empty);

        var rows = await q.OrderBy(r => r.UniverseId).ThenBy(r => r.Id).ToListAsync();
        var slugById = universes.ListUniverses().ToDictionary(u => u.Id, u => u.Slug);

        if (args.Contains("--json"))
        {
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(
                rows, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            return 0;
        }

        Console.WriteLine($"{rows.Count} exclusion axiom(s)" +
                          (args.Contains("--all") ? " (all universes)" : $" in scope for '{universes.CurrentSlug}' (+ universal)"));
        Console.WriteLine(new string('-', 100));
        foreach (var r in rows)
        {
            var scope = r.UniverseId == Guid.Empty ? "*ALL*" : slugById.GetValueOrDefault(r.UniverseId, "?");
            Console.WriteLine($"#{r.Id,-4} [{r.Status,-8}] {r.Source,-7} {scope,-6} " +
                              $"{Describe(r.PredicateA, r.ObjectPatternA)}  ⊥  {Describe(r.PredicateB, r.ObjectPatternB)}");
            Console.WriteLine($"        {r.Rationale}");
        }

        var proposed = rows.Count(r => r.Status == "proposed");
        if (proposed > 0)
            Console.WriteLine($"\n{proposed} axiom(s) awaiting approval — they generate nothing until approved " +
                              "(prose --exclusion-rules --approve --id <n>).");
        return 0;
    }

    private static string Describe(string predicate, string? pattern) =>
        string.IsNullOrWhiteSpace(pattern) ? predicate : $"{predicate}[{Clip(pattern, 46)}]";

    // ── propose ──────────────────────────────────────────────────────────────

    private static async Task<int> ProposeAsync(
        string[] args, IUniverseContext universes, PredicateExclusionService exclusions)
    {
        var pa = Flag(args, "--predicate-a");
        var pb = Flag(args, "--predicate-b");
        var why = Flag(args, "--why") ?? Flag(args, "--rationale");
        if (string.IsNullOrWhiteSpace(pa) || string.IsNullOrWhiteSpace(pb) || string.IsNullOrWhiteSpace(why))
        {
            Console.Error.WriteLine(
                "Usage: prose --exclusion-rules --propose --predicate-a <p> --predicate-b <p> --why \"one sentence\"\n" +
                "              [--object-a \"alt1|alt2\"] [--object-b \"alt1|alt2\"] [--universal]\n" +
                "  --why is required: it is what you will read when deciding whether to approve this,\n" +
                "        and what the finding quotes when the axiom fires.");
            return 2;
        }

        // --universal means Guid.Empty (a logical axiom true in every universe). Default is the
        // current universe, because most axioms are canon facts about one world.
        var universeId = args.Contains("--universal") ? Guid.Empty : universes.CurrentId;

        var row = await exclusions.ProposeLearnedRuleAsync(
            universeId, pa!.Trim(), Flag(args, "--object-a"), pb!.Trim(), Flag(args, "--object-b"), why!.Trim());

        if (row == null)
        {
            Console.Error.WriteLine(
                "[exclusion-rules] Not created. Either an axiom of this exact shape already exists in some " +
                "status (including 'rejected', which is never re-raised), or the two predicates are the same " +
                "(same-predicate conflicts are already ContinuityService.Upsert's job). " +
                "Run prose --exclusion-rules --all to see what is there.");
            return 1;
        }

        Console.WriteLine($"Proposed axiom #{row.Id} ({(universeId == Guid.Empty ? "all universes" : universes.CurrentSlug)}):");
        Console.WriteLine($"  {Describe(row.PredicateA, row.ObjectPatternA)}  ⊥  {Describe(row.PredicateB, row.ObjectPatternB)}");
        Console.WriteLine($"  {row.Rationale}");
        Console.WriteLine();
        Console.WriteLine("It is INERT until approved. Check it first against a real pair:");
        Console.WriteLine($"  prose --exclusion-rules --test --predicate-a {row.PredicateA} --object-a \"...\" " +
                          $"--predicate-b {row.PredicateB} --object-b \"...\"");
        Console.WriteLine($"  prose --exclusion-rules --approve --id {row.Id}");
        return 0;
    }

    // ── approve / reject ─────────────────────────────────────────────────────

    private static async Task<int> SetStatusAsync(
        string[] args, IDbContextFactory<ProseDbContext> dbFactory, string status)
    {
        if (!int.TryParse(Flag(args, "--id"), out var id))
        {
            Console.Error.WriteLine("[exclusion-rules] --approve/--reject requires --id <n> (see the list output).");
            return 2;
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        var row = await db.PredicateExclusions.FirstOrDefaultAsync(r => r.Id == id);
        if (row == null)
        {
            Console.Error.WriteLine($"[exclusion-rules] No axiom #{id}.");
            return 2;
        }

        if (row.Source == "builtin" && status == "rejected")
        {
            // Not refused — a builtin can be wrong, and the author overrides the engine. Just
            // said out loud, because a rejected builtin is a permanent local divergence from
            // what ships.
            Console.WriteLine($"[exclusion-rules] Note: #{id} is a BUILTIN logical axiom. Rejecting it is allowed " +
                              "but permanent for this database — say why in a commit if you do.");
        }

        row.Status = status;
        row.ApprovedAt = DateTime.UtcNow;
        row.ApprovedBy = Environment.UserName;
        await db.SaveChangesAsync();

        Console.WriteLine($"Axiom #{id} is now '{status}'.");
        Console.WriteLine($"  {Describe(row.PredicateA, row.ObjectPatternA)}  ⊥  {Describe(row.PredicateB, row.ObjectPatternB)}");
        if (status == "active")
            Console.WriteLine("  Run prose --tuned-read --slug <slug> --dry to see how many candidates it generates BEFORE paying to adjudicate them.");
        return 0;
    }

    // ── test ─────────────────────────────────────────────────────────────────

    /// <summary>Checks a hypothetical claim pair against a hypothetical rule, with no DB write
    /// and no LLM call — purely <see cref="PredicateExclusionService.Matches"/>. The point is to
    /// discover that a pattern is too broad (or silently never matches) before approving it.</summary>
    private static int Test(string[] args)
    {
        var pa = Flag(args, "--predicate-a");
        var pb = Flag(args, "--predicate-b");
        if (string.IsNullOrWhiteSpace(pa) || string.IsNullOrWhiteSpace(pb))
        {
            Console.Error.WriteLine(
                "Usage: prose --exclusion-rules --test --predicate-a <p> --object-a \"...\" --predicate-b <p> --object-b \"...\"\n" +
                "              [--pattern-a \"alt1|alt2\"] [--pattern-b \"alt1|alt2\"]");
            return 2;
        }

        var rule = new PredicateExclusion
        {
            Id = 0,
            PredicateA = pa!, ObjectPatternA = Flag(args, "--pattern-a"),
            PredicateB = pb!, ObjectPatternB = Flag(args, "--pattern-b"),
            Symmetric = !args.Contains("--directional"),
        };

        var a = new ContinuityClaim { ClaimUid = "test-a", EntityId = "e", Predicate = pa!, Object = Flag(args, "--object-a") ?? "" };
        var b = new ContinuityClaim { ClaimUid = "test-b", EntityId = "e", Predicate = pb!, Object = Flag(args, "--object-b") ?? "" };

        var matched = PredicateExclusionService.Matches(rule, a, b);
        Console.WriteLine($"rule : {Describe(rule.PredicateA, rule.ObjectPatternA)}  ⊥  {Describe(rule.PredicateB, rule.ObjectPatternB)}" +
                          (rule.Symmetric ? "  (symmetric)" : "  (directional)"));
        Console.WriteLine($"claim A: {a.Predicate} = \"{a.Object}\"");
        Console.WriteLine($"claim B: {b.Predicate} = \"{b.Object}\"");
        Console.WriteLine();
        Console.WriteLine(matched
            ? "MATCH — this pair would become a candidate and be adjudicated (one LLM call)."
            : "no match — this pair would not be paired by this rule.");
        return 0;
    }

    private static string Clip(string? s, int max) =>
        string.IsNullOrEmpty(s) ? "" : s.Length <= max ? s : s[..(max - 1)] + "…";

    private static string? Flag(string[] args, string name)
    {
        var i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }
}
