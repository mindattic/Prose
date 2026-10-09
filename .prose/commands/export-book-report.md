# /export-book-report — write the stored book report to the book's export directory

Usage: `/export-book-report <slug-or-code>` (e.g. `/export-book-report ATTE`); no argument = ask
which book.

Run `prose --universe <u> --export-book-report --slug <slug-or-code>`. It reads the book's single
`BookReports` row (the one `/book-report` last overwrote — it does not regenerate anything) and
writes it, in readable markdown, to **`{book export dir}\{CODE}_BookReport.md`**: the same folder the
book's .docx/.epub/.pdf exports land in. Re-running overwrites the file in place, so the folder
always holds exactly one report. The row's `ExportFilePath` records where it went.

If the book has no stored report the command exits 1 and says so — run `/book-report <code>` first.
The command runs inside the Hub, so a change to it needs a Hub redeploy before it takes effect.
