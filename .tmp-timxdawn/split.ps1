$mdPath = 'D:\Projects\MindAttic\Prose\docs\books\tim-and-dawn-debraal-memoir.md'
$outDir = 'D:\Projects\MindAttic\Prose\.tmp-timxdawn'

$raw = [System.IO.File]::ReadAllText($mdPath, [System.Text.Encoding]::UTF8)
$pattern = [regex]'(?m)^## (.+?)\r?$'
$matches = $pattern.Matches($raw)

$noBomUtf8 = New-Object System.Text.UTF8Encoding($false)
$manifest = @()

for ($i = 0; $i -lt $matches.Count; $i++) {
    $m = $matches[$i]
    $title = $m.Groups[1].Value.Trim()
    $bodyStart = $m.Index + $m.Length
    $bodyEnd = if ($i + 1 -lt $matches.Count) { $matches[$i + 1].Index } else { $raw.Length }
    $body = $raw.Substring($bodyStart, $bodyEnd - $bodyStart)

    $lines = $body -split "`n"
    $cleanLines = $lines | Where-Object { $_.Trim() -ne '---' }
    $cleanBody = ($cleanLines -join "`n").Trim() + "`n"

    $num = $i + 1
    $fileName = "{0:D2}.txt" -f $num
    $filePath = Join-Path $outDir $fileName
    [System.IO.File]::WriteAllText($filePath, $cleanBody, $noBomUtf8)

    $manifest += [PSCustomObject]@{
        Num   = $num
        Title = $title
        File  = $fileName
        Chars = $cleanBody.Length
    }
}

$manifest | Format-Table -AutoSize
$manifest | ConvertTo-Json | Out-File -FilePath (Join-Path $outDir 'manifest.json') -Encoding utf8
