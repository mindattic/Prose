$ErrorActionPreference = "Continue"
$exe   = "D:\Projects\MindAttic\MindAttic.Legion\MindAttic.Legion.Cli\bin\Debug\net10.0\legion.exe"
$names = Get-Content "D:\Projects\MindAttic\StreetSamurai\v3\legion_surname_input.txt"
$out   = "D:\Projects\MindAttic\StreetSamurai\v3\legion_per_name_results.json"
$log   = "D:\Projects\MindAttic\StreetSamurai\v3\legion_per_name.log"

$results = @{}
"=== started at $(Get-Date -Format o) - $($names.Count) names ===" | Out-File $log -Encoding UTF8
$used = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::OrdinalIgnoreCase)

Push-Location "D:\Projects\MindAttic\StreetSamurai\v3"
for ($i = 0; $i -lt $names.Count; $i++) {
    $full = $names[$i].Trim()
    if (-not $full) { continue }

    $parts   = $full -split ' ', 2
    $surname = if ($parts.Length -gt 1) { $parts[1] } else { "" }
    $chunks  = $surname -split '-' | Where-Object { $_ }
    if ($chunks.Count -lt 2) {
        $results[$full] = $surname
        "$i  SKIP (no hyphens)  $full" | Out-File $log -Append -Encoding UTF8
        continue
    }

    $available = @($chunks | Where-Object { -not $used.Contains($_) })
    if ($available.Count -eq 0) { $available = $chunks }
    if ($available.Count -eq 1) {
        $pick = $available[0]
        $results[$full] = $pick
        $used.Add($pick) | Out-Null
        "$i  AUTO  $full  =>  $pick  (only one available)" | Out-File $log -Append -Encoding UTF8
        $results | ConvertTo-Json -Depth 3 | Out-File $out -Encoding UTF8
        continue
    }

    $optStr = ($available -join ',')
    $q = 'You are casting director for a cyberpunk thriller (GLMZ, 23rd century). The character "' + $full + '" currently has a multi-barreled last name. Discuss and pick the SINGLE chunk with the most sonic punch -- the surname an audience hears once and remembers, easy to say in dialogue. Trim the rest. Return one of the listed options exactly.'

    $sw   = [System.Diagnostics.Stopwatch]::StartNew()
    $pick = & $exe ask $q --options $optStr --no-auto-context --must-answer 2>$null
    $sw.Stop()
    $pick = ($pick | Out-String).Trim()

    if (-not $pick -or -not ($available -contains $pick)) {
        $pick = $available[0]
        $note = "FALLBACK"
    } else {
        $note = "VOTE"
    }

    $results[$full] = $pick
    $used.Add($pick) | Out-Null
    "$i  $note  ($($sw.Elapsed.TotalSeconds.ToString('F1'))s)  $full  =>  $pick" | Out-File $log -Append -Encoding UTF8
    $results | ConvertTo-Json -Depth 3 | Out-File $out -Encoding UTF8
}
Pop-Location

"=== finished at $(Get-Date -Format o) ===" | Out-File $log -Append -Encoding UTF8
"results: $out" | Out-File $log -Append -Encoding UTF8
