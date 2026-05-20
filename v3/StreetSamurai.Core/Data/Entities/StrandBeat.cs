namespace StreetSamurai.Core.Data.Entities;

/// <summary>
/// Ordered membership: a <see cref="Beat"/> appears in a <see cref="Strand"/>
/// at a fractional <see cref="SortKey"/>. A single beat may live in many
/// strands (composition over duplication); the prose and audio are owned
/// by the Beat row itself, so editing in one place updates everywhere.
/// </summary>
public class StrandBeat
{
    public Guid StrandId { get; set; }
    public Strand? Strand { get; set; }

    public Guid BeatId { get; set; }
    public Beat? Beat { get; set; }

    /// <summary>Fractional sort key within the strand. Inserts between
    /// existing siblings find a midpoint — no downstream renumbering.</summary>
    public double SortKey { get; set; }
}
