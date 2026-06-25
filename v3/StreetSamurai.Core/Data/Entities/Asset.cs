namespace StreetSamurai.Core.Data.Entities;

/// <summary>
/// Binary media asset — book cover, logo, watermark, or promotional image.
/// <para>
/// <b>Type values:</b> cover_image | logo | watermark | banner | thumbnail | promotional
/// </para>
/// <para>
/// <b>Storage:</b> <see cref="Data"/> holds the raw bytes (VARBINARY MAX).
/// When <see cref="StorageUrl"/> is set the engine serves from that URL and
/// <see cref="Data"/> may be null — reserved for future Azure Blob offload.
/// </para>
/// </summary>
public class Asset
{
    public Guid    Id            { get; set; } = Guid.NewGuid();

    /// <summary>cover_image | logo | watermark | banner | thumbnail | promotional</summary>
    public string  Type          { get; set; } = "cover_image";

    /// <summary>Owning strand, if any. Null for global assets (logo, watermark).</summary>
    public Guid?   StrandId      { get; set; }

    /// <summary>Universe scope. Null = cross-universe (logo, watermark).</summary>
    public Guid?   UniverseId    { get; set; }

    public string  FileName      { get; set; } = "";

    /// <summary>MIME type: image/png | image/jpeg | image/webp</summary>
    public string  ContentType   { get; set; } = "image/png";

    /// <summary>Raw image bytes. Null only when StorageUrl is set.</summary>
    public byte[]? Data          { get; set; }

    /// <summary>Reserved: Azure Blob URL when offloaded from DB binary.</summary>
    public string? StorageUrl    { get; set; }

    public long    FileSizeBytes { get; set; }
    public int?    Width         { get; set; }
    public int?    Height        { get; set; }
    public string? Notes         { get; set; }

    public DateTime CreatedAt    { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt    { get; set; } = DateTime.UtcNow;

    // Navigation
    public Strand? Strand        { get; set; }
    public ICollection<CoverImagePrompt> Prompts { get; set; } = new List<CoverImagePrompt>();
}

/// <summary>
/// Structured image-generation prompt for a specific AI generator.
/// Covers all major generators: ChatGPT (DALL-E 3 / gpt-image-1), MidJourney,
/// Google Gemini/Imagen, Stable Diffusion, Ideogram, Flux, Adobe Firefly.
/// </summary>
public class CoverImagePrompt
{
    public Guid    Id             { get; set; } = Guid.NewGuid();

    /// <summary>Strand this prompt was written for. Null = global/template prompt.</summary>
    public Guid?   StrandId       { get; set; }

    /// <summary>Asset produced by API generation from this prompt. Null = not yet generated.</summary>
    public Guid?   AssetId        { get; set; }

    /// <summary>chatgpt | midjourney | gemini | stable_diffusion | ideogram | flux | firefly</summary>
    public string  Generator      { get; set; } = "chatgpt";

    /// <summary>Author label for quick identification: "ATTE v1 dark rain"</summary>
    public string? Label          { get; set; }

    /// <summary>The main generation prompt.</summary>
    public string  PromptText     { get; set; } = "";

    /// <summary>
    /// Negative conditioning for MidJourney (<c>--no ...</c>) and Stable Diffusion.
    /// Not used by ChatGPT or Gemini.
    /// </summary>
    public string? NegativePrompt { get; set; }

    /// <summary>
    /// JSON blob for model-specific parameters.
    /// MidJourney: <c>{"ar":"2:3","v":"6.1","style":"raw"}</c>
    /// Stable Diffusion: <c>{"steps":30,"cfg_scale":7,"sampler":"dpm++_2m"}</c>
    /// ChatGPT: <c>{"size":"1024x1536","quality":"hd"}</c>
    /// </summary>
    public string? Parameters     { get; set; }

    /// <summary>When the image was actually generated via API. Null = not yet generated or generated manually.</summary>
    public DateTime? GeneratedAt  { get; set; }

    public string?   Notes        { get; set; }
    public DateTime  CreatedAt    { get; set; } = DateTime.UtcNow;
    public DateTime  UpdatedAt    { get; set; } = DateTime.UtcNow;

    // Navigation
    public Strand? Strand  { get; set; }
    public Asset?  Asset   { get; set; }
}
