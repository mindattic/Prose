#Requires -Version 5.1
<#
.SYNOPSIS
    StreetSamurai ML & Canon Audit console.

RECOMMENDED FIRST-RUN ORDER:  4 -> 7 -> 8 -> 1
    4  seeds BeatProseMetrics (required by 8 and morning report section 3)
    7  seeds near-duplicate Findings (required by morning report section 4)
    8  trains the score correlation model (skip if no review scores yet)
    1  reads the morning report once everything is seeded
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = "Continue"

$ROOT    = Split-Path $PSScriptRoot -Parent
$ML_ROOT = Join-Path $ROOT "v3\ml"
$VENV    = Join-Path $ML_ROOT ".venv\Scripts\Activate.ps1"
$LOG     = Join-Path $ROOT "v3\ml_nightly.log"
$SS      = Join-Path $ROOT "ss.cmd"

# ── Display helpers ────────────────────────────────────────────────────────────

function Write-Header {
    Clear-Host
    Write-Host ""
    Write-Host "  +================================================================+" -ForegroundColor Cyan
    Write-Host "  |         StreetSamurai  ML & Canon Audit Suite                  |" -ForegroundColor Cyan
    Write-Host "  +================================================================+" -ForegroundColor Cyan
    Write-Host "  |  First run?  Fire in order:  4 -> 7 -> 8 -> 1                 |" -ForegroundColor Yellow
    Write-Host "  |  Every morning:              1  (or 0 to refresh everything)   |" -ForegroundColor Yellow
    Write-Host "  +================================================================+" -ForegroundColor Cyan
    Write-Host ""
}

function Write-Menu {
    Write-Host "  -- Reports ---------------------------------------------------------" -ForegroundColor DarkGray
    Write-Host ""
    Write-Host "   [1]  Morning Report - last 24 h" -ForegroundColor White
    Write-Host "        6 sections: cross-story contradictions, new findings, prose" -ForegroundColor DarkGray
    Write-Host "        outliers, near-dupes, score model summary, score leaderboard." -ForegroundColor DarkGray
    Write-Host "        Run this every morning. Needs sections 4, 7, 8 seeded first." -ForegroundColor DarkGray
    Write-Host ""
    Write-Host "   [2]  Morning Report - last 7 days" -ForegroundColor White
    Write-Host "        Same report over a full week. Use after a gap or after the" -ForegroundColor DarkGray
    Write-Host "        first-run seed sequence [F] to see everything at once." -ForegroundColor DarkGray
    Write-Host ""
    Write-Host "   [3]  View nightly log (last 60 lines)" -ForegroundColor White
    Write-Host "        Raw output from last night's Task Scheduler run (1 AM)." -ForegroundColor DarkGray
    Write-Host "        Look for 'Exit code: 0' at the bottom. Stack traces above" -ForegroundColor DarkGray
    Write-Host "        it are phases that failed and need attention." -ForegroundColor DarkGray
    Write-Host ""
    Write-Host "  -- .NET CLI tasks (fast, no Python) --------------------------------" -ForegroundColor DarkGray
    Write-Host ""
    Write-Host "   [4]  Compute prose metrics - ALL beats                    ~30 sec" -ForegroundColor White
    Write-Host "        Calculates TTR, MTLD, Flesch-Kincaid, dialogue % for every" -ForegroundColor DarkGray
    Write-Host "        enabled beat across all stories. Saves to BeatProseMetrics" -ForegroundColor DarkGray
    Write-Host "        table. Run this FIRST on a fresh install. Required before" -ForegroundColor DarkGray
    Write-Host "        [8] (score model) and morning report section 3 (outliers)." -ForegroundColor DarkGray
    Write-Host ""
    Write-Host "   [5]  Compute prose metrics - single story                  ~5 sec" -ForegroundColor White
    Write-Host "        Same as [4] but for one slug only. Use after writing or" -ForegroundColor DarkGray
    Write-Host "        editing beats in a specific story to refresh just that one." -ForegroundColor DarkGray
    Write-Host ""
    Write-Host "   [6]  Cross-story consistency audit" -ForegroundColor White
    Write-Host "        Finds facts that contradict between novels - e.g. a" -ForegroundColor DarkGray
    Write-Host "        character's age differs across two stories. Exit 0 = clean," -ForegroundColor DarkGray
    Write-Host "        exit 1 = conflicts found. Results also appear in the morning" -ForegroundColor DarkGray
    Write-Host "        report section 1. Conflicts need a human call on which story" -ForegroundColor DarkGray
    Write-Host "        is canon." -ForegroundColor DarkGray
    Write-Host ""
    Write-Host "  -- Python pipeline (GPU-free, local CPU) ---------------------------" -ForegroundColor DarkGray
    Write-Host ""
    Write-Host "   [7]  Find near-duplicates                                   ~5 min" -ForegroundColor White
    Write-Host "        Embeds all beats with MiniLM, then does pairwise cosine" -ForegroundColor DarkGray
    Write-Host "        similarity. Flags cross-story pairs above 0.92 similarity" -ForegroundColor DarkGray
    Write-Host "        into the Findings table. First run is slow (model download" -ForegroundColor DarkGray
    Write-Host "        + embedding ~1500 beats). Run AFTER [4]." -ForegroundColor DarkGray
    Write-Host ""
    Write-Host "   [8]  Score correlation model                                ~1 min" -ForegroundColor White
    Write-Host "        Trains gradient-boosting on prose features vs review scores." -ForegroundColor DarkGray
    Write-Host "        Needs >= 20 reviewed beats to produce meaningful output." -ForegroundColor DarkGray
    Write-Host "        Writes feature-importance report to AppData\MindAttic\ML\." -ForegroundColor DarkGray
    Write-Host "        Skip this if no review scores exist yet." -ForegroundColor DarkGray
    Write-Host ""
    Write-Host "   [9]  Canon Audit phases only (4 + 7 + 8 together)          ~6 min" -ForegroundColor White
    Write-Host "        Runs compute_metrics, find_near_dupes, score_correlation in" -ForegroundColor DarkGray
    Write-Host "        sequence. Use this to refresh all three Canon Audit outputs" -ForegroundColor DarkGray
    Write-Host "        without touching the topic/register ML models." -ForegroundColor DarkGray
    Write-Host ""
    Write-Host "   [0]  Full nightly pipeline - all 9 phases                 ~15 min" -ForegroundColor Yellow
    Write-Host "        Runs everything: gripe extraction, topic model, register" -ForegroundColor DarkGray
    Write-Host "        classifier, gripe/register audits, plus all three Canon" -ForegroundColor DarkGray
    Write-Host "        Audit phases. Identical to what Task Scheduler fires at 1 AM." -ForegroundColor DarkGray
    Write-Host "        Use this for a full mid-day refresh." -ForegroundColor DarkGray
    Write-Host ""
    Write-Host "   [F]  First-run seed sequence (4 -> 7 -> 8 -> 1)          ~20 min" -ForegroundColor Green
    Write-Host "        Runs compute metrics, near-dupe detection, score model, then" -ForegroundColor DarkGray
    Write-Host "        prints the 7-day morning report. Run this once on a fresh" -ForegroundColor DarkGray
    Write-Host "        install or after adding a large batch of new beats." -ForegroundColor DarkGray
    Write-Host ""
    Write-Host "   [Q]  Quit" -ForegroundColor DarkGray
    Write-Host ""
}

# ── Task runners ───────────────────────────────────────────────────────────────

function Invoke-SS {
    param([string[]]$SsArgs)
    Write-Host ""
    Write-Host "  > ss $SsArgs" -ForegroundColor DarkCyan
    Write-Host ""
    & $SS @SsArgs
}

function Invoke-Python {
    param([string]$Phases)
    Write-Host ""
    Write-Host "  > nightly_run.py --phases $Phases" -ForegroundColor DarkCyan
    Write-Host ""
    Push-Location $ML_ROOT
    try {
        & $VENV
        $env:PYTHONIOENCODING = "utf-8"
        python orchestrate\nightly_run.py --phases $Phases
    } finally {
        Pop-Location
    }
}

function Pause-After {
    Write-Host ""
    Write-Host "  -------------------------------------------------------------------" -ForegroundColor DarkGray
    Write-Host "  Press any key to return to menu..." -ForegroundColor DarkGray
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
}

# ── Main loop ──────────────────────────────────────────────────────────────────

while ($true) {
    Write-Header
    Write-Menu

    $choice = Read-Host "  Choice"

    switch ($choice.Trim().ToUpper()) {

        "1" {
            Write-Header
            Invoke-SS "--morning-report", "--since", "24"
            Pause-After
        }

        "2" {
            Write-Header
            Invoke-SS "--morning-report", "--since", "168"
            Pause-After
        }

        "3" {
            Write-Header
            Write-Host ""
            if (Test-Path $LOG) {
                Get-Content $LOG -Tail 60
            } else {
                Write-Host "  Log not found: $LOG" -ForegroundColor Red
                Write-Host "  The nightly job may not have run yet." -ForegroundColor DarkGray
            }
            Pause-After
        }

        "4" {
            Write-Header
            Invoke-SS "--compute-metrics", "--all"
            Pause-After
        }

        "5" {
            Write-Header
            $slug = (Read-Host "  Story slug (e.g. bcoda, sasha_v, rtr)").Trim()
            if ($slug) {
                Invoke-SS "--compute-metrics", "--slug", $slug
            } else {
                Write-Host "  No slug entered - cancelled." -ForegroundColor Yellow
            }
            Pause-After
        }

        "6" {
            Write-Header
            Invoke-SS "--consistency-audit"
            Pause-After
        }

        "7" {
            Write-Header
            Invoke-Python "find_near_dupes"
            Pause-After
        }

        "8" {
            Write-Header
            Invoke-Python "score_correlation"
            Pause-After
        }

        "9" {
            Write-Header
            Invoke-Python "compute_metrics,find_near_dupes,score_correlation"
            Pause-After
        }

        "0" {
            Write-Header
            Write-Host "  Running full pipeline (~15 min) - same as the 1 AM Task Scheduler job." -ForegroundColor Yellow
            Write-Host ""
            Invoke-Python "all"
            Pause-After
        }

        "F" {
            Write-Header
            Write-Host "  First-run seed: compute metrics -> near-dupes -> score model -> report" -ForegroundColor Green
            Write-Host ""

            Write-Host "  -- Step 1 of 4: Compute prose metrics --------------------------" -ForegroundColor Cyan
            Invoke-SS "--compute-metrics", "--all"

            Write-Host ""
            Write-Host "  -- Step 2 of 4: Find near-duplicates ---------------------------" -ForegroundColor Cyan
            Invoke-Python "find_near_dupes"

            Write-Host ""
            Write-Host "  -- Step 3 of 4: Score correlation model ------------------------" -ForegroundColor Cyan
            Invoke-Python "score_correlation"

            Write-Host ""
            Write-Host "  -- Step 4 of 4: Morning report (7 days) ------------------------" -ForegroundColor Cyan
            Invoke-SS "--morning-report", "--since", "168"

            Pause-After
        }

        "Q" { break }

        default { }
    }

    if ($choice.Trim().ToUpper() -eq "Q") { break }
}

Write-Host ""
