# Azure SQL deployment

End-to-end guide for standing StreetSamurai up against Azure SQL Database
with **managed-identity authentication** and **GitHub Actions–driven
schema migrations**. Everything in this folder is idempotent — re-running
any step is safe.

## Architecture

```
                        ┌──────────────────┐
   GitHub master push   │ GitHub Actions    │
   ────────────────────►│  build → migrate  │── OIDC ──┐
                        │      → deploy     │          │
                        └──────────────────┘           ▼
                                  │           ┌──────────────────┐
                                  │           │ Azure SQL        │
                                  │           │ (AAD-only auth)  │
                                  │           │                  │
                                  │           │ GitHub-SP user:  │
                                  │           │   db_ddladmin    │
                                  │           │   datareader     │
                                  │           │   datawriter     │
                                  ▼           │                  │
                        ┌──────────────────┐  │ App-Service MI:  │
                        │ Azure App Service│──┤   db_datareader  │
                        │  (streetsamurai) │  │   db_datawriter  │
                        │ system-assigned  │  └──────────────────┘
                        │ managed identity │
                        └──────────────────┘
```

The App Service runs the app and only needs read/write access. CI/CD's
service principal runs migrations and additionally has DDL rights.
Neither connection uses a password.

## Files in this folder

| File | What it does |
| --- | --- |
| `azure-sql.bicep` | Provisions the SQL logical server + Serverless GP database + firewall rule. AAD-only authentication. |
| `azure-sql.parameters.json` | Per-environment parameters. Fill in three `__REPLACE__` placeholders before running the bootstrap. |
| `grant-managed-identity.sql` | Creates contained-user logins for the App Service MI and the GitHub OIDC SP, grants their database roles. |
| `setup-azure.ps1` | One-shot bootstrap: resource group → Bicep → grant → App Service app setting. Re-runnable. |

## One-time setup

The order matters because each step depends on identifiers produced by
the previous one.

### 1. Prerequisites on your dev box

```powershell
# Azure CLI (https://learn.microsoft.com/cli/azure/install-azure-cli)
az --version

# sqlcmd Go-based client (https://learn.microsoft.com/sql/tools/sqlcmd/sqlcmd-utility)
sqlcmd --version

# .NET 10 SDK (required to publish ApplyMigrations locally)
dotnet --version

# Sign in to the subscription you want to deploy to
az login
az account set --subscription "<sub-id>"
```

### 2. Find your AAD user object id

This becomes the SQL server's AAD admin (you, the human).

```powershell
az ad signed-in-user show --query id -o tsv
# → e.g. 7f1f2c9c-1234-5678-9abc-def012345678

az ad signed-in-user show --query userPrincipalName -o tsv
# → e.g. ryan@mindattic.onmicrosoft.com
```

Drop both values into `azure-sql.parameters.json` (`aadAdminObjectId`
and `aadAdminLogin`).

### 3. GitHub OIDC service principal

Used by `.github/workflows/azure-deploy.yml` to authenticate without a
client secret. One SP per repo.

```powershell
# 3a. Create the AAD app + SP (note both ids — you need them later).
$app  = az ad app create --display-name streetsamurai-github | ConvertFrom-Json
$sp   = az ad sp create --id $app.appId                       | ConvertFrom-Json
$appId   = $app.appId          # this is AZURE_CLIENT_ID for GitHub secrets
$spObjId = $sp.id              # this is the SP OBJECT id — different from appId

# 3b. Add a federated credential that trusts pushes to mindattic/StreetSamurai master.
$body = @{
    name        = 'github-master'
    issuer      = 'https://token.actions.githubusercontent.com'
    subject     = 'repo:mindattic/StreetSamurai:ref:refs/heads/master'
    audiences   = @('api://AzureADTokenExchange')
    description = 'StreetSamurai master deploys'
} | ConvertTo-Json
$body | az ad app federated-credential create --id $app.appId --parameters '@-'
```

Drop `$spObjId` into `azure-sql.parameters.json` (`githubOidcSpObjectId`)
and `$appId` into the **GitHub repo secret** `AZURE_CLIENT_ID` (next
step).

### 4. GitHub repo secrets

In `Settings → Secrets and variables → Actions → New repository secret`:

| Secret name | Value |
| --- | --- |
| `AZURE_CLIENT_ID` | `$appId` from step 3 (the app's *appId* — NOT the SP object id) |
| `AZURE_TENANT_ID` | `az account show --query tenantId -o tsv` |
| `AZURE_SUBSCRIPTION_ID` | `az account show --query id -o tsv` |
| `AZURE_SQL_CONNECTION` | Will be printed by `setup-azure.ps1` (Bicep output `connectionString`). Set this AFTER step 5. |
| `AZURE_WEBAPP_PUBLISH_PROFILE` | Already present from the existing deploy workflow. |

### 5. Run the bootstrap

```powershell
# Fill in the three __REPLACE__ values in azure-sql.parameters.json first.
# Then:
powershell -NoProfile -ExecutionPolicy Bypass `
    -File infra/setup-azure.ps1 `
    -ResourceGroup street-samurai-rg `
    -AppServiceName streetsamurai `
    -GitHubSpName  streetsamurai-github
```

The script:

1. Confirms `az` login + subscription.
2. Creates the resource group if missing.
3. Deploys `azure-sql.bicep`. Output includes the full connection string.
4. Runs `grant-managed-identity.sql` against the new database to create
   the two contained users and grant their roles.
5. Sets the App Service Application Setting
   `ConnectionStrings__StreetSamurai` to the AAD-default connection
   string from the Bicep output.
6. Prints a checklist for the remaining GitHub-secret step.

If anything fails partway, just re-run — every step is guarded.

### 6. First deploy

Push any commit to `master`. Watch the Actions run:

- `build`   — restore + publish + upload artifacts (existing behavior).
- `migrate` — `azure/login@v2` via OIDC, then `ApplyMigrations.exe`
              runs every T-SQL file in `v3/StreetSamurai.Core/Data/Sql/`
              against the Azure SQL DB. Idempotent guards in each script
              make re-runs no-ops once applied.
- `deploy`  — push the artifact to `streetsamurai` App Service slot.

Smoke test: <https://streetsamurai.azurewebsites.net/>

## Connection-string resolution at runtime

`v3/StreetSamurai.Core/Extensions/ServiceCollectionExtensions.cs` reads
the connection string in this priority order:

1. Environment variable `ConnectionStrings__StreetSamurai`
   (App Service Application Setting in production).
2. `IConfiguration.GetConnectionString("StreetSamurai")` from
   `appsettings.json`.
3. LocalDB fallback (`(localdb)\MSSQLLocalDB`).

This means:

- **Local dev** uses the `appsettings.json` LocalDB string. No Azure
  credentials needed.
- **Production App Service** uses the env var with `Authentication=Active
  Directory Default`. `Microsoft.Data.SqlClient` resolves the App
  Service's system-assigned managed identity automatically.
- **GitHub Actions migration job** uses the env var with the same
  AAD-default mode. The OIDC login from `azure/login@v2` populates the
  environment with a federated token; the SqlClient picks it up via the
  same `DefaultAzureCredential` chain.

## Local development

LocalDB still works for everyday writing. Nothing changes:

```powershell
# From the repo root
dotnet run --project v3/ApplyMigrations    # bring local DB to head
dotnet run --project v3/StreetSamurai.Blazor
# → https://localhost:7103/
```

If you want your local dev box to use the **Azure SQL** database instead
of LocalDB (e.g. to repro a production data issue), set the env var
before launching:

```powershell
$env:ConnectionStrings__StreetSamurai = '<connection string from Bicep output>'
dotnet run --project v3/StreetSamurai.Blazor
```

`Authentication=Active Directory Default` will use your `az login`
credentials transparently.

## Cost expectations

- **Serverless GP_S_Gen5_2** with auto-pause (60 min idle):
  ~$5–15 USD/month at light usage, ~$30 at moderate traffic.
- **Auto-pause** is the cost-saver — the DB sleeps after 60 idle minutes
  and resumes on the next query (~10 s cold start). Tune
  `autoPauseDelayMinutes` in `azure-sql.parameters.json` to bias toward
  cost (low, e.g. 60) or latency (high, e.g. 360 or -1 = never).
- Storage at ~$0.115/GB/month for the configured `sqlDatabaseMaxSizeGB`.

## Troubleshooting

### "Login failed for user '<token-identified principal>'"
The principal isn't a database user. Re-run `grant-managed-identity.sql`
(safe; idempotent). Confirm the App Service name matches the AAD display
name of its system-assigned identity (`az webapp identity show --name
streetsamurai --resource-group street-samurai-rg`).

### "Cannot open server '<name>' requested by the login. Client with IP… is not allowed to access the server."
Firewall. The Bicep template allows Azure-internal services (0.0.0.0).
For *local* development against the Azure DB, add a personal rule:

```powershell
az sql server firewall-rule create `
    --resource-group street-samurai-rg `
    --server streetsamurai-sql `
    --name 'my-laptop' `
    --start-ip-address $(curl -s ifconfig.me) `
    --end-ip-address   $(curl -s ifconfig.me)
```

### "The DBContext options are configured but the connection cannot be opened."
Either no env var and LocalDB isn't installed locally, or the env var
is set but the DB doesn't exist yet. Run `setup-azure.ps1`.

### ApplyMigrations fails in CI but works locally
The GitHub SP needs the `db_ddladmin` role (not just datareader/writer).
Re-run `grant-managed-identity.sql` — the GRANT block is idempotent.

### Auto-pause cold start makes the first request slow
Either bump `autoPauseDelayMinutes` higher (less aggressive pause) or
add an Azure Monitor "Keep-Warm" alert that pings the app every 50 min.
