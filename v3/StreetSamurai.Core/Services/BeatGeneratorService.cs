using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Models;

namespace StreetSamurai.Core.Services;

public class BeatGeneratorService
{
    private readonly ILlmService llm;
    private readonly WorldGraphService graph;
    private readonly LoreService canon;
    private readonly EmbeddingService embeddings;
    private readonly IDbContextFactory<StreetSamuraiDbContext> dbFactory;

    public BeatGeneratorService(
        ILlmService llm,
        WorldGraphService graph,
        LoreService canon,
        EmbeddingService embeddings,
        IDbContextFactory<StreetSamuraiDbContext> dbFactory)
    {
        this.llm = llm;
        this.graph = graph;
        this.canon = canon;
        this.embeddings = embeddings;
        this.dbFactory = dbFactory;
    }

    public async Task<string> GenerateBeatAsync(
        BeatContext context,
        CancellationToken ct = default)
    {
        var dialogueBlock = !string.IsNullOrWhiteSpace(context.DialogueContext)
            ? $"\n\n{context.DialogueContext}"
            : "";

        // Pull semantically-similar past beats as in-context style anchors. Voice
        // and pacing tend to drift across long writing sessions; injecting 3
        // beats from canon that are *structurally* near this one (same kind of
        // negotiation, same kind of confrontation, same kind of beat-of-quiet)
        // keeps the writer's register consistent without copying any specific
        // scene. Empty when the prose-embedding cache is cold.
        var anchorBlock = await BuildBeatAnchorsAsync(context, ct);

        var system = $"""
            You are writing a beat in a literary cyberpunk scene set in GLMZ (Meridian 88).

            INNER MONOLOGUE: italicized stand-alone sentences, on their own paragraph, NEVER labeled.
            Source from each POV character's documented psychology — coping_mechanisms, core_fears,
            blind_spots, secret. Specific named things, not abstract archetypes. Do NOT use bracketed
            tags like [WOUND] or [IDEAL] — those are retired.

            STORY BIBLE AND LITERARY RULES:
            {context.StoryBibleContext}

            WORLD CONTEXT (characters, locations, equipment, relationships — use as canon facts):
            {context.RelationshipContext}
            {(context.LocationContext.Length > 0 ? "\nADDITIONAL LOCATION DETAIL:\n" + context.LocationContext : "")}{dialogueBlock}{anchorBlock}
            """;

        var hasDialogue = context.DialogueContext.Length > 0;
        var dialogueInstruction = hasDialogue
            ? """

              DIALOGUE DIRECTION:
              Characters speak in their own voice — see profiles above. Each voice must be immediately
              distinct without dialogue tags. Do not name emotions. Do not have characters explain
              themselves. Subtext is load-bearing. What a character says to fill silence reveals
              more than what they say when they mean to speak.
              """
            : "";

        var user = $"""
            SCENE SO FAR:
            {context.SceneSoFar}

            BEAT GOAL: {context.BeatGoal}

            Write the next beat of the scene. Voice comes from the POV character's documented
            speech_patterns and psychology — clipped or warm, deflective or direct, depending on
            whose head we're in. Inner thoughts surface as *italicized stand-alone lines*, never
            labeled — a person arguing with themselves about a specific named thing.{dialogueInstruction}

            Write 2-4 paragraphs. Make every word count.
            """;

        return await llm.GenerateAsync(system, user, temperature: 0.85, maxTokens: 2048, ct: ct);
    }

    /// <summary>
    /// Pull the top-3 semantically-similar past beats from the prose-embedding
    /// cache, render them as a short "STYLE ANCHORS" block. The query is the
    /// (BeatGoal + last ~1.5k chars of SceneSoFar) — that's what we want to
    /// match in voice/pacing. Returns empty string when the prose cache is
    /// cold or the embedding API is unavailable.
    /// </summary>
    private async Task<string> BuildBeatAnchorsAsync(BeatContext context, CancellationToken ct)
    {
        var query = (context.BeatGoal ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(context.SceneSoFar))
        {
            var tail = context.SceneSoFar.Length > 1500
                ? context.SceneSoFar[^1500..]
                : context.SceneSoFar;
            query = string.IsNullOrEmpty(query) ? tail : $"{query}\n\n{tail}";
        }
        if (string.IsNullOrWhiteSpace(query)) return "";

        IReadOnlyList<ProseEmbeddingHit> hits;
        try { hits = await embeddings.FindSimilarProseAsync(query, k: 4, scopeKind: "beat", ct); }
        catch { return ""; }
        if (hits.Count == 0) return "";

        var beatIds = hits.Select(h => h.ScopeId).ToHashSet();
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var beats = await db.Set<Data.Entities.ChapterBeat>().AsNoTracking()
            .Where(b => beatIds.Contains(b.BeatGuid))
            .Select(b => new { b.BeatGuid, b.Title, b.Synopsis, b.Text })
            .ToListAsync(ct);

        if (beats.Count == 0) return "";

        var sb = new System.Text.StringBuilder();
        sb.AppendLine().AppendLine();
        sb.AppendLine("STYLE ANCHORS (canon beats with adjacent voice/pacing — match the register, do NOT echo specifics):");
        var hitOrder = hits.Select((h, i) => new { h.ScopeId, Order = i }).ToDictionary(x => x.ScopeId, x => x.Order);
        foreach (var beat in beats.OrderBy(b => hitOrder.TryGetValue(b.BeatGuid, out var o) ? o : int.MaxValue).Take(3))
        {
            if (string.IsNullOrWhiteSpace(beat.Text)) continue;
            // Cap each anchor to ~600 chars so the system prompt stays bounded.
            var excerpt = beat.Text.Length > 600 ? beat.Text[..600].TrimEnd() + "…" : beat.Text;
            if (!string.IsNullOrWhiteSpace(beat.Title)) sb.Append("[").Append(beat.Title).AppendLine("]");
            sb.AppendLine(excerpt).AppendLine();
        }
        return sb.ToString();
    }
}

public record BeatContext
{
    public string StoryBibleContext { get; init; } = "";
    public string RelationshipContext { get; init; } = "";
    public string LocationContext { get; init; } = "";
    /// <summary>Per-character voice profiles and cross-character relationship dynamics from DialogueService.</summary>
    public string DialogueContext { get; init; } = "";
    public string SceneSoFar { get; init; } = "";
    public string BeatGoal { get; init; } = "";
}
