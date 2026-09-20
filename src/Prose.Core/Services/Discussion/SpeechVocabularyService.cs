using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;

namespace Prose.Core.Services.Discussion;

/// <summary>
/// The proper nouns of one book, as a Whisper prompt.
///
/// <para>Transcription fails on exactly the words this engine cares most about. "Togishi",
/// "Seito" and "Cacophony" are not in any general-purpose vocabulary, and a model that has never
/// seen them returns plausible English instead — which is worse than a blank, because it reads
/// like something the author said. Whisper's <c>prompt</c> parameter is the fix: text supplied as
/// though it preceded the audio, biasing the decoder toward those spellings.</para>
///
/// <para><b>The dictionary is the corpus's own.</b> The Hub already knows every entity tagged into
/// the prose, and how often, which is a better list than either the author or an LLM would write
/// from memory — and it stays right as the book changes. Ordered by mentions because the prompt
/// budget is small (224 tokens) and a name the book uses two hundred times is the one the author
/// is most likely to say out loud. The tradeoff that ordering accepts: a rare name is exactly the
/// one most likely to be misheard, and it is the first to be cut.</para>
/// </summary>
public sealed class SpeechVocabularyService(IDbContextFactory<ProseDbContext> dbFactory)
{
    /// <summary>
    /// Cached because it is rebuilt on every single utterance and the answer moves only when the
    /// book's entity tags do. Ten minutes is short enough that a newly tagged character is
    /// speakable within one coffee, and long enough that a conversation never pays for the query
    /// twice.
    /// </summary>
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(10);

    private readonly ConcurrentDictionary<Guid, (DateTime At, string Value)> cache = new();

    public async Task<string> ForBookAsync(
        Guid bookNodeId, int maxChars = SpeechService.MaxVocabularyChars, CancellationToken ct = default)
    {
        if (bookNodeId == Guid.Empty) return "";

        if (cache.TryGetValue(bookNodeId, out var hit) && DateTime.UtcNow - hit.At < Ttl)
            return hit.Value;

        var built = await BuildAsync(bookNodeId, maxChars, ct);
        cache[bookNodeId] = (DateTime.UtcNow, built);
        return built;
    }

    /// <summary>Forget a book's list — for after a rename or a retagging pass, when waiting out
    /// the TTL would mean dictating against names that no longer exist.</summary>
    public void Invalidate(Guid bookNodeId) => cache.TryRemove(bookNodeId, out _);

    private async Task<string> BuildAsync(Guid bookNodeId, int maxChars, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        // GetLeafDescendantIdsAsync rather than the one-level ParentNodeId idiom: a book with a
        // split mega-chapter holds its beats two levels down, and the shallow version silently
        // returns an empty vocabulary for exactly the longest books.
        var leaves = await NodeWorkbenchService.GetLeafDescendantIdsAsync(db, bookNodeId, ct);
        if (leaves.Count == 0) return "";

        var beatIds = await db.BeatNodes.AsNoTracking()
            .Where(bn => leaves.Contains(bn.NodeId))
            .Select(bn => bn.BeatId)
            .ToListAsync(ct);
        if (beatIds.Count == 0) return "";

        var ranked = await db.BeatEntityMentions.AsNoTracking()
            .Where(m => beatIds.Contains(m.BeatId))
            .GroupBy(m => m.EntityName)
            .Select(g => new { Name = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(400)
            .ToListAsync(ct);

        // Aliases matter more than names here: a character is addressed by one in dialogue far
        // more often than by the full name the tag carries, and the alias is usually the stranger
        // word of the two.
        var entityIds = await db.BeatEntityMentions.AsNoTracking()
            .Where(m => beatIds.Contains(m.BeatId))
            .Select(m => m.EntityId)
            .Distinct()
            .ToListAsync(ct);

        var aliases = await db.CharacterAliases.AsNoTracking()
            .Where(a => entityIds.Contains(a.CharacterId))
            .Select(a => a.Value)
            .ToListAsync(ct);

        return Compose(ranked.Select(r => r.Name).Concat(aliases), maxChars);
    }

    /// <summary>The sentence the model is primed with. Opening words of a Whisper prompt.</summary>
    public const string Lead = "Names and terms used in this story: ";

    /// <summary>
    /// Turn a ranked list of names into the prompt, dropping what will not fit.
    ///
    /// <para>Pure, and separated from the query, because this is where the mistake would be: the
    /// budget is a hard cap that Whisper enforces by silently truncating from the END, so an
    /// overlong prompt does not fail — it quietly stops priming the last names in the list, and
    /// the only symptom is transcription being a bit worse than it should be.</para>
    /// </summary>
    public static string Compose(IEnumerable<string?> ranked, int maxChars = SpeechService.MaxVocabularyChars)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var budget = maxChars - Lead.Length - 1;   // the closing full stop
        var kept = new List<string>();
        var used = 0;

        foreach (var term in ranked)
        {
            var clean = (term ?? "").Trim();
            // One- and two-letter fragments are tag debris, not names, and spend budget for
            // nothing. A term containing a comma would read as two names in the prompt.
            if (clean.Length < 3 || clean.Contains(',')) continue;
            if (!seen.Add(clean)) continue;

            var cost = clean.Length + (kept.Count > 0 ? 2 : 0);
            // break, not continue: the list is in priority order, and slipping a short late name
            // past a long earlier one would quietly reorder priority by length.
            if (used + cost > budget) break;

            kept.Add(clean);
            used += cost;
        }

        return kept.Count == 0 ? "" : Lead + string.Join(", ", kept) + ".";
    }
}
