---
name: wiki-sync
description: Sync a Prose universe into its MediaWiki (GLMZ -> https://mindattic.com/glmz/) - every entity, relationship and canon book (readable chapter by chapter, with covers and downloads) - by running tools/wiki/sync-wiki.ps1. Runs nightly at 00:30 as the Scheduler job Prose.GLMZ.Wiki.Sync. Usage /wiki-sync [universe] [--retag] [--no-push].
---

# Wiki sync

One command rebuilds a universe's wiki from Prose and pushes only what changed:

```powershell
cd D:\Projects\MindAttic\Prose
powershell -NoProfile -ExecutionPolicy Bypass -File tools\wiki\sync-wiki.ps1 -Universe glmz
```

- Default universe: `glmz`. The engine is here (`tools\wiki`); each wiki's **site** is its own repo beside Prose,
  `<UNIVERSE>.Wiki` (GLMZ: `D:\Projects\MindAttic\GLMZ.Wiki`): `<slug>.books.json` (`{slug: {title, code, order,
  series}}` + `_series`, order = story order), `<slug>.intro.wiki`, `<slug>.logo.png`, `theme\`, the managed
  `mediawiki\LocalSettings.<slug>.php` block, and `deploy.ps1`, which puts theme/settings/logo live and checks them.
  The wiki login is the Vault bucket `%APPDATA%\MindAttic\Wiki\<slug>.json`. A new universe's wiki starts as a copy of
  GLMZ.Wiki with its own books file and login.
- By push: the Scheduler job `Prose.GLMZ.Wiki.Sync` keeps `tools\wiki\wiki-watch.ps1 -Daemon` running (at logon,
  restarted if it dies). The Hub stamps `%LOCALAPPDATA%\MindAttic\Prose\wiki\changes\<universe>.stamp` after every save
  that changes what the wiki shows, which wakes the watcher within seconds; it looks at the wiki for people's edits
  every minute (the host cannot call this PC). Prose changed -> once writing is quiet 2 minutes (at most 30) it
  reconciles, then syncs. The wiki changed -> it reconciles. A full sync runs at least daily. Logs: `watch.log`,
  `sync.log`. Without -Daemon the script runs one tick.
- Story checks (`tools\wiki\wikistory.py`): `Book:<T>/Plants` (registered plants and payoffs by chapter),
  `Book:<T>/Pacing` (chapter lengths, first appearances, plants set/paid), `GLMZ Wiki:Continuity` (the continuity
  store's open contradictions), and `File:GLMZ graph.json`, the shared graph: every entity, book and chapter with
  its edges (relations, relationships, appearances, plants, contradictions).
- Spoilers: text that refers to a book (an entry's paragraphs and list items, a status field as of the entity's last
  book, a whole Plants page) is wrapped `<span|div class="prose-spoiler" data-book="<slug>">`; `{{Spoiler|book=...}}`
  for editors. The theme hides them until the reader ticks the book in "Books I've read" (localStorage JSON for
  visitors, the `userjs-prose-read` option for users); `MediaWiki:Prose-books.json` is that list.
- Re-reading for the read gate: `--read-beats ... --mark-read --read-by claude` after actually reading; contradictions
  go in `--read-note add --kind defect` and the beat stays unread. Writing an entity's fields un-reads every beat that
  mentions it, so batch record edits before a re-read.
- Reconcile (`tools\wiki\wikireconcile.py`): Prose checks the wiki (every built page live and unchanged, by revision
  sha1, NFC-normalized), the wiki checks Prose (MediaWiki's Wantedpages / Lonelypages / Deadendpages; shared scenes in
  the books vs recorded relationships; one-sided relationships), and edits people make on the wiki become field
  proposals with the page held from the sync. Report: `out\reconcile.md` and the wiki page `Project:Reconciliation`.
  `wikireconcile.py decide <id> --accept|--reject [--type KIND]`, then `wikireconcile.py apply` writes accepted ones
  through the Hub (`--set-entity-fields`); the next sync carries them back. `check --tidy` deletes pages the sync made
  that Prose no longer builds (never a person's page).
- Series: `_series` in the books file ([{name, about}], shelf order) and `"series"` on each book; the main page
  shows a shelf per series, each book page names its series, each series has a category page.
- Every wiki change goes live when made (API for pages/CSS/JS, FTPS for LocalSettings/assets), then is verified.
- The admin account shows as `MindAttic` (renamed from the original login; the Vault file has the current name).
- `--retag` -> `-Retag`: re-run `--tag-entity-text` (dry run) so entity text links to what it names. It reads every
  entity, which is slow on Azure SQL and spends the serverless free-offer allowance; run it after large entity edits,
  not every sync.
- `--no-push` -> `-NoPush`: build and write the validation report only.
- Long run (minutes; the first full push of a new universe takes hours): start it detached, e.g.
  `Invoke-CimMethod Win32_Process -MethodName Create` on a `.cmd` that runs the line above with output to a log, so a
  CLI shell ending does not kill it, then read the log.

What a run does: Hub export of every entity, the interchange file (edges), each canon book's beats, a
`--export-node --preview` of each book whose text changed (all formats, into the state folder; the publish folder is
untouched) plus its cover from the publish folder, then `tools\wiki\prose2wiki.py` uploads changed covers and files,
builds the entity pages, `Book:<Title>` pages and their chapters `Book:<Title>/<n>` (named mentions are links; zen
reading mode in dark and light, `?zen=light` in a link opens it), the main page, pushes changed ones, deletes pages of entities that no longer exist,
redirects renamed ones, purges, and writes `repair.md`/`repair.json` to `%LOCALAPPDATA%\MindAttic\Prose\wiki\<slug>\out\`.

The sync never changes Prose data. The validation report is the input for repairs, which are separate, ticketed and
done through the Hub (`--set-entity-fields`, `--merge-entity`, `--delete-alias`, ...); then the next sync carries
them to the wiki. Copy the report's lists into `docs\wiki\<NAME>-validation.md` when they change.
