using Microsoft.EntityFrameworkCore;

namespace Prose.Core.Data.Entities;

/// <summary>
/// "Yes, I meant that" — one check, on one beat, at one exact wording.
///
/// <para><b>This is what makes a blocking gate survivable.</b> The gate stops the author on
/// anything it finds, and some of what it finds will be wrong. Without a memory, one false
/// positive blocks every subsequent save of that beat forever, and the author turns the whole
/// feature off inside a day — which is precisely how Loop 2 died the first time: 46 cliché
/// findings on BCODA, zero ever applied, the tier deleted under RFC 0009.</para>
///
/// <para><b>Keyed on the TEXT, not on the finding.</b> <see cref="TextHash"/> is the beat's hash
/// at the moment the author said it was intentional. Change one word and the dismissal stops
/// applying, because the thing they approved is no longer the thing on the page. That is the same
/// hash-gate shape <c>Beat.ObligationScanHash</c> and <c>BeatChecklistResult.BeatTextHash</c>
/// already use, and it means noise decays instead of accumulating.</para>
/// </summary>
[Index(nameof(BeatId), nameof(TextHash))]
public class BeatCheckDismissal
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid BeatId { get; set; }

    /// <summary>The beat's <c>TextHash</c> when this was dismissed. The dismissal is only valid
    /// while the prose still hashes to this.</summary>
    public string TextHash { get; set; } = "";

    /// <summary>
    /// Which check, and about what.
    ///
    /// <para>Not just the check's kind: a beat can carry two obligations and the author may mean
    /// one of them and not the other. Built from the kind plus a stable hash of the headline, so
    /// dismissing one finding never silences a different one of the same type.</para>
    /// </summary>
    public string CheckKey { get; set; } = "";

    /// <summary>What the author was told, kept verbatim. Six weeks later "I dismissed something
    /// about an obligation" is not a record of anything.</summary>
    public string Headline { get; set; } = "";

    /// <summary>Why they said it was intentional, when they bothered to say. Optional, and never
    /// demanded — a gate that requires an essay is a gate that gets clicked through.</summary>
    public string? Note { get; set; }

    public DateTime At { get; set; } = DateTime.UtcNow;
}
