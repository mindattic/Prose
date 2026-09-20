using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;
using Prose.Core.Services;
using Prose.Core.Services.Audit;

namespace Prose.Cli;

/// <summary>
/// prose --provenance-audit [--slug &lt;slug-or-code-or-id&gt;] [--samples N] [--json]
/// prose --provenance --grade &lt;grade&gt; --entity &lt;id&gt; | --relationship &lt;rowId&gt; | --claim &lt;uid&gt;
/// prose --provenance --grades
///
/// The Story Ledger's provenance surface (Phase 3).
///
/// <para><b>The audit</b> answers the question that was previously an archaeology project:
/// <i>what is in canon that no human ever approved?</i> It counts <c>Entities</c>,
/// <c>CharacterRelationships</c>, and <c>ContinuityClaims</c> by grade, and samples the rows whose
/// grade is neither <c>authored</c> nor <c>observed</c>. Deterministic, free, report-only
/// (docs/LOGIC.md §4).</para>
///
/// <para><b>The grade write</b> is the deliberate human act that promotes a candidate to canon —
/// a <c>scaffolded</c> row becomes <c>authored</c> because someone read it and said so, one row at
/// a time, named explicitly. There is intentionally NO bulk promotion: "mark everything
/// authored" would destroy the only signal this column carries. Rejection of a fabricated ledger
/// claim stays where it already lives, <c>prose --continuity reject</c>, which sets a status
/// rather than a grade — a rejected claim is not a badly-graded fact, it is not a fact.</para>
///
/// <para>Universe-scoped: entity/relationship counts come through the ambient query filter, so
/// this needs a real <c>--universe</c> and stays out of Program.cs's
/// <c>UniverseAgnosticCommands</c>. The id-addressed writes use <c>IgnoreQueryFilters</c>
/// internally so an explicitly-named id always resolves.</para>
/// </summary>
public static class ProvenanceCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var provenance = services.GetRequiredService<ProvenanceService>();

        if (args.Contains("--grades")) return PrintGrades();

        // ── the grade write ──────────────────────────────────────────────────
        if (args.Contains("--provenance") && !args.Contains("--provenance-audit"))
            return await SetGradeAsync(args, provenance);

        // ── the audit ────────────────────────────────────────────────────────
        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        Guid? nodeId = null;
        string? bookSlug = null;

        var slug = Flag(args, "--slug") ?? Flag(args, "--code");
        if (!string.IsNullOrWhiteSpace(slug))
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            nodeId = await NodeRefResolver.ResolveAsync(db, slug);
            if (nodeId == null)
            {
                Console.Error.WriteLine($"[provenance] No node matched '{slug}'.");
                return 2;
            }
            // ContinuityClaims records its book by Node.Slug (see TunedReadService), not by code —
            // resolve it rather than trusting whatever form the caller typed.
            bookSlug = await db.Nodes.IgnoreQueryFilters().AsNoTracking()
                .Where(n => n.Id == nodeId.Value).Select(n => n.Slug).FirstOrDefaultAsync();
        }

        var sampleLimit = int.TryParse(Flag(args, "--samples"), out var s) && s >= 0
            ? s : ProvenanceService.DefaultSampleLimit;

        var report = await provenance.AuditAsync(nodeId, bookSlug, sampleLimit);

        if (args.Contains("--json"))
        {
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(
                report, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            return 0;
        }

        Console.WriteLine(nodeId == null
            ? "[provenance-audit] whole universe"
            : $"[provenance-audit] book '{bookSlug}' ({nodeId.Value:N}) — book-scoped entities only; " +
              "universe-wide entities are shared by every book and are not attributed here");
        Console.WriteLine();

        if (report.Counts.Count == 0)
        {
            Console.WriteLine("  no graded rows in scope.");
            return 0;
        }

        var table = "";
        foreach (var c in report.Counts)
        {
            if (c.Table != table) { Console.WriteLine($"  {c.Table}:"); table = c.Table; }
            var trust = ClaimProvenance.IsTrustworthy(c.Grade) ? "  " : " *";
            Console.WriteLine($"   {trust} {c.Grade,-16} {c.Count,8:N0}");
        }

        Console.WriteLine();
        Console.WriteLine($"  {report.UnapprovedRows:N0} of {report.TotalRows:N0} graded row(s) " +
                          $"({report.UnapprovedFraction:P1}) are marked * — no human ever approved them.");

        if (report.Samples.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("UNAPPROVED (sample):");
            foreach (var g in report.Samples.GroupBy(x => x.Table))
            {
                Console.WriteLine($"  {g.Key}:");
                foreach (var r in g)
                    Console.WriteLine($"    [{r.Grade,-14}] {r.Id}  {Clip(r.Label, 90)}");
            }
        }

        Console.WriteLine();
        Console.WriteLine("A legacy-unknown grade is NOT evidence of a defect — it means the row predates " +
                          "grading (author ruling: grandfather, never mass-flag). Promote what you have read " +
                          "and verified: prose --provenance --grade authored --entity <id>");
        return 0;
    }

    private static async Task<int> SetGradeAsync(string[] args, ProvenanceService provenance)
    {
        var grade = Flag(args, "--grade");
        if (!ClaimProvenance.IsValid(grade))
        {
            Console.Error.WriteLine($"[provenance] --grade is required and must be one of: {string.Join(", ", ClaimProvenance.All)}");
            return 2;
        }

        var entity = Flag(args, "--entity");
        var relationship = Flag(args, "--relationship");
        var claim = Flag(args, "--claim");

        var targets = new[] { entity, relationship, claim }.Count(x => !string.IsNullOrWhiteSpace(x));
        if (targets != 1)
        {
            Console.Error.WriteLine(
                "Usage: prose --provenance --grade <grade> --entity <id>\n" +
                "       prose --provenance --grade <grade> --relationship <rowId>\n" +
                "       prose --provenance --grade <grade> --claim <uid>\n" +
                "Exactly one target per call — a grade is a decision about one thing.");
            return 2;
        }

        if (!string.IsNullOrWhiteSpace(entity))
        {
            if (!Guid.TryParse(entity, out var id))
            {
                Console.Error.WriteLine($"[provenance] '{entity}' is not an entity id (guid, plain or hyphenated).");
                return 2;
            }
            if (!await provenance.SetEntityProvenanceAsync(id, grade!))
            {
                Console.Error.WriteLine($"[provenance] No entity {id:N}.");
                return 2;
            }
            Console.WriteLine($"[provenance] entity {id:N} graded {grade}.");
            return 0;
        }

        if (!string.IsNullOrWhiteSpace(relationship))
        {
            if (!long.TryParse(relationship, out var rowId))
            {
                Console.Error.WriteLine($"[provenance] '{relationship}' is not a relationship row id " +
                                        "(see prose --entity-relationships --character <name>).");
                return 2;
            }
            if (!await provenance.SetRelationshipProvenanceAsync(rowId, grade!))
            {
                Console.Error.WriteLine($"[provenance] No relationship row {rowId}.");
                return 2;
            }
            Console.WriteLine($"[provenance] relationship row {rowId} graded {grade} (read model refreshed).");
            return 0;
        }

        if (!await provenance.SetClaimProvenanceAsync(claim!, grade!))
        {
            Console.Error.WriteLine($"[provenance] No claim '{claim}'.");
            return 2;
        }
        Console.WriteLine($"[provenance] claim {claim} graded {grade}.");
        return 0;
    }

    private static int PrintGrades()
    {
        Console.WriteLine("Provenance grades, in descending trust:");
        Console.WriteLine("  authored        a human decided this. The only grade that is canon without qualification.");
        Console.WriteLine("  observed        extracted from prose with a snippet that MECHANICALLY verifies against the beat text.");
        Console.WriteLine("  inferred        a model produced it without a verifying quote, or derived it. Believable, never authoritative.");
        Console.WriteLine("  scaffolded      auto-created by entity scaffolding. NEVER canon — candidate only.");
        Console.WriteLine("  legacy-unknown  predates grading; grandfathered. Unknown provenance, not evidence of a defect.");
        Console.WriteLine();
        Console.WriteLine("Trustworthy for prose generation: authored, observed.");
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
