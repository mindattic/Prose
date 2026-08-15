$ErrorActionPreference = 'Continue'
$logDir = 'D:\Projects\MindAttic\Prose\logs\autocorrect'
$logFile = Join-Path $logDir ('autocorrect_{0:yyyy-MM-dd_HHmmss}.log' -f (Get-Date))
Set-Location 'D:\Projects\MindAttic\Prose'
dotnet run --project 'D:\Projects\MindAttic\Prose\v3\Prose.Cli' -- --auto-correct-nightly --dry-run *>&1 | Tee-Object -FilePath $logFile
