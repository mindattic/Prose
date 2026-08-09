# Azure SQL Database Deployment Guide

Deploying Prose (a .NET 10 Blazor Server app) to Azure App Service against an Azure SQL Database, authenticated via system-assigned managed identity. This is the canonical Microsoft pattern — no workarounds, no interceptors, no passwords on disk.

**Time to complete:** ~45 minutes if everything works the first time. ~3 hours if you hit the gotchas in the FAQ below.

---

## Architecture

```
        ┌──────────────────────────┐
        │ GitHub Actions           │
        │ build → publish artifact │
        │       → deploy           │ ← OIDC federated identity
        └────────────┬─────────────┘
                     │
                     ▼
        ┌──────────────────────────┐         ┌──────────────────────────┐
        │ Azure App Service        │  AAD    │ Azure SQL Database       │
        │ mindattic-prose  ├─token──►│ Prose            │
        │ (system-assigned MI)     │         │ (AAD-only auth)          │
        │                          │         │                          │
        │ env: ConnectionStrings__ │         │ User: mindattic-prose    │
        │      Prose       │         │   db_datareader          │
        │      = Authentication=   │         │   db_datawriter          │
        │        Active Directory  │         └──────────────────────────┘
        │        Default           │
        └──────────────────────────┘
```

**Key insight:** the App Service has an identity (managed identity = "ID badge"). The database has that identity as a SQL user. The connection string says "use whatever AAD identity you can find" (`Authentication=Active Directory Default`). `Microsoft.Data.SqlClient` 5.x+ resolves to the MI, gets a token, and connects. No passwords anywhere.

---

## Prerequisites

| Tool | Why | Install |
| --- | --- | --- |
| **Azure CLI** (`az`) | Manage Azure resources from the command line | `winget install --id Microsoft.AzureCLI -e` |
| **GitHub CLI** (`gh`) | Trigger + watch deploys | `winget install --id GitHub.cli -e` |
| **SqlPackage** | Export LocalDB → bacpac, import → Azure SQL | `dotnet tool install -g Microsoft.SqlPackage` |
| **PowerShell 7+** | Multi-line scripts behave better than Windows PowerShell 5.1 | Built-in on Win 11, or `winget install Microsoft.PowerShell` |
| **.NET 10 SDK** | Build the app + run schema migrations | Usually present if you're developing Prose |

Azure resources you should already have:

- A subscription
- An App Service named `mindattic-prose` in resource group `MyApps`
- (Optional) Existing Azure SQL Server — we'll create one if not.

---

## Part 1: Database setup (one-time)

### 1.1 Log in to Azure with MFA-safe device-code flow

```powershell
az login --tenant <YOUR-TENANT-ID> --use-device-code
```

If your account has MFA enabled (which it should), the silent browser flow may fail with `AADSTS50076`. The `--use-device-code` flag opens a real browser tab where MFA prompts work normally.

After login, confirm the right subscription is selected:

```powershell
az account show --query "{name:name, id:id, tenantId:tenantId}" -o table
```

### 1.2 Find the resource group + region your App Service lives in

```powershell
az webapp list --query "[].{name:name, resourceGroup:resourceGroup, location:location}" -o table
```

You want everything new (SQL server, DB) in the same resource group + region as the App Service to avoid cross-region latency + charges.

### 1.3 Get your AAD object id + UPN

These become the SQL server's AAD admin (you, the human).

```powershell
az ad signed-in-user show --query id -o tsv
az ad signed-in-user show --query userPrincipalName -o tsv
```

Drop both values into `infra/azure-sql.parameters.json`:

```jsonc
{
  "parameters": {
    "location":              { "value": "centralus" },              // matches App Service region
    "sqlServerName":         { "value": "prose-sql" },      // becomes <name>.database.windows.net
    "sqlDatabaseName":       { "value": "Prose" },
    "aadAdminLogin":         { "value": "you@tenant.onmicrosoft.com" },
    "aadAdminObjectId":      { "value": "<your AAD object id>" },
    "githubOidcSpObjectId":  { "value": "00000000-0000-0000-0000-000000000000" },  // unused for now
    "sqlDatabaseSku":        { "value": "GP_S_Gen5_2" },            // Serverless General Purpose
    "sqlDatabaseMaxSizeGB":  { "value": 32 },
    "autoPauseDelayMinutes": { "value": 60 }
  }
}
```

### 1.4 Deploy the SQL server + database via Bicep

```powershell
az deployment group create `
  --resource-group MyApps `
  --template-file infra/azure-sql.bicep `
  --parameters @infra/azure-sql.parameters.json `
  --name prose-sql-initial
```

Takes ~3–5 minutes. Watch for `"provisioningState": "Succeeded"` in the JSON output. The Bicep creates:

- **SQL logical server** (`<name>.database.windows.net`), AAD-only auth, you as the admin.
- **SQL database** (Serverless GP, 0.5–2 vCores, auto-pause after 60 idle min).
- **Firewall rule** `AllowAzureServices` so the App Service can reach the DB.

### 1.5 Enable the App Service managed identity

```powershell
az webapp identity assign --name mindattic-prose --resource-group MyApps
```

Returns a JSON blob — copy the `principalId` for later reference. This identity is what the App Service presents to the SQL database; it has no permissions yet.

### 1.6 Add your laptop's IP to the SQL firewall (one-time)

The `AllowAzureServices` firewall rule lets the App Service in but doesn't include your laptop. To run admin queries, you need a personal exception.

Easiest: open the Azure Portal Query Editor for the database:

```
https://portal.azure.com/#@/resource/subscriptions/<sub-id>/resourceGroups/MyApps/providers/Microsoft.Sql/servers/prose-sql/databases/Prose/queryEditor
```

It'll show a pink banner "Your IP address isn't allowed" with a one-click **Allowlist IP X.X.X.X** button. Click it.

### 1.7 Grant the App Service identity database access

In the Portal Query Editor (or via PowerShell — see § 1.8), run:

```sql
CREATE USER [mindattic-prose] FROM EXTERNAL PROVIDER;
ALTER ROLE db_datareader ADD MEMBER [mindattic-prose];
ALTER ROLE db_datawriter ADD MEMBER [mindattic-prose];

-- Verify:
SELECT name, type_desc FROM sys.database_principals WHERE name = 'mindattic-prose';
```

The name `[mindattic-prose]` must match the App Service name exactly — Azure resolves it to the managed identity by display name. Expected result: one row showing `mindattic-prose — EXTERNAL_USER`.

### 1.8 (Alternative) GRANT via PowerShell + access token

If you don't want to use the Portal:

```powershell
$token = az account get-access-token --resource https://database.windows.net/ --query accessToken -o tsv
$conn = New-Object System.Data.SqlClient.SqlConnection
$conn.ConnectionString = "Server=tcp:prose-sql.database.windows.net,1433;Database=Prose;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
$conn.AccessToken = $token
$conn.Open()
$cmd = $conn.CreateCommand()
$cmd.CommandText = "CREATE USER [mindattic-prose] FROM EXTERNAL PROVIDER; ALTER ROLE db_datareader ADD MEMBER [mindattic-prose]; ALTER ROLE db_datawriter ADD MEMBER [mindattic-prose];"
$cmd.ExecuteNonQuery() | Out-Null
$conn.Close()
Write-Host "Granted."
```

Note the SQL is on one line. Multi-line `@"..."@` here-strings break PowerShell's `>>` continuation prompt mid-paste.

### 1.9 Set the App Service connection string

**Use the Portal, not `az ... appsettings set` — the CLI mangles values containing spaces.** See FAQ.

```
https://portal.azure.com/#@/resource/subscriptions/<sub-id>/resourceGroups/MyApps/providers/Microsoft.Web/sites/mindattic-prose/configuration
```

Find `ConnectionStrings__Prose` (or **add it** if missing). Set the value to:

```
Server=tcp:prose-sql.database.windows.net,1433;Database=Prose;Authentication=Active Directory Default;Encrypt=True;TrustServerCertificate=False;
```

Click **OK** → **Apply** at top → **Continue** on the restart prompt.

### 1.10 Verify the saved value with JSON (not TSV)

```powershell
az webapp config appsettings list `
  --name mindattic-prose --resource-group MyApps `
  --query "[?name=='ConnectionStrings__Prose']" `
  -o json
```

The `value` field should be the **full** connection string ending at `TrustServerCertificate=False;`. If it looks truncated when output via `-o tsv`, ignore that — TSV display lies about values with internal spaces. JSON is the truth.

---

## Part 2: Populate the database

You have two paths depending on whether you have existing local data.

### Path A: Copy your local DB to the cloud (most common)

This brings schema + all rows in one shot.

**Export local LocalDB to a bacpac:**

```powershell
mkdir D:\tmp -ErrorAction SilentlyContinue | Out-Null
sqlpackage `
  /Action:Export `
  /SourceConnectionString:"Server=(localdb)\MSSQLLocalDB;Database=Prose;Trusted_Connection=True;TrustServerCertificate=True;" `
  /TargetFile:"D:\tmp\prose.bacpac" `
  /OverwriteFiles:True
```

Takes 1–3 minutes. If you hit `Error SQL71501: Error validating element [dbo].[vw_Characters]`, see FAQ — there's a stale view referencing dropped columns. Drop it and retry.

**Drop the empty cloud database** (SqlPackage Import only works against a non-existent target):

```powershell
az sql db delete --resource-group MyApps --server prose-sql --name Prose --yes
```

**Import the bacpac** (recreates the DB at the right tier + with all your data):

```powershell
$token = az account get-access-token --resource https://database.windows.net/ --query accessToken -o tsv

sqlpackage `
  /Action:Import `
  /SourceFile:"D:\tmp\prose.bacpac" `
  /TargetServerName:"prose-sql.database.windows.net" `
  /TargetDatabaseName:"Prose" `
  /AccessToken:$token `
  /p:DatabaseEdition=GeneralPurpose `
  /p:DatabaseServiceObjective=GP_S_Gen5_2 `
  /p:DatabaseMaximumSize=32
```

**Don't use the `Authentication=...` connection-string keyword with SqlPackage** — older versions reject it. Pass `/AccessToken:` directly (token from `az account get-access-token`).

**Don't use `/p:DatabaseMaximumSize=34359738368`** (bytes). It's in **GB** — just `32`.

Takes ~10–30 minutes depending on bacpac size + upload speed.

**Re-grant the App Service MI** (dropping the database dropped the user):

```powershell
$token = az account get-access-token --resource https://database.windows.net/ --query accessToken -o tsv
$conn = New-Object System.Data.SqlClient.SqlConnection
$conn.ConnectionString = "Server=tcp:prose-sql.database.windows.net,1433;Database=Prose;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
$conn.AccessToken = $token
$conn.Open()
$cmd = $conn.CreateCommand()
$cmd.CommandText = "CREATE USER [mindattic-prose] FROM EXTERNAL PROVIDER; ALTER ROLE db_datareader ADD MEMBER [mindattic-prose]; ALTER ROLE db_datawriter ADD MEMBER [mindattic-prose];"
$cmd.ExecuteNonQuery() | Out-Null
$conn.Close()
```

### Path B: Start fresh in the cloud

If you don't need to copy local data — just run the schema migrations against the new empty cloud database:

```powershell
$env:ConnectionStrings__Prose = "Server=tcp:prose-sql.database.windows.net,1433;Database=Prose;Authentication=Active Directory Default;Encrypt=True;TrustServerCertificate=False;"
dotnet run --project v3/Prose.Cli -- --migrate-sql --schema
```

`--migrate-sql --schema` applies every EF Core migration under `v3/Prose.Core/Migrations/` and
enables temporal `SYSTEM_VERSIONING`. Idempotent — re-runs are safe. (The standalone
`ApplyMigrations` console app this used to be has been deleted; this is its replacement.)

---

## Part 3: Deploy the app code

### 3.1 Fix the NuGet config (one-time)

Local dev uses `C:\LocalNuGet` for private MindAttic packages (`MindAttic.Legion`, `MindAttic.Vault`). GitHub-hosted runners can't see that folder. Bundle the .nupkg files into the repo:

```powershell
mkdir D:\Projects\MindAttic\Prose\lib\local-packages -Force | Out-Null
Copy-Item C:\LocalNuGet\MindAttic.Legion.<version>.nupkg lib\local-packages\
Copy-Item C:\LocalNuGet\MindAttic.Vault.<version>.nupkg  lib\local-packages\
```

And update `NuGet.config` at the repo root:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="local-packages" value="./lib/local-packages" />
    <add key="nuget.org"      value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
  </packageSources>
</configuration>
```

When you bump a MindAttic package version, drop the new `.nupkg` in `lib/local-packages/` and update the `PackageReference Version=` in the consumer `.csproj`.

### 3.2 GitHub Actions workflow

`.github/workflows/azure-deploy.yml` should be the simple two-stage build → deploy. No `migrate` job unless you've also set up OIDC federation (separate task, see `infra/README.md`).

```yaml
name: Build and deploy Prose to Azure App Service

on:
  push:
    branches: [master]
  workflow_dispatch:

jobs:
  build:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with: { dotnet-version: '10.x' }
      - run: dotnet restore v3/Prose.Codex/Prose.Codex.csproj
      - run: dotnet publish v3/Prose.Codex/Prose.Codex.csproj -c Release -o publish --no-restore
      - uses: actions/upload-artifact@v4
        with: { name: prose-app, path: publish/ }

  deploy:
    runs-on: windows-latest
    needs: build
    permissions: { id-token: write, contents: read }
    steps:
      - uses: azure/login@v2
        with:
          client-id: ${{ secrets.AZURE_CLIENT_ID }}
          tenant-id: ${{ secrets.AZURE_TENANT_ID }}
          subscription-id: ${{ secrets.AZURE_SUBSCRIPTION_ID }}
      - uses: actions/download-artifact@v4
        with: { name: prose-app, path: publish/ }
      - uses: azure/webapps-deploy@v3
        with:
          app-name: mindattic-prose
          slot-name: Production
          package: publish/
```

No `AZURE_WEBAPP_PUBLISH_PROFILE` secret needed — the deploy job logs in via the same OIDC
federated identity as the migrate job (the `prose-github` SP needs `Website Contributor` scoped
to the App Service; granted once via `az role assignment create`).

### 3.3 Push and watch

```powershell
cd D:\Projects\MindAttic\Prose
git push
gh run watch
```

When both jobs show ✓, the new code is on `mindattic-prose.azurewebsites.net`.

### 3.4 Verify the site

```
https://mindattic-prose.azurewebsites.net/
```

First request after a deploy: ~20–40 sec cold start (App Service spin-up + serverless SQL wake-from-pause). Subsequent requests are fast.

---

## Cost expectations

- **Azure SQL Serverless GP_S_Gen5_2** with auto-pause: ~$5–15/month at light usage. ~$30 at moderate traffic. Auto-pause kicks in after 60 idle minutes; first query after pause has a ~10s cold start.
- **App Service**: depends on your plan SKU. See the next section.
- **Egress**: negligible at this scale.

To bias toward cost over latency, increase `autoPauseDelayMinutes` to a smaller number (cheaper) or to `-1` (never pause, no cold-start).

---

## App Service Plan SKU — F1 vs B1+ for Prose

The App Service plan tier matters a LOT for this app, more than typical web apps. The home page alone fires ~26 parallel `COUNT(*)` queries against the cloud DB, the warm-up populates the embedding cache (~10k entities), and the Quorum review pipeline boots eleven LLM providers. Memory footprint after warm-up is 400–700 MB; CPU spikes during page load.

### SKU comparison

| Resource | F1 (Free) | D1 (Shared) | B1 (Basic) | S1 (Standard) | P1v3 (Premium) |
| --- | --- | --- | --- | --- | --- |
| **Cost / month** | $0 | ~$10 | ~$13 | ~$70 | ~$140 |
| **CPU** | Shared, throttled | Shared, throttled | 1 dedicated core (A-series) | 1 dedicated core | 2 cores (Dv4) |
| **RAM** | **1 GB shared, ~256 MB per app** | 1 GB | 1.75 GB | 1.75 GB | 8 GB |
| **Disk** | 1 GB | 1 GB | 10 GB | 50 GB | 250 GB |
| **CPU quota** | **60 min/day** hard cap → app stopped when exceeded | 240 min/day cap | None | None | None |
| **AlwaysOn supported** | ❌ | ❌ | ✅ | ✅ | ✅ |
| **Cold start tax** | Every ~20 min idle → next req ~30s | Every idle period | Only at deploy/restart | Only at deploy/restart | Only at deploy/restart |
| **Custom domain** | ❌ (only `*.azurewebsites.net`) | ✅ | ✅ | ✅ | ✅ |
| **Custom SSL** | Built-in `*.azurewebsites.net` only | ✅ | ✅ | ✅ | ✅ |
| **Deployment slots** | 0 | 0 | 0 | 5 | 20 |
| **Scale out** | 1 instance | 1 instance | Up to 3 | Up to 10 | Up to 30 |
| **Health checks** | ❌ | ❌ | ❌ | ✅ | ✅ |
| **VNet integration** | ❌ | ❌ | ❌ | ✅ | ✅ |
| **Auto-scale** | ❌ | ❌ | ❌ | ✅ | ✅ |
| **SLA** | None | None | 99.95% | 99.95% | 99.95% |

### Why F1 will hurt this specific app

- **Memory:** the 256 MB per-app ceiling is below Prose's warm-up footprint. The platform will OOM-kill the worker repeatedly — you'll see this as 502s + cold starts at random intervals.
- **CPU quota:** 60 min/day is *active CPU seconds*, not wall clock. A warm-up + a few page loads can burn 1–2 minutes. Heavy use during the day hits the cap by afternoon; **Azure auto-stops the app until midnight UTC**, returning HTTP 403 ("This web app is stopped") to every request.
- **No AlwaysOn:** Every 20-minute idle period unloads the .NET worker. Next request pays a ~30s cold start *on top of* the serverless SQL wake-from-pause. ~40s before anything renders.
- **Cascading stall:** the 26-parallel-COUNT homepage against a cold serverless DB on a memory-constrained worker is the recipe for "click → stall → 403 stopped" that triggers the daily quota cliff.

### Why B1 fixes everything for this app

- 1.75 GB RAM (≈7× more headroom than F1).
- Unlimited CPU minutes — the per-request SLA still applies, but the daily quota disappears.
- AlwaysOn keeps the worker warm. Only the serverless SQL pays cold-start on first hit, ~10s instead of ~40s.
- Custom domain support if you ever want to point `prose.com` at it.

### When B1 isn't enough

Bump to S1 if you want:
- **Deployment slots** for zero-downtime deploys (deploy to staging, smoke-test, swap to production).
- **Health checks** so Azure pulls a sick instance out of rotation automatically.
- **Production-grade SLA** with autoscale.

P1v3+ is overkill for a hobby-scale app. Don't pay for it unless you've measured a need.

### Recommended setting for Prose

For a single-author literary fiction engine with bursty usage, **B1 with AlwaysOn enabled** is the right floor. ~$13/mo. One-line bump (substitute your plan name):

```powershell
# Find your plan name first
az appservice plan list --resource-group MyApps --query "[].name" -o tsv

# Bump the SKU + enable AlwaysOn + restart
az appservice plan update --name <plan-name> --resource-group MyApps --sku B1
az webapp config set --name mindattic-prose --resource-group MyApps --always-on true
az webapp restart --name mindattic-prose --resource-group MyApps
```

Takes ~1 min. No data loss, no DNS change, same URL.

### If money is genuinely tight

Stay on F1 and accept the rough edges:
- Pre-warm with `Invoke-WebRequest https://mindattic-prose.azurewebsites.net/ -UseBasicParsing` before sessions.
- When you see HTTP 403 "This web app is stopped", run `az webapp start --name mindattic-prose --resource-group MyApps` and wait ~30s.
- The CPU-minute quota resets at midnight UTC every night.
- Expect 30–45s page loads on first hit after an idle period.

---

## Day-2 operations

### Schema changes

Add a new EF Core migration under `v3/Prose.Core/Migrations/` (`dotnet ef migrations add <Name>`
from `v3/Prose.Core`), then:

```powershell
$env:ConnectionStrings__Prose = "Server=tcp:prose-sql.database.windows.net,1433;Database=Prose;Authentication=Active Directory Default;Encrypt=True;TrustServerCertificate=False;"
dotnet run --project v3/Prose.Cli -- --migrate-sql --schema
```

Then commit + push the code that depends on the new schema. GitHub Actions deploys the app; the schema is already current because you ran the migration first.

### Connecting from your laptop to the cloud DB

`az login` first (as yourself, the AAD admin), then:

```powershell
$env:ConnectionStrings__Prose = "Server=tcp:prose-sql.database.windows.net,1433;Database=Prose;Authentication=Active Directory Default;Encrypt=True;TrustServerCertificate=False;"
dotnet run --project v3/Prose.Writer
```

`Active Directory Default` will pick up your az-cli credential automatically. Without setting the env var, `appsettings.json` falls back to LocalDB.

### Switching environments

```powershell
# Production (no auto-login, generic error pages)
az webapp config appsettings set --name mindattic-prose --resource-group MyApps --settings ASPNETCORE_ENVIRONMENT=Production

# Development (DevAuth auto-login enabled, full stack traces in browser)
az webapp config appsettings set --name mindattic-prose --resource-group MyApps --settings ASPNETCORE_ENVIRONMENT=Development

# Always restart to pick up the new value
az webapp restart --name mindattic-prose --resource-group MyApps
```

**Don't leave the public-facing site in Development mode** — DevAuth logs anyone in as Administrator without a password.

---

## FAQ

### Q: `az ... appsettings list --query "[?name=='ConnectionStrings__Prose'].value" -o tsv` shows the connection string truncated. What gives?

**A:** `-o tsv` lies about values containing internal spaces. The actual stored value is fine. Always verify with `-o json` instead:

```powershell
az webapp config appsettings list `
  --name mindattic-prose --resource-group MyApps `
  --query "[?name=='ConnectionStrings__Prose']" `
  -o json
```

JSON shows the value byte-for-byte, no formatting lies. I wasted ~90 minutes chasing a phantom truncation here. **Always use `-o json` for diagnosing value contents.**

### Q: `az webapp config appsettings set --settings "key=value with spaces"` mangles the value at the first internal space.

**A:** Known quirk of how PowerShell forwards arguments to az CLI. Workaround: **set values with spaces via the Azure Portal UI, not the CLI**. The Portal preserves the value exactly. Confirmed: a connection string with `Authentication=Active Directory Default` saves correctly via Portal.

`az ... --settings @file.json` is supposed to work for spaced values but has its own bugs on Windows.

### Q: SqlPackage Export fails with `Error SQL71501: Error validating element [dbo].[vw_Characters]: View has an unresolved reference to object [dbo].[Characters].[<some-column>]`.

**A:** A view in your LocalDB references columns that no longer exist on the underlying table (typical after denorm-column cleanups). Drop the stale view and re-export:

```powershell
sqlcmd -S "(localdb)\MSSQLLocalDB" -d Prose -E -Q "DROP VIEW IF EXISTS dbo.vw_Characters;"
```

The view is broken in LocalDB too — dropping it doesn't cost anything live.

### Q: SqlPackage Import fails with `*** Error parsing connection string: Invalid value for key 'authentication'.`

**A:** Your SqlPackage version is older than 5.x and doesn't recognize `Authentication=Active Directory Default`. Pass the access token directly instead:

```powershell
$token = az account get-access-token --resource https://database.windows.net/ --query accessToken -o tsv

sqlpackage `
  /Action:Import `
  /TargetServerName:"prose-sql.database.windows.net" `
  /TargetDatabaseName:"Prose" `
  /AccessToken:$token `
  ...
```

Use `/TargetServerName + /TargetDatabaseName + /AccessToken` instead of `/TargetConnectionString`.

### Q: SqlPackage Import fails with `*** Argument 'MaximumSize' has an invalid value: '34359738368'.`

**A:** `DatabaseMaximumSize` is in **GB**, not bytes. Pass `32`, not `34359738368`.

### Q: GitHub Actions builds fail with `NU1301: The local source 'C:\LocalNuGet' doesn't exist.`

**A:** `NuGet.config` references a local folder that exists only on the dev laptop. Bundle the required `.nupkg` files into the repo at `lib/local-packages/` and point `NuGet.config` at the relative path. See § 3.1.

### Q: GitHub Actions deploy step returns `Internal Server Error (CODE: 500)` from OneDeploy.

**A:** Usually means a prior deploy left the App Service in a stuck state (file locks, partial extraction). Fix:

```powershell
az webapp stop --name mindattic-prose --resource-group MyApps
Start-Sleep -Seconds 20
gh workflow run azure-deploy.yml --ref master
Start-Sleep -Seconds 10
gh run watch
# After ✓:
az webapp start --name mindattic-prose --resource-group MyApps
```

Stopping releases file locks; starting after the deploy ensures a clean process.

### Q: The site returns generic IIS "Internal Server Error" page with no detail.

**A:** ASP.NET Core unhandled exceptions are showing IIS's static error page instead of the .NET-side developer page. Two things to enable:

```powershell
az webapp config appsettings set --name mindattic-prose --resource-group MyApps `
  --settings ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_DETAILEDERRORS=true
az webapp restart --name mindattic-prose --resource-group MyApps
```

Wait 30 sec, refresh — you'll get a yellow exception page with the actual stack trace. Switch back to Production after diagnosis.

### Q: I enabled application logging but `az webapp log tail` only shows IIS request lines and the static 500 HTML.

**A:** `az webapp log tail` doesn't pick up your app's `Console.WriteLine` by default unless `stdoutLogEnabled="true"` is set in web.config. For diagnosing app-level exceptions, **the easier route is `ASPNETCORE_ENVIRONMENT=Development` to see them on the page**, not the log tail.

For real logs, download the LogFiles archive:

```powershell
az webapp log download --name mindattic-prose --resource-group MyApps --log-file D:\tmp\logs.zip
Expand-Archive D:\tmp\logs.zip D:\tmp\logs -Force
# eventlog.xml has the Windows EventLog entries; LogFiles\http\RawLogs\ has IIS requests
```

### Q: `Login failed for user ''` (empty user).

**A:** The connection string is missing the `Authentication=Active Directory Default` keyword, so SqlClient tries to connect with no credentials. Fix the connection string in App Service via the Portal:

```
Server=tcp:prose-sql.database.windows.net,1433;Database=Prose;Authentication=Active Directory Default;Encrypt=True;TrustServerCertificate=False;
```

### Q: `ArgumentException: Invalid value for key 'authentication'.`

**A:** Either:
1. The connection string value got truncated when stored, leaving a partial value like `Active Directory Managed` (instead of `Active Directory Managed Identity`). Re-save via the **Portal** (not CLI) and verify with `-o json`.
2. Your deployed `Microsoft.Data.SqlClient` is too old to recognize that keyword. Check: `dotnet list <csproj> package --include-transitive | grep -i sqlclient`. You want 4.0+ for `Active Directory Default`, 5.x is best.

### Q: `Principal 'mindattic-prose' could not be found at this Azure Active Directory tenant.`

**A:** The MI isn't enabled on the App Service yet, or AAD hasn't propagated the MI's display name. Run:

```powershell
az webapp identity assign --name mindattic-prose --resource-group MyApps
# Wait 60 seconds for AAD to propagate
```

Then retry the `CREATE USER [mindattic-prose] FROM EXTERNAL PROVIDER;` statement.

### Q: `Cannot open server '<name>' requested by the login. Client with IP… is not allowed.`

**A:** Your laptop's IP isn't in the SQL firewall. From the Portal Query Editor banner, click the **Allowlist IP** button. Or from CLI:

```powershell
$ip = (Invoke-RestMethod ifconfig.me).Trim()
az sql server firewall-rule create `
  --resource-group MyApps `
  --server prose-sql `
  --name 'my-laptop' `
  --start-ip-address $ip `
  --end-ip-address   $ip
```

### Q: The site loaded once, but the next request is slow.

**A:** Serverless SQL auto-paused after 60 idle minutes. First query after pause takes ~10 sec to wake the DB. Either accept the cold start or raise `autoPauseDelayMinutes` in `infra/azure-sql.parameters.json` and redeploy the Bicep.

### Q: `Error 403 - This web app is stopped. The web app you have attempted to reach is currently stopped and does not accept any requests.`

**A:** App Service is in the **Stopped** state. Two common causes:

1. **F1 daily CPU quota exhausted.** Free-tier App Services get 60 active CPU minutes per day. When you exceed that, Azure forcibly stops the app until midnight UTC. Prose's homepage burns CPU quickly (26 parallel COUNT queries + embedding cache warm-up + LLM provider init), so this happens easily on F1 with even moderate use.

2. **Manual stop.** If you ran `az webapp stop` earlier in a stop-deploy-start cycle and didn't follow up with `az webapp start`, the app stays stopped.

Confirm and fix:

```powershell
# Check current state
az webapp show --name mindattic-prose --resource-group MyApps --query state -o tsv
# → "Stopped" confirms the diagnosis

# Start it back up
az webapp start --name mindattic-prose --resource-group MyApps
```

If this is happening repeatedly (multiple times per day), you're hitting the F1 quota cliff. See § App Service Plan SKU — F1 vs B1+ — bumping to B1 (~$13/mo) eliminates the quota entirely and adds AlwaysOn support.

### Q: `azure-deploy.yml` fails because secrets `AZURE_CLIENT_ID` / `AZURE_TENANT_ID` are missing.

**A:** You added the `migrate` job (which uses OIDC federated auth) but didn't finish the OIDC setup. Either:
- **Finish OIDC setup** (see `infra/README.md` § GitHub OIDC) and add the secrets.
- **Drop the migrate job** from `azure-deploy.yml` and apply migrations manually with `dotnet run --project v3/Prose.Cli -- --migrate-sql --schema` against the Azure connection string.

The latter is simpler and was the path used in this guide.

### Q: How do I roll back a bad deploy?

**A:** GitHub Actions builds artifacts per commit. The fastest rollback is to revert the commit on master and push — Actions will build + deploy the reverted code.

```powershell
git revert <bad-commit-sha>
git push
gh run watch
```

App Service also has a "Deployment Center → Deployments" UI in the Portal that lets you redeploy any previous artifact with one click.

### Q: My App Service has weird environment variable behavior — settings I set seem to disappear.

**A:** `az webapp config appsettings set --settings KEY=VALUE` **replaces** the listed keys but doesn't touch others. Make sure you're not accidentally overwriting other settings by typo'ing a key name. Always verify with:

```powershell
az webapp config appsettings list --name mindattic-prose --resource-group MyApps --query "[].name" -o tsv | Sort-Object
```

`-o tsv` is fine for **just the names** since names don't have spaces.

### Q: Should I use `Active Directory Default` or `Active Directory Managed Identity` in the connection string?

**A:** **`Active Directory Default`**. It's the modern catch-all keyword (SqlClient 4.0+) that tries managed identity, then az-cli, then env vars, then VS, etc. Same connection string works locally and in App Service.

`Active Directory Managed Identity` is more explicit but only works inside an Azure resource with an MI — fails locally.

### Q: What if I need to recreate the bacpac and import again later?

**A:** Drop the database first (SqlPackage Import won't overwrite an existing one), re-import, re-GRANT:

```powershell
az sql db delete --resource-group MyApps --server prose-sql --name Prose --yes
sqlpackage /Action:Import ...
# Re-run the CREATE USER + GRANT block
```

This is destructive — any data added to the cloud DB since the last bacpac export is lost.

---

## What NOT to do

These are workarounds I tried in real-time that turned out to be unnecessary. Avoid them:

- **A custom `DbConnectionInterceptor` that fetches AAD tokens and attaches them to `SqlConnection.AccessToken`.** Looks clever, isn't needed. Microsoft.Data.SqlClient 5.x+ does this automatically when you use `Authentication=Active Directory Default`. The interceptor was a workaround for the phantom truncation I thought was happening (which turned out to be `-o tsv` lying).

- **Switching to `Authentication=Active Directory Managed Identity`** because Default didn't seem to be storing. The keyword wasn't the problem — the storage was always fine; my verification command was wrong (`-o tsv` instead of `-o json`).

- **Removing the `Authentication=` keyword entirely** to avoid storage truncation. Created the "Login failed for user ''" error because the connection then had no credentials.

- **Heavy `az rest` PATCH dances against the Microsoft.Web Resource Manager API** to set the connection string. The Portal works fine; `-o tsv` was just lying about what was stored.

The whole drama would have collapsed if I'd verified with `-o json` from the start. The canonical Microsoft pattern is:

1. App Service with system-assigned MI.
2. Azure SQL with that MI as an EXTERNAL_USER.
3. Connection string with `Authentication=Active Directory Default`.
4. Done.

No interceptors, no token plumbing in code, no workarounds.

---

## Reference files

- `infra/azure-sql.bicep` — Provisions SQL server + DB.
- `infra/azure-sql.parameters.json` — Per-environment parameters.
- `infra/grant-managed-identity.sql` — Contained-user GRANT script (sqlcmd-flavored with `:setvar`).
- `infra/setup-azure.ps1` — One-shot bootstrap (resource group → Bicep → GRANT → app settings).
- `infra/README.md` — End-to-end deployment guide (includes the OIDC path for CI-driven migrations).
- `.github/workflows/azure-deploy.yml` — Build + deploy pipeline.
- `v3/Prose.Cli/Cli/MigrateSqlCli.cs` — Schema migration runner (`--migrate-sql --schema`).
- `v3/Prose.Core/Migrations/` — EF Core migrations (the live schema source of truth).
- `v3/Prose.Core/Data/Sql/` — Raw `.sql` files, mostly historical pre-EF-migration deltas.

---

*Last updated: 2026-05-23. The hard-won wisdom in this doc cost ~3 hours and one wrong workaround. May the next deploy take 45 minutes.*
