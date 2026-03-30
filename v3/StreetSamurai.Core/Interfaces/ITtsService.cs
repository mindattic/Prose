namespace StreetSamurai.Core.Interfaces;

public interface ITtsService
{
    Task<bool> IsConfiguredAsync();
    Task<byte[]> SynthesizeAsync(string text, string? voiceId = null, CancellationToken ct = default);
}
