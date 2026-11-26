# Prose deploy layout: joining the shared run menu (PRS-T1132)

Spike note, 2026-11-26. Azure is decommissioned (MAT-T297); Prose's data is local LocalDB only (`ConnectionStrings__Prose`).

## Findings

1. **run.ps1 does not deploy to Azure.** Its 566 lines publish three WPF/web apps (Hub, Writer, Launcher) framework-dependent, single-file, into one flat folder, `C:\Apps\MindAttic\Prose\`, and write a `run.bat` there with its own two-item menu (Deploy & Run / Run). The Hub binds 127.0.0.1 and run.ps1 says it is "never the Azure deployment".
2. **The Azure pipeline is separate**: `.github/workflows/azure-deploy.yml` (manual, App Service `mindattic-prose`, GitHub OIDC). It builds `src/Prose.Hub` itself and never calls run.ps1. Since Azure is gone it is dead, and so is the Hetzner target in MAT-T221 as far as run.ps1 goes: a Hetzner pipeline would also be its own workflow, so it does not constrain the local layout.
3. **The shared run menu** (`deploy-lib.ps1`: `Invoke-AppRun`, `Publish-AppConfig`, template `run.ps1`) deploys to `R:\MindAttic\Apps\<repo>\Debug` (framework-dependent) and `\Release` (standalone), found by `Get-Plan` (`.agent\publish.json`, else detected Exe/WinExe/Web projects, else a run.ps1 taking `-Out`). Prose's run.ps1 takes no `-Out`, and the plain detection would find many more exe projects (Cli, Mcp, V4.Cli, ...) than the three desktop apps, so an explicit plan is needed.
4. **Hard-coded `C:\Apps\MindAttic\Prose` paths** that would break on a move:
   - `src/Prose.Writer/HubProcess.cs` (Hub.exe lookup, error text; also compiled into Launcher)
   - `src/tools/launch-app.ps1`, `src/tools/install-shortcuts.ps1`
   - `src/Prose.Hub/tools/run.ps1` (comments), `.prose/hooks/start-prose-hub.ps1`, `.prose/hooks/factory-start.ps1`
   - `.agent/commands/{launch,redeploy,kdp-publish}.md`, `.claude/skills/{launch,redeploy}/SKILL.md`, README.md, src/README.md, docs/CONDUIT.md
   - KdpPublish (own repo) deploys to `R:\MindAttic\Apps\KdpPublish\{Debug,Release}`; the Launcher's `KdpPublishDir` points at the Release copy (MAT-T309).
5. **Flat layout constraint.** Hub, Writer and Launcher share one folder: Hub owns `wwwroot\`, Writer and Launcher find `Hub.exe` beside them. The shared `Publish-DotNet` already publishes several projects into one folder, but Hub is a Web project (Release = self-contained, not single-file) while Writer/Launcher are single-file; in one folder this works but the Hub is long-lived and Writer/Launcher must not stop it when only they redeploy (run.ps1's per-app stop, change detection and `-Start` semantics). `Publish-AppConfig` stops everything running from the target and swaps the whole folder, which would kill a running Hub on every Writer redeploy.

## Recommendation

Prose should join the shared run menu, but via the override mechanism, not by adopting the template wholesale.

- Add `.agent/publish.json` (`kind: dotnet`, projects Hub, Writer, Launcher) and `.agent/run.json` (`Exe: Launcher.exe`; Writer and Hub reachable through the Launcher) so Redeploy builds Debug and Release into `R:\MindAttic\Apps\Prose\{Debug,Release}`.
- Keep run.ps1's per-app change detection, Hub health check and `-Start` as a thin script behind `PreBuild`/`PostBuild`, or reduce run.ps1 to the standard template and accept a full-folder swap (Hub restarts on redeploy) once the Hub health check lives in a `PostBuild`.
- Make the apps locate each other relative to their own folder (`AppContext.BaseDirectory`) first, with the R:\ default path second, so Debug and Release copies each use their own Hub.
- Both configs run the Hub on 127.0.0.1 and the same LocalDB, so Debug and Release must not both run a Hub at once: pick a different port or refuse to start a second.

## Azure leftovers found in Prose (for removal)

- `.github/workflows/azure-deploy.yml` (whole file, App Service, OIDC, repo secrets AZURE_*).
- `scripts/gspl_db.ps1`: opens Azure SQL `sql-mindattic-f65744.database.windows.net` via Entra tokens / `providers.json` AzureSql; must point at LocalDB.
- `.prose/hooks/start-prose-hub.ps1` (comment naming prose.azurewebsites.net), `run.ps1` comments ("never the Azure deployment").
- `src/Prose.Core/Data/Sql/delete_captured_gear_cluster_jsons.ps1` and `engine/data/graph/*_graph.json` mention azure (check whether they are real references).
- Hub "Hosting:Mode=Public" code paths and App Service settings docs (`src/Prose.Hub/Hosting/HubHosting.cs`) exist for the Azure host; keep only if Hetzner (MAT-T221) still wants a public mode.

## Follow-up tickets

1. Prose: `.agent/publish.json` + `.agent/run.json` so the shared menu deploys Hub/Writer/Launcher to R:\MindAttic\Apps\Prose\{Debug,Release}.
2. Prose: apps find sibling exes relative to their own folder; retire the C:\Apps\MindAttic\Prose paths (HubProcess.cs, launch-app.ps1, install-shortcuts.ps1, hooks, skills, docs).
3. Prose: decide run.ps1 fate (thin wrapper around deploy-lib with Hub health check in PostBuild) and stop a Writer redeploy from killing the Hub.
4. Prose: remove Azure leftovers (list above); repoint scripts/gspl_db.ps1 at LocalDB.
5. Prose: Debug/Release Hub coexistence (port or single-instance rule).
