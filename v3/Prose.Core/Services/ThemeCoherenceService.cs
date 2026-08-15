using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Interfaces;

namespace Prose.Core.Services;

// ─────────────────────────────────────────────────────────────────────────────
// ThemeCoherenceService
//
// Operationalizes the "controlling idea" / "moral premise" craft concept shared
// by McKee ("Story"), Truby ("The Anatomy of Story"), and Vogler — a book's theme
// is a testable, value-laden claim ("true strength is admitting weakness," not
// just the topic "strength") that the plot's causal chain of choices proves or
// disproves. Before this service, the engine only ever encoded theme as a
// NEGATIVE generation-time instruction (StoryScienceService: "don't impose theme
// early, let it emerge") — nothing ever checked, after the fact, whether a book
// actually HAS a coherent controlling idea, or whether that idea got told to the
// reader as commentary instead of dramatized through consequence. This is the
// first service to make theme a measurable, audited property rather than a
// prohibition with no corresponding check.
//
//   • AnalyzeAsync — infers the controlling idea from Seed/Bible + the book's own
//     opening and closing beats (the two places a controlling idea is structurally
//     supposed to be legible: posed, then answered), flags theme stated as
//     narrator/character commentary rather than dramatized through choice, and
//     flags when the ending doesn't appear to engage the opening's question at all.
// ─────────────────────────────────────────────────────────────────────────────

public class ThemeCoherenceService(
    ILlmService llm,
    IDbContextFactory<ProseDbContext> dbFactory)
{
    static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private const int BookendBeatCount = 2;

    public async Task<ThemeCoherenceResult> AnalyzeAsync(Guid nodeId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var node = await db.Nodes.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == nodeId, ct)
            ?? throw new InvalidOperationException($"Node {nodeId} not found.");

        // SS-A43: for book-mode nodes, beats live on chapter children.
        // Recurses past any nested Collection (2026-08-09 fix).
        var searchIds = await NodeWorkbenchService.GetLeafDescendantIdsAsync(db, nodeId, ct);

        var beats = await (
            from sb in db.BeatNodes.AsNoTracking()
            join b in db.Beats.AsNoTracking() on sb.BeatId equals b.Id
            where searchIds.Contains(sb.NodeId) && true && b.Text != null && b.Text != ""
            orderby sb.SortKey
            select new { b.Number, b.Text }
        ).ToListAsync(ct);

        if (beats.Count == 0)
            return new ThemeCoherenceResult { NodeSlug = node.Slug ?? "", Error = "No written beats found." };

        var opening = beats.Take(BookendBeatCount).ToList();
        var closing = beats.Count > BookendBeatCount
            ? beats.Skip(Math.Max(0, beats.Count - BookendBeatCount)).ToList()
            : [];
        // 2026-08-14 fix: opening+closing alone (the original 4-beat sample) is structurally
        // blind to a book whose theme is built or complicated in the middle — the normal place
        // for that to happen on any book long enough to have one. Adds a midpoint sample so the
        // model has at least some mid-book signal; still one LLM call, not a per-beat scan.
        var midpointStart = beats.Count / 2 - BookendBeatCount / 2;
        var midpoint = beats.Count > BookendBeatCount * 3
            ? beats.Skip(Math.Max(BookendBeatCount, midpointStart)).Take(BookendBeatCount).ToList()
            : [];

        var openingText = string.Join("\n---\n", opening.Select(b => $"[Beat {b.Number}]\n{Truncate(b.Text!, 1500)}"));
        var midpointText = midpoint.Count > 0
            ? string.Join("\n---\n", midpoint.Select(b => $"[Beat {b.Number}]\n{Truncate(b.Text!, 1500)}"))
            : "(book too short for a distinct midpoint sample)";
        var closingText = closing.Count > 0
            ? string.Join("\n---\n", closing.Select(b => $"[Beat {b.Number}]\n{Truncate(b.Text!, 1500)}"))
            : "(book has fewer than " + (BookendBeatCount + 1) + " written beats — opening and closing overlap)";

        var system = """
            You are a story-structure analyst trained on McKee's "Story" and Truby's "The Anatomy
            of Story." Identify a book's CONTROLLING IDEA — a single value-laden, testable claim
            about how to live (e.g. "true strength is admitting weakness," not the bare topic
            "strength") — and judge whether the book dramatizes it through character choice and
            consequence rather than stating it as commentary.

            DEFINITIONS
            • Controlling Idea: one sentence, causally connects a value + the action that
              produces/destroys it (e.g. "Love conquers fear only when it's tested by real loss").
            • Theme stated as commentary: a narrator or character explicitly articulates the moral
              or lesson in their own voice ("She realized then that real strength was admitting you
              needed help") rather than the reader inferring it from what happens. This is a CRAFT
              violation (telling, not showing) distinct from a missing/incoherent theme.
            • Ending engages the opening: the closing beats' outcome is legible as an answer to (or
              a deliberate refusal to answer) the value-question the opening beats implicitly pose.
              A closing beat that resolves plot but never touches the opening's value-question fails
              this even if the plot itself is fully resolved.

            OUTPUT FORMAT: JSON only, no prose wrapper.
            {
              "controlling_idea": "...",
              "confidence": "high|medium|low",
              "theme_stated_as_commentary": true|false,
              "commentary_quote": "...(verbatim quote if true, else empty)",
              "commentary_beat_number": null|N,
              "ending_engages_opening": true|false,
              "diagnosis": "...(one paragraph)"
            }
            """;

        var user = $"""
            NODE: {node.Title ?? node.Slug}
            SEED: {node.Seed ?? "(none)"}
            BIBLE EXCERPT: {Truncate(node.NodeBible ?? "(none)", 1000)}

            OPENING BEATS:
            {openingText}

            MIDPOINT BEATS (where a theme is often complicated or tested, not just posed/answered):
            {midpointText}

            CLOSING BEATS:
            {closingText}
            """;

        var raw = await llm.GenerateAsync(system, user, temperature: 0.4, maxTokens: 900, ct: ct);
        // Throw rather than fabricate a stub result (2026-08-14 fix). The old fallback left
        // EndingEngagesOpening at its bool default (true) — harmless against the one caller that
        // already checks `Error != null` first and returns before reading it, but a live footgun
        // for any other caller, and it did nothing for the more likely partial-parse case: valid
        // JSON that simply omits ending_engages_opening would deserialize clean, Error would stay
        // null, and EndingEngagesOpening would silently read "true" with no error signal at all.
        // EndingEngagesOpening is now nullable (see model) so a missing field is visibly null,
        // not a fabricated pass, and a total parse failure surfaces as a thrown exception the
        // caller files as its own [incomplete] finding instead of silently returning nothing.
        var result = ParseJson<ThemeCoherenceResult>(raw)
            ?? throw new InvalidOperationException($"Could not parse theme-coherence response: {raw[..Math.Min(200, raw.Length)]}");
        result.NodeSlug = node.Slug ?? "";
        return result;
    }

    static string Truncate(string text, int max) =>
        text.Length <= max ? text : text[..max] + "\n[truncated]";

    static T? ParseJson<T>(string raw)
    {
        try
        {
            var start = raw.IndexOf('{');
            var end = raw.LastIndexOf('}');
            if (start < 0 || end < start) return default;
            return JsonSerializer.Deserialize<T>(raw[start..(end + 1)], new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });
        }
        catch { return default; }
    }
}

public class ThemeCoherenceResult
{
    [JsonPropertyName("controlling_idea")]         public string ControllingIdea         { get; set; } = "";
    [JsonPropertyName("confidence")]                public string Confidence              { get; set; } = "";
    [JsonPropertyName("theme_stated_as_commentary")]public bool   ThemeStatedAsCommentary  { get; set; }
    [JsonPropertyName("commentary_quote")]          public string CommentaryQuote          { get; set; } = "";
    [JsonPropertyName("commentary_beat_number")]    public int?   CommentaryBeatNumber     { get; set; }
    // Nullable, no default (2026-08-14 fix): a bool default of true meant a syntactically valid
    // JSON response that simply omitted this field would silently deserialize as "yes, engages" —
    // no parse error, no thrown exception, indistinguishable from a real judgment. Null now means
    // "the model didn't say" rather than "the model said yes."
    [JsonPropertyName("ending_engages_opening")]    public bool?  EndingEngagesOpening     { get; set; }
    [JsonPropertyName("diagnosis")]                 public string Diagnosis               { get; set; } = "";
    [JsonIgnore]                                    public string NodeSlug                { get; set; } = "";
    [JsonIgnore]                                    public string? Error                  { get; set; }
    [JsonIgnore]                                    public string? RawResponse            { get; set; }
}
