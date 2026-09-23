# Stop: the factory guard (RFC 0015 section 6). A session may not stop while
#   (1) the repo has a changed or new file that no open engine work order's paths cover, or
#   (2) this session ran a raw sqlcmd UPDATE / INSERT / DELETE (only the Hub reaches the DB).
# A violation prints the reasons and exits 2, which sends Claude back to fix it. It blocks once per
# distinct violation set (hash kept in TEMP) and honours stop_hook_active, so it can never loop.
# Hub down: warn and allow the stop (fail open) - the guard must never trap a session.
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [Text.Encoding]::UTF8
$repo = 'D:\Projects\MindAttic\Prose'
$hub = 'http://127.0.0.1:5900'

$in = $null
try { $raw = [Console]::In.ReadToEnd(); if ($raw) { $in = $raw | ConvertFrom-Json } } catch { }

function Convert-GlobToRegex([string]$glob) {
    $g = $glob.Replace('\', '/').Trim()
    $rx = [regex]::Escape($g)
    $rx = $rx.Replace('\*\*/', '(.*/)?').Replace('\*\*', '.*').Replace('\*', '[^/]*').Replace('\?', '[^/]')
    return '^' + $rx + '$'
}

try {
    $orders = Invoke-RestMethod -Uri "$hub/api/factory/orders?status=open&kind=engine" -TimeoutSec 5
} catch {
    [Console]::Error.WriteLine("factory-guard: Hub unreachable, cannot check work orders - allowing stop. Start the Hub before the next session.")
    exit 0
}

$globs = @()
foreach ($o in @($orders)) {
    try { $globs += @(($o.pathsJson | ConvertFrom-Json)) } catch { }
}
$regexes = $globs | Where-Object { $_ } | ForEach-Object { Convert-GlobToRegex $_ }

$violations = New-Object System.Collections.Generic.List[string]

# (1) repo changes outside every open engine order
$status = & git -C $repo status --porcelain --untracked-files=all 2>$null
foreach ($line in @($status)) {
    if (-not $line -or $line.Length -lt 4) { continue }
    $p = $line.Substring(3).Trim('"')
    if ($p -match ' -> ') { $p = ($p -split ' -> ')[-1] }
    $p = $p.Replace('\', '/')
    $covered = $false
    foreach ($rx in $regexes) { if ($p -match $rx) { $covered = $true; break } }
    if (-not $covered) { $violations.Add("uncovered change: $p") }
}

# (2) raw sqlcmd writes in this session's own tool calls
$tp = "$($in.transcript_path)"
if ($tp -and (Test-Path $tp)) {
    foreach ($l in [IO.File]::ReadLines($tp)) {
        if ($l -notmatch 'sqlcmd') { continue }
        try { $e = $l | ConvertFrom-Json } catch { continue }
        foreach ($c in @($e.message.content)) {
            if ($c.type -eq 'tool_use' -and ($c.name -eq 'Bash' -or $c.name -eq 'PowerShell')) {
                $cmd = "$($c.input.command)"
                if ($cmd -match '(?i)sqlcmd' -and $cmd -match '(?i)\b(update|insert|delete|merge|drop|alter)\b') {
                    $violations.Add("raw sqlcmd write in this session: $($cmd.Substring(0, [Math]::Min(120, $cmd.Length)))")
                }
            }
        }
    }
}

if ($violations.Count -eq 0) { exit 0 }

$key = (($violations | Sort-Object) -join "`n")
$hash = [BitConverter]::ToString([Security.Cryptography.SHA256]::Create().ComputeHash([Text.Encoding]::UTF8.GetBytes($key))).Replace('-', '')
$state = Join-Path $env:TEMP ("prose-factory-guard-" + ("$($in.session_id)" -replace '[^A-Za-z0-9-]', '') + ".txt")
$last = if (Test-Path $state) { Get-Content $state -Raw } else { '' }
if ($in.stop_hook_active -and $last.Trim() -eq $hash) {
    [Console]::Error.WriteLine("factory-guard: the same $($violations.Count) violation(s) remain; allowing stop so the session cannot loop. They will block the next stop again.")
    exit 0
}
Set-Content -Path $state -Value $hash -Encoding ascii

[Console]::Error.WriteLine("FACTORY GUARD - this session cannot stop yet ($($violations.Count) violation(s)):")
foreach ($v in $violations | Select-Object -First 25) { [Console]::Error.WriteLine("  - $v") }
if ($violations.Count -gt 25) { [Console]::Error.WriteLine("  ... and $($violations.Count - 25) more") }
[Console]::Error.WriteLine("Fix: commit the work under its engine order (prose --order list), open an engine order that covers these paths under an approved root, or revert them. Never raw sqlcmd: only the Hub writes the DB.")
exit 2
