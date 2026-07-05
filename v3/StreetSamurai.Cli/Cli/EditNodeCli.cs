using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Cli;

/// <summary>
/// <c>ss --edit-story</c> — the review-driven auto-editor. Reads the node's
/// latest review batch, weights each beat's fix-priority (floor × prevalence ×
/// Pareto modifier), and conservatively rewrites the top-N floor-draggers. Emits
/// the before/after PROPOSALS as JSON to a temp file (for an approval survey) —
/// it does NOT write any beat. Apply happens only after the author approves.
///
/// Args (one of --id / --slug required):
///   --id <guid|prefix> | --slug <slug>   The node.
///   --top N                              How many beats to propose (default 5).
///
/// Exit codes: 0 — proposals written (or none needed); 1 — bad args / not found.
/// </summary>
public static class EditNodeCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? id = null, slug = null;
        int top = 5;
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--id":   if (i + 1 < args.Length) id = args[++i]; break;
                case "--slug": if (i + 1 < args.Length) slug = args[++i]; break;
                case "--top":  if (i + 1 < args.Length && int.TryParse(args[++i], out var t)) top = t; break;
            }
        }
        if (string.IsNullOrWhiteSpace(id) && string.IsNullOrWhiteSpace(slug))
        {
            Console.Error.WriteLine("[edit-story] One of --id or --slug is required.");
            Console.Error.WriteLine("Usage: ss --edit-story (--id <guid|prefix> | --slug <slug>) [--top N]");
            return 1;
        }
        if (top <= 0) top = 5;

        var dbFactory = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();
        var reviewer  = services.GetRequiredService<NodeReviewService>();

        Guid nodeId; string nodeSlug, nodeTitle;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var query = db.Nodes.AsNoTracking();
            Node? node;
            if (!string.IsNullOrWhiteSpace(slug))
                node = await query.FirstOrDefaultAsync(s => s.Slug == slug);
            else if (Guid.TryParse(id, out var exact))
                node = await query.FirstOrDefaultAsync(s => s.Id == exact);
            else
            {
                var prefix = id!.ToLowerInvariant();
                var matches = await query.Where(s => s.Id.ToString().StartsWith(prefix)).Take(2).ToListAsync();
                if (matches.Count > 1) { Console.Error.WriteLine($"[edit-story] Id prefix '{id}' is ambiguous."); return 1; }
                node = matches.FirstOrDefault();
            }
            if (node == null)
            {
                Console.Error.WriteLine($"[edit-story] No node found for {(slug != null ? $"slug '{slug}'" : $"id '{id}'")}.");
                return 1;
            }
            nodeId = node.Id; nodeSlug = node.Slug; nodeTitle = node.Title;
        }

        Console.WriteLine("[edit-story] Review-driven auto-editor:");
        Console.WriteLine($"   Slug:  {nodeSlug}");
        Console.WriteLine($"   Title: {nodeTitle}");
        Console.WriteLine($"   Targeting the top {top} floor-dragging beats from the latest review batch (conservative rewrite).");
        Console.WriteLine("[edit-story] Editing — this calls the editor model once per beat…");

        List<NodeReviewService.EditProposal> proposals;
        try
        {
            proposals = await reviewer.ProposeEditsAsync(nodeId, top);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[edit-story] Editor crashed: {ex.Message}");
            return 1;
        }

        if (proposals.Count == 0)
        {
            Console.WriteLine("[edit-story] No proposals — either there are no reviews yet, or no beat is below the floor threshold. Run --review-story first.");
            return 0;
        }

        var outDir = Path.Combine(Path.GetTempPath(), "streetsamurai-edits");
        Directory.CreateDirectory(outDir);
        var outPath = Path.Combine(outDir, $"edit-proposals-{nodeSlug}.json");
        var payload = new
        {
            nodeId, slug = nodeSlug, title = nodeTitle,
            generatedAt = DateTime.UtcNow,
            proposals
        };
        await File.WriteAllTextAsync(outPath, JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));

        Console.WriteLine($"[edit-story] {proposals.Count} proposal(s) written to:");
        Console.WriteLine($"   {outPath}");
        Console.WriteLine();
        foreach (var p in proposals)
        {
            Console.WriteLine($"— Beat #{p.BeatNumber} (pos {p.Position})  score {p.Mean}/5  flags {p.Flags}  priority {p.Priority}{(p.Contested ? "  [CONTESTED]" : "")}");
            Console.WriteLine($"    why: {p.Rationale}");
            if (p.Addresses.Count > 0)
                Console.WriteLine($"    addresses: {string.Join("; ", p.Addresses)}");
        }
        return 0;
    }
}
