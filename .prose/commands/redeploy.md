# /redeploy — rebuild every Prose app into the production folder

Rebuilds and republishes **Hub, Writer, KdpPublish and Launcher** to `C:\Apps\MindAttic\Prose\`,
then restarts the Hub so the session is talking to the build you just made.

Use it after changing any C# under `v3/`. The SessionStart hook
(`.prose/hooks/start-prose-hub.ps1`) only ever redeploys **the Hub**, and only when source is newer
than the deployed exe — it deliberately never touches Writer or KdpPublish, because republishing an
exe the author is looking at would stop it mid-edit. `/redeploy` is the explicit "refresh
everything" step.

## Usage

```
/redeploy                      all four apps
/redeploy Hub Writer           only the ones named
```

Valid names: `Hub`, `Writer`, `Launcher`, `KdpPublish` (case-sensitive, as the script validates).

## What to run

From the repository root:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File v3\tools\deploy-apps.ps1
```

with `-Apps Hub,Writer` when the user named a subset.

The script publishes framework-dependent, win-x64, single-file, via `--artifacts-path` so the build
graph never writes into the repo's `bin/obj` — that is what lets it run while `Prose.Mcp.exe` holds
its own DLLs open for the length of the session. **Never kill `Prose.Mcp.exe` to get a build
through; that ends the user's session.**

## Steps

1. **Warn if the Writer is open.** `Get-Process Writer` — republishing stops it and the author
   loses whatever beat they had open. Ask before stopping it; don't just take it down.
2. **Publish.** Run the command above. It stops each target by *executable path* (not just process
   name — `Hub` and `Launcher` are generic enough to match something unrelated), polls up to 10s
   for the file lock to release, then publishes.
3. **Restart the Hub.** The publish stops it and does not start it again:
   ```powershell
   $env:ASPNETCORE_ENVIRONMENT = 'Development'
   Start-Process 'C:\Apps\MindAttic\Prose\Hub.exe' -WorkingDirectory 'C:\Apps\MindAttic\Prose'
   ```
   `ASPNETCORE_ENVIRONMENT` matters: with nothing set, ASP.NET Core defaults to Production, which
   makes `AddMindAtticAuthentication` fail closed and silently drops `--reset-password`.
4. **Verify, don't assume.** A successful publish is not a working Hub:
   ```
   curl -s -o /dev/null -w "%{http_code}" http://127.0.0.1:5900/api/health    # 200
   curl -s -o /dev/null -w "%{http_code}" http://127.0.0.1:5900/app           # 200
   curl -s -o /dev/null -w "%{http_code}" http://127.0.0.1:5900/writer        # 200
   ```
   `/api/health` is fail-closed — it returns 503 when SQL Server is unreachable — so a 200 there
   means genuinely usable, not merely "a process is listening". Report the exe sizes and the three
   status codes.

## Launchers in `C:\Apps\MindAttic\Prose\`

Each one rebuilds from source *before* starting, so you can never be looking at a stale exe — the
same contract as `C:\Apps\KdpPublish\launch.bat`. They share a directory, so they are named for
what they start rather than all being `launch.bat`:

| File | Rebuilds | Starts |
|---|---|---|
| `Writer.bat` | Hub **and** Writer | `Writer.exe` |
| `Hub.bat` | Hub | `Hub.exe` |
| `launch.bat` | all four | `Launcher.exe` |
| `KdpPublish\launch.bat` | KdpPublish | `Prose.KdpPublish.exe` |

`Writer.bat` rebuilding the Hub is not redundant. The entire editor — every Razor component, the
save path, the entity modal — is `Prose.WriterUi`, compiled **into Hub.exe**. `Writer.exe` is only a
WebView2 window pointed at it. Republishing just `Writer.exe` would leave you editing in yesterday's
UI while believing you had redeployed it.

## Gotchas

- **`-Apps` must tolerate however PowerShell mangles it.** `powershell -File script.ps1 -Apps
  Hub,Writer` passes the single *string* `"Hub,Writer"`, and `-Apps Hub Writer` binds only `Hub` and
  silently drops the rest. Every `.bat` above invokes the script through `-File`, so `deploy-apps.ps1`
  splits and validates the value itself instead of using `[ValidateSet]` — which would have rejected
  the first spelling and quietly mis-deployed the second. Don't "simplify" that back to a ValidateSet.

- **The MCP server is not redeployed by this.** `Prose.Mcp.exe` is a separate long-lived process
  launched by the client, running from the repo's `bin\Release`. If its schema is stale, the fix is
  to exit Claude Code and start a new session — the SessionStart `build-prose-mcp` hook rebuilds it
  while it is not running. `/redeploy` cannot fix a stale MCP schema.
- **KdpPublish deploys to its own subfolder** (`C:\Apps\MindAttic\Prose\KdpPublish\`), not flat with
  the others. It ships its own `wwwroot\` and `launch.bat`, both of which would collide with the
  Hub's. Don't "tidy" it into the parent folder.
- **Only the Launcher gets a `launch.bat`** at the root, for the same reason.
- The old `v3\Prose.Hub\tools\deploy.ps1` still works — it forwards here with `-Apps Hub`.
