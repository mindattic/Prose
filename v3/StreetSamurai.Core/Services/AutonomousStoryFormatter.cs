using System.Text;
using System.Web;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Serializes an AutonomousStory to Markdown or HTML for persistence as a StoryProject body.
/// Used by both the UI (Stories.razor) and the CLI so generated output is byte-identical
/// regardless of entry point.
/// </summary>
public static class AutonomousStoryFormatter
{
    public static string ToHtml(AutonomousStory story)
    {
        var sb = new StringBuilder();
        sb.Append($"<h1>{HttpUtility.HtmlEncode(story.Title)}</h1>");
        sb.Append($"<p><em>Protagonist: {HttpUtility.HtmlEncode(story.Protagonist)}</em></p>");

        int lastAct = 0;
        foreach (var beat in story.Beats)
        {
            if (beat.Act != lastAct)
            {
                var actName = story.Outline?.Acts.FirstOrDefault(a => a.ActNumber == beat.Act)?.Name ?? $"Act {beat.Act}";
                sb.Append($"<h2>Act {beat.Act}: {HttpUtility.HtmlEncode(actName)}</h2>");
                lastAct = beat.Act;
            }

            var paragraphs = beat.Text.Split(["\n\n", "\r\n\r\n"], StringSplitOptions.RemoveEmptyEntries);
            foreach (var p in paragraphs)
                sb.Append($"<p>{HttpUtility.HtmlEncode(p.Trim())}</p>");

            sb.Append("<hr/>");
        }

        return sb.ToString();
    }

    // Markdown is the preferred format. The WriteStory editor treats StoryProject.Html as
    // Markdown source, so emitting Markdown here keeps the round-trip lossless: inline
    // *italics*, em-dashes, and section breaks survive unchanged instead of being
    // HtmlEncoded into literal asterisks on save.
    public static string ToMarkdown(AutonomousStory story)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# {story.Title}");
        sb.AppendLine();
        sb.AppendLine($"*Protagonist: {story.Protagonist}*");
        sb.AppendLine();

        int lastAct = 0;
        foreach (var beat in story.Beats)
        {
            if (beat.Act != lastAct)
            {
                var actName = story.Outline?.Acts.FirstOrDefault(a => a.ActNumber == beat.Act)?.Name ?? $"Act {beat.Act}";
                sb.AppendLine($"## Act {beat.Act}: {actName}");
                sb.AppendLine();
                lastAct = beat.Act;
            }

            sb.AppendLine(beat.Text.Trim());
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }
}
