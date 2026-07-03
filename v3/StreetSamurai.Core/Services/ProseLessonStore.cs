using System.Text;

namespace StreetSamurai.Core.Services;

/// <summary>
/// SQL-KV-backed store for editorial lessons — author rulings that reviewers
/// must respect so they stop penalizing beats the author has already decided
/// are doing their job (score-vs-function, delight, surprise, etc.).
///
/// <para><b>Scope</b> is one of:
/// <list type="bullet">
/// <item><term>global</term><description>applies to every node review.</description></item>
/// <item><term>node:&lt;slug&gt;</term><description>applies only when reviewing that specific node.</description></item>
/// <item><term>beat:&lt;id&gt;</term><description>applies only to that beat (future-use; surfaced alongside global + node).</description></item>
/// </list></para>
///
/// <para><b>Kind</b> values (free-form string; canonical set below):
/// <list type="bullet">
/// <item><term>score-vs-function</term><description>this beat scores low in isolation but earns its place in the sequence.</description></item>
/// <item><term>delight</term><description>deliberate delightful / surprising choice; do not flag as error.</description></item>
/// <item><term>voice</term><description>voice rule the author has ruled acceptable / intentional.</description></item>
/// <item><term>pacing</term><description>pacing decision the author has locked in.</description></item>
/// <item><term>continuity</term><description>continuity choice already adjudicated by the author.</description></item>
/// <item><term>other</term><description>catch-all for any ruling not covered above.</description></item>
/// </list></para>
/// </summary>
public class ProseLessonStore
{
    private const string KvKey = "prose_lessons";

    private readonly SettingsKvStore kv;

    public ProseLessonStore(SettingsKvStore kv)
    {
        this.kv = kv;
    }

    // ── POCOs ────────────────────────────────────────────────────────────────

    public sealed class ProseLesson
    {
        public string Id      { get; set; } = "";
        public string Scope   { get; set; } = "global";
        public string Kind    { get; set; } = "other";
        public string Text    { get; set; } = "";
        public DateTime AddedAt { get; set; }
    }

    public sealed class ProseLessonCollection
    {
        public List<ProseLesson> Lessons { get; set; } = new();
    }

    // ── Write ────────────────────────────────────────────────────────────────

    /// <summary>Add a new lesson and persist it. The Id is generated automatically.</summary>
    public void Add(string scope, string kind, string text)
    {
        var doc = kv.Get<ProseLessonCollection>(KvKey) ?? new ProseLessonCollection();
        doc.Lessons.Add(new ProseLesson
        {
            Id      = Guid.CreateVersion7().ToString(),
            Scope   = scope.Trim(),
            Kind    = kind.Trim(),
            Text    = text.Trim(),
            AddedAt = DateTime.UtcNow,
        });
        kv.Set(KvKey, doc);
    }

    // ── Read ─────────────────────────────────────────────────────────────────

    /// <summary>Returns every lesson across all scopes, ordered by AddedAt.</summary>
    public List<ProseLesson> ListAll()
    {
        var doc = kv.Get<ProseLessonCollection>(KvKey);
        return doc?.Lessons ?? new List<ProseLesson>();
    }

    /// <summary>Returns all <c>global</c> lessons plus any lessons scoped to
    /// <paramref name="nodeSlug"/> (i.e. <c>node:&lt;slug&gt;</c>).
    /// When <paramref name="nodeSlug"/> is null, returns only global lessons.</summary>
    public List<ProseLesson> ListForScope(string? nodeSlug)
    {
        var all = ListAll();
        return all.Where(l =>
            string.Equals(l.Scope, "global", StringComparison.OrdinalIgnoreCase)
            || (!string.IsNullOrWhiteSpace(nodeSlug)
                && string.Equals(l.Scope, $"node:{nodeSlug}", StringComparison.OrdinalIgnoreCase)))
            .OrderBy(l => l.AddedAt)
            .ToList();
    }

    /// <summary>Formats the relevant lessons as a reviewer-facing context block
    /// for injection into ballot prompts, or returns null if there are no lessons
    /// applicable to this scope.</summary>
    public string? FormatBlockForReview(string? nodeSlug)
    {
        var lessons = ListForScope(nodeSlug);
        if (lessons.Count == 0) return null;

        var sb = new StringBuilder();
        sb.AppendLine(
            "EDITORIAL LESSONS — the author has explicitly ruled on these. " +
            "RESPECT them; do NOT penalize a beat for something already ruled acceptable. " +
            "A beat doing its job in the sequence outranks a high standalone polish score.");
        foreach (var l in lessons)
            sb.AppendLine($"- [{l.Kind}] {l.Text}");

        return sb.ToString().TrimEnd();
    }
}
