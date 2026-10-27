using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>prose --set-timeline --slug &lt;slug&gt; [--value &lt;decimal&gt; | --before &lt;ref-slug&gt; |
/// --after &lt;ref-slug&gt; | --same-as &lt;ref-slug&gt; | --clear]</c>
///
/// <para>Gets or sets where a book sits on its universe's story chronology (<see
/// cref="Prose.Core.Data.Entities.Node.Timeline"/>, PRS-T320). Integer part = series/era number
/// (1.0 = the first series' timeframe); fractional part orders books within that bracket. Two
/// books may legitimately share the exact same value — that means nothing in the prose
/// establishes which comes first, not that the data is wrong. <c>--same-as</c> records exactly
/// that tie on purpose. With no mode flag it prints the book's value and every book in the same
/// universe + integer bracket, in order (the "get").</para>
///
/// <para><c>--before</c>/<c>--after</c> place the book relative to a reference book already on the
/// timeline, bisecting the gap to its neighbor within the SAME universe and integer bracket —
/// bisection never crosses an integer boundary, so a book can never land in the wrong era. When
/// the gap has collapsed (a tie, or repeated bisection), every book in that bracket is restriped
/// onto a fresh evenly-spaced ladder first, later books moving forward with it — true ties stay
/// ties through the restripe, only the spacing between distinct values changes.</para>
///
/// <para>Not to be confused with <see cref="TimelineCli"/> (<c>--timeline</c>), the in-story
/// elapsed-time scan of one book's beats.</para>
/// </summary>
public static class BookTimelineCli
{
    private static readonly decimal[] RoundSteps = [0.1m, 0.01m, 0.001m, 0.0001m];
    // decimal(18,6) in the DB: a gap at or below that column's own rounding floor is already a
    // tie once it round-trips through SQL Server, so this sits one order of magnitude above it.
    private const decimal MinGap = 0.00001m;

    private sealed record Sibling(Guid Id, decimal Timeline);

    private static string Fmt(decimal v) => v.ToString("0.0#####", CultureInfo.InvariantCulture);

    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? slug = null, beforeSlug = null, afterSlug = null, sameAsSlug = null, valueArg = null;
        bool clear = false;
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--slug":    if (i + 1 < args.Length) slug = args[++i]; break;
                case "--value":   if (i + 1 < args.Length) valueArg = args[++i]; break;
                case "--before":  if (i + 1 < args.Length) beforeSlug = args[++i]; break;
                case "--after":   if (i + 1 < args.Length) afterSlug = args[++i]; break;
                case "--same-as": if (i + 1 < args.Length) sameAsSlug = args[++i]; break;
                case "--clear":   clear = true; break;
            }
        }

        var modeCount = new[] { valueArg != null, beforeSlug != null, afterSlug != null, sameAsSlug != null, clear }.Count(b => b);
        if (string.IsNullOrWhiteSpace(slug) || modeCount > 1)
        {
            Console.Error.WriteLine("Usage: prose --set-timeline --slug <slug> [--value <decimal> | --before <ref-slug> | --after <ref-slug> | --same-as <ref-slug> | --clear]");
            return 2;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        var targetId = await NodeRefResolver.ResolveAsync(db, slug);
        if (targetId == null) { Console.Error.WriteLine($"[set-timeline] {NodeRefResolver.NotFoundMessage(slug)}"); return 1; }
        // Tracked, unlike NodeRefResolver.ResolveNodeAsync — this command writes.
        var target = await db.Nodes.IgnoreQueryFilters().FirstAsync(n => n.Id == targetId.Value);

        if (modeCount == 0)
            return await PrintAsync(db, target.Id, target.Title, target.UniverseId, target.Timeline);

        if (clear)
        {
            target.Timeline = null;
            await db.SaveChangesAsync();
            Console.WriteLine($"[set-timeline] {target.Title}: Timeline cleared (not yet placed).");
            return 0;
        }

        if (valueArg != null)
        {
            if (!decimal.TryParse(valueArg, NumberStyles.Number, CultureInfo.InvariantCulture, out var value) || value < 0)
            {
                Console.Error.WriteLine($"[set-timeline] --value needs a non-negative decimal number, got '{valueArg}'.");
                return 2;
            }
            value = decimal.Round(value, 6);
            target.Timeline = value;
            await db.SaveChangesAsync();
            Console.WriteLine($"[set-timeline] {target.Title}: Timeline = {Fmt(value)}");
            return 0;
        }

        var refSlug = beforeSlug ?? afterSlug ?? sameAsSlug!;
        var referenceId = await NodeRefResolver.ResolveAsync(db, refSlug);
        if (referenceId == null) { Console.Error.WriteLine($"[set-timeline] reference: {NodeRefResolver.NotFoundMessage(refSlug)}"); return 1; }
        if (referenceId == targetId)
        {
            Console.Error.WriteLine("[set-timeline] the reference book can't be the same book you're placing.");
            return 1;
        }
        var reference = await db.Nodes.AsNoTracking().IgnoreQueryFilters().FirstAsync(n => n.Id == referenceId.Value);
        if (reference.UniverseId != target.UniverseId)
        {
            Console.Error.WriteLine($"[set-timeline] '{target.Title}' and '{reference.Title}' are in different universes — Timeline only orders books within one universe.");
            return 1;
        }
        if (reference.Timeline is not decimal refValue)
        {
            Console.Error.WriteLine($"[set-timeline] '{reference.Title}' has no Timeline yet — give it one first.");
            return 1;
        }

        if (sameAsSlug != null)
        {
            target.Timeline = refValue;
            await db.SaveChangesAsync();
            Console.WriteLine($"[set-timeline] {target.Title}: Timeline = {Fmt(refValue)} (tied with '{reference.Title}' — the prose doesn't establish an order between them)");
            return 0;
        }

        var bracket = decimal.Floor(refValue);
        decimal newValue;
        try
        {
            newValue = await PlaceAsync(db, reference.UniverseId, bracket, reference.Id, target.Id, after: afterSlug != null);
        }
        catch (NoRoomException ex)
        {
            Console.Error.WriteLine($"[set-timeline] {ex.Message}");
            return 1;
        }

        target.Timeline = newValue;
        await db.SaveChangesAsync();
        Console.WriteLine($"[set-timeline] {target.Title}: Timeline = {Fmt(newValue)} ({(afterSlug != null ? "after" : "before")} '{reference.Title}')");
        return 0;
    }

    private static async Task<int> PrintAsync(ProseDbContext db, Guid targetId, string title, Guid universeId, decimal? timeline)
    {
        if (timeline is not decimal value)
        {
            Console.WriteLine($"[set-timeline] {title}: Timeline not set.");
            return 0;
        }
        var bracket = decimal.Floor(value);
        var rows = await db.Nodes.AsNoTracking().IgnoreQueryFilters()
            .Where(n => n.Kind == "book" && n.UniverseId == universeId
                     && n.Timeline != null && n.Timeline >= bracket && n.Timeline < bracket + 1)
            .OrderBy(n => n.Timeline).ThenBy(n => n.Title)
            .Select(n => new { n.Id, n.Title, Timeline = n.Timeline!.Value })
            .ToListAsync();
        Console.WriteLine($"[set-timeline] {title}: Timeline = {Fmt(value)}");
        Console.WriteLine($"  era {Fmt(bracket)} in this universe:");
        foreach (var r in rows)
            Console.WriteLine($"  {(r.Id == targetId ? "*" : " ")} {Fmt(r.Timeline),-10} {r.Title}");
        return 0;
    }

    private static async Task<List<Sibling>> LoadBracketAsync(ProseDbContext db, Guid universeId, decimal bracket, Guid excludeId)
    {
        var rows = await db.Nodes.AsNoTracking().IgnoreQueryFilters()
            .Where(n => n.Kind == "book" && n.UniverseId == universeId && n.Id != excludeId
                     && n.Timeline != null && n.Timeline >= bracket && n.Timeline < bracket + 1)
            .OrderBy(n => n.Timeline)
            .Select(n => new { n.Id, Timeline = n.Timeline!.Value })
            .ToListAsync();
        return rows.Select(r => new Sibling(r.Id, r.Timeline)).ToList();
    }

    /// <summary>Thrown when there is genuinely no room for an insert even after a restripe.</summary>
    private sealed class NoRoomException(string message) : Exception(message);

    /// <summary>Finds the gap the new value falls into and bisects it (never crossing the bracket's
    /// integer boundary); restripes the whole bracket first if the gap has collapsed, then retries
    /// exactly once more — a second collapse means restriping made no difference.</summary>
    private static async Task<decimal> PlaceAsync(ProseDbContext db, Guid universeId, decimal bracket, Guid referenceId, Guid targetId, bool after, bool restripeAllowed = true)
    {
        var siblings = await LoadBracketAsync(db, universeId, bracket, targetId);
        var refValue = siblings.First(s => s.Id == referenceId).Timeline;

        decimal low, high;
        if (after)
        {
            var next = siblings.Where(s => s.Timeline > refValue).OrderBy(s => s.Timeline).FirstOrDefault();
            low = refValue;
            high = next?.Timeline ?? bracket + 1m;
        }
        else
        {
            var prev = siblings.Where(s => s.Timeline < refValue).OrderByDescending(s => s.Timeline).FirstOrDefault();
            low = prev?.Timeline ?? bracket;
            high = refValue;
        }

        if (high - low < MinGap)
        {
            if (!restripeAllowed)
                throw new NoRoomException(
                    $"no room {(after ? "after" : "before")} Timeline {Fmt(refValue)} within era {Fmt(bracket)} even after restriping it. Use --value to place the book explicitly.");
            await RestripeBracketAsync(db, universeId, bracket, targetId);
            return await PlaceAsync(db, universeId, bracket, referenceId, targetId, after, restripeAllowed: false);
        }

        return PickStep(low, high);
    }

    /// <summary>Reassigns every book in this bracket onto a fresh evenly spaced ladder
    /// (bracket + step, bracket + 2·step, …) with the step chosen so the whole ladder stays inside
    /// the bracket and leaves room before its first rung. Genuine ties stay tied: only the gaps
    /// between DISTINCT values are restored. The book being placed is excluded — it is about to get
    /// a new value anyway.</summary>
    private static async Task RestripeBracketAsync(ProseDbContext db, Guid universeId, decimal bracket, Guid excludeId)
    {
        var rows = await db.Nodes.IgnoreQueryFilters()
            .Where(n => n.Kind == "book" && n.UniverseId == universeId && n.Id != excludeId
                     && n.Timeline != null && n.Timeline >= bracket && n.Timeline < bracket + 1)
            .ToListAsync();

        var rungs = rows.Select(n => n.Timeline!.Value).Distinct().OrderBy(v => v).ToList();
        var step = 0.1m;
        while (rungs.Count * step >= 1m && step > MinGap) step /= 10m;
        var newValueByOldValue = rungs.Select((v, i) => (v, newValue: bracket + (i + 1) * step))
            .ToDictionary(x => x.v, x => x.newValue);

        foreach (var n in rows)
            n.Timeline = newValueByOldValue[n.Timeline!.Value];

        await db.SaveChangesAsync();
        Console.WriteLine($"[set-timeline] era {Fmt(bracket)} was out of room — restriped {rows.Count} book(s) onto a fresh {Fmt(step)} ladder.");
    }

    private static decimal PickStep(decimal low, decimal high)
    {
        foreach (var step in RoundSteps)
        {
            var candidate = decimal.Floor(low / step) * step + step;
            if (candidate > low && candidate < high) return candidate;
        }
        return decimal.Round(low + (high - low) / 2m, 6);
    }
}
