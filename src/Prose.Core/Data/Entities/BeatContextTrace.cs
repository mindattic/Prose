using Microsoft.EntityFrameworkCore;

namespace Prose.Core.Data.Entities;

/// <summary>
/// Beat Context Archive, Part F2: a permanent, per-beat snapshot of the whole merged
/// <see cref="Services.BeatGeneratorService.BeatContext"/> — every one of its ~25 guidance/
/// context fields, verbatim, exactly as handed to <c>BeatGeneratorService.GenerateBeatAsync</c>.
/// One JSON blob column, not discrete columns — same precedent <see cref="DcmBeatSnapshot"/>
/// already set: the consumption pattern is "show me everything for beat N," never "search for
/// beats where field X said Y." Combined with the existing <see cref="BeatServiceLog"/> (which
/// records WHICH services fired), this is the direct, structured answer to "what did each
/// stage actually contribute" — the durable alternative to scattering log lines through ~20
/// service files. Written by <see cref="Services.ProseWriterRouter"/> right before the LLM
/// call, best-effort, so a trace exists even for a beat that fails to generate.
/// </summary>
[Index(nameof(BeatId))]
public class BeatContextTrace
{
    public int Id { get; set; }
    public Guid BeatId { get; set; }
    public Guid NodeId { get; set; }
    public Guid UniverseId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>JSON-serialized full <c>BeatContext</c> — every guidance/context field,
    /// verbatim, as actually handed to <c>BeatGeneratorService.GenerateBeatAsync</c>.</summary>
    public string ContextJson { get; set; } = "{}";
}
