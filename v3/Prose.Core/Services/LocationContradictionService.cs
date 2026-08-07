using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;

namespace Prose.Core.Services;

/// <summary>
/// Cross-book location-vs-time contradiction detector.
///
/// Premise: a character can only be in one place at a time. We pull every
/// "X is at Y at time T" presence-fact we can derive — currently:
///   1. <c>Edge.RelationType = 'located_at'</c> with <c>StoryValidFrom..Until</c>
///   2. <c>ChapterBeat.InWorldDate</c> joined to <c>ChapterCharacters</c> +
///      the chapter's authored Place (when wired)
/// — sort by character, walk pairs, and flag any pair where the character is
/// reported in two different places at the same instant (or close enough that
/// physically impossible to traverse — knob: <see cref="MinTravelMinutes"/>).
///
/// Empty result is the common case until <c>InWorldDate</c> /
/// <c>located_at</c> Edges are populated. The detector returns a structured
/// status so the UI can tell users "no data yet, write some chapter dates."
/// </summary>
public class LocationContradictionService
{
    /// <summary>
    /// Minimum minutes we'd accept between two different locations. Below this
    /// the character would need to teleport; flag as a contradiction. Tweakable
    /// for dramatic license — the Pulse moves Mach 6, so 45 min Chicago→Rotterdam
    /// is fine; teleporting block-to-block in 30 seconds isn't.
    /// </summary>
    public int MinTravelMinutes { get; set; } = 5;

    private readonly IDbContextFactory<ProseDbContext> dbFactory;
    private readonly FindingsService findings;
    private readonly ILogger<LocationContradictionService> log;

    public LocationContradictionService(
        IDbContextFactory<ProseDbContext> dbFactory,
        FindingsService findings,
        ILogger<LocationContradictionService> log)
    {
        this.dbFactory = dbFactory;
        this.findings  = findings;
        this.log       = log;
    }

    public sealed record PresenceFact(
        Guid CharacterId,
        string CharacterName,
        Guid? PlaceId,
        string PlaceName,
        DateTime At,
        string Source);

    public sealed record LocationConflict(
        Guid CharacterId,
        string CharacterName,
        DateTime AtA,
        string PlaceA,
        DateTime AtB,
        string PlaceB,
        TimeSpan Delta,
        string SourceA,
        string SourceB);

    public sealed record ScanResult(
        int CharactersExamined,
        int PresenceFacts,
        IReadOnlyList<LocationConflict> Conflicts,
        string StatusNote);

    public async Task<ScanResult> ScanAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        // 1) Edge-derived presence facts: character --located_at--> place, with
        //    a StoryValidFrom we treat as the instant of presence.
        var edgeFacts = await (
            from e in db.Edges.AsNoTracking()
            join src in db.Entities.AsNoTracking() on e.SourceId equals src.Id
            join tgt in db.Entities.AsNoTracking() on e.TargetId equals tgt.Id
            where e.RelationType == "located_at"
               && src.EntityType == "character"
               && tgt.EntityType == "place"
               && e.StoryValidFrom != null
            select new
            {
                CharacterId   = src.Id,
                CharacterName = src.Name,
                PlaceId       = (Guid?)tgt.Id,
                PlaceName     = tgt.Name,
                At            = e.StoryValidFrom!.Value,
                Source        = $"edge:{e.Id}",
            }).ToListAsync(ct);

        // 2) Chapter-beat presence: every (character, beat) joined to the beat's
        //    InWorldDate. We don't infer the beat's place yet — just record the
        //    time of presence so future Place wiring lights up automatically.
        //    Without a beat-level place, we can still detect "char appears in two
        //    beats simultaneously" (rare, but a real authoring bug class).
        var beatFacts = await (
            from cc in db.ChapterCharacters.AsNoTracking()
            join cb in db.ChapterBeats.AsNoTracking() on cc.ChapterId equals cb.ChapterId
            join ent in db.Entities.AsNoTracking() on cc.CharacterId equals ent.Id
            where cc.CharacterId != null
               && cb.InWorldDate != null
            select new
            {
                CharacterId   = cc.CharacterId!.Value,
                CharacterName = ent.Name,
                PlaceId       = (Guid?)null,
                PlaceName     = "(beat)",
                At            = cb.InWorldDate!.Value,
                Source        = $"beat:{cb.BeatGuid:N}",
            }).ToListAsync(ct);

        var all = edgeFacts.Concat(beatFacts)
            .Select(f => new PresenceFact(f.CharacterId, f.CharacterName, f.PlaceId, f.PlaceName, f.At, f.Source))
            .OrderBy(f => f.CharacterId).ThenBy(f => f.At)
            .ToList();

        var conflicts = new List<LocationConflict>();
        var charsExamined = 0;
        foreach (var grp in all.GroupBy(f => f.CharacterId))
        {
            charsExamined++;
            var ordered = grp.ToList();
            for (int i = 0; i < ordered.Count - 1; i++)
            {
                var a = ordered[i];
                var b = ordered[i + 1];
                if (string.IsNullOrEmpty(a.PlaceName) || string.IsNullOrEmpty(b.PlaceName)) continue;
                if (a.PlaceId != null && b.PlaceId != null)
                {
                    if (a.PlaceId == b.PlaceId) continue;                   // same place — no conflict
                }
                else if (a.PlaceId != b.PlaceId) continue;                  // one edge, one beat — can't compare locations
                // both PlaceId == null: beat-vs-beat temporal collision — fall through to delta check

                var delta = b.At - a.At;
                if (delta < TimeSpan.FromMinutes(MinTravelMinutes))
                {
                    conflicts.Add(new LocationConflict(
                        a.CharacterId, a.CharacterName,
                        a.At, a.PlaceName,
                        b.At, b.PlaceName,
                        delta,
                        a.Source, b.Source));
                }
            }
        }

        // Surface every conflict to the Findings inbox so /findings shows them
        // alongside the autonomous contradiction/cliché scan results. dedup_key
        // is FindingsService.Upsert's responsibility — same (character, source-pair)
        // collapses to one row across re-scans.
        foreach (var c in conflicts)
        {
            var sev = c.Delta.TotalMinutes < 1 ? FindingSeverity.High
                    : c.Delta.TotalMinutes < MinTravelMinutes ? FindingSeverity.High
                    : c.Delta.TotalMinutes < MinTravelMinutes * 4 ? FindingSeverity.Medium
                    : FindingSeverity.Low;

            var summary =
                $"{c.CharacterName} appears in '{c.PlaceA}' then '{c.PlaceB}' " +
                $"only {c.Delta.TotalMinutes:F0}min apart (story-time)";
            var snippet =
                $"{c.AtA:yyyy-MM-dd HH:mm:ss}  →  {c.PlaceA}\n" +
                $"{c.AtB:yyyy-MM-dd HH:mm:ss}  →  {c.PlaceB}\n" +
                $"sources: {c.SourceA} / {c.SourceB}";
            var fix =
                "Either fix one of the place assignments, push one event's InWorldDate, " +
                "or accept it (e.g. Pulse transit makes Mach-6 jumps plausible).";

            // Synthetic "file_path" so the row is queryable in the inbox; pair the
            // sources to make the dedup deterministic per conflict.
            var pseudoPath = $"contradiction:char:{c.CharacterId:N}:{c.SourceA}->{c.SourceB}";
            findings.Upsert(pseudoPath, chapterId: null,
                FindingCategory.Contradiction, sev, summary, snippet, fix);
        }

        var status = (all.Count, conflicts.Count) switch
        {
            (0, _) =>
                "No presence facts found. Populate ChapterBeat.InWorldDate (or Edge 'located_at' rows) to enable detection.",
            (_, 0) => "No location contradictions detected.",
            (_, _) => $"{conflicts.Count} potential contradictions across {charsExamined} characters — see /findings.",
        };

        log.LogInformation("Location-contradiction scan: {Facts} facts, {Conflicts} conflicts", all.Count, conflicts.Count);
        return new ScanResult(charsExamined, all.Count, conflicts, status);
    }
}
