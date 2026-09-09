# Export retention

Every book export writes the newest artifact bundle to its normal per-book folder. Before the
write, the existing bundle is copied into that book's `Archives\v<N>\` directory, where `<N>` is
the version parsed from the artifact filename (or the node's current version for metadata without
a version in its name). This applies to DOCX, EPUB, PDF, TXT, Markdown, HTML, JSON, descriptions,
synopses, keywords, and cover images.

The live folder is staging for the newest export. Previous versions are never pruned by an export;
the archive is permanent and belongs to that book. The current `cover.jpg` remains in the live
folder after archiving so exports with automatic cover generation disabled do not lose author art.
