Deploy StreetSamurai. Two-phase: first sync subscribed components from the sibling `MindAttic.UiUx` repo into `v3/StreetSamurai.Blazor/wwwroot/`, then verify a local Release build, then (with explicit opt-in) commit the synced changes and push to master so GitHub Actions can run the Azure App Service deploy.

Default (sync + build, no push) — safe to run any time, no production impact:

```
powershell -NoProfile -ExecutionPolicy Bypass -File "D:\Projects\MindAttic\StreetSamurai\scripts\cli\deploy.ps1"
```

Full deploy (sync + build + commit + push master, triggers production Azure CI/CD):

```
powershell -NoProfile -ExecutionPolicy Bypass -File "D:\Projects\MindAttic\StreetSamurai\scripts\cli\deploy.ps1" -Push
```

Note: do NOT invoke via `cmd /c "D:/.../deploy.bat"` -- the forward slashes in the path get parsed as cmd switches (cmd uses `/` as its switch prefix), so the command silently opens a fresh shell in the directory and exits without running anything. Call deploy.ps1 directly via PowerShell as shown above.

The script:

1. Runs `MindAttic.UiUx/sync/sync-streetsamurai.ps1` to splice the latest subscribed component CSS / JS into `wwwroot/app.css` + `wwwroot/js/`. The csproj already wires this as a `BeforeBuild` MSBuild target, but the explicit step here makes sure the working tree is up to date before any commit happens. Override the components-repo location with the `MINDATTIC_COMPONENTS_ROOT` environment variable.
2. Runs `dotnet build v3/StreetSamurai.Blazor/StreetSamurai.Blazor.csproj -c Release` so any compile errors fail the deploy locally, not on the Azure runner ten minutes later.
3. Reports any uncommitted changes from the sync.
4. With `-Push`: stages `v3/StreetSamurai.Blazor/wwwroot`, commits with a `Sync MindAttic.UiUx for deploy (UTC timestamp)` message, and pushes the current branch. Warns if the branch isn't master (Azure deploy is master-only).

After running, summarize:
- Whether components synced cleanly (or were skipped because the sibling repo was missing — surface that as a warning).
- Whether the Release build succeeded.
- Whether changes were committed + pushed, or just left in the working tree for the user to review.

Flags:
- `-NoSync`  — skip the component pull (useful when iterating on the deploy script itself).
- `-NoBuild` — skip the local build (useful when you've just built and want to push immediately).
- `-Push`    — commit + push (otherwise script is read-only and reversible).
