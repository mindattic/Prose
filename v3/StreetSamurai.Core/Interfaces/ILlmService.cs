namespace StreetSamurai.Core.Interfaces;

public interface ILlmService
{
    Task<bool> IsConfiguredAsync();
    Task<string> GenerateAsync(string system, string user, double temperature = 0.8, int maxTokens = 4096, CancellationToken ct = default);
}
