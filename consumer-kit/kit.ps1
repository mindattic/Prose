<#
.SYNOPSIS
The Prose consumer kit (RFC 0017, docs/CONDUIT.md section 6), the Windows entry point. Runs kit.py, next to this file,
with the arguments given. One copy, in the Prose repo; games invoke it by path and never vendor it.

.EXAMPLE
D:\Projects\MindAttic\Prose\consumer-kit\kit.ps1 push --map
D:\Projects\MindAttic\Prose\consumer-kit\kit.ps1 pull
D:\Projects\MindAttic\Prose\consumer-kit\kit.ps1 apply
D:\Projects\MindAttic\Prose\consumer-kit\kit.ps1 status
#>
$ErrorActionPreference = 'Stop'
$kit = Join-Path $PSScriptRoot 'kit.py'
$python = $null
foreach ($candidate in @('python', 'py', 'python3')) {
  $cmd = Get-Command $candidate -ErrorAction SilentlyContinue
  if ($cmd) { $python = $cmd.Source; break }
}
if (-not $python) { Write-Error 'The Prose consumer kit needs Python 3.9+ on PATH (python, py or python3).'; exit 2 }
$env:PYTHONUTF8 = '1'
& $python -I $kit @args
exit $LASTEXITCODE
