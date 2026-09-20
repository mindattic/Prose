using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Prose.Core.Interfaces;
using Prose.Core.Models;

namespace Prose.Core.Services;

/// <summary>
/// Persists per-book, named/described/kind-tagged motif inventories — manually or LLM-authored
/// (via the <c>plant_motif</c>/<c>propose_motifs</c> MCP tools), not automatically extracted.
/// Stored as Settings('book_motifs:{bookId}').
///
/// Renamed from <c>MotifService</c> 2026-09-01 (was mislabeled "legacy KV-store, do not
/// extend" — it isn't: <c>get_motifs</c>/<c>plant_motif</c>/<c>propose_motifs</c> are live MCP
/// tools with no equivalent in <see cref="MotifLedgerService"/>, which only does automatic
/// per-beat occurrence counting, not named/described motifs a person or model registers by
/// hand). The two are genuinely distinct features that happen to share a domain concept, not a
/// legacy/replacement pair — see that class's own doc comment. <c>BookReviewService</c>'s and
/// <c>AnalyzeWritingQualityImpl</c>'s dependency on this class via the legacy Book/Chapter model
/// remains a known, documented limitation (see their own call sites) — do not read that as
/// grounds to consider this whole class legacy.
/// </summary>
public class AuthoredMotifRegistry
{
    private readonly SettingsKvStore kv;
    private readonly ILogger<AuthoredMotifRegistry> log;

    public AuthoredMotifRegistry(SettingsKvStore kv, ILogger<AuthoredMotifRegistry> log)
    {
        this.kv = kv;
        this.log = log;
    }

    /// <summary>Test-fixture ctor — wraps a SQLite-in-memory factory.</summary>
    public AuthoredMotifRegistry(IPathProvider paths, ILogger<AuthoredMotifRegistry> log)
        : this(new SettingsKvStore(Prose.Core.Data.TestDbFactory.For(paths, "settings")), log) { }

    private static string Key(string bookId) => $"book_motifs:{bookId}";

    public MotifInventory Load(string bookId)
    {
        var inv = kv.Get<MotifInventory>(Key(bookId));
        if (inv == null) return new MotifInventory { BookId = bookId };
        inv.BookId = bookId;
        return inv;
    }

    public void Save(MotifInventory inv)
    {
        inv.Modified = DateTime.UtcNow;
        kv.Set(Key(inv.BookId), inv);
    }

    /// <summary>Record a new motif. Idempotent — duplicates by name (case-insensitive) are merged.</summary>
    public void Plant(string bookId, string name, string description, MotifKind kind, string introducedInChapterId)
    {
        var inv = Load(bookId);
        var existing = inv.Motifs.FirstOrDefault(m => string.Equals(m.Name, name, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            // Merge: keep the original introduction chapter, extend description if longer.
            if (description.Length > existing.Description.Length)
            {
                existing.Description = description;
                Save(inv);
            }
            return;
        }
        inv.Motifs.Add(new BookMotif
        {
            Name = name,
            Description = description,
            Kind = kind,
            IntroducedInChapterId = introducedInChapterId,
        });
        Save(inv);
    }

    /// <summary>
    /// Propose new motif candidates from a chapter's prose. Heuristic: italicized phrases that
    /// recur, capitalized named objects (proper nouns not already in canon as characters/places),
    /// and repeated short phrases. Caller is responsible for showing these to the user for
    /// confirmation — the inventory is never auto-updated.
    /// Thin wrapper over <see cref="ProposeFromText"/> for the legacy Chapter-model caller
    /// (Motifs.razor) — <c>bookId</c>/plant storage was never actually Chapter-coupled (both are
    /// plain string keys via SettingsKvStore), only this entry point's signature was. New callers
    /// against the live Nodes/Beats model should call ProposeFromText directly.
    /// </summary>
    public List<MotifProposal> ProposeFromChapter(string bookId, Chapter chapter, IEnumerable<string> knownEntityNames) =>
        ProposeFromText(bookId, chapter.Title, chapter.Html ?? "", knownEntityNames);

    /// <summary>Same heuristic as <see cref="ProposeFromChapter"/>, decoupled from the legacy
    /// Chapter model — takes plain text and a label (a chapter/book title, used only in the
    /// human-readable Evidence string) so it can run against real Node/Beat prose.</summary>
    public List<MotifProposal> ProposeFromText(string bookId, string label, string prose, IEnumerable<string> knownEntityNames)
    {
        var inventory = Load(bookId);
        var existing = inventory.Motifs.Select(m => m.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var known = knownEntityNames.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var proposals = new List<MotifProposal>();

        // (1) Italicized phrases that recur — *like this* appearing 2+ times.
        var italics = System.Text.RegularExpressions.Regex.Matches(prose, @"\*([^*\n]{8,80})\*")
            .Select(m => m.Groups[1].Value.Trim())
            .GroupBy(s => s, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() >= 2)
            .Select(g => g.Key);
        foreach (var phrase in italics)
        {
            if (existing.Contains(phrase)) continue;
            proposals.Add(new MotifProposal
            {
                Name = phrase,
                Kind = MotifKind.Phrase,
                Description = $"Italicized phrase that recurs in this chapter — repeating it deliberately across chapters would thread a callback.",
                Evidence = $"appears multiple times within \"{label}\"",
            });
        }

        // (2) Capitalized named objects — single capitalized words that aren't characters/places.
        // Filters: must appear 2+ times, length 4-20, not at sentence start (heuristic: preceded by space-then-non-period).
        var named = System.Text.RegularExpressions.Regex.Matches(prose, @"(?<=\s|—)([A-Z][a-z]{3,19})\b")
            .Select(m => m.Groups[1].Value)
            .GroupBy(s => s, StringComparer.Ordinal)
            .Where(g => g.Count() >= 3 && !known.Contains(g.Key) && !existing.Contains(g.Key))
            .Select(g => g.Key)
            .Take(8);  // cap noise
        foreach (var name in named)
        {
            // Exclude obvious non-motifs: month/day names, common english words.
            if (IsStopwordLikeProper(name)) continue;
            proposals.Add(new MotifProposal
            {
                Name = name,
                Kind = MotifKind.Object,
                Description = $"Capitalized named object that recurs in this chapter — {name} appears repeatedly and is not in canon as a character or place.",
                Evidence = $"3+ occurrences in \"{label}\"",
            });
        }

        return proposals;
    }

    private static readonly HashSet<string> StopwordProper = new(StringComparer.OrdinalIgnoreCase)
    {
        "Monday","Tuesday","Wednesday","Thursday","Friday","Saturday","Sunday",
        "January","February","March","April","May","June","July","August","September","October","November","December",
        "Chapter","Part","Book","Volume","Series","Mr","Mrs","Ms","Dr",
    };

    private static bool IsStopwordLikeProper(string word) => StopwordProper.Contains(word);

    /// <summary>Mark a motif as referenced in a chapter. Used by the review pipeline to track health of each thread.</summary>
    public void RecordReference(string bookId, string motifName, string chapterId)
    {
        var inv = Load(bookId);
        var motif = inv.Motifs.FirstOrDefault(m => string.Equals(m.Name, motifName, StringComparison.OrdinalIgnoreCase));
        if (motif == null) return;
        if (!motif.ReferencedInChapterIds.Contains(chapterId))
        {
            motif.ReferencedInChapterIds.Add(chapterId);
            Save(inv);
        }
    }
}
