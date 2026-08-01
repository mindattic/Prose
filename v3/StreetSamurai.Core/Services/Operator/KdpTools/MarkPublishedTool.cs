using System.Text.Json;

namespace StreetSamurai.Core.Services.Operator.KdpTools;

/// <summary>
/// Closes the loop after KDP confirms a book actually published: calls
/// <see cref="KdpMarkPublishedService"/> directly (same service <c>ss --kdp-mark-published</c>
/// wraps) so the book drops off the "needs republish" list on the next manifest run.
/// </summary>
public class MarkPublishedTool : IKdpTool
{
    private readonly KdpMarkPublishedService service;

    public MarkPublishedTool(KdpMarkPublishedService service)
    {
        this.service = service;
    }

    public string Name => "mark_published";

    public string Description =>
        "Record that a book's republish actually completed on KDP. Call this ONLY after " +
        "get_page_status (or a visible confirmation modal) shows the publish succeeded — " +
        "never speculatively. slug identifies the book (from the book list you were given).";

    public string ParametersJsonSchema => """
    {
      "type": "object",
      "properties": {
        "slug": { "type": "string", "description": "The book's slug, as given in your book list." },
        "url": { "type": "string", "description": "Optional: the book's Amazon product URL, if shown or already known." },
        "title_id": { "type": "string", "description": "Optional: KDP's internal dashboard titleId, if captured from a link during this session." }
      },
      "required": ["slug"]
    }
    """;

    public async Task<string> InvokeAsync(JsonElement args, KdpOperatorContext ctx, CancellationToken ct)
    {
        var slug = args.GetProperty("slug").GetString() ?? "";
        var url = args.TryGetProperty("url", out var u) ? u.GetString() : null;
        var titleId = args.TryGetProperty("title_id", out var t) ? t.GetString() : null;

        var repoRoot = KdpManifestService.FindRepoRoot();
        var result = await service.MarkPublishedAsync(slug, url, titleId, repoRoot, ct);
        return JsonSerializer.Serialize(result);
    }
}
