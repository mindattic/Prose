<#
.SYNOPSIS
    Make a working local Prose database in Docker (RA-T4.4.1.1). PowerShell twin of create-local.sh.
.DESCRIPTION
    The migration history does not replay on an empty server (an early migration indexes Nodes.NodeCode while
    it is nvarchar(max)), so this builds the schema from the CURRENT model, stamps every migration EF knows as
    applied (so the Hub's start-up MigrateAsync finds nothing pending), and seeds the GLMZ universe row.
    Needs docker, the .NET SDK and dotnet-ef.
.EXAMPLE
    ./tools/db/create-local.ps1 -Database ProseLocal
#>
param(
    [string] $Database = 'ProseLocal',
    [string] $Container = 'prose-sql',
    [string] $Password = 'Pr0se!Local#2026'
)
$ErrorActionPreference = 'Stop'
$root = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$work = New-Item -ItemType Directory -Path (Join-Path ([IO.Path]::GetTempPath()) ([guid]::NewGuid()))
try {
    $names = docker ps -a --format '{{.Names}}'
    if ($names -notcontains $Container) {
        docker run -d --name $Container -e ACCEPT_EULA=Y -e "MSSQL_SA_PASSWORD=$Password" -p 1433:1433 mcr.microsoft.com/mssql/server:2022-latest | Out-Null
    } else { docker start $Container | Out-Null }
    $sql = @('exec', $Container, '/opt/mssql-tools18/bin/sqlcmd', '-C', '-I', '-S', 'localhost', '-U', 'sa', '-P', $Password)
    for ($i = 0; $i -lt 60; $i++) { docker @sql -Q 'SELECT 1' *> $null; if ($LASTEXITCODE -eq 0) { break }; Start-Sleep 2 }

    $env:ConnectionStrings__Prose = 'Server=localhost;Database=x;User Id=x;Password=y;TrustServerCertificate=True'
    Push-Location (Join-Path $root 'src')
    try {
        Write-Host '[create-local] schema from the model'
        dotnet ef dbcontext script --context ProseDbContext --project Prose.Core --startup-project Prose.Core -o (Join-Path $work 'schema.sql') | Out-Null
        Write-Host '[create-local] migration history'
        $ids = dotnet ef migrations list --no-connect --context ProseDbContext --project Prose.Core --startup-project Prose.Core 2>$null |
            Where-Object { $_ -match '^\d{14}_' } | ForEach-Object { $_ -replace ' \(Pending\)', '' }
    } finally { Pop-Location; Remove-Item Env:ConnectionStrings__Prose }

    $stamp = @('CREATE TABLE [__EFMigrationsHistory] ([MigrationId] nvarchar(150) NOT NULL PRIMARY KEY, [ProductVersion] nvarchar(32) NOT NULL);')
    $stamp += $ids | ForEach-Object { "INSERT INTO [__EFMigrationsHistory] VALUES ('$_','10.0');" }
    $stamp += "INSERT INTO [Universe] (Id, Slug, Name, IsActive, SortKey, CreatedAt) VALUES ('0197e9c9-0001-7000-8000-000000000001','glmz','GLMZ',1,100,SYSUTCDATETIME());"
    Set-Content -Path (Join-Path $work 'stamp.sql') -Value $stamp -Encoding utf8

    docker cp (Join-Path $work 'schema.sql') "${Container}:/tmp/prose-schema.sql" | Out-Null
    docker cp (Join-Path $work 'stamp.sql') "${Container}:/tmp/prose-stamp.sql" | Out-Null
    docker @sql -Q "IF DB_ID('$Database') IS NOT NULL BEGIN ALTER DATABASE [$Database] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$Database]; END; CREATE DATABASE [$Database];" | Out-Null
    docker @sql -d $Database -i /tmp/prose-schema.sql -b | Out-Null
    docker @sql -d $Database -i /tmp/prose-stamp.sql -b | Out-Null
    Write-Host "[create-local] $Database ready"
    Write-Host "`$env:ConnectionStrings__Prose = 'Server=localhost,1433;Database=$Database;User Id=sa;Password=$Password;TrustServerCertificate=True;Encrypt=False'"
} finally { Remove-Item -Recurse -Force $work }
