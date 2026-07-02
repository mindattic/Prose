#Requires -Version 5.1
<#
.SYNOPSIS
    StreetSamurai ML Pipeline — interactive launcher
.DESCRIPTION
    Menu-driven runner for:
      • Gripe topic clustering  (what reviewers keep flagging)
      • Register bleed detection (wrong-protagonist vocabulary)
    Run ml.bat to open this in a new window, or dot-source directly.
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = "Continue"   # let Python errors surface, don't stop the menu

$MLRoot  = $PSScriptRoot   # v3/ml/
$Python  = Join-Path $MLRoot ".venv\Scripts\python.exe"

if (-not (Test-Path $Python)) {
    Write-Host ""
    Write-Host "  Python venv not found at:" -ForegroundColor Red
    Write-Host "  $Python" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "  Set it up first:" -ForegroundColor DarkGray
    Write-Host "    cd v3\ml" -ForegroundColor DarkGray
    Write-Host "    python -m venv .venv" -ForegroundColor DarkGray
    Write-Host "    .venv\Scripts\activate" -ForegroundColor DarkGray
    Write-Host "    pip install -r requirements.txt" -ForegroundColor DarkGray
    Write-Host ""
    Read-Host "Press Enter to exit"
    exit 1
}

# ── helpers ──────────────────────────────────────────────────────────────────

function Invoke-ML {
    param([string]$Script, [string[]]$MLArgs = @())
    Write-Host ""
    Push-Location $MLRoot
    try { & $Python $Script @MLArgs }
    finally { Pop-Location }
    Write-Host ""
    Write-Host "  Done. Press any key..." -ForegroundColor DarkGray
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
}

function Get-Strands {
    $rows = sqlcmd -S "(localdb)\MSSQLLocalDB" -d StreetSamurai `
        -Q "SET NOCOUNT ON; SELECT Slug FROM Strands WHERE IsWIP = 0 ORDER BY Slug" `
        -h -1 2>$null
    return @($rows | Where-Object { $_ -and $_.Trim() -ne "" } | ForEach-Object { $_.Trim() })
}

function Show-Header {
    Clear-Host
    Write-Host ""
    Write-Host "  ╔══════════════════════════════════════╗" -ForegroundColor Cyan
    Write-Host "  ║    StreetSamurai  ML  Pipeline       ║" -ForegroundColor Cyan
    Write-Host "  ╚══════════════════════════════════════╝" -ForegroundColor Cyan
    Write-Host ""
}

function Pick-Item {
    param(
        [string]   $Prompt,
        [string[]] $Items,
        [switch]   $AllowAll,
        [switch]   $AllowBack
    )
    while ($true) {
        Write-Host ""
        Write-Host "  $Prompt" -ForegroundColor Yellow
        Write-Host ""
        for ($i = 0; $i -lt $Items.Count; $i++) {
            Write-Host ("  {0,2}. {1}" -f ($i + 1), $Items[$i])
        }
        if ($AllowAll)  { Write-Host "   A. All strands" -ForegroundColor DarkGray }
        if ($AllowBack) { Write-Host "   B. Back"        -ForegroundColor DarkGray }
        Write-Host ""
        $c = (Read-Host "  ›").Trim()
        if ($AllowAll  -and $c -match "^[aA]$") { return "__ALL__"  }
        if ($AllowBack -and $c -match "^[bB]$") { return "__BACK__" }
        if ($c -match "^\d+$") {
            $idx = [int]$c - 1
            if ($idx -ge 0 -and $idx -lt $Items.Count) { return $Items[$idx] }
        }
        Write-Host "  Invalid — try again." -ForegroundColor Red
    }
}

# ── audit sub-flow ───────────────────────────────────────────────────────────

function Run-Audit {
    param([string]$Strand = "")
    $types = @(
        "Gripes  — recurring patterns in reviewer feedback",
        "Register — wrong-protagonist vocabulary (bleed)",
        "Both"
    )
    $pick = Pick-Item -Prompt "What to audit?" -Items $types -AllowBack
    if ($pick -eq "__BACK__") { return }

    $auditFlag = switch ($pick) {
        $types[0] { "--gripes"   }
        $types[1] { "--register" }
        default   { "--all"      }
    }

    $args = @($auditFlag)
    if ($Strand -and $Strand -ne "__ALL__") { $args += @("--strand", $Strand) }

    Invoke-ML -Script "audit\beat_auditor.py" -MLArgs $args
}

# ── main menu ─────────────────────────────────────────────────────────────────

$running = $true
while ($running) {
    Show-Header
    Write-Host "  1. Full pipeline         extract → train → audit all strands" -ForegroundColor White
    Write-Host "  2. Extract data          refresh gripes + beats from DB"
    Write-Host "  3. Train models          topics + register classifier"
    Write-Host "  4. Audit — all strands   gripes and/or register"
    Write-Host "  5. Audit — pick strand   choose one strand to audit"
    Write-Host "  6. Register fingerprint  top vocabulary words per strand"
    Write-Host "  7. Single phase..."
    Write-Host ""
    Write-Host "  Q. Quit" -ForegroundColor DarkGray
    Write-Host ""
    $choice = (Read-Host "  ›").Trim().ToUpper()

    switch ($choice) {

        "1" {
            Invoke-ML -Script "orchestrate\nightly_run.py" -MLArgs @("--phases", "all")
        }

        "2" {
            Invoke-ML -Script "orchestrate\nightly_run.py" -MLArgs @("--phases", "extract_gripes,extract_beats")
        }

        "3" {
            Invoke-ML -Script "orchestrate\nightly_run.py" -MLArgs @("--phases", "train_topics,train_register")
        }

        "4" {
            Run-Audit
        }

        "5" {
            $strands = Get-Strands
            if (-not $strands) { Write-Host "  Could not load strands." -ForegroundColor Red; Start-Sleep 2; break }
            $strand = Pick-Item -Prompt "Pick a strand:" -Items $strands -AllowBack
            if ($strand -eq "__BACK__") { break }
            Run-Audit -Strand $strand
        }

        "6" {
            $strands = Get-Strands
            if (-not $strands) { Write-Host "  Could not load strands." -ForegroundColor Red; Start-Sleep 2; break }
            $strand = Pick-Item -Prompt "Show fingerprint for:" -Items $strands -AllowBack
            if ($strand -eq "__BACK__") { break }
            Invoke-ML -Script "models\register_classifier.py" -MLArgs @("--top-words", $strand)
        }

        "7" {
            $phases = @(
                "extract_gripes  — pull reviewer feedback from DB",
                "extract_beats   — pull beat texts from DB",
                "train_topics    — BERTopic gripe clustering",
                "train_register  — register bleed classifier",
                "audit_gripes    — write gripe Findings",
                "audit_register  — write register bleed Findings"
            )
            $pick = Pick-Item -Prompt "Run which phase?" -Items $phases -AllowBack
            if ($pick -eq "__BACK__") { break }
            $phase = ($pick -split "\s+")[0]   # strip description

            $phaseArgs = @("--phases", $phase)
            if ($phase -match "^audit_") {
                $strands = Get-Strands
                if ($strands) {
                    $strand = Pick-Item -Prompt "Strand to audit:" -Items $strands -AllowAll -AllowBack
                    if ($strand -eq "__BACK__") { break }
                    if ($strand -ne "__ALL__") { $phaseArgs += @("--strand", $strand) }
                }
            }
            Invoke-ML -Script "orchestrate\nightly_run.py" -MLArgs $phaseArgs
        }

        "Q" { $running = $false }

        default {
            Write-Host "  Invalid choice." -ForegroundColor Red
            Start-Sleep -Milliseconds 400
        }
    }
}

Write-Host ""
Write-Host "  Bye." -ForegroundColor DarkGray
Write-Host ""
