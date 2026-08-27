using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Services;
using System.Text.RegularExpressions;

namespace Prose.Cli;

/// <summary>
/// <c>prose --strip-beat-artifacts --slug &lt;slug&gt; [--dry-run]</c>
///
/// One-off cleanup for a generation artifact observed in the ANTHOLOGY-universe beats: the model
/// echoed a leading markdown scene-heading line (e.g. <c>"# BEAT: Title"</c> or
/// <c>"# Book Title — Beat N"</c>) and a trailing structural marker (a <c>"---"</c> rule followed
/// by <c>"sceneEnd=true"</c>/<c>"sceneEnd=false"</c>, sometimes bold) directly into the stored
/// <c>Beats.Text</c> — neither is stripped anywhere in the export pipeline
/// (<c>BeatMarkup</c> only strips <c>&lt;entity&gt;</c> tags), so both would render literally in
/// the final manuscript. Strips at most one leading heading line and one trailing marker block per
/// beat; leaves everything else untouched. Idempotent — a beat with no matching artifact is a
/// no-op.
/// </summary>
public static class StripBeatArtifactsCli
{
    private static readonly Regex LeadingHeading = new(@"\A#[^\n]*\n\n?", RegexOptions.Compiled);
    private static readonly Regex TrailingMarker = new(
        @"\n+(?:-{3,}\n+)?\*{0,2}sceneEnd=(?:true|false)\*{0,2}\s*\z",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? slug = null;
        var dryRun = args.Contains("--dry-run");
        for (int i = 0; i < args.Length; i++)
            if (args[i] == "--slug" && i + 1 < args.Length) slug = args[++i];

        if (string.IsNullOrWhiteSpace(slug))
        {
            Console.Error.WriteLine("Usage: prose --strip-beat-artifacts --slug <slug> [--dry-run]");
            return 2;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        var reference = slug;
        var nodeId = await NodeRefResolver.ResolveAsync(db, reference);
        if (nodeId == null)
        {
            Console.Error.WriteLine(NodeRefResolver.NotFoundMessage(reference));
            return 1;
        }

        var leafIds = await NodeWorkbenchService.GetLeafDescendantIdsAsync(db, nodeId.Value);
        var beatIds = await db.BeatNodes.Where(bn => leafIds.Contains(bn.NodeId))
            .Select(bn => bn.BeatId).Distinct().ToListAsync();
        var beats = await db.Beats.Where(b => beatIds.Contains(b.Id)).OrderBy(b => b.Number).ToListAsync();

        int changed = 0;
        foreach (var beat in beats)
        {
            var text = beat.Text ?? "";
            if (text.Length == 0) continue;

            var cleaned = LeadingHeading.Replace(text, "", 1);
            cleaned = TrailingMarker.Replace(cleaned, "", 1);
            cleaned = cleaned.Trim();

            if (cleaned == text.Trim()) continue;
            if (cleaned.Length < text.Length / 2)
            {
                Console.WriteLine($"  SKIP beat #{beat.Number} ({beat.Title}) — cleanup would remove more than half the text ({text.Length} -> {cleaned.Length} chars); leaving untouched for manual review.");
                continue;
            }

            changed++;
            Console.WriteLine($"  {(dryRun ? "[dry-run] would clean" : "clean")} beat #{beat.Number} ({beat.Title}): {text.Length} -> {cleaned.Length} chars.");
            if (!dryRun) beat.Text = cleaned;
        }

        if (!dryRun && changed > 0) await db.SaveChangesAsync();

        Console.WriteLine($"[strip-beat-artifacts] {beats.Count} beat(s) scanned, {changed} {(dryRun ? "would be " : "")}cleaned.");
        return 0;
    }
}
