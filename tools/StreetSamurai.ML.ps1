#Requires -Version 5.1
<#
.SYNOPSIS
    StreetSamurai ML & Canon Audit console — fire any pipeline task from a menu.

RECOMMENDED FIRST-RUN ORDER:
    4 → 7 → 8 → 1

    4  seeds BeatProseMetrics (required by 8 and the morning report §3)
    7  seeds near-duplicate Findings (required by the morning report §4)
    8  trains the score correlation model (requires reviewed beats; skip if none yet)
    1  reads the morning report once everything is seeded
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = "Continue"

$ROOT    = Split-Path $PSScriptRoot -Parent
$ML_ROOT = Join-Path $ROOT "v3\ml"
$VENV    = Join-Path $ML_ROOT ".venv\Scripts\Activate.ps1"
$LOG     = Join-Path $ROOT "v3\ml_nightly.log"

# ── Display helpers ────────────────────────────────────────────────────────────

function Write-Header {
    Clear-Host
    Write-Host ""
    Write-Host "  ╔══════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
    Write-Host "  ║          StreetSamurai  ML & Canon Audit Suite               ║" -ForegroundColor Cyan
    Write-Host "  ╠══════════════════════════════════════════════════════════════╣" -ForegroundColor Cyan
    Write-Host "  ║  First run?  Fire in this order:  4  7  8  1                 ║" -ForegroundColor Yellow
    Write-Host "  ║  Every morning:                   1  (or 0 to refresh all)   ║" -ForegroundColor Yellow
    Write-Host "  ╚══════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
    Write-Host ""
}

function Write-Menu {
    Write-Host "  ── Morning Reports ─────────────────────────────────────────────" -ForegroundColor DarkGray
    Write-Host ""
    Write-Host "   [1]  Morning Report — last 24 h" -ForegroundColor White
    Write-Host "        Prints 6 sections to the terminal: cross-story contradictions," -ForegroundColor DarkGray
    Write-Host "        new findings, prose outliers, near-dupes, score model summary," -ForegroundColor DarkGray
    Write-Host "        and the story score leaderboard. Run this every morning." -ForegroundColor DarkGray
    Write-Host ""
    Write-Host "   [2]  Morning Report — last 7 days" -ForegroundColor White
    Write-Host "        Same report but covers a full week. Useful after a gap or" -ForegroundColor DarkGray
    Write-Host "        when catching up after the first-run seed sequence." -ForegroundColor DarkGray
    Write-Host ""
    Write-Host "   [3]  View nightly log (last 60 lines)" -ForegroundColor White
    Write-Host "        Shows the raw output from last night's Task Scheduler run." -ForegroundColor DarkGray
    Write-Host "        Look for 'Exit code: 0' at the bottom. Any stack traces" -ForegroundColor DarkGray
    Write-Host "        above it are phases that failed." -ForegroundColor DarkGray
    Write-Host ""
    Write-Host "  ── .NET CLI tasks  (fast, no Python) ───────────────────────────" -ForegroundColor DarkGray
    Write-Host ""
    Write-Host "   [4]  Compute prose metrics — ALL beats                  ~30 sec" -ForegroundColor White
    Write-Host "        Calculates TTR, MTLD, Flesch-Kincaid, dialogue %" -ForegroundColor DarkGray
    Write-Host "        for every enabled beat across all stories. Saves to" -ForegroundColor DarkGray
    Write-Host "        BeatProseMetrics table. Run this first; required for" -ForegroundColor DarkGray
    Write-Host "        the score correlation model and morning report §3." -ForegroundColor DarkGray
    Write-Host ""
    Write-Host "   [5]  Compute prose metrics — single story               ~5 sec" -ForegroundColor White
    Write-Host "        Same as [4] but for one story slug only. Use after" -ForegroundColor DarkGray
    Write-Host "        writing or editing beats in a specific story." -ForegroundColor DarkGray
    Write-Host ""
    Write-Host "   [6]  Cross-story consistency audit" -ForegroundColor White
    Write-Host "        Finds facts that contradict between stories — e.g. a" -ForegroundColor DarkGray
    Write-Host "        character's age is 34 in one novel and 31 in another." -ForegroundColor DarkGray
    Write-Host "        Exit 0 = clean. Exit 1 = conflicts found (also listed" -ForegroundColor DarkGray
    Write-Host "        in the morning report §1). These need a human call on" -ForegroundColor DarkGray
    Write-Host "        which story is canon." -ForegroundColor DarkGray
    Write-Host ""
    Write-Host "  ── Python pipeline  (GPU-free, local CPU) ──────────────────────" -ForegroundColor DarkGray
    Write-Host ""
    Write-Host "   [7]  Find near-duplicates                               ~5 min" -ForegroundColor White
    Write-Host "        Embeds all 1500+ beats locally with MiniLM, then does" -ForegroundColor DarkGray
    Write-Host "        pairwise cosine similarity. Flags cross-story pairs" -ForegroundColor DarkGray
    Write-Host "        above 0.92 similarity into the Findings table. First" -ForegroundColor DarkGray
    Write-Host "        run is slow (model download + embedding). Run after [4]." -ForegroundColor DarkGray
    Write-Host ""
    Write-Host "   [8]  Score correlation model                            ~1 min" -ForegroundColor White
    Write-Host "        Trains a gradient-boosting model on prose features vs" -ForegroundColor DarkGray
    Write-Host "        review scores. Needs >= 20 reviewed beats. Writes a" -ForegroundColor DarkGray
    Write-Host "        feature-importance report to %APPDATA%\MindAttic\ML\." -ForegroundColor DarkGray
    Write-Host "        Skip this if no review scores exist yet." -ForegroundColor DarkGray
    Write-Host ""
    Write-Host "   [9]  Canon Audit phases only  (4 + 7 + 8 together)     ~6 min" -ForegroundColor White
    Write-Host "        Runs compute_metrics, find_near_dupes, score_correlation" -ForegroundColor DarkGray
    Write-Host "        in sequence. Use this to refresh all three without" -ForegroundColor DarkGray
    Write-Host "        touching the topic/register ML models." -ForegroundColor DarkGray
    Write-Host ""
    Write-Host "   [0]  Full nightly pipeline — all 9 phases              ~15 min" -ForegroundColor Yellow
    Write-Host "        Runs everything: gripe extraction, topic model, register" -ForegroundColor DarkGray
    Write-Host "        classifier, gripe/register audits, plus all three Canon" -ForegroundColor DarkGray
    Write-Host "        Audit phases. Same as what runs at 1 AM automatically." -ForegroundColor DarkGray
    Write-Host "        Use this when you want a full refresh mid-day." -ForegroundColor DarkGray
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
    & ss @SsArgs
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
    Write-Host "  ─────────────────────────────────────────────────────────────" -ForegroundColor DarkGray
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
                Write-Host "  No slug entered — cancelled." -ForegroundColor Yellow
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
            Write-Host "  Running full pipeline (~15 min) — Task Scheduler runs this at 1 AM." -ForegroundColor Yellow
            Write-Host ""
            Invoke-Python "all"
            Pause-After
        }

        "Q" { break }

        default { } # redraw
    }

    if ($choice.Trim().ToUpper() -eq "Q") { break }
}

Write-Host ""
