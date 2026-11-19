using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Interfaces;

namespace Prose.Core.Services;

/// <summary>
/// The LLM pass behind <c>prose --timeline</c> and <c>prose --stamp-in-world</c>: reads a book's
/// beats in reading order, asks the model for each beat's story-relative clock ("Day N, HH:MM"),
/// elapsed minutes, explicit time anchors, continuity conflicts and, when the prose states one,
/// an absolute in-world calendar date (PRS-T1069).
///
/// <para><b>Order.</b> A node with direct beats is read by <c>BeatNodes.SortKey</c>; a book is read
/// leaf-by-leaf in <see cref="NodeWorkbenchService.GetLeafDescendantIdsAsync"/>'s own order (NOT
/// re-sorted by <c>Node.SortKey</c>, which is only comparable within one sibling group), so a
/// split-collection book whose real chapters sit two levels down is still walked. Each episode is
/// sent in chunks of at most <see cref="ChunkSize"/> beats, with the previous chunk's last clock
/// carried forward as context so relative references resolve across the boundary.</para>
///
/// <para><b>Clock overlay only.</b> Per the author ruling on <see cref="Beat"/>,
/// <see cref="Beat.StoryPosition"/> is authoritative for order; what this infers is the overlay
/// that keeps day and night aligned. Nothing here writes; callers decide what to persist.</para>
/// </summary>
public static class BeatTimelineInference
{
    /// <summary>Max beats per LLM call. Above this the output JSON risks hitting the token cap.</summary>
    public const int ChunkSize = 60;

    /// <summary>Absolute in-world dates outside this year range are rejected: the corpus is set in
    /// the 23rd century, so anything else is a misread (a page number, a call sign, a model guess).</summary>
    public const int MinYear = 2100, MaxYear = 2399;

    /// <summary>One beat in reading order with what the model inferred for it.</summary>
    /// <param name="Index">1-based position in this pass's reading order (the [BEAT N] label).</param>
    /// <param name="DayIndex">1-based story day from "Day N"; null when unknown.</param>
    /// <param name="MinuteOfDay">Minutes after midnight from "HH:MM"; null when unknown.</param>
    /// <param name="AbsoluteDate">An absolute in-world date the prose itself states, 2100-2399
    /// only. Missing month/day are filled with 1; <see cref="AbsoluteDatePrecision"/> says which
    /// parts were actually stated.</param>
    /// <param name="AbsoluteDatePrecision">"year", "month", "day" or "minute"; null with no date.</param>
    public sealed record Row(
        int Index,
        Guid BeatId,
        int Number,
        int? StoryPosition,
        double SortKey,
        string? ChapterTitle,
        string? BeatTitle,
        string Text,
        string? Anchor,
        string? AbsoluteTime,
        int? DayIndex,
        int? MinuteOfDay,
        int? ElapsedMinutes,
        bool IsConflict,
        string? ConflictNote,
        DateTime? AbsoluteDate,
        string? AbsoluteDatePrecision,
        string? DateAnchor)
    {
        /// <summary>True when the prose itself states a time for this beat (not carried forward).</summary>
        public bool IsAnchored => Anchor is not null;

        /// <summary>True when both the story day and the time of day are known.</summary>
        public bool HasClock => DayIndex.HasValue && MinuteOfDay.HasValue;

        /// <summary>Minutes from Day 1, 00:00 to this beat; null when <see cref="HasClock"/> is false.</summary>
        public int? OffsetMinutes => HasClock ? (DayIndex!.Value - 1) * 1440 + MinuteOfDay!.Value : null;
    }

    /// <summary>The pass's outcome. <see cref="Error"/> is set (and <see cref="Rows"/> empty)
    /// when there was nothing to read or every chunk failed.</summary>
    public sealed record Result(string NodeTitle, string? NodeSlug, List<Row> Rows, int Chunks, int FailedChunks, string? Error);

    /// <summary>
    /// Runs the inference for <paramref name="node"/>. Progress lines go to <paramref name="output"/>
    /// and chunk failures to <paramref name="errors"/>, worded as <c>--timeline</c> always printed
    /// them; pass null to stay silent.
    /// </summary>
    public static async Task<Result> InferAsync(
        ProseDbContext db, ILlmService llm, Node node,
        TextWriter? output = null, TextWriter? errors = null, CancellationToken ct = default)
    {
        var directBeats = await LoadBeatsAsync(db, node.Id, episodeTitle: null, ct);
        var chunks = new List<(string? episodeTitle, List<BeatRow> beats)>();

        if (directBeats.Count > 0)
        {
            for (int start = 0; start < directBeats.Count; start += ChunkSize)
                chunks.Add((null, directBeats.Skip(start).Take(ChunkSize).ToList()));
        }
        else
        {
            // Descend to LEAF nodes, not just direct children: a split-collection book (Book ->
            // "Chapter N" container with 0 direct beats -> real chapters -> beats) has its real
            // chapters two levels down. Same bug class fixed in WorkflowMonitorService (2026-08-09)
            // and BackfillCoverageCli (2026-08-10).
            var leafIds = await NodeWorkbenchService.GetLeafDescendantIdsAsync(db, node.Id);
            var byId = await db.Nodes.AsNoTracking().IgnoreQueryFilters()
                .Where(s => leafIds.Contains(s.Id))
                .Select(s => new { s.Id, s.Title })
                .ToDictionaryAsync(s => s.Id, ct);
            var children = leafIds.Where(byId.ContainsKey).Select(id => byId[id]).ToList();

            if (children.Count == 0)
                return new Result(node.Title, node.Slug, [], 0, 0, "no beats found (no direct beats and no child episodes)");

            output?.WriteLine($"[timeline] book with {children.Count} episode(s)");
            foreach (var ep in children)
            {
                var epBeats = await LoadBeatsAsync(db, ep.Id, ep.Title, ct);
                if (epBeats.Count == 0) continue;
                for (int start = 0; start < epBeats.Count; start += ChunkSize)
                    chunks.Add((ep.Title, epBeats.Skip(start).Take(ChunkSize).ToList()));
            }
        }

        int totalBeats = chunks.Sum(c => c.beats.Count);
        output?.WriteLine($"[timeline] {totalBeats} beat(s) across {chunks.Count} chunk(s) — extracting time anchors…");

        var rows = new List<Row>();
        string? priorContext = null;
        int globalBeatOffset = 0;        // cumulative beat count before this chunk
        int? globalElapsedOffset = null; // minutes from book start to start of this chunk
        int failed = 0;

        foreach (var (epTitle, beats) in chunks)
        {
            ct.ThrowIfCancellationRequested();
            var label = epTitle is not null ? $"episode \"{epTitle}\"" : "beats";
            output?.Write($"  [{label}: {beats.Count} beat(s)]  ");

            var entries = await CallLlmChunkAsync(llm, beats, globalBeatOffset, priorContext, errors, ct);
            if (entries is null)
            {
                errors?.WriteLine("  [timeline] chunk failed — skipping");
                failed++;
                globalBeatOffset += beats.Count;
                continue;
            }

            output?.WriteLine($"got {entries.Count} row(s)");

            int? chunkLastElapsed = null;
            foreach (var e in entries)
            {
                if (!e.ElapsedMinutes.HasValue) continue;
                globalElapsedOffset ??= 0; // first anchor in the whole book
                e.ElapsedMinutes += globalElapsedOffset.Value;
                chunkLastElapsed = e.ElapsedMinutes;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                // By the beatIndex the model echoed back (global, 1-based), not by row position:
                // one skipped or merged row used to shift every later row onto the wrong beat.
                var beatIdx = entries[i].BeatIndex - globalBeatOffset - 1;
                if (beatIdx < 0 || beatIdx >= beats.Count) beatIdx = Math.Min(i, beats.Count - 1);
                rows.Add(ToRow(entries[i], beats[beatIdx], globalBeatOffset + beatIdx + 1));
            }

            var lastEntry = entries.LastOrDefault(e => e.AbsoluteTime is not null);
            if (lastEntry is not null)
                priorContext = $"The previous chunk ended at approximately {lastEntry.AbsoluteTime}" +
                               (chunkLastElapsed.HasValue ? $" (elapsed from story start: {FormatElapsed(chunkLastElapsed.Value)})" : "") + ".";

            if (chunkLastElapsed.HasValue)
                globalElapsedOffset = chunkLastElapsed;

            globalBeatOffset += beats.Count;
        }

        return new Result(node.Title, node.Slug, rows, chunks.Count, failed,
            rows.Count == 0 ? "no results" : null);
    }

    /// <summary>"Day 3, 14:05" (also "Day 3 14:05", "day 3, 2:05 PM") → (3, 845). Either part may
    /// come back null when the model could not resolve it.</summary>
    public static (int? Day, int? MinuteOfDay) ParseClock(string? absoluteTime)
    {
        if (string.IsNullOrWhiteSpace(absoluteTime)) return (null, null);
        int? day = null, minute = null;

        var d = Regex.Match(absoluteTime, @"\bday\s*(\d{1,4})\b", RegexOptions.IgnoreCase);
        if (d.Success && int.TryParse(d.Groups[1].Value, out var dn) && dn >= 1) day = dn;

        var t = Regex.Match(absoluteTime, @"\b(\d{1,2}):(\d{2})\s*(am|pm|a\.m\.|p\.m\.)?", RegexOptions.IgnoreCase);
        if (t.Success)
        {
            var h = int.Parse(t.Groups[1].Value, CultureInfo.InvariantCulture);
            var m = int.Parse(t.Groups[2].Value, CultureInfo.InvariantCulture);
            var ampm = t.Groups[3].Value.Replace(".", "").ToLowerInvariant();
            if (ampm == "pm" && h < 12) h += 12;
            if (ampm == "am" && h == 12) h = 0;
            if (h is >= 0 and < 24 && m is >= 0 and < 60) minute = h * 60 + m;
        }
        return (day, minute);
    }

    /// <summary>Parses the model's ISO date at whatever precision the prose stated it
    /// ("2226", "2226-03", "2226-03-14", "2226-03-14T09:30"). Returns null for anything that does
    /// not parse or falls outside <see cref="MinYear"/>-<see cref="MaxYear"/> — never a guess.</summary>
    public static (DateTime? Date, string? Precision) ParseAbsoluteDate(string? iso)
    {
        if (string.IsNullOrWhiteSpace(iso)) return (null, null);
        var s = iso.Trim();
        var m = Regex.Match(s, @"^(\d{4})(?:-(\d{2})(?:-(\d{2})(?:[T ](\d{2}):(\d{2})(?::\d{2})?)?)?)?$");
        if (!m.Success) return (null, null);

        var year = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
        if (year < MinYear || year > MaxYear) return (null, null);
        var month = m.Groups[2].Success ? int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture) : 1;
        var day = m.Groups[3].Success ? int.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture) : 1;
        var hour = m.Groups[4].Success ? int.Parse(m.Groups[4].Value, CultureInfo.InvariantCulture) : 0;
        var minute = m.Groups[5].Success ? int.Parse(m.Groups[5].Value, CultureInfo.InvariantCulture) : 0;
        if (month is < 1 or > 12 || day < 1 || day > DateTime.DaysInMonth(year, month) || hour > 23 || minute > 59)
            return (null, null);

        var precision = m.Groups[4].Success ? "minute" : m.Groups[3].Success ? "day" : m.Groups[2].Success ? "month" : "year";
        return (new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Unspecified), precision);
    }

    public static string FormatElapsed(int minutes)
    {
        if (minutes < 60) return $"{minutes}m";
        var h = minutes / 60;
        var m = minutes % 60;
        var d = h / 24;
        h %= 24;
        if (d > 0) return m == 0 ? $"{d}d {h}h" : $"{d}d {h}h {m}m";
        return m == 0 ? $"{h}h" : $"{h}h {m}m";
    }

    private static Row ToRow(TimelineRow e, BeatRow b, int index)
    {
        var (day, minute) = ParseClock(e.AbsoluteTime);
        var (date, precision) = ParseAbsoluteDate(e.AbsoluteDate);
        return new Row(index, b.BeatId, b.Number, b.StoryPosition, b.SortKey, b.EpisodeTitle, b.Title, b.Text,
            e.Anchor, e.AbsoluteTime, day, minute, e.ElapsedMinutes, e.IsConflict, e.ConflictNote,
            date, precision, date is null ? null : e.DateAnchor);
    }

    private static async Task<List<BeatRow>> LoadBeatsAsync(ProseDbContext db, Guid nodeId, string? episodeTitle, CancellationToken ct)
    {
        var raw = await db.BeatNodes
            .Where(sb => sb.NodeId == nodeId)
            .OrderBy(sb => sb.SortKey)
            .Join(db.Beats, sb => sb.BeatId, b => b.Id,
                (sb, b) => new { sb.SortKey, b.Id, b.Number, b.StoryPosition, b.Title, b.Text })
            .ToListAsync(ct);

        return raw.Select(r => new BeatRow(r.Id, r.Number, r.StoryPosition, r.SortKey, r.Title, r.Text, episodeTitle)).ToList();
    }

    private static async Task<List<TimelineRow>?> CallLlmChunkAsync(
        ILlmService llm, List<BeatRow> beats, int globalBeatOffset, string? priorContext,
        TextWriter? errors, CancellationToken ct)
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
              "conflictNote": null,                // one-sentence explanation when isConflict is true
              "absoluteDate": null,                // ISO calendar date the prose of THIS beat explicitly states; null otherwise
              "dateAnchor": null                   // verbatim prose text the absoluteDate came from; null when absoluteDate is null
            }

            Rules:
            - "elapsedMinutes" is from the first time reference found in THIS chunk (unless PRIOR CONTEXT supplies one, then count from that).
            - If a beat has no time signal, carry forward the prior beat's time; set anchor=null.
            - "Day N" counts from the first explicit calendar day. If no day boundary is crossed, use Day 1.
            - Approximations: dawn≈05:30, morning≈08:00, noon=12:00, afternoon≈14:00, dusk/evening≈19:00, night≈22:00, small hours/3AM≈03:00, midnight=00:00.
            - Parallel POV beats (same time as prior, different character) are NOT a conflict; set elapsedMinutes to the same value as the beat they run alongside.
            - Flag isConflict=true only when a beat explicitly states a time that logically cannot follow the prior stated time.
            - "absoluteDate": ONLY when the beat's own prose states a calendar year or date for when THIS scene happens
              (e.g. "March 14, 2226" -> "2226-03-14"; "in 2230" -> "2230"; "the spring of 2217" -> "2217"; "June 2219" -> "2219-06").
              Write it at exactly the precision stated: "YYYY", "YYYY-MM", "YYYY-MM-DD", or "YYYY-MM-DDTHH:MM" when a clock time is stated with the date.
              Never infer, estimate, or carry a date forward from another beat; never fill in a month or day the prose does not state.
              Years mentioned as history, memory, or someone's birth year are NOT the scene's date — use null.
              The setting is the 23rd century: only years 2100-2399 are valid; anything else is null.
            - Return ONLY the JSON array. Ensure the array is complete and properly closed.
            """;

        var sb = new StringBuilder();
        if (priorContext is not null)
            sb.AppendLine($"PRIOR CONTEXT: {priorContext}").AppendLine();

        for (int i = 0; i < beats.Count; i++)
        {
            var b = beats[i];
            var epPart = b.EpisodeTitle is not null ? $" / ep=\"{b.EpisodeTitle}\"" : "";
            var beatPart = b.Title is not null ? $" / \"{b.Title}\"" : "";
            sb.AppendLine($"[BEAT {globalBeatOffset + i + 1} / sk={b.SortKey:F0}{epPart}{beatPart}]");
            sb.AppendLine(b.Text.Trim());
            sb.AppendLine();
        }

        string raw;
        try
        {
            raw = await llm.GenerateAsync(systemPrompt, sb.ToString(), temperature: 0.1, maxTokens: 16000, ct: ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            errors?.WriteLine($"[timeline] LLM error: {ex.Message}");
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
            errors?.WriteLine($"[timeline] JSON parse error: {ex.Message}");
            errors?.WriteLine($"[timeline] raw (first 800 chars):\n{raw[..Math.Min(raw.Length, 800)]}");
            return null;
        }
    }

    private sealed record BeatRow(Guid BeatId, int Number, int? StoryPosition, double SortKey, string? Title, string Text, string? EpisodeTitle);

    private sealed class TimelineRow
    {
        [JsonPropertyName("beatIndex")]      public int     BeatIndex      { get; set; }
        [JsonPropertyName("sortKey")]        public double  SortKey        { get; set; }
        [JsonPropertyName("anchor")]         public string? Anchor         { get; set; }
        [JsonPropertyName("absoluteTime")]   public string? AbsoluteTime   { get; set; }
        [JsonPropertyName("elapsedMinutes")] public int?    ElapsedMinutes { get; set; }
        [JsonPropertyName("isConflict")]     public bool    IsConflict     { get; set; }
        [JsonPropertyName("conflictNote")]   public string? ConflictNote   { get; set; }
        // Read as a raw element: a model that answers "in 2230" with the bare number 2230 must not
        // fail the whole chunk's parse, which a string property would.
        [JsonPropertyName("absoluteDate")]   public JsonElement? AbsoluteDateRaw { get; set; }
        [JsonIgnore] public string? AbsoluteDate => AbsoluteDateRaw switch
        {
            { ValueKind: JsonValueKind.String } el => el.GetString(),
            { ValueKind: JsonValueKind.Number } el => el.GetRawText(),
            _ => null,
        };
        [JsonPropertyName("dateAnchor")]     public string? DateAnchor     { get; set; }
    }
}
