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

- Default universe: `glmz`. Its canon books are `tools\wiki\glmz.books.json` (`{slug: {title, code, order}}`, order =
  story order); its wiki login is the Vault bucket `%APPDATA%\MindAttic\Wiki\glmz.json`. Optional per universe:
  `<slug>.intro.wiki` (the main page's introduction) and `<slug>.logo.png`. Another universe needs the books file and
  the login before its first sync, plus a MediaWiki with `tools\wiki\mediawiki\LocalSettings.prose2wiki.php` appended
  (uploads, the Book namespace, display titles).
- Nightly: the Scheduler job `Prose.GLMZ.Wiki.Sync` (00:30) runs it; its last log is `sync.log` in the state folder.
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
