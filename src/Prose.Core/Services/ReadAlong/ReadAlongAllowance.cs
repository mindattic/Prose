using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Core.Services.ReadAlong;

/// <summary>
/// Optional limits on how much the read-along reads and how many questions the book answers
/// (RFC 0019 §8) — the "free reads your first 100,000 words and answers 10 questions" shape, as a
/// setting rather than a bill. Prose runs on the author's own machine, so the default is no limit at
/// all; an author who wants a budget (an LLM-priced Ask, a shared install) sets one.
///
/// <para>When a limit is reached nothing is deleted and nothing already read is lost: the
/// read-along simply stops reading new chapters, and Ask stops answering, until the limit is raised.
/// Writing is never limited.</para>
/// </summary>
public sealed class ReadAlongAllowance(IDbContextFactory<ProseDbContext> dbFactory, SettingsKvStore settings)
{
    public const string SettingsKey = "readalong.allowance";

    /// <summary>0 means unlimited.</summary>
    public sealed record Limits(int WordLimit = 0, int QuestionLimit = 0);

    public sealed record Usage(int WordsRead, int WordLimit, int QuestionsAsked, int QuestionLimit)
    {
        public bool ReadingCapped => WordLimit > 0 && WordsRead >= WordLimit;
        public bool QuestionsCapped => QuestionLimit > 0 && QuestionsAsked >= QuestionLimit;
        public int? WordsLeft => WordLimit > 0 ? Math.Max(0, WordLimit - WordsRead) : null;
        public int? QuestionsLeft => QuestionLimit > 0 ? Math.Max(0, QuestionLimit - QuestionsAsked) : null;
    }

    public Limits Get() => settings.Get<Limits>(SettingsKey) ?? new Limits();

    public void Set(Limits limits) =>
        settings.Set(SettingsKey, new Limits(Math.Max(0, limits.WordLimit), Math.Max(0, limits.QuestionLimit)));

    public async Task<Usage> GetUsageAsync(CancellationToken ct = default)
    {
        var limits = Get();
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var words = await db.ChapterReadings.IgnoreQueryFilters().Where(r => r.Status == ReadAlongStatus.Done).SumAsync(r => (int?)r.Words, ct) ?? 0;
        var questions = await db.BookChatMessages.IgnoreQueryFilters().CountAsync(m => m.Role == "user", ct);
        return new Usage(words, limits.WordLimit, questions, limits.QuestionLimit);
    }

    /// <summary>Whether a chapter of <paramref name="words"/> may be read. A chapter that would
    /// cross the limit is still read when it is the first one past it (the limit counts words
    /// read, it does not cut a chapter in half); after that, reading stops.</summary>
    public async Task<bool> CanReadAsync(int words, CancellationToken ct = default)
    {
        var limits = Get();
        if (limits.WordLimit <= 0) return true;
        var usage = await GetUsageAsync(ct);
        return usage.WordsRead < limits.WordLimit;
    }

    public async Task<bool> CanAskAsync(CancellationToken ct = default)
    {
        var limits = Get();
        if (limits.QuestionLimit <= 0) return true;
        return !(await GetUsageAsync(ct)).QuestionsCapped;
    }
}
