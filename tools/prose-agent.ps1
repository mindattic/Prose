# Compatibility wrapper. The implementation lives in the shared provider-neutral layer.
$repoRoot = Split-Path -Parent $PSScriptRoot
$shared = Join-Path (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)) 'mindattic-agent-standard\prose-agent.ps1'
if (-not (Test-Path -LiteralPath $shared)) { throw "Shared MindAttic Agent Standard not found: $shared" }
& $shared -RepoRoot $repoRoot @args
exit $LASTEXITCODE
