---
description: Build the KDP manifest, work out which books are new-and-ready or stale on Amazon, confirm with the user, then drive KdpPublish to auto-publish/republish them.
argument-hint: "[--dry-run] [CODE1,CODE2,... to restrict to specific books]"
allowed-tools: Bash, PowerShell, Read, Grep, Glob, AskUserQuestion, Monitor
---

# /kdp-publish — automate a KdpPublish run

**What this does**: rebuilds the KDP manifest (the reconciliation of DB + disk + KDP dashboard
state), classifies every tracked book into "new listing ready", "needs republish (stale)", "blocked
(missing prerequisite)", or "already current", gets explicit human confirmation (this drives a real,
externally-visible change on the live Amazon KDP dashboard and burns real Anthropic API usage), then
launches `Prose.KdpPublish.exe` with exactly the codes that need action.

This is **not** a fire-and-forget CLI job — `Prose.KdpPublish.exe` is a WPF/WebView2 app whose
`KdpOperatorService` drives an Anthropic tool-use agent loop against a live browser pane per book.
It is real, it costs real tokens, and a past run (2026-08-16) failed 100% across 19 books in ~4
seconds each because the Anthropic account had hit its usage cap — check whether that or any
similar standing block is still in effect before spending the confirmation on a run that can't
succeed.

## Step 0 — rebuild the manifest fresh

Never trust a stale `tools/kdp/manifest.json` or the DB's `PublicationStatus` column directly (it
was never backfilled and `prose --kdp-status` is effectively dead — it filters on that column and
silently returns zero rows). Rebuild live:

```
dotnet run --project v3/Prose.Cli -- --kdp-manifest
```

This reconciles DB + the export folders + `tools/kdp/title-ids.json` and writes
`tools/kdp/manifest.json` (camelCase JSON, one entry per tracked book). Read that file.

## Step 1 — classify every entry

For each entry in `manifest.json`, sort into:

- **Republish (stale)** — `needsRepublish == true`. This already encodes the full hard gate
  (`.publish` marker + `cover.jpg` + `description.txt` on disk) plus "the current on-disk `.epub`
  version is newer than what the local `.publish` marker last confirmed as actually published."
  These are live on Amazon and out of date.
- **New listing (ready)** — `meetsHardPublishGate == true` AND `publishUrl == null` AND
  `newListingPlan != null`. Never published before, hard gate passed, and a
  `kdp.newbook.<CODE>` plan (price/categories/DRM/KDP Select/AI-disclosure answers) is already
  configured via `SettingsKvStore`.
- **Blocked — no listing plan** — `meetsHardPublishGate == true` AND `publishUrl == null` AND
  `newListingPlan == null`. Hard gate passed but nobody has authored the one-time first-publish
  metadata yet. **Do not include these in the run** — they will fail with "no first-time-publish
  plan configured." Report them separately so the user knows what's blocking them.
- **Work in progress** — `meetsHardPublishGate == false` (missing `.publish` marker, cover, or
  description, or no `.epub` on disk at all). Not eligible. Worth a one-line mention only if the
  `warning` field says something actionable (e.g. "run `prose --export-node`").
- **Already current** — `publishUrl != null`, `needsRepublish == false`. Nothing to do.

If `$ARGUMENTS` restricts to specific codes (comma-separated), filter the Republish/New-listing sets
down to just those codes before proceeding — ignore the restriction if it names a code that isn't in
either set (report why: already current, blocked, or WIP).

## Step 2 — report and confirm

Print a short table: code, title, classification, and (for Republish) current vs. on-disk version.
Then, **unless invoked with `--dry-run`** (in which case stop here and just report the classification
— do not launch anything), use `AskUserQuestion` to confirm the exact list of codes about to be run.
This is a hard-to-reverse, externally-visible, real-money-adjacent action — do not skip the
confirmation gate even if the user's original ask was "automatically start and publish everything."
Include in the question: the codes, the count, and a one-line reminder that this costs live Anthropic
API usage per book and will make real changes on Amazon's dashboard.

If the combined Republish + New-listing set is empty, report that and stop — nothing to confirm.

## Step 3 — redeploy fresh, then launch

Only after confirmation. First redeploy from source to avoid ever running a stale build (this bit a
past session — a multi-day-stale `Prose.KdpPublish.exe` silently missing a whole feature):

```
powershell -ExecutionPolicy Bypass -File v3\Prose.KdpPublish\tools\deploy.ps1
```

Then launch with the confirmed comma-separated codes as `argv[0]` — this drives the exact same
`RunSelectedAsync` automation the app's own "Start" button triggers, no separate/lesser code path:

```
Start-Process -FilePath "C:\Apps\KdpPublish\Prose.KdpPublish.exe" -ArgumentList "CODE1,CODE2,CODE3"
```

This opens a real WPF window + WebView2 pane and does not block the shell — it needs an interactive
Windows desktop session (this machine, not a headless box) since it drives live browser automation.

## Step 4 — verify against ground truth, not the DB

The app's own status is not observable from here. After launching, wait (the app processes books
sequentially and each one is a real multi-step browser interaction — minutes each, not seconds), then
verify by re-running `dotnet run --project v3/Prose.Cli -- --kdp-manifest` and/or reading each
target book's `<exportFolder>/<CODE>/.publish` marker JSON directly
(`{"File":...,"Asin":...,"PublishedAtUtc":...,"Version":...}`) — a marker is only ever written after
a genuine confirmed-publish modal, never speculatively, so it's the trustworthy signal, not
`Nodes.PublishUrl`/`KdpPublishedAt` alone.

**Watch for the known failure signature**: if every targeted book fails within seconds of launch
with no browser interaction ever happening, that is very likely the Anthropic account hitting a
usage cap (exact past error: `Anthropic API 400: "You have reached your specified API usage
limits."`) — this is a safe no-op (nothing touches the live KDP dashboard before the first API call
succeeds), not a destructive failure, but it means **stop and report it rather than retrying** — a
usage-cap block doesn't clear by retrying, it clears at whatever reset time the account reports.

Report back per code: Published (marker updated, version matches), still Outdated/blocked (no
marker change — note the likely cause if visible), or unknown/needs a manual look (e.g. VATD's
known intermittent `find_and_open_book` flakiness, or a "no first-time-publish plan configured"
failure despite the plan appearing present in `SettingsKvStore` — both are recognized open issues,
not new bugs to chase blindly).

## Argument handling

- No arguments: run the full classification across every tracked book.
- Comma-separated codes: restrict the Republish/New-listing sets to just those codes (see Step 1).
- `--dry-run`: do Steps 0–1 and the report half of Step 2, then stop — never launch the app, never
  ask for confirmation to launch.
