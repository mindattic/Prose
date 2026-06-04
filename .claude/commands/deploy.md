Deploy StreetSamurai via **MindAttic.Deploy** (sibling repo at `D:\Projects\MindAttic\MindAttic.Deploy`). MindAttic.Deploy is the source of truth for every MindAttic deploy; this command shims into it.

The deploy fires the project's existing GitHub Actions workflow (`azure-deploy.yml`) by committing the synced wwwroot and pushing master. The workflow is a **three-stage pipeline — build → migrate → deploy**: `migrate` runs `v3/ApplyMigrations` against Azure SQL (idempotent `Data/Sql/*.sql` column migrations + nested-strand fold + `EnableSystemVersioningAsync()`, which turns on SYSTEM_VERSIONING for Beats/Strands/StrandBeats and the canon temporal set), authenticated via the GitHub OIDC service principal (`db_ddladmin`); `deploy` then ships the artifact to the `streetsamurai` Azure App Service (Production slot). The App Service managed identity stays read/write only — schema changes ride the OIDC principal.

Run this command and report the result:

```
powershell -NoProfile -ExecutionPolicy Bypass -Command "cd D:\Projects\MindAttic\MindAttic.Deploy; npm run deploy -- --app streetsamurai"
```

It will:

1. `git pull` the sibling `MindAttic.UiUx` repo (hard-fail if dirty).
2. Run `MindAttic.UiUx/sync/sync-streetsamurai.ps1` to splice latest components into `v3/StreetSamurai.Blazor/wwwroot/`.
3. `dotnet build v3/StreetSamurai.Blazor -c Release` to catch compile errors locally before pushing.
4. `git add v3/StreetSamurai.Blazor/wwwroot`.
5. If anything staged: `git commit -m "Sync MindAttic.UiUx for deploy (UTC)"`.
6. `git push origin master` — Azure CI/CD fires automatically: **build → migrate → deploy**. The `migrate` stage applies schema + enables temporal versioning against Azure SQL via the OIDC service principal before `deploy` ships the app.
7. Print the Actions URL for monitoring: <https://github.com/mindattic/StreetSamurai/actions/workflows/azure-deploy.yml>.

After running, summarize: which steps ran, what was committed/pushed (or that there were no changes), and the Actions URL.

Notes:
- For a no-push rehearsal (sync + build only), append `--dry-run`: `npm run deploy -- --app streetsamurai --dry-run`. That stages the sync changes but skips the commit + push.
- The legacy `scripts/cli/deploy.ps1` in this repo is dead code -- do not invoke it directly.
- App profile lives in `MindAttic.Deploy/projects.json` under `apps[]` slug `streetsamurai`.
