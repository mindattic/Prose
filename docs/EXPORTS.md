# Export retention

Every book export writes the newest artifact bundle to its normal per-book folder. Before the
write, the existing bundle is copied into that book's `Archives\v<N>\` directory, where `<N>` is
the version parsed from the artifact filename (or the node's current version for metadata without
a version in its name). This applies to DOCX, EPUB, PDF, TXT, Markdown, HTML, JSON, descriptions,
synopses, keywords, and cover images.

The live folder is staging for the newest export. Previous versions are never pruned by an export;
the archive is permanent and belongs to that book. The current `cover.jpg` remains in the live
folder after archiving so exports with automatic cover generation disabled do not lose author art.

# Where rendering lives

Rendering is implemented once, in the shared **MindAttic.Export** library
(github.com/mindattic/MindAttic.Export), consumed as a package from `lib/local-packages`. Prose keeps
everything that needs the database — the read gate, the beat walk and chapter spine, the glossary,
`ExportPathResolver`, the version bump, the press record and the `ArchivedBooks` snapshot — and hands
a renderer-neutral manuscript to the library's docx, epub, pdf, txt and Markdown renderers. The
archive pass above (`ExportCleanupService`), path sanitising and `ProseInline` are the library's too.

- `prose --export-node --slug X --preview <dir>` renders the book's current version into a scratch
  folder, read-only and reproducibly (fixed EPUB id/timestamp and PDF metadata). The read gate still
  applies: an unread book previews its Markdown only. Use it to verify any renderer change against
  the whole corpus before pressing; the 2026-10-08 migration was verified this way on all 36 GLMZ
  books.
- `prose --export-book-report --slug X [--formats md,txt,docx,pdf]` writes `{CODE}_BookReport.md`
  as before and renders the same report as `.txt`, `.docx` and `.pdf` beside it (Letter document
  style). Report files are overwritten in place, as the `.md` always was.
