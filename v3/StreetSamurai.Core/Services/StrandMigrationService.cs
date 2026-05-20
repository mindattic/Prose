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
/// world into the unified <see cref="Beat"/> + <see cref="Strand"/> +
/// <see cref="StrandBeat"/> world.
///
/// Run via <c>ss --migrate-strands</c> or implicitly at startup (the seed
/// service queues it). Safe to re-run: every insert is gated on a
/// <c>NOT EXISTS</c>-style check keyed on the source row's GUID, so a second
/// pass is a no-op (or picks up any new rows added since the last run).
/// </summary>
public class StrandMigrationService
{
    private readonly IDbContextFactory<StreetSamuraiDbContext> dbFactory;
    private readonly ILogger<StrandMigrationService> log;

    public StrandMigrationService(IDbContextFactory<StreetSamuraiDbContext> dbFactory, ILogger<StrandMigrationService> log)
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
        var (episodesAdded, chapterIdToStrand) = await MigrateEpisodesAsync(db, ct);
        var (standaloneBeats, episodeJunctions) = await MigrateEpisodeBeatsAsync(db, ct);

        var report = new MigrationReport(booksAdded, chaptersAdded, beatsAdded, episodesAdded, standaloneBeats, beatJunctions + episodeJunctions);
        log.LogInformation("Strand migration complete: {Report}", report);
        return report;
    }

    // ── Phase 1: Books → Strand(kind=book) ───────────────────────────────
    // Reuse Book.Id as the new Strand.Id so every downstream BookId
    // reference resolves to a Strand by GUID without remapping. Idempotent
    // on Strand.Id — re-running skips books already migrated.
    private async Task<int> MigrateBooksAsync(StreetSamuraiDbContext db, CancellationToken ct)
    {
        var bookRows = await db.Database
            .SqlQueryRaw<BookRow>("SELECT Id, Title, Slug, Tagline FROM Books")
            .ToListAsync(ct);
        var existingStrandIds = (await db.Strands.Select(s => s.Id).ToListAsync(ct)).ToHashSet();

        int added = 0;
        for (int i = 0; i < bookRows.Count; i++)
        {
            var b = bookRows[i];
            if (existingStrandIds.Contains(b.Id)) continue;
            db.Strands.Add(new Strand
            {
                Id       = b.Id,
                Slug     = ResolveSlug(b.Slug, b.Title, b.Id),
                Title    = b.Title ?? "Untitled book",
                Synopsis = b.Tagline,
                Kind     = "book",
                Status   = "draft",
                SortKey  = (i + 1) * 100.0,
            });
            added++;
        }
        if (added > 0) await db.SaveChangesAsync(ct);
        return added;
    }

    // ── Phase 2: Chapters → Strand(kind=chapter, parent=book strand) ─────
    private async Task<int> MigrateChaptersAsync(StreetSamuraiDbContext db, CancellationToken ct)
    {
        var chapterRows = await db.Database
            .SqlQueryRaw<ChapterRow>("SELECT Id, BookId, Number, Title, Synopsis, Status FROM Chapters")
            .ToListAsync(ct);
        var existingStrandIds = (await db.Strands.Select(s => s.Id).ToListAsync(ct)).ToHashSet();
        var slugIndex = (await db.Strands.Select(s => s.Slug).ToListAsync(ct)).ToHashSet(StringComparer.OrdinalIgnoreCase);

        int added = 0;
        foreach (var c in chapterRows.OrderBy(c => c.BookId).ThenBy(c => c.Number))
        {
            if (existingStrandIds.Contains(c.Id)) continue;
            db.Strands.Add(new Strand
            {
                Id             = c.Id,
                Slug           = UniqueSlug(c.Title ?? "chapter", c.Id, slugIndex),
                Title          = c.Title ?? "Untitled chapter",
                Synopsis       = c.Synopsis,
                Kind           = "chapter",
                Status         = string.IsNullOrEmpty(c.Status) ? "draft" : c.Status,
                ParentStrandId = c.BookId,
                SortKey        = c.Number * 100.0,
            });
            added++;
        }
        if (added > 0) await db.SaveChangesAsync(ct);
        return added;
    }

    // ── Phase 3: ChapterBeats → Beat + StrandBeat ────────────────────────
    // The Step-2 schema converged ChapterBeats with audio fields, so each
    // row already carries everything a Beat needs. Beat.Id = ChapterBeat.BeatGuid
    // so SourceBeatGuid links from EpisodeBeats (next phase) still resolve.
    private async Task<(int beatsAdded, int junctionsAdded)> MigrateChapterBeatsAsync(StreetSamuraiDbContext db, CancellationToken ct)
    {
        var rows = await db.Database
            .SqlQueryRaw<ChapterBeatRow>(@"
                SELECT BeatGuid, ChapterId, [Index], Title, Synopsis, Text, Act, StructureRole, SceneType, FacetTag,
                       SortKey, EmotionalTone, PaceHint, AudioPath, DurationSec, NarratedAt, LastRequestId, WasCorrected
                FROM ChapterBeats")
            .ToListAsync(ct);
        var existingBeatIds = (await db.Beats.Select(b => b.Id).ToListAsync(ct)).ToHashSet();
        var existingJunctions = (await db.StrandBeats
            .Select(sb => new { sb.StrandId, sb.BeatId })
            .ToListAsync(ct))
            .Select(x => (x.StrandId, x.BeatId)).ToHashSet();

        int beatsAdded = 0, junctionsAdded = 0;
        foreach (var cb in rows)
        {
            if (!existingBeatIds.Contains(cb.BeatGuid))
            {
                db.Beats.Add(new Beat
                {
                    Id            = cb.BeatGuid,
                    Text          = cb.Text ?? "",
                    TextHash      = string.IsNullOrEmpty(cb.Text) ? null : ComputeTextHash(cb.Text),
                    BeatTitle     = cb.Title,
                    Synopsis      = cb.Synopsis,
                    StructureRole = cb.StructureRole,
                    Act           = cb.Act,
                    SceneType     = string.IsNullOrEmpty(cb.SceneType) ? "scene" : cb.SceneType,
                    FacetTag      = cb.FacetTag,
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
                db.StrandBeats.Add(new StrandBeat { StrandId = cb.ChapterId, BeatId = cb.BeatGuid, SortKey = cb.SortKey });
                junctionsAdded++;
            }
        }
        if (beatsAdded + junctionsAdded > 0) await db.SaveChangesAsync(ct);
        return (beatsAdded, junctionsAdded);
    }

    // ── Phase 4: Episodes → Strand(kind=episode) ─────────────────────────
    // Episodes that were chapter-recordings attach as children of the
    // corresponding chapter strand. Standalone bedtime episodes have no
    // parent. Returns the chapter-id → strand-id map for phase 5.
    private async Task<(int episodesAdded, Dictionary<string, Guid> chapterIdToStrand)> MigrateEpisodesAsync(StreetSamuraiDbContext db, CancellationToken ct)
    {
        var rows = await db.Database
            .SqlQueryRaw<EpisodeRow>(@"
                SELECT Id, Slug, Seed, Title, VoiceId, StartedAt, GenerationCompletedAt, AudioCompletedAt, Status,
                       CharsNarrated, Error, ScriptMarkdownPath, ScriptPdfPath, CombinedAudioPath,
                       LastPlayedSec, ParentEpisodeId, BookId, ChapterId
                FROM Episodes")
            .ToListAsync(ct);
        var existingStrandIds = (await db.Strands.Select(s => s.Id).ToListAsync(ct)).ToHashSet();
        var slugIndex = (await db.Strands.Select(s => s.Slug).ToListAsync(ct)).ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Build the chapter-id → strand-id lookup for episode parent wiring.
        // Legacy code sometimes wrote Chapter.Id as the no-dashes hex form,
        // sometimes with dashes — accept both.
        var chapterIds = await db.Strands
            .Where(s => s.Kind == "chapter")
            .Select(s => s.Id)
            .ToListAsync(ct);
        var chapterIdToStrand = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        foreach (var cid in chapterIds)
        {
            chapterIdToStrand[cid.ToString()]    = cid;
            chapterIdToStrand[cid.ToString("N")] = cid;
        }

        int added = 0;
        foreach (var e in rows)
        {
            if (existingStrandIds.Contains(e.Id)) continue;
            Guid? parentStrand = null;
            if (!string.IsNullOrEmpty(e.ChapterId) && chapterIdToStrand.TryGetValue(e.ChapterId, out var chStrand))
                parentStrand = chStrand;
            db.Strands.Add(new Strand
            {
                Id                    = e.Id,
                Slug                  = string.IsNullOrEmpty(e.Slug)
                                            ? UniqueSlug(e.Title ?? "episode", e.Id, slugIndex)
                                            : EnsureUniqueSlug(e.Slug, e.Id, slugIndex),
                Title                 = e.Title ?? "Untitled episode",
                Synopsis              = null,
                Kind                  = "episode",
                Status                = string.IsNullOrEmpty(e.Status) ? "draft" : e.Status,
                VoiceId               = e.VoiceId,
                ParentStrandId        = parentStrand,
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
        return (added, chapterIdToStrand);
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
                       BeatTitle, Synopsis, StructureRole, Act, SceneType, FacetTag, EmotionalTone, PaceHint,
                       TextHash, SourceBeatGuid, Stale, LastRequestId
                FROM EpisodeBeats")
            .ToListAsync(ct);
        var existingBeatIds = (await db.Beats.Select(b => b.Id).ToListAsync(ct)).ToHashSet();
        var existingJunctions = (await db.StrandBeats
            .Select(sb => new { sb.StrandId, sb.BeatId })
            .ToListAsync(ct))
            .Select(x => (x.StrandId, x.BeatId)).ToHashSet();

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
                    Text          = eb.Text ?? "",
                    TextHash      = eb.TextHash ?? (string.IsNullOrEmpty(eb.Text) ? null : ComputeTextHash(eb.Text)),
                    BeatTitle     = eb.BeatTitle,
                    Synopsis      = eb.Synopsis,
                    StructureRole = eb.StructureRole,
                    Act           = eb.Act,
                    SceneType     = string.IsNullOrEmpty(eb.SceneType) ? "scene" : eb.SceneType,
                    FacetTag      = eb.FacetTag,
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
                db.StrandBeats.Add(new StrandBeat { StrandId = eb.EpisodeId, BeatId = beatId, SortKey = eb.SortKey });
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
        int Act, string? StructureRole, string? SceneType, string? FacetTag, double SortKey,
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
        string? Synopsis, string? StructureRole, int Act, string? SceneType, string? FacetTag,
        string? EmotionalTone, string? PaceHint, string? TextHash, string? SourceBeatGuid,
        bool Stale, string? LastRequestId);
}
