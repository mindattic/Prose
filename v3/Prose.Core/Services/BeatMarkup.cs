using System.Text.RegularExpressions;

namespace Prose.Core.Services;

/// <summary>
/// The single shared implementation for reading/stripping inline
/// <c>&lt;entity repo="..." guid="..."&gt;word&lt;/entity&gt;</c> tags (see
/// <see cref="EntityMentionScanner"/> for how tags get placed). Every reader/listener/tooling-facing
/// consumer of <c>Beats.Text</c> that isn't meant to see raw markup should call
/// <see cref="StripEntityTags"/> — one regex, no per-format variants, so there is exactly one place
/// to fix if the tag shape ever changes.
///
/// Both patterns match the opening <c>&lt;entity ...&gt;</c> tag by attribute NAME, not by a fixed
/// attribute order or exact attribute set — <c>guid</c> is looked up wherever it sits among the
/// tag's attributes. This is deliberate: <c>repo</c> (the entity's table/type, added purely as a
/// lookup-speed hint for future consumers) can be added, reordered, or dropped without this parser
/// silently breaking.
/// </summary>
public static class BeatMarkup
{
    private static readonly Regex EntityTagPattern =
        new(@"<entity\b[^>]*>(.*?)</entity>", RegexOptions.Compiled | RegexOptions.Singleline);

    private static readonly Regex EntityGuidPattern =
        new(@"<entity\b[^>]*\bguid=""([^""]*)""", RegexOptions.Compiled);

    /// <summary>Strips every tag back to its plain inner text — <c>&lt;entity guid="abc"&gt;Declan
    /// Doyle&lt;/entity&gt;</c> becomes <c>Declan Doyle</c>. Safe to call on already-untagged text
    /// (no-op).</summary>
    public static string StripEntityTags(string? text) =>
        string.IsNullOrEmpty(text) ? text ?? "" : EntityTagPattern.Replace(text, "$1");

    /// <summary>Every distinct Entity Guid tagged in this text, in first-occurrence order. This is
    /// the derivation path for <c>BeatEntityMentions</c> once a beat is tagged — parse tags, don't
    /// re-run a name/alias scan.</summary>
    public static IEnumerable<Guid> ExtractEntityGuids(string? text)
    {
        if (string.IsNullOrEmpty(text)) yield break;
        var seen = new HashSet<Guid>();
        foreach (Match m in EntityGuidPattern.Matches(text))
            if (Guid.TryParse(m.Groups[1].Value, out var g) && seen.Add(g))
                yield return g;
    }

    /// <summary>One tag already present in a caller's text: the surface words it wraps, and the
    /// entity guid the caller asserted they mean.</summary>
    public sealed record TaggedMention(string Text, Guid EntityId);

    /// <summary>
    /// Every tag present in <paramref name="text"/>, as (surface text → entity guid) pairs,
    /// de-duplicated on both.
    ///
    /// <para><b>Why this exists.</b> Every beat save strips the incoming tags and re-derives them
    /// from a name scan, deliberately, so a rename can never leave a stale tag in prose
    /// (<c>NodeWorkbenchService.UpdateBeatTextAsync</c>). But <c>EntityMentionScanner</c> refuses —
    /// correctly — to anchor a tag to a surface name claimed by more than one entity, so a tag on
    /// an ambiguous name cannot be re-derived and was simply lost. Found live 2026-09-04 editing
    /// BCODA beat #3289: four <c>&lt;entity guid="01a0030b-…"&gt;Marisol&lt;/entity&gt;</c> tags
    /// vanished on save because the universe holds five Marisols, while Kyle's and Silence's tags
    /// round-tripped fine. The guid still resolved to a live entity — the tag was valid, just not
    /// reconstructible from its own name, which is exactly the information a name scan cannot
    /// recover and a human's explicit markup already had.</para>
    ///
    /// <para>So an incoming tag is read as the caller's DISAMBIGUATION, not as data to distrust.
    /// The staleness property is kept intact: the save path only honours a pinned mention whose
    /// guid still resolves to a live entity in the book's universe, so a tag pointing at a deleted
    /// or archived entity is still dropped exactly as before.</para>
    /// </summary>
    public static List<TaggedMention> ExtractTaggedMentions(string? text)
    {
        var found = new List<TaggedMention>();
        if (string.IsNullOrEmpty(text)) return found;

        var seen = new HashSet<(string, Guid)>();
        foreach (Match m in EntityTagWithGuidPattern.Matches(text))
        {
            if (!Guid.TryParse(m.Groups[1].Value, out var id)) continue;
            var inner = m.Groups[2].Value;
            if (string.IsNullOrWhiteSpace(inner)) continue;
            // A tag wrapping nested markup is not a plain surface name; the scanner's positioning
            // works on plain text, so pinning it would be meaningless.
            if (inner.Contains('<')) continue;
            if (seen.Add((inner, id))) found.Add(new TaggedMention(inner, id));
        }
        return found;
    }

    /// <summary>Captures the guid AND the inner text of one tag together, which
    /// <see cref="EntityGuidPattern"/> (guid only) and <see cref="EntityTagPattern"/> (inner text
    /// only) each see half of.</summary>
    private static readonly Regex EntityTagWithGuidPattern =
        new(@"<entity\b[^>]*\bguid=""([^""]*)""[^>]*>(.*?)</entity>",
            RegexOptions.Compiled | RegexOptions.Singleline);
}
