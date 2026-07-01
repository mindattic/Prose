using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Generates and persists the STRAND BIBLE — a dry, structural plot document
/// created before any prose is written. The bible defines logline, premise,
/// register, characters, a numbered beat spine, and seeds-and-payoffs.
///
/// The prose engine (BeatGeneratorService / Strand.razor LLM sheet) reads the
/// bible as <c>StoryBibleContext</c> on every beat so the full arc is always
/// in view.
///
/// Usage:
///   1. Call <see cref="GenerateAndSaveAsync"/> once (or whenever the story plan changes).
///   2. Read <see cref="GetBibleTextAsync"/> to surface the bible in the UI.
///   3. The <see cref="BeatPlan"/> list returned by <see cref="ParseBeatSpine"/>
///      drives planned-beat creation: each entry becomes a Beat row with
///      <c>Synopsis</c> = the plan, ready for prose expansion.
/// </summary>
public class StrandBibleService
{
    private readonly ILlmService llm;
    private readonly DatabaseService canonDb;
    private readonly IDbContextFactory<StreetSamuraiDbContext> dbFactory;
    private readonly ILogger<StrandBibleService> log;

    public StrandBibleService(
        ILlmService llm,
        DatabaseService canonDb,
        IDbContextFactory<StreetSamuraiDbContext> dbFactory,
        ILogger<StrandBibleService> log)
    {
        this.llm      = llm;
        this.canonDb  = canonDb;
        this.dbFactory = dbFactory;
        this.log       = log;
    }

    // ── Generation ────────────────────────────────────────────────────────

    /// <summary>
    /// Generate a strand bible from <paramref name="seed"/>, save it to the
    /// strand row, and create one planned <see cref="Beat"/> per spine entry
    /// (only if the strand has no beats yet). Returns the markdown text.
    /// </summary>
    public async Task<string> GenerateAndSaveAsync(
        Guid strandId,
        string seed,
        string? title        = null,
        int    targetBeats   = 12,
        CancellationToken ct = default)
    {
        var literaryRules = canonDb.GetLiteraryRulesPrompt() ?? "";
        var system = BuildBibleSystemPrompt(targetBeats, literaryRules);
        var user = $"SEED: {seed}\n{(title != null ? $"WORKING TITLE: {title}" : "")}\nTARGET BEATS: {targetBeats}\n\nWrite the strand bible now.";

        log.LogInformation("[bible] Generating for strand {StrandId} — seed: {Seed}", strandId, seed);

        string bibleText;
        try
        {
            bibleText = await llm.GenerateAsync(system, user, temperature: 0.7, maxTokens: 4096, ct: ct);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "[bible] LLM call failed for strand {StrandId}", strandId);
            throw;
        }

        bibleText = bibleText.Trim();

        // Persist on the strand row
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var strand = await db.Strands.FindAsync([strandId], ct)
            ?? throw new InvalidOperationException($"Strand {strandId} not found.");

        strand.StrandBible            = bibleText;
        strand.StrandBibleGeneratedAt = DateTime.UtcNow;
        strand.UpdatedAt              = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        log.LogInformation("[bible] Saved {Chars} chars for strand {StrandId}", bibleText.Length, strandId);

        // Create planned beats from the spine (only when the strand is blank)
        var beatPlans = ParseBeatSpine(bibleText);
        if (beatPlans.Count > 0)
            await CreatePlannedBeatsAsync(db, strandId, beatPlans, ct);

        return bibleText;
    }

    /// <summary>
    /// Generate a bible text only — no DB writes. Used by PremiseToOutlineService
    /// to produce competing outlines before picking a winner.
    /// </summary>
    public async Task<string> GenerateTextAsync(
        string seed,
        string? title      = null,
        int targetBeats    = 12,
        CancellationToken ct = default)
    {
        var literaryRules = canonDb.GetLiteraryRulesPrompt() ?? "";
        var system = BuildBibleSystemPrompt(targetBeats, literaryRules);
        var user   = $"SEED: {seed}\n{(title != null ? $"WORKING TITLE: {title}" : "")}\nTARGET BEATS: {targetBeats}\n\nWrite the strand bible now.";
        return (await llm.GenerateAsync(system, user, temperature: 0.75, maxTokens: 4096, ct: ct)).Trim();
    }

    /// <summary>
    /// Save a pre-generated bible text to an existing strand row and create
    /// planned beats from its spine. Used after a compete-selection picks a winner.
    /// </summary>
    public async Task SaveBibleAndCreateBeatsAsync(
        Guid strandId,
        string bibleText,
        CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var strand = await db.Strands.FindAsync([strandId], ct)
            ?? throw new InvalidOperationException($"Strand {strandId} not found.");

        strand.StrandBible            = bibleText;
        strand.StrandBibleGeneratedAt = DateTime.UtcNow;
        strand.UpdatedAt              = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        var beatPlans = ParseBeatSpine(bibleText);
        if (beatPlans.Count > 0)
            await CreatePlannedBeatsAsync(db, strandId, beatPlans, ct);

        log.LogInformation("[bible] Saved winning bible ({Chars} chars) for strand {StrandId}", bibleText.Length, strandId);
    }

    // ── Retrieval ─────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the bible text for <paramref name="strandId"/>, or null if none exists.
    /// </summary>
    public async Task<string?> GetBibleTextAsync(Guid strandId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.Strands
            .Where(s => s.Id == strandId)
            .Select(s => s.StrandBible)
            .FirstOrDefaultAsync(ct);
    }

    // ── Beat spine parsing ────────────────────────────────────────────────

    /// <summary>
    /// Parse the "## BEAT SPINE" section of a bible markdown into a list of
    /// <see cref="BeatPlan"/> records. Returns an empty list when the section
    /// is missing or malformed.
    /// </summary>
    public static List<BeatPlan> ParseBeatSpine(string bibleText)
    {
        var plans = new List<BeatPlan>();

        // Isolate the BEAT SPINE section (everything up to the next ## or end)
        var spineMatch = Regex.Match(
            bibleText,
            @"##\s*BEAT SPINE\s*\n(.+?)(?=\n##|\z)",
            RegexOptions.Singleline | RegexOptions.IgnoreCase);

        if (!spineMatch.Success) return plans;

        var spineBlock = spineMatch.Groups[1].Value;

        // Match: "N. [STRUCTURE-ROLE] Title — Description"
        //    or: "N. Title — Description"  (no role bracket)
        var lineRegex = new Regex(
            @"^\s*(\d+)\.\s+(?:\[([^\]]+)\]\s+)?(.+?)(?:\s+[—–-]{1,2}\s+(.+))?$",
            RegexOptions.Multiline);

        foreach (Match m in lineRegex.Matches(spineBlock))
        {
            var index     = int.Parse(m.Groups[1].Value);
            var role      = m.Groups[2].Success ? m.Groups[2].Value.Trim() : "";
            var titlePart = m.Groups[3].Value.Trim();
            var goalPart  = m.Groups[4].Success ? m.Groups[4].Value.Trim() : "";

            if (string.IsNullOrEmpty(goalPart))
            {
                // No em-dash split — treat the whole thing as the goal
                goalPart  = titlePart;
                titlePart = $"Beat {index}";
            }

            plans.Add(new BeatPlan(index, titlePart, goalPart, role));
        }

        return plans;
    }

    // ── Private helpers ───────────────────────────────────────────────────

    private static string BuildBibleSystemPrompt(int targetBeats, string literaryRules) => $"""
        You are a story architect. Given a one-line seed, produce a STRAND BIBLE —
        a dry, structural plan that a prose engine will execute beat by beat.

        Rules for the bible:
        - Declarative sentences only. No purple prose. No florid language.
        - Every beat entry is a fact about what happens, not a lyric about it.
        - Be specific: name characters, name costs, name objects that matter.
        - The bible is a spine. Flesh lives in the prose pass.

        LITERARY RULES (follow these — they define the world's voice):
        {literaryRules}

        Output EXACTLY this markdown format. No extra sections. No preamble.

        # STRAND BIBLE: [Working Title]

        ## LOGLINE
        [One sentence. Who. Does what. At what cost.]

        ## PREMISE
        [2–3 sentences. World situation. Inciting condition. What is at stake.]

        ## REGISTER
        [Tone + pacing. 1–2 sentences. E.g. "Dark-wry. Quiet moments earn their place before the violence."]

        ## CHARACTERS
        - **[Name]** — [Role in this story]. Arc: wants [external goal], needs [internal truth], ends [outcome].

        ## BEAT SPINE
        [Exactly {targetBeats} numbered entries. One line each.]
        [Format: N. [STRUCTURE-ROLE] Title — What happens. What it costs or reveals.]
        [Valid structure roles: OPENING, COMPLICATION, ESCALATION, REVELATION, CONFRONTATION, CLIMAX, RESOLUTION, TRANSITION, QUIET-MOMENT]

        ## SEEDS & PAYOFFS
        - Beat [X] plants [what thread] → Beat [Y] pays it off.
        """;

    private async Task CreatePlannedBeatsAsync(
        StreetSamuraiDbContext db,
        Guid                  strandId,
        List<BeatPlan>        plans,
        CancellationToken     ct)
    {
        var existing = await db.StrandBeats.CountAsync(sb => sb.StrandId == strandId, ct);
        if (existing > 0)
        {
            log.LogInformation("[bible] Strand {StrandId} already has {Count} beats — skipping planned beat creation.", strandId, existing);
            return;
        }

        var baseNumber = (await db.Beats.MaxAsync(b => (int?)b.Number, ct) ?? 0) + 1;
        var now = DateTime.UtcNow;
        int beatIndex = 0;
        foreach (var plan in plans.OrderBy(p => p.Index))
        {
            var beat = new Beat
            {
                Id            = Guid.CreateVersion7(),
                Number        = baseNumber + beatIndex++,
                BeatTitle     = plan.Title,
                Synopsis      = plan.Goal,
                StructureRole = MapStructureRole(plan.StructureRole),
                Text          = "",
                CreatedAt     = now,
                UpdatedAt     = now,
            };
            db.Beats.Add(beat);
            db.StrandBeats.Add(new StrandBeat
            {
                StrandId  = strandId,
                BeatId    = beat.Id,
                SortKey   = plan.Index * 100.0,
                IsEnabled = true,
            });
        }

        await db.SaveChangesAsync(ct);
        log.LogInformation("[bible] Created {Count} planned beats for strand {StrandId}", plans.Count, strandId);
    }

    private static string MapStructureRole(string bibleRole) => bibleRole.ToUpperInvariant() switch
    {
        "OPENING"       => "inciting-incident",
        "COMPLICATION"  => "rising-action",
        "ESCALATION"    => "rising-action",
        "REVELATION"    => "rising-action",
        "CONFRONTATION" => "climax",
        "CLIMAX"        => "climax",
        "RESOLUTION"    => "denouement",
        "TRANSITION"    => "transition",
        "QUIET-MOMENT"  => "scene-break",
        _               => "rising-action",
    };
}

/// <summary>One entry from a strand bible's ## BEAT SPINE section.</summary>
public record BeatPlan(int Index, string Title, string Goal, string StructureRole);
