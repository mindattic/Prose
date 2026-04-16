#Requires -Version 5.1
<#
.SYNOPSIS
    StreetSamurai Script Console -- browse and run all Python and JS utility scripts.

.DESCRIPTION
    Two-panel navigation:

    CATEGORY panel (start here)
      Up / Down    Move between categories
      Enter / ->   Open script list for selected category
      Q / Esc      Quit

    SCRIPT panel
      Up / Down    Move between scripts
      <-           Back to categories
      D            Show full docs
      Enter / ->   Run the selected script
      Q / Esc      Quit
#>

$ErrorActionPreference = "Stop"

# ---- Locate manifest --------------------------------------------------------
$ScriptDir    = Split-Path -Parent $MyInvocation.MyCommand.Path
$ManifestPath = Join-Path $ScriptDir "manifest.json"

if (-not (Test-Path $ManifestPath)) {
    Write-Host "ERROR: manifest.json not found at $ManifestPath" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

$manifest   = Get-Content $ManifestPath -Raw | ConvertFrom-Json
$categories = @($manifest.categories)

# ---- Colors -----------------------------------------------------------------
$CHeader   = "Cyan"
$CActive   = "Yellow"
$CInactive = "DarkGray"
$CAccent   = "DarkCyan"
$CDesc     = "Gray"
$CKey      = "DarkGreen"
$CError    = "Red"
$CDim      = "DarkGray"
$CNormal   = "White"

# ---- State ------------------------------------------------------------------
$panel     = "cat"   # "cat" | "script"
$catIndex  = 0
$itemIndex = 0

function Clamp($v, $min, $max) { [Math]::Max($min, [Math]::Min($max, $v)) }

$HR = "-" * 76

# ---- Shared chrome ----------------------------------------------------------
function Write-Header($title, $crumb) {
    Write-Host ""
    Write-Host "  STREETSAMURAI SCRIPT CONSOLE" -NoNewline -ForegroundColor $CHeader
    if ($crumb) { Write-Host "  >  $crumb" -NoNewline -ForegroundColor $CAccent }
    Write-Host ""
    Write-Host "  $HR" -ForegroundColor $CAccent
    Write-Host ""
}

function Write-Keys($lines) {
    Write-Host ""
    Write-Host "  $HR" -ForegroundColor $CAccent
    foreach ($line in $lines) {
        # Each line is "Key=label  Key=label" -- bold the key part
        Write-Host "  $line" -ForegroundColor $CDim
    }
    Write-Host ""
}

# ---- Category panel ---------------------------------------------------------
function Render-Categories {
    [Console]::Clear()
    Write-Header "Select a Category"

    $maxName = ($categories | ForEach-Object { $_.name.Length } | Measure-Object -Maximum).Maximum + 2

    Write-Host "  $("CATEGORY".PadRight($maxName + 4))TYPE" -ForegroundColor $CDim
    Write-Host "  $HR" -ForegroundColor $CAccent

    for ($i = 0; $i -lt $categories.Count; $i++) {
        $c      = $categories[$i]
        $marker = if ($i -eq $catIndex) { "  > " } else { "    " }
        $nc     = if ($i -eq $catIndex) { $CActive   } else { $CNormal   }
        $dc     = if ($i -eq $catIndex) { $CDesc     } else { $CDim      }
        $cnt    = "($($c.scripts.Count) scripts)"

        $name = $c.name.PadRight($maxName)
        Write-Host $marker       -NoNewline -ForegroundColor $CAccent
        Write-Host $name         -NoNewline -ForegroundColor $nc
        Write-Host "  $($c.type.ToUpper().PadRight(8))$cnt" -ForegroundColor $dc
    }

    Write-Keys @(
        "Up / Down   move between categories",
        "Enter       open script list",
        "Q           quit"
    )
}

# ---- Script panel -----------------------------------------------------------
function Render-Scripts {
    [Console]::Clear()
    $cat     = $categories[$catIndex]
    $scripts = @($cat.scripts)

    Write-Header "$($cat.name)" "($($cat.type.ToUpper()))"

    $nameCol = 20
    if ($scripts.Count -gt 0) {
        $longest = ($scripts | ForEach-Object { $_.file.Length } | Measure-Object -Maximum).Maximum
        $nameCol = $longest + 2
    }
    $descMax = [Math]::Max(20, 76 - $nameCol - 4)

    Write-Host "  $("SCRIPT".PadRight($nameCol))DESCRIPTION" -ForegroundColor $CDim
    Write-Host "  $HR" -ForegroundColor $CAccent

    # Scrolling window
    $windowSize = 14
    $start = [Math]::Max(0, $itemIndex - [Math]::Floor($windowSize / 2))
    $end   = [Math]::Min($scripts.Count - 1, $start + $windowSize - 1)
    $start = [Math]::Max(0, $end - $windowSize + 1)

    for ($i = $start; $i -le $end; $i++) {
        $s      = $scripts[$i]
        $marker = if ($i -eq $itemIndex) { "  > " } else { "    " }
        $nc     = if ($i -eq $itemIndex) { $CActive } else { $CNormal }
        $dc     = if ($i -eq $itemIndex) { $CDesc   } else { $CDim    }

        $name = $s.file.PadRight($nameCol)
        $desc = $s.description
        if ($desc.Length -gt $descMax) { $desc = $desc.Substring(0, $descMax - 3) + "..." }

        Write-Host $marker -NoNewline -ForegroundColor $CAccent
        Write-Host $name   -NoNewline -ForegroundColor $nc
        Write-Host $desc             -ForegroundColor $dc
    }

    if ($scripts.Count -gt $windowSize) {
        Write-Host ""
        Write-Host "  item $($itemIndex + 1) of $($scripts.Count)" -ForegroundColor $CDim
    }

    # Detail for selected
    if ($scripts.Count -gt 0) {
        $sel = $scripts[$itemIndex]
        Write-Host ""
        Write-Host "  $HR" -ForegroundColor $CAccent
        Write-Host "  $($sel.file)" -ForegroundColor $CActive
        Write-Host "  $($sel.description)" -ForegroundColor $CDesc

        if ($sel.PSObject.Properties['args'] -and $sel.args.Count -gt 0) {
            Write-Host ""
            Write-Host "  ARGS:" -ForegroundColor $CKey
            $flagCol = ($sel.args | ForEach-Object { $_.flag.Length } | Measure-Object -Maximum).Maximum + 2
            foreach ($arg in $sel.args) {
                $dflt = if ($null -ne $arg.default) { "  (default: $($arg.default))" } else { "" }
                Write-Host "    $($arg.flag.PadRight($flagCol))$($arg.desc)$dflt" -ForegroundColor $CDim
            }
        }
    }

    Write-Keys @(
        "Up / Down   move between scripts",
        "Enter       run selected script   D  full docs",
        "Esc         back to categories"
    )
}

# ---- Docs view --------------------------------------------------------------
function Show-Docs($s, $cat) {
    [Console]::Clear()
    Write-Header "DOCS" "$($cat.name)  >  $($s.file)"
    Write-Host "  $($s.description)" -ForegroundColor $CNormal
    Write-Host ""

    if ($s.PSObject.Properties['docs'] -and -not [string]::IsNullOrWhiteSpace($s.docs)) {
        Write-Host $s.docs -ForegroundColor $CDesc
        Write-Host ""
    }

    if ($s.PSObject.Properties['args'] -and $s.args.Count -gt 0) {
        Write-Host "  OPTIONS" -ForegroundColor $CKey
        $flagCol = ($s.args | ForEach-Object { $_.flag.Length } | Measure-Object -Maximum).Maximum + 2
        foreach ($arg in $s.args) {
            $dflt = if ($null -ne $arg.default) { "  (default: $($arg.default))" } else { "" }
            Write-Host "    $($arg.flag.PadRight($flagCol))$($arg.desc)$dflt" -ForegroundColor $CActive
        }
        Write-Host ""
    }

    Write-Keys @("Any key   return to script list")
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
}

# ---- Run screen -------------------------------------------------------------
function Run-Script($s, $cat) {
    [Console]::Clear()
    Write-Header "RUN" "$($cat.name)  >  $($s.file)"
    Write-Host "  $($s.description)" -ForegroundColor $CNormal
    Write-Host ""

    if ($s.PSObject.Properties['args'] -and $s.args.Count -gt 0) {
        Write-Host "  AVAILABLE ARGS:" -ForegroundColor $CKey
        $flagCol = ($s.args | ForEach-Object { $_.flag.Length } | Measure-Object -Maximum).Maximum + 2
        foreach ($arg in $s.args) {
            $dflt = if ($null -ne $arg.default) { "  (default: $($arg.default))" } else { "" }
            Write-Host "    $($arg.flag.PadRight($flagCol))$($arg.desc)$dflt" -ForegroundColor $CDim
        }
        Write-Host ""
    }

    Write-Keys @("Enter args below (blank = run with defaults)  Q / Esc  return without running")
    $extraArgs = Read-Host "  > "

    $runner = $cat.type
    $subDir = if ($runner -eq "python") { "py" } else { "js" }
    $path   = Join-Path (Join-Path $ScriptDir $subDir) $s.file

    if (-not (Test-Path $path)) {
        Write-Host ""
        Write-Host "  ERROR: Script not found at $path" -ForegroundColor $CError
        Start-Sleep -Seconds 2
        return
    }

    Write-Host ""
    Write-Host "  $HR" -ForegroundColor $CAccent

    try {
        $cmd = if ($runner -eq "python") { "python" } else { "node" }
        if ([string]::IsNullOrWhiteSpace($extraArgs)) {
            & $cmd $path
        } else {
            & $cmd $path ($extraArgs -split "\s+")
        }
    } catch {
        Write-Host "  ERROR: $_" -ForegroundColor $CError
    }

    Write-Host ""
    Write-Host "  $HR" -ForegroundColor $CAccent
    Write-Host "  Done." -ForegroundColor $CKey
    Write-Keys @("Any key   return to script list")
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
}

# ---- Main loop --------------------------------------------------------------
while ($true) {
    if ($panel -eq "cat") {
        Render-Categories
    } else {
        Render-Scripts
    }

    $key = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
    $vk  = $key.VirtualKeyCode
    $ch  = [char]::ToUpper($key.Character)

    $maxCat  = $categories.Count - 1
    $scripts = @($categories[$catIndex].scripts)
    $maxItem = [Math]::Max(0, $scripts.Count - 1)

    if ($panel -eq "cat") {
        switch ($vk) {
            38 { $catIndex = Clamp ($catIndex - 1) 0 $maxCat }   # Up
            40 { $catIndex = Clamp ($catIndex + 1) 0 $maxCat }   # Down
            13 { $panel = "script"; $itemIndex = 0 }              # Enter
            # Esc ignored at root -- no back to go to
            default { if ($ch -eq 'Q') { [Console]::Clear(); exit 0 } }
        }
    } else {
        switch ($vk) {
            38 { $itemIndex = Clamp ($itemIndex - 1) 0 $maxItem }  # Up
            40 { $itemIndex = Clamp ($itemIndex + 1) 0 $maxItem }  # Down
            13 {                                                     # Enter -> run
                if ($scripts.Count -gt 0) {
                    Run-Script $scripts[$itemIndex] $categories[$catIndex]
                }
            }
            27 { $panel = "cat" }                                   # Esc -> back to categories
            default {
                if ($ch -eq 'D' -and $scripts.Count -gt 0) {
                    Show-Docs $scripts[$itemIndex] $categories[$catIndex]
                }
            }
        }
    }
}
