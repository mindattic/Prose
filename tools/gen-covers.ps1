#!/usr/bin/env pwsh
# tools/gen-covers.ps1 -- generate book covers for all GLMZ strands x 4 image providers

$timestamp = Get-Date -Format "yyyy-MM-ddTHH-mm-ss"
$basePath  = "R:\Desktop\EPub\MindAttic\GLMZ"
$project   = "D:\Projects\MindAttic\StreetSamurai\v3\StreetSamurai.Blazor"

$strands = @(
    @{
        Code  = "ATTE"
        Title = "Attendance"
        Prompt = "Noir cyberpunk book cover. A lone Black woman in a grey contractor coat walks through an oppressive ferrocement government complex. Cold fluorescent light bleeds through rain-streaked glass. A manila folder under her arm, empty corridors behind her. GLMZ 2225, administrative horror. Muted steel and grey palette, vertical composition, cinematic."
    }
    @{
        Code  = "VATD"
        Title = "Vultures at the Door"
        Prompt = "Noir cyberpunk book cover. Two figures in dark body-recovery gear stand beside an industrial black harvest vehicle under neon rain. Magenta and blue neon reflections pool in the wet street below ferrocement towers. GLMZ 2225 black-comedy organ-harvesting noir. Grim, cinematic, gallows-humor energy."
    }
    @{
        Code  = "DWIACE"
        Title = "Death Whispers in a Cat's Ear"
        Prompt = "Cyberpunk noir book cover. A young woman with elegant cat-ear genemods sits alone in a dark GLMZ apartment, her face lit by ghostly blue holographic light. An inhuman shadow stands behind her, wrong proportions, wrong angle. 2225 AI predator mystery. Blue-violet and deep shadow palette, eerie and intimate."
    }
    @{
        Code  = "SPRW"
        Title = "Sparrow"
        Prompt = "Cyberpunk book cover. A solitary man stands on a towering ferrocement balcony, the endless vertical city of GLMZ far below him. High above, a satellite traces a silent arc across the upper atmosphere. A thin thread of light connects them across the void. 2225, contemplative and vast. Deep blue and warm city amber."
    }
    @{
        Code  = "SRZR"
        Title = "Steppin Razor"
        Prompt = "Cyberpunk book cover. A small, dangerous young woman of Vietnamese and Eastern European heritage stands easy with two pistols at her sides, silhouetted against the overwhelming neon skyline of GLMZ. Behind her: flat frontier wasteland. Ahead: towers of impossible scale. Psychedelic neon aurora above. Don't watch her size. GLMZ 2225."
    }
    @{
        Code  = "MNEMO"
        Title = "Mnemosync"
        Prompt = "Cyberpunk book cover. Double-exposure portrait: a woman in broadcast clothes and a courier in night gear, their faces overlapping, memories bleeding between them as ghostly impressions neither can see. Clinical white and warm amber bleed together at the seam. GLMZ 2225 cognitive horror and unwanted intimacy."
    }
    @{
        Code  = "TEST"
        Title = "Testament"
        Prompt = "Cyberpunk book cover. A massive bearded man, 203 cm, military bearing, stands in civilian clothes in a stark government hearing room. Steel-blue eyes. Immovable. Behind him, a ghost image of a battlefield bleeds through in muted wartime tones. GLMZ 2225, military accountability drama. Gunmetal grey."
    }
    @{
        Code  = "MxG"
        Title = "Magenta and Gunmetal"
        Prompt = "Cyberpunk heist book cover. Five freelancer silhouettes sprint across a storm-lashed offshore platform on Lake Michigan at night. Magenta corporate neon from Axiom towers on the mainland vs gunmetal weapons and rain. A VTOL banking hard through storm above. Kinetic and cinematic. GLMZ 2225 crew heist thriller."
    }
    @{
        Code  = "TDIU"
        Title = "The Door Is Unlocked"
        Prompt = "Cyberpunk book cover. A young woman with a duffel bag steps off a high-speed transit pod into the overwhelming vertical canyon of GLMZ, ferrocement towers stretching impossibly high, neon everywhere, crowds pressing in all around her. She looks up, not afraid, just measuring. Big black work boots. Coming-of-age city arrival. Warm amber and deep city blue."
    }
)

$generators = @(
    @{ Name = "ideogram"; Label = "ideogram"; Ext = ".jpg" }
)

$total   = $strands.Count * $generators.Count
$current = 0
$errors  = @()

Write-Host ""
Write-Host "=== GLMZ Cover Generation Batch ===" -ForegroundColor Cyan
Write-Host "  Strands:    $($strands.Count)"
Write-Host "  Generators: $($generators.Count)"
Write-Host "  Total:      $total calls"
Write-Host "  Timestamp:  $timestamp"
Write-Host ""

foreach ($strand in $strands) {
    $strandDir = Join-Path $basePath $strand.Title
    if (-not (Test-Path $strandDir)) {
        New-Item -ItemType Directory -Force $strandDir | Out-Null
        Write-Host "  Created: $strandDir" -ForegroundColor DarkGray
    }

    foreach ($gen in $generators) {
        $current++
        $outFile = Join-Path $strandDir "cover ($($gen.Label)) $timestamp$($gen.Ext)"
        Write-Host "[$current/$total] $($strand.Code) + $($gen.Label) -> $(Split-Path $outFile -Leaf)"

        $dotnetArgs = @(
            "run", "--project", $project, "--",
            "--generate-cover",
            "--strand-code", $strand.Code,
            "--generator", $gen.Name,
            "--prompt", $strand.Prompt,
            "--output", $outFile
        )

        & dotnet @dotnetArgs

        if ($LASTEXITCODE -ne 0) {
            Write-Warning "  FAILED: $($strand.Code) + $($gen.Label) (exit $LASTEXITCODE)"
            $errors += "$($strand.Code) + $($gen.Label)"
        } else {
            if (Test-Path $outFile) {
                $size = (Get-Item $outFile).Length / 1KB
                Write-Host "  OK $([int]$size) KB" -ForegroundColor Green
            } else {
                Write-Host "  OK (saved to DB only)" -ForegroundColor Yellow
            }
        }
        Write-Host ""
    }
}

Write-Host ""
Write-Host "=== Done ===" -ForegroundColor Cyan
Write-Host "  $($total - $errors.Count)/$total succeeded"
if ($errors.Count -gt 0) {
    Write-Host "  Failed:" -ForegroundColor Red
    $errors | ForEach-Object { Write-Host "    - $_" -ForegroundColor Red }
}
