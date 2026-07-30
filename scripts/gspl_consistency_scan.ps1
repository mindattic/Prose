# Deterministic consistency / grammar / citation scan across the four Gospel books.
#
# No LLM, no opinions, no scores. Every finding is mechanically checkable and either a real
# defect or a deliberate exception you can rule on. Covers 100% of enabled beats.
#
# Checks, grouped:
#   MECHANICS  doubled words, multiple spaces, space before punctuation, missing space after a
#              sentence stop, unbalanced quotes/parens/brackets, lowercase sentence starts
#   CITATION   [N] style and placement (corpus convention is "...text [34]." - marker BEFORE
#              the stop), bracket digits that aren't note refs, notes cited zero times, refs
#              with no note
#   SOURCES    inconsistent forms of the same ancient work (Jewish War / Bellum Judaicum / BJ;
#              Antiquities / Ant.; Vita / Life), era style (BCE-CE vs BC-AD)
#   NAMES      spelling variants of recurring proper nouns across the corpus
#   REPEATS    sentences repeated verbatim within one book (copy-paste artefacts)
#   HEADINGS   chapter-title and note-header conventions
#
# Read-only. Prints a report; changes nothing.

param([string]$Book = 'all', [int]$Examples = 4)
$ErrorActionPreference = 'Stop'

$OutputEncoding = [System.Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
. "$PSScriptRoot\gspl_db.ps1"

$conn = Open-SS
$codes = if ($Book -eq 'all') { @('MATTHEW','MARK','LUKE','JOHN') } else { @($Book.ToUpper()) }
$em = [char]8212

$findings = New-Object System.Collections.ArrayList
function Add-Finding($cat, $book, $where, $detail) {
    [void]$findings.Add([pscustomobject]@{ Cat=$cat; Book=$book; Where=$where; Detail=$detail })
}

# recurring proper nouns whose spelling must not drift
$nameVariants = @(
    @{ canon='Yehohanan';  alts=@('Jehohanan','Yehohanon') }
    @{ canon='Sepphoris';  alts=@('Zippori','Tzipori') }
    @{ canon='Kafr Kanna'; alts=@('Kefr Kenna','Kafr Kana') }
    @{ canon='Josephus';   alts=@('Joesphus','Josephius') }
    @{ canon='Quirinius';  alts=@('Quirinus') }
    @{ canon='Eusebius';   alts=@('Eusebios') }
)
# Banias/Panias/Paneas deliberately NOT checked. Verified 2026-07-30: the books use them as
# distinct historical forms on purpose - "Caesarea Philippi, ancient Panias (modern Banias)",
# "Banias (an Arabic corruption of its ancient name, Paneas/Panias)". Correct, not drift.
$workVariants = @(
    @{ label='Josephus, Antiquities';  forms=@('Antiquities','Ant. ','Antiquitates') }
    @{ label='Josephus, Life/Vita';    forms=@('Vita ','Life ') }
)
# "Wars of the Jews" vs "Jewish War" deliberately NOT checked: both uses cite the WHISTON
# translation, whose published title is The Wars of the Jews, while the Loeb is The Jewish War.
# Naming each by its own title is right; normalising them would misquote the editions.

foreach ($code in $codes) {
    $cm = $conn.CreateCommand()
    $cm.CommandText = @"
SELECT c.Title, c.SortKey, bt.Text
FROM Nodes c JOIN Nodes p ON p.Id=c.ParentNodeId
JOIN BeatNodes bn ON bn.NodeId=c.Id JOIN Beats bt ON bt.Id=bn.BeatId
WHERE p.NodeCode=@C AND bn.IsEnabled=1
ORDER BY c.SortKey, bn.SortKey
"@
    [void]$cm.Parameters.AddWithValue('@C', $code)
    $rd = $cm.ExecuteReader()
    $beats = New-Object System.Collections.ArrayList
    while ($rd.Read()) {
        [void]$beats.Add([pscustomobject]@{ Ch=$rd.GetString(0); Text=$rd.GetString(2) })
    }
    $rd.Close()

    $whole = New-Object System.Text.StringBuilder
    $prose = New-Object System.Text.StringBuilder     # everything except Notes
    $sentenceSeen = @{}

    foreach ($b in $beats) {
        $t = $b.Text
        [void]$whole.Append($t); [void]$whole.Append("`n")
        if ($b.Ch -ne 'Notes') { [void]$prose.Append($t); [void]$prose.Append("`n") }
        $ch = $b.Ch
        if ($ch.Length -gt 30) { $ch = $ch.Substring(0,30) }

        # ── MECHANICS ──
        # Same-line only: [ \t]+ rather than \s+. Crossing a newline matched every glossary
        # entry, whose ALL-CAPS heading is repeated as the first word of its body
        # ("ISAIAH\n\nIsaiah of Jerusalem..."), and every gazetteer group heading.
        # "that that" and "had had" are legitimate English; flag the rest.
        foreach ($m in [regex]::Matches($t, '\b(\w+)[ \t]+\1\b', 'IgnoreCase')) {
            # "in In Flaccum" is correct: preposition followed by Philo's actual title.
            if ($m.Groups[1].Value -notmatch '^(that|had|is|in)$') {
                Add-Finding 'MECHANICS' $code $ch ("doubled word: '" + $m.Value + "'")
            }
        }
        foreach ($m in [regex]::Matches($t, '\S {2,}\S')) {
            Add-Finding 'MECHANICS' $code $ch ("multiple spaces: '" + ($m.Value -replace ' ','_') + "'")
        }
        # Horizontal whitespace only, and never before an ellipsis. All 10 original hits were
        # ellipses inside quotations ("Woe to you, Bethsaida! ... it will be more tolerable"),
        # which is correct usage, not a spacing defect.
        foreach ($m in [regex]::Matches($t, '\S[ \t]+[,;:!?](?!\.)|\S[ \t]+\.(?!\.)')) {
            Add-Finding 'MECHANICS' $code $ch ("space before punctuation: '" + $m.Value + "'")
        }
        foreach ($m in [regex]::Matches($t, '[a-z]{2}\.[A-Z][a-z]')) {
            Add-Finding 'MECHANICS' $code $ch ("missing space after stop: '" + $m.Value + "'")
        }
        $dq = ([regex]::Matches($t, '"')).Count
        if ($dq % 2 -ne 0) { Add-Finding 'MECHANICS' $code $ch "odd number of double quotes ($dq)" }
        $op = ([regex]::Matches($t, '\(')).Count; $cl = ([regex]::Matches($t, '\)')).Count
        if ($op -ne $cl) { Add-Finding 'MECHANICS' $code $ch "unbalanced parentheses ($op open, $cl close)" }
        # Epigraphic transcriptions legitimately carry an unpaired bracket: "...]S TIBERIEUM"
        # on the Pilate Stone marks a lacuna. Only flag when no such transcription is present.
        $ob = ([regex]::Matches($t, '\[')).Count; $cb = ([regex]::Matches($t, '\]')).Count
        if ($ob -ne $cb -and $t -notmatch '\.\.\.\]|\[\.\.\.') {
            Add-Finding 'MECHANICS' $code $ch "unbalanced brackets ($ob open, $cb close)"
        }

        # ── CITATION ──
        # convention: marker before the stop -> "...text [34]."   defect: "...text. [34]"
        # [^\s.!?] before the stop so an ellipsis ("poor... [24] woe") is not reported - the
        # marker legitimately follows an elision.
        foreach ($m in [regex]::Matches($t, '[^\s.!?][.!?]\s+\[\d+\]')) {
            Add-Finding 'CITATION' $code $ch ("note marker after the sentence stop: '" + ($m.Value -replace "`n",' ') + "'")
        }
        # NOT checked: adjacent markers like "[261] [262]." Verified 2026-07-30 to be
        # legitimate multi-citation (two notes supporting one claim), correctly placed before
        # the stop. Flagging them buried the 28 real placement defects under 75 false ones.
        # bracketed number that is not a note ref (e.g. a stray "[12" or "[1 ]").
        # Excludes editorial brackets in citations - "Eerdmans, 2017 [1st ed. 2006]" is correct
        # bibliographic form, not a malformed marker.
        foreach ($m in [regex]::Matches($t, '\[\s*\d+\s+\]|\[\d+(?!(?:st|nd|rd|th)\b)[^\]\d]')) {
            Add-Finding 'CITATION' $code $ch ("malformed note marker: '" + $m.Value + "'")
        }
    }

    $wholeS = $whole.ToString()
    $proseS = $prose.ToString()

    # ── SOURCES: inconsistent citation forms for the same work ──
    foreach ($w in $workVariants) {
        $present = @()
        foreach ($f in $w.forms) { $n = ([regex]::Matches($wholeS, [regex]::Escape($f))).Count; if ($n -gt 0) { $present += ("$($f.Trim())=$n") } }
        if ($present.Count -gt 1) {
            Add-Finding 'SOURCES' $code '(book-wide)' ("$($w.label) cited in " + $present.Count + " different forms: " + ($present -join ', '))
        }
    }
    # Era style: the series uses BCE/CE in body prose. BC/AD inside a CITED TITLE is correct and
    # must stay (Schurer's "(175 B.C.-A.D. 135)", Goldsworthy's "100 BC-AD 200", the RINAP
    # volumes' reign dates), so count only BC/AD outside a bibliographic context.
    $titleCtx = '(trans\.|ed\.\)|Rev\. ed|Oxford|Cambridge|Press|Monographs|Library|Loeb|University|Anchor Bible|Doubleday|HarperOne|Brill|Eerdmans|Winona Lake|Eisenbrauns|RINAP|Royal Inscriptions|The History of|The Roman Army|vol\.|ISBN)'
    $bce = ([regex]::Matches($wholeS, '\bBCE\b')).Count + ([regex]::Matches($wholeS, '\bCE\b')).Count
    $bcadProse = 0
    # (?!\.?E) so the dotted "63 B.C.E.-70 C.E." is not counted as a bare BC.
    foreach ($m in [regex]::Matches($wholeS, '\bB\.?C\.?\b(?!\.?E)|\bA\.?D\.?\b(?!\.?E)')) {
        $s = [Math]::Max(0, $m.Index - 150)
        $ctx = $wholeS.Substring($s, [Math]::Min(300, $wholeS.Length - $s))
        if ($ctx -notmatch $titleCtx) { $bcadProse++ }
    }
    if ($bce -gt 0 -and $bcadProse -gt 0) {
        Add-Finding 'SOURCES' $code '(book-wide)' "era style mixed in BODY PROSE: BCE/CE $bce, BC/AD $bcadProse (titles excluded)"
    }

    # ── NAMES ──
    foreach ($nv in $nameVariants) {
        $canonN = ([regex]::Matches($wholeS, "\b$([regex]::Escape($nv.canon))")).Count
        foreach ($alt in $nv.alts) {
            $altN = ([regex]::Matches($wholeS, "\b$([regex]::Escape($alt))")).Count
            if ($altN -gt 0 -and $canonN -gt 0) {
                Add-Finding 'NAMES' $code '(book-wide)' "'$($nv.canon)' ($canonN) also appears as '$alt' ($altN)"
            }
        }
    }

    # ── REPEATS: identical sentences within the book ──
    # Don't split after an abbreviation, or "Church of St. Anne" ends a sentence and three
    # different sentences sharing an opening clause get reported as one repeated sentence.
    foreach ($s in [regex]::Split($proseS, '(?<=[.!?])(?<!\b(?:St|Mt|Dr|Mr|Mrs|Ms|vol|ed|trans|ca|c|no|pp|p)\.)\s+')) {
        $k = $s.Trim()
        if ($k.Length -lt 60) { continue }
        if ($sentenceSeen.ContainsKey($k)) { $sentenceSeen[$k]++ } else { $sentenceSeen[$k] = 1 }
    }
    foreach ($k in ($sentenceSeen.Keys | Where-Object { $sentenceSeen[$_] -gt 1 })) {
        Add-Finding 'REPEATS' $code '(book-wide)' ("sentence appears $($sentenceSeen[$k])x: " + $k.Substring(0,[Math]::Min(90,$k.Length)))
    }

    # ── HEADINGS ──
    foreach ($b in $beats) {
        if ($b.Ch -like 'Chapter*' -and $b.Ch -notmatch "^Chapter \d+( $em .+)?$") {
            Add-Finding 'HEADINGS' $code $b.Ch 'chapter title does not match "Chapter N" or "Chapter N (em dash) Title"'
        }
    }
    $cn2 = $conn.CreateCommand()
    $cn2.CommandText = "SELECT bt.Text FROM Nodes c JOIN Nodes p ON p.Id=c.ParentNodeId JOIN BeatNodes bn ON bn.NodeId=c.Id JOIN Beats bt ON bt.Id=bn.BeatId WHERE p.NodeCode=@C AND c.Title='Notes' AND bn.IsEnabled=1"
    [void]$cn2.Parameters.AddWithValue('@C', $code)
    $r3 = $cn2.ExecuteReader()
    while ($r3.Read()) {
        $first = ($r3.GetString(0) -split "`n")[0]
        if ($first -notmatch "^\d+ $em .+") { Add-Finding 'HEADINGS' $code 'Notes' ("note header not 'N (em dash) Title': " + $first.Substring(0,[Math]::Min(60,$first.Length))) }
    }
    $r3.Close()
}

# ── report ──
Write-Host ""
Write-Host ("TOTAL FINDINGS: {0}" -f $findings.Count)
Write-Host ""
foreach ($cat in @('CITATION','MECHANICS','SOURCES','NAMES','REPEATS','HEADINGS')) {
    $inCat = @($findings | Where-Object Cat -eq $cat)
    if ($inCat.Count -eq 0) { Write-Host ("{0,-10} clean" -f $cat); continue }
    Write-Host ("{0,-10} {1}" -f $cat, $inCat.Count)
    foreach ($g in ($inCat | Group-Object Detail | Sort-Object Count -Descending)) {
        $books = (($g.Group | ForEach-Object { $_.Book }) | Sort-Object -Unique) -join ','
        Write-Host ("    x{0,-4} [{1}] {2}" -f $g.Count, $books, $g.Name)
        if ($g.Count -le $Examples) {
            foreach ($f in ($g.Group | Select-Object -First $Examples)) { Write-Host ("            in {0} / {1}" -f $f.Book, $f.Where) }
        }
    }
    Write-Host ""
}
$conn.Close()
