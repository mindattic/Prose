using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Services;
using Prose.Core.Services.Discussion;

namespace Prose.WriterUi.Services;

/// <summary>
/// Transcription for the Writer.
///
/// <para>Scoped, like the other UI services, and for the same non-optional reason: it pins the
/// flow universe to the book being dictated into before it reads that book's entity tags. Unpinned,
/// every universe-scoped read behind it returns nothing, and the result is not an error — it is a
/// silently empty vocabulary, which looks exactly like transcription simply being bad at invented
/// names.</para>
/// </summary>
public sealed class SpeechUiService(
    IDbContextFactory<ProseDbContext> dbFactory,
    IUniverseContext universe,
    SpeechService speech,
    SpeechVocabularyService vocabulary)
{
    /// <param name="Primed">How many characters of this book's proper nouns the model was given.
    /// Zero means it was guessing at every invented name, which the panel should be able to say.</param>
    public sealed record Heard(
        string Text, string Engine, string Model, double Cost, string? RecordingPath, int Primed);

    public SpeechAvailability Probe() => speech.Probe();

    /// <summary>
    /// Turn a recording from <c>proseMic</c> into words, primed with the book's own names.
    /// </summary>
    public async Task<Heard> TranscribeAsync(
        Guid bookNodeId, string base64, string mimeType, int durationMs, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(base64))
            throw new InvalidOperationException("Nothing was recorded.");

        await ScopeToBookAsync(bookNodeId, ct);

        var primer = await vocabulary.ForBookAsync(bookNodeId, ct: ct);
        var audio = Convert.FromBase64String(base64);

        var transcript = await speech.TranscribeAsync(audio, mimeType, primer, durationMs, ct: ct);

        return new Heard(
            transcript.Text, transcript.Engine, transcript.Model, transcript.Cost,
            transcript.RecordingPath, primer.Length);
    }

    /// <summary>Mirrors <see cref="DiscussionUiService"/>'s own scoping step — see that class for
    /// why this is not optional.</summary>
    private async Task ScopeToBookAsync(Guid bookNodeId, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var universeId = await db.Nodes.AsNoTracking().IgnoreQueryFilters()
            .Where(n => n.Id == bookNodeId)
            .Select(n => (Guid?)n.UniverseId)
            .FirstOrDefaultAsync(ct);
        if (universeId is { } id && id != Guid.Empty) universe.SetFlowUniverse(id);
    }
}
