using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>ss --harvest-voice</c> — distill voice rules from winning nodes into the
/// codified, DB-backed rules the generator reads. Propose-then-approve.
///
///   ss --harvest-voice --slug &lt;slug&gt; [--force]   harvest one node (proposals only)
///   ss --harvest-voice --id &lt;guid|prefix&gt;        same, by id
///   ss --harvest-voice --all-80                  harvest every node scored ≥80%
///   ss --harvest-voice --pending                 list proposed rules awaiting approval
///   ss --harvest-voice --apply &lt;entry-guid&gt;       apply one proposed rule to the live store
///   ss --harvest-voice --reject &lt;entry-guid&gt;      reject a proposed rule (kept in the trail)
///
/// Nothing touches the live rules until --apply. Exit: 0 ok, 1 bad args / not found.
/// </summary>
public static class HarvestVoiceCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? slug = null, id = null, applyId = null, rejectId = null;
        bool all80 = args.Contains("--all-80");
        bool pending = args.Contains("--pending");
        bool force = args.Contains("--force");
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--slug":   if (i + 1 < args.Length) slug = args[++i]; break;
                case "--id":     if (i + 1 < args.Length) id = args[++i]; break;
                case "--apply":  if (i + 1 < args.Length) applyId = args[++i]; break;
                case "--reject": if (i + 1 < args.Length) rejectId = args[++i]; break;
            }
        }

        var harvest = services.GetRequiredService<VoiceHarvestService>();

        // ── apply / reject a specific proposal ──
        if (applyId != null || rejectId != null)
        {
            var raw = applyId ?? rejectId!;
            if (!Guid.TryParse(raw, out var entryId)) { Console.Error.WriteLine("[harvest-voice] --apply/--reject needs a change-log entry GUID."); return 1; }
            var ok = applyId != null ? await harvest.ApplyAsync(entryId) : await harvest.RejectAsync(entryId);
            if (!ok) { Console.Error.WriteLine($"[harvest-voice] Entry {entryId} not found or already resolved."); return 1; }
            Console.WriteLine($"[harvest-voice] {(applyId != null ? "Applied" : "Rejected")} {entryId}.");
            return 0;
        }

        // ── bulk apply every still-proposed rule (after curation/rejects) ──
        if (args.Contains("--apply-all"))
        {
            var rows = await harvest.GetByStatusAsync("proposed");
            int ok = 0;
            foreach (var e in rows)
                if (await harvest.ApplyAsync(e.Id)) { ok++; Console.WriteLine($"[harvest-voice] applied → {e.RuleTarget}: {e.Description[..Math.Min(70, e.Description.Length)]}"); }
            Console.WriteLine($"\n[harvest-voice] Applied {ok}/{rows.Count} proposed rules to the live voice stores.");
            return 0;
        }

        // ── list pending proposals ──
        if (pending)
        {
            var rows = await harvest.GetByStatusAsync("proposed");
            PrintProposals(rows);
            return 0;
        }

        // ── prose-based canon harvest: learn the voice from the finished canon
        //    prose itself (not edit-history). This is what "train on the canon
        //    voice" means when the canon nodes have no workbench edit trail. ──
        if (args.Contains("--canon-prose"))
        {
            var results = await harvest.HarvestCanonProseAsync();
            if (results.Count == 0) { Console.WriteLine("[harvest-voice] No nodes marked canon yet."); return 0; }
            foreach (var r in results)
                Console.WriteLine($"[harvest-voice] {r.Slug} (canon): → {r.Proposals.Count} proposals.");
            Console.WriteLine();
            PrintProposals(results.SelectMany(r => r.Proposals).ToList());
            Console.WriteLine("\nReview, then apply: ss --harvest-voice --apply <entry-guid>");
            return 0;
        }

        // ── harvest ──
        if (all80 || args.Contains("--canon"))
        {
            var canonOnly = args.Contains("--canon");
            var results = canonOnly ? await harvest.HarvestCanonAsync() : await harvest.HarvestAllAboveAsync();
            if (results.Count == 0) { Console.WriteLine(canonOnly ? "[harvest-voice] No nodes marked canon yet." : "[harvest-voice] No nodes scored ≥80%."); return 0; }
            foreach (var r in results)
                Console.WriteLine($"[harvest-voice] {r.Slug} ({r.Score:0.#}%): {r.EditCount} edits + {r.DirectiveCount} directives → {r.Proposals.Count} proposals.");
            Console.WriteLine();
            PrintProposals(results.SelectMany(r => r.Proposals).ToList());
            Console.WriteLine("\nReview, then apply: ss --harvest-voice --apply <entry-guid>");
            return 0;
        }

        if (string.IsNullOrWhiteSpace(slug) && string.IsNullOrWhiteSpace(id))
        {
            Console.Error.WriteLine("[harvest-voice] One of --slug / --id / --all-80 / --pending / --apply / --reject is required.");
            return 1;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        Guid nodeId;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var q = db.Nodes.AsNoTracking();
            Node? node;
            if (!string.IsNullOrWhiteSpace(slug)) node = await q.FirstOrDefaultAsync(s => s.Slug == slug);
            else if (Guid.TryParse(id, out var g)) node = await q.FirstOrDefaultAsync(s => s.Id == g);
            else node = await q.Where(s => s.Id.ToString().StartsWith(id!.ToLower())).Take(2).ToListAsync() switch
            { { Count: 1 } m => m[0], _ => null };
            if (node == null) { Console.Error.WriteLine("[harvest-voice] Node not found."); return 1; }
            nodeId = node.Id;
        }

        try
        {
            var r = await harvest.HarvestNodeAsync(nodeId, force);
            Console.WriteLine($"[harvest-voice] \"{r.Title}\" ({r.Score:0.#}%): {r.EditCount} edits + {r.DirectiveCount} directives → {r.Proposals.Count} proposals.\n");
            PrintProposals(r.Proposals);
            if (r.Proposals.Count > 0)
                Console.WriteLine("\nReview, then apply: ss --harvest-voice --apply <entry-guid>");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine($"[harvest-voice] {ex.Message}"); return 1; }
    }

    private static void PrintProposals(List<VoiceChangeLogEntry> rows)
    {
        if (rows.Count == 0) { Console.WriteLine("[harvest-voice] No proposals."); return; }
        foreach (var e in rows)
        {
            Console.WriteLine($"  {e.Id}");
            Console.WriteLine($"    → {e.RuleTarget}");
            Console.WriteLine($"    {e.Description}");
            if (!string.IsNullOrWhiteSpace(e.Evidence)) Console.WriteLine($"    evidence: {e.Evidence}");
            Console.WriteLine();
        }
    }
}
