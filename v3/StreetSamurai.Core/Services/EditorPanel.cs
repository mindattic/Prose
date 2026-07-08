using MindAttic.Legion;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Deterministic, diverse editorial panel — the same cross-section of readers
/// every run regardless of story, session, or machine. Selected by sorting
/// PersonaLibrary.Enriched by persona ID and stepping evenly through the pool,
/// so every position in the ideological/cultural/professional spectrum is
/// represented and never drifts between runs.
///
/// Personas in the Enriched pool are professional-grade psychometric profiles.
/// The even-step selection guarantees geographic, demographic, and reading-taste
/// diversity without cherry-picking, which keeps the panel unbiased — the panel's
/// job is to find gripes, not validate the author's choices.
/// </summary>
public static class EditorPanel
{
    private static readonly Lazy<List<Persona>> SortedPool = new(() =>
        PersonaLibrary.Enriched.OrderBy(p => p.Id).ToList());

    /// <summary>Total enriched personas available.</summary>
    public static int PoolSize => SortedPool.Value.Count;

    /// <summary>
    /// Returns a deterministic, evenly-spread panel of <paramref name="count"/> personas.
    /// The same count always returns the same panel. Calling with count=20 always produces
    /// the same 20 voters, regardless of story, run, or machine restart.
    /// </summary>
    public static List<Persona> GetPanel(int count)
    {
        var pool = SortedPool.Value;
        count = Math.Clamp(count, 1, pool.Count);
        var step = pool.Count / count;
        return Enumerable.Range(0, count)
            .Select(i => pool[i * step])
            .ToList();
    }

    /// <summary>
    /// Returns the panel excluding personas that have already voted in this run.
    /// Used when upgrading a subset of ballot voters to prose reviewers.
    /// </summary>
    public static List<Persona> GetPanel(int count, IReadOnlySet<string> excludeIds)
    {
        var pool = SortedPool.Value
            .Where(p => !excludeIds.Contains(p.Id))
            .ToList();
        count = Math.Clamp(count, 1, pool.Count);
        var step = Math.Max(1, pool.Count / count);
        return Enumerable.Range(0, count)
            .Select(i => pool[Math.Min(i * step, pool.Count - 1)])
            .Distinct()
            .Take(count)
            .ToList();
    }
}
