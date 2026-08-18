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
}
