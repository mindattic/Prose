using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;

namespace Prose.Core.Services;

/// <summary>
/// The one sanctioned way to turn a user-supplied node reference into a <see cref="Guid"/>.
/// Accepts, in priority order: an exact GUID, a unique GUID prefix, an exact slug, or an exact
/// NodeCode (both case-insensitive).
///
/// <para><b>Why this exists (2026-08-23).</b> Six separate private <c>ResolveNodeIdAsync</c>
/// implementations had grown across <c>BeatCli</c>, <c>Tools.cs</c>, <c>Tools.Nodes.cs</c>,
/// <c>Tools.Voice.cs</c>, <c>Tools.Canon.cs</c> and <c>Tools.Config.cs</c>, and had drifted apart
/// in ways that were silent bugs rather than mere style differences:</para>
/// <list type="bullet">
///   <item><c>Tools.Config.cs</c>'s copy matched <b>slug only</b> — passing a NodeCode or a GUID
///   returned null, and it lacked <c>IgnoreQueryFilters()</c> entirely, so it also returned null
///   for any node outside the ambient universe scope.</item>
///   <item><c>BeatCli</c> and <c>Tools.Nodes.cs</c> applied the 2026-08-17
///   <c>IgnoreQueryFilters()</c> fix to their GUID branch but <b>not</b> their slug/code branch —
///   so an explicit, fully-qualified slug still silently resolved to null cross-universe.</item>
///   <item>None supported a GUID prefix, even though several CLI commands
///   (e.g. <c>ReparentNodeCli</c>) advertise prefix lookup in their own usage text.</item>
/// </list>
///
/// <para><b>IgnoreQueryFilters is correct here, on every branch.</b> A caller who supplies an
/// explicit id/slug/code has already named exactly one node; the ambient universe scope is not a
/// disambiguator for that lookup, it can only suppress the correct answer. This is the same
/// reasoning the per-file 2026-08-17 comments gave — applied consistently instead of to one
/// branch per file.</para>
/// </summary>
public static class NodeRefResolver
{
    /// <summary>
    /// Resolves <paramref name="reference"/> (GUID, GUID prefix, slug, or NodeCode) to a node id,
    /// or null when it matches nothing — or, for a prefix, when it is ambiguous (two or more
    /// nodes share it). An ambiguous prefix returning null rather than an arbitrary first match
    /// is deliberate: silently picking one of several books is far worse than failing.
    /// </summary>
    public static async Task<Guid?> ResolveAsync(ProseDbContext db, string? reference, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reference)) return null;
        var trimmed = reference.Trim();

        if (Guid.TryParse(trimmed, out var guid))
        {
            var exists = await db.Nodes.IgnoreQueryFilters().AsNoTracking()
                .AnyAsync(n => n.Id == guid, ct);
            if (exists) return guid;
            return null; // a well-formed GUID that isn't a node is a caller error, not a slug
        }

        var lowered = trimmed.ToLowerInvariant();

        var bySlugOrCode = await db.Nodes.IgnoreQueryFilters().AsNoTracking()
            .Where(n => n.Slug.ToLower() == lowered
                     || (n.NodeCode != null && n.NodeCode.ToLower() == lowered))
            .Select(n => (Guid?)n.Id)
            .FirstOrDefaultAsync(ct);
        if (bySlugOrCode != null) return bySlugOrCode;

        // GUID-prefix fallback last: only reached when the reference isn't a real slug/code, and
        // only honoured when it identifies exactly one node.
        // Hyphens allowed: a GUID is PRINTED hyphenated, so the natural copy-paste prefix is
        // "019f5767-d08a", and requiring bare hex digits rejected exactly the form a user is most
        // likely to paste while accepting "019f5767" (found 2026-08-24 writing this class's first
        // tests). A hyphen can't make a real slug pass either — slugs contain non-hex letters.
        if (trimmed.Length >= 4 && trimmed.All(c => Uri.IsHexDigit(c) || c == '-'))
        {
            var matches = await db.Nodes.IgnoreQueryFilters().AsNoTracking()
                .Where(n => n.Id.ToString().StartsWith(lowered))
                .Select(n => (Guid?)n.Id)
                .Take(2)
                .ToListAsync(ct);
            if (matches.Count == 1) return matches[0];
        }

        return null;
    }

    /// <summary>
    /// Convenience overload for callers holding a factory rather than a live context.
    /// </summary>
    public static async Task<Guid?> ResolveAsync(
        IDbContextFactory<ProseDbContext> dbFactory, string? reference, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await ResolveAsync(db, reference, ct);
    }

    /// <summary>
    /// Standard not-found message, so every command words this failure the same way and tells the
    /// user what forms are actually accepted (the original cause of the friction this class fixes:
    /// <c>--progress</c> prints NodeCodes, but most commands only ever matched slugs).
    /// </summary>
    public static string NotFoundMessage(string? reference) =>
        $"Node '{reference}' not found. Accepts a slug, a NodeCode (e.g. BCODA, MxG), " +
        "a full GUID, or a unique GUID prefix. Run 'prose --progress' to list books.";
}
