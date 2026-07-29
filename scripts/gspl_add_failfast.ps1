# Adds `$ErrorActionPreference = 'Stop'` to every gspl_*.ps1 that lacks it.
#
# Why: a SqlException from ExecuteNonQuery() surfaces as a MethodInvocationException, which
# is NON-TERMINATING by default. Without 'Stop', a failed INSERT/UPDATE is skipped, the rest
# of the script runs, and it prints "APPLIED" over a write that never landed. That is exactly
# how the PlaceAliases / DeprecatedEntityNames identity-column failures went unnoticed.
#
# Three traps this script exists to avoid, all of which bit a naive first attempt:
#
#  1. BOM. [System.IO.File]::ReadAllText() SILENTLY STRIPS the UTF-8 BOM, so testing
#     $raw[0] -eq [char]0xFEFF is always false and every file gets rewritten without one.
#     A no-BOM .ps1 is then misparsed by Windows PowerShell 5.1 and its em dashes turn to
#     mojibake (docs/GSPL.md 5g0a). So: detect the BOM from the raw BYTES, and restore it.
#  2. Line endings. Splitting and re-joining with the wrong terminator rewrites every line
#     in the file, burying the one-line change in a whole-file diff. So: detect and preserve.
#  3. param() must remain the first statement in a script, so insert AFTER any param block.
#
# Parse-validates into a temp file and only overwrites the original if it parses clean.
# Dry-run by default; pass -Apply to write.

param([switch]$Apply)
$ErrorActionPreference = 'Stop'

$dir = $PSScriptRoot
$LINE = "`$ErrorActionPreference = 'Stop'   # fail loudly: a SqlException is otherwise non-terminating"
$tmp = Join-Path $env:TEMP ("gspl_failfast_" + [System.Guid]::NewGuid().ToString('N') + ".ps1")

$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
$examined = 0; $already = 0; $done = 0; $refused = @()

foreach ($f in (Get-ChildItem "$dir\gspl_*.ps1" | Sort-Object Name)) {
    if ($f.Name -eq 'gspl_add_failfast.ps1' -or $f.Name -eq 'gspl_db.ps1') { continue }
    $examined++

    $bytes = [System.IO.File]::ReadAllBytes($f.FullName)
    $hasBom = $bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF
    $offset = if ($hasBom) { 3 } else { 0 }
    $text = $utf8NoBom.GetString($bytes, $offset, $bytes.Length - $offset)

    if ($text -match 'ErrorActionPreference') { $already++; continue }

    $crlf = ([regex]::Matches($text, "`r`n")).Count
    $bareLf = ([regex]::Matches($text, "(?<!`r)`n")).Count
    $nl = if ($crlf -ge $bareLf) { "`r`n" } else { "`n" }

    $lines = [System.Collections.Generic.List[string]]([regex]::Split($text, "`r`n|`n"))

    # first line that is neither blank nor a comment
    $i = 0
    while ($i -lt $lines.Count -and ($lines[$i].Trim() -eq '' -or $lines[$i].TrimStart().StartsWith('#'))) { $i++ }
    # step past a leading param(...) block, however many lines it spans
    if ($i -lt $lines.Count -and $lines[$i] -match '^\s*param\s*\(') {
        $depth = 0
        do {
            $depth += ([regex]::Matches($lines[$i], '\(')).Count - ([regex]::Matches($lines[$i], '\)')).Count
            $i++
        } while ($i -lt $lines.Count -and $depth -gt 0)
    }
    $lines.Insert($i, $LINE)

    $newText = ($lines -join $nl)
    $outBytes = if ($hasBom) { ,[byte]0xEF + [byte]0xBB + [byte]0xBF + $utf8NoBom.GetBytes($newText) } else { $utf8NoBom.GetBytes($newText) }

    # validate BEFORE touching the real file
    [System.IO.File]::WriteAllBytes($tmp, $outBytes)
    $errs = $null
    [void][System.Management.Automation.Language.Parser]::ParseFile($tmp, [ref]$null, [ref]$errs)
    if ($errs -and $errs.Count -gt 0) {
        $refused += ("{0}: {1} (line {2})" -f $f.Name, $errs[0].Message, $errs[0].Extent.StartLineNumber)
        continue
    }

    Write-Host ("  {0,-40} bom={1,-5} nl={2}  insert at line {3}" -f $f.Name, $hasBom, $(if ($nl -eq "`r`n") { 'CRLF' } else { 'LF' }), ($i + 1))
    if ($Apply) { [System.IO.File]::WriteAllBytes($f.FullName, $outBytes) }
    $done++
}

if (Test-Path $tmp) { Remove-Item $tmp -Force }

Write-Host ""
Write-Host ("examined            : {0}" -f $examined)
Write-Host ("already fail-fast   : {0}" -f $already)
Write-Host ("would gain fail-fast: {0}" -f $done)
Write-Host ("refused (would not parse): {0}" -f $refused.Count)
foreach ($r in $refused) { Write-Host ("  " + $r) }
Write-Host ""
if ($Apply) { Write-Host "APPLIED." } else { Write-Host "DRY RUN - nothing written. Re-run with -Apply." }
