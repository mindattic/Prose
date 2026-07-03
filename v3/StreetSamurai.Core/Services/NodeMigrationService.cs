using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;

namespace StreetSamurai.Core.Services;

/// <summary>
/// One-shot, idempotent migration from the legacy
/// Books / Chapters / ChapterBeats / Episodes / EpisodeBeats five-table
/// world into the unified <see cref="Beat"/> + <see cref="Node"/> +
/// <see cref="NodeBeat"/> world.
///
/// Run via <c>ss --migrate-nodes</c> or implicitly at startup (the seed
/// service queues it). Safe to re-run: every insert is gated on a
/// <c>NOT EXISTS</c>-style check keyed on the source row's GUID, so a second
/// pass is a no-op (or picks up any new rows added since the last run).
/// </summary>
public class NodeMigrationService
{
    private readonly IDbContextFactory<StreetSamuraiDbContext> dbFactory;
    private readonly ILogger<NodeMigrationService> log;

    public NodeMigrationService(IDbContextFactory<StreetSamuraiDbContext> dbFactory, ILogger<NodeMigrationService> log)
    {
        this.dbFactory = dbFactory;
        this.log = log;
    }

    public record MigrationReport(int BooksAdded, int ChaptersAdded, int BeatsAdded, int EpisodesAdded, int StandaloneBeatsAdded, int JunctionRowsAdded);

    public async Task<MigrationReport> MigrateAllAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var booksAdded     = await MigrateBooksAsync(db, ct);
        var chaptersAdded  = await MigrateChaptersAsync(db, ct);
        var (beatsAdded, beatJunctions) = await MigrateChapterBeatsAsync(db, ct);
        var (episodesAdded, chapterIdToNode) = await MigrateEpisodesAsync(db, ct);
        var (standaloneBeats, episodeJunctions) = await MigrateEpisodeBeatsAsync(db, ct);

        var report = new MigrationReport(booksAdded, chaptersAdded, beatsAdded, episodesAdded, standaloneBeats, beatJunctions + episodeJunctions);
        log.LogInformation("Node migration complete: {Report}", report);
        return report;
    }

    // ── Phase 1: Books → Node(kind=book) ───────────────────────────────
    // Reuse Book.Id as the new Node.Id so every downstream BookId
    // reference resolves to a Node by GUID without remapping. Idempotent
    // on Node.Id — re-running skips books already migrated.
    private async Task<int> MigrateBooksAsync(StreetSamuraiDbContext db, CancellationToken ct)
    {
        var bookRows = await db.Database
            .SqlQueryRaw<BookRow>("SELECT Id, Title, Slug, Tagline FROM Books")
            .ToListAsync(ct);
        var existingNodeIds = (await db.Nodes.Select(s => s.Id).ToListAsync(ct)).ToHashSet();

        int added = 0;
        for (int i = 0; i < bookRows.Count; i++)
        {
            var b = bookRows[i];
            if (existingNodeIds.Contains(b.Id)) continue;
            db.Nodes.Add(new StoryNode
            {
                Id       = b.Id,
                Slug     = ResolveSlug(b.Slug, b.Title, b.Id),
                Title    = b.Title ?? "Untitled book",
                Synopsis = b.Tagline,
                Kind     = "story",
                Status   = "draft",
                SortKey  = (i + 1) * 100.0,
            });
            added++;
        }
        if (added > 0) await db.SaveChangesAsync(ct);
        return added;
    }

    // ── Phase 2: Chapters → Node(kind=chapter, parent=book node) ─────
    private async Task<int> MigrateChaptersAsync(StreetSamuraiDbContext db, CancellationToken ct)
    {
        var chapterRows = await db.Database
            .SqlQueryRaw<ChapterRow>("SELECT Id, BookId, Number, Title, Synopsis, Status FROM Chapters")
            .ToListAsync(ct);
        var existingNodeIds = (await db.Nodes.Select(s => s.Id).ToListAsync(ct)).ToHashSet();
        var slugIndex = (await db.Nodes.Select(s => s.Slug).ToListAsync(ct)).ToHashSet(StringComparer.OrdinalIgnoreCase);

        int added = 0;
        foreach (var c in chapterRows.OrderBy(c => c.BookId).ThenBy(c => c.Number))
        {
            if (existingNodeIds.Contains(c.Id)) continue;
            db.Nodes.Add(new ChapterNode
            {
                Id             = c.Id,
                Slug           = UniqueSlug(c.Title ?? "chapter", c.Id, slugIndex),
                Title          = c.Title ?? "Untitled chapter",
                Synopsis       = c.Synopsis,
                Kind           = "chapter",
                Status         = string.IsNullOrEmpty(c.Status) ? "draft" : c.Status,
                ParentNodeId = c.BookId,
                SortKey        = c.Number * 100.0,
            });
            added++;
        }
        if (added > 0) await db.SaveChangesAsync(ct);
        return added;
    }

    // ── Phase 3: ChapterBeats → Beat + NodeBeat ────────────────────────
    // The Step-2 schema converged ChapterBeats with audio fields, so each
    // row already carries everything a Beat needs. Beat.Id = ChapterBeat.BeatGuid
    // so SourceBeatGuid links from EpisodeBeats (next phase) still resolve.
    private async Task<(int beatsAdded, int junctionsAdded)> MigrateChapterBeatsAsync(StreetSamuraiDbContext db, CancellationToken ct)
    {
        var rows = await db.Database
            .SqlQueryRaw<ChapterBeatRow>(@"
                SELECT BeatGuid, ChapterId, [Index], Title, Synopsis, Text, Act, StructureRole, SceneType,
                       SortKey, EmotionalTone, PaceHint, AudioPath, DurationSec, NarratedAt, LastRequestId, WasCorrected
                FROM ChapterBeats")
            .ToListAsync(ct);
        var existingBeatIds = (await db.Beats.Select(b => b.Id).ToListAsync(ct)).ToHashSet();
        var existingJunctions = (await db.NodeBeats
            .Select(sb => new { sb.NodeId, sb.BeatId })
            .ToListAsync(ct))
            .Select(x => (x.NodeId, x.BeatId)).ToHashSet();

        // Beat.Number has a UNIQUE constraint; assign sequential values
        // starting from MAX+1 so re-runs of the migration don't collide with
        // beats added since the last pass. Production hits the same backfill
        // via add_beat_number_20260522.sql; this keeps the in-process tests
        // (sqlite — no migration scripts) green and the prod re-run safe too.
        int nextNumber = (await db.Beats.MaxAsync(b => (int?)b.Number, ct) ?? 0) + 1;

        int beatsAdded = 0, junctionsAdded = 0;
        foreach (var cb in rows)
        {
            if (!existingBeatIds.Contains(cb.BeatGuid))
            {
                db.Beats.Add(new Beat
                {
                    Id            = cb.BeatGuid,
                    Number        = nextNumber++,
                    Text          = cb.Text ?? "",
                    TextHash      = string.IsNullOrEmpty(cb.Text) ? null : ComputeTextHash(cb.Text),
                    BeatTitle     = cb.Title,
                    Synopsis      = cb.Synopsis,
                    StructureRole = cb.StructureRole,
                    Act           = cb.Act,
                    SceneType     = string.IsNullOrEmpty(cb.SceneType) ? "scene" : cb.SceneType,
                    EmotionalTone = cb.EmotionalTone,
                    PaceHint      = cb.PaceHint,
                    AudioPath     = cb.AudioPath,
                    DurationSec   = cb.DurationSec,
                    NarratedAt    = cb.NarratedAt,
                    LastRequestId = cb.LastRequestId,
                    WasCorrected  = cb.WasCorrected,
                });
                beatsAdded++;
            }
            if (!existingJunctions.Contains((cb.ChapterId, cb.BeatGuid)))
            {
                db.NodeBeats.Add(new NodeBeat { NodeId = cb.ChapterId, BeatId = cb.BeatGuid, SortKey = cb.SortKey });
                junctionsAdded++;
            }
        }
        if (beatsAdded + junctionsAdded > 0) await db.SaveChangesAsync(ct);
        return (beatsAdded, junctionsAdded);
    }

    // ── Phase 4: Episodes → Node(kind=episode) ─────────────────────────
    // Episodes that were chapter-recordings attach as children of the
    // corresponding chapter node. Standalone bedtime episodes have no
    // parent. Returns the chapter-id → node-id map for phase 5.
    private async Task<(int episodesAdded, Dictionary<string, Guid> chapterIdToNode)> MigrateEpisodesAsync(StreetSamuraiDbContext db, CancellationToken ct)
    {
        var rows = await db.Database
            .SqlQueryRaw<EpisodeRow>(@"
                SELECT Id, Slug, Seed, Title, VoiceId, StartedAt, GenerationCompletedAt, AudioCompletedAt, Status,
                       CharsNarrated, Error, ScriptMarkdownPath, ScriptPdfPath, CombinedAudioPath,
                       LastPlayedSec, ParentEpisodeId, BookId, ChapterId
                FROM Episodes")
            .ToListAsync(ct);
        var existingNodeIds = (await db.Nodes.Select(s => s.Id).ToListAsync(ct)).ToHashSet();
        var slugIndex = (await db.Nodes.Select(s => s.Slug).ToListAsync(ct)).ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Build the chapter-id → node-id lookup for episode parent wiring.
        // Legacy code sometimes wrote Chapter.Id as the no-dashes hex form,
        // sometimes with dashes — accept both.
        var chapterIds = await db.Nodes
            .Where(s => s.Kind == "chapter")
            .Select(s => s.Id)
            .ToListAsync(ct);
        var chapterIdToNode = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        foreach (var cid in chapterIds)
        {
            chapterIdToNode[cid.ToString()]    = cid;
            chapterIdToNode[cid.ToString("N")] = cid;
        }

        int added = 0;
        foreach (var e in rows)
        {
            if (existingNodeIds.Contains(e.Id)) continue;
            Guid? parentNode = null;
            if (!string.IsNullOrEmpty(e.ChapterId) && chapterIdToNode.TryGetValue(e.ChapterId, out var chNode))
                parentNode = chNode;
            db.Nodes.Add(new ChapterNode
            {
                Id                    = e.Id,
                Slug                  = string.IsNullOrEmpty(e.Slug)
                                            ? UniqueSlug(e.Title ?? "episode", e.Id, slugIndex)
                                            : EnsureUniqueSlug(e.Slug, e.Id, slugIndex),
                Title                 = e.Title ?? "Untitled episode",
                Synopsis              = null,
                Kind                  = "chapter",
                Status                = string.IsNullOrEmpty(e.Status) ? "draft" : e.Status,
                VoiceId               = e.VoiceId,
                ParentNodeId        = parentNode,
                SortKey               = 100.0,
                Seed                  = e.Seed,
                StartedAt             = e.StartedAt,
                GenerationCompletedAt = e.GenerationCompletedAt,
                AudioCompletedAt      = e.AudioCompletedAt,
                CharsNarrated         = e.CharsNarrated,
                CombinedAudioPath     = e.CombinedAudioPath,
                ScriptMarkdownPath    = e.ScriptMarkdownPath,
                ScriptPdfPath         = e.ScriptPdfPath,
                LastPlayedSec         = e.LastPlayedSec,
                Error                 = e.Error,
            });
            added++;
        }
        if (added > 0) await db.SaveChangesAsync(ct);
        return (added, chapterIdToNode);
    }

    // ── Phase 5: EpisodeBeats → existing Beat (chapter-rec) or new Beat (standalone) ─
    // For chapter-recording episodes, the EpisodeBeat is a thin audio
    // wrapper over a ChapterBeat that's already a Beat row — we reuse it
    // and copy audio fields forward when the EpisodeBeat is fresher.
    // Standalone bedtime-episode beats have no canonical source; we mint a
    // new Beat row keyed by a fresh Guid7.
    private async Task<(int standaloneAdded, int junctionsAdded)> MigrateEpisodeBeatsAsync(StreetSamuraiDbContext db, CancellationToken ct)
    {
        var rows = await db.Database
            .SqlQueryRaw<EpisodeBeatRow>(@"
                SELECT EpisodeId, [Index], Text, AudioPath, NarratedAt, DurationSec, WasCorrected, SortKey,
                       BeatTitle, Synopsis, StructureRole, Act, SceneType, EmotionalTone, PaceHint,
                       TextHash, SourceBeatGuid, Stale, LastRequestId
                FROM EpisodeBeats")
            .ToListAsync(ct);
        var existingBeatIds = (await db.Beats.Select(b => b.Id).ToListAsync(ct)).ToHashSet();
        var existingJunctions = (await db.NodeBeats
            .Select(sb => new { sb.NodeId, sb.BeatId })
            .ToListAsync(ct))
            .Select(x => (x.NodeId, x.BeatId)).ToHashSet();

        // Same Number allocation as ChapterBeats — picks up from the highest
        // value present after that pass so the unique index doesn't fire.
        int nextNumber = (await db.Beats.MaxAsync(b => (int?)b.Number, ct) ?? 0) + 1;

        int standaloneAdded = 0, junctionsAdded = 0;
        foreach (var eb in rows)
        {
            Guid beatId;
            if (!string.IsNullOrEmpty(eb.SourceBeatGuid)
                && Guid.TryParse(eb.SourceBeatGuid, out var srcGuid)
                && existingBeatIds.Contains(srcGuid))
            {
                beatId = srcGuid;
                var canon = await db.Beats.FirstAsync(b => b.Id == srcGuid, ct);
                if (string.IsNullOrEmpty(canon.AudioPath) && !string.IsNullOrEmpty(eb.AudioPath))
                {
                    canon.AudioPath     = eb.AudioPath;
                    canon.NarratedAt    = eb.NarratedAt;
                    canon.DurationSec   = eb.DurationSec;
                    canon.LastRequestId = eb.LastRequestId;
                }
                if (eb.Stale) canon.Stale = true;
                if (!string.IsNullOrEmpty(eb.TextHash) && string.IsNullOrEmpty(canon.TextHash))
                    canon.TextHash = eb.TextHash;
            }
            else
            {
                beatId = Guid.CreateVersion7();
                db.Beats.Add(new Beat
                {
                    Id            = beatId,
                    Number        = nextNumber++,
                    Text          = eb.Text ?? "",
                    TextHash      = eb.TextHash ?? (string.IsNullOrEmpty(eb.Text) ? null : ComputeTextHash(eb.Text)),
                    BeatTitle     = eb.BeatTitle,
                    Synopsis      = eb.Synopsis,
                    StructureRole = eb.StructureRole,
                    Act           = eb.Act,
                    SceneType     = string.IsNullOrEmpty(eb.SceneType) ? "scene" : eb.SceneType,
                    EmotionalTone = eb.EmotionalTone,
                    PaceHint      = eb.PaceHint,
                    AudioPath     = eb.AudioPath,
                    NarratedAt    = eb.NarratedAt,
                    DurationSec   = eb.DurationSec,
                    LastRequestId = eb.LastRequestId,
                    Stale         = eb.Stale,
                    WasCorrected  = eb.WasCorrected,
                });
                standaloneAdded++;
            }

            if (!existingJunctions.Contains((eb.EpisodeId, beatId)))
            {
                db.NodeBeats.Add(new NodeBeat { NodeId = eb.EpisodeId, BeatId = beatId, SortKey = eb.SortKey });
                junctionsAdded++;
            }
        }
        if (standaloneAdded + junctionsAdded > 0) await db.SaveChangesAsync(ct);
        return (standaloneAdded, junctionsAdded);
    }

    public static string ComputeTextHash(string text)
    {
        var normalized = (text ?? "").Trim();
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string Slugify(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "untitled";
        var lower = s.ToLowerInvariant();
        var ascii = Regex.Replace(lower, @"[^a-z0-9]+", "-").Trim('-');
        return string.IsNullOrWhiteSpace(ascii) ? "untitled" : ascii;
    }

    private static string UniqueSlug(string title, Guid id, HashSet<string> slugIndex)
    {
        var baseSlug = Slugify(title);
        var withGuid = $"{baseSlug}-{id.ToString("N")[..8]}";
        if (slugIndex.Add(withGuid)) return withGuid;
        // Extreme edge case: collision on the 8-char prefix. Fall back to full id.
        var full = $"{baseSlug}-{id:N}";
        slugIndex.Add(full);
        return full;
    }

    private static string EnsureUniqueSlug(string preferred, Guid id, HashSet<string> slugIndex)
    {
        if (slugIndex.Add(preferred)) return preferred;
        return UniqueSlug(preferred, id, slugIndex);
    }

    private static string ResolveSlug(string? existingSlug, string? title, Guid id)
        => !string.IsNullOrWhiteSpace(existingSlug)
            ? existingSlug
            : Slugify(title ?? "") + "-" + id.ToString("N")[..8];

    // ── DTOs for raw SQL projection ──────────────────────────────────────

    public record BookRow(Guid Id, string? Title, string? Slug, string? Tagline);
    public record ChapterRow(Guid Id, Guid BookId, int Number, string? Title, string? Synopsis, string? Status);
    public record ChapterBeatRow(
        Guid BeatGuid, Guid ChapterId, int Index, string? Title, string? Synopsis, string? Text,
        int Act, string? StructureRole, string? SceneType, double SortKey,
        string? EmotionalTone, string? PaceHint, string? AudioPath, double? DurationSec,
        DateTime? NarratedAt, string? LastRequestId, bool WasCorrected);
    public record EpisodeRow(
        Guid Id, string? Slug, string? Seed, string? Title, string? VoiceId,
        DateTime StartedAt, DateTime? GenerationCompletedAt, DateTime? AudioCompletedAt,
        string? Status, int CharsNarrated, string? Error, string? ScriptMarkdownPath,
        string? ScriptPdfPath, string? CombinedAudioPath, double? LastPlayedSec,
        Guid? ParentEpisodeId, Guid? BookId, string? ChapterId);
    public record EpisodeBeatRow(
        Guid EpisodeId, int Index, string? Text, string? AudioPath, DateTime? NarratedAt,
        double? DurationSec, bool WasCorrected, double SortKey, string? BeatTitle,
        string? Synopsis, string? StructureRole, int Act, string? SceneType,
        string? EmotionalTone, string? PaceHint, string? TextHash, string? SourceBeatGuid,
        bool Stale, string? LastRequestId);
}
