[CmdletBinding()]
param([string]$GlobalClaudeRoot = "$env:USERPROFILE\.claude",[string]$OutputPath)
$ErrorActionPreference='Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($OutputPath)) { $OutputPath = Join-Path $repoRoot 'docs\agent\command-catalog.json' }
$shared = Join-Path (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)) 'mindattic-agent-standard\export-catalog.ps1'
if (-not (Test-Path -LiteralPath $shared)) { throw "Shared catalog exporter not found: $shared" }
& $shared -WorkspaceRoot (Split-Path -Parent $repoRoot) -GlobalClaudeRoot $GlobalClaudeRoot -OutputPath $OutputPath
exit $LASTEXITCODE
