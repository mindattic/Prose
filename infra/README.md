# Azure SQL deployment

End-to-end guide for standing StreetSamurai up against Azure SQL Database with **managed-identity authentication** and **GitHub Actions-driven schema migrations**. Everything in this folder is idempotent — re-running any step is safe.

## Architecture

```
GitHub master push  ->  GitHub Actions (build -> migrate -> deploy)
                                |
                          OIDC federated cred
                                |
                    +-----------+-----------+
                    |                       |
              Azure App Service        Azure SQL Database
              (streetsamurai)          (AAD-only auth)
              system-assigned MI        GitHub SP: db_ddladmin
                                        App Service MI: db_datareader/writer
```

No passwords anywhere. CI/CD uses OIDC; the App Service uses its managed identity.

## Files

| File | Purpose |
| --- | --- |
| `azure-sql.bicep` | Provisions SQL logical server + Serverless GP database + firewall rule |
| `azure-sql.parameters.json` | Per-environment parameters — fill in three `__REPLACE__` values before running |
| `grant-managed-identity.sql` | Creates contained-user logins for the App Service MI and GitHub OIDC SP |
| `setup-azure.ps1` | One-shot bootstrap: resource group → Bicep → grant → App Service app setting |

## One-time setup

### 1. Prerequisites

```powershell
az --version       # Azure CLI
sqlcmd --version   # sqlcmd (Go-based)
dotnet --version   # .NET 10 SDK

az login
az account set --subscription "<sub-id>"
```

### 2. Find your AAD user object id

```powershell
az ad signed-in-user show --query id -o tsv
az ad signed-in-user show --query userPrincipalName -o tsv
```

Drop both into `azure-sql.parameters.json` (`aadAdminObjectId` and `aadAdminLogin`).

### 3. GitHub OIDC service principal

```powershell
$app  = az ad app create --display-name streetsamurai-github | ConvertFrom-Json
$sp   = az ad sp create --id $app.appId | ConvertFrom-Json
$appId   = $app.appId    # -> AZURE_CLIENT_ID GitHub secret
$spObjId = $sp.id        # -> azure-sql.parameters.json githubOidcSpObjectId

$body = @{
    name      = 'github-master'
    issuer    = 'https://token.actions.githubusercontent.com'
    subject   = 'repo:mindattic/StreetSamurai:ref:refs/heads/master'
    audiences = @('api://AzureADTokenExchange')
} | ConvertTo-Json
$body | az ad app federated-credential create --id $app.appId --parameters '@-'
```

### 4. GitHub repo secrets

| Secret | Value |
| --- | --- |
| `AZURE_CLIENT_ID` | `$appId` from step 3 |
| `AZURE_TENANT_ID` | `az account show --query tenantId -o tsv` |
| `AZURE_SUBSCRIPTION_ID` | `az account show --query id -o tsv` |
| `AZURE_SQL_CONNECTION` | Printed by `setup-azure.ps1` after step 5 |
| `AZURE_WEBAPP_PUBLISH_PROFILE` | Already present |

### 5. Run the bootstrap

```powershell
# Fill in the three __REPLACE__ values in azure-sql.parameters.json first, then:
powershell -NoProfile -ExecutionPolicy Bypass `
    -File infra/setup-azure.ps1 `
    -ResourceGroup street-samurai-rg `
    -AppServiceName streetsamurai `
    -GitHubSpName  streetsamurai-github
```

The script: creates the resource group if needed, deploys Bicep, grants database roles, sets the App Service connection string.

### 6. First deploy

Push any commit to `master`. The Actions workflow runs:
- `build` — restore + publish + upload artifacts
- `migrate` — OIDC login → `ApplyMigrations.exe` against Azure SQL
- `deploy` — push artifact to App Service slot

## Connection-string resolution at runtime

`v3/StreetSamurai.Core/Extensions/ServiceCollectionExtensions.cs` resolves in this order:

1. `ConnectionStrings__StreetSamurai` env var (App Service Application Setting in production)
2. `IConfiguration.GetConnectionString("StreetSamurai")` from `appsettings.json`
3. LocalDB fallback

Production uses (1) with `Authentication=Active Directory Default` — the SqlClient resolves the managed identity transparently.

## Local development

LocalDB works for everyday writing. Nothing changes:

```powershell
dotnet run --project v3/ApplyMigrations
dotnet run --project v3/StreetSamurai.Blazor
# -> https://localhost:7103/
```

To point local dev at the Azure SQL database:

```powershell
$env:ConnectionStrings__StreetSamurai = '<connection string from Bicep output>'
dotnet run --project v3/StreetSamurai.Blazor
```

`Authentication=Active Directory Default` uses your `az login` credentials.

## Cost expectations

- **Serverless GP_S_Gen5_2** with 60-minute auto-pause: ~$5–15 USD/month at light usage.
- Auto-pause sleeps the DB after 60 idle minutes; first query after resuming takes ~10s.
- Tune `autoPauseDelayMinutes` in `azure-sql.parameters.json` to balance cost vs latency.

## Troubleshooting

**"Login failed for user '<token-identified principal>'"** — Re-run `grant-managed-identity.sql` (idempotent). Confirm the App Service name matches the AAD display name of its system-assigned identity.

**"Client with IP... is not allowed"** — Add a personal firewall rule:

```powershell
az sql server firewall-rule create `
    --resource-group street-samurai-rg `
    --server streetsamurai-sql `
    --name 'my-laptop' `
    --start-ip-address $(curl -s ifconfig.me) `
    --end-ip-address   $(curl -s ifconfig.me)
```

**ApplyMigrations fails in CI** — The GitHub SP needs `db_ddladmin`. Re-run `grant-managed-identity.sql`.
