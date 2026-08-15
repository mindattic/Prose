# Recover a VIGL beat destroyed by the 2026-08-13 self-healing-audit regeneration.
#
# Source of truth: the KDP upload artifact VIGL.epub (2026-08-12 10:00), which was proven
# byte-identical to the pre-damage Beats.Text once emphasis markers are dropped:
#   ch-010 epub 47182 - 46 asterisks = 47136  (pre-damage DB length)
#   ch-023 epub 76872 -  4 asterisks = 76868
#   ch-024 epub 80848 -  2 asterisks = 80846
# so this writes emphasis-tag inner text with NO markdown markers, and refuses to emit a
# file unless the reconstructed length matches -ExpectLength exactly.
param(
    [Parameter(Mandatory = $true)][string]$XhtmlPath,
    [Parameter(Mandatory = $true)][int]$ExpectLength,
    [Parameter(Mandatory = $true)][string]$OutFile
)

$ErrorActionPreference = 'Stop'

$raw  = [System.IO.File]::ReadAllText($XhtmlPath, [System.Text.Encoding]::UTF8)
$body = [regex]::Match($raw, '(?s)<body[^>]*>(.*?)</body>').Groups[1].Value
$body = [regex]::Replace($body, '(?s)<h[1-6][^>]*>.*?</h[1-6]>', '')

$paras = [regex]::Matches($body, '(?s)<p[^>]*>(.*?)</p>') | ForEach-Object { $_.Groups[1].Value }

$out = foreach ($p in $paras) {
    $s = $p
    $s = [regex]::Replace($s, '<br\s*/?>', "`n")
    $s = [regex]::Replace($s, '<[^>]+>', '')      # drop ALL tags, emphasis included, no markers
    $s = [System.Net.WebUtility]::HtmlDecode($s)
    $s.Trim()
}

$text = ($out -join "`n`n").Trim()

"paragraphs : $($paras.Count)"
"chars      : $($text.Length)"
"expected   : $ExpectLength"

if ($text.Length -ne $ExpectLength) {
    throw "Length mismatch: got $($text.Length), expected $ExpectLength. Refusing to write."
}

[System.IO.File]::WriteAllText($OutFile, $text, (New-Object System.Text.UTF8Encoding $false))
"MATCH - wrote $OutFile"
