# /launch — start one deployed Prose app

Starts a single app from `C:\Apps\MindAttic\Prose\`, or opens the entity wiki. **It never
publishes** — that is `/redeploy`.

## Usage

```
/launch writer      the book editor (a WebView2 window onto the Hub)
/launch hub         the resident server
/launch kdp         KdpPublish
/launch launcher    the "pick an app" shell
/launch wiki        the entity browser at /repo, in Chrome
```

Case-insensitive. `kdppublish` → `kdp`; `repo`, `repos`, `entities`, `encyclopedia` → `wiki`.

## What to run

From the repository root:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File src\tools\launch-app.ps1 -App writer
```

Add `-Force` to start a second instance of something already running (refused for the Hub).

## Why this does not publish

Publishing has to **stop** a running Hub to overwrite a 70 MB single-file exe. That drops every
CLI/MCP client, ends any in-flight command, and loses the Hub's in-memory state (`DocContextStack`,
`EntityContextStack`, the universe graph). That is far too much to do to someone who only asked to
open a window.

So the two commands are deliberately separate: **`/redeploy` when you want new code, `/launch` when
you want the app.** The cost of that split is that `/launch` could open a stale build, so it never
does so silently — it compares the deployed exe against the newest source file under `src\` and
prints a warning naming the file, then starts the deployed build anyway. Warn, don't rebuild: the
choice stays with the author.

## Behaviour worth knowing

- **The Hub is a singleton.** Only one process can bind `127.0.0.1:5900`; a second would fail on
  startup. `/launch hub` reports the running one and refuses to start another, even with `-Force`.
  To replace it with new code, use `/redeploy Hub`.
- **`wiki` is a page, not a process.** The entity browser is `Prose.WriterUi` compiled into
  `Hub.exe` and rendered live from SQL — there is no wiki executable and never will be. The target
  makes sure the Hub is healthy (starting it if needed), then opens Chrome at
  `http://127.0.0.1:5900/repo`.
- **`ASPNETCORE_ENVIRONMENT=Development` is set before starting anything**, not just the Hub:
  Writer and Launcher each start the Hub themselves when it is not up, and a child process inherits
  it. With nothing set, ASP.NET Core defaults to Production, which makes
  `AddMindAtticAuthentication` fail closed.
- **Readiness is polled, not assumed.** After starting Hub / Writer / Launcher the script waits for
  `/api/health` to answer 200. That endpoint is fail-closed — 503 when SQL Server is unreachable —
  so a 200 means genuinely usable, not merely "a process is listening".
- **Already-running processes are matched by executable path**, not just name: `Hub` and `Launcher`
  are generic enough to collide with something unrelated, and this decides whether to start a
  second copy.

## Gotchas

- **An app that was never deployed cannot be launched.** The script says so and points at
  `/redeploy <App>` rather than failing obscurely.
- **This does not restart anything.** If Writer is already open, `/launch writer` reports it rather
  than opening a second window; pass `-Force` if a second really is wanted.
- **Related:** `/run` is a separate, older command that starts `src/Prose.Writer` with
  `dotnet run --launch-profile http` on port 5200. That describes a web host Prose.Writer no longer
  is — it became a WPF + WebView2 shell onto the Hub's 5900 on 2026-09-11. Prefer `/launch writer`.
