namespace Prose.WriterUi.Services;

/// <summary>
/// Links into the Writer (RFC 0019, RA-S2.2.1): <c>/writer?book=&lt;id|slug|code&gt;&amp;beat=&lt;id&gt;</c> opens
/// that book on that beat. Used by every door in the Story workspace, so a continuity card or a codex receipt
/// lands the author on the exact paragraph to fix. Pure: no DI, no database.
/// </summary>
public static class WriterLink
{
    /// <summary>The book a link names: by id, then slug, then code (case-insensitive). Null when none.</summary>
    public static WriterService.BookListItem? ResolveBook(IEnumerable<WriterService.BookListItem> books, string? key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;
        key = key.Trim();
        var list = books as IReadOnlyCollection<WriterService.BookListItem> ?? books.ToList();
        if (Guid.TryParse(key, out var id) && list.FirstOrDefault(b => b.Id == id) is { } byId) return byId;
        return list.FirstOrDefault(b => string.Equals(b.Slug, key, StringComparison.OrdinalIgnoreCase))
            ?? list.FirstOrDefault(b => string.Equals(b.NodeCode, key, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>The Writer URL for a book, optionally on one beat.</summary>
    public static string For(Guid bookId, Guid? beatId = null) =>
        beatId is { } b ? $"/writer?book={bookId:D}&beat={b:D}" : $"/writer?book={bookId:D}";
}
