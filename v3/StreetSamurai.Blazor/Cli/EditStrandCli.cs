using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Blazor.Cli;

/// <summary>
/// <c>ss --edit-strand</c> — the review-driven auto-editor. Reads the strand's
/// latest review batch, weights each beat's fix-priority (floor × prevalence ×
/// Pareto modifier), and conservatively rewrites the top-N floor-draggers. Emits
/// the before/after PROPOSALS as JSON to a temp file (for an approval survey) —
/// it does NOT write any beat. Apply happens only after the author approves.
///
/// Args (one of --id / --slug required):
///   --id <guid|prefix> | --slug <slug>   The strand.
///   --top N                              How many beats to propose (default 5).
///
/// Exit codes: 0 — proposals written (or none needed); 1 — bad args / not found.
/// </summary>
public static class EditStrandCli
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
            Console.Error.WriteLine("[edit-strand] One of --id or --slug is required.");
            Console.Error.WriteLine("Usage: ss --edit-strand (--id <guid|prefix> | --slug <slug>) [--top N]");
            return 1;
        }
        if (top <= 0) top = 5;

        var dbFactory = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();
        var reviewer  = services.GetRequiredService<StrandReviewService>();

        Guid strandId; string strandSlug, strandTitle;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var query = db.Strands.AsNoTracking();
            Strand? strand;
            if (!string.IsNullOrWhiteSpace(slug))
                strand = await query.FirstOrDefaultAsync(s => s.Slug == slug);
            else if (Guid.TryParse(id, out var exact))
                strand = await query.FirstOrDefaultAsync(s => s.Id == exact);
            else
            {
                var prefix = id!.ToLowerInvariant();
                var matches = await query.Where(s => s.Id.ToString().StartsWith(prefix)).Take(2).ToListAsync();
                if (matches.Count > 1) { Console.Error.WriteLine($"[edit-strand] Id prefix '{id}' is ambiguous."); return 1; }
                strand = matches.FirstOrDefault();
            }
            if (strand == null)
            {
                Console.Error.WriteLine($"[edit-strand] No strand found for {(slug != null ? $"slug '{slug}'" : $"id '{id}'")}.");
                return 1;
            }
            strandId = strand.Id; strandSlug = strand.Slug; strandTitle = strand.Title;
        }

        Console.WriteLine("[edit-strand] Review-driven auto-editor:");
        Console.WriteLine($"   Slug:  {strandSlug}");
        Console.WriteLine($"   Title: {strandTitle}");
        Console.WriteLine($"   Targeting the top {top} floor-dragging beats from the latest review batch (conservative rewrite).");
        Console.WriteLine("[edit-strand] Editing — this calls the editor model once per beat…");

        List<StrandReviewService.EditProposal> proposals;
        try
        {
            proposals = await reviewer.ProposeEditsAsync(strandId, top);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[edit-strand] Editor crashed: {ex.Message}");
            return 1;
        }

        if (proposals.Count == 0)
        {
            Console.WriteLine("[edit-strand] No proposals — either there are no reviews yet, or no beat is below the floor threshold. Run --review-strand first.");
            return 0;
        }

        var outDir = Path.Combine(Path.GetTempPath(), "streetsamurai-edits");
        Directory.CreateDirectory(outDir);
        var outPath = Path.Combine(outDir, $"edit-proposals-{strandSlug}.json");
        var payload = new
        {
            strandId, slug = strandSlug, title = strandTitle,
            generatedAt = DateTime.UtcNow,
            proposals
        };
        await File.WriteAllTextAsync(outPath, JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));

        Console.WriteLine($"[edit-strand] {proposals.Count} proposal(s) written to:");
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
