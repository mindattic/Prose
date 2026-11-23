using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;
using Prose.Core.Services;
using Prose.Core.Services.ReadAlong;

namespace Prose.Cli;

/// <summary>
/// CLI surface for the read-along (RFC 0019). Deterministic and local: no command here spends
/// tokens except <c>--ask-book</c>, and only when an LLM is configured. None of them writes prose.
///
///   prose --import-manuscript --path &lt;file|folder&gt; [--title "T"] [--into &lt;book&gt;] [--read] [--dry-run] [--json]
///   prose --read-along --slug &lt;book&gt; [--chapter &lt;slug&gt;] [--force] [--json]
///   prose --read-along --slug &lt;book&gt; --status [--json]
///   prose --read-along --all                       one sweep over every followed book (the job, on demand)
///   prose --story-bible --slug &lt;book&gt; [--drafts | --keep &lt;id&gt; [--name N] [--category C] | --toss &lt;id&gt; |
///         --keep-all [--kind entry|detail] | --entry &lt;name|id&gt; | --add "Name" [--category C] [--description D] |
///         --set &lt;name|id&gt; --attr A --value V | --remove &lt;name|id&gt;] [--json]
///   prose --continuity-inbox --slug &lt;book&gt; [--all] [--explain &lt;id&gt;] [--rule &lt;id&gt; --action accept-new|keep-existing|retcon|dismiss [--note "…"]] [--reopen &lt;id&gt;] [--json]
///   prose --story-atlas --slug &lt;book&gt; [--as-of N] [--no-drafts] [--json]
///   prose --ask-book "Question" --slug &lt;book&gt; [--json] | --slug &lt;book&gt; --history | --clear
///   prose --brainstorm --slug &lt;book&gt; [--add "Title" [--body B] [--color C] [--x X --y Y] | --delete &lt;id&gt;] [--json]
///   prose --read-allowance [--words N] [--questions N]     0 = unlimited (the default)
/// </summary>
public static class ReadAlongCli
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public static async Task<int> RunAsync(string[] args, IServiceProvider sp)
    {
        string? Arg(string name) { var i = Array.IndexOf(args, name); return i >= 0 && i + 1 < args.Length && !args[i + 1].StartsWith("--") ? args[i + 1] : null; }
        bool Has(string name) => args.Contains(name);
        var json = Has("--json");

        void Out(object o, Action text) { if (json) Console.WriteLine(JsonSerializer.Serialize(o, Json)); else text(); }

        if (Has("--read-allowance"))
        {
            var allowance = sp.GetRequiredService<ReadAlongAllowance>();
            var cur = allowance.Get();
            if (Arg("--words") is { } w || Arg("--questions") is not null)
            {
                allowance.Set(new ReadAlongAllowance.Limits(
                    int.TryParse(Arg("--words"), out var wl) ? wl : cur.WordLimit,
                    int.TryParse(Arg("--questions"), out var ql) ? ql : cur.QuestionLimit));
            }
            var u = await allowance.GetUsageAsync();
            Out(u, () => Console.WriteLine(
                $"Words read: {u.WordsRead:N0} / {(u.WordLimit > 0 ? u.WordLimit.ToString("N0") : "unlimited")}   " +
                $"Questions: {u.QuestionsAsked} / {(u.QuestionLimit > 0 ? u.QuestionLimit.ToString() : "unlimited")}"));
            return 0;
        }

        if (Has("--import-manuscript"))
        {
            var path = Arg("--path");
            if (path is null) { Console.Error.WriteLine("usage: prose --import-manuscript --path <file|folder> [--title T] [--into <book>] [--read] [--dry-run]"); return 2; }
            List<ManuscriptParser.InputFile> files;
            try { files = ManuscriptImportService.FilesFrom(Path.GetFullPath(path)); }
            catch (FileNotFoundException ex) { Console.Error.WriteLine(ex.Message); return 1; }
            if (files.Count == 0) { Console.Error.WriteLine("No .docx, .txt or .md files there."); return 1; }

            if (Has("--dry-run"))
            {
                var preview = ManuscriptImportService.Preview(files, Arg("--title"));
                Out(preview, () =>
                {
                    Console.WriteLine($"{preview.Title}: {preview.Chapters.Count()} chapters, {preview.Words:N0} words (dry run, nothing written)");
                    foreach (var v in preview.Volumes)
                    {
                        if (v.Title is not null) Console.WriteLine($"  {v.Title}");
                        foreach (var c in v.Chapters) Console.WriteLine($"    {c.Title} — {c.Paragraphs.Count} paragraphs, {c.Words:N0} words");
                    }
                });
                return 0;
            }

            Guid? into = null;
            if (Arg("--into") is { } intoSlug)
            {
                into = await ResolveAsync(sp, intoSlug);
                if (into is null) { Console.Error.WriteLine($"No book '{intoSlug}'."); return 1; }
            }
            var importer = sp.GetRequiredService<ManuscriptImportService>();
            var result = await importer.ImportAsync(files, Arg("--title"), into);
            if (!json) Console.WriteLine($"Imported {result.Chapters.Count} chapters ({result.Words:N0} words) into {result.Title} ({result.BookSlug}).");
            if (Has("--read"))
            {
                var reads = await sp.GetRequiredService<ReadAlongService>().ReadBookAsync(result.BookId,
                    progress: json ? null : new Progress<ReadAlongService.ReadProgress>(p =>
                    {
                        if (p.Last is { Skipped: false } l)
                            Console.WriteLine($"  ✓ {l.Title} — met {Met(l.Met)} · {l.Details} story details{(l.Contradictions > 0 ? $" · {l.Contradictions} possible contradiction{(l.Contradictions == 1 ? "" : "s")}" : "")}");
                    }));
                Out(new { result, reads }, () => Console.WriteLine($"Read {reads.Count(r => !r.Skipped)} of {reads.Count} chapters."));
            }
            else Out(result, () => { });
            return 0;
        }

        var slug = Arg("--slug") ?? Arg("--book");

        if (Has("--read-along") && Has("--all"))
        {
            var n = await sp.GetRequiredService<ReadAlongBackgroundService>().SweepAsync(TimeSpan.Zero, CancellationToken.None);
            Out(new { read = n }, () => Console.WriteLine($"Read-along sweep: read {n} chapter(s)."));
            return 0;
        }

        if (slug is null) { Console.Error.WriteLine("--slug <book> is required."); return 2; }
        var bookId = await ResolveAsync(sp, slug);
        if (bookId is null) { Console.Error.WriteLine($"No book '{slug}'."); return 1; }
        var book = bookId.Value;

        if (Has("--read-along"))
        {
            var ra = sp.GetRequiredService<ReadAlongService>();
            if (Has("--status"))
            {
                var s = await ra.GetStatusAsync(book);
                Out(s, () =>
                {
                    Console.WriteLine($"Read {s.ChaptersRead} of {s.ChaptersTotal} chapters as they stand ({s.ChaptersStale} changed since) · {s.WordsRead:N0} words");
                    Console.WriteLine($"{s.PendingSuggestions} story-bible drafts waiting · {s.OpenConflicts} open continuity cards · {s.Entries} entries");
                    foreach (var l in s.Log)
                        Console.WriteLine($"  {(l.Current ? "✓" : "↻")} {l.Title} — met {Met(l.Met)} · {l.Details} story details{(l.Contradictions > 0 ? $" · {l.Contradictions} possible contradiction(s)" : "")}");
                });
                return 0;
            }
            List<ReadAlongService.ChapterReadResult> results;
            if (Arg("--chapter") is { } chSlug)
            {
                var chId = await ResolveAsync(sp, chSlug);
                if (chId is null) { Console.Error.WriteLine($"No chapter '{chSlug}'."); return 1; }
                results = [await ra.ReadChapterAsync(book, chId.Value, Has("--force"))];
            }
            else results = await ra.ReadBookAsync(book, Has("--force"));
            Out(results, () =>
            {
                foreach (var r in results)
                    Console.WriteLine(r.Skipped
                        ? $"  – {r.Title}: {r.Reason}"
                        : $"  ✓ {r.Title} — met {Met(r.Met)} · {r.Details} story details{(r.Contradictions > 0 ? $" · {r.Contradictions} possible contradiction(s)" : "")}");
                if (results.Any(r => r.Reason == "allowance"))
                    Console.WriteLine("Stopped at the reading allowance; everything read so far stays. Raise it with prose --read-allowance --words N.");
            });
            return 0;
        }

        if (Has("--story-bible"))
        {
            var bible = sp.GetRequiredService<StoryBibleService>();
            if (Arg("--keep") is { } keepId && Guid.TryParse(keepId, out var kid))
            {
                var r = await bible.KeepAsync(kid, Arg("--name"), Arg("--category"));
                Out(r, () => Console.WriteLine(r.Ok ? $"Kept → entity {r.EntityId} ({r.FactsConfirmed} details confirmed)" : $"Not kept: {r.Error}"));
                return r.Ok ? 0 : 1;
            }
            if (Arg("--toss") is { } tossId && Guid.TryParse(tossId, out var tid))
            {
                var ok = await bible.TossAsync(tid);
                Out(new { ok }, () => Console.WriteLine(ok ? "Tossed. It won't be suggested again." : "No pending draft with that id."));
                return ok ? 0 : 1;
            }
            if (Has("--keep-all"))
            {
                var n = await bible.KeepAllAsync(book, Arg("--kind"));
                Out(new { kept = n }, () => Console.WriteLine($"Kept {n} draft(s)."));
                return 0;
            }
            if (Arg("--add") is { } addName)
            {
                var id = await bible.AddEntryAsync(book, addName, Arg("--category") ?? "character", Arg("--description"));
                Out(new { id }, () => Console.WriteLine($"Added {addName} → {id}"));
                return 0;
            }
            if (Arg("--set") is { } setRef)
            {
                var eid = await EntryIdAsync(sp, book, setRef);
                if (eid is null || Arg("--attr") is not { } attr || Arg("--value") is not { } val) { Console.Error.WriteLine("usage: --set <name|id> --attr A --value V"); return 2; }
                var fid = await bible.SetDetailAsync(book, eid.Value, attr, val);
                Out(new { factId = fid }, () => Console.WriteLine($"Recorded {attr} = {val}"));
                return 0;
            }
            if (Arg("--remove") is { } remRef)
            {
                var eid = await EntryIdAsync(sp, book, remRef);
                var ok = eid is not null && await bible.RemoveEntryAsync(book, eid.Value);
                Out(new { ok }, () => Console.WriteLine(ok ? "Removed from this book's bible." : "No such entry."));
                return ok ? 0 : 1;
            }
            if (Arg("--entry") is { } entryRef)
            {
                var eid = await EntryIdAsync(sp, book, entryRef);
                var e = eid is null ? null : await bible.GetEntryAsync(book, eid.Value);
                if (e is null) { Console.Error.WriteLine("No such entry."); return 1; }
                Out(e, () =>
                {
                    Console.WriteLine($"{e.Name} ({e.Category}){(e.Aliases.Count > 0 ? " aka " + string.Join(", ", e.Aliases) : "")}");
                    if (e.Description is not null) Console.WriteLine("  " + e.Description);
                    foreach (var d in e.Details)
                        Console.WriteLine($"  {d.Attribute}: {d.Value}   “{d.Quote}” — {d.ChapterTitle}");
                    foreach (var l in e.Links) Console.WriteLine($"  ↔ {l.Type} {l.Name}");
                    if (e.PendingDetails.Count > 0) Console.WriteLine($"  {e.PendingDetails.Count} detail draft(s) waiting.");
                });
                return 0;
            }
            if (Has("--drafts"))
            {
                var drafts = await bible.GetDraftsAsync(book);
                Out(drafts, () =>
                {
                    foreach (var d in drafts)
                    {
                        Console.WriteLine($"[{d.Id}] {d.Kind.ToUpperInvariant()} {d.Category}: {d.Name} — {d.ChapterTitle}");
                        foreach (var a in d.Attributes) Console.WriteLine($"      {a.Key}: {a.Value}   “{a.Quote}”");
                    }
                    Console.WriteLine($"{drafts.Count} draft(s). Nothing is final until you keep it: --keep <id> or --toss <id>.");
                });
                return 0;
            }
            var codex = await bible.GetCodexAsync(book);
            Out(codex, () =>
            {
                foreach (var c in codex)
                {
                    Console.WriteLine($"{c.Category} ({c.Items.Count})");
                    foreach (var i in c.Items) Console.WriteLine($"  {(i.IsDraft ? "✎" : "•")} {i.Name}{(i.Facts > 0 ? $" · {i.Facts} details" : "")}{(i.PendingDetails > 0 ? $" · {i.PendingDetails} waiting" : "")}");
                }
            });
            return 0;
        }

        if (Has("--continuity-inbox"))
        {
            var inbox = sp.GetRequiredService<ContinuityInboxService>();
            if (Arg("--explain") is { } ex && Guid.TryParse(ex, out var exId))
            {
                var text = await inbox.ExplainAsync(exId);
                Out(new { explanation = text }, () => Console.WriteLine(text ?? "No such card."));
                return text is null ? 1 : 0;
            }
            if (Arg("--rule") is { } rule && Guid.TryParse(rule, out var rid))
            {
                var (ok, err) = await inbox.ResolveAsync(rid, Arg("--action") ?? "", Arg("--note"));
                Out(new { ok, error = err }, () => Console.WriteLine(ok ? "Ruled." : err));
                return ok ? 0 : 1;
            }
            if (Arg("--reopen") is { } reo && Guid.TryParse(reo, out var reoId))
            {
                var ok = await inbox.ReopenAsync(reoId);
                Out(new { ok }, () => Console.WriteLine(ok ? "Re-opened." : "Nothing to re-open."));
                return ok ? 0 : 1;
            }
            var cards = await inbox.ListAsync(book, Has("--all") ? "all" : "open");
            Out(cards, () =>
            {
                Console.WriteLine($"{cards.Count(c => c.Status == "open")} thing(s) worth a look.");
                foreach (var c in cards)
                {
                    Console.WriteLine($"[{c.Id}] {c.EntityName} · {c.Attribute} · {c.Kind} {c.Confidence}%{(c.Status != "open" ? $" · {c.Status}" : "")}");
                    Console.WriteLine($"    EXISTING  {c.ExistingValue}   “{c.Existing.Quote}” — {c.Existing.ChapterTitle}");
                    Console.WriteLine($"    PROPOSED  {c.ProposedValue}   “{c.Proposed.Quote}” — {c.Proposed.ChapterTitle}");
                }
            });
            return 0;
        }

        if (Has("--story-atlas"))
        {
            var a = await sp.GetRequiredService<StoryAtlasService>().GetAsync(book, int.TryParse(Arg("--as-of"), out var asOf) ? asOf : null, !Has("--no-drafts"));
            Out(a, () =>
            {
                Console.WriteLine($"Story map: {a.Stars.Count} entries · {a.Threads.Count} threads · {a.Chapters.Count} chapters");
                foreach (var s in a.Stars.Take(60)) Console.WriteLine($"  {s.Name} ({s.Category}) — from ch {s.FirstChapter}, {s.Mentions} mentions, {s.Degree} threads{(s.Draft ? " · draft" : "")}");
            });
            return 0;
        }

        if (Has("--ask-book"))
        {
            var chat = sp.GetRequiredService<BookChatService>();
            if (Has("--clear")) { var n = await chat.ClearAsync(book); Out(new { cleared = n }, () => Console.WriteLine($"Cleared {n} message(s).")); return 0; }
            if (Has("--history"))
            {
                var h = await chat.HistoryAsync(book);
                Out(h, () => { foreach (var m in h) Console.WriteLine($"{m.Role}: {m.Text}"); });
                return 0;
            }
            var q = Arg("--ask-book");
            if (q is null) { Console.Error.WriteLine("usage: prose --ask-book \"Question\" --slug <book>"); return 2; }
            var ans = await chat.AskAsync(book, q);
            Out(ans, () =>
            {
                Console.WriteLine(ans.Reply.Text);
                foreach (var d in ans.Reply.Doors) Console.WriteLine($"  ❝ {d.ChapterTitle} · “{d.Quote}”");
            });
            return ans.Refused is null ? 0 : 1;
        }

        if (Has("--brainstorm"))
        {
            var bs = sp.GetRequiredService<BrainstormService>();
            if (Arg("--add") is { } title)
            {
                var c = await bs.AddAsync(book, title, Arg("--body") ?? "", double.TryParse(Arg("--x"), out var x) ? x : 40,
                    double.TryParse(Arg("--y"), out var y) ? y : 40, Arg("--color") ?? "idea");
                Out(c, () => Console.WriteLine($"Card {c.Id} added."));
                return 0;
            }
            if (Arg("--delete") is { } del && Guid.TryParse(del, out var did))
            {
                var ok = await bs.DeleteAsync(did);
                Out(new { ok }, () => Console.WriteLine(ok ? "Deleted." : "No such card."));
                return ok ? 0 : 1;
            }
            var cards = await bs.ListAsync(book);
            Out(cards, () => { foreach (var c in cards) Console.WriteLine($"[{c.Id}] ({c.Color}) {c.Title}{(c.Body.Length > 0 ? " — " + c.Body : "")}"); });
            return 0;
        }

        Console.Error.WriteLine("Unknown read-along command. See prose --help.");
        return 2;
    }

    private static string Met(IReadOnlyList<string> met) =>
        met.Count == 0 ? "no one new" : met.Count <= 3 ? string.Join(", ", met) : $"{string.Join(", ", met.Take(2))} and {met.Count - 2} more";

    private static async Task<Guid?> ResolveAsync(IServiceProvider sp, string slug)
    {
        await using var db = await sp.GetRequiredService<IDbContextFactory<ProseDbContext>>().CreateDbContextAsync();
        return await NodeRefResolver.ResolveAsync(db, slug);
    }

    private static async Task<Guid?> EntryIdAsync(IServiceProvider sp, Guid book, string nameOrId)
    {
        if (Guid.TryParse(nameOrId, out var id)) return id;
        var codex = await sp.GetRequiredService<StoryBibleService>().GetCodexAsync(book);
        return codex.SelectMany(c => c.Items).Where(i => !i.IsDraft)
            .FirstOrDefault(i => string.Equals(i.Name, nameOrId, StringComparison.OrdinalIgnoreCase))?.Id;
    }
}
