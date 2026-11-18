# Shared DB helpers for GSPL scripts. Dot-source it:
#
#     . "$PSScriptRoot\gspl_db.ps1"
#     $conn = Open-SS            # an OPEN connection to the Azure SQL Prose database
#     New-SSRow $conn 'PlaceAliases' @{ PlaceId = $id; Position = 0; Value = 'Banias' }
#
# WHY THIS EXISTS
#
# Two failure modes bit us on 2026-07-29 and both reported success:
#
# 1. IDENTITY columns. 152 tables in this DB have a bigint/int IDENTITY `Id`
#    (PlaceAliases, DeprecatedEntityNames, EntityProperties, every *Aliases and
#    *StoryHooks table, WoundLedger, Edges, Findings, Tags...). Supplying a value for
#    one raises "Operand type clash: uniqueidentifier is incompatible with bigint".
#    New-SSRow reads sys.columns and DROPS identity columns from the INSERT, so the
#    mistake cannot be made. Contrast: Beats/Nodes/BeatNodes use uniqueidentifier Ids
#    that you DO supply - hence the confusion.
#
# 2. Non-terminating .NET exceptions. A SqlException from ExecuteNonQuery() surfaces as
#    a MethodInvocationException, which by default does NOT stop the script. So the
#    write is skipped, the following statements run, and the script prints "APPLIED".
#    Assert-SSApplied throws on a terminating error AND on an unexpected row count.
#
# Set $ErrorActionPreference = 'Stop' in the calling script too - this file sets it for
# its own scope only.

# NOTE: deliberately no Set-StrictMode here. Dot-sourcing runs in the CALLER's scope, and
# strict mode leaks out of this file into everything downstream - it broke an unrelated
# $LASTEXITCODE probe the first time. $ErrorActionPreference = 'Stop' leaks too, but that
# leak is the point.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Data

$script:SSIdentityCache = @{}

# WHERE THE DATABASE IS (PRS-T1009)
#
# Prose lives in Azure SQL (Entra-only sign-in), not on a SQL Server on this PC. Open-SS is the ONE
# way a script opens a connection, and it returns an OPEN connection to that database. Never build a
# connection string in a script.
#
# Windows PowerShell's System.Data.SqlClient cannot parse "Authentication=Active Directory ...", so
# Open-SS signs in with an access token (SqlConnection.AccessToken) instead. The token comes from,
# in order:
#   1. the service principal in %APPDATA%\MindAttic\Database\providers.json
#      (AzureSql.TenantId / ClientId / ClientSecret); never printed;
#   2. the developer's own sign-in:  az account get-access-token --resource https://database.windows.net/
#      (run `az login` once).
#
# The database is serverless and pauses when idle; the first open after a pause takes about a
# minute. Open-SS retries for up to three minutes and prints one line per retry.
#
# Overrides: -ConnectionString, -Server/-Database, or the env var ConnectionStrings__Prose (in that
# order) name a different target. A LocalDB / localhost / this-PC target is IGNORED with a warning:
# local SQL Server is retired for Prose, and a stale local string must not send writes to a copy
# nobody reads.
$script:SSAzureServer   = 'sql-mindattic-f65744.database.windows.net'
$script:SSOpenMaxWait   = 180      # seconds an open keeps retrying while the database resumes
$script:SSToken         = $null
$script:SSTokenExpires  = [datetime]::MinValue

function Test-SSLocalTarget {
    param([string]$Server)
    if (-not $Server) { return $false }
    $s = ($Server.Trim() -replace '^(tcp|np|lpc):', '')
    if ($s -match '(?i)^\(localdb\)|^\(local\)|^localhost|^127\.0\.0\.1|^\.($|[\\,])|^\\\\\.\\pipe') { return $true }
    if ($env:COMPUTERNAME -and $s -match ('(?i)^' + [regex]::Escape($env:COMPUTERNAME) + '($|[\\,])')) { return $true }
    return $false
}

function Test-SSAzureTarget {
    param([string]$Server)
    return [bool]($Server -match '(?i)\.database\.(windows\.net|usgovcloudapi\.net|chinacloudapi\.cn|cloudapi\.de|fabric\.microsoft\.com)')
}

# Reads a connection string of any dialect (System.Data.SqlClient would throw on "Authentication=").
function Get-SSConnectionStringParts {
    param([Parameter(Mandatory)][string]$ConnectionString)
    $b = New-Object System.Data.Common.DbConnectionStringBuilder
    $b.set_ConnectionString($ConnectionString)
    $get = {
        param($names)
        foreach ($n in $names) { if ($b.ContainsKey($n)) { return [string]$b[$n] } }
        return $null
    }
    return @{
        Server         = & $get @('server', 'data source', 'address', 'addr', 'network address')
        Database       = & $get @('database', 'initial catalog')
        Authentication = & $get @('authentication')
        Password       = & $get @('password', 'pwd')
    }
}

# An access token for Azure SQL. Cached for the life of the script. Never written to the host.
function Get-SSAccessToken {
    if ($script:SSToken -and [datetime]::UtcNow -lt $script:SSTokenExpires) { return $script:SSToken }
    $why = New-Object System.Collections.Generic.List[string]

    $file = Join-Path $env:APPDATA 'MindAttic\Database\providers.json'
    if (Test-Path -LiteralPath $file) {
        $secret = $null
        try {
            $sp = (Get-Content -LiteralPath $file -Raw | ConvertFrom-Json).AzureSql
            if ($sp -and $sp.TenantId -and $sp.ClientId -and $sp.ClientSecret) {
                $secret = [string]$sp.ClientSecret
                [Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12
                $body = @{
                    grant_type    = 'client_credentials'
                    client_id     = [string]$sp.ClientId
                    client_secret = $secret
                    scope         = 'https://database.windows.net/.default'
                }
                $r = Invoke-RestMethod -Method Post -Uri ("https://login.microsoftonline.com/" + $sp.TenantId + "/oauth2/v2.0/token") `
                    -Body $body -ContentType 'application/x-www-form-urlencoded' -TimeoutSec 30
                if ($r.access_token) {
                    $script:SSToken = [string]$r.access_token
                    $script:SSTokenExpires = [datetime]::UtcNow.AddSeconds([Math]::Max(60, [int]$r.expires_in - 300))
                    return $script:SSToken
                }
                $why.Add('service principal: the token endpoint returned no access token')
            } else {
                $why.Add('service principal: providers.json has no complete AzureSql.TenantId/ClientId/ClientSecret')
            }
        } catch {
            $m = [string]$_.Exception.Message
            if ($secret) { $m = $m.Replace($secret, '***') }
            $why.Add('service principal: ' + $m)
        }
    } else {
        $why.Add("service principal: $file not found")
    }

    if (Get-Command az -ErrorAction SilentlyContinue) {
        # Local to this function: az writes warnings to stderr, which 'Stop' would turn into a throw.
        $ErrorActionPreference = 'Continue'
        $t = $null
        try { $t = & az account get-access-token --resource https://database.windows.net/ --query accessToken -o tsv 2>$null } catch { }
        $t = [string]($t | Select-Object -First 1)
        if ($LASTEXITCODE -eq 0 -and $t -and $t.Trim()) {
            $script:SSToken = $t.Trim()
            $script:SSTokenExpires = [datetime]::UtcNow.AddMinutes(30)
            return $script:SSToken
        }
        $why.Add('az: "az account get-access-token" failed (run "az login")')
    } else {
        $why.Add('az: the Azure CLI is not installed')
    }

    throw ("SS-DB: no access token for Azure SQL. " + ($why -join '; '))
}

# The SqlException number behind a PowerShell method-call error, or $null.
function Get-SSSqlErrorNumber {
    param($ErrorRecord)
    for ($e = $ErrorRecord.Exception; $null -ne $e; $e = $e.InnerException) {
        if ($e -is [System.Data.SqlClient.SqlException]) { return $e.Number }
    }
    return $null
}

function Open-SS {
    param([string]$Server, [string]$Database = 'Prose', [string]$ConnectionString)

    $explicit = $null; $from = $null
    if ($ConnectionString)                 { $explicit = $ConnectionString; $from = '-ConnectionString' }
    elseif ($Server)                       { $explicit = "Server=$Server;Database=$Database;Trusted_Connection=True;TrustServerCertificate=True;"; $from = '-Server' }
    elseif ($env:ConnectionStrings__Prose) { $explicit = $env:ConnectionStrings__Prose; $from = 'env ConnectionStrings__Prose' }

    $targetServer = $script:SSAzureServer
    $targetDb     = $Database
    $asIs         = $null      # a non-Azure target is opened with its own string, untouched
    if ($explicit) {
        $parts = Get-SSConnectionStringParts $explicit
        if (Test-SSLocalTarget $parts.Server) {
            Write-Warning ("gspl_db: ignoring $from (server '" + $parts.Server + "' is a local SQL Server; Prose lives in Azure SQL now). Using $($script:SSAzureServer).")
        } elseif (-not (Test-SSAzureTarget $parts.Server)) {
            $asIs = $explicit
        } elseif (-not $parts.Authentication -and $parts.Password) {
            $asIs = $explicit                      # Azure SQL with a SQL login: nothing to translate
        } else {
            $targetServer = $parts.Server
            if ($parts.Database) { $targetDb = $parts.Database }
        }
    }

    if ($asIs) {
        $c = New-Object System.Data.SqlClient.SqlConnection($asIs)
        $c.Open()
        return $c
    }

    if ($targetServer -notmatch '^(?i)tcp:') { $targetServer = "tcp:$targetServer" }
    if ($targetServer -notmatch ',\d+$')     { $targetServer = "$targetServer,1433" }
    $cs = "Server=$targetServer;Database=$targetDb;Encrypt=True;TrustServerCertificate=False;Connect Timeout=60;"
    $token = Get-SSAccessToken

    # Errors a paused, resuming, failing-over or throttled Azure SQL database answers a login with
    # (the same list as ProseSqlResilience.IsTransientOpenFailure).
    $transient = @(40613, 40197, 40501, 40540, 40143, 49918, 49919, 49920, 10928, 10929, 4221, 615, -2, -1, 53, 64, 121, 233, 10053, 10054, 10060)
    $clock = [System.Diagnostics.Stopwatch]::StartNew()
    for ($attempt = 1; ; $attempt++) {
        $c = New-Object System.Data.SqlClient.SqlConnection($cs)
        $c.AccessToken = $token
        try {
            $c.Open()
            return $c
        } catch {
            $c.Dispose()
            $number = Get-SSSqlErrorNumber $_
            if ($null -eq $number -or $transient -notcontains $number -or $clock.Elapsed.TotalSeconds -ge $script:SSOpenMaxWait) {
                throw ("SS-DB: cannot open '$targetDb' on $targetServer" + $(if ($null -ne $number) { " (error $number)" }) + ": " + $_.Exception.Message)
            }
            $delay = [Math]::Min(10, 2 * $attempt)
            Write-Host ("    gspl_db: '$targetDb' is not available yet (error $number, probably resuming); attempt $attempt failed, retrying in ${delay}s")
            Start-Sleep -Seconds $delay
        }
    }
}

function Get-SSIdentityColumns {
    param([Parameter(Mandatory)]$Conn, [Parameter(Mandatory)][string]$Table)
    if ($script:SSIdentityCache.ContainsKey($Table)) { return $script:SSIdentityCache[$Table] }
    $cmd = $Conn.CreateCommand()
    $cmd.CommandText = "SELECT c.name FROM sys.columns c WHERE c.object_id = OBJECT_ID(@t) AND c.is_identity = 1"
    [void]$cmd.Parameters.AddWithValue('@t', $Table)
    $rd = $cmd.ExecuteReader()
    $cols = New-Object System.Collections.Generic.List[string]
    while ($rd.Read()) { $cols.Add($rd.GetString(0)) }
    $rd.Close()
    # Computed and rowversion columns cannot be inserted into either.
    $cmd2 = $Conn.CreateCommand()
    $cmd2.CommandText = "SELECT c.name FROM sys.columns c WHERE c.object_id = OBJECT_ID(@t) AND (c.is_computed = 1 OR c.generated_always_type <> 0)"
    [void]$cmd2.Parameters.AddWithValue('@t', $Table)
    $rd2 = $cmd2.ExecuteReader()
    while ($rd2.Read()) { $cols.Add($rd2.GetString(0)) }
    $rd2.Close()
    $script:SSIdentityCache[$Table] = $cols
    return $cols
}

# Runs a non-query and FAILS LOUDLY. Returns rows affected.
# -Expect asserts an exact row count; -AtLeast asserts a minimum.
function Invoke-SSNonQuery {
    param(
        [Parameter(Mandatory)]$Conn,
        [Parameter(Mandatory)][string]$Sql,
        [hashtable]$Params = @{},
        [int]$Expect = -1,
        [int]$AtLeast = -1,
        [string]$What = 'statement'
    )
    $cmd = $Conn.CreateCommand()
    $cmd.CommandText = $Sql
    foreach ($k in $Params.Keys) {
        $v = $Params[$k]
        if ($null -eq $v) { [void]$cmd.Parameters.AddWithValue("@$k", [System.DBNull]::Value) }
        else { [void]$cmd.Parameters.AddWithValue("@$k", $v) }
    }
    try {
        $rows = $cmd.ExecuteNonQuery()
    } catch {
        # Re-throw as a terminating error so the caller cannot sail past it.
        throw ("SS-DB FAILED [$What]: " + $_.Exception.Message + "`n  SQL: " + ($Sql -replace '\s+', ' ').Trim())
    }
    if ($Expect -ge 0 -and $rows -ne $Expect) {
        throw "SS-DB WRONG ROW COUNT [$What]: expected $Expect, got $rows"
    }
    if ($AtLeast -ge 0 -and $rows -lt $AtLeast) {
        throw "SS-DB WRONG ROW COUNT [$What]: expected at least $AtLeast, got $rows"
    }
    return $rows
}

function Invoke-SSScalar {
    param([Parameter(Mandatory)]$Conn, [Parameter(Mandatory)][string]$Sql, [hashtable]$Params = @{})
    $cmd = $Conn.CreateCommand()
    $cmd.CommandText = $Sql
    foreach ($k in $Params.Keys) { [void]$cmd.Parameters.AddWithValue("@$k", $Params[$k]) }
    try { return $cmd.ExecuteScalar() }
    catch { throw ("SS-DB FAILED [scalar]: " + $_.Exception.Message + "`n  SQL: " + ($Sql -replace '\s+', ' ').Trim()) }
}

# INSERT that automatically omits IDENTITY / computed columns, then verifies one row landed.
# Pass -Quiet to suppress the note about which columns were dropped.
function New-SSRow {
    param(
        [Parameter(Mandatory)]$Conn,
        [Parameter(Mandatory)][string]$Table,
        [Parameter(Mandatory)][hashtable]$Values,
        [switch]$Quiet
    )
    $identity = Get-SSIdentityColumns $Conn $Table
    $use = @{}
    $dropped = @()
    foreach ($k in $Values.Keys) {
        if ($identity -contains $k) { $dropped += $k; continue }
        $use[$k] = $Values[$k]
    }
    if ($dropped.Count -gt 0 -and -not $Quiet) {
        Write-Host ("    note: $Table." + ($dropped -join ',') + " is IDENTITY/computed - value ignored, DB assigns it")
    }
    if ($use.Count -eq 0) { throw "SS-DB: nothing to insert into $Table (all supplied columns were identity/computed)" }
    $cols = @($use.Keys)
    $sql = "INSERT INTO [$Table] (" + (($cols | ForEach-Object { "[$_]" }) -join ', ') + ") VALUES (" + (($cols | ForEach-Object { "@$_" }) -join ', ') + ")"
    return Invoke-SSNonQuery $Conn $sql $use -Expect 1 -What "insert $Table"
}

# Post-write verification. Throws unless the query returns the expected count.
function Assert-SSCount {
    param(
        [Parameter(Mandatory)]$Conn,
        [Parameter(Mandatory)][string]$Sql,
        [hashtable]$Params = @{},
        [Parameter(Mandatory)][int]$Expect,
        [string]$What = 'post-write check'
    )
    $got = [int](Invoke-SSScalar $Conn $Sql $Params)
    if ($got -ne $Expect) { throw "SS-DB VERIFY FAILED [$What]: expected $Expect, found $got" }
    Write-Host ("    verified: $What = $got")
}

function Get-SSSha256Hex {
    param([Parameter(Mandatory)][string]$Text)
    $sha = [System.Security.Cryptography.SHA256]::Create()
    return ([System.BitConverter]::ToString($sha.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($Text.Trim()))) -replace '-', '').ToLower()
}
