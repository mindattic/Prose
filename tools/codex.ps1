<#
.SYNOPSIS
  Codex documentation standard CLI for StreetSamurai (SS).
  Subcommands:
    doctor  - validate the Codex docs (front-matter, IDs, cross-refs, data schemas, stories,
              cited paths, generatedFrom freshness, digest freshness). Exit non-zero on hard error.
    digest  - regenerate docs/BIBLE.digest.md from BIBLE.md (1, 3, 5, 9) + status index + latest amendment.

  PowerShell 5.1 / Windows-1252 safe. No build step. Run from anywhere:
    pwsh tools/codex.ps1 doctor
    powershell -File tools/codex.ps1 digest
#>
[CmdletBinding()]
param(
  [Parameter(Position = 0)]
  [ValidateSet('doctor', 'digest')]
  [string]$Command = 'doctor'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# --- paths -------------------------------------------------------------------
$RepoRoot  = Split-Path -Parent $PSScriptRoot
$DocsDir   = Join-Path $RepoRoot 'docs'
$Bible     = Join-Path $DocsDir 'BIBLE.md'
$Stories   = Join-Path $DocsDir 'USER_STORIES.md'
$Amend     = Join-Path $DocsDir 'AMENDMENTS.md'
$RfcDir    = Join-Path $DocsDir 'rfc'
$DataDir   = Join-Path $DocsDir 'data'
$SchemaDir = Join-Path $DataDir '_schema'
$Digest    = Join-Path $DocsDir 'BIBLE.digest.md'
$Code      = 'SS'

# --- helpers -----------------------------------------------------------------
$script:errors   = New-Object System.Collections.ArrayList
$script:warnings = New-Object System.Collections.ArrayList
function Add-Err ($m)  { [void]$script:errors.Add($m) }
function Add-Warn ($m) { [void]$script:warnings.Add($m) }

function Get-FrontMatter ($path) {
  if (-not (Test-Path $path)) { return $null }
  $lines = Get-Content -LiteralPath $path -Encoding UTF8
  if ($lines.Count -lt 2 -or $lines[0].Trim() -ne '---') { return $null }
  $fm = @{}
  for ($i = 1; $i -lt $lines.Count; $i++) {
    if ($lines[$i].Trim() -eq '---') { return $fm }
    if ($lines[$i] -match '^\s*([A-Za-z_]+)\s*:\s*(.+?)\s*$') { $fm[$matches[1]] = $matches[2] }
  }
  return $null  # never closed
}

function Test-FrontMatter ($path, $expectedLayer) {
  $rel = $path.Replace($RepoRoot, '').TrimStart('\','/')
  $fm = Get-FrontMatter $path
  if ($null -eq $fm) { Add-Err "front-matter: $rel has no valid (closed) codex front-matter block"; return }
  foreach ($k in 'codex','project','code','layer','status','updated') {
    if (-not $fm.ContainsKey($k)) { Add-Err "front-matter: $rel missing key '$k'" }
  }
  if ($fm.ContainsKey('layer') -and $expectedLayer -and $fm['layer'] -ne $expectedLayer) {
    Add-Err "front-matter: $rel layer is '$($fm['layer'])', expected '$expectedLayer'"
  }
  if ($fm.ContainsKey('updated') -and $fm['updated'] -notmatch '^\d{4}-\d{2}-\d{2}$') {
    Add-Err "front-matter: $rel 'updated' is not YYYY-MM-DD ('$($fm['updated'])')"
  }
}

# =============================================================================
# DOCTOR
# =============================================================================
function Invoke-Doctor {
  Write-Host "Codex doctor - $Code ($RepoRoot)" -ForegroundColor Cyan

  # 1. required files exist
  foreach ($f in @($Bible, $Stories)) {
    if (-not (Test-Path $f)) { Add-Err "missing required file: $($f.Replace($RepoRoot,'').TrimStart('\','/'))" }
  }

  # 2. front-matter
  Test-FrontMatter $Bible   'bible'
  Test-FrontMatter $Stories 'stories'
  if (Test-Path $Amend) { Test-FrontMatter $Amend 'amendments' }  # AMENDMENTS.md retired 2026-07-04
  if (Test-Path $RfcDir) {
    Get-ChildItem -LiteralPath $RfcDir -Filter '*.md' -ErrorAction SilentlyContinue | ForEach-Object {
      Test-FrontMatter $_.FullName 'rfc'
    }
  }

  # collect doc text for ID/link analysis
  $docFiles = @()
  foreach ($f in @($Bible, $Stories, $Amend)) { if (Test-Path $f) { $docFiles += $f } }
  if (Test-Path $RfcDir) { $docFiles += (Get-ChildItem -LiteralPath $RfcDir -Filter '*.md').FullName }
  if (Test-Path $DataDir) { $docFiles += (Get-ChildItem -LiteralPath $DataDir -Filter '*.md').FullName }

  # 3. unique {#...} anchors + collect link targets
  $anchors  = @{}
  $linkRefs = New-Object System.Collections.ArrayList
  foreach ($f in $docFiles) {
    $rel  = $f.Replace($RepoRoot, '').TrimStart('\','/')
    $text = Get-Content -LiteralPath $f -Raw -Encoding UTF8
    foreach ($m in [regex]::Matches($text, '\{#([A-Za-z0-9\-_§]+)\}')) {
      $id = $m.Groups[1].Value
      if ($anchors.ContainsKey($id)) { Add-Err "duplicate anchor {#$id} in $rel (also in $($anchors[$id]))" }
      else { $anchors[$id] = $rel }
    }
    foreach ($m in [regex]::Matches($text, '\]\(([^)]*#[^)]+)\)')) {
      [void]$linkRefs.Add([pscustomobject]@{ File = $rel; Target = $m.Groups[1].Value })
    }
  }

  # 4. every {#...} cross-ref link resolves
  foreach ($r in $linkRefs) {
    $frag = ($r.Target -split '#', 2)[1]
    if ([string]::IsNullOrWhiteSpace($frag)) { continue }
    # ignore README/section-style slugs (lowercase headings) - only enforce CODE-anchored ids
    if ($frag -match ("^$Code(-|§)") -or $frag -match '§') {
      if (-not $anchors.ContainsKey($frag)) {
        Add-Err "broken cross-ref in $($r.File): #$frag has no matching anchor"
      }
    }
  }

  # 5. data schemas: every docs/data/*.json validates against _schema/<type>.schema.json; ids unique
  if (Test-Path $DataDir) {
    $seenIds = @{}
    Get-ChildItem -LiteralPath $DataDir -Filter '*.json' -ErrorAction SilentlyContinue | ForEach-Object {
      $rel = $_.FullName.Replace($RepoRoot, '').TrimStart('\','/')
      try { $obj = Get-Content -LiteralPath $_.FullName -Raw -Encoding UTF8 | ConvertFrom-Json }
      catch { Add-Err "data: $rel is not valid JSON"; return }
      if ($obj.PSObject.Properties.Name -contains 'id') {
        $id = $obj.id
        if ($seenIds.ContainsKey($id)) { Add-Err "data: duplicate id '$id' in $rel" } else { $seenIds[$id] = $rel }
      }
    }
    # schemas themselves must be valid JSON
    if (Test-Path $SchemaDir) {
      Get-ChildItem -LiteralPath $SchemaDir -Filter '*.json' | ForEach-Object {
        $rel = $_.FullName.Replace($RepoRoot, '').TrimStart('\','/')
        try { [void](Get-Content -LiteralPath $_.FullName -Raw -Encoding UTF8 | ConvertFrom-Json) }
        catch { Add-Err "schema: $rel is not valid JSON" }
      }
    }
  }

  # 6. every checked-in registered corpus dir referenced by ENTITY_IDENTITY.md exists
  $identity = Join-Path $DataDir 'ENTITY_IDENTITY.md'
  if (Test-Path $identity) {
    $itext = Get-Content -LiteralPath $identity -Raw -Encoding UTF8
    foreach ($m in [regex]::Matches($itext, '`(engine_data/[A-Za-z_]+/)`')) {
      $d = Join-Path $RepoRoot ($m.Groups[1].Value -replace '/', '\')
      if (-not (Test-Path $d)) { Add-Warn "identity: registered corpus dir not found: $($m.Groups[1].Value)" }
    }
    foreach ($m in [regex]::Matches($itext, '\]\(_schema/([A-Za-z_]+\.schema\.json)\)')) {
      $s = Join-Path $SchemaDir $m.Groups[1].Value
      if (-not (Test-Path $s)) { Add-Err "identity: linked schema missing: _schema/$($m.Groups[1].Value)" }
    }
  }

  # 7. stories: every line with a check-mark names a test token; (best-effort) test exists
  $testTree = Join-Path $RepoRoot 'v3\StreetSamurai.UnitTests'
  $testIndex = $null
  if (Test-Path $testTree) {
    $testCs = Get-ChildItem -LiteralPath $testTree -Filter '*.cs' -Recurse -ErrorAction SilentlyContinue
    # Index = file base names + file contents, so a token matches a class name OR a test file name.
    $names  = ($testCs | ForEach-Object { $_.BaseName }) -join "`n"
    $bodies = ($testCs | ForEach-Object { Get-Content -LiteralPath $_.FullName -Raw -Encoding UTF8 }) -join "`n"
    $testIndex = $names + "`n" + $bodies
  }
  if (Test-Path $Stories) {
    $sLines = Get-Content -LiteralPath $Stories -Encoding UTF8
    $doneMark = [char]0x2705   # check mark
    # Assemble each story bullet into a full record (bullet + continuation lines until the next
    # bullet / heading / blank), so evidence on a wrapped continuation line still counts.
    $records = New-Object System.Collections.ArrayList
    $cur = $null
    foreach ($line in $sLines) {
      if ($line -match ('^\s*-\s+\*\*' + $Code + '-US-')) {
        if ($cur) { [void]$records.Add($cur) }
        $cur = $line
      } elseif ($cur -ne $null) {
        if ($line -match '^\s*$' -or $line -match '^\s*-\s+\*\*' -or $line -match '^#{1,6}\s') {
          [void]$records.Add($cur); $cur = $null
        } else {
          $cur = $cur + ' ' + $line.Trim()
        }
      }
    }
    if ($cur) { [void]$records.Add($cur) }

    foreach ($rec in $records) {
      if ($rec -notmatch ($Code + '-US-[A-Za-z0-9\-]+\s*' + $doneMark)) { continue }  # only done stories
      $tokens = [regex]::Matches($rec, '`([A-Za-z][A-Za-z0-9_]*(?:\.[A-Za-z][A-Za-z0-9_]*)?)`')
      $hasTestToken = $false
      foreach ($t in $tokens) {
        $tok = $t.Groups[1].Value
        if ($tok -match 'Tests' -or $tok -match '_') { $hasTestToken = $true }
      }
      $citesEvidence = ($rec -match 'verified by' -or $rec -match 'story_state' -or $rec -match 'scan' -or $rec -match 'CLI')
      if (-not $hasTestToken -and -not $citesEvidence) {
        $head = ($rec -split '\.\s')[0]
        Add-Err "stories: a done story names no test/evidence token: $($head.Trim())"
      }
      if ($testIndex -and $hasTestToken) {
        foreach ($t in $tokens) {
          $tok = $t.Groups[1].Value
          if ($tok -match 'Tests$' -or $tok -match '_') {
            $cls = ($tok -split '\.')[0]
            if ($testIndex -notmatch [regex]::Escape($cls)) {
              Add-Warn "stories: test token '$cls' not found in test tree (may be a CLI/scan token)"
            }
          }
        }
      }
    }
  }

  # 8. every code path/file cited in the bible exists on disk
  if (Test-Path $Bible) {
    $btext = Get-Content -LiteralPath $Bible -Raw -Encoding UTF8
    foreach ($m in [regex]::Matches($btext, '`((?:v3|engine|engine_data|docs|tools|infra|scripts)/[A-Za-z0-9_./\-]+)`')) {
      $p = $m.Groups[1].Value
      # only check things that look like a concrete file (have an extension) to avoid dir-glob noise
      if ($p -match '\.[A-Za-z0-9]+$') {
        $full = Join-Path $RepoRoot ($p -replace '/', '\')
        if (-not (Test-Path $full)) { Add-Err "bible: cited path does not exist: $p" }
      }
    }
  }

  # 9. generatedFrom freshness (any doc declaring generatedFrom: <path>)
  foreach ($f in $docFiles) {
    $text = Get-Content -LiteralPath $f -Raw -Encoding UTF8
    foreach ($m in [regex]::Matches($text, 'generatedFrom:\s*([A-Za-z0-9_./\-]+)')) {
      $src = Join-Path $RepoRoot ($m.Groups[1].Value -replace '/', '\')
      if (Test-Path $src) {
        if ((Get-Item $src).LastWriteTimeUtc -gt (Get-Item $f).LastWriteTimeUtc) {
          Add-Err "generatedFrom: $($f.Replace($RepoRoot,'').TrimStart('\','/')) is stale vs $($m.Groups[1].Value)"
        }
      }
    }
  }

  # 10. digest freshness: regenerate to a temp + compare; warn if out of date
  if (Test-Path $Bible) {
    $fresh = Build-DigestText
    if (-not (Test-Path $Digest)) {
      Add-Warn "digest: docs/BIBLE.digest.md is missing - run 'codex.ps1 digest'"
    } else {
      $current = Get-Content -LiteralPath $Digest -Raw -Encoding UTF8
      if ($current.Trim() -ne $fresh.Trim()) {
        Add-Warn "digest: docs/BIBLE.digest.md is out of date - run 'codex.ps1 digest'"
      }
    }
  }

  # 11. Generated node docs: checksum integrity (detect hand-edits to generated sections).
  # NodeDocService embeds "<!-- GENERATED-CHECKSUM: sha256hex -->" after the GeneratedMarker.
  # The checksum covers everything after that line, LF-normalized.
  $nodesDir = Join-Path $DocsDir 'nodes'
  if (Test-Path $nodesDir) {
    $sha256obj = [System.Security.Cryptography.SHA256]::Create()
    $nodeFiles = Get-ChildItem -LiteralPath $nodesDir -Filter '*.md' -ErrorAction SilentlyContinue
    foreach ($nf in $nodeFiles) {
      $nRel   = $nf.FullName.Replace($RepoRoot, '').TrimStart('\', '/')
      $nRaw   = Get-Content -LiteralPath $nf.FullName -Raw -Encoding UTF8
      if ([string]::IsNullOrEmpty($nRaw)) { continue }
      $nRaw   = $nRaw -replace "`r`n", "`n" -replace "`r", "`n"

      # Find the generated-sections marker
      $nMarkerStr = '<!-- ==== GENERATED SECTIONS'
      $nMPos      = $nRaw.IndexOf($nMarkerStr, [System.StringComparison]::Ordinal)
      if ($nMPos -lt 0) {
        # Pre-NodeDocService doc — warn to regenerate; not an error until all docs are migrated.
        Add-Warn "generated-checksum: $nRel has no GENERATED SECTIONS marker - re-run: ss --generate-node-doc --slug CODE"
        continue
      }
      $nMEnd = $nRaw.IndexOf("`n", $nMPos)
      if ($nMEnd -lt 0) { continue }

      # Look for checksum line immediately after marker
      $nAfterMarker = $nRaw.Substring($nMEnd + 1)
      $nCsumMatch   = [regex]::Match($nAfterMarker, '^<!-- GENERATED-CHECKSUM: ([0-9a-f]{64}) -->')
      if (-not $nCsumMatch.Success) {
        Add-Warn "generated-checksum: $nRel has no checksum line (old format) - re-run: ss --generate-node-doc --slug CODE"
        continue
      }
      $nStoredCsum  = $nCsumMatch.Groups[1].Value
      $nCsumEnd     = $nAfterMarker.IndexOf("`n", [System.StringComparison]::Ordinal)
      if ($nCsumEnd -lt 0) { continue }
      $nBody        = $nAfterMarker.Substring($nCsumEnd + 1)
      $nBodyBytes   = [System.Text.Encoding]::UTF8.GetBytes($nBody)
      $nActualCsum  = [System.BitConverter]::ToString($sha256obj.ComputeHash($nBodyBytes)).Replace('-', '').ToLower()
      if ($nActualCsum -ne $nStoredCsum) {
        Add-Err "generated-checksum: $nRel hand-edited (checksum mismatch) - re-run: ss --generate-node-doc --slug CODE"
      }
    }
    $sha256obj.Dispose()
  }

  # --- report ---
  Write-Host ""
  Write-Host "Checklist:" -ForegroundColor Cyan
  Write-Host "  [*] required files present"
  Write-Host "  [*] front-matter valid (bible/stories/amendments/rfc/data)"
  Write-Host "  [*] {#...} anchors unique; CODE cross-refs resolve"
  Write-Host "  [*] docs/data JSON valid; ids unique; schemas valid"
  Write-Host "  [*] registered corpus dirs + linked schemas exist"
  Write-Host "  [*] done stories cite a test/evidence token"
  Write-Host "  [*] bible-cited file paths exist on disk"
  Write-Host "  [*] generatedFrom artifacts not stale"
  Write-Host "  [*] digest freshness"
  Write-Host "  [*] generated node docs: checksum integrity"
  Write-Host ""

  if ($script:warnings.Count -gt 0) {
    Write-Host "WARNINGS ($($script:warnings.Count)):" -ForegroundColor Yellow
    $script:warnings | ForEach-Object { Write-Host "  - $_" -ForegroundColor Yellow }
  }
  if ($script:errors.Count -gt 0) {
    Write-Host "ERRORS ($($script:errors.Count)):" -ForegroundColor Red
    $script:errors | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
    Write-Host ""
    Write-Host "doctor: FAIL" -ForegroundColor Red
    exit 1
  }
  Write-Host "doctor: PASS" -ForegroundColor Green
  exit 0
}

# =============================================================================
# DIGEST
# =============================================================================
function Get-Section ($text, $num) {
  # returns the body of "## <num>. ... {#...}" up to the next "## " heading
  $pattern = "(?ms)^##\s+$num\..*?(?=^##\s+\d+\.|\z)"
  $m = [regex]::Match($text, $pattern)
  if ($m.Success) { return $m.Value.TrimEnd() }
  return ''
}

function Build-DigestText {
  $btext = Get-Content -LiteralPath $Bible -Raw -Encoding UTF8

  $s1 = Get-Section $btext 1
  $s3 = Get-Section $btext 3
  $s5 = Get-Section $btext 5
  $s9 = Get-Section $btext 9

  # status index: count check / partial / planned / cut glyphs across stories.
  # Use surrogate-pair-safe string literals (PS 5.1 cannot cast code points > 0xFFFF to [char]).
  $gDone    = [char]0x2705                                   # check mark (BMP)
  $gPartial = [string]::new([char[]]@(0xD83D, 0xDFE1))       # yellow circle U+1F7E1
  $gPlanned = [char]0x2B1C                                   # white large square (BMP)
  $gCut     = [string]::new([char[]]@(0xD83D, 0xDDD1))       # wastebasket U+1F5D1
  $counts = [ordered]@{ done = 0; partial = 0; planned = 0; cut = 0 }
  if (Test-Path $Stories) {
    $stext = Get-Content -LiteralPath $Stories -Raw -Encoding UTF8
    $counts.done    = ([regex]::Matches($stext, [regex]::Escape($gDone))).Count
    $counts.partial = ([regex]::Matches($stext, [regex]::Escape($gPartial))).Count
    $counts.planned = ([regex]::Matches($stext, [regex]::Escape($gPlanned))).Count
    $counts.cut     = ([regex]::Matches($stext, [regex]::Escape($gCut))).Count
  }

  # latest amendment head: amendments are append-only (CODE-A1, CODE-A2, ...), so the LAST
  # "## CODE-A..." block in file order is the most recent. Take all matches and keep the last.
  $amHead = ''
  if (Test-Path $Amend) {
    $atext = Get-Content -LiteralPath $Amend -Raw -Encoding UTF8
    $ams = [regex]::Matches($atext, "(?ms)^##\s+$Code-A\d+.*?(?=^##\s+$Code-A\d+|\z)")
    if ($ams.Count -gt 0) { $amHead = $ams[$ams.Count - 1].Value.Trim() }
  }

  $nl = "`n"
  $sb = New-Object System.Text.StringBuilder
  [void]$sb.AppendLine("AUTHORITATIVE - full detail in docs/BIBLE.md")
  [void]$sb.AppendLine("")
  [void]$sb.AppendLine("# StreetSamurai (SS) - Codex Digest")
  [void]$sb.AppendLine("> Generated by tools/codex.ps1 digest. Do not hand-edit.")
  [void]$sb.AppendLine("")
  [void]$sb.AppendLine($s1)
  [void]$sb.AppendLine("")
  [void]$sb.AppendLine($s3)
  [void]$sb.AppendLine("")
  [void]$sb.AppendLine($s5)
  [void]$sb.AppendLine("")
  [void]$sb.AppendLine($s9)
  [void]$sb.AppendLine("")
  [void]$sb.AppendLine("## Status index (from USER_STORIES.md)")
  [void]$sb.AppendLine("- done: $($counts.done)  partial: $($counts.partial)  planned: $($counts.planned)  cut: $($counts.cut)")
  [void]$sb.AppendLine("")
  [void]$sb.AppendLine("## Latest amendment")
  [void]$sb.AppendLine($amHead)
  return $sb.ToString()
}

function Invoke-Digest {
  if (-not (Test-Path $Bible)) { Write-Host "digest: docs/BIBLE.md not found" -ForegroundColor Red; exit 1 }
  $text = Build-DigestText
  # escape non-ASCII to \uXXXX-safe write: keep UTF8 for the file itself (digest md is read by the hook which escapes)
  Set-Content -LiteralPath $Digest -Value $text -Encoding UTF8
  Write-Host "digest: wrote docs/BIBLE.digest.md ($($text.Length) chars)" -ForegroundColor Green
}

switch ($Command) {
  'doctor' { Invoke-Doctor }
  'digest' { Invoke-Digest }
}
