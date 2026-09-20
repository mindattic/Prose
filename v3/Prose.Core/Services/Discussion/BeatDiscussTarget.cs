using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Core.Services.Discussion;

/// <summary>
/// A span of beat prose — the surface this whole feature was built for.
///
/// <para><b>Anchors resolve against the reader-visible text</b> — entity tags and emphasis markers
/// removed — not against the stored markup. Two reasons, and the second is the important one:</para>
/// <list type="number">
///   <item>The author selects from rendered prose, where a chip reads as a plain name. Anchoring
///   to what they actually saw is the only definition that cannot surprise them.</item>
///   <item>Every beat save <b>re-derives entity tags</b>, so the stored text churns by ~55
///   characters per added wrapper while the reader-visible text does not move at all. Anchoring to
///   the stored form would detach threads on saves that changed nothing a reader could see.</item>
/// </list>
/// <para>The cost is one mapping, owed by the splice path when it arrives: an offset here is a
/// position in the stripped text and must be converted before it can address stored markup.</para>
/// </summary>
public sealed class BeatDiscussTarget(IDbContextFactory<ProseDbContext> dbFactory) : IDiscussTarget
{
    public string Kind => DiscussionTargetKind.Beat;

    public async Task<DiscussSubject?> LoadAsync(Guid id, string? field, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var beat = await db.Beats.AsNoTracking()
            .Where(b => b.Id == id)
            .Select(b => new { b.Id, b.Text, b.TextHash, b.Number })
            .FirstOrDefaultAsync(ct);

        if (beat is null) return null;

        return new DiscussSubject(
            Kind,
            beat.Id,
            Field: null,
            Text: PlainText(beat.Text),
            // The hash still fingerprints the STORED text — it is the beat's identity, shared with
            // every other hash gate in the schema, and must not fork into a second definition.
            TextHash: beat.TextHash,
            Label: $"Beat #{beat.Number}");
    }

    /// <summary>The beat as a reader sees it. Must stay in step with what the editor's
    /// <c>serialize()</c> produces, which is what the browser sends when a selection is made.</summary>
    public static string PlainText(string? stored)
        => ProseInline.StripFormatting(BeatMarkup.StripEntityTags(stored ?? ""));
}
