---
layer: release
status: living
---
# Prose — Release charter

> The one finish line this repo is working toward. Every open ticket in this repo sits under one of the epics
> below, and every epic is part of the milestone. Work that does not fit is out of scope until this milestone is
> reached (owner, 2026-11-27, MAT-T319).

## 1. What this is
A local, canon-grounded narrative engine (SQL Server canon, the Hub, the Writer, a CLI and an MCP server) that
takes books through the Novel Factory's gates, plus the canon of its universes; GLMZ is the flagship universe.

## 2. The milestone: Prose 1.0, GLMZ Season 1 pressed {#milestone}
- **Milestone:** MS-22, "Prose 1.0: GLMZ Season 1 pressed on a local engine"
- **Published means:** an internal tool deployed by the shared run menu to `R:\MindAttic\Apps\Prose\{Debug,Release}`
  (Hub, Writer, Launcher) on the local `Prose` database; its output is the GLMZ Season 1 books, exported in every
  format and readable on the GLMZ wiki (<https://mindattic.com/glmz/>, site repo `Prose.Wiki.GLMZ`), with KDP
  publishing at the author's choice.
- **The natural stopping point:** the engine runs entirely on the local machine with no Azure left in it, deploys
  like every other MindAttic app, and has proved itself by carrying the whole GLMZ canon catalog through the
  factory: every canon book tagged, contributed, read (F5), its entities verified (F1) and pressed (F7), with the
  owner-approved weaves and known canon contradictions fixed and the wiki in step. That is a finished engine with a
  finished season behind it, and the right place to stop before any new book, new lore pillar or new system.

## 3. Done when (acceptance checks)
Each line is also a `tickets milestone check`.
1. build — `dotnet build src\Prose.slnx` builds clean on main.
2. exists — `R:\MindAttic\Apps\Prose\Release\Hub.exe` (deployed by the shared run menu) and no Azure leftovers
   (`.github/workflows/azure-deploy.yml` gone).
3. harness — `prose --factory status` shows F1–F7 green for every GLMZ canon book in `Prose.Wiki.GLMZ\glmz.books.json`.
4. signoff — the owner reads the Season 1 books on the wiki and decides the KDP exports (PRS-T792).

## 4. Epics, in priority order
| Priority | Epic (ticket) | What it delivers | Features |
|---|---|---|---|
| 900 | PRS-T1272 Local engine and deploy | No Azure left; shared run-menu deploy; known engine and KDP bugs fixed | PRS-T1276 Retire Azure, run on LocalDB (990), PRS-T1277 Shared run-menu deploy (980), PRS-T1278 Engine and KDP bug fixes (970) |
| 800 | PRS-T1273 GLMZ catalog contributions | Every canon book tagged, contributed, F5-read and F1-verified | PRS-T1279 In-flight book campaigns (890), PRS-T1280 Remaining catalog campaigns (880), PRS-T1281 Tagging close-out (870) |
| 700 | PRS-T1274 Approved weaves and canon fixes | The approved biomass/traffic weave finished in the DB; known canon breaks fixed | PRS-T1282 Weave close-out (790), PRS-T1283 Canon corrections (780) |
| 600 | PRS-T1275 Season 1 press and wiki | Final re-read, F1 recommit, F7 export of every pressed book; owner's KDP call; wiki synced | PRS-T1284 Final re-read and export (690), PRS-T1285 Wiki in step with Season 1 (680) |
| 500 | GW-T6 GLMZ wiki carries Season 1 (repo Prose.Wiki.GLMZ) | Site docs current, canon list complete, live check | GW-T7 Site docs current (590), GW-T8 Canon list and live check (580) |

The remaining per-book campaigns (PRS-T1137–T1154) are split into chapter units when each is started, as IxS
(PRS-T1210), BTL (PRS-T1155) and ICFI (PRS-T1156) were.

## 5. Out of scope (not before this milestone)
New requests that land here are filed `proposed`, never approved automatically.
- Hosting Prose anywhere (Azure is gone; Hetzner, App Service, a hosted Hub with login) — Prose is local-only by
  author decision (2026-08-23); the Hetzner DB and scheduler move stays with the workspace plan (MAT-T221) and
  comes back only after this milestone.
- The game conduit and game-text kinds for GridGame and SpaceGod (PRS-T917) — a second product; it waits until
  the book engine is finished.
- New lore pillars and new books: Schism passages (PRS-T943/T974), the bestiary overhaul (PRS-T987/T993), weaving
  KTFOW background into every book, the Veiled Ones/Sojourner rename, the imperial-first measurement rule, the
  AI→SI terminology sweep and a 23-book book-report pass.
- Other universes' campaigns (Gospel, nonfiction, Scry) and the non-canon GLMZ collections (The Voice You Trust,
  GLMZ Stories Vol. 1).
- Hub cosmetics (tray icon, hidden console), the export-folder restructure and web publishing of exports.
- A multi-universe wiki watcher (PRS-T1077) — there is one wiki.
- Open-ended bug hunts — real bugs are filed one by one as fix tickets.

## 6. Current verified state
- Local-only engine: no hosted deployment (README, 2026-08-23); the run.ps1 deploy spike is in `docs/DEPLOY_LAYOUT.md`
  (93cb7bb7e).
- The Novel Factory line (read gate, splice, rulings, capture, entity verification, work orders) with tests
  (`ReadGateTests`, `BeatSpliceTests`); BIBLE §6.
- Every unread beat in the 23 canon books plus Babineaux's Guide read and receipted (6fb70e021, PRS-T1032); read
  findings resolved (8ba7a6b31).
- The GLMZ wiki: every canon book as Book: pages, chapters, covers, downloads, Amazon links (654d21b31, 1fd4e0a61),
  sync on change and reconciliation (81c4e6295, 2491f7612), timeline and chronology (2b543059b, 055ba2d27; wiki
  45b5a8d), spoiler tags (28f89716a).
- The approved biomass and traffic weaves applied to 17 beats in 11 books (786d47d32); the canon docs for both are
  committed, their entity scripts staged in `tools/canon/`.
- Contributions campaigns finished for the five factory books and in progress for IxS, BTL and ICFI.

## 7. Decision record
- 2026-11-27 — charter drafted from README/BIBLE/RFC 0015/DEPLOY_LAYOUT/tickets (PRS-T1271); Legion vote: Approve,
  9/9 voters, plurality quorum reached, consensus 1.0; no dissent.
- Triage: 143 open → done 10, duplicate 9, wont-do 56, kept 68; 2 new tasks (PRS-T1286 README on LocalDB,
  PRS-T1287 full wiki sync) plus the tree (4 epics, 10 features). Tickets being worked (PRS-T1167, T1168, T1215,
  T1263, T1264, T1267) were only re-parented. PRS-T1271 was the triage job itself.
- The Hetzner move tickets (PRS-T1126–T1131) were closed wont-do: Prose is local-only. If the owner still wants the
  Prose DB on the Hetzner server, refile it under MAT-T221 after this milestone.
