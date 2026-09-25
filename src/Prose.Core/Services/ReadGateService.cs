using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Core.Services;

/// <summary>Why a beat counts as unread.</summary>
public enum UnreadReason
{
    /// <summary>No read of this beat has ever been recorded.</summary>
    NeverRead,
    /// <summary>The words changed since it was read.</summary>
    TextChanged,
    /// <summary>The words are the same but the beat before or after it is not: a chapter moved,
    /// or a beat was inserted or deleted beside it. The seam has not been read.</summary>
    Moved,
    /// <summary>An entity the beat mentions was edited after the beat was read.</summary>
    EntityChanged,
}

public sealed record UnreadBeat(int Position, int Number, Guid BeatId, UnreadReason Reason, string? Detail = null);

public sealed record ReadStatus(Guid BookId, int TotalBeats, IReadOnlyList<UnreadBeat> Unread)
{
    public bool AllRead => Unread.Count == 0;
}

/// <summary>Thrown by every shippable export when any beat of the book is unread. There is no
/// override: the only way past it is to read the listed beats.</summary>
public sealed class UnreadBeatsException(ReadStatus status)
    : InvalidOperationException(ReadGateService.Describe(status))
{
    public ReadStatus Status { get; } = status;
}

/// <summary>
/// Which beats of a book have not been read as they stand, and the export gate that enforces it.
///
/// <para><b>Author ruling 2026-09-22:</b> the book is the book. Beats are the prose; canon entities
/// are what it draws on. Nothing else is kept, so nothing can drift. What used to be "is the outline
/// / ledger / spine still in sync" becomes one computed question: <i>has every beat been read, as
/// it stands now, where it stands now?</i></para>
///
/// <para><b>Computed, never stored.</b> A beat is unread when it has no receipt, when its
/// <see cref="Beat.TextHash"/> differs from the receipt's, when the beat before or after it in
/// reading order differs from the receipt's, or when an entity it mentions has a
/// <c>ModifiedAt</c> after the receipt. None of these is a flag a writer has to remember to set.</para>
///
/// <para><b>Enforced where the book ships.</b> <see cref="EnsureReadAsync"/> is called at the top
/// of every shippable export (docx, epub, pdf, audio txt). It has no force parameter, and a unit
/// test fails the build if one of those exports stops calling it. Markdown export is exempt: it is
/// the pre-edit backup format, and nothing ships from it.</para>
///
/// <para><b>Receipts are written by the act of reading.</b> <c>read_beats</c> /
/// <c>prose --read-beats</c> with mark-read record exactly the beats whose text they returned, at
/// the hash of the text they returned. What was delivered is what gets marked.</para>
/// </summary>
public sealed class ReadGateService(IDbContextFactory<ProseDbContext> dbFactory, NodeWorkbenchService workbench)
{
    /// <summary>Status of every beat in <paramref name="nodeId"/>, judged in its BOOK's reading order
    /// (so exporting one chapter does not mistake the chapter boundary for a moved seam).</summary>
    public async Task<ReadStatus> GetStatusAsync(Guid nodeId, CancellationToken ct = default)
    {
        Guid bookId;
        await using (var db = await dbFactory.CreateDbContextAsync(ct))
            bookId = await NodeWorkbenchService.ResolveBookAncestorIdAsync(db, nodeId, ct) ?? nodeId;

        var bookOrder = await workbench.GetOrderedBeatsAsync(bookId, ct);
        HashSet<Guid>? scope = null;
        if (bookId != nodeId)
            scope = (await workbench.GetOrderedBeatsAsync(nodeId, ct)).Select(o => o.Beat.Id).ToHashSet();

        var ids = bookOrder.Select(o => o.Beat.Id).ToList();
        Dictionary<Guid, BeatReadReceipt> receipts;
        Dictionary<Guid, (string Name, DateTime ModifiedAt)> entities;
        // RFC 0015 §3.2 [RT#3]: what a beat mentions is read from its own tags, now. The
        // BeatEntityMentions table is written by a background task after each save, so a gate that
        // read it could judge a just-saved beat against mentions that were not there yet.
        // DistinctBy: a beat linked under two nodes appears twice in the walk, and a duplicate key
        // made the gate — and with it factory status and next — throw.
        var tagged = bookOrder.DistinctBy(o => o.Beat.Id).ToDictionary(o => o.Beat.Id, o => BeatMarkup.ExtractEntityGuids(o.Beat.Text).ToList());
        var entityIds = tagged.Values.SelectMany(g => g).Distinct().ToList();
        await using (var db = await dbFactory.CreateDbContextAsync(ct))
        {
            receipts = await db.BeatReadReceipts.AsNoTracking()
                .Where(r => ids.Contains(r.BeatId)).ToDictionaryAsync(r => r.BeatId, ct);
            entities = (await db.Entities.IgnoreQueryFilters().AsNoTracking()
                    .Where(e => entityIds.Contains(e.Id)).Select(e => new { e.Id, e.Name, e.ModifiedAt }).ToListAsync(ct))
                .ToDictionary(e => e.Id, e => (e.Name, e.ModifiedAt));
        }
        var mentionsByBeat = tagged
            .SelectMany(kv => kv.Value.Where(entities.ContainsKey)
                .Select(g => (BeatId: kv.Key, entities[g].Name, entities[g].ModifiedAt)))
            .ToLookup(m => m.BeatId);

        var unread = new List<UnreadBeat>();
        for (var i = 0; i < bookOrder.Count; i++)
        {
            var beat = bookOrder[i].Beat;
            if (scope != null && !scope.Contains(beat.Id)) continue;
            var prev = i > 0 ? bookOrder[i - 1].Beat.Id : (Guid?)null;
            var next = i < bookOrder.Count - 1 ? bookOrder[i + 1].Beat.Id : (Guid?)null;
            var why = Judge(beat, prev, next, receipts.GetValueOrDefault(beat.Id), mentionsByBeat[beat.Id]);
            if (why is { } u) unread.Add(new UnreadBeat(i + 1, beat.Number, beat.Id, u.Reason, u.Detail));
        }
        return new ReadStatus(bookId, scope?.Count ?? bookOrder.Count, unread);
    }

    /// <summary>The hash the read gate compares: the stored one, or — when a raw write left it NULL
    /// (the drift-guard trigger does) — the same hash computed from the text. Comparing NULL against
    /// the "" a caller delivered meant such a beat could never be marked read, and blocked export.</summary>
    public static string HashOf(Beat beat) => beat.TextHash ?? Beat.ComputeHash(beat.Text);

    /// <summary>The rule, pure. Null = read as it stands.</summary>
    public static (UnreadReason Reason, string? Detail)? Judge(
        Beat beat, Guid? prev, Guid? next, BeatReadReceipt? receipt,
        IEnumerable<(Guid BeatId, string Name, DateTime ModifiedAt)> mentions)
    {
        if (receipt == null) return (UnreadReason.NeverRead, null);
        if (!string.Equals(receipt.TextHash, HashOf(beat), StringComparison.Ordinal)) return (UnreadReason.TextChanged, null);
        if (receipt.PrevBeatId != prev || receipt.NextBeatId != next)
            return (UnreadReason.Moved, receipt.PrevBeatId != prev ? "the beat before it changed" : "the beat after it changed");
        var changed = mentions.Where(m => m.ModifiedAt > receipt.ReadAt).Select(m => m.Name).Distinct().ToList();
        return changed.Count > 0 ? (UnreadReason.EntityChanged, string.Join(", ", changed)) : null;
    }

    public sealed record ReadMention(Guid BeatId, int Number);

    /// <summary>RFC 0015 §3.4 [RT#1]: what an entity write costs, before it is made. The beats that
    /// count as read now and would stop counting the moment <paramref name="entityId"/>'s record
    /// changes: tagged with it, text unchanged since the read, and read after the entity last changed.
    /// Every book, since a record change un-reads its mentions everywhere.</summary>
    public async Task<List<ReadMention>> ReadBeatsMentioningAsync(Guid entityId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var modifiedAt = await db.Entities.IgnoreQueryFilters().AsNoTracking()
            .Where(e => e.Id == entityId).Select(e => (DateTime?)e.ModifiedAt).FirstOrDefaultAsync(ct);
        if (modifiedAt == null) return [];
        var dashed = entityId.ToString("D");
        var bare = entityId.ToString("N");
        var rows = await db.BeatReadReceipts.AsNoTracking()
            .Join(db.Beats.IgnoreQueryFilters().AsNoTracking(), r => r.BeatId, b => b.Id,
                (r, b) => new { r.ReadAt, ReadHash = r.TextHash, b.Id, b.Number, b.TextHash, b.Text })
            .Where(x => x.ReadAt >= modifiedAt && x.ReadHash == x.TextHash
                        && (x.Text.Contains(dashed) || x.Text.Contains(bare)))
            .ToListAsync(ct);
        return rows.Where(x => BeatMarkup.ExtractEntityGuids(x.Text).Contains(entityId))
            .Select(x => new ReadMention(x.Id, x.Number)).OrderBy(x => x.Number).ToList();
    }

    /// <summary>The export gate. Throws <see cref="UnreadBeatsException"/> if anything in the node is
    /// unread. Deliberately takes no override.</summary>
    public async Task EnsureReadAsync(Guid nodeId, CancellationToken ct = default)
    {
        var status = await GetStatusAsync(nodeId, ct);
        if (!status.AllRead) throw new UnreadBeatsException(status);
    }

    /// <summary>Record that these beats were read as delivered: (beat id, the text hash of what was
    /// delivered). Neighbours are taken from the book's reading order now. A beat whose text changed
    /// between delivery and this call is NOT marked, since what was read is no longer on the page.
    /// Returns how many were marked.</summary>
    public async Task<int> MarkReadAsync(Guid nodeId, IEnumerable<(Guid BeatId, string TextHash)> delivered,
        string readBy, CancellationToken ct = default)
    {
        Guid bookId;
        await using (var db0 = await dbFactory.CreateDbContextAsync(ct))
            bookId = await NodeWorkbenchService.ResolveBookAncestorIdAsync(db0, nodeId, ct) ?? nodeId;
        var order = await workbench.GetOrderedBeatsAsync(bookId, ct);
        var index = order.Select((o, i) => (o.Beat, i)).DistinctBy(x => x.Beat.Id).ToDictionary(x => x.Beat.Id, x => x.i);

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var marked = 0;
        var now = DateTime.UtcNow;
        foreach (var (beatId, hash) in delivered)
        {
            if (!index.TryGetValue(beatId, out var i)) continue;
            if (!string.Equals(HashOf(order[i].Beat), hash, StringComparison.Ordinal)) continue;
            var row = await db.BeatReadReceipts.FirstOrDefaultAsync(r => r.BeatId == beatId, ct);
            if (row == null) { row = new BeatReadReceipt { BeatId = beatId }; db.BeatReadReceipts.Add(row); }
            row.TextHash = hash;
            row.PrevBeatId = i > 0 ? order[i - 1].Beat.Id : null;
            row.NextBeatId = i < order.Count - 1 ? order[i + 1].Beat.Id : null;
            row.ReadAt = now;
            row.ReadBy = string.IsNullOrWhiteSpace(readBy) ? "unknown" : readBy.Trim();
            marked++;
        }
        await db.SaveChangesAsync(ct);
        return marked;
    }

    // ── Read notes: what a read found on a beat (defect | question | note) ─────

    public static readonly string[] NoteKinds = ["defect", "question", "note"];

    /// <summary>File a note on one beat (by global Beat.Number), stamped with its current hash.</summary>
    public async Task<BeatReadNote> AddNoteAsync(Guid nodeId, int beatNumber, string kind, string text, string readBy,
        CancellationToken ct = default)
    {
        kind = (kind ?? "").Trim().ToLowerInvariant();
        if (!NoteKinds.Contains(kind)) throw new ArgumentException($"kind must be one of {string.Join(", ", NoteKinds)}.");
        if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("text is required.");
        var beat = (await workbench.GetOrderedBeatsAsync(nodeId, ct)).Select(o => o.Beat).FirstOrDefault(b => b.Number == beatNumber)
                   ?? throw new ArgumentException($"#{beatNumber} is not a beat of this node.");
        var note = new BeatReadNote
        {
            BeatId = beat.Id, TextHash = HashOf(beat), Kind = kind, Text = text.Trim(),
            ReadBy = string.IsNullOrWhiteSpace(readBy) ? "unknown" : readBy.Trim(),
        };
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        db.BeatReadNotes.Add(note);
        await db.SaveChangesAsync(ct);
        return note;
    }

    public sealed record NoteRow(Guid Id, int Position, int Number, string Kind, string Status, string Text,
        bool BeatChangedSince, int? ResolvedByNumber, DateTime At, string ReadBy);

    /// <summary>Notes on a node's beats in reading order. <c>BeatChangedSince</c> is computed: the beat's
    /// text no longer hashes to what the note was written against.</summary>
    public async Task<List<NoteRow>> ListNotesAsync(Guid nodeId, string? status = "open", string? kind = null,
        CancellationToken ct = default)
    {
        // Normalised and checked like AddNoteAsync: "Open" or "Defect" used to match nothing and
        // report zero notes, which reads exactly like "no open defects".
        status = (status ?? "").Trim().ToLowerInvariant();
        kind = (kind ?? "").Trim().ToLowerInvariant();
        if (status.Length > 0 && status is not ("open" or "resolved" or "all"))
            throw new ArgumentException("status must be open, resolved or all.");
        if (kind.Length > 0 && !NoteKinds.Contains(kind))
            throw new ArgumentException($"kind must be one of {string.Join(", ", NoteKinds)}.");
        var order = await workbench.GetOrderedBeatsAsync(nodeId, ct);
        var pos = order.Select((o, i) => (o.Beat, i)).DistinctBy(x => x.Beat.Id).ToDictionary(x => x.Beat.Id, x => (Position: x.i + 1, x.Beat));
        var ids = pos.Keys.ToList();
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var q = db.BeatReadNotes.AsNoTracking().Where(n => ids.Contains(n.BeatId));
        if (status.Length > 0 && status != "all") q = q.Where(n => n.Status == status);
        if (kind.Length > 0) q = q.Where(n => n.Kind == kind);
        var numberById = pos.ToDictionary(p => p.Key, p => p.Value.Beat.Number);
        return (await q.ToListAsync(ct))
            .Select(n => new NoteRow(n.Id, pos[n.BeatId].Position, pos[n.BeatId].Beat.Number, n.Kind, n.Status, n.Text,
                !string.Equals(HashOf(pos[n.BeatId].Beat), n.TextHash, StringComparison.Ordinal),
                n.ResolvedByBeatId is { } r && numberById.TryGetValue(r, out var rn) ? rn : null, n.At, n.ReadBy))
            .OrderBy(n => n.Position).ThenBy(n => n.At).ToList();
    }

    /// <summary>Resolve a note — a question answered or a defect fixed — optionally naming the beat
    /// (global Beat.Number) that answered it.</summary>
    public async Task<bool> ResolveNoteAsync(Guid noteId, Guid nodeId, int? byBeatNumber = null, CancellationToken ct = default)
    {
        var beats = (await workbench.GetOrderedBeatsAsync(nodeId, ct)).Select(o => o.Beat).ToList();
        Guid? byBeat = null;
        if (byBeatNumber is { } n)
            byBeat = beats.FirstOrDefault(b => b.Number == n)?.Id
                     ?? throw new ArgumentException($"#{n} is not a beat of this node.");
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var note = await db.BeatReadNotes.FirstOrDefaultAsync(x => x.Id == noteId, ct);
        // A note on another book's beat is not this node's to resolve.
        if (note == null || !beats.Any(b => b.Id == note.BeatId)) return false;
        note.Status = "resolved";
        // Resolving again without naming a beat keeps the beat already recorded.
        if (byBeat != null || note.ResolvedByBeatId == null) note.ResolvedByBeatId = byBeat;
        await db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>Human summary: counts by reason, then the unread beats as runs of positions.</summary>
    public static string Describe(ReadStatus s)
    {
        if (s.AllRead) return $"All {s.TotalBeats} beats read as they stand.";
        var byReason = string.Join(", ", s.Unread.GroupBy(u => u.Reason).Select(g => $"{g.Count()} {Label(g.Key)}"));
        return $"{s.Unread.Count} of {s.TotalBeats} beats are unread ({byReason}). Read positions {Runs(s.Unread.Select(u => u.Position))}, " +
               "then export. There is no override.";
    }

    private static string Label(UnreadReason r) => r switch
    {
        UnreadReason.NeverRead => "never read",
        UnreadReason.TextChanged => "text changed",
        UnreadReason.Moved => "moved or neighbour changed",
        UnreadReason.EntityChanged => "an entity they mention changed",
        _ => r.ToString(),
    };

    /// <summary>1,2,3,7,9,10 → "1–3, 7, 9–10".</summary>
    public static string Runs(IEnumerable<int> positions)
    {
        var p = positions.Distinct().OrderBy(x => x).ToList();
        var parts = new List<string>();
        for (var i = 0; i < p.Count;)
        {
            var j = i;
            while (j + 1 < p.Count && p[j + 1] == p[j] + 1) j++;
            parts.Add(i == j ? $"{p[i]}" : $"{p[i]}–{p[j]}");
            i = j + 1;
        }
        return string.Join(", ", parts);
    }
}
