using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using System.Collections.Concurrent;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Manages the separation between universal facts (world mechanics, vocabulary, social structure
/// that apply to EVERY book in this universe) and book-specific facts (<c>Node.NodeBible</c>).
///
/// Universal facts live in <c>Universe.WorldFacts</c> and are injected into every beat-generation
/// prompt regardless of which book is being written — so a fact like "Pulse pods are individual
/// spheres with a neuretic train hallucination" is always present without requiring each book
/// bible to repeat it. Book facts (<c>Node.NodeBible</c>) stay book-scoped.
///
/// The human-editable source is <c>docs/universes/&lt;slug&gt;.md</c>; <c>ss --sync-markdown</c>
/// syncs it into <c>Universe.WorldFacts</c>.
/// </summary>
public class UniversalFactsService
{
    private readonly IDbContextFactory<StreetSamuraiDbContext> dbFactory;
    private readonly IUniverseContext universe;

    // WorldFacts is large, stable, and never changes mid-session; cache it per universe.
    private static readonly ConcurrentDictionary<Guid, string> worldFactsCache = new();

    public UniversalFactsService(
        IDbContextFactory<StreetSamuraiDbContext> dbFactory,
        IUniverseContext universe)
    {
        this.dbFactory = dbFactory;
        this.universe = universe;
    }

    /// <summary>
    /// Returns the world facts for the current universe, or empty string if none are set.
    /// Suitable for direct injection into a generation prompt.
    /// </summary>
    public async Task<string> GetWorldFactsAsync(CancellationToken ct = default)
    {
        var id = universe.CurrentId;
        if (id == Guid.Empty) return "";

        if (worldFactsCache.TryGetValue(id, out var cached))
            return cached;

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var facts = await db.Set<Universe>()
            .AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => u.WorldFacts)
            .FirstOrDefaultAsync(ct);
        var value = facts ?? "";
        worldFactsCache[id] = value;
        return value;
    }

    /// <summary>
    /// Returns the book-specific bible text for <paramref name="nodeId"/>, or empty string
    /// if the node has no bible yet. Book facts are distinct from universal facts — they
    /// cover only the arc, characters, and rules for that specific book node.
    /// </summary>
    public async Task<string> GetBookBibleAsync(Guid nodeId, CancellationToken ct = default)
    {
        if (nodeId == Guid.Empty) return "";
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var bible = await db.Nodes
            .AsNoTracking()
            .Where(n => n.Id == nodeId)
            .Select(n => n.NodeBible)
            .FirstOrDefaultAsync(ct);
        return bible ?? "";
    }

    /// <summary>
    /// Saves <paramref name="facts"/> as the world facts for the current universe.
    /// Pass null or empty to clear.
    /// </summary>
    public async Task SetWorldFactsAsync(string? facts, CancellationToken ct = default)
    {
        var id = universe.CurrentId;
        if (id == Guid.Empty) throw new InvalidOperationException("No universe selected.");
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var row = await db.Set<Universe>().FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new InvalidOperationException($"Universe {id} not found.");
        row.WorldFacts = string.IsNullOrWhiteSpace(facts) ? null : facts.Trim();
        await db.SaveChangesAsync(ct);
        worldFactsCache.TryRemove(id, out _);
        universe.Refresh();
    }

    /// <summary>
    /// Returns a formatted prompt block containing universal facts, ready to embed in a
    /// generation system prompt. Returns empty string when no facts are set.
    /// </summary>
    public async Task<string> BuildWorldFactsBlockAsync(CancellationToken ct = default)
    {
        var facts = await GetWorldFactsAsync(ct);
        return string.IsNullOrWhiteSpace(facts)
            ? ""
            : $"\n\nUNIVERSAL WORLD FACTS (apply to every book in this universe — treat as hard canon):\n{facts}";
    }
}
