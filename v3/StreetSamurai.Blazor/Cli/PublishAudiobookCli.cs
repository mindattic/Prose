using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Blazor.Cli;

/// <summary>
/// <c>ss --publish-audiobook (--id &lt;guid|prefix&gt; | --slug &lt;slug&gt;) [--robust] [--tts ENGINE]</c>
/// — render the whole strand as ONE continuous narration (no per-beat voice
/// drift) and write the MP3 to the user's Downloads folder. The headless twin of
/// the "Publish Audiobook" button.
/// <para><c>--tts</c> selects the engine: <c>elevenlabs</c> (default, paid, highest
/// fidelity) or a FREE fully-local engine — <c>piper</c> (bundled exe, fastest),
/// <c>kokoro</c> (Python, CPU-friendly, recommended free default), or <c>chatterbox</c>
/// (Python, Resemble Chatterbox-Turbo, most expressive). Local engines need no API key
/// and cost nothing per character — built for bedtime/draft listens.</para>
/// <para><c>--robust</c> retunes this strand's frozen voice snapshot to Robust
/// stability (1.0) before recording — the explicit opt-in that lets a strand
/// first narrated at Natural (0.5) adopt the most consistent v3 narrator on
/// re-record. Persisted, so every later re-record stays Robust.</para>
/// </summary>
public static class PublishAudiobookCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? id = null, slug = null, tts = null;
        bool robust = false;
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--id":     if (i + 1 < args.Length) id = args[++i]; break;
                case "--slug":   if (i + 1 < args.Length) slug = args[++i]; break;
                case "--robust": robust = true; break;
                case "--tts":    if (i + 1 < args.Length) tts = args[++i]; break;
            }
        }
        if (string.IsNullOrWhiteSpace(id) && string.IsNullOrWhiteSpace(slug))
        {
            Console.Error.WriteLine("[publish-audiobook] One of --id or --slug is required.");
            return 1;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();
        var workbench = services.GetRequiredService<StrandWorkbenchService>();

        Guid strandId; string strandTitle;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var q = db.Strands.AsNoTracking();
            Strand? strand;
            if (!string.IsNullOrWhiteSpace(slug)) strand = await q.FirstOrDefaultAsync(s => s.Slug == slug);
            else if (Guid.TryParse(id, out var g)) strand = await q.FirstOrDefaultAsync(s => s.Id == g);
            else strand = await q.Where(s => s.Id.ToString().StartsWith(id!.ToLower())).Take(2).ToListAsync() switch
            { { Count: 1 } m => m[0], _ => null };
            if (strand == null) { Console.Error.WriteLine("[publish-audiobook] Strand not found."); return 1; }
            strandId = strand.Id; strandTitle = strand.Title;
        }

        Console.WriteLine($"[publish-audiobook] Narrating \"{strandTitle}\" in one pass{(robust ? " (retuning to Robust stability)" : "")}{(tts != null ? $" via {tts}" : "")}…");
        try
        {
            var path = await workbench.PublishAudiobookAsync(strandId, robust, tts);
            if (path == null) { Console.Error.WriteLine("[publish-audiobook] Nothing to narrate — the strand has no beat text."); return 1; }
            Console.WriteLine($"[publish-audiobook] Wrote: {path}");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine($"[publish-audiobook] Failed: {ex.Message}"); return 1; }
    }
}
