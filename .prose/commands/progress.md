# /progress — Strand Progress Dashboard

Show a dashboard table of all non-archived strands (books) with their Code, Title, Kind, Status,
Score, and estimated Pages.

## Instructions

Run:

```
prose --progress
```

This is a Hub-routed command (nothing reaches the database except through Prose.Hub, reads
included) that queries the current Book/Chapter/Beat model directly: every non-archived
`BookNode`, its `NodeCode`/`Title`/`Kind`/`Status`, and a page estimate (`words / 250`) computed by
walking each book's leaf-descendant chapters' beats. Cross-universe by design — this is a
dashboard of every book, not one universe's.

Output is already sorted (score descending, unscored last) and already omits stub rows (0 pages,
no score) unless doing so would leave fewer than 10 rows — relay the table as printed, or pass
`--json` first and reformat if the user wants a different rendering.

**Note on history:** this command previously ran a raw `sqlcmd` query against `Strands`/
`StrandBeats`/`StrandReviewSummaries` — tables that predate the current Book/Chapter/Beat model
and no longer exist. `prose --progress` (`v3/Prose.Cli/Cli/ProgressCli.cs`) is the real
replacement against the live schema, not a resurrection of the old query.
