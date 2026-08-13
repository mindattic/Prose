using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Interfaces;

namespace Prose.Core.Services;

/// <summary>
/// Generates and persists the NODE BIBLE — a dry, structural plot document
/// created before any prose is written. The bible defines logline, premise,
/// register, characters, a numbered beat spine, and seeds-and-payoffs.
///
/// The prose engine (BeatGeneratorService / Node.razor LLM sheet) reads the
/// bible as <c>BookBibleContext</c> on every beat so the full arc is always
/// in view.
///
/// Usage:
///   1. Call <see cref="GenerateAndSaveAsync"/> once (or whenever the book plan changes).
///   2. Read <see cref="GetBibleTextAsync"/> to surface the bible in the UI.
///   3. The <see cref="BeatPlan"/> list returned by <see cref="ParseBeatSpine"/>
///      drives planned-beat creation: each entry becomes a Beat row with
///      <c>Synopsis</c> = the plan, ready for prose expansion.
/// </summary>
public class NodeBibleService
{
    private readonly ILlmService llm;
    private readonly DatabaseService canonDb;
    private readonly IDbContextFactory<ProseDbContext> dbFactory;
    private readonly ILogger<NodeBibleService> log;

    public NodeBibleService(
        ILlmService llm,
        DatabaseService canonDb,
        IDbContextFactory<ProseDbContext> dbFactory,
        ILogger<NodeBibleService> log)
    {
        this.llm      = llm;
        this.canonDb  = canonDb;
        this.dbFactory = dbFactory;
        this.log       = log;
    }

    // ── Generation ────────────────────────────────────────────────────────

    /// <summary>
    /// Generate a node bible from <paramref name="seed"/>, save it to the
    /// node row, and create one planned <see cref="Beat"/> per spine entry
    /// (only if the node has no beats yet). Returns the markdown text.
    /// </summary>
    public async Task<string> GenerateAndSaveAsync(
        Guid nodeId,
        string seed,
        string? title        = null,
        int    targetBeats   = 12,
        CancellationToken ct = default)
    {
        var literaryRules = canonDb.GetLiteraryRulesPrompt() ?? "";
        var system = BuildBibleSystemPrompt(targetBeats, literaryRules);
        var user = $"SEED: {seed}\n{(title != null ? $"WORKING TITLE: {title}" : "")}\nTARGET BEATS: {targetBeats}\n\nWrite the node bible now.";

        log.LogInformation("[bible] Generating for node {NodeId} — seed: {Seed}", nodeId, seed);

        string bibleText;
        try
        {
            bibleText = await llm.GenerateAsync(system, user, temperature: 0.7, maxTokens: 4096, ct: ct);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "[bible] LLM call failed for node {NodeId}", nodeId);
            throw;
        }

        bibleText = bibleText.Trim();

        // Persist on the node row
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var node = await db.Nodes.FindAsync([nodeId], ct)
            ?? throw new InvalidOperationException($"Node {nodeId} not found.");

        node.NodeBible            = bibleText;
        node.NodeBibleGeneratedAt = DateTime.UtcNow;
        node.UpdatedAt              = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        log.LogInformation("[bible] Saved {Chars} chars for node {NodeId}", bibleText.Length, nodeId);

        // Create planned beats from the spine (only when the node is blank)
        var beatPlans = ParseBeatSpine(bibleText);
        if (beatPlans.Count > 0)
            await CreatePlannedBeatsAsync(db, nodeId, beatPlans, ct);

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
        var user   = $"SEED: {seed}\n{(title != null ? $"WORKING TITLE: {title}" : "")}\nTARGET BEATS: {targetBeats}\n\nWrite the node bible now.";
        return (await llm.GenerateAsync(system, user, temperature: 0.75, maxTokens: 4096, ct: ct)).Trim();
    }

    /// <summary>
    /// Save a pre-generated bible text to an existing node row and create
    /// planned beats from its spine. Used after a compete-selection picks a winner.
    /// </summary>
    public async Task SaveBibleAndCreateBeatsAsync(
        Guid nodeId,
        string bibleText,
        CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var node = await db.Nodes.FindAsync([nodeId], ct)
            ?? throw new InvalidOperationException($"Node {nodeId} not found.");

        node.NodeBible            = bibleText;
        node.NodeBibleGeneratedAt = DateTime.UtcNow;
        node.UpdatedAt              = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        var beatPlans = ParseBeatSpine(bibleText);
        if (beatPlans.Count > 0)
            await CreatePlannedBeatsAsync(db, nodeId, beatPlans, ct);

        log.LogInformation("[bible] Saved winning bible ({Chars} chars) for node {NodeId}", bibleText.Length, nodeId);
    }

    // ── Retrieval ─────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the bible text for <paramref name="nodeId"/>, or null if none exists.
    /// </summary>
    public async Task<string?> GetBibleTextAsync(Guid nodeId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.Nodes
            .Where(s => s.Id == nodeId)
            .Select(s => s.NodeBible)
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
        You are a story architect. Given a one-line seed, produce a NODE BIBLE —
        a dry, structural plan that a prose engine will execute beat by beat.

        Rules for the bible:
        - Declarative sentences only. No purple prose. No florid language.
        - Every beat entry is a fact about what happens, not a lyric about it.
        - Be specific: name characters, name costs, name objects that matter.
        - The bible is a spine. Flesh lives in the prose pass.

        LITERARY RULES (follow these — they define the world's voice):
        {literaryRules}

        Output EXACTLY this markdown format. No extra sections. No preamble.

        # NODE BIBLE: [Working Title]

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
        ProseDbContext db,
        Guid                  nodeId,
        List<BeatPlan>        plans,
        CancellationToken     ct)
    {
        // Serializable transaction guards against concurrent bible generations producing
        // duplicate Beat.Number values (MAX+1 race).
        await using var tx = await db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable, ct);
        try
        {

        // SS-A43: beats live on chapter children for book-mode stories.
        // GetLeafDescendantIdsAsync already returns leaves in reading order (SortKey-ordered,
        // depth-first) so beat distribution below stays deterministic; also recurses past any
        // nested Collection (2026-08-09 fix).
        var childIds = await NodeWorkbenchService.GetLeafDescendantIdsAsync(db, nodeId, ct);
        var existing = await db.BeatNodes.CountAsync(sb => childIds.Contains(sb.NodeId) && true, ct);
        if (existing > 0)
        {
            log.LogInformation("[bible] Node {NodeId} already has {Count} beats — skipping planned beat creation.", nodeId, existing);
            await tx.RollbackAsync(CancellationToken.None);
            return;
        }

        var baseNumber = (await db.Beats.MaxAsync(b => (int?)b.Number, ct) ?? 0) + 1;
        var now = DateTime.UtcNow;
        // Distribute beats evenly across chapter children in SortKey order.
        var sortedPlans = plans.OrderBy(p => p.Index).ToList();
        int totalPlans = sortedPlans.Count;
        for (int i = 0; i < totalPlans; i++)
        {
            var plan = sortedPlans[i];
            var beat = new Beat
            {
                Id            = Guid.CreateVersion7(),
                Number        = baseNumber + i,
                Title         = plan.Title,
                Description   = plan.Goal,
                StructureRole = MapStructureRole(plan.StructureRole),
                Text          = "",
                CreatedAt     = now,
                UpdatedAt     = now,
            };
            db.Beats.Add(beat);
            db.BeatNodes.Add(new BeatNode
            {
                NodeId    = childIds.Count > 0
                    ? childIds[i * childIds.Count / totalPlans]
                    : nodeId,
                BeatId    = beat.Id,
                SortKey   = plan.Index * 100.0,
            });
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        log.LogInformation("[bible] Created {Count} planned beats for node {NodeId}", plans.Count, nodeId);

        } // end try
        catch
        {
            await tx.RollbackAsync(CancellationToken.None);
            throw;
        }
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

/// <summary>One entry from a node bible's ## BEAT SPINE section.</summary>
public record BeatPlan(int Index, string Title, string Goal, string StructureRole);
