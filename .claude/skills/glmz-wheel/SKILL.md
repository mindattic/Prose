---
name: glmz-wheel
description: Run the GLMZ focus wheel (Gauss-Seidel): one book at a time is the focus, read against the current text of every other GLMZ book, corrected and enriched by hand-reviewed splices, then the next book. Usage /glmz-wheel [cycle N] [slug ...]; no argument = select the books that need a turn and run one cycle.
---

# GLMZ focus wheel

The author's design (2026-10-04/05, order 01a10a85): "center on one book, and then draw all texture from every other
book to update it, then once that's done you make another book the focus", cycling until nothing meaningful is
left, so every book is its best self and a true piece of one world. Cycle 1 ran on all 22 books (docs/GLMZ_CONSOLIDATION.md,
Round 7). The author then set the cheaper rules below.

Universe GLMZ. Pass `--universe glmz` on every CLI call. One prose.cmd at a time. No export, no audio.

## The rules that keep it cheap

1. **Mechanical defects never need a reader.** Before any turn run `prose --universe glmz --ruling violations --node <slug>`
   for the selected books and fix every hit by hand-reviewed splice. The page-laws catch spaced hyphens used as dashes,
   doubled punctuation and retired spellings (01a10d17-e920, 01a10d19-40c1, 01a10d17-fe30). A new mechanical defect
   found twice by hand becomes another page-law, never a reader task.
2. **A book is read only when something it shares changed.** `python tools/select.py <work>` lists the books whose
   shared, specific entities were renamed into, rewritten or drawn in by another book's applied rows since that
   book's last turn, and the beat positions that mention them (select.json). Only those beats are read, with two
   beats either side. Cycle 1 to cycle 2 this is 9% of the catalog's words. A whole-book read is for cycle 1, a book
   new to the wheel, or an author request.
3. **Borrowing is optional and capped at 3 rows per book; zero is normal.** Corrections are required.
4. **Author questions first.** Open author flags (order 01a10cd8 for cycle 1) are settled before the next cycle;
   an answer fixes more than a reading pass.
5. **A cycle is dry** when select.py selects no book, or every selected book's docket is `[]`. Stop at a dry cycle
   or at 10 cycles.

## One cycle

Work dir: a scratch folder (never the repo). `books.txt` there lists the catalog slugs, one per line, in wheel order:
atte blst btl bcoda cxc crit dwiace ixs icfi mxg mnemo nxr pxl rtr sprw srzr test tlc twd twu undr vatd
(SCON is excluded: the author's dumping ground, not held to fact).

1. Open an author work order for the cycle under 01a10a85 and name it in every commit and ruling.
2. `powershell -File tools/dump.ps1 -Work <work>` (UTF-8; it warns if a dump is garbled).
3. Law sweep (rule 1) on every book; fix and apply hits first.
4. `python tools/select.py <work>`; the selected books, in wheel order, are this cycle.
5. For each selected book, in order, strictly one at a time:
   - Launch one reader agent with `brief.md` (cycle number, focus slug, work dir, and its select.json positions).
   - Review every row yourself against the quoted beats (`python tools/show.py <slug> <cycle>` from the work dir).
     Reject: rulings re-litigated (institutional devices, Meridian 2208/2211 as two acts, hand-painted Seam signs),
     borrowed details not found in the source book, another book's secret or ending, plot-fact rewrites, timeline
     fixes whose arithmetic misreads the prose. About 9% of cycle-1 rows failed review.
   - `powershell -File tools/apply.ps1 -Work <work> -Slug <slug> -Cycle <n> -Order <id> -DryOnly`, then without
     -DryOnly. It archives, splices, tags, captures, re-reads the changed beats, re-dumps, F1-verifies the changed
     beats' entities, and shows read status and law hits.
   - Write the reader's EDGES with link_entities (read back), and add its FLAGS to the cycle's author order.
6. After the last book: `prose --factory status` on every touched book; any F1-red unit gets
   `powershell -File tools/f1.ps1 -Slug <slug> -All` in the foreground (background shells get reaped).
7. Append the cycle to docs/GLMZ_CONSOLIDATION.md, commit your own hunks under the engine order, update the wheel
   memory, end with /quicksave.

## Tags that will not stay off
The save re-derives tags from every name and alias. To keep a surface untagged in one book, record an incidental
ruling for it (`record_ruling kind=incidental pattern=<surface> node=<book>`), then `--retag-name ... --from <id>`.
If the surface is a wrong alias on a record, remove the alias instead. `--pin-name` now names any tag that blocks it.
