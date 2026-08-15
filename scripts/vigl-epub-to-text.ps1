# Convert one VIGL.epub chapter XHTML back to the plain beat text the DB stores.
# Used to recover beats destroyed by the 2026-08-13 self-healing-audit truncation.
# Validate against an INTACT chapter before trusting it on a damaged one.
param(
    [Parameter(Mandatory = $true)][string]$XhtmlPath,
    [string]$OutFile
)

$ErrorActionPreference = 'Stop'

$raw = [System.IO.File]::ReadAllText($XhtmlPath, [System.Text.Encoding]::UTF8)

# Keep only the <body>, then drop the chapter heading — the DB text has no title line.
$body = [regex]::Match($raw, '(?s)<body[^>]*>(.*?)</body>').Groups[1].Value
$body = [regex]::Replace($body, '(?s)<h[1-6][^>]*>.*?</h[1-6]>', '')

# Pull paragraphs in document order.
$paras = [regex]::Matches($body, '(?s)<p[^>]*>(.*?)</p>') | ForEach-Object { $_.Groups[1].Value }

$out = foreach ($p in $paras) {
    $s = $p
    # Inline emphasis in the DB is markdown, not tags.
    $s = [regex]::Replace($s, '(?s)<(strong|b)>(.*?)</\1>', '**$2**')
    $s = [regex]::Replace($s, '(?s)<(em|i)>(.*?)</\1>',     '*$2*')
    $s = [regex]::Replace($s, '<br\s*/?>', "`n")
    $s = [regex]::Replace($s, '<[^>]+>', '')          # strip any residual tags
    $s = [System.Net.WebUtility]::HtmlDecode($s)
    $s.Trim()
}

$text = ($out -join "`n`n").Trim()

if ($OutFile) {
    [System.IO.File]::WriteAllText($OutFile, $text, (New-Object System.Text.UTF8Encoding $false))
}

"paragraphs : $($paras.Count)"
"chars      : $($text.Length)"
"em-dashes  : $(([regex]::Matches($text,[char]0x2014)).Count)"
"head       : $($text.Substring(0,[Math]::Min(140,$text.Length)))"
