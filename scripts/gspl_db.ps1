# Shared DB helpers for GSPL scripts. Dot-source it:
#
#     . "$PSScriptRoot\gspl_db.ps1"
#     $conn = Open-SS
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

function Open-SS {
    param([string]$Server = '(localdb)\MSSQLLocalDB', [string]$Database = 'StreetSamurai')
    $c = New-Object System.Data.SqlClient.SqlConnection("Server=$Server;Database=$Database;Trusted_Connection=True;TrustServerCertificate=True;")
    $c.Open()
    return $c
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
