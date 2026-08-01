using System.Text.Json;

namespace StreetSamurai.Core.Services.Operator.KdpTools;

/// <summary>
/// Attaches a manuscript file to the current page's manuscript upload control via
/// <see cref="IKdpBrowser.InjectFileAsync"/> (CDP <c>DOM.setFileInputFiles</c>) — no native OS
/// file dialog ever opens. Verified against the live "Edit eBook content" page: the manuscript
/// file input is the first <c>input[type=file]</c> in DOM order (the Manuscript section sits
/// above the Cover section on that page), which is why the default selector targets it directly
/// rather than the cover upload control.
/// </summary>
public class UploadManuscriptTool : IKdpTool
{
    public string Name => "upload_manuscript";

    public string Description =>
        "Attach a manuscript file to the current 'Edit eBook content' page's manuscript " +
        "upload control. No native file dialog appears — the file is attached directly. " +
        "Call this once you're on a book's Edit eBook content page. After calling, use " +
        "get_page_status to confirm KDP accepted the upload before continuing.";

    public string ParametersJsonSchema => """
    {
      "type": "object",
      "properties": {
        "file_path": { "type": "string", "description": "Absolute path to the .epub manuscript file to upload." }
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
            await ctx.Browser.InjectFileAsync(filePath, "input[type=file]", ct);
            return JsonSerializer.Serialize(new { ok = true, file = filePath });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }
}
