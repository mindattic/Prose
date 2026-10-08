using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Prose.Core.Kdp;
using Prose.Core.Services.Operator.KdpTools;

namespace Prose.Core.Services.Operator;

/// <summary>
/// Full-roster sequential "ground truth" sweep — replaces the old subset-driven
/// KdpPublish RunSelectedCoreAsync. Visits every signed-off book in manifest order, reads what
/// KDP's own Content step actually shows (read-only — no clicks), and reconciles: confirms a book
/// that's already correct, republishes one that's stale, or logs and moves on if KDP is still in
/// its post-publish "Publishing" window.
///
/// A held book (not signed off) is left entirely alone here, even if it's currently live —
/// flagging a held-but-live book for a human to take down by hand is computed separately, straight
/// from the manifest (<see cref="KdpManifestEntry.ReadyToPublish"/>/<see cref="KdpManifestEntry.PublishUrl"/>),
/// since it needs no live KDP visit at all.
///
/// Deliberately does not drive any real unpublish/take-down click against KDP — that's out of
/// scope for now (see the project plan this was built from). The only new live-KDP interaction
/// this adds is a read.
/// </summary>
public sealed class KdpReconcileService
{
    private readonly KdpOperatorService operatorService;
    private readonly KdpStore kdpStore;
    private readonly KdpMarkPublishedService markPublishedService;
    private readonly ILogger<KdpReconcileService> log;

    public KdpReconcileService(
        KdpOperatorService operatorService,
        KdpStore kdpStore,
        KdpMarkPublishedService markPublishedService,
        ILogger<KdpReconcileService> log)
    {
        this.operatorService = operatorService;
        this.kdpStore = kdpStore;
        this.markPublishedService = markPublishedService;
        this.log = log;
    }

    /// <summary>One book's result from a ground-truth visit this pass. Not persisted — the caller
    /// (KdpPublish's MainWindow) keeps these in memory per run, the same lifetime as its existing
    /// bookshelf-scan rows, and splices them onto the next manifest refresh for the panel.</summary>
    public sealed record GroundTruthResult(string? Filename, string? LastModifiedText, DateTimeOffset CheckedAt, bool MatchesExpected);

    public async IAsyncEnumerable<(string Code, OperatorEvent Event)> RunAsync(
        IReadOnlyList<KdpManifestEntry> manifest,
        KdpOperatorContext ctx,
        IDictionary<string, GroundTruthResult> groundTruthByCode,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var findAndOpen = new FindAndOpenBookTool();
        var markPublishing = new MarkPublishingDetectedTool(markPublishedService);

        foreach (var book in manifest)
        {
            if (ct.IsCancellationRequested) yield break;

            // KDP's own post-publish review window (up to ~72h; PublishingDetectedWindow adds
            // margin) — a real, temporary, non-actionable state. Don't re-visit or re-publish
            // while it's active; it'll clear on a later pass.
            if (book.PublicationStatus == "Publishing")
            {
                yield return (book.Code, new OperatorEvent.Info("still in KDP's post-publish window — skipping this pass."));
                continue;
            }

            // Held (never signed off, or explicitly Held): leave it alone. A held book that's
            // STILL live is flagged in the panel straight from PublishUrl/ReadyToPublish — no
            // live visit needed to know that, so there's nothing for this pass to do here.
            if (!book.ReadyToPublish) continue;

            if (string.IsNullOrWhiteSpace(book.PublishUrl))
            {
                // Never been live — nothing to confirm against; go straight to the existing
                // publish flow, which re-checks cover/description/version itself.
                yield return (book.Code, new OperatorEvent.Info("not yet live — publishing."));
                await foreach (var evt in StreamBookAsync(book, ctx, ct))
                    yield return (book.Code, evt);
                continue;
            }

            var (findResultJson, findError) = await TryFindAndOpenAsync(findAndOpen, book, ctx, ct);
            if (findError != null)
            {
                yield return (book.Code, new OperatorEvent.Error($"couldn't reach it on the bookshelf — {findError}"));
                continue;
            }

            bool found;
            bool likelyPublishing;
            using (var findDoc = JsonDocument.Parse(findResultJson!))
            {
                found = findDoc.RootElement.TryGetProperty("found", out var foundEl) && foundEl.GetBoolean();
                likelyPublishing = findDoc.RootElement.TryGetProperty("likelyPublishing", out var lp) && lp.GetBoolean();
            }
            if (!found)
            {
                if (likelyPublishing)
                {
                    await TryMarkPublishingAsync(markPublishing, book, ctx, ct);
                    yield return (book.Code, new OperatorEvent.Info("KDP shows it mid-publish (no edit link yet) — skipping this pass."));
                }
                else
                {
                    yield return (book.Code, new OperatorEvent.Error("marked live locally but not found on the bookshelf — needs a look."));
                }
                continue;
            }

            var (ground, groundError) = await TryReadGroundTruthAsync(ctx, ct);
            if (groundError != null)
            {
                yield return (book.Code, new OperatorEvent.Error($"couldn't read the Content page — {groundError}"));
                continue;
            }

            var groundVersionMatch = ground!.Filename is { } f ? KdpManifestService.VersionFileRx.Match(f) : null;
            if (ground.Filename is null || groundVersionMatch is not { Success: true })
            {
                yield return (book.Code, new OperatorEvent.Error(
                    $"couldn't read a manuscript filename off the Content page — leaving it as-is rather than guessing. Excerpt: {ground.RawExcerpt}"));
                continue;
            }

            var liveVersion = int.Parse(groundVersionMatch.Groups["ver"].Value);
            var matchesExpected = liveVersion >= book.Version;
            groundTruthByCode[book.Code] = new GroundTruthResult(ground.Filename, ground.LastModifiedText, DateTimeOffset.UtcNow, matchesExpected);

            if (!matchesExpected)
            {
                yield return (book.Code, new OperatorEvent.Info($"KDP still shows \"{ground.Filename}\" — publishing v{book.Version}."));
                await foreach (var evt in StreamBookAsync(book, ctx, ct))
                    yield return (book.Code, evt);
                continue;
            }

            yield return (book.Code, new OperatorEvent.Info($"confirmed — \"{ground.Filename}\" is live and matches v{book.Version}."));

            // Ground truth over guesses: if the local record didn't already know about this exact
            // filename (drift — e.g. a prior run's mark_published step got interrupted), fix it
            // now rather than leaving it stale for next time.
            var localFile = book.LocalPublishMarker?.File;
            if (!string.Equals(localFile, ground.Filename, StringComparison.OrdinalIgnoreCase))
            {
                var corrected = await TryRecordPublishAsync(book, ground.Filename, liveVersion, ct);
                if (corrected)
                    yield return (book.Code, new OperatorEvent.Info($"local record was out of sync — corrected it to \"{ground.Filename}\"."));
            }
        }
    }

    /// <summary>
    /// Streams one book's publish-flow events, catching a mid-stream failure so one bad book
    /// can't take down the rest of the sweep (the same resilience RunSelectedCoreAsync had) —
    /// written as manual MoveNextAsync stepping because C# forbids a yield return lexically
    /// inside a try block that has a catch clause.
    /// </summary>
    private async IAsyncEnumerable<OperatorEvent> StreamBookAsync(
        KdpManifestEntry book, KdpOperatorContext ctx, [EnumeratorCancellation] CancellationToken ct)
    {
        var enumerator = operatorService.ProcessBookAsync(book, ctx, ct).GetAsyncEnumerator(ct);
        try
        {
            while (true)
            {
                bool hasNext;
                string? stepError = null;
                try
                {
                    hasNext = await enumerator.MoveNextAsync();
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    hasNext = false;
                    stepError = ex.Message;
                }

                if (stepError != null)
                {
                    yield return new OperatorEvent.Error($"unexpected failure — {stepError}");
                    yield break;
                }
                if (!hasNext) yield break;
                yield return enumerator.Current;
            }
        }
        finally
        {
            await enumerator.DisposeAsync();
        }
    }

    private static async Task<(string? Json, string? Error)> TryFindAndOpenAsync(
        FindAndOpenBookTool tool, KdpManifestEntry book, KdpOperatorContext ctx, CancellationToken ct)
    {
        try
        {
            var argsJson = JsonSerializer.Serialize(new { title = book.Title, known_title_id = book.KdpTitleId, known_asin = book.Asin });
            using var argsDoc = JsonDocument.Parse(argsJson);
            var result = await tool.InvokeAsync(argsDoc.RootElement, ctx, ct);
            return (result, null);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return (null, ex.Message);
        }
    }

    private static async Task TryMarkPublishingAsync(
        MarkPublishingDetectedTool tool, KdpManifestEntry book, KdpOperatorContext ctx, CancellationToken ct)
    {
        try
        {
            var argsJson = JsonSerializer.Serialize(new { slug = book.Code });
            using var argsDoc = JsonDocument.Parse(argsJson);
            await tool.InvokeAsync(argsDoc.RootElement, ctx, ct);
        }
        catch { /* best-effort bookkeeping; the limbo log line fires regardless */ }
    }

    private static async Task<(KdpGroundTruthReader.Result? Result, string? Error)> TryReadGroundTruthAsync(
        KdpOperatorContext ctx, CancellationToken ct)
    {
        try
        {
            var result = await KdpGroundTruthReader.ReadAsync(ctx.Browser, ct);
            return (result, null);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return (null, ex.Message);
        }
    }

    private async Task<bool> TryRecordPublishAsync(KdpManifestEntry book, string filename, int version, CancellationToken ct)
    {
        try
        {
            await kdpStore.RecordPublishAsync(book.Code, new KdpPublishSnapshot
            {
                File = filename,
                Version = version,
                Asin = book.Asin,
                PublishedAt = DateTimeOffset.UtcNow,
            }, KdpPublishSource.Operator, ct);
            return true;
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Could not reconcile local publish record for {Code}", book.Code);
            return false;
        }
    }
}
