# RA-S5.1.1 · An escaped `\*` stays a literal asterisk everywhere

- **Level:** story
- **Status:** open
- **Parent:** RA-F5.1 · Literal asterisks (MindAttic.Export)
- **Acceptance:** `ProseInline.Parse("a \\*b\\* c")` yields plain text `a *b* c`. Imported text with a real asterisk is escaped on the way in.

## Children (build in this order)

1. RA-T5.1.1.1 · Escape support in `ProseInline` (open)
2. RA-T5.1.1.2 · Import escapes literal asterisks (open)
