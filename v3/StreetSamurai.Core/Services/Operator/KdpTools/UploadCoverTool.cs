using System.Text.Json;

namespace StreetSamurai.Core.Services.Operator.KdpTools;

/// <summary>
/// Attaches a cover image file on the new-listing "Kindle eBook Content" page's cover upload
/// control, via the same CDP <c>DOM.setFileInputFiles</c> mechanism as
/// <see cref="UploadManuscriptTool"/> — confirmed live the cover file input's stable id is
/// <c>data-assets-cover-file-upload-AjaxInput</c> (accepts .tiff/.tif/.jpeg/.jpg). Before this
/// input is usable, the page defaults to the "Use Cover Creator" choice — click the "Upload a
/// cover you already have (JPG/TIFF only)" accordion row first via click_button (it's an ordinary
/// clickable link, no special handling needed) to reveal this control.
/// </summary>
public class UploadCoverTool : IKdpTool
{
    private const string CoverInputId = "data-assets-cover-file-upload-AjaxInput";

    public string Name => "upload_cover";

    public string Description =>
        "Attach a cover image file to the current page's cover upload control (id " +
        "data-assets-cover-file-upload-AjaxInput). Only works after clicking 'Upload a cover " +
        "you already have (JPG/TIFF only)' — use click_button for that first if you haven't " +
        "already. No native file dialog appears. After calling, use get_page_status to confirm " +
        "KDP accepted the cover before continuing.";

    public string ParametersJsonSchema => """
    {
      "type": "object",
      "properties": {
        "file_path": { "type": "string", "description": "Absolute path to the cover .jpg/.jpeg/.tif/.tiff file to upload." }
      },
      "required": ["file_path"]
    }
    """;

    public async Task<string> InvokeAsync(JsonElement args, KdpOperatorContext ctx, CancellationToken ct)
    {
        var filePath = args.GetProperty("file_path").GetString() ?? "";
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            return JsonSerializer.Serialize(new { error = $"File not found: {filePath}" });

        try
        {
            await ctx.Browser.InjectFileAsync(filePath, $"#{CoverInputId}", ct);
            return JsonSerializer.Serialize(new { ok = true, file = filePath });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }
}
