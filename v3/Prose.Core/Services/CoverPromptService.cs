using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Interfaces;

namespace Prose.Core.Services;

/// <summary>
/// Generates <see cref="Node.CoverPrompt"/> — a single-paragraph visual description
/// for an AI image model to render as a book cover — from the book's own Summary/
/// Description, title, and universe. Saved to the node row; feeds
/// <see cref="CoverImageService"/> as the prompt handed to whichever image provider
/// the author picks (OpenAI / Stability / Google Imagen).
///
/// The prompt asks the image model to render the book's exact title AS PART of the
/// artwork — including a genre-appropriate typography style description (a serif
/// display face, a neon sans-serif, an illuminated blackletter, etc.) — rather than
/// leaving blank space for a title to be composited on afterward. Modern image models
/// (gpt-image-1 in particular) render short display text reliably enough to make this
/// the better default; CoverTitleCompositorService remains available as a manual
/// fallback (<c>ss --composite-cover-title</c>) for renders where the text comes out
/// garbled.
///
/// The prompt is deliberately kept commercial-cover-safe (atmospheric/suggestive,
/// never explicit) regardless of how graphic the book's interior prose is — covers
/// sell on newsstands and image APIs refuse explicit violence/nudity prompts outright.
/// </summary>
public class CoverPromptService
{
    private readonly ILlmService llm;
    private readonly IDbContextFactory<ProseDbContext> dbFactory;
    private readonly ILogger<CoverPromptService> log;

    public CoverPromptService(
        ILlmService llm,
        IDbContextFactory<ProseDbContext> dbFactory,
        ILogger<CoverPromptService> log)
    {
        this.llm       = llm;
        this.dbFactory = dbFactory;
        this.log       = log;
    }

    /// <summary>Generates and saves a cover prompt for the given node. Returns the prompt text.</summary>
    public async Task<string> GenerateAndSaveAsync(Guid nodeId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var node = await db.Nodes.FindAsync([nodeId], ct)
            ?? throw new InvalidOperationException($"Node {nodeId} not found.");

        var universe = await db.Universes.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == node.UniverseId, ct);

        var blurb = FirstNonEmpty(node.Summary, node.Description, node.Seed)
            ?? throw new InvalidOperationException(
                $"Node '{node.Slug}' has no Summary, Description, or Seed to base a cover prompt on. Set one first.");

        var system = BuildSystemPrompt();
        var user = BuildUserPrompt(node, universe, blurb);

        log.LogInformation("[cover-prompt] Generating for node {NodeId} ({Slug})", nodeId, node.Slug);

        var promptText = (await llm.GenerateAsync(system, user, temperature: 0.8, maxTokens: 600, ct: ct)).Trim();

        node.CoverPrompt            = promptText;
        node.CoverPromptGeneratedAt = DateTime.UtcNow;
        node.UpdatedAt              = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        log.LogInformation("[cover-prompt] Saved {Chars} chars for node {NodeId}", promptText.Length, nodeId);
        return promptText;
    }

    private static string? FirstNonEmpty(params string?[] values)
        => values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

    private static string BuildSystemPrompt() => """
        You write image-generation prompts for commercial book covers. Given a book's title,
        genre/universe, and blurb, produce ONE tight paragraph (90-160 words) describing exactly
        what should appear on the cover, INCLUDING the title typography rendered as part of the
        artwork itself (not left blank for something else to add later).

        Cover the following, in this order:
        1. The central subject/figure and setting/background, grounded in the blurb.
        2. Lighting, mood, and color palette.
        3. Composition: portrait orientation, single strong focal image, clear open area
           (usually the top third, but wherever the composition actually earns it) reserved
           for the title so it doesn't fight the art for attention.
        4. The title lockup itself: state the exact title text in quotes, exactly as given,
           and describe a specific, genre-appropriate typography TREATMENT for it — the
           typeface style (e.g. "a bold engraved serif in worn gold foil", "a sharp glitching
           neon-sans with chromatic aberration", "hand-illuminated blackletter capitals",
           "a clean minimalist sans-serif letterspaced wide"), its color/finish, and roughly
           where it sits in the composition. Pick a treatment that matches the book's genre
           and mood, not a generic default.

        Hard rules:
        - Output ONLY the image prompt itself — no preamble, no labels, no quotes around the
          whole thing (quotes AROUND the title text itself are fine and expected).
        - Render ONLY the title text — never an author name, publisher logo, tagline, ISBN,
          or any other text. One title, rendered once.
        - Keep it commercial-cover-safe: atmospheric, suggestive, iconic — not explicit. Do not
          describe graphic violence, gore, nudity, or explicit sexual content even if the book's
          interior content is graphic; the cover sells the book on a storefront and image
          generators refuse explicit prompts outright. Imply intensity through mood, shadow,
          posture, and symbolism instead of depicting it directly.
        - Match the visual language of the book's universe/genre (e.g. neon-lit cyberpunk density
          for a near-future dystopia, painterly dark-fantasy for a sword-and-sorcery epic,
          restrained documentary realism for nonfiction/history) in BOTH the art and the
          typography treatment.
        - Ground every element in the blurb given — no generic stock-photo imagery unconnected
          to the actual story.
        - AVOID sharp, close-up, hero-prop framing of small manufactured objects with intricate
          mechanical structure — hand tools, blades, firearms, machinery, gadgets, anything with
          moving parts, joints, or a handle-plus-head assembly (the same failure class as hands:
          image models render the connecting geometry as physically nonsensical). If such an
          object matters to the scene, keep it SMALL in frame, motion-blurred, deep in shadow,
          silhouetted, partially obscured, or simply out of focus — never the sharp macro subject.
          Prefer whole-figure, landscape, architectural, or symbolic/atmospheric elements as the
          hero subject instead; let props stay props.
        """;

    private static string BuildUserPrompt(Node node, Universe? universe, string blurb)
    {
        var lines = new List<string>
        {
            $"TITLE (render this exact text on the cover): {node.Title}",
            $"KIND: {node.Kind}",
        };
        if (universe != null)
        {
            lines.Add($"UNIVERSE: {universe.Name}" + (string.IsNullOrWhiteSpace(universe.Theme) ? "" : $" ({universe.Theme})"));
        }
        lines.Add($"BLURB: {blurb}");
        lines.Add("");
        lines.Add("Write the cover image prompt now.");
        return string.Join('\n', lines);
    }
}
