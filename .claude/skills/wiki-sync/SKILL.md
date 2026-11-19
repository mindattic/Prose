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
- On change: the Scheduler job `Prose.GLMZ.Wiki.Sync` runs `tools\wiki\wiki-watch.ps1` every 5 minutes. The Hub
  stamps `%LOCALAPPDATA%\MindAttic\Prose\wiki\changes\<universe>.stamp` after every save that changes what the wiki
  shows; a person's wiki edit shows up in one API call. Prose changed -> once writing is quiet 5 minutes (at most an
  hour) it reconciles, then syncs. The wiki changed -> it reconciles. A full sync runs at least daily. Logs:
  `watch.log`, `sync.log`.
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
