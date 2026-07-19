<#
.SYNOPSIS
    Export a self-contained HTML artifact to Downloads as a .htm file.
    Inlines any external CSS/JS references found in the source file.

.PARAMETER Source
    Path to the source .html file (required).

.PARAMETER Name
    Output filename without extension. Defaults to the source filename with .htm extension.

.EXAMPLE
    .\export-artifact.ps1 -Source "C:\...\scratchpad\report.html" -Name "ldgr-prose-report"
#>
param(
    [Parameter(Mandatory)][string]$Source,
    [string]$Name
)

$outputName = if ($Name) { "$Name.htm" } else {
    [IO.Path]::GetFileNameWithoutExtension($Source) + ".htm"
}
$outputPath = "C:\Users\ryand\Downloads\$outputName"

if (-not (Test-Path $Source)) {
    Write-Error "Source file not found: $Source"
    exit 1
}

$content = [IO.File]::ReadAllText($Source, [Text.Encoding]::UTF8)

# Inline external CSS
$content = [regex]::Replace($content, '(?i)<link\s[^>]*rel=["\x27]stylesheet["\x27][^>]*href=["\x27]([^"\x27]+)["\x27][^>]*>', {
    param($m)
    $href = $m.Groups[1].Value
    if ($href -match '^https?://') {
        try {
            $css = (Invoke-WebRequest -Uri $href -UseBasicParsing -TimeoutSec 10).Content
            "<style>$css</style>"
        } catch { $m.Value }
    } else { $m.Value }
})

# Inline external JS
$content = [regex]::Replace($content, '(?i)<script\s+src=["\x27]([^"\x27]+)["\x27][^>]*></script>', {
    param($m)
    $src = $m.Groups[1].Value
    if ($src -match '^https?://') {
        try {
            $js = (Invoke-WebRequest -Uri $src -UseBasicParsing -TimeoutSec 10).Content
            "<script>$js</script>"
        } catch { $m.Value }
    } else { $m.Value }
})

[IO.File]::WriteAllText($outputPath, $content, [Text.Encoding]::UTF8)
Write-Host "Exported: $outputPath ($([Math]::Round((Get-Item $outputPath).Length / 1KB, 1)) KB)"
