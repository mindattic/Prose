using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Interfaces;
using Prose.Core.Services;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Prose.Cli;

/// <summary>
/// <c>prose --timeline (--slug slug | --id id)</c>
///   Scans every beat in a node (or every episode in a book) for clock and day
///   references, infers story-relative timestamps, and prints an elapsed-time table.
///   Flags continuity conflicts where stated time contradicts the prior sequence.
///
///   ● = explicit time anchor extracted from prose
///   ⚡ = time conflict detected
///   (blank) = time carried forward from prior beat
///
///   For books the timeline is stitched episode-by-episode, keeping elapsed time
///   continuous across episode boundaries.
/// </summary>
public static class TimelineCli
{
    // Max beats per LLM call. Above this the output JSON risks hitting the token cap.
    private const int ChunkSize = 60;

    public static async Task<int> RunAsync(string[] args, IServiceProvider sp)
    {
        string? slug = null, id = null, code = null;
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--slug": if (i + 1 < args.Length) slug = args[++i]; break;
                case "--id":   if (i + 1 < args.Length) id   = args[++i]; break;
                case "--code": if (i + 1 < args.Length) code = args[++i]; break;
            }
        }

        if (string.IsNullOrWhiteSpace(slug) && string.IsNullOrWhiteSpace(id) && string.IsNullOrWhiteSpace(code))
        {
            Console.Error.WriteLine("usage: prose --timeline (--slug <slug> | --id <id> | --code <code>)");
            return 1;
        }

        var dbFactory = sp.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        Core.Data.Entities.Node? node;
        if (!string.IsNullOrWhiteSpace(code))
            node = await db.Nodes.AsNoTracking()
                .FirstOrDefaultAsync(s => s.NodeCode == code.ToUpperInvariant());
        else if (!string.IsNullOrWhiteSpace(slug))
            node = await db.Nodes.AsNoTracking().FirstOrDefaultAsync(s => s.Slug == slug);
        else
            node = await db.Nodes.AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id.ToString().Replace("-", "").StartsWith(id!.Replace("-", "")));

        if (node is null)
        {
            Console.Error.WriteLine("[timeline] node not found");
            return 1;
        }

        Console.WriteLine($"[timeline] {node.Title}  ({node.Slug})");

        // Load direct beats for this node.
        var directBeats = await LoadBeatsAsync(db, node.Id, episodeTitle: null);

        // Organise into chunks: either the direct beats chunked, or each episode as its own chunk.
        var chunks = new List<(string? episodeTitle, List<BeatRow> beats)>();

        if (directBeats.Count > 0)
        {
            // Single node — split into ≤ChunkSize blocks.
            for (int start = 0; start < directBeats.Count; start += ChunkSize)
                chunks.Add((null, directBeats.Skip(start).Take(ChunkSize).ToList()));
        }
        else
        {
            // Book / collection — load each episode separately. Descend to LEAF nodes, not
            // just direct children — a split-collection book (Book -> "Chapter N" container
            // with 0 direct beats -> real chapters -> beats, e.g. BLST/ICFI/RTR/VIGL) has its
            // real chapters/episodes two levels down. Direct-children-only used to silently
            // report "no beats found" for these books. Same bug class fixed in
            // WorkflowMonitorService (2026-08-09) and BackfillCoverageCli (2026-08-10).
            // Preserve GetLeafDescendantIdsAsync's own return order rather than re-sorting by
            // Node.SortKey, which is only comparable within one parent's sibling group.
            var leafIds = await NodeWorkbenchService.GetLeafDescendantIdsAsync(db, node.Id);
            var byId = await db.Nodes.AsNoTracking().IgnoreQueryFilters()
                .Where(s => leafIds.Contains(s.Id))
                .Select(s => new { s.Id, s.Title })
                .ToDictionaryAsync(s => s.Id);
            var children = leafIds.Where(byId.ContainsKey).Select(id => byId[id]).ToList();

            if (children.Count == 0)
            {
                Console.Error.WriteLine("[timeline] no beats found (no direct beats and no child episodes)");
                return 1;
            }

            Console.WriteLine($"[timeline] book with {children.Count} episode(s)");
            foreach (var ep in children)
            {
                var epBeats = await LoadBeatsAsync(db, ep.Id, ep.Title);
                if (epBeats.Count == 0) continue;

                // Split large episodes into sub-chunks as well.
                for (int start = 0; start < epBeats.Count; start += ChunkSize)
                    chunks.Add((ep.Title, epBeats.Skip(start).Take(ChunkSize).ToList()));
            }
        }

        int totalBeats = chunks.Sum(c => c.beats.Count);
        Console.WriteLine($"[timeline] {totalBeats} beat(s) across {chunks.Count} chunk(s) — extracting time anchors…");

        var llm = sp.GetRequiredService<ILlmService>();
        var allEntries = new List<(TimelineRow row, BeatRow beat)>();

        // "priorContext" carries forward across chunks so relative refs resolve correctly.
        string? priorContext = null;
        int globalBeatOffset = 0;   // cumulative beat count before this chunk
        int? globalElapsedOffset = null;  // minutes from book start to start of this chunk

        foreach (var (epTitle, beats) in chunks)
        {
            var label = epTitle is not null ? $"episode \"{epTitle}\"" : "beats";
            Console.Write($"  [{label}: {beats.Count} beat(s)]  ");

            var entries = await CallLlmChunkAsync(llm, beats, globalBeatOffset, priorContext);
            if (entries is null)
            {
                Console.Error.WriteLine("  [timeline] chunk failed — skipping");
                globalBeatOffset += beats.Count;
                continue;
            }

            Console.WriteLine($"got {entries.Count} row(s)");

            // Find last known elapsed in this chunk, apply the global offset.
            int? chunkLastElapsed = null;
            foreach (var e in entries)
            {
                if (e.ElapsedMinutes.HasValue)
                {
                    // Shift to book-global elapsed.
                    if (globalElapsedOffset is null && e.ElapsedMinutes.HasValue)
                        globalElapsedOffset = 0; // first anchor in the whole book

                    e.ElapsedMinutes = e.ElapsedMinutes + (globalElapsedOffset ?? 0);
                    chunkLastElapsed = e.ElapsedMinutes;
                }
            }

            // Pair entries with their original beats for display.
            for (int i = 0; i < entries.Count; i++)
            {
                var beatIdx = i; // chunk-local
                var beat = (beatIdx < beats.Count) ? beats[beatIdx] : beats[^1];
                allEntries.Add((entries[i], beat));
            }

            // Build context string for the next chunk.
            var lastEntry = entries.LastOrDefault(e => e.AbsoluteTime is not null);
            if (lastEntry is not null)
                priorContext = $"The previous chunk ended at approximately {lastEntry.AbsoluteTime}" +
                               (chunkLastElapsed.HasValue ? $" (elapsed from story start: {FormatElapsed(chunkLastElapsed.Value)})" : "") + ".";

            // Advance the global elapsed offset to the end of this chunk.
            if (chunkLastElapsed.HasValue)
                globalElapsedOffset = chunkLastElapsed;

            globalBeatOffset += beats.Count;
        }

        if (allEntries.Count == 0)
        {
            Console.Error.WriteLine("[timeline] no results");
            return 1;
        }

        // Print the combined table.
        Console.WriteLine();
        Console.WriteLine($"=== Timeline: {node.Title} ===");
        Console.WriteLine();
        Console.WriteLine($"  {"":2} {"sk",6}  {"Time",-24} {"Elapsed",-12} Beat");
        Console.WriteLine(new string('─', 92));

        int conflicts = 0, anchored = 0;
        string? currentEp = null;
        int? lastElapsed = null;

        foreach (var (entry, beat) in allEntries)
        {
            // Episode header when we enter a new one.
            if (beat.EpisodeTitle != currentEp)
            {
                currentEp = beat.EpisodeTitle;
                if (currentEp is not null)
                    Console.WriteLine($"  ── {currentEp} ──");
            }

            var preview = beat.Text.Trim().Replace('\n', ' ').Replace('\r', ' ');
            if (preview.Length > 55) preview = preview[..55] + "…";

            var marker  = entry.IsConflict ? "⚡" : entry.Anchor is not null ? "●" : " ";
            var timeStr = entry.AbsoluteTime ?? "(unknown)";
            var elapsed = entry.ElapsedMinutes.HasValue ? FormatElapsed(entry.ElapsedMinutes.Value) : "?";

            if (entry.Anchor is not null) anchored++;
            if (entry.ElapsedMinutes.HasValue) lastElapsed = entry.ElapsedMinutes;

            Console.WriteLine($"  {marker,2} {entry.SortKey,6:F0}  {timeStr,-24} {elapsed,-12} {preview}");

            if (entry.IsConflict)
            {
                Console.WriteLine($"         ↳ CONFLICT: {entry.ConflictNote}");
                conflicts++;
            }
        }

        Console.WriteLine(new string('─', 92));
        Console.WriteLine();
        if (lastElapsed.HasValue)
            Console.WriteLine($"  Total story span:  ~{FormatElapsed(lastElapsed.Value)}");
        Console.WriteLine($"  Beats: {allEntries.Count}  ·  Time-anchored: {anchored}  ·  Conflicts: {conflicts}");
        Console.WriteLine();

        return 0;
    }

    private static async Task<List<BeatRow>> LoadBeatsAsync(
        ProseDbContext db, Guid nodeId, string? episodeTitle)
    {
        var raw = await db.BeatNodes
            .Where(sb => sb.NodeId == nodeId && true)
            .OrderBy(sb => sb.SortKey)
            .Join(db.Beats, sb => sb.BeatId, b => b.Id,
                (sb, b) => new { sb.SortKey, b.Title, b.Text })
            .ToListAsync();

        return raw.Select(r => new BeatRow(r.SortKey, r.Title, r.Text, episodeTitle)).ToList();
    }

    private static async Task<List<TimelineRow>?> CallLlmChunkAsync(
        ILlmService llm, List<BeatRow> beats, int globalBeatOffset, string? priorContext)
    {
        const string systemPrompt = """
            You are a story continuity editor. Extract a timeline from the prose beats provided.

            For EACH beat produce one JSON object. Return ONLY a valid JSON array — no markdown fences, no prose, nothing else.

            Fields per object:
            {
              "beatIndex": 1,                     // 1-based global beat index from the [BEAT N] label
              "sortKey": 5,                        // the sk= value from the label
              "anchor": "3:44 AM",                 // verbatim text from prose; null if no time reference
              "absoluteTime": "Day 1, 03:44",      // best-estimate story time; null if unresolvable
              "elapsedMinutes": 0,                 // minutes from the FIRST anchored beat in THIS CHUNK; null if unknown
              "isConflict": false,                 // true if this beat's time contradicts the prior sequence
              "conflictNote": null                 // one-sentence explanation when isConflict is true
            }

            Rules:
            - "elapsedMinutes" is from the first time reference found in THIS chunk (unless PRIOR CONTEXT supplies one, then count from that).
            - If a beat has no time signal, carry forward the prior beat's time; set anchor=null.
            - "Day N" counts from the first explicit calendar day. If no day boundary is crossed, use Day 1.
            - Approximations: dawn≈05:30, morning≈08:00, noon=12:00, afternoon≈14:00, dusk/evening≈19:00, night≈22:00, small hours/3AM≈03:00, midnight=00:00.
            - Parallel POV beats (same time as prior, different character) are NOT a conflict; set elapsedMinutes to the same value as the beat they run alongside.
            - Flag isConflict=true only when a beat explicitly states a time that logically cannot follow the prior stated time.
            - Return ONLY the JSON array. Ensure the array is complete and properly closed.
            """;

        var sb = new StringBuilder();
        if (priorContext is not null)
            sb.AppendLine($"PRIOR CONTEXT: {priorContext}").AppendLine();

        for (int i = 0; i < beats.Count; i++)
        {
            var b = beats[i];
            var epPart   = b.EpisodeTitle is not null ? $" / ep=\"{b.EpisodeTitle}\"" : "";
            var beatPart = b.Title is not null ? $" / \"{b.Title}\"" : "";
            sb.AppendLine($"[BEAT {globalBeatOffset + i + 1} / sk={b.SortKey:F0}{epPart}{beatPart}]");
            sb.AppendLine(b.Text.Trim());
            sb.AppendLine();
        }

        string raw;
        try
        {
            raw = await llm.GenerateAsync(systemPrompt, sb.ToString(), temperature: 0.1, maxTokens: 16000);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[timeline] LLM error: {ex.Message}");
            return null;
        }

        var json = raw.Trim();
        if (json.StartsWith("```"))
        {
            json = string.Join("\n", json.Split('\n').Skip(1));
            var end = json.LastIndexOf("```", StringComparison.Ordinal);
            if (end >= 0) json = json[..end].Trim();
        }

        try
        {
            return JsonSerializer.Deserialize<List<TimelineRow>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[timeline] JSON parse error: {ex.Message}");
            Console.Error.WriteLine($"[timeline] raw (first 800 chars):\n{raw[..Math.Min(raw.Length, 800)]}");
            return null;
        }
    }

    private static string FormatElapsed(int minutes)
    {
        if (minutes < 60) return $"{minutes}m";
        var h = minutes / 60;
        var m = minutes % 60;
        var d = h / 24;
        h %= 24;
        if (d > 0) return m == 0 ? $"{d}d {h}h" : $"{d}d {h}h {m}m";
        return m == 0 ? $"{h}h" : $"{h}h {m}m";
    }

    private record BeatRow(double SortKey, string? Title, string Text, string? EpisodeTitle);

    private class TimelineRow
    {
        [JsonPropertyName("beatIndex")]      public int     BeatIndex      { get; set; }
        [JsonPropertyName("sortKey")]        public double  SortKey        { get; set; }
        [JsonPropertyName("anchor")]         public string? Anchor         { get; set; }
        [JsonPropertyName("absoluteTime")]   public string? AbsoluteTime   { get; set; }
        [JsonPropertyName("elapsedMinutes")] public int?    ElapsedMinutes { get; set; }
        [JsonPropertyName("isConflict")]     public bool    IsConflict     { get; set; }
        [JsonPropertyName("conflictNote")]   public string? ConflictNote   { get; set; }
    }
}
