namespace Prose.Core.Interfaces;

/// <summary>A rendered cover image plus the file extension it should be saved with.</summary>
public record CoverImageResult(byte[] Bytes, string Extension);

/// <summary>
/// One image-generation backend that can render a book cover from a text prompt.
/// Implementations: <c>openai</c> (gpt-image-1), <c>stability</c> (Stable Image / SD3),
/// <c>google</c> (Imagen via the Gemini API). Selected by <see cref="Id"/> in
/// <c>CoverImageService</c> so the author can pick per book which backend to use.
/// </summary>
public interface ICoverImageProvider
{
    /// <summary>Stable provider key used in CLI/MCP calls and stored on
    /// <c>Node.CoverImageProvider</c>: "openai" | "stability" | "google".</summary>
    string Id { get; }

    /// <summary>True once the provider's API key is configured in Settings.</summary>
    bool IsConfigured { get; }

    /// <summary>Renders a portrait-orientation cover image from <paramref name="prompt"/>.</summary>
    Task<CoverImageResult> GenerateAsync(string prompt, CancellationToken ct = default);
}
