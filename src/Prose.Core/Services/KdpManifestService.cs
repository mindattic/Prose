using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Kdp;

namespace Prose.Core.Services;

/// <summary>
/// Builds the ground-truth list of what needs to go up (or come down) on KDP, by reconciling
/// three sources that must never be hand-copied into each other:
///   1. DB (<see cref="Prose.Core.Data.Entities.Node"/>) — Title, Description, Kind,
///      Author, Version, PublicationStatus, KdpPublishedAt, PublishUrl, keywords.
///   2. Disk (the universe's export folder, <see cref="ExportPathResolver"/> convention) —
///      the actual .docx/.epub files <c>--export-node</c> produced, plus description.txt /
///      keywords.txt mirrors.
///   3. The KDP store (<see cref="KdpStore"/>, machine-local SQLite) — the NodeCode → KDP
///      dashboard "titleId" crosswalk (harvested from the bookshelf's "Edit eBook content"
///      link; lets a link jump straight to a book's edit page instead of hunting through the
///      bookshelf UI), and each book's sign-off gate and last confirmed publish (what the
///      per-book <c>.publish</c> marker files used to hold). tools/kdp/title-ids.json and the
///      markers are import/export only now — see <see cref="KdpJsonTransfer"/>.
///
/// Shared by <c>prose --kdp-manifest</c> (CLI: <c>KdpManifestCli</c>, a thin wrapper that prints a
/// table and writes manifest.json + the regenerated browser userscript) and the KdpPublish WPF
/// app (which consumes the entries in-process — no subprocess, no JSON round-trip). Business
/// logic lives here exactly once; both front ends must produce identical output.
/// </summary>
public class KdpManifestService
{
    /// <summary>Shared with <see cref="KdpMarkPublishedService"/> so both sides parse "Title
    /// V{N}.ext" filenames identically — never duplicate this pattern.</summary>
    internal static readonly Regex VersionFileRx = new(@"^(?<code>.+) V(?<ver>\d+)\.(?<ext>docx|epub)$", RegexOptions.IgnoreCase);

    /// <summary>How long a <see cref="PublishMarker.PublishingDetectedAtUtc"/> timestamp is
    /// trusted before falling back to the normal stale/Outdated computation. KDP's own
    /// "Live - Updates publishing" window is documented at up to ~72 hours; this adds margin
    /// so a slow republish doesn't flap back to "Outdated" while Amazon is still processing it.</summary>
    public static readonly TimeSpan PublishingDetectedWindow = TimeSpan.FromHours(96);

    private readonly IDbContextFactory<ProseDbContext> dbFactory;
    private readonly SettingsService settings;
    private readonly SettingsKvStore kv;
    private readonly KdpStore kdpStore;

    public KdpManifestService(IDbContextFactory<ProseDbContext> dbFactory, SettingsService settings, SettingsKvStore kv, KdpStore kdpStore)
    {
        this.dbFactory = dbFactory;
        this.settings = settings;
        this.kv = kv;
        this.kdpStore = kdpStore;
    }

    /// <summary>Locates the repo root (walks up from <paramref name="startDir"/> — typically
    /// <c>AppContext.BaseDirectory</c> — looking for <c>.git</c>). Shared by every KDP front end
    /// so <c>tools/kdp/</c> paths (manifest.json, the userscript, the first-run import's legacy
    /// JSON, staging, KdpFilePicker.exe) resolve the same way everywhere. A deployed exe
    /// (C:\Apps\...) has no .git above it, so it then tries the current directory, then
    /// <c>PROSE_REPO_PATH</c> / the canonical checkout (the fallback <c>FactoryProbes</c> uses),
    /// and only then settles for the current directory as before.</summary>
    public static string FindRepoRoot(string? startDir = null)
    {
        static string? WalkUp(string start)
        {
            var dir = new DirectoryInfo(start);
            while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, ".git")))
                dir = dir.Parent;
            return dir?.FullName;
        }

        if (WalkUp(startDir ?? AppContext.BaseDirectory) is { } fromBase) return fromBase;
        if (startDir == null)
        {
            if (WalkUp(Directory.GetCurrentDirectory()) is { } fromCwd) return fromCwd;
            var canonical = Environment.GetEnvironmentVariable("PROSE_REPO_PATH") is { Length: > 0 } p ? p : @"D:\Projects\MindAttic\Prose";
            if (Directory.Exists(Path.Combine(canonical, ".git"))) return canonical;
        }
        return Directory.GetCurrentDirectory();
    }

    public async Task<List<KdpManifestEntry>> BuildAsync(string repoRoot, CancellationToken ct = default)
    {
        var titleIds = await kdpStore.GetTitlesAsync(ct);
        var kdpBooks = await kdpStore.GetBooksAsync(ct);

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var universeNames = await db.Set<Prose.Core.Data.Entities.Universe>()
            .AsNoTracking()
            .ToDictionaryAsync(u => u.Id, u => u.Slug, ct);

        // Tracked = either the (currently unused-in-practice) PublicationStatus field is set, or
        // — the real signal — PublishUrl is populated, meaning the book is demonstrably live on
        // Amazon. PublicationStatus was added as the intended tracker but was never backfilled;
        // every row has it NULL even for books that have been live for weeks, so scoping to it
        // alone (as prose --kdp-status does) silently returns zero rows. PublishUrl is ground truth.
        var nodes = await db.Nodes
            .AsNoTracking().IgnoreQueryFilters()
            .Where(n => n.PublicationStatus != null || n.PublishUrl != null)
            .OrderBy(n => n.NodeCode)
            .ToListAsync(ct);

        // Broaden beyond DB-tracked rows: include every book folder actually present on disk
        // under each universe's export directory, even if it has neither PublicationStatus nor
        // PublishUrl set (not yet published — e.g. PXL, LDGR, IxS). These fall through to
        // "WorkInProgress" (hard gate unmet) or "Unpublished" (gate met, never published) via the
        // same per-node logic below (no PublishUrl means the "live but no baseline" branch never
        // fires), so a checklist consumer (KdpPublish) can show every known book, not just the
        // ones already live.
        var existingCodes = nodes.Select(n => n.NodeCode).Where(c => c != null).ToHashSet();
        var existingIds = nodes.Select(n => n.Id).ToHashSet();
        var discoveredCodes = new HashSet<string>();
        foreach (var slug in universeNames.Values.Distinct())
        {
            var baseDir = settings.GetExportDirectory(slug);
            if (!Directory.Exists(baseDir)) continue;
            foreach (var folder in Directory.GetDirectories(baseDir))
            {
                var folderCode = Path.GetFileName(folder);
                if (string.IsNullOrWhiteSpace(folderCode) || existingCodes.Contains(folderCode)) continue;
                discoveredCodes.Add(folderCode);
            }
        }
        if (discoveredCodes.Count > 0)
        {
            var discoveredNodes = await db.Nodes
                .AsNoTracking().IgnoreQueryFilters()
                .Where(n => n.NodeCode != null && discoveredCodes.Contains(n.NodeCode))
                .ToListAsync(ct);
            foreach (var dn in discoveredNodes)
                if (existingIds.Add(dn.Id)) nodes.Add(dn);
        }
        nodes = nodes.OrderBy(n => n.NodeCode).ToList();

        var entries = new List<KdpManifestEntry>();

        foreach (var n in nodes)
        {
            var universeSlug = universeNames.TryGetValue(n.UniverseId, out var slug) ? slug : "glmz";
            var code = n.NodeCode ?? n.Slug;

            bool hasPublishUrl = !string.IsNullOrWhiteSpace(n.PublishUrl);
            string? baselineWarning = null;
            if (hasPublishUrl && n.KdpPublishedAt == null)
            {
                // Live on Amazon (PublishUrl set) but we never recorded when — can't tell if the
                // current disk version has already gone up or not. Conservative: flag for a check
                // rather than silently assuming it's current.
                baselineWarning = "KdpPublishedAt never recorded for this book — treating as needing a check; run --kdp-mark-published once you confirm what's live.";
            }
            // stale/effectiveStatus are finalized further down, once hasNewerVersionThanPublished
            // is known — that (not a raw beat-edit timestamp) is the accurate "does this book
            // actually need a republish" signal. A beat can be edited today with no fresh
            // .epub ever exported, in which case there's genuinely nothing new to upload and the
            // sidebar must say Published, not Outdated.
            bool stale = false;
            string effectiveStatus = "-";

            var baseDir = settings.GetExportDirectory(universeSlug);
            string nodeDir; string fileBaseName;
            try
            {
                (nodeDir, fileBaseName) = await ExportPathResolver.ResolveAsync(db, n, baseDir, ct);
            }
            catch
            {
                nodeDir = Path.Combine(baseDir, code);
                fileBaseName = code;
            }

            // Always take the HIGHEST version actually present on disk, not just whatever DB's
            // Version column says — a stray newer file (interrupted export, manual copy, a tool
            // bumping the file without updating DB) must never be silently skipped in favor of an
            // older one. DB's Version is cross-checked only to produce a drift warning.
            string? docxPath = null;
            string? epubPath = null;
            var version = n.Version;
            string? warning = baselineWarning;

            if (Directory.Exists(nodeDir))
            {
                var best = Directory.GetFiles(nodeDir)
                    .Select(f => VersionFileRx.Match(Path.GetFileName(f)))
                    .Where(m => m.Success)
                    .Select(m => int.Parse(m.Groups["ver"].Value))
                    .DefaultIfEmpty(-1)
                    .Max();
                if (best >= 0)
                {
                    version = best;
                    docxPath = File.Exists(Path.Combine(nodeDir, $"{fileBaseName} V{best}.docx")) ? Path.Combine(nodeDir, $"{fileBaseName} V{best}.docx") : null;
                    epubPath = File.Exists(Path.Combine(nodeDir, $"{fileBaseName} V{best}.epub")) ? Path.Combine(nodeDir, $"{fileBaseName} V{best}.epub") : null;
                    if (best != n.Version)
                        warning = (warning == null ? "" : warning + " ") + $"DB Version={n.Version} but highest file on disk is V{best} — using the disk version.";
                }
            }
            if (docxPath == null) warning = (warning == null ? "" : warning + " ") + "No .docx found on disk — run prose --export-node.";
            if (epubPath == null) warning = (warning == null ? "" : warning + " ") + "No .epub found on disk — run prose --export-node.";

            // Stage the current manuscript at a short, constant path. No script or browser agent
            // can drive a native OS file-picker dialog to an arbitrary path (that boundary is a
            // real browser/OS sandbox, not something a smarter prompt works around) — the only
            // thing that can shrink is the human's part of that click, from "type a long R:\...
            // path" to "double-click <CODE>.epub in the same folder every time". (KdpPublish uses
            // DOM.setFileInputFiles instead and doesn't need this staging step at all, but keeping
            // it here costs nothing and the browser-extension pipeline still depends on it.)
            // .epub, not .docx: that's the format KDP's manuscript upload actually wants — it
            // accepts .docx too (auto-converting it), but .epub is the more faithful upload.
            string? stagedPath = null;
            string? filePickerCommand = null;
            var manuscriptSourcePath = epubPath ?? docxPath;
            if (manuscriptSourcePath != null)
            {
                try
                {
                    var stageDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "KDP-Upload");
                    Directory.CreateDirectory(stageDir);
                    var stagedExt = Path.GetExtension(manuscriptSourcePath);
                    stagedPath = Path.Combine(stageDir, $"{code}{stagedExt}");
                    File.Copy(manuscriptSourcePath, stagedPath, overwrite: true);

                    var pickerExe = Path.Combine(repoRoot, "tools", "kdp", "KdpFilePicker", "bin", "Debug", "net10.0-windows", "KdpFilePicker.exe");
                    if (File.Exists(pickerExe))
                        filePickerCommand = $"\"{pickerExe}\" \"{stagedPath}\"";
                }
                catch (Exception ex)
                {
                    stagedPath = null;
                    warning = (warning == null ? "" : warning + " ") + $"Staging copy failed: {ex.Message}";
                }
            }

            var description = ReadIfExists(Path.Combine(nodeDir, "description.txt")) ?? n.Description ?? "";
            var keywordsTxt = ReadIfExists(Path.Combine(nodeDir, "keywords.txt"));
            var keywords = !string.IsNullOrWhiteSpace(keywordsTxt)
                ? keywordsTxt.Split('\n').Select(k => k.Trim()).Where(k => k.Length > 0).ToList()
                : new List<string>();

            // Asin/KdpTitleId are DB columns now (canon), not recomputed each time — but fall
            // back to the legacy derivations (regex off PublishUrl, the KDP store's title-id
            // crosswalk) for any book published before these columns existed and not yet backfilled.
            var asin = n.Asin;
            if (string.IsNullOrWhiteSpace(asin) && !string.IsNullOrWhiteSpace(n.PublishUrl))
            {
                var m = Regex.Match(n.PublishUrl, @"/dp/([A-Z0-9]{10})");
                if (m.Success) asin = m.Groups[1].Value;
            }

            titleIds.TryGetValue(code, out var titleIdInfo);
            kdpBooks.TryGetValue(code, out var kdpBook);
            var titleId = n.KdpTitleId ?? titleIdInfo?.TitleId;
            var directEditUrl = titleId is string tid && tid.Length > 0
                ? $"https://kdp.amazon.com/en_US/title-setup/kindle/{tid}/content"
                : null;

            // First-time-publish metadata (price/categories/DRM/KDP Select/AI disclosure) — a
            // book only needs this once, hand-authored via kv.Set("kdp.newbook.<CODE>", ...)
            // before its first publish run. Irrelevant (and left null) for every already-live
            // book, which republishes off the manuscript/subtitle alone.
            var newListingPlan = kv.Get<KdpNewListingPlan>($"kdp.newbook.{code}");

            // Human-controlled publish gate: the book's sign-off in the KDP store (it used to be
            // the mere presence of a .publish marker file in the export folder). A book nobody
            // signed off (`prose --kdp-signoff --code <CODE>`, or KdpPublish's Sign Off button)
            // is never touched, so a full automated sweep (publish-new-and-republish-newer) can
            // run unattended. Authoritative, not just a UI hint — RunSelectedAsync refuses to
            // process a selected code lacking it even if it was manually checked.
            //
            // The store also keeps what was last actually confirmed published — {file, version,
            // asin, publishedAt} — recorded only after a real publish-confirmation modal, never
            // speculatively (see mark_published's own rule). If that filename matches the current
            // highest-version file on disk, this book needs no republish work at all —
            // UpToDateViaLocalMarker lets the caller skip launching the browser entirely instead
            // of opening the book just to discover the same thing three steps in.
            var readyToPublish = kdpBook?.SignOff.Ready == true;
            var publishMarker = KdpJsonTransfer.ToPublishMarker(kdpBook);
            // Transition hint only (the file is not read): a marker dropped into a folder after
            // the one-time import no longer signs a book off, so say so instead of silently
            // treating the book as work in progress.
            if (!readyToPublish && File.Exists(Path.Combine(nodeDir, KdpJsonTransfer.MarkerFileName)))
                warning = (warning == null ? "" : warning + " ") +
                          $"A legacy .publish file is in {nodeDir}, but the KDP store has no sign-off for {code} — the file no longer counts. Sign it off with `prose --kdp-signoff --code {code}`.";
            var currentManuscriptFilename = epubPath != null ? Path.GetFileName(epubPath)
                : docxPath != null ? Path.GetFileName(docxPath) : null;
            var upToDateViaLocalMarker = readyToPublish
                && currentManuscriptFilename != null
                && publishMarker?.File != null
                && string.Equals(publishMarker.File, currentManuscriptFilename, StringComparison.OrdinalIgnoreCase);

            // Hard, code-level publish gate (not prompt guidance — see
            // feedback_kdp_publish_gate_hardcoded): a book is only eligible to publish or
            // republish if ALL of these hold. cover.jpg/description.txt must exist ON DISK
            // (falling back to the DB Description column, as `description` above does, is fine
            // for display but not for gating — a book with no description.txt genuinely never
            // finished its export). The version check compares against the marker's OWN last-
            // published version (falling back to parsing it out of the marker's recorded
            // filename for markers written before the Version field existed), defaulting to 0
            // for a book that has never published — so a first-time publish with any real
            // version on disk still passes.
            var hasCover = File.Exists(Path.Combine(nodeDir, "cover.jpg"));
            var hasDescriptionFile = File.Exists(Path.Combine(nodeDir, "description.txt"));

            // Has the CURRENT on-disk manuscript already been confirmed published? Prefer the
            // marker's numeric Version (exact, unambiguous); fall back to a plain filename
            // string comparison for legacy markers written before Version existed. Critically:
            // a marker is only ever trusted as proof of a real publish if it also carries a
            // PublishedAtUtc timestamp — mark_published only ever writes File/Version/Asin
            // together WITH PublishedAtUtc after a genuine confirmed publish (never
            // speculatively), so requiring PublishedAtUtc here is a defensive second check, not
            // a redundant one: it means a matching filename can never be mistaken for "already
            // published" if that version's publish was never actually confirmed (e.g. a
            // first-time listing left sitting in Draft, or a hand-edited/malformed marker) —
            // there is simply nothing to compare against, so it falls through to "needs work".
            var hasConfirmedPublish = publishMarker?.PublishedAtUtc != null;
            bool alreadyPublishedThisVersion;
            if (!hasConfirmedPublish)
                alreadyPublishedThisVersion = false;
            else if (publishMarker?.Version is int publishedVersion)
                alreadyPublishedThisVersion = version <= publishedVersion;
            else
                alreadyPublishedThisVersion = currentManuscriptFilename != null
                    && string.Equals(publishMarker?.File, currentManuscriptFilename, StringComparison.OrdinalIgnoreCase);
            var hasNewerVersionThanPublished = epubPath != null && !alreadyPublishedThisVersion;
            var meetsHardPublishGate = readyToPublish && hasCover && hasDescriptionFile && hasNewerVersionThanPublished;

            // A book missing any of the four hard-gate ingredients (.publish marker, cover.jpg,
            // description.txt, an actual .epub on disk) cannot be (re)published no matter what
            // its prior PublishUrl/KdpPublishedAt history says — it must show as WorkInProgress,
            // not a stale "Published"/"Outdated" that implies it's one click away from going
            // live. Same four ingredients KdpOperatorService's runtime gate enforces authoritatively
            // (see MeetsHardPublishGate) — never re-derive this rule differently in the two places.
            var isIncomplete = !readyToPublish || !hasCover || !hasDescriptionFile || epubPath == null;

            // Finalize status/NeedsRepublish now that the accurate signal is known. A book that's
            // live but has no confirmed-publish record at all (see hasConfirmedPublish above)
            // can't be judged either way — keep the original "needs a check" baseline warning
            // rather than asserting Outdated or Published on no real evidence.
            if (!(hasPublishUrl && n.KdpPublishedAt == null))
            {
                stale = !isIncomplete && hasPublishUrl && hasNewerVersionThanPublished;
                // "Unpublished" (distinct from "WorkInProgress"): the hard gate is fully met —
                // .publish marker, cover, description, a genuinely newer .epub — but the book has
                // never gone live (no PublishUrl). This used to fall back to the DB's
                // PublicationStatus column, defaulting to "WorkInProgress" when that column was
                // null (true for most rows — it was never backfilled, see remarks above) — which
                // wrongly implied the book still had missing prerequisites when it was actually
                // ready and simply hadn't been run yet. Only used when PublicationStatus isn't
                // already set to something meaningful (e.g. a legacy "Published" row whose
                // PublishUrl was never captured — see JOAN/NEPH/PXL/QRT/TWD/TWU/VIGL).
                effectiveStatus = isIncomplete
                    ? "WorkInProgress"
                    : stale ? "Outdated" : (hasPublishUrl ? "Published" : (n.PublicationStatus ?? "Unpublished"));

                // KDP can be mid-publish on its own side (the "Live - Updates publishing" window,
                // up to ~72 hours after a recent republish) even though our local marker still
                // names the previous version as last-confirmed — that's a real, temporary,
                // non-actionable state on Amazon's end, not a republish waiting on us.
                // mark_publishing_detected (called by find_and_open_book's likelyPublishing
                // signal) records when this was last observed; while that timestamp is recent,
                // report "Publishing" instead of "Outdated" and don't flag it for a redundant run.
                if (stale && publishMarker?.PublishingDetectedAtUtc is string detectedRaw
                    && DateTime.TryParse(detectedRaw, null, System.Globalization.DateTimeStyles.RoundtripKind, out var detectedAt)
                    && DateTime.UtcNow - detectedAt < PublishingDetectedWindow)
                {
                    effectiveStatus = "Publishing";
                    stale = false;
                }
            }

            entries.Add(new KdpManifestEntry(
                Code: code,
                Slug: n.Slug,
                Title: n.Title,
                Subtitle: n.Subtitle,
                Author: string.IsNullOrWhiteSpace(n.Author) ? "MindAttic" : n.Author,
                Kind: n.Kind,
                Universe: universeSlug ?? "glmz",
                Version: version,
                DocxPath: docxPath,
                EpubPath: epubPath,
                StagedPath: stagedPath,
                FilePickerCommand: filePickerCommand,
                FolderPath: nodeDir,
                Description: description.Trim(),
                Keywords: keywords,
                PublicationStatus: effectiveStatus,
                NeedsRepublish: stale,
                KdpPublishedAt: n.KdpPublishedAt,
                PublishUrl: n.PublishUrl,
                Asin: asin,
                KdpTitleId: titleId,
                KdpDirectEditUrl: directEditUrl,
                Warning: warning,
                NewListingPlan: newListingPlan,
                ReadyToPublish: readyToPublish,
                LocalPublishMarker: publishMarker,
                UpToDateViaLocalMarker: upToDateViaLocalMarker,
                HasCover: hasCover,
                HasDescriptionFile: hasDescriptionFile,
                HasNewerVersionThanPublished: hasNewerVersionThanPublished,
                MeetsHardPublishGate: meetsHardPublishGate
            ));
        }

        return entries;
    }

    private static string? ReadIfExists(string path) => File.Exists(path) ? File.ReadAllText(path) : null;

}

public record KdpManifestEntry(
    string Code,
    string Slug,
    string Title,
    string? Subtitle,
    string Author,
    string Kind,
    string Universe,
    int Version,
    string? DocxPath,
    string? EpubPath,
    string? StagedPath,
    string? FilePickerCommand,
    string FolderPath,
    string Description,
    List<string> Keywords,
    string PublicationStatus,
    bool NeedsRepublish,
    DateTime? KdpPublishedAt,
    string? PublishUrl,
    string? Asin,
    string? KdpTitleId,
    string? KdpDirectEditUrl,
    string? Warning,
    KdpNewListingPlan? NewListingPlan,
    bool ReadyToPublish,
    PublishMarker? LocalPublishMarker,
    bool UpToDateViaLocalMarker,
    bool HasCover,
    bool HasDescriptionFile,
    bool HasNewerVersionThanPublished,
    /// <summary>The single hard, code-level publish gate: .publish marker + cover.jpg +
    /// description.txt all present on disk, AND the current .epub version is strictly higher
    /// than whatever the marker recorded as last published. Callers (KdpOperatorService,
    /// KdpPublish's RunSelectedAsync) must check this — never re-derive the rule inline.</summary>
    bool MeetsHardPublishGate
);

/// <summary>
/// A book's last confirmed publish, in the shape of the legacy <c>.publish</c> marker body —
/// e.g. <c>{"File":"Story V1.epub","ASIN":"ABC123","PublishedAtUtc":"2026-08-02T23:03:00Z"}</c>.
/// The data now lives in the KDP store (<see cref="KdpBook.LastPublish"/>, recorded only after a
/// genuine publish-confirmation modal, never speculatively); this record is how the manifest
/// (manifest.json's <c>localPublishMarker</c>) and the marker import/export
/// (<see cref="KdpJsonTransfer"/>) still present it, so neither shape changed. Deliberately a
/// loose bag of nullable fields so old marker files keep importing.
/// </summary>
public record PublishMarker(
    string? File,
    string? Asin,
    string? PublishedAtUtc,
    int? Version = null,
    /// <summary>Set by <c>mark_publishing_detected</c> when <c>find_and_open_book</c> observes
    /// KDP hiding the edit-content link (the "Live - Updates publishing" window that can last up
    /// to ~72 hours after a recent republish). <see cref="KdpManifestService"/> reports
    /// "Publishing" instead of "Outdated" while this timestamp is recent — see
    /// <see cref="KdpManifestService.PublishingDetectedWindow"/>. Overwritten with null the next
    /// time <c>mark_published</c> refreshes the marker after a genuine confirmed publish.</summary>
    string? PublishingDetectedAtUtc = null
);

/// <summary>
/// Hand-authored, one-time metadata for a book's FIRST publish on KDP — everything the republish
/// flow never touches because it's fixed at creation (price, categories, DRM, KDP Select
/// enrollment, the AI-generated-content disclosure). Stored via <c>kv.Set("kdp.newbook.&lt;CODE&gt;",
/// ...)</c> (see <see cref="SettingsKvStore"/>) before running a book through the new-listing
/// flow; irrelevant once the book is live (KdpManifestEntry.Asin/KdpTitleId/PublishUrl take over).
/// </summary>
public record KdpNewListingPlan(
    decimal PriceUsd,
    List<List<string>> CategoryPaths,
    bool KdpSelect,
    bool Drm,
    // Exact KDP dropdown option text for the AI-generated-content questionnaire's three fields
    // (confirmed live stable ids generative-ai-questionnaire-text/-images/-translations) — e.g.
    // "None" or "Entire work, with extensive editing" for text/translations, "One or a few
    // AI-generated images, with minimal or no editing" for images. AiTextTool/AiImagesTool fill
    // the "Which tool(s) did you use" field that appears when the option isn't "None".
    string AiTextOption,
    string? AiTextTool,
    string AiImagesOption,
    string? AiImagesTool,
    string AiTranslationsOption
);
