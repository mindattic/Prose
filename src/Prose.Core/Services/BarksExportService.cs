using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;

namespace Prose.Core.Services;

/// <summary>
/// Portable-writing-service plan, Phase 4 — RFC 0007 Phase 2's <c>--barks-export</c>, built
/// narrow: a pure filter over whatever dialog beats already exist (hand-authored, or produced via
/// <see cref="OneShotGenerationService"/> and saved into a book), not the full "book-as-deliverable"
/// (GDD/script-book) pattern, which stays design-only until a real consumer exists to shape it
/// against (see RFC 0007 §"Phase 2").
///
/// A beat's single speaker is its recorded POV entity (<see cref="VerificationContextService.
/// GetPovEntityIdAsync"/> — the same <c>BeatEntityPresence PresenceType='pov'</c> row DCM's
/// Register layer already pins dominant). A beat with no recorded POV is skipped and counted, not
/// silently dropped — the RFC 0007 postmortem specifically calls out silent filtering as a
/// recurring bug class worth avoiding.
/// </summary>
public class BarksExportService(
    IDbContextFactory<ProseDbContext> dbFactory,
    IUniverseContext universeContext,
    NodeWorkbenchService workbench,
    VerificationContextService verificationContext)
{
    public record Bark(string BarkId, string SpeakerEntitySlug, string Text, string Context);
    public record ExportResult(IReadOnlyList<Bark> Barks, int Skipped, string UniverseSlug);

    public async Task<ExportResult> ExportAsync(string universeSlug, string? nodeRef = null, CancellationToken ct = default)
    {
        var match = universeContext.ListUniverses()
            .FirstOrDefault(u => string.Equals(u.Slug, universeSlug, StringComparison.OrdinalIgnoreCase));
        if (match == null)
            throw new InvalidOperationException($"Unknown universe '{universeSlug}'.");

        universeContext.SetFlowUniverse(match.Id);
        try
        {
            List<(NodeWorkbenchService.OrderedBeat Ordered, string RootSlug)> targets = new();

            if (!string.IsNullOrWhiteSpace(nodeRef))
            {
                var resolvedId = await NodeRefResolver.ResolveAsync(dbFactory, nodeRef, ct);
                if (resolvedId == null)
                    throw new InvalidOperationException(NodeRefResolver.NotFoundMessage(nodeRef));

                await using var db = await dbFactory.CreateDbContextAsync(ct);
                var node = await db.Nodes.AsNoTracking().IgnoreQueryFilters()
                    .FirstOrDefaultAsync(n => n.Id == resolvedId.Value, ct);
                if (node == null)
                    throw new InvalidOperationException(NodeRefResolver.NotFoundMessage(nodeRef));

                var beats = await workbench.GetOrderedBeatsAsync(node.Id, ct);
                targets.AddRange(beats.Select(b => (b, node.Slug)));
            }
            else
            {
                await using var db = await dbFactory.CreateDbContextAsync(ct);
                var bookIds = await db.Nodes.AsNoTracking()
                    .Where(n => n.Kind == "book")
                    .OrderBy(n => n.SortKey)
                    .Select(n => new { n.Id, n.Slug })
                    .ToListAsync(ct);

                foreach (var book in bookIds)
                {
                    var beats = await workbench.GetOrderedBeatsAsync(book.Id, ct);
                    targets.AddRange(beats.Select(b => (b, book.Slug)));
                }
            }

            var barks = new List<Bark>();
            int skipped = 0;

            foreach (var (ordered, rootSlug) in targets)
            {
                var beat = ordered.Beat;
                if (string.IsNullOrWhiteSpace(beat.Text)) continue; // not authored yet — not a "skip", just nothing to export

                var povId = await verificationContext.GetPovEntityIdAsync(beat.Id, ct);
                if (povId == null) { skipped++; continue; }

                await using var db = await dbFactory.CreateDbContextAsync(ct);
                var speakerSlug = await db.Entities.AsNoTracking().IgnoreQueryFilters()
                    .Where(e => e.Id == povId.Value)
                    .Select(e => e.Slug)
                    .FirstOrDefaultAsync(ct);
                if (string.IsNullOrEmpty(speakerSlug)) { skipped++; continue; } // orphaned POV reference

                var barkId = $"{rootSlug}:{beat.Id:N}";
                var context = !string.IsNullOrWhiteSpace(beat.Description) ? beat.Description
                    : !string.IsNullOrWhiteSpace(beat.Title) ? beat.Title
                    : "";
                barks.Add(new Bark(barkId, speakerSlug, beat.Text.Trim(), context ?? ""));
            }

            return new ExportResult(barks, skipped, match.Slug);
        }
        finally
        {
            universeContext.SetFlowUniverse(null);
        }
    }
}
