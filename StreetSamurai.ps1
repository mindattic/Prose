$root = $PSScriptRoot

# -- Helpers -----------------------------------------------------------------------

function Write-Header {
    param([string[]]$Breadcrumbs = @())
    Clear-Host
    Write-Host ""
    $trail = @("Street Samurai") + $Breadcrumbs
    $line = ""
    for ($i = 0; $i -lt $trail.Count; $i++) {
        if ($i -gt 0) { $line += " > " }
        $line += $trail[$i]
    }
    Write-Host "  $line" -ForegroundColor Red
    Write-Host "  $('=' * 60)" -ForegroundColor DarkRed
    Write-Host ""
}

function Wait-ForKey {
    Write-Host ""
    Write-Host "  Press any key to continue..." -ForegroundColor DarkGray
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
}

function Read-MenuKey {
    Write-Host "  > " -NoNewline
    $key = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
    if ($key.VirtualKeyCode -eq 37 -or $key.VirtualKeyCode -eq 8 -or $key.VirtualKeyCode -eq 27) {
        Write-Host "<"
        return "Z"
    }
    $char = $key.Character.ToString().Trim().ToUpper()
    if ($char) { Write-Host $char }
    return $char
}

function Read-Input {
    param([string]$Prompt)
    Write-Host "  $Prompt" -ForegroundColor Yellow
    Write-Host "  > " -NoNewline
    return (Read-Host).Trim()
}

function Run-Python {
    param([string]$Command, [switch]$Pause)
    Write-Host ""
    Write-Host "  Running..." -ForegroundColor DarkGray
    Write-Host ""
    Push-Location $root
    try {
        Invoke-Expression "python $Command"
    } finally {
        Pop-Location
    }
    if ($Pause) { Wait-ForKey }
}

# -- Status Dashboard --------------------------------------------------------------

function Write-CanonStatus {
    $wbCount = (Get-ChildItem "$root\worldbuilding\*.md" -ErrorAction SilentlyContinue | Where-Object { $_.Name -notlike "ARCHIVED_*" }).Count
    $charCount = (Get-ChildItem "$root\characters\*.yaml" -ErrorAction SilentlyContinue).Count
    $essCount = (Get-ChildItem "$root\essences" -Recurse -Filter "*.yaml" -ErrorAction SilentlyContinue).Count
    $storyCount = (Get-ChildItem "$root\stories" -Directory -ErrorAction SilentlyContinue).Count

    $chromaExists = Test-Path "$root\engine_data\chromadb"
    $graphExists = Test-Path "$root\engine_data\knowledge_graph.json"

    $pendingCount = (Get-ChildItem "$root\canon_queue\pending\*.yaml" -ErrorAction SilentlyContinue).Count

    Write-Host "  Canon Vault" -ForegroundColor DarkCyan
    Write-Host "  $('-' * 60)" -ForegroundColor DarkGray
    Write-Host "    Worldbuilding docs:   " -NoNewline -ForegroundColor White
    Write-Host "$wbCount" -ForegroundColor Cyan
    Write-Host "    Character files:      " -NoNewline -ForegroundColor White
    Write-Host "$charCount" -ForegroundColor Cyan
    Write-Host "    Essence files:        " -NoNewline -ForegroundColor White
    Write-Host "$essCount" -ForegroundColor Cyan
    Write-Host ""

    Write-Host "  Engine Status" -ForegroundColor DarkCyan
    Write-Host "  $('-' * 60)" -ForegroundColor DarkGray
    Write-Host "    Vector index:         " -NoNewline -ForegroundColor White
    if ($chromaExists) {
        Write-Host "BUILT" -ForegroundColor Green
    } else {
        Write-Host "NOT BUILT (run Build Canon)" -ForegroundColor Yellow
    }
    Write-Host "    Knowledge graph:      " -NoNewline -ForegroundColor White
    if ($graphExists) {
        Write-Host "BUILT" -ForegroundColor Green
    } else {
        Write-Host "NOT BUILT (run Build Canon)" -ForegroundColor Yellow
    }
    Write-Host ""

    Write-Host "  Output" -ForegroundColor DarkCyan
    Write-Host "  $('-' * 60)" -ForegroundColor DarkGray
    Write-Host "    Generated scenes:     " -NoNewline -ForegroundColor White
    Write-Host "$storyCount" -ForegroundColor $(if ($storyCount -gt 0) { "Cyan" } else { "DarkGray" })
    Write-Host "    Canon queue pending:  " -NoNewline -ForegroundColor White
    if ($pendingCount -gt 0) {
        Write-Host "$pendingCount awaiting review" -ForegroundColor Yellow
    } else {
        Write-Host "empty" -ForegroundColor DarkGray
    }

    # Git status
    $gitChanges = git -C $root status --porcelain 2>$null
    $gitCount = if ($gitChanges) { $gitChanges.Count } else { 0 }
    Write-Host "    Git changes:          " -NoNewline -ForegroundColor White
    if ($gitCount -eq 0) {
        Write-Host "clean" -ForegroundColor Green
    } else {
        Write-Host "$gitCount uncommitted" -ForegroundColor Yellow
    }
    Write-Host ""
}

# -- Build Canon -------------------------------------------------------------------

function Invoke-BuildCanon {
    Write-Header -Breadcrumbs "Build Canon Index"
    Write-Host "  Rebuilding vector store and knowledge graph from worldbuilding..." -ForegroundColor Yellow
    Run-Python "-m engine.pipeline build" -Pause
}

# -- Generate Scene ----------------------------------------------------------------

function Invoke-GenerateScene {
    Write-Header -Breadcrumbs "Generate Scene"

    $goal = Read-Input "Scene goal (what happens?):"
    if ([string]::IsNullOrWhiteSpace($goal)) { return }

    Write-Host ""
    $location = Read-Input "Location (or Enter for none):"

    Write-Host ""
    $entitiesRaw = Read-Input "Entities involved (space-separated, or Enter for none):"
    $entities = if ($entitiesRaw) { $entitiesRaw -split '\s+' } else { @() }

    Write-Host ""
    $themesRaw = Read-Input "Themes (space-separated, or Enter for none):"
    $themes = if ($themesRaw) { $themesRaw -split '\s+' } else { @() }

    Write-Host ""
    $beatsRaw = Read-Input "Number of beats (Enter for 5):"
    $beats = if ($beatsRaw) { [int]$beatsRaw } else { 5 }

    # Build the command
    $cmd = "-m engine.pipeline generate --goal `"$goal`""
    if ($location) { $cmd += " --location `"$location`"" }
    if ($entities.Count -gt 0) { $cmd += " --entities $($entities -join ' ')" }
    if ($themes.Count -gt 0) { $cmd += " --themes $($themes -join ' ')" }
    $cmd += " --beats $beats"

    Write-Header -Breadcrumbs "Generate Scene", "Running"
    Write-Host "  Goal: $goal" -ForegroundColor Cyan
    if ($location) { Write-Host "  Location: $location" -ForegroundColor Cyan }
    if ($entities.Count -gt 0) { Write-Host "  Entities: $($entities -join ', ')" -ForegroundColor Cyan }
    Write-Host ""

    Run-Python $cmd -Pause
}

# -- Validate Text -----------------------------------------------------------------

function Invoke-ValidateText {
    Write-Header -Breadcrumbs "Validate Text"

    $filePath = Read-Input "Path to text file to validate:"
    if ([string]::IsNullOrWhiteSpace($filePath)) { return }
    if (-not (Test-Path $filePath)) {
        Write-Host "  File not found: $filePath" -ForegroundColor Red
        Wait-ForKey
        return
    }

    Write-Host ""
    $entitiesRaw = Read-Input "Expected entities (space-separated, or Enter):"
    $entities = if ($entitiesRaw) { $entitiesRaw -split '\s+' } else { @() }

    $cmd = "-m engine.pipeline validate `"$filePath`""
    if ($entities.Count -gt 0) { $cmd += " --entities $($entities -join ' ')" }

    Run-Python $cmd -Pause
}

# -- Canon Queue -------------------------------------------------------------------

function Invoke-CanonQueue {
    Write-Header -Breadcrumbs "Canon Queue"
    Run-Python "-m engine.pipeline queue" -Pause
}

# -- Quick Commands ----------------------------------------------------------------

function Invoke-ListEssences {
    Write-Header -Breadcrumbs "Essences"
    Run-Python "-m src.main list-essences" -Pause
}

function Invoke-ShowCharacter {
    Write-Header -Breadcrumbs "Character"
    Run-Python "-m src.main show-character" -Pause
}

function Invoke-ListFacets {
    Write-Header -Breadcrumbs "Facets"
    Run-Python "-m src.main list-facets" -Pause
}

# -- Session (existing src/ system) ------------------------------------------------

function Invoke-NewSession {
    Write-Header -Breadcrumbs "New Session (Beat Writer)"

    $goal = Read-Input "Scene goal:"
    if ([string]::IsNullOrWhiteSpace($goal)) { return }

    Write-Host ""
    $location = Read-Input "Location essence (or Enter for none):"

    Write-Host ""
    $npcsRaw = Read-Input "NPC essences (space-separated, or Enter for none):"

    $cmd = "-m src.main new-session --scene-goal `"$goal`""
    if ($location) { $cmd += " --location `"$location`"" }
    if ($npcsRaw) {
        $npcs = $npcsRaw -split '\s+'
        foreach ($npc in $npcs) { $cmd += " --npcs `"$npc`"" }
    }

    Run-Python $cmd -Pause
}

# -- Git Operations ----------------------------------------------------------------

function Invoke-CommitSync {
    Write-Header -Breadcrumbs "Commit & Sync"

    $changes = git -C $root status --porcelain 2>$null
    if (-not $changes) {
        Write-Host "  Nothing to commit. Working tree clean." -ForegroundColor DarkGray
        Wait-ForKey
        return
    }

    Write-Host "  Changes:" -ForegroundColor DarkCyan
    foreach ($line in $changes) {
        $code = $line.Substring(0, 2).Trim()
        $file = $line.Substring(3)
        $color = switch -Wildcard ($code) {
            "??" { "Green" }
            "A"  { "Green" }
            "D"  { "Red" }
            default { "Yellow" }
        }
        Write-Host "    $code $file" -ForegroundColor $color
    }

    Write-Host ""
    $msg = Read-Input "Commit message (Enter for auto-generate):"

    if ([string]::IsNullOrWhiteSpace($msg)) {
        $added = ($changes | Where-Object { $_ -match '^\?\?' }).Count
        $modified = ($changes | Where-Object { $_ -match '^ ?M' }).Count
        $parts = @()
        if ($added -gt 0) { $parts += "Add $added file(s)" }
        if ($modified -gt 0) { $parts += "Update $modified file(s)" }
        if ($parts.Count -eq 0) { $parts += "Update files" }
        $msg = $parts -join "; "
    }

    Write-Host ""
    Write-Host "  Committing..." -NoNewline -ForegroundColor White
    git -C $root add -A 2>$null
    git -C $root commit -m "$msg`n`nCo-Authored-By: Claude Opus 4.6 <noreply@anthropic.com>" 2>$null | Out-Null

    if ($LASTEXITCODE -ne 0) {
        Write-Host " failed!" -ForegroundColor Red
        Wait-ForKey
        return
    }
    Write-Host " done" -ForegroundColor Green

    Write-Host "  Pushing..." -NoNewline -ForegroundColor White
    git -C $root push 2>$null | Out-Null

    if ($LASTEXITCODE -ne 0) {
        Write-Host " failed!" -ForegroundColor Red
    } else {
        Write-Host " synced" -ForegroundColor Green
    }
    Wait-ForKey
}

# -- Main Menu ---------------------------------------------------------------------

while ($true) {
    Write-Header

    Write-Host "  Canon Engine" -ForegroundColor DarkCyan
    Write-Host "  $('-' * 60)" -ForegroundColor DarkGray
    Write-Host "    [1]  Build Canon Index       (embed + graph)" -ForegroundColor White
    Write-Host "    [2]  Generate Scene           (RAG + validate)" -ForegroundColor White
    Write-Host "    [3]  Validate Text            (check against canon)" -ForegroundColor White
    Write-Host "    [4]  Canon Queue              (review pending)" -ForegroundColor White
    Write-Host ""

    Write-Host "  Beat Writer (src/)" -ForegroundColor DarkCyan
    Write-Host "  $('-' * 60)" -ForegroundColor DarkGray
    Write-Host "    [5]  New Session              (facet-driven beats)" -ForegroundColor White
    Write-Host ""

    Write-Host "  World Info" -ForegroundColor DarkCyan
    Write-Host "  $('-' * 60)" -ForegroundColor DarkGray
    Write-Host "    [6]  Show Character" -ForegroundColor White
    Write-Host "    [7]  List Facets" -ForegroundColor White
    Write-Host "    [8]  List Essences" -ForegroundColor White
    Write-Host ""

    Write-Host "  Git" -ForegroundColor DarkCyan
    Write-Host "  $('-' * 60)" -ForegroundColor DarkGray
    Write-Host "    [9]  Commit & Sync" -ForegroundColor White
    Write-Host ""

    Write-Host "   [Q]  Quit" -ForegroundColor DarkGray
    Write-Host ""

    Write-CanonStatus

    $choice = Read-MenuKey

    switch ($choice) {
        "1" { Invoke-BuildCanon }
        "2" { Invoke-GenerateScene }
        "3" { Invoke-ValidateText }
        "4" { Invoke-CanonQueue }
        "5" { Invoke-NewSession }
        "6" { Invoke-ShowCharacter }
        "7" { Invoke-ListFacets }
        "8" { Invoke-ListEssences }
        "9" { Invoke-CommitSync }
        "Q" { Clear-Host; exit 0 }
    }
}
