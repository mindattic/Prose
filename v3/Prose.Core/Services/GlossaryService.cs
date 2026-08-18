using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Interfaces;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Prose.Core.Services;

public record GlossaryGenerateResult(string UniverseSlug, int TermCount, string HtmlPath, string JsonPath, string TxtPath);
public record BookGlossaryGenerateResult(string NodeCode, int TermCount, int UniverseTermCount, string HtmlPath, string JsonPath, string TxtPath);

/// <summary>
/// Master Glossary — universe-scoped acronym/term definitions, generated to each universe's
/// base folder (docs/universes/{SLUG}/Glossary.htm|.json|.txt) and, per book, filtered down to
/// only the terms that actually appear in that book's live prose (docs/nodes/{CODE}-Glossary.*).
///
/// This exists so a book never has to interrupt its own voice to spell out an acronym before
/// its first use (SS-LAW-20) — the reader gets the full definition in back matter, with more
/// room for context than an in-voice gloss would ever earn on the page.
///
/// The per-book subset is detected live against current beat text, not stored as a join —
/// a term dropped from prose falls out of the book's glossary on the next regenerate, and a
/// term added to the universe glossary after a book's last edit picks up automatically.
/// </summary>
public class GlossaryService(
    IDbContextFactory<ProseDbContext> dbFactory,
    IPathProvider paths,
    ILogger<GlossaryService> log)
{
    public async Task<GlossaryTerm> UpsertAsync(
        Guid universeId, string term, string? fullForm, string definition, string? category, double sortKey = 0,
        CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var existing = await db.GlossaryTerms.IgnoreQueryFilters()
            .FirstOrDefaultAsync(g => g.UniverseId == universeId && g.Term == term, ct);

        if (existing != null)
        {
            existing.FullForm = fullForm;
            existing.Definition = definition;
            existing.Category = category;
            existing.SortKey = sortKey;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            existing = new GlossaryTerm
            {
                UniverseId = universeId, Term = term, FullForm = fullForm,
                Definition = definition, Category = category, SortKey = sortKey,
            };
            db.GlossaryTerms.Add(existing);
        }
        await db.SaveChangesAsync(ct);
        return existing;
    }

    public async Task<IReadOnlyList<GlossaryTerm>> ListAsync(Guid universeId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.GlossaryTerms.IgnoreQueryFilters()
            .Where(g => g.UniverseId == universeId)
            // Standard back-of-book glossary convention: one flat alphabetical list, no
            // category grouping (author decision 2026-08-05 — Category stays on the entity
            // for potential future use, just isn't used to group rendering).
            .OrderBy(g => g.Term)
            .ToListAsync(ct);
    }

    public async Task<GlossaryGenerateResult> GenerateMasterAsync(Guid universeId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var universe = await db.Universes.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == universeId, ct)
            ?? throw new InvalidOperationException($"Universe {universeId} not found.");
        var terms = await ListAsync(universeId, ct);

        var dir = Path.Combine(paths.DataRoot, "docs", "universes", universe.Slug.ToUpperInvariant());
        var (html, json, txt) = RenderAll($"{universe.Name} — Master Glossary", terms);

        var htmlPath = Path.Combine(dir, "Glossary.htm");
        var jsonPath = Path.Combine(dir, "Glossary.json");
        var txtPath = Path.Combine(dir, "Glossary.txt");
        await GeneratedFileWriter.WriteReadOnlyAsync(htmlPath, html, ct);
        await GeneratedFileWriter.WriteReadOnlyAsync(jsonPath, json, ct);
        await GeneratedFileWriter.WriteReadOnlyAsync(txtPath, txt, ct);

        log.LogInformation("[glossary] {Universe} master glossary — {Count} terms -> {Dir}",
            universe.Name, terms.Count, dir);
        return new GlossaryGenerateResult(universe.Slug, terms.Count, htmlPath, jsonPath, txtPath);
    }

    /// <summary>The subset of a book's universe glossary whose terms appear in its live
    /// prose, alphabetically ordered — for callers (e.g. export services) that want the list
    /// without writing the docs/nodes/{CODE}-Glossary.* mirror files.</summary>
    public async Task<IReadOnlyList<GlossaryTerm>> GetUsedTermsAsync(Guid nodeId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var node = await db.Nodes.IgnoreQueryFilters().FirstOrDefaultAsync(n => n.Id == nodeId, ct)
            ?? throw new InvalidOperationException($"Node {nodeId} not found.");
        var (used, _) = await GetUsedTermsCoreAsync(db, node, ct);
        return used;
    }

    async Task<(List<GlossaryTerm> Used, IReadOnlyList<GlossaryTerm> All)> GetUsedTermsCoreAsync(
        ProseDbContext db, Node node, CancellationToken ct)
    {
        var allTerms = await ListAsync(node.UniverseId, ct);
        var prose = await GetBookProseAsync(db, node.Id, ct);

        // Base set: terms that literally appear in the book's own live prose.
        var usedIds = new HashSet<Guid>(allTerms.Where(t => AppearsInText(t.Term, prose)).Select(t => t.Id));

        // Recursive cross-reference expansion: if a term already in the set explains itself
        // by name-dropping ANOTHER glossary term inside its own Definition/FullForm (e.g. SR's
        // definition mentions DataEast), that referenced term belongs in the book's glossary
        // too -- a reader shouldn't hit a second unexplained cross-reference inside the very
        // explanation meant to remove that friction. Repeats until no new terms are pulled in;
        // guaranteed to terminate because usedIds only grows and is capped by allTerms.Count.
        bool grew = true;
        while (grew)
        {
            grew = false;
            var usedTerms = allTerms.Where(t => usedIds.Contains(t.Id)).ToList();
            foreach (var candidate in allTerms)
            {
                if (usedIds.Contains(candidate.Id)) continue;
                var referenced = usedTerms.Any(t =>
                    AppearsInText(candidate.Term, t.Definition) ||
                    (!string.IsNullOrWhiteSpace(t.FullForm) && AppearsInText(candidate.Term, t.FullForm!)));
                if (referenced)
                {
                    usedIds.Add(candidate.Id);
                    grew = true;
                }
            }
        }

        var used = allTerms.Where(t => usedIds.Contains(t.Id)).OrderBy(t => t.Term).ToList();
        return (used, allTerms);
    }

    public async Task<BookGlossaryGenerateResult> GenerateForBookAsync(Guid nodeId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var node = await db.Nodes.IgnoreQueryFilters().FirstOrDefaultAsync(n => n.Id == nodeId, ct)
            ?? throw new InvalidOperationException($"Node {nodeId} not found.");

        var (used, allTerms) = await GetUsedTermsCoreAsync(db, node, ct);

        var nodeCode = (node.NodeCode ?? node.Slug).ToUpperInvariant();
        var dir = Path.Combine(paths.DataRoot, "docs", "nodes");
        var (html, json, txt) = RenderAll($"{node.Title} — Glossary", used);

        var htmlPath = Path.Combine(dir, $"{nodeCode}-Glossary.htm");
        var jsonPath = Path.Combine(dir, $"{nodeCode}-Glossary.json");
        var txtPath = Path.Combine(dir, $"{nodeCode}-Glossary.txt");
        await GeneratedFileWriter.WriteReadOnlyAsync(htmlPath, html, ct);
        await GeneratedFileWriter.WriteReadOnlyAsync(jsonPath, json, ct);
        await GeneratedFileWriter.WriteReadOnlyAsync(txtPath, txt, ct);

        log.LogInformation("[glossary] {NodeCode} book glossary — {Count}/{Total} universe terms used -> {Dir}",
            nodeCode, used.Count, allTerms.Count, dir);
        return new BookGlossaryGenerateResult(nodeCode, used.Count, allTerms.Count, htmlPath, jsonPath, txtPath);
    }

    // ── Detection ──────────────────────────────────────────────────────────

    /// <summary>Plural-insensitive: a headword and its regular "-s" plural are one entry
    /// regardless of which form was authored (e.g. "neuretic" and "neuretics" both match a
    /// single "neuretics" row, or a single "neuretic" row) — strip a trailing "s" from the
    /// headword down to its stem, then allow an optional trailing "s" back on the match.
    /// Also strips a trailing ", The" (back-of-book alphabetization convention, e.g.
    /// "Liturgy, The") before stemming, since prose never contains that literal inverted
    /// form — the bare headword ("Liturgy") still matches inside "the Liturgy" text.</summary>
    static bool AppearsInText(string term, string text)
    {
        var head = term.EndsWith(", The", StringComparison.OrdinalIgnoreCase) ? term[..^5].TrimEnd() : term;
        var stem = head.Length > 2 && head.EndsWith("s", StringComparison.OrdinalIgnoreCase) ? head[..^1] : head;
        return Regex.IsMatch(text, $@"(?<![A-Za-z0-9]){Regex.Escape(stem)}s?(?![A-Za-z0-9])", RegexOptions.IgnoreCase);
    }

    static async Task<string> GetBookProseAsync(ProseDbContext db, Guid nodeId, CancellationToken ct)
    {
        // Recurses past any nested Collection (2026-08-09 fix) — the old Include-based
        // direct-children query missed a split chapter's grandchildren (their BeatNodes
        // navigation is empty; the beats moved to the new sub-chapters during the split).
        // Term detection is order-independent (just a presence scan), so no ordering concern.
        var leafIds = await NodeWorkbenchService.GetLeafDescendantIdsAsync(db, nodeId, ct);
        var beatNodes = await db.BeatNodes.AsNoTracking().IgnoreQueryFilters()
            .Where(bn => leafIds.Contains(bn.NodeId))
            .Include(bn => bn.Beat)
            .ToListAsync(ct);

        // Strip inline entity-GUID tags (corpus-trust-recovery Phase 1a) — mandatory, not
        // optional polish: AppearsInText's regex could otherwise match inside a tag's own
        // guid="..." attribute text. This does not by itself close the original "Silence"
        // ambiguity (a glossary term colliding with an ordinary word) — GlossaryTerm has no FK to
        // Entities.Id today, so keying term-inclusion off a specific entity's tag presence would
        // need a separate, larger schema change; logged, not built here.
        return string.Join("\n\n", beatNodes
            .Where(bn => true)
            .Select(bn => BeatMarkup.StripEntityTags(bn.Beat!.Text))
            .Where(t => !string.IsNullOrWhiteSpace(t)));
    }

    // ── Rendering ──────────────────────────────────────────────────────────

    static (string Html, string Json, string Txt) RenderAll(string title, IReadOnlyList<GlossaryTerm> terms)
    {
        var json = JsonSerializer.Serialize(new
        {
            title,
            generatedAt = DateTime.UtcNow,
            termCount = terms.Count,
            terms = terms.Select(t => new { term = t.Term, fullForm = t.FullForm, definition = t.Definition, category = t.Category }),
        }, new JsonSerializerOptions { WriteIndented = true });

        return (RenderHtml(title, terms), json, RenderTxt(title, terms));
    }

    static string RenderTxt(string title, IReadOnlyList<GlossaryTerm> terms)
    {
        var sb = new StringBuilder();
        sb.AppendLine(title.ToUpperInvariant());
        sb.AppendLine(new string('=', title.Length));
        // Flat alphabetical list — no category grouping (author decision 2026-08-05).
        foreach (var t in terms)
        {
            var head = t.FullForm is { Length: > 0 } ff ? $"{t.Term} ({ff})" : t.Term;
            sb.AppendLine();
            sb.AppendLine(head);
            sb.AppendLine(t.Definition);
        }
        return sb.ToString();
    }

    static string RenderHtml(string title, IReadOnlyList<GlossaryTerm> terms)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html><head><meta charset=\"utf-8\">");
        sb.AppendLine($"<title>{Escape(title)}</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("body{font-family:Georgia,'Times New Roman',serif;max-width:720px;margin:40px auto;padding:0 20px;line-height:1.6;color:#1B202E;background:#EDEEF2;}");
        sb.AppendLine("h1{font-size:26px;}");
        sb.AppendLine("dt{font-weight:bold;margin-top:14px;font-size:15px;} dt .full{font-weight:normal;color:#5C6275;font-style:italic;} dd{margin:4px 0 0;color:#1B202E;}");
        sb.AppendLine(".meta{color:#8A90A0;font-size:13px;}");
        sb.AppendLine("</style></head><body>");
        sb.AppendLine($"<h1>{Escape(title)}</h1><p class=\"meta\">{terms.Count} terms.</p><dl>");
        // Flat alphabetical list — no category grouping (author decision 2026-08-05).
        foreach (var t in terms)
        {
            var full = t.FullForm is { Length: > 0 } ff ? $" <span class=\"full\">&mdash; {Escape(ff)}</span>" : "";
            sb.AppendLine($"<dt>{Escape(t.Term)}{full}</dt><dd>{Escape(t.Definition)}</dd>");
        }
        sb.AppendLine("</dl>");
        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    static string Escape(string s) => System.Net.WebUtility.HtmlEncode(s);
}
