using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Core.Composition.Prompt;

/// <summary>What the voice tier found, and — as importantly — whether it found anything.</summary>
/// <param name="Block">The rendered prompt block. Empty when nothing was retrieved.</param>
/// <param name="ExemplarCount">How many of the author's own beats are in it.</param>
public sealed record VoiceAnchor(string Block, int ExemplarCount)
{
    public bool Fired => ExemplarCount > 0;
    public static readonly VoiceAnchor None = new("", 0);
}

/// <summary>
/// The voice tier (RFC 0012 §3.2 tier D): a few of the author's own beats, chosen by embedding
/// similarity to what is being written, shown as exemplars of how this book sounds.
///
/// <para><b>Why this is a service and not a private helper.</b> v3's equivalent —
/// <c>BeatGeneratorService.BuildBeatAnchorsAsync</c> — queried <c>ScopeKind='beat'</c> embeddings
/// while the pipeline only ever wrote <c>ScopeKind='BeatNode'</c>. Its own comment records the
/// consequence: <i>"That made every style-anchor lookup return zero hits, silently, for every beat
/// ever generated."</i> The lookup was repointed, but nothing measures whether it now returns rows,
/// so the failure is still invisible. <see cref="VoiceAnchor.ExemplarCount"/> exists so "wired" and
/// "firing" stop being the same claim: a caller can record the count, and a zero is a fact rather
/// than an absence.</para>
///
/// <para>Retrieval is scoped to the book being written. Exemplars from another book would teach the
/// writer the wrong voice, which is worse than teaching it none.</para>
/// </summary>
public sealed class VoiceAnchorService(
    EmbeddingService embeddings,
    IDbContextFactory<ProseDbContext> dbFactory)
{
    /// <summary>Characters of one exemplar kept. Whole beats would crowd the tier's own ceiling.</summary>
    public const int ExemplarChars = 900;

    public async Task<VoiceAnchor> BuildAsync(
        Guid bookNodeId, string queryText, int k = 3, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(queryText)) return VoiceAnchor.None;

        IReadOnlyList<ProseEmbeddingHit> hits;
        // A cold or failed embedding store must degrade to "no voice block", never to a failed
        // write — the beat is still writable without exemplars, just less in-voice.
        try { hits = await embeddings.FindSimilarBeatNodesAsync(queryText, k, bookNodeId, ct); }
        catch (OperationCanceledException) { throw; }
        catch { return VoiceAnchor.None; }

        if (hits.Count == 0) return VoiceAnchor.None;

        var ids = hits.Select(h => h.ScopeId).ToHashSet();
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var texts = await db.Beats.AsNoTracking()
            .Where(b => ids.Contains(b.Id) && b.Text != null && b.Text != "")
            .Select(b => b.Text!)
            .ToListAsync(ct);

        if (texts.Count == 0) return VoiceAnchor.None;

        var lines = new List<string>
        {
            "HOW THIS BOOK SOUNDS — passages from this same book, for voice only. Match their",
            "register, rhythm and restraint. Do NOT reuse their events, characters or images.",
        };
        foreach (var t in texts)
        {
            var stripped = BeatMarkup.StripEntityTags(t).Trim();
            if (stripped.Length > ExemplarChars) stripped = stripped[..ExemplarChars].TrimEnd() + "…";
            lines.Add("");
            lines.Add(stripped);
        }

        return new VoiceAnchor(string.Join("\n", lines), texts.Count);
    }
}
