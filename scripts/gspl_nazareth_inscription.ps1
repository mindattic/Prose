# Adds the Nazareth Inscription to "The Objects: What Survives" in all four Gospels,
# with its notes and a glossary entry.
#
# Why here: the chapter's subject is objects that were tested. Every Passion relic in it was
# dated (radiocarbon) and came back too late. The Nazareth Inscription belongs alongside them
# as the one case corrected a different way — not dated but SOURCED, by isotope analysis of the
# stone itself in 2020. It is also the strongest remaining gap in the corpus: it was for most of
# a century the most-cited documentary evidence for the empty tomb and had zero mentions.
#
# Inserted BEFORE the "Set them all in a row" synthesis, so that paragraph's claim about
# "every relic of the Passion submitted to a dateable test" stays true — the inscription is not
# a Passion relic and its test was not a date test.
#
# Idempotent: re-running detects the passage and skips. Anchor-based, fails loudly if the
# chapter has been restructured.

param([switch]$Apply)
$ErrorActionPreference = 'Stop'

$OutputEncoding = [System.Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
. "$PSScriptRoot\gspl_db.ps1"

$conn = Open-SS
$em = [char]8212

$books = @(
  @{ code='MATTHEW'; notes='019FA01D-FA22-76C6-976C-3EA4F4D54A14'; gloss='' }
  @{ code='MARK';    notes='019FA968-1B3B-75DC-84CF-0C7D9C4E783C'; gloss='' }
  @{ code='LUKE';    notes='019FA96B-18E2-7BB4-BAEB-11ACA24934F4'; gloss='' }
  @{ code='JOHN';    notes='019FA96D-7D48-75E0-9BD9-2190171276DC'; gloss='' }
)

$ANCHOR = 'Set them all in a row'
$MARKER = 'diatagma Kaisaros'

function Get-Chapter([string]$code, [string]$title) {
    $c = $conn.CreateCommand()
    $c.CommandText = @"
SELECT c.Id, bt.Id, bt.Text
FROM Nodes c JOIN Nodes p ON p.Id=c.ParentNodeId
JOIN BeatNodes bn ON bn.NodeId=c.Id JOIN Beats bt ON bt.Id=bn.BeatId
WHERE p.NodeCode=@Code AND c.Title=@T AND bn.IsEnabled=1
"@
    [void]$c.Parameters.AddWithValue('@Code', $code)
    [void]$c.Parameters.AddWithValue('@T', $title)
    $r = $c.ExecuteReader()
    $out = $null
    if ($r.Read()) { $out = [pscustomobject]@{ NodeId=$r.GetGuid(0); BeatId=$r.GetGuid(1); Text=$r.GetString(2) } }
    $r.Close()
    return $out
}

foreach ($b in $books) {
    Write-Host "=== $($b.code)"
    $obj = Get-Chapter $b.code 'The Objects: What Survives'
    if ($null -eq $obj) { throw "no Objects chapter for $($b.code)" }
    if ($obj.Text.Contains($MARKER)) { Write-Host "    already present, skipping"; continue }
    $at = $obj.Text.IndexOf($ANCHOR, [System.StringComparison]::Ordinal)
    if ($at -lt 0) { throw "anchor '$ANCHOR' not found in $($b.code) Objects chapter" }

    $n = [int](Invoke-SSScalar $conn @"
SELECT ISNULL(MAX(CAST(LEFT(bt.Text,CHARINDEX(' ',bt.Text)-1) AS INT)),0)
FROM BeatNodes bn JOIN Beats bt ON bt.Id=bn.BeatId
WHERE bn.NodeId=@N AND bn.IsEnabled=1
  AND CHARINDEX(' ',bt.Text)>1 AND LEFT(bt.Text,CHARINDEX(' ',bt.Text)-1) NOT LIKE '%[^0-9]%'
"@ @{ N = [guid]$b.notes }) + 1
    $nText = $n; $nProv = $n + 1; $nIso = $n + 2

    # Matthew alone carries the stolen-body accusation; the other three must not imply they do.
    if ($b.code -eq 'MATTHEW') {
        $tie = "Matthew alone records the accusation that the disciples stole the body while the guard slept (28:11-15). An imperial edict against precisely that crime, from precisely that town, would be external corroboration of a controversy this Gospel says was already circulating."
    } else {
        $tie = "The accusation that the disciples stole the body while the guard slept appears in Matthew alone (Matthew 28:11-15) and not in this Gospel $em which is worth saying plainly, because the inscription was routinely offered as corroboration for it."
    }

    $passage = @"
One object in this chapter is not a relic. It was never carried in procession, never kissed, never housed in a reliquary $em and for most of a century it was the single most-cited piece of documentary evidence for the empty tomb. It is a slab of white marble a little over two feet high, carrying twenty-two lines of Greek beneath the heading diatagma Kaisaros: ordinance of Caesar. The text orders that tombs and graves remain undisturbed in perpetuity, and prescribes a capital charge against anyone who breaks open a sepulchre, removes a body, or shifts it elsewhere with malicious intent [$nText].

The appeal is immediate. Here is a Roman emperor, apparently within the first half of the first century, making the removal of a body from a tomb a capital matter $em and the stone was said to have come from Nazareth. $tie

The provenance was always the weak point, and it was never concealed. Nobody excavated this stone. It was bought in 1878 for the private collection of the German antiquarian Wilhelm Froehner, whose own note recorded only that it had been sent from Nazareth: no findspot, no excavation, no witness. The collection passed to the Bibliothèque nationale in Paris in 1925, where the historian Michael Rostovtzeff noticed the text and brought it to Franz Cumont, who published the first edition and commentary in 1930 [$nProv]. Everything that rested on the words from Nazareth rested on a dealer's note about an object with no archaeological context whatsoever.

In 2020 the question was put to the stone instead of the paperwork. A team led by Kyle Harper and Michael McCormick measured stable carbon and oxygen isotopes in the marble and matched the signature to a particular quarry: the upper quarry on the Greek island of Kos, in the Aegean, roughly four hundred miles from Galilee [$nIso]. The authors propose that the edict is better explained by an outrage Kos itself remembers $em the desecration of the tomb of Nikias, a tyrant of the island, whose corpse was reportedly dug up and abused by the people he had governed.

What that settles, and what it does not, is worth stating precisely. It does not prove the edict had nothing to do with Judea; an emperor's ordinance could be cut into stone anywhere in his empire. It does remove the only reason anyone ever connected this stone to Nazareth, and it offers a rival occasion with a named victim and a location that matches the rock. The inscription remains a genuine Roman edict about tomb-robbing, plausibly first-century, and still valuable for what it shows about how seriously the empire treated graves. It has simply stopped being evidence about this story.

It is also a different kind of correction from the others here. The Shroud and the titulus were dated. This one was sourced. The question was not how old the object is but where it came from, and the answer arrived from a mass spectrometer ninety years after the argument started.

"@

    $newText = $obj.Text.Insert($at, $passage)
    Write-Host ("    passage inserted before the synthesis ({0} chars); notes {1}, {2}, {3}" -f $passage.Length, $nText, $nProv, $nIso)

    if (-not $Apply) { continue }

    [void](Invoke-SSNonQuery $conn "UPDATE Beats SET Text=@T, TextHash=@H, UpdatedAt=SYSUTCDATETIME() WHERE Id=@Id" `
        @{ T = $newText; H = (Get-SSSha256Hex $newText); Id = $obj.BeatId } -Expect 1 -What "$($b.code) Objects chapter")

    # ── notes ──
    $noteBodies = @(
      @{ num=$nText; title='The text of the Nazareth Inscription'
         body="Greek text, translation, and commentary in Franz Cumont, ""Un rescrit imperial sur la violation de sepulture,"" Revue historique (Paris, 1930). The inscription is headed diatagma Kaisaros, ordinance of Caesar, and orders that tombs and graves remain permanently undisturbed, prescribing a capital charge against anyone who breaks into a sepulchre, extracts a buried body, or removes it elsewhere with malicious intent. Two limits on what the stone itself supplies: no emperor is named anywhere in the text, and the inscription carries no internal date $em the first-century range usually assigned to it rests on the shapes of the letters, not on any statement in the document." }
      @{ num=$nProv; title='How the stone reached Paris, and what its provenance actually rests on'
         body="The slab was acquired in 1878 by the German antiquarian Wilhelm Froehner for his private collection; his catalogue note recorded only that it had been sent from Nazareth, giving no findspot, no excavation record, and no witness to its discovery. The Froehner collection passed to the Bibliothèque nationale de France in 1925, where the inscription is held in the Cabinet des Medailles. The historian Michael Rostovtzeff recognised the text's significance there and drew it to the attention of Franz Cumont, who published the first edition in Revue historique in 1930 (see note $nText). The Nazareth attribution therefore rested entirely on a dealer's unverified note about an unexcavated object $em a point conceded by both defenders and critics of the Nazareth reading long before 2020." }
      @{ num=$nIso; title='The 2020 isotope study that relocated the marble to Kos'
         body="Kyle Harper, Michael McCormick, Matthew Hamilton, Chantal Peiffert, Raymond Michels and colleagues, ""Establishing the provenance of the Nazareth Inscription: Using stable isotopes to resolve a historic controversy and trace ancient marble production,"" Journal of Archaeological Science: Reports (2020). Stable carbon and oxygen isotope analysis of the marble produced a signature $em enrichment in carbon-13 with marked depletion in oxygen-18 $em matching the upper quarry on the Greek island of Kos rather than any quarry in the southern Levant. The authors propose that the edict is most plausibly connected to the desecration of the tomb of Nikias, a tyrant of Kos, whose body was reportedly exhumed and abused after his death. The study establishes where the stone was quarried; it does not and cannot establish where the edict was promulgated, and the authors do not claim otherwise." }
    )
    foreach ($nb in $noteBodies) {
        $sk = [double](Invoke-SSScalar $conn "SELECT ISNULL(MAX(bn.SortKey),0)+50 FROM BeatNodes bn WHERE bn.NodeId=@N" @{ N=[guid]$b.notes })
        $bnum = [int](Invoke-SSScalar $conn "SELECT MAX(Number) FROM Beats" @{}) + 1
        $txt = "$($nb.num) $em $($nb.title)" + "`n`n" + $nb.body.Trim()
        $id = [guid]::NewGuid()
        [void](New-SSRow $conn 'Beats' @{ Id=$id; Text=$txt; TextHash=(Get-SSSha256Hex $txt); Act=0; SceneType='scene'; Kind='prose'
            Number=$bnum; Stale=0; WasCorrected=0; IsChapterStart=0; Version=0; EntityStale=0
            CreatedAt=[datetime]::UtcNow; UpdatedAt=[datetime]::UtcNow } -Quiet)
        [void](New-SSRow $conn 'BeatNodes' @{ NodeId=[guid]$b.notes; BeatId=$id; SortKey=$sk; IsEnabled=1 } -Quiet)
    }
    Write-Host "    3 notes appended"

    # ── glossary ──
    $gl = Get-Chapter $b.code 'Glossary'
    if ($null -ne $gl) {
        $glossHead = 'NAZARETH INSCRIPTION'
        $exists = [int](Invoke-SSScalar $conn "SELECT COUNT(*) FROM BeatNodes bn JOIN Beats bt ON bt.Id=bn.BeatId WHERE bn.NodeId=@N AND bn.IsEnabled=1 AND bt.Text LIKE @L" @{ N=$gl.NodeId; L="$glossHead%" })
        if ($exists -eq 0) {
            $body = "$glossHead" + "`n`n" + "A marble slab inscribed in Greek with an imperial edict $em headed diatagma Kaisaros, ordinance of Caesar $em forbidding the disturbance of tombs and prescribing a capital charge for removing a buried body. Bought in 1878 by the antiquarian Wilhelm Froehner with a note saying only that it came from Nazareth, and never excavated. For most of the twentieth century it was offered as documentary corroboration of the empty-tomb controversy. In 2020 stable-isotope analysis matched its marble to a quarry on the Greek island of Kos, removing the basis for the Nazareth attribution and suggesting a different occasion entirely: the desecrated tomb of Nikias, a tyrant of Kos. Still a real Roman edict about grave-robbing; no longer evidence about this story."
            $sk = [double](Invoke-SSScalar $conn "SELECT ISNULL(MAX(bn.SortKey),0)+50 FROM BeatNodes bn WHERE bn.NodeId=@N" @{ N=$gl.NodeId })
            $bnum = [int](Invoke-SSScalar $conn "SELECT MAX(Number) FROM Beats" @{}) + 1
            $id = [guid]::NewGuid()
            [void](New-SSRow $conn 'Beats' @{ Id=$id; Text=$body; TextHash=(Get-SSSha256Hex $body); Act=0; SceneType='scene'; Kind='prose'
                Number=$bnum; Stale=0; WasCorrected=0; IsChapterStart=0; Version=0; EntityStale=0
                CreatedAt=[datetime]::UtcNow; UpdatedAt=[datetime]::UtcNow } -Quiet)
            [void](New-SSRow $conn 'BeatNodes' @{ NodeId=$gl.NodeId; BeatId=$id; SortKey=$sk; IsEnabled=1 } -Quiet)
            Write-Host "    glossary entry added (re-sort A-Z separately if desired)"
        } else { Write-Host "    glossary entry already present" }
    }
    Write-Host "    APPLIED"
}

Write-Host ""
if ($Apply) { Write-Host "APPLIED." } else { Write-Host "DRY RUN - nothing written. Re-run with -Apply." }
$conn.Close()
