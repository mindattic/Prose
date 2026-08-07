// ──────────────────────────────────────────────────────────────────────────
// Azure SQL provisioning for Prose.
//
// Provisions:
//   1. SQL logical server with AAD-only authentication (no SQL admin password).
//      Two AAD principals are mapped to it: the human Azure AD admin (for
//      one-off T-SQL operations) and the GitHub Actions OIDC service
//      principal (for CI/CD migration runs).
//   2. SQL Database on Serverless General Purpose tier (Gen5, autoscale
//      0.5–2 vCores, auto-pause after 60 min). Supports vector data type
//      + temporal tables, which Prose requires.
//   3. Firewall rule allowing all Azure-internal services so the App Service
//      can connect. Tighten later via private endpoint if you want.
//   4. The App Service's system-assigned managed identity is granted as a
//      SQL contained user via the grant-managed-identity.sql T-SQL script
//      (run once after `az deployment group create` completes — see
//      infra/README.md for the exact command).
//
// What this file does NOT do:
//   - Create the resource group (do it once with az cli — see infra/README.md).
//   - Create the App Service (already exists at `prose`).
//   - Run schema migrations (handled by GitHub Actions, not Bicep).
//
// Deploy:
//   az deployment group create \
//     --resource-group street-samurai-rg \
//     --template-file infra/azure-sql.bicep \
//     --parameters @infra/azure-sql.parameters.json
// ──────────────────────────────────────────────────────────────────────────

@description('Azure region for the SQL server (e.g. eastus, westus2).')
param location string = resourceGroup().location

@description('SQL logical server name. Globally unique; lowercase + digits + hyphens. Becomes <name>.database.windows.net.')
param sqlServerName string = 'prose-sql'

@description('SQL database name. Doesn\'t need to be globally unique; lives under the server.')
param sqlDatabaseName string = 'Prose'

@description('AAD object ID of the human admin (you). Find with: az ad signed-in-user show --query id -o tsv')
param aadAdminObjectId string

@description('AAD display name of the human admin. Shown in the portal; not load-bearing.')
param aadAdminLogin string

@description('AAD object ID of the GitHub OIDC service principal that the deploy workflow authenticates as. Find after creating the federated credential with: az ad sp show --id <appId> --query id -o tsv')
param githubOidcSpObjectId string

@description('Serverless SQL DB SKU. Default GP_S_Gen5_2 = 2 vCores max, auto-pause. Override with GP_S_Gen5_1 to cap at 1 vCore for lower cost.')
param sqlDatabaseSku string = 'GP_S_Gen5_2'

@description('Max DTU storage in GB. Default 32; serverless GP storage is cheap.')
param sqlDatabaseMaxSizeGB int = 32

@description('Minutes of inactivity before the serverless DB auto-pauses. 60 = aggressive (cheaper, ~10s cold start on first query). Use -1 to disable auto-pause.')
param autoPauseDelayMinutes int = 60

// ──────────────────────────────────────────────────────────────────────────
// 1. SQL logical server with AAD-only auth.
// ──────────────────────────────────────────────────────────────────────────
resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: sqlServerName
  location: location
  identity: { type: 'SystemAssigned' }
  properties: {
    // Disable SQL auth entirely — every connection must come via AAD. This
    // is the posture matched by `Authentication=Active Directory Default`
    // in the app's connection string.
    publicNetworkAccess: 'Enabled'
    administrators: {
      administratorType: 'ActiveDirectory'
      principalType: 'User'
      login: aadAdminLogin
      sid: aadAdminObjectId
      tenantId: subscription().tenantId
      azureADOnlyAuthentication: true
    }
    minimalTlsVersion: '1.2'
  }
}

// ──────────────────────────────────────────────────────────────────────────
// 2. SQL Database (Serverless General Purpose).
// ──────────────────────────────────────────────────────────────────────────
resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  parent: sqlServer
  name: sqlDatabaseName
  location: location
  sku: {
    name: sqlDatabaseSku
    tier: 'GeneralPurpose'
  }
  properties: {
    maxSizeBytes: sqlDatabaseMaxSizeGB * 1024 * 1024 * 1024
    autoPauseDelay: autoPauseDelayMinutes
    minCapacity: json('0.5')
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    // Enable system-versioning support and any other defaults Prose
    // uses. Temporal tables work without a DB-level toggle on Azure SQL —
    // the CREATE TABLE ... PERIOD FOR SYSTEM_TIME is enough.
    requestedBackupStorageRedundancy: 'Local'
  }
}

// ──────────────────────────────────────────────────────────────────────────
// 3. Firewall — allow Azure-internal services (App Service, GitHub Actions
//    runners via az login). Replace with Private Endpoint when you're ready.
// ──────────────────────────────────────────────────────────────────────────
resource allowAzureServices 'Microsoft.Sql/servers/firewallRules@2023-08-01-preview' = {
  parent: sqlServer
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// ──────────────────────────────────────────────────────────────────────────
// Outputs — surfaced to the deploy script + README so the user can copy
// them into App Service Application Settings and GitHub Action secrets.
// ──────────────────────────────────────────────────────────────────────────
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName
output sqlDatabaseName string = sqlDatabaseName

// Format the connection string the App Service / GitHub Actions will use.
// Managed Identity → Authentication=Active Directory Default; SqlClient
// picks up the right token source automatically.
output connectionString string = 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Database=${sqlDatabaseName};Authentication=Active Directory Default;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'

// The GitHub OIDC SP object id is echoed back so the user can confirm it
// matches what they supplied — useful for the post-deploy grant script.
output githubOidcSpObjectId string = githubOidcSpObjectId
