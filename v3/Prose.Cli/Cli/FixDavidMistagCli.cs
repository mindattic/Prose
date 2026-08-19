using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>prose --fix-david-mistag --universe gospel [--dry-run]</c>
///
/// One-time correction for a bug introduced by this session's own gap-fill Round 3 seed: seeding
/// bare "David" (King David) as a character collided with the many modern/medieval scholars named
/// David cited in Matthew's footnote apparatus (David Stern, David Hendin, David Instone-Brewer,
/// David Noel Freedman, David Kimhi, ...) — the scanner has no way to tell "David" (a real single-
/// token Biblical reference) apart from "David" as the first word of an unregistered two-word
/// citation name, so it wrongly tagged the King David entity onto ~19 of the 49 beats a plain
/// re-tag produced.
///
/// Fix: unwrap the David entity tag specifically where it's immediately followed by a capitalized
/// word with no intervening punctuation (comma/period/apostrophe/quote) — the exact, verified
/// signature of "David &lt;Surname&gt;" citation mentions, confirmed by manual inspection of all 49
/// tagged beats. Genuine King David references ("David's line," "David was the father of," "David,
/// 10th century BCE") are never followed by an immediate capitalized word and are left untouched.
/// Recomputes TextHash after the edit, matching TagEntitiesCli's own write convention.
/// </summary>
public static class FixDavidMistagCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var dryRun = args.Contains("--dry-run");
        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        var david = await db.Entities.FirstOrDefaultAsync(e => e.EntityType == "character" && e.Name == "David");
        if (david == null) { Console.WriteLine("[fix-david-mistag] 'David' entity not found — nothing to fix."); return 1; }

        // Guid.ToString() (used by BeatMarkup.ApplyTags) always produces lowercase hex — match
        // that exactly, NOT case-insensitively: RegexOptions.IgnoreCase would also relax the
        // [A-Z] lookahead below to match lowercase letters too, defeating the entire point of
        // checking for a capitalized follow-on word (caught in dry-run review before this shipped
        // — an early version wrongly flagged real King David references like "David recalls Uriah").
        var pattern = new Regex(
            $"""<entity repo="character" guid="{david.Id.ToString().ToLowerInvariant()}">David</entity>(?=\s[A-Z])""");

        var beats = await db.Beats.Where(b => b.Text != null && b.Text.Contains("David</entity>")).ToListAsync();
        int fixedCount = 0, occurrences = 0;
        var touchedBeatIds = new List<Guid>();
        foreach (var beat in beats)
        {
            var matches = pattern.Matches(beat.Text!);
            if (matches.Count == 0) continue;

            Console.WriteLine($"[fix-david-mistag] beat #{beat.Number}: unwrapping {matches.Count} mis-tag(s){(dryRun ? " (dry-run)" : "")}");
            occurrences += matches.Count;
            fixedCount++;
            if (dryRun) continue;

            beat.Text = pattern.Replace(beat.Text!, "David");
            beat.TextHash = NodeWorkbenchService.ComputeTextHash(beat.Text);
            touchedBeatIds.Add(beat.Id);
        }

        if (!dryRun)
        {
            await db.SaveChangesAsync();
            // BeatEntityMentions is derived from the tags themselves — re-derive for exactly the
            // beats whose tags changed, same as TagEntitiesCli does after a real re-tag.
            foreach (var beatId in touchedBeatIds)
            {
                var beat = beats.First(b => b.Id == beatId);
                await EntityMentionScanner.DeriveAndSaveMentionsAsync(dbFactory, beatId, beat.Text!);
            }
        }

        Console.WriteLine($"[fix-david-mistag] Done{(dryRun ? " (dry-run, nothing written)" : "")}. {fixedCount} beat(s) touched, {occurrences} mis-tag(s) unwrapped.");
        return 0;
    }
}
