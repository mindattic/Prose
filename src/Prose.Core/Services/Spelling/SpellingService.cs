using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using WeCantSpell.Hunspell;

namespace Prose.Core.Services.Spelling;

/// <summary>
/// The Writer's spelling check: open-source Hunspell (WeCantSpell.Hunspell, MIT) over the SCOWL
/// en_US dictionary (wooorm/dictionaries, MIT AND BSD; see Dictionaries/LICENSE-en_US.txt), plus
/// two sources of words the language dictionary cannot know:
///
/// <list type="bullet">
/// <item><description><b>The author's dictionary</b>, the <see cref="SpellingWord"/> table. One
/// entry covers its plural and possessive forms (author, 2026-09-26: "CorpoNation, CorpoNations,
/// CorpoNation's - all part of the same entry"). An entry with capitals in it must be written with
/// them; an all-lowercase entry matches any capitalisation.</description></item>
/// <item><description><b>The world's names</b>: every word of every entity name and character
/// alias in the book's universe, any capitalisation. "credstick" is known because the Credstick
/// technology exists.</description></item>
/// </list>
///
/// <para>Words written entirely in capitals are not checked (the word-processor convention): the
/// books are full of contract text and call signs, and flagging every one of them buries the real
/// typos. Words containing a digit are not checked either (Φ30, 2D, 0247).</para>
///
/// <para>A singleton. The author's dictionary is cached and reloaded after every add or remove
/// made through this service; the CLI and MCP reach it inside the Hub's process, so their edits
/// land in the same cache. Entity names are cached per universe for a few minutes.</para>
/// </summary>
public sealed class SpellingService(IDbContextFactory<ProseDbContext> dbFactory)
{
    private static readonly TimeSpan EntityWordsTtl = TimeSpan.FromMinutes(5);

    // Letters and combining marks, joined by apostrophes. Hyphens split: "fear-sweat" is two words.
    private static readonly Regex WordShape = new(@"^[\p{L}\p{M}]+(?:'[\p{L}\p{M}]+)*$", RegexOptions.Compiled);
    private static readonly Regex NameToken = new(@"[\p{L}\p{M}]+(?:['’][\p{L}\p{M}]+)*", RegexOptions.Compiled);

    private static readonly Lazy<WordList> English = new(LoadEnglish, LazyThreadSafetyMode.ExecutionAndPublication);

    private readonly SemaphoreSlim gate = new(1, 1);
    private volatile AuthorWords? authorWords;
    private readonly ConcurrentDictionary<Guid, (DateTime At, HashSet<string> Words)> entityWords = new();

    // ── Checking ─────────────────────────────────────────────────────────

    /// <summary>The words in <paramref name="words"/> that are misspelled, each once, as given.</summary>
    /// <param name="bookOrUniverseId">A book (any node) or universe id, for the world's names. Null
    /// checks against the language and the author's dictionary only.</param>
    public async Task<IReadOnlyList<string>> MisspelledAsync(
        IEnumerable<string> words, Guid? bookOrUniverseId = null, CancellationToken ct = default)
    {
        var author = await AuthorWordsAsync(ct);
        var world = await WorldWordsAsync(bookOrUniverseId, ct);
        var bad = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var raw in words)
        {
            if (string.IsNullOrWhiteSpace(raw) || !seen.Add(raw)) continue;
            if (!IsKnown(raw, author, world)) bad.Add(raw);
        }
        return bad;
    }

    /// <summary>True when the word is spelled right (or is not a word this checks at all).</summary>
    public async Task<bool> IsKnownAsync(string word, Guid? bookOrUniverseId = null, CancellationToken ct = default) =>
        IsKnown(word, await AuthorWordsAsync(ct), await WorldWordsAsync(bookOrUniverseId, ct));

    /// <summary>Corrections for a misspelled word, best first: the author's own words and the
    /// world's names that are a letter or two away, then Hunspell's suggestions. Empty when nothing
    /// is close.</summary>
    public async Task<IReadOnlyList<string>> SuggestAsync(
        string word, Guid? bookOrUniverseId = null, int max = 6, CancellationToken ct = default)
    {
        var w = Normalize(word);
        if (w.Length == 0) return [];
        var author = await AuthorWordsAsync(ct);
        var world = await WorldWordsAsync(bookOrUniverseId, ct);

        var own = author.Entries.Concat(world)
            .Select(e => (e, d: Distance(e.ToLowerInvariant(), w.ToLowerInvariant())))
            .Where(x => x.d > 0 && x.d <= (w.Length <= 4 ? 1 : 2))
            .OrderBy(x => x.d).ThenBy(x => x.e, StringComparer.Ordinal)
            .Select(x => MatchCase(x.e, w));

        IEnumerable<string> hunspell;
        try { hunspell = English.Value.Suggest(w); }
        catch (Exception) { hunspell = []; }

        return own.Concat(hunspell)
            .Where(s => !string.Equals(s, w, StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .Take(Math.Max(1, max))
            .ToList();
    }

    internal static bool IsKnown(string raw, AuthorWords author, HashSet<string> world)
    {
        var w = Normalize(raw);
        if (w.Length < 2) return true;                              // a stray letter, or nothing
        if (w.Any(char.IsDigit)) return true;                       // Φ30, 2D, 0247
        if (!WordShape.IsMatch(w)) return true;                     // not a word we can judge
        if (w.Any(char.IsLetter) && w.Where(char.IsLetter).All(char.IsUpper)) return true; // ALL CAPS

        if (author.Accepts(w) || WorldAccepts(world, w)) return true;
        if (Base(w) is { } b && (author.Accepts(b) || WorldAccepts(world, b))) return true;

        try { return English.Value.Check(w); }
        catch (Exception) { return true; }                          // never flag on a checker fault
    }

    /// <summary>A world name, or its plural: the page word with a plural ending taken off.</summary>
    private static bool WorldAccepts(HashSet<string> world, string w)
    {
        if (world.Count == 0) return false;
        var l = w.ToLowerInvariant();
        if (world.Contains(l)) return true;
        if (l.EndsWith("ies") && world.Contains(l[..^3] + "y")) return true;
        if (l.EndsWith("es") && world.Contains(l[..^2])) return true;
        return l.EndsWith('s') && world.Contains(l[..^1]);
    }

    /// <summary>Curly apostrophes to straight, and apostrophes and hyphens off the ends.</summary>
    internal static string Normalize(string raw) =>
        (raw ?? "").Trim().Replace('’', '\'').Replace('‘', '\'').Trim('\'', '-');

    /// <summary>The word with a possessive taken off ("CorpoNation's" and "CorpoNations'" both
    /// give their bare form), or null when there is none.</summary>
    private static string? Base(string w)
    {
        if (w.EndsWith("'s", StringComparison.OrdinalIgnoreCase) && w.Length > 2) return w[..^2];
        return null;
    }

    // ── The author's dictionary ──────────────────────────────────────────

    public async Task<IReadOnlyList<SpellingWord>> ListAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.SpellingWords.AsNoTracking().OrderBy(w => w.Word).ToListAsync(ct);
    }

    public sealed record AddResult(bool Added, SpellingWord Row, string? Note);

    /// <summary>Add a word. A possessive is stored as its bare form ("CorpoNation's" adds
    /// "CorpoNation"), since the entry covers it anyway. Refused (<see cref="ArgumentException"/>)
    /// when it is not a single word. Adding a word that is already there is not an error.</summary>
    public async Task<AddResult> AddAsync(string word, string addedBy = "author", CancellationToken ct = default)
    {
        var w = Normalize(word);
        if (Base(w) is { } bare) w = bare;
        if (w.EndsWith('\'')) w = w.TrimEnd('\'');
        if (w.Length == 0) throw new ArgumentException("the word is empty.");
        if (w.Length > SpellingWord.MaxLength) throw new ArgumentException($"a word is at most {SpellingWord.MaxLength} characters.");
        if (!WordShape.IsMatch(w)) throw new ArgumentException($"'{word}' is not a single word (letters and apostrophes only; a hyphenated word is two).");

        await gate.WaitAsync(ct);
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            var existing = (await db.SpellingWords.ToListAsync(ct))
                .FirstOrDefault(x => string.Equals(x.Word, w, StringComparison.OrdinalIgnoreCase));
            if (existing is not null)
                return new AddResult(false, existing, string.Equals(existing.Word, w, StringComparison.Ordinal)
                    ? "already in the dictionary"
                    : $"already in the dictionary as '{existing.Word}'");

            var row = new SpellingWord { Word = w, AddedBy = string.IsNullOrWhiteSpace(addedBy) ? "author" : addedBy.Trim() };
            db.SpellingWords.Add(row);
            await db.SaveChangesAsync(ct);
            authorWords = null;
            return new AddResult(true, row, null);
        }
        finally { gate.Release(); }
    }

    /// <summary>Remove a word, matched without regard to case. False when it was not there.</summary>
    public async Task<bool> RemoveAsync(string word, CancellationToken ct = default)
    {
        var w = Normalize(word);
        await gate.WaitAsync(ct);
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            var rows = (await db.SpellingWords.ToListAsync(ct))
                .Where(x => string.Equals(x.Word, w, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (rows.Count == 0) return false;
            db.SpellingWords.RemoveRange(rows);
            await db.SaveChangesAsync(ct);
            authorWords = null;
            return true;
        }
        finally { gate.Release(); }
    }

    private async Task<AuthorWords> AuthorWordsAsync(CancellationToken ct)
    {
        if (authorWords is { } cached) return cached;
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var words = await db.SpellingWords.AsNoTracking().Select(w => w.Word).ToListAsync(ct);
        return authorWords = new AuthorWords(words);
    }

    /// <summary>
    /// The author's words, with every form each one covers. An all-lowercase entry matches any
    /// capitalisation; an entry with a capital in it matches as written (its ALL-CAPS form is never
    /// checked at all).
    /// </summary>
    internal sealed class AuthorWords
    {
        private readonly HashSet<string> exact = new(StringComparer.Ordinal);
        private readonly HashSet<string> anyCase = new(StringComparer.Ordinal);

        public IReadOnlyList<string> Entries { get; }

        public AuthorWords(IEnumerable<string> words)
        {
            Entries = words.Where(w => !string.IsNullOrWhiteSpace(w)).Select(Normalize).Distinct().ToList();
            foreach (var e in Entries)
            {
                var lower = e.ToLowerInvariant();
                var target = e == lower ? anyCase : exact;
                foreach (var f in Forms(e == lower ? lower : e)) target.Add(f);
            }
        }

        public bool Accepts(string w) => exact.Contains(w) || anyCase.Contains(w.ToLowerInvariant());

        /// <summary>The word, its plural (-s, -es, consonant-y to -ies) and its possessives' bare
        /// stems. Possessives are handled by <see cref="Base"/> before this is asked.</summary>
        public static IEnumerable<string> Forms(string e)
        {
            yield return e;
            yield return e + "s";
            yield return e + "es";
            if (e.Length > 1 && (e[^1] is 'y' or 'Y') && !"aeiouAEIOU".Contains(e[^2]))
                yield return e[..^1] + (e[^1] == 'Y' ? "IES" : "ies");
            yield return e + "s'";
        }
    }

    // ── The world's names ────────────────────────────────────────────────

    private async Task<HashSet<string>> WorldWordsAsync(Guid? bookOrUniverseId, CancellationToken ct)
    {
        if (bookOrUniverseId is not { } id || id == Guid.Empty) return [];
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        // A book id is the common case (the Writer knows its book); a universe id works too.
        var universe = await db.Nodes.IgnoreQueryFilters().AsNoTracking()
            .Where(n => n.Id == id).Select(n => (Guid?)n.UniverseId).FirstOrDefaultAsync(ct) ?? id;

        if (entityWords.TryGetValue(universe, out var hit) && DateTime.UtcNow - hit.At < EntityWordsTtl)
            return hit.Words;

        var names = await db.Entities.IgnoreQueryFilters().AsNoTracking()
            .Where(e => e.UniverseId == universe)
            .Select(e => e.Name).ToListAsync(ct);
        var aliases = await db.CharacterAliases.AsNoTracking()
            .Join(db.Entities.IgnoreQueryFilters().AsNoTracking().Where(e => e.UniverseId == universe),
                a => a.CharacterId, e => e.Id, (a, e) => a.Value)
            .ToListAsync(ct);

        var words = new HashSet<string>(StringComparer.Ordinal);
        foreach (var name in names.Concat(aliases))
            foreach (Match m in NameToken.Matches(name ?? ""))
                if (m.Value.Length >= 2) words.Add(Normalize(m.Value).ToLowerInvariant());

        entityWords[universe] = (DateTime.UtcNow, words);
        return words;
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private static WordList LoadEnglish()
    {
        var asm = typeof(SpellingService).Assembly;
        using var dic = asm.GetManifestResourceStream("Prose.Spelling.en_US.dic")
            ?? throw new InvalidOperationException("the en_US.dic resource is missing from Prose.Core.");
        using var aff = asm.GetManifestResourceStream("Prose.Spelling.en_US.aff")
            ?? throw new InvalidOperationException("the en_US.aff resource is missing from Prose.Core.");
        return WordList.CreateFromStreams(dic, aff);
    }

    /// <summary>Give a suggestion the typed word's capitalisation when the entry has none of its own.</summary>
    private static string MatchCase(string entry, string typed)
    {
        if (entry != entry.ToLowerInvariant() || typed.Length == 0) return entry;
        return char.IsUpper(typed[0]) ? char.ToUpperInvariant(entry[0]) + entry[1..] : entry;
    }

    /// <summary>Optimal-string-alignment distance: an insertion, deletion, substitution or swap of
    /// neighbours costs one.</summary>
    private static int Distance(string a, string b)
    {
        var d = new int[a.Length + 1, b.Length + 1];
        for (var i = 0; i <= a.Length; i++) d[i, 0] = i;
        for (var j = 0; j <= b.Length; j++) d[0, j] = j;
        for (var i = 1; i <= a.Length; i++)
            for (var j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
                if (i > 1 && j > 1 && a[i - 1] == b[j - 2] && a[i - 2] == b[j - 1])
                    d[i, j] = Math.Min(d[i, j], d[i - 2, j - 2] + 1);
            }
        return d[a.Length, b.Length];
    }
}
