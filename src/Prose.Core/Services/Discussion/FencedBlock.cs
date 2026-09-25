namespace Prose.Core.Services.Discussion;

/// <summary>
/// Lifting the replacement prose out of an answer.
///
/// <para>The system prompt asks for a replacement "on its own, in a fenced block, with one line
/// saying what changed", precisely so this can take it whole. The alternative — inferring which
/// sentences of the answer are the new prose — is how a proposal ends up containing the
/// assistant's explanation of itself.</para>
///
/// <para><b>The LAST fence, not the first.</b> An answer that argues before it drafts often quotes
/// the existing passage first to show what it is changing, and taking the first fence would
/// propose replacing the passage with itself.</para>
/// </summary>
public static class FencedBlock
{
    /// <summary>The contents of the final ``` fence, or null when there is none.</summary>
    public static string? Last(string? markdown)
    {
        if (string.IsNullOrEmpty(markdown)) return null;

        var lines = markdown.Replace("\r\n", "\n").Split('\n');

        // Fences pair FORWARDS (open, close, open, close…). Walking backwards paired an unclosed
        // final fence with the previous block's closing fence, and proposed the explanation text
        // between them as the replacement prose.
        var fences = new List<int>();
        for (var i = 0; i < lines.Length; i++)
            if (IsFence(lines[i])) fences.Add(i);

        // An unmatched final fence is a half-written answer, not a replacement. Proposing the rest
        // of the reply as prose would be considerably worse than proposing nothing.
        if (fences.Count < 2 || fences.Count % 2 != 0) return null;

        var open = fences[^2];
        var close = fences[^1];
        var body = string.Join("\n", lines[(open + 1)..close]).Trim('\n');
        return body.Length == 0 ? null : body;
    }

    /// <summary>A fence line: three or more backticks, optionally with a language tag.</summary>
    private static bool IsFence(string line)
    {
        var trimmed = line.TrimStart();
        return trimmed.StartsWith("```", StringComparison.Ordinal);
    }
}
