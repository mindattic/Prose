# ──────────────────────────────────────────────────────────────────────────
# setup-azure.ps1
#
# One-shot bootstrap that wires Prose's local code to a fresh Azure
# SQL deployment. Designed to be re-runnable: every step checks for existing
# state and skips if already done. Reads its config from
# infra/azure-sql.parameters.json (after you fill in the three __REPLACE__
# fields).
#
# What it does:
#   1. Confirms az cli login + subscription.
#   2. Ensures the resource group exists.
#   3. Deploys infra/azure-sql.bicep (Azure SQL server + serverless DB,
#      AAD-only auth, firewall rule).
#   4. Runs infra/grant-managed-identity.sql against the DB to create
#      contained users for the App Service MI and the GitHub OIDC SP.
#   5. Sets the App Service Application Setting ConnectionStrings__
#      Prose to the AAD-default connection string.
#   6. Prints a checklist for the things that still require human action
#      (GitHub repo secrets, smoke test).
#
# What it does NOT do:
#   - Create the App Service (already exists at `mindattic-prose`).
#   - Create the GitHub OIDC service principal + federated credential
#     (one-shot per repo; instructions in infra/README.md § "GitHub OIDC").
#   - Run schema migrations (GitHub Actions does that on the next push).
#
# Usage:
#   powershell -NoProfile -ExecutionPolicy Bypass -File infra/setup-azure.ps1 `
#       -ResourceGroup MyApps `
#       -AppServiceName mindattic-prose `
#       -GitHubSpName  prose-github
# ──────────────────────────────────────────────────────────────────────────
[CmdletBinding()]
param(
    [string]$ResourceGroup  = 'MyApps',
    [string]$AppServiceName = 'mindattic-prose',
    [string]$GitHubSpName   = 'prose-github',
    [string]$ParametersFile = (Join-Path $PSScriptRoot 'azure-sql.parameters.json'),
    [switch]$SkipGrant,
    [switch]$SkipAppSettings
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Section($msg) { Write-Host "`n==> $msg" -ForegroundColor Cyan }
function Note($msg)    { Write-Host "    $msg" -ForegroundColor Gray }
function Warn($msg)    { Write-Warning $msg }

# ── 0. Prereq check ──────────────────────────────────────────────────────
Section 'Checking prerequisites'
foreach ($cmd in @('az', 'sqlcmd')) {
    if (-not (Get-Command $cmd -ErrorAction SilentlyContinue)) {
        throw "Required CLI '$cmd' not on PATH. Install:`n  az      -> https://learn.microsoft.com/cli/azure/install-azure-cli`n  sqlcmd  -> https://learn.microsoft.com/sql/tools/sqlcmd/sqlcmd-utility"
    }
}
$accountJson = (az account show 2>$null)
if (-not $accountJson) { throw "Not logged in. Run: az login" }
$account = $accountJson | ConvertFrom-Json
Note "Subscription: $($account.name) ($($account.id))"
Note "Tenant:       $($account.tenantId)"

if (-not (Test-Path $ParametersFile)) {
    throw "Parameters file not found: $ParametersFile"
}
$paramsText = Get-Content $ParametersFile -Raw
if ($paramsText -match '__REPLACE__') {
    throw "infra/azure-sql.parameters.json still has __REPLACE__ placeholders. Fill them in first (see infra/README.md § Bicep parameters)."
}
$params = ($paramsText | ConvertFrom-Json).parameters
$sqlServerName   = $params.sqlServerName.value
$sqlDatabaseName = $params.sqlDatabaseName.value
$location        = $params.location.value

# ── 1. Resource group ────────────────────────────────────────────────────
Section "Ensuring resource group '$ResourceGroup' exists in '$location'"
$rgExists = (az group exists --name $ResourceGroup) -eq 'true'
if ($rgExists) {
    Note "Resource group already exists."
} else {
    az group create --name $ResourceGroup --location $location | Out-Null
    Note "Created resource group."
}

# ── 2. Deploy Bicep ──────────────────────────────────────────────────────
Section "Deploying Azure SQL via Bicep"
$deploymentName = "prose-sql-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
$deployJson = az deployment group create `
    --resource-group $ResourceGroup `
    --template-file (Join-Path $PSScriptRoot 'azure-sql.bicep') `
    --parameters "@$ParametersFile" `
    --name $deploymentName `
    --query 'properties.outputs' `
    -o json
if ($LASTEXITCODE -ne 0) { throw "Bicep deployment failed." }

$outputs = $deployJson | ConvertFrom-Json
$serverFqdn      = $outputs.sqlServerFqdn.value
$connectionString = $outputs.connectionString.value
Note "Server FQDN: $serverFqdn"
Note "Database:    $sqlDatabaseName"

# ── 3. GRANT roles to App Service MI + GitHub SP ─────────────────────────
if (-not $SkipGrant) {
    Section "Granting database roles to '$AppServiceName' (App Service MI) + '$GitHubSpName' (GitHub OIDC SP)"
    $grantScript = Join-Path $PSScriptRoot 'grant-managed-identity.sql'
    & sqlcmd `
        -S $serverFqdn `
        -d $sqlDatabaseName `
        -G `
        -i $grantScript `
        -v APP_SERVICE_NAME=$AppServiceName GITHUB_SP_NAME=$GitHubSpName
    if ($LASTEXITCODE -ne 0) { throw "sqlcmd grant step failed (exit $LASTEXITCODE)." }
    Note "Roles granted."
} else {
    Warn "Skipping GRANT step (-SkipGrant). Run infra/grant-managed-identity.sql manually before the first deploy."
}

# ── 4. App Service Application Setting ───────────────────────────────────
if (-not $SkipAppSettings) {
    Section "Setting App Service application setting ConnectionStrings__Prose on '$AppServiceName'"
    az webapp config appsettings set `
        --resource-group $ResourceGroup `
        --name $AppServiceName `
        --settings "ConnectionStrings__Prose=$connectionString" `
        --output none
    if ($LASTEXITCODE -ne 0) { throw "App Service appsettings update failed (exit $LASTEXITCODE)." }
    Note "App setting written."
} else {
    Warn "Skipping App Service app settings (-SkipAppSettings)."
}

# ── 5. Next steps ────────────────────────────────────────────────────────
Section 'Setup complete. Next steps:'
Write-Host @"
  [ ] GitHub Actions OIDC — confirm three repo secrets exist
      (Settings → Secrets and variables → Actions):
        AZURE_CLIENT_ID        = appId of the prose-github SP
        AZURE_TENANT_ID        = $($account.tenantId)
        AZURE_SUBSCRIPTION_ID  = $($account.id)
        AZURE_SQL_CONNECTION   = $connectionString

  [ ] Push a no-op commit to master and watch the deploy job:
        https://github.com/mindattic/Prose/actions

  [ ] Smoke test once the Action's "Apply DB migrations" step succeeds:
        https://prose.azurewebsites.net/

  [ ] (Optional) Tighten the firewall: replace AllowAzureServices with a
      Private Endpoint once you've verified the path works end-to-end.
"@ -ForegroundColor Yellow
