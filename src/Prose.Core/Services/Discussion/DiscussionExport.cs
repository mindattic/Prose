using System.Text;
using Prose.Core.Data.Entities;

namespace Prose.Core.Services.Discussion;

/// <summary>
/// A discussion, as markdown.
///
/// <para><b>Why a conversation needs an exit.</b> A thread is the record of why a passage is the
/// way it is — the prose-level answer to the question the decision ledger answers at the engine
/// level. That record is worth nothing if it can only be read inside one panel of one window: the
/// author wants to paste it into a note, send it to someone, or keep it beside the chapter.</para>
///
/// <para>Renders every block, including the ones a reader would otherwise never see in text form
/// — a confirmation and its steps, a measured table, the counts behind a chart. A transcript that
/// silently dropped the evidence would make the conversation look like opinion.</para>
/// </summary>
public static class DiscussionExport
{
    public static string ToMarkdown(
        DiscussionThread thread,
        IReadOnlyList<DiscussionTurn> turns,
        string? bookTitle = null,
        string? beatLabel = null)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"# {Title(thread)}");
        sb.AppendLine();

        var where = string.Join(" · ", new[] { bookTitle, beatLabel }.Where(s => !string.IsNullOrWhiteSpace(s)));
        if (where.Length > 0) sb.AppendLine($"*{where}*").AppendLine();

        sb.AppendLine($"> {Quote(thread.AnchorQuote)}");
        sb.AppendLine();

        // State is on the transcript because a thread about a line that was CUT reads completely
        // differently from one about a line that is still there, and the quote alone cannot say
        // which.
        sb.AppendLine($"State: **{thread.State}** · opened {thread.CreatedAt.ToLocalTime():yyyy-MM-dd HH:mm}");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();

        foreach (var turn in turns)
        {
            var who = turn.Role == DiscussionRole.Author ? "You" : "Claude";
            var how = turn.InputMode == DiscussionInputMode.Voice ? " (spoken)" : "";
            var what = turn.Intent is { Length: > 0 } ? $" · {turn.Intent}" : "";

            sb.AppendLine($"### {who}{how}{what}");
            sb.AppendLine($"<sub>{turn.At.ToLocalTime():yyyy-MM-dd HH:mm}" +
                          (turn.Cost > 0 ? $" · {turn.Cost:C4}" : "") + "</sub>");
            sb.AppendLine();

            foreach (var block in DiscussionContent.Deserialize(turn.ContentJson))
                Render(sb, block);

            sb.AppendLine();
        }

        var total = turns.Sum(t => t.Cost);
        if (total > 0) sb.AppendLine("---").AppendLine().AppendLine($"Total cost: {total:C4}");

        return sb.ToString();
    }

    private static void Render(StringBuilder sb, DiscussionBlock block)
    {
        switch (block)
        {
            case DiscussionBlock.Text t:
                sb.AppendLine(t.Markdown.Trim()).AppendLine();
                break;

            case DiscussionBlock.Quote q:
                sb.AppendLine($"> {Quote(q.Excerpt)}");
                if (q.Label is { Length: > 0 }) sb.AppendLine($"> — {q.Label}");
                sb.AppendLine();
                break;

            case DiscussionBlock.Choice c:
                sb.AppendLine($"**{c.Question}**").AppendLine();
                foreach (var o in c.Options)
                    sb.AppendLine($"- {o.Label}{(o.Detail is null ? "" : $" — {o.Detail}")}");
                sb.AppendLine();
                break;

            case DiscussionBlock.Confirm k:
                sb.AppendLine($"**Heard:** {k.Restatement}").AppendLine();
                for (var i = 0; i < k.Steps.Count; i++) sb.AppendLine($"{i + 1}. {k.Steps[i]}");
                if (k.Caution is not null) sb.AppendLine().AppendLine($"**Caution:** {k.Caution}");
                sb.AppendLine();
                break;

            case DiscussionBlock.Proposal p:
                sb.AppendLine($"**A change was proposed** (`{p.ProposalId}`).").AppendLine();
                break;

            case DiscussionBlock.Table t2:
                if (t2.Caption is { Length: > 0 }) sb.AppendLine($"**{t2.Caption}**").AppendLine();
                sb.AppendLine("| " + string.Join(" | ", t2.Columns.Select(Cell)) + " |");
                sb.AppendLine("|" + string.Concat(t2.Columns.Select(_ => " --- |")));
                foreach (var row in t2.Rows)
                    sb.AppendLine("| " + string.Join(" | ", Fit(row, t2.Columns.Count).Select(Cell)) + " |");
                sb.AppendLine();
                break;

            case DiscussionBlock.Chart c2:
                // As a table, not as a picture. A chart in a markdown transcript would have to be
                // an image file beside it, and a transcript that depends on a sidecar is one that
                // arrives broken; the numbers are what the chart was showing anyway.
                sb.AppendLine($"**{c2.Title}**").AppendLine();
                foreach (var bar in c2.Bars)
                    sb.AppendLine($"- {bar.Label}: {bar.Value:0.##}{c2.Unit}");
                sb.AppendLine();
                break;
        }
    }

    private static string Title(DiscussionThread thread)
        => string.IsNullOrWhiteSpace(thread.Title)
            ? $"Discussion — {Shorten(thread.AnchorQuote, 60)}"
            : thread.Title!;

    /// <summary>Flattened to one line: a multi-line blockquote needs "&gt; " on every line, and a
    /// transcript with a broken quote block in it reads as corrupt.</summary>
    private static string Quote(string? s)
        => (s ?? "").Replace("\r", "").Replace('\n', ' ').Trim();

    /// <summary>A pipe inside a cell ends the cell. Escaped rather than stripped, so a passage
    /// that genuinely contains one still reads correctly.</summary>
    private static string Cell(string? s)
        => (s ?? "").Replace("|", "\\|").Replace("\r", "").Replace('\n', ' ');

    /// <summary>Pads a short row and truncates a long one. A malformed table breaks every row
    /// after it, and a transcript is not worth throwing an exception over.</summary>
    private static IEnumerable<string> Fit(IReadOnlyList<string> row, int width)
    {
        for (var i = 0; i < width; i++) yield return i < row.Count ? row[i] : "";
    }

    private static string Shorten(string? s, int max)
    {
        s = Quote(s);
        return s.Length <= max ? s : s[..(max - 1)] + "…";
    }
}
