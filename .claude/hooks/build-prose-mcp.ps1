<#
  SessionStart hook - keeps Prose.Mcp pre-built so its `--no-build` MCP registration
  (see v3/Prose.Mcp/README.md) always launches against fresh code instead of failing
  the stdio handshake on `dotnet run`'s own restore/build preamble polluting stdout.
  Incremental build - a few seconds when nothing changed. Never blocks session start:
  a build failure is logged to stderr but does not stop the session (the MCP connection
  will simply fail visibly via `claude mcp list`, same as any other build break).
#>
$ErrorActionPreference = 'Continue'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$proj     = Join-Path $repoRoot 'v3\Prose.Mcp\Prose.Mcp.csproj'

if (Test-Path $proj) {
    try {
        & dotnet build $proj --configuration Release *> $null
    } catch {
        Write-Error "[build-prose-mcp] build failed: $_"
    }
}

Write-Output '{}'
exit 0
