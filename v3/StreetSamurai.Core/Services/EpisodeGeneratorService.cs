using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Turns a DrawnSeed into a complete Episode of paragraph-granular beats.
///
/// One LLM call per episode for now. The episode length is variable — the
/// generator is told the seed and a target shape, and the model writes until
/// the story ends. Each paragraph is split into its own EpisodeBeat row so
/// audio narration can stream per-paragraph and so corrections can target
/// specific beats.
///
/// The system prompt loads the Bushido Coda v8 style guide from
/// engine/bushido_coda_v3/00_style_guide.md when present. This keeps the
/// generator on-voice without re-litigating tone in the user prompt.
/// </summary>
public class EpisodeGeneratorService
{
    private readonly ClaudeService llm;
    private readonly EpisodeSeedService seeds;
    private readonly IDbContextFactory<StreetSamuraiDbContext> dbFactory;
    private readonly IPathProvider paths;
    private readonly EpisodeExportService export;
    private readonly ILogger<EpisodeGeneratorService> log;

    public EpisodeGeneratorService(
        ClaudeService llm,
        EpisodeSeedService seeds,
        IDbContextFactory<StreetSamuraiDbContext> dbFactory,
        IPathProvider paths,
        EpisodeExportService export,
        ILogger<EpisodeGeneratorService> log)
    {
        this.llm = llm;
        this.seeds = seeds;
        this.dbFactory = dbFactory;
        this.paths = paths;
        this.export = export;
        this.log = log;
    }

    /// <summary>
    /// Generate a complete episode end-to-end: draw a seed, build context,
    /// call the LLM, split into beats, persist. Returns the new Episode id.
    /// Audio narration is the next pipeline stage, run separately.
    /// </summary>
    /// <param name="voiceId">Optional ElevenLabs voice id. Null → audio service falls back to SettingsService default.</param>
    public Task<Guid> GenerateAsync(string? voiceId = null, CancellationToken ct = default)
        => GenerateInternalAsync(customSeed: null, parentEpisodeId: null, voiceId, ct);

    /// <summary>Generate an episode from an explicit user-supplied seed —
    /// bypasses the template pool. Used by <c>ss --write-strand --seed</c>
    /// and any caller that wants a specific story prompt.</summary>
    public Task<Guid> GenerateFromSeedAsync(string customSeed, string? voiceId = null, CancellationToken ct = default)
        => GenerateInternalAsync(customSeed, parentEpisodeId: null, voiceId, ct);

    /// <summary>Generate a "Continue this story" episode that picks up from a
    /// prior one. The prior episode's title and last beat get folded into the
    /// seed so the LLM can write a sequel. The new episode's ParentEpisodeId
    /// points back at the source.</summary>
    public async Task<Guid> ContinueAsync(Guid parentId, string? voiceId = null, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var parent = await db.Episodes
            .AsNoTracking()
            .Include(e => e.Beats)
            .FirstOrDefaultAsync(e => e.Id == parentId, ct)
            ?? throw new InvalidOperationException($"Parent episode {parentId} not found.");

        var lastBeat = parent.Beats.OrderByDescending(b => b.Index).FirstOrDefault()?.Text ?? "";
        // Trim the closing beat so the seed stays short — the model just needs a hook.
        var closer = lastBeat.Length > 600 ? lastBeat[..600] + "…" : lastBeat;

        var continuationSeed =
            $"Continuing from \"{parent.Title}\". The previous episode closed on: " +
            $"\"{closer.Replace("\"", "'")}\" Pick up the thread the next night.";

        return await GenerateInternalAsync(continuationSeed, parentId, voiceId, ct);
    }

    private async Task<Guid> GenerateInternalAsync(
        string? customSeed, Guid? parentEpisodeId, string? voiceId, CancellationToken ct)
    {
        // Custom seed takes precedence (continuation flow); otherwise draw from the pool.
        string realizedSeed;
        DrawnSeed? drawn = null;
        if (!string.IsNullOrWhiteSpace(customSeed))
        {
            realizedSeed = customSeed;
        }
        else
        {
            drawn = await seeds.DrawAsync(ct);
            realizedSeed = drawn.Realized;
        }

        // Open and stamp the new episode immediately so the UI can poll it.
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        // UUIDv7 — time-ordered, globally unique, matches the canon Entity scheme.
        var episode = new Episode
        {
            Id = Guid.CreateVersion7(),
            Seed = realizedSeed,
            Title = "(generating)",
            Status = "generating",
            StartedAt = DateTime.UtcNow,
            VoiceId = voiceId,
            ParentEpisodeId = parentEpisodeId,
        };
        db.Episodes.Add(episode);
        await db.SaveChangesAsync(ct);

        try
        {
            var system = BuildSystemPrompt();
            var user = drawn != null
                ? await BuildUserPromptAsync(db, drawn, ct)
                : BuildCustomSeedPrompt(realizedSeed);

            log.LogInformation("Episode #{Id} generating with seed: {Seed}", episode.Id, realizedSeed);

            // One LLM call. The model decides how long the story is.
            var prose = await llm.GenerateAsync(
                system: system,
                user: user,
                temperature: 0.9,
                maxTokens: 8000,
                ct: ct);

            // First line is the title (per the prompt's contract). Strip it.
            var (title, body) = ExtractTitleAndBody(prose);

            episode.Title = string.IsNullOrWhiteSpace(title) ? "Untitled Adventure" : title;
            episode.Slug  = await GenerateUniqueSlugAsync(db, episode.Title, episode.Id, ct);

            // Split body into beats. A beat is a paragraph (blank-line separated).
            var beats = SplitIntoBeats(body);
            for (int i = 0; i < beats.Count; i++)
            {
                db.EpisodeBeats.Add(new EpisodeBeat
                {
                    EpisodeId = episode.Id,
                    Index = i,
                    SortKey = i * 100.0, // big gaps for future splits
                    Text = beats[i],
                });
            }

            episode.Status = "ready_for_audio";
            episode.GenerationCompletedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);

            log.LogInformation("Episode #{Id} generation complete: title={Title}, beats={BeatCount}",
                episode.Id, episode.Title, beats.Count);

            // File the script artifacts now — the audio pipeline runs after this
            // and shouldn't gate on disk-write success of an MD/PDF that exists
            // for archive purposes only.
            try { await export.ExportScriptAsync(episode.Id, ct); }
            catch (Exception ex) { log.LogWarning(ex, "Episode #{Id} script export failed (non-fatal)", episode.Id); }

            return episode.Id;
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Episode #{Id} generation failed", episode.Id);
            episode.Status = "failed";
            episode.Error = ex.Message;
            await db.SaveChangesAsync(ct);
            throw;
        }
    }

    // ── Prompt construction ─────────────────────────────────────────────

    private string BuildSystemPrompt()
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are the narrator of an episodic cyberpunk adventure starring Kyle Ellen Corbin-Vister.");
        sb.AppendLine("Setting: 2226, the Greater Lake Michigan Zone (GLMZ), a corponation-ruled vertical city.");
        sb.AppendLine();
        sb.AppendLine("KYLE — folk hero. Does not change. Same blade, same revolver, same noodle bowl, same code.");
        sb.AppendLine("- 27, lean, six feet of nothing, the hardware in his skull bills him in calories");
        sb.AppendLine("- NeoCortex Atlas array — ballistic precognition, augmentation signature read, the array warms behind his sternum like a coal blown to life");
        sb.AppendLine("- Carries a katana on his right shoulder in a matte friction sheath (102cm, carbon-edged steel forged by his dead mentor Seo). The street calls it Silence. It is a sword.");
        sb.AppendLine("- Carries Cacophony on his low left hip — bird's-head grip moon-clip revolver shotgun, 5 chambers, mixed loads of buckshot and slug");
        sb.AppendLine("- Lives in The Pivot, Unit 2F. Across the hall: Pixel in 2E, his hardware tech and friend");
        sb.AppendLine("- Eats at Mrs. Chen Wei-Lin's noodle stall every night at standard rate");
        sb.AppendLine("- Works freelance for fixer Sable; on the Lotus Syndicate freelance roster after Mira's interview");
        sb.AppendLine("- Speaks short. 1-6 words per line by default. Does not explain himself.");
        sb.AppendLine();
        sb.AppendLine("VOICE — locked from chapter 1 v8 of Bushido Coda:");
        sb.AppendLine("1. Short paragraphs. Single sentences are paragraphs. White space carries pacing.");
        sb.AppendLine("2. NO parenthetical narrator asides. NO fourth-wall breaks. NO (this will be more fully explored later).");
        sb.AppendLine("3. NO sword mythology — the katana cuts. No corundum strop, no piezoelectric, no 'literature on Silence.' Kyle wins because Kyle is very good.");
        sb.AppendLine("4. Every described object does double duty: corp indictment + character/setting metaphor. Brand names land hard (Carrion Logistics LLC, Hyacinth, Arcturus, Helix, Hydraulic Solutions, NeoCortex, Pinnacle Civil).");
        sb.AppendLine("5. Surroundings as metaphors for inhabitants. The building leans the way people lean after a long shift.");
        sb.AppendLine("6. Kyle's calculations are on the page — firing solutions, the 0.3-second window, the array warmth. SHOW the read; never narrate the filing.");
        sb.AppendLine("7. Action is pure cool, no jokes during. Italic SFX on their own line: *Crack.* *Tink.* *Tang.* *Clang.*");
        sb.AppendLine("8. Corp jokes punch via hypocrisy: state the corp claim, state the corp result, let the gap do the joke. 'They were the same corp.'");
        sb.AppendLine("9. One thesis line per episode max. Short declarative. 'Surprise was free.' 'Kneecaps are math.'");
        sb.AppendLine("10. One unbranded object somewhere in the story — the moral fulcrum. Explicitly contrast with the brand-density everywhere else.");
        sb.AppendLine("11. Close on a small definite line. Not a triple coda. Bell-strike: 4-12 words.");
        sb.AppendLine();
        sb.AppendLine("CONTINUITY — soft, not driving. Bushido Coda has happened: Hua owes Kyle Φ85,000 on his terms; an E.L.F. named Puppeteer is resident in his stack; Dr. Park is off-GLMZ. Reference these threads sparingly, never as the night's focus. Kyle is the folk hero. The night is its own story.");
        sb.AppendLine();
        sb.AppendLine("OUTPUT CONTRACT — strict.");
        sb.AppendLine("Line 1: a short title (no '#', no formatting, just the title).");
        sb.AppendLine("Line 2: empty.");
        sb.AppendLine("Lines 3+: the episode prose, paragraphs separated by a single blank line.");
        sb.AppendLine("Use roman-numeral section breaks (I, II, III) inside the prose. No 'Part I —' headers; just the numeral on its own line followed by a blank line.");
        sb.AppendLine("Do NOT prefix the title with the word 'Title'. Do not return any commentary outside the story itself.");
        return sb.ToString();
    }

    /// <summary>User prompt for a continuation episode. No DB entity hooks; the
    /// continuation seed already carries the prior-episode context.</summary>
    private static string BuildCustomSeedPrompt(string realizedSeed)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Tonight's adventure seed:");
        sb.AppendLine();
        sb.AppendLine(realizedSeed);
        sb.AppendLine();
        sb.AppendLine("Write the adventure. Take as long as it takes. End when the story ends.");
        return sb.ToString();
    }

    private async Task<string> BuildUserPromptAsync(StreetSamuraiDbContext db, DrawnSeed seed, CancellationToken ct)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Tonight's adventure seed:");
        sb.AppendLine();
        sb.AppendLine(seed.Realized);
        sb.AppendLine();

        // Hook context for the named character.
        if (seed.CharacterId is { } cid)
        {
            var hook = await db.CharacterStoryHooks
                .AsNoTracking()
                .Where(h => h.CharacterId == cid)
                .Select(h => h.Hook)
                .FirstOrDefaultAsync(ct);
            if (!string.IsNullOrWhiteSpace(hook))
            {
                sb.AppendLine($"Hook on this character: {hook}");
                sb.AppendLine();
            }
        }

        // Hook context for the named place.
        if (seed.PlaceId is { } pid)
        {
            var hook = await db.PlaceStoryHooks
                .AsNoTracking()
                .Where(h => h.PlaceId == pid)
                .Select(h => h.Hook)
                .FirstOrDefaultAsync(ct);
            if (!string.IsNullOrWhiteSpace(hook))
            {
                sb.AppendLine($"Hook on this place: {hook}");
                sb.AppendLine();
            }
        }

        sb.AppendLine("Write the adventure. Take as long as it takes. End when the story ends.");
        return sb.ToString();
    }

    // ── Output parsing ──────────────────────────────────────────────────

    private static (string title, string body) ExtractTitleAndBody(string prose)
    {
        var trimmed = prose.TrimStart();
        var firstNewline = trimmed.IndexOf('\n');
        if (firstNewline < 0) return ("Untitled Adventure", trimmed);

        var title = trimmed[..firstNewline].Trim().TrimStart('#').Trim();
        var body = trimmed[(firstNewline + 1)..].TrimStart('\r', '\n');
        return (title, body);
    }

    private static List<string> SplitIntoBeats(string body)
    {
        // Paragraphs separated by blank lines. Drop any pure-numeral lines (the
        // roman-numeral section markers) — they're structural, not narration.
        var paras = body.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim())
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Where(p => !IsSectionNumeral(p))
            .ToList();
        return paras;
    }

    private static bool IsSectionNumeral(string p)
    {
        // Roman numerals I-X, on their own line, possibly with a closing period.
        var s = p.TrimEnd('.').Trim();
        return s.Length <= 4 && s.All(c => "IVXivx".Contains(c));
    }

    // ── Slug ────────────────────────────────────────────────────────────

    /// <summary>Compute a URL-safe slug for a title. Lowercase, ASCII alphanum,
    /// hyphen-separated, capped to 80 chars. Removes diacritics and collapses
    /// runs of separators.</summary>
    public static string Slugify(string title)
    {
        if (string.IsNullOrWhiteSpace(title)) return "untitled";

        // Strip combining marks so "Sasha Võ" becomes "Sasha Vo".
        var normalized = title.Normalize(System.Text.NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            var cat = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch);
            if (cat == System.Globalization.UnicodeCategory.NonSpacingMark) continue;
            sb.Append(ch);
        }
        var ascii = sb.ToString().Normalize(System.Text.NormalizationForm.FormC).ToLowerInvariant();

        // Replace non-alphanumeric with hyphens, collapse runs.
        var slug = new StringBuilder(ascii.Length);
        bool lastWasSep = false;
        foreach (var ch in ascii)
        {
            if (char.IsLetterOrDigit(ch))
            {
                slug.Append(ch);
                lastWasSep = false;
            }
            else if (!lastWasSep)
            {
                slug.Append('-');
                lastWasSep = true;
            }
        }
        var result = slug.ToString().Trim('-');
        if (result.Length > 80) result = result[..80].TrimEnd('-');
        return string.IsNullOrEmpty(result) ? "untitled" : result;
    }

    /// <summary>Generate a unique slug for this title. If the base slug is already
    /// taken by another episode, append the first 8 hex chars of the episode id
    /// for guaranteed uniqueness without polluting the URL with a full guid.</summary>
    private static async Task<string> GenerateUniqueSlugAsync(
        StreetSamuraiDbContext db, string title, Guid episodeId, CancellationToken ct)
    {
        var baseSlug = Slugify(title);
        var taken = await db.Episodes
            .AsNoTracking()
            .Where(e => e.Id != episodeId && e.Slug == baseSlug)
            .AnyAsync(ct);
        return taken
            ? $"{baseSlug}-{episodeId.ToString("N")[..8]}"
            : baseSlug;
    }
}
