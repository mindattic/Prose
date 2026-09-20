using System.Text.Json;

namespace Prose.Core.Services.Operator.KdpTools;

/// <summary>
/// Records the "Live - Updates publishing" state <c>find_and_open_book</c> detects when a book's
/// row exists but has no edit-content link (KDP hides that menu while an edition is already
/// mid-publish). Call this immediately after that signal, before log_note/stop, so the sidebar
/// reports "Publishing" instead of "Outdated" until the ~72-hour window clears — see
/// <see cref="KdpMarkPublishedService.MarkPublishingDetectedAsync"/>.
/// </summary>
public class MarkPublishingDetectedTool : IKdpTool
{
    private readonly KdpMarkPublishedService service;

    public MarkPublishingDetectedTool(KdpMarkPublishedService service)
    {
        this.service = service;
    }

    public string Name => "mark_publishing_detected";

    public string Description =>
        "Record that find_and_open_book found this book's row on the bookshelf with no edit " +
        "link — KDP's normal, temporary 'Live - Updates publishing' state, not an error. Call " +
        "this with the book's slug right after that signal, before log_note and stopping, so " +
        "the sidebar shows 'Publishing' instead of 'Outdated' until KDP's review window clears.";

    public string ParametersJsonSchema => """
    {
      "type": "object",
      "properties": {
        "slug": { "type": "string", "description": "The book's slug, as given in your book list." }
      },
      "required": ["slug"]
    }
    """;

    public async Task<string> InvokeAsync(JsonElement args, KdpOperatorContext ctx, CancellationToken ct)
    {
        var slug = args.GetProperty("slug").GetString() ?? "";
        var ok = await service.MarkPublishingDetectedAsync(slug, ct);
        return JsonSerializer.Serialize(new { recorded = ok });
    }
}
